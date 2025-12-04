using DG.Tweening;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Projectiles
{
    /// <summary>
    /// 准备系统UI
    /// - 房主看到: "开始游戏 (X/Y)" 按钮，X是准备人数，Y是总玩家数
    /// - 其他玩家看到: "准备" / "取消准备" 按钮
    /// </summary>
    public class ReadyUI : MonoBehaviour
    {
        public FusionLobbyUI lobbyUI;

        [Header("UI References")]
        [SerializeField] private CanvasGroup readyPanel;
        [SerializeField] private Button readyButton;
        [SerializeField] private TMP_Text buttonText;
        [SerializeField] private TMP_Text statusText;

        [Header("Button Colors (Optional)")]
        [SerializeField] private Color readyColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color notReadyColor = new Color(0.8f, 0.2f, 0.2f);
        [SerializeField] private Color hostColor = new Color(0.2f, 0.5f, 0.8f);
        [SerializeField] private Color disabledColor = new Color(0.5f, 0.5f, 0.5f);

        [Header("Text Settings")]
        private string hostButtonText = "Start";
        private string readyText = "Ready";
        private string cancelReadyText = "Cancel Ready";
        private string waitingText = "Waiting for other players ready...";
        private string allReadyText = "All players are ready!";

        private ReadySystem _readySystem;
        private Image _buttonImage;
        private bool _isInitialized;

        // UNITY LIFECYCLE

        private void Start()
        {
            readyPanel.alpha = 0f;
            if (readyButton != null)
            {
                readyButton.onClick.AddListener(OnButtonClicked);
                _buttonImage = readyButton.GetComponent<Image>();
            }

            // 尝试查找ReadySystem
            TryInitialize();
        }

        private void Update()
        {
            // 如果还没初始化，持续尝试
            if (!_isInitialized)
            {
                TryInitialize();
            }
        }

        private void OnDestroy()
        {
            if (_readySystem != null)
            {
                _readySystem.OnReadyStateChanged -= UpdateUI;
                _readySystem.OnGameStarted -= OnGameStarted;
            }
        }

        // PRIVATE METHODS

        private void TryInitialize()
        {
            // 尝试通过Context获取
            var contextBehaviours = FindObjectsOfType<ContextBehaviour>();
            foreach (var cb in contextBehaviours)
            {
                if (cb.Context != null && cb.Context.ReadySystem != null)
                {
                    _readySystem = cb.Context.ReadySystem;
                    break;
                }
            }

            // 如果Context中没有，直接查找
            if (_readySystem == null)
            {
                _readySystem = FindObjectOfType<ReadySystem>();
            }

            if (_readySystem != null)
            {
                _isInitialized = true;
                _readySystem.OnReadyStateChanged += UpdateUI;
                _readySystem.OnGameStarted += OnGameStarted;

                if (readyPanel != null)
                    readyPanel.SetActive(true);

                UpdateUI();
                Debug.Log("[ReadyUI] Initialized successfully");
            }
        }

        private void OnButtonClicked()
        {
            if (_readySystem == null) return;

            if (_readySystem.IsLocalPlayerHost())
            {
                // 房主点击开始游戏
                if (_readySystem.AreAllPlayersReady())
                {
                    _readySystem.StartGame();
                }
            }
            else
            {
                // 其他玩家切换准备状态
                _readySystem.ToggleReady();
            }
        }

        private void UpdateUI()
        {
            if (_readySystem == null) return;

            int readyCount = _readySystem.GetReadyCount();
            int playerCount = _readySystem.GetPlayerCount();
            bool isHost = _readySystem.IsLocalPlayerHost();
            bool isReady = _readySystem.IsLocalPlayerReady();
            bool allReady = _readySystem.AreAllPlayersReady();

            // 更新按钮文本
            if (buttonText != null)
            {
                if (isHost)
                {
                    // 房主显示: "开始游戏 (准备人数/总人数)"
                    buttonText.text = $"{hostButtonText} ({readyCount}/{playerCount})";
                }
                else
                {
                    // 其他玩家显示: 准备/取消准备
                    buttonText.text = isReady ? cancelReadyText : readyText;
                }
            }

            // 更新按钮颜色
            if (_buttonImage != null)
            {
                if (isHost)
                {
                    _buttonImage.color = allReady ? hostColor : disabledColor;
                }
                else
                {
                    _buttonImage.color = isReady ? readyColor : notReadyColor;
                }
            }

            // 更新按钮交互状态
            if (readyButton != null)
            {
                if (isHost)
                {
                    // 房主只有在所有人准备好后才能点击
                    //readyButton.interactable = allReady && playerCount > 1;
                    readyButton.interactable = allReady;
                }
                else
                {
                    // 其他玩家随时可以切换
                    readyButton.interactable = true;
                }
            }

            // 更新状态文本
            if (statusText != null)
            {
                if (playerCount <= 1)
                {
                    statusText.text = "Waiting for other players to join...";
                }
                else if (allReady)
                {
                    statusText.text = allReadyText;
                }
                else
                {
                    int otherPlayersReady = isHost ? readyCount - 1 : readyCount;
                    int otherPlayersTotal = isHost ? playerCount - 1 : playerCount;
                    statusText.text = $"{waitingText} ({otherPlayersReady}/{otherPlayersTotal})";
                }
            }

            lobbyUI.ShowLoading(false);
            SetPanelVisualization(readyPanel, 255f, true);
        }

        private void OnGameStarted()
        {
            // 游戏开始后隐藏准备界面
            if (readyPanel != null)
            {
                readyPanel.SetActive(false);
            }

            Debug.Log("[ReadyUI] Game started, hiding ready panel");
        }

        // PUBLIC METHODS

        /// <summary>
        /// 手动设置ReadySystem引用
        /// </summary>
        public void SetReadySystem(ReadySystem system)
        {
            if (_readySystem != null)
            {
                _readySystem.OnReadyStateChanged -= UpdateUI;
                _readySystem.OnGameStarted -= OnGameStarted;
            }

            _readySystem = system;

            if (_readySystem != null)
            {
                _isInitialized = true;
                _readySystem.OnReadyStateChanged += UpdateUI;
                _readySystem.OnGameStarted += OnGameStarted;
                UpdateUI();
            }
        }

        /// <summary>
        /// 显示/隐藏准备面板
        /// </summary>
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
    }
}