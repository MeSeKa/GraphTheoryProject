using UnityEngine;
using UnityEngine.InputSystem;

public class SelectionManager : MonoBehaviour
{
    [Header("Node Materials")]
    [SerializeField] public Material normalNodeMaterial;
    [SerializeField] public Material sourceNodeMaterial;
    [SerializeField] public Material destinationNodeMaterial;
    [SerializeField] public Material visitedNodeMaterial;
    [SerializeField] public Material pathNodeMaterial;

    [Header("Edge Materials")]
    [SerializeField] public Material normalEdgeMaterial;
    [SerializeField] public Material visitedEdgeMaterial;
    [SerializeField] public Material pathEdgeMaterial;

    public GraphNode SourceNode      { get; private set; }
    public GraphNode DestinationNode { get; private set; }

    private void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

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
}
