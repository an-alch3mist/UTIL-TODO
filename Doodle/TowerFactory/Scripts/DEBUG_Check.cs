using System.Collections;

using UnityEngine;
using SPACE_UTIL;

public class DEBUG_Check : MonoBehaviour
{
	[SerializeField] private BuildingData buildingData;
	[SerializeField] private ResourceData resourceData;

	private void Start()
	{
		Debug.Log(C.method(this, "cyan", adMssg: "Starting DEBUG_Check"));

		// Wait frames for all systems to initialize
		StartCoroutine(TestPlacement());
	}

	private IEnumerator TestPlacement()
	{
		yield return null; // Wait one frame
		yield return null; // Wait one frame
		yield return null; // Wait one frame

		// Wait until ALL systems are ready
    yield return new WaitUntil(() => 
        InputSystemTowerFactory.Instance != null &&
        GridSystem.Instance != null &&
        ResourceManager.Instance != null &&
        ConveyorBeltSystem.Instance != null
    );
		ResourceManager.Instance.PlayerInventory.AddResource(resourceData, 1000);

		// Check if systems are ready
		if (InputSystemTowerFactory.Instance == null)
		{
			Debug.LogError("InputSystemTowerFactory.Instance is NULL!");
		}

		if (GridSystem.Instance == null)
		{
			Debug.LogError("GridSystem.Instance is NULL!");
		}

		if (ResourceManager.Instance == null)
		{
			Debug.LogError("ResourceManager.Instance is NULL!");
		}

		if (buildingData == null)
		{
			Debug.LogError("BuildingData is not assigned in DEBUG_Check!");
		}

		// Log current resources
		int wood = ResourceManager.Instance.PlayerInventory.GetResourceAmount(buildingData.buildCosts[0].resource);
		Debug.Log($"Current Wood: {wood}");

		// Enter placement mode
		Debug.Log(C.method(this, "lime", adMssg: $"Entering placement mode for: {buildingData.buildingName}"));
		InputSystemTowerFactory.Instance.EnterPlacementMode(buildingData);
	}

	private void Update()
	{
		// Debug key to manually trigger placement
		if (Input.GetKeyDown(KeyCode.B))
		{
			if (buildingData != null)
			{
				InputSystemTowerFactory.Instance.EnterPlacementMode(buildingData);
			}
		}

		// Debug key to check current mode
		if (Input.GetKeyDown(KeyCode.M))
		{
			var mode = InputSystemTowerFactory.Instance.GetCurrentMode();
			Debug.Log($"Current Input Mode: {mode}");
		}
	}
}
