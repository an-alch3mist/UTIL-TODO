// ============================================================
//  WanderBehaviour.cs
//  NPC wanders randomly within a radius, pausing at spots.
// ============================================================

using UnityEngine;
using UnityEngine.AI;

namespace NPCSystem
{
    public class WanderBehaviour : Behaviour
    {
        [Header("Wander Settings")]
        [SerializeField] private float _wanderRadius   = 8f;
        [SerializeField] private float _minWaitTime    = 2f;
        [SerializeField] private float _maxWaitTime    = 6f;
        [SerializeField] private bool  _runOccasionally = false;
        [SerializeField] [Range(0f, 1f)] private float _runChance = 0.15f;

        private enum EWanderState { Walking, Waiting }
        private EWanderState _state;
        private Vector3      _homePosition;
        private float        _waitTimer;
        private float        _waitDuration;

        protected override void Awake()
        {
            base.Awake();
            BehaviourName = "Wander";
            Priority      = BehaviourPriority.Wander;
        }

        public override void Initialize(NPC npc)
        {
            base.Initialize(npc);
            _homePosition = npc.transform.position;
        }

        protected override void OnActivated()
        {
            _state = EWanderState.Waiting;
            _waitTimer = 0f;
            _waitDuration = Random.Range(_minWaitTime, _maxWaitTime);
        }

        protected override void OnDeactivated()
        {
            Movement?.StopWalk(false);
        }

        public override void BehaviourUpdate()
        {
            switch (_state)
            {
                case EWanderState.Waiting:
                    _waitTimer += Time.deltaTime;
                    if (_waitTimer >= _waitDuration)
                        StartWalk();
                    break;

                case EWanderState.Walking:
                    // Walk callback handles transition; nothing to do per-frame
                    break;
            }
        }

        private void StartWalk()
        {
            Vector3 dest = GetRandomNavMeshPoint(_homePosition, _wanderRadius);
            bool run = _runOccasionally && Random.value < _runChance;

            bool ok = Movement.WalkTo(dest, OnWalkFinished, run);
            if (ok)
                _state = EWanderState.Walking;
            else
                BeginWait();
        }

        private void OnWalkFinished(EWalkResult result)
        {
            WalkCallback(result);
            BeginWait();
        }

        private void BeginWait()
        {
            _state        = EWanderState.Waiting;
            _waitTimer    = 0f;
            _waitDuration = Random.Range(_minWaitTime, _maxWaitTime);
        }

        private Vector3 GetRandomNavMeshPoint(Vector3 origin, float radius)
        {
            for (int i = 0; i < 10; i++)
            {
                Vector3 randomPoint = origin + Random.insideUnitSphere * radius;
                randomPoint.y = origin.y;

                if (NavMesh.SamplePosition(randomPoint, out var hit, 2f, NavMesh.AllAreas))
                    return hit.position;
            }
            return origin;
        }
    }
}
