using UnityEngine;
using SPACE_UTIL;

namespace SPACE_DOODLE_CODEMONKEY
{
    /// <summary>
    /// Health-only entity. No armour slot.
    /// Use for: wildlife, civilians, simple NPCs, objectives.
    /// </summary>
    public class NoArmourHealthSystem : HealthSystemBase
    {
        [Header("Health")]
        [SerializeField] private float startMaxHealth = 100f;

		public override float MaxHealth => startMaxHealth;
		public override float MaxArmour => 0f; // no armour — base guards divide-by-zero

        private void Awake()
        {
            Debug.Log(C.method(this));
            InitCurrHealthArmour();
        }

        public override void RepairArmour(float amount) { } // no armour to repair
    }
}
