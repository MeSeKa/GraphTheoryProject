using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class HexLevelValidatorWindow : EditorWindow
{
    private HexLevelData _level;
    private ValidationResult _result;

    [MenuItem("HexWorld/Level Validator")]
    static void Open() => GetWindow<HexLevelValidatorWindow>("Level Validator");

    private void OnGUI()
    {
        EditorGUILayout.LabelField("HexWorld — Level Validator", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        _level = (HexLevelData)EditorGUILayout.ObjectField("Level Data", _level, typeof(HexLevelData), false);

        EditorGUI.BeginDisabledGroup(_level == null);
        if (GUILayout.Button("Validate")) _result = Validate(_level);
        EditorGUI.EndDisabledGroup();

        if (_result == null) return;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("── Sonuçlar ──", EditorStyles.boldLabel);

        // Min-cut
        EditorGUILayout.LabelField($"Min-Cut: {_result.MinCut}");

        // Source / Dest degree
        Color prev = GUI.color;
        GUI.color = _result.SourceDegree < 4 ? Color.red : Color.green;
        EditorGUILayout.LabelField($"Source degree: {_result.SourceDegree}" + (_result.SourceDegree < 4 ? "  ⚠ < 4" : "  ✓"));
        GUI.color = _result.DestDegree < 4 ? Color.red : Color.green;
        EditorGUILayout.LabelField($"Destination degree: {_result.DestDegree}" + (_result.DestDegree < 4 ? "  ⚠ < 4" : "  ✓"));
        GUI.color = prev;

        // Trivial cut uyarısı
        if (_result.HasTrivialCut)
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "TRIVIAL CUT DETECTED: Source veya Destination'ın komşularını keserek bölüm " +
                $"{_result.MinCut} hamlede bitirilebilir. Source/Dest degree'sini artır veya " +
                "çevre köprüleri pahalı (Stone/Metal) yap.",
                MessageType.Error);
        }
        else
        {
            EditorGUILayout.HelpBox("Trivial cut yok. Optimal cut interior'da.", MessageType.Info);
        }

        // Cut setleri
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("── Minimum Cut Setleri ──", EditorStyles.boldLabel);
        if (_result.CutSets.Count == 0)
        {
            EditorGUILayout.LabelField("(Bağlantı yok — source zaten izole)");
        }
        else
        {
            foreach (var set in _result.CutSets)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField($"Set ({set.Edges.Count} kesme):", EditorStyles.miniBoldLabel);
                foreach (var e in set.Edges)
                    EditorGUILayout.LabelField($"  [{e.q1},{e.r1}] → [{e.q2},{e.r2}]  ({e.edgeType})", EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();
            }
        }
    }

    // ── Validation Logic ──

    private static ValidationResult Validate(HexLevelData data)
    {
        var result = new ValidationResult();

        // Build adjacency from HexBridgeEntry list
        var src = data.sourceTile;
        var dst = data.destinationTile;

        // node id map
        var nodeIds = new Dictionary<Vector2Int, int>();
        for (int i = 0; i < data.tiles.Length; i++)
            nodeIds[new Vector2Int(data.tiles[i].q, data.tiles[i].r)] = i;

        int n = data.tiles.Length;
        int srcId = nodeIds.TryGetValue(src, out int s) ? s : -1;
        int dstId = nodeIds.TryGetValue(dst, out int d) ? d : -1;

        if (srcId < 0 || dstId < 0)
        {
            result.MinCut = -1;
            return result;
        }

        // Degree count
        result.SourceDegree = 0;
        result.DestDegree   = 0;
        foreach (var b in data.bridges)
        {
            var a1 = new Vector2Int(b.q1, b.r1);
            var a2 = new Vector2Int(b.q2, b.r2);
            if (a1 == src || a2 == src) result.SourceDegree++;
            if (a1 == dst || a2 == dst) result.DestDegree++;
        }

        // Edmonds-Karp max-flow → min-cut value
        // Unit capacity per edge (each bridge = 1 cut)
        int[,] cap = BuildCapacity(n, data.bridges, nodeIds);
        result.MinCut = MaxFlow(cap, n, srcId, dstId, out int[] parentFlow);

        // Trivial: min-cut ≤ min(srcDeg, dstDeg) AND one of them equals min-cut
        result.HasTrivialCut = result.MinCut > 0 &&
            (result.SourceDegree == result.MinCut || result.DestDegree == result.MinCut);

        // Enumerate all minimum cut sets (simple: BFS-reachable from src on residual)
        result.CutSets = FindCutSets(data, nodeIds, n, srcId, dstId, result.MinCut);

        return result;
    }

    private static int[,] BuildCapacity(int n, HexBridgeEntry[] bridges, Dictionary<Vector2Int, int> ids)
    {
        int[,] cap = new int[n, n];
        foreach (var b in bridges)
        {
            if (!ids.TryGetValue(new Vector2Int(b.q1, b.r1), out int u)) continue;
            if (!ids.TryGetValue(new Vector2Int(b.q2, b.r2), out int v)) continue;
            cap[u, v]++;
            cap[v, u]++;
        }
        return cap;
    }

    private static int MaxFlow(int[,] cap, int n, int s, int t, out int[] lastParent)
    {
        int[,] residual = (int[,])cap.Clone();
        int flow = 0;
        lastParent = new int[n];

        while (BFS(residual, n, s, t, out int[] parent))
        {
            // Find min capacity along path
            int pathFlow = int.MaxValue;
            for (int v = t; v != s; v = parent[v])
                pathFlow = Mathf.Min(pathFlow, residual[parent[v], v]);

            // Update residual
            for (int v = t; v != s; v = parent[v])
            {
                residual[parent[v], v] -= pathFlow;
                residual[v, parent[v]] += pathFlow;
            }
            flow += pathFlow;
            lastParent = parent;
        }

        // Final BFS to get reachable set for cut identification
        BFS(residual, n, s, t, out lastParent);
        return flow;
    }

    private static bool BFS(int[,] residual, int n, int s, int t, out int[] parent)
    {
        parent = new int[n];
        for (int i = 0; i < n; i++) parent[i] = -1;
        parent[s] = s;
        var queue = new Queue<int>();
        queue.Enqueue(s);
        while (queue.Count > 0)
        {
            int u = queue.Dequeue();
            for (int v = 0; v < n; v++)
            {
                if (parent[v] == -1 && residual[u, v] > 0)
                {
                    parent[v] = u;
                    if (v == t) return true;
                    queue.Enqueue(v);
                }
            }
        }
        return false;
    }

    private static List<CutSet> FindCutSets(HexLevelData data, Dictionary<Vector2Int, int> nodeIds,
        int n, int srcId, int dstId, int minCut)
    {
        var sets = new List<CutSet>();

        // Get reachable set from src on final residual graph
        int[,] cap = BuildCapacity(n, data.bridges, nodeIds);
        MaxFlow(cap, n, srcId, dstId, out _);

        // Re-run to get final residual
        int[,] residual = BuildCapacity(n, data.bridges, nodeIds);
        BFS_Reachable(residual, n, srcId, dstId, out bool[] reachable);

        // Cut edges: u reachable, v not reachable
        var cut = new CutSet();
        foreach (var b in data.bridges)
        {
            if (!nodeIds.TryGetValue(new Vector2Int(b.q1, b.r1), out int u)) continue;
            if (!nodeIds.TryGetValue(new Vector2Int(b.q2, b.r2), out int v)) continue;
            bool uReach = reachable[u];
            bool vReach = reachable[v];
            if ((uReach && !vReach) || (!uReach && vReach))
                cut.Edges.Add(b);
        }
        if (cut.Edges.Count > 0) sets.Add(cut);

        return sets;
    }

    private static void BFS_Reachable(int[,] residual, int n, int s, int t, out bool[] reachable)
    {
        // Run max-flow first to get final residual state
        MaxFlow(residual, n, s, t, out _);

        reachable = new bool[n];
        var queue = new Queue<int>();
        reachable[s] = true;
        queue.Enqueue(s);
        while (queue.Count > 0)
        {
            int u = queue.Dequeue();
            for (int v = 0; v < n; v++)
            {
                if (!reachable[v] && residual[u, v] > 0)
                {
                    reachable[v] = true;
                    queue.Enqueue(v);
                }
            }
        }
    }

    // ── Data Classes ──

    private class ValidationResult
    {
        public int          MinCut;
        public int          SourceDegree;
        public int          DestDegree;
        public bool         HasTrivialCut;
        public List<CutSet> CutSets = new();
    }

    private class CutSet
    {
        public List<HexBridgeEntry> Edges = new();
    }
}
