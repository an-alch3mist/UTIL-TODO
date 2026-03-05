using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using SPACE_UTIL;

namespace SPACE_DOODLE_CODEMONKEY
{
	public class UnifiedStackableUIHealthSystem : MonoBehaviour
	{
		protected UnifiedHealthSystem hSystem { get; private set; }
		protected float targetHealthAmount;
		protected float targetArmourAmount;

		public void Bind(UnifiedHealthSystem healthSystem)
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



		[Header("Bar Images (Filled type)")]
		[SerializeField] Image healthFillImage;
		[SerializeField] Image armourFillImage;
		[SerializeField] Color HealthHigh = new Color(0.18f, 0.80f, 0.44f);
		[SerializeField] Color HealthLow = new Color(0.91f, 0.22f, 0.22f);
		[SerializeField] Color ArmourFull = new Color(0.40f, 0.74f, 0.98f);
		[SerializeField] Color ArmourGone = new Color(0.25f, 0.25f, 0.30f);

		[Header("Smooth Fill")]
		[SerializeField] float lerpSpeed = 8f;
		private void Update()
		{
			// lerp >>
			healthFillImage.fillAmount = Mathf.Lerp(healthFillImage.fillAmount, targetHealthAmount, Time.deltaTime * lerpSpeed);
			armourFillImage.fillAmount = Mathf.Lerp(armourFillImage.fillAmount, targetArmourAmount, Time.deltaTime * lerpSpeed);
			// << lerp

			// update UI >>
			healthFillImage.color = Color.Lerp(HealthLow, HealthHigh, healthFillImage.fillAmount);
			armourFillImage.color = Color.Lerp(ArmourGone, ArmourFull, armourFillImage.fillAmount);
			//
			if (armourFillImage.fillAmount.zero(1E-2))
				armourFillImage.Q().upNamed("outline").gf().toggle(false);
			else
				armourFillImage.Q().upNamed("outline").gf().toggle(true);
			// << update UI
		}
		private void OnDestroy() => Unbind();
	}
}
