using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

/// <summary>
/// Управляет PvP матчем: синхронизация готовности, обмен юнитами, результаты раундов.
/// </summary>
public class PvPGameManager : MonoBehaviourPunCallbacks
{
    public static PvPGameManager Instance { get; private set; }

    // Photon event codes
    private const byte EVENT_READY       = 1;
    private const byte EVENT_SEND_UNITS  = 2;
    private const byte EVENT_ROUND_RESULT = 3;

    [Header("UI")]
    [SerializeField] private Button          readyButton;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI roundText;
    [SerializeField] private TextMeshProUGUI myKingHpText;
    [SerializeField] private TextMeshProUGUI opponentKingHpText;
    [SerializeField] private GameObject      waitingOverlay;    // "Waiting for opponent..."
    [SerializeField] private GameObject      gameOverPanel;
    [SerializeField] private TextMeshProUGUI gameOverText;

    [Header("References")]
    [SerializeField] private PathData        pathData;
    private KingUnit kingUnit;

    [Header("Unit Data — должны совпадать у обоих игроков")]
    [SerializeField] private UnitData[]      allUnitDatas;  // [0]=Swordsman, [1]=Archer

    [Header("Economy")]
    [SerializeField] private int goldPerRound = 60;   // золото за выживание раунда
    [SerializeField] private int goldPerKill  = 100;  // золото за убийство вражеского юнита

    [Header("Debug (только для теста в редакторе)")]
    [SerializeField] private bool showDebugButton = true;

    // ── state ─────────────────────────────────────────────────────────────────

    private int     round           = 1;
    private bool    localReady      = false;
    private bool    opponentReady   = false;
    private int     enemiesAlive    = 0;
    private bool    matchOver       = false;

    private readonly List<PvPUnitEnemy> activeEnemies = new();

    // ── lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        // Очищаем статический список — иначе NullReference при переходе на другую сцену
        PvPUnitEnemy.ActiveList.Clear();
    }

    private void Start()
    {
        readyButton.onClick.AddListener(OnReadyPressed);
        waitingOverlay.SetActive(false);
        gameOverPanel.SetActive(false);

        // Показываем клетки в начальной фазе подготовки
        GameEvents.RaisePrepPhaseStarted();

        if (showDebugButton)
            CreateDebugButton();

        // King спавнится через KingSpawner — ищем с задержкой
        StartCoroutine(FindKingDelayed());
    }

    private System.Collections.IEnumerator FindKingDelayed()
    {
        yield return new WaitForSeconds(0.5f);
        kingUnit = FindAnyObjectByType<KingUnit>();
        if (kingUnit == null)
            Debug.LogWarning("[PvPGameManager] KingUnit не найден на сцене!");
        UpdateUI();

        // Показываем HP противника как его начальное значение (узнаем после первого раунда)
        // До этого показываем прочерк
        if (opponentKingHpText != null)
            opponentKingHpText.text = "Enemy King: ---";
    }

    public override void OnEnable()
    {
        base.OnEnable();
        PhotonNetwork.NetworkingClient.EventReceived += OnPhotonEvent;
        PvPUnitEnemy.OnDied       += OnPvPEnemyDied;
        PvPUnitEnemy.OnReachedEnd += OnPvPEnemyReachedBase;
    }

    public override void OnDisable()
    {
        base.OnDisable();
        PhotonNetwork.NetworkingClient.EventReceived -= OnPhotonEvent;
        PvPUnitEnemy.OnDied       -= OnPvPEnemyDied;
        PvPUnitEnemy.OnReachedEnd -= OnPvPEnemyReachedBase;
    }

    // ── Ready ─────────────────────────────────────────────────────────────────

    private void OnReadyPressed()
    {
        if (localReady || matchOver) return;

        localReady = true;
        readyButton.interactable = false;
        SetStatus("Waiting for opponent...");

        // Отправляем Ready событие
        PhotonNetwork.RaiseEvent(EVENT_READY, null,
            new RaiseEventOptions { Receivers = ReceiverGroup.Others },
            SendOptions.SendReliable);

        CheckBothReady();
    }

    private void CheckBothReady()
    {
        if (!localReady || !opponentReady) return;

        localReady    = false;
        opponentReady = false;

        StartCoroutine(StartRound());
    }

    // ── Round ─────────────────────────────────────────────────────────────────

    private IEnumerator StartRound()
    {
        SetStatus($"Round {round} — sending units!");
        yield return new WaitForSeconds(0.5f);

        // Собираем размещённых юнитов
        var unitInfos = CollectPlacedUnits();

        // Отправляем противнику
        PhotonNetwork.RaiseEvent(EVENT_SEND_UNITS, unitInfos,
            new RaiseEventOptions { Receivers = ReceiverGroup.Others },
            SendOptions.SendReliable);

        SetStatus("Defending...");
        readyButton.gameObject.SetActive(false);
        waitingOverlay.SetActive(false);

        // Скрываем клетки сетки во время волны
        GameEvents.RaiseWavePhaseStarted();
    }

    // ── Спавн юнитов противника ───────────────────────────────────────────────

    private void SpawnOpponentUnits(int[] unitIndices)
    {
        activeEnemies.Clear();
        enemiesAlive = 0;

        if (pathData == null || pathData.waypoints == null || pathData.waypoints.Length == 0)
        {
            Debug.LogWarning("[PvPGameManager] PathData не назначен!");
            return;
        }

        int laneCount = 3;
        for (int i = 0; i < unitIndices.Length; i++)
        {
            int idx = unitIndices[i];
            if (idx < 0 || idx >= allUnitDatas.Length) continue;

            SpawnPvPEnemy(allUnitDatas[idx], i % laneCount);
            enemiesAlive++;
        }
    }

    private void SpawnPvPEnemy(UnitData data, int lane)
    {
        var go = new GameObject($"PvPEnemy_{data.displayName}");
        go.AddComponent<CapsuleCollider>();

        var enemy = go.AddComponent<PvPUnitEnemy>();

        // Получаем путь для дорожки
        Vector3[] path = GetLanePath(lane);
        if (path == null || path.Length == 0) return;

        go.transform.position = path[0];
        enemy.Init(data, path);
        activeEnemies.Add(enemy);
        PvPUnitEnemy.ActiveList.Add(enemy);
    }

    private Vector3[] GetLanePath(int lane)
    {
        // Смещение дорожек — как в основной игре
        float[] offsets = { -2.5f, 0f, 2.5f };
        float xOffset   = lane < offsets.Length ? offsets[lane] : 0f;

        var path = new Vector3[pathData.waypoints.Length];
        for (int i = 0; i < path.Length; i++)
            path[i] = pathData.waypoints[i] + new Vector3(xOffset, 0f, 0f);

        return path;
    }

    // ── Результаты ────────────────────────────────────────────────────────────

    public void OnPvPEnemyDied(PvPUnitEnemy enemy)
    {
        activeEnemies.Remove(enemy);
        enemiesAlive = Mathf.Max(0, enemiesAlive - 1);
        GameStateMachine.Instance?.EarnGold(goldPerKill);
        CheckRoundEnd();
    }

    public void OnPvPEnemyReachedBase(PvPUnitEnemy enemy)
    {
        activeEnemies.Remove(enemy);
        enemiesAlive = Mathf.Max(0, enemiesAlive - 1);

        // Враг дошёл — атакует короля
        if (kingUnit != null && kingUnit.IsAlive)
            kingUnit.TakeDamage(20f);

        // Если Король умер — сразу показываем поражение, не ждём конца волны
        if (kingUnit != null && !kingUnit.IsAlive)
        {
            ShowGameOver(false);
            return;
        }

        CheckRoundEnd();
    }

    private void CheckRoundEnd()
    {
        if (enemiesAlive > 0) return;

        // Раунд окончен — отправляем HP короля противнику (только если подключены)
        int myKingHp = kingUnit != null ? kingUnit.CurrentHp : 0;

        if (PhotonNetwork.IsConnected)
            PhotonNetwork.RaiseEvent(EVENT_ROUND_RESULT, myKingHp,
                new RaiseEventOptions { Receivers = ReceiverGroup.Others },
                SendOptions.SendReliable);

        if (myKingHp <= 0)
        {
            ShowGameOver(false);
            return;
        }

        StartCoroutine(EndRoundSequence());
    }

    private IEnumerator EndRoundSequence()
    {
        SetStatus("Round over!");
        yield return new WaitForSeconds(1.5f);

        // Матч мог завершиться пока ждали (победа пришла по Photon)
        if (matchOver) yield break;

        // Respawn и восстановление HP юнитов — как в одиночной игре
        GameEvents.RaiseWavePhaseEnded();

        // Золото за выживание раунда
        GameStateMachine.Instance?.EarnGold(goldPerRound);

        round++;
        UpdateUI();
        SetStatus("Build for next round.");
        readyButton.gameObject.SetActive(true);
        readyButton.interactable = true;

        // Показываем клетки сетки в фазе подготовки
        GameEvents.RaisePrepPhaseStarted();
    }

    // ── Photon Events ─────────────────────────────────────────────────────────

    private void OnPhotonEvent(EventData eventData)
    {
        switch (eventData.Code)
        {
            case EVENT_READY:
                opponentReady = true;
                CheckBothReady();
                break;

            case EVENT_SEND_UNITS:
                var unitIndices = (int[])eventData.CustomData;
                SpawnOpponentUnits(unitIndices);
                break;

            case EVENT_ROUND_RESULT:
                int opponentKingHp = (int)eventData.CustomData;
                UpdateOpponentKingHp(opponentKingHp);
                if (opponentKingHp <= 0)
                    ShowGameOver(true);
                break;
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        ShowGameOver(true); // противник вышел — победа
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Собирает индексы размещённых юнитов для передачи противнику.</summary>
    private int[] CollectPlacedUnits()
    {
        var result = new List<int>();
        var allUnits = FindObjectsByType<Unit>(FindObjectsInactive.Exclude);

        foreach (var unit in allUnits)
        {
            if (unit is KingUnit) continue;

            for (int i = 0; i < allUnitDatas.Length; i++)
            {
                if (unit.Data == allUnitDatas[i])
                {
                    result.Add(i);
                    break;
                }
            }
        }

        return result.ToArray();
    }

    private void UpdateOpponentKingHp(int hp)
    {
        if (opponentKingHpText != null)
            opponentKingHpText.text = $"Enemy King: {hp}";
    }

    private void UpdateUI()
    {
        if (roundText != null) roundText.text = $"Round {round}";
        if (myKingHpText != null && kingUnit != null)
            myKingHpText.text = $"Your King: {kingUnit.CurrentHp}";
    }

    private void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
    }

    private void ShowGameOver(bool won)
    {
        matchOver = true;
        gameOverPanel.SetActive(true);
        if (gameOverText != null)
            gameOverText.text = won ? "Victory!\nOpponent's King is dead." : "Defeat!\nYour King has fallen.";
    }

    // ── Debug ─────────────────────────────────────────────────────────────────

    private void CreateDebugButton()
    {
        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        var go = new GameObject("DebugTestWaveBtn");
        go.transform.SetParent(canvas.transform, false);

        var rt             = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0f, 0f);
        rt.anchorMax        = new Vector2(0f, 0f);
        rt.pivot            = new Vector2(0f, 0f);
        rt.anchoredPosition = new Vector2(12f, 12f);
        rt.sizeDelta        = new Vector2(160f, 50f);

        var img   = go.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(0.8f, 0.5f, 0f, 0.9f);

        var btn = go.AddComponent<UnityEngine.UI.Button>();
        btn.onClick.AddListener(DebugSpawnTestWave);

        var textGo = new GameObject("Label");
        textGo.transform.SetParent(go.transform, false);
        var textRt       = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
        var tmp          = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text         = "TEST WAVE";
        tmp.fontSize     = 18f;
        tmp.fontStyle    = FontStyles.Bold;
        tmp.color        = Color.white;
        tmp.alignment    = TextAlignmentOptions.Center;
    }

    /// <summary>Симулирует получение юнитов от противника — 3 мечника по дорожкам.</summary>
    private void DebugSpawnTestWave()
    {
        if (allUnitDatas == null || allUnitDatas.Length == 0)
        {
            Debug.LogWarning("[PvPGameManager] allUnitDatas не назначен!");
            return;
        }

        // Спавним 3 мечника (индекс 0) по трём дорожкам
        int[] testUnits = { 0, 0, 0 };
        SpawnOpponentUnits(testUnits);

        GameEvents.RaiseWavePhaseStarted();
        readyButton.gameObject.SetActive(false);
        SetStatus("DEBUG: Test wave started!");
    }

}
