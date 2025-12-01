namespace Fusion
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;
    using TMPro;
    using Fusion.Sockets;

    /// <summary>
    /// 大厅UI系统 - 替代FusionBootstrapDebugGUI
    /// 功能：输入昵称、创建房间、查看房间列表、加入房间、刷新列表
    /// 
    /// 【修复版本 v2】添加调试日志，修复点击房间无法加入的问题
    /// </summary>
    [RequireComponent(typeof(FusionBootstrap))]
    public class FusionLobbyUI : MonoBehaviour, INetworkRunnerCallbacks
    {
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
        [SerializeField] private GameObject loadingIndicator;

        [Header("=== Settings ===")]
        [SerializeField] private GameMode gameMode = GameMode.Shared;
        [SerializeField] private float autoRefreshInterval = 3f;

        [Header("=== Debug ===")]
        [SerializeField] private bool enableDebugLogs = true;

        // Private fields
        private FusionBootstrap _bootstrap;
        private NetworkRunner _lobbyRunner;
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
                DebugLog("已禁用 FusionBootstrapDebugGUI");
            }

            // 设置 FusionBootstrap 为手动模式
            _bootstrap.StartMode = FusionBootstrap.StartModes.Manual;
            DebugLog("FusionBootstrap 设置为 Manual 模式");
        }

        private void Start()
        {
            SetupButtonListeners();
            ShowLobby();
            ConnectToLobby();
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
        }

        #endregion

        #region UI Setup

        private void SetupButtonListeners()
        {
            if (createRoomButton != null)
            {
                createRoomButton.onClick.AddListener(OnCreateRoomClicked);
                DebugLog("已绑定创建房间按钮");
            }
            else
            {
                DebugLog("警告: createRoomButton 未分配!", true);
            }

            if (refreshButton != null)
            {
                refreshButton.onClick.AddListener(OnRefreshClicked);
                DebugLog("已绑定刷新按钮");
            }
        }

        private void ShowLobby()
        {
            if (lobbyPanel != null)
                lobbyPanel.SetActive(true);

            _isJoiningGame = false;  // 重置状态
            SetStatus("请输入昵称，然后创建或加入房间");
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
                DebugLog("已经在连接大厅或已连接");
                return;
            }

            _isConnectingToLobby = true;
            SetStatus("正在连接大厅...");
            ShowLoading(true);

            var lobbyRunnerGO = new GameObject("LobbyRunner_ForRoomList");
            _lobbyRunner = lobbyRunnerGO.AddComponent<NetworkRunner>();
            _lobbyRunner.AddCallbacks(this);

            DebugLog("正在连接 SessionLobby...");

            var result = await _lobbyRunner.JoinSessionLobby(SessionLobby.ClientServer);

            _isConnectingToLobby = false;
            ShowLoading(false);

            if (result.Ok)
            {
                SetStatus("已连接大厅，等待房间列表...");
                DebugLog("大厅连接成功");
            }
            else
            {
                SetStatus($"连接大厅失败: {result.ShutdownReason}");
                DebugLog($"大厅连接失败: {result.ShutdownReason}", true);
                DisconnectFromLobby();
            }
        }

        private void DisconnectFromLobby()
        {
            if (_lobbyRunner != null)
            {
                DebugLog("正在断开大厅连接...");
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
                SetStatus("暂无房间，请创建一个新房间");
                return;
            }

            SetStatus($"找到 {_sessionList.Count} 个房间 (点击加入)");

            foreach (var session in _sessionList)
            {
                CreateRoomListItem(session);
            }
        }

        private void CreateRoomListItem(SessionInfo session)
        {
            DebugLog($"创建房间列表项: {session.Name}");

            // 如果没有 Prefab，创建一个简单的按钮
            if (roomItemPrefab == null)
            {
                DebugLog("roomItemPrefab 未分配，创建简单按钮", true);
                CreateSimpleRoomButton(session);
                return;
            }

            if (roomListContent == null)
            {
                DebugLog("roomListContent 未分配!", true);
                return;
            }

            var item = Instantiate(roomItemPrefab, roomListContent);
            _roomItemInstances.Add(item);

            var roomListItem = item.GetComponent<RoomListItem>();
            if (roomListItem != null)
            {
                roomListItem.Setup(session, OnRoomItemClicked);
                DebugLog($"房间 {session.Name} 使用 RoomListItem 组件");
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
                        DebugLog($"点击了房间按钮: {roomName}");
                        OnRoomItemClickedByName(roomName);
                    });
                    DebugLog($"房间 {session.Name} 按钮已绑定点击事件");
                }
                else
                {
                    DebugLog($"房间 {session.Name} 的 Prefab 中没有 Button 组件!", true);
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
                Debug.Log($"[Lobby] 房间: {session.Name} - {session.PlayerCount}/{session.MaxPlayers} (无法显示UI)");
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
                DebugLog($"点击简单按钮: {roomName}");
                OnRoomItemClickedByName(roomName);
            });

            _roomItemInstances.Add(buttonGO);
            DebugLog($"创建了简单房间按钮: {session.Name}");
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
            DebugLog("点击创建房间按钮");

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
            DebugLog("点击刷新按钮");

            if (_lobbyRunner == null)
            {
                ConnectToLobby();
            }
            else
            {
                SetStatus("正在刷新房间列表...");
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
                DebugLog("已经在加入游戏中，忽略", true);
                return;
            }

            if (!ValidateNickname()) return;

            _isJoiningGame = true;
            SetStatus($"正在创建房间: {roomName}...");
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

            ShowLoading(false);
        }

        private void JoinRoom(string roomName)
        {
            DebugLog($"JoinRoom: {roomName}, _isJoiningGame={_isJoiningGame}");

            if (_isJoiningGame)
            {
                DebugLog("已经在加入游戏中，忽略", true);
                return;
            }

            if (!ValidateNickname()) return;

            _isJoiningGame = true;
            SetStatus($"正在加入房间: {roomName}...");
            ShowLoading(true);

            DisconnectFromLobby();

            _bootstrap.DefaultRoomName = roomName;
            DebugLog($"设置 DefaultRoomName = {roomName}");

            HideLobby();

            DebugLog($"调用 FusionBootstrap 加入, GameMode={gameMode}");

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

            ShowLoading(false);
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
                SetStatus("昵称最多16个字符！");
                return false;
            }

            PlayerNickname = nickname;
            DebugLog($"玩家昵称: {nickname}");
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

        private void ShowLoading(bool show)
        {
            if (loadingIndicator != null)
            {
                loadingIndicator.SetActive(show);
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

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            if (runner == _lobbyRunner)
            {
                DebugLog($"大厅 Runner 关闭: {shutdownReason}");
                _lobbyRunner = null;
            }
        }

        // 空实现
        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
        public void OnInput(NetworkRunner runner, NetworkInput input) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnConnectedToServer(NetworkRunner runner) { }
        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
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
            Debug.Log($"[RoomListItem] 点击加入房间: {_sessionInfo.Name}");
            _onJoinClicked?.Invoke(_sessionInfo);
        }
    }
}