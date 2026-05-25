using DG.Tweening;
using UnityEngine;

public class HexInteractIndicator : MonoBehaviour
{
    [Header("Renderer — Indicator child'ını buraya bağla")]
    [SerializeField] public SpriteRenderer sr;

    [Header("Sprites")]
    [SerializeField] public Sprite normalSprite;
    [SerializeField] public Sprite hoverSprite;

    [Header("Hover rengi")]
    [SerializeField] Color hoverColor = new Color(1f, 0.85f, 0.1f, 1f);

    [Header("Hover scale çarpanı")]
    [SerializeField] float hoverScaleMultiplier = 1.4f;

    [Header("Kameraya dönük Y offset (world)")]
    [SerializeField] float yOffset = 1.5f;

    private Vector3 _baseScale;
    private Color   _baseColor;
    private bool    _visible;

    private void Awake()
    {
        if (sr == null) return;

        if (normalSprite != null) sr.sprite = normalSprite;

        // Parent hiyerarşisinden çıkar — parent scale/rotation artık etkilemez
        sr.transform.SetParent(null, worldPositionStays: true);

        // Unparent sonrası local = world, prefab'daki gerçek boyutu al
        _baseScale = sr.transform.localScale;
        _baseColor = sr.color;
        sr.enabled = false;
    }

    private void OnDestroy()
    {
        // Bridge/tile yıkılınca detached indicator'ı da yok et
        if (sr != null) Destroy(sr.gameObject);
    }

    public void SetVisible(bool visible)
    {
        if (sr == null) return;
        _visible   = visible;
        sr.enabled = visible;
        if (!visible) return;

        if (normalSprite != null) sr.sprite = normalSprite;
        sr.color = _baseColor;

        DOTween.Kill(sr.transform);
        sr.transform.localScale = _baseScale;
        sr.transform.DOPunchScale(_baseScale * 0.3f, 0.3f, 2, 0.5f);
    }

    public void SetHover(bool hovered)
    {
        if (sr == null || !_visible) return;

        if (hovered && hoverSprite != null)       sr.sprite = hoverSprite;
        else if (!hovered && normalSprite != null) sr.sprite = normalSprite;

        DOTween.Kill(sr);
        DOTween.Kill(sr.transform);
        sr.DOColor(hovered ? hoverColor : _baseColor, 0.12f);
        sr.transform.DOScale(hovered ? _baseScale * hoverScaleMultiplier : _baseScale, 0.15f);
    }

    private void LateUpdate()
    {
        if (!_visible || sr == null || Camera.main == null) return;
        // Bridge/tile'ı world'de takip et — parent yok, scale/rotation kirlenmez
        sr.transform.position = transform.position + Vector3.up * yOffset;
        sr.transform.rotation = Camera.main.transform.rotation;
    }
}
