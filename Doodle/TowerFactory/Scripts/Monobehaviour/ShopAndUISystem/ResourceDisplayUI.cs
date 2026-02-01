using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Resource display UI - shows player's current resources.
/// </summary>
public class ResourceDisplayUI : MonoBehaviour
{
	[Header("UI Elements")]
	[SerializeField] private Transform resourceContainer;
	[SerializeField] private GameObject resourceEntryPrefab;

	private Dictionary<ResourceData, ResourceDisplayEntry> displayEntries = new Dictionary<ResourceData, ResourceDisplayEntry>();

	void Start()
	{
		if (ResourceManager.Instance != null)
		{
			// Subscribe to resource changes
			ResourceManager.Instance.PlayerInventory.OnResourceChanged += OnResourceChanged;

			// Display initial resources
			var resources = ResourceManager.Instance.PlayerInventory.GetAllResources();
			foreach (var stack in resources)
			{
				CreateOrUpdateDisplay(stack.resource, stack.amount);
			}
		}
	}

	private void OnResourceChanged(ResourceData resource, int amount)
	{
		CreateOrUpdateDisplay(resource, amount);
	}

	private void CreateOrUpdateDisplay(ResourceData resource, int amount)
	{
		if (resource == null)
			return;

		// Update existing or create new
		if (displayEntries.ContainsKey(resource))
		{
			displayEntries[resource].UpdateAmount(amount);
		}
		else
		{
			CreateDisplay(resource, amount);
		}
	}

	private void CreateDisplay(ResourceData resource, int amount)
	{
		if (resourceEntryPrefab == null || resourceContainer == null)
			return;

		GameObject entryObj = Instantiate(resourceEntryPrefab, resourceContainer);
		ResourceDisplayEntry entry = entryObj.GetComponent<ResourceDisplayEntry>();

		if (entry == null)
		{
			entry = entryObj.AddComponent<ResourceDisplayEntry>();
		}

		entry.Initialize(resource, amount);
		displayEntries[resource] = entry;
	}
}