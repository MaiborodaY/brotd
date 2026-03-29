using UnityEngine;

/// <summary>
/// Монк-хилер: ищет союзника с наименьшим HP, подходит к нему и лечит.
/// Врагов не атакует.
/// </summary>
public class MonkUnit : Unit
{
    [Header("Healing")]
    public float healAmount   = 20f;
    public float healCooldown = 2f;
    public float healRange    = 1f;

    private Unit   healTarget;
    private float  healTimer;

    protected override void Update()
    {
        if (State == UnitState.Dead) return;

        healthBar?.SetHeight(Data.hpBarHeight);

        healTarget = FindMostWoundedAlly();

        if (healTarget == null)
        {
            if (State == UnitState.Fighting) SetState(UnitState.Idle);
            return;
        }

        SetState(UnitState.Fighting);

        float dist = Vector3.Distance(transform.position, healTarget.transform.position);

        if (dist > healRange)
        {
            Vector3 dir = healTarget.transform.position - transform.position;
            dir.y = 0f;
            transform.position += dir.normalized * (Data.moveSpeed * Time.deltaTime);
            return;
        }

        healTimer -= Time.deltaTime;
        if (healTimer <= 0f)
        {
            healTimer = healCooldown;
            healTarget.Heal(healAmount);
            GameEvents.RaiseUnitAttack(this);
        }
    }

    private Unit FindMostWoundedAlly()
    {
        Unit target  = null;
        int  lowestHp = int.MaxValue;

        foreach (var unit in Unit.ActiveUnits)
        {
            if (unit == this) continue;
            if (!unit.IsAlive) continue;
            if (unit.CurrentHp >= unit.MaxHp) continue;
            if (unit.CurrentHp < lowestHp)
            {
                lowestHp = unit.CurrentHp;
                target   = unit;
            }
        }
        return target;
    }

    protected override void PerformAttack(Enemy target) { }
}
