# Build Your NPC System — Problem-Solving Course

> **Who this is for:** Advanced Unity developer, Mixamo animations ready, no NavMesh experience.  
> **Goal:** Build the full NPC system from scratch, understanding every decision.  
> **Format:** Each phase = one problem to solve + design pattern + pseudocode + checkpoint.

---

## How to Use This Guide

Each phase answers three questions:

```
PROBLEM   → What breaks without solving this?
PATTERN   → What design principle solves it cleanly?
BUILD IT  → Pseudocode + exact implementation steps
```

Do not skip ahead. Every phase depends on the previous one.  
Write the code yourself — use the provided implementation files only as reference when stuck.

---

## Phase Map

```
PHASE 0 ─── NavMesh Fundamentals          (foundation — do this first)
PHASE 1 ─── GameClock                     (time system — drives everything)
PHASE 2 ─── NPCDefinition (Data layer)    (ScriptableObject config)
PHASE 3 ─── NPCHealth                     (state + events)
PHASE 4 ─── NPCMovement                   (NavMesh wrapper + callback)
PHASE 5 ─── NPCAwareness                  (vision + alert levels)
PHASE 6 ─── NPCAnimation                  (Mixamo animator wiring)
PHASE 7 ─── Behaviour base class          (Template Method lifecycle)
PHASE 8 ─── NPCBehaviour stack            (Priority Strategy pattern)
PHASE 9 ─── Concrete Behaviours × 14     (each one a mini-problem)
PHASE 10 ── NPCAction base class          (scheduled task template)
PHASE 11 ── NPCScheduleManager            (clock-driven queue)
PHASE 12 ── NPCSignal (commands)          (Command pattern)
PHASE 13 ── NPC root class                (composition hub)
PHASE 14 ── Character specializations     (inheritance extensions)
PHASE 15 ── Integration + full test       (end-to-end)
```

---

# PHASE 0 — NavMesh Fundamentals

## The Problem

Your character model exists in the scene but has no concept of walkable ground, obstacles, or how to path around them. Before writing a single line of NPC code, the movement layer must work.

## Design Pattern: Wrapper / Adapter

You will wrap Unity's `NavMeshAgent` behind your own `NPCMovement` class. This means the rest of your NPC system never talks directly to `NavMeshAgent`. If you later switch to A* Pathfinding Pro, you only change `NPCMovement` — nothing else breaks.

## Step 0.1 — Bake your first NavMesh

**Do this in Unity:**

1. Window → AI → Navigation (open the Navigation panel)
2. Select every static piece of ground/floor geometry in your scene
3. In the Inspector → Static dropdown → check **Navigation Static**
4. In Navigation panel → **Bake** tab → hit **Bake**
5. You will see a blue overlay on walkable surfaces — that is your NavMesh

**Verify it worked:**
```
Blue area = NPC can walk here
No blue = obstacle or not baked
```

**Common gotcha:** If your ground has no blue, it might not be marked Navigation Static. Select it, Inspector → top-right "Static" dropdown → check Navigation Static → rebake.

## Step 0.2 — First movement experiment

Create a test scene with:
- A baked NavMesh plane
- Your character model
- No NPC code yet — just raw NavMeshAgent

**Add to your character temporarily:**

```csharp
// TestNavMesh.cs — TEMPORARY, delete after Phase 0
using UnityEngine;
using UnityEngine.AI;

public class TestNavMesh : MonoBehaviour
{
    NavMeshAgent agent;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        // Set speed
        agent.speed = 3.5f;
        // Stop radius
        agent.stoppingDistance = 0.5f;
    }

    void Update()
    {
        // Click anywhere → NPC walks there
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
                agent.SetDestination(hit.point);
        }
    }
}
```

**Inspector setup:**
- Add `NavMeshAgent` component to your character
- Add `TestNavMesh` component
- Play → click on the ground → character should walk there

**Problems you will hit and how to solve them:**

| Problem | Cause | Fix |
|---|---|---|
| Character slides without animation | NavMeshAgent moves the transform, but you haven't wired it to the Animator yet | Normal — handled in Phase 6 |
| Character sinks into ground | NavMeshAgent base offset wrong | NavMeshAgent → Base Offset = character height / 2 |
| Character doesn't move | NavMesh not baked or destination off NavMesh | Rebake; check blue overlay covers destination |
| Character spins in place | `updateRotation = true` but root motion fights it | Disable root motion on Animator during testing |

## Step 0.3 — Understand NavMeshAgent properties you will use

```
speed              → controls walk/run (you'll change this dynamically)
stoppingDistance   → how close is "arrived"
isStopped          → pauses the agent (keeps path, just freezes)
ResetPath()        → clears destination entirely
remainingDistance  → meters left to destination
pathPending        → true while calculating path
isOnNavMesh        → safety check before any agent call
velocity           → current movement vector (drives animations)
```

**Checkpoint 0:** Your character walks to a clicked position. NavMesh is baked. Delete `TestNavMesh.cs` and move to Phase 1.

---

# PHASE 1 — GameClock

## The Problem

NPC schedules need time. "Walk to patrol at 8 AM, eat lunch at noon, sleep at 10 PM." Unity's `Time.time` is real seconds since launch — not useful. You need an in-game clock that:

- Runs in configurable "game minutes" per real second
- Fires events every minute (so schedule can react)
- Fires an event at midnight (day rollover)
- Can be paused, resumed, and fast-forwarded

## Design Pattern: Singleton + Observer

The clock is a **Singleton** (one exists in the scene, everything can access it). It uses the **Observer** pattern — it fires events, other systems subscribe without the clock knowing who's listening.

**Why not just use `Time.time`?**  
Because `Time.time` can't be paused, looped, or sped up independently. A game clock you control can run 4× faster for testing, pause for dialogue, and reset at midnight.

## Pseudocode

```
CLASS GameClock (Singleton MonoBehaviour)

    FIELDS:
        secondsPerMinute : float   // how fast time runs
        startTimeMinutes : int     // what time the game starts at
        currentMinute    : int     // 0-1439
        currentDay       : int
        timer            : float   // accumulator

    EVENTS:
        OnMinutePassed(currentMinute : int)
        OnDayStarted(dayNumber : int)

    ON AWAKE:
        register as singleton
        currentMinute = startTimeMinutes

    ON UPDATE:
        if paused → return
        timer += deltaTime
        if timer >= secondsPerMinute:
            timer -= secondsPerMinute
            AdvanceMinute()

    FUNCTION AdvanceMinute():
        currentMinute++
        if currentMinute >= 1440:
            currentMinute = 0
            currentDay++
            FIRE OnDayStarted(currentDay)
        FIRE OnMinutePassed(currentMinute)

    FUNCTION Pause() / Resume() / AdvanceBy(minutes) / SetTime(minute)
    
    PROPERTY Hour   → currentMinute / 60
    PROPERTY Minute → currentMinute % 60
```

## Build It

**Step 1.1** — Create `GameClock.cs`. Implement the pseudocode above.

**Step 1.2** — Test it:

```csharp
// In a temporary TestClock.cs
void Start()
{
    GameClock.Instance.OnMinutePassed += (min) =>
        Debug.Log($"Time: {min/60:D2}:{min%60:D2}");
}
```

Set `secondsPerMinute = 0.1` → you should see time flying in the console.

**Step 1.3** — Add this utility you will need later:

```
FUNCTION MinutesUntil(targetMinute):
    if targetMinute >= currentMinute:
        return targetMinute - currentMinute
    else:
        return (1440 - currentMinute) + targetMinute   // wraps midnight
```

**Checkpoint 1:** Console shows time advancing every second. `OnMinutePassed` fires. Test `AdvanceBy(120)` and confirm time jumps 2 hours.

---

# PHASE 2 — NPCDefinition (The Data Layer)

## The Problem

You will make many different NPCs: guards, customers, police, employees. Each has different speed, health, sight range, faction. If you hardcode these values into components, every change requires a code edit and recompile. Designers can't tweak balance without touching code.

**The bigger problem:** You need to share configuration across multiple prefab variants without duplicating it.

## Design Pattern: ScriptableObject as Data Asset

A `ScriptableObject` asset lives in your project folder — not in a scene. It is:
- Created once, referenced by many NPC prefabs
- Editable by designers in the Inspector with zero code
- Never mutated at runtime (it is static configuration)

This is the **Definition/Instance Split** principle from Schedule-1:
```
NPCDefinition  (ScriptableObject)  =  "what kind of NPC is this?"
NPC            (MonoBehaviour)     =  "this specific living NPC"
```

Think of a book. `NPCDefinition` is the template/archetype. Each NPC in the world is a printed copy.

## Pseudocode

```
[CreateAssetMenu] 
CLASS NPCDefinition : ScriptableObject

    // Identity
    displayName  : string
    uniqueID     : string      // GUID
    archetype    : ENPCArchetype   // Civilian / Employee / Police / Combat

    // Health
    maxHealth          : float
    knockoutChance     : float    // 0-1 chance lethal hit KOs instead of kills
    unconsciousDuration: float    // seconds until auto-revive

    // Movement
    walkSpeed     : float
    runSpeed      : float
    rotationSpeed : float
    stoppingDistance : float

    // Awareness  
    sightRange         : float
    sightAngle         : float   // full cone degrees
    hearingRange       : float
    calmDownRate       : float   // how fast alert decays
    sightOcclusionMask : LayerMask

    // Combat
    canFight    : bool
    canShoot    : bool
    attackRange : float
    attackDamage: float
    attackCooldown : float

    // Civilian reactions
    callsPoliceOnCrime : bool
    fleesThreat        : bool
    panicChance        : float

    ON VALIDATE:
        if uniqueID is empty → generate new GUID
        runSpeed = max(runSpeed, walkSpeed)   // run can't be slower than walk
```

