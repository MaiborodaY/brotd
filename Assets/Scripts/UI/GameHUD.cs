using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Управляет всем Canvas UI в игре.
/// Подключается к событиям через GameEvents — не зависит от других систем напрямую.
/// </summary>
public class GameHUD : MonoBehaviour
{
    [Header("Top Bar")]
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI livesText;
    [SerializeField] private TextMeshProUGUI waveText;
    [SerializeField] private TextMeshProUGUI enemiesText;

    [Header("Bottom Bar — Unit Buttons")]
    [SerializeField] private UnitButton[] unitButtons;    // назначить в инспекторе

    [Header("Prep Phase")]
    [SerializeField] private Button startWaveButton;
    [SerializeField] private GameObject prepPanel;

    [Header("Overlay Panels")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject victoryPanel;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void OnEnable()
    {
        GameEvents.OnGoldChanged           += UpdateGold;
        GameEvents.OnLivesChanged          += UpdateLives;
        GameEvents.OnWaveChanged           += UpdateWave;
        GameEvents.OnEnemiesRemainingChanged += UpdateEnemies;
        GameEvents.OnPrepPhaseStarted      += ShowPrepPhase;
        GameEvents.OnWavePhaseStarted      += ShowWavePhase;
        GameEvents.OnGameOver              += ShowGameOver;
        GameEvents.OnVictory               += ShowVictory;
    }

    private void OnDisable()
    {
        GameEvents.OnGoldChanged           -= UpdateGold;
        GameEvents.OnLivesChanged          -= UpdateLives;
        GameEvents.OnWaveChanged           -= UpdateWave;
        GameEvents.OnEnemiesRemainingChanged -= UpdateEnemies;
        GameEvents.OnPrepPhaseStarted      -= ShowPrepPhase;
        GameEvents.OnWavePhaseStarted      -= ShowWavePhase;
        GameEvents.OnGameOver              -= ShowGameOver;
        GameEvents.OnVictory               -= ShowVictory;
    }

    private void Start()
    {
        // Кнопка старта волны
        if (startWaveButton != null)
            startWaveButton.onClick.AddListener(OnStartWavePressed);

        // Кнопки рестарта в панелях
        WireRestartButton(gameOverPanel);
        WireRestartButton(victoryPanel);

        // Кнопка рестарта в топ-баре (создаём кодом)
        CreateTopRestartButton();

        // Начальные значения
        if (GameStateMachine.Instance != null)
        {
            UpdateGold(GameStateMachine.Instance.CurrentGold);
            UpdateLives(GameStateMachine.Instance.CurrentLives);
        }

        UpdateWave(0);
        UpdateEnemies(0);

        HideOverlays();
        ShowPrepPhase();
    }

    private void WireRestartButton(GameObject panel)
    {
        if (panel == null) return;
        foreach (var btn in panel.GetComponentsInChildren<Button>(true))
        {
            // Пропускаем кнопки с компонентом MainMenuButton — у них свой обработчик
            if (btn.GetComponent<MainMenuButton>() != null) continue;
            btn.onClick.AddListener(OnRestartPressed);
        }
    }

    // ── Top Bar ───────────────────────────────────────────────────────────────

    private void UpdateGold(int gold)
    {
        if (goldText != null) goldText.text = gold.ToString();
    }

    private void UpdateLives(int lives)
    {
        if (livesText != null) livesText.text = lives.ToString();
    }

    private void UpdateWave(int wave)
    {
        if (waveText != null) waveText.text = (wave + 1).ToString();
    }

    private void UpdateEnemies(int count)
    {
        if (enemiesText != null) enemiesText.text = count.ToString();
    }

    // ── Фазы ─────────────────────────────────────────────────────────────────

    [Header("PvP Mode")]
    [SerializeField] private bool isPvPMode = false;

    private void ShowPrepPhase()
    {
        if (prepPanel != null)     prepPanel.SetActive(true);
        if (startWaveButton != null) startWaveButton.gameObject.SetActive(!isPvPMode);

        foreach (var btn in unitButtons)
            btn?.SetInteractable(true);
    }

    private void ShowWavePhase()
    {
        if (prepPanel != null)     prepPanel.SetActive(false);
        if (startWaveButton != null) startWaveButton.gameObject.SetActive(false);

        foreach (var btn in unitButtons)
            btn?.SetInteractable(false);
    }

    // ── Overlay ───────────────────────────────────────────────────────────────

    private void HideOverlays()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (victoryPanel != null)  victoryPanel.SetActive(false);
    }

    private void ShowGameOver()
    {
        if (isPvPMode) return;
        Time.timeScale = 0f;
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
    }

