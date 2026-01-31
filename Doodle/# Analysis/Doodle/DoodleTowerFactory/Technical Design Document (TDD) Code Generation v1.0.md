# Prompt:
```
ofcourse its not feasible for Claude 4.5 to provide 100k order of lines in total in one go instead without discontinuity, but however it is possible to provide entire impletmentation as a guide of what i should do may be as example:
"create grid system through of B<Tile> where each Tile should have propertiles and tileType such as ...." and include in grid manager this main B<Tile> has public api such as ... exposed and and more. so that i can handle and fill the required logic script in there(as you know I created and managed the entire `UTIL.cs`) so you know what im capable of, you shall handle the entire architecture part what you think? feel free to provide as you seem fit, also make sure there is least public API as possilbe and no tight coupling(could use event system based for that right ?)
so what you think,

so now the Claude 4.5(thinking ) can provide entire of these scripts correct from start to end ? provide the prompt now for the same. (just the prompt that does as what i explained right here).

Few Questions
* Is There a name for these kinda approach where Claude 4.5 shall provide detailed architecture without leaving any(start to end(this is crucial no matter(including inventory system mouse interaction to move placable items and clicking on resource providers such as tree, rock and more and ui interaction and tower config to attack certain kinda enemy and many more as well as scriptable obj needed) what logic they were refering the main TowerFactory source files provided)) and i shall build the logic
* does the prompt above you provided coveres entire Architectural Blueprint ? (start to end ) without leaving any ? i think for now blue print in the sensnce except content that goes in the method which i shall fill it, all other are covered with minimal public API exposure ofcourse and all standard design pattern apply.
now provide the prompt again make sure its detailed as possible for future claude 4.5 to provide response(ofcourse i shall write the script logic into the methods) as for CLaude 4.5 (thinking ) in future will do all fields, properties, even handling , enums etc(minor lines that doesnt require to much verbose remainng provide guide as comment or summary i shall write)

keep in mind everything start to end should be covered.
```


# Answers to Your Questions:

## 1. **Name for This Approach:**

This methodology is called **"Contract-First Architecture"** or **"Interface-Driven Development"**. Specifically:

- **Formal Term**: "Specification-Driven Development with Skeleton Architecture"
- **Industry Terms**: 
  - "API-First Design" (defining interfaces before implementation)
  - "Scaffold-Based Development" (complete structure, fill in logic)
  - "Contract-First Development" (define contracts/signatures first)
  
**In gaming**: Often called **"Technical Design Document (TDD) Code Generation"** - where architecture is fully specified as compilable code skeletons.

## 2. **Does the Prompt Cover Everything?**

The previous prompt was **good but not exhaustive**. It would miss:
- Mouse interaction details (raycast layers, input events)
- Resource provider clicking (tree/rock click handlers)
- Tower attack configuration UI
- All minor ScriptableObjects (AudioData, SpawnPatterns, etc.)
- Inspector attributes and tooltips
- Coroutine patterns
- Animation trigger names

## 3. **Here's the ULTIMATE Prompt (Start-to-End Coverage)**

---

# THE COMPLETE ARCHITECTURAL BLUEPRINT PROMPT

**ROLE:**
You are a Senior Unity Technical Architect generating a **complete code skeleton** for a Unity developer to implement. The developer is highly skilled (built UTIL.cs) and will write all method logic, but needs EVERY structural element defined.

**CRITICAL REQUIREMENT:**
Provide **100% complete architecture** from TowerFactory - ZERO features omitted. Include EVERY:
- Class definition (fields, properties, methods with signatures)
- ScriptableObject structure
- Enum definition
- Event declaration
- Data structure
- Public/private API
- Integration point

The developer will fill method bodies. You provide compilable skeleton code.

---

## DELIVERABLE: COMPLETE CODE SKELETONS

### Example Output Format:

