using System;
using UnityEngine;

/// <summary>
/// Описание одной волны врагов.
/// Create → BroTD → WaveData
/// </summary>
[CreateAssetMenu(menuName = "BroTD/WaveData", fileName = "WaveData")]
public class WaveData : ScriptableObject
{
    [Serializable]
    public struct SpawnEntry
    {
        public EnemyData enemyData;
        [Tooltip("Сколько врагов этого типа заспавнить")]
        public int count;
        [Tooltip("Если true — спавнится только на центральной дорожке (для боссов)")]
        public bool centerLaneOnly;
    }

    [Header("Wave Config")]
    public SpawnEntry[] entries;

    [Tooltip("Задержка между спавном каждого врага в секундах")]
    public float spawnInterval = 2f;

    [Tooltip("Золото, которое даётся игроку в начале волны (бонус подготовки)")]
    public int prepGoldBonus = 20;
}
