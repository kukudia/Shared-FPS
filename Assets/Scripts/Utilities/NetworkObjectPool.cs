using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Fusion;

namespace Projectiles
{
    /// <summary>
    /// NetworkObject 对象池
    /// 【修复版本】正确处理场景切换时的对象生命周期
    /// </summary>
    public class NetworkObjectPool : Fusion.Behaviour, INetworkObjectProvider
    {
        public SceneContext Context { get; set; }

        [Header("Pool Settings")]
        [Tooltip("是否在场景切换时保持池中对象（使用 DontDestroyOnLoad）")]
        [SerializeField] private bool persistAcrossScenes = true;

        private Dictionary<NetworkPrefabId, Stack<NetworkObject>> _cached = new(32);
        private Dictionary<NetworkObject, NetworkPrefabId> _borrowed = new();

        // 跟踪哪些对象被标记为 DontDestroyOnLoad
        private HashSet<NetworkObject> _persistentObjects = new();

        private void Awake()
        {
            // 监听场景卸载事件，用于清理无效引用
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }

        private void OnSceneUnloaded(UnityEngine.SceneManagement.Scene scene)
        {
            CleanupDestroyedReferences();
        }

        NetworkObjectAcquireResult INetworkObjectProvider.AcquirePrefabInstance(NetworkRunner runner, in NetworkPrefabAcquireContext context, out NetworkObject result)
        {
            if (_cached.TryGetValue(context.PrefabId, out var objects) == false)
            {
                objects = _cached[context.PrefabId] = new Stack<NetworkObject>();
            }

            // 尝试从池中获取有效对象（跳过已被销毁的对象）
            while (objects.Count > 0)
            {
                var oldInstance = objects.Pop();

                // 检查对象是否已被销毁（场景切换时可能发生）
                // 使用 try-catch 是因为 Unity 的 == null 检查有时不够可靠
                if (oldInstance == null)
                {
                    continue;
                }

                try
                {
                    // 额外检查：尝试访问 gameObject 来确保对象真正存在
                    if (oldInstance.gameObject == null)
                    {
                        continue;
                    }
                }
                catch (MissingReferenceException)
                {
                    // 对象已被销毁但引用还在
                    Debug.LogWarning($"[NetworkObjectPool] Skipping destroyed object for PrefabId {context.PrefabId}");
                    continue;
                }

                // 从 persistent 集合中移除（因为现在被借出使用了）
                _persistentObjects.Remove(oldInstance);
                _borrowed[oldInstance] = context.PrefabId;

#if UNITY_EDITOR
                try
                {
                    var originalPrefab = runner.Config.PrefabTable.Load(context.PrefabId, true);
                    if (originalPrefab != null)
                    {
                        oldInstance.name = originalPrefab.name;
                    }
                }
                catch (MissingReferenceException)
                {
                    // 对象在设置名称时被销毁，跳过这个对象
                    _borrowed.Remove(oldInstance);
                    continue;
                }
#endif

                try
                {
                    oldInstance.gameObject.SetActive(true);
                }
                catch (MissingReferenceException)
                {
                    // 对象在激活时被销毁，跳过这个对象
                    _borrowed.Remove(oldInstance);
                    continue;
                }

                result = oldInstance;
                return NetworkObjectAcquireResult.Success;
            }

            // 池中没有可用对象，创建新实例
            var original = runner.Config.PrefabTable.Load(context.PrefabId, true);
            if (original == null)
            {
                result = default;
                return NetworkObjectAcquireResult.Failed;
            }

            var instance = Instantiate(original);

            // 如果需要跨场景保持，设置 DontDestroyOnLoad
            if (persistAcrossScenes)
            {
                DontDestroyOnLoad(instance.gameObject);
            }
            else
            {
                runner.MoveToRunnerScene(instance.gameObject);
            }

#if UNITY_EDITOR
            instance.name = original.name;
#endif

            _borrowed[instance] = context.PrefabId;

            AssignContext(instance);

            for (int i = 0; i < instance.NestedObjects.Length; i++)
            {
                AssignContext(instance.NestedObjects[i]);
            }

            result = instance;
            return NetworkObjectAcquireResult.Success;
        }

