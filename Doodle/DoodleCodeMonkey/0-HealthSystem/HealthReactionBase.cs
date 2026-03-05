using UnityEngine;
using UnityEngine.UI;

using SPACE_UTIL;

namespace SPACE_DOODLE_CODEMONKEY
{
	public abstract class HealthReactionBase : MonoBehaviour
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
			hSystem.OnCertainEventOccurRequired2FloatParam += OnCertainChangeOccuredPerhaps;
			hSystem.OnDeath += OnDeath;

			OnBindInit(); // ad init()
		}
		public void Unbind()
		{

			if (hSystem == null) return;
			Debug.Log(C.method(this, "orange"));

			hSystem.OnHealthChanged -= OnHealthChanged;
			hSystem.OnArmourChanged -= OnArmourChanged;
			hSystem.OnCertainEventOccurRequired2FloatParam -= OnCertainChangeOccuredPerhaps;
			hSystem.OnDeath -= OnDeath;

			hSystem = null;
		}

		// ── Subclasses override these ──────────────────────────────────────
		protected virtual void OnBindInit()
		{
			this.targetHealthAmount = hSystem.getHealthPercent;
			this.targetArmourAmount = hSystem.getArmourPercent;
		}
		protected void OnHealthChanged() { this.targetHealthAmount = hSystem.getHealthPercent; }
		protected void OnArmourChanged() { this.targetArmourAmount = hSystem.getArmourPercent; }
		protected void OnCertainChangeOccuredPerhaps(float a, float b) { Debug.Log($"certain change occured {a}, {b}"); }
		protected void OnDeath() { this.targetHealthAmount = 0f; this.targetArmourAmount = 0f; }

		
	}
}