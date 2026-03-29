using UnityEngine;

/// <summary>
/// Заменяет 3D-модель на спрайт (статичный или анимированный), всегда смотрит на камеру.
/// Коллайдер и HP-бар остаются нетронутыми.
/// </summary>
public class BillboardSprite : MonoBehaviour
{
    [SerializeField] private float size = 1.2f;

    public Animator SpriteAnimator { get; private set; }

    private Transform spriteRoot;

    // ── Фабричные методы ──────────────────────────────────────────────────────

    /// Статичный спрайт (иконка)
    public static BillboardSprite AddTo(GameObject target, Sprite sprite, float size = 1.2f)
    {
        HideMesh(target);
        var bs  = target.AddComponent<BillboardSprite>();
        bs.size = size;
        bs.BuildStatic(sprite);
        return bs;
    }

    /// Анимированный спрайт (Animator Controller)
    public static BillboardSprite AddTo(GameObject target, RuntimeAnimatorController controller, float size = 0.3f)
    {
        HideMesh(target);
        var bs  = target.AddComponent<BillboardSprite>();
        bs.size = size;
        bs.BuildAnimated(controller);
        return bs;
    }

    // ── Построение ────────────────────────────────────────────────────────────

    private void BuildStatic(Sprite sprite)
    {
        spriteRoot = new GameObject("Visual").transform;
        spriteRoot.SetParent(transform, false);

        var sr = spriteRoot.gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 1;

        float texSize = Mathf.Max(sprite.texture.width, sprite.texture.height);
        float scale   = size / (texSize / sprite.pixelsPerUnit);
        spriteRoot.localScale = Vector3.one * scale;
    }

    private void BuildAnimated(RuntimeAnimatorController controller)
    {
        var existing = transform.Find("Visual");
        if (existing != null)
        {
            spriteRoot = existing;
        }
        else
        {
            spriteRoot = new GameObject("Visual").transform;
            spriteRoot.SetParent(transform, false);
        }

        if (spriteRoot.GetComponent<SpriteRenderer>() == null)
            spriteRoot.gameObject.AddComponent<SpriteRenderer>().sortingOrder = 1;

        SpriteAnimator = spriteRoot.GetComponent<Animator>();
        if (SpriteAnimator == null)
            SpriteAnimator = spriteRoot.gameObject.AddComponent<Animator>();

        SpriteAnimator.runtimeAnimatorController = controller;
    }

    private static void HideMesh(GameObject target)
    {
        var r = target.GetComponent<Renderer>();
        if (r != null) r.enabled = false;
    }

    // ── Billboard ─────────────────────────────────────────────────────────────

    private void LateUpdate()
    {
        if (spriteRoot == null) return;

        var cam = Camera.main;
        if (cam != null)
            spriteRoot.rotation = cam.transform.rotation;
    }
}
