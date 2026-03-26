using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Враг в PvP режиме — выглядит как юнит противника, движется как обычный Enemy.
/// Создаётся динамически на основе UnitData противника.
/// </summary>
[RequireComponent(typeof(Collider))]
public class PvPUnitEnemy : MonoBehaviour
{
    /// <summary>Все активные PvP враги — используется Unit.FindNearestEnemy.</summary>
    public static readonly List<PvPUnitEnemy> ActiveList = new();

    private UnitData   unitData;
    private Vector3[]  waypoints;
    private int        waypointIndex;
    private float      hp;
    private HealthBar  healthBar;

    private Unit          engagedUnit;
    private Unit          chasingUnit;
    private float         attackTimer;
    private bool          isFighting;
    private EnemyAnimator animator;

    public bool IsAlive => hp > 0f;
    public Vector3 Position => transform.position;

    // ── Инициализация ─────────────────────────────────────────────────────────

    public void Init(UnitData data, Vector3[] path)
    {
        unitData      = data;
        waypoints     = path;
        waypointIndex = 0;
        hp            = data.maxHealth;
        attackTimer   = 0f;
        isFighting    = false;
        engagedUnit   = null;
        chasingUnit   = null;

        // Визуал — спрайт юнита противника
        if (data.icon != null)
            BillboardSprite.AddTo(gameObject, data.icon);

        ApplyEnemyTint();
        AddGroundMarker();

        healthBar = HealthBar.AddTo(gameObject, new Vector3(0f, data.hpBarHeight, 0f));
        healthBar.SetFill((int)hp, data.maxHealth);

        // Анимация боба
        animator = gameObject.AddComponent<EnemyAnimator>();
    }

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void OnEnable()
    {
        GameEvents.OnUnitDied += OnUnitDied;
    }

    private void OnDisable()
    {
        GameEvents.OnUnitDied -= OnUnitDied;
    }

    // ── Update ────────────────────────────────────────────────────────────────

    private void Update()
    {
        if (!IsAlive) return;

        if (isFighting)
            UpdateFighting();
        else
            UpdateMoving();

        ApplySeparation();
    }

    private void UpdateMoving()
    {
        if (waypoints == null || waypointIndex >= waypoints.Length) return;

        // Ищем ближайший юнит-защитника
        if (chasingUnit == null || !chasingUnit.IsAlive)
            chasingUnit = FindNearestUnit(unitData.detectionRange);

        if (chasingUnit != null && chasingUnit.IsAlive)
        {
            Vector3 unitPos = chasingUnit.transform.position;
            unitPos.y = transform.position.y;
            float dist = Vector3.Distance(transform.position, unitPos);

            if (dist <= unitData.attackRange)
            {
                // Входим в бой
                engagedUnit = chasingUnit;
                chasingUnit = null;
                isFighting  = true;
                return;
            }

            // Идём к юниту
            transform.position = Vector3.MoveTowards(
                transform.position, unitPos, unitData.moveSpeed * Time.deltaTime);
            transform.forward = (unitPos - transform.position).normalized;
            return;
        }

        // Обычное движение по пути
        Vector3 target = waypoints[waypointIndex];
        Vector3 dir    = target - transform.position;
        dir.y = 0f;

        if (dir.magnitude < 0.1f)
        {
            waypointIndex++;
            if (waypointIndex >= waypoints.Length)
                ReachedEnd();
            return;
        }

        transform.position += dir.normalized * (unitData.moveSpeed * Time.deltaTime);
        transform.forward   = dir.normalized;
    }

    private void UpdateFighting()
    {
        if (engagedUnit == null || !engagedUnit.IsAlive)
        {
            engagedUnit = null;
            isFighting  = false;
            return;
        }

        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0f)
        {
            attackTimer = unitData.attackCooldown;
            engagedUnit.TakeDamage(unitData.attackDamage);
            animator?.TriggerPunch();
        }
    }

    // ── Получение урона ───────────────────────────────────────────────────────

    public void TakeDamage(float damage)
    {
        if (!IsAlive) return;
        hp -= damage;
        healthBar?.SetFill(Mathf.Max(0, (int)hp), unitData.maxHealth);
        if (hp <= 0f) Die();
    }

    // ── События ───────────────────────────────────────────────────────────────

    public static event System.Action<PvPUnitEnemy> OnDied;
    public static event System.Action<PvPUnitEnemy> OnReachedEnd;

    private void OnUnitDied(Unit unit)
    {
        if (engagedUnit == unit) { engagedUnit = null; isFighting = false; }
        if (chasingUnit == unit) chasingUnit = null;
    }

    private void Die()
    {
        ActiveList.Remove(this);
        gameObject.SetActive(false);
        OnDied?.Invoke(this);
    }

    private void ReachedEnd()
    {
        ActiveList.Remove(this);
        gameObject.SetActive(false);
        OnReachedEnd?.Invoke(this);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    // ── Separation ────────────────────────────────────────────────────────────

    private const float SeparationRadius = 0.9f;
    private const float SeparationForce  = 2f;

    private void ApplySeparation()
    {
        var cols = Physics.OverlapSphere(transform.position, SeparationRadius);
        Vector3 push = Vector3.zero;
        foreach (var col in cols)
        {
            if (col.gameObject == gameObject) continue;
            if (!col.TryGetComponent<PvPUnitEnemy>(out _)) continue;
            Vector3 diff = transform.position - col.transform.position;
            diff.y = 0f;
            float dist = diff.magnitude;
            if (dist < 0.01f) dist = 0.01f;
            push += diff.normalized * (SeparationRadius - dist) / SeparationRadius;
        }
        if (push != Vector3.zero)
            transform.position += push * SeparationForce * Time.deltaTime;
    }

    // ── Визуальное отличие от защитников ─────────────────────────────────────

    private static readonly Color EnemyTint = new Color(1f, 0.45f, 0.45f, 1f);

    private void ApplyEnemyTint()
    {
        // Красноватый тинт на спрайте — применяем с задержкой 1 кадр,
        // т.к. BillboardSprite добавляет SpriteRenderer в том же кадре
        StartCoroutine(TintNextFrame());
    }

    private System.Collections.IEnumerator TintNextFrame()
    {
        yield return null; // ждём один кадр пока BillboardSprite создаст SpriteRenderer
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.color = EnemyTint;
    }

    private void AddGroundMarker()
    {
        // Красный круг под ногами — плоский диск на земле
        var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.name = "EnemyMarker";
        marker.transform.SetParent(transform, false);
        marker.transform.localPosition = new Vector3(0f, -0.45f, 0f);
        marker.transform.localScale    = new Vector3(0.7f, 0.02f, 0.7f);

        Object.Destroy(marker.GetComponent<Collider>());

        var shader = Shader.Find("Universal Render Pipeline/Unlit")
                  ?? Shader.Find("Unlit/Color");
        var mat    = new Material(shader);
        mat.color  = new Color(0.9f, 0.1f, 0.1f, 0.85f);
        marker.GetComponent<Renderer>().material = mat;
    }

    private Unit FindNearestUnit(float range)
    {
        var cols = Physics.OverlapSphere(transform.position, range);
        Unit nearest = null;
        float minSqDist = range * range;
        foreach (var col in cols)
        {
            if (!col.TryGetComponent<Unit>(out var unit)) continue;
            if (!unit.IsAlive) continue;
            float sqDist = (unit.transform.position - transform.position).sqrMagnitude;
            if (sqDist < minSqDist) { minSqDist = sqDist; nearest = unit; }
        }
        return nearest;
    }
}
