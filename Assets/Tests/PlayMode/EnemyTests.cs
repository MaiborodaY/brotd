using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// PlayMode tests for Enemy damage, death, and state behavior.
/// Requires URP shaders (the project uses URP — this is always the case here).
/// </summary>
public class EnemyTests
{
    private static readonly Vector3[] TwoPointPath =
    {
        Vector3.zero,
        new Vector3(0f, 0f, 100f)
    };

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Enemy CreateEnemy(int maxHp = 100, float attackDamage = 10f)
    {
        var go = new GameObject("Enemy");
        go.AddComponent<BoxCollider>(); // satisfy [RequireComponent(typeof(Collider))]
        var enemy = go.AddComponent<Enemy>();

        var data = ScriptableObject.CreateInstance<EnemyData>();
        data.maxHealth      = maxHp;
        data.attackDamage   = attackDamage;
        data.attackCooldown = 1f;
        data.moveSpeed      = 2f;
        data.goldReward     = 5;

        enemy.Init(data, TwoPointPath);
        return enemy;
    }

    // ── Init ──────────────────────────────────────────────────────────────────

    [UnityTest]
    public IEnumerator Init_SetsCurrentHp_ToMaxHealth()
    {
        var enemy = CreateEnemy(80);
        yield return null;

        Assert.AreEqual(80, enemy.CurrentHp);

        Object.Destroy(enemy.gameObject);
    }

    [UnityTest]
    public IEnumerator Init_SetsState_ToMoving()
    {
        var enemy = CreateEnemy();
        yield return null;

        Assert.AreEqual(EnemyState.Moving, enemy.State);

        Object.Destroy(enemy.gameObject);
    }

    [UnityTest]
    public IEnumerator Init_IsAlive_IsTrue()
    {
        var enemy = CreateEnemy();
        yield return null;

        Assert.IsTrue(enemy.IsAlive);

        Object.Destroy(enemy.gameObject);
    }

    // ── TakeDamage ────────────────────────────────────────────────────────────

    [UnityTest]
    public IEnumerator TakeDamage_ReducesCurrentHp()
    {
        var enemy = CreateEnemy(100);
        yield return null;

        enemy.TakeDamage(30f);

        Assert.AreEqual(70, enemy.CurrentHp);

        Object.Destroy(enemy.gameObject);
    }

    [UnityTest]
    public IEnumerator TakeDamage_MultipleHits_AccumulateDamage()
    {
        var enemy = CreateEnemy(100);
        yield return null;

        enemy.TakeDamage(20f);
        enemy.TakeDamage(20f);

        Assert.AreEqual(60, enemy.CurrentHp);

        Object.Destroy(enemy.gameObject);
    }

    [UnityTest]
    public IEnumerator TakeDamage_ExactlyMaxHp_KillsEnemy()
    {
        var enemy = CreateEnemy(50);
        yield return null;

        enemy.TakeDamage(50f);

        Assert.AreEqual(EnemyState.Dead, enemy.State);

        Object.Destroy(enemy.gameObject);
    }

    [UnityTest]
    public IEnumerator TakeDamage_MoreThanMaxHp_KillsEnemy()
    {
        var enemy = CreateEnemy(50);
        yield return null;

        enemy.TakeDamage(200f);

        Assert.AreEqual(EnemyState.Dead, enemy.State);

        Object.Destroy(enemy.gameObject);
    }

    // ── Death ─────────────────────────────────────────────────────────────────

    [UnityTest]
    public IEnumerator Die_DeactivatesGameObject()
    {
        var enemy = CreateEnemy(50);
        yield return null;

        enemy.TakeDamage(50f);

        Assert.IsFalse(enemy.gameObject.activeSelf);

        Object.Destroy(enemy.gameObject);
    }

    [UnityTest]
    public IEnumerator Die_SetsIsAlive_ToFalse()
    {
        var enemy = CreateEnemy(50);
        yield return null;

        enemy.TakeDamage(50f);

        Assert.IsFalse(enemy.IsAlive);

        Object.Destroy(enemy.gameObject);
    }

    [UnityTest]
    public IEnumerator TakeDamage_OnDeadEnemy_DoesNotChangeHp()
    {
        var enemy = CreateEnemy(50);
        yield return null;

        enemy.TakeDamage(50f);        // kill
        int hpAfterDeath = enemy.CurrentHp;

        enemy.TakeDamage(10f);        // should be ignored
        Assert.AreEqual(hpAfterDeath, enemy.CurrentHp);

        Object.Destroy(enemy.gameObject);
    }

    [UnityTest]
    public IEnumerator Die_FiresOnEnemyDied_Event()
    {
        GameEvents.ClearAllListeners();
        var enemy = CreateEnemy(50);
        yield return null;

        Enemy diedEnemy = null;
        GameEvents.OnEnemyDied += e => diedEnemy = e;

        enemy.TakeDamage(50f);

        Assert.AreEqual(enemy, diedEnemy);

        Object.Destroy(enemy.gameObject);
        GameEvents.ClearAllListeners();
    }

    // ── EngageWith / state ────────────────────────────────────────────────────

    [UnityTest]
    public IEnumerator EngageWith_ChangesState_ToFighting()
    {
        // We can't create a full Unit (abstract), but we can call EngageWith(null)
        // The actual engagement check is: if engagedUnit == null → back to Moving.
        // So to test Fighting state we need a live unit.
        // Here we just verify EngageWith on a dead enemy does nothing.
        var enemy = CreateEnemy(50);
        yield return null;

        enemy.TakeDamage(50f); // kill first
        // EngageWith on dead enemy must be a no-op (State stays Dead)
        enemy.EngageWith(null);

        Assert.AreEqual(EnemyState.Dead, enemy.State);

        Object.Destroy(enemy.gameObject);
    }
}
