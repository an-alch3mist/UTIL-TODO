using System;
using UnityEngine;
using UnityEngine.InputSystem;

using SPACE_UTIL;

/// <summary>
/// Input system managing two modes: Normal and Placement.
/// Handles building placement, rotation, removal, and interaction.
/// </summary>
public class InputSystemTowerFactory : MonoBehaviour
{
	[Header("Camera")]
	[SerializeField] private Camera mainCamera;

	[Header("Layers")]
	[SerializeField] private LayerMask groundLayerMask;
	[SerializeField] private LayerMask buildingLayerMask;

	[Header("Settings")]
	[SerializeField] private float raycastMaxDistance = 1000f;

	// Input mode
	private InputMode currentMode = InputMode.Normal;
	private Building ghostBuilding; // Preview building in placement mode
	private BuildingData selectedBuildingData;

	// Hover tracking
	private Building currentHoveredBuilding;
	public static InputSystemTowerFactory Instance { get; private set; }

	void Awake()
	{
		Debug.Log(C.method(this));
		Instance = this;

		if (mainCamera == null)
			mainCamera = Camera.main;
	}
	void Update()
	{
		if (currentMode == InputMode.Normal)
		{
			UpdateNormalMode();
		}
		else if (currentMode == InputMode.Placement)
		{
			UpdatePlacementMode();
		}
	}

	#region Normal Mode
	private void UpdateNormalMode()
	{
		Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
		RaycastHit hit;

		// Raycast to buildings
		if (Physics.Raycast(ray, out hit, raycastMaxDistance, buildingLayerMask))
		{
			Building building = hit.collider.GetComponentInParent<Building>();

			if (building != currentHoveredBuilding)
			{
				// Unhighlight previous
				if (currentHoveredBuilding != null)
				{
					currentHoveredBuilding.DisableHighlight();
				}

				// Highlight new
				currentHoveredBuilding = building;
				if (currentHoveredBuilding != null)
				{
					currentHoveredBuilding.EnableHighlight();
				}
			}

			// Handle input on hovered building
			if (currentHoveredBuilding != null)
			{
				// F key - remove building
				if (Input.GetKeyDown(KeyCode.F))
				{
					RemoveBuilding(currentHoveredBuilding);
				}

				// C key - copy building
				if (Input.GetKeyDown(KeyCode.C))
				{
					CopyBuilding(currentHoveredBuilding);
				}

				// Left click - select building (for future features)
				if (Input.GetMouseButtonDown(0))
				{
					SelectBuilding(currentHoveredBuilding);
				}
			}
		}
		else
		{
			// No building hovered
			if (currentHoveredBuilding != null)
			{
				currentHoveredBuilding.DisableHighlight();
				currentHoveredBuilding = null;
			}
		}
	}

	private void RemoveBuilding(Building building)
	{
		if (building == null || building.PlacementComponent == null)
			return;

		// Refund resources
		if (ResourceManager.Instance != null && building.buildingData != null)
		{
			ResourceManager.Instance.RefundResources(building.buildingData, 0.5f);
		}

		// Unplace and destroy
		building.PlacementComponent.Unplace();
		Destroy(building.gameObject);

		currentHoveredBuilding = null;
	}

	private void CopyBuilding(Building building)
	{
		if (building == null || building.buildingData == null)
			return;

		// Enter placement mode with same building type
		EnterPlacementMode(building.buildingData);
	}

	private void SelectBuilding(Building building)
	{
		// For future features: show building info UI, allow upgrades, etc.
		Debug.Log($"Selected building: {building.buildingData?.buildingName}");
	}
	#endregion

	#region Placement Mode
	/// <summary>
	/// Enter placement mode with selected building.
	/// Called from shop UI or when copying a building.
	/// </summary>
	public void EnterPlacementMode(BuildingData buildingData)
	{
		if (buildingData == null || buildingData.prefab == null)
			return;
		Debug.Log(C.method(this, "cyan"));
		// Check if can afford
		// if (ResourceManager.Instance != null && !ResourceManager.Instance.CanAfford(buildingData))
		if (ResourceManager.Instance == null)
		{
			Debug.Log($"Cannot afford {buildingData.buildingName}");
			return;
		}

		selectedBuildingData = buildingData;
		currentMode = InputMode.Placement;

		// Create ghost building
		GameObject ghostObj = Instantiate(buildingData.prefab);
		ghostBuilding = ghostObj.GetComponent<Building>();

		if (ghostBuilding != null && ghostBuilding.PlacementComponent != null)
		{
			ghostBuilding.PlacementComponent.isGhost = true;

			// Make ghost semi-transparent (would need material setup)
			SetGhostMaterial(ghostBuilding);
		}

		Debug.Log($"Entered placement mode for: {buildingData.buildingName}");
	}

