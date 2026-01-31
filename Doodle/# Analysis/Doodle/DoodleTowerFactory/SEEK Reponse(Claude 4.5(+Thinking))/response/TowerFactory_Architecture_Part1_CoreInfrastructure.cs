/*
 * ============================================================================
 * TOWERFACTORY - COMPLETE ARCHITECTURAL SKELETON
 * Part 1: Core Infrastructure, Grid System, Events, Game Management
 * ============================================================================
 * 
 * This is a COMPLETE code skeleton for a Unity developer to implement.
 * ALL method bodies are marked with TODO comments.
 * The developer will fill in the logic - this provides 100% structure.
 * 
 * Dependencies: UTIL.cs (v2, Board<T>, C class utilities)
 * 
 * ============================================================================
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SPACE_UTIL; // For v2, Board<T>, C utilities

namespace LightTower
{
    #region ═══════════════════════════════════════════════════════════════
    #region ENUMS - CORE GAME DEFINITIONS
    #endregion
    #region ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// All resource types in the game (raw + processed)
    /// </summary>
    public enum ResourceType
    {
        // Raw Resources
        Wood,
        Stone,
        Iron,
        Coal,
        Copper,
        Crystal,
        LightCrystal,
        LightEssence,
        
        // Processed Resources
        WoodenPlanks,
        IronIngot,
        CopperWire,
        SteelBar,
        CrystalShard,
        WoodenStake,
        IronNails,
        
        // Special
        Gold,
        Mana
    }

    /// <summary>
    /// Building categories for UI organization
    /// </summary>
    public enum BuildingCategory
    {
        Production,  // Extractors, processors
        Logistics,   // Conveyors, storage
        Defense,     // Towers, walls
        Utility,     // Beacons, special buildings
        Special      // Quest-specific, unique buildings
    }

    /// <summary>
    /// Tower targeting strategies
    /// </summary>
    public enum TargetingStrategy
    {
        First,           // Furthest along path
        Last,            // Closest to start
        Nearest,         // Closest to tower
        Farthest,        // Farthest from tower
        Slowest,         // Lowest move speed
        Fastest,         // Highest move speed
        LowestHealth,    // Weakest health
        HighestHealth,   // Strongest health
        HighestArmor,    // Most armor
        HighestShield,   // Most shield
        MostTotalLife,   // Highest (health + armor + shield)
        LeastTotalLife   // Lowest (health + armor + shield)
    }

    /// <summary>
    /// Enemy type classification
    /// </summary>
    public enum EnemyType
    {
        Ground,
        Flying,
        Boss,
        Elite
    }

    /// <summary>
    /// Conveyor belt types
    /// </summary>
    public enum ConveyorType
    {
        Straight,
        Curve,
        Splitter,
        Combiner,
        Underground,
        Crossing,
        Storage
    }

    /// <summary>
    /// Tile types for grid system
    /// </summary>
    public enum TileType
    {
        Default,      // Buildable ground
        Path,         // Enemy path
        Border,       // Map edge
        ResourceNode, // Has resource to extract
        Water,        // Unbuildable
        Void          // Outside map bounds
    }

    /// <summary>
    /// Orientation for directional buildings
    /// </summary>
    public enum EOrientation
    {
        North = 0,   // 0 degrees
        East = 90,   // 90 degrees
        South = 180, // 180 degrees
        West = 270   // 270 degrees
    }

    /// <summary>
    /// Game difficulty levels
    /// </summary>
    public enum EGameDifficulty
    {
        Easy,
        Normal,
        Hard,
        Nightmare
    }

    /// <summary>
    /// Input modes for different gameplay states
    /// </summary>
    public enum EInputMode
    {
        Standard,    // Normal gameplay
        BuyMode,     // Placing buildings
        EditMode,    // Moving/selling buildings
        Paused,      // Game paused
        Tutorial     // Tutorial active
    }

    /// <summary>
    /// Cycle mode for wave system
    /// </summary>
    public enum ECycleMode
    {
        Wave,   // Discrete waves
        Round,  // Continuous spawning
        Both    // Mixed
    }

    /// <summary>
    /// Damage type for combat calculations
    /// </summary>
    public enum DamageType
    {
        Physical,
        Magical,
        Pure,     // Ignores armor
        True      // Ignores everything
    }

    /// <summary>
    /// Stat modifiers operation type
    /// </summary>
    public enum ModifierOperation
    {
        Add,       // Flat addition
        Multiply,  // Percentage multiplication
        Override   // Set to specific value
    }

    /// <summary>
    /// Game state enumeration
    /// </summary>
    public enum GameState
    {
        MainMenu,
        Loading,
        Playing,
        Paused,
        BuildMode,
        Victory,
        Defeat,
        Tutorial
    }

    #endregion

