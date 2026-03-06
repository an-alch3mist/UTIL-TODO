// ============================================================
//  CoweringBehaviour.cs
//  NPC backs away from threat and plays cowering animation.
//  Triggered when alert level hits Alert but NPC can't fight.
// ============================================================

using UnityEngine;
using UnityEngine.AI;

namespace NPCSystem
{
    public class CoweringBehaviour : Behaviour
    {
        [Header("Cowering Settings")]
        [SerializeField] private float _backawayDistance  = 3f;
        [SerializeField] private float _lookAtThreatSpeed = 3f;

        protected override void Awake()
        {
            base.Awake();
            BehaviourName = "Cowering";
            Priority      = BehaviourPriority.Cowering;
        }

        protected override void OnActivated()
        {
            Animation?.TriggerScared();
            TryBackAway();
        }

        protected override void OnDeactivated()
        {
            Movement?.StopWalk(false);
        }

        public override void BehaviourUpdate()
        {
            // Keep facing threat while cowering
            if (Awareness?.PrimaryThreat != null)
                Movement?.FaceDirection(Awareness.PrimaryThreat.transform.position,
                                        _lookAtThreatSpeed);
        }

        public override void OnActiveTick()
        {
            // Periodically re-evaluate back-away position
            TryBackAway();
        }

        private void TryBackAway()
        {
            if (Awareness?.PrimaryThreat == null) return;

            Vector3 awayDir  = (Npc.transform.position - Awareness.PrimaryThreat.transform.position).normalized;
            Vector3 backspot = Npc.transform.position + awayDir * _backawayDistance;

            if (NavMesh.SamplePosition(backspot, out var hit, 2f, NavMesh.AllAreas))
                Movement?.WalkTo(hit.position);
        }
    }
}


// ============================================================
//  HeavyFlinchBehaviour.cs
//  Plays a heavy flinch animation, briefly interrupts others.
//  Very high priority — short duration.
// ============================================================
namespace NPCSystem
{
    public class HeavyFlinchBehaviour : Behaviour
    {
        [Header("Flinch Settings")]
        [SerializeField] private float _flinchDuration = 0.8f;
        private float _flinchTimer;

        protected override void Awake()
        {
            base.Awake();
            BehaviourName  = "HeavyFlinch";
            Priority       = BehaviourPriority.HeavyFlinch;
        }

        /// <summary>Trigger the flinch from outside (e.g. NPCHealth.OnDamaged).</summary>
        public void TriggerFlinch()
        {
            Enable();
            if (!Active) Activate();
        }

        protected override void OnActivated()
        {
            _flinchTimer = 0f;
            Animation?.TriggerHeavyHit();
            Movement?.StopWalk(false);
        }

        public override void BehaviourUpdate()
        {
            _flinchTimer += UnityEngine.Time.deltaTime;
            if (_flinchTimer >= _flinchDuration)
            {
                Disable();
            }
        }

        protected override void OnDisabled()
        {
            // Let NPCBehaviour re-evaluate after flinch
        }
    }
}


// ============================================================
//  FaceTargetBehaviour.cs
//  Makes the NPC smoothly face a target transform.
//  Used during conversations, quest hand-offs, etc.
// ============================================================
namespace NPCSystem
{
    public class FaceTargetBehaviour : Behaviour
    {
        [Header("Face Target Settings")]
        [SerializeField] private float _rotationSpeed = 5f;

        private UnityEngine.Transform _target;

        protected override void Awake()
        {
            base.Awake();
            BehaviourName = "FaceTarget";
            Priority      = BehaviourPriority.FaceTarget;
        }

        /// <summary>Set the target to face and enable this behaviour.</summary>
        public void SetTarget(UnityEngine.Transform target)
        {
            _target = target;
            if (target != null) Enable();
            else Disable();
        }

        public override void BehaviourUpdate()
        {
            if (_target == null) { Disable(); return; }
            Movement?.FaceDirection(_target.position, _rotationSpeed);
        }

        protected override void OnDeactivated()
        {
            _target = null;
        }
    }
}
