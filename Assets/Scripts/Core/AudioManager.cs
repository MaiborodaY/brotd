using UnityEngine;

/// <summary>
/// Воспроизводит звуки в ответ на GameEvents.
/// Назначь AudioClip-ы в инспекторе. Незаполненные слоты просто молчат.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Unit")]
    [SerializeField] private AudioClip sfxUnitPlace;
    [SerializeField] private AudioClip sfxUnitDeath;
    [SerializeField] private AudioClip sfxMeleeAttack;   // звук удара мечника
    [SerializeField] private AudioClip sfxRangedAttack;  // звук выстрела лучника

    [Header("Enemy")]
    [SerializeField] private AudioClip sfxEnemyHit;
    [SerializeField] private AudioClip sfxEnemyDeath;
    [SerializeField] private AudioClip sfxEnemyReachedBase;

    [Header("Game")]
    [SerializeField] private AudioClip sfxWaveStart;
    [SerializeField] private AudioClip sfxVictory;
    [SerializeField] private AudioClip sfxGameOver;

    [Header("Music")]
    [SerializeField] private AudioClip bgMusic;
    [SerializeField] [Range(0f, 1f)] private float musicVolume = 0.4f;

    [Header("Volume")]
    [SerializeField] [Range(0f, 1f)] private float hitVolume    = 0.4f;
    [SerializeField] [Range(0f, 1f)] private float globalVolume = 1f;

    private AudioSource source;
    private AudioSource musicSource;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake  = false;
        source.spatialBlend = 0f;

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.spatialBlend = 0f;
        musicSource.loop         = true;
        musicSource.volume       = musicVolume;
        if (bgMusic != null)
        {
            musicSource.clip = bgMusic;
            musicSource.Play();
        }

        // Генерируем процедурные звуки для пустых слотов
        if (sfxUnitPlace       == null) sfxUnitPlace       = ProceduralAudio.UnitPlace();
        if (sfxUnitDeath       == null) sfxUnitDeath       = ProceduralAudio.UnitDeath();
        if (sfxEnemyHit        == null) sfxEnemyHit        = ProceduralAudio.EnemyHit();
        if (sfxEnemyDeath      == null) sfxEnemyDeath      = ProceduralAudio.EnemyDeath();
        if (sfxEnemyReachedBase== null) sfxEnemyReachedBase= ProceduralAudio.EnemyReachedBase();
        if (sfxWaveStart       == null) sfxWaveStart       = ProceduralAudio.WaveStart();
        if (sfxVictory         == null) sfxVictory         = ProceduralAudio.Victory();
        if (sfxGameOver        == null) sfxGameOver        = ProceduralAudio.GameOver();
    }

    private void OnEnable()
    {
        GameEvents.OnUnitPlaced        += OnUnitPlaced;
        GameEvents.OnUnitDied          += OnUnitDied;
        GameEvents.OnUnitAttack        += OnUnitAttack;
        GameEvents.OnEnemyHit          += OnEnemyHit;
        GameEvents.OnEnemyDied         += OnEnemyDied;
        GameEvents.OnEnemyReachedBase  += OnEnemyReachedBase;
        GameEvents.OnWavePhaseStarted  += OnWaveStarted;
        GameEvents.OnVictory           += OnVictory;
        GameEvents.OnGameOver          += OnGameOver;
    }

    private void OnDisable()
    {
        GameEvents.OnUnitPlaced        -= OnUnitPlaced;
        GameEvents.OnUnitDied          -= OnUnitDied;
        GameEvents.OnUnitAttack        -= OnUnitAttack;
        GameEvents.OnEnemyHit          -= OnEnemyHit;
        GameEvents.OnEnemyDied         -= OnEnemyDied;
        GameEvents.OnEnemyReachedBase  -= OnEnemyReachedBase;
        GameEvents.OnWavePhaseStarted  -= OnWaveStarted;
        GameEvents.OnVictory           -= OnVictory;
        GameEvents.OnGameOver          -= OnGameOver;
    }

    // ── Обработчики ───────────────────────────────────────────────────────────

    private void OnUnitPlaced(Unit _)           => Play(sfxUnitPlace);
    private void OnUnitDied(Unit _)             => Play(sfxUnitDeath);
    private void OnUnitAttack(Unit unit)
    {
        if (unit.Data.unitType == UnitType.Melee)  Play(sfxMeleeAttack);
        else                                        Play(sfxRangedAttack);
    }
    private void OnEnemyDied(Enemy _)           => Play(sfxEnemyDeath);
    private void OnEnemyReachedBase(Enemy _)    => Play(sfxEnemyReachedBase);
    private void OnWaveStarted()                => Play(sfxWaveStart);
    private void OnVictory()                    => Play(sfxVictory);
    private void OnGameOver()                   => Play(sfxGameOver);

    private void OnEnemyHit(Vector3 _)          => Play(sfxEnemyHit, hitVolume);

    // ── Воспроизведение ───────────────────────────────────────────────────────

    private void Play(AudioClip clip, float volume = -1f)
    {
        if (clip == null || source == null) return;
        source.PlayOneShot(clip, (volume < 0 ? globalVolume : volume));
    }
}
