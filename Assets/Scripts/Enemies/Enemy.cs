using UnityEngine;

public enum EnemyState { Moving, Fighting, Dead }

/// <summary>
/// Враг движется по пути. Если мечник его атакует — останавливается и дерётся.
/// Если дошёл до конца — отнимает жизнь.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Enemy : MonoBehaviour
{
    public EnemyData Data          { get; private set; }
    public EnemyState State        { get; private set; } = EnemyState.Moving;
    public int        CurrentHp    { get; private set; }
    public bool       IsAlive      => State != EnemyState.Dead;

    private Vector3[] waypoints;
    private int       waypointIndex;
    private Unit      engagedUnit;
    private Unit      chasingUnit;
    private float         attackTimer;

    private const float SeparationRadius = 1f;
    private const float SeparationForce  = 2f;
    private HealthBar     healthBar;
    private FloatingLabel floatingLabel;

    // ── Инициализация ─────────────────────────────────────────────────────────

    public void Init(EnemyData data, Vector3[] path)
    {
        Data           = data;
        waypoints      = path;
        waypointIndex  = 0;
        CurrentHp      = data.maxHealth;
        attackTimer    = 0f;
        engagedUnit    = null;
        chasingUnit    = null;
        State          = EnemyState.Moving;

        if (healthBar == null)
            healthBar = HealthBar.AddTo(gameObject, new Vector3(0f, data.hpBarHeight, 0f));
        healthBar.SetFill(CurrentHp, data.maxHealth);

        if (GetComponent<BillboardSprite>() == null)
        {
            if (data.animatorController != null)
            {
                var billboard = BillboardSprite.AddTo(gameObject, data.animatorController);
                var visuals   = GetComponent<EnemyVisuals>() ?? gameObject.AddComponent<EnemyVisuals>();
                visuals.Init(this, billboard.SpriteAnimator);
            }
            else if (data.icon != null)
            {
                BillboardSprite.AddTo(gameObject, data.icon);
            }
            else if (floatingLabel == null)
            {
                floatingLabel = FloatingLabel.AddTo(gameObject, ProceduralIcons.BasicEnemy(),
                    new Vector3(0f, 1.3f, 0f));
            }
        }
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

    private void Update()
    {
        if (!IsAlive) return;

        switch (State)
        {
            case EnemyState.Moving:   UpdateMoving();   break;
            case EnemyState.Fighting: UpdateFighting(); break;
        }

        ApplySeparation();
    }

    // ── Движение ──────────────────────────────────────────────────────────────

    private void UpdateMoving()
    {
        if (waypoints == null || waypointIndex >= waypoints.Length)
        {
            ReachBase();
            return;
        }

        AdvanceWaypointIfPassed();
        if (waypointIndex >= waypoints.Length) { ReachBase(); return; }

        // Detect nearby units and chase them
        if (chasingUnit == null || !chasingUnit.IsAlive)
            chasingUnit = FindNearestUnit(Data.detectionRange);

        if (chasingUnit != null && chasingUnit.IsAlive)
        {
            Vector3 unitPos = chasingUnit.transform.position;
            unitPos.y = transform.position.y;

            if (Vector3.Distance(transform.position, unitPos) <= Data.attackRange)
            {
                EngageWith(chasingUnit);
                chasingUnit = null;
                return;
            }

            transform.position = Vector3.MoveTowards(
                transform.position, unitPos, Data.moveSpeed * Time.deltaTime);
            return;
        }

        // Normal path movement
        Vector3 target = waypoints[waypointIndex];
        target.y = transform.position.y;

        transform.position = Vector3.MoveTowards(
            transform.position, target, Data.moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target) < 0.05f)
        {
            waypointIndex++;
            if (waypointIndex >= waypoints.Length)
                ReachBase();
        }
    }

    private void AdvanceWaypointIfPassed()
    {
        if (waypoints == null || waypoints.Length < 2) return;
        Vector3 pathDir = (waypoints[waypoints.Length - 1] - waypoints[0]).normalized;
        while (waypointIndex < waypoints.Length)
        {
            Vector3 toWp = waypoints[waypointIndex] - transform.position;
            if (Vector3.Dot(toWp, pathDir) <= 0f)
                waypointIndex++;
            else
                break;
        }
    }

    private void ApplySeparation()
    {
        var cols = Physics.OverlapSphere(transform.position, SeparationRadius);
        Vector3 push = Vector3.zero;
        foreach (var col in cols)
        {
            if (col.gameObject == gameObject) continue;
            if (!col.TryGetComponent<Enemy>(out _)) continue;
            Vector3 diff = transform.position - col.transform.position;
            diff.y = 0f;
            float dist = diff.magnitude;
            if (dist < 0.01f) dist = 0.01f;
            push += diff.normalized * (SeparationRadius - dist) / SeparationRadius;
        }
        if (push != Vector3.zero)
            transform.position += push * SeparationForce * Time.deltaTime;
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
            if (sqDist < minSqDist)
            {
                minSqDist = sqDist;
                nearest = unit;
            }
        }
        return nearest;
    }

    private void ReachBase()
    {
        // Финишная линия — считаем как пропущенного врага
        GameEvents.RaiseEnemyLeaked(this);
        GameEvents.RaiseEnemyReachedBase(this);
        // Враг остаётся живым — Король сам его найдёт и атакует
    }

    // ── Бой ───────────────────────────────────────────────────────────────────

    /// <summary>Вызывается юнитом когда он начинает атаковать этого врага.</summary>
    public void EngageWith(Unit unit)
    {
        if (State == EnemyState.Dead) return;
        engagedUnit = unit;
        SetState(EnemyState.Fighting);
    }

    private void UpdateFighting()
    {
        // Если юнит умер или ушёл — продолжаем движение
        if (engagedUnit == null || !engagedUnit.IsAlive)
        {
            engagedUnit = null;
            SetState(EnemyState.Moving);
            return;
        }

        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0f)
        {
            attackTimer = Data.attackCooldown;
            engagedUnit.TakeDamage(Data.attackDamage);
            GameEvents.RaiseEnemyAttack(this);
        }
    }

    // ── Получение урона / смерть ──────────────────────────────────────────────

    public void TakeDamage(float damage)
    {
        if (!IsAlive) return;

        CurrentHp -= Mathf.RoundToInt(damage);
        healthBar?.SetFill(CurrentHp, Data.maxHealth);
        GameEvents.RaiseEnemyHit(transform.position);
        if (CurrentHp <= 0)
            Die();
    }

    private void Die()
    {
        SetState(EnemyState.Dead);
        GameStateMachine.Instance?.EarnGold(Data.goldReward);
        GameEvents.RaiseEnemyDied(this);
        gameObject.SetActive(false);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SetState(EnemyState newState)
    {
        if (State == newState) return;
        State = newState;
    }

    private void OnUnitDied(Unit unit)
    {
        if (engagedUnit == unit)
        {
            engagedUnit = null;
            if (State == EnemyState.Fighting)
                SetState(EnemyState.Moving);
        }
        if (chasingUnit == unit)
            chasingUnit = null;
    }
}
