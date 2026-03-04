using UnityEngine;
using SPACE_UTIL;

namespace SPACE_DOODLE_CODEMONKEY
{
	/// <summary>
	/// Standard enemy / player health system.
	/// Declares its own Inspector values and implements the abstract max properties.
	/// All game logic is inherited from HealthSystemBase.
	/// </summary>
	public class EntityHealthSystem : HealthSystemBase
	{
		[Header("Health")] [SerializeField] private float startMaxHealth = 100f;
		[Header("Armour")] [SerializeField] private float startMaxArmour = 50f;

		// ── Contract — base reads these, never the raw serialized fields ───
		public override float MaxHealth => startMaxHealth;
		public override float MaxArmour => startMaxArmour;
		private void Awake()
		{
			Debug.Log(C.method(this));
			InitCurrHealthArmour();
		}

		public override void Heal(float amount)
		{
			base.Heal(amount);
		}
		public override void Kill()
		{
			base.Kill();
		}
		public override void RepairArmour(float amount)
		{
			base.RepairArmour(amount);
		}
		public override void Revive(float healthAmount)
		{
			base.Revive(healthAmount);
		}
		public override void TakeDamage(float amount)
		{
			base.TakeDamage(amount);
		}
	}
}