## Build It

**Step 2.1** — Create the `ENPCArchetype` and `ENPCFaction` enums in `NPCEnums.cs`. Create all enums for the whole project at once:

```
ENPCArchetype: Civilian, Customer, Employee, CombatNPC, Police
ENPCFaction:   Civilian, Customer, Employee, Police, Cartel, Gang
EHealthState:  Alive, Unconscious, Dead
EAlertLevel:   Calm, Suspicious, Alert, Combat
EWalkResult:   Success, Failed, Interrupted, Timeout
EKnockdownCause: None, Melee, Gunshot, Explosion, Taser, Fall
ELocomotionState: Idle, Walking, Running, Crouching, Ragdoll
EActionState: Pending, Active, Completed, Interrupted, Skipped
```

Also create a static class `BehaviourPriority` with int constants:
```
Idle=100, Wander=200, Patrol=300, Stationary=400,
FaceTarget=1000, Cowering=4000, CallPolice=5000,
Flee=6000, Combat=7000, HeavyFlinch=8000,
Unconscious=8500, Ragdoll=9000, Dead=10000
```

**Step 2.2** — Create `NPCDefinition.cs` as a ScriptableObject.

**Step 2.3** — In Unity: right-click in Project → Create → NPCSystem → NPC Definition. Create two assets: `NPC_Civilian` and `NPC_Guard`. Fill in different values.

**Checkpoint 2:** You have two `.asset` files in your project. Each has different values. No code changes were needed to add a second NPC type.

---

# PHASE 3 — NPCHealth

## The Problem

NPCs need HP. When they reach 0, they should either die or get knocked out (with a chance roll). Other systems need to know when damage happens, when death happens, when revival happens — but you don't want `NPCHealth` to directly call those systems (that creates tight coupling and makes the code brittle).

## Design Pattern: Observer via C# Action Events

`NPCHealth` owns and mutates the HP value. Everything else **subscribes** to its events.

```
NPCHealth fires events → NPCAnimation plays hit anim
                       → NPCBehaviour enables DeadBehaviour
                       → UI updates health bar
                       → Achievement system records death
```

None of these subscribers are known to `NPCHealth`. You add new subscribers without touching `NPCHealth`. This is the **Open/Closed Principle** in action.

**Why C# `event Action` instead of Unity's `UnityEvent`?**
- Faster at runtime (no reflection overhead)  
- Easier to subscribe/unsubscribe in code  
- Supports typed parameters  
- No Inspector coupling  

## Pseudocode

```
CLASS NPCHealth : MonoBehaviour

    EVENTS:
        OnHealthChanged(current: float, max: float)
        OnDamaged(amount: float, source: GameObject)
        OnKnockedOut(cause: EKnockdownCause)
        OnRevived()
        OnDied(cause: EKnockdownCause)

    FIELDS:
        maxHealth     : float
        currentHealth : float
        state         : EHealthState    // Alive / Unconscious / Dead
        reviveTimer   : float
        reviveTimerActive : bool

    PROPERTIES:
        IsAlive       → state == Alive
        IsUnconscious → state == Unconscious
        IsDead        → state == Dead
        IsVulnerable  → IsAlive OR IsUnconscious
        HealthPercent → currentHealth / maxHealth

    FUNCTION Initialize(definition):
        maxHealth = definition.maxHealth
        currentHealth = maxHealth
        state = Alive

    FUNCTION TakeDamage(amount, source, cause):
        if NOT IsVulnerable OR amount <= 0 → return
        clamp amount to currentHealth
        currentHealth -= amount
        FIRE OnDamaged(amount, source)
        FIRE OnHealthChanged(currentHealth, maxHealth)
        if currentHealth <= 0:
            HandleLethalDamage(cause)

    FUNCTION HandleLethalDamage(cause):
        roll knockoutChance
        if lucky → KnockOut(cause)
        else     → Kill(cause)

    FUNCTION KnockOut(cause):
        if NOT IsAlive → return
        set state = Unconscious
        currentHealth = max(1, currentHealth)   // keep 1 HP
        FIRE OnKnockedOut(cause)
        if unconsciousDuration > 0:
            start revive timer

    FUNCTION Revive():
        if NOT IsUnconscious → return
        set state = Alive
        currentHealth = maxHealth * 0.25    // revive at quarter health
        FIRE OnRevived()

    FUNCTION Kill(cause):
        if IsDead → return
        currentHealth = 0
        stop revive timer
        set state = Dead
        FIRE OnDied(cause)

    FUNCTION Heal(amount):
        if NOT IsAlive → return
        currentHealth = min(currentHealth + amount, maxHealth)
        FIRE OnHealthChanged

    ON UPDATE:
        if reviveTimerActive:
            count down
            if reaches 0 → Revive()
```

## Build It

**Step 3.1** — Create `NPCHealth.cs`. Implement the pseudocode.

**Step 3.2** — Test with a temporary test component:

```csharp
// TestHealth.cs (temporary)
NPCHealth health;

void Start()
{
    health = GetComponent<NPCHealth>();
    // Manually create a definition for testing
    health.OnDamaged    += (dmg, src) => Debug.Log($"Ouch! -{dmg} HP");
    health.OnKnockedOut += (c)        => Debug.Log("Knocked out!");
    health.OnDied       += (c)        => Debug.Log("Dead!");
    health.OnRevived    += ()         => Debug.Log("Revived!");
}

void Update()
{
    if (Input.GetKeyDown(KeyCode.H)) health.TakeDamage(25f, null);
    if (Input.GetKeyDown(KeyCode.K)) health.Kill(EKnockdownCause.Melee);
    if (Input.GetKeyDown(KeyCode.R)) health.Revive();
}
```

**Checkpoint 3:** Press H five times → NPC either dies or gets knocked out based on `knockoutChance`. Knockout NPC auto-revives after `unconsciousDuration` seconds.

---

# PHASE 4 — NPCMovement

## The Problem

Every behaviour that needs to move an NPC (patrol, flee, walk to station) will need to start a walk, and know when it's done and whether it succeeded. If they each talk to `NavMeshAgent` directly, you'll have:

- `NavMeshAgent` calls scattered across 14+ files  
- No timeout protection (NPC walks forever if stuck)  
- No stuck detection  
- Can't swap pathfinding library later  

## Design Pattern: Adapter + Callback

`NPCMovement` adapts `NavMeshAgent` to a clean API. Every walk gets a single typed callback when it ends — success, fail, interrupted, or timeout.

```csharp
// What calling code looks like everywhere:
Movement.WalkTo(destination, OnArrived, run: true);

void OnArrived(EWalkResult result)
{
    switch(result)
    {
        case Success:     // do next thing
        case Failed:      // path not found
        case Interrupted: // someone called StopWalk()
        case Timeout:     // took too long
    }
}
```

## Pseudocode

```
CLASS NPCMovement : MonoBehaviour

    REQUIRES: NavMeshAgent, NPC

    FIELDS:
        agent          : NavMeshAgent
        walkTimeout    : float          // max seconds per walk
        arrivalThreshold: float
        onWalkComplete : WalkCallback   // delegate, called when walk ends
        hasActiveWalk  : bool
        walkTimer      : float
        stuckTimer     : float
        prevPosition   : Vector3

    FUNCTION Initialize(definition):
        agent.speed = definition.walkSpeed
        agent.stoppingDistance = definition.stoppingDistance
        agent.angularSpeed = definition.rotationSpeed * 100

    FUNCTION WalkTo(destination, callback, run, teleportOnFail):
        StopWalk(fireCallback: false)   // cancel any active walk first
        
        // Validate destination is on NavMesh
        if NOT NavMesh.SamplePosition(destination, out hit, 2f):
            if teleportOnFail → teleport; callback(Success)
            else → callback(Failed)
            return false
        
        store destination, callback
        reset walkTimer, stuckTimer, prevPosition
        hasActiveWalk = true
        SetSpeed(run ? RunSpeed : WalkSpeed)
        agent.SetDestination(hit.position)
        agent.isStopped = false
        return true

    ON UPDATE:
        if NOT hasActiveWalk → return
        
        walkTimer += deltaTime
        if walkTimer >= walkTimeout → FinishWalk(Timeout)
        
        // Arrival check
        if NOT agent.pathPending AND remainingDistance <= stoppingDistance:
            if NOT agent.hasPath OR velocity.sqrMagnitude < 0.01:
                FinishWalk(Success)
        
        // Stuck detection
        moved = distance(position, prevPosition) / deltaTime
        if moved < STUCK_THRESHOLD:
            stuckTimer += deltaTime
            if stuckTimer >= STUCK_TIMEOUT:
                if teleportOnFail → teleport; FinishWalk(Success)
                else → FinishWalk(Failed)
        else:
            stuckTimer = 0
        
        prevPosition = position

    FUNCTION StopWalk(fireCallback):
        if hasActiveWalk AND fireCallback → FinishWalk(Interrupted)
        hasActiveWalk = false
        agent.isStopped = true
        agent.ResetPath()

    FUNCTION FinishWalk(result):
        hasActiveWalk = false
        agent.isStopped = true
        save callback ref; clear field
        call saved callback(result)

    FUNCTION Teleport(position):
        StopWalk(false)
        agent.Warp(position)

    FUNCTION FaceDirection(worldPosition, speed):
        direction = (worldPosition - position).normalized
        direction.y = 0
        target = Quaternion.LookRotation(direction)
        rotation = Slerp(rotation, target, deltaTime * speed)

    FUNCTION EnableNavAgent(bool):
        agent.enabled = bool
```

