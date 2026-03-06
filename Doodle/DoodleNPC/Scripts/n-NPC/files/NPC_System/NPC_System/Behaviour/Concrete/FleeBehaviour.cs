// ============================================================
//  FleeBehaviour.cs
//  NPC runs away from the threat until out of sight range
//  or a safe distance is reached.
// ============================================================

using UnityEngine;
using UnityEngine.AI;

namespace NPCSystem
{
    public class FleeBehaviour : Behaviour
    {
        [Header("Flee Settings")]
        [SerializeField] private float _safeDistance       = 20f;
        [SerializeField] private float _recalcInterval     = 1.5f;
        [SerializeField] private float _fleePointScanRadius = 12f;
        [SerializeField] private int   _fleePointAttempts   = 8;

        private float _recalcTimer;

        protected override void Awake()
        {
            base.Awake();
            BehaviourName = "Flee";
            Priority      = BehaviourPriority.Flee;
        }

        protected override void OnActivated()
        {
            _recalcTimer = _recalcInterval; // force immediate flee calc
        }

        protected override void OnDeactivated()
        {
            Movement?.StopWalk(false);
        }

        public override void BehaviourUpdate()
        {
            if (Awareness?.PrimaryThreat == null) { Disable(); return; }

            float distToThreat = Vector3.Distance(
                Npc.transform.position,
                Awareness.PrimaryThreat.transform.position);

            if (distToThreat >= _safeDistance)
            {
                // Made it to safety
                Disable();
                Awareness.Calm();
                return;
            }

            _recalcTimer += Time.deltaTime;
            if (_recalcTimer >= _recalcInterval)
            {
                _recalcTimer = 0f;
                FleeFromThreat();
            }
        }

        private void FleeFromThreat()
        {
            Vector3 threatPos = Awareness.PrimaryThreat.transform.position;
            Vector3 awayDir   = (Npc.transform.position - threatPos).normalized;

            Vector3 bestPoint = Npc.transform.position;
            float   bestScore = 0f;

            for (int i = 0; i < _fleePointAttempts; i++)
            {
                // Try random points biased away from threat
                Vector3 candidate = Npc.transform.position +
                    (awayDir + Random.insideUnitSphere * 0.4f).normalized * _fleePointScanRadius;
                candidate.y = Npc.transform.position.y;

                if (!NavMesh.SamplePosition(candidate, out var hit, 2.5f, NavMesh.AllAreas))
                    continue;

                float score = Vector3.Distance(hit.position, threatPos);
                if (score > bestScore) { bestScore = bestPoint.y; bestPoint = hit.position; }
            }

            Movement?.WalkTo(bestPoint, WalkCallback, run: true);
        }

        protected override void WalkCallback(EWalkResult result) { /* recalc handles retry */ }
    }
}


// ============================================================
//  CombatBehaviour.cs
//  Melee or ranged combat. Chases target, attacks in range.
//  Respects CanFight / CanShoot from NPCDefinition.
// ============================================================
namespace NPCSystem
{
    public class CombatBehaviour : Behaviour
    {
        [Header("Combat Settings")]
        [SerializeField] private float _chaseUpdateInterval = 0.3f;

        private float _attackTimer;
        private float _chaseTimer;

        protected override void Awake()
        {
            base.Awake();
            BehaviourName = "Combat";
            Priority      = BehaviourPriority.Combat;
        }

        public override bool WantsToBeActive()
            => Enabled && Awareness?.PrimaryThreat != null;

        protected override void OnActivated()
        {
            _attackTimer = 0f;
            _chaseTimer  = _chaseUpdateInterval;
        }

        protected override void OnDeactivated()
        {
            Movement?.StopWalk(false);
        }

        public override void BehaviourUpdate()
        {
            if (Awareness?.PrimaryThreat == null) { Disable(); return; }

            float dist = Vector3.Distance(Npc.transform.position,
                                          Awareness.PrimaryThreat.transform.position);

            // Face threat
            Movement?.FaceDirection(Awareness.PrimaryThreat.transform.position, 8f);

            float attackRange = Npc.Definition?.AttackRange ?? 1.8f;

            if (dist <= attackRange)
            {
                // In attack range — stop and attack
                Movement?.StopWalk(false);
                _attackTimer += Time.deltaTime;

                float cooldown = Npc.Definition?.AttackCooldown ?? 1f;
                if (_attackTimer >= cooldown)
                {
                    _attackTimer = 0f;
                    DoAttack();
                }
            }
            else
            {
                // Chase
                _chaseTimer += Time.deltaTime;
                if (_chaseTimer >= _chaseUpdateInterval)
                {
                    _chaseTimer = 0f;
                    Movement?.WalkTo(Awareness.PrimaryThreat.transform.position,
                                     null, run: true);
                }
            }
        }

        private void DoAttack()
        {
            Animation?.TriggerAttack();

            var targetHealth = Awareness.PrimaryThreat.GetComponent<NPCHealth>();
            if (targetHealth != null)
                targetHealth.TakeDamage(Npc.Definition?.AttackDamage ?? 15f,
                                        Npc.gameObject, EKnockdownCause.Melee);

            // For ranged: add projectile spawn here
        }
    }
}
