using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// BroTD → Setup PvP Scene
/// Копирует Main.unity, убирает WaveManager, добавляет PvP UI и PvPGameManager.
/// </summary>
public static class SetupPvPScene
{
    [MenuItem("BroTD/Setup PvP Scene")]
    public static void Run()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        // Копируем Main.unity → PvP.unity
        const string src = "Assets/Scenes/Main.unity";
        const string dst = "Assets/Scenes/PvP.unity";

        if (!System.IO.File.Exists(src.Replace('/', '\\')))
        {
            Debug.LogError("[SetupPvPScene] Main.unity не найден!");
            return;
        }

        AssetDatabase.CopyAsset(src, dst);
        AssetDatabase.Refresh();

        // Открываем копию
        var scene = EditorSceneManager.OpenScene(dst, OpenSceneMode.Single);

        // ── Убираем WaveManager ───────────────────────────────────────────────
        foreach (var go in scene.GetRootGameObjects())
        {
            if (go.name == "[WaveManager]")
            {
                Object.DestroyImmediate(go);
                break;
            }
        }

        // ── Находим Canvas ────────────────────────────────────────────────────
        Canvas canvas = null;
        foreach (var go in scene.GetRootGameObjects())
        {
            canvas = go.GetComponent<Canvas>();
            if (canvas != null) break;
        }

        if (canvas == null)
        {
            Debug.LogError("[SetupPvPScene] Canvas не найден в сцене!");
            return;
        }

        // ── Находим PrepPanel чтобы добавить кнопку Ready рядом ──────────────
        var prepPanel = canvas.transform.Find("PrepPanel");

        // ── Создаём PvP UI панель ─────────────────────────────────────────────
        var pvpUI = new GameObject("[PvPUI]");
        pvpUI.transform.SetParent(canvas.transform, false);

        // Статус текст (сверху по центру)
        var statusGo  = CreateText("StatusText", pvpUI.transform, "Build your army!", 32);
        var statusRect = statusGo.GetComponent<RectTransform>();
        statusRect.anchorMin        = new Vector2(0.5f, 1f);
        statusRect.anchorMax        = new Vector2(0.5f, 1f);
        statusRect.pivot            = new Vector2(0.5f, 1f);
        statusRect.anchoredPosition = new Vector2(0f, -110f);
        statusRect.sizeDelta        = new Vector2(500f, 50f);
        statusGo.GetComponent<TextMeshProUGUI>().color = new Color(1f, 0.9f, 0.3f);

        // HP короля противника
        var oppHpGo   = CreateText("OpponentKingHp", pvpUI.transform, "Enemy King: ???", 28);
        var oppHpRect = oppHpGo.GetComponent<RectTransform>();
        oppHpRect.anchorMin        = new Vector2(0f, 1f);
        oppHpRect.anchorMax        = new Vector2(0f, 1f);
        oppHpRect.pivot            = new Vector2(0f, 1f);
        oppHpRect.anchoredPosition = new Vector2(10f, -110f);
        oppHpRect.sizeDelta        = new Vector2(280f, 45f);
        oppHpGo.GetComponent<TextMeshProUGUI>().color = new Color(1f, 0.4f, 0.4f);

        // Кнопка Ready (вместо Start Wave или рядом)
        var readyGo   = CreateButton("ReadyButton", pvpUI.transform, "READY",
            new Color(0.15f, 0.6f, 0.25f));
        var readyRect = readyGo.GetComponent<RectTransform>();
        readyRect.anchorMin        = new Vector2(0.5f, 0f);
        readyRect.anchorMax        = new Vector2(0.5f, 0f);
        readyRect.pivot            = new Vector2(0.5f, 0f);
        readyRect.anchoredPosition = new Vector2(120f, 20f);
        readyRect.sizeDelta        = new Vector2(220f, 80f);

        // Waiting overlay
        var waitingGo = new GameObject("WaitingOverlay");
        waitingGo.transform.SetParent(pvpUI.transform, false);
        var waitImg   = waitingGo.AddComponent<Image>();
        waitImg.color = new Color(0f, 0f, 0f, 0.6f);
        var waitRect  = waitingGo.GetComponent<RectTransform>();
        waitRect.anchorMin = Vector2.zero;
        waitRect.anchorMax = Vector2.one;
        waitRect.offsetMin = Vector2.zero;
        waitRect.offsetMax = Vector2.zero;
        var waitText  = CreateText("WaitText", waitingGo.transform, "Waiting for opponent...", 40);
        var wtRect    = waitText.GetComponent<RectTransform>();
        wtRect.anchorMin = new Vector2(0.5f, 0.5f);
        wtRect.anchorMax = new Vector2(0.5f, 0.5f);
        wtRect.sizeDelta = new Vector2(600f, 80f);
        wtRect.anchoredPosition = Vector2.zero;
        waitingGo.SetActive(false);