    private void ShowVictory()
    {
        if (isPvPMode) return;
        if (victoryPanel != null) victoryPanel.SetActive(true);
    }

    // ── Кнопки ───────────────────────────────────────────────────────────────

    private void OnStartWavePressed()
    {
        GameStateMachine.Instance?.StartWave();
    }

    /// <summary>Вызывается из UnitButton при нажатии.</summary>
    public void OnUnitButtonPressed(UnitData data)
    {
        PlacementSystem.Instance?.SelectUnit(data);
    }

    /// <summary>Вызывается кнопкой Restart на GameOver/Victory панели.</summary>
    public void OnRestartPressed()
    {
        GameStateMachine.Instance?.Restart();
    }

    private GameObject menuPopup;

    private void CreateTopRestartButton()
    {
        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        float scale = Screen.dpi > 0 ? Screen.dpi / 160f : 1f;
        float btnW  = Mathf.Clamp(120f * scale, 100f, 220f);
        float btnH  = Mathf.Clamp(50f  * scale, 44f,  90f);

        // ── Кнопка "Menu" ────────────────────────────────────────────────────
        var go = new GameObject("MenuButton");
        go.transform.SetParent(canvas.transform, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(1f, 1f);
        rt.anchorMax        = new Vector2(1f, 1f);
        rt.pivot            = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-12f, -(btnH + 60f));
        rt.sizeDelta        = new Vector2(btnW, btnH);

        var img   = go.AddComponent<Image>();
        img.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);

        var btn    = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        colors.pressedColor     = new Color(0.2f, 0.2f, 0.5f, 1f);
        btn.colors = colors;

        var textGo = new GameObject("Label");
        textGo.transform.SetParent(go.transform, false);
        var textRt       = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
        var tmp          = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text         = "Menu";
        tmp.fontSize     = Mathf.Clamp(18f * scale, 16f, 32f);
        tmp.fontStyle    = FontStyles.Bold;
        tmp.color        = Color.white;
        tmp.alignment    = TextAlignmentOptions.Center;

        // ── Попап с вариантами ────────────────────────────────────────────────
        menuPopup = new GameObject("MenuPopup");
        menuPopup.transform.SetParent(canvas.transform, false);

        var popupRt             = menuPopup.AddComponent<RectTransform>();
        popupRt.anchorMin        = new Vector2(1f, 1f);
        popupRt.anchorMax        = new Vector2(1f, 1f);
        popupRt.pivot            = new Vector2(1f, 1f);
        popupRt.anchoredPosition = new Vector2(-12f, -(btnH * 2f + 70f));
        popupRt.sizeDelta        = new Vector2(btnW, btnH * 2.4f);

        var popupImg   = menuPopup.AddComponent<Image>();
        popupImg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

        // Кнопка Main Menu
        AddPopupButton("Main Menu", menuPopup.transform, 0, btnW, btnH, scale,
            new Color(0.2f, 0.4f, 0.8f), SceneLoader.LoadMainMenu);

        // Кнопка Restart
        AddPopupButton("Restart", menuPopup.transform, 1, btnW, btnH, scale,
            new Color(0.5f, 0.2f, 0.2f), OnRestartPressed);

        menuPopup.SetActive(false);

        // Открываем/закрываем попап по нажатию
        btn.onClick.AddListener(() => menuPopup.SetActive(!menuPopup.activeSelf));
    }

    private void AddPopupButton(string label, Transform parent, int index,
        float btnW, float btnH, float scale, Color color,
        UnityEngine.Events.UnityAction action)
    {
        var go  = new GameObject(label + "Btn");
        go.transform.SetParent(parent, false);

        var rt          = go.AddComponent<RectTransform>();
        rt.anchorMin    = new Vector2(0f, 1f);
        rt.anchorMax    = new Vector2(1f, 1f);
        rt.pivot        = new Vector2(0.5f, 1f);
        rt.offsetMin    = new Vector2(4f, -(btnH * 1.1f) * (index + 1) + 4f);
        rt.offsetMax    = new Vector2(-4f, -(btnH * 1.1f) * index - 4f);

        var img   = go.AddComponent<Image>();
        img.color = color;

        var btn = go.AddComponent<Button>();
        btn.onClick.AddListener(action);
        btn.onClick.AddListener(() => menuPopup.SetActive(false));

        var textGo       = new GameObject("Label");
        textGo.transform.SetParent(go.transform, false);
        var textRt       = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
        var tmp          = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text         = label;
        tmp.fontSize     = Mathf.Clamp(16f * scale, 14f, 28f);
        tmp.fontStyle    = FontStyles.Bold;
        tmp.color        = Color.white;
        tmp.alignment    = TextAlignmentOptions.Center;
    }
}
