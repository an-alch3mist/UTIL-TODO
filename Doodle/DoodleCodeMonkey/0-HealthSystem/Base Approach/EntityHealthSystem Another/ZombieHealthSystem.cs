using UnityEngine;
using SPACE_UTIL;

namespace SPACE_DOODLE_CODEMONKEY
{
    /// <summary>
    /// Regenerates health over time and auto-revives after a delay when killed.
    /// Use for: zombies, regenerating enemies, undead, trolls.
    /// </summary>
    public class ZombieHealthSystem : HealthSystemBase
    {
        [Header("Health")]
        [SerializeField] private float startMaxHealth = 100f;
        [Header("Regen")]
        [SerializeField] private float regenRate    = 5f;  // HP per second while alive
        [SerializeField] private float reviveDelay  = 4f;  // seconds before auto-revive

		protected override float MaxHealth => startMaxHealth;
		protected override float MaxArmour => 0f;

        private float deadTimer;

        private void Awake()
        {
            Debug.Log(C.method(this));
            InitCurrHealthArmour();
			this.OnDeath += () => { deadTimer = reviveDelay; };
        }

		private void Update()
		{
			if (getIsAlive == false)
			{
				// Debug.Log($"deadTimerWhenDead: {deadTimer} ");
				deadTimer -= Time.deltaTime;
				
				if (deadTimer <= 0f)
					Revive(50f);
				
			}
			// Debug.Log($"deadTimer when alive: {deadTimer} ");
		}
        public override void RepairArmour(float amount) { } // no armour
    }
}