## Build It

**Step 4.1** — Create `NPCMovement.cs` with `NavMeshAgent` field and the above pseudocode.

**Step 4.2** — Critical NavMeshAgent settings to set in `Initialize()`:
```csharp
agent.updateRotation = true;  // let NavMesh handle rotation
agent.updatePosition = true;  // let NavMesh handle position
agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
```

**Step 4.3** — Test timeout detection:

```csharp
// Put NPC on NavMesh, call WalkTo with unreachable position
movement.WalkTo(new Vector3(999, 0, 999), (result) =>
{
    Debug.Log($"Walk ended: {result}");  // should log Timeout after walkTimeout seconds
});
```

**Step 4.4** — NavMesh pitfalls to handle now:

```
ALWAYS check agent.isActiveAndEnabled before calling agent methods
ALWAYS check agent.isOnNavMesh before SetDestination
NavMesh.SamplePosition radius=2f handles destinations slightly off-mesh
```

**Checkpoint 4:** NPC walks to a clicked position, callback fires `Success`. Walking to an unreachable spot eventually fires `Timeout`. Calling `StopWalk()` fires `Interrupted`.

---

# PHASE 5 — NPCAwareness

## The Problem

NPCs need to notice the player without checking every frame for every NPC. Vision must respect walls. Alert level should escalate gradually and decay when threat is gone. Multiple systems react to awareness changes — but awareness shouldn't call them directly.

## Design Pattern: Tick-based Observer

Run awareness checks on a timer (every 0.25s), not every frame. This is 4× cheaper. Use events (Observer) so awareness never directly references behaviour or combat systems.

```
NOT this:
    OnSeePlayer() { combatBehaviour.Enable(); }  ← tight coupling

THIS:
    OnThreatSpotted fires → any subscriber reacts independently
```

## Pseudocode

```
CLASS NPCAwareness : MonoBehaviour

    EVENTS:
        OnAlertLevelChanged(old: EAlertLevel, new: EAlertLevel)
        OnThreatSpotted(threat: GameObject)
        OnThreatLost(threat: GameObject)
        OnSoundHeard(position: Vector3)

    FIELDS:
        alertLevel        : EAlertLevel   // Calm / Suspicious / Alert / Combat
        primaryThreat     : GameObject
        lastKnownThreatPos: Vector3
        hasLineOfSight    : bool
        suspicion         : float         // 0-1 meter
        tickTimer         : float
        lostSightTimer    : float

    CONSTANTS:
        TICK_RATE    = 0.25     // seconds between awareness checks
        LOST_SIGHT_GRACE = 3.0  // seconds before threat is "lost"

    ON UPDATE:
        tickTimer += deltaTime
        if tickTimer < TICK_RATE → return
        tickTimer = 0
        RunAwarenessTick()

    FUNCTION RunAwarenessTick():
        spotted = CheckForThreats()   // vision check
        
        if spotted:
            lostSightTimer = 0
            hasLineOfSight = true
            if alertLevel < Alert → SetAlertLevel(Alert)
        
        else if primaryThreat != null:
            lostSightTimer += TICK_RATE
            hasLineOfSight = false
            if lostSightTimer >= LOST_SIGHT_GRACE:
                LoseThreat()
        
        else:
            // No threat — calm down over time
            if alertLevel > Calm AND calmDownRate > 0:
                suspicion -= calmDownRate * TICK_RATE
                if suspicion <= 0 → SetAlertLevel(Calm)

    FUNCTION CheckForThreats():
        for each GameObject with tag "Player":
            if CanSee(player.transform):
                if primaryThreat != this player:
                    primaryThreat = player
                    FIRE OnThreatSpotted(player)
                lastKnownThreatPos = player.position
                return true
        return false

    FUNCTION CanSee(target):
        toTarget = target.position - position
        dist = toTarget.magnitude
        
        if dist > sightRange → return false
        angle = Vector3.Angle(transform.forward, toTarget.normalized)
        if angle > sightAngle/2 → return false
        
        // Raycast from eye position for occlusion
        eyePos    = position + Vector3.up * 1.6
        targetPos = target.position + Vector3.up * 1.0
        if Physics.Raycast(eyePos, toward targetPos, dist, occlusionMask):
            return false   // wall in the way
        
        return true

    FUNCTION CanHear(position, soundRadius):
        effectiveRange = min(soundRadius, hearingRange)
        return distance(position, self.position) <= effectiveRange

    FUNCTION HearSound(position, radius):
        if NOT CanHear(position, radius) → return
        FIRE OnSoundHeard(position)
        lastKnownThreatPos = position
        if alertLevel < Suspicious → SetAlertLevel(Suspicious)

    FUNCTION SetThreat(threat):  // called externally for instant awareness
        primaryThreat = threat
        lastKnownThreatPos = threat.position
        SetAlertLevel(Combat)
        FIRE OnThreatSpotted(threat)

    FUNCTION LoseThreat():
        save old threat; primaryThreat = null
        hasLineOfSight = false
        lostSightTimer = 0
        FIRE OnThreatLost(old threat)
        if alertLevel >= Alert → SetAlertLevel(Suspicious)

    FUNCTION SetAlertLevel(new):
        if new == alertLevel → return
        old = alertLevel
        alertLevel = new
        FIRE OnAlertLevelChanged(old, new)

    FUNCTION Calm():
        LoseThreat()
        SetAlertLevel(Calm)
        suspicion = 0
```

## Build It

**Step 5.1** — Create `NPCAwareness.cs`.

**Step 5.2** — Set up sight gizmos for debugging. In `OnDrawGizmosSelected()`:

```
Draw yellow wire sphere = sight range
Draw cyan wire sphere = hearing range  
Draw two rays from forward at ±sightAngle/2 = sight cone
```

This lets you visually verify the cone is correct in the Scene view.

**Step 5.3** — Tag your player GameObject with the tag `Player` (Edit → Tags & Layers).

**Step 5.4** — Set `sightOcclusionMask` to your wall/building layers. **Exclude** the Player and NPC layers so they don't block each other's vision rays.

**Step 5.5** — Test:

```csharp
// Temporary test
awareness.OnThreatSpotted     += (t) => Debug.Log("SPOTTED: " + t.name);
awareness.OnThreatLost        += (t) => Debug.Log("LOST: " + t.name);
awareness.OnAlertLevelChanged += (o, n) => Debug.Log($"Alert: {o}→{n}");
```

Walk player into NPC sight cone → `SPOTTED`. Walk behind wall → after 3s → `LOST`.

**Checkpoint 5:** Awareness ticks every 0.25s. Vision cone works. Wall occlusion works. Alert level escalates and decays. Events fire correctly.

---

# PHASE 6 — NPCAnimation

## The Problem

Mixamo gives you animation clips. Unity's Animator Controller wires them with states and transitions. But calling `animator.SetFloat("Speed", 1f)` as a string everywhere is:

- Error-prone (typos silently fail)
- Slow (string hash lookup every call)
- Scattered (animation intent spread across all behaviours)

## Design Pattern: Facade + Pre-Hashed Parameters

Create a **Facade** — one class that is the single point of contact for all animation. Pre-hash all parameter names once at class load time using `Animator.StringToHash()`.

```csharp
// One time at class level (not per-frame):
private static readonly int HASH_SPEED = Animator.StringToHash("Speed");

// Then at runtime (fast int lookup):
animator.SetFloat(HASH_SPEED, 1.5f);
```

## Animator Controller Setup (Do This in Unity First)

**Step 6.1 — Create your Animator Controller:**

1. Right-click Project → Create → Animator Controller → `NPCAnimator`
2. Assign it to your character's Animator component
3. Open the Animator window (Window → Animation → Animator)

**Step 6.2 — Create parameters:**

In the Animator window, Parameters tab → add these:

| Name | Type | Used For |
|---|---|---|
| `Speed` | Float | 0=idle, 1=walk, 2=run |
| `IsRunning` | Bool | transition walk→run |
| `IsCrouching` | Bool | crouch state |
| `IsDead` | Bool | death state entry |
| `IsUnconscious` | Bool | KO state entry |
| `AlertLevel` | Int | 0=calm, 3=combat idle |
| `Hit` | Trigger | small hit react |
| `HeavyHit` | Trigger | large hit react |
| `Attack` | Trigger | melee swing |
| `Scared` | Trigger | panic react |
| `Talk` | Trigger | talking/phone |
| `IdleVariant` | Int | random idle variety |

**Step 6.3 — Wire Mixamo clips to states:**

```
Blend Tree (Locomotion):
    Speed 0.0 → Idle
    Speed 1.0 → Walk
    Speed 2.0 → Run

Separate states:
    Dead         (IsDead = true → transition in)
    Unconscious  (IsUnconscious = true → transition in)
    Hit          (Hit trigger → play → exit)
    HeavyHit     (HeavyHit trigger → play → exit)
    Attack       (Attack trigger → play → exit)
    Talk         (Talk trigger → looping talk anim)
```

**Step 6.4 — Mixamo-specific setup:**

