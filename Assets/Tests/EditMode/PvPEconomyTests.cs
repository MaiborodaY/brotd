using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Тесты PvP экономики: золото за убийство и за раунд.
/// </summary>
public class PvPEconomyTests
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

    [Test]
    public void GoldPerKill_100_AddsCorrectly()
    {
        int goldPerKill = 100;
        gsm.EarnGold(goldPerKill);
        Assert.AreEqual(100, gsm.CurrentGold);
    }

    [Test]
    public void ThreeKills_GivesCorrectTotal()
    {
        int goldPerKill = 100;
        gsm.EarnGold(goldPerKill);
        gsm.EarnGold(goldPerKill);
        gsm.EarnGold(goldPerKill);
        Assert.AreEqual(300, gsm.CurrentGold);
    }

    [Test]
    public void RoundBonus_PlusKills_TotalCorrect()
    {
        int goldPerRound = 60;
        int goldPerKill  = 100;
        int kills        = 3;

        gsm.EarnGold(goldPerRound);
        for (int i = 0; i < kills; i++)
            gsm.EarnGold(goldPerKill);

        Assert.AreEqual(360, gsm.CurrentGold); // 60 + 3*100
    }

    [Test]
    public void CanAffordUnit_AfterKills()
    {
        int unitCost = 150;
        gsm.EarnGold(100); // убил 1 юнита
        gsm.EarnGold(100); // убил 2 юнита

        bool canAfford = gsm.TrySpendGold(unitCost);
        Assert.IsTrue(canAfford);
        Assert.AreEqual(50, gsm.CurrentGold); // 200 - 150
    }
}
