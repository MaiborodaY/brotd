using UnityEngine;

/// <summary>
/// Переключает анимации орка напрямую по состоянию Enemy.
/// Названия клипов должны совпадать с теми что в Animator Controller.
/// </summary>
public class EnemyVisuals : MonoBehaviour
{
    [SerializeField] private string clipWalk   = "Orc_Walk";
    [SerializeField] private string clipAttack = "Orc_Attack";
    [SerializeField] private string clipDie    = "Orc_Die";

    private Enemy      enemy;
    private Animator   animator;
    private EnemyState lastState = (EnemyState)(-1); // гарантируем первое обновление

    public void Init(Enemy e, Animator a)
    {
        enemy     = e;
        animator  = a;
        lastState = (EnemyState)(-1);
    }

    private void Update()
    {
        if (enemy == null || animator == null) return;
        if (enemy.State == lastState) return;

        lastState = enemy.State;

        switch (enemy.State)
        {
            case EnemyState.Moving:   animator.Play(clipWalk);   break;
            case EnemyState.Fighting: animator.Play(clipAttack); break;
            case EnemyState.Dead:     animator.Play(clipDie);    break;
        }
    }
}
