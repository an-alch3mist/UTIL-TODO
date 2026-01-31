# Detailed analysis documents have been created for:
## well lets see
- Grid & Tile System
- Conveyor & Transport System
- Production System (Extractors, Processors, Recipes)
- Resource & Inventory Management
- Tower Combat System
- Enemy AI & Pathfinding
- UI & Tooltip System
- Player Progression & Abilities
- Save/Load System
- Audio Management
- Time Management & Wave Spawning

```prompt
Act As Professional Script Analyser Indie GameDev In Unity3D 2020.3+.

a grid based towerFactory automation is contained in the attached file and its source by other author what you think ? 
could you provide indepth analysis how each system are handled in a single `*.md` file all 
the in-depth analysis made(such as conveyor system, objectPlaced, inventory, enemy system and UI and many more).

Also This Is Comprehensive UTIL System That I Built To Make Utilization Across All The GAME I Create, 
what you think, how this compares to the TowerFactory Source I Provided Before ? 
What System In `UTIL.cs`[Attached] Are Better and require improvements compared to TowerFactory Scripts


alright now provide step-by-step guide to implement (TowerFactory source i provided note that this was from original author) im tryna create similar for learning purpose, follows for now:
Use My Provided `UTIL.cs` for v2, Board<T>, and whenever required.

make sure to keep .Net2.0 Compatible since its Unity3D 2020.3+
and ofcourse its a 2D grid layed on XZ plane in Unity3D world


Attached:
- Attached Scripts Of Core TowerFactory.
- [Attached game image for your reference how it looks like]
- Attached ScriptableObject and asset settings(ignore if they are unity built ins)

so in other words a fully functional mvp(that has everything the main source provided)
- Grid & Tile System
- Conveyor & Transport System (shall be implemented later)(not functional(transportation, and back loading, auto align) but can be placed in world)
- Production System(not functional(shall be implemented later)(transportation, and back loading, auto align) (Extractors, Processors, Recipes)
- Resource & Inventory Management(the exrtactors dont produce any resources for now but can be placed in grid)
- Tower Combat System
- Enemy AI & Pathfinding
- UI & Tooltip System
- Player Progression & Abilities
- Save/Load System
- Audio Management
- Time Management & Wave Spawning

keep in mind while a conveyor belt is of size 1 x 1, some extractor or processors are 2 x 2 or 1 x 2 or 2 x 2 jagged etc same goes for towers, and same goes for resourceProducers such as tree, rocks etc not exactly 1 x 1 and not perfect rectagle bounds
i guess use rayCast to the collider of the placed objects or towers (since they got different height and has box collider/colliders in them)
provide a complete scene guide along.

make sure to provide a detailed .cs file for everything 
Design Patterns Used

Component-Based Architecture - Heavy use of MonoBehaviour composition
Manager Pattern - Centralized managers (GameManager, TimeManager, SpawnersManager)
Factory Pattern - Object pooling and procedural generation
Observer Pattern - Event-driven communication (delegates/UnityEvents)
Strategy Pattern - Pluggable targeting systems, movement patterns
Command Pattern - Ability system with queueing
Singleton Pattern - Global managers (via static instances)

that has all the design patterns
- use UTIL.cs where required and
- also use the save/load system in the existing UTIL.cs which is plain json save/load will do for now.
- every things a basic shape(basic prefab) and inventory icons and more for now, also just 1 enemy for now.

The Main TowerFactory Is Done By Different Main Author(i provided you the source to reference of how design patterns and architecture to be implemented workds), for now i got UTIL.cs which i use for all my games.

What I Want From You ?
A from scratch implementation of Tower Factory (refer the TowerFactory Scripts, ScriptableObjects if you got any question),

ofcource for now in a unity project i just have UTIL.cs (a comprehesive system i built).

Now provide the remaining scripts(either guide or pseudo code make sure its as close to one-one replica of main TowerFactory Source provided) where i should proceed (which has everything such as object placement (via mouse interaction along with rotation and green red altert where they can be placed and where not and weather got enough resources collected to buy item from inventory and more) towers enemy enemy path and a* similar to how it is in tower factory i provided) (conveyor system i shall impletemet later for now working prototype except conveyor, extractor everything else should does the job, 
Now provide the guide and pseudo code how it should be implemented).
```

```
make sure to keep .Net2.0 Compatible since its Unity3D 2020.3+
- along with that camera movement(use cinemachine if necessary where its a bird eyes view and middle mouse scroll to zoom in out, middle mouse clickk drag to rotate in the y axis and WASD to move around, q, e to rotate or move to next closesnt 45Deg snap you get what i mean)
```


Here is the improvised and structured prompt designed to get the best possible result from Claude 4.5 (Thinking) or a similar high-reasoning model.


# Prompt Start

**Role & Context:**
Act as a Senior Unity Solutions Architect and Gameplay Programmer specializing in Automation/Factory simulation games (e.g., Factorio, Shapez). You are an expert in C# Design Patterns (SOLID principles) and Unity 2020.3+ optimization.

