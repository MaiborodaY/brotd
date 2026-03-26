using UnityEngine;

/// <summary>
/// Лучник: стреляет по врагам в радиусе, не заставляет их останавливаться.
/// Враг продолжает идти пока не встретит мечника или не умрёт.
/// </summary>
public class RangedUnit : Unit
{
    protected override void PerformAttack(Enemy target)
    {
        target.TakeDamage(Data.attackDamage);
        GameEvents.RaiseUnitAttack(this);

        // Если враг вплотную — останавливаем его, он дерётся с нами
        if (Vector3.Distance(transform.position, target.transform.position) <= 1.5f)
            target.EngageWith(this);

        Arrow.Shoot(transform.position, target);
    }

    protected override void OnStateChanged(UnitState newState) { }
}
