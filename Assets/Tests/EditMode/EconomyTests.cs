using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Тесты экономики: золото и жизни в GameStateMachine.
/// EditMode — не требует сцены, Start() не вызывается автоматически.
/// Начальное золото = 0, накапливаем через EarnGold.
/// </summary>
public class EconomyTests
{
    private GameStateMachine gsm;

    [SetUp]
    public void SetUp()
    {
        var go = new GameObject("GSM");
        gsm = go.AddComponent<GameStateMachine>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(gsm.gameObject);
    }

    // ── EarnGold ──────────────────────────────────────────────────────────────

    [Test]
    public void EarnGold_IncreasesGold()
    {
        gsm.EarnGold(100);
        Assert.AreEqual(100, gsm.CurrentGold);
    }

    [Test]
    public void EarnGold_Accumulates()
    {
        gsm.EarnGold(50);
        gsm.EarnGold(75);
        Assert.AreEqual(125, gsm.CurrentGold);
    }

    // ── TrySpendGold ──────────────────────────────────────────────────────────

    [Test]
    public void TrySpendGold_WhenEnoughGold_ReturnsTrue()
    {
        gsm.EarnGold(200);
        bool result = gsm.TrySpendGold(100);
        Assert.IsTrue(result);
    }

    [Test]
    public void TrySpendGold_WhenEnoughGold_DeductsCorrectly()
    {
        gsm.EarnGold(200);
        gsm.TrySpendGold(80);
        Assert.AreEqual(120, gsm.CurrentGold);
    }

    [Test]
    public void TrySpendGold_WhenNotEnoughGold_ReturnsFalse()
    {
        gsm.EarnGold(50);
        bool result = gsm.TrySpendGold(100);
        Assert.IsFalse(result);
    }

    [Test]
    public void TrySpendGold_WhenNotEnoughGold_DoesNotDeduct()
    {
        gsm.EarnGold(50);
        gsm.TrySpendGold(100);
        Assert.AreEqual(50, gsm.CurrentGold); // золото не изменилось
    }

    [Test]
    public void TrySpendGold_ExactAmount_LeavesZero()
    {
        gsm.EarnGold(100);
        gsm.TrySpendGold(100);
        Assert.AreEqual(0, gsm.CurrentGold);
    }
}
