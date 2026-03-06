# NPC System — Complete Implementation Guide

> A full NPC framework inspired by Schedule-1's architecture.  
> Covers every system, design pattern, setup step, and extension point.

---

## Table of Contents

1. [Architecture Overview](#1-architecture-overview)
2. [File Reference](#2-file-reference)
3. [Setup: Scene Requirements](#3-setup-scene-requirements)
4. [Setup: Building an NPC Prefab](#4-setup-building-an-npc-prefab)
5. [NPCDefinition ScriptableObject](#5-npcdefinition-scriptableobject)
6. [PatrolRoute ScriptableObject](#6-patrolroute-scriptableobject)
7. [Health System](#7-health-system)
8. [Movement System](#8-movement-system)
9. [Awareness System](#9-awareness-system)
10. [Animation System](#10-animation-system)
11. [Behaviour System — Full Guide](#11-behaviour-system--full-guide)
12. [Schedule System — Full Guide](#12-schedule-system--full-guide)
13. [Character Classes](#13-character-classes)
14. [Design Patterns Reference](#14-design-patterns-reference)
15. [Extending the System](#15-extending-the-system)
16. [Common Recipes](#16-common-recipes)

---

## 1. Architecture Overview

```
                         ┌──────────────────────┐
                         │       NPC.cs          │  ← root hub, no logic
                         │  (holds all refs)     │
                         └──────┬───────────────┘
                                │
           ┌────────────────────┼──────────────────────────┐
           │                    │                          │
    ┌──────▼──────┐   ┌─────────▼────────┐    ┌───────────▼──────────┐
    │  NPCHealth  │   │  NPCMovement     │    │  NPCAwareness        │
    │  HP / KO /  │   │  NavMesh wrap    │    │  Vision / Hearing    │
    │  Death      │   │  + walk callback │    │  Alert levels        │
    └──────┬──────┘   └─────────┬────────┘    └───────────┬──────────┘
           │ events              │ locomotion               │ events
           └────────────┐        │          ┌──────────────┘
                        ▼        ▼          ▼
               ┌────────────────────────────────┐
               │      NPCBehaviour.cs           │  ← Priority Stack
               │  [sorted List<Behaviour>]      │
               │  Idle < Wander < Patrol <      │
               │  Cowering < Flee < Combat <    │
               │  Flinch < Unconscious < Dead   │
               └────────────────────────────────┘

               ┌────────────────────────────────┐
               │   NPCScheduleManager.cs        │  ← Time-driven
               │   [List<NPCAction>]            │    action queue
               │   [List<NPCSignal>]            │
               │   Driven by GameClock events   │
               └────────────────────────────────┘
```

### The Three Core Layers

| Layer | What it is | Examples |
|---|---|---|
| **Data** | ScriptableObject assets, never mutated at runtime | `NPCDefinition`, `PatrolRoute` |
| **State** | C# classes and components that change at runtime | `NPCHealth`, `NPCMovement`, `NPCAwareness` |
| **Behaviour** | MonoBehaviour logic that responds to state changes | All `Behaviour` subclasses |

---

## 2. File Reference

```
NPC_System/
├── Core/
│   ├── NPCEnums.cs             All enumerations + BehaviourPriority constants
│   ├── GameClock.cs            In-game clock singleton (fires OnMinutePassed)
│   ├── NPC.cs                  Root NPC component — holds all sub-system refs
│   ├── NPCHealth.cs            HP, knockout, death with C# Action events
│   ├── NPCMovement.cs          NavMeshAgent wrapper with typed walk callback
│   ├── NPCAwareness.cs         Vision cone, hearing, alert level system
│   └── NPCAnimation.cs         Animator wrapper with hashed parameters
│
├── Behaviour/
│   ├── Behaviour.cs            Abstract base — full lifecycle template
│   ├── NPCBehaviour.cs         Priority stack manager
│   └── Concrete/
│       ├── IdleBehaviour.cs    Lowest priority — stands with idle anim variants
│       ├── WanderBehaviour.cs  Random wander with configurable radius + pauses
│       ├── PatrolBehaviour.cs  Loop/pingpong/random patrol along PatrolRoute
│       ├── StationaryBehaviour.cs  Walk to fixed point and stand
│       ├── CoweringBehaviour.cs    Back away from threat + cower anim
│       ├── FleeBehaviour.cs    Run away from threat to safe distance
│       ├── CombatBehaviour.cs  Chase and melee/ranged attack
│       ├── RagdollBehaviour.cs Toggle physics ragdoll
│       ├── DeadBehaviour.cs    Terminal state — disables agent
│       ├── UnconsciousBehaviour.cs Knocked out state
│       ├── HeavyFlinchBehaviour.cs Short interrupt for big hits
│       ├── FaceTargetBehaviour.cs  Smooth look-at for dialogue/interaction
│       ├── CallPoliceBehaviour.cs  Crime witness reaction with cooldown
│       └── GenericDialogueBehaviour.cs  Pause and play conversation anim
│
├── Schedule/
│   ├── NPCAction.cs            Abstract base — Template Method lifecycle
│   ├── NPCSignal.cs            Discrete command base extending NPCAction
│   ├── NPCScheduleManager.cs   Clock-driven queue + signal injection
│   ├── Actions/
│   │   └── ScheduledActions.cs WalkToLocation, Idle, Patrol, Sleep
│   └── Signals/
│       └── NPCSignals.cs       WalkToLocation, WaitForDuration, FaceTarget, UseObject
│
├── Data/
│   ├── NPCDefinition.cs        ScriptableObject — all static NPC data
│   └── PatrolRoute.cs          ScriptableObject — ordered waypoints
│
└── CharacterClasses/
    └── CharacterClasses.cs     CustomerNPC, PoliceNPC, EmployeeNPC
```

---

## 3. Setup: Scene Requirements

### Step 1 — Add GameClock

Create an empty GameObject named `_GameClock` in your scene. Add the `GameClock` component.

```
_GameClock
  └── GameClock.cs
        SecondsPerMinute: 1.0      (1 real second = 1 game minute)
        StartTimeMinutes: 480      (starts at 8:00 AM)
```

**Tip:** Set `SecondsPerMinute = 0.25` to make time run 4× faster during testing.

### Step 2 — NavMesh

Bake a NavMesh for your level. All NPC movement uses `NavMeshAgent`. No A* plugin is required but any A*-based system can replace the internals of `NPCMovement` without changing any public API.

### Step 3 — Player tag

Make sure your Player GameObject has the **tag `Player`**. `NPCAwareness` searches by this tag. Change `CheckForThreats()` in `NPCAwareness.cs` if you use a different detection method.

### Step 4 — Layer mask for sight occlusion

In your `NPCDefinition`, set **SightOcclusionMask** to include walls, buildings, and any opaque geometry. Exclude NPCs and Players so they don't occlude each other.

---

## 4. Setup: Building an NPC Prefab

### Required Components (root GameObject)

```
NPC_Character (GameObject)
├── NPC.cs                    ← add first
├── NPCHealth.cs
├── NavMeshAgent              ← Unity built-in
├── NPCMovement.cs
├── NPCAwareness.cs
├── Animator                  ← Unity built-in
├── NPCAnimation.cs
├── NPCBehaviour.cs
├── NPCScheduleManager.cs
└── [Behaviour children — see below]
```

### Adding Behaviour Children

Each behaviour is a **separate child GameObject** with one Behaviour component. This keeps them independently serializable in the Inspector.

```
NPC_Character
├── ... (root components above)
└── Behaviours (empty parent)
    ├── Idle          → IdleBehaviour.cs    Priority=100   EnabledOnAwake=✓
    ├── Wander        → WanderBehaviour.cs  Priority=200
    ├── Patrol        → PatrolBehaviour.cs  Priority=300
    ├── Stationary    → StationaryBehaviour.cs  Priority=400
    ├── FaceTarget    → FaceTargetBehaviour.cs  Priority=1000
    ├── CallPolice    → CallPoliceBehaviour.cs  Priority=5000
    ├── Cowering      → CoweringBehaviour.cs    Priority=4000
    ├── Flee          → FleeBehaviour.cs        Priority=6000
    ├── Combat        → CombatBehaviour.cs      Priority=7000
    ├── HeavyFlinch   → HeavyFlinchBehaviour.cs Priority=8000
    ├── Unconscious   → UnconsciousBehaviour.cs Priority=8500
    ├── Ragdoll       → RagdollBehaviour.cs     Priority=9000
    ├── Dead          → DeadBehaviour.cs        Priority=10000
    └── Dialogue      → GenericDialogueBehaviour.cs  Priority=3000
```

### Wiring NPCBehaviour Inspector References

On the `NPCBehaviour` component, assign each behaviour reference in the Inspector:
- `IdleBehaviour` → drag Idle child
- `WanderBehaviour` → drag Wander child
- `CombatBehaviour` → drag Combat child
- ... etc.

### Adding a Schedule

For NPCs with daily routines, add `NPCAction` children under a `Schedule` empty:

```
NPC_Character
└── Schedule
    ├── Action_Sleep    → ScheduledAction_Sleep.cs     StartTime=0    (midnight)
    ├── Action_Patrol   → ScheduledAction_Patrol.cs    StartTime=480  (8 AM)
    ├── Action_Idle     → ScheduledAction_Idle.cs      StartTime=720  (noon)
    └── Action_Patrol2  → ScheduledAction_Patrol.cs    StartTime=900  (3 PM)
```

---

## 5. NPCDefinition ScriptableObject

Create a definition via `Right-click → NPCSystem → NPC Definition`.

### Key Fields

| Field | Description | Typical Values |
|---|---|---|
| `DisplayName` | Shown in debug UI | "Guard", "Dealer" |
| `Archetype` | Drives default behaviour wiring | `Civilian`, `Police`, `Employee` |
| `Faction` | For faction-based targeting | `Civilian`, `Police`, `Cartel` |
| `MaxHealth` | HP pool | 100 (civilian), 200 (police) |
| `KnockoutChance` | Chance lethal hit KOs instead of kills | 0.2–0.4 |
| `UnconsciousDuration` | Seconds until auto-revive. 0 = never | 30–60 |
| `WalkSpeed` / `RunSpeed` | NavMesh speeds | 2.5 / 5.5 |
| `SightRange` | Cone distance in meters | 8–15 |
| `SightAngle` | Full cone angle in degrees | 90–120 |
| `HearingRange` | Radius for sound detection | 4–8 |
| `CanFight` | Enables CombatBehaviour | true for guards/police |
| `CallsPoliceOnCrime` | Enables CallPoliceBehaviour | true for civilians |
| `FleesThreat` | Enables FleeBehaviour | true for weak civilians |
| `SightOcclusionMask` | Layers that block sight rays | Default + Wall layers |

---

## 6. PatrolRoute ScriptableObject

Create via `Right-click → NPCSystem → Patrol Route`.

### Setup

1. Place empty GameObjects in your scene at each waypoint position
2. Name them clearly: `Patrol_Checkpoint_A`, `Patrol_Checkpoint_B`, etc.
3. In the `PatrolRoute` asset, add the names to `WaypointObjectNames[]`
4. `PatrolRoute.Resolve()` is called automatically by `PatrolBehaviour.Initialize()`

### Modes

| Mode | Behaviour |
|---|---|
| `Loop` | A → B → C → A → B → ... |
| `PingPong` | A → B → C → B → A → B → ... |
| `Random` | Random waypoint each time (no repeat) |

---

## 7. Health System

### API

```csharp
// Deal damage (from weapon, explosion, etc.)
npc.TakeDamage(float amount, GameObject source, EKnockdownCause cause);

// Heal
npc.Health.Heal(float amount);

// Force knock out without killing
npc.Health.KnockOut(EKnockdownCause.Taser);

// Force revive
npc.Health.Revive();

// Instant kill
npc.Health.Kill(EKnockdownCause.Gunshot);
```

### Subscribing to Events

```csharp
npc.Health.OnDamaged    += (amount, source) => { ... };
npc.Health.OnKnockedOut += (cause) => { ... };
npc.Health.OnRevived    += () => { ... };
npc.Health.OnDied       += (cause) => { ... };

// Or via the root NPC class:
npc.OnDied    += (cause) => { ... };
npc.OnDamaged += (amount, source) => { ... };
```

### State Checks

```csharp
npc.IsAlive        // true if EHealthState.Alive
npc.IsDead         // true if EHealthState.Dead
npc.IsUnconscious  // true if EHealthState.Unconscious
npc.Health.HealthPercent  // 0.0–1.0 for HP bars
```

---

## 8. Movement System

### Walk API

```csharp
// Walk to a position, get a callback on finish
npc.Movement.WalkTo(
    Vector3 destination,
    NPCMovement.WalkCallback callback,
    bool run = false,
    bool teleportOnFail = false
);

// Walk toward a moving Transform
npc.Movement.WalkToTransform(
    Transform target,
    NPCMovement.WalkCallback callback,
    float arriveRange = 1.5f,
    bool run = false
);

// Stop all movement
npc.Movement.StopWalk(bool fireCallback = true);

// Teleport (no pathfinding)
npc.Movement.Teleport(Vector3 position);

// Face a direction smoothly
npc.Movement.FaceDirection(Vector3 worldPosition, float speedMultiplier = 1f);
```

### WalkCallback

```csharp
void OnWalkFinished(EWalkResult result)
{
    switch (result)
    {
        case EWalkResult.Success:     // reached destination
        case EWalkResult.Failed:      // no path found
        case EWalkResult.Interrupted: // StopWalk() was called
        case EWalkResult.Timeout:     // took too long
    }
}
```

### Speed Modes

```csharp
npc.Movement.SetSpeedMode(ELocomotionState.Walking);  // WalkSpeed from definition
npc.Movement.SetSpeedMode(ELocomotionState.Running);  // RunSpeed from definition
```

---

## 9. Awareness System

### Alert Level Flow

```
Calm ──(sight/sound)──► Suspicious ──(confirmed)──► Alert ──(attack)──► Combat
 ▲                                                                         │
 └──────────────────────(threat lost + calm down over time)───────────────┘
```

### Events

```csharp
npc.Awareness.OnAlertLevelChanged += (oldLevel, newLevel) => { ... };
npc.Awareness.OnThreatSpotted     += (gameObject) => { ... };
npc.Awareness.OnThreatLost        += (gameObject) => { ... };
npc.Awareness.OnSoundHeard        += (worldPosition) => { ... };
```

### Manual Triggers

```csharp
// Make NPC aware of a threat immediately (bypasses vision check)
npc.Awareness.SetThreat(playerGameObject);

// Trigger sound awareness (gunshot, explosion)
npc.Awareness.HearSound(soundWorldPos, soundRadius);

// Manually check if NPC can see a specific transform
bool canSee = npc.Awareness.CanSee(someTransform);

// Force calm
npc.Awareness.Calm();
```

### Properties

```csharp
npc.Awareness.AlertLevel          // current EAlertLevel
npc.Awareness.PrimaryThreat       // the threat GameObject (or null)
npc.Awareness.LastKnownThreatPos  // last known position
npc.Awareness.HasLineOfSight      // currently seeing threat
```

---

## 10. Animation System

### Animator Parameters (set up these in your Animator Controller)

| Parameter Name | Type | Driven By |
|---|---|---|
| `Speed` | Float | `NPCMovement.IsMoving` — 0 = idle, 1 = walk, 2 = run |
| `IsRunning` | Bool | `ELocomotionState.Running` |
| `IsCrouching` | Bool | Manual call |
| `IsDead` | Bool | `NPCHealth.OnDied` |
| `IsUnconscious` | Bool | `NPCHealth.OnKnockedOut` |
| `AlertLevel` | Int | `NPCAwareness.AlertLevel` (0=Calm, 3=Combat) |
| `Hit` | Trigger | Small damage |
| `HeavyHit` | Trigger | Large damage (≥10) |
| `Attack` | Trigger | Melee swing |
| `Greet` | Trigger | Greeting interaction |
| `Scared` | Trigger | Fear reaction |
| `Talk` | Trigger | Dialogue / phone use |
| `IdleVariant` | Int | Rotated by IdleBehaviour |

### Ragdoll Setup

1. Add `Rigidbody` and `Collider` to every bone that should ragdoll
2. Assign all ragdoll Rigidbodies to `NPCAnimation._ragdollBodies[]`
3. Assign all ragdoll Colliders to `NPCAnimation._ragdollColliders[]`
4. The system toggles them via `SetRagdollActive(bool)`

---

## 11. Behaviour System — Full Guide

### How the Priority Stack Works

Every tick (default 0.5s), `NPCBehaviour.EvaluateStack()` runs:

```
For each Behaviour in stack (highest priority first):
    If b.Enabled AND b.WantsToBeActive():
        ► This is the new ActiveBehaviour
        ► Deactivate the previous one
        ► Activate this one
        Break
```

**Key insight:** You never write state-machine transition code. You just `Enable()` or `Disable()` individual behaviours and the stack handles which one runs.

### Lifecycle in Detail

```
                 Enable()
                    │
    ┌───────────────▼───────────────────────────┐
    │              ENABLED                      │
    │  Participates in priority evaluation      │
    │                                           │
    │  Override: OnEnabled()                    │
    │  Override: WantsToBeActive() → bool       │
    └──────────────────────────────────────────┬┘
                                               │ EvaluateStack() picks this one
                                               ▼
    ┌──────────────────────────────────────────┐
    │              ACTIVE                       │
    │  BehaviourUpdate() runs every frame       │
    │  OnActiveTick() runs every 0.5s          │
    │  BehaviourLateUpdate() after Update      │
    │                                           │
    │  Override: OnActivated()                  │
    │  Override: BehaviourUpdate()              │
    │  Override: OnActiveTick()                 │
    │  Override: OnDeactivated()                │
    └──────────────────────────────────────────┘
```

### Writing a Custom Behaviour

```csharp
public class MyCustomBehaviour : Behaviour
{
    [Header("My Settings")]
    [SerializeField] private float _myParam = 3f;

    protected override void Awake()
    {
        base.Awake();
        BehaviourName  = "MyCustom";
        Priority       = 750;   // pick a value from BehaviourPriority constants
        EnabledOnAwake = false;
    }

    // Override: when should this behaviour run?
    public override bool WantsToBeActive()
        => Enabled && /* your condition */;

    // Override: runs once when this becomes active
    protected override void OnActivated()
    {
        Movement?.WalkTo(someTarget, WalkCallback);
    }

    // Override: runs every frame while active
    public override void BehaviourUpdate()
    {
        // per-frame logic
    }

    // Override: runs every 0.5s (good for expensive checks)
    public override void OnActiveTick()
    {
        // periodic check
    }

    // Override: runs once when deactivated
    protected override void OnDeactivated()
    {
        // cleanup
    }
}
```

Then add it to the NPC prefab as a child and assign the field reference in `NPCBehaviour`.

---

## 12. Schedule System — Full Guide

### Time Format

All times are **minutes from midnight (0–1439)**:
```
0    = 12:00 AM (midnight)
480  = 08:00 AM
720  = 12:00 PM (noon)
1080 = 06:00 PM
1320 = 10:00 PM
1439 = 11:59 PM
```

### Scheduled Actions (time-window tasks)

Scheduled actions run at their `StartTime` and end at `GetEndTime()`.

```csharp
// Example: NPC sleeps midnight–8 AM
[StartTime=0]   ScheduledAction_Sleep        → end=480
[StartTime=480] ScheduledAction_Patrol       → end=720  (8 AM–noon)
[StartTime=720] ScheduledAction_Idle         → end=900  (noon–3 PM)
[StartTime=900] ScheduledAction_WalkToLocation → end=1080 (3 PM–6 PM)
[StartTime=1080] ScheduledAction_Patrol      → end=1320 (6 PM–10 PM)
[StartTime=1320] ScheduledAction_Sleep       → end=1440
```

### Writing a Custom Scheduled Action

```csharp
public class ScheduledAction_EatFood : NPCAction
{
    [Header("Eat Food")]
    public string TableObjectName;
    public int    DurationMinutes = 30;

    private Transform _table;

    public override string GetName()            => "Eat Food";
    public override string GetTimeDescription() => $"Eat at {StartTime/60:D2}:{StartTime%60:D2}";
    public override int    GetEndTime()         => StartTime + DurationMinutes;

    public override void Started()
    {
        base.Started();
        _table = GameObject.Find(TableObjectName)?.transform;
        if (_table != null)
            Movement?.WalkTo(_table.position, OnArrived);
    }

    private void OnArrived(EWalkResult result)
    {
        if (result == EWalkResult.Success)
            Animation?.TriggerTalk();  // use eating anim in your controller
    }

    public override void End()
    {
        base.End();
        // Walk back to default position
    }
}
```

### Signals (instant commands)

Signals override the current schedule at any time from external code:

```csharp
// Create signal on the NPC prefab as a child GameObject
// Then fire it from any game system:

var walkSignal = npc.GetComponentInChildren<NPCSignal_WalkToLocation>();
walkSignal.TargetTransform = someTransform;
walkSignal.RunToTarget     = true;
npc.IssueSignal(walkSignal);

// Or use the schedule manager directly:
npc.Schedule.IssueSignal(signal);
```

**Signals and scheduled actions respect priority:**
- Signal with higher `Priority` than current action → overrides it
- When signal ends, the interrupted scheduled action resumes via `JumpTo()`

---

## 13. Character Classes

### CustomerNPC

```csharp
// Spawn a customer and begin product request
var customer = Instantiate(customerPrefab);
customer.Initialize(definition);
customer.BeginProductRequest();

// Subscribe to deal events
customer.OnRequestStarted  += (c) => ShowDealUI();
customer.OnDealAccepted    += (c) => ProcessPayment();
customer.OnDealRejected    += (c) => ShowRejectAnim();
customer.OnDealLeft        += (c) => DeductReputation();

// Player hands over product
bool accepted = customer.EvaluateDeal(
    offeredQuality: 0.8f,
    offeredPrice:   50f,
    expectedPrice:  45f
);
```

### PoliceNPC

```csharp
// Respond to a crime report
policeNPC.RespondToCrime(playerGameObject);

// Attempt to arrest the player
policeNPC.AttemptArrest(playerGameObject);

// Subscribe
policeNPC.OnArrestAttempt += (cop, target) => StartArrestMinigame();
```

### EmployeeNPC

```csharp
// Assign to a station
employeeNPC.AssignToStation(chemistryStationTransform, "Chemistry");

// Unassign
employeeNPC.Unassign();

// Subscribe to task completion
employeeNPC.OnTaskCompleted += (emp) => OutputProductFromStation();
employeeNPC.WorkEfficiency  // multiply production speed by this
```

---

## 14. Design Patterns Reference

### Template Method
Every `Behaviour` and `NPCAction` uses Template Method. The abstract base defines the lifecycle skeleton (`Started`, `ActiveUpdate`, `MinPassed`, `End`). Subclasses only override what they need.

```
NPCAction.Started() { ActionState = Active; }   ← base does bookkeeping
    └─ ScheduledAction_Patrol.Started()          ← override adds patrol logic
           { base.Started(); patrol.Enable(); }
```

### Priority Strategy Stack
`NPCBehaviour` holds a sorted list. The evaluation loop picks the first enabled, wanting behaviour. No state-machine transition table exists.

### Observer (C# Action delegates)
`NPCHealth`, `NPCAwareness`, `NPCScheduleManager` all expose `event Action<...>`. Systems communicate without direct references to each other.

### Factory Method
`NPCDefinition` contains configuration. `NPC.Initialize(definition)` is the factory — it wires up the entire NPC from a single ScriptableObject asset.

### Command
Every `NPCSignal` is a Command: it encapsulates a discrete task, can be queued, has a priority, and fires callbacks on completion.

---

## 15. Extending the System

### Adding a New Behaviour

1. Create `MyBehaviour.cs` extending `Behaviour`
2. Set `BehaviourName`, `Priority` in `Awake()`
3. Override `OnActivated()`, `BehaviourUpdate()`, `OnDeactivated()`
4. Add as child GameObject on NPC prefab
5. Add field reference on `NPCBehaviour` and assign in Inspector
6. `Enable()` it when the condition occurs (in `NPCBehaviour.OnAlertLevelChanged`, etc.)

### Adding a New Scheduled Action

1. Create `ScheduledAction_MyTask.cs` extending `NPCAction`
2. Implement `GetName()`, `GetTimeDescription()`, `GetEndTime()`
3. Override `Started()`, optionally `MinPassed()` and `End()`
4. Add as child of the NPC's `Schedule` GameObject
5. Set `StartTime` in the Inspector

### Adding a New Signal

1. Create `NPCSignal_MyCommand.cs` extending `NPCSignal`
2. Override `Started()` to trigger the action
3. Call `End()` or `Interrupt()` when done
4. Issue from any external system via `npc.IssueSignal(signal)`

### Replacing NavMesh with A*

In `NPCMovement.cs`, replace all `NavMeshAgent` calls with your A* agent:
```csharp
// Replace:
_agent.SetDestination(Destination);
_agent.isStopped = true;

// With A* equivalent:
_aiPath.destination = Destination;
_aiPath.canMove = false;
```
The public API (`WalkTo`, `StopWalk`, `Teleport`, `FaceDirection`) stays identical — nothing outside `NPCMovement` needs to change.

### Adding Networked NPCs (FishNet/Mirror)

1. Change `NPC.cs` base from `MonoBehaviour` to `NetworkBehaviour`
2. Change `Behaviour.cs` and `NPCAction.cs` base to `NetworkBehaviour`
3. Add `[ServerRpc]` to mutating calls (TakeDamage, IssueSignal)
4. Add `[ObserversRpc]` to state-sync calls (health changes, behaviour switches)
5. Add `[SyncVar]` to key state fields (AlertLevel, ActiveBehaviourIndex)

---

## 16. Common Recipes

### Give NPC a daily routine

```csharp
// In Inspector on Schedule children:
// Sleep:  StartTime=0,    DurationMinutes=480
// Patrol: StartTime=480,  Route=MorningRoute
// Idle:   StartTime=720,  DurationMinutes=60
// Walk:   StartTime=780,  TargetObjectName="ShopEntrance"
// Patrol: StartTime=900,  Route=AfternoonRoute
// Sleep:  StartTime=1320, DurationMinutes=480
```

### Instantly send NPC to a location

```csharp
var signal = npc.GetComponentInChildren<NPCSignal_WalkToLocation>();
signal.TargetPosition = destinationPos;
signal.UseTransform   = false;
signal.RunToTarget    = false;
npc.IssueSignal(signal);
```

### Make NPC react to a gunshot

```csharp
// In your weapon firing code:
foreach (var npc in FindObjectsOfType<NPC>())
    npc.Awareness.HearSound(gunPosition, gunSoundRadius: 20f);
```

### Start a conversation

```csharp
// Stop NPC and play talk anim for 5 seconds
npc.LookAt(playerTransform);
npc.StartDialogue(5f);

// Subscribe to know when it ends
npc.BehaviourManager
   .GetBehaviour<GenericDialogueBehaviour>()
   .OnDialogueEnded += () => ProcessDialogueResult();
```

### Wire up a health bar

```csharp
npc.Health.OnHealthChanged += (current, max) =>
{
    healthBarFill.fillAmount = current / max;
};
```

### Assign an employee to a workstation

```csharp
var emp = Instantiate(employeePrefab).GetComponent<EmployeeNPC>();
emp.Initialize(employeeDefinition);
emp.AssignToStation(chemStationTransform, "Chemistry");
emp.OnTaskCompleted += (e) => Debug.Log($"{e.DisplayName} finished their task!");
```

---

*System designed after analysing the Schedule-1 decompiled source.*  
*Replace namespace `NPCSystem` with your project's namespace as needed.*
