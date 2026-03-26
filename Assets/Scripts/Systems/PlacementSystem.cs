using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// Обрабатывает тап/клик в фазу подготовки.
/// Игрок выбирает тип юнита в UI → тапает на клетку → юнит появляется.
/// </summary>
public class PlacementSystem : MonoBehaviour
{
    public static PlacementSystem Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask cellLayer;       // слой на котором лежат GridCell

    private UnitData selectedUnitData;
    private GridCell hoveredCell;
    private bool     justSelected;
    private Vector2  tapPosition;    // позиция тапа сохранённая в момент TouchPhase.Ended
    private float    placeCooldown;  // защита от двойного срабатывания TouchPhase.Ended на Android
    private bool     isPlacing => selectedUnitData != null;

    public UnitData SelectedData => selectedUnitData;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    private void OnEnable()
    {
        GameEvents.OnWavePhaseStarted += CancelPlacement;
    }

    private void OnDisable()
    {
        GameEvents.OnWavePhaseStarted -= CancelPlacement;
    }

    private void Update()
    {
        if (GameStateMachine.Instance == null ||
            GameStateMachine.Instance.CurrentPhase != GamePhase.Prep)
            return;

        UpdateHover();
        HandleInput();
    }

    // ── Выбор юнита из UI ─────────────────────────────────────────────────────

    public void SelectUnit(UnitData data)
    {
        selectedUnitData = data;
        justSelected     = true;
    }

    public void CancelPlacement()
    {
        selectedUnitData = null;
        if (hoveredCell != null && !hoveredCell.IsOccupied)
            hoveredCell.ShowNormal();
        hoveredCell = null;
        GameEvents.RaisePlacementCancelled();
    }

    // ── Hover ─────────────────────────────────────────────────────────────────

    private void UpdateHover()
    {
        if (!isPlacing) return;

        Ray ray = GetPointerRay();
        GridCell cell = null;

        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            cell = hit.collider.GetComponentInParent<GridCell>();
            if (cell == null && GridSystem.Instance != null)
            {
                var nearest = GridSystem.Instance.GetNearestFreeCell(hit.point);
                if (nearest != null && Vector3.Distance(nearest.WorldPosition, hit.point) <= 1.5f)
                    cell = nearest;
            }
        }

        if (cell == hoveredCell) return;

        if (hoveredCell != null && !hoveredCell.IsOccupied)
            hoveredCell.ShowNormal();

        hoveredCell = cell;

        if (hoveredCell != null && !hoveredCell.IsOccupied)
            hoveredCell.ShowHover();
    }

    // ── Input ─────────────────────────────────────────────────────────────────

    private void HandleInput()
    {
        if (placeCooldown > 0f) { placeCooldown -= Time.deltaTime; return; }

        bool tapped = false;

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
        {
            tapped     = true;
            tapPosition = Mouse.current.position.ReadValue();
        }
        else if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;
            if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Ended)
            {
                tapped      = true;
                tapPosition = touch.position.ReadValue();
            }
        }
#else
        if (Input.GetMouseButtonUp(0))
        {
            tapped      = true;
            tapPosition = Input.mousePosition;
        }
#endif

        if (!tapped) return;

        // Пропускаем если тап попал на UI элемент
        if (IsTapOverUI(tapPosition)) return;

        // Пропускаем если это был свайп камеры
        if (CameraController.IsDragging) return;

        // Пропускаем фрейм в котором была нажата кнопка выбора юнита
        if (justSelected) { justSelected = false; return; }

        if (!isPlacing) return;

        Ray ray = mainCamera.ScreenPointToRay(tapPosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 100f))
            return;

        GridCell cell = hit.collider.GetComponentInParent<GridCell>();

        if (cell == null && GridSystem.Instance != null)
            cell = GridSystem.Instance.GetNearestFreeCell(hit.point);

        if (cell == null || cell.IsOccupied)
            return;

        if (Vector3.Distance(cell.WorldPosition, hit.point) > 1.5f)
            return;

        TryPlaceUnit(cell);
    }

    // ── Размещение ────────────────────────────────────────────────────────────

    private void TryPlaceUnit(GridCell cell)
    {
        if (!GameStateMachine.Instance.TrySpendGold(selectedUnitData.goldCost))
        {
            Debug.LogWarning($"[Placement] Недостаточно золота! Нужно {selectedUnitData.goldCost}, есть {GameStateMachine.Instance.CurrentGold}");
            return;
        }

        GameObject go = Instantiate(selectedUnitData.prefab, cell.WorldPosition, Quaternion.identity);

        Unit unit = go.GetComponent<Unit>();
        if (unit == null)
        {
            Debug.LogError($"PlacementSystem: prefab {selectedUnitData.prefab.name} не содержит компонент Unit.");
            Destroy(go);
            GameStateMachine.Instance.EarnGold(selectedUnitData.goldCost); // возврат
            return;
        }

        unit.Init(selectedUnitData, cell);
        cell.PlaceUnit(unit);

        // Сбрасываем выбор сразу — защита от двойного тача и стандартная практика TD
        selectedUnitData = null;
        placeCooldown    = 0.5f;
        GameEvents.RaiseUnitPlaced(unit);
        GameEvents.RaisePlacementCancelled(); // кнопки UI снимают подсветку
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private static readonly List<RaycastResult> uiRaycastResults = new();

    private static bool IsTapOverUI(Vector2 screenPos)
    {
        if (EventSystem.current == null) return false;
        var ped = new PointerEventData(EventSystem.current) { position = screenPos };
        uiRaycastResults.Clear();
        EventSystem.current.RaycastAll(ped, uiRaycastResults);
        return uiRaycastResults.Count > 0;
    }

    private Ray GetPointerRay()
    {
#if ENABLE_INPUT_SYSTEM
        Vector2 screenPos = Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : Touchscreen.current != null
                ? Touchscreen.current.primaryTouch.position.ReadValue()
                : Vector2.zero;
#else
        Vector2 screenPos = Input.mousePosition;
#endif
        return mainCamera.ScreenPointToRay(screenPos);
    }
}
