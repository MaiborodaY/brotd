using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// BroTD → Setup PvP Lobby Scene
/// </summary>
public static class SetupPvPLobby
{
    [MenuItem("BroTD/Setup PvP Lobby Scene")]
    public static void Run()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Камера
        var camGo = new GameObject("Main Camera");
        var cam   = camGo.AddComponent<Camera>();
        cam.clearFlags      = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.08f, 0.08f, 0.12f);
        cam.orthographic    = true;
        camGo.tag           = "MainCamera";

        // Canvas
        var canvasGo = new GameObject("Canvas");
        var canvas   = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(720, 1280);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        // ── Connecting Panel ──────────────────────────────────────────────────
        var connectingPanel = CreatePanel("ConnectingPanel", canvasGo.transform, new Color(0.08f, 0.08f, 0.15f));
        CreateLabel("Label", connectingPanel.transform, "Connecting...", 40, new Vector2(0, 0), new Vector2(600, 80));

        // ── Lobby Panel ───────────────────────────────────────────────────────
        var lobbyPanel = CreatePanel("LobbyPanel", canvasGo.transform, new Color(0.08f, 0.08f, 0.15f));

        CreateLabel("Title", lobbyPanel.transform, "PvP Mode", 60, new Vector2(0, 400), new Vector2(500, 100));

        var inputGo = new GameObject("RoomCodeInput");
        inputGo.transform.SetParent(lobbyPanel.transform, false);
        var inputRect = inputGo.AddComponent<RectTransform>();
        SetRect(inputRect, new Vector2(0, 100), new Vector2(400, 80));
        var inputImg = inputGo.AddComponent<Image>();
        inputImg.color = new Color(0.2f, 0.2f, 0.3f);
        var inputField = inputGo.AddComponent<TMP_InputField>();

        var inputTextGo = new GameObject("Text");
        inputTextGo.transform.SetParent(inputGo.transform, false);
        var inputTextRect = inputTextGo.AddComponent<RectTransform>();
        inputTextRect.anchorMin = Vector2.zero;
        inputTextRect.anchorMax = Vector2.one;
        inputTextRect.offsetMin = new Vector2(10, 0);
        inputTextRect.offsetMax = new Vector2(-10, 0);
        var inputTmp = inputTextGo.AddComponent<TextMeshProUGUI>();
        inputTmp.fontSize  = 36;
        inputTmp.color     = Color.white;
        inputTmp.alignment = TextAlignmentOptions.Center;
        inputField.textComponent = inputTmp;

        var placeholderGo = new GameObject("Placeholder");
        placeholderGo.transform.SetParent(inputGo.transform, false);
        var phRect = placeholderGo.AddComponent<RectTransform>();
        phRect.anchorMin = Vector2.zero;
        phRect.anchorMax = Vector2.one;
        phRect.offsetMin = new Vector2(10, 0);
        phRect.offsetMax = new Vector2(-10, 0);
        var phTmp = placeholderGo.AddComponent<TextMeshProUGUI>();
        phTmp.text      = "Enter room code";
        phTmp.fontSize  = 32;
        phTmp.color     = new Color(0.5f, 0.5f, 0.5f);
        phTmp.alignment = TextAlignmentOptions.Center;
        phTmp.fontStyle = FontStyles.Italic;
        inputField.placeholder = phTmp;

        var createBtn  = CreateButton("CreateRoomButton", lobbyPanel.transform, "Create Room",
            new Color(0.2f, 0.6f, 0.3f), new Vector2(0, -60), new Vector2(380, 90));
        var joinBtn    = CreateButton("JoinRoomButton", lobbyPanel.transform, "Join Room",
            new Color(0.2f, 0.4f, 0.8f), new Vector2(0, -170), new Vector2(380, 90));
        var statusText = CreateLabel("StatusText", lobbyPanel.transform, "", 28,
            new Vector2(0, -280), new Vector2(500, 60));
        statusText.GetComponent<TextMeshProUGUI>().color = new Color(1f, 0.4f, 0.4f);

        var backBtn = CreateButton("BackButton", lobbyPanel.transform, "Back to Menu",
            new Color(0.3f, 0.3f, 0.3f), new Vector2(0, -380), new Vector2(300, 70));
        backBtn.GetComponent<Button>().onClick.AddListener(SceneLoader.LoadMainMenu);

        // ── Waiting Panel ─────────────────────────────────────────────────────
        var waitingPanel = CreatePanel("WaitingPanel", canvasGo.transform, new Color(0.08f, 0.08f, 0.15f));
        var roomCodeDisplay = CreateLabel("RoomCodeDisplay", waitingPanel.transform,
            "Room code:\n------", 40, new Vector2(0, 100), new Vector2(500, 200));
        var cancelBtn = CreateButton("CancelButton", waitingPanel.transform, "Cancel",
            new Color(0.6f, 0.2f, 0.2f), new Vector2(0, -150), new Vector2(300, 80));

        // ── PvPLobby компонент ────────────────────────────────────────────────
        var lobbyScript = canvasGo.AddComponent<PvPLobby>();
        var so = new SerializedObject(lobbyScript);
        so.FindProperty("connectingPanel").objectReferenceValue  = connectingPanel;
        so.FindProperty("lobbyPanel").objectReferenceValue       = lobbyPanel;
        so.FindProperty("waitingPanel").objectReferenceValue     = waitingPanel;
        so.FindProperty("roomCodeInput").objectReferenceValue    = inputField;
        so.FindProperty("createRoomButton").objectReferenceValue = createBtn.GetComponent<Button>();
        so.FindProperty("joinRoomButton").objectReferenceValue   = joinBtn.GetComponent<Button>();
        so.FindProperty("statusText").objectReferenceValue       = statusText.GetComponent<TextMeshProUGUI>();
        so.FindProperty("roomCodeDisplay").objectReferenceValue  = roomCodeDisplay.GetComponent<TextMeshProUGUI>();
        so.FindProperty("cancelButton").objectReferenceValue     = cancelBtn.GetComponent<Button>();
        so.ApplyModifiedProperties();

        // EventSystem
        var es = new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        var esType = System.Type.GetType(
            "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (esType != null) es.AddComponent(esType);
        else es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // Сохраняем
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/PvPLobby.unity");
        AddScenesToBuild();
        Debug.Log("[SetupPvPLobby] PvPLobby сцена создана.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static GameObject CreatePanel(string name, Transform parent, Color color)
    {
        var go   = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img  = go.AddComponent<Image>();
        img.color = color;
        var rt   = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return go;
    }

    private static GameObject CreateLabel(string name, Transform parent, string text,
        int fontSize, Vector2 pos, Vector2 size)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
        SetRect(go.GetComponent<RectTransform>(), pos, size);
        return go;
    }

    private static GameObject CreateButton(string name, Transform parent, string label,
        Color color, Vector2 pos, Vector2 size)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        go.AddComponent<Button>();
        SetRect(go.GetComponent<RectTransform>(), pos, size);

        var textGo = new GameObject("Label");
        textGo.transform.SetParent(go.transform, false);
        var tmp    = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 32;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
        var tRect     = textGo.GetComponent<RectTransform>();
        tRect.anchorMin = Vector2.zero;
        tRect.anchorMax = Vector2.one;
        tRect.offsetMin = Vector2.zero;
        tRect.offsetMax = Vector2.zero;
        return go;
    }

    private static void SetRect(RectTransform rt, Vector2 anchoredPos, Vector2 size)
    {
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = size;
    }

    private static void AddScenesToBuild()
    {
        var scenes = new[]
        {
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/Main.unity",
            "Assets/Scenes/PvPLobby.unity",
        };
        var buildScenes = new EditorBuildSettingsScene[scenes.Length];
        for (int i = 0; i < scenes.Length; i++)
            buildScenes[i] = new EditorBuildSettingsScene(scenes[i], true);
        EditorBuildSettings.scenes = buildScenes;
    }
}