    #region ═══════════════════════════════════════════════════════════════
    #region EVENTS - CENTRALIZED EVENT BUS
    #endregion
    #region ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Centralized event bus for game-wide communication.
    /// All systems communicate through this static event hub.
    /// 
    /// USAGE PATTERN:
    /// - Subscribe in OnEnable: GameEvents.OnBuildingPlaced += HandleBuildingPlaced;
    /// - Unsubscribe in OnDisable: GameEvents.OnBuildingPlaced -= HandleBuildingPlaced;
    /// - Fire events: GameEvents.OnBuildingPlaced?.Invoke(building);
    /// </summary>
    public static class GameEvents
    {
        #region Building Events
        /// <summary>Fired when a building is successfully placed on the grid</summary>
        public static event Action<PlacementComponent> OnBuildingPlaced;
        
        /// <summary>Fired when a building is destroyed (not sold)</summary>
        public static event Action<PlacementComponent> OnBuildingDestroyed;
        
        /// <summary>Fired when a building is sold by the player</summary>
        public static event Action<PlacementComponent> OnBuildingSold;
        
        /// <summary>Fired when a building position changes (drag)</summary>
        public static event Action<PlacementComponent, v2> OnBuildingMoved;
        
        /// <summary>Fired when a building is rotated</summary>
        public static event Action<PlacementComponent, EOrientation> OnBuildingRotated;
        #endregion

        #region Resource Events
        /// <summary>Fired when resources are added to inventory</summary>
        public static event Action<ResourceType, int> OnResourceGained;
        
        /// <summary>Fired when resources are spent from inventory</summary>
        public static event Action<ResourceType, int> OnResourceSpent;
        
        /// <summary>Fired when a resource node is fully depleted</summary>
        public static event Action<Tile, ResourceType, int> OnResourceNodeDepleted;
        
        /// <summary>Fired when player clicks a resource node to collect</summary>
        public static event Action<ResourceType, int> OnResourceClicked;
        
        /// <summary>Fired when resources are stored in storage buildings</summary>
        public static event Action<ResourceType, int> OnResourceStored;
        #endregion

        #region Combat Events
        /// <summary>Fired when an enemy spawns into the world</summary>
        public static event Action<Enemy> OnEnemySpawned;
        
        /// <summary>Fired when an enemy is killed (includes killer tower)</summary>
        public static event Action<Enemy, Tower> OnEnemyKilled;
        
        /// <summary>Fired when a tower fires a projectile</summary>
        public static event Action<Tower, Enemy> OnTowerFired;
        
        /// <summary>Fired when an enemy takes damage</summary>
        public static event Action<Enemy, int, DamageType> OnEnemyDamaged;
        
        /// <summary>Fired when an enemy reaches the end of the path</summary>
        public static event Action<Enemy> OnEnemyReachedEnd;
        
        /// <summary>Fired when a projectile hits a target</summary>
        public static event Action<Projectile, Enemy> OnProjectileHit;
        #endregion

        #region Wave/Cycle Events
        /// <summary>Fired when a new wave starts</summary>
        public static event Action<int> OnWaveStarted;
        
        /// <summary>Fired when a wave is completed</summary>
        public static event Action<int> OnWaveCompleted;
        
        /// <summary>Fired when a cycle (round) starts</summary>
        public static event Action<int> OnCycleStarted;
        
        /// <summary>Fired when a cycle completes</summary>
        public static event Action<int> OnCycleCompleted;
        
        /// <summary>Fired when all cycles are completed (victory)</summary>
        public static event Action OnAllCyclesCompleted;
        #endregion

        #region UI Events
        /// <summary>Fired when an object is selected (building, enemy, etc.)</summary>
        public static event Action<ISelectable> OnObjectSelected;
        
        /// <summary>Fired when selection is cleared</summary>
        public static event Action OnObjectDeselected;
        
        /// <summary>Fired when a tooltip should be shown</summary>
        public static event Action<TooltipComponent> OnTooltipShow;
        
        /// <summary>Fired when tooltip should be hidden</summary>
        public static event Action OnTooltipHide;
        
        /// <summary>Fired when store/shop UI is opened</summary>
        public static event Action OnStoreOpened;
        
        /// <summary>Fired when store/shop UI is closed</summary>
        public static event Action OnStoreClosed;
        #endregion

        #region Game State Events
        /// <summary>Fired when game is paused</summary>
        public static event Action OnGamePaused;
        
        /// <summary>Fired when game is resumed</summary>
        public static event Action OnGameResumed;
        
        /// <summary>Fired when time scale changes (1x, 2x, 4x, etc.)</summary>
        public static event Action<float> OnTimeScaleChanged;
        
        /// <summary>Fired when game state changes</summary>
        public static event Action<GameState, GameState> OnGameStateChanged; // old, new
        
        /// <summary>Fired when victory condition is met</summary>
        public static event Action OnVictory;
        
        /// <summary>Fired when defeat condition is met</summary>
        public static event Action OnDefeat;
        #endregion

        #region Factory/Conveyor Events
        /// <summary>Fired when a conveyor belt is placed</summary>
        public static event Action<ConveyorBelt> OnConveyorPlaced;
        
