using System.Collections.Generic;
using UnityEngine;
using SPACE_UTIL;

/// <summary>
/// Singleton that owns the Board&lt;Tile&gt; and is the single source of truth
/// for all building placement, movement and removal.
///
/// Usage:
///   GridManager.I.CanPlace(so, pivot, rot)
///   GridManager.I.Place(so, pivot, rot)
///   GridManager.I.Move(instance, newPivot, newRot)
///   GridManager.I.Remove(instance)
/// </summary>
public class GridManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────
    public static GridManager I { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Grid Setup")]
    public v2     boardSize   = new v2(20, 20);
    public float  cellSize    = 1f;
    public Vector3 worldOrigin = Vector3.zero;

    [Header("Prefab Root")]
    [Tooltip("All BuildingInstance roots will be parented here.")]
    public Transform buildingRoot;

    // ── State ─────────────────────────────────────────────────────────────────
    public Board<Tile> board { get; private set; }

    private List<BuildingInstance> _allInstances = new List<BuildingInstance>();
    public  IReadOnlyList<BuildingInstance> allInstances => _allInstances;

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;

        board = new Board<Tile>(boardSize, new Tile());

        // Board<T> shares the same default_val object for every cell by default —
        // we need distinct Tile instances per cell.
        for (int x = 0; x < boardSize.x; x++)
            for (int y = 0; y < boardSize.y; y++)
                board[x, y] = new Tile();
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  PUBLIC API
    // ═════════════════════════════════════════════════════════════════════════

    // ── 1. Query ──────────────────────────────────────────────────────────────

    /// <summary>True if all tiles the building would occupy are free and in-bounds.</summary>
    public bool CanPlace(BuildingSO buildingData, v2 pivotCoord, int rotation,
                         BuildingInstance ignoreInstance = null)
    {
        foreach (var offset in buildingData.GetRotatedOffsets(rotation))
        {
            v2 coord = pivotCoord + offset;
            if (!InBounds(coord))              return false;
            var tile = board[coord];
            if (tile.isOccupied && tile.occupant != ignoreInstance) return false;
            if (tile.type == TileType.Blocked) return false;
        }
        return true;
    }

    // ── 2. Place ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Spawns a building at <paramref name="pivotCoord"/> and marks the board.
    /// Returns null if placement is invalid.
    /// </summary>
    public BuildingInstance Place(BuildingSO buildingData, v2 pivotCoord, int rotation)
    {
        if (!CanPlace(buildingData, pivotCoord, rotation))
        {
            Debug.LogWarning($"[GridManager] Cannot place '{buildingData.buildingName}' " +
                             $"at {pivotCoord} rot={rotation}");
            return null;
        }

        // Spawn root GO
        var root = new GameObject($"Building_{buildingData.buildingName}_{pivotCoord}");
        root.transform.SetParent(buildingRoot != null ? buildingRoot : transform);

        var instance = root.AddComponent<BuildingInstance>();
        instance.Initialise(buildingData, pivotCoord, rotation, cellSize, worldOrigin);

        // Mark tiles
        MarkTiles(instance, occupy: true);
        _allInstances.Add(instance);

        BuildingEvents.FirePlaced(instance);
        return instance;
    }

    // ── 3. Remove ─────────────────────────────────────────────────────────────

    /// <summary>Removes a building, clears its tiles, destroys its GO.</summary>
    public void Remove(BuildingInstance instance)
    {
        if (instance == null) return;

        BuildingEvents.FireRemoved(instance);   // fire BEFORE destroy — data still readable
        MarkTiles(instance, occupy: false);
        _allInstances.Remove(instance);
        instance.Despawn();
        Destroy(instance.gameObject);
    }

    // ── 4a. Lift / Land (used by BuildingSelector for drag) ───────────────────

    /// <summary>
    /// Clears the building's footprint from the board without moving the GO.
    /// Call before dragging so CanPlace doesn't treat the building as blocking itself.
    /// Must be paired with LandOn().
    /// </summary>
    public void LiftOff(BuildingInstance instance)
    {
        if (instance == null) return;
        MarkTiles(instance, occupy: false);
    }

    /// <summary>
    /// Writes the building to a new position on the board and updates its visuals.
    /// Pair with LiftOff(). If the target is blocked this will force-place —
    /// validate with CanPlace first.
    /// </summary>
    public void LandOn(BuildingInstance instance, v2 newPivot, int newRotation)
    {
        if (instance == null) return;
        instance.ApplyNewPlacement(newPivot, newRotation, cellSize, worldOrigin);
        MarkTiles(instance, occupy: true);
    }

    // ── 4b. Atomic move (convenience, keeps old API working) ─────────────────

    /// <summary>
    /// Atomically validates + moves a building. Returns false if blocked.
    /// For drag-based movement use LiftOff / LandOn instead.
    /// </summary>
    public bool Move(BuildingInstance instance, v2 newPivot, int newRotation)
    {
        if (instance == null) return false;

        MarkTiles(instance, occupy: false);

        if (!CanPlace(instance.data, newPivot, newRotation))
        {
            MarkTiles(instance, occupy: true);
            Debug.LogWarning($"[GridManager] Cannot move '{instance.data.buildingName}' " +
                             $"to {newPivot} rot={newRotation}");
            return false;
        }

        v2  oldPivot = instance.pivotCoord;
        int oldRot   = instance.rotation;

        instance.ApplyNewPlacement(newPivot, newRotation, cellSize, worldOrigin);
        MarkTiles(instance, occupy: true);

        BuildingEvents.FireMoved(instance, oldPivot, oldRot);
        return true;
    }

    // ── 5. Rotate in-place ────────────────────────────────────────────────────

    public bool Rotate(BuildingInstance instance, int deltaRotation = 1) =>
        Move(instance, instance.pivotCoord, instance.rotation + deltaRotation);

    // ═════════════════════════════════════════════════════════════════════════
    //  COORDINATE HELPERS
    // ═════════════════════════════════════════════════════════════════════════

    public bool InBounds(v2 coord) =>
        coord.x >= 0 && coord.y >= 0 && coord.x < boardSize.x && coord.y < boardSize.y;

    /// <summary>Converts a world-space point to the nearest grid coord.</summary>
    public v2 WorldToGrid(Vector3 worldPos)
    {
        Vector3 local = worldPos - worldOrigin;
        return new v2(
            Mathf.RoundToInt(local.x / cellSize),
            Mathf.RoundToInt(local.z / cellSize)   // XZ plane
        );
    }

    /// <summary>Converts a grid coord to the world-space centre of that tile.</summary>
    public Vector3 GridToWorld(v2 coord) =>
        worldOrigin + new Vector3(coord.x * cellSize, 0, coord.y * cellSize);

    // ═════════════════════════════════════════════════════════════════════════
    //  PRIVATE HELPERS
    // ═════════════════════════════════════════════════════════════════════════

    private void MarkTiles(BuildingInstance instance, bool occupy)
    {
        var partOffsetsByPart = instance.data.GetRotatedOffsetsByPart(instance.rotation);

        for (int partIdx = 0; partIdx < partOffsetsByPart.Count; partIdx++)
        {
            foreach (var offset in partOffsetsByPart[partIdx])
            {
                v2 coord = instance.pivotCoord + offset;
                if (!InBounds(coord)) continue;

                var tile = board[coord];
                if (occupy)
                {
                    tile.occupant          = instance;
                    tile.occupantPartIndex = partIdx;
                }
                else
                {
                    tile.Clear();
                }
            }
        }
    }

    // ── Debug visualisation ───────────────────────────────────────────────────
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (board == null) return;

        for (int x = 0; x < boardSize.x; x++)
        {
            for (int y = 0; y < boardSize.y; y++)
            {
                var tile = board[x, y];
                Gizmos.color = tile.isOccupied ? new Color(1f, 0.3f, 0.3f, 0.4f)
                                               : new Color(0.3f, 1f, 0.5f, 0.15f);
                Vector3 centre = GridToWorld((x, y));
                Gizmos.DrawCube(centre, new Vector3(cellSize * 0.95f, 0.02f, cellSize * 0.95f));
                Gizmos.color = Color.white * 0.3f;
                Gizmos.DrawWireCube(centre, new Vector3(cellSize, 0.02f, cellSize));
            }
        }
    }
#endif
}
