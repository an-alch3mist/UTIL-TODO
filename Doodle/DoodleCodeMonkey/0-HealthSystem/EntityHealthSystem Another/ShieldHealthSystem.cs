using UnityEngine;
using SPACE_UTIL;

namespace SPACE_DOODLE_CODEMONKEY
{
    /// <summary>
    /// Regenerating shield replaces permanent armour.
    /// Any hit resets the recharge window. Shield fills back up automatically.
    /// Use for: sci-fi soldiers, drones, mechs, energy-shielded enemies.
    /// </summary>
    public class ShieldHealthSystem : HealthSystemBase
    {
        [Header("Health")]
        [SerializeField] private float startMaxHealth = 100f;
        [Header("Shield (Armour)")]
        [SerializeField] private float startMaxArmour = 60f;
        [SerializeField] private float rechargeDelay  = 3f;   // seconds after last hit before recharge
        [SerializeField] private float rechargeRate   = 15f;  // armour per second

		public override float MaxHealth => startMaxHealth;
		public override float MaxArmour => startMaxArmour;

        private float rechargeTimer;

        private void Awake()
        {
            Debug.Log(C.method(this));
            InitCurrHealthArmour();
        }

        public override void TakeDamage(float amount)
        {
            rechargeTimer = rechargeDelay; // any hit resets the recharge window
            base.TakeDamage(amount);
        }

        public override void RepairArmour(float amount) { } // shield self-repairs only

        private void Update()
        {
            if (!getIsAlive) return;
            if (currentArmour >= MaxArmour) return;

            if (rechargeTimer > 0f)
            {
                rechargeTimer -= Time.deltaTime;
                return;
            }

            currentArmour = Mathf.Min(currentArmour + rechargeRate * Time.deltaTime, MaxArmour);
            NotifyArmourChanged(currentArmour, MaxArmour);
        }
    }
}
