using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Projectiles.UI;

namespace Projectiles
{
    /// <summary>
    /// Handles general game input and debug input - cursor locking, peer switching.
    /// 【修复版本】支持场景切换后重新获取引用
    /// </summary>
    public class GeneralInput : MonoBehaviour
    {
        // PUBLIC MEMBERS

        public bool IsLocked => Cursor.lockState == CursorLockMode.Locked;

        // PRIVATE MEMBERS
        private UIScreenEffects _uIScreenEffects;
        private static int _lastSingleInputChange;
        private static int _cursorLockRequests;

        private PlayerAgent _agent;

        // PUBLIC METHODS

        public void RequestCursorLock()
        {
            // Static requests count is used for multi-peer setup
            _cursorLockRequests++;
            //Debug.Log($"[GeneralInput] RequestCursorLock - requests count: {_cursorLockRequests}");

            if (_cursorLockRequests == 1)
            {
                // First lock request, let's lock
                //Debug.Log("[GeneralInput] First lock request, locking cursor...");
                SetLockedState(true);
            }
        }

        public void RequestCursorRelease()
        {
            _cursorLockRequests--;
            //Debug.Log($"[GeneralInput] RequestCursorRelease - requests count: {_cursorLockRequests}");

            Assert.Check(_cursorLockRequests >= 0, "Cursor lock requests are negative, this should not happen");

            if (_cursorLockRequests == 0)
            {
                SetLockedState(false);
            }
        }

        /// <summary>
        /// 强制刷新引用（场景切换后调用）
        /// </summary>
        public void RefreshReferences()
        {
            _agent = null;
            _uIScreenEffects = null;
            FindReferences();
        }

        /// <summary>
        /// 强制锁定光标（游戏开始后调用）
        /// </summary>
        public void ForceLockCursor()
        {
            //Debug.Log("[GeneralInput] ForceLockCursor called");
            RefreshReferences();

            if (_agent != null && _agent.gameStart)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                //Debug.Log("[GeneralInput] Cursor force locked");
            }
            else
            {
                Debug.LogWarning($"[GeneralInput] ForceLockCursor - Cannot lock: _agent={(_agent != null ? _agent.name : "NULL")}, gameStart={(_agent != null ? _agent.gameStart.ToString() : "N/A")}");
            }
        }

        // MONOBEHAVIOUR

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
        {
            //Debug.Log($"[GeneralInput] Scene loaded: {scene.name}, refreshing references...");
            // 延迟刷新引用，确保新场景对象已初始化
            StartCoroutine(RefreshReferencesDelayed());
        }

        private System.Collections.IEnumerator RefreshReferencesDelayed()
        {
            yield return null; // 等待一帧
            RefreshReferences();
        }

        private void Update()
        {
            // 确保引用有效
            FindReferences();

            // Only one single input change per frame is possible (important for multi-peer multi-input game)
            if (_lastSingleInputChange == Time.frameCount)
                return;

            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            // Enter key is used for locking/unlocking cursor in game view
            if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame)
            {
                SetLockedState(Cursor.lockState != CursorLockMode.Locked);
                _lastSingleInputChange = Time.frameCount;
            }

            // Check switching peer in multi-peer mode
            if (keyboard.numpad0Key.wasPressedThisFrame || keyboard.uKey.wasPressedThisFrame)
            {
                SetActiveRunner(-1);
            }
            else if (keyboard.numpad1Key.wasPressedThisFrame || keyboard.iKey.wasPressedThisFrame)
            {
                SetActiveRunner(0);
            }
            else if (keyboard.numpad2Key.wasPressedThisFrame || keyboard.oKey.wasPressedThisFrame)
            {
                SetActiveRunner(1);
            }
            else if (keyboard.numpad3Key.wasPressedThisFrame || keyboard.pKey.wasPressedThisFrame)
            {
                SetActiveRunner(2);
            }
        }

        // PRIVATE METHODS

        private void FindReferences()
        {
            if (_uIScreenEffects == null)
            {
                _uIScreenEffects = FindFirstObjectByType<UIScreenEffects>();
            }

            if (_agent == null)
            {
                // 查找本地玩家的 PlayerAgent
                var agents = FindObjectsByType<PlayerAgent>(FindObjectsSortMode.None);
                //Debug.Log($"[GeneralInput] FindReferences - Found {agents.Length} PlayerAgents in scene");

                foreach (var agent in agents)
                {
                    bool hasObject = agent.Object != null;
                    bool hasInputAuth = hasObject && agent.HasInputAuthority;
                    //Debug.Log($"[GeneralInput]   - Agent: {agent.name}, Object: {(hasObject ? "Valid" : "NULL")}, HasInputAuthority: {hasInputAuth}, gameStart: {agent.gameStart}");

                    if (hasObject && hasInputAuth)
                    {
                        _agent = agent;
                        Debug.Log($"[GeneralInput] Found local PlayerAgent: {agent.name}");
                        break;
                    }
                }

                // 如果没找到有输入权限的，就用第一个
                if (_agent == null && agents.Length > 0)
                {
                    _agent = agents[0];
                    Debug.Log($"[GeneralInput] No local agent found, using first: {_agent.name}");
                }

                if (_agent == null)
                {
                    Debug.LogWarning("[GeneralInput] No PlayerAgent found in scene!");
                }
            }
        }

        private void SetLockedState(bool value)
        {
            Debug.Log($"[GeneralInput] SetLockedState called with value={value}");
            FindReferences();

            // 检查游戏是否已开始
            bool gameStarted = _agent != null && _agent.IsGameStartedSafe;

            Debug.Log($"[GeneralInput] SetLockedState - _agent: {(_agent != null ? _agent.name : "NULL")}, gameStarted: {gameStarted}");

            if (value && !gameStarted)
            {
                // 游戏未开始时不锁定光标
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                Debug.Log("[GeneralInput] Game not started, cursor remains unlocked");
            }
            else
            {
                Cursor.lockState = value ? CursorLockMode.Locked : CursorLockMode.None;
                Cursor.visible = !value;
                Debug.Log($"[GeneralInput] Setting cursor - lockState: {Cursor.lockState}, visible: {Cursor.visible}");
            }

            if (_uIScreenEffects != null)
            {
                _uIScreenEffects.OnPause(!value);
            }

            Debug.Log($"[GeneralInput] Final cursor state - lockState: {Cursor.lockState}, visible: {Cursor.visible}, gameStarted: {gameStarted}");
        }

        private void SetActiveRunner(int index)
        {
            var enumerator = NetworkRunner.GetInstancesEnumerator();

            int currentIndex = -1;
            while (enumerator.MoveNext() == true)
            {
                var runner = enumerator.Current;

                // Skip temporary runner
                if (runner.LocalPlayer.IsRealPlayer == false)
                    continue;

                currentIndex++;

                runner.SetVisible(index < 0 || currentIndex == index);
                runner.ProvideInput = index < 0 || currentIndex == index;
            }
        }
    }
}