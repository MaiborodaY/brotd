using UnityEngine;

/// <summary>
/// Описание пути врагов. Создаётся как ScriptableObject Asset → правой кнопкой в Project → Create → BroTD → PathData.
/// Содержит список точек в мировых координатах — задаётся через EnemySpawner или вручную.
/// </summary>
[CreateAssetMenu(menuName = "BroTD/PathData", fileName = "PathData")]
public class PathData : ScriptableObject
{
    [Tooltip("Точки пути в мировых координатах, от старта к базе")]
    public Vector3[] waypoints;
}
