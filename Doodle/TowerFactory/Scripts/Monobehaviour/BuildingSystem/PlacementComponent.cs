using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using SPACE_UTIL;

/// <summary>
/// Component handling grid placement, rotation, and position management for buildings.
/// Attached to all building GameObjects.
/// </summary>
public class PlacementComponent : MonoBehaviour
{
	[Header("Placement State")]
	public bool isPlaced = false;
	public bool isGhost = false; // True when in placement preview mode
	public v2 gridPosition;
	public int rotationIndex = 0; // 0=0°, 1=90°, 2=180°, 3=270°

	[Header("References")]
	public Building building;

	// Events
	public event Action<PlacementComponent> onPlace;
	public event Action<PlacementComponent> onUnplace;
	public event Action<PlacementComponent> onRotate;

	/// <summary>
	/// Place building on grid at current position.
	/// </summary>
	public bool Place()
	{
		if (isPlaced || building == null || building.buildingData == null)
			return false;

		GridSystem grid = GridSystem.Instance;
		if (grid == null)
			return false;

		// Check if position is valid
		v2[] occupiedTiles = building.buildingData.GetRotatedOccupiedTiles(rotationIndex);
		if (!grid.IsPositionValid(gridPosition, building.Size, occupiedTiles))
			return false;

		// Occupy tiles
		grid.OccupyTiles(gridPosition, building.Size, building, occupiedTiles);

		// Update placement state
		isPlaced = true;
		isGhost = false;

		// Update world position
		UpdateWorldPosition();

		// Fire event
		onPlace?.Invoke(this);

		return true;
	}

	/// <summary>
	/// Remove building from grid.
	/// </summary>
	public void Unplace()
	{
		if (!isPlaced || building == null)
			return;

		GridSystem grid = GridSystem.Instance;
		if (grid != null)
		{
			v2[] occupiedTiles = building.buildingData.GetRotatedOccupiedTiles(rotationIndex);
			grid.FreeTiles(gridPosition, building.Size, occupiedTiles);
		}

		isPlaced = false;
		onUnplace?.Invoke(this);
	}

	/// <summary>
	/// Rotate building 90 degrees clockwise.
	/// </summary>
	public void Rotate()
	{
		rotationIndex = (rotationIndex + 1) % 4;

		// Update visual rotation
		float targetRotation = rotationIndex * 90f;
		transform.rotation = Quaternion.Euler(0, targetRotation, 0);

		onRotate?.Invoke(this);
	}

	/// <summary>
	/// Set grid position and update world position.
	/// </summary>
	public void SetGridPosition(v2 newGridPos)
	{
		gridPosition = newGridPos;
		UpdateWorldPosition();
	}

	/// <summary>
	/// Update world position based on grid position.
	/// </summary>
	public void UpdateWorldPosition()
	{
		GridSystem grid = GridSystem.Instance;
		if (grid != null)
		{
			Vector3 worldPos = grid.GridToWorld(gridPosition);
			transform.position = worldPos;
		}
	}

	/// <summary>
	/// Check if building can be placed at current grid position with current rotation.
	/// </summary>
	public bool CanPlaceAtCurrentPosition()
	{
		if (building == null || building.buildingData == null)
			return false;

		GridSystem grid = GridSystem.Instance;
		if (grid == null)
			return false;

		v2[] occupiedTiles = building.buildingData.GetRotatedOccupiedTiles(rotationIndex);
		return grid.IsPositionValid(gridPosition, building.Size, occupiedTiles);
	}
}