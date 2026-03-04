using UnityEngine;
using SPACE_UTIL;

namespace SPACE_DOODLE_CODEMONKEY
{
	public class HealthSystemDemo : MonoBehaviour
	{
		[SerializeField] private HealthSystemBase healthSystem;
		[SerializeField] private HealthReactionBase healthReaction;

		private void Awake()
		{
			Debug.Log(C.method(this));

			healthReaction.Bind(healthSystem);
			healthSystem.OnDeath += () => Debug.Log("Enemy terminated!");
		}

		private void Update()
		{
			if (healthSystem == null) return;
			//
			if (Input.GetKeyDown(KeyCode.Q)) healthSystem.TakeDamage(25f);
			if (Input.GetKeyDown(KeyCode.W)) healthSystem.Heal(20f);
			if (Input.GetKeyDown(KeyCode.E)) healthSystem.RepairArmour(20f);
			if (Input.GetKeyDown(KeyCode.T)) healthSystem.Kill();
			if (Input.GetKeyDown(KeyCode.R)) healthSystem.Revive(50f);
		}
	}
}
