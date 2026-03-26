using UnityEngine;

/// <summary>
/// Генерирует AudioClip-ы процедурно — без файлов.
/// </summary>
public static class ProceduralAudio
{
    private const int SampleRate = 44100;

    // ── Публичные клипы ───────────────────────────────────────────────────────

    /// Короткий щелчок вверх — постановка юнита
    public static AudioClip UnitPlace()
        => Tone("UnitPlace", freq0: 400f, freq1: 800f, duration: 0.12f, volume: 0.5f, shape: Shape.Sine);

    /// Глухой удар — попадание по врагу
    public static AudioClip EnemyHit()
        => Tone("EnemyHit", freq0: 180f, freq1: 80f, duration: 0.07f, volume: 0.35f, shape: Shape.Noise);

    /// Нисходящий тон — смерть врага
    public static AudioClip EnemyDeath()
        => Tone("EnemyDeath", freq0: 300f, freq1: 80f, duration: 0.22f, volume: 0.5f, shape: Shape.Sine);

    /// Низкий удар — враг добрался до базы
    public static AudioClip EnemyReachedBase()
        => Tone("EnemyBase", freq0: 120f, freq1: 60f, duration: 0.3f, volume: 0.7f, shape: Shape.Sine);

    /// Смерть юнита — нисходящий шум
    public static AudioClip UnitDeath()
        => Tone("UnitDeath", freq0: 250f, freq1: 100f, duration: 0.25f, volume: 0.5f, shape: Shape.Noise);

    /// Старт волны — восходящий тон
    public static AudioClip WaveStart()
        => Tone("WaveStart", freq0: 300f, freq1: 600f, duration: 0.3f, volume: 0.6f, shape: Shape.Sine);

    /// Победа — короткий восходящий аккорд
    public static AudioClip Victory()
        => Chord("Victory", new[] { 400f, 500f, 600f }, duration: 0.6f, volume: 0.6f);

    /// Поражение — нисходящий аккорд
    public static AudioClip GameOver()
        => Chord("GameOver", new[] { 300f, 240f, 180f }, duration: 0.8f, volume: 0.6f);

    // ── Генераторы ────────────────────────────────────────────────────────────

    private enum Shape { Sine, Noise }

    private static AudioClip Tone(string name,
        float freq0, float freq1, float duration, float volume, Shape shape)
    {
        int samples = Mathf.CeilToInt(SampleRate * duration);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t     = (float)i / samples;           // 0..1
            float freq  = Mathf.Lerp(freq0, freq1, t);  // скользящая частота
            float env   = Envelope(t);                   // огибающая громкости

            float sample = shape == Shape.Sine
                ? Mathf.Sin(2f * Mathf.PI * freq * i / SampleRate)
                : Random.Range(-1f, 1f);                 // белый шум

            data[i] = sample * env * volume;
        }

        var clip = AudioClip.Create(name, samples, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static AudioClip Chord(string name, float[] freqs, float duration, float volume)
    {
        int samples = Mathf.CeilToInt(SampleRate * duration);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t   = (float)i / samples;
            float env = Envelope(t);
            float sum = 0f;
            foreach (var f in freqs)
                sum += Mathf.Sin(2f * Mathf.PI * f * i / SampleRate);
            data[i] = (sum / freqs.Length) * env * volume;
        }

        var clip = AudioClip.Create(name, samples, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // Плавный fade-in + fade-out
    private static float Envelope(float t)
    {
        float attack  = 0.05f;
        float release = 0.3f;
        if (t < attack)  return t / attack;
        if (t > 1f - release) return (1f - t) / release;
        return 1f;
    }
}
