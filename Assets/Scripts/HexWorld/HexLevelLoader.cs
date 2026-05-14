using System.Collections.Generic;
using UnityEngine;

public class HexLevelLoader : MonoBehaviour
{
    [Header("Tile Prefabs (by theme)")]
    [SerializeField] HexTile grassTilePrefab;
    [SerializeField] HexTile stoneTilePrefab;
    [SerializeField] HexTile sandTilePrefab;

    [Header("Special Tile Prefabs")]
    [SerializeField] HexTile sourceTilePrefab;
    [SerializeField] HexTile destinationTilePrefab;

    [Header("Bridge Prefabs")]
    [SerializeField] HexBridge woodBridgePrefab;
    [SerializeField] HexBridge stoneBridgePrefab;
    [SerializeField] HexBridge metalBridgePrefab;
    [SerializeField] HexBridge unbreakableBridgePrefab;

    [Header("Settings")]
    [SerializeField] public float hexSize = 4f;

    [Header("Fallback Materials (if no special prefab)")]
    [SerializeField] Material sourceTileMaterial;
    [SerializeField] Material destinationTileMaterial;

    public HexTile SourceTile      { get; private set; }
    public HexTile DestinationTile { get; private set; }

    private readonly Dictionary<Vector2Int, HexTile> _tileMap = new();

    public List<HexTile> LoadLevel(HexLevelData data)
    {
        ClearLevel();

        var srcKey = data.sourceTile;
        var dstKey = data.destinationTile;

        foreach (var entry in data.tiles)
        {
            var key = new Vector2Int(entry.q, entry.r);
            Vector3 worldPos = HexGrid.AxialToWorld(entry.q, entry.r, hexSize);

            HexTile prefab;
            if (key == srcKey && sourceTilePrefab != null)
                prefab = sourceTilePrefab;
            else if (key == dstKey && destinationTilePrefab != null)
                prefab = destinationTilePrefab;
            else
                prefab = TilePrefabFor(ResolveType(entry.tileType, data.tileType));

            var tile = Instantiate(prefab, worldPos, Quaternion.identity, transform);
            tile.q    = entry.q;
            tile.r    = entry.r;
            tile.name = $"Tile({entry.q},{entry.r})";
            _tileMap[key] = tile;
        }

        if (_tileMap.TryGetValue(srcKey, out var src))
        {
            SourceTile = src;
            if (sourceTilePrefab == null) src.SetMaterial(sourceTileMaterial);
        }
        if (_tileMap.TryGetValue(dstKey, out var dst))
        {
            DestinationTile = dst;
            if (destinationTilePrefab == null) dst.SetMaterial(destinationTileMaterial);
        }

        foreach (var entry in data.bridges)
        {
            var keyA = new Vector2Int(entry.q1, entry.r1);
            var keyB = new Vector2Int(entry.q2, entry.r2);
            if (!_tileMap.TryGetValue(keyA, out var tA) || !_tileMap.TryGetValue(keyB, out var tB)) continue;

            var prefab = BridgePrefabFor(entry.edgeType);
            if (prefab == null) continue;

            var bridge = Instantiate(prefab, transform);
            bridge.name = $"Bridge({entry.q1},{entry.r1})-({entry.q2},{entry.r2})";
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

    static HexTileType ResolveType(HexTileType perTile, HexTileType levelDefault) =>
        perTile == HexTileType.Default ? levelDefault : perTile;

    HexTile TilePrefabFor(HexTileType type) => type switch
    {
        HexTileType.Stone => stoneTilePrefab ? stoneTilePrefab : grassTilePrefab,
        HexTileType.Sand  => sandTilePrefab  ? sandTilePrefab  : grassTilePrefab,
        _                 => grassTilePrefab  // Grass veya Default (resolve edilmiş olmalı)
    };

    HexBridge BridgePrefabFor(EdgeType type) => type switch
    {
        EdgeType.Stone       => stoneBridgePrefab,
        EdgeType.Metal       => metalBridgePrefab,
        EdgeType.Unbreakable => unbreakableBridgePrefab ? unbreakableBridgePrefab : metalBridgePrefab,
        _                    => woodBridgePrefab
    };
}
