// ============================================================
//  NPCMovement.cs
//  Wraps Unity's NavMeshAgent to provide a clean API for
//  NPC locomotion with typed callbacks.
//
//  Design:
//   - Behaviours/Actions call SetDestination() and get a
//     callback via WalkResult when the walk ends
//   - Supports Walk / Run / Combat speed tiers
//   - Teleport-on-fail option for stuck NPCs
//   - Swap NavMeshAgent for A* Pathfinding Pro by replacing
//     the _agent calls — the public API stays identical
//
//  To replace with A*: implement the same methods calling
//  Seeker.StartPath() and IAstarAI instead of NavMeshAgent.
// ============================================================

using System;
using UnityEngine;
using UnityEngine.AI;

namespace NPCSystem
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(NPC))]
    public class NPCMovement : MonoBehaviour
    {
        // -------------------------------------------------------
        // Nested: walk result callback
        // -------------------------------------------------------
        public delegate void WalkCallback(EWalkResult result);

        // -------------------------------------------------------
        // Inspector
        // -------------------------------------------------------
        [Header("Pathing")]
        [Tooltip("Max seconds to reach a destination before timing out.")]
        [SerializeField] private float _walkTimeout = 15f;

        [Tooltip("If true, teleport to destination if path fails.")]
        [SerializeField] private bool _teleportOnFail = false;

        [Tooltip("Distance within which we consider the NPC has arrived.")]
        [SerializeField] private float _arrivalThreshold = 0.35f;

        // -------------------------------------------------------
        // State
        // -------------------------------------------------------
        public bool           IsMoving    { get; private set; }
        public Vector3        Destination { get; private set; }
        public ELocomotionState LocomotionState { get; private set; }

        // -------------------------------------------------------
        // Private
        // -------------------------------------------------------
        private NavMeshAgent  _agent;
        private NPC           _npc;
        private WalkCallback  _onWalkComplete;
        private float         _walkTimer;
        private bool          _hasActiveWalk;
        private Vector3       _prevPosition;
        private float         _stuckTimer;
        private const float   STUCK_THRESHOLD = 0.05f;   // min movement per second
        private const float   STUCK_TIMEOUT   = 3f;

        // -------------------------------------------------------
        // Unity
        // -------------------------------------------------------
        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _npc   = GetComponent<NPC>();

            _agent.stoppingDistance = 0.4f;
            _agent.updateRotation   = true;
            _agent.updatePosition   = true;
        }

        public void Initialize(NPCDefinition def)
        {
            _agent.speed            = def.WalkSpeed;
            _agent.angularSpeed     = def.RotationSpeed * 100f;
            _agent.stoppingDistance = def.StoppingDistance;
        }

        private void Update()
        {
            if (!_hasActiveWalk) return;

            _walkTimer += Time.deltaTime;

            // Timeout check
            if (_walkTimer >= _walkTimeout)
            {
                FinishWalk(EWalkResult.Timeout);
                return;
            }

            // Arrival check
            if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
            {
                if (!_agent.hasPath || _agent.velocity.sqrMagnitude < 0.01f)
                {
                    FinishWalk(EWalkResult.Success);
                    return;
                }
            }

            // Stuck detection
            float moved = Vector3.Distance(transform.position, _prevPosition);
            if (moved / Time.deltaTime < STUCK_THRESHOLD)
            {
                _stuckTimer += Time.deltaTime;
                if (_stuckTimer >= STUCK_TIMEOUT)
                {
                    if (_teleportOnFail)
                    {
                        transform.position = Destination;
                        FinishWalk(EWalkResult.Success);
                    }
                    else
                    {
                        FinishWalk(EWalkResult.Failed);
                    }
                    return;
                }
            }
            else
            {
                _stuckTimer = 0f;
            }

            _prevPosition = transform.position;
        }

        // -------------------------------------------------------
        // Public API
        // -------------------------------------------------------

        /// <summary>Walk to a world position, fire callback on completion.</summary>
        public bool WalkTo(Vector3 destination, WalkCallback callback = null,
                           bool run = false, bool teleportOnFail = false)
        {
            StopWalk(false);

            if (!NavMesh.SamplePosition(destination, out var hit, 2f, NavMesh.AllAreas))
            {
                if (teleportOnFail) { transform.position = destination; callback?.Invoke(EWalkResult.Success); }
                else callback?.Invoke(EWalkResult.Failed);
                return false;
            }

            Destination     = hit.position;
            _onWalkComplete = callback;
            _walkTimer      = 0f;
            _stuckTimer     = 0f;
            _prevPosition   = transform.position;
            _hasActiveWalk  = true;
            IsMoving        = true;

            SetSpeedMode(run ? ELocomotionState.Running : ELocomotionState.Walking);
            _agent.SetDestination(Destination);
            _agent.isStopped = false;
            return true;
        }

        /// <summary>Walk toward a Transform (dynamic destination, updates each frame).</summary>
        public bool WalkToTransform(Transform target, WalkCallback callback = null,
                                    float arriveRange = 1.5f, bool run = false)
        {
            StopWalk(false);
            _onWalkComplete = callback;
            _walkTimer      = 0f;
            _stuckTimer     = 0f;
            _hasActiveWalk  = true;
            IsMoving        = true;

            SetSpeedMode(run ? ELocomotionState.Running : ELocomotionState.Walking);

            // Start coroutine-style follow (simple version)
            StartCoroutine(FollowTransform(target, arriveRange));
            return true;
        }

        private System.Collections.IEnumerator FollowTransform(Transform target, float arriveRange)
        {
            while (_hasActiveWalk && target != null)
            {
                _agent.SetDestination(target.position);
                if (Vector3.Distance(transform.position, target.position) <= arriveRange)
                {
                    FinishWalk(EWalkResult.Success);
                    yield break;
                }
                yield return new WaitForSeconds(0.2f);
            }
        }

        /// <summary>Stop all movement immediately. Optionally fire interrupted callback.</summary>
        public void StopWalk(bool fireCallback = true)
        {
            if (_hasActiveWalk && fireCallback)
                FinishWalk(EWalkResult.Interrupted);

            _hasActiveWalk = false;
            IsMoving       = false;

            if (_agent.isActiveAndEnabled && _agent.isOnNavMesh)
            {
                _agent.isStopped = true;
                _agent.ResetPath();
            }
        }

        /// <summary>Face toward a world position over time.</summary>
        public void FaceDirection(Vector3 worldPosition, float speedMultiplier = 1f)
        {
            Vector3 dir = (worldPosition - transform.position).normalized;
            if (dir == Vector3.zero) return;
            dir.y = 0;
            Quaternion target = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, target,
                Time.deltaTime * (_agent.angularSpeed / 100f) * speedMultiplier);
        }

        /// <summary>Teleport immediately without pathfinding.</summary>
        public void Teleport(Vector3 position)
        {
            StopWalk(false);
            _agent.Warp(position);
        }

        // -------------------------------------------------------
        // Speed modes
        // -------------------------------------------------------
        public void SetSpeedMode(ELocomotionState mode)
        {
            LocomotionState = mode;

            if (_npc.Definition == null) return;

            switch (mode)
            {
                case ELocomotionState.Walking:  _agent.speed = _npc.Definition.WalkSpeed;   break;
                case ELocomotionState.Running:  _agent.speed = _npc.Definition.RunSpeed;    break;
                case ELocomotionState.Idle:
                case ELocomotionState.Crouching: _agent.speed = _npc.Definition.WalkSpeed * 0.5f; break;
            }
        }

        // -------------------------------------------------------
        // Enable / Disable agent
        // -------------------------------------------------------
        public void EnableNavAgent(bool enable)
        {
            if (_agent.isActiveAndEnabled)
                _agent.enabled = enable;
        }

        // -------------------------------------------------------
        // Private helpers
        // -------------------------------------------------------
        private void FinishWalk(EWalkResult result)
        {
            bool wasActive = _hasActiveWalk;
            _hasActiveWalk = false;
            IsMoving       = false;

            if (_agent.isActiveAndEnabled && _agent.isOnNavMesh)
                _agent.isStopped = true;

            if (wasActive)
                _onWalkComplete?.Invoke(result);

            _onWalkComplete = null;
        }
    }
}
