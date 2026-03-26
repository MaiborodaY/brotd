using TMPro;
using UnityEngine;

/// <summary>
/// Иконка или подпись над юнитом в мировом пространстве, всегда смотрит на камеру.
/// </summary>
public class FloatingLabel : MonoBehaviour
{
    private Vector3   offset;
    private Transform root;

    // ── Фабричные методы ──────────────────────────────────────────────────────

    /// Создаёт иконку из текстуры (пиксель-арт)
    public static FloatingLabel AddTo(GameObject target, Texture2D icon, Vector3 localOffset)
    {
        var fl    = target.AddComponent<FloatingLabel>();
        fl.offset = localOffset;
        fl.BuildIcon(icon);
        return fl;
    }

    /// Создаёт текстовую подпись (fallback)
    public static FloatingLabel AddTo(GameObject target, string text, Vector3 localOffset)
    {
        var fl    = target.AddComponent<FloatingLabel>();
        fl.offset = localOffset;
        fl.BuildText(text);
        return fl;
    }

    // ── Построение ────────────────────────────────────────────────────────────

    private void BuildIcon(Texture2D icon)
    {
        root = new GameObject("IconRoot").transform;
        root.SetParent(transform, false);

        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "Icon";
        quad.transform.SetParent(root, false);
        quad.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
        Destroy(quad.GetComponent<Collider>());

        var mat   = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.mainTexture = icon;
        // Включаем прозрачность
        mat.SetFloat("_Surface", 1f);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = 3000;

        quad.GetComponent<Renderer>().material = mat;
    }

    private void BuildText(string text)
    {
        root = new GameObject("LabelRoot").transform;
        root.SetParent(transform, false);

        var go  = new GameObject("LabelText");
        go.transform.SetParent(root, false);

        var tmp           = go.AddComponent<TextMeshPro>();
        tmp.text          = text;
        tmp.fontSize      = 3f;
        tmp.fontStyle     = FontStyles.Bold;
        tmp.color         = Color.white;
        tmp.alignment     = TextAlignmentOptions.Center;
        tmp.outlineWidth  = 0.2f;
        tmp.outlineColor  = new Color32(0, 0, 0, 200);

        var rt       = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(2f, 1f);
    }

    // ── Billboard ─────────────────────────────────────────────────────────────

    private void LateUpdate()
    {
        if (root == null) return;

        root.position = transform.position + offset;

        var cam = Camera.main;
        if (cam != null)
            root.rotation = Quaternion.LookRotation(root.position - cam.transform.position);
    }
}
