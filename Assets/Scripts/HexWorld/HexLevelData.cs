using UnityEngine;

[System.Serializable]
public struct HexTileEntry
{
    public int q, r;
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
    public string levelName   = "Level 1";
    public int    levelNumber = 1;

    [Header("Tool Inventory")]
    public int scissorsCount;
    public int axeCount;
    public int bombCount;
    public int jokerCount;

    [Header("Grid")]
    public HexTileEntry[]   tiles;
    public HexBridgeEntry[] bridges;

    [Header("Source / Destination")]
    public Vector2Int sourceTile;
    public Vector2Int destinationTile;
}