Mixamo clips come as Humanoid. For each clip in the Inspector:
- Rig → Animation Type: **Humanoid** (should already be set)
- Animation tab → **Loop Time**: check for walk/run/idle, uncheck for one-shots
- Root Motion: disable on all clips (let NavMeshAgent drive position)

## Pseudocode

```
CLASS NPCAnimation : MonoBehaviour

    REQUIRES: Animator, NPC

    STATIC FIELDS (hashes — computed once):
        HASH_SPEED, HASH_IS_RUNNING, HASH_IS_CROUCHING
        HASH_IS_DEAD, HASH_IS_UNCONSCIOUS, HASH_ALERT_LEVEL
        HASH_HIT, HASH_HEAVY_HIT, HASH_ATTACK
        HASH_SCARED, HASH_TALK, HASH_IDLE_VARIANT

    FIELDS:
        animator        : Animator
        movement        : NPCMovement
        health          : NPCHealth
        ragdollBodies   : Rigidbody[]
        ragdollColliders: Collider[]
        ragdollActive   : bool

    ON START:
        subscribe: health.OnDamaged    → HandleDamaged
        subscribe: health.OnKnockedOut → HandleKnockedOut
        subscribe: health.OnRevived    → HandleRevived
        subscribe: health.OnDied       → HandleDied
        SetRagdollActive(false)   // kinematic by default

    ON DESTROY:
        unsubscribe all

    ON UPDATE:
        if ragdollActive → return
        speed = 0
        if movement.IsMoving:
            speed = movement.LocomotionState == Running ? 2f : 1f
        animator.SetFloat(HASH_SPEED, speed, smoothing=0.1, deltaTime)
        animator.SetBool(HASH_IS_RUNNING, locomotion == Running)

    // Public setters
    FUNCTION SetAlertLevel(level) → animator.SetInteger(HASH_ALERT_LEVEL, level)
    FUNCTION SetCrouching(bool)   → animator.SetBool(HASH_IS_CROUCHING, bool)
    FUNCTION TriggerHit()         → animator.SetTrigger(HASH_HIT)
    FUNCTION TriggerHeavyHit()    → animator.SetTrigger(HASH_HEAVY_HIT)
    FUNCTION TriggerAttack()      → animator.SetTrigger(HASH_ATTACK)
    FUNCTION TriggerScared()      → animator.SetTrigger(HASH_SCARED)
    FUNCTION TriggerTalk()        → animator.SetTrigger(HASH_TALK)
    FUNCTION SetIdleVariant(n)    → animator.SetInteger(HASH_IDLE_VARIANT, n)

    // Ragdoll
    FUNCTION SetRagdollActive(active):
        ragdollActive = active
        animator.enabled = NOT active
        for each rb in ragdollBodies:
            rb.isKinematic = NOT active
        for each col in ragdollColliders:
            col.enabled = active

    // Health handlers
    FUNCTION HandleDamaged(amount, source):
        if amount > 30 → TriggerHeavyHit()
        else           → TriggerHit()

    FUNCTION HandleKnockedOut(cause):
        animator.SetBool(IS_UNCONSCIOUS, true)
        if cause == Explosion or Fall → SetRagdollActive(true)

    FUNCTION HandleRevived():
        SetRagdollActive(false)
        animator.SetBool(IS_UNCONSCIOUS, false)

    FUNCTION HandleDied(cause):
        animator.SetBool(IS_DEAD, true)
        if cause == Explosion or Gunshot → SetRagdollActive(true)
```

## Ragdoll Setup in Unity

1. Character needs `Rigidbody` + `Collider` on each major bone (hips, spine, chest, head, upper/lower arms, upper/lower legs)
2. Use Unity's built-in wizard: GameObject menu → 3D Object → Ragdoll → drag your bones
3. Add the generated Rigidbodies to `NPCAnimation.ragdollBodies[]` in Inspector
4. Add the generated Colliders to `NPCAnimation.ragdollColliders[]` in Inspector

**Checkpoint 6:** Walk character → Speed parameter animates. Deal damage → hit anim plays. Kill → IsDead triggers. Ragdoll activates on explosion kill.

---

# PHASE 7 — Behaviour Base Class

## The Problem

You have 14 different behaviours. All of them share the same lifecycle: they can be enabled/disabled (participates in priority), activated/deactivated (currently running), paused/resumed (e.g. for cutscenes). If each implements this lifecycle independently, you get 14 inconsistent implementations.

## Design Pattern: Template Method

Define the lifecycle **skeleton** in the abstract base class. Subclasses only override the hooks they need.

```
Base:                           Subclass:
Enable()        ← calls →      OnEnabled()   ← override this
Activate()      ← calls →      OnActivated() ← override this
BehaviourUpdate()              BehaviourUpdate() ← override this
Deactivate()    ← calls →      OnDeactivated() ← override this
Disable()       ← calls →      OnDisabled()  ← override this
```

The base manages the **state** (`Enabled`, `Active`, `Paused`). The subclass provides the **content**.

## Pseudocode

```
ABSTRACT CLASS Behaviour : MonoBehaviour

    INSPECTOR:
        behaviourName  : string
        priority       : int
        enabledOnAwake : bool

    PROPERTIES:
        Enabled : bool
        Active  : bool
        Paused  : bool
        Npc     : NPC

    PROTECTED REFERENCES (via Npc):
        Movement  → Npc.Movement
        Health    → Npc.Health
        Awareness → Npc.Awareness
        Animation → Npc.Animation
        BehaviourManager → Npc.BehaviourManager

    SHARED STATE:
        consecutivePathingFailures : int
        MAX_PATHING_FAILURES = 5

    ON AWAKE:
        Npc = GetComponent<NPC>()
        if enabledOnAwake → Enabled = true

    FUNCTION Initialize(npc):
        Npc = npc

    // ── Enable / Disable ──────────────────────────────────
    FUNCTION Enable():
        if already Enabled → return
        Enabled = true
        call OnEnabled()

    FUNCTION Disable():
        if NOT Enabled → return
        if Active → Deactivate()
        Enabled = false
        call OnDisabled()

    VIRTUAL FUNCTION OnEnabled()  { }    // override to register listeners
    VIRTUAL FUNCTION OnDisabled() { }    // override to unregister listeners

    // ── Activate / Deactivate ─────────────────────────────
    FUNCTION Activate():
        if already Active → return
        Active = true
        Paused = false
        consecutivePathingFailures = 0
        call OnActivated()

    FUNCTION Deactivate():
        if NOT Active → return
        Active = false
        Paused = false
        call OnDeactivated()
        Movement.StopWalk(fireCallback=false)

    VIRTUAL FUNCTION OnActivated()   { }   // override: start walk, play anim
    VIRTUAL FUNCTION OnDeactivated() { }   // override: cleanup, stop sounds

    // ── Pause / Resume ────────────────────────────────────
    FUNCTION Pause():
        if NOT Active OR already Paused → return
        Paused = true
        call OnPaused()

    FUNCTION Resume():
        if NOT Active OR NOT Paused → return
        Paused = false
        call OnResumed()

    VIRTUAL FUNCTION OnPaused()  { }
    VIRTUAL FUNCTION OnResumed() { }

    // ── Per-frame (only while Active AND NOT Paused) ──────
    VIRTUAL FUNCTION BehaviourUpdate()     { }   // every frame
    VIRTUAL FUNCTION BehaviourLateUpdate() { }   // LateUpdate
    VIRTUAL FUNCTION OnActiveTick()        { }   // every 0.5s

    // ── Priority hook ─────────────────────────────────────
    VIRTUAL FUNCTION WantsToBeActive():
        return Enabled   // default: always wants to run when enabled

    // ── Pathing ───────────────────────────────────────────
    VIRTUAL FUNCTION WalkCallback(result):
        if result == Failed or Timeout:
            consecutivePathingFailures++
            if >= MAX → OnPathingFailed()
        else:
            consecutivePathingFailures = 0

    VIRTUAL FUNCTION OnPathingFailed():
        log warning
```

## Build It

**Step 7.1** — Create abstract `Behaviour.cs`. Implement exactly the pseudocode above.

**Step 7.2** — Write one simple concrete test to verify the lifecycle works:

```csharp
// TestBehaviour.cs
public class TestBehaviour : Behaviour
{
    protected override void Awake()
    {
        base.Awake();
        BehaviourName = "Test";
        Priority = 999;
    }

    protected override void OnActivated()
        => Debug.Log("TestBehaviour ACTIVATED");

    protected override void OnDeactivated()
        => Debug.Log("TestBehaviour DEACTIVATED");

    public override void BehaviourUpdate()
        => Debug.Log("TestBehaviour UPDATE");
}
```

Add to a GameObject. Call `Enable()` then `Activate()` manually from a test script. Verify logs appear.

**Checkpoint 7:** The lifecycle works. Calling `Enable()` → `Activate()` → `Deactivate()` → `Disable()` fires the correct virtual hooks in order.

---

# PHASE 8 — NPCBehaviour Stack (Priority Manager)

## The Problem

At any moment, many behaviours are enabled simultaneously. Dead behaviour should always win over combat, which should always win over wandering. But the winning one should change dynamically as conditions change — without a hand-written state transition table.

## Design Pattern: Priority-Sorted Strategy Stack

```
Sorted list (highest first): Dead(10000) > Ragdoll(9000) > ... > Idle(100)

Every 0.5s:
    Walk the list from top
    First Enabled behaviour where WantsToBeActive() == true → wins
    Activate that one, deactivate all others
```

