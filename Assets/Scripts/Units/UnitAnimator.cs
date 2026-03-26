using UnityEngine;

/// <summary>
/// Простая процедурная анимация для юнитов с billboard-спрайтом.
/// Добавь на тот же GameObject что и Unit.
/// </summary>
public class UnitAnimator : MonoBehaviour
{
    [Header("Idle Bob")]
    [SerializeField] private float bobHeight    = 0.08f;
    [SerializeField] private float bobSpeed     = 2.2f;

    [Header("Attack Punch")]
    [SerializeField] private float punchScale   = 1.25f;
    [SerializeField] private float punchDuration = 0.12f;

    [Header("Death")]
    [SerializeField] private float deathFallSpeed  = 2f;
    [SerializeField] private float deathFadeSpeed  = 3f;

    // ── internal ──────────────────────────────────────────────────────────────

    private Vector3      baseLocalPos;
    private Vector3      baseLocalScale;
    private float        bobOffset;          // случайный сдвиг фазы чтобы юниты не синхронили
    private float        punchTimer;
    private bool         isDead;

    private Unit         unit;
    private Transform    visual;             // дочерний объект со спрайтом — двигаем только его
    private SpriteRenderer spriteRenderer;   // для fade при смерти

    // ── lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        unit      = GetComponent<Unit>();
        bobOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    // BillboardSprite создаётся в Unit.Init() который вызывается после Awake,
    // поэтому ищем SpriteRenderer лениво при первом Update
    private void InitVisual()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        // Если нашли дочерний спрайт — бобим только его, корень не трогаем
        // Если нет — создаём пустой дочерний объект чтобы не блокировать движение
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

    private void OnEnable()
    {
        GameEvents.OnUnitDied   += HandleUnitDied;
        GameEvents.OnUnitAttack += HandleUnitAttack;
    }

    private void OnDisable()
    {
        GameEvents.OnUnitDied   -= HandleUnitDied;
        GameEvents.OnUnitAttack -= HandleUnitAttack;
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
        float t = punchTimer / punchDuration;                  // 1→0

        // Быстрый пик в начале, потом возврат
        float s = 1f + (punchScale - 1f) * Mathf.Sin(t * Mathf.PI);
        visual.localScale = baseLocalScale * s;

        if (punchTimer <= 0f)
            visual.localScale = baseLocalScale;
    }

    private void TriggerPunch()
    {
        punchTimer = punchDuration;
    }

    // ── death ─────────────────────────────────────────────────────────────────

    private void UpdateDeath()
    {
        // Падает вниз
        visual.localPosition += Vector3.down * deathFallSpeed * Time.deltaTime;

        // Исчезает
        if (spriteRenderer != null)
        {
            var c = spriteRenderer.color;
            c.a -= deathFadeSpeed * Time.deltaTime;
            spriteRenderer.color = c;
        }
    }

    // ── events ────────────────────────────────────────────────────────────────

    private void HandleUnitAttack(Unit attacker)
    {
        if (attacker != unit) return;
        TriggerPunch();
    }

    private void HandleUnitDied(Unit dead)
    {
        if (dead != unit) return;
        isDead = true;
    }
}
