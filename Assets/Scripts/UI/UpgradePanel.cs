using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Панель апгрейда юнита. Появляется при тапе по установленному юниту в фазу подготовки.
/// Создаётся автоматически из GameHUD.
/// </summary>
public class UpgradePanel : MonoBehaviour
{
    [SerializeField] private float uiScale = 1.5f;   // множитель размера, меняй в Inspector

    private GameObject       panel;
    private TextMeshProUGUI  titleText;
    private TextMeshProUGUI  levelText;
    private TextMeshProUGUI  costText;
    private Button           upgradeButton;
    private Unit             currentUnit;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Start()
    {
        BuildUI();
        if (panel != null) panel.SetActive(false);
    }

    private void OnEnable()
    {
        GameEvents.OnUnitTapped        += HandleUnitTapped;
        GameEvents.OnWavePhaseStarted  += HidePanel;
        GameEvents.OnPlacementCancelled += HidePanel;
    }

    private void OnDisable()
    {
        GameEvents.OnUnitTapped        -= HandleUnitTapped;
        GameEvents.OnWavePhaseStarted  -= HidePanel;
        GameEvents.OnPlacementCancelled -= HidePanel;
    }

    // ── Показ / скрытие ───────────────────────────────────────────────────────

    private void HandleUnitTapped(Unit unit)
    {
        if (unit == null) { HidePanel(); return; }
        currentUnit = unit;
        Refresh();
        if (panel != null) panel.SetActive(true);
    }

    private void HidePanel()
    {
        currentUnit = null;
        if (panel != null) panel.SetActive(false);
    }

    // ── Обновление отображения ────────────────────────────────────────────────

    private void Refresh()
    {
        if (currentUnit == null) { HidePanel(); return; }

        titleText.text = currentUnit.Data != null ? currentUnit.Data.displayName : "Unit";

        string stars = "";
        for (int i = 1; i <= 3; i++)
            stars += i <= currentUnit.Level
                ? "<color=#FFD700>\u25A0</color> "   // золотой квадрат
                : "<color=#555555>\u25A1</color> ";  // серый квадрат
        levelText.text = stars.TrimEnd();

        bool canUpgrade = currentUnit.CanUpgrade;
        costText.text   = canUpgrade ? $"Upgrade: {currentUnit.UpgradeCost}g" : "Max Level";

        upgradeButton.interactable = canUpgrade &&
            GameStateMachine.Instance != null &&
            GameStateMachine.Instance.CurrentGold >= currentUnit.UpgradeCost;
    }

    private void OnUpgradePressed()
    {
        if (currentUnit == null || !currentUnit.CanUpgrade) return;
        if (GameStateMachine.Instance == null) return;
        if (!GameStateMachine.Instance.TrySpendGold(currentUnit.UpgradeCost)) return;

        currentUnit.Upgrade();
        Refresh();
    }

    // ── Построение UI ─────────────────────────────────────────────────────────

    private void BuildUI()
    {
        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        float scale  = uiScale;
        float panelW = 400f * scale;
        float panelH = 260f * scale;
        float fs     = 18f  * scale;

        panel = new GameObject("UpgradePanel");
        panel.transform.SetParent(canvas.transform, false);
        var rt         = panel.AddComponent<RectTransform>();
        rt.anchorMin   = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta   = new Vector2(panelW, panelH);
        panel.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.93f);

        titleText = AddLabel("Title", panel, new Vector2(0, panelH * 0.32f),
            new Vector2(panelW - 20f, 35f * scale), fs + 2f, FontStyles.Bold, Color.white);

        levelText = AddLabel("Stars", panel, new Vector2(0, panelH * 0.1f),
            new Vector2(panelW - 20f, 30f * scale), fs + 2f, FontStyles.Normal,
            new Color(1f, 0.85f, 0.1f));

        costText = AddLabel("Cost", panel, new Vector2(0, -panelH * 0.08f),
            new Vector2(panelW - 20f, 25f * scale), fs - 2f, FontStyles.Normal,
            new Color(0.85f, 0.85f, 0.85f));

        float btnW = panelW * 0.43f;
        float btnH = Mathf.Clamp(44f * scale, 36f, 64f);
        float btnY = -panelH * 0.34f;

        var upgGo    = AddButton("UpgradeBtn", panel,
            new Vector2(-panelW * 0.26f, btnY), new Vector2(btnW, btnH),
            "Upgrade", new Color(0.15f, 0.55f, 0.2f), fs);
        upgradeButton = upgGo.GetComponent<Button>();
        upgradeButton.onClick.AddListener(OnUpgradePressed);

        var closeGo  = AddButton("CloseBtn", panel,
            new Vector2(panelW * 0.26f, btnY), new Vector2(btnW, btnH),
            "Close", new Color(0.5f, 0.2f, 0.2f), fs);
        closeGo.GetComponent<Button>().onClick.AddListener(HidePanel);
    }

    private static TextMeshProUGUI AddLabel(string name, GameObject parent,
        Vector2 pos, Vector2 size, float fontSize, FontStyles style, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var rt         = go.AddComponent<RectTransform>();
        rt.anchorMin   = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta   = size;
        var tmp        = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize   = fontSize;
        tmp.fontStyle  = style;
        tmp.color      = color;
        tmp.alignment  = TextAlignmentOptions.Center;
        return tmp;
    }

    private static GameObject AddButton(string name, GameObject parent,
        Vector2 pos, Vector2 size, string label, Color color, float fontSize)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var rt         = go.AddComponent<RectTransform>();
        rt.anchorMin   = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta   = size;
        go.AddComponent<Image>().color = color;
        go.AddComponent<Button>();

        var textGo       = new GameObject("Label");
        textGo.transform.SetParent(go.transform, false);
        var textRt       = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = textRt.offsetMax = Vector2.zero;
        var tmp          = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text         = label;
        tmp.fontSize     = fontSize;
        tmp.fontStyle    = FontStyles.Bold;
        tmp.color        = Color.white;
        tmp.alignment    = TextAlignmentOptions.Center;

        return go;
    }
}
