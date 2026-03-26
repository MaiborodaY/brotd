using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// PlayMode tests for GameStateMachine gold/lives logic and phase transitions.
/// Each test creates a fresh GameStateMachine so there are no cross-test dependencies.
/// </summary>
public class GameStateMachineTests
{
    private GameObject go;
    private GameStateMachine gsm;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        GameEvents.ClearAllListeners();
        Time.timeScale = 1f;

        go  = new GameObject("GameStateMachine");
        gsm = go.AddComponent<GameStateMachine>();

        yield return null; // let Awake + Start run
    }

    [TearDown]
    public void TearDown()
    {
        if (go != null) Object.Destroy(go);
        GameEvents.ClearAllListeners();
        Time.timeScale = 1f;
    }

    // ── Initial state ─────────────────────────────────────────────────────────

    [Test]
    public void Start_InitializesGold_ToNonZeroDefault()
    {
        Assert.Greater(gsm.CurrentGold, 0);
    }

    [Test]
    public void Start_InitializesLives_ToNonZeroDefault()
    {
        Assert.Greater(gsm.CurrentLives, 0);
    }

    [Test]
    public void Start_PhaseIsPrep()
    {
        Assert.AreEqual(GamePhase.Prep, gsm.CurrentPhase);
    }

    // ── TrySpendGold ──────────────────────────────────────────────────────────

    [Test]
    public void TrySpendGold_ReturnsFalse_WhenNotEnoughGold()
    {
        gsm.TrySpendGold(gsm.CurrentGold); // drain to zero
        bool result = gsm.TrySpendGold(1);
        Assert.IsFalse(result);
    }

    [Test]
    public void TrySpendGold_ReturnsTrue_WhenEnoughGold()
    {
        bool result = gsm.TrySpendGold(1);
        Assert.IsTrue(result);
    }

    [Test]
    public void TrySpendGold_DeductsCorrectAmount()
    {
        int before = gsm.CurrentGold;
        gsm.TrySpendGold(50);
        Assert.AreEqual(before - 50, gsm.CurrentGold);
    }

    [Test]
    public void TrySpendGold_DoesNotDeduct_WhenNotEnoughGold()
    {
        gsm.TrySpendGold(gsm.CurrentGold); // drain
        int before = gsm.CurrentGold;      // should be 0
        gsm.TrySpendGold(10);
        Assert.AreEqual(before, gsm.CurrentGold);
    }

    [Test]
    public void TrySpendGold_FiresOnGoldChanged_OnSuccess()
    {
        int received = -1;
        GameEvents.OnGoldChanged += g => received = g;

        gsm.TrySpendGold(30);

        Assert.AreEqual(gsm.CurrentGold, received);
    }

    [Test]
    public void TrySpendGold_DoesNotFireOnGoldChanged_OnFailure()
    {
        gsm.TrySpendGold(gsm.CurrentGold); // drain
        int callCount = 0;
        GameEvents.OnGoldChanged += _ => callCount++;

        gsm.TrySpendGold(999); // should fail silently

        Assert.AreEqual(0, callCount);
    }

    // ── EarnGold ──────────────────────────────────────────────────────────────

    [Test]
    public void EarnGold_IncreasesGoldByAmount()
    {
        int before = gsm.CurrentGold;
        gsm.EarnGold(25);
        Assert.AreEqual(before + 25, gsm.CurrentGold);
    }

    [Test]
    public void EarnGold_FiresOnGoldChanged_WithNewTotal()
    {
        int received = -1;
        GameEvents.OnGoldChanged += g => received = g;

        gsm.EarnGold(25);

        Assert.AreEqual(gsm.CurrentGold, received);
    }

    [Test]
    public void EarnGold_Zero_DoesNotChangeGold()
    {
        int before = gsm.CurrentGold;
        gsm.EarnGold(0);
        Assert.AreEqual(before, gsm.CurrentGold);
    }

    // ── Phase transitions ─────────────────────────────────────────────────────

    [Test]
    public void StartWave_TransitionsToWave_WhenInPrep()
    {
        Assert.AreEqual(GamePhase.Prep, gsm.CurrentPhase);
        gsm.StartWave();
        Assert.AreEqual(GamePhase.Wave, gsm.CurrentPhase);
    }

    [Test]
    public void StartWave_IsIgnored_WhenAlreadyInWave()
    {
        gsm.StartWave();
        gsm.StartWave(); // second call should be ignored
        Assert.AreEqual(GamePhase.Wave, gsm.CurrentPhase);
    }

    [Test]
    public void StartWave_FiresOnWavePhaseStarted()
    {
        bool fired = false;
        GameEvents.OnWavePhaseStarted += () => fired = true;

        gsm.StartWave();

        Assert.IsTrue(fired);
    }

    [Test]
    public void EndWave_WithNoMoreWaves_EntersVictory()
    {
        gsm.StartWave();
        gsm.EndWave(hasMoreWaves: false);
        Assert.AreEqual(GamePhase.Victory, gsm.CurrentPhase);
    }

    [Test]
    public void EndWave_WithNoMoreWaves_FiresOnVictory()
    {
        bool fired = false;
        GameEvents.OnVictory += () => fired = true;

        gsm.StartWave();
        gsm.EndWave(hasMoreWaves: false);

        Assert.IsTrue(fired);
    }

    [Test]
    public void EndWave_WithMoreWaves_EntersWaveResult()
    {
        gsm.StartWave();
        gsm.EndWave(hasMoreWaves: true);
        Assert.AreEqual(GamePhase.WaveResult, gsm.CurrentPhase);
    }

    [Test]
    public void EndWave_WithMoreWaves_FiresOnWavePhaseEnded()
    {
        bool fired = false;
        GameEvents.OnWavePhaseEnded += () => fired = true;

        gsm.StartWave();
        gsm.EndWave(hasMoreWaves: true);

        Assert.IsTrue(fired);
    }

    [Test]
    public void EndWave_IsIgnored_WhenNotInWavePhase()
    {
        // Still in Prep — EndWave should do nothing
        gsm.EndWave(hasMoreWaves: false);
        Assert.AreEqual(GamePhase.Prep, gsm.CurrentPhase);
    }
}
