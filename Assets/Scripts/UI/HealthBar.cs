using UnityEngine;

/// <summary>
/// HP бар из двух квадов: тёмный фон + зелёная заливка.
/// Не использует Canvas — работает надёжно в 3D.
/// </summary>
public class HealthBar : MonoBehaviour
{
    [SerializeField] private float worldOffsetY = 1.4f;  // высота над pivot в мировых единицах
    [SerializeField] private float   width  = 1.0f;
    [SerializeField] private float   height = 0.1f;

    private Transform fillTransform;
    private Transform barRoot;   // прямая ссылка вместо GetChild(0)
    private float     maxHp;

    // ── Фабричный метод ───────────────────────────────────────────────────────

    public static HealthBar AddTo(GameObject target, Vector3? offset = null)
    {
        var hb = target.AddComponent<HealthBar>();
        if (offset.HasValue) hb.worldOffsetY = offset.Value.y;
        return hb;
    }

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        BuildQuads();
    }

    private void OnEnable()
    {
        if (barRoot != null) barRoot.gameObject.SetActive(true);
    }

    private void OnDisable()
    {
        if (barRoot != null) barRoot.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (barRoot != null)
            Destroy(barRoot.gameObject);
    }

    private void LateUpdate()
    {
        if (barRoot == null) return;

        // Фиксированная высота в мировых координатах (не зависит от scale юнита)
        barRoot.position = new Vector3(
            transform.position.x,
            transform.position.y + worldOffsetY,
            transform.position.z);

        // Полный billboard — всегда параллельно экрану (не tilted при наклонной камере)
        var cam = Camera.main;
        if (cam != null)
            barRoot.rotation = cam.transform.rotation;
    }

    // ── API ───────────────────────────────────────────────────────────────────

    public void SetHeight(float worldY) => worldOffsetY = worldY;

    public void SetFill(int current, int max)
    {
        if (fillTransform == null || max <= 0) return;

        maxHp = max;
        float ratio = Mathf.Clamp01((float)current / max);

        // Масштабируем X заливки
        var s = fillTransform.localScale;
        s.x = ratio;
        fillTransform.localScale = s;

        // Сдвигаем заливку влево чтобы она убывала справа
        var p = fillTransform.localPosition;
        p.x = (ratio - 1f) * width * 0.5f;
        fillTransform.localPosition = p;
    }

    // ── Построение ───────────────────────────────────────────────────────────

    private void BuildQuads()
    {
        // Отцепляем от юнита в мировое пространство — иначе наследует scale родителя
        var root = new GameObject("HPBarRoot");
        root.transform.SetParent(null);
        barRoot = root.transform;

        // Фон (тёмный)
        var bg = CreateQuad("BG", root.transform,
            new Color(0.15f, 0.15f, 0.15f, 0.9f));
        bg.localScale    = new Vector3(width, height, 1f);
        bg.localPosition = Vector3.zero;

        // Заливка (зелёная)
        var fill = CreateQuad("Fill", root.transform, Color.green);
        fill.localScale    = new Vector3(width, height * 0.8f, 1f);
        fill.localPosition = new Vector3(0f, 0f, -0.01f); // чуть ближе к камере
        fillTransform = fill;
    }

    private static Transform CreateQuad(string name, Transform parent, Color color)
    {
        // Создаём вручную без MeshCollider — CreatePrimitive добавляет его автоматически,
        // и он вырезается Android code stripping'ом, вызывая краш.
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var mf   = go.AddComponent<MeshFilter>();
        mf.mesh  = Resources.GetBuiltinResource<Mesh>("Quad.fbx");

        var shader = Shader.Find("Universal Render Pipeline/Unlit")
                  ?? Shader.Find("Sprites/Default")
                  ?? Shader.Find("Unlit/Color");
        var mat    = new Material(shader);
        mat.color  = color;
        go.AddComponent<MeshRenderer>().material = mat;

        return go.transform;
    }
}
