# Prompt
```
what should be prompt to Claude 4.5 (provide the detailed prompt)
if given following files (which is the entire source of Main Project by author) and `UTIL.cs` and need complete Project not just MVP in one single prompt ?
```

# COMPLETE PROMPT FOR CLAUDE 4.5

**ROLE & CONTEXT:**
You are a Senior Unity Solutions Architect specializing in Tower Defense and Factory Automation games. You have deep expertise in C# design patterns, Unity optimization, and game architecture.

**THE TASK:**
Rebuild the COMPLETE "TowerFactory" game from the attached source code, integrating my custom UTIL.cs library as the foundation. This is NOT an MVP - I need the full game with ALL features from the original.

**ATTACHED FILES:**
1. `TowerFactory_Scripts_ALL_CORE_.md` - Complete C# source (426 scripts, 42,000+ lines)
2. `TowerFactory_ScriptableObjects_Assets__ALL_With_Unity_BuiltIns_.md` - All ScriptableObjects and data assets
3. `towerFactoryShader_ALL_Core_.md` - Custom shaders
4. `UTIL.cs` - MY CORE LIBRARY (mandatory integration - use v2 for coordinates, Board<T> for grids)

----
## DELIVERABLE REQUIREMENTS

### 1. COMPLETE FEATURE PARITY
Implement EVERY system from the original TowerFactory:

**Core Systems:**
- ✅ Grid system (XZ plane, using `UTIL.Board<GridCell>` and `UTIL.v2`)
- ✅ Placement system (multi-tile, rotation, jagged shapes, ghost visuals, validation)
- ✅ Input system (mouse raycasting, drag-and-drop, building movement, rotation hotkeys)

**Gameplay Systems:**
- ✅ Tower system (combat, targeting strategies, projectiles, damage, range, upgrades)
- ✅ Enemy system (spawning, pathfinding, waves, multiple enemy types, health/armor/shields)
- ✅ Factory automation (conveyors, extractors, processors, item transport, recipes)
- ✅ Conveyor system (straight, curves, splitters, combiners, underground, topological sorting)
- ✅ Resource system (extraction, storage, processing, item flow simulation)

**Advanced Features:**
- ✅ Fog of War (grid-based visibility, exploration)
- ✅ Wave system (timed spawns, difficulty scaling, spawn patterns)
- ✅ Player upgrades (tech tree, unlocks, stat modifiers)
- ✅ Economy (resources, building costs, selling, income)
- ✅ Tutorial system (quest tracking, UI hints, progression)
- ✅ Map generation (procedural object placement, grid-based spawning)

**UI Systems:**
- ✅ HUD (resources, health, wave counter, time controls)
- ✅ Build menu (categorized building browser, hotbar, cost display)
- ✅ Tooltips (building stats, recipes, upgrade trees)
- ✅ Selection system (building info panels, upgrade UI)
- ✅ Store/Shop system (building purchase, resource management)
- ✅ Settings (graphics, audio, controls, keybindings)
- ✅ Victory/Defeat screens (stats, rewards, progression)

**Technical Systems:**
- ✅ Save/Load (complete game state using `UTIL.cs` JSON serialization)
- ✅ Object pooling (projectiles, enemies, effects, particles)
- ✅ Audio system (SFX, music, ambience, mixer groups)
- ✅ Camera system (isometric view, zoom, rotation, pan, bounds)
- ✅ Time management (pause, slow-mo, fast-forward)
- ✅ Animation (towers, enemies, buildings, UI transitions)
- ✅ Particle effects (impacts, explosions, ambient effects)
- ✅ Achievement system (Steam integration patterns)

---

### 2. MANDATORY UTIL.CS INTEGRATION

**Replace ALL grid/coordinate systems with UTIL.cs equivalents:**

```cs
// CRITICAL SETUP (do this FIRST)
using SPACE_UTIL;

void Awake() {
    v2.axisY = 'z';  // XZ plane for 3D world, 2D grid logic
}

// REPLACE original coordinate handling
// ❌ OLD (TowerFactory): Vector3 worldPos; int x = Mathf.RoundToInt(worldPos.x);
// ✅ NEW (UTIL.cs):      v2 gridCoord = worldPos;  // Implicit conversion!

// REPLACE original grid storage
// ❌ OLD: GridCell[,] cells;
// ✅ NEW: Board<GridCell> grid;

// REPLACE manual serialization
// ❌ OLD: Custom Vector3SerializationSurrogate, binary formatters
// ✅ NEW: JsonUtility with UTIL.Board<T> flat array (already serializable)
```