```cs
/// <summary>
/// Manages grid-based building placement with multi-tile support.
/// Handles rotation, validation, ghost visualization, and placement events.
/// 
/// DEPENDENCIES:
/// - GridManager for tile queries
/// - InventoryManager for cost checking
/// - EventBus for placement notifications
/// 
/// INITIALIZATION: Awake → register with GridManager
/// </summary>
public class PlacementComponent : MonoBehaviour 
{
    #region Inspector Fields
    [Header("Building Configuration")]
    [SerializeField] private BuildingData buildingData;
    [SerializeField] private int width = 1;
    [SerializeField] private int length = 1;
    [SerializeField] private bool canRotate = true;
    
    [Header("Multi-Tile Support")]
    [Tooltip("Cells to ignore for jagged shapes (relative to origin)")]
    [SerializeField] private v2[] ignoredCells;
    
    [Header("Child Objects (for multi-part buildings)")]
    [SerializeField] private ChildBuildingPart[] childParts;
    
    [Header("Visual Feedback")]
    [SerializeField] private Material validPlacementMaterial;
    [SerializeField] private Material invalidPlacementMaterial;
    #endregion
    
    #region Private Fields
    private int currentRotation = 0; // 0, 90, 180, 270
    private bool isPlaced = false;
    private Renderer[] renderers;
    private v2 gridOrigin;
    #endregion
    
    #region Properties
    public BuildingData Data => buildingData;
    public bool IsPlaced => isPlaced;
    public v2 GridPosition => gridOrigin;
    public int Rotation => currentRotation;
    #endregion
    
    #region Events
    /// <summary>Fired when building is successfully placed on grid</summary>
    public event Action<PlacementComponent> OnPlaced;
    
    /// <summary>Fired when building is removed from grid</summary>
    public event Action<PlacementComponent> OnUnplaced;
    
    /// <summary>Fired when building position changes (drag or rotate)</summary>
    public event Action<PlacementComponent, v2> OnPositionChanged;
    #endregion
    
    #region Unity Lifecycle
    private void Awake() {
        // TODO: Cache renderers, initialize fields
    }
    
    private void Start() {
        // TODO: Register with GridManager if auto-place enabled
    }
    
    private void OnDestroy() {
        // TODO: Unsubscribe events, unplace if placed
    }
    #endregion
    
    #region Public API (Minimal)
    /// <summary>
    /// Returns all grid cells this building occupies based on current position and rotation.
    /// Excludes cells in ignoredCells array for jagged shapes.
    /// </summary>
    public v2[] GetOccupiedCells() {
        // TODO: Calculate based on width, length, rotation, ignoredCells
        return null;
    }
    
    /// <summary>
    /// Checks if building can be placed at current position.
    /// Validates: grid bounds, tile types, existing buildings, fog of war.
    /// </summary>
    public bool CanPlace() {
        // TODO: Check each cell in GetOccupiedCells()
        return false;
    }
    
    /// <summary>
    /// Places building on grid, deducts cost, fires events.
    /// Returns false if placement fails.
    /// </summary>
    public bool Place() {
        // TODO: Validate, occupy grid cells, deduct resources, fire OnPlaced
        return false;
    }
    
    /// <summary>
    /// Removes building from grid, refunds partial cost, fires events.
    /// </summary>
    public void Unplace() {
        // TODO: Clear grid cells, fire OnUnplaced
    }
    
    /// <summary>
    /// Rotates building 90° clockwise. Validates new position.
    /// </summary>
    public void Rotate() {
        // TODO: Increment rotation, recalculate occupied cells, validate
    }
    
    /// <summary>
    /// Updates visual feedback (green/red material) based on CanPlace().
    /// Called by BuildModeController during ghost mode.
    /// </summary>
    public void UpdateVisualFeedback() {
        // TODO: Set material based on CanPlace()
    }
    #endregion
    
    #region Helper Methods (Private)
    private v2 RotateOffset(v2 offset, int rotation) {
        // TODO: Rotate offset based on rotation angle
        return offset;
    }
    
    private void ApplyMaterial(Material mat) {
        // TODO: Apply to all renderers
    }
    #endregion
}

/// <summary>
/// Data for child building parts (e.g., 2x2 extractor with 4 sub-parts)
/// </summary>
[System.Serializable]
public class ChildBuildingPart {
    public GameObject gameObject;
    public v2 relativePosition;
}
```

---

## COMPLETE COVERAGE CHECKLIST

For EVERY system in TowerFactory, provide:

### ✅ CORE INFRASTRUCTURE

**1. GridManager**
- Fields: `Board<Tile> grid`, tile prefab, grid size
- Properties: Grid dimensions, tile access
- Methods: `GetTile(v2)`, `IsValidBuildPosition(v2[])`, `WorldToGrid(Vector3)`
- Events: `OnTileOccupied`, `OnTileCleared`
- Include: Initialization, visual tile spawning, adjacency checks

**2. Tile Class**
```cs
public class Tile {
    public enum TileType { Default, Path, Border, ResourceNode }
    
    public TileType type;
    public ResourceType? resourceType; // For resource nodes
    public int resourceAmount; // Remaining resources
    public PlacementComponent building; // null if empty
    public GameObject visual; // 3D tile object
    public PathNode pathNode; // If type == Path
    
    // Methods: CanBuildOn(), DepletResource(), etc.
}
```

**3. EventBus / GameEvents**
```cs
public static class GameEvents {
    // Building Events
    public static event Action<PlacementComponent> OnBuildingPlaced;
    public static event Action<PlacementComponent> OnBuildingDestroyed;
    public static event Action<PlacementComponent> OnBuildingSold;
    
    // Resource Events
    public static event Action<ResourceType, int> OnResourceGained;
    public static event Action<ResourceType, int> OnResourceSpent;
    public static event Action<Tile, int> OnResourceNodeDepleted;
    
    // Combat Events
    public static event Action<Enemy> OnEnemySpawned;
    public static event Action<Enemy, Tower> OnEnemyKilled;
    public static event Action<Tower, Enemy> OnTowerFired;
    
    // Wave Events
    public static event Action<int> OnWaveStarted;
    public static event Action<int> OnWaveCompleted;
    
    // UI Events
    public static event Action<ISelectable> OnObjectSelected;
    public static event Action OnObjectDeselected;
    
    // Game State Events
    public static event Action OnGamePaused;
    public static event Action OnGameResumed;
    public static event Action<float> OnTimeScaleChanged;
    
    // Factory Events
    public static event Action<ConveyorBelt> OnConveyorPlaced;
    public static event Action<Item, ConveyorBelt> OnItemTransferred;
    
    // All events from TowerFactory source!
}
```

