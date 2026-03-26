using UnityEngine;

/// <summary>
/// Мечник: стоит на клетке, бьёт врага в ближнем бою.
/// Враг, которого атакует мечник, останавливается и сражается с ним.
/// </summary>
public class MeleeUnit : Unit
{
    protected override void PerformAttack(Enemy target)
    {
        target.TakeDamage(Data.attackDamage);
        target.EngageWith(this);
        GameEvents.RaiseUnitAttack(this);
    }

    protected override void OnStateChanged(UnitState newState)
    {
        // Здесь можно запускать анимации когда они появятся
    }
}
