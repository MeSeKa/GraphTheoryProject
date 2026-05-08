using System.Collections.Generic;
using UnityEngine;

public class GraphGenerator : MonoBehaviour
{
    #region Fields

    [Header("Prefabs")]
    [SerializeField] GraphNode nodePrefab;
    [SerializeField] GraphEdge edgePrefab;

    [Header("References")]
    [SerializeField] SelectionManager selectionManager;

    [Header("Random Graph Settings")]
    [SerializeField] public int   nodeCount       = 8;
    [SerializeField] public float spawnRadius     = 5f;
    [SerializeField] public float minNodeDistance = 1.5f;
    [SerializeField, Range(0f, 1f)] public float edgeProbability = 0.4f;
    [SerializeField] public int maxDegree = 4;

    [Header("Game Mode Settings")]
    [SerializeField] public EdgeType[] allowedEdgeTypes;
    [SerializeField] public bool       ensureConnectivity;

    [Header("Bipartite Graph Settings")]
    [SerializeField] int bipartiteGroupACount = 4;
    [SerializeField] int bipartiteGroupBCount = 4;
    [SerializeField] float bipartiteSideOffset = 3f;
    [SerializeField] float bipartiteScatter = 1.5f; // random spread within each group's zone

    #endregion

    #region Random Graph

    [ContextMenu("Generate Graph")]
    public (GraphNode source, GraphNode destination) GenerateGraph()
    {
        ClearGraph();
        var nodes = SpawnNodes();
        ConnectNodes(nodes);
        if (ensureConnectivity && nodes.Count >= 2) EnsureConnectivity(nodes);
        return (nodes.Count > 0 ? nodes[0] : null,
                nodes.Count > 1 ? nodes[nodes.Count - 1] : null);
    }

    private List<GraphNode> SpawnNodes()
    {
        var nodes     = new List<GraphNode>(nodeCount);
        var positions = new List<Vector3>(nodeCount);
        const int maxAttempts = 100;

        for (int i = 0; i < nodeCount; i++)
        {
            Vector3 pos    = Vector3.zero;
            bool    placed = false;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                Vector2 rand = Random.insideUnitCircle * spawnRadius;
                pos = new Vector3(rand.x, 0f, rand.y);

                bool tooClose = false;
                foreach (var p in positions)
                {
                    if (Vector3.Distance(pos, p) < minNodeDistance) { tooClose = true; break; }
                }

                if (!tooClose) { placed = true; break; }
            }

            if (!placed)
                Debug.LogWarning($"Node {i} için uygun konum bulunamadı, spawnRadius veya minNodeDistance'ı ayarla.");

            positions.Add(pos);
            nodes.Add(Instantiate(nodePrefab, pos, Quaternion.identity, transform));
        }

        return nodes;
    }

    private void ConnectNodes(List<GraphNode> nodes)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            for (int j = i + 1; j < nodes.Count; j++)
            {
                if (nodes[i].edges.Count >= maxDegree) break;
                if (nodes[j].edges.Count >= maxDegree) continue;
                if (Random.value > edgeProbability) continue;

                GraphEdge edge = Instantiate(edgePrefab, transform);
                edge.Setup(nodes[i], nodes[j]);
                AssignEdgeType(edge);
            }
        }
    }

    private void AssignEdgeType(GraphEdge edge)
    {
        if (selectionManager == null || allowedEdgeTypes == null || allowedEdgeTypes.Length == 0) return;
        EdgeType type = allowedEdgeTypes[Random.Range(0, allowedEdgeTypes.Length)];
        edge.AssignType(type, selectionManager.GetEdgeTypeMaterial(type));
    }

    private void EnsureConnectivity(List<GraphNode> nodes)
    {
        var connected = new HashSet<GraphNode> { nodes[0] };
        var remaining = new HashSet<GraphNode>(nodes);
        remaining.Remove(nodes[0]);

        while (remaining.Count > 0)
        {
            GraphNode bestC = null, bestR = null;
            float bestDist = float.MaxValue;

            foreach (var c in connected)
            {
                foreach (var r in remaining)
                {
                    float d = Vector3.Distance(c.transform.position, r.transform.position);
                    if (d < bestDist) { bestDist = d; bestC = c; bestR = r; }
                }
            }

            bool alreadyLinked = bestR.edges.Exists(e => e.GetOtherNode(bestR) == bestC);
            if (!alreadyLinked)
            {
                GraphEdge edge = Instantiate(edgePrefab, transform);
                edge.Setup(bestC, bestR);
                AssignEdgeType(edge);
            }

            connected.Add(bestR);
            remaining.Remove(bestR);
        }
    }

    #endregion

    #region Bipartite Graph

    [ContextMenu("Generate Bipartite Graph")]
    public void GenerateBipartiteGraph()
    {
        ClearGraph();
        var groupA = SpawnGroupNodes(bipartiteGroupACount, -bipartiteSideOffset);
        var groupB = SpawnGroupNodes(bipartiteGroupBCount,  bipartiteSideOffset);
        ConnectBipartiteGroups(groupA, groupB);
    }

    private List<GraphNode> SpawnGroupNodes(int count, float xCenter)
    {
        var nodes     = new List<GraphNode>(count);
        var positions = new List<Vector3>(count);
        const int maxAttempts = 100;

        for (int i = 0; i < count; i++)
        {
            Vector3 pos    = Vector3.zero;
            bool    placed = false;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                float x = xCenter + Random.Range(-bipartiteScatter, bipartiteScatter);
                float z = Random.Range(-spawnRadius, spawnRadius);
                pos = new Vector3(x, 0f, z);

                bool tooClose = false;
                foreach (var p in positions)
                {
                    if (Vector3.Distance(pos, p) < minNodeDistance) { tooClose = true; break; }
                }

                if (!tooClose) { placed = true; break; }
            }

            if (!placed)
                Debug.LogWarning($"Bipartite node {i} için uygun konum bulunamadı.");

            positions.Add(pos);
            nodes.Add(Instantiate(nodePrefab, pos, Quaternion.identity, transform));
        }

        return nodes;
    }

    private void ConnectBipartiteGroups(List<GraphNode> groupA, List<GraphNode> groupB)
    {
        foreach (var a in groupA)
        {
            foreach (var b in groupB)
            {
                if (a.edges.Count >= maxDegree) break;
                if (b.edges.Count >= maxDegree) continue;
                if (Random.value > edgeProbability) continue;

                GraphEdge edge = Instantiate(edgePrefab, transform);
                edge.Setup(a, b);
                AssignEdgeType(edge);
            }
        }
    }

    #endregion

    #region Helpers

    [ContextMenu("Clear Graph")]
    public void ClearGraph()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);
    }

    #endregion
}
