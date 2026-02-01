using System;
using System.Collections.Generic;
using UnityEngine;

using SPACE_UTIL;


/// <summary>
/// Simple storage building - accepts items and stores them.
/// Can be used as a sink for testing or as actual storage.
/// </summary>
public class StorageBuilding : Building
{
	[Header("Storage Configuration")]
	[SerializeField] private int maxCapacity = 100;
	[SerializeField] private bool addToPlayerInventory = true;

	private Dictionary<ResourceData, int> storedResources = new Dictionary<ResourceData, int>();
	private int totalStored = 0;

	public override void OnPlace()
	{
		// Storage buildings don't need special placement logic
	}

	public override void OnRemove()
	{
		// Return stored items to player
		if (addToPlayerInventory && ResourceManager.Instance != null)
		{
			foreach (var kvp in storedResources)
			{
				ResourceManager.Instance.PlayerInventory.AddResource(kvp.Key, kvp.Value);
			}
		}

		storedResources.Clear();
		totalStored = 0;
	}

	/// <summary>
	/// Store a resource item.
	/// </summary>
	public bool StoreItem(ResourceData resource)
	{
		if (totalStored >= maxCapacity)
			return false;

		if (!storedResources.ContainsKey(resource))
		{
			storedResources[resource] = 0;
		}

		storedResources[resource]++;
		totalStored++;

		if (addToPlayerInventory && ResourceManager.Instance != null)
		{
			ResourceManager.Instance.PlayerInventory.AddResource(resource, 1);
		}

		return true;
	}

	public int GetTotalStored() => totalStored;
	public int GetStoredAmount(ResourceData resource)
	{
		return storedResources.ContainsKey(resource) ? storedResources[resource] : 0;
	}

	public bool IsFull() => totalStored >= maxCapacity;
}
