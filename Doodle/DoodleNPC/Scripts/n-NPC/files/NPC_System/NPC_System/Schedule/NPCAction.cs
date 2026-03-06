// ============================================================
//  NPCAction.cs
//  Abstract base for all NPC scheduled actions.
//
//  Design Pattern: Template Method
//   - Defines the action lifecycle as virtual/abstract hooks
//   - Subclasses implement abstract methods and override only
//     the hooks they need
//
//  Lifecycle:
//    Pending ──(ShouldStart? + StartTime reached)──► Started()
//                                                      │
//                                                  LateStarted()
//                                                      │
//                                                  [ActiveUpdate() / MinPassed()]
//                                                      │
//                                                  End() / Interrupt()
//                                                      │
//                                                   Completed / Interrupted
// ============================================================

using System;
using UnityEngine;

namespace NPCSystem
{
    [Serializable]
    public abstract class NPCAction : MonoBehaviour
    {
        // -------------------------------------------------------
        // Inspector
        // -------------------------------------------------------
        [Header("Action Timing")]
        [Tooltip("In-game minute (0–1439) when this action should start.")]
        public int StartTime = 480;   // default 8 AM

        [Tooltip("Higher priority actions override lower ones at the same time.")]
        [SerializeField] protected int _priority = 0;

        // -------------------------------------------------------
        // Events
        // -------------------------------------------------------
        public event Action OnEnded;

        // -------------------------------------------------------
        // State
        // -------------------------------------------------------
        public EActionState ActionState { get; protected set; } = EActionState.Pending;
        public bool IsActive            => ActionState == EActionState.Active;
        public bool IsSignal            => this is NPCSignal;
        public int  Priority            => _priority;

        // -------------------------------------------------------
        // References (set by NPCScheduleManager)
        // -------------------------------------------------------
        protected NPC           Npc;
        protected NPCMovement   Movement  => Npc?.Movement;
        protected NPCAwareness  Awareness => Npc?.Awareness;
        protected NPCBehaviour  BehaviourManager => Npc?.BehaviourManager;

        protected int ConsecutivePathingFailures;
        protected const int MAX_PATHING_FAILURES = 5;

        // -------------------------------------------------------
        // Called by NPCScheduleManager
        // -------------------------------------------------------
        public void SetNPC(NPC npc) => Npc = npc;

        // -------------------------------------------------------
        // Template Method: Abstract contract
        // -------------------------------------------------------
        /// <summary>Human-readable name shown in debug/UI.</summary>
        public abstract string GetName();

        /// <summary>Description of the action's time window for UI display.</summary>
        public abstract string GetTimeDescription();

        /// <summary>
        /// The in-game minute at which this action should end.
        /// Return -1 for open-ended actions (signals handle their own termination).
        /// </summary>
        public abstract int GetEndTime();

        // -------------------------------------------------------
        // Virtual lifecycle hooks (override as needed)
        // -------------------------------------------------------

        /// <summary>
        /// Return true if this action should start right now.
        /// Default: always ready. Override to add conditions.
        /// </summary>
        public virtual bool ShouldStart() => true;

        /// <summary>Called the moment the action becomes active.</summary>
        public virtual void Started()      { ActionState = EActionState.Active; }

        /// <summary>Called on the tick after Started() — good for setup that depends on Started.</summary>
        public virtual void LateStarted()  { }

        /// <summary>Called every frame while active.</summary>
        public virtual void ActiveUpdate() { }

        /// <summary>Called each in-game minute while active.</summary>
        public virtual void MinPassed()    { }

        /// <summary>Called when the action ends normally (end time reached).</summary>
        public virtual void End()
        {
            ActionState = EActionState.Completed;
            Movement?.StopWalk(false);
            OnEnded?.Invoke();
        }

        /// <summary>Called when the action is stopped before its natural end.</summary>
        public virtual void Interrupt()
        {
            ActionState = EActionState.Interrupted;
            Movement?.StopWalk(false);
            OnEnded?.Invoke();
        }

        /// <summary>Called when the start time was passed without being started.</summary>
        public virtual void Skipped()
        {
            ActionState = EActionState.Skipped;
            OnEnded?.Invoke();
        }

        /// <summary>Called when trying to resume an interrupted action (JumpTo behaviour).</summary>
        public virtual void JumpTo()       { Started(); }

        /// <summary>Called when JumpTo fails (pathfinding failure).</summary>
        public virtual void ResumeFailed() { Interrupt(); }

        // -------------------------------------------------------
        // Helper: SetStartTime
        // -------------------------------------------------------
        public virtual void SetStartTime(int minute)
        {
            StartTime = Mathf.Clamp(minute, 0, 1439);
        }

        // -------------------------------------------------------
        // Pathing helper
        // -------------------------------------------------------
        protected virtual void WalkCallback(EWalkResult result)
        {
            if (result == EWalkResult.Failed || result == EWalkResult.Timeout)
            {
                ConsecutivePathingFailures++;
                if (ConsecutivePathingFailures >= MAX_PATHING_FAILURES)
                    OnPathingFailed();
            }
            else
            {
                ConsecutivePathingFailures = 0;
            }
        }

        protected virtual void OnPathingFailed()
        {
            Debug.LogWarning($"[NPCAction:{GetName()}] Max pathing failures.");
            Interrupt();
        }

        // -------------------------------------------------------
        // Debug
        // -------------------------------------------------------
        public override string ToString() =>
            $"{GetName()} [Start:{StartTime}] [State:{ActionState}]";
    }
}
