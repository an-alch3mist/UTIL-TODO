# Tile System — Complete Reference
> Tower-defence / factory-style grid placement for Unity 3D 2020.3+  
> Uses `Board<Tile>` and `v2` from `UTIL.cs` (namespace `SPACE_UTIL`)

---

## Table of Contents
1. [File Map](#1-file-map)
2. [Scene Hierarchy](#2-scene-hierarchy)
3. [Prefab Structures](#3-prefab-structures)
4. [ScriptableObject Setup](#4-scriptableobject-setup)
5. [Component Reference](#5-component-reference)
6. [Coordinate System](#6-coordinate-system)
7. [Rotation Math](#7-rotation-math)
8. [Event Bus Reference](#8-event-bus-reference)
9. [Interaction Flow](#9-interaction-flow)
10. [Extension Stubs](#10-extension-stubs)
11. [Checklist — Bringing It Live](#11-checklist--bringing-it-live)

---

## 1. File Map

```
UTIL.cs                     ← your shared utility (v2, Board<T>, Q() etc.)

── Core Grid ───────────────────────────────────────────────────────
Tile.cs                     ← data stored per cell in Board<Tile>
GridManager.cs              ← owns Board<Tile>, all placement logic (Singleton)

── Building Data ────────────────────────────────────────────────────
BuildingSO.cs               ← ScriptableObject: shape definition + part list
                               (NO runtime logic, NO MonoBehaviour)

── Building Runtime ─────────────────────────────────────────────────
BuildingInstance.cs         ← MonoBehaviour on placed building root GO
BuildingPartProxy.cs        ← tiny component on each part GO → O(1) owner lookup
BuildingHoverProxy.cs       ← per-part collider hover → delegates to BuildingInstance

── Player Input ─────────────────────────────────────────────────────
BuildingSelector.cs         ← click-to-select, hold-to-drag, indicator control
BuildingPlacer.cs           ← new-building placement mode (separate from move)

── Event Bus ────────────────────────────────────────────────────────
BuildingEvents.cs           ← static C# events, fired by GridManager / Selector

── Part Behaviors ───────────────────────────────────────────────────
IPartBehavior.cs            ← interface: OnPlaced / OnMoved / OnRemoved
ConveyorBelt.cs             ← STUB — wiring ready, logic not yet implemented
ConveyorTickSystem.cs       ← STUB — singleton tick manager, no logic yet
BallistaBehavior.cs         ← example attack tower behavior
```

---

## 2. Scene Hierarchy

```
Scene
│
├── [Manager]                           ← Empty GO, tag: Untagged
│   ├── GridManager                     ← GridManager.cs
│   │     boardSize      = (20, 20)
│   │     cellSize       = 1
│   │     worldOrigin    = (0, 0, 0)
│   │     buildingRoot   → [Buildings]  (drag reference)
│   │
│   ├── ConveyorTickSystem              ← ConveyorTickSystem.cs (stub, no setup needed yet)
│   │
│   ├── BuildingSelector               ← BuildingSelector.cs
│   │     selectionCamera → Main Camera
│   │     buildingLayerMask = Building  (layer)
│   │     groundLayerMask  = Ground     (layer)
│   │     dragThresholdPx  = 6
│   │
│   └── BuildingPlacer                 ← BuildingPlacer.cs
│         ghostValidMaterial   → GhostGreen (Material)
│         ghostInvalidMaterial → GhostRed   (Material)
│         placementCamera      → Main Camera
│         groundLayerMask      = Ground
│
├── [Buildings]                         ← Empty GO, parent for all placed buildings
│   │                                     Assign to GridManager.buildingRoot
│   ├── Building_BallistaTower_(2,3)    ← spawned at runtime by GridManager.Place()
│   ├── Building_ConveyorBlock_(5,1)    ←  "
│   └── ...
│
├── [Environment]
│   ├── Ground                          ← MeshCollider, Layer: Ground
│   └── ...
│
└── Main Camera
```

> **Layer setup required**  
> Create two layers in Edit → Project Settings → Tags and Layers:  
> - `Building` — assign to all part GOs (auto-done via prefab layer setting)  
> - `Ground` — assign to your ground plane

---

## 3. Prefab Structures

### 3a. Building Root Prefab

This is NOT a prefab you drag into the scene yourself.  
`GridManager.Place()` creates it at runtime from a plain `new GameObject`.  
However, if you want a pre-authored root with the Indicator child, create it as a prefab and assign it to `BuildingSO.rootPrefab` (optional field, otherwise root is a bare GO).

```
Building_BallistaTower          ← root GO
│   Components:
│     ✓ BuildingInstance        ← auto-added by GridManager.Place()
│     ✓ indicatorValidMat       ← assign in prefab or via BuildingSO
│     ✓ indicatorInvalidMat
│
├── Indicator                   ← child GO, MUST be named exactly "Indicator"
│     Components:
│       MeshRenderer            ← flat quad or bounding-box mesh
│       MeshFilter
│     Notes:
│       - Disabled by default (SetActive false)
│       - BuildingInstance enables it on drag-start, disables on release
│       - Material is swapped to green or red by SetIndicator(bool)
│       - Position it at y = -0.01 so it sits flush on the ground
│
├── Part0_B-slab                ← spawned by BuildingInstance from BuildingSO.parts[0].prefab
│     Layer: Building
│     Components:
│       MeshFilter + MeshRenderer
│       MeshCollider (or BoxCollider)
│       BuildingPartProxy       ← auto-added if missing
│       BuildingHoverProxy      ← auto-added if Collider present
│       BallistaBehavior        ← implements IPartBehavior, added in prefab editor
│
├── Part1_O-mid                 ← from parts[1].prefab
│     Layer: Building
│     Components:
│       MeshFilter + MeshRenderer
│       MeshCollider
│       BuildingPartProxy       ← auto-added
│       BuildingHoverProxy      ← auto-added
│       ConveyorBelt            ← stub, implements IPartBehavior
│
└── Part2_E-entrance            ← from parts[2].prefab
      Layer: Building
      ...
```

### 3b. Part Prefab (what you author in the editor)

Each `BuildingPart.prefab` is a self-contained mesh+collider.  
It knows **nothing** about the grid — all grid awareness comes from `IPartBehavior.OnPlaced()`.

```
ConveyorTile_Prefab
│   Layer: Building
│   Components:
│     MeshFilter
│     MeshRenderer
│     BoxCollider              ← required for selection + hover
│     ConveyorBelt             ← IPartBehavior, stub for now
│   Notes:
│     - Do NOT add BuildingPartProxy or BuildingHoverProxy in editor
│       (BuildingInstance.SpawnParts adds them at runtime automatically)
│     - Do NOT add BuildingInstance here (that lives on the root)
│     - Scale the collider to cover the tile footprint, not just the visual mesh
```

---

## 4. ScriptableObject Setup

### Creating a Building SO

```
Right-click in Project window
→ Create → TileSystem → Building
```

One SO per building type. Store them in:
```
Assets/
  Data/
    Buildings/
      SO_BallistaTower.asset
      SO_ConveyorBlock.asset
      SO_BlockWall.asset
```

---

### BuildingSO Fields

```
┌─────────────────────────────────────────────────────────────────┐
│ BuildingSO  (ScriptableObject)                                  │
├──────────────────────┬──────────────────────────────────────────┤
│ buildingName         │ "Ballista Tower"                         │
│ description          │ "Attacks enemies in range."              │
│ icon                 │ Sprite (UI thumbnail)                    │
├──────────────────────┼──────────────────────────────────────────┤
│ parts                │ List<BuildingPart>  (see below)          │
├──────────────────────┼──────────────────────────────────────────┤
│ pivotNote            │ "top-right corner" (documentation only)  │
└──────────────────────┴──────────────────────────────────────────┘
```

### BuildingPart Fields (element inside `parts` list)

```
┌─────────────────────────────────────────────────────────────────┐
│ BuildingPart                                                    │
├──────────────────────┬──────────────────────────────────────────┤
│ prefab               │ GameObject prefab (mesh + collider)      │
│ occupiedOffsets      │ List<v2>  — tile offsets from pivot      │
│ prefabLocalOffset    │ Vector3   — visual nudge from pivot world │
│ label                │ "MainBody" / "Entrance" (editor clarity) │
└──────────────────────┴──────────────────────────────────────────┘
```

---

### Full Example: `SO_BallistaTower`

The building from the original prompt:

```
BBE     (y = 0 relative to pivot)
BOE     (y = -1)
O       (y = -2)
    ^
    Pivot = top-right E = local (0, 0)
```

```
buildingName: "Ballista Tower"
pivotNote:    "Top-right E tile"

parts[0]
  label:           "B-Slab"
  prefab:          BallistaSlab_Prefab    ← single mesh spanning 3 tiles
  occupiedOffsets: (-2, 0), (-1, 0), (-2,-1)
  prefabLocalOffset: (-1.5, 0, -0.5)     ← visual centre of the slab

parts[1]
  label:           "O-Mid"
  prefab:          BlockO_Prefab
  occupiedOffsets: (-1,-1)
  prefabLocalOffset: (0, 0, 0)

parts[2]
  label:           "O-Bot"
  prefab:          BlockO_Prefab          ← can reuse same prefab
  occupiedOffsets: (-2,-2)
  prefabLocalOffset: (0, 0, 0)

parts[3]
  label:           "E-Top"               ← pivot tile
  prefab:          EntranceTile_Prefab
  occupiedOffsets: (0, 0)
  prefabLocalOffset: (0, 0, 0)

parts[4]
  label:           "E-Bot"
  prefab:          EntranceTile_Prefab
  occupiedOffsets: (0,-1)
  prefabLocalOffset: (0, 0, 0)
```

`OnValidate()` inside `BuildingSO` will log a warning if any two parts share an offset — catches authoring mistakes immediately.

---

### Full Example: `SO_ConveyorBlock`

A standalone single-tile conveyor (1×1, pivot = the tile itself):

```
C       ← pivot = (0,0)
```

```
buildingName: "Conveyor Belt"
pivotNote:    "The tile itself"

parts[0]
  label:           "Belt"
  prefab:          ConveyorTile_Prefab    ← has ConveyorBelt component
  occupiedOffsets: (0, 0)
  prefabLocalOffset: (0, 0, 0)
```

---

### Full Example: `SO_BlockWall` (2×1 horizontal wall)

```
WW      ← pivot = right tile = (0,0)
```

```
buildingName: "Wall Block"
pivotNote:    "Right tile"

parts[0]
  label:           "WallBody"
  prefab:          Wall2x1_Prefab         ← single mesh, 2 tiles wide
  occupiedOffsets: (0, 0), (-1, 0)
  prefabLocalOffset: (-0.5, 0, 0)        ← centre of the 2-tile span
```

---

## 5. Component Reference

### GridManager

| Member | Type | Description |
|--------|------|-------------|
| `board` | `Board<Tile>` | The live grid data |
| `boardSize` | `v2` | Set once in inspector |
| `cellSize` | `float` | World units per tile |
| `worldOrigin` | `Vector3` | World position of tile (0,0) |
| `buildingRoot` | `Transform` | Parent for spawned building GOs |
| `Place(so, pivot, rot)` | `BuildingInstance` | Spawn + mark board |
| `Remove(instance)` | `void` | Clear board + destroy GO |
| `Move(instance, pivot, rot)` | `bool` | Atomic validated move |
| `LiftOff(instance)` | `void` | Clear board only (drag start) |
| `LandOn(instance, pivot, rot)` | `void` | Write board + reposition (drag end) |
| `CanPlace(so, pivot, rot, ignore)` | `bool` | Validates before committing |
| `WorldToGrid(worldPos)` | `v2` | Raycast hit → grid coord |
| `GridToWorld(coord)` | `Vector3` | Grid coord → world centre |
| `InBounds(coord)` | `bool` | Boundary check |

---

### BuildingInstance

| Member | Description |
|--------|-------------|
| `data` | The `BuildingSO` this instance was built from |
| `pivotCoord` | Current grid coord of the building's pivot |
| `rotation` | 0-3, each step = 90° CW |
| `occupiedCoords` | Read-only list of all grid coords currently claimed |
| `PreviewAt(pivot, rot)` | Move visual root without touching board (drag) |
| `ApplyNewPlacement(...)` | Reposition after board commit |
| `SetIndicatorActive(bool)` | Enable/disable Indicator child GO |
| `SetIndicator(bool)` | Swap indicator material green/red |
| `OnHighlight(proxy)` | **STUB** — called on any part hover. Add your effect here |
| `OnUnhighlight(proxy)` | **STUB** — called when hover ends. Remove your effect here |
| `GetPartBehavior<T>(idx)` | Returns component T on the part GO at index idx |

---

### BuildingPartProxy

Sits on every part GO. Auto-added by `BuildingInstance.SpawnParts`.

| Member | Description |
|--------|-------------|
| `owner` | The parent `BuildingInstance` |
| `partIndex` | Which index in `BuildingSO.parts` this GO represents |

Usage in raycast hit:
```csharp
var proxy = hit.collider.GetComponent<BuildingPartProxy>();
int partIdx = proxy.partIndex;   // which part was clicked
```

---

### BuildingHoverProxy

Sits on every part GO that has a Collider. Auto-added by `BuildingInstance.SpawnParts`.

Uses Unity's `OnMouseEnter` / `OnMouseExit` messages.  
Caches owner via `Q().up<BuildingInstance>()` in `Awake()`.  
Calls `owner.OnHighlight(this)` / `owner.OnUnhighlight(this)`.

> **Requires** `Physics.queriesHitTriggers` or non-trigger colliders.  
> Works automatically — no inspector wiring needed.

---

### BuildingSelector

| Inspector Field | Description |
|-----------------|-------------|
| `selectionCamera` | Leave null → uses Camera.main |
| `buildingLayerMask` | Layer(s) containing part colliders |
| `groundLayerMask` | Layer(s) containing the ground plane |
| `dragThresholdPx` | Pixels mouse must move before drag activates (default 6) |

| Public API | Description |
|------------|-------------|
| `selected` | Currently selected `BuildingInstance` (null if none) |
| `isDragging` | True while a drag is in progress |
| `Deselect()` | Programmatic deselect (also cancels drag) |

---

### BuildingEvents (static bus)

| Event | Signature | When |
|-------|-----------|------|
| `onPlaced` | `Action<BuildingInstance>` | After board is marked and GO spawned |
| `onRemoved` | `Action<BuildingInstance>` | Before GO destroyed |
| `onMoved` | `Action<BuildingInstance, BuildingMoveArgs>` | After confirmed move/rotate |
| `onSelected` | `Action<BuildingInstance>` | On click-select |
| `onDeselected` | `Action<BuildingInstance>` | On deselect or before new select |
| `onDragUpdated` | `Action<BuildingInstance, BuildingDragArgs>` | Every frame while dragging |

`BuildingMoveArgs` — `oldPivot` (v2), `oldRotation` (int)  
`BuildingDragArgs` — `candidatePivot` (v2), `candidateRotation` (int), `isValid` (bool)

---

## 6. Coordinate System

```
Board uses XZ plane (Y = up).

    v2.y (grid)
      ↑
   3  . . . . .
   2  . . . . .
   1  . . . . .
   0  . . . . .    → v2.x (grid)
      0 1 2 3 4

World position of tile (x, y):
  worldOrigin + Vector3(x * cellSize, 0, y * cellSize)

v2.axisY must be set to 'z' (not 'y') for XZ plane:
  v2.axisY = 'z';    ← set this once in your bootstrap / GridManager.Awake
```

---

## 7. Rotation Math

Pivot stays fixed. Offsets rotate around it.

| Steps CW | Formula | Example: (-2, 0) |
|-----------|---------|-----------------|
| 0 (North) | `(x, y)` | `(-2, 0)` |
| 1 (East) | `(y, -x)` | `(0, 2)` |
| 2 (South) | `(-x, -y)` | `(2, 0)` |
| 3 (West) | `(-y, x)` | `(0, -2)` |

Visual understanding — the `BBE/BOE/O` building at each rotation:

```
Rot 0            Rot 1           Rot 2           Rot 3
BBE              OO              O               EE
BOE     →    →   OBB    →    →   EOB    →    →   BBo
O               E               EBB             O
(pivot=E top-r) (pivot=E bot-l) (pivot=E bot-l) (pivot=E top-r)
```

---

## 8. Event Bus Reference

### Subscribing (in a MonoBehaviour)

```csharp
private void OnEnable()
{
    BuildingEvents.onPlaced   += HandlePlaced;
    BuildingEvents.onRemoved  += HandleRemoved;
    BuildingEvents.onMoved    += HandleMoved;
    BuildingEvents.onSelected += HandleSelected;
}

private void OnDisable()
{
    BuildingEvents.onPlaced   -= HandlePlaced;
    BuildingEvents.onRemoved  -= HandleRemoved;
    BuildingEvents.onMoved    -= HandleMoved;
    BuildingEvents.onSelected -= HandleSelected;
}
```

### Subscribing (in an IPartBehavior — inside the placed building lifetime)

```csharp
public void OnPlaced(BuildingInstance owner, int partIndex)
{
    _owner = owner;
    BuildingEvents.onPlaced  += OnAnyBuildingPlaced;
    BuildingEvents.onMoved   += OnAnyBuildingMoved;
}

public void OnRemoved()
{
    BuildingEvents.onPlaced  -= OnAnyBuildingPlaced;
    BuildingEvents.onMoved   -= OnAnyBuildingMoved;
}
```

### Common patterns

```csharp
// UI panel — open when building selected
BuildingEvents.onSelected += b => {
    infoPanel.SetActive(true);
    buildingNameLabel.text = b.data.buildingName;
};

// Pathfinder — invalidate when layout changes
BuildingEvents.onPlaced  += _ => pathfinder.Invalidate();
BuildingEvents.onRemoved += _ => pathfinder.Invalidate();
BuildingEvents.onMoved   += (_, __) => pathfinder.Invalidate();

// Drag preview — show connector lines
BuildingEvents.onDragUpdated += (b, args) => {
    connectorUI.UpdatePreview(b, args.candidatePivot, args.isValid);
};
```

---

## 9. Interaction Flow

### 9a. Placing a new building

```
UI Button clicked
  → BuildingPlacer.SelectBuilding(someSO)
      → spawns ghost cube tiles (one per occupied offset)
      → each frame: raycast ground → CanPlace → RefreshGhost (green/red)
      → R key: rotate candidate
  → Left click (valid)
      → GridManager.Place(so, coord, rot)
          → spawns root GO + BuildingInstance
          → BuildingInstance.SpawnParts()
              → Instantiates each part prefab
              → auto-adds BuildingPartProxy
              → auto-adds BuildingHoverProxy (if Collider present)
              → calls IPartBehavior.OnPlaced() on each part
          → MarkTiles() — writes board
          → BuildingEvents.FirePlaced()
      → ghost tiles destroyed
  → Right click / Escape: cancel, ghost destroyed
```

### 9b. Selecting and moving a placed building

```
Player clicks on a part collider
  → BuildingSelector.HandleMouseDown()
      → Physics.Raycast with buildingLayerMask
      → hit.collider.gameObject.Q().up<BuildingInstance>().gf<BuildingInstance>()
      → BuildingEvents.FireSelected(instance)

Player holds mouse and moves
  → pixel distance > dragThresholdPx
  → BuildingSelector.BeginDrag()
      → GridManager.LiftOff(instance)   ← clears board tiles
      → instance.SetIndicatorActive(true)
      → each frame:
          → raycast ground → candidate coord
          → GridManager.CanPlace(data, coord, rot, ignoreInstance: instance)
          → instance.PreviewAt(coord, rot)   ← moves visual, no board write
          → instance.SetIndicator(valid)     ← green / red material swap
          → BuildingEvents.FireDragUpdated()

Player releases mouse
  → if valid:
      → GridManager.LandOn(instance, coord, rot)  ← writes board at new pos
      → BuildingEvents.FireMoved(instance, oldPivot, oldRot)
  → if invalid:
      → GridManager.LandOn(instance, oldPivot, oldRot)  ← snap back
  → instance.SetIndicatorActive(false)
```

### 9c. Hovering over a building

```
Cursor enters any part collider
  → Unity fires OnMouseEnter on that part GO
  → BuildingHoverProxy.OnMouseEnter()
      → _owner.OnHighlight(this)
          → [STUB — add your highlight logic here]

Cursor leaves all part colliders
  → Unity fires OnMouseExit
  → BuildingHoverProxy.OnMouseExit()
      → _owner.OnUnhighlight(this)
          → [STUB — add your unhighlight logic here]
```

---

## 10. Extension Stubs

### Implementing OnHighlight (outline / emissive / shader swap)

Open `BuildingInstance.cs` and fill in these two methods:

```csharp
public void OnHighlight(BuildingHoverProxy sourceProxy)
{
    // Option A: enable a global outline camera effect
    // OutlineManager.I.Add(this);

    // Option B: set emissive on all part renderers
    // foreach (var r in GetComponentsInChildren<Renderer>())
    //     r.material.SetFloat("_EmissiveStrength", 1f);

    // Option C: activate a highlight child GO (like the Indicator but always-present)
    // gameObject.Q().downNamed("Highlight").gf()?.SetActive(true);
}

public void OnUnhighlight(BuildingHoverProxy sourceProxy)
{
    // Reverse of above
}
```

> Note: If your building has multiple part colliders, `OnMouseExit` from one may
> fire before `OnMouseEnter` on the next (Unity limitation with adjacent colliders).
> If you see flicker, add an `_hoverCount` int: increment on Enter, decrement on Exit,
> only unhighlight when count reaches 0.

### Implementing ConveyorBelt (future sprint)

Open `ConveyorBelt.cs`. The `TODO` comments mark every insertion point:

```csharp
public void OnPlaced(BuildingInstance owner, int partIndex)
{
    _owner     = owner;
    _partIndex = partIndex;

    // TODO 1: Subscribe to BuildingEvents for neighbor relinking
    BuildingEvents.onPlaced  += OnAnyBuildingChanged;
    BuildingEvents.onMoved   += OnAnyBuildingMoved;
    BuildingEvents.onRemoved += OnAnyBuildingChanged;

    // TODO 2: Register with ConveyorTickSystem
    ConveyorTickSystem.Register(this);

    // TODO 3: Find initial neighbor
    RefreshSelf();
    FindNextConveyor();
}
```

---

## 11. Checklist — Bringing It Live

### Project Settings
- [ ] Create `Building` layer, assign to all part prefabs
- [ ] Create `Ground` layer, assign to ground mesh
- [ ] Set `v2.axisY = 'z'` somewhere in boot (GridManager.Awake or a boot script)

### Scene
- [ ] `[Manager]` empty GO with `GridManager`, `ConveyorTickSystem`, `BuildingSelector`, `BuildingPlacer`
- [ ] `[Buildings]` empty GO assigned to `GridManager.buildingRoot`
- [ ] Ground plane has `MeshCollider` on layer `Ground`
- [ ] Camera assigned in `BuildingSelector.selectionCamera` and `BuildingPlacer.placementCamera`

### Materials
- [ ] `GhostGreen.mat` — transparent green, no shadow cast (assign to `BuildingPlacer.ghostValidMaterial`)
- [ ] `GhostRed.mat` — transparent red (assign to `BuildingPlacer.ghostInvalidMaterial`)
- [ ] Indicator materials assigned on `BuildingInstance` component (or baked into root prefab)

### Per Building Prefab
- [ ] Root has child named exactly `Indicator` with MeshRenderer
- [ ] Each part prefab has a Collider on layer `Building`
- [ ] Behavior components (`ConveyorBelt`, `BallistaBehavior`, etc.) added to part prefabs in editor
- [ ] `BuildingPartProxy` and `BuildingHoverProxy` are NOT added in editor (auto-added at runtime)

### Per ScriptableObject
- [ ] `buildingName` filled
- [ ] At least one `parts` entry with a valid `prefab` reference
- [ ] `occupiedOffsets` do not overlap between parts (watch `OnValidate` warnings)
- [ ] `prefabLocalOffset` tuned to visual centre of each part mesh