This is a **Strategy** pattern where the active strategy is selected dynamically by priority, not hardcoded transitions. Adding a new behaviour type = add one entry to the list. No `switch` statements.

## Pseudocode

```
CLASS NPCBehaviour : MonoBehaviour

    INSPECTOR:
        idleBehaviour      : IdleBehaviour
        wanderBehaviour    : WanderBehaviour
        [... all 14 behaviour refs]

    FIELDS:
        activeBehaviour : Behaviour
        behaviourStack  : List<Behaviour>   // sorted highest priority first
        tickTimer       : float
        TICK_RATE = 0.5

    FUNCTION Initialize(npc):
        // Collect ALL Behaviour components on this GameObject + children
        behaviourStack = GetComponentsInChildren<Behaviour>(includeInactive=true)
        
        // Initialize each with NPC reference
        for each b in behaviourStack:
            b.Initialize(npc)
        
        // Sort descending by priority
        Sort(behaviourStack, by b.Priority descending)
        
        // Wire health events
        health.OnKnockedOut += OnKnockedOut
        health.OnRevived    += OnRevived
        health.OnDied       += OnDied
        
        // Wire awareness events
        awareness.OnAlertLevelChanged += OnAlertLevelChanged

        // Start with idle
        idleBehaviour.Enable()
        EvaluateStack()

    ON UPDATE:
        if activeBehaviour != null AND Active AND NOT Paused:
            activeBehaviour.BehaviourUpdate()
        
        tickTimer += deltaTime
        if tickTimer >= TICK_RATE:
            tickTimer = 0
            EvaluateStack()
            if activeBehaviour.Active → activeBehaviour.OnActiveTick()

    ON LATE UPDATE:
        if activeBehaviour != null AND Active AND NOT Paused:
            activeBehaviour.BehaviourLateUpdate()

    FUNCTION EvaluateStack():
        wanted = null
        for each behaviour in behaviourStack (highest priority first):
            if b.Enabled AND b.WantsToBeActive():
                wanted = b
                break
        
        if wanted == activeBehaviour → return   // no change needed
        
        if activeBehaviour != null AND Active:
            activeBehaviour.Deactivate()
        
        activeBehaviour = wanted
        if activeBehaviour != null AND NOT Active:
            activeBehaviour.Activate()

    // Health event responses
    FUNCTION OnKnockedOut(cause):
        combatBehaviour.Disable()
        fleeBehaviour.Disable()
        unconsciousBehaviour.Enable()
        EvaluateStack()

    FUNCTION OnRevived():
        unconsciousBehaviour.Disable()
        idleBehaviour.Enable()
        EvaluateStack()

    FUNCTION OnDied(cause):
        disable ALL behaviours except DeadBehaviour
        deadBehaviour.Enable()
        EvaluateStack()

    // Awareness event responses
    FUNCTION OnAlertLevelChanged(old, new):
        SWITCH new:
            Calm:
                combatBehaviour.Disable()
                fleeBehaviour.Disable()
                callPoliceBehaviour.Disable()
                coweringBehaviour.Disable()
                idleBehaviour.Enable()
            
            Alert:
                if definition.FleesThreat    → fleeBehaviour.Enable()
                if definition.CallsPolice    → callPoliceBehaviour.Enable()
                else                         → coweringBehaviour.Enable()
            
            Combat:
                if definition.CanFight → combatBehaviour.Enable()
                else if definition.FleesThreat → fleeBehaviour.Enable()
        
        EvaluateStack()

    // Public utilities
    FUNCTION GetBehaviour<T>() → search stack for T
    FUNCTION EnableBehaviour<T>()  → find T, Enable()
    FUNCTION DisableBehaviour<T>() → find T, Disable()
    FUNCTION ForceActivate(b)  → bypass priority, directly activate b
    FUNCTION PauseActive()     → activeBehaviour.Pause()
    FUNCTION ResumeActive()    → activeBehaviour.Resume()
```

## Build It

**Step 8.1** — Create `NPCBehaviour.cs`. Implement the pseudocode.

**Step 8.2** — Critical `EvaluateStack` detail — NPC should subscribe to health/awareness events in `Initialize()` NOT in `Awake()`. Why? `NPC.Initialize()` may be called after `Awake()` — the events may not be ready yet.

**Step 8.3** — Test with just IdleBehaviour and one other:

```
behaviourStack = [Combat(7000), Idle(100)]
Both Enabled, neither WantsToBeActive returns false

Expected: Idle is active (highest that wants to run)
Enable Combat → EvaluateStack → Combat becomes active
Disable Combat → EvaluateStack → Idle becomes active again
```

**Checkpoint 8:** Two behaviours. Priority stack correctly picks the higher one. When the higher one is disabled, the lower one activates automatically. No manual transition code needed.

---

# PHASE 9 — The 14 Concrete Behaviours

## The Problem

Each behaviour is a self-contained unit of NPC logic. The approach is the same for all of them: override the lifecycle hooks from `Behaviour`.

## Pattern Per Behaviour

| Behaviour | Key Override | Core Challenge |
|---|---|---|
| `IdleBehaviour` | `OnActivated`, `BehaviourUpdate` | Random idle variant cycling |
| `WanderBehaviour` | `OnActivated`, `BehaviourUpdate` | Random NavMesh point sampling |
| `PatrolBehaviour` | `OnActivated`, `BehaviourUpdate` | Waypoint sequence + wait time |
| `StationaryBehaviour` | `OnActivated` | Walk to point, then stand |
| `CoweringBehaviour` | `OnActivated`, `BehaviourUpdate`, `OnActiveTick` | Back-away from threat |
| `FleeBehaviour` | `OnActivated`, `BehaviourUpdate` | Away-direction point sampling |
| `CombatBehaviour` | `BehaviourUpdate` | Chase + attack in range |
| `RagdollBehaviour` | `OnActivated`, `OnDeactivated` | Toggle ragdoll + disable agent |
| `DeadBehaviour` | `OnActivated` | Disable agent, fire event, NEVER deactivate |
| `UnconsciousBehaviour` | `OnActivated`, `OnDeactivated` | Disable/re-enable agent |
| `HeavyFlinchBehaviour` | `OnActivated`, `BehaviourUpdate` | Timer → auto-disable |
| `FaceTargetBehaviour` | `BehaviourUpdate` | Slerp rotation toward target |
| `CallPoliceBehaviour` | `OnActivated`, `BehaviourUpdate` | Timer → disable with cooldown |
| `GenericDialogueBehaviour` | `OnActivated`, `BehaviourUpdate` | External trigger, timed end |

## Build Order (do these in this order — each is a stepping stone)

### Step 9.1 — IdleBehaviour (simplest possible)

```
PROBLEM: Default state when nothing else runs.

PSEUDOCODE:
    Awake: priority = 100, enabledOnAwake = true
    
    OnActivated:
        Movement.StopWalk()
        Animation.SetIdleVariant(random)
        variantTimer = 0

    BehaviourUpdate:
        variantTimer += deltaTime
        if variantTimer > variantChangeTime:
            variantTimer = 0
            Animation.SetIdleVariant(random)
```

### Step 9.2 — WanderBehaviour

```
PROBLEM: NPC needs to feel alive by moving around.

KEY CHALLENGE: How do you find a random valid NavMesh point?

FUNCTION GetRandomNavMeshPoint(origin, radius):
    TRY 10 TIMES:
        randomPoint = origin + Random.insideUnitSphere * radius
        randomPoint.y = origin.y          // stay on flat plane first
        if NavMesh.SamplePosition(randomPoint, out hit, 2f):
            return hit.position
    return origin   // fallback: stay put

PSEUDOCODE:
    Awake: priority = 200
    OnActivated: state = Waiting, wait timer starts
    
    BehaviourUpdate:
        if Waiting:
            waitTimer += deltaTime
            if waitTimer > waitDuration → StartWalk()
    
    StartWalk:
        dest = GetRandomNavMeshPoint(homePosition, wanderRadius)
        Movement.WalkTo(dest, OnWalkFinished, run=false)
        state = Walking
    
    OnWalkFinished(result):
        WalkCallback(result)       // handles pathing failure count
        BeginWait()                // always wait after walk, even if failed
```

### Step 9.3 — PatrolBehaviour

```
PROBLEM: NPC needs to walk a fixed route.

PATTERN: State machine internal to the behaviour (Walking / Waiting).

PSEUDOCODE:
    Awake: priority = 300
    Initialize: patrolRoute.Resolve()  // find GameObjects in scene

    OnActivated:
        if no waypoints → Disable(); return
        state = Waiting
        waitTimer = 0

    BehaviourUpdate:
        if Waiting:
            waitTimer += deltaTime
            if waitTimer >= route.waypointWaitTime → WalkToNext()
    
    WalkToNext:
        wp = resolvedWaypoints[currentIndex]
        state = Walking
        Movement.WalkTo(wp.position, OnWaypointReached, run=route.runBetweenWaypoints)
    
    OnWaypointReached(result):
        WalkCallback(result)
        currentIndex = route.GetNextIndex(currentIndex, ref direction)
        state = Waiting; waitTimer = 0
    
    OnPathingFailed:
        // Skip stuck waypoint, try next
        currentIndex = route.GetNextIndex(currentIndex, ref direction)
        ConsecutivePathingFailures = 0
```

### Step 9.4 — FleeBehaviour

