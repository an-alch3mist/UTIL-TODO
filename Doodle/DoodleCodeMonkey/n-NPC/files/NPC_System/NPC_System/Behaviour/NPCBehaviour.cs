// ============================================================
//  NPCBehaviour.cs
//  Manages the priority-sorted behaviour stack.
//
//  Design Pattern: Priority Strategy Stack
//   - Maintains a sorted list of Behaviour components
//   - Every frame: find highest-priority Enabled behaviour
//     that WantsToBeActive() → activate it, deactivate rest
//   - Adding a new behaviour type requires zero changes here
//
//  How behaviours get triggered:
//   - External code calls Enable()/Disable() on behaviours
//   - NPCAwareness fires events → NPCBehaviour enables
//     the appropriate behaviour (e.g. OnThreatSpotted → CombatBehaviour.Enable())
//   - NPCHealth fires events → RagdollBehaviour / DeadBehaviour
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace NPCSystem
{
    [RequireComponent(typeof(NPC))]
    public class NPCBehaviour : MonoBehaviour
    {
        // -------------------------------------------------------
        // Inspector references — assign in Prefab Inspector
        // -------------------------------------------------------
        [Header("Default Behaviours (assign in Inspector)")]
        public IdleBehaviour           IdleBehaviour;
        public WanderBehaviour         WanderBehaviour;
        public StationaryBehaviour     StationaryBehaviour;
        public PatrolBehaviour         PatrolBehaviour;
        public CoweringBehaviour       CoweringBehaviour;
        public RagdollBehaviour        RagdollBehaviour;
        public FleeBehaviour           FleeBehaviour;
        public CombatBehaviour         CombatBehaviour;
        public DeadBehaviour           DeadBehaviour;
        public UnconsciousBehaviour    UnconsciousBehaviour;
        public HeavyFlinchBehaviour    HeavyFlinchBehaviour;
        public FaceTargetBehaviour     FaceTargetBehaviour;
        public CallPoliceBehaviour     CallPoliceBehaviour;
        public GenericDialogueBehaviour GenericDialogueBehaviour;

        // -------------------------------------------------------
        // State
        // -------------------------------------------------------
        public Behaviour ActiveBehaviour { get; private set; }

        // -------------------------------------------------------
        // Private
        // -------------------------------------------------------
        private NPC              _npc;
        private NPCHealth        _health;
        private NPCAwareness     _awareness;

        private readonly List<Behaviour> _behaviourStack  = new List<Behaviour>();
        private const float              TICK_RATE        = 0.5f;
        private float                    _tickTimer;

        // -------------------------------------------------------
        // Unity
        // -------------------------------------------------------
        private void Awake()
        {
            _npc       = GetComponent<NPC>();
            _health    = GetComponent<NPCHealth>();
            _awareness = GetComponent<NPCAwareness>();
        }

        public void Initialize(NPC npc)
        {
            // Collect all Behaviour components on this GameObject
            // (including children)
            var all = GetComponentsInChildren<Behaviour>(true);
            _behaviourStack.Clear();
            _behaviourStack.AddRange(all);

            // Initialize each
            foreach (var b in _behaviourStack)
                b.Initialize(npc);

            SortStack();

            // Wire health events
            if (_health != null)
            {
                _health.OnKnockedOut += OnKnockedOut;
                _health.OnRevived    += OnRevived;
                _health.OnDied       += OnDied;
            }

            // Wire awareness events
            if (_awareness != null)
            {
                _awareness.OnAlertLevelChanged += OnAlertLevelChanged;
                _awareness.OnThreatSpotted     += OnThreatSpotted;
                _awareness.OnThreatLost        += OnThreatLost;
            }

            // Start default behaviour
            IdleBehaviour?.Enable();
            EvaluateStack();
        }

        private void OnDestroy()
        {
            if (_health != null)
            {
                _health.OnKnockedOut -= OnKnockedOut;
                _health.OnRevived    -= OnRevived;
                _health.OnDied       -= OnDied;
            }

            if (_awareness != null)
            {
                _awareness.OnAlertLevelChanged -= OnAlertLevelChanged;
                _awareness.OnThreatSpotted     -= OnThreatSpotted;
                _awareness.OnThreatLost        -= OnThreatLost;
            }
        }

        // -------------------------------------------------------
        // Update
        // -------------------------------------------------------
        private void Update()
        {
            if (ActiveBehaviour != null && ActiveBehaviour.Active && !ActiveBehaviour.Paused)
                ActiveBehaviour.BehaviourUpdate();

            _tickTimer += Time.deltaTime;
            if (_tickTimer >= TICK_RATE)
            {
                _tickTimer = 0f;
                EvaluateStack();

                if (ActiveBehaviour != null && ActiveBehaviour.Active)
                    ActiveBehaviour.OnActiveTick();
            }
        }

        private void LateUpdate()
        {
            if (ActiveBehaviour != null && ActiveBehaviour.Active && !ActiveBehaviour.Paused)
                ActiveBehaviour.BehaviourLateUpdate();
        }

        // -------------------------------------------------------
        // Stack evaluation (the core of the priority system)
        // -------------------------------------------------------
        private void EvaluateStack()
        {
            Behaviour wanted = null;

            foreach (var b in _behaviourStack)   // sorted highest-priority first
            {
                if (b.Enabled && b.WantsToBeActive())
                {
                    wanted = b;
                    break;
                }
            }

            if (wanted == ActiveBehaviour) return;

            // Deactivate old
            if (ActiveBehaviour != null && ActiveBehaviour.Active)
                ActiveBehaviour.Deactivate();

            // Activate new
            ActiveBehaviour = wanted;
            if (ActiveBehaviour != null && !ActiveBehaviour.Active)
                ActiveBehaviour.Activate();
        }

        private void SortStack()
        {
            _behaviourStack.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        }

        // -------------------------------------------------------
        // Public API
        // -------------------------------------------------------

        /// <summary>Get a behaviour by type.</summary>
        public T GetBehaviour<T>() where T : Behaviour
        {
            foreach (var b in _behaviourStack)
                if (b is T typed) return typed;
            return null;
        }

        /// <summary>Enable a behaviour by type.</summary>
        public void EnableBehaviour<T>() where T : Behaviour
            => GetBehaviour<T>()?.Enable();

        /// <summary>Disable a behaviour by type.</summary>
        public void DisableBehaviour<T>() where T : Behaviour
            => GetBehaviour<T>()?.Disable();

        /// <summary>Force-activate a specific behaviour, bypassing priority. Use sparingly.</summary>
        public void ForceActivate(Behaviour b)
        {
            if (!_behaviourStack.Contains(b)) return;

            if (ActiveBehaviour != null && ActiveBehaviour.Active)
                ActiveBehaviour.Deactivate();

            ActiveBehaviour = b;
            b.Enable();
            b.Activate();
        }

        /// <summary>Pause the currently active behaviour (e.g. for dialogue).</summary>
        public void PauseActive()  => ActiveBehaviour?.Pause();

        /// <summary>Resume the currently active behaviour.</summary>
        public void ResumeActive() => ActiveBehaviour?.Resume();

        // -------------------------------------------------------
        // Health event handlers
        // -------------------------------------------------------
        private void OnKnockedOut(EKnockdownCause cause)
        {
            CombatBehaviour?.Disable();
            FleeBehaviour?.Disable();
            UnconsciousBehaviour?.Enable();
            EvaluateStack();
        }

        private void OnRevived()
        {
            UnconsciousBehaviour?.Disable();
            IdleBehaviour?.Enable();
            EvaluateStack();
        }

        private void OnDied(EKnockdownCause cause)
        {
            // Disable everything, enable Dead
            foreach (var b in _behaviourStack)
                if (!(b is DeadBehaviour) && !(b is RagdollBehaviour))
                    b.Disable();

            DeadBehaviour?.Enable();
            EvaluateStack();
        }

        // -------------------------------------------------------
        // Awareness event handlers
        // -------------------------------------------------------
        private void OnAlertLevelChanged(EAlertLevel oldLevel, EAlertLevel newLevel)
        {
            switch (newLevel)
            {
                case EAlertLevel.Calm:
                    CombatBehaviour?.Disable();
                    FleeBehaviour?.Disable();
                    CallPoliceBehaviour?.Disable();
                    CoweringBehaviour?.Disable();
                    IdleBehaviour?.Enable();
                    break;

                case EAlertLevel.Suspicious:
                    // Subtle change — don't fully stop what NPC is doing
                    break;

                case EAlertLevel.Alert:
                    if (_npc.Definition.FleesThreat)
                        FleeBehaviour?.Enable();
                    else if (_npc.Definition.CallsPoliceOnCrime)
                        CallPoliceBehaviour?.Enable();
                    else
                        CoweringBehaviour?.Enable();
                    break;

                case EAlertLevel.Combat:
                    if (_npc.Definition.CanFight || _npc.Definition.CanShoot)
                        CombatBehaviour?.Enable();
                    else if (_npc.Definition.FleesThreat)
                        FleeBehaviour?.Enable();
                    break;
            }

            EvaluateStack();
        }

        private void OnThreatSpotted(GameObject threat)
        {
            // Handled via alert level change
        }

        private void OnThreatLost(GameObject threat)
        {
            // Alert level handles the disable
        }
    }
}
