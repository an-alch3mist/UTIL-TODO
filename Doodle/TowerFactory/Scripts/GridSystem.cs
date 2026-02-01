using System.Collections.Generic;
using UnityEngine;

using SPACE_UTIL;

/// <summary>
/// Core grid system for building placement, pathfinding, and tile management.
/// Uses v2 (2D integer coordinates) for grid positions.
/// </summary>
public class GridSystem : MonoBehaviour
{
    [Header("Grid Configuration")]
    [SerializeField] private int gridWidth = 50;
    [SerializeField] private int gridHeight = 50;
    [SerializeField] private float tileSize = 1f;
    [SerializeField] private Vector3 gridOrigin = Vector3.zero;

    private Board<GridTile> grid;

	public static GridSystem Instance { get; private set; }
	public int Width => gridWidth;
    public int Height => gridHeight;
    public float TileSize => tileSize;

    void Awake()
    {
        Instance = this;
		Debug.Log(GridSystem.Instance);
        InitializeGrid();
    }

    void InitializeGrid()
    {
        v2.axisY = 'z'; // Use XZ plane for 3D grid

        grid = new Board<GridTile>(new v2(gridWidth, gridHeight), null);

        // Initialize all tiles
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                v2 gridPos = new v2(x, z);
                Vector3 worldPos = GridToWorld(gridPos);

                grid[x, z] = new GridTile
                {
                    gridPos = gridPos,
                    worldPos = worldPos,
                    isOccupied = false,
                    buildingOccupied = null,
                    terrainType = TerrainType.Normal,
                    isWalkable = true
                };
            }
        }
    }

    #region Coordinate Conversion
    public Vector3 GridToWorld(v2 gridPos)
    {
        return gridOrigin + new Vector3(gridPos.x * tileSize, 0, gridPos.y * tileSize);
    }

    public v2 WorldToGrid(Vector3 worldPos)
    {
        Vector3 localPos = worldPos - gridOrigin;
        int x = Mathf.RoundToInt(localPos.x / tileSize);
        int z = Mathf.RoundToInt(localPos.z / tileSize);
        return new v2(x, z);
    }
    #endregion

    #region Tile Access
    public GridTile GetTile(v2 gridPos)
    {
        if (!IsInBounds(gridPos)) return null;
        return grid[gridPos.x, gridPos.y];
    }

    public GridTile GetTile(int x, int z)
    {
        return GetTile(new v2(x, z));
    }

    public bool IsInBounds(v2 gridPos)
    {
        return gridPos.x >= 0 && gridPos.x < gridWidth &&
               gridPos.y >= 0 && gridPos.y < gridHeight;
    }
    #endregion

    #region Building Placement Validation
    /// <summary>
    /// Check if an area is valid for building placement.
    /// Validates all tiles that the building would occupy.
    /// </summary>
    public bool IsPositionValid(v2 gridPos, v2 size, v2[] occupiedOffsets = null)
    {
        // Use occupiedOffsets for jagged shapes, or size for rectangular buildings
        if (occupiedOffsets != null && occupiedOffsets.Length > 0)
        {
            foreach (v2 offset in occupiedOffsets)
            {
                v2 checkPos = gridPos + offset;
                if (!IsTileValidForPlacement(checkPos))
                    return false;
            }
        }
        else
        {
            // Rectangular building
            for (int x = 0; x < size.x; x++)
            {
                for (int z = 0; z < size.y; z++)
                {
                    v2 checkPos = gridPos + new v2(x, z);
                    if (!IsTileValidForPlacement(checkPos))
                        return false;
                }
            }
        }
        return true;
    }

    private bool IsTileValidForPlacement(v2 gridPos)
    {
        if (!IsInBounds(gridPos))
            return false;

        GridTile tile = GetTile(gridPos);
        if (tile == null)
            return false;

        // Can't build if occupied
        if (tile.isOccupied)
            return false;

        // Can't build on blocking terrain
        if (tile.terrainType == TerrainType.Tree ||
            tile.terrainType == TerrainType.Rock ||
            tile.terrainType == TerrainType.Bonfire)
            return false;

        // Obelisks are decorative and don't block (but still can't build ON them)
        if (tile.terrainType == TerrainType.Obelisk)
            return false;

        return true;
    }
    #endregion

    #region Building Occupation
    /// <summary>
    /// Mark tiles as occupied by a building and update walkability for pathfinding.
    /// </summary>
    public void OccupyTiles(v2 gridPos, v2 size, Building building, v2[] occupiedOffsets = null)
    {
        if (occupiedOffsets != null && occupiedOffsets.Length > 0)
        {
            foreach (v2 offset in occupiedOffsets)
            {
                v2 tilePos = gridPos + offset;
                OccupySingleTile(tilePos, building);
            }
        }
        else
        {
            for (int x = 0; x < size.x; x++)
            {
                for (int z = 0; z < size.y; z++)
                {
                    v2 tilePos = gridPos + new v2(x, z);
                    OccupySingleTile(tilePos, building);
                }
            }
        }
    }

    private void OccupySingleTile(v2 gridPos, Building building)
    {
        GridTile tile = GetTile(gridPos);
        if (tile != null)
        {
            tile.isOccupied = true;
            tile.buildingOccupied = building;
            tile.isWalkable = false; // Buildings block enemy pathfinding
        }
    }

    /// <summary>
    /// Free tiles when a building is removed and update walkability.
    /// </summary>
    public void FreeTiles(v2 gridPos, v2 size, v2[] occupiedOffsets = null)
    {
        if (occupiedOffsets != null && occupiedOffsets.Length > 0)
        {
            foreach (v2 offset in occupiedOffsets)
            {
                v2 tilePos = gridPos + offset;
                FreeSingleTile(tilePos);
            }
        }
        else
        {
            for (int x = 0; x < size.x; x++)
            {
                for (int z = 0; z < size.y; z++)
                {
                    v2 tilePos = gridPos + new v2(x, z);
                    FreeSingleTile(tilePos);
                }
            }
        }
    }

    private void FreeSingleTile(v2 gridPos)
    {
        GridTile tile = GetTile(gridPos);
        if (tile != null)
        {
            tile.isOccupied = false;
            tile.buildingOccupied = null;

            // Restore walkability if terrain allows
            tile.isWalkable = tile.terrainType == TerrainType.Normal ||
                             tile.terrainType == TerrainType.Obelisk;
        }
    }
    #endregion

    #region Terrain Management
    /// <summary>
    /// Set terrain type for a tile (tree, rock, obelisk, bonfire).
    /// </summary>
    public void SetTerrainType(v2 gridPos, TerrainType terrainType)
    {
        GridTile tile = GetTile(gridPos);
        if (tile != null)
        {
            tile.terrainType = terrainType;

            // Update walkability based on terrain
            if (tile.terrainType == TerrainType.Obelisk)
            {
                tile.isWalkable = true; // Obelisks are decorative and walkable
            }
            else if (tile.terrainType == TerrainType.Tree ||
                    tile.terrainType == TerrainType.Rock ||
                    tile.terrainType == TerrainType.Bonfire)
            {
                tile.isWalkable = false; // These block movement
            }
        }
    }

    /// <summary>
    /// Remove terrain obstacle (harvest tree/rock).
    /// </summary>
    public void ClearTerrain(v2 gridPos)
    {
        GridTile tile = GetTile(gridPos);
        if (tile != null)
        {
            tile.terrainType = TerrainType.Normal;
            if (!tile.isOccupied)
            {
                tile.isWalkable = true;
            }
        }
    }
    #endregion

    #region Adjacency
    /// <summary>
    /// Get adjacent buildings in cardinal directions (N, E, S, W).
    /// </summary>
    public List<Building> GetAdjacentBuildings(v2 gridPos)
    {
        List<Building> adjacent = new List<Building>();
        v2[] directions = { new v2(0, 1), new v2(1, 0), new v2(0, -1), new v2(-1, 0) };

        foreach (v2 dir in directions)
        {
            v2 checkPos = gridPos + dir;
            GridTile tile = GetTile(checkPos);
            if (tile != null && tile.buildingOccupied != null)
            {
                if (!adjacent.Contains(tile.buildingOccupied))
                {
                    adjacent.Add(tile.buildingOccupied);
                }
            }
        }

        return adjacent;
    }

    /// <summary>
    /// Get building at specific position.
    /// </summary>
    public Building GetBuildingAt(v2 gridPos)
    {
        GridTile tile = GetTile(gridPos);
        return tile?.buildingOccupied;
    }
    #endregion

    #region Pathfinding (A* for enemies)
    /// <summary>
    /// Check if a tile is walkable for enemies.
    /// </summary>
    public bool IsWalkable(v2 gridPos)
    {
        GridTile tile = GetTile(gridPos);
        return tile != null && tile.isWalkable;
    }

    /// <summary>
    /// Find path from start to goal using A* algorithm.
    /// Used for enemy pathfinding in tower defense.
    /// </summary>
    public List<v2> FindPath(v2 start, v2 goal)
    {
        // Simple A* implementation
        HashSet<v2> closedSet = new HashSet<v2>();
        List<PathNode> openSet = new List<PathNode>();
        Dictionary<v2, PathNode> allNodes = new Dictionary<v2, PathNode>();

        PathNode startNode = new PathNode(start, 0, GetHeuristic(start, goal));
        openSet.Add(startNode);
        allNodes[start] = startNode;

        while (openSet.Count > 0)
        {
            // Get node with lowest f
            PathNode current = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].f < current.f)
                    current = openSet[i];
            }

            if (current.position == goal)
            {
                return ReconstructPath(current);
            }

            openSet.Remove(current);
            closedSet.Add(current.position);

            // Check neighbors
            v2[] directions = { new v2(0, 1), new v2(1, 0), new v2(0, -1), new v2(-1, 0) };
            foreach (v2 dir in directions)
            {
                v2 neighborPos = current.position + dir;

                if (!IsWalkable(neighborPos) || closedSet.Contains(neighborPos))
                    continue;

                float tentativeG = current.g + 1;

                if (!allNodes.ContainsKey(neighborPos))
                {
                    PathNode neighbor = new PathNode(neighborPos, tentativeG, GetHeuristic(neighborPos, goal));
                    neighbor.parent = current;
                    openSet.Add(neighbor);
                    allNodes[neighborPos] = neighbor;
                }
                else if (tentativeG < allNodes[neighborPos].g)
                {
                    PathNode neighbor = allNodes[neighborPos];
                    neighbor.g = tentativeG;
                    neighbor.parent = current;
                }
            }
        }

        return null; // No path found
    }

    private float GetHeuristic(v2 a, v2 b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y); // Manhattan distance
    }

    private List<v2> ReconstructPath(PathNode endNode)
    {
        List<v2> path = new List<v2>();
        PathNode current = endNode;
        while (current != null)
        {
            path.Add(current.position);
            current = current.parent;
        }
        path.Reverse();
        return path;
    }

    private class PathNode
    {
        public v2 position;
        public float g; // Cost from start
        public float h; // Heuristic to goal
        public float f => g + h;
        public PathNode parent;

        public PathNode(v2 pos, float g, float h)
        {
            this.position = pos;
            this.g = g;
            this.h = h;
        }
    }
    #endregion

    #region Debug Visualization
    void OnDrawGizmos()
    {
        if (!Application.isPlaying || grid == null)
            return;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                GridTile tile = grid[x, z];
                if (tile == null) continue;

                Vector3 center = tile.worldPos + Vector3.up * 0.01f;

                // Color based on tile state
                if (tile.isOccupied)
                    Gizmos.color = new Color(1, 0, 0, 0.3f); // Red for occupied
                else if (!tile.isWalkable)
                    Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.3f); // Gray for blocked
                else
                    Gizmos.color = new Color(0, 1, 0, 0.1f); // Green for free

                Gizmos.DrawCube(center, new Vector3(tileSize * 0.9f, 0.05f, tileSize * 0.9f));
            }
        }
    }
    #endregion
}

/// <summary>
/// Data for a single grid tile.
/// Tracks occupation, terrain, and walkability for pathfinding.
/// </summary>
[System.Serializable]
public class GridTile
{
    public v2 gridPos;
    public Vector3 worldPos;

    // Building placement
    public bool isOccupied;
    public Building buildingOccupied;

    // Terrain/obstacles
    public TerrainType terrainType;

    // Enemy pathfinding (CRITICAL for tower defense)
    public bool isWalkable;

}