using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class GraphManager : MonoBehaviour
{
    #region Fields

    [SerializeField] SelectionManager selectionManager;
    [SerializeField] CameraFramer     cameraFramer;
    [SerializeField] GraphGenerator   graphGenerator;
    [SerializeField] ToolManager      toolManager;
    [SerializeField] LevelManager     levelManager;

    [Header("UI Panels")]
    [SerializeField] GameObject mainButtonPanel;

    [Header("UI Buttons — Main Panel")]
    [SerializeField] Button gameModeButton;
    [SerializeField] Button bfsButton;
    [SerializeField] Button dfsButton;
    [SerializeField] Button startButton;
    [SerializeField] Button generateButton;
    [SerializeField] Button generateBipartiteButton;
    [SerializeField] Button bipartiteCheckButton;
    [SerializeField] Button cutEdgeButton;

    [Header("UI Buttons — Cut Edge Mode")]
    [SerializeField] Button exitCutEdgeModeButton;

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
    private bool      _cutEdgeModeActive;
    private bool      _gameModeActive;

    #endregion

    #region Lifecycle

    private void Start()
    {
        gameModeButton?.onClick.AddListener(() => levelManager.ShowLevelSelect());

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

        cutEdgeButton.onClick.AddListener(EnterCutEdgeMode);
        exitCutEdgeModeButton.onClick.AddListener(ExitCutEdgeMode);

        exitCutEdgeModeButton.gameObject.SetActive(false);
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

        ResetGraph();
        source.SetMaterial(selectionManager.sourceNodeMaterial);
        destination.SetMaterial(selectionManager.destinationNodeMaterial);

        Pathfinder.PathfinderResult result = SelectedAlgorithm == Algorithm.BFS
            ? Pathfinder.BFS(source, destination)
            : Pathfinder.DFS(source, destination);

        BeginCoroutine(RunTraversal(result, source, destination));
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
            EndCoroutine();
            yield break;
        }

        SetStatus($"{SelectedAlgorithm} | Path found — tracing back...");
        yield return StartCoroutine(ShowPath(result.PathSteps, source, destination));

        SetStatus($"{SelectedAlgorithm} | Done. Path length: {result.PathSteps.Count} edge(s).");
        EndCoroutine();
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
        ResetGraph();
        var allNodes = FindObjectsByType<GraphNode>(FindObjectsSortMode.None);
        var result   = Pathfinder.CheckBipartite(allNodes);

        BeginCoroutine(RunBipartiteCheck(result));
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
            SetStatus("Bipartite | Graph is bipartite — rearranging nodes...");
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

        EndCoroutine();
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

    #region Game Mode

    public void EnterGameMode()
    {
        _gameModeActive = true;
        selectionManager.SelectionEnabled = false;
        mainButtonPanel.SetActive(false);
    }

    public void ExitGameMode()
    {
        _gameModeActive = false;
        selectionManager.SelectionEnabled = true;
        mainButtonPanel.SetActive(true);
        ResetGraph();
    }

    #endregion

    #region Cut Edge Mode

    private void Update()
    {
        if      (_gameModeActive)    HandleGameModeClick();
        else if (_cutEdgeModeActive) HandleCutEdgeModeClick();
    }

    private void HandleCutEdgeModeClick()
    {
        var mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;

        Ray ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit)) return;

        GraphEdge edge = hit.collider.GetComponent<GraphEdge>();
        if (edge != null) CutEdge(edge);
    }

    private void HandleGameModeClick()
    {
        var mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;

        Ray ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit)) return;

        GraphEdge edge = hit.collider.GetComponent<GraphEdge>();
        if (edge == null) return;

        if (toolManager.CanCut(edge.edgeType))
        {
            CutEdge(edge);
            levelManager.CheckWinCondition();
        }
        else
        {
            StartCoroutine(FlashEdgeError(edge));
            SetStatus($"Wrong tool! {edge.edgeType} edges require {GetRequiredTool(edge.edgeType)}.");
        }
    }

    private IEnumerator FlashEdgeError(GraphEdge edge)
    {
        edge.SetMaterial(selectionManager.errorEdgeMaterial);
        yield return new WaitForSeconds(0.4f);
        if (edge != null) edge.RestoreTypeMaterial();
    }

    private static ToolType GetRequiredTool(EdgeType edgeType) => edgeType switch
    {
        EdgeType.Rope  => ToolType.Scissors,
        EdgeType.Wood  => ToolType.Axe,
        EdgeType.Stone => ToolType.Bomb,
        _              => ToolType.Scissors
    };

    private void EnterCutEdgeMode()
    {
        _cutEdgeModeActive = true;
        selectionManager.SelectionEnabled = false;
        mainButtonPanel.SetActive(false);
        exitCutEdgeModeButton.gameObject.SetActive(true);
        SetStatus("Cut Edge | Click an edge to remove it.");
    }

    private void ExitCutEdgeMode()
    {
        _cutEdgeModeActive = false;
        selectionManager.SelectionEnabled = true;
        mainButtonPanel.SetActive(true);
        exitCutEdgeModeButton.gameObject.SetActive(false);
        SetStatus("Cut Edge mode exited.");
    }

    private void CutEdge(GraphEdge edge)
    {
        if (edge.nodeA != null) edge.nodeA.edges.Remove(edge);
        if (edge.nodeB != null) edge.nodeB.edges.Remove(edge);
        SetStatus("Cut Edge | Edge removed.");
        Destroy(edge.gameObject);
    }

    #endregion

    #region Helpers

    // Starts a coroutine and disables cut edge button for its duration
    private void BeginCoroutine(IEnumerator routine)
    {
        if (_activeCoroutine != null) StopCoroutine(_activeCoroutine);
        cutEdgeButton.interactable = false;
        _activeCoroutine = StartCoroutine(routine);
    }

    // Called at the end of each managed coroutine
    private void EndCoroutine()
    {
        _activeCoroutine = null;
        cutEdgeButton.interactable = true;
    }

    private void SetStatus(string message)
    {
        if (statusText != null) statusText.text = message;
    }

    #endregion
}