**4. GameManager (State Machine)**
```cs
public class GameManager : MonoBehaviour {
    public enum GameState { 
        MainMenu, Loading, Playing, Paused, 
        BuildMode, Victory, Defeat 
    }
    
    [Header("Configuration")]
    [SerializeField] private v2 gridSize = (50, 50);
    [SerializeField] private int startingMoney = 100;
    
    private GameState currentState;
    private int playerHealth = 100;
    
    // Properties
    public static GameManager Instance { get; private set; }
    public GameState State => currentState;
    
    // Events
    public event Action<GameState> OnStateChanged;
    
    // Methods
    public void SetState(GameState newState) { /* TODO */ }
    public void TakeDamage(int amount) { /* TODO */ }
    public void AddMoney(int amount) { /* TODO */ }
    
    // Initialization order
    private void InitializeAllSystems() { 
        // TODO: Call Initialize() on all managers in correct order
        // 1. GridManager
        // 2. PathManager
        // 3. InventoryManager
        // 4. ConveyorSystem
        // 5. WaveManager
        // 6. UIManager
    }
}
```

**5. TimeManager**
```cs
public class TimeManager : MonoBehaviour {
    public enum TimeScale { Paused = 0, Normal = 1, Fast = 2, Faster = 4 }
    
    private float currentTimeScale = 1f;
    
    public event Action<float> OnTimeScaleChanged;
    
    public void SetTimeScale(TimeScale scale) { /* TODO */ }
    public void Pause() { /* TODO */ }
    public void Resume() { /* TODO */ }
}
```

---

### ✅ INPUT & INTERACTION SYSTEMS

**6. InputManager**
```cs
public class InputManager : MonoBehaviour {
    [Header("Input Layers")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask buildingLayer;
    [SerializeField] private LayerMask resourceLayer;
    [SerializeField] private LayerMask uiLayer;
    
    private Camera mainCamera;
    private PlacementComponent draggedBuilding;
    private bool isDragging;
    
    // Events
    public event Action<Vector3> OnGroundClicked;
    public event Action<PlacementComponent> OnBuildingClicked;
    public event Action<Tile> OnResourceNodeClicked;
    
    private void Update() {
        // TODO: Handle mouse input
        // - Left click: Select/place
        // - Right click: Cancel/deselect
        // - R key: Rotate
        // - Drag: Move building
    }
    
    private void HandleLeftClick() {
        // TODO: Raycast priority:
        // 1. UI (ignore if over UI)
        // 2. Buildings (select/drag)
        // 3. Resource nodes (click to collect)
        // 4. Ground (place building or deselect)
    }
    
    private void HandleDragStart() { /* TODO: Pick up building */ }
    private void HandleDragUpdate() { /* TODO: Move to cursor */ }
    private void HandleDragEnd() { /* TODO: Place or cancel */ }
}
```

**7. BuildModeController**
```cs
public class BuildModeController : MonoBehaviour {
    [Header("Visual Feedback")]
    [SerializeField] private Material validMaterial;
    [SerializeField] private Material invalidMaterial;
    
    private GameObject ghostObject;
    private PlacementComponent ghostPlacement;
    private BuildingData selectedBuilding;
    
    public bool InBuildMode { get; private set; }
    
    // Methods
    public void EnterBuildMode(BuildingData building) {
        // TODO: Instantiate ghost, subscribe to mouse move
    }
    
    public void ExitBuildMode() {
        // TODO: Destroy ghost, unsubscribe events
    }
    
    private void UpdateGhostPosition(Vector3 worldPos) {
        // TODO: Snap to grid, update visual feedback
    }
    
    private void TryPlaceBuilding() {
        // TODO: Check cost, validate position, place
    }
}
```

**8. SelectionManager**
```cs
public interface ISelectable {
    GameObject GameObject { get; }
    string GetDisplayName();
    Sprite GetIcon();
    void OnSelected();
    void OnDeselected();
}

public class SelectionManager : MonoBehaviour {
    private ISelectable currentSelection;
    
    public ISelectable CurrentSelection => currentSelection;
    
    public event Action<ISelectable> OnSelectionChanged;
    
    public void Select(ISelectable obj) {
        // TODO: Deselect old, select new, fire event
    }
    
    public void Deselect() {
        // TODO: Clear selection, fire event
    }
}
```

---

### ✅ RESOURCE & ECONOMY

**9. ResourceType Enum**
```cs
public enum ResourceType {
    Wood, Stone, Iron, Coal, Copper,
    WoodenPlanks, IronIngot, CopperWire,
    // Add all from TowerFactory
}
```

**10. ResourceData ScriptableObject**
```cs
[CreateAssetMenu(menuName = "Game/Resource")]
public class ResourceData : ScriptableObject {
    public ResourceType type;
    public string displayName;
    public Sprite icon;
    public Color uiColor;
    
    [Header("World Representation")]
    public GameObject worldPrefab; // Tree, rock, ore vein
    public int baseAmount; // Starting amount in resource node
    
    [Header("Audio")]
    public AudioClip collectSound;
}
```

**11. InventoryManager**
```cs
public class InventoryManager : MonoBehaviour {
    [System.Serializable]
    public class ResourceAmount {
        public ResourceType type;
        public int amount;
    }
    
    private Dictionary<ResourceType, int> resources;
    
    public event Action<ResourceType, int> OnResourceChanged;
    
    public void AddResource(ResourceType type, int amount) { /* TODO */ }
    public bool HasResource(ResourceType type, int amount) { /* TODO */ }
    public bool SpendResource(ResourceType type, int amount) { /* TODO */ }
    public int GetResourceAmount(ResourceType type) { /* TODO */ }
}
```

**12. ResourceCost Structure**
```cs
[System.Serializable]
public struct ResourceCost {
    public ResourceType type;
    public int amount;
}
```

