using UnityEngine;
using SPACE_UTIL;

/// <summary>
/// Data stored in each cell of Board&lt;Tile&gt;.
/// Keeps track of what building occupies it (if any).
/// </summary>
[System.Serializable]
public class Tile
{
    // ── Static state ──────────────────────────────────────────────────────────
    public TileType type = TileType.Empty;

    // ── Runtime occupancy ─────────────────────────────────────────────────────
    /// <summary>The building currently sitting on this tile (null = free).</summary>
    [System.NonSerialized] public BuildingInstance occupant = null;

    /// <summary>Which part index inside occupant.data.parts owns this tile.</summary>
    [System.NonSerialized] public int occupantPartIndex = -1;

    // ── Convenience ───────────────────────────────────────────────────────────
    public bool isOccupied  => occupant != null;
    public bool isWalkable  => !isOccupied && type != TileType.Blocked;

    public void Clear()
    {
        occupant          = null;
        occupantPartIndex = -1;
    }

    public override string ToString() =>
        isOccupied ? "X" : (type == TileType.Blocked ? "#" : ".");
}

public enum TileType
{
    Empty,
    Blocked,   // terrain obstacle, wall, etc.
    Road,      // future: pathfinding hint
}
