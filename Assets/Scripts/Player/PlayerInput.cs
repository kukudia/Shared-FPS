using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Fusion;
using Fusion.Addons.SimpleKCC;
using System;

namespace Projectiles
{
    public enum EInputButton
    {
        Fire = 0,
        AltFire = 1,
        Jump = 2,
        Reload = 3,
    }

    public struct GameplayInput : INetworkInput
    {
        public int WeaponSlot => WeaponButton - 1;

        public Vector2 MoveDirection;
        public Vector2 LookRotationDelta;
        public byte WeaponButton;
        public NetworkButtons Buttons;
    }

    /// <summary>
    /// PlayerInput handles accumulating player input from Unity and passes the accumulated input to Fusion.
    /// 【修复版本】支持场景切换后重新初始化输入系统
    /// </summary>
    public sealed class PlayerInput : ContextBehaviour, IBeforeUpdate, IAfterTick
    {
        // PUBLIC METHODS

        public NetworkButtons PreviousButtons => _previousButtons;
        public Vector2 AccumulatedLook => _lookRotationAccumulator.AccumulatedValue;

        // PRIVATE MEMBERS

        [SerializeField]
        private float _lookSensitivity = 3;

        [Networked]
        private NetworkButtons _previousButtons { get; set; }

        private GameplayInput _accumulatedInput;
        private Vector2Accumulator _lookRotationAccumulator = new(0.02f, true);

        private PlayerAgent _agent;
        private bool _isInputRegistered;
        private NetworkEvents _registeredNetworkEvents;

        // NetworkBehaviour INTERFACE

        public override void Spawned()
        {
            Debug.Log($"[PlayerInput] Spawned called - Object: {Object?.Id}, InputAuthority: {Object?.InputAuthority}, HasInputAuthority: {HasInputAuthority}");

            // Only local player needs networked properties (previous input buttons).
            ReplicateToAll(false);
            ReplicateTo(Object.InputAuthority, true);

            if (HasInputAuthority == false)
            {
                Debug.Log("[PlayerInput] Spawned - No InputAuthority, skipping input registration");
                return;
            }

            Debug.Log("[PlayerInput] Spawned - Has InputAuthority, registering input...");
            RegisterInput();
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            Debug.Log($"[PlayerInput] Despawned called");
            UnregisterInput();
        }

        // 每帧检查并确保输入已注册（处理场景切换的情况）
        public override void FixedUpdateNetwork()
        {
            if (HasInputAuthority == false)
                return;

            // 检查输入是否需要重新注册
            if (!_isInputRegistered || _registeredNetworkEvents == null)
            {
                Debug.Log($"[PlayerInput] FixedUpdateNetwork - Input needs re-registration. _isInputRegistered={_isInputRegistered}, _registeredNetworkEvents={((_registeredNetworkEvents != null) ? "Valid" : "NULL")}");
                RegisterInput();
            }
        }

        // IBeforeUpdate INTERFACE

        private float _lastDebugTime;

