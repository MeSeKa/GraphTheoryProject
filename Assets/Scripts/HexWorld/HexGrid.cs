using UnityEngine;

// Pointy-top axial hex grid math.
// hexSize = distance between adjacent tile centres (island radius + gap).
public static class HexGrid
{
    public static Vector3 AxialToWorld(int q, int r, float hexSize)
    {
        float x = hexSize * (Mathf.Sqrt(3f) * q + Mathf.Sqrt(3f) / 2f * r);
        float z = hexSize * (1.5f * r);
        return new Vector3(x, 0f, z);
    }

    // Six pointy-top axial neighbour directions.
    public static readonly Vector2Int[] Neighbours =
    {
        new(1,  0), new(1, -1), new(0, -1),
        new(-1, 0), new(-1, 1), new(0,  1)
    };

    public static bool AreAdjacent(int q1, int r1, int q2, int r2)
    {
        int dq = q2 - q1, dr = r2 - r1;
        foreach (var n in Neighbours)
            if (n.x == dq && n.y == dr) return true;
        return false;
    }
}
