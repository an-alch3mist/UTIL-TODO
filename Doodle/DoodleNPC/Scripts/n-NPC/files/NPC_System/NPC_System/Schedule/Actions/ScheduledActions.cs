// ============================================================
//  ScheduledAction_WalkToLocation.cs
//  NPC walks to a named Transform at its scheduled start time.
//  Waits at the location until end time, then returns.
// ============================================================

using UnityEngine;

namespace NPCSystem
{
    public class ScheduledAction_WalkToLocation : NPCAction
    {
        [Header("Walk To Location")]
        [Tooltip("Scene object name to walk to.")]
        public string TargetObjectName;

        [Tooltip("Duration at target location in in-game minutes.")]
        public int DurationMinutes = 60;

        [Tooltip("Face this direction while waiting (world Y degrees). -1 = face walk direction.")]
        public float WaitFacingAngle = -1f;

        private Transform _target;
        private int       _startedAtMinute;
        private bool      _arrived;

        public override string GetName()            => $"Walk To {TargetObjectName}";
        public override string GetTimeDescription() =>
            $"{StartTime / 60:D2}:{StartTime % 60:D2} → {GetEndTime() / 60:D2}:{GetEndTime() % 60:D2}";
        public override int GetEndTime() => StartTime + DurationMinutes;

        public override void Started()
        {
            base.Started();
            _arrived = false;
            _startedAtMinute = GameClock.Instance?.CurrentMinute ?? StartTime;

            _target = GameObject.Find(TargetObjectName)?.transform;
            if (_target == null)
            {
                Debug.LogWarning($"[WalkToLocation] Target '{TargetObjectName}' not found.");
                End();
                return;
            }

            Movement?.WalkTo(_target.position, OnArrived);
        }

        private void OnArrived(EWalkResult result)
        {
            WalkCallback(result);
            if (result == EWalkResult.Success)
            {
                _arrived = true;
                if (WaitFacingAngle >= 0f)
                    Npc.transform.rotation = Quaternion.Euler(0f, WaitFacingAngle, 0f);
            }
        }

        public override void MinPassed()
        {
            // End check handled by NPCScheduleManager via GetEndTime()
        }

        public override void End()
        {
            base.End();
            _arrived = false;
        }
    }
}


// ============================================================
//  ScheduledAction_Idle.cs
//  NPC idles in place for a time window.
//  Simplest action type — just stands still.
// ============================================================
namespace NPCSystem
{
    public class ScheduledAction_Idle : NPCAction
    {
        [Header("Idle Action")]
        public int DurationMinutes = 30;

        public override string GetName()            => "Idle";
        public override string GetTimeDescription() =>
            $"{StartTime / 60:D2}:{StartTime % 60:D2} for {DurationMinutes} min";
        public override int GetEndTime() => StartTime + DurationMinutes;

        public override void Started()
        {
            base.Started();
            Movement?.StopWalk(false);
        }
    }
}


// ============================================================
//  ScheduledAction_Patrol.cs
//  NPC patrols a route during a scheduled time window.
//  Activates PatrolBehaviour and deactivates at end time.
// ============================================================
namespace NPCSystem
{
    public class ScheduledAction_Patrol : NPCAction
    {
        [Header("Patrol Action")]
        public PatrolRoute Route;
        public int         DurationMinutes = 120;

        public override string GetName()            => "Patrol";
        public override string GetTimeDescription() =>
            $"Patrol {StartTime / 60:D2}:{StartTime % 60:D2}→{GetEndTime() / 60:D2}:{GetEndTime() % 60:D2}";
        public override int GetEndTime() => StartTime + DurationMinutes;

        public override void Started()
        {
            base.Started();
            var patrol = BehaviourManager?.GetBehaviour<PatrolBehaviour>();
            if (patrol != null)
            {
                if (Route != null) patrol.Route = Route;
                patrol.Enable();
            }
        }

        public override void End()
        {
            base.End();
            BehaviourManager?.DisableBehaviour<PatrolBehaviour>();
        }

        public override void Interrupt()
        {
            base.Interrupt();
            BehaviourManager?.DisableBehaviour<PatrolBehaviour>();
        }
    }
}


// ============================================================
//  ScheduledAction_Sleep.cs
//  NPC sleeps at a bed/location. Freezes AI entirely.
// ============================================================
namespace NPCSystem
{
    public class ScheduledAction_Sleep : NPCAction
    {
        [Header("Sleep Action")]
        public string BedObjectName;
        public int    DurationMinutes = 480;   // 8 hours

        private UnityEngine.Transform _bed;

        public override string GetName()            => "Sleep";
        public override string GetTimeDescription() =>
            $"Sleep {StartTime / 60:D2}:{StartTime % 60:D2} ({DurationMinutes} min)";
        public override int GetEndTime() => StartTime + DurationMinutes;

        public override void Started()
        {
            base.Started();

            _bed = UnityEngine.GameObject.Find(BedObjectName)?.transform;
            if (_bed != null)
                Movement?.WalkTo(_bed.position, OnArrived);
            else
            {
                // No bed — sleep in place
                Movement?.StopWalk(false);
            }
        }

        private void OnArrived(EWalkResult result)
        {
            // Snap to bed position and freeze
            if (result == EWalkResult.Success && _bed != null)
            {
                Npc.transform.position = _bed.position;
                Npc.transform.rotation = _bed.rotation;
            }
            Movement?.EnableNavAgent(false);
        }

        public override void End()
        {
            base.End();
            Movement?.EnableNavAgent(true);
        }
    }
}
