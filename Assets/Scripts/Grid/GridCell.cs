using UnityEngine;

/// <summary>
/// Одна клетка на пути врагов. Хранит своё положение, визуал и текущий юнит.
/// </summary>
public class GridCell : MonoBehaviour
{
    public Vector3 WorldPosition => transform.position;
    public bool IsOccupied       => OccupyingUnit != null;
    public Unit OccupyingUnit    { get; private set; }

    [Header("Visuals")]
    [SerializeField] private Renderer cellRenderer;
    [SerializeField] private Color normalColor    = new Color(0.3f, 0.6f, 1f, 0.4f);
    [SerializeField] private Color hoverColor     = new Color(0.3f, 1f, 0.4f, 0.6f);
    [SerializeField] private Color occupiedColor  = new Color(1f, 0.3f, 0.3f, 0.4f);
    [SerializeField] private Color hiddenColor    = new Color(0f, 0f, 0f, 0f);

    private void Awake()
    {
        if (cellRenderer == null)
            cellRenderer = GetComponent<Renderer>();
    }

    // ── Состояние визуала ─────────────────────────────────────────────────────

    public void ShowNormal()   => SetColor(normalColor);
    public void ShowHover()    => SetColor(hoverColor);
    public void ShowOccupied() => SetColor(occupiedColor);
    public void Hide()         => SetColor(hiddenColor);

    private void SetColor(Color c)
    {
        if (cellRenderer != null)
            cellRenderer.material.color = c;
    }

    // ── Юниты ─────────────────────────────────────────────────────────────────

    public void PlaceUnit(Unit unit)
    {
        OccupyingUnit = unit;
        ShowOccupied();
    }

    public void ClearUnit()
    {
        OccupyingUnit = null;
        ShowNormal();
    }
}
