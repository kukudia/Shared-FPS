using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using Fusion;


/// <summary>
/// 编辑器工具 - 自动创建大厅UI Canvas
/// 使用方法: 在Unity菜单栏选择 Fusion > Create Lobby UI
/// </summary>
public class FusionLobbyUICreator : Editor
{
    [MenuItem("Fusion/Create Lobby UI")]
    public static void CreateLobbyUI()
    {
        // 创建Canvas
        var canvasGO = new GameObject("LobbyCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // 创建主面板
        var lobbyPanel = CreatePanel(canvasGO.transform, "LobbyPanel", new Vector2(400, 600));

        // 创建标题
        CreateText(lobbyPanel.transform, "TitleText", "Lobby", 36, new Vector2(0, 250));

        // 创建昵称输入
        CreateText(lobbyPanel.transform, "NicknameLabel", "Nickname:", 20, new Vector2(-120, 180));
        var nicknameInput = CreateInputField(lobbyPanel.transform, "NicknameInput", "Input nickname...", new Vector2(50, 180), new Vector2(200, 40));

        // 创建房间名输入
        CreateText(lobbyPanel.transform, "RoomNameLabel", "Room name:", 20, new Vector2(-120, 120));
        var roomNameInput = CreateInputField(lobbyPanel.transform, "RoomNameInput", "Input room name...", new Vector2(50, 120), new Vector2(200, 40));

        // 创建最大玩家数输入
        CreateText(lobbyPanel.transform, "MaxPlayersLabel", "Maxium player:", 20, new Vector2(-120, 60));
        var maxPlayersInput = CreateInputField(lobbyPanel.transform, "MaxPlayersInput", "4", new Vector2(50, 60), new Vector2(100, 40));

        // 创建按钮
        var createButton = CreateButton(lobbyPanel.transform, "CreateRoomButton", "Create room", new Vector2(0, -10), new Vector2(200, 50));
        var refreshButton = CreateButton(lobbyPanel.transform, "RefreshButton", "Refresh list", new Vector2(0, -70), new Vector2(200, 50));

        // 创建状态文本
        var statusText = CreateText(lobbyPanel.transform, "StatusText", "Input nickname", 16, new Vector2(0, -130));

        // 创建房间列表面板
        var roomListPanel = CreatePanel(lobbyPanel.transform, "RoomListPanel", new Vector2(380, 200));
        roomListPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -280);

        // 创建Scroll View
        var scrollView = CreateScrollView(roomListPanel.transform, "RoomListScrollView");

        // 创建加载指示器
        var loadingIndicator = CreateText(lobbyPanel.transform, "LoadingIndicator", "Loading...", 20, new Vector2(0, 0));
        loadingIndicator.SetActive(false);

        // 创建房间项Prefab
        var roomItemPrefab = CreateRoomItemPrefab();

        // 查找并配置FusionLobbyUI组件
        var fusionBootstrap = FindObjectOfType<FusionBootstrap>();
        if (fusionBootstrap != null)
        {
            var lobbyUI = fusionBootstrap.GetComponent<FusionLobbyUI>();
            if (lobbyUI == null)
            {
                lobbyUI = fusionBootstrap.gameObject.AddComponent<FusionLobbyUI>();
            }

            // 使用SerializedObject来设置私有字段
            var serializedObject = new SerializedObject(lobbyUI);

            serializedObject.FindProperty("lobbyPanel").objectReferenceValue = lobbyPanel;
            serializedObject.FindProperty("roomListPanel").objectReferenceValue = roomListPanel;
            serializedObject.FindProperty("nicknameInput").objectReferenceValue = nicknameInput.GetComponent<TMP_InputField>();
            serializedObject.FindProperty("roomNameInput").objectReferenceValue = roomNameInput.GetComponent<TMP_InputField>();
            serializedObject.FindProperty("maxPlayersInput").objectReferenceValue = maxPlayersInput.GetComponent<TMP_InputField>();
            serializedObject.FindProperty("createRoomButton").objectReferenceValue = createButton.GetComponent<Button>();
            serializedObject.FindProperty("refreshButton").objectReferenceValue = refreshButton.GetComponent<Button>();
            serializedObject.FindProperty("statusText").objectReferenceValue = statusText.GetComponent<TMP_Text>();
            serializedObject.FindProperty("loadingIndicator").objectReferenceValue = loadingIndicator;
            serializedObject.FindProperty("roomListContent").objectReferenceValue = scrollView.transform.Find("Viewport/Content");
            serializedObject.FindProperty("roomItemPrefab").objectReferenceValue = roomItemPrefab;

            serializedObject.ApplyModifiedProperties();

            Debug.Log("FusionLobbyUI 已配置完成！");
        }
        else
        {
            Debug.LogWarning("未找到FusionBootstrap组件，请手动配置FusionLobbyUI的引用。");
        }

        Selection.activeGameObject = canvasGO;
        Debug.Log("大厅UI创建完成！");
    }

