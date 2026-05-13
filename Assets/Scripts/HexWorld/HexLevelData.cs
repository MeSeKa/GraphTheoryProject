using UnityEngine;

public enum HexTileType { Default, Grass, Stone, Sand }

[System.Serializable]
public struct HexTileEntry
{
    public int         q, r;
    public HexTileType tileType;
}

[System.Serializable]
public struct HexBridgeEntry
{
    public int      q1, r1;
    public int      q2, r2;
    public EdgeType edgeType;
}

[CreateAssetMenu(fileName = "HexLevel", menuName = "HexWorld/Level Data")]
public class HexLevelData : ScriptableObject
{
    [Header("Info")]
    public string      levelName   = "Level 1";
    public int         levelNumber = 1;
    public HexTileType tileType    = HexTileType.Grass;

    [Header("Tool Inventory")]
    public int axeCount;
    public int pickaxeCount;
    public int ironShearsCount;
    public int bombCount;

    [Header("Economy")]
    public int startingGold = 0;

    [Header("Price Discounts (%)  0 = no discount")]
    [Range(0, 100)] public int axeDiscount;
    [Range(0, 100)] public int pickaxeDiscount;
    [Range(0, 100)] public int ironShearsDiscount;
    [Range(0, 100)] public int bombDiscount;

    [Header("Grid")]
    public HexTileEntry[]   tiles;
    public HexBridgeEntry[] bridges;

    [Header("Source / Destination")]
    public Vector2Int sourceTile;
    public Vector2Int destinationTile;
}
