using UnityEngine;
using SPACE_UTIL;

namespace SPACE_DOODLE_CODEMONKEY
{
    /// <summary>
    /// Destructible prop. No armour, no healing, no revive.
    /// Use for: crates, barrels, doors, destructible walls.
    /// </summary>
    public class BarrelHealthSystem : HealthSystemBase
    {
        [Header("Health")]
        [SerializeField] private float startMaxHealth = 30f;

        protected override float MaxHealth => startMaxHealth;
        protected override float MaxArmour => 0f;

        private void Awake()
        {
            Debug.Log(C.method(this));
            InitCurrHealthArmour();
        }

        // public override void Heal(float amount)         { } // props don't heal
		public override void RepairArmour(float amount) { } // props have no armour
		// public override void Revive(float healthAmount) { } // props don't revive
    }
}
