using DG.Tweening;
using UnityEngine;

public class HexBridge : MonoBehaviour
{
    [HideInInspector] public HexTile  tileA;
    [HideInInspector] public HexTile  tileB;
    [HideInInspector] public EdgeType edgeType;

    [SerializeField] Renderer bridgeRenderer;

    private Material _typeMaterial;
    private const float AnimDuration = 0.35f;

    public void Initialize(HexTile a, HexTile b, EdgeType type, Material typeMat)
    {
        tileA         = a;
        tileB         = b;
        edgeType      = type;
        _typeMaterial = typeMat;
        SetMaterial(typeMat);

        // Position: midpoint between the two tile centres
        Vector3 posA = a.transform.position;
        Vector3 posB = b.transform.position;
        Vector3 mid  = (posA + posB) * 0.5f;

        transform.position = mid;
        // Rotate so the bridge's forward axis aligns A → B
        Vector3 dir = (posB - posA).normalized;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

        if (!a.bridges.Contains(this)) a.bridges.Add(this);
        if (!b.bridges.Contains(this)) b.bridges.Add(this);
    }

    public void SetMaterial(Material mat)
    {
        if (bridgeRenderer != null && mat != null) bridgeRenderer.material = mat;
    }

    public void RestoreTypeMaterial()
    {
        if (_typeMaterial) SetMaterial(_typeMaterial);
    }

    public void AnimateDestroyed(Material destroyedMat)
    {
        if (bridgeRenderer == null) return;
        Color target = destroyedMat.GetColor("_BaseColor");
        bridgeRenderer.material.DOColor(target, "_BaseColor", AnimDuration);
    }

    public HexTile GetOtherTile(HexTile from) => from == tileA ? tileB : tileA;
}