        /// <summary>Fired when a conveyor belt is removed</summary>
        public static event Action<ConveyorBelt> OnConveyorRemoved;
        
        /// <summary>Fired when an item is transferred between conveyors</summary>
        public static event Action<Item, ConveyorBelt, ConveyorBelt> OnItemTransferred; // item, from, to
        
        /// <summary>Fired when a processor completes crafting</summary>
        public static event Action<Processor, Recipe> OnProcessorCraftComplete;
        
        /// <summary>Fired when an extractor produces a resource</summary>
        public static event Action<Extractor, ResourceType> OnExtractorProduce;
        #endregion

        #region Player Events
        /// <summary>Fired when player health changes</summary>
        public static event Action<int, int> OnPlayerHealthChanged; // current, max
        
        /// <summary>Fired when player money changes</summary>
        public static event Action<int> OnPlayerMoneyChanged;
        
        /// <summary>Fired when a player upgrade is purchased</summary>
        public static event Action<PlayerUpgrade> OnPlayerUpgradePurchased;
        #endregion

        #region Tutorial Events
        /// <summary>Fired when a tutorial quest starts</summary>
        public static event Action<TutorialQuestData> OnTutorialQuestStarted;
        
        /// <summary>Fired when a tutorial quest is completed</summary>
        public static event Action<TutorialQuestData> OnTutorialQuestCompleted;
        
        /// <summary>Fired when tutorial completes entirely</summary>
        public static event Action OnTutorialCompleted;
        #endregion

        #region Fog of War Events
        /// <summary>Fired when an area is explored for the first time</summary>
        public static event Action<v2, int> OnAreaExplored; // center, radius
        
        /// <summary>Fired when vision area changes</summary>
        public static event Action<v2, int, bool> OnVisionChanged; // center, radius, isVisible
        #endregion

        #region Helper Methods for Invoking Events
        public static void InvokeBuildingPlaced(PlacementComponent building)
        {
            OnBuildingPlaced?.Invoke(building);
        }

        public static void InvokeResourceGained(ResourceType type, int amount)
        {
            OnResourceGained?.Invoke(type, amount);
        }

        public static void InvokeEnemyKilled(Enemy enemy, Tower tower)
        {
            OnEnemyKilled?.Invoke(enemy, tower);
        }
        
        // TODO: Add more helper methods as needed for commonly used events
        #endregion
    }

    #endregion

    #region ═══════════════════════════════════════════════════════════════
    #region DATA STRUCTURES
    #endregion
    #region ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Represents a cost of resources for building/upgrading
    /// </summary>
    [System.Serializable]
    public struct ResourceCost
    {
        public ResourceType type;
        public int amount;

        public ResourceCost(ResourceType type, int amount)
        {
            this.type = type;
            this.amount = amount;
        }

        public override string ToString()
        {
            return $"{amount}x {type}";
        }
    }

    /// <summary>
    /// Complete cost structure (can include multiple resources + money)
    /// </summary>
    [System.Serializable]
    public class Cost
    {
        [Header("Resources")]
        public ResourceCost[] resources = new ResourceCost[0];
        
        [Header("Currency")]
        public int money = 0;

        /// <summary>
        /// Check if player can afford this cost
        /// </summary>
        public bool CanAfford(PlayerData playerData)
        {
            /* TODO:
             * 1. Check if player has enough money
             * 2. Check if player has enough of each resource
             * 3. Return true only if all checks pass
             */
            return false;
        }

        /// <summary>
        /// Deduct this cost from player resources
        /// </summary>
        public void Spend(PlayerData playerData)
        {
            /* TODO:
             * 1. Deduct money
             * 2. Deduct each resource
             * 3. Fire GameEvents.OnResourceSpent for each resource
             */
        }

        /// <summary>
        /// Refund a percentage of this cost
        /// </summary>
        public void Refund(PlayerData playerData, float percentage = 0.5f)
        {
            /* TODO:
             * 1. Refund money * percentage
             * 2. Refund each resource * percentage
             * 3. Fire GameEvents.OnResourceGained for each resource
             */
        }
    }

    /// <summary>
    /// Stat modifier for upgrades/buffs
    /// </summary>
    [System.Serializable]
    public class StatModifier
    {
        public string statName;          // "damage", "range", "fireRate", etc.
        public ModifierOperation operation;
        public float value;
        public int priority = 0;         // Lower priority applied first

        public StatModifier(string statName, ModifierOperation operation, float value, int priority = 0)
        {
            this.statName = statName;
            this.operation = operation;
            this.value = value;
            this.priority = priority;
        }
    }

    /// <summary>
    /// Represents items being transported on conveyors
    /// </summary>
    [System.Serializable]
    public class Item
    {
        public ResourceType type;
        public GameObject visual;              // 3D representation on belt
        public ConveyorBelt currentBelt;       // Which belt this item is on
        public float positionOnBelt;           // 0 to 1 progress along belt
        public float moveSpeed;                // Units per second
        
        [System.NonSerialized]
        public Transform transform;            // Cached transform reference

