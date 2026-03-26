using UnityEngine;

/// <summary>
/// Все статы врага в одном ScriptableObject.
/// Create → BroTD → EnemyData
/// </summary>
[CreateAssetMenu(menuName = "BroTD/EnemyData", fileName = "EnemyData")]
public class EnemyData : ScriptableObject
{
    [Header("Identity")]
    public string displayName = "Enemy";
    [Tooltip("PNG иконка над врагом. Если не назначена — используется процедурная.")]
    public Sprite icon;
    [Tooltip("Animator Controller с анимациями Walk/Attack/Die. Если назначен — используется вместо иконки.")]
    public RuntimeAnimatorController animatorController;
    public GameObject prefab;

    [Header("Movement")]
    public float moveSpeed       = 2f;
    [Tooltip("Радиус видения: враг замечает юнитов в этом радиусе и идёт к ним. 4f покрывает всю ширину карты (3 линии)")]
    public float detectionRange  = 4f;

    [Header("Combat")]
    public int   maxHealth      = 100;
    public float attackDamage   = 10f;
    public float attackRange    = 1f;
    public float attackCooldown = 1f;

    [Header("Economy")]
    [Tooltip("Золото, которое получает игрок за убийство")]
    public int goldReward = 10;

    [Header("Visuals")]
    [Tooltip("Высота полоски HP над pivot врага (мировые единицы). Увеличь для боссов.")]
    public float hpBarHeight = 0.9f;
}
