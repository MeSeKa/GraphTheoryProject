using System.Collections.Generic;
using UnityEngine;

public class GraphNode : MonoBehaviour
{
    [HideInInspector] public List<GraphEdge> edges = new List<GraphEdge>();

    [SerializeField] Renderer nodeRenderer;

    public void SetMaterial(Material mat)
    {
        nodeRenderer.material = mat;
    }
}
