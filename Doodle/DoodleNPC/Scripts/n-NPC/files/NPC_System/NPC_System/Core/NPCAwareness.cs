// ============================================================
//  NPCAwareness.cs
//  Vision, hearing, and threat awareness.
//
//  Design:
//   - Tick-based (not every frame) for performance
//   - Line-of-sight raycasting through occlusion mask
//   - Alert level ramps up when threats detected
//   - Alert level decays when threat is lost
//   - NPCBehaviour listens to OnAlertLevelChanged
//     and enables the right behaviour
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace NPCSystem
{
    [RequireComponent(typeof(NPC))]
    public class NPCAwareness : MonoBehaviour
    {
        // -------------------------------------------------------
        // Events
        // -------------------------------------------------------
        public event Action<EAlertLevel, EAlertLevel> OnAlertLevelChanged;  // (old, new)
        public event Action<GameObject>               OnThreatSpotted;
        public event Action<GameObject>               OnThreatLost;
        public event Action<Vector3>                  OnSoundHeard;

        // -------------------------------------------------------
        // State
        // -------------------------------------------------------
        public EAlertLevel      AlertLevel        { get; private set; } = EAlertLevel.Calm;
        public GameObject       PrimaryThreat     { get; private set; }
        public Vector3          LastKnownThreatPos{ get; private set; }
        public bool             HasLineOfSight    { get; private set; }
        public float            Suspicion         { get; private set; }   // 0–1

        // -------------------------------------------------------
        // Inspector
        // -------------------------------------------------------
        [Header("Tick Rate")]
        [SerializeField] private float _awarenessTickRate = 0.25f;   // seconds between checks

        [Header("Debug")]
        [SerializeField] private bool  _drawGizmos = false;

        // -------------------------------------------------------
        // Private
        // -------------------------------------------------------
        private NPC           _npc;
        private NPCDefinition _def;
        private float         _tickTimer;
        private float         _lostSightTimer;
        private const float   LOST_SIGHT_GRACE = 3f;   // seconds before threat is "lost"
        private readonly List<GameObject> _knownThreats = new List<GameObject>();

        // -------------------------------------------------------
        // Unity
        // -------------------------------------------------------
        private void Awake()
        {
            _npc = GetComponent<NPC>();
        }

        public void Initialize(NPCDefinition def)
        {
            _def = def;
        }

        private void Update()
        {
            _tickTimer += Time.deltaTime;
            if (_tickTimer < _awarenessTickRate) return;
            _tickTimer = 0f;

            RunAwarenessTick();
        }

        // -------------------------------------------------------
        // Core tick
        // -------------------------------------------------------
        private void RunAwarenessTick()
        {
            if (_def == null || !_npc.IsAlive) return;

            bool spotted = CheckForThreats();

            if (spotted)
            {
                _lostSightTimer = 0f;

                if (AlertLevel < EAlertLevel.Alert)
                    SetAlertLevel(EAlertLevel.Alert);
            }
            else if (PrimaryThreat != null)
            {
                _lostSightTimer += _awarenessTickRate;
                HasLineOfSight   = false;

                if (_lostSightTimer >= LOST_SIGHT_GRACE)
                    LoseThreat();
            }
            else
            {
                // Calm down over time
                if (AlertLevel > EAlertLevel.Calm && _def.CalmDownRate > 0f)
                {
                    Suspicion -= _def.CalmDownRate * _awarenessTickRate;
                    if (Suspicion <= 0f)
                    {
                        Suspicion = 0f;
                        SetAlertLevel(EAlertLevel.Calm);
                    }
                }
            }
        }

        // -------------------------------------------------------
        // Vision check
        // -------------------------------------------------------
        private bool CheckForThreats()
        {
            // Search for "Player" tagged objects — extend this to
            // any tagged threat (e.g. "Enemy") as needed.
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            bool spotted = false;

            foreach (var player in players)
            {
                if (CanSee(player.transform))
                {
                    spotted = true;

                    if (PrimaryThreat != player)
                    {
                        PrimaryThreat = player;
                        OnThreatSpotted?.Invoke(player);
                    }

                    LastKnownThreatPos = player.transform.position;
                    HasLineOfSight     = true;
                    break;
                }
            }

            return spotted;
        }

        /// <summary>True if this NPC has clear line-of-sight to target.</summary>
        public bool CanSee(Transform target)
        {
            if (_def == null || target == null) return false;

            Vector3 toTarget = target.position - transform.position;
            float   dist     = toTarget.magnitude;

            // Range check
            if (dist > _def.SightRange) return false;

            // Angle check
            float angle = Vector3.Angle(transform.forward, toTarget.normalized);
            if (angle > _def.SightAngle * 0.5f) return false;

            // Occlusion check
            Vector3 eyePos    = transform.position + Vector3.up * 1.6f;
            Vector3 targetPos = target.position    + Vector3.up * 1.0f;
            Vector3 dir       = (targetPos - eyePos).normalized;

            if (Physics.Raycast(eyePos, dir, dist, _def.SightOcclusionMask))
                return false;

            return true;
        }

        /// <summary>True if target is within hearing range.</summary>
        public bool CanHear(Vector3 position, float soundRadius)
        {
            if (_def == null) return false;
            float effectiveRange = Mathf.Min(soundRadius, _def.HearingRange);
            return Vector3.Distance(transform.position, position) <= effectiveRange;
        }

        // -------------------------------------------------------
        // External triggers
        // -------------------------------------------------------

        /// <summary>Call when this NPC hears a loud sound (gunshot, explosion).</summary>
        public void HearSound(Vector3 position, float radius)
        {
            if (!CanHear(position, radius)) return;

            OnSoundHeard?.Invoke(position);
            LastKnownThreatPos = position;

            if (AlertLevel < EAlertLevel.Suspicious)
                SetAlertLevel(EAlertLevel.Suspicious);
        }

        /// <summary>Immediately set a specific threat (called by combat, crime witness).</summary>
        public void SetThreat(GameObject threat)
        {
            if (threat == null) return;

            bool isNew = PrimaryThreat != threat;
            PrimaryThreat      = threat;
            LastKnownThreatPos = threat.transform.position;
            _lostSightTimer    = 0f;

            SetAlertLevel(EAlertLevel.Combat);

            if (isNew)
                OnThreatSpotted?.Invoke(threat);
        }

        /// <summary>Manually force the NPC back to calm.</summary>
        public void Calm()
        {
            LoseThreat();
            SetAlertLevel(EAlertLevel.Calm);
            Suspicion = 0f;
        }

        // -------------------------------------------------------
        // Private helpers
        // -------------------------------------------------------
        private void LoseThreat()
        {
            if (PrimaryThreat != null)
            {
                var lost = PrimaryThreat;
                PrimaryThreat   = null;
                HasLineOfSight  = false;
                _lostSightTimer = 0f;
                OnThreatLost?.Invoke(lost);
            }

            if (AlertLevel >= EAlertLevel.Alert)
                SetAlertLevel(EAlertLevel.Suspicious);
        }

        private void SetAlertLevel(EAlertLevel newLevel)
        {
            if (newLevel == AlertLevel) return;
            var old = AlertLevel;
            AlertLevel = newLevel;
            OnAlertLevelChanged?.Invoke(old, newLevel);
        }

        // -------------------------------------------------------
        // Gizmos
        // -------------------------------------------------------
        private void OnDrawGizmosSelected()
        {
            if (!_drawGizmos || _def == null) return;

            // Sight range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _def.SightRange);

            // Hearing range
            Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, _def.HearingRange);

            // Sight cone (approximate with two lines)
            Gizmos.color = Color.yellow;
            float halfAngle = _def.SightAngle * 0.5f * Mathf.Deg2Rad;
            Vector3 left    = Quaternion.Euler(0, -_def.SightAngle * 0.5f, 0) * transform.forward;
            Vector3 right   = Quaternion.Euler(0,  _def.SightAngle * 0.5f, 0) * transform.forward;
            Gizmos.DrawRay(transform.position, left  * _def.SightRange);
            Gizmos.DrawRay(transform.position, right * _def.SightRange);
        }
    }
}