        public Item(ResourceType type, GameObject visual)
        {
            this.type = type;
            this.visual = visual;
            this.transform = visual?.transform;
            this.positionOnBelt = 0f;
        }

        /// <summary>
        /// Update item position along belt
        /// </summary>
        public void UpdatePosition(float deltaTime)
        {
            /* TODO:
             * 1. Move positionOnBelt forward by moveSpeed * deltaTime
             * 2. Update visual.transform.position based on currentBelt.GetPositionAtProgress(positionOnBelt)
             * 3. If positionOnBelt >= 1, try to transfer to next belt
             */
        }
    }

    /// <summary>
    /// Path data for enemy movement
    /// </summary>
    [System.Serializable]
    public class Path
    {
        public Vector3[] positions;           // Waypoints (bezier curve points)
        public float[] distanceToPosition;    // Cumulative distance to each waypoint
        public float totalLength;             // Total path length

        /// <summary>
        /// Get position at specific distance along path
        /// </summary>
        public Vector3 GetPositionAtDistance(float distance)
        {
            /* TODO:
             * 1. Find which segment the distance falls into
             * 2. Lerp between waypoints based on local distance in segment
             * 3. Return interpolated position
             */
            return Vector3.zero;
        }

        /// <summary>
        /// Get direction at specific distance along path
        /// </summary>
        public Vector3 GetDirectionAtDistance(float distance)
        {
            /* TODO:
             * 1. Sample position at distance and distance + epsilon
             * 2. Return normalized direction vector
             */
            return Vector3.forward;
        }
    }

    /// <summary>
    /// Path node for pathfinding graph
    /// </summary>
    [System.Serializable]
    public class PathNode
    {
        public v2 gridPosition;
        public Tile tile;
        public List<PathNode> connections = new List<PathNode>();
        public List<Path> paths = new List<Path>();  // Pre-baked bezier paths to connected nodes

        /// <summary>
        /// Get random path from this node (for splits in path)
        /// </summary>
        public Path GetRandomPath()
        {
            /* TODO:
             * Return random path from paths list
             */
            return null;
        }
    }

    #endregion

    #region ═══════════════════════════════════════════════════════════════
    #region GRID SYSTEM - TILE & GRID MANAGER
    #endregion
    #region ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Represents a single grid cell/tile in the game world.
    /// Stores all information about what's on this tile.
    /// 
    /// CRITICAL: This class is serialized for save/load.
    /// </summary>
    [System.Serializable]
    public class Tile
    {
        #region Core Data
        public v2 gridPosition;
        public TileType type = TileType.Default;
        public Vector3 worldPosition;
        
        [Header("Visual")]
        public GameObject visual;           // 3D tile mesh/sprite
        public Renderer tileRenderer;       // For material changes (fog of war, etc.)
        #endregion

        #region Occupancy
        public PlacementComponent building; // null if empty, otherwise the building on this tile
        public bool isOccupied => building != null;
        #endregion

        #region Resource Data (if type == ResourceNode)
        public ResourceType? resourceType;
        public int resourceAmount;          // Remaining harvestable amount
        public int maxResourceAmount;       // Starting amount
        public GameObject resourceVisual;   // Tree, rock, ore vein visual
        #endregion

        #region Path Data (if type == Path)
        public PathTile pathTile;           // Reference to PathTile component if this is a path
        public PathNode pathNode;           // Pathfinding node data
        #endregion

        #region Fog of War
        public bool isExplored = false;     // Has player seen this tile?
        public bool isVisible = false;      // Is currently in vision range?
        #endregion

        #region Methods
        /// <summary>
        /// Can a building be placed on this tile?
        /// </summary>
        public bool CanBuildOn(bool ignorePath = false)
        {
            /* TODO:
             * 1. Check if tile is occupied
             * 2. Check if tile type allows building
             * 3. If ignorePath is false, reject Path tiles
             * 4. Check if tile is explored (if fog of war enabled)
             * 5. Return true if all checks pass
             */
            return false;
        }

        /// <summary>
        /// Mark this tile as occupied by a building
        /// </summary>
        public void SetBuilding(PlacementComponent buildingComponent)
        {
            /* TODO:
             * Set building reference
             */
            building = buildingComponent;
        }

        /// <summary>
        /// Clear building from this tile
        /// </summary>
        public void ClearBuilding()
        {
            /* TODO:
             * Set building to null
             */
            building = null;
        }

        /// <summary>
        /// Deplete resource on this tile (when extracted or clicked)
        /// </summary>
        public void DepleteResource(int amount)
        {
            /* TODO:
             * 1. Reduce resourceAmount by amount
             * 2. If resourceAmount <= 0:
             *    - Destroy resourceVisual
             *    - Fire GameEvents.OnResourceNodeDepleted
             *    - Change type to Default
             */
        }

