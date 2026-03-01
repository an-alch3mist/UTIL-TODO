using UnityEngine;
using UnityEngine.UI;

using SPACE_UTIL;

namespace SPACE_DOODLE_CODEMONKEY
{
    public class StackableHealthBarUI : HealthUIBase
    {
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
			healthFillImage.fillAmount = Mathf.Lerp(healthFillImage.fillAmount, targetHealthAmount, Time.deltaTime * lerpSpeed);
			healthFillImage.color = Color.Lerp(HealthLow, HealthHigh, healthFillImage.fillAmount);

			armourFillImage.fillAmount = Mathf.Lerp(armourFillImage.fillAmount, targetArmourAmount, Time.deltaTime * lerpSpeed);
			armourFillImage.color = Color.Lerp(ArmourGone, ArmourFull, armourFillImage.fillAmount);
			//
			if (armourFillImage.fillAmount.zero(1E-2))
				armourFillImage.Q().upNamed("outline").gf().toggle(false);
			else
				armourFillImage.Q().upNamed("outline").gf().toggle(true);
		}
	}
}
