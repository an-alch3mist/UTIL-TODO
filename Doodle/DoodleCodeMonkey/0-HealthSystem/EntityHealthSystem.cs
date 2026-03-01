using UnityEngine;
using SPACE_UTIL;

namespace SPACE_DOODLE_CODEMONKEY
{
	public class EntityHealthSystem : HealthSystemBase
	{
		[Header("Health")]
		[SerializeField] private float maxHealth = 100f;
		[Header("Armour")]
		[SerializeField] private float maxArmour = 50f;
		private float currentHealth;
		private float currentArmour;
		private void Awake()
		{
			Debug.Log(C.method(this));
			currentHealth = maxHealth; currentArmour = maxArmour;
		}

		public override bool getIsAlive => currentHealth > 0f;
		public override float getHealthPercent { get => this.currentHealth * 1f / this.maxHealth; }
		public override float getArmourPercent { get => this.currentArmour * 1f / this.maxArmour; }


		public override void TakeDamage(float amount)
		{
			if (!getIsAlive || amount <= 0f) return;

			if (currentArmour > 0f)
			{
				float absorbed = Mathf.Min(currentArmour, amount);
				currentArmour -= absorbed;
				amount -= absorbed;
				NotifyArmourChanged(currentArmour, maxArmour);
			}

			if (amount > 0f)
			{
				currentHealth = Mathf.Max(currentHealth - amount, 0f);
				NotifyHealthChanged(currentHealth, maxHealth);

				if (currentHealth <= 0f)
					NotifyDeath();
			}
		}
		public override void Heal(float amount)
		{
			if (!getIsAlive || amount <= 0f) return;
			currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
			NotifyHealthChanged(currentHealth, maxHealth);
		}
		public override void RepairArmour(float amount)
		{
			if (getIsAlive == false) return;
			if (amount <= 0f) return;
			currentArmour = Mathf.Min(currentArmour + amount, maxArmour);
			NotifyArmourChanged(currentArmour, maxArmour);
		}
		public override void Kill()
		{
			if (!getIsAlive) return;
			currentHealth = 0f;
			currentArmour = 0f;
			NotifyHealthChanged(currentHealth, maxHealth);
			NotifyArmourChanged(currentArmour, maxHealth);
			NotifyDeath();
		}
		public override void Revive(float healthAmount)
		{
			if (getIsAlive) return;
			currentHealth = Mathf.Clamp(healthAmount, 1f, maxHealth);
			NotifyHealthChanged(currentHealth, maxHealth);
			NotifyArmourChanged(currentArmour, maxArmour);
		}
	}
}
