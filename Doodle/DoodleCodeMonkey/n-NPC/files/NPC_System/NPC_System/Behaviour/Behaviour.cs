// ============================================================
//  Behaviour.cs
//  Abstract base class for ALL NPC behaviours.
//
//  Design Pattern: Template Method
//   The base defines the state machine lifecycle (Enable,
//   Disable, Activate, Deactivate, Pause, Resume).
//   Subclasses override only the virtual hooks they care about.
//
//  Lifecycle:
//
//    Disabled ──(Enable)──► Enabled
//                              │
//                          (Activate)
//                              │
//                              ▼
//                           Active ──► BehaviourUpdate() runs
//                              │       OnActiveTick() runs
//                           (Pause)
//                              │
//                           Paused ── BehaviourUpdate() skipped
//                              │
//                           (Resume)
//                              │
//                          (Deactivate)
//                              │
//    Disabled ◄──(Disable)── Enabled
//
//  Priority: Higher number = takes over lower.
//  NPCBehaviour stack always runs the highest-priority
//  Enabled behaviour.
// ============================================================

using System;
using UnityEngine;
using UnityEngine.Events;

namespace NPCSystem
{
    public abstract class Behaviour : MonoBehaviour
    {
        // -------------------------------------------------------
        // Inspector
        // -------------------------------------------------------
        [Header("Behaviour Settings")]
        [Tooltip("Human-readable name for this behaviour.")]
        public string BehaviourName = "Unnamed Behaviour";

        [Tooltip("Higher number = takes priority over lower. See BehaviourPriority constants.")]
        public int Priority = 0;

        [Tooltip("Is this behaviour enabled from the start?")]
        public bool EnabledOnAwake = false;

        [Header("Events (Inspector-wired)")]
        public UnityEvent OnEnableEvent;
        public UnityEvent OnDisableEvent;
        public UnityEvent OnActivateEvent;
        public UnityEvent OnDeactivateEvent;

        // -------------------------------------------------------
        // State
        // -------------------------------------------------------
        public bool   Enabled   { get; protected set; }
        public bool   Active    { get; private   set; }
        public bool   Paused    { get; private   set; }
        public NPC    Npc       { get; private   set; }

        // -------------------------------------------------------
        // Pathing failure counter (shared across many behaviours)
        // -------------------------------------------------------
        protected int ConsecutivePathingFailures;
        protected const int MAX_PATHING_FAILURES = 5;

        // -------------------------------------------------------
        // Convenience references (set after Initialize is called)
        // -------------------------------------------------------
        protected NPCMovement  Movement  => Npc?.Movement;
        protected NPCHealth    Health    => Npc?.Health;
        protected NPCAwareness Awareness => Npc?.Awareness;
        protected NPCAnimation Animation => Npc?.Animation;
        protected NPCBehaviour BehaviourManager => Npc?.BehaviourManager;

        // -------------------------------------------------------
        // Unity
        // -------------------------------------------------------
        protected virtual void Awake()
        {
            Npc = GetComponent<NPC>();

            if (EnabledOnAwake)
                Enabled = true;
        }

        protected virtual void OnValidate()
        {
            if (string.IsNullOrEmpty(BehaviourName))
                BehaviourName = GetType().Name;
        }

        // -------------------------------------------------------
        // Called once by NPCBehaviour after NPC is initialized
        // -------------------------------------------------------
        public virtual void Initialize(NPC npc)
        {
            Npc = npc;
        }

        // -------------------------------------------------------
        // Enable / Disable  (controls whether this behaviour
        // participates in the priority evaluation)
        // -------------------------------------------------------
        public virtual void Enable()
        {
            if (Enabled) return;
            Enabled = true;
            OnEnableEvent?.Invoke();
            OnEnabled();
        }

        public virtual void Disable()
        {
            if (!Enabled) return;

            if (Active)
                Deactivate();

            Enabled = false;
            OnDisableEvent?.Invoke();
            OnDisabled();
        }

        /// <summary>Override to react when enabled (e.g. register listeners).</summary>
        protected virtual void OnEnabled()  { }

        /// <summary>Override to react when disabled (e.g. unregister listeners).</summary>
        protected virtual void OnDisabled() { }

        // -------------------------------------------------------
        // Activate / Deactivate  (this behaviour becomes the
        // currently running behaviour in the stack)
        // -------------------------------------------------------
        public virtual void Activate()
        {
            if (Active) return;
            Active  = true;
            Paused  = false;
            ConsecutivePathingFailures = 0;
            OnActivateEvent?.Invoke();
            OnActivated();
        }

        public virtual void Deactivate()
        {
            if (!Active) return;
            Active = false;
            Paused = false;
            OnDeactivateEvent?.Invoke();
            OnDeactivated();

            // Stop any movement this behaviour started
            Movement?.StopWalk(false);
        }

        /// <summary>Override to react on activation (start walking, play anim, etc.).</summary>
        protected virtual void OnActivated()   { }

        /// <summary>Override to react on deactivation (cleanup, stop sounds, etc.).</summary>
        protected virtual void OnDeactivated() { }

        // -------------------------------------------------------
        // Pause / Resume
        // -------------------------------------------------------
        public virtual void Pause()
        {
            if (!Active || Paused) return;
            Paused = true;
            OnPaused();
        }

        public virtual void Resume()
        {
            if (!Active || !Paused) return;
            Paused = false;
            OnResumed();
        }

        protected virtual void OnPaused()  { }
        protected virtual void OnResumed() { }

        // -------------------------------------------------------
        // Per-frame update (only called when Active and not Paused)
        // -------------------------------------------------------
        /// <summary>
        /// Main per-frame logic. Called by NPCBehaviour.Update()
        /// only when this behaviour is the active one.
        /// </summary>
        public virtual void BehaviourUpdate()  { }

        /// <summary>
        /// LateUpdate equivalent. Called after BehaviourUpdate.
        /// Use for look-at, IK, etc.
        /// </summary>
        public virtual void BehaviourLateUpdate() { }

        /// <summary>
        /// Called on a fixed tick (e.g. every 0.5s).
        /// Good for expensive checks that don't need per-frame precision.
        /// </summary>
        public virtual void OnActiveTick() { }

        // -------------------------------------------------------
        // Priority evaluation hook
        // Called every frame by NPCBehaviour to decide if this
        // behaviour WANTS to be the active one.
        // -------------------------------------------------------
        /// <summary>
        /// Return true if this behaviour should be running right now.
        /// Only checked when the behaviour is Enabled.
        /// Default: always wants to be active when enabled.
        /// </summary>
        public virtual bool WantsToBeActive() => Enabled;

        // -------------------------------------------------------
        // Pathing helper (shared)
        // -------------------------------------------------------
        protected virtual void WalkCallback(EWalkResult result)
        {
            if (result == EWalkResult.Failed || result == EWalkResult.Timeout)
            {
                ConsecutivePathingFailures++;
                if (ConsecutivePathingFailures >= MAX_PATHING_FAILURES)
                    OnPathingFailed();
            }
            else if (result == EWalkResult.Success)
            {
                ConsecutivePathingFailures = 0;
            }
        }

        /// <summary>Called when MAX_PATHING_FAILURES is reached.</summary>
        protected virtual void OnPathingFailed()
        {
            Debug.LogWarning($"[{BehaviourName}] Max pathing failures reached on {Npc?.name}");
        }

        // -------------------------------------------------------
        // Debug
        // -------------------------------------------------------
        public override string ToString() =>
            $"{BehaviourName} [P:{Priority}] [En:{Enabled}] [Ac:{Active}]";
    }
}
