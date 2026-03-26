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
    public static BillboardSprite AddTo(GameObject target, RuntimeAnimatorController controller, float size = 0.1f)
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
        var child = CreateSpriteRoot();
        var sr         = child.AddComponent<SpriteRenderer>();
        sr.sprite      = sprite;
        sr.sortingOrder = 1;

        float texSize = Mathf.Max(sprite.texture.width, sprite.texture.height);
        float scale   = size / (texSize / sprite.pixelsPerUnit);
        child.transform.localScale = Vector3.one * scale;
    }

    private void BuildAnimated(RuntimeAnimatorController controller)
    {
        var child = CreateSpriteRoot();
        var sr = child.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 1;

        SpriteAnimator = child.AddComponent<Animator>();
        SpriteAnimator.runtimeAnimatorController = controller;

        // Нормализуем масштаб через кадр — когда Animator проставит первый спрайт
        StartCoroutine(NormalizeScale(child.transform, sr));
    }

    private System.Collections.IEnumerator NormalizeScale(Transform t, SpriteRenderer sr)
    {
        yield return null;
        t.localScale = Vector3.one * size;
    }

    private GameObject CreateSpriteRoot()
    {
        spriteRoot = new GameObject("SpriteRoot").transform;
        spriteRoot.SetParent(transform, false);

        var child = new GameObject("Sprite");
        child.transform.SetParent(spriteRoot, false);
        return child;
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
            spriteRoot.rotation = Quaternion.LookRotation(
                spriteRoot.position - cam.transform.position);
    }
}