        /// <summary>
        /// Update visual based on fog of war state
        /// </summary>
        public void UpdateFogVisual()
        {
            /* TODO:
             * 1. If not explored: Set completely dark
             * 2. If explored but not visible: Set to greyscale/dimmed
             * 3. If visible: Set to full color
             */
        }
        #endregion
    }

    /// <summary>
    /// Path-specific tile component (attached to path tiles)
    /// </summary>
    public class PathTile : MonoBehaviour
    {
        [SerializeField] private v2 gridPosition;
        [SerializeField] private Tile tile;
        
        public PathNode pathNode;
        public List<PathTile> connectedPathTiles = new List<PathTile>();

        public v2 GridPosition => gridPosition;
        public Tile Tile => tile;

        private void Awake()
        {
            /* TODO:
             * Initialize pathNode
             */
        }

        /// <summary>
        /// Build connections to neighboring path tiles
        /// </summary>
        public void BuildConnections(GridManager gridManager)
        {
            /* TODO:
             * 1. Check all 4 cardinal directions
             * 2. If neighbor is also a path tile, add to connectedPathTiles
             * 3. Create PathNode connection
             */
        }
    }

    /// <summary>
    /// Manages the game grid. Singleton pattern.
    /// Handles tile creation, queries, and grid-wide operations.
    /// 
    /// INITIALIZATION ORDER:
    /// 1. GameManager.Awake creates GridManager
    /// 2. GridManager.Initialize(gridSize) creates grid
    /// 3. GridManager.SpawnVisualTiles() creates 3D tiles
    /// 4. PathManager.Initialize() builds path graph
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        #region Singleton
        public static GridManager Instance { get; private set; }
        #endregion

        #region Inspector Fields
        [Header("Grid Configuration")]
        [SerializeField] private v2 gridSize = (50, 50);
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private Vector3 gridOrigin = Vector3.zero;

        [Header("Tile Prefabs")]
        [SerializeField] private GameObject defaultTilePrefab;
        [SerializeField] private GameObject pathTilePrefab;
        [SerializeField] private GameObject borderTilePrefab;
        [SerializeField] private GameObject waterTilePrefab;

        [Header("Debug")]
        [SerializeField] private bool showGridGizmos = false;
        [SerializeField] private Color gridColor = Color.green;
        #endregion

        #region Private Fields
        private Board<Tile> grid;
        private Transform tilesParent;
        private bool isInitialized = false;
        #endregion

        #region Properties
        public v2 GridSize => gridSize;
        public float CellSize => cellSize;
        public Vector3 GridOrigin => gridOrigin;
        public bool IsInitialized => isInitialized;
        #endregion

        #region Events
        public event Action<v2> OnTileOccupied;
        public event Action<v2> OnTileCleared;
        public event Action OnGridInitialized;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            /* TODO:
             * 1. Setup singleton
             * 2. Create tiles parent transform
             */
            
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDrawGizmos()
        {
            /* TODO:
             * If showGridGizmos:
             * - Draw grid lines
             * - Draw occupied tiles in different color
             */
        }
        #endregion

        #region Initialization
        /// <summary>
        /// Initialize the grid with given size
        /// </summary>
        public void Initialize(v2 size)
        {
            /* TODO:
             * 1. Set gridSize
             * 2. Create Board<Tile> with default Tile objects
             * 3. For each tile:
             *    - Set gridPosition
             *    - Calculate worldPosition
             *    - Set default type
             * 4. Set isInitialized = true
             * 5. Fire OnGridInitialized event
             */
        }

        /// <summary>
        /// Spawn visual 3D tiles for the entire grid
        /// </summary>
        public void SpawnVisualTiles()
        {
            /* TODO:
             * 1. For each tile in grid:
             *    - Instantiate appropriate prefab based on tile.type
             *    - Set position = tile.worldPosition
             *    - Parent to tilesParent
             *    - Store GameObject reference in tile.visual
             *    - Cache tile.tileRenderer
             */
        }
        #endregion

        #region Tile Access
        /// <summary>
        /// Get tile at grid coordinate
        /// </summary>
        public Tile GetTile(v2 gridPos)
        {
            /* TODO:
             * 1. Check if position is in bounds
             * 2. Return grid[gridPos]
             */
            return null;
        }

        /// <summary>
        /// Get tile at grid coordinate (x, y)
        /// </summary>
        public Tile GetTile(int x, int y)
        {
            return GetTile((x, y));
        }

        /// <summary>
        /// Try to get tile, returns false if out of bounds
        /// </summary>
        public bool TryGetTile(v2 gridPos, out Tile tile)
        {
            /* TODO:
             * 1. Check bounds
             * 2. Set tile = grid[gridPos] if valid
             * 3. Return success/failure
             */
            tile = null;
            return false;
        }
        #endregion

        #region Coordinate Conversion
        /// <summary>
        /// Convert world position to grid coordinate
        /// </summary>
        public v2 WorldToGrid(Vector3 worldPos)
        {
            /* TODO:
             * 1. Subtract gridOrigin from worldPos
             * 2. Divide by cellSize
             * 3. Round to nearest integer
             * 4. Return as v2
             */
            return (0, 0);
        }

