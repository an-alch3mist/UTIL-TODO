using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using SPACE_UTIL;

/// <summary>
/// Global resource manager - singleton access to inventory.
/// </summary>
public class ResourceManager : MonoBehaviour
{
	[SerializeField] private Inventory playerInventory = new Inventory();

	// Starting resources
	[Header("Starting Resources")]
	[SerializeField] private ResourceData woodResource;
	[SerializeField] private int startingWood = 50;
	[SerializeField] private ResourceData stoneResource;
	[SerializeField] private int startingStone = 50;

	private static ResourceManager instance;
	public static ResourceManager Instance => instance;
	public Inventory PlayerInventory => playerInventory;

	void Awake()
	{
		instance = this;
		Debug.Log(ResourceManager.Instance);
		// Give starting resources
		if (woodResource != null)
			playerInventory.AddResource(woodResource, startingWood);
		if (stoneResource != null)
			playerInventory.AddResource(stoneResource, startingStone);
	}

	/// <summary>
	/// Quick access to check if can afford building.
	/// </summary>
	public bool CanAfford(BuildingData building)
	{
		return playerInventory.CanAfford(building);
	}

	/// <summary>
	/// Quick access to spend resources.
	/// </summary>
	public bool SpendResources(BuildingData building)
	{
		return playerInventory.SpendResources(building);
	}

	/// <summary>
	/// Quick access to refund resources.
	/// </summary>
	public void RefundResources(BuildingData building, float percentage = 0.5f)
	{
		playerInventory.RefundResources(building, percentage);
	}
}

