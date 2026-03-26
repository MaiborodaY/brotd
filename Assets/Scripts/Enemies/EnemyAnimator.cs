using UnityEngine;

/// <summary>
/// Процедурная анимация для врагов с billboard-спрайтом (боссы и обычные).
/// Добавь на тот же GameObject что и Enemy.
/// </summary>
public class EnemyAnimator : MonoBehaviour
{
    [Header("Idle Bob")]
    [SerializeField] private float bobHeight    = 0.08f;
    [SerializeField] private float bobSpeed     = 2.0f;

    [Header("Attack Punch")]
    [SerializeField] private float punchScale   = 1.2f;
    [SerializeField] private float punchDuration = 0.14f;

    [Header("Death")]
    [SerializeField] private float deathFallSpeed = 2f;
    [SerializeField] private float deathFadeSpeed = 3f;

    // ── internal ──────────────────────────────────────────────────────────────

    private Vector3        baseLocalPos;
    private Vector3        baseLocalScale;
    private float          bobOffset;
    private float          punchTimer;
    private bool           isDead;

    private Enemy          enemy;
    private Transform      visual;
    private SpriteRenderer spriteRenderer;

    // ── lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        enemy     = GetComponent<Enemy>();
        bobOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    private void OnEnable()
    {
        GameEvents.OnEnemyAttack += HandleEnemyAttack;
        GameEvents.OnEnemyDied   += HandleEnemyDied;
    }

    private void OnDisable()
    {
        GameEvents.OnEnemyAttack -= HandleEnemyAttack;
        GameEvents.OnEnemyDied   -= HandleEnemyDied;
    }

    private void Update()
    {
        if (visual == null) InitVisual();

        if (isDead)
        {
            UpdateDeath();
            return;
        }

        UpdateBob();
        UpdatePunch();
    }

    // ── инициализация visual (лениво — после того как BillboardSprite создан) ─

    private void InitVisual()
    {
        // Ищем SpriteRenderer (billboard юниты) или MeshRenderer (капсулы/меши)
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            visual = spriteRenderer.transform;
        }
        else
        {
            var mesh = GetComponentInChildren<MeshRenderer>();
            visual = mesh != null ? mesh.transform : transform;
        }

        baseLocalPos   = visual.localPosition;
        baseLocalScale = visual.localScale;
    }

    // ── bob ───────────────────────────────────────────────────────────────────

    private void UpdateBob()
    {
        float y = Mathf.Sin(Time.time * bobSpeed + bobOffset) * bobHeight;
        visual.localPosition = baseLocalPos + new Vector3(0f, y, 0f);
    }

    // ── punch ─────────────────────────────────────────────────────────────────

    private void UpdatePunch()
    {
        if (punchTimer <= 0f) return;

        punchTimer -= Time.deltaTime;
        float t = punchTimer / punchDuration;
        float s = 1f + (punchScale - 1f) * Mathf.Sin(t * Mathf.PI);
        visual.localScale = baseLocalScale * s;

        if (punchTimer <= 0f)
            visual.localScale = baseLocalScale;
    }

    // ── death ─────────────────────────────────────────────────────────────────

    private void UpdateDeath()
    {
        visual.localPosition += Vector3.down * deathFallSpeed * Time.deltaTime;

        if (spriteRenderer != null)
        {
            var c = spriteRenderer.color;
            c.a -= deathFadeSpeed * Time.deltaTime;
            spriteRenderer.color = c;
        }
    }

    // ── events ────────────────────────────────────────────────────────────────

    /// <summary>Вызывается напрямую если на объекте нет компонента Enemy (например PvPUnitEnemy).</summary>
    public void TriggerPunch() => punchTimer = punchDuration;

    private void HandleEnemyAttack(Enemy attacker)
    {
        if (attacker != enemy) return;
        punchTimer = punchDuration;
    }

    private void HandleEnemyDied(Enemy dead)
    {
        if (dead != enemy) return;
        isDead = true;
    }
}
