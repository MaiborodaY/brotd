using UnityEngine;

public enum UnitState { Idle, Fighting, Dead }

/// <summary>
/// Базовый класс юнита. MeleeUnit и RangedUnit наследуют от него.
/// Стоит на клетке, атакует врагов, умирает, respawn-ится после волны.
/// </summary>
[RequireComponent(typeof(Collider))]
public abstract class Unit : MonoBehaviour
{
    public static readonly System.Collections.Generic.List<Unit> ActiveUnits = new();

    [HideInInspector] public UnitData Data;
    [HideInInspector] public GridCell Cell;

    public UnitState State        { get; private set; } = UnitState.Idle;
    public int       CurrentHp   { get; private set; }
    public bool      IsAlive     => State != UnitState.Dead;
    public int       Level       { get; private set; } = 1;
    public int       MaxHp       { get; private set; }
    public float     AttackDamage { get; private set; }

    protected Enemy        targetEnemy;
    protected PvPUnitEnemy targetPvPEnemy;
    protected float        attackTimer;
    protected HealthBar    healthBar;
    private   FloatingLabel    floatingLabel;
    private   UnitLevelBadge   levelBadge;

    // ── Инициализация ─────────────────────────────────────────────────────────

    public void Init(UnitData data, GridCell cell)
    {
        Data         = data;
        Cell         = cell;
        Level        = 1;
        MaxHp        = data.maxHealth;
        AttackDamage = data.attackDamage;
        CurrentHp    = MaxHp;
        attackTimer  = 0f;
        State        = UnitState.Idle;
        targetEnemy  = null;

        if (healthBar == null)
            healthBar = HealthBar.AddTo(gameObject, new Vector3(0f, data.hpBarHeight, 0f));
        else
            healthBar.SetHeight(data.hpBarHeight);
        healthBar.SetFill(CurrentHp, MaxHp);

        if (levelBadge == null)
            levelBadge = UnitLevelBadge.AddTo(gameObject, data.hpBarHeight + 0.25f);
        levelBadge.SetLevel(Level);

        if (floatingLabel == null && GetComponent<BillboardSprite>() == null
                                  && GetComponent<TinySwordsUnitAnimator>() == null)
        {
            if (data.animatorController != null)
            {
                var anim = gameObject.AddComponent<TinySwordsUnitAnimator>();
                anim.Init(data.animatorController, data.attackAnimName);
            }
            else if (data.icon != null)
            {
                BillboardSprite.AddTo(gameObject, data.icon);
            }
            else
            {
                var tex = data.unitType == UnitType.Melee
                    ? ProceduralIcons.Swordsman()
                    : ProceduralIcons.Archer();
                floatingLabel = FloatingLabel.AddTo(gameObject, tex, new Vector3(0f, 1.5f, 0f));
            }
        }
    }

    // ── Апгрейд ───────────────────────────────────────────────────────────────

    public bool CanUpgrade => Level < 3 && Data.upgradeCosts.Length >= Level;

    public int UpgradeCost => CanUpgrade ? Data.upgradeCosts[Level - 1] : 0;

    public void Upgrade()
    {
        if (!CanUpgrade) return;

        Level++;
        int   oldMax  = MaxHp;
        MaxHp        = Mathf.RoundToInt(Data.maxHealth * Mathf.Pow(1.3f, Level - 1));
        AttackDamage  = Data.attackDamage * Mathf.Pow(1.25f, Level - 1);
        CurrentHp     = Mathf.RoundToInt((float)CurrentHp / oldMax * MaxHp);
        healthBar?.SetFill(CurrentHp, MaxHp);

        var controller = Level == 2 ? Data.level2Controller : Data.level3Controller;
        if (controller != null)
            GetComponent<TinySwordsUnitAnimator>()?.SwapController(controller);

        levelBadge?.SetLevel(Level);
    }

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void OnEnable()
    {
        ActiveUnits.Add(this);
        GameEvents.OnEnemyDied        += OnEnemyDied;
        GameEvents.OnWavePhaseEnded   += HandleWaveEnded;
    }

    private void OnDisable()
    {
        ActiveUnits.Remove(this);
        GameEvents.OnEnemyDied        -= OnEnemyDied;
        GameEvents.OnWavePhaseEnded   -= HandleWaveEnded;
    }

    protected virtual void Update()
    {
        if (State == UnitState.Dead) return;

        healthBar?.SetHeight(Data.hpBarHeight);

        FindTargetIfNeeded();

        // PvP режим — таргетим PvPUnitEnemy
        if (targetEnemy == null && targetPvPEnemy != null)
        {
            if (!targetPvPEnemy.IsAlive) { targetPvPEnemy = null; return; }

            SetState(UnitState.Fighting);
            float pvpDist = Vector3.Distance(transform.position, targetPvPEnemy.transform.position);
            if (pvpDist > Data.attackRange)
            {
                Vector3 pvpDir = (targetPvPEnemy.transform.position - transform.position);
                pvpDir.y = 0f;
                transform.position += pvpDir.normalized * (Data.moveSpeed * Time.deltaTime);
                return;
            }
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0f)
            {
                attackTimer = Data.attackCooldown;
                targetPvPEnemy.TakeDamage(AttackDamage);
                GameEvents.RaiseUnitAttack(this);
            }
            return;
        }

        if (targetEnemy == null)
        {
            if (State == UnitState.Fighting)
                SetState(UnitState.Idle);
            return;
        }

