using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GraphManager : MonoBehaviour
{
    [SerializeField] SelectionManager selectionManager;
    [SerializeField] CameraFramer cameraFramer;
    [SerializeField] GraphGenerator graphGenerator;

    [Header("UI Buttons")]
    [SerializeField] Button bfsButton;
    [SerializeField] Button dfsButton;
    [SerializeField] Button startButton;
    [SerializeField] Button generateButton;

    [Header("Traversal")]
    [SerializeField] float stepDelay = 0.5f;

    public enum Algorithm { BFS, DFS }
    public Algorithm SelectedAlgorithm { get; private set; } = Algorithm.BFS;

    private Coroutine _traversalCoroutine;

    private void Start()
    {
        bfsButton.onClick.AddListener(() => SelectedAlgorithm = Algorithm.BFS);
        dfsButton.onClick.AddListener(() => SelectedAlgorithm = Algorithm.DFS);
        startButton.onClick.AddListener(OnStartClicked);
        generateButton.onClick.AddListener(() =>
        {
            graphGenerator.GenerateGraph();
            cameraFramer.FrameAllNodes();
        });
    }

    private void OnStartClicked()
    {
        GraphNode source      = selectionManager.SourceNode;
        GraphNode destination = selectionManager.DestinationNode;

        if (source == null || destination == null)
        {
            Debug.LogWarning("Önce bir Source ve Destination node seç.");
            return;
        }

        if (_traversalCoroutine != null) StopCoroutine(_traversalCoroutine);

        ResetGraph();
        source.SetMaterial(selectionManager.sourceNodeMaterial);
        destination.SetMaterial(selectionManager.destinationNodeMaterial);

        Pathfinder.PathfinderResult result = SelectedAlgorithm == Algorithm.BFS
            ? Pathfinder.BFS(source, destination)
            : Pathfinder.DFS(source, destination);

        _traversalCoroutine = StartCoroutine(RunTraversal(result, source, destination));
    }

    private void ResetGraph()
    {
        foreach (var node in FindObjectsByType<GraphNode>(FindObjectsSortMode.None))
            node.SetMaterial(selectionManager.normalNodeMaterial);

        foreach (var edge in FindObjectsByType<GraphEdge>(FindObjectsSortMode.None))
            edge.SetMaterial(selectionManager.normalEdgeMaterial);
    }

    private IEnumerator RunTraversal(Pathfinder.PathfinderResult result, GraphNode source, GraphNode destination)
    {
        foreach (var step in result.TraversalSteps)
        {
            step.Edge.AnimateVisited(selectionManager.visitedEdgeMaterial);

            if (step.ArrivalNode != source && step.ArrivalNode != destination)
                step.ArrivalNode.SetMaterial(selectionManager.visitedNodeMaterial);

            yield return new WaitForSeconds(stepDelay);
        }

        yield return new WaitForSeconds(stepDelay);
        yield return StartCoroutine(ShowPath(result.PathSteps, source, destination));

        _traversalCoroutine = null;
    }

    private IEnumerator ShowPath(List<Pathfinder.TraversalStep> pathSteps, GraphNode source, GraphNode destination)
    {
        source.SetMaterial(selectionManager.pathNodeMaterial);
        yield return new WaitForSeconds(stepDelay);

        foreach (var step in pathSteps)
        {
            step.Edge.AnimateVisited(selectionManager.pathEdgeMaterial);

            if (step.ArrivalNode != destination)
                step.ArrivalNode.SetMaterial(selectionManager.pathNodeMaterial);

            yield return new WaitForSeconds(stepDelay);
        }

        destination.SetMaterial(selectionManager.pathNodeMaterial);
    }
}
