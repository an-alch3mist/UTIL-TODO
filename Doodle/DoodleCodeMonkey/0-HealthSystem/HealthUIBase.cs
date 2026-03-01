using UnityEngine;
using UnityEngine.UI;

namespace SPACE_DOODLE_CODEMONKEY
{
	public abstract class HealthUIBase : MonoBehaviour
	{
		protected HealthSystemBase hSystem { get; private set; }
		protected float targetHealthAmount;
		protected float targetArmourAmount;

		public void Bind(HealthSystemBase healthSystem)
		{
			Unbind();
			hSystem = healthSystem;

			hSystem.OnHealthChanged += OnHealthChanged;
			hSystem.OnArmourChanged += OnArmourChanged;
			hSystem.OnDeath += OnDeath;

			OnBind(); // ad init()
		}
		public void Unbind()
		{
			if (hSystem == null) return;

			hSystem.OnHealthChanged -= OnHealthChanged;
			hSystem.OnArmourChanged -= OnArmourChanged;
			hSystem.OnDeath -= OnDeath;

			hSystem = null;
		}

		// ── Subclasses override these ──────────────────────────────────────
		protected virtual void OnBind()
		{
			this.targetHealthAmount = hSystem.getHealthPercent;
			this.targetArmourAmount = hSystem.getArmourPercent;
		}
		protected void OnHealthChanged(float current, float max) { this.targetHealthAmount = hSystem.getHealthPercent; }
		protected void OnArmourChanged(float current, float max) { this.targetArmourAmount = hSystem.getArmourPercent; }
		protected void OnDeath() { this.targetHealthAmount = 0f; this.targetArmourAmount = 0f; }

		private void OnDestroy() => Unbind();
	}
}