        /// <summary>
        /// Convert grid coordinate to world position (center of tile)
        /// </summary>
        public Vector3 GridToWorld(v2 gridPos)
        {
            /* TODO:
             * 1. Multiply gridPos by cellSize
             * 2. Add gridOrigin
             * 3. Add cellSize/2 offset to center the position
             */
            return Vector3.zero;
        }

        /// <summary>
        /// Snap world position to nearest grid cell center
        /// </summary>
        public Vector3 SnapToGrid(Vector3 worldPos)
        {
            return GridToWorld(WorldToGrid(worldPos));
        }
        #endregion

        #region Bounds Checking
        /// <summary>
        /// Check if grid position is within grid bounds
        /// </summary>
        public bool IsInBounds(v2 gridPos)
        {
            return gridPos.x >= 0 && gridPos.x < gridSize.x && 
                   gridPos.y >= 0 && gridPos.y < gridSize.y;
        }

        /// <summary>
        /// Check if all positions are within bounds
        /// </summary>
        public bool AreAllInBounds(v2[] positions)
        {
            /* TODO:
             * Return true only if all positions pass IsInBounds
             */
            return false;
        }
        #endregion

        #region Placement Validation
        /// <summary>
        /// Check if a building can be placed at these positions
        /// </summary>
        public bool IsValidBuildPosition(v2[] positions, bool ignorePath = false)
        {
            /* TODO:
             * 1. Check all positions are in bounds
             * 2. For each position:
             *    - Get tile
             *    - Check tile.CanBuildOn(ignorePath)
             * 3. Return true only if all checks pass
             */
            return false;
        }

        /// <summary>
        /// Check if a single position is valid for building
        /// </summary>
        public bool IsValidBuildPosition(v2 position, bool ignorePath = false)
        {
            return IsValidBuildPosition(new v2[] { position }, ignorePath);
        }
        #endregion

        #region Tile Modification
        /// <summary>
        /// Occupy tiles with a building
        /// </summary>
        public void OccupyTiles(v2[] positions, PlacementComponent building)
        {
            /* TODO:
             * 1. For each position:
             *    - Get tile
             *    - Call tile.SetBuilding(building)
             *    - Fire OnTileOccupied event
             */
        }

        /// <summary>
        /// Clear tiles (remove building reference)
        /// </summary>
        public void ClearTiles(v2[] positions)
        {
            /* TODO:
             * 1. For each position:
             *    - Get tile
             *    - Call tile.ClearBuilding()
             *    - Fire OnTileCleared event
             */
        }
        #endregion

        #region Neighbors & Area Queries
        /// <summary>
        /// Get all tiles in a radius around a center point
        /// </summary>
        public List<Tile> GetTilesInRadius(v2 center, int radius)
        {
            /* TODO:
             * 1. Create result list
             * 2. For x from (center.x - radius) to (center.x + radius):
             *    For y from (center.y - radius) to (center.y + radius):
             *       - Check if position is in bounds
             *       - Calculate distance from center
             *       - If distance <= radius, add tile to list
             * 3. Return list
             */
            return new List<Tile>();
        }

        /// <summary>
        /// Get all tiles in a rectangular area
        /// </summary>
        public List<Tile> GetTilesInArea(v2 min, v2 max)
        {
            /* TODO:
             * 1. Create result list
             * 2. For x from min.x to max.x:
             *    For y from min.y to max.y:
             *       - If in bounds, add tile to list
             * 3. Return list
             */
            return new List<Tile>();
        }

        /// <summary>
        /// Get all cardinal neighbors (N, E, S, W)
        /// </summary>
        public List<Tile> GetCardinalNeighbors(v2 gridPos)
        {
            /* TODO:
             * 1. Check tiles at (gridPos + v2.right), (gridPos + v2.up), etc.
             * 2. Add valid tiles to list
             * 3. Return list
             */
            return new List<Tile>();
        }

        /// <summary>
        /// Get all 8 neighbors (including diagonals)
        /// </summary>
        public List<Tile> GetAllNeighbors(v2 gridPos)
        {
            /* TODO:
             * Similar to GetCardinalNeighbors but include diagonals
             */
            return new List<Tile>();
        }
        #endregion

        #region Resource Nodes
        /// <summary>
        /// Create a resource node on a tile
        /// </summary>
        public void CreateResourceNode(v2 gridPos, ResourceType resourceType, int amount, GameObject visual)
        {
            /* TODO:
             * 1. Get tile at gridPos
             * 2. Set tile.type = TileType.ResourceNode
             * 3. Set tile.resourceType = resourceType
             * 4. Set tile.resourceAmount = amount
             * 5. Set tile.maxResourceAmount = amount
             * 6. Instantiate visual at tile position
             * 7. Store reference in tile.resourceVisual
             */
        }

