using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// BroTD → Setup Main Menu Scene
/// Создаёт сцену главного меню с кнопками Single Player и PvP.
/// </summary>
public static class SetupMainMenu
{
    [MenuItem("BroTD/Setup Main Menu Scene")]
    public static void Run()
    {
        // Сохраняем текущую сцену перед тем как что-то делать
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.Log("[SetupMainMenu] Отменено пользователем.");
            return;
        }

        // Создаём новую сцену
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "MainMenu";

        // Камера
        var camGo = new GameObject("Main Camera");
        var cam   = camGo.AddComponent<Camera>();
        cam.clearFlags       = CameraClearFlags.SolidColor;
        cam.backgroundColor  = new Color(0.08f, 0.08f, 0.12f);
        cam.orthographic     = true;
        camGo.tag            = "MainCamera";

        // Canvas
        var canvasGo = new GameObject("Canvas");
        var canvas   = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(720, 1280);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        // Фон
        var bg    = CreateImage("Background", canvasGo.transform, new Color(0.08f, 0.08f, 0.15f));
        var bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // Заголовок
        var title = CreateText("Title", canvasGo.transform, "BRO TD", 72, FontStyle.Bold);
        var titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin        = new Vector2(0.5f, 0.75f);
        titleRect.anchorMax        = new Vector2(0.5f, 0.75f);
        titleRect.sizeDelta        = new Vector2(600, 120);
        titleRect.anchoredPosition = Vector2.zero;
        title.GetComponent<TextMeshProUGUI>().color = new Color(1f, 0.8f, 0.2f);

        // Подзаголовок
        var sub = CreateText("Subtitle", canvasGo.transform, "Tower Defense", 36, FontStyle.Normal);
        var subRect = sub.GetComponent<RectTransform>();
        subRect.anchorMin        = new Vector2(0.5f, 0.65f);
        subRect.anchorMax        = new Vector2(0.5f, 0.65f);
        subRect.sizeDelta        = new Vector2(500, 60);
        subRect.anchoredPosition = Vector2.zero;
        sub.GetComponent<TextMeshProUGUI>().color = new Color(0.7f, 0.7f, 0.7f);

        // Кнопка Single Player
        var spBtn = CreateButton("SinglePlayerButton", canvasGo.transform,
            "Single Player", new Color(0.2f, 0.5f, 0.9f));
        var spRect = spBtn.GetComponent<RectTransform>();
        spRect.anchorMin        = new Vector2(0.5f, 0.5f);
        spRect.anchorMax        = new Vector2(0.5f, 0.5f);
        spRect.sizeDelta        = new Vector2(400, 100);
        spRect.anchoredPosition = new Vector2(0, 40);

        // Кнопка PvP
        var pvpBtn = CreateButton("PvPButton", canvasGo.transform,
            "PvP Mode", new Color(0.8f, 0.3f, 0.2f));
        var pvpRect = pvpBtn.GetComponent<RectTransform>();
        pvpRect.anchorMin        = new Vector2(0.5f, 0.5f);
        pvpRect.anchorMax        = new Vector2(0.5f, 0.5f);
        pvpRect.sizeDelta        = new Vector2(400, 100);
        pvpRect.anchoredPosition = new Vector2(0, -80);

        // MainMenuUI компонент
        var menuUI = canvasGo.AddComponent<MainMenuUI>();
        var so     = new SerializedObject(menuUI);
        so.FindProperty("singlePlayerButton").objectReferenceValue = spBtn.GetComponent<Button>();
        so.FindProperty("pvpButton").objectReferenceValue          = pvpBtn.GetComponent<Button>();
        so.ApplyModifiedProperties();

        // EventSystem — используем InputSystemUIInputModule для нового Input System
        var es = new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        var esType = System.Type.GetType(
            "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (esType != null)
            es.AddComponent(esType);
        else
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // Сохраняем сцену
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainMenu.unity");

        // Добавляем все сцены в Build Settings
        AddScenesToBuild();

        Debug.Log("[SetupMainMenu] MainMenu сцена создана. Добавь её первой в Build Settings.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static GameObject CreateImage(string name, Transform parent, Color color)
    {
        var go   = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img  = go.AddComponent<Image>();
        img.color = color;
        return go;
    }

    private static GameObject CreateText(string name, Transform parent, string text, int size, FontStyle style)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.alignment = TextAlignmentOptions.Center;
        return go;
    }

    private static GameObject CreateButton(string name, Transform parent, string label, Color color)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);

        var img = go.AddComponent<Image>();
        img.color = color;

        var btn = go.AddComponent<Button>();
        var colors      = btn.colors;
        colors.highlightedColor = color * 1.2f;
        colors.pressedColor     = color * 0.8f;
        btn.colors      = colors;

        // Текст кнопки
        var textGo  = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var tmp     = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 36;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;

        var textRect    = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return go;
    }

    private static void AddScenesToBuild()
    {
        var scenes = new[]
        {
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/Main.unity",
        };

        var buildScenes = new EditorBuildSettingsScene[scenes.Length];
        for (int i = 0; i < scenes.Length; i++)
            buildScenes[i] = new EditorBuildSettingsScene(scenes[i], true);

        EditorBuildSettings.scenes = buildScenes;
    }
}
