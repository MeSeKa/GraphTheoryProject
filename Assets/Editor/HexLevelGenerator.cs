using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Solvable HexLevel üreten generator.
// Yaklaşım: Hex gridi sol/sağ olarak ikiye böl, tam cutSize adet
// kesme kenarı ekle (garantili min-cut), her partisyon içinde
// spanning tree + ek kenarlarla bağlantı sağla.
public static class HexLevelGenerator
{
    public class Params
    {
        public int    radius              = 3;
        public int    cutSize             = 2;
        public float  woodRatio           = 0.5f;
        public float  stoneRatio          = 0.3f;
        public float  metalRatio          = 0.2f;
        public float  internalDensity     = 0.4f;
        public bool   internalUnbreakable = false;
        public int    extraTools          = 0;
        public int    bombCount           = 0;
        public int    startingGold        = 500;
        public string levelName           = "Generated Level";
        public int    levelNumber         = 1;
    }

    public static HexLevelData Generate(Params p)
    {
        var rng = new System.Random();
        int R = Mathf.Max(1, p.radius);

        var allTiles = GetHexGrid(R);
        Vector2Int src = new(-R, 0);
        Vector2Int dst = new( R, 0);

        var leftSet  = new HashSet<Vector2Int>(allTiles.Where(t => t.x <= 0));
        var rightSet = new HashSet<Vector2Int>(allTiles.Where(t => t.x >  0));

        // Cross-boundary edge candidates (left q=0 ↔ right q=1)
        var crossPool = new List<(Vector2Int a, Vector2Int b)>();
        foreach (var tile in leftSet)
            foreach (var d in HexGrid.Neighbours)
            {
                var nb = tile + d;
                if (rightSet.Contains(nb)) crossPool.Add((tile, nb));
            }

        Shuffle(crossPool, rng);
        int cutCount = Mathf.Min(p.cutSize, crossPool.Count);
        var cutEdges = crossPool.Take(cutCount).ToList();

        // Connectivity within each partition
        var leftEdges  = SpanningTree(leftSet,  src, rng);
        var rightEdges = SpanningTree(rightSet, dst, rng);
        leftEdges .AddRange(ExtraEdges(leftSet,  leftEdges,  p.internalDensity, rng));
        rightEdges.AddRange(ExtraEdges(rightSet, rightEdges, p.internalDensity, rng));

        // Assign bridge types to cut edges
        float total = Mathf.Max(p.woodRatio + p.stoneRatio + p.metalRatio, 0.001f);
        int axe = 0, pick = 0, shears = 0;
        var bridges = new List<HexBridgeEntry>();

        foreach (var (a, b) in cutEdges)
        {
            var type = RollEdgeType(p.woodRatio, p.stoneRatio, p.metalRatio, total, rng);
            if      (type == EdgeType.Wood)  axe++;
            else if (type == EdgeType.Stone) pick++;
            else                             shears++;
            bridges.Add(MakeBridge(a, b, type));
        }

        // Internal bridges
        foreach (var (a, b) in leftEdges.Concat(rightEdges))
        {
            var type = p.internalUnbreakable
                ? EdgeType.Unbreakable
                : RollEdgeType(0.65f, 0.25f, 0.10f, 1f, rng);
            bridges.Add(MakeBridge(a, b, type));
        }

        var data = ScriptableObject.CreateInstance<HexLevelData>();
        data.levelName       = p.levelName;
        data.levelNumber     = p.levelNumber;
        data.tileType        = HexTileType.Grass;
        data.startingGold    = p.startingGold;
        data.sourceTile      = src;
        data.destinationTile = dst;
        data.tiles = allTiles
            .Select(t => new HexTileEntry { q = t.x, r = t.y, tileType = HexTileType.Default })
            .ToArray();
        data.bridges         = bridges.ToArray();
        data.axeCount        = axe    + (p.extraTools >= 1 ? 1 : 0);
        data.pickaxeCount    = pick   + (p.extraTools >= 2 ? 1 : 0);
        data.ironShearsCount = shears + (p.extraTools >= 3 ? 1 : 0);
        data.bombCount       = p.bombCount;

        return data;
    }

    // ── Public Helpers ──

    public static List<Vector2Int> GetHexGrid(int radius)
    {
        var result = new List<Vector2Int>();
        for (int q = -radius; q <= radius; q++)
        {
            int rMin = Mathf.Max(-radius, -q - radius);
            int rMax = Mathf.Min( radius, -q + radius);
            for (int r = rMin; r <= rMax; r++)
                result.Add(new Vector2Int(q, r));
        }
        return result;
    }

    // ── Private Helpers ──

    static List<(Vector2Int, Vector2Int)> SpanningTree(
        HashSet<Vector2Int> tiles, Vector2Int start, System.Random rng)
    {
        var edges    = new List<(Vector2Int, Vector2Int)>();
        var visited  = new HashSet<Vector2Int> { start };
        var frontier = new List<(Vector2Int from, Vector2Int to)>();

        AddFrontier(start, tiles, visited, frontier);

        while (frontier.Count > 0 && visited.Count < tiles.Count)
        {
            int i = rng.Next(frontier.Count);
            var (from, to) = frontier[i];
            frontier.RemoveAt(i);
            if (visited.Contains(to)) continue;
            visited.Add(to);
            edges.Add((from, to));
            AddFrontier(to, tiles, visited, frontier);
        }
        return edges;
    }

    static void AddFrontier(Vector2Int tile, HashSet<Vector2Int> tiles,
        HashSet<Vector2Int> visited, List<(Vector2Int, Vector2Int)> frontier)
    {
        foreach (var d in HexGrid.Neighbours)
        {
            var nb = tile + d;
            if (tiles.Contains(nb) && !visited.Contains(nb))
                frontier.Add((tile, nb));
        }
    }

    static List<(Vector2Int, Vector2Int)> ExtraEdges(
        HashSet<Vector2Int> tiles,
        List<(Vector2Int, Vector2Int)> existing,
        float density, System.Random rng)
    {
        var seen = new HashSet<(Vector2Int, Vector2Int)>();
        foreach (var (a, b) in existing) { seen.Add((a, b)); seen.Add((b, a)); }

        var extras = new List<(Vector2Int, Vector2Int)>();
        foreach (var tile in tiles)
            foreach (var d in HexGrid.Neighbours)
            {
                var nb = tile + d;
                if (tiles.Contains(nb) && !seen.Contains((tile, nb)) && rng.NextDouble() < density)
                {
                    extras.Add((tile, nb));
                    seen.Add((tile, nb));
                    seen.Add((nb, tile));
                }
            }
        return extras;
    }

    static EdgeType RollEdgeType(float wood, float stone, float metal, float total, System.Random rng)
    {
        double roll = rng.NextDouble() * total;
        if (roll < wood)              return EdgeType.Wood;
        if (roll < wood + stone)      return EdgeType.Stone;
        return EdgeType.Metal;
    }

    static HexBridgeEntry MakeBridge(Vector2Int a, Vector2Int b, EdgeType type) =>
        new() { q1 = a.x, r1 = a.y, q2 = b.x, r2 = b.y, edgeType = type };

    static void Shuffle<T>(List<T> list, System.Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
