using UnityEngine;

/// <summary>
/// Король — последний защитник базы.
/// Атакует как мечник, возвращается на спавн когда нет врагов.
/// HP не восстанавливается между волнами. Смерть = Game Over.
/// </summary>
public class KingUnit : MeleeUnit
{
    private Vector3 spawnPosition;

    public void InitKing(UnitData data, Vector3 position)
    {
        spawnPosition = position;
        transform.position = position;
        Init(data, cell: null);
    }

    // ── Возврат на базу ───────────────────────────────────────────────────────

    protected override void Update()
    {
        base.Update();

        // Когда нет цели — возвращаемся на спавн
        if (State == UnitState.Idle && IsAlive)
        {
            float dist = Vector3.Distance(transform.position, spawnPosition);
            if (dist > 0.1f)
                transform.position = Vector3.MoveTowards(
                    transform.position, spawnPosition, Data.moveSpeed * Time.deltaTime);
        }
    }

    // ── Конец волны — позиция сбрасывается, HP нет ───────────────────────────

    protected override void OnWaveEndedNoCell()
    {
        attackTimer = 0f;
        targetEnemy = null;
        transform.position = spawnPosition;
        gameObject.SetActive(true);
    }

    // ── Переопределения ───────────────────────────────────────────────────────

    protected override bool RestoreHpOnWaveEnd() => false;

    protected override bool SurviveOnDeath()     => true;

    protected override void OnDied()
    {
        GameStateMachine.Instance?.KingDied();
    }
}
