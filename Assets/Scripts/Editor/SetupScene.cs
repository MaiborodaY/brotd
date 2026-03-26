using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.IO;

/// <summary>
/// Меню: BroTD → Setup Scene
/// Создаёт все ScriptableObject-ассеты, префабы и собирает сцену с нуля.
/// Запускать один раз после импорта скриптов.
/// </summary>
public static class SetupScene
{
    private const string AssetsRoot  = "Assets";
    private const string DataPath    = "Assets/Data";
    private const string PrefabsPath = "Assets/Prefabs";

    [MenuItem("BroTD/Setup Scene", priority = 0)]
    public static void Run()
    {
        EnsureFolders();

        // 1. ScriptableObjects
        var pathData     = CreatePathData();
        var meleeData    = CreateUnitData("Swordsman", UnitType.Melee,  80, 15f, 1.2f, 1f, 60);
        var archerData   = CreateUnitData("Archer",    UnitType.Ranged, 40, 8f,  3.5f, 0.8f, 50);
        var kingData     = CreateUnitData("King",      UnitType.Melee, 300, 20f, 1.5f, 1f,  0);
        var basicEnemy   = CreateEnemyData("BasicEnemy", 100, 2f,   10f, 1.2f, 1f, 10);
        var fastEnemy    = CreateEnemyData("FastEnemy",   60, 3.5f,  8f, 1.2f, 1f, 15);
        var bossEnemy    = CreateEnemyData("BossEnemy",  900, 0.8f, 40f, 1.5f, 2f, 150);
        var wave1        = CreateWaveData("Wave1", new[]{(basicEnemy,5)},              2f,  20);
        var wave2        = CreateWaveData("Wave2", new[]{(basicEnemy,6),(fastEnemy,2)}, 1.8f, 25);
        var wave3        = CreateWaveData("Wave3", new[]{(basicEnemy,5),(fastEnemy,5)}, 1.5f, 30);
        var wave7        = CreateWaveData("Wave7", new[]{(bossEnemy, 1)},              4f, 200);

        // 2. Префабы
        var cellPrefab        = CreateCellPrefab();
        var meleePrefab       = CreateUnitPrefab("MeleePrefab",  typeof(MeleeUnit),  Color.blue);
        var archerPrefab      = CreateUnitPrefab("ArcherPrefab", typeof(RangedUnit), Color.green);
        var kingPrefab        = CreateKingPrefab();
        var basicEnemyPrefab  = CreateEnemyPrefab("BasicEnemyPrefab", Color.red,          1f);
        var fastEnemyPrefab   = CreateEnemyPrefab("FastEnemyPrefab",  new Color(1f,0.5f,0f), 1f);
        var bossEnemyPrefab   = CreateEnemyPrefab("BossEnemyPrefab",  new Color(0.5f,0f,0.5f), 2f);

        // Присваиваем префабы в данные
        meleeData.prefab      = meleePrefab;
        archerData.prefab     = archerPrefab;
        basicEnemy.prefab     = basicEnemyPrefab;
        fastEnemy.prefab      = fastEnemyPrefab;
        bossEnemy.prefab      = bossEnemyPrefab;
        EditorUtility.SetDirty(bossEnemy);
        EditorUtility.SetDirty(meleeData);
        EditorUtility.SetDirty(archerData);
        EditorUtility.SetDirty(basicEnemy);
        EditorUtility.SetDirty(fastEnemy);

        // 3. Сцена
        BuildScene(pathData, cellPrefab, meleeData, archerData, kingData, kingPrefab,
                   new[] { wave1, wave2, wave3, wave7 });

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[BroTD] SetupScene завершён. Нажми Play!");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ScriptableObjects
    // ═══════════════════════════════════════════════════════════════════════════

    static PathData CreatePathData()
    {
        string path = $"{DataPath}/PathData.asset";
        var so = AssetDatabase.LoadAssetAtPath<PathData>(path)
                 ?? CreateSO<PathData>(path);

        so.waypoints = new Vector3[]
        {
            new Vector3(0f, 0.6f,  12f),
            new Vector3(0f, 0.6f,   0f),
            new Vector3(0f, 0.6f, -12f),
        };
        EditorUtility.SetDirty(so);
        return so;
    }

    static UnitData CreateUnitData(string displayName, UnitType type,
        int hp, float dmg, float range, float cooldown, int cost)
    {
        string path = $"{DataPath}/{displayName}Data.asset";
        var so = AssetDatabase.LoadAssetAtPath<UnitData>(path)
                 ?? CreateSO<UnitData>(path);

        so.displayName    = displayName;
        so.label          = type == UnitType.Melee ? "⚔" : "↑";
        so.unitType       = type;
        so.maxHealth      = hp;
        so.attackDamage   = dmg;
        so.attackRange    = range;
        so.attackCooldown = cooldown;
        so.goldCost       = cost;
        EditorUtility.SetDirty(so);
        return so;
    }

    static EnemyData CreateEnemyData(string displayName,
        int hp, float speed, float dmg, float range, float cooldown, int reward)
    {
        string path = $"{DataPath}/{displayName}Data.asset";
        var so = AssetDatabase.LoadAssetAtPath<EnemyData>(path)
                 ?? CreateSO<EnemyData>(path);

        so.displayName    = displayName;
        so.maxHealth      = hp;
        so.moveSpeed      = speed;
        so.attackDamage   = dmg;
        so.attackRange    = range;
        so.attackCooldown = cooldown;
        so.goldReward     = reward;
        EditorUtility.SetDirty(so);
        return so;
    }

    static WaveData CreateWaveData(string name,
        (EnemyData data, int count)[] entries, float interval, int bonus)
    {
        string path = $"{DataPath}/{name}.asset";
        var so = AssetDatabase.LoadAssetAtPath<WaveData>(path)
                 ?? CreateSO<WaveData>(path);

        so.spawnInterval  = interval;
        so.prepGoldBonus  = bonus;
        so.entries = new WaveData.SpawnEntry[entries.Length];
        for (int i = 0; i < entries.Length; i++)
            so.entries[i] = new WaveData.SpawnEntry { enemyData = entries[i].data, count = entries[i].count };
        EditorUtility.SetDirty(so);
        return so;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Префабы
    // ═══════════════════════════════════════════════════════════════════════════

    static GameObject CreateCellPrefab()
    {
        string path = $"{PrefabsPath}/GridCell.prefab";
        if (File.Exists(path)) return AssetDatabase.LoadAssetAtPath<GameObject>(path);

        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "GridCell";
        go.transform.localScale = new Vector3(1f, 0.05f, 1f);

        // Полупрозрачный материал
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = new Color(0.3f, 0.6f, 1f, 0.4f);
        mat.SetFloat("_Surface", 1); // Transparent
        go.GetComponent<Renderer>().sharedMaterial = mat;
        AssetDatabase.CreateAsset(mat, $"{DataPath}/CellMat.mat");

        // Collider — trigger для raycast
        var col = go.GetComponent<BoxCollider>();
        col.isTrigger = false;

        go.AddComponent<GridCell>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    static GameObject CreateUnitPrefab(string name, System.Type unitScript, Color color)
    {
        string path = $"{PrefabsPath}/{name}.prefab";
        if (File.Exists(path)) return AssetDatabase.LoadAssetAtPath<GameObject>(path);

        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = name;

        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = color;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        AssetDatabase.CreateAsset(mat, $"{DataPath}/{name}Mat.mat");

        go.AddComponent(unitScript);

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    static GameObject CreateKingPrefab()
    {
        string path = $"{PrefabsPath}/KingPrefab.prefab";
        if (File.Exists(path)) return AssetDatabase.LoadAssetAtPath<GameObject>(path);

        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = "KingPrefab";
        go.transform.localScale = Vector3.one * 1.6f; // Больше обычного юнита

        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = new Color(1f, 0.85f, 0f); // Золотой
        go.GetComponent<Renderer>().sharedMaterial = mat;
        AssetDatabase.CreateAsset(mat, $"{DataPath}/KingMat.mat");

        go.AddComponent<KingUnit>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    static GameObject CreateEnemyPrefab(string name, Color color, float scale = 1f)
    {
        string path = $"{PrefabsPath}/{name}.prefab";
        if (File.Exists(path)) return AssetDatabase.LoadAssetAtPath<GameObject>(path);

        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.localScale = Vector3.one * (0.8f * scale);

        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = color;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        AssetDatabase.CreateAsset(mat, $"{DataPath}/{name}Mat.mat");

        go.AddComponent<Enemy>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Сборка сцены
    // ═══════════════════════════════════════════════════════════════════════════

    static void BuildScene(PathData pathData, GameObject cellPrefab,
        UnitData meleeData, UnitData archerData, UnitData kingData, GameObject kingPrefab,
        WaveData[] waves)
    {
        // Сохраняем пользовательские параметры перед удалением объектов
        int savedGold  = ReadInt("[GameStateMachine]", typeof(GameStateMachine), "startingGold",  500);
        int savedLives = ReadInt("[GameStateMachine]", typeof(GameStateMachine), "startingLives", 0);
        var savedWaves = ReadObjectArray("[WaveManager]", typeof(WaveManager), "waves");

        // Сохраняем AudioClip-ы из AudioManager
        var savedSfxUnitPlace        = ReadObjectField("[AudioManager]", typeof(AudioManager), "sfxUnitPlace");
        var savedSfxUnitDeath        = ReadObjectField("[AudioManager]", typeof(AudioManager), "sfxUnitDeath");
        var savedSfxEnemyHit         = ReadObjectField("[AudioManager]", typeof(AudioManager), "sfxEnemyHit");
        var savedSfxEnemyDeath       = ReadObjectField("[AudioManager]", typeof(AudioManager), "sfxEnemyDeath");
        var savedSfxEnemyReachedBase = ReadObjectField("[AudioManager]", typeof(AudioManager), "sfxEnemyReachedBase");
        var savedSfxWaveStart        = ReadObjectField("[AudioManager]", typeof(AudioManager), "sfxWaveStart");
        var savedSfxVictory          = ReadObjectField("[AudioManager]", typeof(AudioManager), "sfxVictory");
        var savedSfxGameOver         = ReadObjectField("[AudioManager]", typeof(AudioManager), "sfxGameOver");
        var savedBgMusic             = ReadObjectField("[AudioManager]", typeof(AudioManager), "bgMusic");
        float savedMusicVolume       = ReadFloat("[AudioManager]", typeof(AudioManager), "musicVolume", 0.4f);
        float savedHitVolume         = ReadFloat("[AudioManager]", typeof(AudioManager), "hitVolume",   0.4f);
        float savedGlobalVolume      = ReadFloat("[AudioManager]", typeof(AudioManager), "globalVolume", 1f);

        // Чистим старые объекты
        foreach (var name in new[]
        {
            "GameManager", "EnemySpawner", "EnemyPrototype",
            "MVP_Map", "Canvas", "EventSystem",
            "[AudioManager]", "[GameStateMachine]", "[GridSystem]", "[WaveManager]", "[PlacementSystem]"
        })
        {
            var old = GameObject.Find(name);
            if (old != null) Object.DestroyImmediate(old);
        }

        // ── Camera ────────────────────────────────────────────────────────────
        var cam = Camera.main?.gameObject ?? new GameObject("Main Camera");
        cam.tag = "MainCamera";
        var camComp = cam.GetOrAddComponent<Camera>();
        camComp.orthographic = false;
        camComp.fieldOfView  = 60f;
        cam.transform.position = new Vector3(0f, 10f, -8f); // ближе к карте
        cam.transform.rotation = Quaternion.Euler(70f, 0f, 0f);

        var camCtrl = cam.GetOrAddComponent<CameraController>();
        SetSerializedField(camCtrl, "minZ",             -14f);
        SetSerializedField(camCtrl, "maxZ",              14f);
        SetSerializedField(camCtrl, "dragSensitivity",   0.03f);
        SetSerializedField(camCtrl, "smoothSpeed",       8f);

        // ── Map ───────────────────────────────────────────────────────────────
        var map = new GameObject("MVP_Map");

        // Ground
        var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground";
        ground.transform.SetParent(map.transform);
        ground.transform.position   = Vector3.zero;
        ground.transform.localScale = new Vector3(12f, 0.1f, 30f);
        // Ищем существующий материал земли по всем возможным путям
        var existingGroundMat = FindGroundMaterial();
        if (existingGroundMat != null)
        {
            ground.GetComponent<Renderer>().sharedMaterial = existingGroundMat;
        }
        else
        {
            var groundMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            groundMat.color = new Color(0.25f, 0.25f, 0.25f);
            ground.GetComponent<Renderer>().sharedMaterial = groundMat;
            AssetDatabase.CreateAsset(groundMat, $"{DataPath}/GroundMat.mat");
        }

        // KingSpawner — спавнит Короля на позиции базы
        var kingSpawnerGo = new GameObject("KingSpawner");
        kingSpawnerGo.transform.SetParent(map.transform);
        kingSpawnerGo.transform.position = new Vector3(0f, 0.6f, -13f);
        var spawner = kingSpawnerGo.AddComponent<KingSpawner>();
        SetSerializedField(spawner, "kingData",   kingData);
        SetSerializedField(spawner, "kingPrefab", kingPrefab);

        // Path lines — по одной на каждую дорожку (3 штуки, смещены по X)
        float laneSpacing = 2f;
        int   laneCount   = 3;
        for (int lane = 0; lane < laneCount; lane++)
        {
            float xOffset = (lane - (laneCount - 1) / 2f) * laneSpacing;
            var lineGo = new GameObject($"RouteLine_{lane}");
            lineGo.transform.SetParent(map.transform);
            var lr = lineGo.AddComponent<LineRenderer>();
            lr.positionCount = pathData.waypoints.Length;
            var offsetPoints = new Vector3[pathData.waypoints.Length];
            for (int k = 0; k < pathData.waypoints.Length; k++)
                offsetPoints[k] = pathData.waypoints[k] + new Vector3(xOffset, 0, 0);
            lr.SetPositions(offsetPoints);
            lr.startWidth    = 0.12f;
            lr.endWidth      = 0.12f;
            lr.useWorldSpace = true;
            lr.enabled       = false; // невидимы в игре
        }

        // ── Core Systems ──────────────────────────────────────────────────────

        // AudioManager
        var amGo = new GameObject("[AudioManager]");
        var am = amGo.AddComponent<AudioManager>();
        if (savedSfxUnitPlace        != null) SetSerializedField(am, "sfxUnitPlace",        savedSfxUnitPlace);
        if (savedSfxUnitDeath        != null) SetSerializedField(am, "sfxUnitDeath",        savedSfxUnitDeath);
        if (savedSfxEnemyHit         != null) SetSerializedField(am, "sfxEnemyHit",         savedSfxEnemyHit);
        if (savedSfxEnemyDeath       != null) SetSerializedField(am, "sfxEnemyDeath",       savedSfxEnemyDeath);
        if (savedSfxEnemyReachedBase != null) SetSerializedField(am, "sfxEnemyReachedBase", savedSfxEnemyReachedBase);
        if (savedSfxWaveStart        != null) SetSerializedField(am, "sfxWaveStart",        savedSfxWaveStart);
        if (savedSfxVictory          != null) SetSerializedField(am, "sfxVictory",          savedSfxVictory);
        if (savedSfxGameOver         != null) SetSerializedField(am, "sfxGameOver",         savedSfxGameOver);
        if (savedBgMusic             != null) SetSerializedField(am, "bgMusic",             savedBgMusic);
        SetSerializedField(am, "musicVolume",  savedMusicVolume);
        SetSerializedField(am, "hitVolume",    savedHitVolume);
        SetSerializedField(am, "globalVolume", savedGlobalVolume);

        // GameStateMachine
        var gsmGo = new GameObject("[GameStateMachine]");
        var gsm = gsmGo.AddComponent<GameStateMachine>();
        SetSerializedField(gsm, "startingLives", savedLives);
        SetSerializedField(gsm, "startingGold",  savedGold);

        // Spawn point (начало пути)
        var spawnGo = new GameObject("SpawnPoint");
        spawnGo.transform.SetParent(map.transform);
        spawnGo.transform.position = pathData.waypoints[0];

        // GridSystem
        var gridGo = new GameObject("[GridSystem]");
        var grid = gridGo.AddComponent<GridSystem>();
        SetSerializedField(grid, "pathData",    pathData);
        SetSerializedField(grid, "cellPrefab", cellPrefab);
        SetSerializedField(grid, "cellSpacing",  1.5f);
        SetSerializedField(grid, "cellSize",     1.0f);
        SetSerializedField(grid, "cellY",        0.61f);
        SetSerializedField(grid, "laneCount",    3);
        SetSerializedField(grid, "laneSpacing",  2f);

        // WaveManager
        var wmGo = new GameObject("[WaveManager]");
        var wm = wmGo.AddComponent<WaveManager>();
        SetSerializedField(wm, "pathData",    pathData);
        SetSerializedField(wm, "spawnPoint", spawnGo.transform);
        SetSerializedField(wm, "laneCount",  3);
        SetSerializedField(wm, "laneSpacing", 2f);
        // Восстанавливаем волны если они были настроены вручную
        WaveData[] wavesToAssign = waves;
        if (savedWaves != null && savedWaves.Length > 0)
        {
            wavesToAssign = new WaveData[savedWaves.Length];
            for (int i = 0; i < savedWaves.Length; i++)
                wavesToAssign[i] = savedWaves[i] as WaveData;
        }
        SetSerializedField(wm, "waves", wavesToAssign);

        // PlacementSystem
        var psGo = new GameObject("[PlacementSystem]");
        var ps = psGo.AddComponent<PlacementSystem>();
        SetSerializedField(ps, "mainCamera", camComp);
        // -1 = Everything; GridCell проверяется по компоненту после raycast
        SetSerializedField(ps, "cellLayer", (LayerMask)(-1));

        // ── Canvas UI ─────────────────────────────────────────────────────────
        BuildCanvas(meleeData, archerData);

        // ── Marks dirty ──────────────────────────────────────────────────────
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Canvas UI
    // ═══════════════════════════════════════════════════════════════════════════

    static void BuildCanvas(UnitData meleeData, UnitData archerData)
    {
        // Canvas
        var canvasGo = new GameObject("Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode          = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution  = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight   = 0.5f;   // баланс ширина/высота
        canvasGo.AddComponent<GraphicRaycaster>();

        // EventSystem — используем новый Input System UI module
        if (GameObject.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        var hud = canvasGo.AddComponent<GameHUD>();

        // ── Top Bar ───────────────────────────────────────────────────────────
        var topBar = MakePanel(canvasGo.transform, "TopBar",
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0, -100), new Vector2(0, 0), 100);
        topBar.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.05f, 0.92f);

        // HorizontalLayoutGroup — 4 ячейки, равномерно
        var layout = topBar.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment         = TextAnchor.MiddleCenter;
        layout.spacing                = 0;
        layout.padding                = new RectOffset(8, 8, 8, 8);
        layout.childForceExpandWidth  = true;
        layout.childForceExpandHeight = true;
        layout.childControlWidth      = true;
        layout.childControlHeight     = true;

        var goldText    = MakeStatLabel(topBar.transform, "GoldText",    "coin",  "150",  new Color(1f, 0.85f, 0.1f));
        var livesText   = MakeStatLabel(topBar.transform, "LivesText",   "heart", "10",   new Color(1f, 0.3f, 0.3f));
        var waveText    = MakeStatLabel(topBar.transform, "WaveText",    "wave",  "1",    new Color(0.4f, 0.8f, 1f));
        var enemiesText = MakeStatLabel(topBar.transform, "EnemiesText", "enemy", "0",    new Color(0.9f, 0.9f, 0.9f));

        SetSerializedField(hud, "goldText",    goldText);
        SetSerializedField(hud, "livesText",   livesText);
        SetSerializedField(hud, "waveText",    waveText);
        SetSerializedField(hud, "enemiesText", enemiesText);

        // ── Bottom Bar ────────────────────────────────────────────────────────
        var bottomBar = MakePanel(canvasGo.transform, "PrepPanel",
            new Vector2(0, 0), new Vector2(1, 0),
            new Vector2(0, 0), new Vector2(0, 220), 220);
        bottomBar.GetComponent<Image>().color = new Color(0, 0, 0, 0.7f);

        SetSerializedField(hud, "prepPanel", bottomBar);

        // Кнопки юнитов
        var meleeBtn  = MakeUnitButton(bottomBar.transform, "MeleeBtn",  meleeData,
            new Vector2(0.18f, 0.5f));
        var archerBtn = MakeUnitButton(bottomBar.transform, "ArcherBtn", archerData,
            new Vector2(0.42f, 0.5f));

        var unitButtons = new[] { meleeBtn, archerBtn };
        SetSerializedField(hud, "unitButtons", unitButtons);

        // Кнопка Start Wave
        var startBtn = MakeButton(bottomBar.transform, "StartWaveBtn", "Start Wave",
            new Vector2(0.76f, 0.5f), new Vector2(280, 110));
        SetSerializedField(hud, "startWaveButton", startBtn);

        // ── Game Over Panel ───────────────────────────────────────────────────
        var gameOverPanel = MakeOverlayPanel(canvasGo.transform, "GameOverPanel",
            "GAME OVER", new Color(0.7f, 0f, 0f, 0.85f));
        var goRestartBtn = MakeButton(gameOverPanel.transform, "RestartBtn", "Restart",
            new Vector2(0.5f, 0.35f), new Vector2(250, 80));
        goRestartBtn.onClick.AddListener(() => hud.OnRestartPressed());
        SetSerializedField(hud, "gameOverPanel", gameOverPanel);
        gameOverPanel.SetActive(false);

        // ── Victory Panel ─────────────────────────────────────────────────────
        var victoryPanel = MakeOverlayPanel(canvasGo.transform, "VictoryPanel",
            "VICTORY!", new Color(0f, 0.5f, 0f, 0.85f));
        var vRestartBtn = MakeButton(victoryPanel.transform, "RestartBtn", "Play Again",
            new Vector2(0.5f, 0.35f), new Vector2(250, 80));
        vRestartBtn.onClick.AddListener(() => hud.OnRestartPressed());
        SetSerializedField(hud, "victoryPanel", victoryPanel);
        victoryPanel.SetActive(false);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // UI helpers
    // ═══════════════════════════════════════════════════════════════════════════

    static GameObject MakePanel(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax, float height)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        rt.sizeDelta = new Vector2(0, height);
        return go;
    }

    /// <summary>Ячейка шапки: маленький лейбл сверху + крупное значение снизу.</summary>
    static TextMeshProUGUI MakeStatLabel(Transform parent, string name,
        string label, string value, Color valueColor)
    {
        var cell = new GameObject(name);
        cell.transform.SetParent(parent, false);
        var img = cell.AddComponent<Image>();
        img.color = new Color(1, 1, 1, 0f); // прозрачный фон

        // Вертикальный layout внутри ячейки
        var vl = cell.AddComponent<VerticalLayoutGroup>();
        vl.childAlignment        = TextAnchor.MiddleCenter;
        vl.childForceExpandWidth = true;
        vl.childForceExpandHeight= true;
        vl.childControlWidth     = true;
        vl.childControlHeight    = true;
        vl.spacing               = -4;

        // Маленький лейбл (GOLD, LIVES и т.д.)
        var labelGo  = new GameObject("Label");
        labelGo.transform.SetParent(cell.transform, false);
        var labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
        labelTmp.text        = label.ToUpper();
        labelTmp.fontSize    = 18;
        labelTmp.fontStyle   = FontStyles.Bold;
        labelTmp.color       = new Color(0.7f, 0.7f, 0.7f);
        labelTmp.alignment   = TextAlignmentOptions.Center;
        var labelFit         = labelGo.AddComponent<ContentSizeFitter>();
        labelFit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Крупное значение
        var valueGo  = new GameObject("Value");
        valueGo.transform.SetParent(cell.transform, false);
        var valueTmp = valueGo.AddComponent<TextMeshProUGUI>();
        valueTmp.text             = value;
        valueTmp.enableAutoSizing = true;
        valueTmp.fontSizeMin      = 20;
        valueTmp.fontSizeMax      = 44;
        valueTmp.fontStyle        = FontStyles.Bold;
        valueTmp.color            = valueColor;
        valueTmp.alignment        = TextAlignmentOptions.Center;
        var valueFit              = valueGo.AddComponent<ContentSizeFitter>();
        valueFit.verticalFit      = ContentSizeFitter.FitMode.PreferredSize;

        return valueTmp;
    }

    static GameObject MakeLabel(Transform parent, string name, string text,
        int fontSize, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text                    = text;
        tmp.enableAutoSizing        = true;
        tmp.fontSizeMin             = 18;
        tmp.fontSizeMax             = fontSize;
        tmp.fontStyle               = FontStyles.Bold;
        tmp.color                   = Color.white;
        tmp.alignment               = TextAlignmentOptions.MidlineLeft;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin                = anchorMin;
        rt.anchorMax                = anchorMax;
        rt.anchoredPosition         = anchoredPos;
        rt.sizeDelta                = size;
        return go;
    }

    static Button MakeButton(Transform parent, string name, string label,
        Vector2 anchor, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.5f, 0.9f);
        var btn = go.AddComponent<Button>();
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta = size;

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(go.transform, false);
        var tmp = labelGo.AddComponent<TextMeshProUGUI>();
        tmp.text             = label;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin      = 18;
        tmp.fontSizeMax      = 52;
        tmp.fontStyle        = FontStyles.Bold;
        tmp.color            = Color.white;
        tmp.alignment        = TextAlignmentOptions.Center;
        var lrt = labelGo.GetComponent<RectTransform>();
        lrt.anchorMin        = new Vector2(0.05f, 0.05f);
        lrt.anchorMax        = new Vector2(0.95f, 0.95f);
        lrt.offsetMin        = lrt.offsetMax = Vector2.zero;

        return btn;
    }

    static UnitButton MakeUnitButton(Transform parent, string name,
        UnitData data, Vector2 anchor)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.15f, 0.15f, 0.15f);
        var btn = go.AddComponent<Button>();
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta = new Vector2(220, 190);

        // Имя
        var nameGo = new GameObject("Name");
        nameGo.transform.SetParent(go.transform, false);
        var nameTmp = nameGo.AddComponent<TextMeshProUGUI>();
        nameTmp.text             = data.displayName;
        nameTmp.enableAutoSizing = true;
        nameTmp.fontSizeMin      = 16;
        nameTmp.fontSizeMax      = 40;
        nameTmp.fontStyle        = FontStyles.Bold;
        nameTmp.color            = Color.white;
        nameTmp.alignment        = TextAlignmentOptions.Center;
        var nrt = nameGo.GetComponent<RectTransform>();
        nrt.anchorMin = new Vector2(0.05f, 0.55f);
        nrt.anchorMax = new Vector2(0.95f, 0.95f);
        nrt.offsetMin = nrt.offsetMax = Vector2.zero;

        // Цена
        var costGo = new GameObject("Cost");
        costGo.transform.SetParent(go.transform, false);
        var costTmp = costGo.AddComponent<TextMeshProUGUI>();
        costTmp.text             = $"{data.goldCost}g";
        costTmp.enableAutoSizing = true;
        costTmp.fontSizeMin      = 16;
        costTmp.fontSizeMax      = 40;
        costTmp.fontStyle        = FontStyles.Bold;
        costTmp.color            = Color.yellow;
        costTmp.alignment        = TextAlignmentOptions.Center;
        var crt = costGo.GetComponent<RectTransform>();
        crt.anchorMin = Vector2.zero;
        crt.anchorMax = new Vector2(1, 0.35f);
        crt.offsetMin = crt.offsetMax = Vector2.zero;

        var ub = go.AddComponent<UnitButton>();
        SetSerializedField(ub, "unitData", data);
        SetSerializedField(ub, "button",   btn);
        SetSerializedField(ub, "nameText", nameTmp);
        SetSerializedField(ub, "costText", costTmp);

        return ub;
    }

    static GameObject MakeOverlayPanel(Transform parent, string name,
        string title, Color bgColor)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = bgColor;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(go.transform, false);
        var tmp = titleGo.AddComponent<TextMeshProUGUI>();
        tmp.text      = title;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin      = 40;
        tmp.fontSizeMax      = 100;
        tmp.fontStyle        = FontStyles.Bold;
        tmp.color     = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        var trt = titleGo.GetComponent<RectTransform>();
        trt.anchorMin     = new Vector2(0, 0.55f);
        trt.anchorMax     = new Vector2(1, 0.75f);
        trt.offsetMin = trt.offsetMax = Vector2.zero;

        return go;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Утилиты
    // ═══════════════════════════════════════════════════════════════════════════

    static void EnsureFolders()
    {
        foreach (var folder in new[] { DataPath, PrefabsPath, "Assets/Scripts/Editor" })
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                var parts = folder.Split('/');
                string current = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    string next = current + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(next))
                        AssetDatabase.CreateFolder(current, parts[i]);
                    current = next;
                }
            }
        }
    }

