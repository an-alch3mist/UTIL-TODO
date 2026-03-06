// ============================================================
//  NPCSignal_WalkToLocation.cs
//  Immediately walks NPC to a target position.
//  Fires OnEnded when arrived or failed.
// ============================================================

using System;

using UnityEngine;

namespace NPCSystem
{
    public class NPCSignal_WalkToLocation : NPCSignal
    {
        [Header("Walk Signal")]
        public Transform  TargetTransform;
        public Vector3    TargetPosition;
        public bool       UseTransform = true;
        public bool       RunToTarget  = false;
        public bool       TeleportOnFail = false;

        public override string GetName()            => "Signal_WalkToLocation";
        public override string GetTimeDescription() => "Walk Signal";

        public override void Started()
        {
            base.Started();
            Vector3 dest = UseTransform && TargetTransform != null
                ? TargetTransform.position
                : TargetPosition;

            Movement?.WalkTo(dest, OnArrived, RunToTarget, TeleportOnFail);
        }

        private void OnArrived(EWalkResult result)
        {
            WalkCallback(result);
            End();
        }

        protected override void OnPathingFailed()
        {
            if (TeleportOnFail)
            {
                Vector3 dest = UseTransform && TargetTransform != null
                    ? TargetTransform.position : TargetPosition;
                Movement?.Teleport(dest);
                End();
            }
            else
            {
                Interrupt();
            }
        }
    }
}


// ============================================================
//  NPCSignal_WaitForDuration.cs
//  NPC stands in place for N in-game minutes.
// ============================================================
namespace NPCSystem
{
    public class NPCSignal_WaitForDuration : NPCSignal
    {
        [Header("Wait Signal")]
        public int WaitMinutes = 5;

        private int _minutesWaited;

        public override string GetName()            => "Signal_Wait";
        public override string GetTimeDescription() => $"Wait {WaitMinutes} min";
        public override int    GetEndTime()         => -1;

        public override void Started()
        {
            base.Started();
            _minutesWaited = 0;
            Movement?.StopWalk(false);
        }

        public override void MinPassed()
        {
            _minutesWaited++;
            if (_minutesWaited >= WaitMinutes)
                End();
        }
    }
}


// ============================================================
//  NPCSignal_FaceTarget.cs
//  NPC faces a target transform for N seconds, then ends.
// ============================================================

namespace NPCSystem
{
    public class NPCSignal_FaceTarget : NPCSignal
    {
        [Header("Face Target Signal")]
        public Transform  Target;
        public float      Duration = 3f;

        private float _timer;

        public override string GetName()            => "Signal_FaceTarget";
        public override string GetTimeDescription() => "Face Target";
        public override int    GetEndTime()         => -1;

        public override void Started()
        {
            base.Started();
            _timer = 0f;
            Movement?.StopWalk(false);

            var face = BehaviourManager?.GetBehaviour<FaceTargetBehaviour>();
            face?.SetTarget(Target);
        }

        public override void ActiveUpdate()
        {
            _timer += Time.deltaTime;
            if (_timer >= Duration)
                End();
        }

        public override void End()
        {
            BehaviourManager?.GetBehaviour<FaceTargetBehaviour>()?.Disable();
            base.End();
        }
    }
}


// ============================================================
//  NPCSignal_UseObject.cs
//  NPC walks to an interactable object and "uses" it.
//  Fires OnObjectUsed callback when interaction completes.
// ============================================================

namespace NPCSystem
{
    public class NPCSignal_UseObject : NPCSignal
    {
        [Header("Use Object Signal")]
        public UnityEngine.Transform ObjectTransform;
        public float                 UseTime = 3f;   // real-time seconds at the object
        public float                 UseRange = 1.5f;

        public event Action OnObjectUsed;

        private enum EState { WalkingTo, Using }
        private EState _state;
        private float  _useTimer;

        public override string GetName()            => "Signal_UseObject";
        public override string GetTimeDescription() => "Use Object";
        public override int    GetEndTime()         => -1;

        public override void Started()
        {
            base.Started();

            if (ObjectTransform == null) { Interrupt(); return; }

            _state = EState.WalkingTo;
            Movement?.WalkTo(ObjectTransform.position, OnArrived);
        }

        private void OnArrived(EWalkResult result)
        {
            if (result != EWalkResult.Success) { WalkCallback(result); return; }

            _state     = EState.Using;
            _useTimer  = 0f;
            Movement?.StopWalk(false);
            Movement?.FaceDirection(ObjectTransform.position);
           // Animation?.TriggerTalk();   // generic interaction anim — replace with specific
        }

        public override void ActiveUpdate()
        {
            if (_state != EState.Using) return;

            _useTimer += UnityEngine.Time.deltaTime;
            if (_useTimer >= UseTime)
            {
                OnObjectUsed?.Invoke();
                End();
            }
        }
    }
}
