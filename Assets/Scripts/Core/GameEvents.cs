using System;
using UnityEngine;

/// <summary>
/// Централизованная шина событий. Все системы общаются через неё — никаких FindObjectOfType.
/// </summary>
public static class GameEvents
{
    // ── Фазы игры ─────────────────────────────────────────────────────────────
    public static event Action OnPrepPhaseStarted;
    public static event Action OnWavePhaseStarted;
    public static event Action OnWavePhaseEnded;
    public static event Action OnGameOver;
    public static event Action OnVictory;

    // ── Волны ─────────────────────────────────────────────────────────────────
    public static event Action<int> OnWaveChanged;         // номер волны (0-based)
    public static event Action<int> OnEnemiesRemainingChanged; // сколько врагов осталось живых

    // ── Враги ─────────────────────────────────────────────────────────────────
    public static event Action<Enemy> OnEnemySpawned;
    public static event Action<Enemy> OnEnemyDied;
    public static event Action<Enemy> OnEnemyReachedBase;
    public static event Action<Enemy> OnEnemyLeaked;
    public static event Action<Enemy> OnEnemyAttack;
    public static event Action<Vector3> OnEnemyHit;

    // ── Юниты ─────────────────────────────────────────────────────────────────
    public static event Action<Unit> OnUnitPlaced;
    public static event Action<Unit> OnUnitDied;
    public static event Action<Unit> OnUnitRespawned;
    public static event Action<Unit> OnUnitAttack;

    // ── Ресурсы ───────────────────────────────────────────────────────────────
    public static event Action<int> OnGoldChanged;         // текущий баланс
    public static event Action<int> OnLivesChanged;        // текущие жизни

    // ── Размещение ────────────────────────────────────────────────────────────
    public static event Action<GridCell> OnCellSelected;
    public static event Action OnPlacementCancelled;
    public static event Action<UnitData> OnUnitSelected;

    // =========================================================================
    // Вызовы (вызываются только из владеющих систем)
    // =========================================================================

    public static void RaisePrepPhaseStarted()           => OnPrepPhaseStarted?.Invoke();
    public static void RaiseWavePhaseStarted()           => OnWavePhaseStarted?.Invoke();
    public static void RaiseWavePhaseEnded()             => OnWavePhaseEnded?.Invoke();
    public static void RaiseGameOver()                   => OnGameOver?.Invoke();
    public static void RaiseVictory()                    => OnVictory?.Invoke();

    public static void RaiseWaveChanged(int wave)        => OnWaveChanged?.Invoke(wave);
    public static void RaiseEnemiesRemainingChanged(int count) => OnEnemiesRemainingChanged?.Invoke(count);

    public static void RaiseEnemySpawned(Enemy e)        => OnEnemySpawned?.Invoke(e);
    public static void RaiseEnemyDied(Enemy e)           => OnEnemyDied?.Invoke(e);
    public static void RaiseEnemyReachedBase(Enemy e)    => OnEnemyReachedBase?.Invoke(e);
    public static void RaiseEnemyLeaked(Enemy e)         => OnEnemyLeaked?.Invoke(e);
    public static void RaiseEnemyAttack(Enemy e)         => OnEnemyAttack?.Invoke(e);
    public static void RaiseEnemyHit(Vector3 pos)        => OnEnemyHit?.Invoke(pos);

    public static void RaiseUnitPlaced(Unit u)           => OnUnitPlaced?.Invoke(u);
    public static void RaiseUnitDied(Unit u)             => OnUnitDied?.Invoke(u);
    public static void RaiseUnitRespawned(Unit u)        => OnUnitRespawned?.Invoke(u);
    public static void RaiseUnitAttack(Unit u)           => OnUnitAttack?.Invoke(u);

    public static void RaiseGoldChanged(int gold)        => OnGoldChanged?.Invoke(gold);
    public static void RaiseLivesChanged(int lives)      => OnLivesChanged?.Invoke(lives);

    public static void RaiseCellSelected(GridCell cell)  => OnCellSelected?.Invoke(cell);
    public static void RaisePlacementCancelled()         => OnPlacementCancelled?.Invoke();
    public static void RaiseUnitSelected(UnitData d)     => OnUnitSelected?.Invoke(d);

    // =========================================================================
    // Очистка подписок при выгрузке сцены (вызывать из GameStateMachine)
    // =========================================================================
    public static void ClearAllListeners()
    {
        OnPrepPhaseStarted = null;
        OnWavePhaseStarted = null;
        OnWavePhaseEnded = null;
        OnGameOver = null;
        OnVictory = null;
        OnWaveChanged = null;
        OnEnemiesRemainingChanged = null;
        OnEnemySpawned = null;
        OnEnemyDied = null;
        OnEnemyReachedBase = null;
        OnEnemyLeaked = null;
        OnEnemyAttack = null;
        OnEnemyHit = null;
        OnUnitPlaced = null;
        OnUnitDied = null;
        OnUnitRespawned = null;
        OnUnitAttack = null;
        OnGoldChanged = null;
        OnLivesChanged = null;
        OnCellSelected = null;
        OnPlacementCancelled = null;
        OnUnitSelected = null;
    }
}
