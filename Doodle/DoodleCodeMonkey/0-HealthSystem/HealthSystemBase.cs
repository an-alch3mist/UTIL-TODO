using System;
using UnityEngine;

namespace SPACE_DOODLE_CODEMONKEY
{
	public abstract class HealthSystemBase : MonoBehaviour
	{
		// ── Abstract max values — each concrete class defines these ────────
		protected abstract float MaxHealth { get; }
		protected abstract float MaxArmour { get; }

		// ── Runtime state — owned here ─────────────────────────────────────
		protected float currentHealth;
		protected float currentArmour;

		// ── Init helper — reads from abstract properties, no args needed ───
		protected void InitialiseHealth()
		{
			currentHealth = MaxHealth;
			currentArmour = MaxArmour;
		}

		// ── Queries ────────────────────────────────────────────────────────
		public virtual float getHealthPercent => MaxHealth > 0f ? currentHealth / MaxHealth : 0f;
		public virtual float getArmourPercent => MaxArmour > 0f ? currentArmour / MaxArmour : 0f;
		public virtual bool getIsAlive => currentHealth > 0f;

		// ── Commands — virtual with standard armour-first flow ─────────────
		public virtual void TakeDamage(float amount)
		{
			if (!getIsAlive || amount <= 0f) return;

			if (currentArmour > 0f)
			{
				float absorbed = Mathf.Min(currentArmour, amount);
				currentArmour -= absorbed;
				amount -= absorbed;
				NotifyArmourChanged(currentArmour, MaxArmour);
			}

			if (amount > 0f)
			{
				currentHealth = Mathf.Max(currentHealth - amount, 0f);
				NotifyHealthChanged(currentHealth, MaxHealth);
				if (currentHealth <= 0f) NotifyDeath();
			}
		}

		public virtual void Heal(float amount)
		{
			if (!getIsAlive || amount <= 0f) return;
			currentHealth = Mathf.Min(currentHealth + amount, MaxHealth);
			NotifyHealthChanged(currentHealth, MaxHealth);
		}

		public virtual void RepairArmour(float amount)
		{
			if (!getIsAlive || amount <= 0f) return;
			currentArmour = Mathf.Min(currentArmour + amount, MaxArmour);
			NotifyArmourChanged(currentArmour, MaxArmour);
		}

		public virtual void Kill()
		{
			if (!getIsAlive) return;
			currentHealth = 0f;
			currentArmour = 0f;
			NotifyHealthChanged(currentHealth, MaxHealth);
			NotifyArmourChanged(currentArmour, MaxArmour);
			NotifyDeath();
		}

		public virtual void Revive(float healthAmount)
		{
			if (getIsAlive) return;
			currentHealth = Mathf.Clamp(healthAmount, 1f, MaxHealth);
			NotifyHealthChanged(currentHealth, MaxHealth);
			NotifyArmourChanged(currentArmour, MaxArmour);
		}

		// ── Events ─────────────────────────────────────────────────────────
		public event Action<float, float> OnHealthChanged;
		public event Action<float, float> OnArmourChanged;
		public event Action OnDeath;

		protected void NotifyHealthChanged(float c, float m) => OnHealthChanged?.Invoke(c, m);
		protected void NotifyArmourChanged(float c, float m) => OnArmourChanged?.Invoke(c, m);
		protected void NotifyDeath() => OnDeath?.Invoke();
	}
}