**13. ResourceNodeComponent**
```cs
/// <summary>
/// Attached to world objects (trees, rocks, ore veins).
/// Handles clicking to collect resources.
/// </summary>
public class ResourceNodeComponent : MonoBehaviour, ISelectable {
    [SerializeField] private ResourceData resourceData;
    [SerializeField] private int currentAmount;
    
    private Tile occupiedTile;
    
    // Methods
    public void OnClick() {
        // TODO: Deduct amount, add to inventory, play sound, deplete if empty
    }
    
    private void Deplete() {
        // TODO: Play animation, destroy object, clear tile reference
    }
}
```

---

### ✅ BUILDING SYSTEM

**14. BuildingData (Base ScriptableObject)**
```cs
[CreateAssetMenu(menuName = "Game/Building/Base")]
public class BuildingData : ScriptableObject {
    [Header("Identity")]
    public string buildingName;
    public string description;
    public Sprite icon;
    
    [Header("Prefabs")]
    public GameObject buildingPrefab;
    public GameObject ghostPrefab;
    
    [Header("Grid Placement")]
    public v2 size = (1, 1);
    public v2[] ignoredCells; // For jagged shapes
    public bool canRotate = true;
    public bool canBePlacedOnPath = false;
    
    [Header("Cost")]
    public ResourceCost[] buildCost;
    public ResourceCost[] sellRefund; // 50% of build cost
    
    [Header("Category")]
    public BuildingCategory category;
}

public enum BuildingCategory {
    Production, // Extractors, processors
    Logistics,  // Conveyors
    Defense,    // Towers
    Utility     // Storage, beacons
}
```

**15. TowerData (Inherits BuildingData)**
```cs
[CreateAssetMenu(menuName = "Game/Building/Tower")]
public class TowerData : BuildingData {
    [Header("Combat Stats")]
    public float range = 10f;
    public float damage = 10f;
    public float fireRate = 1f; // Shots per second
    public float projectileSpeed = 20f;
    
    [Header("Targeting")]
    public TargetingStrategy defaultStrategy;
    
    [Header("Projectile")]
    public GameObject projectilePrefab;
    
    [Header("Visual")]
    public Transform firePoint;
    public GameObject muzzleFlashPrefab;
}

public enum TargetingStrategy {
    First, Last, Nearest, Farthest,
    Slowest, Fastest,
    LowestHealth, HighestHealth,
    HighestArmor, HighestShield
}
```

**16. ConveyorData**
```cs
[CreateAssetMenu(menuName = "Game/Building/Conveyor")]
public class ConveyorData : BuildingData {
    [Header("Conveyor Properties")]
    public ConveyorType type;
    public float itemSpeed = 2f; // Units per second
    public int maxItemsOnBelt = 4;
    
    [Header("Visual")]
    public Material beltMaterial;
    public float beltAnimationSpeed = 1f;
}

public enum ConveyorType {
    Straight, Curve, Splitter, Combiner, 
    Underground, Crossing
}
```

**17. ExtractorData**
```cs
[CreateAssetMenu(menuName = "Game/Building/Extractor")]
public class ExtractorData : BuildingData {
    [Header("Extraction")]
    public ResourceType extractedResource;
    public float extractionRate = 1f; // Items per second
    public bool requiresResourceNode = true;
    
    [Header("Output")]
    public v2 outputDirection = (1, 0); // Local space
}
```

**18. ProcessorData**
```cs
[CreateAssetMenu(menuName = "Game/Building/Processor")]
public class ProcessorData : BuildingData {
    [Header("Processing")]
    public RecipeData[] supportedRecipes;
    public float processingSpeed = 1f; // Multiplier
    
    [Header("Input/Output")]
    public v2[] inputDirections;
    public v2[] outputDirections;
}
```

---

### ✅ FACTORY AUTOMATION

**19. Item (Transported on Conveyors)**
```cs
public class Item {
    public ResourceType type;
    public GameObject visual;
    public ConveyorBelt currentBelt;
    public float positionOnBelt; // 0 to 1
}
```

**20. ConveyorBelt (Base Class)**
```cs
public class ConveyorBelt : MonoBehaviour {
    [SerializeField] protected ConveyorData data;
    
    protected List<Item> items = new List<Item>();
    protected ConveyorBelt outputBelt;
    
    public ConveyorData Data => data;
    
    // Methods
    public virtual bool CanAcceptItem() { /* TODO */ }
    public virtual void AddItem(Item item) { /* TODO */ }
    public virtual void UpdateItems(float deltaTime) { /* TODO */ }
    protected virtual ConveyorBelt GetOutputBelt() { /* TODO */ }
}
```

**21. ConveyorSplitter**
```cs
public class ConveyorSplitter : ConveyorBelt {
    private ConveyorBelt leftOutput;
    private ConveyorBelt rightOutput;
    private bool lastOutputWasLeft;
    
    protected override ConveyorBelt GetOutputBelt() {
        // TODO: Alternate between left and right
    }
}
```

**22. ConveyorCombiner**
```cs
public class ConveyorCombiner : ConveyorBelt {
    private ConveyorBelt leftInput;
    private ConveyorBelt rightInput;
    
    public override bool CanAcceptItem() {
        // TODO: Check if not full
    }
}
```

