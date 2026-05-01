using System.Collections.Generic;
using UnityEngine;

public class GraphGenerator : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] GraphNode nodePrefab;
    [SerializeField] GraphEdge edgePrefab;

    [Header("Graph Settings")]
    [SerializeField] int nodeCount = 8;
    [SerializeField] float spawnRadius = 5f;
    [SerializeField] float minNodeDistance = 1.5f;
    [SerializeField, Range(0f, 1f)] float edgeProbability = 0.4f;
    [SerializeField] int maxDegree = 4;

    [ContextMenu("Generate Graph")]
    public void GenerateGraph()
    {
        ClearGraph();
        var nodes = SpawnNodes();
        ConnectNodes(nodes);
    }

    [ContextMenu("Clear Graph")]
    public void ClearGraph()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);
    }

    private List<GraphNode> SpawnNodes()
    {
        var nodes = new List<GraphNode>(nodeCount);
        var positions = new List<Vector3>(nodeCount);
        const int maxAttempts = 100;

        for (int i = 0; i < nodeCount; i++)
        {
            Vector3 pos = Vector3.zero;
            bool placed = false;

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
            }
        }
    }
}
