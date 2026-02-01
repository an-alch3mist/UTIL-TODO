using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using SPACE_UTIL;

/// <summary>
/// Resource source building (tree, rock) that can be harvested.
/// </summary>
public class ResourceSourceProducer : StaticBuilding
{
	[Header("Resource Source")]
	[SerializeField] private ResourceData providedResource;
	[SerializeField] private int resourceAmount = 50;
	[SerializeField] private int currentAmount;

	void Start()
	{
		currentAmount = resourceAmount;
	}

	/// <summary>
	/// Harvest resources from this source.
	/// </summary>
	public bool Harvest(int amount)
	{
		if (currentAmount <= 0)
			return false;

		int harvested = Mathf.Min(amount, currentAmount);
		currentAmount -= harvested;

		// Add to player inventory
		if (ResourceManager.Instance != null && providedResource != null)
		{
			ResourceManager.Instance.PlayerInventory.AddResource(providedResource, harvested);
		}

		// Destroy if depleted
		if (currentAmount <= 0)
		{
			OnDepleted();
		}

		return true;
	}

	/// <summary>
	/// Called when resource is fully harvested.
	/// </summary>
	protected virtual void OnDepleted()
	{
		// Free grid tiles
		if (placementComponent != null)
		{
			placementComponent.Unplace();
		}

		// Clear terrain in grid
		GridSystem grid = GridSystem.Instance;
		if (grid != null)
		{
			grid.ClearTerrain(GridPosition);
		}

		Destroy(gameObject);
	}

	public int GetRemainingAmount() => currentAmount;
	public ResourceData GetResourceData() => providedResource;
}