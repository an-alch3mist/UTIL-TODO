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
		protected void InitCurrHealthArmour()
		{
			currentHealth = MaxHealth;
			currentArmour = MaxArmour;
		}

		#region public API(Queries, Commands, Events)
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
				NotifyArmourChanged();
			}

			if (amount > 0f)
			{
				currentHealth = Mathf.Max(currentHealth - amount, 0f);
				NotifyHealthChanged();
				if (currentHealth <= 0f) NotifyDeath();
			}
		}
		public virtual void Heal(float amount)
		{
			if (!getIsAlive || amount <= 0f) return;
			currentHealth = Mathf.Min(currentHealth + amount, MaxHealth);
			NotifyHealthChanged();
		}
		public virtual void RepairArmour(float amount)
		{
			if (!getIsAlive || amount <= 0f) return;
			currentArmour = Mathf.Min(currentArmour + amount, MaxArmour);
			NotifyArmourChanged();
		}
		public virtual void Revive(float healthAmount)
		{
			if (getIsAlive == true) return;
			currentHealth = Mathf.Clamp(healthAmount, 1f, MaxHealth);
			NotifyHealthChanged();
			NotifyArmourChanged();
		}

		// ── Events ─────────────────────────────────────────────────────────
		public event Action OnHealthChanged;
		public event Action OnArmourChanged;
		public event Action<float, float> OnCertainEventOccurRequired2FloatParam;
		public event Action OnDeath;
		public event Action OnPhaseChanged;// Fired once when the boss crosses the phase threshold. 
		#endregion

		#region Notify Events
		protected void NotifyHealthChanged() => OnHealthChanged?.Invoke();
		protected void NotifyArmourChanged() => OnArmourChanged?.Invoke();
		protected void NotifyCertainChangeOccured(float a, float b) => OnCertainEventOccurRequired2FloatParam?.Invoke(a, b);
		protected void NotifyDeath() => OnDeath?.Invoke();
		protected void NotifyPhaseChange() => OnPhaseChanged?.Invoke(); // if subscribers count > 0, notify the subscribers 
		#endregion
	}
}