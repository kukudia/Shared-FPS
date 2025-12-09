namespace Fusion
{
    using System;
    using System.Collections.Generic;
    using DG.Tweening;
    using Fusion.Sockets;
    using Projectiles;
    using TMPro;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.UI;

    /// <summary>
    /// 大厅UI系统 - 替代FusionBootstrapDebugGUI
    /// 功能：输入昵称、创建房间、查看房间列表、加入房间、刷新列表
    /// 
    /// 【修复版本 v3】添加离开房间功能
    /// </summary>
    [RequireComponent(typeof(FusionBootstrap))]
    public class FusionLobbyUI : MonoBehaviour, INetworkRunnerCallbacks
    {
        public string lobbySceneName;

        [Header("=== UI References ===")]
        [SerializeField] private GameObject lobbyPanel;

        [Header("Input Fields")]
        [SerializeField] private TMP_InputField nicknameInput;
        [SerializeField] private TMP_InputField roomNameInput;

        [Header("Buttons")]
        [SerializeField] private Button createRoomButton;
        [SerializeField] private Button refreshButton;

        [Header("Room List")]
        [SerializeField] private Transform roomListContent;
        [SerializeField] private GameObject roomItemPrefab;

        [Header("Status")]
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private CanvasGroup loadingIndicator;

        [Header("=== Settings ===")]
        [SerializeField] private GameMode gameMode = GameMode.Shared;
        [SerializeField] private float autoRefreshInterval = 3f;

        [Header("=== Debug ===")]
        [SerializeField] private bool enableDebugLogs = true;

        // Private fields
        private FusionBootstrap _bootstrap;
        private NetworkRunner _lobbyRunner;
        private NetworkRunner _gameRunner; // 游戏中的Runner
        private List<SessionInfo> _sessionList = new List<SessionInfo>();
        private List<GameObject> _roomItemInstances = new List<GameObject>();
        private float _lastRefreshTime;
        private bool _isConnectingToLobby;
        private bool _isJoiningGame;

        // 静态变量用于跨场景传递玩家昵称
        public static string PlayerNickname { get; private set; }

        #region Unity Lifecycle

        private void Awake()
        {
            _bootstrap = GetComponent<FusionBootstrap>();

            // 禁用原来的DebugGUI
            var debugGUI = GetComponent<FusionBootstrapDebugGUI>();
            if (debugGUI != null)
            {
                debugGUI.enabled = false;
                debugGUI.useGUI = false;
                DebugLog("Disabled FusionBootstrapDebugGUI");
            }

            // 设置 FusionBootstrap 为手动模式
            _bootstrap.StartMode = FusionBootstrap.StartModes.Manual;
            DebugLog("FusionBootstrap set to Manual mode");
        }

        private void Start()
        {
            SetupButtonListeners();
            Initialized();
        }

        private void Initialized()
        {
            loadingIndicator.alpha = 0f;
            ShowLobby();
            ConnectToLobby();
            if (!SceneManager.GetSceneByName(lobbySceneName).isLoaded)
            {
                SceneManager.LoadScene(lobbySceneName, LoadSceneMode.Additive);
            }
        }

        private void Update()
        {
            // 自动刷新
            if (_lobbyRunner != null && !_isJoiningGame && Time.time - _lastRefreshTime > autoRefreshInterval)
            {
                _lastRefreshTime = Time.time;
            }
        }

        private void OnDestroy()
        {
            DisconnectFromLobby();
            LeaveGame();
        }

        #endregion

        #region UI Setup

        private void SetupButtonListeners()
        {
            if (createRoomButton != null)
            {
                createRoomButton.onClick.AddListener(OnCreateRoomClicked);
                DebugLog("The 'Create Room' button has been bound.");
            }
            else
            {
                DebugLog("Warning: createRoomButton not allocated!", true);
            }

            if (refreshButton != null)
            {
                refreshButton.onClick.AddListener(OnRefreshClicked);
                DebugLog("Refresh button has been bound");
            }
        }

        private void ShowLobby()
        {
            if (lobbyPanel != null)
                lobbyPanel.SetActive(true);

            _isJoiningGame = false;  // 重置状态
            SetStatus("Please enter your nickname, then create or join a room.");
        }

        private void HideLobby()
        {
            if (lobbyPanel != null)
                lobbyPanel.SetActive(false);
        }

        #endregion

        #region Lobby Connection

        private async void ConnectToLobby()
        {
            if (_isConnectingToLobby || _lobbyRunner != null)
            {
                DebugLog("Already in the connection lobby or connected");
                return;
            }

            _isConnectingToLobby = true;
            SetStatus("Connecting to the lobby...");
            ShowLoading(true);

            var lobbyRunnerGO = new GameObject("LobbyRunner_ForRoomList");
            _lobbyRunner = lobbyRunnerGO.AddComponent<NetworkRunner>();
            _lobbyRunner.AddCallbacks(this);

            DebugLog("Connecting to SessionLobby...");

            var result = await _lobbyRunner.JoinSessionLobby(SessionLobby.ClientServer);

            _isConnectingToLobby = false;
            ShowLoading(false);

            if (result.Ok)
            {
                SetStatus("Connected to lobby, awaiting room list...");
                DebugLog("The lobby has connected successfully.");
            }
            else
            {
                SetStatus($"Failed to connect to the lobby: {result.ShutdownReason}");
                DebugLog($"Failed to connect to the lobby: {result.ShutdownReason}", true);
                DisconnectFromLobby();
            }
        }

        private void DisconnectFromLobby()
        {
            if (_lobbyRunner != null)
            {
                DebugLog("Disconnecting from the lobby...");
                _lobbyRunner.RemoveCallbacks(this);
                _lobbyRunner.Shutdown();

                if (_lobbyRunner.gameObject != null)
                    Destroy(_lobbyRunner.gameObject);

                _lobbyRunner = null;
            }
        }

        #endregion

        #region Room List UI

        private void UpdateRoomListUI()
        {
            // 清除现有的房间项
            foreach (var item in _roomItemInstances)
            {
                if (item != null) Destroy(item);
            }
            _roomItemInstances.Clear();

            if (_sessionList.Count == 0)
            {
                SetStatus("No rooms available. Please create a new room.");
                return;
            }

            SetStatus($"Found {_sessionList.Count} rooms (click to join)");

            foreach (var session in _sessionList)
            {
                CreateRoomListItem(session);
            }
        }

        private void CreateRoomListItem(SessionInfo session)
        {
            DebugLog($"Create room list item: {session.Name}");

            // 如果没有 Prefab，创建一个简单的按钮
            if (roomItemPrefab == null)
            {
                DebugLog("roomItemPrefab not assigned, create simple button", true);
                CreateSimpleRoomButton(session);
                return;
            }

            if (roomListContent == null)
            {
                DebugLog("roomListContent Unassigned!", true);
                return;
            }

            var item = Instantiate(roomItemPrefab, roomListContent);
            _roomItemInstances.Add(item);

            var roomListItem = item.GetComponent<RoomListItem>();
            if (roomListItem != null)
            {
                roomListItem.Setup(session, OnRoomItemClicked);
                DebugLog($"Room {session.Name} using the RoomListItem component");
            }
            else
            {
                // 备用方案：直接设置
                var texts = item.GetComponentsInChildren<TMP_Text>();
                if (texts.Length > 0)
                {
                    texts[0].text = $"{session.Name} ({session.PlayerCount}/{session.MaxPlayers})";
                }

                var button = item.GetComponentInChildren<Button>();
                if (button != null)
                {
                    // 【关键】必须用局部变量捕获 session
                    string roomName = session.Name;
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => {
                        DebugLog($"clicked the room button: {roomName}");
                        OnRoomItemClickedByName(roomName);
                    });
                    DebugLog($"The button for room {session.Name} has been bound to a click event.");
                }
                else
                {
                    DebugLog($"The room {session.Name} does not contain a Button component in its Prefab!", true);
                }
            }
        }

        /// <summary>
        /// 如果没有设置 Prefab，创建简单的按钮
        /// </summary>
        private void CreateSimpleRoomButton(SessionInfo session)
        {
            if (roomListContent == null)
            {
                Debug.Log($"[Lobby] Room: {session.Name} - {session.PlayerCount}/{session.MaxPlayers} (Unable to display UI)");
                return;
            }

            // 创建一个简单的按钮 GameObject
            var buttonGO = new GameObject($"Room_{session.Name}");
            buttonGO.transform.SetParent(roomListContent, false);

            var rectTransform = buttonGO.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(300, 50);

            var image = buttonGO.AddComponent<Image>();
            image.color = new Color(0.3f, 0.3f, 0.3f, 1f);

            var button = buttonGO.AddComponent<Button>();
            button.targetGraphic = image;

            // 文本
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform, false);
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = $"{session.Name} ({session.PlayerCount}/{session.MaxPlayers})";
            text.fontSize = 18;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;

            // 绑定点击事件
            string roomName = session.Name;
            button.onClick.AddListener(() => {
                DebugLog($"Click the simple button: {roomName}");
                OnRoomItemClickedByName(roomName);
            });

            _roomItemInstances.Add(buttonGO);
            DebugLog($"Created a 'Simple Room' button: {session.Name}");
        }

        private void OnRoomItemClicked(SessionInfo session)
        {
            DebugLog($"OnRoomItemClicked: {session.Name}");
            JoinRoom(session.Name);
        }

        private void OnRoomItemClickedByName(string roomName)
        {
            DebugLog($"OnRoomItemClickedByName: {roomName}");
            JoinRoom(roomName);
        }

        #endregion

        #region Button Handlers

        private void OnCreateRoomClicked()
        {
            DebugLog("Click the Create Room button");

            if (!ValidateNickname()) return;

            string roomName = roomNameInput != null ? roomNameInput.text.Trim() : "";
            if (string.IsNullOrEmpty(roomName))
            {
                roomName = $"Room_{UnityEngine.Random.Range(1000, 9999)}";
            }

            CreateRoom(roomName);
        }

        private void OnRefreshClicked()
        {
            DebugLog("Click the refresh button");

            if (_lobbyRunner == null)
            {
                ConnectToLobby();
            }
            else
            {
                SetStatus("Refreshing room list...");
            }
            _lastRefreshTime = Time.time;
        }

        #endregion

        #region Room Operations

        private void CreateRoom(string roomName)
        {
            DebugLog($"CreateRoom: {roomName}, _isJoiningGame={_isJoiningGame}");

            if (_isJoiningGame)
            {
                DebugLog("Already joining the game, ignore.", true);
                return;
            }

            if (!ValidateNickname()) return;

            _isJoiningGame = true;
            SetStatus($"Creating a room: {roomName}...");
            ShowLoading(true);

            DisconnectFromLobby();

            _bootstrap.DefaultRoomName = roomName;
            DebugLog($"设置 DefaultRoomName = {roomName}");

            HideLobby();

            DebugLog($"调用 FusionBootstrap 启动, GameMode={gameMode}");

            switch (gameMode)
            {
                case GameMode.Shared:
                    _bootstrap.StartSharedClient();
                    break;
                case GameMode.Host:
                    _bootstrap.StartHost();
                    break;
                case GameMode.Server:
                    _bootstrap.StartServer();
                    break;
                case GameMode.AutoHostOrClient:
                    _bootstrap.StartAutoClient();
                    break;
                default:
                    _bootstrap.StartSharedClient();
                    break;
            }

            ShowLoading(true);
        }

        private void JoinRoom(string roomName)
        {
            DebugLog($"JoinRoom: {roomName}, _isJoiningGame={_isJoiningGame}");

            if (_isJoiningGame)
            {
                DebugLog("Already joining the game, ignore.", true);
                return;
            }

            if (!ValidateNickname()) return;

            _isJoiningGame = true;
            SetStatus($"Joining the room: {roomName}...");
            ShowLoading(true);

            DisconnectFromLobby();

            _bootstrap.DefaultRoomName = roomName;
            DebugLog($"Set DefaultRoomName = {roomName}");

            HideLobby();

            DebugLog($"Call FusionBootstrap to join, GameMode={gameMode}");

            switch (gameMode)
            {
                case GameMode.Shared:
                    _bootstrap.StartSharedClient();
                    break;
                case GameMode.AutoHostOrClient:
                    _bootstrap.StartAutoClient();
                    break;
                case GameMode.Client:
                    _bootstrap.StartClient();
                    break;
                default:
                    _bootstrap.StartSharedClient();
                    break;
            }

            ShowLoading(true);
        }

        /// <summary>
        /// 离开当前游戏房间，返回大厅
        /// </summary>
        public void LeaveRoom()
        {
            DebugLog("LeaveRoom called");

            LeaveGame();

            // 重置状态
            _isJoiningGame = false;

            Initialized();
        }

        /// <summary>
        /// 关闭游戏Runner
        /// </summary>
        private void LeaveGame()
        {
            // 查找当前活跃的游戏Runner
            var runners = FindObjectsOfType<NetworkRunner>();
            foreach (var runner in runners)
            {
                // 排除大厅Runner
                if (runner != _lobbyRunner && runner.IsRunning)
                {
                    DebugLog($"Shutting down game runner: {runner.name}");
                    runner.Shutdown();
                }
            }

            _gameRunner = null;
        }

        #endregion

        #region Validation & Utilities

        private bool ValidateNickname()
        {
            string nickname = nicknameInput != null ? nicknameInput.text.Trim() : "";

            if (string.IsNullOrEmpty(nickname))
            {
                nickname = $"Player_{UnityEngine.Random.Range(100, 999)}";
                if (nicknameInput != null)
                    nicknameInput.text = nickname;
            }

            if (nickname.Length > 16)
            {
                SetStatus("Nicknames must be no longer than 16 characters! ");
                return false;
            }

            PlayerNickname = nickname;
            DebugLog($"Player Nickname: {nickname}");
            return true;
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
            Debug.Log($"[Lobby] {message}");
        }

        public void ShowLoading(bool show)
        {
            if (loadingIndicator != null)
            {
                SetPanelVisualization(loadingIndicator, 200f, show);
            }
        }

        private void DebugLog(string message, bool isWarning = false)
        {
            if (!enableDebugLogs) return;

            if (isWarning)
                Debug.LogWarning($"[LobbyUI] {message}");
            else
                Debug.Log($"[LobbyUI] {message}");
        }

        #endregion

        #region INetworkRunnerCallbacks

        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {
            if (runner != _lobbyRunner) return;

            DebugLog($"收到房间列表更新，共 {sessionList.Count} 个房间");

            foreach (var session in sessionList)
            {
                DebugLog($"  - {session.Name}: {session.PlayerCount}/{session.MaxPlayers}");
            }

            _sessionList = sessionList;
            UpdateRoomListUI();
        }

        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        {
            Initialized();
        }

        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            Initialized();
        }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            if (runner == _lobbyRunner)
            {
                DebugLog($"大厅 Runner 关闭: {shutdownReason}");
                _lobbyRunner = null;
            }
        }

        private void SetPanelVisualization(CanvasGroup group, float targetAlpha, bool load)
        {
            DOTween.Kill(group);

            if (load)
            {
                group.DOFade(targetAlpha, 0.1f);
            }
            else
            {
                group.DOFade(0f, 0.7f);
            }
        }

        // 空实现
        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
        public void OnInput(NetworkRunner runner, NetworkInput input) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnConnectedToServer(NetworkRunner runner) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

        #endregion
    }

    /// <summary>
    /// 房间列表项组件
    /// </summary>
    public class RoomListItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text roomNameText;
        [SerializeField] private TMP_Text playerCountText;
        [SerializeField] private Button joinButton;

        private SessionInfo _sessionInfo;
        private Action<SessionInfo> _onJoinClicked;

        public void Setup(SessionInfo session, Action<SessionInfo> onJoinClicked)
        {
            _sessionInfo = session;
            _onJoinClicked = onJoinClicked;

            if (roomNameText != null)
                roomNameText.text = session.Name;

            if (playerCountText != null)
                playerCountText.text = $"{session.PlayerCount}/{session.MaxPlayers}";

            if (joinButton != null)
            {
                joinButton.onClick.RemoveAllListeners();
                joinButton.onClick.AddListener(OnJoinButtonClicked);
                joinButton.interactable = session.PlayerCount < session.MaxPlayers;
            }

            // 如果没有单独的按钮，整个Item都可点击
            var itemButton = GetComponent<Button>();
            if (itemButton != null && itemButton != joinButton)
            {
                itemButton.onClick.RemoveAllListeners();
                itemButton.onClick.AddListener(OnJoinButtonClicked);
            }
        }

        private void OnJoinButtonClicked()
        {
            Debug.Log($"[RoomListItem] Click to join the room: {_sessionInfo.Name}");
            _onJoinClicked?.Invoke(_sessionInfo);
        }
    }
}