**23. ConveyorSystem (Topological Sort)**
```cs
public class ConveyorSystem : MonoBehaviour {
    private List<ConveyorBelt> allBelts = new List<ConveyorBelt>();
    private List<ConveyorBelt> executionOrder;
    
    public void RegisterBelt(ConveyorBelt belt) { /* TODO */ }
    public void UnregisterBelt(ConveyorBelt belt) { /* TODO */ }
    
    private void RecalculateTopology() {
        // TODO: Kahn's algorithm for topological sort
    }
    
    private void Update() {
        // TODO: Update belts in execution order
        foreach (var belt in executionOrder) {
            belt.UpdateItems(Time.deltaTime);
        }
    }
}
```

**24. Extractor**
```cs
public class Extractor : MonoBehaviour {
    [SerializeField] private ExtractorData data;
    
    private Tile resourceTile;
    private ConveyorBelt outputBelt;
    private float extractionTimer;
    
    private void Start() {
        // TODO: Find resource tile, find output belt
    }
    
    private void Update() {
        // TODO: Extract resources, create items, push to belt
    }
}
```

**25. RecipeData**
```cs
[CreateAssetMenu(menuName = "Game/Recipe")]
public class RecipeData : ScriptableObject {
    [System.Serializable]
    public class Ingredient {
        public ResourceType type;
        public int amount;
    }
    
    public string recipeName;
    public Ingredient[] inputs;
    public Ingredient[] outputs;
    public float craftingTime = 5f;
}
```

**26. Processor**
```cs
public class Processor : MonoBehaviour {
    [SerializeField] private ProcessorData data;
    
    private RecipeData currentRecipe;
    private Dictionary<ResourceType, int> inputBuffer;
    private float craftingProgress;
    
    private void Update() {
        // TODO: Accept items from input belts, craft, output to belts
    }
    
    private bool HasIngredients(RecipeData recipe) { /* TODO */ }
    private void StartCrafting(RecipeData recipe) { /* TODO */ }
    private void FinishCrafting() { /* TODO */ }
}
```

---

### ✅ COMBAT SYSTEM

**27. EnemyData**
```cs
[CreateAssetMenu(menuName = "Game/Enemy")]
public class EnemyData : ScriptableObject {
    [Header("Identity")]
    public string enemyName;
    public Sprite icon;
    
    [Header("Prefab")]
    public GameObject prefab;
    
    [Header("Stats")]
    public int health = 100;
    public int armor = 0;
    public int shield = 0;
    public float moveSpeed = 3f;
    
    [Header("Rewards")]
    public int moneyReward = 10;
    public ResourceAmount[] resourceDrops;
    
    [Header("Behavior")]
    public EnemyType type;
}

public enum EnemyType {
    Ground, Flying, Boss
}
```

**28. Enemy**
```cs
public class Enemy : MonoBehaviour, ISelectable {
    [SerializeField] private EnemyData data;
    
    private int currentHealth;
    private int currentArmor;
    private int currentShield;
    private Path currentPath;
    private int currentWaypointIndex;
    
    public EnemyData Data => data;
    public bool IsAlive => currentHealth > 0;
    
    // Methods
    public void TakeDamage(int damage) {
        // TODO: Apply to shield → armor → health
    }
    
    private void Move() {
        // TODO: Follow path waypoints
    }
    
    private void Die() {
        // TODO: Drop rewards, fire event, destroy
    }
    
    private void ReachedEnd() {
        // TODO: Damage player base, destroy
    }
}
```

**29. Path & PathNode**
```cs
public class Path {
    public List<Vector3> waypoints;
    public float totalLength;
    
    public Vector3 GetPositionAtDistance(float distance) {
        // TODO: Lerp between waypoints
    }
}

public class PathNode {
    public v2 gridPosition;
    public List<PathNode> connections;
    public List<Path> paths; // Pre-baked bezier curves
}
```

**30. PathManager**
```cs
public class PathManager : MonoBehaviour {
    private List<PathNode> allNodes = new List<PathNode>();
    private PathNode startNode;
    private PathNode endNode;
    
    public PathNode StartNode => startNode;
    
    public void BuildPaths() {
        // TODO: Find all path tiles, build graph, generate bezier curves
    }
    
    public Path GetRandomPathFrom(PathNode node) {
        // TODO: Return random path if splits
    }
}
```

**31. TowerCombatComponent**
```cs
public class TowerCombatComponent : MonoBehaviour {
    [SerializeField] private TowerData data;
    
    private List<Enemy> enemiesInRange = new List<Enemy>();
    private Enemy currentTarget;
    private float fireCooldown;
    private TargetingStrategy currentStrategy;
    
    // Methods
    private void Update() {
        // TODO: Find targets, aim, fire
    }
    
    private void FindTargets() {
        // TODO: OverlapSphere for enemies in range
    }
    
    private Enemy SelectTarget() {
        // TODO: Apply targeting strategy
    }
    
    private void Fire(Enemy target) {
        // TODO: Instantiate projectile, play effects
    }
}
```

**32. Projectile**
```cs
public class Projectile : MonoBehaviour {
    [SerializeField] private float speed = 20f;
    [SerializeField] private int damage = 10;
    
    private Transform target;
    
    public void SetTarget(Transform t) { target = t; }
    
    private void Update() {
        // TODO: Move toward target, apply damage on hit
    }
}
```

---

### ✅ WAVE & SPAWN SYSTEM

**33. WaveData**
```cs
[CreateAssetMenu(menuName = "Game/Wave")]
public class WaveData : ScriptableObject {
    [System.Serializable]
    public class EnemySpawn {
        public EnemyData enemy;
        public int count;
        public float spawnInterval; // Seconds between spawns
    }
    
    public int waveNumber;
    public EnemySpawn[] spawns;
    public float timeUntilNextWave = 30f;
}
```