```
PROBLEM: NPC needs to run AWAY from threat toward safety.
Not just "random direction" — biased away from threat.

KEY ALGORITHM: Score candidate points by distance from threat.

FUNCTION FindFleePoint():
    threatPos = awareness.PrimaryThreat.position
    awayDir = (position - threatPos).normalized
    
    bestPoint = position
    bestScore = 0
    
    REPEAT 8 TIMES:
        // Candidates biased in away direction with slight randomness
        candidate = position + (awayDir + Random.insideUnitSphere*0.4).normalized * scanRadius
        candidate.y = position.y
        
        if NavMesh.SamplePosition(candidate, out hit, 2.5f):
            score = distance(hit.position, threatPos)  // farther = better
            if score > bestScore:
                bestScore = bestPoint.y    // bug note: should be score
                bestPoint = hit.position
    
    return bestPoint

PSEUDOCODE:
    Awake: priority = 6000
    
    WantsToBeActive: Enabled AND awareness.PrimaryThreat != null
    
    BehaviourUpdate:
        if no threat → Disable(); return
        
        dist = distance to threat
        if dist >= safeDistance:
            Disable()
            awareness.Calm()
            return
        
        recalcTimer += deltaTime
        if recalcTimer >= recalcInterval:
            recalcTimer = 0
            Movement.WalkTo(FindFleePoint(), null, run=true)
```

### Step 9.5 — CombatBehaviour

```
PROBLEM: Chase target, stop when in attack range, attack on cooldown.

PSEUDOCODE:
    Awake: priority = 7000
    
    WantsToBeActive: Enabled AND awareness.PrimaryThreat != null
    
    BehaviourUpdate:
        if no threat → Disable(); return
        
        dist = distance to threat
        Movement.FaceDirection(threat.position)
        
        if dist <= attackRange:
            Movement.StopWalk()
            attackTimer += deltaTime
            if attackTimer >= attackCooldown:
                attackTimer = 0
                DoAttack()
        else:
            // Chase — recalculate destination periodically
            chaseTimer += deltaTime
            if chaseTimer >= chaseInterval:
                chaseTimer = 0
                Movement.WalkTo(threat.position, null, run=true)
    
    DoAttack:
        Animation.TriggerAttack()
        targetHealth = threat.GetComponent<NPCHealth>()
        if targetHealth → targetHealth.TakeDamage(attackDamage, gameObject)
```

### Step 9.6 — DeadBehaviour (special case)

```
PROBLEM: Death is a terminal state. NPC should NEVER exit it.

PATTERN: Override Deactivate() and Disable() to do nothing.

PSEUDOCODE:
    Awake: priority = 10000
    
    OnActivated:
        Movement.StopWalk(false)
        Movement.EnableNavAgent(false)
        FIRE OnNPCDead event

    // OVERRIDE — terminal state, cannot exit:
    FUNCTION Deactivate() { }   // do nothing
    FUNCTION Disable()    { }   // do nothing
```

### Step 9.7 — HeavyFlinchBehaviour

```
PROBLEM: Big hits should briefly interrupt any behaviour.
Disable itself after a short time automatically.

PSEUDOCODE:
    Awake: priority = 8000, enabledOnAwake = false
    
    PUBLIC FUNCTION TriggerFlinch():
        Enable()
        if NOT Active → Activate()   // force activate via behaviour manager
    
    OnActivated:
        flinchTimer = 0
        Animation.TriggerHeavyHit()
        Movement.StopWalk(false)
    
    BehaviourUpdate:
        flinchTimer += deltaTime
        if flinchTimer >= flinchDuration:
            Disable()   // self-disables → stack re-evaluates
```

### Step 9.8 — CallPoliceBehaviour

```
PROBLEM: Witness crime → stand still, "call police", then cool down before can call again.
Cannot fire repeatedly.

PSEUDOCODE:
    Awake: priority = 5000
    
    FIELDS:
        onCooldown    : bool
        cooldownTimer : float
    
    WantsToBeActive: Enabled AND NOT onCooldown
    
    OnActivated:
        callTimer = 0
        Movement.StopWalk(false)
        Animation.TriggerTalk()
    
    BehaviourUpdate:
        callTimer += deltaTime
        if callTimer >= callDuration:
            onCooldown = true
            cooldownTimer = 0
            Disable()
            awareness.Calm()
    
    // Note: cooldown is handled in a plain Update (not BehaviourUpdate)
    // because this runs even when behaviour is disabled
    UPDATE (not BehaviourUpdate):
        if onCooldown:
            cooldownTimer += deltaTime
            if cooldownTimer >= cooldownDuration:
                onCooldown = false
```

**Checkpoint 9:** Create a test NPC with all 14 behaviours. Verify:
- Walk player into sight → NPC goes Idle→Combat (if CanFight) or Idle→Flee
- Deal damage → flinch briefly interrupts
- Kill NPC → DeadBehaviour activates and cannot be reversed
- Remove threat → NPC calms down, returns to Idle/Wander

---

# PHASE 10 — NPCAction Base Class

## The Problem

An NPC has a daily routine: sleep 8 hours, patrol 4 hours, idle at noon, work till 10 PM. These are time-window tasks that need the same lifecycle: start at a given minute, run until end time, support interruption, support being skipped if the start time is passed.

## Design Pattern: Template Method (same as Behaviour, different context)

```
ABSTRACT:  GetName(), GetTimeDescription(), GetEndTime()  ← must implement
VIRTUAL:   Started(), LateStarted(), MinPassed(), End(),
           Interrupt(), ShouldStart(), JumpTo()           ← override as needed
```

## Pseudocode

```
ABSTRACT CLASS NPCAction : MonoBehaviour

    INSPECTOR:
        startTime : int   // 0-1439 minutes
        priority  : int

    EVENT:
        OnEnded : Action

    STATE:
        actionState : EActionState   // Pending/Active/Completed/Interrupted/Skipped

    PROPERTIES:
        IsActive  → actionState == Active
        IsSignal  → this instanceof NPCSignal

    REFERENCES:
        Npc       : NPC          (set by NPCScheduleManager)
        Movement  → Npc.Movement
        Awareness → Npc.Awareness
        BehaviourManager → Npc.BehaviourManager

    FUNCTION SetNPC(npc) → Npc = npc

    // Must implement:
    ABSTRACT GetName()            → string
    ABSTRACT GetTimeDescription() → string
    ABSTRACT GetEndTime()         → int   // -1 = open-ended

    // Override as needed:
    VIRTUAL ShouldStart()  → true
    VIRTUAL Started()      → actionState = Active
    VIRTUAL LateStarted()  → { }
    VIRTUAL ActiveUpdate() → { }
    VIRTUAL MinPassed()    → { }
    VIRTUAL End():
        actionState = Completed
        Movement.StopWalk(false)
        FIRE OnEnded
    VIRTUAL Interrupt():
        actionState = Interrupted
        Movement.StopWalk(false)
        FIRE OnEnded
    VIRTUAL Skipped():
        actionState = Skipped
        FIRE OnEnded
    VIRTUAL JumpTo() → Started()    // resume mid-task on load
    VIRTUAL ResumeFailed() → Interrupt()

    PROTECTED WalkCallback(result):
        if Failed or Timeout:
            consecutivePathingFailures++
            if >= MAX → OnPathingFailed()
        else:
            consecutivePathingFailures = 0
    
    VIRTUAL OnPathingFailed():
        log warning
        Interrupt()
```

## Build It

**Step 10.1** — Create `NPCAction.cs`.

**Step 10.2** — Write one concrete action to verify the abstract class works:

```csharp
// ScheduledAction_Idle — simplest possible scheduled action
public class ScheduledAction_Idle : NPCAction
{
    public int DurationMinutes = 30;

    public override string GetName()            => "Idle";
    public override string GetTimeDescription() => $"Idle for {DurationMinutes} min";
    public override int    GetEndTime()         => StartTime + DurationMinutes;

    public override void Started()
    {
        base.Started();
        Movement?.StopWalk(false);
    }
    // End() handled automatically by NPCScheduleManager checking GetEndTime()
}
```

**Checkpoint 10:** `ScheduledAction_Idle` compiles. `Started()` base sets `ActionState = Active`. `GetEndTime()` returns correct end time.

---

# PHASE 11 — NPCScheduleManager

## The Problem

The action base class defines what an action is. Something needs to:
- Hold the list of all actions
- Start the right one at the right time
- End the current one at its end time
- Handle day rollover (reset all actions)
- Handle loading mid-day (jump to whatever should be running)

## Design Pattern: Queue driven by the Observer clock

Subscribe to `GameClock.OnMinutePassed`. Each minute, check if the next action should start. This is **event-driven** — the schedule doesn't poll; it reacts to the clock.

## Pseudocode

