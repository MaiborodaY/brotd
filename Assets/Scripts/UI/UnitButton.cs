using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnitButton : MonoBehaviour
{
    [SerializeField] private UnitData            unitData;
    [SerializeField] private Image               iconImage;
    [SerializeField] private TextMeshProUGUI     nameText;
    [SerializeField] private TextMeshProUGUI     costText;
    [SerializeField] private Button              button;

    private Image    bgImage;
    private Color    normalColor   = new Color(0.15f, 0.15f, 0.15f);
    private Color    selectedColor = new Color(0.9f, 0.7f, 0f);      // жёлтый — выбран

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        bgImage = GetComponent<Image>();
        button.onClick.AddListener(OnClick);
    }

    private void Start()
    {
        if (unitData == null) return;

        if (iconImage != null && unitData.icon != null)
            iconImage.sprite = unitData.icon;

        if (nameText != null)
            nameText.text = unitData.displayName;

        if (costText != null)
            costText.text = $"{unitData.goldCost}g";
    }

    private void OnEnable()
    {
        GameEvents.OnGoldChanged        += RefreshAffordable;
        GameEvents.OnPlacementCancelled += Deselect;
        GameEvents.OnWavePhaseStarted   += Deselect;
        GameEvents.OnUnitSelected       += OnAnyUnitSelected;
    }

    private void OnDisable()
    {
        GameEvents.OnGoldChanged        -= RefreshAffordable;
        GameEvents.OnPlacementCancelled -= Deselect;
        GameEvents.OnWavePhaseStarted   -= Deselect;
        GameEvents.OnUnitSelected       -= OnAnyUnitSelected;
    }

    private void OnClick()
    {
        if (unitData == null) return;

        // Повторный тап — отменить выбор
        if (PlacementSystem.Instance != null &&
            PlacementSystem.Instance.SelectedData == unitData)
        {
            PlacementSystem.Instance.CancelPlacement();
            return;
        }

        GameEvents.RaiseUnitSelected(unitData); // остальные кнопки отожмутся
        PlacementSystem.Instance?.SelectUnit(unitData);
        SetSelected(true);
    }

    private void OnAnyUnitSelected(UnitData selected)
    {
        if (selected != unitData)
            SetSelected(false);
    }

    public void SetInteractable(bool value)
    {
        if (button != null)
            button.interactable = value;
    }

    private void SetSelected(bool selected)
    {
        if (bgImage != null)
            bgImage.color = selected ? selectedColor : normalColor;
    }

    private void Deselect()
    {
        SetSelected(false);
    }

    private void RefreshAffordable(int currentGold)
    {
        if (button == null || unitData == null) return;
        button.interactable = currentGold >= unitData.goldCost;
    }
}