        void IBeforeUpdate.BeforeUpdate()
        {
            // 每秒输出一次调试信息
            bool shouldLog = Time.time - _lastDebugTime > 1f;

            if (HasInputAuthority == false)
            {
                if (shouldLog)
                {
                    Debug.Log($"[PlayerInput] BeforeUpdate - No InputAuthority, skipping");
                    _lastDebugTime = Time.time;
                }
                return;
            }

            // 确保输入已注册
            if (!_isInputRegistered)
            {
                Debug.Log($"[PlayerInput] BeforeUpdate - Input not registered, registering now...");
                RegisterInput();
            }

            // 检查游戏是否已开始
            if (_agent == null)
            {
                if (shouldLog)
                {
                    Debug.LogWarning($"[PlayerInput] BeforeUpdate - _agent is NULL!");
                    _lastDebugTime = Time.time;
                }
                _accumulatedInput = default;
                return;
            }

            if (!_agent.gameStart)
            {
                if (shouldLog)
                {
                    Debug.Log($"[PlayerInput] BeforeUpdate - gameStart=false, waiting...");
                    _lastDebugTime = Time.time;
                }
                _accumulatedInput = default;
                return;
            }

            // Input is tracked only if the cursor is locked and runner should provide input
            // 使用安全的方式获取 GeneralInput
            var generalInput = GetGeneralInput();

            if (shouldLog)
            {
                Debug.Log($"[PlayerInput] BeforeUpdate Status:\n" +
                    $"  - HasInputAuthority: {HasInputAuthority}\n" +
                    $"  - _isInputRegistered: {_isInputRegistered}\n" +
                    $"  - _agent: {(_agent != null ? _agent.name : "NULL")}\n" +
                    $"  - _agent.gameStart: {_agent.gameStart}\n" +
                    $"  - Runner: {(Runner != null ? "Valid" : "NULL")}\n" +
                    $"  - Runner.ProvideInput: {(Runner != null ? Runner.ProvideInput.ToString() : "N/A")}\n" +
                    $"  - GeneralInput: {(generalInput != null ? "Found" : "NULL")}\n" +
                    $"  - GeneralInput.IsLocked: {(generalInput != null ? generalInput.IsLocked.ToString() : "N/A")}\n" +
                    $"  - _agent.InputBlocked: {_agent.InputBlocked}\n" +
                    $"  - Context: {(Context != null ? "Valid" : "NULL")}\n" +
                    $"  - Context.GeneralInput: {(Context?.GeneralInput != null ? "Valid" : "NULL")}");
                _lastDebugTime = Time.time;
            }

            if (Runner == null)
            {
                Debug.LogError("[PlayerInput] Runner is NULL!");
                _accumulatedInput = default;
                return;
            }

            if (Runner.ProvideInput == false)
            {
                if (shouldLog) Debug.Log("[PlayerInput] Runner.ProvideInput is FALSE");
                _accumulatedInput = default;
                return;
            }

            if (generalInput == null)
            {
                if (shouldLog) Debug.LogWarning("[PlayerInput] GeneralInput is NULL");
                _accumulatedInput = default;
                return;
            }

            if (generalInput.IsLocked == false)
            {
                if (shouldLog) Debug.Log("[PlayerInput] Cursor is NOT locked");
                _accumulatedInput = default;
                return;
            }

            if (_agent.InputBlocked == true)
            {
                if (shouldLog) Debug.Log("[PlayerInput] Input is BLOCKED");
                _accumulatedInput = default;
                return;
            }

            var mouse = Mouse.current;
            if (mouse != null)
            {
                var mouseDelta = mouse.delta.ReadValue();

                var lookRotationDelta = new Vector2(-mouseDelta.y, mouseDelta.x);
                lookRotationDelta *= _lookSensitivity / 60f;
                _lookRotationAccumulator.Accumulate(lookRotationDelta);

                _accumulatedInput.Buttons.Set(EInputButton.Fire, mouse.leftButton.isPressed);
                _accumulatedInput.Buttons.Set(EInputButton.AltFire, mouse.rightButton.isPressed);
            }

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                var moveDirection = Vector2.zero;

                if (keyboard.wKey.isPressed) { moveDirection += Vector2.up; }
                if (keyboard.sKey.isPressed) { moveDirection += Vector2.down; }
                if (keyboard.aKey.isPressed) { moveDirection += Vector2.left; }
                if (keyboard.dKey.isPressed) { moveDirection += Vector2.right; }

                _accumulatedInput.MoveDirection = moveDirection.normalized;

                _accumulatedInput.Buttons.Set(EInputButton.Jump, keyboard.spaceKey.isPressed);
                _accumulatedInput.Buttons.Set(EInputButton.Reload, keyboard.rKey.isPressed);

                _accumulatedInput.WeaponButton = 0;
                for (int i = (int)Key.Digit1; i <= (int)Key.Digit9; i++)
                {
                    if (keyboard[(Key)i].isPressed == true)
                    {
                        _accumulatedInput.WeaponButton = (byte)(i - (int)Key.Digit1 + 1);
                        break;
                    }
                }
            }
        }

        // IAfterTick INTERFACE