```
CLASS NPCScheduleManager : MonoBehaviour

    EVENTS:
        OnActionStarted(action)
        OnActionEnded(action)

    STATE:
        currentAction      : NPCAction
        activeSignal       : NPCSignal
        scheduledActions   : List<NPCAction>   // sorted by StartTime
        signals            : List<NPCSignal>   // sorted by Priority
        currentActionIndex : int
        initialized        : bool

    FUNCTION Initialize(npc):
        // Collect all NPCAction children
        all = GetComponentsInChildren<NPCAction>(includeInactive=true)
        for each a in all:
            a.SetNPC(npc)
            if a is NPCSignal → signals.Add(a)
            else              → scheduledActions.Add(a)
        
        SortByTime(scheduledActions)
        SortByPriority(signals)

        // Subscribe to clock
        GameClock.OnMinutePassed += OnMinutePassed
        GameClock.OnDayStarted   += OnDayStarted

        initialized = true
        JumpToCurrentTime(GameClock.currentMinute)

    ON DESTROY:
        unsubscribe from GameClock

    ON UPDATE:
        if active signal → signal.ActiveUpdate()
        else if current action → action.ActiveUpdate()

    // Every in-game minute:
    FUNCTION OnMinutePassed(minute):
        // Tick
        if activeSignal → activeSignal.MinPassed()
        else → currentAction?.MinPassed()
        
        // Check end time of current action
        if currentAction AND currentAction.IsActive:
            if currentAction.GetEndTime() > 0 AND minute >= GetEndTime():
                EndCurrentAction()
        
        // Check if next action should start
        TryStartNextAction(minute)

    FUNCTION OnDayStarted(day):
        reset all signal.StartedThisCycle
        if currentAction AND Active → EndCurrentAction()
        currentActionIndex = -1
        JumpToCurrentTime(0)

    FUNCTION TryStartNextAction(minute):
        nextIndex = currentActionIndex + 1
        if nextIndex >= scheduledActions.Count → return
        
        next = scheduledActions[nextIndex]
        if minute < next.StartTime → return   // not time yet
        if NOT next.ShouldStart() → return    // condition not met
        
        StartAction(next, nextIndex)

    FUNCTION StartAction(action, index):
        EndCurrentAction()                // end previous
        currentActionIndex = index
        currentAction = action
        action.Started()
        action.LateStarted()
        FIRE OnActionStarted(action)

    FUNCTION EndCurrentAction():
        if no current action → return
        a = currentAction
        if a.IsActive → a.End()
        currentAction = null
        FIRE OnActionEnded(a)

    // SIGNAL INJECTION:
    FUNCTION IssueSignal(signal):
        if signal.StartedThisCycle → return   // already ran today
        
        // Check if signal has higher priority
        if activeSignal AND signal.Priority <= activeSignal.Priority → return
        if currentAction AND signal.Priority <= currentAction.Priority → return
        
        // Interrupt what's running
        if activeSignal → activeSignal.Interrupt()
        else if currentAction.IsActive → currentAction.Interrupt()
        
        activeSignal = signal
        signal.Started()
        signal.LateStarted()
        signal.OnEnded += OnSignalEnded
        FIRE OnActionStarted(signal)

    FUNCTION OnSignalEnded():
        activeSignal.OnEnded -= OnSignalEnded
        FIRE OnActionEnded(activeSignal)
        activeSignal = null
        
        // Resume interrupted scheduled action if one exists
        if currentAction AND NOT currentAction.IsActive:
            currentAction.JumpTo()

    // On scene load / mid-day:
    FUNCTION JumpToCurrentTime(minute):
        bestIndex = -1
        for i = 0 to scheduledActions.Count:
            if scheduledActions[i].StartTime <= minute:
                bestIndex = i
        
        if bestIndex < 0 → return
        
        action = scheduledActions[bestIndex]
        if NOT action.ShouldStart() → return
        
        currentActionIndex = bestIndex
        currentAction = action
        action.JumpTo()
        FIRE OnActionStarted(action)
```

## Build It

**Step 11.1** — Create `NPCScheduleManager.cs`.

**Step 11.2** — Test the schedule without signals first:

```
NPC with 3 actions:
    [0]   ScheduledAction_Idle, StartTime=0,   Duration=60
    [1]   ScheduledAction_Idle, StartTime=60,  Duration=60
    [2]   ScheduledAction_Idle, StartTime=120, Duration=60

Set clock to fast mode (0.1s/minute).
Verify OnActionStarted fires 3 times at minutes 0, 60, 120.
```

**Step 11.3** — Test JumpToCurrentTime:

```
Set clock to minute 90. Initialize NPC.
Expected: Action[1] (StartTime=60) starts immediately via JumpTo.
```

**Checkpoint 11:** NPC transitions through scheduled actions in time order. Actions end at their end time. `JumpToCurrentTime` activates the correct mid-day action on scene load.

---

# PHASE 12 — NPCSignal (Command Pattern)

## The Problem

External systems need to immediately override what an NPC is doing: "summon this NPC to the door now", "make this NPC walk to the player right now". These aren't time-window actions — they are instant commands that need to interrupt the current schedule.

## Design Pattern: Command

Each signal is a **Command** object that encapsulates:
- What the NPC should do (the execute logic)
- A completion callback (`OnEnded`)
- A priority (signals can interrupt each other too)

```
Caller:  npc.IssueSignal(walkSignal)
Signal:  walks NPC to target → fires OnEnded when done
Manager: resumes interrupted schedule action
```

## Pseudocode

```
CLASS NPCSignal : NPCAction

    ADDED FIELDS:
        maxDuration : int           // max minutes before timeout. 0 = no limit
        startedThisCycle : bool

    OVERRIDES:
        GetName()    → "Signal"
        GetEndTime() → -1          // signals manage their own end

    OVERRIDE Started():
        base.Started()
        startedThisCycle = true

    OVERRIDE MinPassed():
        if maxDuration > 0:
            count elapsed minutes
            if elapsed >= maxDuration → End()

    FUNCTION ResetCycle() → startedThisCycle = false   // called on day rollover
```

**Concrete signals to build:**

### NPCSignal_WalkToLocation

```
FIELDS: targetTransform, targetPosition, useTransform, run, teleportOnFail

Started():
    dest = useTransform ? targetTransform.position : targetPosition
    Movement.WalkTo(dest, OnArrived, run, teleportOnFail)

OnArrived(result):
    WalkCallback(result)
    End()           // signal completes when NPC arrives

OnPathingFailed():
    if teleportOnFail → Teleport(dest); End()
    else              → Interrupt()
```

### NPCSignal_WaitForDuration

```
FIELDS: waitMinutes, minutesWaited

Started():
    minutesWaited = 0
    Movement.StopWalk()

MinPassed():
    minutesWaited++
    if minutesWaited >= waitMinutes → End()
```

### NPCSignal_FaceTarget

```
FIELDS: target, duration, timer

Started():
    timer = 0
    BehaviourManager.GetBehaviour<FaceTargetBehaviour>().SetTarget(target)

ActiveUpdate():
    timer += deltaTime
    if timer >= duration → End()

End():
    BehaviourManager.GetBehaviour<FaceTargetBehaviour>().Disable()
    base.End()
```

### NPCSignal_UseObject

```
FIELDS: objectTransform, useTime, useRange
STATE: WalkingTo / Using

Started():
    state = WalkingTo
    Movement.WalkTo(objectTransform.position, OnArrived)

OnArrived(result):
    if result != Success → Interrupt(); return
    state = Using
    useTimer = 0
    Animation.TriggerTalk()

ActiveUpdate():
    if Using:
        useTimer += deltaTime
        if useTimer >= useTime:
            FIRE OnObjectUsed
            End()
```

**Checkpoint 12:** Issue a `NPCSignal_WalkToLocation` to a NPC mid-patrol. NPC interrupts patrol, walks to target, returns to patrol automatically.

---

# PHASE 13 — NPC Root Class

## The Problem

All these components exist on one GameObject. How does `CombatBehaviour` get to `NPCMovement`? How does anything outside the NPC know if it's alive? Every component can't use `GetComponent<>()` constantly — that's expensive and brittle.

## Design Pattern: Facade (Component Hub)

`NPC.cs` is the single point of access for all sub-systems. Everyone goes through the root:

```
combatBehaviour.Movement     → Npc.Movement
externalCode.TakeDamage(...)  → npc.TakeDamage(...)
ui.HealthBar → npc.Health.HealthPercent
```

`NPC` itself holds zero logic. It's pure composition: collect all components, expose them, delegate to them.

## Pseudocode

```
CLASS NPC : MonoBehaviour

    // Sub-system properties (read-only external access)
    Definition      : NPCDefinition
    Health          : NPCHealth
    Movement        : NPCMovement
    Awareness       : NPCAwareness
    Animation       : NPCAnimation
    BehaviourManager: NPCBehaviour
    Schedule        : NPCScheduleManager

    // Convenience state
    IsAlive       → Health.IsAlive
    IsDead        → Health.IsDead
    IsUnconscious → Health.IsUnconscious
    DisplayName   → Definition.DisplayName ?? gameObject.name

    // Inspector
    [SerializeField] definition : NPCDefinition

    // Events (bubble from sub-systems for easy external access)
    OnInitialized : Action<NPC>
    OnDied        : Action<EKnockdownCause>
    OnDamaged     : Action<float, GameObject>

    ON AWAKE:
        // Cache all components
        Health           = GetComponent<NPCHealth>()
        Movement         = GetComponent<NPCMovement>()
        Awareness        = GetComponent<NPCAwareness>()
        Animation        = GetComponent<NPCAnimation>()
        BehaviourManager = GetComponent<NPCBehaviour>()
        Schedule         = GetComponent<NPCScheduleManager>()

    ON START:
        if definition != null → Initialize(definition)

    VIRTUAL FUNCTION Initialize(definition):
        Definition = definition
        
        Health.Initialize(definition)
        Movement.Initialize(definition)
        Awareness.Initialize(definition)
        
        // Wire health events upward to NPC level
        Health.OnDied    += HandleDied
        Health.OnDamaged += HandleDamaged
        
        BehaviourManager.Initialize(this)
        Schedule.Initialize(this)
        
        FIRE OnInitialized(this)

    // Public action API (external code calls NPC, not sub-systems)
    FUNCTION TakeDamage(amount, source, cause):
        Health.TakeDamage(amount, source, cause)

    FUNCTION IssueSignal(signal):
        Schedule.IssueSignal(signal)

    FUNCTION LookAt(target):
        BehaviourManager.GetBehaviour<FaceTargetBehaviour>().SetTarget(target)

    FUNCTION StartDialogue(duration):
        BehaviourManager.GetBehaviour<GenericDialogueBehaviour>().StartDialogue(duration)

    FUNCTION SetThreat(threat):
        Awareness.SetThreat(threat)

    FUNCTION Calm():
        Awareness.Calm()

    PRIVATE HandleDied(cause):
        FIRE OnDied(cause)
        BehaviourManager.GetBehaviour<HeavyFlinchBehaviour>().Disable()

    PRIVATE HandleDamaged(amount, source):
        FIRE OnDamaged(amount, source)
        if amount >= 10 → BehaviourManager.GetBehaviour<HeavyFlinchBehaviour>().TriggerFlinch()
        if source != null → Awareness.SetThreat(source)
```

