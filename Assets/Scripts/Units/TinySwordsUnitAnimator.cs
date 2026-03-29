using UnityEngine;

/// <summary>
/// Анимирует юнита через Animator Controller из Tiny Swords.
/// Каждый кадр синхронизирует анимацию с реальным состоянием юнита.
/// </summary>
public class TinySwordsUnitAnimator : MonoBehaviour
{
    private Animator  animator;
    private Transform visual;
    private Unit      unit;
    private string    attackAnimName = "Attack 1";
    private Vector3   lastPosition;
    private string    currentAnim;

    // ── Инициализация ─────────────────────────────────────────────────────────

    public void Init(RuntimeAnimatorController controller, string attackAnim)
    {
        attackAnimName = attackAnim;
        unit           = GetComponent<Unit>();
        lastPosition   = transform.position;

        var existing = transform.Find("Visual");
        var go = existing != null ? existing.gameObject : new GameObject("Visual");
        if (existing == null) go.transform.SetParent(transform, false);
        visual = go.transform;

        if (go.GetComponent<SpriteRenderer>() == null) go.AddComponent<SpriteRenderer>();
        animator = go.GetComponent<Animator>();
        if (animator == null) animator = go.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;

        var oldAnim = GetComponent<UnitAnimator>();
        if (oldAnim != null) oldAnim.enabled = false;
    }

    // ── Update ────────────────────────────────────────────────────────────────

    private void Update()
    {
        if (unit == null || animator == null) return;

        float moved  = Vector3.Distance(transform.position, lastPosition);
        bool isMoving = moved > 0.001f;
        lastPosition  = transform.position;

        string target = unit.State switch
        {
            UnitState.Fighting => isMoving ? "Run" : attackAnimName,
            UnitState.Idle     => isMoving ? "Run" : "Idle",
            _                  => "Idle"
        };

        if (target != currentAnim)
        {
            currentAnim = target;
            int hash = Animator.StringToHash(target);
            if (animator.HasState(0, hash))
                animator.Play(hash, 0, 0f);
        }
    }

    private void LateUpdate()
    {
        if (visual == null) return;
        var cam = Camera.main;
        if (cam != null) visual.rotation = cam.transform.rotation;
    }
}
