using UnityEngine;
using UnityEngine.InputSystem;

public class SelectionManager : MonoBehaviour
{
    #region Materials

    [Header("Node Materials")]
    [SerializeField] public Material normalNodeMaterial;
    [SerializeField] public Material sourceNodeMaterial;
    [SerializeField] public Material destinationNodeMaterial;
    [SerializeField] public Material visitedNodeMaterial;
    [SerializeField] public Material pathNodeMaterial;

    [Header("Bipartite Node Materials")]
    [SerializeField] public Material bipartiteGroupAMaterial;
    [SerializeField] public Material bipartiteGroupBMaterial;

    [Header("Edge Materials")]
    [SerializeField] public Material normalEdgeMaterial;
    [SerializeField] public Material visitedEdgeMaterial;
    [SerializeField] public Material pathEdgeMaterial;
    [SerializeField] public Material conflictEdgeMaterial;

    [Header("Edge Type Materials")]
    [SerializeField] public Material ropeEdgeMaterial;
    [SerializeField] public Material woodEdgeMaterial;
    [SerializeField] public Material stoneEdgeMaterial;

    [Header("Feedback")]
    [SerializeField] public Material errorEdgeMaterial;

    public Material GetEdgeTypeMaterial(EdgeType type) => type switch
    {
        EdgeType.Rope  => ropeEdgeMaterial,
        EdgeType.Wood  => woodEdgeMaterial,
        EdgeType.Stone => stoneEdgeMaterial,
        _              => normalEdgeMaterial
    };

    #endregion

    #region State

    public GraphNode SourceNode      { get; private set; }
    public GraphNode DestinationNode { get; private set; }
    public bool SelectionEnabled     { get; set; } = true;

    #endregion

    #region Input

    private void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null || !SelectionEnabled) return;

        if (mouse.leftButton.wasPressedThisFrame)  TrySelect(isSource: true);
        if (mouse.rightButton.wasPressedThisFrame) TrySelect(isSource: false);
    }

    private void TrySelect(bool isSource)
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit)) return;

        GraphNode node = hit.collider.GetComponent<GraphNode>();
        if (node == null) return;

        if (isSource)
        {
            if (SourceNode != null) SourceNode.SetMaterial(normalNodeMaterial);
            SourceNode = node;
            node.SetMaterial(sourceNodeMaterial);
        }
        else
        {
            if (DestinationNode != null) DestinationNode.SetMaterial(normalNodeMaterial);
            DestinationNode = node;
            node.SetMaterial(destinationNodeMaterial);
        }
    }

    #endregion
}