**UTIL.cs Usage Examples:**
```cs
// Grid initialization
Board<GridCell> grid = new Board<GridCell>((50, 50), new GridCell());

// Coordinate conversion (automatic)
v2 gridPos = transform.position;  // Vector3 → v2 (auto-rounds)
Vector3 worldPos = gridPos;       // v2 → Vector3 (implicit)

// Direction helpers
foreach (v2 dir in v2.getDIR(includeDiagonal: false)) {
    v2 neighbor = currentPos + dir;
    GridCell cell = grid[neighbor];
}

// Bounds checking
if (coord.inRange(grid.m, grid.M)) {
    // Safe access
}

// Serialization (zero effort)
string json = JsonUtility.ToJson(grid);
Board<GridCell> loaded = JsonUtility.FromJson<Board<GridCell>>(json);
```

---

### 3. ARCHITECTURE REQUIREMENTS

**Enforce these design patterns from the original:**
- **Singleton**: GameManager, GridManager, TimeManager, AudioManager, etc.
- **Observer**: C# events for all state changes (resources, building placement, waves)
- **Component**: Modular systems (CombatComponent, MovementComponent, StatsComponent)
- **Strategy**: Interchangeable behaviors (tower targeting, enemy AI, spawn patterns)
- **Factory**: Object pooling for projectiles, enemies, effects
- **Command**: Reversible actions (build/sell, save/load)

**Maintain the original's modular structure:**
```
Managers/
  ├─ GameManager (game state, win/loss conditions)
  ├─ GridManager (Board<GridCell>, tile management)
  ├─ PlacementManager (build mode, ghost objects)
  ├─ TimeManager (pause, speed controls)
  ├─ WaveManager (enemy spawning)
  ├─ ConveyorSystem (item transport, topological sort)
  ├─ InventoryManager (resources, storage)
  └─ AudioManager (SFX, music)

Components/
  ├─ PlacementComponent (multi-tile buildings)
  ├─ TowerCombatComponent (targeting, firing)
  ├─ EnemyMovement (pathfinding)
  ├─ ConveyorBelt (item transport)
  ├─ Extractor (resource generation)
  └─ Processor (recipe crafting)

ScriptableObjects/
  ├─ BuildingData (towers, factories)
  ├─ EnemyData (stats, behavior)
  ├─ ResourceData (types, icons)
  ├─ RecipeData (inputs, outputs)
  └─ WaveData (spawn patterns)
```

---

### 4. CODE GENERATION GUIDELINES

**DO:**
- ✅ Provide COMPLETE, working implementations (not pseudo-code)
- ✅ Include ALL methods and properties (public AND private)
- ✅ Add detailed comments explaining complex logic
- ✅ Use UTIL.cs extensively (v2, Board<T>, extension methods)
- ✅ Match the original's naming conventions and structure
- ✅ Implement error handling and edge cases
- ✅ Add Unity Inspector attributes ([SerializeField], [Tooltip], etc.)

**DON'T:**
- ❌ Skip features or mark them as "TODO"
- ❌ Use pseudo-code or incomplete snippets
- ❌ Ignore the original's implementation details
- ❌ Create simplified "MVP" versions
- ❌ Omit helper methods or utilities
- ❌ Forget UTIL.cs integration in any grid/coordinate code

---

### 5. SPECIFIC IMPLEMENTATION PRIORITIES

**Focus Areas (analyze the source deeply):**

1. **Conveyor System:**
   - Item transport simulation (moving items between belts)
   - Topological sorting for execution order
   - Splitter/combiner logic (item distribution)
   - Underground belt connections
   - Belt rotation and connectivity rules

2. **Pathfinding:**
   - Study the original's PathTile system (northPaths, eastPaths, etc.)
   - Implement path following with bezier curve interpolation
   - Handle path splits (enemy chooses random branch)
   - Pre-calculate path distances for tower targeting

3. **Multi-Tile Buildings:**
   - Rotation logic for non-square buildings (1x2, 2x3, etc.)
   - Jagged shapes using `ignoredPositions[]`
   - Child object management (multi-part buildings)
   - Visual rotation animations

4. **Recipe System:**
   - Input/output matching for processors
   - Crafting time and yield calculations
   - Conveyor-to-processor item transfer
   - Storage and buffer management

5. **Tower Targeting:**
   - Implement ALL targeting strategies from original:
     - First, Last, Nearest, Farthest
     - Slowest, Fastest
     - Lowest HP, Highest HP, Most Total Life
     - Highest Armor, Highest Shield
   - Range calculations and overlap detection
   - Priority target switching

---

### 6. DELIVERABLE FORMAT

**Organize the output as:**

# SECTION 1: Core Foundation
## 1.1 UTIL.cs Integration Setup
[Code for GameBootstrap, v2 configuration]

