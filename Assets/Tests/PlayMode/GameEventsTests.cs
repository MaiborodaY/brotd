using NUnit.Framework;

/// <summary>
/// EditMode tests for the GameEvents static event bus.
/// No MonoBehaviour or scene needed — pure C# logic.
/// </summary>
public class GameEventsTests
{
    [SetUp]
    public void SetUp() => GameEvents.ClearAllListeners();

    [TearDown]
    public void TearDown() => GameEvents.ClearAllListeners();

    // ── Gold ──────────────────────────────────────────────────────────────────

    [Test]
    public void RaiseGoldChanged_FiresWithCorrectValue()
    {
        int received = -1;
        GameEvents.OnGoldChanged += g => received = g;

        GameEvents.RaiseGoldChanged(42);

        Assert.AreEqual(42, received);
    }

    [Test]
    public void RaiseGoldChanged_Zero_IsPropagated()
    {
        int received = -1;
        GameEvents.OnGoldChanged += g => received = g;

        GameEvents.RaiseGoldChanged(0);

        Assert.AreEqual(0, received);
    }

    // ── Lives ─────────────────────────────────────────────────────────────────

    [Test]
    public void RaiseLivesChanged_FiresWithCorrectValue()
    {
        int received = -1;
        GameEvents.OnLivesChanged += l => received = l;

        GameEvents.RaiseLivesChanged(5);

        Assert.AreEqual(5, received);
    }

    // ── Phase events ──────────────────────────────────────────────────────────

    [Test]
    public void RaisePrepPhaseStarted_FiresEvent()
    {
        bool fired = false;
        GameEvents.OnPrepPhaseStarted += () => fired = true;

        GameEvents.RaisePrepPhaseStarted();

        Assert.IsTrue(fired);
    }

    [Test]
    public void RaiseWavePhaseStarted_FiresEvent()
    {
        bool fired = false;
        GameEvents.OnWavePhaseStarted += () => fired = true;

        GameEvents.RaiseWavePhaseStarted();

        Assert.IsTrue(fired);
    }

    [Test]
    public void RaiseWavePhaseEnded_FiresEvent()
    {
        bool fired = false;
        GameEvents.OnWavePhaseEnded += () => fired = true;

        GameEvents.RaiseWavePhaseEnded();

        Assert.IsTrue(fired);
    }

    [Test]
    public void RaiseGameOver_FiresEvent()
    {
        bool fired = false;
        GameEvents.OnGameOver += () => fired = true;

        GameEvents.RaiseGameOver();

        Assert.IsTrue(fired);
    }

    [Test]
    public void RaiseVictory_FiresEvent()
    {
        bool fired = false;
        GameEvents.OnVictory += () => fired = true;

        GameEvents.RaiseVictory();

        Assert.IsTrue(fired);
    }

    // ── Wave counters ─────────────────────────────────────────────────────────

    [Test]
    public void RaiseWaveChanged_FiresWithCorrectIndex()
    {
        int received = -1;
        GameEvents.OnWaveChanged += w => received = w;

        GameEvents.RaiseWaveChanged(2);

        Assert.AreEqual(2, received);
    }

    [Test]
    public void RaiseEnemiesRemainingChanged_FiresWithCorrectCount()
    {
        int received = -1;
        GameEvents.OnEnemiesRemainingChanged += c => received = c;

        GameEvents.RaiseEnemiesRemainingChanged(7);

        Assert.AreEqual(7, received);
    }

    // ── ClearAllListeners ─────────────────────────────────────────────────────

    [Test]
    public void ClearAllListeners_PreventsSubscribedHandlerFromFiring()
    {
        int callCount = 0;
        GameEvents.OnGoldChanged += _ => callCount++;

        GameEvents.ClearAllListeners();
        GameEvents.RaiseGoldChanged(99);

        Assert.AreEqual(0, callCount);
    }

    [Test]
    public void ClearAllListeners_ClearsMultipleEvents()
    {
        bool goldFired = false;
        bool livesFired = false;
        GameEvents.OnGoldChanged  += _ => goldFired = true;
        GameEvents.OnLivesChanged += _ => livesFired = true;

        GameEvents.ClearAllListeners();
        GameEvents.RaiseGoldChanged(10);
        GameEvents.RaiseLivesChanged(5);

        Assert.IsFalse(goldFired);
        Assert.IsFalse(livesFired);
    }

    // ── Multiple subscribers ──────────────────────────────────────────────────

    [Test]
    public void MultipleSubscribers_AllReceiveSameEvent()
    {
        int total = 0;
        GameEvents.OnWaveChanged += w => total += w;
        GameEvents.OnWaveChanged += w => total += w;

        GameEvents.RaiseWaveChanged(3);

        Assert.AreEqual(6, total); // 3 + 3
    }

    // ── No subscribers ────────────────────────────────────────────────────────

    [Test]
    public void RaiseWithNoSubscribers_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => GameEvents.RaiseGoldChanged(10));
        Assert.DoesNotThrow(() => GameEvents.RaiseLivesChanged(5));
        Assert.DoesNotThrow(() => GameEvents.RaiseWaveChanged(1));
        Assert.DoesNotThrow(() => GameEvents.RaisePrepPhaseStarted());
        Assert.DoesNotThrow(() => GameEvents.RaiseWavePhaseStarted());
        Assert.DoesNotThrow(() => GameEvents.RaiseGameOver());
        Assert.DoesNotThrow(() => GameEvents.RaiseVictory());
    }
}
