using DG.Tweening;
using UnityEngine;

public class GraphEdge : MonoBehaviour
{
    [SerializeField] public GraphNode nodeA;
    [SerializeField] public GraphNode nodeB;

    [SerializeField] Renderer edgeRenderer;

    private const float AnimDuration = 0.4f;

    private void Start()
    {
        if (nodeA != null && !nodeA.edges.Contains(this)) nodeA.edges.Add(this);
        if (nodeB != null && !nodeB.edges.Contains(this)) nodeB.edges.Add(this);
    }

    public GraphNode GetOtherNode(GraphNode from) => from == nodeA ? nodeB : nodeA;

    public void AnimateVisited(Material visitedMat)
    {
        Color target = visitedMat.GetColor("_BaseColor");
        edgeRenderer.material.DOColor(target, "_BaseColor", AnimDuration);
    }

    public void SetMaterial(Material mat)
    {
        edgeRenderer.material = mat;
    }
}
