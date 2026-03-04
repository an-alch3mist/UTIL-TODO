using UnityEngine;
using SPACE_UTIL;

namespace SPACE_DOODLE_CODEMONKEY
{
    /// <summary>
    /// Server-authoritative health. The local client never calculates damage.
    /// Call ApplyServerState from your Netcode / Mirror / Photon layer on each sync.
    /// Use for: any multiplayer entity.
    /// </summary>
    public class NetworkHealthSystem : HealthSystemBase
    {
		public override float MaxHealth => 100f; // overwritten on first server sync
		public override float MaxArmour => 50f;  // overwritten on first server sync

        private void Awake()
        {
            Debug.Log(C.method(this));
            InitCurrHealthArmour();
        }

        /// <summary>
        /// Called by the network layer when the server sends a health update.
        /// This is the only way state changes on the local client.
        /// </summary>
        public void ApplyServerState(float health, float armour, float hMax, float aMax)
        {
            // write directly — server is the authority
            currentHealth = health;
            currentArmour = armour;

            // update the runtime max values so percent calculations stay correct
            // (cast to backing field via a workaround since MaxHealth is a property)
            // If your project uses backing fields directly, assign those instead.
            NotifyHealthChanged(currentHealth, hMax);
            NotifyArmourChanged(currentArmour, aMax);
            if (currentHealth <= 0f) NotifyDeath();
        }

        // ── Local calls are intentional no-ops — server is authoritative ──
        public override void TakeDamage(float amount)  { }
        public override void Heal(float amount)         { }
        public override void RepairArmour(float amount) { }
        public override void Kill()                    { }
        public override void Revive(float healthAmount) { }
    }
}
