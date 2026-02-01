using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Simple hotbar for quick building selection (1-9 keys).
/// </summary>
public class HotbarUI : MonoBehaviour
{
	[Header("Configuration")]
	[SerializeField] private List<BuildingData> hotbarBuildings = new List<BuildingData>(9);

	void Update()
	{
		// Check number keys 1-9
		for (int i = 0; i < 9 && i < hotbarBuildings.Count; i++)
		{
			if (Input.GetKeyDown(KeyCode.Alpha1 + i))
			{
				SelectBuilding(i);
			}
		}
	}

	private void SelectBuilding(int index)
	{
		if (index < 0 || index >= hotbarBuildings.Count)
			return;

		BuildingData building = hotbarBuildings[index];
		if (building == null)
			return;

		// Enter placement mode
		if (InputSystemTowerFactory.Instance != null)
		{
			InputSystemTowerFactory.Instance.OnBuildingSelectedFromShop(building);
		}
	}

	/// <summary>
	/// Set building for hotbar slot.
	/// </summary>
	public void SetHotbarSlot(int index, BuildingData building)
	{
		if (index < 0 || index >= 9)
			return;

		// Ensure list is large enough
		while (hotbarBuildings.Count <= index)
		{
			hotbarBuildings.Add(null);
		}

		hotbarBuildings[index] = building;
	}
}
