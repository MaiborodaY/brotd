using UnityEngine;

/// <summary>
/// Спавнит Короля в начале игры на позиции этого объекта.
/// </summary>
public class KingSpawner : MonoBehaviour
{
    [SerializeField] private UnitData  kingData;
    [SerializeField] private GameObject kingPrefab;

    private void Start()
    {
        if (kingData == null || kingPrefab == null)
        {
            Debug.LogWarning("[KingSpawner] kingData или kingPrefab не назначены.");
            return;
        }

        var go   = Instantiate(kingPrefab, transform.position, Quaternion.identity);
        var king = go.GetComponent<KingUnit>();
        if (king == null)
        {
            Debug.LogError("[KingSpawner] KingPrefab не содержит KingUnit.");
            Destroy(go);
            return;
        }

        king.InitKing(kingData, transform.position);
    }
}