## 1.2 Grid System
[GridManager, GridCell, complete implementation]

## 1.3 Data Structures
[All ScriptableObject definitions matching original]

# SECTION 2: Placement & Building System
[PlacementComponent, BuildModeController, multi-tile logic]

# SECTION 3: Gameplay Core
[Towers, Enemies, Combat, Movement]

# SECTION 4: Factory Automation
[Conveyors, Extractors, Processors, Item Transport]

# SECTION 5: Wave & Spawn System
[WaveManager, SpawnManager, Enemy AI]

# SECTION 6: UI Systems
[HUD, Menus, Tooltips, Selection]

# SECTION 7: Technical Systems
[Save/Load, Audio, Camera, Pooling, Time]

# SECTION 8: Advanced Features
[Fog of War, Upgrades, Tutorial, Achievements]

# SECTION 9: Integration Guide
[How to assemble everything, initialization order]

# SECTION 10: Testing & Validation
[Key scenarios to test, expected behaviors]

---
### 7. QUALITY STANDARDS

**The code must be:**
- ✅ **Production-ready** (no placeholders or stubs)
- ✅ **Fully commented** (explain WHY, not just WHAT)
- ✅ **Performance-optimized** (object pooling, efficient loops)
- ✅ **Unity 2020.3+ compatible** (.NET Standard 2.0)
- ✅ **Error-resistant** (null checks, bounds validation)
- ✅ **Debuggable** (helpful Debug.Log messages with context)
- ✅ **Maintainable** (clear variable names, logical structure)

---

### 8. REFERENCE EXAMPLES FROM ORIGINAL

**Study these key files in depth:**

**Grid & Placement:**
- `Grid.cs` (lines 26939-27085) - Grid cell management
- `PlacementComponent.cs` (lines 17421-17884) - Multi-tile building logic
- `Tile.cs` (lines 591-667) - Tile types and validation

**Enemy & Pathfinding:**
- `PathTile.cs` (lines 699-1100) - Path system architecture
- `EnemyMovement.cs` (lines 1-400) - Movement along paths
- `Path.cs` - Bezier curve path handling

**Conveyors:**
- `ConveyorBelt.cs` (lines 38124+) - Base belt class
- `ConveyorBeltSystem.cs` (lines 37081-37165) - System coordinator
- `ConveyorBeltGroup.cs` (lines 37225+) - Grouping and topology

**Combat:**
- `TowerCombatComponent.cs` (lines 9000+) - Tower firing logic
- `TowerTargetProvider.cs` (lines 5800+) - Targeting strategies
- `Projectile.cs` (lines 11000+) - Projectile movement

**Data:**
- `GameplayObjectData.cs` - Base data for all buildings
- `EnemyData.cs` - Enemy stats and configuration
- `Recipe.cs` - Crafting recipe definitions

---

### 9. EXPECTED OUTPUT SIZE

Given the original has:
- **426 scripts** (~42,000 lines)
- **556 ScriptableObject assets**
- **11 custom shaders**

Your output should include:
- ~30-50 core scripts (consolidated, no duplicate boilerplate)
- ~20 ScriptableObject class definitions
- Complete implementations of ALL features
- Integration code for UTIL.cs
- Assembly instructions

**Estimated total: 8,000-12,000 lines of production code**

---

### 10. FINAL CHECKLIST

Before submitting, ensure:
- [ ] EVERY feature from TowerFactory is implemented
- [ ] ALL grid/coordinate code uses `UTIL.v2` and `Board<T>`
- [ ] Save/Load uses UTIL.cs JSON serialization
- [ ] No "MVP" shortcuts or missing features
- [ ] All design patterns from original are preserved
- [ ] Code is complete, tested, and production-ready
- [ ] Integration guide explains how to assemble the project
- [ ] No placeholder comments like "TODO" or "implement later"

---

## BEGIN IMPLEMENTATION

Read through ALL attached source files thoroughly, then generate the COMPLETE game implementation with full UTIL.cs integration. Start with Section 1 (Core Foundation) and work through all 10 sections systematically.

---
## Why This Prompt Works:

1. **Clear scope**: "COMPLETE game, not MVP" is emphasized multiple times
2. **Detailed requirements**: Every major system is explicitly listed
3. **UTIL.cs integration**: Specific examples of how to use v2 and Board<T>
4. **Structure**: 10-section format guides the response organization
5. **Quality standards**: Production-ready code, no shortcuts
6. **Reference points**: Direct line numbers from source files
7. **Deliverable format**: Markdown sections that can be easily extracted
8. **Checklist**: Final verification ensures nothing is missed

This prompt should get you a comprehensive, fully-implemented game that integrates your UTIL.cs library throughout!