        SetState(UnitState.Fighting);

        float distToEnemy = Vector3.Distance(transform.position, targetEnemy.transform.position);
        if (distToEnemy > Data.attackRange)
        {
            Vector3 dir = (targetEnemy.transform.position - transform.position);
            dir.y = 0f;
            transform.position += dir.normalized * (Data.moveSpeed * Time.deltaTime);
            ApplySeparation();
            return;
        }

        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0f)
        {
            attackTimer = Data.attackCooldown;
            PerformAttack(targetEnemy);
        }
    }

// ── Атака (переопределяется в подклассах) ─────────────────────────────────

    protected abstract void PerformAttack(Enemy target);

    // ── Поиск цели ────────────────────────────────────────────────────────────

    private void FindTargetIfNeeded()
    {
        if (targetEnemy != null && targetEnemy.IsAlive &&
            Vector3.Distance(transform.position, targetEnemy.transform.position) <= Data.detectionRange)
            return;

        targetEnemy = FindNearestEnemy();

        // PvP режим — ищем PvPUnitEnemy если обычных врагов нет
        if (targetEnemy == null)
            targetPvPEnemy = FindNearestPvPEnemy();
    }

    private PvPUnitEnemy FindNearestPvPEnemy()
    {
        PvPUnitEnemy nearest = null;
        float minSqDist = Data.detectionRange * Data.detectionRange;

        foreach (var pvpEnemy in PvPUnitEnemy.ActiveList)
        {
            if (pvpEnemy == null || !pvpEnemy.IsAlive) continue;
            float sqDist = (pvpEnemy.transform.position - transform.position).sqrMagnitude;
            if (sqDist <= minSqDist)
            {
                minSqDist = sqDist;
                nearest   = pvpEnemy;
            }
        }
        return nearest;
    }

    private Enemy FindNearestEnemy()
    {
        Enemy nearest = null;
        float minSqDist = Data.detectionRange * Data.detectionRange;

        foreach (var enemy in WaveManager.ActiveEnemies)
        {
            if (!enemy.IsAlive) continue;
            float sqDist = (enemy.transform.position - transform.position).sqrMagnitude;
            if (sqDist <= minSqDist)
            {
                minSqDist = sqDist;
                nearest = enemy;
            }
        }

        return nearest;
    }

    // ── Получение урона / смерть ──────────────────────────────────────────────

    public void Heal(float amount)
    {
        if (!IsAlive) return;
        CurrentHp = Mathf.Min(CurrentHp + Mathf.RoundToInt(amount), MaxHp);
        healthBar?.SetFill(CurrentHp, MaxHp);
    }

    public void TakeDamage(float damage)
    {
        if (State == UnitState.Dead) return;

        CurrentHp -= Mathf.RoundToInt(damage);
        healthBar?.SetFill(CurrentHp, Data.maxHealth);
        if (CurrentHp <= 0)
            Die();
    }

    private void Die()
    {
        SetState(UnitState.Dead);
        Cell?.ClearUnit();
        GameEvents.RaiseUnitDied(this);
        OnDied();
        if (!SurviveOnDeath())
            gameObject.SetActive(false);
    }

    /// <summary>Вызывается при смерти — переопределяется в KingUnit.</summary>
    protected virtual void OnDied() { }

    /// <summary>Если true — объект не деактивируется при смерти (для Короля).</summary>
    protected virtual bool SurviveOnDeath() => false;

    // ── Respawn после волны ───────────────────────────────────────────────────

    private void HandleWaveEnded()
    {
        if (Cell == null)
        {
            OnWaveEndedNoCell();
            return;
        }

        if (RestoreHpOnWaveEnd())
            CurrentHp = MaxHp;

        attackTimer = 0f;
        targetEnemy = null;

        transform.position = Cell.WorldPosition;
        gameObject.SetActive(true);
        Cell.PlaceUnit(this);
        healthBar?.SetFill(CurrentHp, MaxHp);

        SetState(UnitState.Idle);
        GameEvents.RaiseUnitRespawned(this);
    }

    /// <summary>Если false — HP не восстанавливается между волнами (для Короля).</summary>
    protected virtual bool RestoreHpOnWaveEnd() => true;

    /// <summary>Вызывается при окончании волны если Cell == null (для Короля).</summary>
    protected virtual void OnWaveEndedNoCell() { }

    private void ApplySeparation()
    {
        var cols = Physics.OverlapSphere(transform.position, 0.8f);
        Vector3 push = Vector3.zero;
        foreach (var col in cols)
        {
            if (col.gameObject == gameObject) continue;
            if (!col.TryGetComponent<Unit>(out _)) continue;
            Vector3 diff = transform.position - col.transform.position;
            diff.y = 0f;
            float dist = diff.magnitude;
            if (dist < 0.01f) dist = 0.01f;
            push += diff.normalized * (0.8f - dist) / 0.8f;
        }
        if (push != Vector3.zero)
            transform.position += push * 2f * Time.deltaTime;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    protected void SetState(UnitState newState)
    {
        if (State == newState) return;
        State = newState;
        OnStateChanged(newState);
    }

    protected virtual void OnStateChanged(UnitState newState) { }

    private void OnEnemyDied(Enemy e)
    {
        if (targetEnemy == e)
            targetEnemy = null;
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        if (Data == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, Data.attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, Data.detectionRange);
    }
}
