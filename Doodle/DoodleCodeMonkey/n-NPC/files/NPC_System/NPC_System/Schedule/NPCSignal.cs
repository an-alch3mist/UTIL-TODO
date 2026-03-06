// ============================================================
//  NPCSignal.cs
//  A special NPCAction that represents a discrete, triggered
//  command rather than a time-window scheduled activity.
//
//  Signals can be fired at any time by external code and
//  interrupt the current schedule action (if lower priority).
//
//  Design: Extends NPCAction but adds:
//   - MaxDuration limit
//   - StartedThisCycle tracking (prevents re-firing same cycle)
//   - Default GetEndTime returns -1 (open-ended)
// ============================================================

using UnityEngine;

namespace NPCSystem
{
    public class NPCSignal : NPCAction
    {
        [Header("Signal Settings")]
        [Tooltip("Max in-game minutes before this signal times out. 0 = no limit.")]
        public int MaxDuration = 0;

        public bool StartedThisCycle { get; protected set; }

        // -------------------------------------------------------
        // Template Method overrides
        // -------------------------------------------------------
        public override string GetName()            => "Signal";
        public override string GetTimeDescription() => $"Signal @ {StartTime}";
        public override int    GetEndTime()         => -1;  // signals manage their own end

        public override void Started()
        {
            base.Started();
            StartedThisCycle = true;
        }

        public override void MinPassed()
        {
            if (MaxDuration > 0)
            {
                // Count minutes since started
                var clock = GameClock.Instance;
                if (clock != null)
                {
                    int elapsed = clock.MinutesUntil(StartTime);   // approximate
                    if (elapsed >= MaxDuration)
                        End();
                }
            }
        }

        /// <summary>Reset cycle flag at day start so signal can fire again.</summary>
        public void ResetCycle() => StartedThisCycle = false;
    }
}