        // Game Over панель
        var gameOverGo  = new GameObject("PvPGameOverPanel");
        gameOverGo.transform.SetParent(pvpUI.transform, false);
        var goImg       = gameOverGo.AddComponent<Image>();
        goImg.color     = new Color(0f, 0f, 0f, 0.85f);
        var goRect      = gameOverGo.GetComponent<RectTransform>();
        goRect.anchorMin = Vector2.zero;
        goRect.anchorMax = Vector2.one;
        goRect.offsetMin = Vector2.zero;
        goRect.offsetMax = Vector2.zero;

        var gameOverText = CreateText("GameOverText", gameOverGo.transform, "Game Over", 56);
        var gotRect      = gameOverText.GetComponent<RectTransform>();
        gotRect.anchorMin        = new Vector2(0.5f, 0.5f);
        gotRect.anchorMax        = new Vector2(0.5f, 0.5f);
        gotRect.sizeDelta        = new Vector2(600f, 120f);
        gotRect.anchoredPosition = new Vector2(0f, 80f);

        var menuBtn = CreateButton("MainMenuBtn", gameOverGo.transform,
            "Main Menu", new Color(0.2f, 0.4f, 0.8f));
        var mbRect  = menuBtn.GetComponent<RectTransform>();
        mbRect.anchorMin        = new Vector2(0.5f, 0.5f);
        mbRect.anchorMax        = new Vector2(0.5f, 0.5f);
        mbRect.sizeDelta        = new Vector2(300f, 80f);
        mbRect.anchoredPosition = new Vector2(0f, -60f);
        menuBtn.GetComponent<Button>().onClick.AddListener(SceneLoader.LoadMainMenu);
        menuBtn.AddComponent<MainMenuButton>();

        gameOverGo.SetActive(false);

        // ── PvPGameManager объект ─────────────────────────────────────────────
        var mgrGo  = new GameObject("[PvPGameManager]");
        var mgr    = mgrGo.AddComponent<PvPGameManager>();

        var so     = new SerializedObject(mgr);
        so.FindProperty("readyButton").objectReferenceValue        = readyGo.GetComponent<Button>();
        so.FindProperty("statusText").objectReferenceValue         = statusGo.GetComponent<TextMeshProUGUI>();
        so.FindProperty("opponentKingHpText").objectReferenceValue = oppHpGo.GetComponent<TextMeshProUGUI>();
        so.FindProperty("waitingOverlay").objectReferenceValue     = waitingGo;
        so.FindProperty("gameOverPanel").objectReferenceValue      = gameOverGo;
        so.FindProperty("gameOverText").objectReferenceValue       = gameOverText.GetComponent<TextMeshProUGUI>();
        so.ApplyModifiedProperties();

        // ── Build Settings ────────────────────────────────────────────────────
        var buildScenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity",  true),
            new EditorBuildSettingsScene("Assets/Scenes/Main.unity",      true),
            new EditorBuildSettingsScene("Assets/Scenes/PvPLobby.unity",  true),
            new EditorBuildSettingsScene("Assets/Scenes/PvP.unity",       true),
        };
        EditorBuildSettings.scenes = buildScenes;

        EditorSceneManager.SaveScene(scene, dst);
        Debug.Log("[SetupPvPScene] PvP сцена создана. Назначь KingUnit, PathData и AllUnitDatas в [PvPGameManager].");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static GameObject CreateText(string name, Transform parent, string text, int size)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
        go.AddComponent<RectTransform>();
        return go;
    }

    private static GameObject CreateButton(string name, Transform parent, string label, Color color)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        go.AddComponent<Button>();

        var textGo = new GameObject("Label");
        textGo.transform.SetParent(go.transform, false);
        var tmp    = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 34;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
        var tRect     = textGo.GetComponent<RectTransform>();
        tRect.anchorMin = Vector2.zero;
        tRect.anchorMax = Vector2.one;
        tRect.offsetMin = Vector2.zero;
        tRect.offsetMax = Vector2.zero;
        return go;
    }
}