	private void UpdatePlacementMode()
	{
		if (ghostBuilding == null)
		{
			ExitPlacementMode();
			return;
		}

		Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
		RaycastHit hit;

		// Raycast to ground to find placement position
		if (Physics.Raycast(ray, out hit, raycastMaxDistance, groundLayerMask))
		{
			GridSystem grid = GridSystem.Instance;
			if (grid != null)
			{
				// Convert hit point to grid position
				v2 gridPos = grid.WorldToGrid(hit.point);

				// Update ghost position
				ghostBuilding.PlacementComponent.SetGridPosition(gridPos);

				// Check if placement is valid
				bool isValid = ghostBuilding.PlacementComponent.CanPlaceAtCurrentPosition();

				// Check if can afford
				if (isValid && ResourceManager.Instance != null)
				{
					isValid = ResourceManager.Instance.CanAfford(selectedBuildingData);
				}

				// Update indicator
				if (isValid)
				{
					ghostBuilding.ShowValidPlacementIndicator();
				}
				else
				{
					ghostBuilding.ShowInvalidPlacementIndicator();
				}

				// Left click - place building
				if (Input.GetMouseButtonDown(0) && isValid)
				{
					PlaceBuilding();
					return;
				}
			}
		}

		// R key - rotate
		if (Input.GetKeyDown(KeyCode.R))
		{
			RotateGhostBuilding();
		}

		// Right click or ESC - cancel
		if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
		{
			ExitPlacementMode();
		}
	}

	private void PlaceBuilding()
	{
		if (ghostBuilding == null || selectedBuildingData == null)
			return;

		// Spend resources
		if (ResourceManager.Instance != null)
		{
			if (!ResourceManager.Instance.SpendResources(selectedBuildingData))
			{
				Debug.Log("Cannot afford building!");
				return;
			}
		}

		// Place the building
		if (ghostBuilding.PlacementComponent.Place())
		{
			Debug.Log($"Placed: {selectedBuildingData.buildingName}");

			// Keep in placement mode for continuous placement
			// Create new ghost for next placement
			GameObject newGhostObj = Instantiate(selectedBuildingData.prefab);
			Building newGhost = newGhostObj.GetComponent<Building>();

			if (newGhost != null && newGhost.PlacementComponent != null)
			{
				newGhost.PlacementComponent.isGhost = true;
				newGhost.PlacementComponent.rotationIndex = ghostBuilding.PlacementComponent.rotationIndex;
				SetGhostMaterial(newGhost);

				ghostBuilding = newGhost;
			}
		}
		else
		{
			Debug.Log("Failed to place building!");
		}
	}

	private void RotateGhostBuilding()
	{
		if (ghostBuilding == null || ghostBuilding.PlacementComponent == null)
			return;

		ghostBuilding.PlacementComponent.Rotate();
	}

	private void ExitPlacementMode()
	{
		if (ghostBuilding != null)
		{
			Destroy(ghostBuilding.gameObject);
			ghostBuilding = null;
		}

		selectedBuildingData = null;
		currentMode = InputMode.Normal;

		Debug.Log("Exited placement mode");
	}

	private void SetGhostMaterial(Building building)
	{
		// Make ghost semi-transparent
		// This would require proper material setup with transparent shader
		// For now, just a placeholder

		Renderer[] renderers = building.GetComponentsInChildren<Renderer>();
		foreach (var renderer in renderers)
		{
			foreach (var mat in renderer.materials)
			{
				Color color = mat.color;
				color.a = 0.5f;
				mat.color = color;

				// Would need to set material to transparent mode:
				// mat.SetFloat("_Mode", 3);
				// mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
				// mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
				// mat.SetInt("_ZWrite", 0);
				// mat.DisableKeyword("_ALPHATEST_ON");
				// mat.EnableKeyword("_ALPHABLEND_ON");
				// mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
				// mat.renderQueue = 3000;
			}
		}
	}
	#endregion

	#region Public API for UI
	/// <summary>
	/// Called by shop UI when building is selected.
	/// </summary>
	public void OnBuildingSelectedFromShop(BuildingData buildingData)
	{
		EnterPlacementMode(buildingData);
	}

	/// <summary>
	/// Check current input mode.
	/// </summary>
	public InputMode GetCurrentMode() => currentMode;

	/// <summary>
	/// Force exit placement mode (called by UI cancel button).
	/// </summary>
	public void CancelPlacement()
	{
		if (currentMode == InputMode.Placement)
		{
			ExitPlacementMode();
		}
	}
	#endregion
}