        /// <summary>
        /// Get all resource nodes of a specific type
        /// </summary>
        public List<Tile> GetResourceNodesOfType(ResourceType type)
        {
            /* TODO:
             * 1. Iterate through all tiles
             * 2. Filter for type == ResourceNode && resourceType == type
             * 3. Return filtered list
             */
            return new List<Tile>();
        }
        #endregion

        #region Pathfinding Support
        /// <summary>
        /// Get all path tiles
        /// </summary>
        public List<Tile> GetAllPathTiles()
        {
            /* TODO:
             * Filter grid for tiles where type == TileType.Path
             */
            return new List<Tile>();
        }

        /// <summary>
        /// Find path tiles connected to this position
        /// </summary>
        public List<PathTile> GetConnectedPathTiles(v2 gridPos)
        {
            /* TODO:
             * 1. Get tile at gridPos
             * 2. If not a path tile, return empty list
             * 3. Return tile.pathTile.connectedPathTiles
             */
            return new List<PathTile>();
        }
        #endregion

        #region Save/Load Support
        /// <summary>
        /// Get save data for grid
        /// </summary>
        public GridSaveData GetSaveData()
        {
            /* TODO:
             * 1. Create GridSaveData
             * 2. Store grid size
             * 3. For each occupied tile, save building data
             * 4. Return save data
             */
            return null;
        }