**34. WaveManager**
```cs
public class WaveManager : MonoBehaviour {
    [SerializeField] private WaveData[] waves;
    
    private int currentWaveIndex = 0;
    private int enemiesRemaining;
    private bool waveInProgress;
    
    public event Action<int> OnWaveStarted;
    public event Action<int> OnWaveCompleted;
    
    public void StartWave() {
        // TODO: Spawn enemies from wave data
    }
    
    private IEnumerator SpawnWave(WaveData wave) {
        // TODO: Spawn enemies with intervals
    }
}
```

**35. SpawnManager**
```cs
public class SpawnManager : MonoBehaviour {
    [SerializeField] private Transform[] spawnPoints;
    
    public void SpawnEnemy(EnemyData data, Vector3 position) {
        // TODO: Instantiate, initialize with path
    }
}
```

---

### ✅ UI SYSTEM

**36. UIManager (Panel State Machine)**
```cs
public class UIManager : MonoBehaviour {
    [Header("Panels")]
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private GameObject buildMenuPanel;
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private GameObject settingsPanel;
    
    public void ShowPanel(GameObject panel) { /* TODO */ }
    public void HideAllPanels() { /* TODO */ }
}
```

**37. HUDController**
```cs
public class HUDController : MonoBehaviour {
    [Header("Resource Display")]
    [SerializeField] private ResourceUIElement[] resourceDisplays;
    
    [Header("Wave Info")]
    [SerializeField] private TMPro.TextMeshProUGUI waveNumberText;
    [SerializeField] private TMPro.TextMeshProUGUI enemiesRemainingText;
    
    [Header("Player Info")]
    [SerializeField] private TMPro.TextMeshProUGUI healthText;
    
    private void OnEnable() {
        // TODO: Subscribe to GameEvents
    }
    
    private void UpdateResourceDisplay(ResourceType type, int amount) {
        // TODO: Update UI
    }
}
```

**38. TooltipSystem**
```cs
public class TooltipSystem : MonoBehaviour {
    [SerializeField] private GameObject tooltipPrefab;
    [SerializeField] private TMPro.TextMeshProUGUI titleText;
    [SerializeField] private TMPro.TextMeshProUGUI descriptionText;
    
    public void Show(string title, string description, Vector3 worldPos) {
        // TODO: Position at world pos, show
    }
    
    public void Hide() { /* TODO */ }
}
```

**39. BuildingInfoPanel**
```cs
public class BuildingInfoPanel : MonoBehaviour {
    [SerializeField] private TMPro.TextMeshProUGUI buildingNameText;
    [SerializeField] private Image buildingIcon;
    [SerializeField] private TMPro.TextMeshProUGUI statsText;
    [SerializeField] private Button sellButton;
    
    public void ShowBuildingInfo(PlacementComponent building) {
        // TODO: Display building data
    }
}
```

**40. BuildMenuController**
```cs
public class BuildMenuController : MonoBehaviour {
    [SerializeField] private BuildingData[] availableBuildings;
    [SerializeField] private GameObject buildingButtonPrefab;
    [SerializeField] private Transform buttonContainer;
    
    private void Start() {
        // TODO: Generate buttons for each building
    }
    
    private void OnBuildingButtonClicked(BuildingData data) {
        // TODO: Enter build mode
    }
}
```

---

### ✅ PERSISTENCE

**41. SaveData Structures**
```cs
[System.Serializable]
public class GameSaveData {
    public GridSaveData grid;
    public InventorySaveData inventory;
    public WaveSaveData wave;
    public PlayerSaveData player;
}

[System.Serializable]
public class GridSaveData {
    public v2 size;
    public SavedBuilding[] buildings;
}

[System.Serializable]
public class SavedBuilding {
    public string buildingDataName; // ScriptableObject name
    public v2 gridPosition;
    public int rotation;
    public string customData; // JSON for building-specific state
}

[System.Serializable]
public class InventorySaveData {
    public ResourceType[] types;
    public int[] amounts;
}

[System.Serializable]
public class WaveSaveData {
    public int currentWaveIndex;
    public float timeUntilNextWave;
}

[System.Serializable]
public class PlayerSaveData {
    public int health;
    public int money;
}
```

**42. SaveManager**
```cs
public class SaveManager : MonoBehaviour {
    private string savePath => Path.Combine(Application.persistentDataPath, "save.json");
    
    public void SaveGame() {
        // TODO: Collect data from all systems, serialize, write to file
    }
    
    public void LoadGame() {
        // TODO: Read file, deserialize, restore all systems
    }
    
    private GridSaveData SerializeGrid() { /* TODO */ }
    private void DeserializeGrid(GridSaveData data) { /* TODO */ }
}
```

---

### ✅ AUDIO SYSTEM

**43. AudioManager**
```cs
public class AudioManager : MonoBehaviour {
    public enum AudioMixerGroup { Master, SFX, Music, Ambience, UI }
    
    [Header("Audio Sources")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource ambienceSource;
    
    public void PlaySFX(AudioClip clip, Vector3 position) { /* TODO */ }
    public void PlayMusic(AudioClip clip) { /* TODO */ }
    public void SetVolume(AudioMixerGroup group, float volume) { /* TODO */ }
}
```

**44. AudioData**
```cs
[System.Serializable]
public class AudioData {
    public AudioClip[] clips;
    public float volume = 1f;
    public float pitch = 1f;
    public Vector2 pitchRandomRange = Vector2.one;
    
    public AudioClip GetRandomClip() {
        // TODO: Return random clip
    }
}
```

