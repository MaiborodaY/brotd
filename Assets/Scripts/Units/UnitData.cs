using UnityEngine;

public enum UnitType { Melee, Ranged }

/// <summary>
/// Все статы юнита в одном ScriptableObject.
/// Create → BroTD → UnitData
/// </summary>
[CreateAssetMenu(menuName = "BroTD/UnitData", fileName = "UnitData")]
public class UnitData : ScriptableObject
{
    [Header("Identity")]
    public string displayName = "Unit";
    [Tooltip("Подпись над юнитом: ⚔ для воина, ↑ для лучника")]
    public string label       = "?";
    public UnitType unitType  = UnitType.Melee;
    public Sprite icon;
    public GameObject prefab;

    [Header("Movement")]
    public float moveSpeed      = 2f;
    [Tooltip("Радиус видения: юнит замечает врагов в этом радиусе и идёт к ним. Для лучника = attackRange (не двигается)")]
    public float detectionRange = 4f;

    [Header("Combat")]
    public int   maxHealth      = 50;
    public float attackDamage   = 10f;
    public float attackRange    = 1.5f;
    public float attackCooldown = 1f;   // секунд между атаками

    [Header("Economy")]
    public int goldCost = 50;

    [Header("Visuals")]
    [Tooltip("Высота полоски HP над pivot юнита (мировые единицы). Увеличь для крупных юнитов.")]
    public float hpBarHeight = 1.4f;
}
