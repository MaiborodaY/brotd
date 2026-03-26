using UnityEngine;

/// <summary>
/// Стрела лучника: летит к цели, поворачивается по направлению движения, исчезает при попадании.
/// Урон уже нанесён в момент выстрела — стрела только визуальная.
/// </summary>
public class Arrow : MonoBehaviour
{
    [SerializeField] private float speed = 14f;

    private Enemy   target;
    private Vector3 fallbackPos; // если враг умер пока летела

    // ── Фабричный метод ───────────────────────────────────────────────────────

    public static Arrow Shoot(Vector3 from, Enemy target, float speed = 14f)
    {
        var go    = BuildMesh();
        go.transform.position = from + Vector3.up * 0.3f;

        var arrow       = go.AddComponent<Arrow>();
        arrow.target    = target;
        arrow.fallbackPos = target.transform.position;
        arrow.speed     = speed;

        // Смотрим на цель сразу при спавне
        go.transform.LookAt(target.transform.position + Vector3.up * 0.3f);

        return arrow;
    }

    // ── Update ────────────────────────────────────────────────────────────────

    private void Update()
    {
        Vector3 dest = (target != null && target.IsAlive)
            ? target.transform.position + Vector3.up * 0.3f
            : fallbackPos;

        // Поворачиваемся по направлению полёта
        Vector3 dir = (dest - transform.position);
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(dir.normalized);

        transform.position = Vector3.MoveTowards(
            transform.position, dest, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, dest) < 0.15f)
            Destroy(gameObject);
    }

    // ── Построение меша ───────────────────────────────────────────────────────

    private static GameObject BuildMesh()
    {
        var root = new GameObject("Arrow");

        // Древко — тонкий вытянутый цилиндр
        var shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        shaft.name = "Shaft";
        shaft.transform.SetParent(root.transform, false);
        shaft.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // вдоль Z
        shaft.transform.localPosition = new Vector3(0f, 0f, 0.1f);
        shaft.transform.localScale    = new Vector3(0.06f, 0.35f, 0.06f);
        Destroy(shaft.GetComponent<Collider>());
        SetColor(shaft, new Color(0.55f, 0.35f, 0.15f)); // коричневый

        // Наконечник — маленький вытянутый куб
        var tip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tip.name = "Tip";
        tip.transform.SetParent(root.transform, false);
        tip.transform.localPosition = new Vector3(0f, 0f, 0.45f);
        tip.transform.localScale    = new Vector3(0.07f, 0.07f, 0.18f);
        Destroy(tip.GetComponent<Collider>());
        SetColor(tip, new Color(0.55f, 0.55f, 0.6f)); // серый металл

        // Оперение — маленький плоский куб сзади
        var feather = GameObject.CreatePrimitive(PrimitiveType.Cube);
        feather.name = "Feather";
        feather.transform.SetParent(root.transform, false);
        feather.transform.localPosition = new Vector3(0f, 0f, -0.25f);
        feather.transform.localScale    = new Vector3(0.18f, 0.18f, 0.05f);
        Destroy(feather.GetComponent<Collider>());
        SetColor(feather, new Color(0.85f, 0.2f, 0.2f)); // красное перо

        return root;
    }

    private static void SetColor(GameObject go, Color color)
    {
        var mat   = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = color;
        go.GetComponent<Renderer>().material = mat;
    }
}