        void IAfterTick.AfterTick()
        {
            _previousButtons = GetInput<GameplayInput>().GetValueOrDefault().Buttons;
        }

        // MONOBEHAVIOUR

        private void Awake()
        {
            _agent = GetComponent<PlayerAgent>();
        }

        private void OnEnable()
        {
            // 监听场景加载完成事件
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            UnregisterInput();
        }

        // PRIVATE METHODS

        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
        {
            // 场景加载后重新初始化
            if (HasInputAuthority)
            {
                Debug.Log($"[PlayerInput] Scene loaded: {scene.name}, re-initializing input...");

                Context.Gameplay.SpawnPlayerAgentFromReady(_agent.Owner);

                // 延迟一帧执行，确保新场景的对象都已初始化
                StartCoroutine(ReinitializeInputDelayed());

                _agent.gameStart = true;
            }
        }

        private System.Collections.IEnumerator ReinitializeInputDelayed()
        {
            yield return null; // 等待一帧

            RegisterInput();

            // 重新请求锁定光标
            var generalInput = GetGeneralInput();
            if (generalInput != null && _agent.gameStart)
            {
                generalInput.RequestCursorLock();
                Debug.Log("[PlayerInput] Re-requested cursor lock after scene load");
            }
        }

        private void RegisterInput()
        {
            Debug.Log($"[PlayerInput] RegisterInput called - Runner: {(Runner != null ? "Valid" : "NULL")}");

            if (Runner == null)
            {
                Debug.LogWarning("[PlayerInput] Runner is null, cannot register input");
                return;
            }

            // 先取消之前的注册
            UnregisterInput();

            var networkEvents = Runner.GetComponent<NetworkEvents>();
            Debug.Log($"[PlayerInput] NetworkEvents: {(networkEvents != null ? "Found" : "NULL")}");

            if (networkEvents != null)
            {
                networkEvents.OnInput.AddListener(OnInput);
                _registeredNetworkEvents = networkEvents;
                _isInputRegistered = true;
                Debug.Log($"[PlayerInput] Input registered successfully to NetworkEvents on {Runner.name}");

                // 请求锁定光标
                var generalInput = GetGeneralInput();
                Debug.Log($"[PlayerInput] GeneralInput for cursor lock: {(generalInput != null ? "Found" : "NULL")}");

                if (generalInput != null)
                {
                    generalInput.RequestCursorLock();
                    Debug.Log("[PlayerInput] Cursor lock requested");
                }
            }
            else
            {
                Debug.LogError("[PlayerInput] NetworkEvents not found on Runner!");
            }
        }

        private void UnregisterInput()
        {
            if (_registeredNetworkEvents != null)
            {
                _registeredNetworkEvents.OnInput.RemoveListener(OnInput);
                _registeredNetworkEvents = null;
            }
            _isInputRegistered = false;
        }

        /// <summary>
        /// 安全获取 GeneralInput（处理 Context 可能为 null 的情况）
        /// </summary>
        private GeneralInput GetGeneralInput()
        {
            // 优先从 Context 获取
            if (Context != null && Context.GeneralInput != null)
            {
                return Context.GeneralInput;
            }

            // 备用方案：直接查找
            return FindFirstObjectByType<GeneralInput>();
        }

        private void OnInput(NetworkRunner runner, NetworkInput networkInput)
        {
            // Mouse movement (delta values) is aligned to engine update.
            _accumulatedInput.LookRotationDelta = _lookRotationAccumulator.ConsumeTickAligned(runner);

            if (_agent.InputBlocked == true)
            {
                Debug.Log("[PlayerInput] OnInput - Input blocked, not setting input");
                return;
            }

            // 调试：显示输入内容
            if (_accumulatedInput.MoveDirection != Vector2.zero || _accumulatedInput.LookRotationDelta != Vector2.zero)
            {
                // Debug.Log($"[PlayerInput] OnInput - Setting input: Move={_accumulatedInput.MoveDirection}, Look={_accumulatedInput.LookRotationDelta}");
            }

            networkInput.Set(_accumulatedInput);
        }
    }
}