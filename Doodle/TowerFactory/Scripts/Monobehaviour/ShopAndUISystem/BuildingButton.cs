using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Individual building button in shop UI.
/// </summary>
public class BuildingButton : MonoBehaviour
{
	[Header("UI Elements")]
	[SerializeField] private Image iconImage;
	[SerializeField] private TextMeshProUGUI nameText;
	[SerializeField] private TextMeshProUGUI descriptionText;
	[SerializeField] private Transform costContainer;
	[SerializeField] private GameObject costEntryPrefab;
	[SerializeField] private Button button;

	private BuildingData buildingData;
	private List<GameObject> costEntries = new List<GameObject>();

	public event Action<BuildingData> OnClicked;

	/// <summary>
	/// Initialize button with building data.
	/// </summary>
	public void Initialize(BuildingData data)
	{
		buildingData = data;

		// Set icon
		if (iconImage != null && data.icon != null)
		{
			iconImage.sprite = data.icon;
		}

		// Set name
		if (nameText != null)
		{
			nameText.text = data.buildingName;
		}

		// Set description
		if (descriptionText != null)
		{
			descriptionText.text = data.description;
		}

		// Setup costs
		SetupCosts();

		// Setup button
		if (button != null)
		{
			button.onClick.AddListener(() => OnClicked?.Invoke(buildingData));
		}

		UpdateAffordability();
	}

	/// <summary>
	/// Setup cost display entries.
	/// </summary>
	private void SetupCosts()
	{
		if (costContainer == null || costEntryPrefab == null)
			return;

		// Clear existing
		foreach (var entry in costEntries)
		{
			if (entry != null)
				Destroy(entry);
		}
		costEntries.Clear();

		if (buildingData.buildCosts == null)
			return;

		// Create cost entries
		foreach (var cost in buildingData.buildCosts)
		{
			GameObject entryObj = Instantiate(costEntryPrefab, costContainer);
			costEntries.Add(entryObj);

			// Setup entry (would need custom component)
			var iconImg = entryObj.GetComponentInChildren<Image>();
			var amountTxt = entryObj.GetComponentInChildren<TextMeshProUGUI>();

			if (iconImg != null && cost.resource != null)
			{
				iconImg.sprite = cost.resource.icon;
			}

			if (amountTxt != null)
			{
				amountTxt.text = cost.amount.ToString();
			}
		}
	}

	/// <summary>
	/// Update button state based on whether player can afford.
	/// </summary>
	public void UpdateAffordability()
	{
		if (button == null || buildingData == null)
			return;

		bool canAfford = true;

		if (ResourceManager.Instance != null)
		{
			canAfford = ResourceManager.Instance.CanAfford(buildingData);
		}

		button.interactable = canAfford;

		// Visual feedback
		var colors = button.colors;
		if (canAfford)
		{
			colors.normalColor = Color.white;
		}
		else
		{
			colors.normalColor = new Color(0.5f, 0.5f, 0.5f, 0.8f);
		}
		button.colors = colors;
	}
}