using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Фазы игры.
/// </summary>
public enum GamePhase
{
    Prep,        // Фаза подготовки — игрок расставляет юнитов
    Wave,        // Волна идёт — враги движутся, юниты дерутся
    WaveResult,  // Волна закончилась — результат перед следующей
    GameOver,    // Жизни кончились
    Victory      // Все волны пройдены
}

/// <summary>
/// Управляет фазами игры. Единственный компонент, который меняет GamePhase.
/// </summary>
public class GameStateMachine : MonoBehaviour
{
    public static GameStateMachine Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private int startingLives = 10;
    [SerializeField] private int startingGold  = 150;

    public GamePhase CurrentPhase { get; private set; } = GamePhase.Prep;
    public int CurrentLives       { get; private set; }
    public int CurrentGold        { get; private set; }

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Защита от нуля если SetupScene не проставил поля
        CurrentLives = startingLives > 0 ? startingLives : 10;
        CurrentGold  = startingGold  > 0 ? startingGold  : 500;

        GameEvents.OnEnemyReachedBase += HandleEnemyReachedBase;

        // Уведомляем UI о начальных значениях
        GameEvents.RaiseGoldChanged(CurrentGold);
        GameEvents.RaiseLivesChanged(CurrentLives);

        EnterPrep();
    }

    private void OnDestroy()
    {
        GameEvents.OnEnemyReachedBase -= HandleEnemyReachedBase;

        if (Instance == this)
            GameEvents.ClearAllListeners();
    }

    // ── Переходы между фазами ─────────────────────────────────────────────────

    public void EnterPrep()
    {
        if (CurrentPhase == GamePhase.GameOver || CurrentPhase == GamePhase.Victory)
            return;

        CurrentPhase = GamePhase.Prep;
        GameEvents.RaisePrepPhaseStarted();
    }

    public void StartWave()
    {
        if (CurrentPhase != GamePhase.Prep)
            return;

        CurrentPhase = GamePhase.Wave;
        GameEvents.RaiseWavePhaseStarted();
    }

    /// <summary>
    /// Вызывается WaveManager когда все враги волны мертвы или добрались до базы.
    /// </summary>
    public void EndWave(bool hasMoreWaves)
    {
        if (CurrentPhase != GamePhase.Wave)
            return;

        CurrentPhase = GamePhase.WaveResult;
        GameEvents.RaiseWavePhaseEnded();

        if (!hasMoreWaves)
        {
            EnterVictory();
            return;
        }

        // Небольшая задержка перед возвратом в Prep — юниты respawn, UI показывает результат
        Invoke(nameof(EnterPrep), 2f);
    }

    private void EnterVictory()
    {
        CurrentPhase = GamePhase.Victory;
        GameEvents.RaiseVictory();
    }

    private void EnterGameOver()
    {
        CurrentPhase = GamePhase.GameOver;
        GameEvents.RaiseGameOver();
    }

    // ── Ресурсы ───────────────────────────────────────────────────────────────

    public bool TrySpendGold(int amount)
    {
        if (CurrentGold < amount)
            return false;

        CurrentGold -= amount;
        GameEvents.RaiseGoldChanged(CurrentGold);
        return true;
    }

    public void EarnGold(int amount)
    {
        CurrentGold += amount;
        GameEvents.RaiseGoldChanged(CurrentGold);
    }

    // ── Обработка событий ─────────────────────────────────────────────────────

    private void HandleEnemyReachedBase(Enemy enemy)
    {
        // Враг добежал до финишной линии — считаем статистику (пропущенные враги)
        CurrentLives++;
        GameEvents.RaiseLivesChanged(CurrentLives);
    }

    /// <summary>Вызывается когда Король умирает — единственное условие поражения.</summary>
    public void KingDied()
    {
        EnterGameOver();
    }

    // ── Рестарт ───────────────────────────────────────────────────────────────

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