**Checkpoint 13:** Add all components to one GameObject. Create a `NPCDefinition` asset. Press Play → `NPC.Start()` calls `Initialize()` → all sub-systems initialize → `IdleBehaviour` activates. Everything works.

---

# PHASE 14 — Character Specializations

## The Problem

A `CustomerNPC` needs deal evaluation logic. A `PoliceNPC` needs arrest logic. An `EmployeeNPC` needs task assignment. Putting all of this in the base `NPC` class creates a bloated God class.

## Design Pattern: Inheritance for specialization only

Extend `NPC` only to add domain-specific behaviour. The base class stays clean. Each specialization adds only what is unique to that character type.

## CustomerNPC

```
EXTENDS NPC

EVENTS:
    OnRequestStarted(CustomerNPC)
    OnDealAccepted(CustomerNPC)
    OnDealRejected(CustomerNPC)
    OnDealLeft(CustomerNPC)

FIELDS:
    isRequestingProduct : bool
    satisfaction        : float   // 0-1, carries over between sessions
    requestTimer        : float

FUNCTION BeginProductRequest():
    if already requesting → return
    isRequestingProduct = true
    requestTimer = 0
    BehaviourManager.DisableBehaviour<WanderBehaviour>()
    BehaviourManager.EnableBehaviour<StationaryBehaviour>()
    FIRE OnRequestStarted(this)

ON UPDATE:
    if isRequestingProduct:
        requestTimer += deltaTime
        if requestTimer >= timeout → LeaveWithoutDeal()

FUNCTION EvaluateDeal(quality, offeredPrice, expectedPrice):
    qualityScore = clamp01(quality)
    priceScore   = offeredPrice <= expectedPrice*1.2 ? 1 : inverse ratio
    
    acceptChance = (qualityScore*0.5 + priceScore*0.5) * satisfaction
    accepted = Random.value < acceptChance
    
    if accepted:
        satisfaction += bonus
        CompleteDeal()
    else:
        satisfaction -= small penalty
        FIRE OnDealRejected

    return accepted
```

## PoliceNPC

```
EXTENDS NPC

EVENTS:
    OnArrestAttempt(PoliceNPC, GameObject)

OVERRIDES Initialize:
    base.Initialize(def)
    BehaviourManager.EnableBehaviour<PatrolBehaviour>()
    BehaviourManager.EnableBehaviour<CombatBehaviour>()

FUNCTION RespondToCrime(criminal):
    SetThreat(criminal)
    BehaviourManager.GetBehaviour<CombatBehaviour>().Enable()

FUNCTION AttemptArrest(target):
    isArresting = true
    arrestTarget = target
    Walk to target → when in range → CompleteArrest()
```

## EmployeeNPC

```
EXTENDS NPC

EVENTS:
    OnTaskAssigned(EmployeeNPC, string taskName)
    OnTaskCompleted(EmployeeNPC)

FIELDS:
    currentTaskName   : string
    workEfficiency    : float    // multiplier on work speed
    assignedStation   : Transform

FUNCTION AssignToStation(station, taskName):
    assignedStation = station
    currentTaskName = taskName
    stationary = BehaviourManager.GetBehaviour<StationaryBehaviour>()
    stationary.StationPoint = station
    stationary.Enable()
    FIRE OnTaskAssigned(this, taskName)

FUNCTION Unassign():
    assignedStation = null
    currentTaskName = "None"
    BehaviourManager.DisableBehaviour<StationaryBehaviour>()
    BehaviourManager.EnableBehaviour<WanderBehaviour>()
```

**Checkpoint 14:** Create a `CustomerNPC` prefab. Call `BeginProductRequest()`. Subscribe to `OnDealAccepted` and `OnDealLeft`. Evaluate a deal — confirm the events fire correctly.

---

# PHASE 15 — Integration and Full Test

## The End-to-End Test Scene

Build this test scene:

```
Scene:
├── _GameClock           (GameClock.cs, 0.1 seconds/minute for fast testing)
├── Floor                (NavMesh baked, large plane)
├── Walls                (a few obstacles to test occlusion)
├── Player               (tag: "Player", capsule with PlayerController)
│
├── NPC_Civilian
│   ├── NPCDefinition = NPC_Civilian asset
│   ├── Schedule:
│   │   ├── Action_Idle  [StartTime=0,  Duration=480]
│   │   └── Action_Wander [StartTime=480, Duration=960]
│   └── All 14 Behaviours
│
├── NPC_Guard
│   ├── NPCDefinition = NPC_Guard asset (CanFight=true)
│   ├── Schedule:
│   │   └── Action_Patrol [StartTime=0, Route=GuardRoute]
│   └── All 14 Behaviours
│
└── NPC_Customer
    ├── CustomerNPC component
    └── All standard behaviours + customer-specific
```

## Test Checklist

**Schedule:**
- [ ] Fast-forward clock past 480 → civilian transitions from Idle to Wander action
- [ ] `LogSchedule()` in console shows correct action list

**Awareness:**
- [ ] Walk player into guard's sight cone → guard spots player
- [ ] Walk behind a wall → after 3s → guard loses player
- [ ] Fire a gunshot sound event → nearby civilians hear it

**Behaviour stack:**
- [ ] Guard spots player → CombatBehaviour activates (CanFight=true)
- [ ] Civilian spots player → FleeBehaviour activates (FleesThreat=true)
- [ ] Deal 80+ damage to guard → guard dies → DeadBehaviour activates, cannot exit

**Health:**
- [ ] Deal damage → hit animation plays
- [ ] KO → ragdoll activates if explosion cause
- [ ] Revive timer works after knockout

**Signals:**
- [ ] Issue `NPCSignal_WalkToLocation` to guard mid-patrol
- [ ] Guard interrupts patrol, walks to target
- [ ] Signal ends → guard resumes patrol

**Customer:**
- [ ] `BeginProductRequest()` → customer stands still
- [ ] `EvaluateDeal(0.8f, 45f, 50f)` → deal likely accepted (good quality, under expected)
- [ ] Request timeout → `OnDealLeft` fires

## Common Issues and Fixes

| Symptom | Likely Cause | Fix |
|---|---|---|
| NPC doesn't move | NavMesh not baked, or NPC off NavMesh | Rebake; ensure NPC spawns on blue area |
| Behaviour never activates | Not added to behaviourStack | Check `NPCBehaviour.GetComponentsInChildren` found it |
| Wrong behaviour priority wins | Priority values conflicting | Print behaviourStack order via `LogSchedule()` |
| Vision ignores walls | OcclusionMask not set | Assign wall layers to `sightOcclusionMask` |
| Schedule doesn't start | GameClock not in scene | Add `_GameClock` GameObject |
| Animation doesn't play | Parameter name typo | Use Animator.StringToHash and verify hash matches |
| Walk never calls Success | Arrival threshold too tight | Increase `stoppingDistance` and `arrivalThreshold` |

---

## Summary: Design Patterns Used

| Phase | Pattern | Why |
|---|---|---|
| 0 | **Adapter** (NavMesh wrapper) | Swap pathfinding without changing callers |
| 1 | **Singleton + Observer** (GameClock) | One clock, many subscribers |
| 2 | **ScriptableObject / Data Asset** | Config separate from runtime, designer-editable |
| 3 | **Observer via C# Events** (NPCHealth) | Decoupled damage notifications |
| 4 | **Callback / Delegate** (WalkResult) | Typed completion notification |
| 5 | **Observer + Tick-based** (Awareness) | Cheap detection, decoupled reactions |
| 6 | **Facade + Pre-Hashed Params** (Animation) | Single animation contact point, fast |
| 7 | **Template Method** (Behaviour base) | Lifecycle skeleton, subclass fills content |
| 8 | **Priority Strategy Stack** | Dynamic behaviour selection, no transition table |
| 9 | **Template Method** (each behaviour) | Each overrides only what's unique |
| 10 | **Template Method** (NPCAction) | Consistent schedule lifecycle |
| 11 | **Event-driven Queue** (ScheduleManager) | Clock fires → schedule reacts |
| 12 | **Command** (NPCSignal) | Encapsulated one-shot NPC task |
| 13 | **Facade / Component Hub** (NPC root) | Single access point for all sub-systems |
| 14 | **Inheritance for specialization** | Add only domain-specific behaviour |

---

*You now have all the information to build the complete system. Build one phase, test it fully, then move to the next.*
