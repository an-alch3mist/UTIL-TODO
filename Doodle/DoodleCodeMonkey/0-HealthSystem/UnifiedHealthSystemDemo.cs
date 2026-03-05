using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SPACE_UTIL;

namespace SPACE_DOODLE_CODEMONKEY
{
	/*
	| Category                    | Unified Enum Base Class | Drop-into-New-Project Approach |
	|-----------------------------|------------------------|---------------------------------|
	| Files required              | ✅ One file            | ❌ 8+ files                    |
	| Inspector usability         | ✅ One dropdown        | ❌ Pick the right prefab       |
	| Adding a new type           | ⚠️ Touch every method  | ✅ New file only               |
	| Debugging a specific type   | ⚠️ Hunt through guards | ✅ Open that one class         |
	| Combining behaviours        | ❌ Gets messy          | ✅ Natural                     |
	*/
	public class UnifiedHealthSystemDemo : MonoBehaviour
	{
		[SerializeField] UnifiedHealthSystem unifiedHealthSystem;
		[SerializeField] UnifiedStackableUIHealthSystem unifiedStackableUIHealthSystem;

		private void Start()
		{
			Debug.Log(C.method(this));
			this.unifiedStackableUIHealthSystem.Bind(this.unifiedHealthSystem);
		}

		private void Update()
		{
			if (INPUT.K.InstantDown(KeyCode.Q)) unifiedHealthSystem.TakeDamage(50f);
			if (INPUT.K.InstantDown(KeyCode.W)) unifiedHealthSystem.Heal(20f);
			if (INPUT.K.InstantDown(KeyCode.E)) unifiedHealthSystem.RepairArmour(10f);
			if (INPUT.K.InstantDown(KeyCode.R)) unifiedHealthSystem.Revive(20f);
			if (INPUT.K.InstantDown(KeyCode.T)) unifiedHealthSystem.TakeDamage((float)1e3);
		}
	} 
}