        /// <summary>
        /// Load grid from save data
        /// </summary>
        public void LoadFromSaveData(GridSaveData saveData)
        {
            /* TODO:
             * 1. Restore grid size
             * 2. For each saved building, instantiate and place
             */
        }
        #endregion
    }

    #endregion

    #region ═══════════════════════════════════════════════════════════════
    #region GAME MANAGER - CORE GAME LOOP & STATE
    #endregion
    #region ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Central game manager. Controls game state, initialization, and coordinates all systems.
    /// Singleton pattern.
    /// 
    /// INITIALIZATION ORDER:
    /// 1. Awake: Setup singleton, find/create managers
    /// 2. Start: Initialize all systems in correct order
    /// 3. InitializeAllSystems: Call Initialize() on each manager
    /// 
    /// SYSTEM INITIALIZATION ORDER:
    /// 1. GridManager
    /// 2. PathManager  
    /// 3. PlayerData
    /// 4. TimeManager
    /// 5. ConveyorSystem
    /// 6. SpawnersManager
    /// 7. CyclesManager
    /// 8. UIManager
    /// 9. AudioSystem
    /// 10. FogOfWarController (if enabled)
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        #region Singleton
        public static GameManager Instance { get; private set; }
        #endregion

        #region Inspector Fields
        [Header("Game Configuration")]
        [SerializeField] private v2 gridSize = (50, 50);
        [SerializeField] private EGameDifficulty difficulty = EGameDifficulty.Normal;
        [SerializeField] private bool enableFogOfWar = true;
        [SerializeField] private bool enableTutorial = false;

        [Header("Manager References")]
        [SerializeField] private GridManager gridManager;
        [SerializeField] private PathManager pathManager;
        [SerializeField] private TimeManager timeManager;
        [SerializeField] private ConveyorBeltSystem conveyorSystem;
        [SerializeField] private SpawnersManager spawnersManager;
        [SerializeField] private CyclesManager cyclesManager;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private AudioSystem audioSystem;
        [SerializeField] private FogOfWarController fogOfWarController;
        [SerializeField] private PlayerUpgradesManager playerUpgradesManager;
        [SerializeField] private TutorialGameManager tutorialManager;

        [Header("Debug")]
        [SerializeField] private bool debugMode = false;
        #endregion

        #region Private Fields
        private GameState currentState = GameState.Loading;
        private GameState previousState;
        private bool isInitialized = false;
        #endregion

        #region Properties
        public GameState CurrentState => currentState;
        public EGameDifficulty Difficulty => difficulty;
        public bool EnableFogOfWar => enableFogOfWar;
        public bool IsInitialized => isInitialized;
        public bool DebugMode => debugMode;

        // Manager accessors
        public GridManager Grid => gridManager;
        public PathManager Paths => pathManager;
        public TimeManager Time => timeManager;
        public ConveyorBeltSystem Conveyors => conveyorSystem;
        public SpawnersManager Spawners => spawnersManager;
        public CyclesManager Cycles => cyclesManager;
        public UIManager UI => uiManager;
        public AudioSystem Audio => audioSystem;
        public FogOfWarController FogOfWar => fogOfWarController;
        public PlayerUpgradesManager Upgrades => playerUpgradesManager;
        #endregion

        #region Events
        public event Action OnGameInitialized;
        public event Action<GameState, GameState> OnStateChanged; // previousState, newState
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            /* TODO:
             * 1. Setup singleton (destroy duplicates)
             * 2. DontDestroyOnLoad(gameObject)
             * 3. Find or create all manager references if null
             * 4. Subscribe to key events
             */
            
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            /* TODO:
             * Call InitializeAllSystems()
             */
        }

        private void OnDestroy()
        {
            /* TODO:
             * Unsubscribe from all events
             */
        }
        #endregion

        #region Initialization
        /// <summary>
        /// Initialize all game systems in correct dependency order
        /// </summary>
        private void InitializeAllSystems()
        {
            /* TODO:
             * CRITICAL: Initialize in this exact order:
             * 
             * 1. GridManager.Initialize(gridSize)
             * 2. GridManager.SpawnVisualTiles()
             * 3. PathManager.Initialize()
             * 4. PathManager.BuildPaths()
             * 5. PlayerData.Instance.Initialize()
             * 6. TimeManager.Initialize()
             * 7. ConveyorBeltSystem.Initialize()
             * 8. SpawnersManager.Initialize()
             * 9. CyclesManager.Initialize()
             * 10. UIManager.Initialize()
             * 11. AudioSystem.Initialize()
             * 12. If enableFogOfWar: FogOfWarController.Initialize(gridSize)
             * 13. If enableTutorial: TutorialGameManager.Initialize()
             * 14. PlayerUpgradesManager.Initialize()
             * 
             * Finally:
             * - Set isInitialized = true
             * - Fire OnGameInitialized
             * - SetState(GameState.Playing) or GameState.Tutorial
             */
        }
        #endregion

        #region State Management
        /// <summary>
        /// Change game state
        /// </summary>
        public void SetState(GameState newState)
        {
            /* TODO:
             * 1. Store previousState = currentState
             * 2. Set currentState = newState
             * 3. Handle state exit logic (previousState)
             * 4. Handle state enter logic (newState)
             * 5. Fire OnStateChanged event
             * 6. Fire GameEvents.OnGameStateChanged
             */
        }

        /// <summary>
        /// Handle entering a new state
        /// </summary>
        private void OnStateEnter(GameState state)
        {
            /* TODO:
             * Switch on state:
             * - MainMenu: Load menu scene
             * - Loading: Show loading screen
             * - Playing: Resume game, unpause
             * - Paused: Pause game, show pause menu
             * - BuildMode: Enable build mode UI
             * - Victory: Show victory screen, stop spawning
             * - Defeat: Show defeat screen, stop spawning
             * - Tutorial: Start tutorial
             */
        }

        /// <summary>
        /// Handle exiting a state
        /// </summary>
        private void OnStateExit(GameState state)
        {
            /* TODO:
             * Switch on state:
             * - Cleanup state-specific resources
             * - Hide state-specific UI
             */
        }
        #endregion

        #region Game Control
        /// <summary>
        /// Pause the game
        /// </summary>
        public void PauseGame()
        {
            /* TODO:
             * 1. SetState(GameState.Paused)
             * 2. TimeManager.Pause()
             * 3. Fire GameEvents.OnGamePaused
             */
        }

        /// <summary>
        /// Resume the game
        /// </summary>
        public void ResumeGame()
        {
            /* TODO:
             * 1. SetState(previousState) or GameState.Playing
             * 2. TimeManager.Resume()
             * 3. Fire GameEvents.OnGameResumed
             */
        }

        /// <summary>
        /// Restart the current level
        /// </summary>
        public void RestartLevel()
        {
            /* TODO:
             * 1. Cleanup all game objects
             * 2. Reset all managers
             * 3. Reload scene or reinitialize
             */
        }

        /// <summary>
        /// Return to main menu
        /// </summary>
        public void ReturnToMainMenu()
        {
            /* TODO:
             * 1. Cleanup game
             * 2. Load main menu scene
             */
        }
        #endregion

        #region Victory/Defeat
        /// <summary>
        /// Trigger victory condition
        /// </summary>
        public void TriggerVictory()
        {
            /* TODO:
             * 1. SetState(GameState.Victory)
             * 2. Stop all spawners
             * 3. Play victory music/effects
             * 4. Fire GameEvents.OnVictory
             * 5. Show victory UI
             * 6. Calculate rewards
             */
        }

        /// <summary>
        /// Trigger defeat condition
        /// </summary>
        public void TriggerDefeat()
        {
            /* TODO:
             * 1. SetState(GameState.Defeat)
             * 2. Stop all spawners
             * 3. Play defeat music/effects
             * 4. Fire GameEvents.OnDefeat
             * 5. Show defeat UI
             */
        }
        #endregion

        #region Save/Load
        /// <summary>
        /// Save current game state
        /// </summary>
        public void SaveGame()
        {
            /* TODO:
             * 1. Collect save data from all systems
             * 2. Serialize to JSON
             * 3. Write to file using SaveSystem
             */
        }

        /// <summary>
        /// Load game from save data
        /// </summary>
        public void LoadGame()
        {
            /* TODO:
             * 1. Read save file
             * 2. Deserialize JSON
             * 3. Load data into all systems
             */
        }
        #endregion

        #region Event Handlers
        private void OnAllCyclesCompleted()
        {
            /* TODO:
             * Call TriggerVictory()
             */
        }

        private void OnPlayerHealthZero()
        {
            /* TODO:
             * Call TriggerDefeat()
             */
        }
        #endregion
    }

    #endregion
}
