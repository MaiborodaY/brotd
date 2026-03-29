using UnityEngine;

/// <summary>
/// Три звёздочки над юнитом. Скрыты на 1-м уровне, появляются при апгрейде.
/// Позиция и размер задаются автоматически; billboard следует за камерой.
/// </summary>
public class UnitLevelBadge : MonoBehaviour
{
    private Transform   badgeRoot;
    private Transform[] dots;
    private float       offsetY;
    private int         currentLevel = 1;

    private static readonly Color ColorEarned = new Color(1f, 0.85f, 0.1f);   // золотой
    private static readonly Color ColorEmpty  = new Color(0.3f, 0.3f, 0.3f, 0.7f);

    // ── Фабричный метод ───────────────────────────────────────────────────────

    public static UnitLevelBadge AddTo(GameObject target, float yOffset)
    {
        var badge    = target.AddComponent<UnitLevelBadge>();
        badge.offsetY = yOffset;
        return badge;
    }

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Awake()         => Build();
    private void OnDestroy()     { if (badgeRoot != null) Destroy(badgeRoot.gameObject); }
    private void OnEnable()      { if (badgeRoot != null) badgeRoot.gameObject.SetActive(currentLevel > 1); }
    private void OnDisable()     { if (badgeRoot != null) badgeRoot.gameObject.SetActive(false); }

    private void LateUpdate()
    {
        if (badgeRoot == null) return;
        badgeRoot.position = new Vector3(
            transform.position.x,
            transform.position.y + offsetY,
            transform.position.z);
        var cam = Camera.main;
        if (cam != null) badgeRoot.rotation = cam.transform.rotation;
    }

    // ── API ───────────────────────────────────────────────────────────────────

    public void SetLevel(int level)
    {
        currentLevel = level;
        if (badgeRoot == null || dots == null) return;

        badgeRoot.gameObject.SetActive(level > 1);

        for (int i = 0; i < dots.Length; i++)
        {
            var mr = dots[i].GetComponent<MeshRenderer>();
            if (mr != null)
                mr.material.color = i < level ? ColorEarned : ColorEmpty;
        }
    }

    // ── Построение ───────────────────────────────────────────────────────────

    private void Build()
    {
        var root    = new GameObject("LevelBadgeRoot");
        root.transform.SetParent(null);
        badgeRoot   = root.transform;
        badgeRoot.gameObject.SetActive(false); // скрыт пока уровень 1

        var shader = Shader.Find("Universal Render Pipeline/Unlit")
                  ?? Shader.Find("Sprites/Default")
                  ?? Shader.Find("Unlit/Color");

        float dotSize = 0.11f;
        float spacing = 0.16f;
        dots = new Transform[3];

        for (int i = 0; i < 3; i++)
        {
            var go = new GameObject($"Dot{i + 1}");
            go.transform.SetParent(badgeRoot, false);

            var mf  = go.AddComponent<MeshFilter>();
            mf.mesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");

            var mat = new Material(shader);
            mat.color = ColorEmpty;
            go.AddComponent<MeshRenderer>().material = mat;

            go.transform.localScale    = new Vector3(dotSize, dotSize, 1f);
            go.transform.localPosition = new Vector3((i - 1) * spacing, 0f, 0f);
            dots[i] = go.transform;
        }
    }
}