---

### ✅ CAMERA SYSTEM

**45. IsometricCamera**
```cs
public class IsometricCamera : MonoBehaviour {
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float rotationSpeed = 90f;
    
    [Header("Zoom")]
    [SerializeField] private float zoomSpeed = 5f;
    [SerializeField] private float minZoom = 5f;
    [SerializeField] private float maxZoom = 20f;
    
    [Header("Bounds")]
    [SerializeField] private Bounds cameraBounds;
    
    private float currentZoom = 10f;
    
    private void Update() {
        // TODO: WASD movement, Q/E rotation, scroll zoom
    }
    
    private void ClampToBounds() { /* TODO */ }
}
```

---

### ✅ ADVANCED FEATURES

**46. FogOfWarController**
```cs
public class FogOfWarController : MonoBehaviour {
    private Board<bool> exploredTiles;
    private Board<bool> visibleTiles;
    
    public void Initialize(v2 gridSize) {
        // TODO: Create boards
    }
    
    public void RevealArea(v2 center, int radius) {
        // TODO: Mark tiles as explored/visible
    }
    
    public bool IsPositionVisible(v2 pos) { /* TODO */ }
    public bool IsPositionExplored(v2 pos) { /* TODO */ }
}
```

**47. UpgradeData**
```cs
[CreateAssetMenu(menuName = "Game/Upgrade")]
public class UpgradeData : ScriptableObject {
    public string upgradeName;
    public string description;
    public Sprite icon;
    public ResourceCost[] cost;
    public UpgradeType type;
    
    // Stat modifiers
    public StatModifier[] modifiers;
}

public enum UpgradeType {
    TowerDamage, TowerRange, TowerFireRate,
    ConveyorSpeed, ExtractorRate,
    PlayerHealth, ResourceBonus
}

[System.Serializable]
public struct StatModifier {
    public string targetStat; // "damage", "range", etc.
    public ModifierOperation operation; // Add, Multiply
    public float value;
}

public enum ModifierOperation { Add, Multiply, Override }
```

**48. UpgradeManager**
```cs
public class UpgradeManager : MonoBehaviour {
    [SerializeField] private UpgradeData[] allUpgrades;
    
    private List<UpgradeData> purchasedUpgrades = new List<UpgradeData>();
    
    public bool CanPurchase(UpgradeData upgrade) { /* TODO */ }
    public void Purchase(UpgradeData upgrade) { /* TODO */ }
    public void ApplyUpgrades() { /* TODO: Apply to all buildings */ }
}
```

---

### ✅ OBJECT POOLING

**49. ObjectPool (Generic)**
```cs
public class ObjectPool<T> where T : Component {
    private T prefab;
    private Queue<T> availableObjects = new Queue<T>();
    private Transform poolParent;
    
    public ObjectPool(T prefab, int initialSize, Transform parent) {
        // TODO: Pre-instantiate objects
    }
    
    public T Get() {
        // TODO: Dequeue or instantiate new
    }
    
    public void Return(T obj) {
        // TODO: Disable and enqueue
    }
}
```

**50. ProjectilePooler**
```cs
public class ProjectilePooler : MonoBehaviour {
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private int poolSize = 50;
    
    private ObjectPool<Projectile> pool;
    
    private void Start() {
        // TODO: Initialize pool
    }
    
    public Projectile GetProjectile() { /* TODO */ }
    public void ReturnProjectile(Projectile proj) { /* TODO */ }
}
```

---

### ✅ TUTORIAL SYSTEM

**51. TutorialQuestData**
```cs
[CreateAssetMenu(menuName = "Game/Tutorial/Quest")]
public class TutorialQuestData : ScriptableObject {
    public string questTitle;
    public string questDescription;
    public Sprite questIcon;
    
    // Completion conditions
    public TutorialConditionType conditionType;
    public int targetCount; // e.g., "Build 3 towers"
    public BuildingData targetBuilding; // if type == BuildSpecificBuilding
    public ResourceType targetResource; // if type == CollectResource
}

public enum TutorialConditionType {
    BuildAnyBuilding, BuildSpecificBuilding,
    CollectResource, KillEnemies,
    CompleteWave, PlaceConveyor
}
```

**52. TutorialManager**
```cs
public class TutorialManager : MonoBehaviour {
    [SerializeField] private TutorialQuestData[] quests;
    
    private int currentQuestIndex = 0;
    private int progressCount = 0;
    
    public event Action<TutorialQuestData> OnQuestStarted;
    public event Action<TutorialQuestData> OnQuestCompleted;
    
    private void OnEnable() {
        // TODO: Subscribe to relevant events based on quest type
    }
    
    private void CheckQuestCompletion() { /* TODO */ }
    private void StartNextQuest() { /* TODO */ }
}
```

---

## COMPLETE FEATURE LIST (VERIFY AGAINST TOWER FACTORY)

From the TowerFactory source, include skeletons for:

### ✅ Grid & Placement
- [x] GridManager with Board<Tile>
- [x] PlacementComponent (multi-tile, rotation, jagged)
- [x] BuildModeController (ghost objects)
- [x] Drag-and-drop building movement

### ✅ Input & Interaction
- [x] InputManager (mouse raycasting)
- [x] Resource node clicking (trees, rocks)
- [x] Building selection
- [x] UI interaction (buttons, menus)

