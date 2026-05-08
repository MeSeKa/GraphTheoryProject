using System.Collections.Generic;
using UnityEngine;

public class HexTile : MonoBehaviour
{
    [HideInInspector] public int q, r;
    [HideInInspector] public List<HexBridge> bridges = new();

    [SerializeField] Renderer tileRenderer;

    public void SetMaterial(Material mat)
    {
        if (tileRenderer) tileRenderer.material = mat;
    }
}
