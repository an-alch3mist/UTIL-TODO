using System;
using UnityEngine;
using SPACE_UTIL;

namespace SPACE_DOODLE_CODEMONKEY
{
    /// <summary>
    /// Multi-phase boss. Phase 1 applies damage resistance.
    /// Transitions to phase 2 at the health threshold and fires OnPhaseChanged.
    /// Use for: end-of-level bosses, elite enemies, any entity with distinct behavioural phases.
    /// </summary>
    public class BossHealthSystem : HealthSystemBase
    {
        [Header("Health")]
        [SerializeField] private float startMaxHealth   = 500f;
        [Header("Phase")]
        [SerializeField] private float phaseThreshold   = 0.5f;  // 50 % HP triggers phase 2
        [SerializeField] private float phase1Resistance = 0.5f;  // 50 % damage reduction in phase 1

		public override float MaxHealth => startMaxHealth;
		public override float MaxArmour => 0f;

        private int phase = 1;

        /// <summary>Fired once when the boss crosses the phase threshold.</summary>
        public event Action OnPhaseChanged;

        private void Awake()
        {
            Debug.Log(C.method(this));
            InitCurrHealthArmour();
        }

        public override void TakeDamage(float amount)
        {
            if (phase == 1) amount *= (1f - phase1Resistance);

            base.TakeDamage(amount);

            if (phase == 1 && getHealthPercent < phaseThreshold)
            {
                phase = 2;
                OnPhaseChanged?.Invoke();
            }
        }
    }
}