### ✅ Resources & Economy
- [x] InventoryManager
- [x] ResourceData ScriptableObjects
- [x] Resource nodes (collectible)
- [x] Building costs & refunds

### ✅ Buildings
- [x] BuildingData hierarchy (Tower, Conveyor, Extractor, Processor)
- [x] Multi-tile placement validation
- [x] Rotation system
- [x] Child building parts

### ✅ Towers & Combat
- [x] TowerCombatComponent
- [x] All targeting strategies (10 types)
- [x] Projectiles & projectile pooling
- [x] Damage calculation (shield → armor → health)

### ✅ Enemies & Pathfinding
- [x] EnemyData ScriptableObjects
- [x] Enemy movement (path following)
- [x] PathManager (graph, bezier curves)
- [x] Wave system

### ✅ Factory Automation
- [x] ConveyorBelt (all types: straight, curve, splitter, combiner, underground, crossing)
- [x] ConveyorSystem (topological sort)
- [x] Item transport simulation
- [x] Extractor (resource → belt)
- [x] Processor (recipes, crafting)

### ✅ UI
- [x] HUD (resources, wave, health)
- [x] Build menu (categorized buildings)
- [x] Tooltips
- [x] Building info panel
- [x] Settings menu
- [x] Victory/defeat screens

### ✅ Advanced Systems
- [x] FogOfWar (exploration)
- [x] Upgrades (tech tree)
- [x] Tutorial (quest system)
- [x] Save/Load (JSON with UTIL.cs)
- [x] Audio system (SFX, music)
- [x] Camera (isometric, zoom, rotation)
- [x] Time controls (pause, speed)
- [x] Object pooling

---

## FINAL INSTRUCTIONS

**For EVERY system above:**
1. Define the complete class skeleton
2. Include ALL fields (with [SerializeField], [Header], [Tooltip])
3. Include ALL properties
4. Include ALL events
5. Include ALL public methods (with XML comments)
6. Include ALL private method signatures (with TODO comments)
7. Include initialization order notes
8. Include dependency notes

**Method Bodies:**
Leave as `{ /* TODO: [brief description] */ }` - the developer will implement.

**Example Method:**
```cs
/// <summary>
/// Validates if building can be placed at current position.
/// Checks: grid bounds, tile types, existing buildings, resources, fog of war.
/// </summary>
/// <returns>True if placement is valid</returns>
public bool CanPlace() {
    /* TODO: 
     * 1. Get occupied cells from GetOccupiedCells()
     * 2. For each cell: check grid bounds
     * 3. Check tile.CanBuildOn() (unless canBuildOnAnyTile)
     * 4. Check tile.building == null or tile.building == this
     * 5. Check FogOfWar.IsPositionVisible(cell)
     * 6. Return true if all checks pass
     */
    return false;
}
```

**Cross-Reference TowerFactory Source:**
Study these files closely and match their architecture:
- Grid.cs (lines 26939-27085)
- PlacementComponent.cs (lines 17421-17884)
- TowerCombatComponent.cs
- ConveyorBeltSystem.cs (lines 37081-37165)
- EnemyMovement.cs
- All ScriptableObject structures

**OUTPUT LENGTH:**
Expect 10,000-15,000 lines of skeleton code covering 100% of TowerFactory's features.

---

## BEGIN GENERATION

Read ALL attached TowerFactory source files. Generate the complete architectural skeleton matching the format shown above. Start with foundational systems (Grid, Events, GameManager) and work through all gameplay, factory, UI, and technical systems. Include EVERY feature from the original - no omissions.
```

---

This prompt ensures Claude 4.5 will provide:
- ✅ **Complete class skeletons** (all fields, properties, methods)
- ✅ **ALL features** from TowerFactory (including mouse interaction, resource clicking, tower config UI, etc.)
- ✅ **Minimal public APIs** (2-5 essential methods per system)
- ✅ **Event-driven architecture** (loose coupling)
- ✅ **UTIL.cs integration** (v2, Board<T> everywhere)
- ✅ **Detailed TODO comments** (so you know what logic to write)
- ✅ **Start-to-end coverage** (nothing omitted)

The developer (you) writes the logic inside method bodies. Claude provides the complete architectural framework.
```

# TODO Coverage:

## Prompt
```
btw the conveyor belt combiner and spliter should be roundRobin no? 
i,e say 3 in and 1 out combiner if among 0, 1, 2 input 
(even if any belt not connected say 0 is not connected it consider 1, 2 ) and accept from 1 than 2 than 1 say in 1 the resource havent reached tehre yet then it keep accepting from 2 and roundrobin occr once 1 reaches and 2 reaches make sure it also take into consideration the timing to move from in to out should be exact same as normal belt are all these covered too? and also extractor cant produce if belt ahead is filled and wood extractor is size 2 x 1 where the 1st tile is wood animation axe cutting prefab and 2nd is a conveyor belt this entire 2 x 1 is extractor and also as for processor which convert 1 to other resource requires certain amount of say wood to make planks and other features, and also conveyor belt auto align rotation and other features such as clogged and more exist ? also what bbaout final destination where resource are consumed and destroyed  make sure all these covered.

and what what about visual que when placing any placable such as belt, exractor, processor, 
tower etc red/green basd on weather can be placed or no ? i guss that is coevred, 

also what about enemy ai refere source TowerFactory Scripts All file attached to see how enemy ai is covered
are all these covered along with input of placing buildings ? and highlight when hovered(just add a public method or event based which ever you think right so it highlight when hovered on any knda building such as tower or conveyor) .
```