    private static GameObject CreatePanel(Transform parent, string name, Vector2 size)
    {
        var panel = new GameObject(name);
        panel.transform.SetParent(parent, false);

        var rectTransform = panel.AddComponent<RectTransform>();
        rectTransform.sizeDelta = size;

        var image = panel.AddComponent<Image>();
        image.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

        return panel;
    }

    private static GameObject CreateText(Transform parent, string name, string text, int fontSize, Vector2 position)
    {
        var textGO = new GameObject(name);
        textGO.transform.SetParent(parent, false);

        var rectTransform = textGO.AddComponent<RectTransform>();
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = new Vector2(300, 50);

        var tmpText = textGO.AddComponent<TextMeshProUGUI>();
        tmpText.text = text;
        tmpText.fontSize = fontSize;
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.color = Color.white;

        return textGO;
    }

    private static GameObject CreateInputField(Transform parent, string name, string placeholder, Vector2 position, Vector2 size)
    {
        var inputGO = new GameObject(name);
        inputGO.transform.SetParent(parent, false);

        var rectTransform = inputGO.AddComponent<RectTransform>();
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = size;

        var image = inputGO.AddComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        var inputField = inputGO.AddComponent<TMP_InputField>();

        // 创建文本区域
        var textArea = new GameObject("Text Area");
        textArea.transform.SetParent(inputGO.transform, false);
        var textAreaRect = textArea.AddComponent<RectTransform>();
        textAreaRect.anchorMin = Vector2.zero;
        textAreaRect.anchorMax = Vector2.one;
        textAreaRect.offsetMin = new Vector2(10, 5);
        textAreaRect.offsetMax = new Vector2(-10, -5);
        textArea.AddComponent<RectMask2D>();

        // 创建Placeholder
        var placeholderGO = new GameObject("Placeholder");
        placeholderGO.transform.SetParent(textArea.transform, false);
        var placeholderRect = placeholderGO.AddComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.offsetMin = Vector2.zero;
        placeholderRect.offsetMax = Vector2.zero;
        var placeholderText = placeholderGO.AddComponent<TextMeshProUGUI>();
        placeholderText.text = placeholder;
        placeholderText.fontSize = 18;
        placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        placeholderText.alignment = TextAlignmentOptions.Left;

        // 创建Text
        var textGO = new GameObject("Text");
        textGO.transform.SetParent(textArea.transform, false);
        var textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        var inputText = textGO.AddComponent<TextMeshProUGUI>();
        inputText.fontSize = 18;
        inputText.color = Color.white;
        inputText.alignment = TextAlignmentOptions.Left;

        // 配置InputField
        inputField.textViewport = textAreaRect;
        inputField.textComponent = inputText;
        inputField.placeholder = placeholderText;

        return inputGO;
    }

    private static GameObject CreateButton(Transform parent, string name, string text, Vector2 position, Vector2 size)
    {
        var buttonGO = new GameObject(name);
        buttonGO.transform.SetParent(parent, false);

        var rectTransform = buttonGO.AddComponent<RectTransform>();
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = size;

        var image = buttonGO.AddComponent<Image>();
        image.color = new Color(0.2f, 0.4f, 0.8f, 1f);

        var button = buttonGO.AddComponent<Button>();
        button.targetGraphic = image;

        // 设置按钮颜色状态
        var colors = button.colors;
        colors.normalColor = new Color(0.2f, 0.4f, 0.8f, 1f);
        colors.highlightedColor = new Color(0.3f, 0.5f, 0.9f, 1f);
        colors.pressedColor = new Color(0.1f, 0.3f, 0.7f, 1f);
        button.colors = colors;

        // 创建按钮文字
        var textGO = new GameObject("Text");
        textGO.transform.SetParent(buttonGO.transform, false);
        var textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        var tmpText = textGO.AddComponent<TextMeshProUGUI>();
        tmpText.text = text;
        tmpText.fontSize = 20;
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.color = Color.white;

        return buttonGO;
    }

    private static GameObject CreateScrollView(Transform parent, string name)
    {
        var scrollGO = new GameObject(name);
        scrollGO.transform.SetParent(parent, false);

        var scrollRect = scrollGO.AddComponent<RectTransform>();
        scrollRect.anchorMin = Vector2.zero;
        scrollRect.anchorMax = Vector2.one;
        scrollRect.offsetMin = new Vector2(10, 10);
        scrollRect.offsetMax = new Vector2(-10, -10);

        var scrollView = scrollGO.AddComponent<ScrollRect>();
        scrollView.horizontal = false;
        scrollView.vertical = true;

        scrollGO.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 1f);

