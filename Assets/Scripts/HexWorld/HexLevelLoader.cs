using System.Collections.Generic;
using UnityEngine;

public class HexLevelLoader : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] HexTile   tilePrefab;
    [SerializeField] HexBridge ropeBridgePrefab;
    [SerializeField] HexBridge woodBridgePrefab;
    [SerializeField] HexBridge stoneBridgePrefab;

    [Header("Settings")]
    [SerializeField] public float hexSize = 4f;

    [Header("Materials (source / destination)")]
    [SerializeField] Material sourceTileMaterial;
    [SerializeField] Material destinationTileMaterial;

    // Loaded at runtime — read by HexGameManager
    public HexTile SourceTile      { get; private set; }
    public HexTile DestinationTile { get; private set; }

    private readonly Dictionary<Vector2Int, HexTile> _tileMap = new();

    public List<HexTile> LoadLevel(HexLevelData data)
    {
        ClearLevel();

        // ── Spawn tiles ──
        foreach (var entry in data.tiles)
        {
            Vector3 worldPos = HexGrid.AxialToWorld(entry.q, entry.r, hexSize);
            var tile = Instantiate(tilePrefab, worldPos, Quaternion.identity, transform);
            tile.q = entry.q;
            tile.r = entry.r;
            tile.name = $"Tile({entry.q},{entry.r})";
            _tileMap[new Vector2Int(entry.q, entry.r)] = tile;
        }

        // Source / destination materials
        var srcKey  = data.sourceTile;
        var dstKey  = data.destinationTile;
        if (_tileMap.TryGetValue(srcKey,  out var src))  { SourceTile = src;  src.SetMaterial(sourceTileMaterial); }
        if (_tileMap.TryGetValue(dstKey,  out var dst))  { DestinationTile = dst; dst.SetMaterial(destinationTileMaterial); }

        // ── Spawn bridges ──
        foreach (var entry in data.bridges)
        {
            var keyA = new Vector2Int(entry.q1, entry.r1);
            var keyB = new Vector2Int(entry.q2, entry.r2);
            if (!_tileMap.TryGetValue(keyA, out var tA) || !_tileMap.TryGetValue(keyB, out var tB)) continue;

            var prefab = BridgePrefabFor(entry.edgeType);
            if (prefab == null) continue;

            var bridge = Instantiate(prefab, transform);
            bridge.name = $"Bridge({entry.q1},{entry.r1})-({entry.q2},{entry.r2})";
            // Material will be assigned by HexGameManager after load
            bridge.Initialize(tA, tB, entry.edgeType, null);
        }

        return new List<HexTile>(_tileMap.Values);
    }

    public void ClearLevel()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);
        _tileMap.Clear();
        SourceTile      = null;
        DestinationTile = null;
    }

    HexBridge BridgePrefabFor(EdgeType type) => type switch
    {
        EdgeType.Rope  => ropeBridgePrefab,
        EdgeType.Wood  => woodBridgePrefab,
        EdgeType.Stone => stoneBridgePrefab,
        _              => ropeBridgePrefab
    };
}
