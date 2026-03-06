// ============================================================
//  NPCAnimation.cs
//  Wraps Unity's Animator with a type-safe API.
//  All animator parameter names are stored as static hashes
//  to avoid per-frame string comparisons.
//
//  Design:
//   - Subscribe to NPCHealth/NPCMovement events
//   - Expose SetXxx() methods that behaviours call
//   - Never call animator.SetXxx("stringName") at runtime;
//     always use cached int hashes
// ============================================================

using UnityEngine;

namespace NPCSystem
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(NPC))]
    public class NPCAnimation : MonoBehaviour
    {
        // -------------------------------------------------------
        // Animator parameter hashes  (no string lookups at runtime)
        // -------------------------------------------------------
        private static readonly int HASH_SPEED        = Animator.StringToHash("Speed");
        private static readonly int HASH_IS_RUNNING   = Animator.StringToHash("IsRunning");
        private static readonly int HASH_IS_CROUCHING = Animator.StringToHash("IsCrouching");
        private static readonly int HASH_IS_DEAD      = Animator.StringToHash("IsDead");
        private static readonly int HASH_IS_UNCONSCIOUS = Animator.StringToHash("IsUnconscious");
        private static readonly int HASH_ALERT_LEVEL  = Animator.StringToHash("AlertLevel");
        private static readonly int HASH_HIT          = Animator.StringToHash("Hit");
        private static readonly int HASH_HEAVY_HIT    = Animator.StringToHash("HeavyHit");
        private static readonly int HASH_ATTACK       = Animator.StringToHash("Attack");
        private static readonly int HASH_GREET        = Animator.StringToHash("Greet");
        private static readonly int HASH_SCARED       = Animator.StringToHash("Scared");
        private static readonly int HASH_TALK         = Animator.StringToHash("Talk");
        private static readonly int HASH_IDLE_VARIANT = Animator.StringToHash("IdleVariant");

        // -------------------------------------------------------
        // Inspector
        // -------------------------------------------------------
        [Header("Ragdoll")]
        [SerializeField] private Rigidbody[] _ragdollBodies;
        [SerializeField] private Collider[]  _ragdollColliders;

        // -------------------------------------------------------
        // State
        // -------------------------------------------------------
        public bool RagdollActive { get; private set; }

        // -------------------------------------------------------
        // Private
        // -------------------------------------------------------
        private Animator     _animator;
        private NPC          _npc;
        private NPCHealth    _health;
        private NPCMovement  _movement;

        // -------------------------------------------------------
        // Unity
        // -------------------------------------------------------
        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _npc      = GetComponent<NPC>();
            _health   = GetComponent<NPCHealth>();
            _movement = GetComponent<NPCMovement>();
        }

        private void Start()
        {
            // Subscribe to health events
            if (_health != null)
            {
                _health.OnDamaged    += HandleDamaged;
                _health.OnKnockedOut += HandleKnockedOut;
                _health.OnRevived    += HandleRevived;
                _health.OnDied       += HandleDied;
            }

            // Set ragdoll to kinematic by default
            SetRagdollActive(false);
        }

        private void OnDestroy()
        {
            if (_health != null)
            {
                _health.OnDamaged    -= HandleDamaged;
                _health.OnKnockedOut -= HandleKnockedOut;
                _health.OnRevived    -= HandleRevived;
                _health.OnDied       -= HandleDied;
            }
        }

        private void Update()
        {
            if (RagdollActive || _animator == null) return;

            // Drive Speed from NavMesh velocity
            if (_movement != null)
            {
                float speed = _movement.IsMoving
                    ? _movement.LocomotionState == ELocomotionState.Running ? 2f : 1f
                    : 0f;
                _animator.SetFloat(HASH_SPEED, speed, 0.1f, Time.deltaTime);
                _animator.SetBool(HASH_IS_RUNNING,
                    _movement.LocomotionState == ELocomotionState.Running);
            }
        }

        // -------------------------------------------------------
        // Public setters
        // -------------------------------------------------------
        public void SetCrouching(bool crouching)
            => _animator?.SetBool(HASH_IS_CROUCHING, crouching);

        public void SetAlertLevel(EAlertLevel level)
            => _animator?.SetInteger(HASH_ALERT_LEVEL, (int)level);

        public void TriggerHit()       => _animator?.SetTrigger(HASH_HIT);
        public void TriggerHeavyHit()  => _animator?.SetTrigger(HASH_HEAVY_HIT);
        public void TriggerAttack()    => _animator?.SetTrigger(HASH_ATTACK);
        public void TriggerGreet()     => _animator?.SetTrigger(HASH_GREET);
        public void TriggerScared()    => _animator?.SetTrigger(HASH_SCARED);
        public void TriggerTalk()      => _animator?.SetTrigger(HASH_TALK);

        public void SetIdleVariant(int variant)
            => _animator?.SetInteger(HASH_IDLE_VARIANT, variant);

        // -------------------------------------------------------
        // Ragdoll
        // -------------------------------------------------------
        public void SetRagdollActive(bool active)
        {
            RagdollActive = active;

            if (_animator != null)
                _animator.enabled = !active;

            foreach (var rb in _ragdollBodies)
            {
                rb.isKinematic = !active;
                rb.useGravity  = active;
            }

            foreach (var col in _ragdollColliders)
                col.enabled = active;
        }

        /// <summary>Apply a physics impulse to ragdoll bodies (e.g. explosion knockback).</summary>
        public void ApplyRagdollForce(Vector3 force, Vector3 origin)
        {
            if (!RagdollActive) return;
            foreach (var rb in _ragdollBodies)
                rb.AddExplosionForce(force.magnitude, origin, 3f, 0.5f, ForceMode.Impulse);
        }

        // -------------------------------------------------------
        // Health event handlers
        // -------------------------------------------------------
        private void HandleDamaged(float amount, GameObject source)
        {
            if (amount > 30f)
                TriggerHeavyHit();
            else
                TriggerHit();
        }

        private void HandleKnockedOut(EKnockdownCause cause)
        {
            _animator?.SetBool(HASH_IS_UNCONSCIOUS, true);
            _animator?.SetBool(HASH_IS_DEAD, false);

            if (cause == EKnockdownCause.Explosion || cause == EKnockdownCause.Fall)
                SetRagdollActive(true);
        }

        private void HandleRevived()
        {
            SetRagdollActive(false);
            _animator?.SetBool(HASH_IS_UNCONSCIOUS, false);
        }

        private void HandleDied(EKnockdownCause cause)
        {
            _animator?.SetBool(HASH_IS_DEAD, true);

            if (cause == EKnockdownCause.Explosion || cause == EKnockdownCause.Gunshot)
                SetRagdollActive(true);
        }
    }
}
