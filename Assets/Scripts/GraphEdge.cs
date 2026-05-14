using DG.Tweening;
using UnityEngine;

public enum EdgeType { Rope, Wood, Stone, Metal, Unbreakable }

public class GraphEdge : MonoBehaviour
{
    #region Fields

    [SerializeField] public GraphNode nodeA;
    [SerializeField] public GraphNode nodeB;
    [SerializeField] public EdgeType  edgeType;

    [SerializeField] Renderer edgeRenderer;

    private const float AnimDuration = 0.4f;
    private Vector3  _baseScale;
    private Material _typeMaterial;

    #endregion

    #region Setup

    private void Awake()
    {
        _baseScale = transform.localScale;
    }

    private void Start()
    {
        if (nodeA != null && nodeB != null) UpdateTransform();

        if (nodeA != null && !nodeA.edges.Contains(this)) nodeA.edges.Add(this);
        if (nodeB != null && !nodeB.edges.Contains(this)) nodeB.edges.Add(this);
    }

    public void Setup(GraphNode a, GraphNode b)
    {
        _baseScale = transform.localScale;
        nodeA = a;
        nodeB = b;
        UpdateTransform();
        if (!nodeA.edges.Contains(this)) nodeA.edges.Add(this);
        if (!nodeB.edges.Contains(this)) nodeB.edges.Add(this);
    }

    // Public so GraphManager can update edge positions during node movement animations
    public void UpdateTransform()
    {
        Vector3 dir = nodeB.transform.position - nodeA.transform.position;
        transform.position = (nodeA.transform.position + nodeB.transform.position) * 0.5f;
        transform.rotation = Quaternion.FromToRotation(Vector3.up, dir.normalized);
        transform.localScale = new Vector3(_baseScale.x, dir.magnitude * 0.5f, _baseScale.z);
    }

    #endregion

    #region Materials

    public void AssignType(EdgeType type, Material typeMaterial)
    {
        edgeType      = type;
        _typeMaterial = typeMaterial;
        SetMaterial(typeMaterial);
    }

    public void RestoreTypeMaterial()
    {
        if (_typeMaterial != null) SetMaterial(_typeMaterial);
    }

    public void AnimateVisited(Material visitedMat)
    {
        Color target = visitedMat.GetColor("_BaseColor");
        edgeRenderer.material.DOColor(target, "_BaseColor", AnimDuration);
    }

    public void SetMaterial(Material mat)
    {
        edgeRenderer.material = mat;
    }

    #endregion

    #region Helpers

    public GraphNode GetOtherNode(GraphNode from) => from == nodeA ? nodeB : nodeA;

    #endregion
}