    static T CreateSO<T>(string path) where T : ScriptableObject
    {
        var so = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(so, path);
        return so;
    }

    /// Устанавливает serialized field по имени через SerializedObject.
    /// Ищет материал земли по всем стандартным путям.
    static Material FindGroundMaterial()
    {
        string[] candidates = {
            $"{DataPath}/GroundMat.mat",
            "Assets/Materials/GroundMaterial.mat",
            "Assets/Materials/GroundMat.mat",
            $"{DataPath}/GroundMaterial.mat",
        };
        foreach (var path in candidates)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;
        }
        // Последний вариант — поиск по имени во всём проекте
        var guids = AssetDatabase.FindAssets("Ground t:Material");
        foreach (var guid in guids)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if (mat != null) return mat;
        }
        return null;
    }

    /// Читает массив Object-ссылок из существующего компонента на сцене.
    static Object[] ReadObjectArray(string goName, System.Type componentType, string fieldName)
    {
        var go = GameObject.Find(goName);
        if (go == null) return null;
        var comp = go.GetComponent(componentType);
        if (comp == null) return null;
        var so   = new SerializedObject(comp);
        var prop = so.FindProperty(fieldName);
        if (prop == null || !prop.isArray) return null;
        var result = new Object[prop.arraySize];
        for (int i = 0; i < prop.arraySize; i++)
            result[i] = prop.GetArrayElementAtIndex(i).objectReferenceValue;
        return result;
    }

    /// Читает int-поле из существующего компонента на сцене. Возвращает defaultValue если объект не найден.
    static int ReadInt(string goName, System.Type componentType, string fieldName, int defaultValue)
    {
        var go = GameObject.Find(goName);
        if (go == null) return defaultValue;
        var comp = go.GetComponent(componentType);
        if (comp == null) return defaultValue;
        var so   = new SerializedObject(comp);
        var prop = so.FindProperty(fieldName);
        return prop != null ? prop.intValue : defaultValue;
    }

    /// Читает float-поле из существующего компонента на сцене.
    static float ReadFloat(string goName, System.Type componentType, string fieldName, float defaultValue)
    {
        var go = GameObject.Find(goName);
        if (go == null) return defaultValue;
        var comp = go.GetComponent(componentType);
        if (comp == null) return defaultValue;
        var so   = new SerializedObject(comp);
        var prop = so.FindProperty(fieldName);
        return prop != null ? prop.floatValue : defaultValue;
    }

    /// Читает одну Object-ссылку из существующего компонента на сцене.
    static Object ReadObjectField(string goName, System.Type componentType, string fieldName)
    {
        var go = GameObject.Find(goName);
        if (go == null) return null;
        var comp = go.GetComponent(componentType);
        if (comp == null) return null;
        var so   = new SerializedObject(comp);
        var prop = so.FindProperty(fieldName);
        return prop != null ? prop.objectReferenceValue : null;
    }

    static void SetSerializedField(Object target, string fieldName, object value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(fieldName);
        if (prop == null)
        {
            Debug.LogWarning($"SetupScene: поле '{fieldName}' не найдено в {target.GetType().Name}");
            return;
        }
        AssignSerializedProperty(prop, value);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Instance);
        field?.SetValue(target, value);
    }

    static void AssignSerializedProperty(SerializedProperty prop, object value)
    {
        switch (prop.propertyType)
        {
            case SerializedPropertyType.ObjectReference:
                prop.objectReferenceValue = value as Object; break;
            case SerializedPropertyType.Integer:
                prop.intValue = (int)value; break;
            case SerializedPropertyType.Float:
                prop.floatValue = (float)value; break;
            case SerializedPropertyType.Boolean:
                prop.boolValue = (bool)value; break;
            case SerializedPropertyType.LayerMask:
                prop.intValue = (int)(LayerMask)value; break;
            case SerializedPropertyType.ArraySize:
                prop.arraySize = (int)value; break;
            case SerializedPropertyType.Generic when value is WaveData[] waves:
                prop.arraySize = waves.Length;
                for (int i = 0; i < waves.Length; i++)
                    prop.GetArrayElementAtIndex(i).objectReferenceValue = waves[i];
                break;
            case SerializedPropertyType.Generic when value is UnitButton[] btns:
                prop.arraySize = btns.Length;
                for (int i = 0; i < btns.Length; i++)
                    prop.GetArrayElementAtIndex(i).objectReferenceValue = btns[i];
                break;
            default:
                Debug.LogWarning($"AssignSerializedProperty: неизвестный тип {prop.propertyType} для '{prop.name}'");
                break;
        }
    }
}

static class ComponentExtensions
{
    public static T GetOrAddComponent<T>(this GameObject go) where T : Component
        => go.GetComponent<T>() ?? go.AddComponent<T>();
}
