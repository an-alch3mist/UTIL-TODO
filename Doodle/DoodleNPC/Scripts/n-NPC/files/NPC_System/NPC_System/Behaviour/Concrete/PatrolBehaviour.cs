// ============================================================
//  StationaryBehaviour.cs
//  NPC stands at a fixed position (workstation, door guard).
//  When activated, walks TO the station point, then stands.
// ============================================================

using UnityEngine;

namespace NPCSystem
{
    public class StationaryBehaviour : Behaviour
    {
        [Header("Stationary Settings")]
        [SerializeField] private Transform _stationPoint;
        [SerializeField] private float     _faceDirectionAngle = 0f;   // world-space Y degrees

        private enum EState { Walking, Standing }
        private EState _state;

        public Transform StationPoint
        {
            get => _stationPoint;
            set => _stationPoint = value;
        }

        protected override void Awake()
        {
            base.Awake();
            BehaviourName = "Stationary";
            Priority      = BehaviourPriority.Stationary;
        }

        protected override void OnActivated()
        {
            if (_stationPoint == null) { _state = EState.Standing; return; }

            _state = EState.Walking;
            Movement?.WalkTo(_stationPoint.position, OnArrived);
        }

        protected override void OnDeactivated()
        {
            Movement?.StopWalk(false);
        }

        private void OnArrived(EWalkResult result)
        {
            _state = EState.Standing;
            // Face configured direction
            Npc.transform.rotation = Quaternion.Euler(0f, _faceDirectionAngle, 0f);
        }

        public override void BehaviourUpdate()
        {
            // Stand still — nothing to do per-frame
        }
    }
}


// ============================================================
//  PatrolBehaviour.cs
//  NPC walks a PatrolRoute (loop, ping-pong, or random).
//  Supports wait time at each waypoint.
// ============================================================
namespace NPCSystem
{
    public class PatrolBehaviour : Behaviour
    {
        [Header("Patrol Settings")]
        [SerializeField] private PatrolRoute _patrolRoute;

        private enum EPatrolState { Walking, Waiting }
        private EPatrolState _state;
        private int          _currentIndex;
        private int          _direction = 1;
        private float        _waitTimer;

        public PatrolRoute Route
        {
            get => _patrolRoute;
            set { _patrolRoute = value; if (_patrolRoute != null) _patrolRoute.Resolve(); }
        }

        protected override void Awake()
        {
            base.Awake();
            BehaviourName = "Patrol";
            Priority      = BehaviourPriority.Patrol;
        }

        public override void Initialize(NPC npc)
        {
            base.Initialize(npc);
            _patrolRoute?.Resolve();
        }

        protected override void OnActivated()
        {
            if (_patrolRoute == null || _patrolRoute.ResolvedWaypoints.Count == 0)
            {
                Disable();
                return;
            }
            _state = EPatrolState.Waiting;
            _waitTimer = 0f;
        }

        protected override void OnDeactivated()
        {
            Movement?.StopWalk(false);
        }

        public override void BehaviourUpdate()
        {
            switch (_state)
            {
                case EPatrolState.Waiting:
                    _waitTimer += UnityEngine.Time.deltaTime;
                    if (_waitTimer >= _patrolRoute.WaypointWaitTime)
                        WalkToNext();
                    break;
            }
        }

        private void WalkToNext()
        {
            if (_patrolRoute.ResolvedWaypoints.Count == 0) return;

            var wp = _patrolRoute.ResolvedWaypoints[_currentIndex];
            _state = EPatrolState.Walking;

            Movement?.WalkTo(wp.position, OnWaypointReached, _patrolRoute.RunBetweenWaypoints);
        }

        private void OnWaypointReached(EWalkResult result)
        {
            WalkCallback(result);

            // Advance to next waypoint
            _currentIndex = _patrolRoute.GetNextIndex(_currentIndex, ref _direction);

            // Wait at current waypoint
            _state     = EPatrolState.Waiting;
            _waitTimer = 0f;
        }

        protected override void OnPathingFailed()
        {
            base.OnPathingFailed();
            // Skip to next waypoint if stuck
            _currentIndex = _patrolRoute.GetNextIndex(_currentIndex, ref _direction);
            ConsecutivePathingFailures = 0;
        }
    }
}
