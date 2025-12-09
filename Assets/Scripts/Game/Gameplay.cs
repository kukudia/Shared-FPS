using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Projectiles
{
    /// <summary>
    /// Represents the actual gameplay loop. Handles PlayerAgent spawning and despawning for each Player that joins gameplay.
    /// 【已修改】整合了准备系统 + 场景加载后自动重生所有玩家
    /// </summary>
    public class Gameplay : ContextBehaviour
    {
        // PUBLIC MEMBERS

        [Networked, Capacity(200)]
        public NetworkDictionary<PlayerRef, Player> Players { get; }

        // PRIVATE METHODS

        [Header("Ready System")]
        [SerializeField] private ReadySystem readySystemPrefab;

        public SpawnPoint[] _spawnPoints;
        public SpawnPointInGame[] _spawnPointsInGame;
        private int _lastSpawnPoint = -1;

        private List<SpawnRequest> _spawnRequests = new();
        private ReadySystem _readySystem;

        // PUBLIC METHODS

        public void Join(Player player)
        {
            if (HasStateAuthority == false)
                return;

            var playerRef = player.Object.InputAuthority;

            if (Players.ContainsKey(playerRef) == true)
            {
                Debug.LogError($"Player {playerRef} already joined");
                return;
            }

            Players.Add(playerRef, player);

            OnPlayerJoined(player);
        }

        public void Leave(Player player)
        {
            if (HasStateAuthority == false)
                return;

            if (Players.ContainsKey(player.Object.InputAuthority) == false)
                return;

            var playerRef = player.Object.InputAuthority;
            Players.Remove(playerRef);

            // 通知准备系统玩家离开
            if (_readySystem != null)
            {
                _readySystem.OnPlayerLeft(playerRef);
            }

            OnPlayerLeft(player);
        }

        // NetworkBehaviour INTERFACE

        public override void Spawned()
        {
            // Register to context
            Context.Gameplay = this;

            // 生成准备系统(仅服务器)
            if (HasStateAuthority && readySystemPrefab != null)
            {
                _readySystem = Runner.Spawn(readySystemPrefab);
                Debug.Log("[Gameplay] ReadySystem spawned");
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (HasStateAuthority == false)
                return;

            int currentTick = Runner.Tick;

            for (int i = _spawnRequests.Count - 1; i >= 0; i--)
            {
                var request = _spawnRequests[i];

                if (request.Tick > currentTick)
                    continue;

                _spawnRequests.RemoveAt(i);

                if (request.Player == null || request.Player.Object == null)
                    continue; // Player no longer valid

                if (Players.ContainsKey(request.Player.Object.InputAuthority) == false)
                    continue; // Player left gameplay

                SpawnPlayerAgent(request.Player);
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            // Clear from context
            Context.Gameplay = null;

            // 清理准备系统
            if (_readySystem != null && HasStateAuthority)
            {
                Runner.Despawn(_readySystem.Object);
                _readySystem = null;
            }
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += SceneLoadDone;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= SceneLoadDone;
        }

        public void SceneLoadDone(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
        {
            if (scene != SceneManager.GetSceneByName(Context.ReadySystem.gameplaySceneName))
            {
                Debug.Log("[Gameplay] SceneLoadDone but not gameplay scene.");
                return;
            }
            Debug.Log("[Gameplay] SceneLoadDone called");

            // 仅服务器处理重生逻辑
            //if (!HasStateAuthority)
            //    return;

            // 检查是否是游戏场景加载完成
            if (_readySystem != null && _readySystem.GameStarted)
            {
                Debug.Log($"[Gameplay] Game scene loaded, respawning all players. Player count: {Players.Count}");

                // 刷新生成点列表
                _spawnPointsInGame = SceneManager.GetSceneByName(_readySystem.gameplaySceneName).FindObjectsOfTypeInOrder<SpawnPointInGame>(false);
                Debug.Log($"[Gameplay] Found {_spawnPointsInGame.Length} spawn points in game scene");

                // 为所有已连接的玩家重新生成 Agent
                foreach (var kvp in Players)
                {
                    var player = kvp.Value;

                    // 检查玩家是否已经有 Agent
                    //if (player.ActiveAgent != null)
                    //{
                    //    Debug.Log($"[Gameplay] Player {kvp.Key} already has an agent, skipping respawn");
                    //    continue;
                    //}

                    Debug.Log($"[Gameplay] Respawning player {kvp.Key}");
                    SpawnPlayerAgent(player);
                }
            }
        }

        // PROTECTED METHODS

        protected virtual void OnPlayerJoined(Player player)
        {
            SpawnPlayerAgent(player);
        }

        protected virtual void OnPlayerLeft(Player player)
        {
            DespawnPlayerAgent(player);
        }

        protected virtual void OnPlayerDeath(Player player)
        {
            AddSpawnRequest(player, 3f);
        }

        protected virtual void OnPlayerAgentSpawned(PlayerAgent agent)
        {
            agent.Health.SetImmortality(3f);

            // 默认gameStart为false,等待准备系统启动
            agent.gameStart = false;

            // 如果游戏已经开始,直接设置为true
            if (Context.ReadySystem != null && Context.ReadySystem.GameStarted)
            {
                agent.gameStart = true;
                Debug.Log($"[Gameplay] Agent spawned with gameStart=true for player {agent.Object.InputAuthority}");
            }
        }

        protected virtual void OnPlayerAgentDespawned(PlayerAgent agent)
        {
        }

        public void SpawnPlayerAgentFromReady(Player player)
        {
            SpawnPlayerAgent(player);
        }

        protected void SpawnPlayerAgent(Player player)
        {
            DespawnPlayerAgent(player);

            var agent = SpawnAgent(player.Object.InputAuthority, player.AgentPrefab) as PlayerAgent;
            player.AssignAgent(agent);

            agent.Health.FatalHitTaken += OnFatalHitTaken;

            OnPlayerAgentSpawned(agent);
        }

        protected void DespawnPlayerAgent(Player player)
        {
            if (player.ActiveAgent == null)
                return;

            player.ActiveAgent.Health.FatalHitTaken -= OnFatalHitTaken;

            OnPlayerAgentDespawned(player.ActiveAgent);

            DespawnAgent(player.ActiveAgent);
            player.ClearAgent();
        }

        protected void AddSpawnRequest(Player player, float spawnDelay)
        {
            int delayTicks = Mathf.RoundToInt(Runner.TickRate * spawnDelay);

            _spawnRequests.Add(new SpawnRequest()
            {
                Player = player,
                Tick = Runner.Tick + delayTicks,
            });
        }

        // PRIVATE METHODS

        private void OnFatalHitTaken(HitData hitData)
        {
            var health = hitData.Target as Health;

            if (health == null)
                return;

            if (Players.TryGet(health.Object.InputAuthority, out Player player) == true)
            {
                OnPlayerDeath(player);
            }
        }

        private PlayerAgent SpawnAgent(PlayerRef inputAuthority, PlayerAgent agentPrefab)
        {
            if (Context.ReadySystem.GameStarted)
            {
                if (_spawnPointsInGame.Length == 0)
                {
                    _spawnPointsInGame = SceneManager.GetSceneByName(Context.ReadySystem.gameplaySceneName).FindObjectsOfTypeInOrder<SpawnPointInGame>(false);
                }
                _lastSpawnPoint = (_lastSpawnPoint + 1) % _spawnPointsInGame.Length;
                var spawnPointInGame = _spawnPointsInGame[_lastSpawnPoint].transform;
                var agent = Runner.Spawn(agentPrefab, spawnPointInGame.position, spawnPointInGame.rotation, inputAuthority);
                Debug.Log($"[Gameplay] {agent} spawn at {spawnPointInGame}");
                return agent;
            }
            else
            {
                if (_spawnPoints.Length == 0)
                {
                    _spawnPoints = Runner.SimulationUnityScene.FindObjectsOfTypeInOrder<SpawnPoint>(false);
                }
                _lastSpawnPoint = (_lastSpawnPoint + 1) % _spawnPoints.Length;
                var spawnPoint = _spawnPoints[_lastSpawnPoint].transform;
                var agent = Runner.Spawn(agentPrefab, spawnPoint.position, spawnPoint.rotation, inputAuthority);
                Debug.Log($"[Gameplay] {agent} spawn at {spawnPoint}");
                return agent;
            }
        }

        private void DespawnAgent(PlayerAgent agent)
        {
            if (agent == null)
                return;

            Runner.Despawn(agent.Object);
        }

        // HELPERS

        public struct SpawnRequest
        {
            public Player Player;
            public int Tick;
        }
    }
}
