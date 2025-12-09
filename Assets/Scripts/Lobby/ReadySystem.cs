using System;
using System.Collections;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Projectiles
{
    /// <summary>
    /// 准备系统 - 处理玩家准备状态的网络同步
    /// </summary>
    public class ReadySystem : ContextBehaviour
    {
        // 事件
        public event Action OnReadyStateChanged;
        public event Action OnGameStarted;

        [Header("Settings")]
        [SerializeField] private int maxPlayers = 4;
        public string gameplaySceneName = "Gameplay";

        [Header("Scene Loading Options")]
        [Tooltip("是否使用 Additive 模式加载场景（保持玩家对象）")]
        [SerializeField] private bool useAdditiveSceneLoading = true;

        [Tooltip("加载新场景后是否卸载当前场景")]
        [SerializeField] private bool unloadCurrentScene = true;

        [Tooltip("当前大厅场景名称（用于卸载）")]
        [SerializeField] private string lobbySceneName = "Lobby";

        // 网络同步的准备状态字典
        [Networked, Capacity(16)]
        private NetworkDictionary<PlayerRef, NetworkBool> ReadyStates { get; }

        // 游戏是否已开始
        [Networked]
        public NetworkBool GameStarted { get; private set; }

        // 场景是否正在加载
        [Networked]
        private NetworkBool IsLoadingScene { get; set; }

        // 属性
        public int MaxPlayers => maxPlayers;
        public bool IsHost => Runner != null && Runner.IsServer;

        // PUBLIC METHODS

        public int GetReadyCount()
        {
            int count = 0;
            foreach (var kvp in ReadyStates)
            {
                if (kvp.Value) count++;
            }
            return count;
        }

        public int GetPlayerCount() => ReadyStates.Count;

        public bool IsLocalPlayerReady()
        {
            if (Runner == null) return false;
            return ReadyStates.TryGet(Runner.LocalPlayer, out var isReady) && isReady;
        }

        public bool AreAllPlayersReady()
        {
            //if (ReadyStates.Count <= 1) return false;

            foreach (var kvp in ReadyStates)
            {
                if (IsPlayerHost(kvp.Key)) continue;
                if (!kvp.Value) return false;
            }
            return true;
        }

        public bool IsPlayerHost(PlayerRef player)
        {
            if (Runner == null) return false;
            if (Runner.IsServer && player == Runner.LocalPlayer) return true;
            return Object.StateAuthority == player;
        }

        public bool IsLocalPlayerHost()
        {
            if (Runner == null) return false;
            return IsPlayerHost(Runner.LocalPlayer);
        }

        public void ToggleReady()
        {
            if (Runner == null || GameStarted || IsLocalPlayerHost()) return;
            RPC_ToggleReady(Runner.LocalPlayer);
        }

        public void StartGame()
        {
            if (!IsLocalPlayerHost() || !AreAllPlayersReady()) return;
            RPC_StartGame();
        }

        // NetworkBehaviour INTERFACE

        public override void Spawned()
        {
            Context.ReadySystem = this;

            if (HasStateAuthority)
            {
                GameStarted = false;
                IsLoadingScene = false;
            }

            if (Runner.LocalPlayer != PlayerRef.None)
            {
                RPC_RegisterPlayer(Runner.LocalPlayer);
            }

            Debug.Log($"[ReadySystem] Spawned - IsServer: {Runner.IsServer}, LocalPlayer: {Runner.LocalPlayer}");
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (Context != null) Context.ReadySystem = null;
        }

        // RPC METHODS

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_RegisterPlayer(PlayerRef player)
        {
            if (!HasStateAuthority) return;

            if (!ReadyStates.ContainsKey(player))
            {
                bool isHost = IsPlayerHost(player);
                ReadyStates.Add(player, isHost);
                Debug.Log($"[ReadySystem] Player {player} registered (IsHost: {isHost})");
            }

            RPC_NotifyReadyStateChanged();
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_UnregisterPlayer(PlayerRef player)
        {
            if (!HasStateAuthority) return;

            if (ReadyStates.ContainsKey(player))
            {
                ReadyStates.Remove(player);
                Debug.Log($"[ReadySystem] Player {player} unregistered");
            }

            RPC_NotifyReadyStateChanged();
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_ToggleReady(PlayerRef player)
        {
            if (!HasStateAuthority || GameStarted || IsPlayerHost(player)) return;

            if (ReadyStates.TryGet(player, out var currentState))
            {
                ReadyStates.Set(player, !currentState);
                Debug.Log($"[ReadySystem] Player {player} ready state: {!currentState}");
            }

            RPC_NotifyReadyStateChanged();
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_NotifyReadyStateChanged()
        {
            OnReadyStateChanged?.Invoke();
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_StartGame()
        {
            if (!HasStateAuthority || GameStarted || !AreAllPlayersReady() || IsLoadingScene) return;

            GameStarted = true;
            IsLoadingScene = true;
            Debug.Log("[ReadySystem] Game Starting!");

            RPC_OnGameStart();
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_OnGameStart()
        {
            Debug.Log($"[ReadySystem] RPC_OnGameStart received - HasStateAuthority: {HasStateAuthority}, Runner: {(Runner != null ? Runner.name : "NULL")}");

            // 标记所有玩家对象为跨场景保持
            Debug.Log("[ReadySystem] Marking players as persistent...");
            MarkPlayersAsPersistent();

            // 刷新并锁定光标（确保输入正常工作）
            Debug.Log("[ReadySystem] Refreshing input after game start...");
            //RefreshInputAfterGameStart();

            // 触发事件，隐藏UI
            Debug.Log("[ReadySystem] Invoking OnGameStarted event...");
            OnGameStarted?.Invoke();

            // 启动所有PlayerAgent
            //Debug.Log("[ReadySystem] Starting all PlayerAgents...");
            //StartAllPlayerAgents();

            LoadGameplayScene();
        }

        /// <summary>
        /// 游戏开始后刷新输入系统
        /// </summary>
        private void RefreshInputAfterGameStart()
        {
            if (Context == null || Context.GeneralInput == null)
            {
                Debug.LogWarning("[ReadySystem] Context.GeneralInput is null, cannot refresh input");
                return;
            }

            // 刷新 GeneralInput 的 PlayerAgent 引用并锁定光标
            Context.GeneralInput.ForceLockCursor();
            Debug.Log("[ReadySystem] Input refreshed and cursor locked");
        }

        // PRIVATE METHODS

        /// <summary>
        /// 将所有玩家对象标记为跨场景保持
        /// </summary>
        private void MarkPlayersAsPersistent()
        {
            if (Context.Gameplay == null) return;

            foreach (var kvp in Context.Gameplay.Players)
            {
                var player = kvp.Value;
                if (player != null && player.Object != null)
                {
                    // 使用 Fusion 的方法将对象移动到 Runner 场景（DontDestroyOnLoad）
                    Runner.MoveToRunnerScene(player.gameObject);

                    if (player.ActiveAgent != null)
                    {
                        Runner.MoveToRunnerScene(player.ActiveAgent.gameObject);
                        Debug.Log($"[ReadySystem] Marked PlayerAgent {player.ActiveAgent.name} as persistent");
                    }

                    Debug.Log($"[ReadySystem] Marked Player {kvp.Key} as persistent");
                }
            }

            // 标记 ReadySystem 和 Gameplay 为跨场景保持
            Runner.MoveToRunnerScene(this.gameObject);

            if (Context.Gameplay != null)
            {
                Runner.MoveToRunnerScene(Context.Gameplay.gameObject);
            }
        }

        private void LoadGameplayScene()
        {
            if (!HasStateAuthority || string.IsNullOrEmpty(gameplaySceneName)) return;

            Debug.Log($"[ReadySystem] Loading scene: {gameplaySceneName}, Additive: {useAdditiveSceneLoading}");

            Runner.LoadScene(gameplaySceneName, LoadSceneMode.Additive);

            //if (useAdditiveSceneLoading)
            //{
            //    // 使用 Additive 模式加载（推荐）- 现有的 NetworkObject 不会被销毁
            //    Runner.LoadScene(gameplaySceneName, LoadSceneMode.Additive);

            //    // 延迟卸载当前场景
            //    if (unloadCurrentScene && !string.IsNullOrEmpty(lobbySceneName))
            //    {
            //        StartCoroutine(UnloadLobbySceneDelayed());
            //    }
            //}
            //else
            //{
            //    // 普通场景加载 - 需要配合 NetworkObjectPool 的修复使用
            //    Runner.LoadScene(gameplaySceneName);
            //}
        }

        private IEnumerator UnloadLobbySceneDelayed()
        {
            yield return null;
            yield return new WaitForSeconds(1f);

            var lobbyScene = SceneManager.GetSceneByName(lobbySceneName);
            if (lobbyScene.isLoaded)
            {
                Debug.Log($"[ReadySystem] Unloading lobby scene: {lobbySceneName}");
                SceneManager.UnloadSceneAsync(lobbyScene);
            }

            IsLoadingScene = false;
        }

        public void OnPlayerLeft(PlayerRef player)
        {
            RPC_UnregisterPlayer(player);
        }
    }
}
