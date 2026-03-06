// ============================================================
//  IdleBehaviour.cs
//  Default fallback — NPC stands in place with idle anims.
//  The lowest-priority behaviour; always enabled.
// ============================================================

using UnityEngine;

namespace NPCSystem
{
    public class IdleBehaviour : Behaviour
    {
        [Header("Idle Settings")]
        [SerializeField] private float _idleAnimVariantChangeTime = 8f;
        [SerializeField] private int   _idleVariantCount = 3;

        private float _variantTimer;

        protected override void Awake()
        {
            base.Awake();
            BehaviourName = "Idle";
            Priority      = BehaviourPriority.Idle;
            EnabledOnAwake = true;
        }

        protected override void OnActivated()
        {
            Movement?.StopWalk(false);
            Animation?.SetIdleVariant(Random.Range(0, _idleVariantCount));
            _variantTimer = 0f;
        }

        public override void BehaviourUpdate()
        {
            _variantTimer += Time.deltaTime;
            if (_variantTimer >= _idleAnimVariantChangeTime)
            {
                _variantTimer = 0f;
                Animation?.SetIdleVariant(Random.Range(0, _idleVariantCount));
            }
        }
    }
}
