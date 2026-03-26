using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Тесты боевой логики: урон, смерть, HP у PvPUnitEnemy.
/// </summary>
public class CombatTests
{
    // ── PvPUnitEnemy HP / смерть ──────────────────────────────────────────────

    private PvPUnitEnemy CreateEnemy(int maxHp)
    {
        var go = new GameObject("PvPEnemy");
        go.AddComponent<CapsuleCollider>();
        var enemy = go.AddComponent<PvPUnitEnemy>();

        var data = ScriptableObject.CreateInstance<UnitData>();
        data.maxHealth    = maxHp;
        data.moveSpeed    = 1f;
        data.attackDamage = 10f;
        data.attackRange  = 1f;
        data.attackCooldown = 1f;
        data.detectionRange = 3f;
        data.hpBarHeight  = 1f;

        var path = new Vector3[] { Vector3.zero, Vector3.forward * 10f };
        enemy.Init(data, path);

        return enemy;
    }

    [TearDown]
    public void TearDown()
    {
        // Чистим все объекты после каждого теста
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            Object.DestroyImmediate(go);

        PvPUnitEnemy.ActiveList.Clear();
    }

    [Test]
    public void PvPEnemy_StartsAlive()
    {
        var enemy = CreateEnemy(100);
        Assert.IsTrue(enemy.IsAlive);
    }

    [Test]
    public void TakeDamage_ReducesHP_EnemyStaysAlive()
    {
        var enemy = CreateEnemy(100);
        enemy.TakeDamage(40f);
        Assert.IsTrue(enemy.IsAlive);
    }

    [Test]
    public void TakeDamage_ExactHP_EnemyDies()
    {
        var enemy = CreateEnemy(100);
        enemy.TakeDamage(100f);
        Assert.IsFalse(enemy.IsAlive);
    }

    [Test]
    public void TakeDamage_OverKill_EnemyDies()
    {
        var enemy = CreateEnemy(100);
        enemy.TakeDamage(999f);
        Assert.IsFalse(enemy.IsAlive);
    }

    [Test]
    public void TakeDamage_WhenDead_DoesNothing()
    {
        var enemy = CreateEnemy(100);
        enemy.TakeDamage(100f); // убиваем
        // Повторный урон не должен крашить
        Assert.DoesNotThrow(() => enemy.TakeDamage(50f));
    }

    [Test]
    public void OnDied_Event_Fires()
    {
        var enemy = CreateEnemy(50);
        bool fired = false;
        PvPUnitEnemy.OnDied += _ => fired = true;

        enemy.TakeDamage(50f);

        PvPUnitEnemy.OnDied -= _ => fired = true;
        Assert.IsTrue(fired);
    }

    [Test]
    public void ActiveList_RemovesEnemyOnDeath()
    {
        var enemy = CreateEnemy(50);
        int countBefore = PvPUnitEnemy.ActiveList.Count;

        enemy.TakeDamage(50f);

        Assert.AreEqual(countBefore - 1, PvPUnitEnemy.ActiveList.Count);
    }
}
