using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Управляет волнами: спавнит врагов, отслеживает когда волна закончилась,
/// сообщает GameStateMachine о переходах.
/// </summary>
public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    /// <summary>Все активные (живые) враги на сцене.</summary>
    public static IReadOnlyList<Enemy> ActiveEnemies => instance_activeEnemies;
    private static readonly List<Enemy> instance_activeEnemies = new();

    [Header("References")]
    [SerializeField] private PathData pathData;
    [SerializeField] private Transform spawnPoint;

    [Header("Lanes")]
    [SerializeField] private int   laneCount   = 3;
    [SerializeField] private float laneSpacing = 2f;

    [Header("Waves")]
    [SerializeField] private WaveData[] waves;

    private int   currentWaveIndex = -1;
    private int   enemiesAlive;
    private int   enemiesSpawned;
    private int   totalEnemiesInWave;
    private bool  spawningComplete;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        instance_activeEnemies.Clear(); // сброс при перезагрузке сцены
    }

    private void OnEnable()
    {
        GameEvents.OnWavePhaseStarted += StartNextWave;
        GameEvents.OnEnemyDied        += OnEnemyRemoved;
        GameEvents.OnEnemyReachedBase += OnEnemyRemoved;
    }

    private void OnDisable()
    {
        GameEvents.OnWavePhaseStarted -= StartNextWave;
        GameEvents.OnEnemyDied        -= OnEnemyRemoved;
        GameEvents.OnEnemyReachedBase -= OnEnemyRemoved;
    }

    // ── Волны ─────────────────────────────────────────────────────────────────

    private void StartNextWave()
    {
        currentWaveIndex++;

        if (currentWaveIndex >= waves.Length)
        {
            GameStateMachine.Instance.EndWave(hasMoreWaves: false);
            return;
        }

        WaveData wave = waves[currentWaveIndex];

        // Бонус золота в начале волны
        if (wave.prepGoldBonus > 0)
            GameStateMachine.Instance.EarnGold(wave.prepGoldBonus);

        // Считаем всего врагов в волне
        totalEnemiesInWave = 0;
        foreach (var entry in wave.entries)
            totalEnemiesInWave += entry.count * (entry.centerLaneOnly ? 1 : laneCount);

        enemiesAlive   = 0;
        enemiesSpawned = 0;
        spawningComplete = false;

        instance_activeEnemies.Clear();

        GameEvents.RaiseWaveChanged(currentWaveIndex);
        GameEvents.RaiseEnemiesRemainingChanged(totalEnemiesInWave);

        StartCoroutine(SpawnRoutine(wave));
    }

    private IEnumerator SpawnRoutine(WaveData wave)
    {
        foreach (var entry in wave.entries)
        {
            for (int i = 0; i < entry.count; i++)
            {
                if (entry.centerLaneOnly)
                {
                    SpawnEnemy(entry.enemyData, laneCount / 2);
                }
                else
                {
                    for (int lane = 0; lane < laneCount; lane++)
                    {
                        SpawnEnemy(entry.enemyData, lane);
                        yield return new WaitForSeconds(Random.Range(0.2f, 0.7f));
                    }
                }
                yield return new WaitForSeconds(wave.spawnInterval);
            }
        }

        spawningComplete = true;
        CheckWaveEnd();
    }

    private void SpawnEnemy(EnemyData data, int lane)
    {
        if (data.prefab == null)
        {
            Debug.LogWarning($"WaveManager: у {data.name} нет prefab.");
            return;
        }

        float xOffset = (lane - (laneCount - 1) / 2f) * laneSpacing;

        Vector3 baseSpawn = spawnPoint != null ? spawnPoint.position : pathData.waypoints[0];
        Vector3 pos = baseSpawn + new Vector3(xOffset, 0, 0);

        // Смещаем весь путь на X для этой дорожки
        var laneWaypoints = new Vector3[pathData.waypoints.Length];
        for (int i = 0; i < pathData.waypoints.Length; i++)
            laneWaypoints[i] = pathData.waypoints[i] + new Vector3(xOffset, 0, 0);

        GameObject go = Instantiate(data.prefab, pos, Quaternion.identity);

        Enemy enemy = go.GetComponent<Enemy>();
        if (enemy == null)
        {
            Debug.LogError($"WaveManager: prefab {data.prefab.name} не содержит Enemy.");
            Destroy(go);
            return;
        }

        enemy.Init(data, laneWaypoints);
        instance_activeEnemies.Add(enemy);
        enemiesAlive++;
        enemiesSpawned++;

        GameEvents.RaiseEnemySpawned(enemy);
    }

    // ── Отслеживание конца волны ──────────────────────────────────────────────

    private void OnEnemyRemoved(Enemy enemy)
    {
        instance_activeEnemies.Remove(enemy);
        enemiesAlive--;
        GameEvents.RaiseEnemiesRemainingChanged(Mathf.Max(0, totalEnemiesInWave - enemiesSpawned + enemiesAlive));
        CheckWaveEnd();
    }

    private void CheckWaveEnd()
    {
        if (!spawningComplete) return;
        if (enemiesAlive > 0) return;

        bool hasMore = currentWaveIndex + 1 < waves.Length;
        GameStateMachine.Instance.EndWave(hasMore);
    }
}