        void INetworkObjectProvider.ReleaseInstance(NetworkRunner runner, in NetworkObjectReleaseContext context)
        {
            if (context.IsNestedObject == true)
                return;

            NetworkObject instance = context.Object;
            if (instance == null)
                return;

            if (instance.NetworkTypeId.IsSceneObject == false && runner.IsShutdown == false)
            {
                if (_borrowed.TryGetValue(instance, out var prefabID) == true)
                {
                    _borrowed.Remove(instance);
                    _cached[prefabID].Push(instance);

                    instance.gameObject.SetActive(false);
                    instance.transform.parent = null;
                    instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

                    // 确保缓存的对象保持在 DontDestroyOnLoad
                    if (persistAcrossScenes)
                    {
                        DontDestroyOnLoad(instance.gameObject);
                        _persistentObjects.Add(instance);
                    }

#if UNITY_EDITOR
                    instance.name = $"(Cached) {instance.name}";
#endif
                }
                else
                {
                    Destroy(instance.gameObject);
                }
            }
            else
            {
                Destroy(instance.gameObject);
            }
        }

        NetworkPrefabId INetworkObjectProvider.GetPrefabId(NetworkRunner runner, NetworkObjectGuid prefabGuid)
        {
            return runner.Prefabs.GetId(prefabGuid);
        }

        /// <summary>
        /// 清理对象池，在场景切换前调用可以避免引用已销毁对象的问题
        /// </summary>
        public void ClearPool()
        {
            // 销毁所有缓存的对象
            foreach (var kvp in _cached)
            {
                var stack = kvp.Value;
                while (stack.Count > 0)
                {
                    var obj = stack.Pop();
                    if (obj != null)
                    {
                        _persistentObjects.Remove(obj);
                        Destroy(obj.gameObject);
                    }
                }
            }
            _cached.Clear();

            // 清理 borrowed 字典中已销毁的引用
            CleanupBorrowedReferences();
        }

        /// <summary>
        /// 清理所有已被销毁的缓存引用（不销毁有效对象）
        /// </summary>
        public void CleanupDestroyedReferences()
        {
            // 清理 _cached 中的无效引用
            foreach (var kvp in _cached)
            {
                var stack = kvp.Value;
                var validObjects = new Stack<NetworkObject>();

                while (stack.Count > 0)
                {
                    var obj = stack.Pop();
                    if (obj != null)
                    {
                        validObjects.Push(obj);
                    }
                    else
                    {
                        Debug.Log("[NetworkObjectPool] Removed destroyed object reference from cache");
                    }
                }

                // 将有效对象放回栈中
                while (validObjects.Count > 0)
                {
                    stack.Push(validObjects.Pop());
                }
            }

            // 清理 borrowed 和 persistent 集合
            CleanupBorrowedReferences();
            _persistentObjects.RemoveWhere(obj => obj == null);
        }

        private void CleanupBorrowedReferences()
        {
            var keysToRemove = new List<NetworkObject>();
            foreach (var kvp in _borrowed)
            {
                if (kvp.Key == null)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
            foreach (var key in keysToRemove)
            {
                _borrowed.Remove(key);
            }
        }

        private void AssignContext(NetworkObject instance)
        {
            for (int i = 0, count = instance.NetworkedBehaviours.Length; i < count; i++)
            {
                if (instance.NetworkedBehaviours[i] is IContextBehaviour cachedBehaviour)
                {
                    cachedBehaviour.Context = Context;
                }
            }
        }

        /// <summary>
        /// 获取池统计信息（调试用）
        /// </summary>
        public string GetPoolStats()
        {
            int totalCached = 0;
            foreach (var kvp in _cached)
            {
                totalCached += kvp.Value.Count;
            }
            return $"Cached: {totalCached}, Borrowed: {_borrowed.Count}, Persistent: {_persistentObjects.Count}";
        }
    }
}