using UnityEngine;
using SPACE_UTIL;

namespace SPACE_DOODLE_CODEMONKEY
{
    /// <summary>
    /// Invincible entity. All damage and kill commands are no-ops.
    /// Swap in the Inspector at runtime — zero code changes anywhere else.
    /// Use for: cutscene characters, tutorial sequences, debug/cheat modes.
    /// </summary>
    public class GodModeHealthSystem : HealthSystemBase
    {
		public override float MaxHealth => 1f;
		public override float MaxArmour => 0f;

        private void Awake()
        {
            Debug.Log(C.method(this));
            InitCurrHealthArmour();
        }

        // ── Queries always report full / alive ─────────────────────────────
        public override float getHealthPercent => 1f;
        public override float getArmourPercent => 0f;
        public override bool  getIsAlive       => true;

        // ── All commands are intentional no-ops ────────────────────────────
        public override void TakeDamage(float amount)  { }
        public override void Kill()                    { }
        public override void Heal(float amount)         { }
        public override void RepairArmour(float amount) { }
        public override void Revive(float healthAmount) { }
    }
}
