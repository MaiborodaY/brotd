using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Генерирует клетки вдоль пути врагов и управляет их состоянием.
/// Клетки ставятся с заданным шагом между вейпоинтами пути.
/// </summary>
public class GridSystem : MonoBehaviour
{
    public static GridSystem Instance { get; private set; }

    [Header("References")]
    [SerializeField] private PathData pathData;
    [SerializeField] private GameObject cellPrefab;

    [Header("Grid Settings")]
    [SerializeField] private float cellSpacing      = 1.5f;  // расстояние между клетками
    [SerializeField] private float cellSize         = 1f;    // размер клетки
    [SerializeField] private float cellY            = 0.61f; // высота клеток над землёй
    [SerializeField] private int   laneCount        = 3;     // количество дорожек
    [SerializeField] private float laneSpacing      = 2f;    // расстояние между дорожками
    [SerializeField] private float kingZoneDistance = 3f;    // зона у Короля — клетки не ставятся

    private readonly List<GridCell> cells = new();

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        GenerateCells();

        GameEvents.OnPrepPhaseStarted += ShowAllCells;
        GameEvents.OnWavePhaseStarted += HideAllCells;

        // Если GameStateMachine уже в Prep (запустился раньше нас) — сразу показываем клетки
        if (GameStateMachine.Instance != null &&
            GameStateMachine.Instance.CurrentPhase == GamePhase.Prep)
            ShowAllCells();
        else
            HideAllCells();
    }

    private void OnDestroy()
    {
        GameEvents.OnPrepPhaseStarted -= ShowAllCells;
        GameEvents.OnWavePhaseStarted -= HideAllCells;
    }

    // ── Генерация ─────────────────────────────────────────────────────────────

    private void GenerateCells()
    {
        if (pathData == null || pathData.waypoints == null || pathData.waypoints.Length < 2)
        {
            Debug.LogError("GridSystem: PathData не задан! Назначь его в инспекторе [GridSystem].");
            return;
        }

        cells.Clear();

        // Удаляем старые клетки (если regenerate)
        foreach (Transform child in transform)
            Destroy(child.gameObject);


        for (int lane = 0; lane < laneCount; lane++)
        {
            float xOffset = (lane - (laneCount - 1) / 2f) * laneSpacing;

            for (int i = 0; i < pathData.waypoints.Length - 1; i++)
            {
                Vector3 from = pathData.waypoints[i]     + new Vector3(xOffset, 0, 0);
                Vector3 to   = pathData.waypoints[i + 1] + new Vector3(xOffset, 0, 0);
                float segmentLength = Vector3.Distance(from, to);
                int count = Mathf.FloorToInt(segmentLength / cellSpacing);

                Vector3 lastWaypoint = pathData.waypoints[pathData.waypoints.Length - 1];

                for (int j = 0; j <= count; j++)
                {
                    float t = (count == 0) ? 0f : (float)j / count;
                    Vector3 pos = Vector3.Lerp(from, to, t);
                    pos.y = cellY;

                    // Не ставим клетки в зоне Короля
                    float distToEnd = Vector3.Distance(
                        new Vector3(pos.x, 0, pos.z),
                        new Vector3(lastWaypoint.x, 0, lastWaypoint.z));
                    if (distToEnd <= kingZoneDistance) continue;

                    SpawnCell(pos);
                }
            }
        }
    }

    private void SpawnCell(Vector3 position)
    {
        if (cellPrefab == null) return;

        GameObject go = Instantiate(cellPrefab, position, Quaternion.identity, transform);
        go.transform.localScale = Vector3.one * cellSize;

        if (go.TryGetComponent(out GridCell cell))
            cells.Add(cell);
    }

    // ── Видимость ─────────────────────────────────────────────────────────────

    private void ShowAllCells()
    {
        foreach (var cell in cells)
        {
            if (cell.IsOccupied) cell.ShowOccupied();
            else cell.ShowNormal();
        }
    }

    private void HideAllCells()
    {
        foreach (var cell in cells)
            cell.Hide();
    }

    // ── Поиск клеток ─────────────────────────────────────────────────────────

    /// <summary>Возвращает ближайшую свободную клетку к мировой позиции.</summary>
    public GridCell GetNearestFreeCell(Vector3 worldPos)
    {
        GridCell nearest = null;
        float minDist = float.MaxValue;

        foreach (var cell in cells)
        {
            if (cell.IsOccupied) continue;
            float d = Vector3.Distance(cell.WorldPosition, worldPos);
            if (d < minDist) { minDist = d; nearest = cell; }
        }

        return nearest;
    }

    /// <summary>Возвращает ближайшую клетку к мировой позиции (занятую или нет).</summary>
    public GridCell GetNearestCell(Vector3 worldPos)
    {
        GridCell nearest = null;
        float minDist = float.MaxValue;

        foreach (var cell in cells)
        {
            float d = Vector3.Distance(cell.WorldPosition, worldPos);
            if (d < minDist) { minDist = d; nearest = cell; }
        }

        return nearest;
    }

    public IReadOnlyList<GridCell> AllCells => cells;
}
