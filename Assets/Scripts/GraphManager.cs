using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GraphManager : MonoBehaviour
{
    #region Fields

    [SerializeField] SelectionManager selectionManager;
    [SerializeField] CameraFramer     cameraFramer;
    [SerializeField] GraphGenerator   graphGenerator;

    [Header("UI Buttons")]
    [SerializeField] Button bfsButton;
    [SerializeField] Button dfsButton;
    [SerializeField] Button startButton;
    [SerializeField] Button generateButton;
    [SerializeField] Button generateBipartiteButton;
    [SerializeField] Button bipartiteCheckButton;

    [Header("UI Text")]
    [SerializeField] TMP_Text statusText;

    [Header("Traversal")]
    [SerializeField] float stepDelay = 0.5f;

    [Header("Bipartite Layout")]
    [SerializeField] float bipartiteSeparation   = 4f;
    [SerializeField] float bipartiteNodeSpacing  = 1.5f;
    [SerializeField] float bipartiteAnimDuration = 0.8f;

    public enum Algorithm { BFS, DFS }
    public Algorithm SelectedAlgorithm { get; private set; } = Algorithm.BFS;

    private Coroutine _activeCoroutine;

    #endregion

    #region Lifecycle

    private void Start()
    {
        bfsButton.onClick.AddListener(() =>
        {
            SelectedAlgorithm = Algorithm.BFS;
            SetStatus("Algorithm: BFS");
        });
        dfsButton.onClick.AddListener(() =>
        {
            SelectedAlgorithm = Algorithm.DFS;
            SetStatus("Algorithm: DFS");
        });

        startButton.onClick.AddListener(OnStartClicked);

        generateButton.onClick.AddListener(() =>
        {
            graphGenerator.GenerateGraph();
            cameraFramer.FrameAllNodes();
            SetStatus("Random graph generated.");
        });

        generateBipartiteButton.onClick.AddListener(() =>
        {
            graphGenerator.GenerateBipartiteGraph();
            cameraFramer.FrameAllNodes();
            SetStatus("Bipartite graph generated.");
        });

        bipartiteCheckButton.onClick.AddListener(OnBipartiteCheckClicked);

        SetStatus("Algorithm: BFS");
    }

    #endregion

    #region Traversal

    private void OnStartClicked()
    {
        GraphNode source      = selectionManager.SourceNode;
        GraphNode destination = selectionManager.DestinationNode;

        if (source == null || destination == null)
        {
            SetStatus($"{SelectedAlgorithm} | Select a source and destination node first.");
            return;
        }

        if (_activeCoroutine != null) StopCoroutine(_activeCoroutine);

        ResetGraph();
        source.SetMaterial(selectionManager.sourceNodeMaterial);
        destination.SetMaterial(selectionManager.destinationNodeMaterial);

        Pathfinder.PathfinderResult result = SelectedAlgorithm == Algorithm.BFS
            ? Pathfinder.BFS(source, destination)
            : Pathfinder.DFS(source, destination);

        _activeCoroutine = StartCoroutine(RunTraversal(result, source, destination));
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
        int total = result.TraversalSteps.Count;
        int step  = 0;

        SetStatus($"{SelectedAlgorithm} | Traversing graph... (0 / {total})");

        foreach (var s in result.TraversalSteps)
        {
            step++;
            s.Edge.AnimateVisited(selectionManager.visitedEdgeMaterial);

            if (s.ArrivalNode != source && s.ArrivalNode != destination)
                s.ArrivalNode.SetMaterial(selectionManager.visitedNodeMaterial);

            SetStatus($"{SelectedAlgorithm} | Traversing graph... ({step} / {total})");
            yield return new WaitForSeconds(stepDelay);
        }

        yield return new WaitForSeconds(stepDelay);

        if (result.PathSteps.Count == 0)
        {
            SetStatus($"{SelectedAlgorithm} | Path not found.");
            _activeCoroutine = null;
            yield break;
        }

        SetStatus($"{SelectedAlgorithm} | Path found — tracing back...");
        yield return StartCoroutine(ShowPath(result.PathSteps, source, destination));

        SetStatus($"{SelectedAlgorithm} | Done. Path length: {result.PathSteps.Count} edge(s).");
        _activeCoroutine = null;
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

    #endregion

    #region Bipartite

    private void OnBipartiteCheckClicked()
    {
        if (_activeCoroutine != null) StopCoroutine(_activeCoroutine);

        ResetGraph();
        var allNodes = FindObjectsByType<GraphNode>(FindObjectsSortMode.None);
        var result   = Pathfinder.CheckBipartite(allNodes);

        _activeCoroutine = StartCoroutine(RunBipartiteCheck(result));
    }

    private IEnumerator RunBipartiteCheck(Pathfinder.BipartiteResult result)
    {
        int total = result.Steps.Count;
        int step  = 0;

        SetStatus($"Bipartite | Coloring graph... (0 / {total})");

        foreach (var s in result.Steps)
        {
            step++;
            var mat = s.Group == 0
                ? selectionManager.bipartiteGroupAMaterial
                : selectionManager.bipartiteGroupBMaterial;
            s.Node.SetMaterial(mat);
            SetStatus($"Bipartite | Coloring graph... ({step} / {total})");
            yield return new WaitForSeconds(stepDelay);
        }

        yield return new WaitForSeconds(stepDelay);

        if (result.IsBipartite)
        {
            SetStatus($"Bipartite | Graph is bipartite — rearranging nodes...");
            yield return StartCoroutine(AnimateBipartiteLayout(result.GroupA, result.GroupB));
            SetStatus($"Bipartite | Graph is bipartite. Group A: {result.GroupA.Count} node(s), Group B: {result.GroupB.Count} node(s).");
        }
        else
        {
            SetStatus("Bipartite | Graph is NOT bipartite — highlighting conflict edge...");
            yield return new WaitForSeconds(stepDelay);

            if (result.ConflictEdge != null)
                result.ConflictEdge.AnimateVisited(selectionManager.conflictEdgeMaterial);

            SetStatus("Bipartite | Graph is NOT bipartite. Same-color nodes are connected — odd cycle detected.");
        }

        _activeCoroutine = null;
    }

    private IEnumerator AnimateBipartiteLayout(List<GraphNode> groupA, List<GraphNode> groupB)
    {
        TweenGroupToColumn(groupA, -bipartiteSeparation);
        TweenGroupToColumn(groupB,  bipartiteSeparation);

        yield return StartCoroutine(UpdateEdgesDuring(bipartiteAnimDuration + 0.05f));
    }

    private void TweenGroupToColumn(List<GraphNode> group, float xPos)
    {
        int count = group.Count;
        for (int i = 0; i < count; i++)
        {
            float z      = (i - (count - 1) * 0.5f) * bipartiteNodeSpacing;
            var   target = new Vector3(xPos, 0f, z);
            group[i].transform.DOMove(target, bipartiteAnimDuration).SetEase(Ease.OutCubic);
        }
    }

    private IEnumerator UpdateEdgesDuring(float duration)
    {
        float elapsed = 0f;
        var   edges   = FindObjectsByType<GraphEdge>(FindObjectsSortMode.None);
        while (elapsed < duration)
        {
            foreach (var e in edges) e.UpdateTransform();
            elapsed += Time.deltaTime;
            yield return null;
        }
        foreach (var e in edges) e.UpdateTransform();
    }

    #endregion

    #region Helpers

    private void SetStatus(string message)
    {
        if (statusText != null) statusText.text = message;
    }

    #endregion
}
