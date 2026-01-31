/*
 * ============================================================================
 * TOWERFACTORY - COMPLETE ARCHITECTURAL SKELETON
 * Part 2: Placement System, Building Architecture, Input Management
 * ============================================================================
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using SPACE_UTIL;

namespace LightTower
{
    #region ═══════════════════════════════════════════════════════════════
    #region PLACEMENT SYSTEM
    #endregion
    #region ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Handles building placement on the grid.
    /// Supports multi-tile buildings, rotation, jagged shapes, and child building parts.
    /// 
    /// CRITICAL FEATURES:
    /// - Multi-tile placement (width × length)
    /// - 90-degree rotation support
    /// - Ignored cells for jagged shapes (L-shaped, T-shaped buildings)
    /// - Child building parts (e.g., 2x2 extractor with 4 sub-parts)
    /// - Ghost visualization during placement
    /// - Cost validation and resource deduction
    /// - Grid occupation tracking
    /// 
    /// DEPENDENCIES:
    /// - GridManager for tile queries and occupation
    /// - PlayerData for cost checking
    /// - EventBus for placement notifications
    /// 
    /// INITIALIZATION: Awake → cache components, register with systems
    /// </summary>
    public class PlacementComponent : MonoBehaviour
    {
        #region Inspector Fields
        [Header("Building Reference")]
        [SerializeField] private BuildingData buildingData;
        
        [Header("Grid Size")]
        [Tooltip("Width in grid cells (X-axis)")]
        [SerializeField] private int width = 1;
        [Tooltip("Length in grid cells (Y-axis)")]  
        [SerializeField] private int length = 1;
        [SerializeField] private bool canRotate = true;
        
        [Header("Multi-Tile Configuration")]
        [Tooltip("Cells to ignore for jagged shapes (relative to origin). Example: L-shaped building")]
        [SerializeField] private v2[] ignoredCells = new v2[0];
        
        [Header("Child Building Parts")]
        [Tooltip("For multi-part buildings (e.g., 4 separate pieces for a 2x2 structure)")]
        [SerializeField] private ChildBuildingPart[] childParts = new ChildBuildingPart[0];
        
        [Header("Visual Feedback")]
        [SerializeField] private Material validPlacementMaterial;
        [SerializeField] private Material invalidPlacementMaterial;
        [SerializeField] private Material ghostMaterial;
        
        [Header("Placement Rules")]
        [SerializeField] private bool canPlaceOnPath = false;
        [SerializeField] private bool requiresResourceNode = false;
        [SerializeField] private ResourceType? requiredResourceType = null;
        
        [Header("Audio")]
        [SerializeField] private AudioData placementSound;
        [SerializeField] private AudioData sellSound;
        #endregion

        #region Private Fields
        private int currentRotation = 0;        // 0, 90, 180, 270 degrees
        private EOrientation orientation = EOrientation.North;
        private bool isPlaced = false;
        private bool isGhost = false;           // Is this a ghost preview?
        private v2 gridOrigin;                  // Bottom-left grid position
        private Vector3 worldPosition;
        private Renderer[] renderers;
        private Material[] originalMaterials;
        private v2[] cachedOccupiedCells;       // Cache for performance
        private bool occupiedCellsDirty = true;
        #endregion

        #region Properties
        public BuildingData Data => buildingData;
        public bool IsPlaced => isPlaced;
        public bool IsGhost => isGhost;
        public v2 GridPosition => gridOrigin;
        public Vector3 WorldPosition => worldPosition;
        public int Rotation => currentRotation;
        public EOrientation Orientation => orientation;
        public int Width => width;
        public int Length => length;
        public bool CanRotate => canRotate;
        #endregion

        #region Events
        /// <summary>Fired when building is successfully placed on grid</summary>
        public event Action<PlacementComponent> OnPlaced;
        
        /// <summary>Fired when building is removed from grid</summary>
        public event Action<PlacementComponent> OnUnplaced;
        
        /// <summary>Fired when building position changes (drag or move)</summary>
        public event Action<PlacementComponent, v2> OnPositionChanged;
        
        /// <summary>Fired when building is rotated</summary>
        public event Action<PlacementComponent, EOrientation> OnRotated;
        
        /// <summary>Fired when building is sold</summary>
        public event Action<PlacementComponent> OnSold;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            /* TODO:
             * 1. Cache all Renderer components (including children)
             * 2. Store original materials
             * 3. Initialize occupied cells cache
             * 4. Set gridOrigin based on current position if not ghost
             */
        }

        private void Start()
        {
            /* TODO:
             * If not a ghost and auto-place is enabled:
             * - Calculate grid position from world position
             * - Call Place() automatically
             */
        }

        private void OnDestroy()
        {
            /* TODO:
             * 1. Unsubscribe from all events
             * 2. If placed, call Unplace() to clear grid
             */
        }
        #endregion

        #region Public API - Placement
        /// <summary>
        /// Returns all grid cells this building occupies based on current position and rotation.
        /// Excludes cells in ignoredCells array for jagged shapes.
        /// Results are cached until position/rotation changes.
        /// </summary>
        public v2[] GetOccupiedCells()
        {
            /* TODO:
             * If cache is not dirty, return cachedOccupiedCells
             * 
             * Otherwise:
             * 1. Create list of cells
             * 2. For x from 0 to width-1:
             *    For y from 0 to length-1:
             *       - Create local cell position (x, y)
             *       - Check if cell is in ignoredCells array - if so, skip
             *       - Rotate local position based on currentRotation
             *       - Add gridOrigin to get final position
             *       - Add to list
             * 3. Cache result
             * 4. Set dirty flag = false
             * 5. Return array
             */
            return new v2[0];
        }

        /// <summary>
        /// Checks if building can be placed at current position.
        /// Validates: grid bounds, tile types, existing buildings, fog of war, resource nodes.
        /// </summary>
        public bool CanPlace()
        {
            /* TODO:
             * 1. Get occupied cells via GetOccupiedCells()
             * 2. Check if all cells are in grid bounds
             * 3. For each cell:
             *    - Get tile from GridManager
             *    - Check tile.CanBuildOn(canPlaceOnPath)
             *    - If requiresResourceNode:
             *       * Check at least one tile has resourceType matching requiredResourceType
             *    - Check fog of war (if enabled and tile not visible, reject)
             * 4. If buildingData.Cost exists, check if player can afford
             * 5. Return true if all checks pass
             */
            return false;
        }

        /// <summary>
        /// Places building on grid, deducts cost, fires events.
        /// Returns false if placement fails validation.
        /// </summary>
        public bool Place()
        {
            /* TODO:
             * 1. If already placed, return false
             * 2. Call CanPlace() - if false, return false
             * 3. Deduct cost from PlayerData
             * 4. Occupy grid cells via GridManager.OccupyTiles()
             * 5. Set isPlaced = true
             * 6. If isGhost, convert to real building:
             *    - Restore original materials
             *    - Set isGhost = false
             * 7. Play placement sound
             * 8. Fire OnPlaced event
             * 9. Fire GameEvents.OnBuildingPlaced
             * 10. Mark fog of war area as visible (if fog enabled)
             * 11. Return true
             */
            return false;
        }

        /// <summary>
        /// Removes building from grid (destroy without refund).
        /// Fires events.
        /// </summary>
        public void Unplace()
        {
            /* TODO:
             * 1. If not placed, return
             * 2. Clear grid cells via GridManager.ClearTiles()
             * 3. Set isPlaced = false
             * 4. Fire OnUnplaced event
             * 5. Fire GameEvents.OnBuildingDestroyed
             */
        }

        /// <summary>
        /// Sell building - unplace and refund partial cost.
        /// </summary>
        public void Sell()
        {
            /* TODO:
             * 1. If not placed, return
             * 2. Calculate refund (50% of buildingData.Cost)
             * 3. Add refund to PlayerData
             * 4. Clear grid cells
             * 5. Play sell sound
             * 6. Fire OnSold event
             * 7. Fire GameEvents.OnBuildingSold
             * 8. Destroy GameObject
             */
        }

        /// <summary>
        /// Move building to new grid position (for edit mode).
        /// Validates new position before moving.
        /// </summary>
        public bool MoveTo(v2 newGridPosition)
        {
            /* TODO:
             * 1. Store old position
             * 2. If placed, clear current grid cells
             * 3. Set gridOrigin = newGridPosition
             * 4. Mark cache dirty
             * 5. If !CanPlace():
             *    - Revert to old position
             *    - Re-occupy old cells if was placed
             *    - Return false
             * 6. If placed, occupy new cells
             * 7. Update worldPosition
             * 8. Update transform.position
             * 9. Fire OnPositionChanged event
             * 10. Return true
             */
            return false;
        }

        /// <summary>
        /// Rotates building 90° clockwise. Validates new orientation.
        /// </summary>
        public void Rotate()
        {
            /* TODO:
             * 1. If !canRotate, return
             * 2. Store old rotation
             * 3. Increment currentRotation by 90 (mod 360)
             * 4. Update orientation enum
             * 5. Mark cache dirty
             * 6. If placed and !CanPlace():
             *    - Revert rotation
             *    - Show error message/feedback
             *    - Return
             * 7. If placed, update grid occupation
             * 8. Update visual rotation (transform.rotation)
             * 9. Update child parts rotation
             * 10. Fire OnRotated event
             * 11. Fire GameEvents.OnBuildingRotated
             */
        }

        /// <summary>
        /// Update visual feedback (green/red material) based on CanPlace().
        /// Called by BuildModeController during ghost mode.
        /// </summary>
        public void UpdateVisualFeedback()
        {
            /* TODO:
             * 1. Determine if placement is valid via CanPlace()
             * 2. If valid: Apply validPlacementMaterial
             * 3. If invalid: Apply invalidPlacementMaterial
             * 4. Apply to all renderers
             * 5. Update child parts materials
             */
        }
        #endregion

        #region Public API - Ghost Mode
        /// <summary>
        /// Convert this building to a ghost preview
        /// </summary>
        public void SetAsGhost()
        {
            /* TODO:
             * 1. Set isGhost = true
             * 2. Apply ghost material to all renderers
             * 3. Disable any active components (combat, production, etc.)
             * 4. Enable visual feedback updates
             */
        }

        /// <summary>
        /// Convert ghost to real building
        /// </summary>
        public void ConvertFromGhost()
        {
            /* TODO:
             * 1. Set isGhost = false
             * 2. Restore original materials
             * 3. Enable components
             * 4. Disable visual feedback updates
             */
        }
        #endregion

        #region Public API - Properties
        /// <summary>
        /// Get the grid position of the building's origin (bottom-left corner)
        /// </summary>
        public v2 GetGridOrigin()
        {
            return gridOrigin;
        }

        /// <summary>
        /// Set grid position (updates world position automatically)
        /// </summary>
        public void SetGridPosition(v2 gridPos)
        {
            /* TODO:
             * 1. Set gridOrigin = gridPos
             * 2. Calculate worldPosition from GridManager.GridToWorld()
             * 3. Update transform.position
             * 4. Mark cache dirty
             */
        }

        /// <summary>
        /// Get size based on current rotation
        /// </summary>
        public v2 GetRotatedSize()
        {
            /* TODO:
             * If rotation is 90 or 270 degrees:
             * - Return (length, width)  // Swap dimensions
             * Else:
             * - Return (width, length)
             */
            return (width, length);
        }
        #endregion

        #region Helper Methods (Private)
        /// <summary>
        /// Rotate a local offset based on current rotation
        /// </summary>
        private v2 RotateOffset(v2 offset, int rotation)
        {
            /* TODO:
             * Switch on rotation:
             * - 0°: return offset as-is
             * - 90°: return (-offset.y, offset.x)
             * - 180°: return (-offset.x, -offset.y)
             * - 270°: return (offset.y, -offset.x)
             */
            return offset;
        }

        /// <summary>
        /// Apply material to all renderers
        /// </summary>
        private void ApplyMaterial(Material mat)
        {
            /* TODO:
             * For each renderer:
             * - Set renderer.material = mat
             */
        }

        /// <summary>
        /// Restore original materials
        /// </summary>
        private void RestoreOriginalMaterials()
        {
            /* TODO:
             * For each renderer with index i:
             * - Set renderer.material = originalMaterials[i]
             */
        }

        /// <summary>
        /// Update child building parts positions/rotations
        /// </summary>
        private void UpdateChildParts()
        {
            /* TODO:
             * For each child part:
             * 1. Rotate relative position based on currentRotation
             * 2. Calculate world position
             * 3. Update child GameObject transform
             */
        }

        /// <summary>
        /// Mark occupied cells cache as dirty
        /// </summary>
        private void InvalidateCache()
        {
            occupiedCellsDirty = true;
        }

        /// <summary>
        /// Check if a resource node is at any occupied cell
        /// </summary>
        private bool HasResourceNodeAt(v2[] cells, ResourceType resourceType)
        {
            /* TODO:
             * For each cell in cells:
             * 1. Get tile from GridManager
             * 2. If tile.type == TileType.ResourceNode && tile.resourceType == resourceType:
             *    - Return true
             * Return false if no match found
             */
            return false;
        }
        #endregion

        #region Debug Helpers
        private void OnDrawGizmosSelected()
        {
            /* TODO:
             * If in editor:
             * - Draw grid cells this building occupies
             * - Color green if CanPlace(), red otherwise
             * - Draw ignored cells in yellow
             */
        }
        #endregion
    }

    /// <summary>
    /// Data for child building parts (e.g., 2x2 extractor with 4 sub-parts)
    /// </summary>
    [System.Serializable]
    public class ChildBuildingPart
    {
        public GameObject gameObject;
        public v2 relativePosition;           // Position relative to building origin
        public EOrientation relativeRotation; // Rotation relative to building
        public bool inheritRotation = true;   // Should this part rotate with the building?
    }

    #endregion

    #region ═══════════════════════════════════════════════════════════════
    #region BUILDING DATA ARCHITECTURE (ScriptableObjects)
    #endregion
    #region ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Base ScriptableObject for all building types.
    /// Contains common properties shared by all buildings.
    /// 
    /// INHERITANCE HIERARCHY:
    /// BuildingData (base)
    /// ├── TowerData
    /// ├── ConveyorData
    /// ├── ExtractorData
    /// ├── ProcessorData
    /// ├── BeaconData
    /// └── StorageData
    /// </summary>
    [CreateAssetMenu(fileName = "NewBuilding", menuName = "TowerFactory/Buildings/Base Building", order = 0)]
    public class BuildingData : ScriptableObject
    {
        #region Identity
        [Header("Identity")]
        public string buildingName = "New Building";
        [TextArea(3, 5)]
        public string description = "";
        public Sprite icon;
        public BuildingCategory category = BuildingCategory.Production;
        #endregion

        #region Prefabs
        [Header("Prefabs")]
        [Tooltip("The actual building prefab with all components")]
        public GameObject buildingPrefab;
        
        [Tooltip("Ghost preview prefab for placement (optional, uses buildingPrefab if null)")]
        public GameObject ghostPrefab;
        #endregion

        #region Grid Placement
        [Header("Grid Placement")]
        public v2 size = (1, 1);              // Width × Length
        
        [Tooltip("For jagged shapes (L, T, etc.) - cells to exclude from footprint")]
        public v2[] ignoredCells = new v2[0];
        
        public bool canRotate = true;
        public bool canPlaceOnPath = false;
        public bool requiresResourceNode = false;
        public ResourceType? requiredResourceType = null;
        #endregion

        #region Cost & Economy
        [Header("Cost & Economy")]
        public Cost buildCost = new Cost();
        
        [Tooltip("Percentage of build cost refunded when sold (default 50%)")]
        [Range(0f, 1f)]
        public float sellRefundPercentage = 0.5f;
        #endregion

        #region Visual & Audio
        [Header("Visual")]
        public Material validPlacementMaterial;
        public Material invalidPlacementMaterial;
        
        [Header("Audio")]
        public AudioData placementSound;
        public AudioData destroySound;
        public AudioData sellSound;
        #endregion

        #region Upgrade Support
        [Header("Upgrade System")]
        public bool canBeUpgraded = false;
        public BuildingData[] upgradesTo;     // Next tier buildings
        #endregion

        #region Helper Methods
        /// <summary>
        /// Calculate sell refund cost
        /// </summary>
        public Cost GetSellRefund()
        {
            /* TODO:
             * Create new Cost with:
             * - money = buildCost.money * sellRefundPercentage
             * - resources = each buildCost resource * sellRefundPercentage
             */
            return null;
        }

        /// <summary>
        /// Get total footprint area (excluding ignored cells)
        /// </summary>
        public int GetFootprintArea()
        {
            /* TODO:
             * Return (size.x * size.y) - ignoredCells.Length
             */
            return 0;
        }
        #endregion
    }

    /// <summary>
    /// ScriptableObject for Tower buildings (combat structures)
    /// </summary>
    [CreateAssetMenu(fileName = "NewTower", menuName = "TowerFactory/Buildings/Tower", order = 1)]
    public class TowerData : BuildingData
    {
        #region Combat Stats
        [Header("Combat Stats")]
        public float range = 10f;
        public float damage = 10f;
        public float fireRate = 1f;           // Attacks per second
        public DamageType damageType = DamageType.Physical;
        
        [Header("Projectile")]
        public float projectileSpeed = 20f;
        public GameObject projectilePrefab;
        public bool useSplashDamage = false;
        public float splashRadius = 0f;
        public float splashDamagePercentage = 0.5f;
        #endregion

        #region Targeting
        [Header("Targeting")]
        public TargetingStrategy defaultTargetingStrategy = TargetingStrategy.First;
        public bool canTargetGround = true;
        public bool canTargetFlying = false;
        #endregion

        #region Visual & Effects
        [Header("Visual")]
        public Transform firePoint;           // Where projectiles spawn
        public GameObject muzzleFlashPrefab;
        public GameObject rangeIndicatorPrefab;
        
        [Header("Audio")]
        public AudioData fireSound;
        public AudioData reloadSound;
        #endregion

        #region Upgrade Paths
        [Header("Tower Upgrades")]
        public StatModifier[] levelUpModifiers; // Stats gained per level
        public int maxLevel = 5;
        #endregion
    }

    /// <summary>
    /// ScriptableObject for Conveyor Belt buildings
    /// </summary>
    [CreateAssetMenu(fileName = "NewConveyor", menuName = "TowerFactory/Buildings/Conveyor", order = 2)]
    public class ConveyorData : BuildingData
    {
        #region Conveyor Properties
        [Header("Conveyor Properties")]
        public ConveyorType conveyorType = ConveyorType.Straight;
        public float itemSpeed = 2f;         // Units per second
        public int maxItemsOnBelt = 4;
        
        [Tooltip("For splitters: how items are distributed")]
        public bool alternateSplit = true;
        
        [Tooltip("For underground: how many tiles it skips")]
        public int undergroundDistance = 4;
        #endregion

        #region Visual & Animation
        [Header("Visual")]
        public Material beltMaterial;
        public float beltAnimationSpeed = 1f;
        public Vector2 textureScrollSpeed = new Vector2(0f, 0.5f);
        #endregion

        #region Direction Configuration
        [Header("Direction Configuration")]
        [Tooltip("Input direction (local space)")]
        public v2 inputDirection = (0, -1);  // Default: from bottom
        
        [Tooltip("Output direction (local space)")]
        public v2 outputDirection = (0, 1);  // Default: to top
        
        [Tooltip("For splitters/combiners")]
        public v2[] additionalInputs = new v2[0];
        public v2[] additionalOutputs = new v2[0];
        #endregion
    }

    /// <summary>
    /// ScriptableObject for Extractor buildings (resource gathering)
    /// </summary>
    [CreateAssetMenu(fileName = "NewExtractor", menuName = "TowerFactory/Buildings/Extractor", order = 3)]
    public class ExtractorData : BuildingData
    {
        #region Extraction Properties
        [Header("Extraction")]
        public ResourceType extractedResource;
        public float extractionRate = 1f;    // Items per second
        public bool requiresResourceNode = true;
        public bool depletesNode = true;     // Does extraction reduce node amount?
        #endregion

        #region Output Configuration
        [Header("Output")]
        public v2 outputDirection = (1, 0);  // Local space direction to output belt
        public int outputAmountPerCycle = 1;
        #endregion

        #region Area Extraction
        [Header("Area Extraction (Advanced)")]
        public bool extractFromArea = false;
        public int extractionRadius = 1;     // For area extractors
        #endregion

        #region Visual & Audio
        [Header("Visual")]
        public GameObject extractionVFXPrefab;
        public Transform extractionPoint;
        
        [Header("Audio")]
        public AudioData extractionSound;
        public bool loopExtractionSound = true;
        #endregion
    }

    /// <summary>
    /// ScriptableObject for Processor buildings (crafting/recipes)
    /// </summary>
    [CreateAssetMenu(fileName = "NewProcessor", menuName = "TowerFactory/Buildings/Processor", order = 4)]
    public class ProcessorData : BuildingData
    {
        #region Processing Properties
        [Header("Processing")]
        public Recipe[] supportedRecipes;
        public float processingSpeedMultiplier = 1f;
        public int maxInputBuffer = 10;      // How many items can queue
        public int maxOutputBuffer = 10;
        #endregion

        #region Input/Output Configuration
        [Header("Input/Output Directions")]
        [Tooltip("Where this processor accepts items from (local space)")]
        public v2[] inputDirections = new v2[] { (0, -1) };
        
        [Tooltip("Where this processor outputs items to (local space)")]
        public v2[] outputDirections = new v2[] { (0, 1) };
        #endregion

        #region Visual & Audio
        [Header("Visual")]
        public GameObject processingVFXPrefab;
        public Transform processingPoint;
        
        [Header("Audio")]
        public AudioData processingSound;
        public AudioData completeSound;
        #endregion
    }

    /// <summary>
    /// ScriptableObject for Beacon buildings (buff/debuff area effects)
    /// </summary>
    [CreateAssetMenu(fileName = "NewBeacon", menuName = "TowerFactory/Buildings/Beacon", order = 5)]
    public class BeaconData : BuildingData
    {
        #region Beacon Properties
        [Header("Beacon Properties")]
        public float effectRadius = 5f;
        public StatModifier[] modifiers;     // What stats this beacon modifies
        
        [Tooltip("What types of buildings this beacon affects")]
        public BuildingCategory[] affectedCategories = new BuildingCategory[] 
        { 
            BuildingCategory.Defense 
        };
        #endregion

        #region Visual
        [Header("Visual")]
        public GameObject rangeIndicatorPrefab;
        public Color effectColor = Color.cyan;
        public GameObject buffVFXPrefab;
        #endregion
    }

    /// <summary>
    /// ScriptableObject for Storage buildings
    /// </summary>
    [CreateAssetMenu(fileName = "NewStorage", menuName = "TowerFactory/Buildings/Storage", order = 6)]
    public class StorageData : BuildingData
    {
        #region Storage Properties
        [Header("Storage Properties")]
        public int storageCapacity = 100;
        public ResourceType[] allowedResourceTypes; // Empty = all types allowed
        #endregion

        #region Input/Output
        [Header("Input/Output")]
        public v2[] inputDirections = new v2[0];
        public v2[] outputDirections = new v2[0];
        public bool autoOutput = false;
        public float outputRate = 1f;        // Items per second if autoOutput
        #endregion
    }

    #endregion

    #region ═══════════════════════════════════════════════════════════════
    #region INPUT MANAGEMENT SYSTEM
    #endregion
    #region ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Centralized input manager. Handles all mouse/keyboard input.
    /// Routes input to appropriate systems based on current input mode.
    /// 
    /// INPUT PRIORITY (highest to lowest):
    /// 1. UI elements (if mouse over UI, ignore game input)
    /// 2. Build mode (placing/rotating buildings)
    /// 3. Edit mode (moving/selling buildings)
    /// 4. Selection (clicking buildings/enemies)
    /// 5. Camera control
    /// 
    /// DEPENDENCIES:
    /// - New Input System (Unity's InputSystem)
    /// - EventSystem (for UI detection)
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        #region Singleton
        public static InputManager Instance { get; private set; }
        #endregion

        #region Inspector Fields
        [Header("Input Layers")]
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private LayerMask buildingLayer;
        [SerializeField] private LayerMask resourceLayer;
        [SerializeField] private LayerMask enemyLayer;
        [SerializeField] private LayerMask uiLayer;

        [Header("Input Settings")]
        [SerializeField] private float dragThreshold = 0.1f;
        [SerializeField] private float doubleClickTime = 0.3f;
        
        [Header("Keybindings")]
        [SerializeField] private KeyCode rotateKey = KeyCode.R;
        [SerializeField] private KeyCode sellKey = KeyCode.X;
        [SerializeField] private KeyCode pauseKey = KeyCode.Space;
        [SerializeField] private KeyCode cancelKey = KeyCode.Escape;
        #endregion

        #region Private Fields
        private Camera mainCamera;
        private EInputMode currentInputMode = EInputMode.Standard;
        
        // Mouse state
        private Vector3 mouseWorldPosition;
        private v2 mouseGridPosition;
        private bool isMouseOverUI;
        
        // Dragging state
        private bool isDragging = false;
        private Vector3 dragStartPosition;
        private PlacementComponent draggedBuilding;
        
        // Click detection
        private float lastClickTime;
        private Vector3 lastClickPosition;
        
        // Selection
        private ISelectable currentSelection;
        
        // Input System actions
        private InputAction mousePositionAction;
        private InputAction mouseClickAction;
        private InputAction mouseRightClickAction;
        #endregion

        #region Properties
        public EInputMode CurrentInputMode => currentInputMode;
        public Vector3 MouseWorldPosition => mouseWorldPosition;
        public v2 MouseGridPosition => mouseGridPosition;
        public bool IsMouseOverUI => isMouseOverUI;
        public ISelectable CurrentSelection => currentSelection;
        #endregion

        #region Events
        public event Action<Vector3> OnGroundClicked;
        public event Action<PlacementComponent> OnBuildingClicked;
        public event Action<Enemy> OnEnemyClicked;
        public event Action<Tile> OnResourceNodeClicked;
        public event Action OnCancelAction;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            /* TODO:
             * 1. Setup singleton
             * 2. Get main camera reference
             * 3. Setup Input System actions
             */
            
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            /* TODO:
             * Enable Input System actions
             */
        }

        private void OnDisable()
        {
            /* TODO:
             * Disable Input System actions
             */
        }

        private void Update()
        {
            /* TODO:
             * 1. Update mouse world position
             * 2. Update mouse grid position
             * 3. Check if mouse is over UI
             * 4. Handle keyboard input (rotate, sell, pause, cancel)
             * 5. Handle mouse input based on currentInputMode
             * 6. Update drag state
             */
        }
        #endregion

        #region Input Mode Management
        /// <summary>
        /// Set the current input mode
        /// </summary>
        public void SetInputMode(EInputMode mode)
        {
            /* TODO:
             * 1. Store previous mode
             * 2. Set currentInputMode = mode
             * 3. Cleanup previous mode (cancel drags, etc.)
             * 4. Initialize new mode
             */
        }
        #endregion

        #region Mouse Input Handling
        /// <summary>
        /// Handle left mouse click
        /// </summary>
        private void HandleLeftClick()
        {
            /* TODO:
             * Check mouse over UI - if true, return early
             * 
             * Raycast priority:
             * 1. Buildings: Select or start drag
             * 2. Enemies: Select enemy
             * 3. Resource nodes: Collect resource
             * 4. Ground: Deselect or place building (if in build mode)
             * 
             * If double-click detected:
             * - Select all buildings of same type
             */
        }

        /// <summary>
        /// Handle right mouse click
        /// </summary>
        private void HandleRightClick()
        {
            /* TODO:
             * 1. If in build mode: Cancel and return to standard mode
             * 2. If dragging: Cancel drag
             * 3. Otherwise: Deselect current selection
             */
        }

        /// <summary>
        /// Handle mouse drag start
        /// </summary>
        private void HandleDragStart(PlacementComponent building)
        {
            /* TODO:
             * 1. If currentInputMode != EditMode, return
             * 2. Set isDragging = true
             * 3. Set draggedBuilding = building
             * 4. Store dragStartPosition
             * 5. Unplace building from grid (but keep GameObject)
             * 6. Convert building to ghost mode
             */
        }

        /// <summary>
        /// Handle mouse drag update
        /// </summary>
        private void HandleDragUpdate()
        {
            /* TODO:
             * 1. If !isDragging, return
             * 2. Update draggedBuilding position to mouse grid position
             * 3. Update visual feedback (green/red)
             */
        }

        /// <summary>
        /// Handle mouse drag end
        /// </summary>
        private void HandleDragEnd()
        {
            /* TODO:
             * 1. If !isDragging, return
             * 2. Try to place draggedBuilding at current position
             * 3. If placement fails:
             *    - Return to original position
             *    - Or destroy if was newly created
             * 4. Reset drag state
             * 5. Convert from ghost mode
             */
        }
        #endregion

        #region Keyboard Input Handling
        /// <summary>
        /// Handle rotate key press
        /// </summary>
        private void HandleRotateInput()
        {
            /* TODO:
             * If currentSelection is PlacementComponent:
             * - Call selection.Rotate()
             * If in build mode with ghost building:
             * - Rotate ghost building
             */
        }

        /// <summary>
        /// Handle sell key press
        /// </summary>
        private void HandleSellInput()
        {
            /* TODO:
             * If currentSelection is PlacementComponent:
             * - Show sell confirmation
             * - Call selection.Sell()
             */
        }

        /// <summary>
        /// Handle pause key press
        /// </summary>
        private void HandlePauseInput()
        {
            /* TODO:
             * Toggle GameManager pause state
             */
        }

        /// <summary>
        /// Handle cancel/escape key press
        /// </summary>
        private void HandleCancelInput()
        {
            /* TODO:
             * Priority:
             * 1. If in build mode: Exit build mode
             * 2. If dragging: Cancel drag
             * 3. If selection active: Deselect
             * 4. Otherwise: Open pause menu
             */
        }
        #endregion

        #region Raycasting Helpers
        /// <summary>
        /// Raycast to find object at mouse position
        /// </summary>
        private bool RaycastAtMouse(LayerMask layerMask, out RaycastHit hit)
        {
            /* TODO:
             * 1. Create ray from camera through mouse position
             * 2. Perform raycast with layerMask
             * 3. Return true if hit something
             */
            hit = new RaycastHit();
            return false;
        }

        /// <summary>
        /// Check if mouse is currently over UI
        /// </summary>
        private bool IsPointerOverUI()
        {
            /* TODO:
             * Use EventSystem.current.IsPointerOverGameObject()
             */
            return false;
        }

        /// <summary>
        /// Get world position of mouse on ground plane
        /// </summary>
        private Vector3 GetMouseWorldPosition()
        {
            /* TODO:
             * 1. Create ray from camera through mouse
             * 2. Raycast against ground layer
             * 3. Return hit point or Vector3.zero
             */
            return Vector3.zero;
        }
        #endregion

        #region Selection Management
        /// <summary>
        /// Select an object
        /// </summary>
        public void Select(ISelectable selectable)
        {
            /* TODO:
             * 1. Deselect current selection
             * 2. Set currentSelection = selectable
             * 3. Call selectable.OnSelected()
             * 4. Fire OnObjectSelected event
             * 5. Fire GameEvents.OnObjectSelected
             */
        }

        /// <summary>
        /// Deselect current selection
        /// </summary>
        public void Deselect()
        {
            /* TODO:
             * 1. If currentSelection != null:
             *    - Call currentSelection.OnDeselected()
             * 2. Set currentSelection = null
             * 3. Fire OnObjectDeselected event
             * 4. Fire GameEvents.OnObjectDeselected
             */
        }
        #endregion
    }

    /// <summary>
    /// Interface for selectable objects (buildings, enemies, etc.)
    /// </summary>
    public interface ISelectable
    {
        GameObject GameObject { get; }
        string GetDisplayName();
        Sprite GetIcon();
        string GetDescription();
        void OnSelected();
        void OnDeselected();
    }

    #endregion
}