**The Situation:**
I am building a grid-based Tower Defense Factory automation game on the XZ plane (3D world, 2D logic). I have two key resources:

1. **Reference Source:** A functional codebase ("TowerFactory") written by another author, containing core mechanics like grids, enemies, conveyorSystem, and towers many more(every designPattern and Source Main Game Implements).
2. **My Toolset (`UTIL.cs`):** A comprehensive, personal utility library I use across all my projects, featuring a custom `v2` coordinate struct, `Board<T>`, and serialization helpers.

**Your Objective:**
I need a complete architectural breakdown and a step-by-step implementation guide to rebuild the "TowerFactory" MVP from scratch. However, **you must integrate my `UTIL.cs**` as the foundation (specifically `v2` for coordinates and `Board<T>` for grid management), replacing the original author's equivalent systems where applicable.
---

### **Task 1: Deep Code Audit & Comparison**

Analyze the attached `TowerFactory Scripts(ALL_CORE).md` and `TowerFactory ScriptableObjects...md` against my `UTIL.cs`.

1. **Reference Analysis:** Create a single markdown section analyzing how the original author handled:
* **The Grid:** Data structure, world-to-grid conversion.
* **Object Placement:** Validation, rotation, multi-tile objects (1x1, 2x2, jagged).
* **Enemy AI:** Pathfinding (A*), movement logic.
* **Architecture:** Identify the implementation of Component-Based, Manager, Factory, Observer, Strategy, Command, and Singleton patterns.


2. **`UTIL.cs` Gap Analysis:** Compare my library to the reference.
* How does my `UTIL.v2` compare to the reference's coordinate system?
* Can `UTIL.Board<T>` fully replace the reference's tile management?
* Critique `UTIL.cs`: What is better? What is missing or needs adaptation to match the TowerFactory requirements (e.g., pathfinding interfaces)?



---

### **Task 2: The Implementation Guide (The MVP)**

Provide a comprehensive, step-by-step guide to building the MVP. This is **not** just a copy-paste of the original code; it is a **refactored reconstruction** using `UTIL.cs` as the core.

**MVP Scope:**

* **Grid:** XZ Plane, using `UTIL.Board<T>`.
* **Placement System:** Mouse interaction (Raycast), Ghost visuals (Green/Red valid checks), cost deduction, Rotation.
* **Inventory:** Basic resource checking.
* **Game Objects:**
* Towers (Functional: Aiming/Shooting).
* Enemies (Functional: Spawning, A* Pathfinding toward base).
* Factory Buildings (Conveyors, Extractors, Processors): **Placement logic ONLY.** (Note: Actual item transportation/processing logic is deferred, but they must be placeable and occupy grid space).

* **Data:** Handling multi-tile bounds (e.g., 2x2, 1x2 or even 2 x 2jagged ) using Raycasts/Colliders.
* **Conveyors**: Use Topological sorting if not there in mainSource.
* **UI:** Basic HUD and Tooltips.
* **Save/Load:** Use the JSON system inside `UTIL.cs` for now.

**For each step, provide:**

1. **Architectural Summary:** Which patterns are used here?
2. **Pseudo-Code / Skeleton C#:** Detailed enough to implement, showing clearly how to inject `UTIL.cs`. *Do not generate 1000 lines of boilerplate of Method and Variables and main logic for every single method along with summary to them of public and private API, but strictly the core logic.*
critical: make sure it covers everything main TowerFactory has, without leaving any(inpuding moving objects around via mouse and weather object can be placed or not at certain place) and in a nutshell it should be almost everything TowerFactory Did with slight improvement via `UTIL.cs`(my comprehensive library).

**Specific Implementation Requirements(Refer The Main Source Of Tower Factory In Depth):**
* **Environment:** Unity 2020.3+, .NET 2.0/Standard 2.0 compatible.
* **Grid Logic:** `v2` (int x, int y) from `UTIL.cs`.
* **Input:** Raycasting against variable height colliders (towers are tall, conveyors are flat), as well as moving and rotating gameObjects around or visual indicator weather an object(belt or extractor or tower etc) from inventory can be placed or not and many more.
* **Patterns to Enforce:**
* *Managers* (Game, Time, Spawn, Enemy and many more) as Singletons.
* *Factory* for object pooling.
* *Command* for the build system.
* *Observer* for events (resources changed, building placed).

**Attached Files:**

* `TowerFactory Scripts(ALL_CORE).md` (Reference Source)
* `TowerFactory ScriptableObjects...md` (Reference Data)
* `Reference Image Of Tower Factory` (Visual Context)

* `UTIL.cs` (My Core Library - **Mandatory Usage**)

**Output Format:**
A detailed `3. Step-by-Step Implementation Guide` of all the mechanics the main TowerFactory has;

make sure it covers everything main TowerFactory has, without leaving any(inpuding moving objects around via 
mouse and weather object can be placed or not at certain place) and 
## In a nutshell it should be almost everything
TowerFactory Did with slight improvement via `UTIL.cs`(my comprehensive library).

# Prompt End