        // Viewport
        var viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollGO.transform, false);
        var viewportRect = viewport.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        viewport.AddComponent<Mask>().showMaskGraphic = false;
        viewport.AddComponent<Image>();

        // Content
        var content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        var contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.sizeDelta = new Vector2(0, 0);

        var layoutGroup = content.AddComponent<VerticalLayoutGroup>();
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.spacing = 5;
        layoutGroup.padding = new RectOffset(5, 5, 5, 5);

        var sizeFitter = content.AddComponent<ContentSizeFitter>();
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollView.viewport = viewportRect;
        scrollView.content = contentRect;

        return scrollGO;
    }

    private static GameObject CreateRoomItemPrefab()
    {
        var itemGO = new GameObject("RoomItemPrefab");

        var rectTransform = itemGO.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(350, 60);

        var image = itemGO.AddComponent<Image>();
        image.color = new Color(0.25f, 0.25f, 0.25f, 1f);

        var layoutGroup = itemGO.AddComponent<HorizontalLayoutGroup>();
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = true;
        layoutGroup.spacing = 10;
        layoutGroup.padding = new RectOffset(10, 10, 5, 5);

        // 房间名
        var roomNameGO = new GameObject("RoomNameText");
        roomNameGO.transform.SetParent(itemGO.transform, false);
        var roomNameRect = roomNameGO.AddComponent<RectTransform>();
        var roomNameLayout = roomNameGO.AddComponent<LayoutElement>();
        roomNameLayout.flexibleWidth = 1;
        var roomNameText = roomNameGO.AddComponent<TextMeshProUGUI>();
        roomNameText.text = "Room name";
        roomNameText.fontSize = 18;
        roomNameText.alignment = TextAlignmentOptions.Left;
        roomNameText.color = Color.white;

        // 玩家数
        var playerCountGO = new GameObject("PlayerCountText");
        playerCountGO.transform.SetParent(itemGO.transform, false);
        var playerCountRect = playerCountGO.AddComponent<RectTransform>();
        var playerCountLayout = playerCountGO.AddComponent<LayoutElement>();
        playerCountLayout.preferredWidth = 60;
        var playerCountText = playerCountGO.AddComponent<TextMeshProUGUI>();
        playerCountText.text = "0/4";
        playerCountText.fontSize = 16;
        playerCountText.alignment = TextAlignmentOptions.Center;
        playerCountText.color = Color.white;

        // 加入按钮
        var joinButtonGO = new GameObject("JoinButton");
        joinButtonGO.transform.SetParent(itemGO.transform, false);
        var joinButtonRect = joinButtonGO.AddComponent<RectTransform>();
        var joinButtonLayout = joinButtonGO.AddComponent<LayoutElement>();
        joinButtonLayout.preferredWidth = 80;
        var joinButtonImage = joinButtonGO.AddComponent<Image>();
        joinButtonImage.color = new Color(0.2f, 0.6f, 0.2f, 1f);
        var joinButton = joinButtonGO.AddComponent<Button>();
        joinButton.targetGraphic = joinButtonImage;

        var joinTextGO = new GameObject("Text");
        joinTextGO.transform.SetParent(joinButtonGO.transform, false);
        var joinTextRect = joinTextGO.AddComponent<RectTransform>();
        joinTextRect.anchorMin = Vector2.zero;
        joinTextRect.anchorMax = Vector2.one;
        joinTextRect.offsetMin = Vector2.zero;
        joinTextRect.offsetMax = Vector2.zero;
        var joinText = joinTextGO.AddComponent<TextMeshProUGUI>();
        joinText.text = "Join";
        joinText.fontSize = 16;
        joinText.alignment = TextAlignmentOptions.Center;
        joinText.color = Color.white;

        // 添加RoomListItem组件
        var roomListItem = itemGO.AddComponent<RoomListItem>();

        // 使用SerializedObject设置引用
        var serializedObject = new SerializedObject(roomListItem);
        serializedObject.FindProperty("roomNameText").objectReferenceValue = roomNameText;
        serializedObject.FindProperty("playerCountText").objectReferenceValue = playerCountText;
        serializedObject.FindProperty("joinButton").objectReferenceValue = joinButton;
        serializedObject.ApplyModifiedProperties();

        // 保存为Prefab
        string prefabPath = "Assets/RoomItemPrefab.prefab";
        PrefabUtility.SaveAsPrefabAsset(itemGO, prefabPath);
        DestroyImmediate(itemGO);

        return AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
    }
}
#endif
