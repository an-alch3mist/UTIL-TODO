# Schedule-1 Style NPC System — Complete Implementation

> **Plain Unity C# · NavMesh · No networking framework**  
> Every feature from the Schedule-1 architecture — priority behaviour stacks, daily schedulers,
> vision/hearing awareness, health/ragdoll/knockdown, dialogue, patrol groups, and typed NPC
> subtypes — implemented in ~40 files with full working logic.

---

## Table of Contents

1. [Project Setup & Requirements](#1-project-setup--requirements)
2. [File Structure](#2-file-structure)
3. [Infrastructure](#3-infrastructure)
4. [Core NPC Components](#4-core-npc-components)
5. [Behaviour System — Base](#5-behaviour-system--base)
6. [Concrete Behaviours — Idle & Navigation](#6-concrete-behaviours--idle--navigation)
7. [Concrete Behaviours — Social](#7-concrete-behaviours--social)
8. [Concrete Behaviours — Threat Response](#8-concrete-behaviours--threat-response)
9. [Schedule System](#9-schedule-system)
10. [Concrete Signals & Events](#10-concrete-signals--events)
11. [Supporting Types](#11-supporting-types)
12. [NPC Subtypes](#12-npc-subtypes)
13. [Animator Setup & Animation Constants](#13-animator-setup--animation-constants)
14. [Integration Guide & Usage Examples](#14-integration-guide--usage-examples)

---

## 1. Project Setup & Requirements

**Unity version:** 2022.3 LTS or newer  
**Required packages:**  
- AI Navigation (com.unity.ai.navigation) for NavMesh  
- Cinemachine (optional, for camera follow during chase)

**Layer setup in Project Settings → Physics:**
```
Layer 8:  NPC
Layer 9:  Player
Layer 10: Obstacle
Layer 11: Ground
```

**Tags required:** `Player`, `NPC`

**Animator Parameters (all NPCs share this contract):**
```
Float  : Speed          (0–2, drives walk/run blend tree)
Float  : TurnSpeed      (for strafe/turning)
Bool   : IsMoving
Bool   : IsSitting
Bool   : IsKnockedOut
Bool   : IsDead
Bool   : IsRagdoll
Trigger: Idle           (random idle triggers)
Trigger: Cower
Trigger: TakeDamage
Trigger: Attack
Trigger: Consume
Trigger: GetUp
Trigger: PhoneCall
```

---

## 2. File Structure

```
Assets/NPCSystem/
├── Infrastructure/
│   ├── Singleton.cs
│   └── GameClock.cs
├── Core/
│   ├── NPC.cs
│   ├── NPCMovement.cs
│   ├── NPCHealth.cs
│   ├── NPCAwareness.cs
│   ├── VisionCone.cs
│   ├── NPCAnimation.cs
│   └── NPCScheduleManager.cs
├── Behaviour/
│   ├── Behaviour.cs
│   ├── NPCBehaviour.cs
│   ├── NPCActions.cs
│   ├── NPCResponses.cs
│   ├── Idle/
│   │   ├── IdleBehaviour.cs
│   │   ├── WanderBehaviour.cs
│   │   └── SitBehaviour.cs
│   ├── Navigation/
│   │   ├── PatrolBehaviour.cs
│   │   ├── StationaryBehaviour.cs
│   │   └── FollowBehaviour.cs
│   ├── Social/
│   │   ├── GenericDialogueBehaviour.cs
│   │   ├── RequestProductBehaviour.cs
│   │   └── ConsumeProductBehaviour.cs
│   └── ThreatResponse/
│       ├── CoweringBehaviour.cs
│       ├── FleeBehaviour.cs
│       ├── CombatBehaviour.cs
│       ├── CallPoliceBehaviour.cs
│       ├── HeavyFlinchBehaviour.cs
│       ├── RagdollBehaviour.cs
│       ├── UnconsciousBehaviour.cs
│       └── DeadBehaviour.cs
├── Schedule/
│   ├── NPCAction.cs
│   ├── NPCSignal.cs
│   ├── NPCEvent.cs
│   ├── Signals/
│   │   ├── NPCSignal_WalkToLocation.cs
│   │   ├── NPCSignal_WaitAtLocation.cs
│   │   ├── NPCSignal_Sit.cs
│   │   └── NPCSignal_UseObject.cs
│   └── Events/
│       ├── NPCEvent_Idle.cs
│       ├── NPCEvent_Patrol.cs
│       └── NPCEvent_Conversation.cs
├── Supporting/
│   ├── FootPatrolRoute.cs
│   ├── PatrolGroup.cs
│   ├── NPCInteractable.cs
│   └── NPCRelationData.cs
└── Types/
    ├── CustomerNPC.cs
    ├── PoliceNPC.cs
    └── EmployeeNPC.cs
```

---

## 3. Infrastructure

### `Singleton.cs`

```csharp
using UnityEngine;

namespace NPCSystem
{
    /// <summary>
    /// Generic MonoBehaviour singleton. Automatically destroys duplicate instances.
    /// </summary>
    public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindObjectOfType<T>();
                return _instance;
            }
        }

        public static bool InstanceExists => _instance != null;

        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = (T)this;
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
    }
}
```

---

### `GameClock.cs`

```csharp
using System;
using UnityEngine;

namespace NPCSystem
{
    /// <summary>
    /// In-game 24-hour clock. Time is in minutes from midnight (0–1439).
    /// Default: SecondsPerMinute = 1 means 1 real second = 1 game minute.
    /// A full day = 1440 real seconds = 24 real minutes.
    /// </summary>
    public class GameClock : Singleton<GameClock>
    {
        [Header("Settings")]
        [Tooltip("How many real seconds equal one in-game minute. Lower = faster day.")]
        public float SecondsPerMinute = 1f;

        [Range(0, 1439)]
        public int StartTime = 480; // 8:00 AM

        [Header("Debug (read-only)")]
        [SerializeField] private int _currentTime;

        private float _accumulator;

        // ── Public State ──────────────────────────────────────────────────────
        public int CurrentTime   => _currentTime;
        public int Hour          => _currentTime / 60;
        public int Minute        => _currentTime % 60;
        public int Day           { get; private set; } = 1;

        // ── Events ────────────────────────────────────────────────────────────
        public event Action<int> onMinuteChanged;  // arg: new CurrentTime
        public event Action<int> onHourChanged;    // arg: new Hour
        public event Action<int> onDayChanged;     // arg: new Day number

        // ── Unity ─────────────────────────────────────────────────────────────
        protected override void Awake()
        {
            base.Awake();
            _currentTime = Mathf.Clamp(StartTime, 0, 1439);
        }

        private void Update()
        {
            _accumulator += Time.deltaTime;
            while (_accumulator >= SecondsPerMinute)
            {
                _accumulator -= SecondsPerMinute;
                AdvanceMinute();
            }
        }

        // ── Private ───────────────────────────────────────────────────────────
        private void AdvanceMinute()
        {
            int prevHour = Hour;
            _currentTime++;

            if (_currentTime >= 1440)
            {
                _currentTime = 0;
                Day++;
                onDayChanged?.Invoke(Day);
            }

            onMinuteChanged?.Invoke(_currentTime);

            if (Hour != prevHour)
                onHourChanged?.Invoke(Hour);
        }

        // ── Public API ────────────────────────────────────────────────────────
        /// <summary>Returns HH:MM string. use24Hour=false gives 12h format.</summary>
        public string GetTimeString(bool use24Hour = true)
        {
            if (use24Hour) return $"{Hour:00}:{Minute:00}";
            int h = Hour % 12; if (h == 0) h = 12;
            return $"{h}:{Minute:00} {(Hour < 12 ? "AM" : "PM")}";
        }

        /// <summary>True if current time is within [startTime, endTime).</summary>
        public bool IsTimeBetween(int startTime, int endTime)
        {
            if (startTime <= endTime)
                return _currentTime >= startTime && _currentTime < endTime;
            return _currentTime >= startTime || _currentTime < endTime; // wraps midnight
        }

        /// <summary>Minutes until targetTime, wrapping if needed.</summary>
        public int MinutesUntil(int targetTime)
        {
            int diff = targetTime - _currentTime;
            return diff < 0 ? diff + 1440 : diff;
        }

        /// <summary>Force-set the clock (editor testing).</summary>
        public void SetTime(int minutes) => _currentTime = Mathf.Clamp(minutes, 0, 1439);
    }
}
```

---

## 4. Core NPC Components

### `NPC.cs`

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NPCSystem
{
    /// <summary>
    /// Root NPC component. Wires all subsystems together and exposes a clean API.
    /// Requires: NPCBehaviour, NPCMovement, NPCHealth, NPCAwareness,
    ///           NPCAnimation, NPCScheduleManager, NPCActions on the same GameObject.
    /// </summary>
    [RequireComponent(typeof(NPCBehaviour))]
    [RequireComponent(typeof(NPCMovement))]
    [RequireComponent(typeof(NPCHealth))]
    [RequireComponent(typeof(NPCAwareness))]
    [RequireComponent(typeof(NPCAnimation))]
    [RequireComponent(typeof(NPCScheduleManager))]
    [RequireComponent(typeof(NPCActions))]
    public class NPC : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("Identity")]
        public string FirstName;
        public string LastName;
        public string ID;
        public NPCType Type;
        public EMapRegion Region;
        public Sprite MugshotSprite;

        [Header("Personality")]
        [Range(0f, 1f)] public float Aggression = 0.3f;
        [Tooltip("If true, re-spawns next in-game day after death instead of 3 days.")]
        public bool IsImportant;

        [Header("Settings")]
        public bool CanOpenDoors = true;
        public bool AwarenessActiveByDefault = true;

        [Header("References")]
        [SerializeField] protected Transform _modelContainer;
        [SerializeField] protected List<Renderer> _outlineRenderers;

        // ── Subsystem References (auto-resolved on Awake) ─────────────────────
        public NPCBehaviour    Behaviour    { get; private set; }
        public NPCMovement     Movement     { get; private set; }
        public NPCHealth       Health       { get; private set; }
        public NPCAwareness    Awareness    { get; private set; }
        public NPCAnimation    Animation    { get; private set; }
        public NPCScheduleManager Schedule  { get; private set; }
        public NPCActions      Actions      { get; private set; }

        // ── State ─────────────────────────────────────────────────────────────
        public string FullName => string.IsNullOrEmpty(LastName) ? FirstName : $"{FirstName} {LastName}";
        public bool   IsConscious => !Health.IsDead && !Health.IsKnockedOut;
        public float  Scale       { get; private set; } = 1f;
        public bool   IsVisible   { get; protected set; }

        // ── Events ────────────────────────────────────────────────────────────
        public event Action<NPC> onSpawned;
        public event Action<NPC> onDied;
        public event Action<NPC> onKnockedOut;
        public event Action<NPC> onRevived;
        public event Action<bool> onVisibilityChanged;

        // ── Unity ─────────────────────────────────────────────────────────────
        protected virtual void Awake()
        {
            Behaviour = GetComponent<NPCBehaviour>();
            Movement  = GetComponent<NPCMovement>();
            Health    = GetComponent<NPCHealth>();
            Awareness = GetComponent<NPCAwareness>();
            Animation = GetComponent<NPCAnimation>();
            Schedule  = GetComponent<NPCScheduleManager>();
            Actions   = GetComponent<NPCActions>();
        }

        protected virtual void Start()
        {
            Health.onDied       += HandleDied;
            Health.onKnockedOut += HandleKnockedOut;
            Health.onRevived    += HandleRevived;

            Awareness.SetAwarenessActive(AwarenessActiveByDefault);
            onSpawned?.Invoke(this);
        }

        // ── Health Event Handlers ─────────────────────────────────────────────
        protected virtual void HandleDied()
        {
            Movement.Stop();
            Schedule.DisableSchedule();
            Behaviour.ForceActivateBehaviour<DeadBehaviour>();
            onDied?.Invoke(this);
        }

        protected virtual void HandleKnockedOut()
        {
            Movement.Stop();
            Behaviour.EnableBehaviour<UnconsciousBehaviour>();
            onKnockedOut?.Invoke(this);
        }

        protected virtual void HandleRevived()
        {
            Behaviour.DisableBehaviour<UnconsciousBehaviour>();
            Schedule.EnableSchedule();
            onRevived?.Invoke(this);
        }

        // ── Public API ────────────────────────────────────────────────────────
        public void SetVisible(bool visible)
        {
            if (IsVisible == visible) return;
            IsVisible = visible;
            onVisibilityChanged?.Invoke(visible);
        }

        public void SetScale(float scale)
        {
            Scale = scale;
            _modelContainer.localScale = Vector3.one * scale;
        }

        public void ShowOutline(Color color)
        {
            foreach (var r in _outlineRenderers)
            {
                // Swap material or enable outline component here
                // depends on your outline solution (EPO, HighlightPlus, custom)
                r.material.SetColor("_OutlineColor", color);
                r.material.SetFloat("_OutlineWidth", 3f);
            }
        }

        public void HideOutline()
        {
            foreach (var r in _outlineRenderers)
                r.material.SetFloat("_OutlineWidth", 0f);
        }
    }

    // ── Supporting Enums ──────────────────────────────────────────────────────
    public enum NPCType
    {
        Civilian,
        Customer,
        Police,
        Employee,
        Dealer,
        CartelMember,
        Vendor
    }

    public enum EMapRegion
    {
        Downtown,
        Suburbs,
        Industrial,
        Uptown,
        Docks
    }
}
```

---

### `NPCMovement.cs`

```csharp
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace NPCSystem
{
    /// <summary>
    /// Wraps NavMeshAgent. All pathfinding goes through this component.
    /// Tracks consecutive pathing failures and exposes a clean callback-based API.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class NPCMovement : MonoBehaviour
    {
        public enum EAgentType   { Humanoid, BigHumanoid }
        public enum EStance      { None, Stanced, Crouched }
        public enum WalkResult   { Failed, Interrupted, Stopped, Partial, Success }

        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("Speeds")]
        public float WalkSpeed      = 1.4f;
        public float RunSpeed       = 3.5f;
        public float MoveSpeedMultiplier = 1f;

        [Header("Obstacle Avoidance")]
        public ObstacleAvoidanceType DefaultAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;

        [Header("Slippery Mode (drug effects)")]
        public bool SlipperyMode;
        public float SlipperyMultiplier = 0.3f;

        [Header("References")]
        public NavMeshAgent Agent;
        public CapsuleCollider CapsuleCol;

        // ── State ─────────────────────────────────────────────────────────────
        public bool    HasDestination      { get; private set; }
        public bool    IsPaused            { get; private set; }
        public bool    IsRunning           { get; private set; }
        public Vector3 CurrentDestination  { get; private set; }
        public EStance Stance              { get; private set; }
        public bool    Disoriented         { get; set; }

        public bool IsMoving =>
            Agent.enabled && Agent.hasPath && Agent.remainingDistance > Agent.stoppingDistance;

        public Vector3 FootPosition => transform.position;

        // ── Private ───────────────────────────────────────────────────────────
        private NPC             _npc;
        private Action<WalkResult> _walkCallback;
        private float           _successThreshold = 1f;
        private int             _consecutivePathingFailures;
        private Coroutine       _faceRoutine;
        private Coroutine       _stuckCheckRoutine;
        private Vector3         _lastPosition;
        private float           _stuckTimer;

        private const int   MAX_PATHING_FAILURES = 5;
        private const float STUCK_CHECK_INTERVAL = 2f;
        private const float STUCK_THRESHOLD      = 0.2f;

        // ── Unity ─────────────────────────────────────────────────────────────
        private void Awake()
        {
            _npc  = GetComponent<NPC>();
            Agent = Agent != null ? Agent : GetComponent<NavMeshAgent>();
            Agent.obstacleAvoidanceType = DefaultAvoidanceType;
            ApplySpeed();
        }

        private void Update()
        {
            if (!Agent.enabled || IsPaused || !HasDestination) return;

            // Destination reached?
            if (!Agent.pathPending && Agent.remainingDistance <= _successThreshold)
            {
                CompleteWalk(WalkResult.Success);
                return;
            }

            // Path failed?
            if (!Agent.pathPending && Agent.pathStatus == NavMeshPathStatus.PathInvalid)
            {
                _consecutivePathingFailures++;
                CompleteWalk(_consecutivePathingFailures >= MAX_PATHING_FAILURES
                    ? WalkResult.Failed : WalkResult.Partial);
            }
        }

        // ── Public API ────────────────────────────────────────────────────────
        /// <summary>Walk to a world position. Fires callback on arrival, failure, or interrupt.</summary>
        public void WalkTo(Vector3 destination, Action<WalkResult> callback = null,
                           float successThreshold = 0.5f, bool run = false)
        {
            if (!Agent.enabled) { callback?.Invoke(WalkResult.Failed); return; }

            // Interrupt previous walk
            if (HasDestination)
                InvokeCallback(WalkResult.Interrupted);

            // Sample NavMesh for nearest reachable point
            if (!NavMesh.SamplePosition(destination, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                _consecutivePathingFailures++;
                callback?.Invoke(WalkResult.Failed);
                return;
            }

            CurrentDestination  = hit.position;
            _walkCallback       = callback;
            _successThreshold   = successThreshold;
            HasDestination      = true;
            IsRunning           = run;
            ApplySpeed();

            Agent.SetDestination(CurrentDestination);
            StartStuckCheck();
        }

        /// <summary>Walk to a Transform's position (re-paths if Transform moves).</summary>
        public void WalkTo(Transform target, Action<WalkResult> callback = null,
                           float successThreshold = 1.5f, bool run = false)
        {
            WalkTo(target.position, callback, successThreshold, run);
        }

        /// <summary>Stop movement immediately.</summary>
        public void Stop()
        {
            if (HasDestination)
                CompleteWalk(WalkResult.Stopped);

            if (Agent.enabled)
            {
                Agent.ResetPath();
                Agent.velocity = Vector3.zero;
            }
        }

        /// <summary>Instantly move NPC to position on NavMesh.</summary>
        public void Teleport(Vector3 position)
        {
            Stop();
            Agent.Warp(position);
        }

        /// <summary>Pause NavMesh movement (NPC stays in place).</summary>
        public void Pause()
        {
            IsPaused = true;
            Agent.isStopped = true;
        }

        /// <summary>Resume after pause.</summary>
        public void Resume()
        {
            IsPaused = false;
            Agent.isStopped = false;
        }

        public void SetAgentEnabled(bool enabled)
        {
            Agent.enabled = enabled;
            if (CapsuleCol) CapsuleCol.enabled = enabled;
        }

        /// <summary>Smoothly rotate to face a direction over lerpTime seconds.</summary>
        public void FaceDirection(Vector3 forward, float lerpTime = 0.3f)
        {
            if (_faceRoutine != null) StopCoroutine(_faceRoutine);
            _faceRoutine = StartCoroutine(FaceDirectionRoutine(forward, lerpTime));
        }

        public void SetStance(EStance stance)
        {
            Stance = stance;
            // Drive animator crouching state here if needed
        }

        public void SetRunning(bool run)
        {
            IsRunning = run;
            ApplySpeed();
        }

        // ── Private ───────────────────────────────────────────────────────────
        private void ApplySpeed()
        {
            float baseSpeed = IsRunning ? RunSpeed : WalkSpeed;
            float mult      = SlipperyMode ? SlipperyMultiplier : 1f;
            Agent.speed     = baseSpeed * MoveSpeedMultiplier * mult;
        }

        private void CompleteWalk(WalkResult result)
        {
            HasDestination = false;
            StopStuckCheck();

            if (result == WalkResult.Success)
                _consecutivePathingFailures = 0;

            InvokeCallback(result);
        }

        private void InvokeCallback(WalkResult result)
        {
            var cb = _walkCallback;
            _walkCallback = null;
            cb?.Invoke(result);
        }

        private void StartStuckCheck()
        {
            StopStuckCheck();
            _stuckCheckRoutine = StartCoroutine(StuckCheckRoutine());
        }

        private void StopStuckCheck()
        {
            if (_stuckCheckRoutine != null)
            {
                StopCoroutine(_stuckCheckRoutine);
                _stuckCheckRoutine = null;
            }
        }

        private IEnumerator StuckCheckRoutine()
        {
            _lastPosition = transform.position;
            while (HasDestination)
            {
                yield return new WaitForSeconds(STUCK_CHECK_INTERVAL);
                float moved = Vector3.Distance(transform.position, _lastPosition);
                if (HasDestination && moved < STUCK_THRESHOLD)
                {
                    // NPC hasn't moved — count as pathing failure
                    _consecutivePathingFailures++;
                    if (_consecutivePathingFailures >= MAX_PATHING_FAILURES)
                        CompleteWalk(WalkResult.Failed);
                }
                _lastPosition = transform.position;
            }
        }

        private IEnumerator FaceDirectionRoutine(Vector3 forward, float lerpTime)
        {
            Quaternion start  = transform.rotation;
            Quaternion target = Quaternion.LookRotation(forward, Vector3.up);
            float      t      = 0f;

            while (t < 1f)
            {
                t += Time.deltaTime / Mathf.Max(lerpTime, 0.01f);
                transform.rotation = Quaternion.Slerp(start, target, t);
                yield return null;
            }

            transform.rotation = target;
            _faceRoutine       = null;
        }
    }
}
```

---

### `NPCHealth.cs`

```csharp
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace NPCSystem
{
    /// <summary>
    /// Manages NPC health, knockdown, death, and revival timing.
    /// All damage/heal/kill calls should go through this component.
    /// </summary>
    [DisallowMultipleComponent]
    public class NPCHealth : MonoBehaviour
    {
        public enum DamageType { Generic, Melee, Ranged, Explosion, Vehicle }

        public const int REVIVE_DAYS = 3;   // days before important NPC revives

        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("Settings")]
        public float MaxHealth = 100f;
        public bool  Invincible;
        public bool  CanRevive  = true;
        [Tooltip("If true, NPC goes unconscious instead of dying when health hits 0.")]
        public bool  KnockOutInsteadOfKill;

        [Header("Events (wired in Inspector)")]
        public UnityEvent onDieUnityEvent;
        public UnityEvent onKnockedOutUnityEvent;
        public UnityEvent onReviveUnityEvent;

        // ── State ─────────────────────────────────────────────────────────────
        public float  CurrentHealth     { get; private set; }
        public float  NormalizedHealth  => MaxHealth > 0 ? CurrentHealth / MaxHealth : 0f;
        public bool   IsDead            { get; private set; }
        public bool   IsKnockedOut      { get; private set; }
        public int    DaysSinceDeath    { get; private set; }
        public int    HoursSinceAttackedByPlayer { get; private set; }

        // ── Events ────────────────────────────────────────────────────────────
        public event Action             onDied;
        public event Action             onKnockedOut;
        public event Action             onRevived;
        public event Action<float>      onTookDamage;   // arg: amount
        public event Action<float>      onHealed;        // arg: amount

        // ── Private ───────────────────────────────────────────────────────────
        private NPC       _npc;
        private Coroutine _autoReviveRoutine;

        // ── Unity ─────────────────────────────────────────────────────────────
        private void Awake()
        {
            _npc          = GetComponent<NPC>();
            CurrentHealth = MaxHealth;
        }

        private void Start()
        {
            GameClock.Instance.onDayChanged += OnDayChanged;
        }

        private void OnDestroy()
        {
            if (GameClock.InstanceExists)
                GameClock.Instance.onDayChanged -= OnDayChanged;
        }

        // ── Public API ────────────────────────────────────────────────────────
        public void TakeDamage(float amount, DamageType type = DamageType.Generic)
        {
            if (Invincible || IsDead) return;
            if (amount <= 0f) return;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
            onTookDamage?.Invoke(amount);

            if (CurrentHealth <= 0f)
            {
                if (KnockOutInsteadOfKill && !IsKnockedOut)
                    KnockOut();
                else
                    Die();
            }
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f) return;
            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
            onHealed?.Invoke(amount);
        }

        public void RestoreHealth()
        {
            CurrentHealth = MaxHealth;
            onHealed?.Invoke(MaxHealth);
        }

        public virtual void KnockOut()
        {
            if (IsKnockedOut || IsDead) return;
            IsKnockedOut = true;
            onKnockedOut?.Invoke();
            onKnockedOutUnityEvent?.Invoke();

            if (CanRevive)
                _autoReviveRoutine = StartCoroutine(AutoReviveRoutine());
        }

        public virtual void Revive()
        {
            if (!IsKnockedOut) return;
            if (_autoReviveRoutine != null) StopCoroutine(_autoReviveRoutine);
            IsKnockedOut  = false;
            CurrentHealth = MaxHealth * 0.25f;   // revive at 25% health
            onRevived?.Invoke();
            onReviveUnityEvent?.Invoke();
        }

        public virtual void Die()
        {
            if (IsDead) return;
            IsDead        = true;
            IsKnockedOut  = false;
            CurrentHealth = 0f;
            DaysSinceDeath = 0;
            onDied?.Invoke();
            onDieUnityEvent?.Invoke();
        }

        public void NotifyAttackedByPlayer()
        {
            HoursSinceAttackedByPlayer = 0;
        }

        // ── Private ───────────────────────────────────────────────────────────
        private IEnumerator AutoReviveRoutine()
        {
            yield return new WaitForSeconds(30f);   // 30 real-seconds ≈ 30 in-game min
            if (IsKnockedOut && !IsDead)
                Revive();
        }

        private void OnDayChanged(int day)
        {
            HoursSinceAttackedByPlayer++;
            if (IsDead)
            {
                DaysSinceDeath++;
                // Important NPCs revive after 1 day, others after 3
                int reviveDays = (_npc != null && _npc.IsImportant) ? 1 : REVIVE_DAYS;
                if (DaysSinceDeath >= reviveDays && CanRevive)
                    RespawnNextDay();
            }
        }

        private void RespawnNextDay()
        {
            IsDead         = false;
            DaysSinceDeath = 0;
            CurrentHealth  = MaxHealth;
            onRevived?.Invoke();
        }
    }
}
```

---

### `VisionCone.cs`

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace NPCSystem
{
    /// <summary>
    /// Physics-based vision cone. Performs sphere-cast + angle check + line-of-sight raycast.
    /// Attach to the NPC's head transform for correct eyeline.
    /// </summary>
    public class VisionCone : MonoBehaviour
    {
        [Header("Settings")]
        public float Range       = 16f;
        [Range(10f, 180f)]
        public float Angle       = 110f;
        public float UpdateRate  = 0.15f;   // seconds between vision checks
        public LayerMask TargetMask;        // Layer 9 (Player)
        public LayerMask ObstacleMask;      // Layer 10 + Layer 11

        public List<Transform> VisibleTargets { get; private set; } = new List<Transform>();

        private float _timer;

        private void Update()
        {
            _timer += Time.deltaTime;
            if (_timer >= UpdateRate)
            {
                _timer = 0f;
                FindVisibleTargets();
            }
        }

        private void FindVisibleTargets()
        {
            VisibleTargets.Clear();

            Collider[] hits = Physics.OverlapSphere(transform.position, Range, TargetMask);
            foreach (var hit in hits)
            {
                Vector3 dirToTarget = (hit.transform.position - transform.position).normalized;
                float   angle       = Vector3.Angle(transform.forward, dirToTarget);

                if (angle > Angle / 2f) continue;

                float dist = Vector3.Distance(transform.position, hit.transform.position);
                if (!Physics.Raycast(transform.position, dirToTarget, dist, ObstacleMask))
                    VisibleTargets.Add(hit.transform);
            }
        }

        public bool CanSeeTransform(Transform target)
        {
            if (target == null) return false;
            Vector3 dir  = (target.position - transform.position).normalized;
            float   dist = Vector3.Distance(transform.position, target.position);
            float   ang  = Vector3.Angle(transform.forward, dir);

            if (dist > Range || ang > Angle / 2f) return false;
            return !Physics.Raycast(transform.position, dir, dist, ObstacleMask);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, Range);
            Vector3 leftBound  = Quaternion.Euler(0, -Angle / 2f, 0) * transform.forward;
            Vector3 rightBound = Quaternion.Euler(0,  Angle / 2f, 0) * transform.forward;
            Gizmos.DrawRay(transform.position, leftBound  * Range);
            Gizmos.DrawRay(transform.position, rightBound * Range);
        }
    }
}
```

---

### `NPCAwareness.cs`

```csharp
using System;
using UnityEngine;
using UnityEngine.Events;

namespace NPCSystem
{
    /// <summary>
    /// Integrates VisionCone + hearing radius + crime/threat event dispatch.
    /// NPCResponses subscribes to these events to decide how to react.
    /// </summary>
    public class NPCAwareness : MonoBehaviour
    {
        public const float PLAYER_AIM_DETECTION_RANGE = 15f;

        [Header("References")]
        public VisionCone VisionCone;

        [Header("Hearing")]
        public float HearingRange       = 12f;
        public float GunshotHearRange   = 50f;
        public float ExplosionHearRange = 80f;

        // ── Events ────────────────────────────────────────────────────────────
        public UnityEvent<Transform> onPlayerSeen;
        public UnityEvent            onPlayerLost;
        public UnityEvent<Transform> onNoticedCrime;
        public UnityEvent<Transform> onNoticedDrugDealing;
        public UnityEvent            onGunshotHeard;
        public UnityEvent            onExplosionHeard;
        public UnityEvent<Transform> onHitByCar;

        // ── State ─────────────────────────────────────────────────────────────
        public bool      IsAwarenessActive { get; private set; }
        public Transform CurrentThreat     { get; private set; }
        public bool      IsThreatVisible   => VisionCone != null &&
                                              CurrentThreat != null &&
                                              VisionCone.CanSeeTransform(CurrentThreat);

        // ── Private ───────────────────────────────────────────────────────────
        private NPC   _npc;
        private bool  _playerWasVisible;
        private float _updateRate = 0.2f;
        private float _timer;

        // ── Unity ─────────────────────────────────────────────────────────────
        private void Awake() => _npc = GetComponent<NPC>();

        private void Update()
        {
            if (!IsAwarenessActive) return;

            _timer += Time.deltaTime;
            if (_timer < _updateRate) return;
            _timer = 0f;

            CheckVision();
        }

        // ── Private ───────────────────────────────────────────────────────────
        private void CheckVision()
        {
            if (VisionCone == null) return;
            bool playerVisible = VisionCone.VisibleTargets.Count > 0;

            if (playerVisible && !_playerWasVisible)
            {
                CurrentThreat    = VisionCone.VisibleTargets[0];
                _playerWasVisible = true;
                onPlayerSeen?.Invoke(CurrentThreat);
            }
            else if (!playerVisible && _playerWasVisible)
            {
                _playerWasVisible = false;
                onPlayerLost?.Invoke();
            }
        }

        // ── Public API ────────────────────────────────────────────────────────
        public void SetAwarenessActive(bool active) => IsAwarenessActive = active;

        /// <summary>Called when a noise is heard (from NoiseEmitter components).</summary>
        public void OnNoiseReceived(NoiseType type, Vector3 origin)
        {
            if (!IsAwarenessActive) return;
            float dist = Vector3.Distance(transform.position, origin);

            switch (type)
            {
                case NoiseType.Gunshot   when dist <= GunshotHearRange:   onGunshotHeard?.Invoke();   break;
                case NoiseType.Explosion when dist <= ExplosionHearRange: onExplosionHeard?.Invoke(); break;
            }
        }

        /// <summary>Called by crime systems when player commits a witnessed crime.</summary>
        public void OnCrimeWitnessed(Transform perpetrator)
        {
            if (!IsAwarenessActive) return;
            onNoticedCrime?.Invoke(perpetrator);
        }

        public void OnHitByVehicle(Transform vehicle) => onHitByCar?.Invoke(vehicle);
    }

    public enum NoiseType { Footstep, Gunshot, Explosion, Crash, Voice }
}
```

---

### `NPCAnimation.cs`

```csharp
using UnityEngine;

namespace NPCSystem
{
    /// <summary>
    /// Thin wrapper over Animator. Uses pre-hashed IDs for zero-GC parameter setting.
    /// Drives locomotion blend tree and all behaviour-specific triggers.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class NPCAnimation : MonoBehaviour
    {
        // ── Animator Parameter Hashes ─────────────────────────────────────────
        public static readonly int SpeedHash       = Animator.StringToHash("Speed");
        public static readonly int TurnSpeedHash   = Animator.StringToHash("TurnSpeed");
        public static readonly int IsMovingHash    = Animator.StringToHash("IsMoving");
        public static readonly int IsSittingHash   = Animator.StringToHash("IsSitting");
        public static readonly int IsKnockedHash   = Animator.StringToHash("IsKnockedOut");
        public static readonly int IsDeadHash      = Animator.StringToHash("IsDead");
        public static readonly int IdleTrigger     = Animator.StringToHash("Idle");
        public static readonly int CowerTrigger    = Animator.StringToHash("Cower");
        public static readonly int DamageTrigger   = Animator.StringToHash("TakeDamage");
        public static readonly int AttackTrigger   = Animator.StringToHash("Attack");
        public static readonly int ConsumeTrigger  = Animator.StringToHash("Consume");
        public static readonly int GetUpTrigger    = Animator.StringToHash("GetUp");
        public static readonly int PhoneCallTrigger= Animator.StringToHash("PhoneCall");

        // ── References ────────────────────────────────────────────────────────
        public Animator Animator { get; private set; }

        private NPCMovement _movement;

        // ── Unity ─────────────────────────────────────────────────────────────
        private void Awake()
        {
            Animator  = GetComponent<Animator>();
            _movement = GetComponent<NPCMovement>();
        }

        private void Update()
        {
            if (_movement == null) return;

            float speed    = _movement.Agent.velocity.magnitude / _movement.RunSpeed;
            float turnRate = Vector3.Dot(
                transform.right,
                _movement.Agent.desiredVelocity.normalized);

            Animator.SetFloat(SpeedHash,     speed,    0.1f, Time.deltaTime);
            Animator.SetFloat(TurnSpeedHash, turnRate, 0.1f, Time.deltaTime);
            Animator.SetBool(IsMovingHash,   _movement.IsMoving);
        }

        // ── Public API ────────────────────────────────────────────────────────
        public void SetSitting(bool sitting)    => Animator.SetBool(IsSittingHash,  sitting);
        public void SetKnockedOut(bool knocked) => Animator.SetBool(IsDeadHash,     knocked);
        public void SetDead(bool dead)          => Animator.SetBool(IsDeadHash,     dead);

        public void TriggerIdle()       => Animator.SetTrigger(IdleTrigger);
        public void TriggerCower()      => Animator.SetTrigger(CowerTrigger);
        public void TriggerDamage()     => Animator.SetTrigger(DamageTrigger);
        public void TriggerAttack()     => Animator.SetTrigger(AttackTrigger);
        public void TriggerConsume()    => Animator.SetTrigger(ConsumeTrigger);
        public void TriggerGetUp()      => Animator.SetTrigger(GetUpTrigger);
        public void TriggerPhoneCall()  => Animator.SetTrigger(PhoneCallTrigger);

        public void PlayAnimation(string stateName, int layer = 0)
            => Animator.Play(stateName, layer);

        public bool IsInState(string stateName, int layer = 0)
            => Animator.GetCurrentAnimatorStateInfo(layer).IsName(stateName);
    }
}
```

---

### `NPCScheduleManager.cs`

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NPCSystem
{
    /// <summary>
    /// Runs the NPC's daily schedule. Each NPCAction has a StartTime (0–1439).
    /// Each minute the clock advances, the manager checks whether pending actions
    /// should start, and whether the current action should end.
    ///
    /// Actions are evaluated in Priority order (highest first) when multiple
    /// actions share the same start time.
    /// </summary>
    public class NPCScheduleManager : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("Debug")]
        public bool DebugMode;

        [Header("Schedule (populated in Inspector or at runtime)")]
        public List<NPCAction> ActionList = new List<NPCAction>();

        // ── State ─────────────────────────────────────────────────────────────
        public bool      ScheduleEnabled    { get; private set; }
        public bool      CurfewModeEnabled  { get; private set; }
        public NPCAction ActiveAction       { get; private set; }
        public NPC       Npc               { get; private set; }

        // ── Private ───────────────────────────────────────────────────────────
        private int          _lastProcessedTime = -1;
        private List<NPCAction> _sorted = new List<NPCAction>();

        // ── Unity ─────────────────────────────────────────────────────────────
        private void Awake()
        {
            Npc = GetComponent<NPC>();
        }

        private void Start()
        {
            InitializeActions();
            EnableSchedule();

            if (GameClock.InstanceExists)
                GameClock.Instance.onMinuteChanged += OnMinutePassed;
        }

        private void OnDestroy()
        {
            if (GameClock.InstanceExists)
                GameClock.Instance.onMinuteChanged -= OnMinutePassed;
        }

        // ── Public API ────────────────────────────────────────────────────────
        public void EnableSchedule()
        {
            ScheduleEnabled     = true;
            _lastProcessedTime  = -1;
            EnforceState(initial: true);
        }

        public void DisableSchedule()
        {
            ScheduleEnabled = false;
            ActiveAction?.Interrupt();
            ActiveAction = null;
        }

        public void AddAction(NPCAction action)
        {
            action.Initialize(Npc, this);
            ActionList.Add(action);
            SortActions();
        }

        public void RemoveAction(NPCAction action)
        {
            if (ActiveAction == action)
            {
                action.Interrupt();
                ActiveAction = null;
            }
            ActionList.Remove(action);
        }

        public void InterruptCurrentAction()
        {
            ActiveAction?.Interrupt();
            ActiveAction = null;
        }

        public void SetCurfewModeEnabled(bool enabled)
        {
            CurfewModeEnabled = enabled;
            if (enabled) OnCurfewEnabled(); else OnCurfewDisabled();
        }

        // ── Private ───────────────────────────────────────────────────────────
        public void InitializeActions()
        {
            foreach (var action in ActionList)
                action.Initialize(Npc, this);
            SortActions();
        }

        private void SortActions()
        {
            _sorted = new List<NPCAction>(ActionList);
            _sorted.Sort((a, b) =>
            {
                int tComp = a.StartTime.CompareTo(b.StartTime);
                return tComp != 0 ? tComp : b.Priority.CompareTo(a.Priority);
            });
        }

        private void OnMinutePassed(int currentTime)
        {
            if (!ScheduleEnabled) return;

            ActiveAction?.MinPassed();
            ActiveAction?.ActiveUpdate();

            // Check if active action should end
            if (ActiveAction != null && currentTime >= ActiveAction.GetEndTime())
            {
                ActiveAction.End();
                ActiveAction = null;
            }

            // Find and start new actions
            List<NPCAction> candidates = GetActionsStartingAt(currentTime);
            foreach (var action in candidates)
            {
                if (!action.ShouldStart()) continue;
                StartAction(action);
                break; // only start one per minute
            }

            _lastProcessedTime = currentTime;
        }

        private List<NPCAction> GetActionsStartingAt(int time)
        {
            var result = new List<NPCAction>();
            foreach (var a in _sorted)
                if (a.StartTime == time) result.Add(a);
            return result;
        }

        /// <summary>
        /// Called on schedule enable or on day wrap — finds which action should
        /// be active right now (handles loading a save mid-day).
        /// </summary>
        private void EnforceState(bool initial = false)
        {
            if (!ScheduleEnabled || !GameClock.InstanceExists) return;

            int now = GameClock.Instance.CurrentTime;

            NPCAction best = null;
            foreach (var action in _sorted)
            {
                if (action.StartTime > now) continue;
                if (now >= action.GetEndTime()) continue;
                if (best == null || action.Priority > best.Priority)
                    best = action;
            }

            if (best != null && best != ActiveAction)
            {
                ActiveAction?.Interrupt();
                StartAction(best);
                if (initial) best.JumpTo();  // no "walk to" animation on initial load
            }
        }

        private void StartAction(NPCAction action)
        {
            ActiveAction?.End();
            ActiveAction = action;
            action.Start_Internal();
            if (DebugMode) Debug.Log($"[{Npc?.FirstName}] Started action: {action.GetName()}");
        }

        protected virtual void OnCurfewEnabled()  { }
        protected virtual void OnCurfewDisabled() { }
    }
}
```

---

### `NPCActions.cs`

```csharp
using UnityEngine;

namespace NPCSystem
{
    /// <summary>
    /// High-level action dispatcher. External systems call these methods to trigger
    /// NPC reactions without knowing which specific behaviour handles them.
    /// </summary>
    public class NPCActions : MonoBehaviour
    {
        private NPC _npc;

        private void Awake() => _npc = GetComponent<NPC>();

        public void Cower()
        {
            _npc.Behaviour.EnableBehaviour<CoweringBehaviour>();
        }

        public void Flee(Transform from)
        {
            var flee = _npc.Behaviour.GetBehaviour<FleeBehaviour>();
            if (flee != null) flee.SetThreat(from);
            _npc.Behaviour.EnableBehaviour<FleeBehaviour>();
        }

        public void CallPolice(Transform perpetrator)
        {
            var cb = _npc.Behaviour.GetBehaviour<CallPoliceBehaviour>();
            if (cb != null) cb.SetPerpetrator(perpetrator);
            _npc.Behaviour.EnableBehaviour<CallPoliceBehaviour>();
        }

        public void EnterCombat(Transform target)
        {
            var combat = _npc.Behaviour.GetBehaviour<CombatBehaviour>();
            if (combat != null) combat.SetTarget(target);
            _npc.Behaviour.EnableBehaviour<CombatBehaviour>();
        }

        public void FacePlayer(Transform player)
        {
            Vector3 dir = (player.position - _npc.transform.position);
            dir.y = 0f;
            if (dir != Vector3.zero) _npc.Movement.FaceDirection(dir.normalized);
        }

        public void TriggerRagdoll()
        {
            _npc.Behaviour.EnableBehaviour<RagdollBehaviour>();
        }
    }
}
```

---

### `NPCResponses.cs`

```csharp
using UnityEngine;

namespace NPCSystem
{
    /// <summary>
    /// Base class for NPC response logic. Subscribes to NPCAwareness events and
    /// translates them into NPCActions calls. Subclass per NPC type for type-specific
    /// responses (civilian panics, police engages, employee keeps working).
    /// </summary>
    public class NPCResponses : MonoBehaviour
    {
        protected NPC         _npc;
        protected NPCActions  _actions;
        protected NPCAwareness _awareness;

        protected virtual void Awake()
        {
            _npc       = GetComponent<NPC>();
            _actions   = GetComponent<NPCActions>();
            _awareness = GetComponent<NPCAwareness>();
        }

        protected virtual void Start()
        {
            _awareness.onPlayerSeen.AddListener(OnPlayerSeen);
            _awareness.onPlayerLost.AddListener(OnPlayerLost);
            _awareness.onNoticedCrime.AddListener(OnCrimeWitnessed);
            _awareness.onGunshotHeard.AddListener(OnGunshotHeard);
            _awareness.onExplosionHeard.AddListener(OnExplosionHeard);
        }

        protected virtual void OnPlayerSeen(Transform player)   { }
        protected virtual void OnPlayerLost()                    { }
        protected virtual void OnCrimeWitnessed(Transform p)     { }
        protected virtual void OnGunshotHeard()                  { }
        protected virtual void OnExplosionHeard()                 { }
    }
}
```

---

## 5. Behaviour System — Base

### `Behaviour.cs`

```csharp
using System;
using UnityEngine;
using UnityEngine.Events;

namespace NPCSystem
{
    /// <summary>
    /// Base class for all NPC behaviours. Attached as MonoBehaviour components on the NPC prefab.
    /// NPCBehaviour maintains a priority-sorted stack and activates the highest-priority
    /// enabled behaviour each frame.
    ///
    /// Lifecycle:
    ///   Enable() / Disable()          — behaviour is eligible to run
    ///   Activate() / Deactivate()     — behaviour is the current active one
    ///   Pause() / Resume()            — temporary suspension (e.g. for dialogue)
    ///   BehaviourUpdate()             — called each frame while active
    ///   BehaviourLateUpdate()         — called each LateUpdate while active
    ///   OnActiveTick()                — called on game clock tick while active
    /// </summary>
    public abstract class Behaviour : MonoBehaviour
    {
        public const int MAX_CONSECUTIVE_PATHING_FAILURES = 5;

        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("Behaviour Settings")]
        public string BehaviourName = "Unnamed Behaviour";
        [Tooltip("Higher number takes priority over lower.")]
        public int Priority = 10;
        public bool EnabledOnAwake;

        [Header("Events (optional Inspector wiring)")]
        public UnityEvent onEnable;
        public UnityEvent onDisable;
        public UnityEvent onBegin;
        public UnityEvent onEnd;

        // ── State ─────────────────────────────────────────────────────────────
        public bool Enabled   { get; protected set; }
        public bool IsActive  { get; private set; }
        public bool IsPaused  { get; private set; }
        public int  BehaviourIndex { get; set; }

        // ── References ────────────────────────────────────────────────────────
        public NPCBehaviour BehaviourManager { get; private set; }
        public NPC          Npc              { get; private set; }

        protected NPCMovement  Movement  => Npc?.Movement;
        protected NPCHealth    Health    => Npc?.Health;
        protected NPCAwareness Awareness => Npc?.Awareness;
        protected NPCAnimation Anim      => Npc?.Animation;

        protected int _consecutivePathingFailures;

        // ── Unity ─────────────────────────────────────────────────────────────
        protected virtual void Awake()
        {
            BehaviourManager = GetComponent<NPCBehaviour>();
            Npc              = GetComponent<NPC>();
        }

        protected virtual void Start()
        {
            if (EnabledOnAwake) Enable();
        }

        // ── Enable / Disable ──────────────────────────────────────────────────
        public virtual void Enable()
        {
            if (Enabled) return;
            Enabled = true;
            onEnable?.Invoke();
        }

        public virtual void Disable()
        {
            if (!Enabled) return;
            if (IsActive) Deactivate();
            Enabled = false;
            onDisable?.Invoke();
        }

        // ── Activate / Deactivate ─────────────────────────────────────────────
        public virtual void Activate()
        {
            IsActive = true;
            IsPaused = false;
            _consecutivePathingFailures = 0;
            onBegin?.Invoke();
        }

        public virtual void Deactivate()
        {
            if (!IsActive) return;
            IsActive = false;
            IsPaused = false;
            onEnd?.Invoke();
        }

        // ── Pause / Resume ────────────────────────────────────────────────────
        public virtual void Pause()  => IsPaused = true;
        public virtual void Resume() => IsPaused = false;

        // ── Frame Updates (called by NPCBehaviour) ────────────────────────────
        public virtual void BehaviourUpdate()     { }
        public virtual void BehaviourLateUpdate() { }
        public virtual void OnActiveTick()        { }

        // ── Navigation Helper ─────────────────────────────────────────────────
        protected void SetDestination(Vector3 pos, bool teleportOnFail = false,
                                      float threshold = 0.5f, bool run = false)
        {
            Movement?.WalkTo(pos, result =>
            {
                if (result == NPCMovement.WalkResult.Failed)
                {
                    _consecutivePathingFailures++;
                    if (teleportOnFail && _consecutivePathingFailures >= MAX_CONSECUTIVE_PATHING_FAILURES)
                        Movement.Teleport(pos);
                    OnPathFailed(result);
                }
                else
                {
                    _consecutivePathingFailures = 0;
                    OnPathCompleted(result);
                }
            }, threshold, run);
        }

        protected virtual void OnPathCompleted(NPCMovement.WalkResult result) { }
        protected virtual void OnPathFailed(NPCMovement.WalkResult result)    { }
    }
}
```

---

### `NPCBehaviour.cs`

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace NPCSystem
{
    /// <summary>
    /// Priority-sorted behaviour stack manager.
    ///
    /// Each frame, evaluates all enabled behaviours and activates the one with
    /// the highest priority. If the active behaviour changes, the old one is
    /// Deactivated and the new one is Activated.
    ///
    /// This replaces a state machine: add behaviours to the GameObject, set their
    /// priority, Enable() them when relevant — the stack handles the rest.
    /// </summary>
    public class NPCBehaviour : MonoBehaviour
    {
        [Header("Debug")]
        public bool DebugMode;

        // ── State ─────────────────────────────────────────────────────────────
        public Behaviour     ActiveBehaviour   { get; private set; }
        public NPC           Npc               { get; private set; }

        [SerializeField]
        private List<Behaviour> _behaviourStack = new List<Behaviour>();

        private List<Behaviour> _enabledBehaviours = new List<Behaviour>();
        private bool            _stackDirty;

        // ── Unity ─────────────────────────────────────────────────────────────
        private void Awake()
        {
            Npc = GetComponent<NPC>();

            // Auto-collect all Behaviour components on this GameObject
            var behaviours = GetComponents<Behaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                behaviours[i].BehaviourIndex = i;
                _behaviourStack.Add(behaviours[i]);
            }

            SortBehaviourStack();
        }

        private void Update()
        {
            if (ActiveBehaviour != null && ActiveBehaviour.IsActive && !ActiveBehaviour.IsPaused)
                ActiveBehaviour.BehaviourUpdate();

            EvaluateStack();
        }

        private void LateUpdate()
        {
            if (ActiveBehaviour != null && ActiveBehaviour.IsActive && !ActiveBehaviour.IsPaused)
                ActiveBehaviour.BehaviourLateUpdate();
        }

        // ── Stack Evaluation ──────────────────────────────────────────────────
        private void EvaluateStack()
        {
            Behaviour best = GetHighestEnabledBehaviour();

            if (best == ActiveBehaviour) return;

            // Deactivate current
            if (ActiveBehaviour != null)
            {
                ActiveBehaviour.Deactivate();
                if (DebugMode) Debug.Log($"[{Npc?.FirstName}] Deactivated: {ActiveBehaviour.BehaviourName}");
            }

            ActiveBehaviour = best;

            // Activate new
            if (ActiveBehaviour != null)
            {
                ActiveBehaviour.Activate();
                if (DebugMode) Debug.Log($"[{Npc?.FirstName}] Activated: {ActiveBehaviour.BehaviourName}");
            }
        }

        private Behaviour GetHighestEnabledBehaviour()
        {
            Behaviour best = null;
            foreach (var b in _behaviourStack)
            {
                if (!b.Enabled) continue;
                if (best == null || b.Priority > best.Priority)
                    best = b;
            }
            return best;
        }

        public void SortBehaviourStack()
        {
            _behaviourStack.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        }

        // ── Public API ────────────────────────────────────────────────────────
        public T GetBehaviour<T>() where T : Behaviour
        {
            foreach (var b in _behaviourStack)
                if (b is T tb) return tb;
            return null;
        }

        public void EnableBehaviour<T>() where T : Behaviour
        {
            var b = GetBehaviour<T>();
            b?.Enable();
        }

        public void DisableBehaviour<T>() where T : Behaviour
        {
            var b = GetBehaviour<T>();
            b?.Disable();
        }

        /// <summary>
        /// Bypasses normal priority evaluation and immediately activates this behaviour.
        /// Used for critical states like death that should never be interrupted.
        /// </summary>
        public void ForceActivateBehaviour<T>() where T : Behaviour
        {
            var b = GetBehaviour<T>();
            if (b == null) return;

            // Disable everything else except this one
            foreach (var behaviour in _behaviourStack)
                if (behaviour != b && behaviour.IsActive)
                    behaviour.Deactivate();

            b.Enable();
            b.Activate();
            ActiveBehaviour = b;
        }
    }
}
```

---

## 6. Concrete Behaviours — Idle & Navigation

### `IdleBehaviour.cs`

```csharp
using UnityEngine;

namespace NPCSystem
{
    /// <summary>Priority 1 — Default fallback. NPC stands and plays occasional idle animations.</summary>
    public class IdleBehaviour : Behaviour
    {
        [Header("Idle Settings")]
        public float MinIdleInterval = 8f;
        public float MaxIdleInterval = 20f;

        private float _nextIdleTime;

        protected override void Start()
        {
            BehaviourName = "Idle";
            Priority      = 1;
            EnabledOnAwake = true;
            base.Start();
        }

        public override void Activate()
        {
            base.Activate();
            Movement?.Stop();
            ScheduleNextIdle();
        }

        public override void BehaviourUpdate()
        {
            if (Time.time >= _nextIdleTime)
            {
                Anim?.TriggerIdle();
                ScheduleNextIdle();
            }
        }

        private void ScheduleNextIdle()
            => _nextIdleTime = Time.time + Random.Range(MinIdleInterval, MaxIdleInterval);
    }
}
```

---

### `WanderBehaviour.cs`

```csharp
using UnityEngine;
using UnityEngine.AI;

namespace NPCSystem
{
    /// <summary>Priority 10 — Wanders randomly within a radius of the spawn origin.</summary>
    public class WanderBehaviour : Behaviour
    {
        [Header("Wander Settings")]
        public float WanderRadius  = 10f;
        public float MinWaitTime   = 2f;
        public float MaxWaitTime   = 6f;

        private Vector3 _origin;
        private float   _waitTimer;
        private bool    _isWaiting;

        protected override void Start()
        {
            BehaviourName = "Wander";
            Priority      = 10;
            base.Start();
        }

        public override void Activate()
        {
            base.Activate();
            _origin   = transform.position;
            _isWaiting = false;
            PickNewDestination();
        }

        public override void Deactivate()
        {
            Movement?.Stop();
            base.Deactivate();
        }

        public override void BehaviourUpdate()
        {
            if (_isWaiting)
            {
                _waitTimer -= Time.deltaTime;
                if (_waitTimer <= 0f)
                {
                    _isWaiting = false;
                    PickNewDestination();
                }
            }
        }

        protected override void OnPathCompleted(NPCMovement.WalkResult result)
        {
            // Arrived — wait before picking next destination
            _isWaiting = true;
            _waitTimer = Random.Range(MinWaitTime, MaxWaitTime);
        }

        protected override void OnPathFailed(NPCMovement.WalkResult result)
        {
            // Retry with a new random point
            _isWaiting = true;
            _waitTimer = 1f;
        }

        private void PickNewDestination()
        {
            for (int i = 0; i < 8; i++)
            {
                Vector2 rand   = Random.insideUnitCircle * WanderRadius;
                Vector3 target = _origin + new Vector3(rand.x, 0f, rand.y);

                if (NavMesh.SamplePosition(target, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                {
                    SetDestination(hit.position);
                    return;
                }
            }

            // Couldn't find a point — wait and retry
            _isWaiting = true;
            _waitTimer = 2f;
        }
    }
}
```

---

### `PatrolBehaviour.cs`

```csharp
using UnityEngine;

namespace NPCSystem
{
    /// <summary>
    /// Priority 20 — Follows a FootPatrolRoute waypoint list.
    /// Supports Loop and PingPong modes. Optional PatrolGroup coordination.
    /// </summary>
    public class PatrolBehaviour : Behaviour
    {
        public enum PatrolMode { Loop, PingPong, Once }

        [Header("Patrol Settings")]
        public FootPatrolRoute Route;
        public PatrolMode      Mode           = PatrolMode.Loop;
        public float           WaypointWait   = 1.5f;

        private int   _waypointIndex;
        private int   _direction = 1;
        private bool  _waiting;
        private float _waitTimer;
        private PatrolGroup _group;

        protected override void Start()
        {
            BehaviourName = "Patrol";
            Priority      = 20;
            base.Start();
        }

        public override void Activate()
        {
            base.Activate();
            _waypointIndex = 0;
            MoveToCurrentWaypoint();
        }

        public override void Deactivate()
        {
            Movement?.Stop();
            base.Deactivate();
        }

        public override void BehaviourUpdate()
        {
            if (!_waiting) return;

            _waitTimer -= Time.deltaTime;
            if (_waitTimer <= 0f)
            {
                _waiting = false;
                AdvanceWaypoint();
            }
        }

        public void SetGroup(PatrolGroup group) => _group = group;

        protected override void OnPathCompleted(NPCMovement.WalkResult result)
        {
            _group?.OnMemberArrived(Npc);

            if (WaypointWait > 0f)
            {
                _waiting   = true;
                _waitTimer = WaypointWait;
            }
            else
            {
                AdvanceWaypoint();
            }
        }

        protected override void OnPathFailed(NPCMovement.WalkResult result)
        {
            AdvanceWaypoint(); // Skip failed waypoint
        }

        private void MoveToCurrentWaypoint()
        {
            if (Route == null || Route.Waypoints.Length == 0) return;
            Transform wp = Route.Waypoints[_waypointIndex];
            if (wp != null) SetDestination(wp.position, teleportOnFail: false, threshold: 0.8f);
        }

        private void AdvanceWaypoint()
        {
            if (Route == null) return;
            int count = Route.Waypoints.Length;

            switch (Mode)
            {
                case PatrolMode.Loop:
                    _waypointIndex = (_waypointIndex + 1) % count;
                    break;
                case PatrolMode.PingPong:
                    _waypointIndex += _direction;
                    if (_waypointIndex >= count - 1 || _waypointIndex <= 0)
                        _direction *= -1;
                    _waypointIndex = Mathf.Clamp(_waypointIndex, 0, count - 1);
                    break;
                case PatrolMode.Once:
                    _waypointIndex = Mathf.Min(_waypointIndex + 1, count - 1);
                    break;
            }

            MoveToCurrentWaypoint();
        }
    }
}
```

---

### `StationaryBehaviour.cs`

```csharp
using UnityEngine;

namespace NPCSystem
{
    /// <summary>
    /// Priority 30 — NPC walks to a fixed station point and stays there,
    /// optionally facing a look target (e.g. a workstation or counter).
    /// </summary>
    public class StationaryBehaviour : Behaviour
    {
        [Header("Station Settings")]
        public Transform StationPoint;
        public Transform LookAtTarget;
        public float     LookAtLerpSpeed = 2f;

        private bool _atStation;

        protected override void Start()
        {
            BehaviourName = "Stationary";
            Priority      = 30;
            base.Start();
        }

        public override void Activate()
        {
            base.Activate();
            _atStation = false;
            if (StationPoint != null)
                SetDestination(StationPoint.position, teleportOnFail: true, threshold: 0.4f);
        }

        public override void BehaviourUpdate()
        {
            if (!_atStation || LookAtTarget == null) return;

            Vector3 dir = (LookAtTarget.position - transform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(dir),
                    Time.deltaTime * LookAtLerpSpeed);
        }

        public override void Deactivate()
        {
            _atStation = false;
            base.Deactivate();
        }

        protected override void OnPathCompleted(NPCMovement.WalkResult result)
        {
            _atStation = true;
        }

        public void SetStation(Transform point, Transform lookAt = null)
        {
            StationPoint = point;
            LookAtTarget = lookAt;
            if (IsActive)
            {
                _atStation = false;
                SetDestination(StationPoint.position, teleportOnFail: true, threshold: 0.4f);
            }
        }
    }
}
```

---

### `SitBehaviour.cs`

```csharp
using UnityEngine;

namespace NPCSystem
{
    /// <summary>Priority 25 — NPC walks to a seat and plays sitting animation.</summary>
    public class SitBehaviour : Behaviour
    {
        [Header("Sit Settings")]
        public Transform SeatTransform;
        public float     SeatArrivalThreshold = 0.4f;

        private bool _seated;

        protected override void Start()
        {
            BehaviourName = "Sit";
            Priority      = 25;
            base.Start();
        }

        public override void Activate()
        {
            base.Activate();
            _seated = false;
            if (SeatTransform != null)
                SetDestination(SeatTransform.position, teleportOnFail: true,
                               threshold: SeatArrivalThreshold);
        }

        public override void Deactivate()
        {
            if (_seated)
            {
                Anim?.SetSitting(false);
                Movement?.SetAgentEnabled(true);
            }
            _seated = false;
            base.Deactivate();
        }

        protected override void OnPathCompleted(NPCMovement.WalkResult result)
        {
            _seated = true;
            // Snap to seat position/rotation
            if (SeatTransform != null)
            {
                transform.position = SeatTransform.position;
                transform.rotation = SeatTransform.rotation;
            }
            Movement?.SetAgentEnabled(false); // disable NavMesh while seated
            Anim?.SetSitting(true);
        }

        public void SetSeat(Transform seat)
        {
            SeatTransform = seat;
            if (IsActive && !_seated)
                SetDestination(seat.position, teleportOnFail: true, threshold: SeatArrivalThreshold);
        }
    }
}
```

---

### `FollowBehaviour.cs`

```csharp
using UnityEngine;

namespace NPCSystem
{
    /// <summary>Priority 40 — Follows a target Transform, maintaining a desired distance.</summary>
    public class FollowBehaviour : Behaviour
    {
        [Header("Follow Settings")]
        public Transform Target;
        public float     StopDistance   = 1.5f;
        public float     StartDistance  = 3.0f;   // re-path threshold
        public bool      RunWhenFar     = true;
        public float     RunThreshold   = 8f;

        private float _repathTimer;
        private float _repathInterval = 0.5f;

        protected override void Start()
        {
            BehaviourName = "Follow";
            Priority      = 40;
            base.Start();
        }

        public override void Deactivate()
        {
            Movement?.Stop();
            base.Deactivate();
        }

        public override void BehaviourUpdate()
        {
            if (Target == null) return;

            _repathTimer += Time.deltaTime;
            if (_repathTimer < _repathInterval) return;
            _repathTimer = 0f;

            float dist = Vector3.Distance(transform.position, Target.position);
            if (dist <= StopDistance) { Movement?.Stop(); return; }
            if (dist > StartDistance)
            {
                bool run = RunWhenFar && dist > RunThreshold;
                SetDestination(Target.position, threshold: StopDistance, run: run);
            }
        }

        public void SetTarget(Transform target) => Target = target;
    }
}
```

---

## 7. Concrete Behaviours — Social

### `GenericDialogueBehaviour.cs`

```csharp
using UnityEngine;

namespace NPCSystem
{
    /// <summary>
    /// Priority 50 — NPC faces and holds a conversation with the player.
    /// Interrupts most navigation behaviours. Disables movement while talking.
    /// </summary>
    public class GenericDialogueBehaviour : Behaviour
    {
        public Transform CurrentConversant { get; private set; }

        protected override void Start()
        {
            BehaviourName = "GenericDialogue";
            Priority      = 50;
            base.Start();
        }

        public override void Activate()
        {
            base.Activate();
            Movement?.Stop();
        }

        public override void BehaviourUpdate()
        {
            if (CurrentConversant == null) return;

            // Always face the conversant
            Vector3 dir = (CurrentConversant.position - transform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(dir),
                    Time.deltaTime * 5f);
            }
        }

        public void BeginConversation(Transform with)
        {
            CurrentConversant = with;
            Enable();
        }

        public void EndConversation()
        {
            CurrentConversant = null;
            Disable();
        }
    }
}
```

---

### `RequestProductBehaviour.cs`

```csharp
using System;
using UnityEngine;

namespace NPCSystem
{
    /// <summary>
    /// Priority 200 — Customer NPC approaches the player to request/buy a product.
    /// Walks to the player, enters deal interaction range, fires onDealRequested.
    /// </summary>
    public class RequestProductBehaviour : Behaviour
    {
        [Header("Deal Settings")]
        public float DealRange    = 2.0f;
        public float ApproachSpeed = 1.4f;

        public event Action onDealRequested;   // fires when in range
        public event Action onDealComplete;

        private Transform _playerTarget;
        private bool      _inRange;
        private float     _rangeCheckTimer;

        protected override void Start()
        {
            BehaviourName = "RequestProduct";
            Priority      = 200;
            base.Start();
        }

        public override void Activate()
        {
            base.Activate();
            _inRange = false;

            // Find player in scene
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                _playerTarget = player.transform;
                SetDestination(_playerTarget.position, threshold: DealRange, run: false);
            }
            else
                Disable();
        }

        public override void Deactivate()
        {
            _inRange = false;
            _playerTarget = null;
            Movement?.Stop();
            base.Deactivate();
        }

        public override void BehaviourUpdate()
        {
            if (_inRange || _playerTarget == null) return;

            // Re-path toward player as they move
            _rangeCheckTimer += Time.deltaTime;
            if (_rangeCheckTimer > 0.3f)
            {
                _rangeCheckTimer = 0f;
                float dist = Vector3.Distance(transform.position, _playerTarget.position);
                if (dist <= DealRange)
                    OnArrivedAtPlayer();
                else
                    SetDestination(_playerTarget.position, threshold: DealRange);
            }
        }

        private void OnArrivedAtPlayer()
        {
            _inRange = true;
            Movement?.Stop();
            Anim?.TriggerIdle();
            Movement?.FaceDirection((_playerTarget.position - transform.position).normalized);
            onDealRequested?.Invoke();
        }

        public void CompleteDeal()
        {
            onDealComplete?.Invoke();
            Disable();
        }
    }
}
```

---

### `ConsumeProductBehaviour.cs`

```csharp
using System;
using System.Collections;
using UnityEngine;

namespace NPCSystem
{
    /// <summary>
    /// Priority 300 — NPC consumes a product (drug, food, etc.).
    /// Plays consume animation, waits for effect duration, then fires onConsumed.
    /// </summary>
    public class ConsumeProductBehaviour : Behaviour
    {
        [Header("Consume Settings")]
        public float ConsumeDuration = 3f;

        public event Action onConsumed;
        public event Action onEffectsApplied;

        private Coroutine _consumeRoutine;

        protected override void Start()
        {
            BehaviourName = "ConsumeProduct";
            Priority      = 300;
            base.Start();
        }

        public override void Activate()
        {
            base.Activate();
            Movement?.Stop();
            Anim?.TriggerConsume();
            _consumeRoutine = StartCoroutine(ConsumeRoutine());
        }

        public override void Deactivate()
        {
            if (_consumeRoutine != null) StopCoroutine(_consumeRoutine);
            base.Deactivate();
        }

        public void TriggerConsume()
        {
            Enable();
        }

        private IEnumerator ConsumeRoutine()
        {
            yield return new WaitForSeconds(ConsumeDuration);
            onEffectsApplied?.Invoke();
            yield return new WaitForSeconds(1f);
            onConsumed?.Invoke();
            Disable();
        }
    }
}
```

---

## 8. Concrete Behaviours — Threat Response

### `CoweringBehaviour.cs`

```csharp
using System.Collections;
using UnityEngine;

namespace NPCSystem
{
    /// <summary>
    /// Priority 500 — NPC is frightened. Backs away from threat, plays cower animation.
    /// Disables after duration unless re-triggered.
    /// </summary>
    public class CoweringBehaviour : Behaviour
    {
        [Header("Cowering Settings")]
        public float CowerDuration = 15f;
        public float BackAwayDist  = 3f;

        private Transform  _threat;
        private float      _timer;
        private Coroutine  _cowerRoutine;

        protected override void Start()
        {
            BehaviourName = "Cowering";
            Priority      = 500;
            base.Start();
        }

        public override void Activate()
        {
            base.Activate();
            _timer = CowerDuration;
            Anim?.TriggerCower();

            if (_threat != null)
                BackAway();

            _cowerRoutine = StartCoroutine(CowerTimer());
        }

        public override void Deactivate()
        {
            if (_cowerRoutine != null) StopCoroutine(_cowerRoutine);
            Movement?.Stop();
            base.Deactivate();
        }

        public override void BehaviourUpdate()
        {
            // Always face the threat
            if (_threat == null) return;
            Vector3 dir = (_threat.position - transform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(
                    transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 3f);
        }

        public void SetThreat(Transform threat)
        {
            _threat = threat;
            _timer  = CowerDuration; // refresh duration on re-trigger
        }

        private void BackAway()
        {
            if (_threat == null) return;
            Vector3 away = (transform.position - _threat.position).normalized;
            Vector3 dest = transform.position + away * BackAwayDist;
            SetDestination(dest);
        }

        private IEnumerator CowerTimer()
        {
            yield return new WaitForSeconds(CowerDuration);
            Disable();
        }
    }
}
```

---

### `FleeBehaviour.cs`

```csharp
using UnityEngine;
using UnityEngine.AI;

namespace NPCSystem
{
    /// <summary>
    /// Priority 700 — NPC runs away from a threat. Samples NavMesh points in the
    /// opposite direction and picks the farthest reachable one.
    /// </summary>
    public class FleeBehaviour : Behaviour
    {
        [Header("Flee Settings")]
        public float FleeDuration    = 20f;
        public float FleeRadius      = 15f;
        public int   SampleAttempts  = 12;

        private Transform _threat;
        private float     _fleeTimer;
        private bool      _reached;

        protected override void Start()
        {
            BehaviourName = "Flee";
            Priority      = 700;
            base.Start();
        }

        public override void Activate()
        {
            base.Activate();
            _fleeTimer = FleeDuration;
            _reached   = false;
            Movement?.SetRunning(true);
            FindFleeDestination();
        }

        public override void Deactivate()
        {
            Movement?.SetRunning(false);
            Movement?.Stop();
            base.Deactivate();
        }

        public override void BehaviourUpdate()
        {
            _fleeTimer -= Time.deltaTime;
            if (_fleeTimer <= 0f)
            {
                Disable();
                return;
            }

            if (_reached)
            {
                _reached = false;
                FindFleeDestination();
            }
        }

        public void SetThreat(Transform threat)
        {
            _threat = threat;
            if (IsActive) FindFleeDestination();
        }

        protected override void OnPathCompleted(NPCMovement.WalkResult result) => _reached = true;
        protected override void OnPathFailed(NPCMovement.WalkResult result)    => _reached = true;

        private void FindFleeDestination()
        {
            if (_threat == null) { Disable(); return; }

            Vector3 awayDir = (transform.position - _threat.position).normalized;
            Vector3 best    = transform.position;
            float   bestDist = 0f;

            for (int i = 0; i < SampleAttempts; i++)
            {
                // Bias samples away from threat
                Vector2 rand    = Random.insideUnitCircle;
                Vector3 dir     = (awayDir + new Vector3(rand.x * 0.4f, 0f, rand.y * 0.4f)).normalized;
                Vector3 attempt = transform.position + dir * FleeRadius;

                if (!NavMesh.SamplePosition(attempt, out NavMeshHit hit, 3f, NavMesh.AllAreas))
                    continue;

                float distFromThreat = Vector3.Distance(hit.position, _threat.position);
                if (distFromThreat > bestDist)
                {
                    bestDist = distFromThreat;
                    best     = hit.position;
                }
            }

            SetDestination(best, teleportOnFail: false, run: true);
        }
    }
}
```

---

### `CombatBehaviour.cs`

```csharp
using System.Collections;
using UnityEngine;

namespace NPCSystem
{
    /// <summary>
    /// Priority 800 — NPC engages in melee combat with a target.
    /// Chases if out of attack range. Flees if health drops below threshold.
    /// </summary>
    public class CombatBehaviour : Behaviour
    {
        [Header("Combat Settings")]
        public float AttackRange       = 1.8f;
        public float AttackCooldown    = 1.5f;
        public float FleeHealthThresh  = 0.2f;   // flee if health < 20%
        public float DamagePerHit      = 15f;
        public float ChaseRadius       = 20f;

        private Transform  _target;
        private float      _attackTimer;
        private bool       _isCombatRunning;

        protected override void Start()
        {
            BehaviourName = "Combat";
            Priority      = 800;
            base.Start();
        }

        public override void Activate()
        {
            base.Activate();
            _isCombatRunning = true;
            Movement?.SetRunning(true);
        }

        public override void Deactivate()
        {
            _isCombatRunning = false;
            Movement?.SetRunning(false);
            Movement?.Stop();
            base.Deactivate();
        }

        public override void BehaviourUpdate()
        {
            if (_target == null) { Disable(); return; }

            // Flee if too hurt
            if (Health != null && Health.NormalizedHealth < FleeHealthThresh)
            {
                var flee = BehaviourManager.GetBehaviour<FleeBehaviour>();
                flee?.SetThreat(_target);
                BehaviourManager.EnableBehaviour<FleeBehaviour>();
                Disable();
                return;
            }

            float dist = Vector3.Distance(transform.position, _target.position);

            // Lost target
            if (dist > ChaseRadius) { Disable(); return; }

            _attackTimer -= Time.deltaTime;

            if (dist <= AttackRange)
            {
                Movement?.Stop();
                // Face target
                Vector3 dir = (_target.position - transform.position); dir.y = 0f;
                if (dir.sqrMagnitude > 0.01f) transform.rotation =
                    Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 8f);

                if (_attackTimer <= 0f)
                {
                    PerformAttack();
                    _attackTimer = AttackCooldown;
                }
            }
            else
            {
                // Chase
                SetDestination(_target.position, threshold: AttackRange - 0.1f, run: true);
            }
        }

        public void SetTarget(Transform target)
        {
            _target      = target;
            _attackTimer = 0f;
        }

        private void PerformAttack()
        {
            Anim?.TriggerAttack();

            // Damage target if it has NPCHealth / Player health
            var targetHealth = _target.GetComponent<NPCHealth>();
            targetHealth?.TakeDamage(DamagePerHit);
        }
    }
}
```

---

### `CallPoliceBehaviour.cs`

```csharp
using System;
using System.Collections;
using UnityEngine;

namespace NPCSystem
{
    /// <summary>
    /// Priority 600 — NPC witnesses a crime, animates phone call, fires event,
    /// then switches to flee.
    /// </summary>
    public class CallPoliceBehaviour : Behaviour
    {
        [Header("Call Police Settings")]
        public float ReactDelay    = 0.5f;
        public float CallDuration  = 4.0f;

        public event Action<Transform> onPoliceCallMade; // passes perpetrator

        private Transform  _perpetrator;
        private Coroutine  _callRoutine;

        protected override void Start()
        {
            BehaviourName = "CallPolice";
            Priority      = 600;
            base.Start();
        }

        public override void Activate()
        {
            base.Activate();
            Movement?.Stop();
            _callRoutine = StartCoroutine(CallRoutine());
        }

        public override void Deactivate()
        {
            if (_callRoutine != null) StopCoroutine(_callRoutine);
            base.Deactivate();
        }

        public void SetPerpetrator(Transform perpetrator) => _perpetrator = perpetrator;

        private IEnumerator CallRoutine()
        {
            // React delay — look scared
            yield return new WaitForSeconds(ReactDelay);

            // Look at perpetrator
            if (_perpetrator != null)
                Movement?.FaceDirection((_perpetrator.position - transform.position).normalized);

            // Phone call animation
            Anim?.TriggerPhoneCall();
            yield return new WaitForSeconds(CallDuration);

            // Police called event
            onPoliceCallMade?.Invoke(_perpetrator);

            // Then flee
            var flee = BehaviourManager.GetBehaviour<FleeBehaviour>();
            flee?.SetThreat(_perpetrator);
            BehaviourManager.EnableBehaviour<FleeBehaviour>();
            Disable();
        }
    }
}
```

---

### `HeavyFlinchBehaviour.cs`

```csharp
using System.Collections;
using UnityEngine;

namespace NPCSystem
{
    /// <summary>Priority 850 — Short stagger on heavy impact before returning to normal.</summary>
    public class HeavyFlinchBehaviour : Behaviour
    {
        public float FlinchDuration = 0.8f;

        protected override void Start()
        {
            BehaviourName = "HeavyFlinch";
            Priority      = 850;
            base.Start();
        }

        public override void Activate()
        {
            base.Activate();
            Movement?.Stop();
            Anim?.TriggerDamage();
            StartCoroutine(FlinchRoutine());
        }

        public void TriggerFlinch() => Enable();

        private IEnumerator FlinchRoutine()
        {
            yield return new WaitForSeconds(FlinchDuration);
            Disable();
        }
    }
}
```

---

### `RagdollBehaviour.cs`

```csharp
using System.Collections;
using UnityEngine;

namespace NPCSystem
{
    /// <summary>
    /// Priority 900 — Activates ragdoll physics. NPC recovers after duration
    /// (unless killed). Requires Rigidbody + Collider components on bones.
    /// </summary>
    public class RagdollBehaviour : Behaviour
    {
        [Header("Ragdoll Settings")]
        public float RecoverDuration = 4f;
        public bool  CanRecover      = true;

        [Header("References")]
        [Tooltip("All bone Rigidbodies that make up the ragdoll.")]
        public Rigidbody[] BoneRigidbodies;
        public Collider[]  BoneColliders;

        private Coroutine _recoverRoutine;

        protected override void Start()
        {
            BehaviourName = "Ragdoll";
            Priority      = 900;
            SetRagdollEnabled(false);
            base.Start();
        }

        public override void Activate()
        {
            base.Activate();
            SetRagdollEnabled(true);
            Movement?.SetAgentEnabled(false);
            Anim?.Animator.enabled = false;

            if (CanRecover)
                _recoverRoutine = StartCoroutine(RecoverRoutine());
        }

        public override void Deactivate()
        {
            if (_recoverRoutine != null) StopCoroutine(_recoverRoutine);
            SetRagdollEnabled(false);
            Movement?.SetAgentEnabled(true);
            if (Anim != null) Anim.Animator.enabled = true;
            Anim?.TriggerGetUp();
            base.Deactivate();
        }

        public void ApplyImpulse(Vector3 force, Vector3 contactPoint)
        {
            if (!IsActive) Enable();
            Rigidbody closest = GetClosestBone(contactPoint);
            closest?.AddForceAtPosition(force, contactPoint, ForceMode.Impulse);
        }

        private IEnumerator RecoverRoutine()
        {
            yield return new WaitForSeconds(RecoverDuration);
            // Snap back to upright near ragdoll position
            Vector3 pos = BoneRigidbodies.Length > 0
                ? BoneRigidbodies[0].position : transform.position;
            Movement?.Teleport(pos);
            Disable();
        }

        private void SetRagdollEnabled(bool enabled)
        {
            foreach (var rb in BoneRigidbodies)
            {
                rb.isKinematic = !enabled;
                rb.useGravity  = enabled;
            }
            foreach (var col in BoneColliders)
                col.enabled = enabled;
        }

        private Rigidbody GetClosestBone(Vector3 point)
        {
            Rigidbody best  = null;
            float     bestD = float.MaxValue;
            foreach (var rb in BoneRigidbodies)
            {
                float d = Vector3.SqrMagnitude(rb.position - point);
                if (d < bestD) { bestD = d; best = rb; }
            }
            return best;
        }
    }
}
```

---

### `UnconsciousBehaviour.cs`

```csharp
using System.Collections;
using UnityEngine;

namespace NPCSystem
{
    /// <summary>
    /// Priority 950 — NPC is knocked out. Plays down animation. Revives automatically
    /// after duration unless killed.
    /// </summary>
    public class UnconsciousBehaviour : Behaviour
    {
        public float ReviveDuration = 30f;

        private Coroutine _reviveRoutine;

        protected override void Start()
        {
            BehaviourName = "Unconscious";
            Priority      = 950;
            base.Start();
        }

        public override void Activate()
        {
            base.Activate();
            Movement?.Stop();
            Movement?.SetAgentEnabled(false);
            Anim?.SetKnockedOut(true);
            _reviveRoutine = StartCoroutine(ReviveRoutine());
        }

        public override void Deactivate()
        {
            if (_reviveRoutine != null) StopCoroutine(_reviveRoutine);
            Movement?.SetAgentEnabled(true);
            Anim?.SetKnockedOut(false);
            Anim?.TriggerGetUp();
            base.Deactivate();
        }

        private IEnumerator ReviveRoutine()
        {
            yield return new WaitForSeconds(ReviveDuration);
            Health?.Revive();
            // NPCHealth.Revive() fires onRevived → NPC.HandleRevived() → DisableBehaviour
        }
    }
}
```

---

### `DeadBehaviour.cs`

```csharp
using UnityEngine;

namespace NPCSystem
{
    /// <summary>
    /// Priority 1000 — Terminal state. NPC is dead. Disables all movement and
    /// awareness. This behaviour never ends unless the NPC is explicitly respawned.
    /// </summary>
    public class DeadBehaviour : Behaviour
    {
        protected override void Start()
        {
            BehaviourName = "Dead";
            Priority      = 1000;
            base.Start();
        }

        public override void Activate()
        {
            base.Activate();
            Movement?.Stop();
            Movement?.SetAgentEnabled(false);
            Awareness?.SetAwarenessActive(false);
            Anim?.SetDead(true);
        }

        /// <summary>Dead behaviour NEVER deactivates unless explicitly respawned.</summary>
        public override void Deactivate()
        {
            Anim?.SetDead(false);
            Movement?.SetAgentEnabled(true);
            Awareness?.SetAwarenessActive(true);
            base.Deactivate();
        }
    }
}
```

---

## 9. Schedule System

### `NPCAction.cs`

```csharp
using System;
using UnityEngine;

namespace NPCSystem
{
    /// <summary>
    /// Abstract base for all scheduled NPC actions. Each action has a StartTime
    /// (in-game minutes) and an end condition. The NPCScheduleManager activates
    /// actions when their start time arrives.
    ///
    /// Subclass for time-range events (NPCEvent) or discrete tasks (NPCSignal).
    /// </summary>
    [Serializable]
    public abstract class NPCAction : MonoBehaviour
    {
        public const int MAX_CONSECUTIVE_PATHING_FAILURES = 5;

        [Header("Schedule")]
        [Tooltip("In-game minutes from midnight (0–1439). 480 = 8:00 AM.")]
        public int StartTime;
        [Tooltip("Higher priority actions run first when multiple share a start time.")]
        public int Priority;

        // ── State ─────────────────────────────────────────────────────────────
        public bool IsActive    { get; protected set; }
        public bool HasStarted  { get; protected set; }

        // ── References (set by NPCScheduleManager) ────────────────────────────
        protected NPC                 _npc;
        protected NPCScheduleManager  _schedule;
        protected int _consecutivePathingFailures;

        // ── Events ────────────────────────────────────────────────────────────
        public event Action onEnded;

        // ── Abstract Interface (must implement) ───────────────────────────────
        public abstract string GetName();
        public abstract string GetTimeDescription();
        public abstract int    GetEndTime();

        // ── Virtual Lifecycle (override as needed) ────────────────────────────
        /// <summary>Called when the action is due to start.</summary>
        public virtual void Started()     { }

        /// <summary>Called one frame after Started() — use to defer initialization.</summary>
        public virtual void LateStarted() { }

        /// <summary>
        /// Called when loading mid-schedule — skip the "walk to start" setup
        /// and jump directly to the active state.
        /// </summary>
        public virtual void JumpTo()      { }

        /// <summary>Called every in-game minute while active.</summary>
        public virtual void MinPassed()   { }

        /// <summary>Called every frame while active (from NPCScheduleManager.Update).</summary>
        public virtual void ActiveUpdate(){ }

        /// <summary>Normal end when time expires.</summary>
        public virtual void End()
        {
            IsActive = false;
            onEnded?.Invoke();
        }

        /// <summary>Forced end (e.g. higher-priority action starts).</summary>
        public virtual void Interrupt()
        {
            if (!IsActive) return;
            IsActive = false;
        }

        /// <summary>Return false to skip this action even when its time arrives.</summary>
        public virtual bool ShouldStart() => true;

        // ── Called by NPCScheduleManager ──────────────────────────────────────
        public void Initialize(NPC npc, NPCScheduleManager schedule)
        {
            _npc      = npc;
            _schedule = schedule;
            OnInitialized();
        }

        protected virtual void OnInitialized() { }

        internal void Start_Internal()
        {
            IsActive   = true;
            HasStarted = true;
            Started();
        }

        // ── Navigation Helper ─────────────────────────────────────────────────
        protected void SetDestination(Vector3 pos, bool teleportOnFail = false,
                                      float threshold = 0.5f, bool run = false)
        {
            _npc?.Movement.WalkTo(pos, result =>
            {
                if (result == NPCMovement.WalkResult.Failed)
                {
                    _consecutivePathingFailures++;
                    if (teleportOnFail && _consecutivePathingFailures >= MAX_CONSECUTIVE_PATHING_FAILURES)
                        _npc.Movement.Teleport(pos);
                    OnPathFailed(result);
                }
                else
                {
                    _consecutivePathingFailures = 0;
                    OnPathCompleted(result);
                }
            }, threshold, run);
        }

        protected virtual void OnPathCompleted(NPCMovement.WalkResult result) { }
        protected virtual void OnPathFailed(NPCMovement.WalkResult result)    { }
    }
}
```

---

### `NPCSignal.cs`

```csharp
using UnityEngine;

namespace NPCSystem
{
    /// <summary>
    /// A one-shot discrete task with a maximum duration.
    /// Signals are the "things an NPC does" between events:
    ///   walk to the store, use the ATM, sit on a bench, etc.
    /// They end either when the task completes or MaxDuration expires.
    /// </summary>
    public abstract class NPCSignal : NPCAction
    {
        [Header("Signal Settings")]
        [Tooltip("Signal ends after this many in-game minutes even if not complete.")]
        public int MaxDuration = 10;

        public bool StartedThisCycle { get; protected set; }

        private int _startedAtTime;

        public override void Started()
        {
            base.Started();
            StartedThisCycle = true;

            if (GameClock.InstanceExists)
                _startedAtTime = GameClock.Instance.CurrentTime;
        }

        public override void MinPassed()
        {
            base.MinPassed();

            if (!GameClock.InstanceExists) return;
            int elapsed = GameClock.Instance.MinutesUntil(_startedAtTime);
            // MinutesUntil gives minutes TO that time; invert for elapsed
            int elapsedMin = MaxDuration - GameClock.Instance.MinutesUntil(_startedAtTime + MaxDuration);

            if (elapsedMin >= MaxDuration)
                End();
        }

        public override int GetEndTime()
        {
            if (!GameClock.InstanceExists) return StartTime + MaxDuration;
            return (StartTime + MaxDuration) % 1440;
        }

        /// <summary>Call this when the signal's task is successfully completed.</summary>
        protected void CompleteSignal()
        {
            StartedThisCycle = false;
            End();
        }
    }
}
```

---

### `NPCEvent.cs`

```csharp
using UnityEngine;

namespace NPCSystem
{
    /// <summary>
    /// A time-range event: has a definite StartTime and EndTime.
    /// Examples: "Work shift 9:00–17:00", "Sleep 23:00–07:00", "Patrol 14:00–16:00".
    /// </summary>
    public abstract class NPCEvent : NPCAction
    {
        [Header("Event Duration")]
        [Tooltip("In-game minutes when this event ends (0–1439).")]
        public int EndTime = 600;

        public override int    GetEndTime()         => EndTime;
        public override string GetTimeDescription() =>
            $"{StartTime / 60:00}:{StartTime % 60:00} – {EndTime / 60:00}:{EndTime % 60:00}";
    }
}
```

---

## 10. Concrete Signals & Events

### `NPCSignal_WalkToLocation.cs`

```csharp
using UnityEngine;

namespace NPCSystem
{
    /// <summary>Walk to a target Transform or fixed position, then end the signal.</summary>
    public class NPCSignal_WalkToLocation : NPCSignal
    {
        [Header("Destination")]
        public Transform TargetTransform;
        public Vector3   TargetPosition;
        public float     ArrivalThreshold = 0.5f;
        public bool      RunToDestination;

        public override string GetName()            => "Walk To Location";
        public override string GetTimeDescription() => $"Walk at {StartTime / 60:00}:{StartTime % 60:00}";

        public override void Started()
        {
            base.Started();
            Vector3 dest = TargetTransform != null ? TargetTransform.position : TargetPosition;
            SetDestination(dest, teleportOnFail: true,
                           threshold: ArrivalThreshold, run: RunToDestination);
        }

        public override void JumpTo()
        {
            // Already at destination when loaded mid-schedule
            CompleteSignal();
        }

        protected override void OnPathCompleted(NPCMovement.WalkResult result) => CompleteSignal();
        protected override void OnPathFailed(NPCMovement.WalkResult result)     => CompleteSignal();
    }
}
```

---

### `NPCSignal_WaitAtLocation.cs`

```csharp
using System.Collections;
using UnityEngine;

namespace NPCSystem
{
    /// <summary>Walk to a location, wait there for WaitDuration game-minutes, then end.</summary>
    public class NPCSignal_WaitAtLocation : NPCSignal
    {
        [Header("Wait Settings")]
        public Transform WaitPoint;
        public int       WaitDuration = 5;   // in-game minutes

        private int       _arrivedAtTime;
        private bool      _waiting;

        public override string GetName()            => "Wait At Location";
        public override string GetTimeDescription() => $"Wait {WaitDuration}min at {StartTime / 60:00}:{StartTime % 60:00}";

        public override void Started()
        {
            base.Started();
            if (WaitPoint != null)
                SetDestination(WaitPoint.position, teleportOnFail: true);
            else
                BeginWait();
        }

        public override void MinPassed()
        {
            if (!_waiting) return;
            int elapsed = GameClock.Instance.CurrentTime - _arrivedAtTime;
            if (elapsed >= WaitDuration) CompleteSignal();
        }

        protected override void OnPathCompleted(NPCMovement.WalkResult result) => BeginWait();
        protected override void OnPathFailed(NPCMovement.WalkResult result)    => BeginWait();

        private void BeginWait()
        {
            _waiting      = true;
            _arrivedAtTime = GameClock.InstanceExists ? GameClock.Instance.CurrentTime : 0;
        }
    }
}
```

---

### `NPCSignal_Sit.cs`

```csharp
using UnityEngine;

namespace NPCSystem
{
    /// <summary>Walk to a seat, sit for the remainder of the signal's MaxDuration.</summary>
    public class NPCSignal_Sit : NPCSignal
    {
        [Header("Sit Settings")]
        public Transform SeatTransform;

        public override string GetName()            => "Sit";
        public override string GetTimeDescription() => $"Sit at {StartTime / 60:00}:{StartTime % 60:00}";

        public override void Started()
        {
            base.Started();
            if (SeatTransform != null)
            {
                var sit = _npc?.Behaviour.GetBehaviour<SitBehaviour>();
                sit?.SetSeat(SeatTransform);
                _npc?.Behaviour.EnableBehaviour<SitBehaviour>();
                SetDestination(SeatTransform.position, teleportOnFail: true, threshold: 0.5f);
            }
        }

        public override void End()
        {
            _npc?.Behaviour.DisableBehaviour<SitBehaviour>();
            base.End();
        }

        public override void Interrupt()
        {
            _npc?.Behaviour.DisableBehaviour<SitBehaviour>();
            base.Interrupt();
        }
    }
}
```

---

### `NPCSignal_UseObject.cs`

```csharp
using System;
using UnityEngine;

namespace NPCSystem
{
    /// <summary>Walk to an NPCInteractable, trigger its Use() callback, then end.</summary>
    public class NPCSignal_UseObject : NPCSignal
    {
        [Header("Use Settings")]
        public NPCInteractable Target;
        public float           UseRange = 1.5f;

        public override string GetName()            => $"Use {(Target != null ? Target.name : "Object")}";
        public override string GetTimeDescription() => $"Use object at {StartTime / 60:00}:{StartTime % 60:00}";

        public override void Started()
        {
            base.Started();
            if (Target != null)
                SetDestination(Target.InteractionPoint.position,
                               teleportOnFail: true, threshold: UseRange);
            else
                CompleteSignal();
        }

        protected override void OnPathCompleted(NPCMovement.WalkResult result)
        {
            Target?.Use(_npc);
            CompleteSignal();
        }

        protected override void OnPathFailed(NPCMovement.WalkResult result) => CompleteSignal();
    }
}
```

---

### `NPCEvent_Idle.cs`

```csharp
using UnityEngine;

namespace NPCSystem
{
    /// <summary>Stand idle at the current position for the event's duration.</summary>
    public class NPCEvent_Idle : NPCEvent
    {
        [Header("Idle Station (optional)")]
        public Transform IdlePoint;

        public override string GetName() => "Idle";

        public override void Started()
        {
            if (IdlePoint != null)
                SetDestination(IdlePoint.position, teleportOnFail: true);
            else
                _npc?.Movement.Stop();
        }

        public override void JumpTo()
        {
            _npc?.Movement.Stop();
        }
    }
}
```

---

### `NPCEvent_Patrol.cs`

```csharp
using UnityEngine;

namespace NPCSystem
{
    /// <summary>Follow a patrol route for the event's duration.</summary>
    public class NPCEvent_Patrol : NPCEvent
    {
        [Header("Patrol")]
        public FootPatrolRoute Route;

        public override string GetName() => "Patrol";

        public override void Started()
        {
            var patrol = _npc?.Behaviour.GetBehaviour<PatrolBehaviour>();
            if (patrol != null)
            {
                patrol.Route = Route;
                _npc.Behaviour.EnableBehaviour<PatrolBehaviour>();
            }
        }

        public override void End()
        {
            _npc?.Behaviour.DisableBehaviour<PatrolBehaviour>();
            base.End();
        }

        public override void Interrupt()
        {
            _npc?.Behaviour.DisableBehaviour<PatrolBehaviour>();
            base.Interrupt();
        }
    }
}
```

---

### `NPCEvent_Conversation.cs`

```csharp
using System;
using UnityEngine;

namespace NPCSystem
{
    /// <summary>
    /// Two NPCs meet at a ConversationPoint and converse for the event duration.
    /// Each participant needs this event in their schedule at the same time.
    /// </summary>
    public class NPCEvent_Conversation : NPCEvent
    {
        [Header("Conversation")]
        public Transform   ConversationPoint;
        public NPC         ConversationPartner;
        public float       StandRadius = 1.0f;

        public event Action<NPC, NPC> onConversationStarted;

        private bool _arrived;

        public override string GetName() => "Conversation";

        public override void Started()
        {
            _arrived = false;
            if (ConversationPoint != null)
            {
                // Offset slightly so two NPCs don't stack
                Vector3 offset = UnityEngine.Random.insideUnitSphere * StandRadius;
                offset.y = 0f;
                SetDestination(ConversationPoint.position + offset,
                               teleportOnFail: true, threshold: 0.6f);
            }
        }

        public override void JumpTo()
        {
            _arrived = true;
            BeginConversation();
        }

        protected override void OnPathCompleted(NPCMovement.WalkResult result)
        {
            if (!_arrived)
            {
                _arrived = true;
                BeginConversation();
            }
        }

        public override void End()
        {
            _npc?.Behaviour.GetBehaviour<GenericDialogueBehaviour>()?.EndConversation();
            base.End();
        }

        private void BeginConversation()
        {
            // Face partner
            if (ConversationPartner != null)
            {
                Vector3 dir = (ConversationPartner.transform.position - _npc.transform.position);
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.01f) _npc.Movement.FaceDirection(dir.normalized);
            }

            var dialogue = _npc?.Behaviour.GetBehaviour<GenericDialogueBehaviour>();
            if (dialogue != null && ConversationPartner != null)
                dialogue.BeginConversation(ConversationPartner.transform);

            onConversationStarted?.Invoke(_npc, ConversationPartner);
        }
    }
}
```

---

## 11. Supporting Types

### `FootPatrolRoute.cs`

```csharp
using UnityEngine;

namespace NPCSystem
{
    /// <summary>
    /// Defines an ordered list of patrol waypoints. Multiple NPCs can share the
    /// same route; PatrolGroup manages their spacing and coordination.
    /// </summary>
    public class FootPatrolRoute : MonoBehaviour
    {
        [Tooltip("Ordered waypoints. NPC visits them in sequence.")]
        public Transform[] Waypoints;

        private void OnDrawGizmos()
        {
            if (Waypoints == null || Waypoints.Length < 2) return;
            Gizmos.color = Color.cyan;
            for (int i = 0; i < Waypoints.Length; i++)
            {
                if (Waypoints[i] == null) continue;
                Gizmos.DrawSphere(Waypoints[i].position, 0.2f);
                int next = (i + 1) % Waypoints.Length;
                if (Waypoints[next] != null)
                    Gizmos.DrawLine(Waypoints[i].position, Waypoints[next].position);
            }
        }
    }
}
```

---

### `PatrolGroup.cs`

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace NPCSystem
{
    /// <summary>
    /// Coordinates multiple NPCs patrolling the same route.
    /// Ensures group advances to next waypoint only when all members arrive.
    /// </summary>
    public class PatrolGroup
    {
        public FootPatrolRoute Route;
        public List<NPC>       Members   = new List<NPC>();
        public int             CurrentWaypoint { get; private set; }

        private HashSet<NPC> _arrivedMembers = new HashSet<NPC>();

        public PatrolGroup(FootPatrolRoute route) => Route = route;

        public void AddMember(NPC npc)
        {
            Members.Add(npc);
            var patrol = npc.Behaviour.GetBehaviour<PatrolBehaviour>();
            patrol?.SetGroup(this);
        }

        public Vector3 GetDestination(NPC member)
        {
            if (Route == null || Route.Waypoints.Length == 0)
                return member.transform.position;

            int index = (Members.IndexOf(member) + CurrentWaypoint) % Route.Waypoints.Length;
            return Route.Waypoints[index].position;
        }

        /// <summary>Called by PatrolBehaviour when a member arrives at their waypoint.</summary>
        public void OnMemberArrived(NPC member)
        {
            _arrivedMembers.Add(member);
            if (IsGroupReadyToAdvance())
                AdvanceGroup();
        }

        public bool IsGroupReadyToAdvance() =>
            _arrivedMembers.Count >= Members.Count;

        public void AdvanceGroup()
        {
            _arrivedMembers.Clear();
            CurrentWaypoint = (CurrentWaypoint + 1) % Route.Waypoints.Length;
        }

        public void DisbandGroup()
        {
            foreach (var m in Members)
                m.Behaviour.GetBehaviour<PatrolBehaviour>()?.SetGroup(null);
            Members.Clear();
        }
    }
}
```

---

### `NPCInteractable.cs`

```csharp
using System;
using UnityEngine;

namespace NPCSystem
{
    /// <summary>
    /// Marks a world object that NPCs can walk to and "use".
    /// Examples: ATM, vending machine, workstation, park bench.
    /// </summary>
    public class NPCInteractable : MonoBehaviour
    {
        [Header("Interaction")]
        public Transform InteractionPoint;
        [Tooltip("How many NPCs can interact at once.")]
        public int       MaxSimultaneousUsers = 1;

        public event Action<NPC> onUsed;

        private int _currentUsers;

        public bool CanBeUsed => _currentUsers < MaxSimultaneousUsers;

        public bool Use(NPC npc)
        {
            if (!CanBeUsed) return false;
            _currentUsers++;
            onUsed?.Invoke(npc);
            return true;
        }

        public void Release(NPC npc) => _currentUsers = Mathf.Max(0, _currentUsers - 1);

        private void OnDrawGizmos()
        {
            if (InteractionPoint == null) return;
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(InteractionPoint.position, 0.15f);
        }
    }
}
```

---

### `NPCRelationData.cs`

```csharp
using System;
using UnityEngine;

namespace NPCSystem
{
    /// <summary>
    /// Tracks relationship values between this NPC and the player (0–100).
    /// Relationship affects dialogue options, willingness to deal, and crime responses.
    /// </summary>
    [Serializable]
    public class NPCRelationData
    {
        public const int MIN_RELATION = 0;
        public const int MAX_RELATION = 100;

        [Range(0, 100)] public int StartingRelation = 50;

        public int   CurrentRelation { get; private set; }
        public float NormalizedRelation => CurrentRelation / (float)MAX_RELATION;

        public event Action<int, int> onRelationChanged; // old, new

        public void Initialize() => CurrentRelation = StartingRelation;

        public void ChangeRelation(int delta)
        {
            int old = CurrentRelation;
            CurrentRelation = Mathf.Clamp(CurrentRelation + delta, MIN_RELATION, MAX_RELATION);
            if (CurrentRelation != old)
                onRelationChanged?.Invoke(old, CurrentRelation);
        }

        public bool IsFriendly  => CurrentRelation >= 70;
        public bool IsNeutral   => CurrentRelation >= 40 && CurrentRelation < 70;
        public bool IsHostile   => CurrentRelation < 40;
    }
}
```

---

## 12. NPC Subtypes

### `CustomerNPC.cs`

```csharp
using System;
using UnityEngine;

namespace NPCSystem
{
    /// <summary>
    /// Customer subtype. Tracks drug need level; when it crosses threshold,
    /// enables RequestProductBehaviour to seek out the player.
    /// Subscribes to crime awareness events and responds with cowering/fleeing.
    /// </summary>
    public class CustomerNPC : NPC
    {
        [Header("Customer Settings")]
        [Range(0f, 1f)] public float StartingNeedLevel = 0f;
        [Range(0f, 1f)] public float NeedThreshold     = 0.7f;
        public float NeedIncreasePerMinute              = 0.002f;

        [Header("Relationship")]
        public NPCRelationData RelationData = new NPCRelationData();

        public float NeedLevel { get; private set; }
        public bool  WantsToBuy => NeedLevel >= NeedThreshold;

        public event Action<CustomerNPC> onNeedThresholdCrossed;

        private bool _hasRequestedProduct;
        private NPCResponses_Customer _customerResponses;

        protected override void Awake()
        {
            base.Awake();
            _customerResponses = GetComponent<NPCResponses_Customer>();
            Type = NPCType.Customer;
        }

        protected override void Start()
        {
            base.Start();
            NeedLevel = StartingNeedLevel;
            RelationData.Initialize();

            if (GameClock.InstanceExists)
                GameClock.Instance.onMinuteChanged += OnMinutePassed;

            // Wire up RequestProduct completion
            var requestBehaviour = Behaviour.GetBehaviour<RequestProductBehaviour>();
            if (requestBehaviour != null)
            {
                requestBehaviour.onDealComplete += OnDealCompleted;
                requestBehaviour.onDealRequested += OnDealRequested;
            }
        }

        private void OnDestroy()
        {
            if (GameClock.InstanceExists)
                GameClock.Instance.onMinuteChanged -= OnMinutePassed;
        }

        private void OnMinutePassed(int time)
        {
            if (!IsConscious) return;

            NeedLevel += NeedIncreasePerMinute;

            if (!_hasRequestedProduct && WantsToBuy)
            {
                _hasRequestedProduct = true;
                Behaviour.EnableBehaviour<RequestProductBehaviour>();
                onNeedThresholdCrossed?.Invoke(this);
            }
        }

        private void OnDealRequested()
        {
            // Face player, play deal animation, open deal UI etc.
            // Hook into your deal system here.
        }

        private void OnDealCompleted()
        {
            NeedLevel              = 0f;
            _hasRequestedProduct   = false;
            Behaviour.DisableBehaviour<RequestProductBehaviour>();
        }
    }
}
```

---

### `NPCResponses_Customer.cs` (civilian crime responses)

```csharp
using UnityEngine;

namespace NPCSystem
{
    /// <summary>Customer/civilian crime response: witness crime → call police or flee.</summary>
    public class NPCResponses_Customer : NPCResponses
    {
        [Header("Response Settings")]
        [Tooltip("Chance (0–1) that NPC calls police instead of just fleeing.")]
        [Range(0f, 1f)] public float CallPoliceChance = 0.4f;

        protected override void OnPlayerSeen(Transform player)     { }
        protected override void OnPlayerLost()                     { }

        protected override void OnCrimeWitnessed(Transform perpetrator)
        {
            if (Random.value < CallPoliceChance)
                _actions.CallPolice(perpetrator);
            else
                _actions.Flee(perpetrator);
        }

        protected override void OnGunshotHeard()
        {
            _actions.Flee(null);
        }

        protected override void OnExplosionHeard()
        {
            _actions.Flee(null);
        }
    }
}
```

---

### `PoliceNPC.cs`

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace NPCSystem
{
    /// <summary>
    /// Police NPC. Patrols by default. Responds to crime reports (CallPolice events)
    /// by going to the crime location and engaging the perpetrator.
    /// Has an arrest mechanic — if player is in range and wanted, arrests them.
    /// </summary>
    public class PoliceNPC : NPC
    {
        [Header("Police Settings")]
        public float ArrestRange    = 2.5f;
        public float InvestigateRange = 20f;

        public bool IsOnDuty { get; private set; } = true;

        private Transform _suspectTarget;
        private static readonly List<PoliceNPC> _allPolice = new List<PoliceNPC>();

        public static IReadOnlyList<PoliceNPC> AllPolice => _allPolice;

        protected override void Awake()
        {
            base.Awake();
            Type = NPCType.Police;
        }

        protected override void Start()
        {
            base.Start();
            _allPolice.Add(this);

            // Subscribe to crime call events
            Awareness.onNoticedCrime.AddListener(EngageTarget);
        }

        private void OnDestroy() => _allPolice.Remove(this);

        // ── Called by other NPCs' CallPoliceBehaviour events ──────────────────
        public void RespondToCrime(Transform crimeLocation, Transform perpetrator)
        {
            _suspectTarget = perpetrator;
            var patrol = Behaviour.GetBehaviour<PatrolBehaviour>();
            patrol?.Disable();

            // Walk to crime scene
            Movement.WalkTo(crimeLocation.position, result =>
            {
                if (_suspectTarget != null)
                    EngageTarget(_suspectTarget);
            }, run: true);
        }

        private void EngageTarget(Transform target)
        {
            _suspectTarget = target;

            float dist = Vector3.Distance(transform.position, target.position);
            if (dist <= ArrestRange)
                AttemptArrest(target);
            else
            {
                // Chase
                var combat = Behaviour.GetBehaviour<CombatBehaviour>();
                combat?.SetTarget(target);
                Behaviour.EnableBehaviour<CombatBehaviour>();
            }
        }

        private void AttemptArrest(Transform suspect)
        {
            // Hook into your WantedLevel / Crime system here
            // e.g. PlayerCrimeData.Instance.Arrest(suspect);
            Debug.Log($"[{FirstName}] Arresting {suspect.name}");
        }

        public void GoOffDuty()
        {
            IsOnDuty = false;
            Behaviour.DisableBehaviour<PatrolBehaviour>();
            Behaviour.EnableBehaviour<WanderBehaviour>();
        }

        public void GoOnDuty()
        {
            IsOnDuty = true;
            Behaviour.DisableBehaviour<WanderBehaviour>();
            Behaviour.EnableBehaviour<PatrolBehaviour>();
        }
    }
}
```

---

### `EmployeeNPC.cs`

```csharp
using UnityEngine;

namespace NPCSystem
{
    /// <summary>
    /// Employee NPC. Goes to their assigned station during work hours.
    /// Ignores most threat responses while working (unless directly attacked).
    /// Player can assign stations via management system.
    /// </summary>
    public class EmployeeNPC : NPC
    {
        [Header("Employee Settings")]
        public int  WorkStartTime = 480;   // 8:00 AM
        public int  WorkEndTime   = 1020;  // 17:00 (5 PM)
        public NPCInteractable AssignedStation;

        public bool IsWorking { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            Type = NPCType.Employee;
        }

        protected override void Start()
        {
            base.Start();

            if (GameClock.InstanceExists)
            {
                GameClock.Instance.onMinuteChanged += OnMinutePassed;
                EvaluateWorkState(GameClock.Instance.CurrentTime);
            }
        }

        private void OnDestroy()
        {
            if (GameClock.InstanceExists)
                GameClock.Instance.onMinuteChanged -= OnMinutePassed;
        }

        private void OnMinutePassed(int time)  => EvaluateWorkState(time);

        private void EvaluateWorkState(int time)
        {
            bool shouldWork = GameClock.Instance.IsTimeBetween(WorkStartTime, WorkEndTime);

            if (shouldWork && !IsWorking)
                StartWork();
            else if (!shouldWork && IsWorking)
                EndWork();
        }

        private void StartWork()
        {
            IsWorking = true;

            if (AssignedStation != null)
            {
                var stationary = Behaviour.GetBehaviour<StationaryBehaviour>();
                if (stationary != null)
                {
                    stationary.SetStation(
                        AssignedStation.InteractionPoint,
                        null);
                    Behaviour.EnableBehaviour<StationaryBehaviour>();
                }
            }
        }

        private void EndWork()
        {
            IsWorking = false;
            Behaviour.DisableBehaviour<StationaryBehaviour>();
            Behaviour.EnableBehaviour<WanderBehaviour>();
        }

        public void AssignToStation(NPCInteractable station)
        {
            AssignedStation = station;
            if (IsWorking)
            {
                var stationary = Behaviour.GetBehaviour<StationaryBehaviour>();
                stationary?.SetStation(station.InteractionPoint);
            }
        }
    }
}
```

---

## 13. Animator Setup & Animation Constants

Create a Unity Animator Controller with this layer/state structure:

```
Base Layer
├── Locomotion (Blend Tree)
│   ├── Idle        (Speed = 0)
│   ├── Walk        (Speed = 0.4 – 0.7)
│   └── Run         (Speed = 0.7 – 1.0)
├── AnyState → TakeDamage     (TakeDamage trigger)
├── AnyState → KnockedOut     (IsKnockedOut = true)
├── AnyState → Dead           (IsDead = true)
└── Sub-State Machine: Actions
    ├── Sit                   (IsSitting = true)
    ├── GetUp                 (GetUp trigger, → Locomotion)
    ├── Cower                 (Cower trigger, loops)
    ├── PhoneCall             (PhoneCall trigger)
    ├── Attack                (Attack trigger)
    └── Consume               (Consume trigger)
```

**Locomotion blend tree:** driven by `Speed` parameter (0–2), with `TurnSpeed`
used in the 2D Freeform Directional variant if you want strafe animations.

---

## 14. Integration Guide & Usage Examples

### Setting up an NPC prefab

1. Create a new GameObject.
2. Add a **NavMeshAgent** component. Set Speed, Stopping Distance, Obstacle Avoidance Radius.
3. Add these components in order:
   - `NPC` (base or subtype like CustomerNPC)
   - `NPCMovement`
   - `NPCHealth`
   - `NPCAwareness`
   - `VisionCone` (child of head Transform)
   - `NPCAnimation`
   - `NPCScheduleManager`
   - `NPCActions`
   - `NPCBehaviour` ← **must come after all Behaviour components**
4. Add all desired `Behaviour` components:
   - `IdleBehaviour` (Priority 1, EnabledOnAwake = true)
   - `WanderBehaviour` (Priority 10)
   - `PatrolBehaviour` (Priority 20, Route = assign FootPatrolRoute)
   - `StationaryBehaviour` (Priority 30)
   - `CoweringBehaviour` (Priority 500)
   - `CallPoliceBehaviour` (Priority 600)
   - `FleeBehaviour` (Priority 700)
   - `CombatBehaviour` (Priority 800)
   - `RagdollBehaviour` (Priority 900)
   - `UnconsciousBehaviour` (Priority 950)
   - `DeadBehaviour` (Priority 1000)
5. For **Customer**: also add `RequestProductBehaviour`, `ConsumeProductBehaviour`.
6. Assign `VisionCone` reference in `NPCAwareness` inspector.
7. Assign `Animator` to NPCAnimation.

---

### Adding a daily schedule in the Inspector

Add `NPCAction` components (each is also a MonoBehaviour) to the NPC GameObject.
They appear in the `NPCScheduleManager.ActionList` at runtime after `InitializeActions()`.

**Example: Shop worker schedule**

```
NPCEvent_Idle          StartTime=0    EndTime=480     (Sleep  00:00–08:00)
NPCSignal_WalkToLocation StartTime=480 Target=ShopEntrance
NPCEvent_Stationary    StartTime=490  EndTime=1020    (Work  08:10–17:00)
NPCSignal_WalkToLocation StartTime=1020 Target=ParkBench
NPCEvent_Idle          StartTime=1030 EndTime=1320    (Leisure 17:10–22:00)
NPCSignal_WalkToLocation StartTime=1320 Target=HomeEntrance
NPCEvent_Idle          StartTime=1330 EndTime=1440    (Sleep  22:10–24:00)
```

---

### Triggering behaviours from game systems

```csharp
// Player commits a crime witnessed by an NPC:
npc.Actions.CallPolice(playerTransform);

// External damage (explosion, car collision):
npc.Health.TakeDamage(50f, NPCHealth.DamageType.Explosion);
npc.Actions.TriggerRagdoll();

// NPC spots player contraband (from VisionCone event):
npc.Awareness.OnCrimeWitnessed(playerTransform);

// Summon NPC to a location (management system):
npc.Schedule.InterruptCurrentAction();
npc.Movement.WalkTo(summonPoint, run: true);

// Assign employee to new station:
var employee = npc as EmployeeNPC;
employee?.AssignToStation(chemistryStation.GetComponent<NPCInteractable>());

// Check NPC state from UI:
bool isBusy   = npc.Behaviour.ActiveBehaviour?.Priority > 100;
string status = npc.Behaviour.ActiveBehaviour?.BehaviourName ?? "None";
```

---

### Wiring up awareness for crime events

```csharp
// On your crime system, when player commits crime:
public static void BroadcastCrime(Vector3 position, Transform player)
{
    float crimeRange = 30f;
    var npcs = Physics.OverlapSphere(position, crimeRange, LayerMask.GetMask("NPC"));
    foreach (var col in npcs)
    {
        var awareness = col.GetComponent<NPCAwareness>();
        if (awareness != null && awareness.VisionCone.CanSeeTransform(player.transform))
            awareness.OnCrimeWitnessed(player.transform);
    }
}
```

---

### Priority reference table

| Priority | Behaviour                     | When active                              |
|----------|-------------------------------|------------------------------------------|
| 1        | `IdleBehaviour`               | Always (fallback)                        |
| 10       | `WanderBehaviour`             | Enabled in schedule down-time            |
| 20       | `PatrolBehaviour`             | Enabled during patrol events             |
| 25       | `SitBehaviour`                | Signal-driven (e.g. NPCSignal_Sit)       |
| 30       | `StationaryBehaviour`         | Work shift / station assignment          |
| 40       | `FollowBehaviour`             | Following player or companion            |
| 50       | `GenericDialogueBehaviour`    | Active conversation                      |
| 200      | `RequestProductBehaviour`     | Customer want-level crossed threshold    |
| 300      | `ConsumeProductBehaviour`     | After deal completed                     |
| 500      | `CoweringBehaviour`           | Player brandishes weapon                 |
| 600      | `CallPoliceBehaviour`         | NPC witnesses crime                      |
| 700      | `FleeBehaviour`               | Credible threat                          |
| 800      | `CombatBehaviour`             | Police/cartel engaging                   |
| 850      | `HeavyFlinchBehaviour`        | Heavy hit received                       |
| 900      | `RagdollBehaviour`            | Massive impact, explosion                |
| 950      | `UnconsciousBehaviour`        | Knocked out                              |
| 1000     | `DeadBehaviour`               | Dead — terminal                          |

---

### Design pattern summary

| Pattern              | Implementation                                                             |
|----------------------|----------------------------------------------------------------------------|
| **Template Method**  | `NPCAction`, `Behaviour`, `NPCResponses` — base defines lifecycle, subclasses fill in hooks |
| **Priority Stack**   | `NPCBehaviour.behaviourStack` sorted by Priority; highest enabled wins     |
| **Command**          | `NPCSignal` subclasses — each encapsulates one discrete NPC task           |
| **Observer (Action)**| `NPCHealth`, `NPCAwareness`, `RequestProductBehaviour` all expose C# events |
| **Strategy**         | `NPCResponses` subclasses swap crime/threat response logic per NPC type    |
| **Component**        | Each concern is its own MonoBehaviour; NPC is just the wire-up + root ref  |
| **Factory (implicit)**| `NPCScheduleManager.InitializeActions()` wires all actions to their owner  |
| **Singleton**        | `GameClock` — shared game time without coupling to specific scene objects  |
