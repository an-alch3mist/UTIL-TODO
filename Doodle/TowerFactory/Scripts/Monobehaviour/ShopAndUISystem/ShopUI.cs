using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Shop UI system - displays available buildings organized by category.
/// Works with ALL building types: conveyor belts, extractors, processors, towers, decorations.
/// </summary>
public class ShopUI : MonoBehaviour
{
	[Header("UI References")]
	[SerializeField] private GameObject shopPanel;
	[SerializeField] private Transform buildingButtonContainer;
	[SerializeField] private GameObject buildingButtonPrefab;

	[Header("Category Buttons")]
	[SerializeField] private Button beltCategoryButton;
	[SerializeField] private Button productionCategoryButton;
	[SerializeField] private Button towerCategoryButton;
	[SerializeField] private Button decorationCategoryButton;

	[Header("Building Data")]
	[SerializeField] private List<BuildingData> availableBuildings = new List<BuildingData>();

	private BuildingCategory currentCategory = BuildingCategory.ConveyorBelt;
	private List<BuildingButton> buildingButtons = new List<BuildingButton>();

	private static ShopUI instance;
	public static ShopUI Instance => instance;

	void Awake()
	{
		if (instance != null && instance != this)
		{
			Destroy(gameObject);
			return;
		}
		instance = this;
	}

	void Start()
	{
		// Setup category buttons
		if (beltCategoryButton != null)
			beltCategoryButton.onClick.AddListener(() => ShowCategory(BuildingCategory.ConveyorBelt));
		if (productionCategoryButton != null)
			productionCategoryButton.onClick.AddListener(() => ShowCategory(BuildingCategory.Production));
		if (towerCategoryButton != null)
			towerCategoryButton.onClick.AddListener(() => ShowCategory(BuildingCategory.Tower));
		if (decorationCategoryButton != null)
			decorationCategoryButton.onClick.AddListener(() => ShowCategory(BuildingCategory.Decoration));

		// Subscribe to inventory changes
		if (ResourceManager.Instance != null)
		{
			ResourceManager.Instance.PlayerInventory.OnResourceChanged += OnResourceChanged;
		}

		// Initial display
		ShowCategory(currentCategory);
	}

	/// <summary>
	/// Show buildings for specific category.
	/// </summary>
	public void ShowCategory(BuildingCategory category)
	{
		currentCategory = category;

		// Clear existing buttons
		foreach (var button in buildingButtons)
		{
			if (button != null && button.gameObject != null)
				Destroy(button.gameObject);
		}
		buildingButtons.Clear();

		// Create buttons for buildings in this category
		foreach (var buildingData in availableBuildings)
		{
			if (buildingData.category == category)
			{
				CreateBuildingButton(buildingData);
			}
		}

		UpdateButtonStates();
	}

	/// <summary>
	/// Create a button for a building.
	/// </summary>
	private void CreateBuildingButton(BuildingData buildingData)
	{
		if (buildingButtonPrefab == null || buildingButtonContainer == null)
			return;

		GameObject buttonObj = Instantiate(buildingButtonPrefab, buildingButtonContainer);
		BuildingButton buildingButton = buttonObj.GetComponent<BuildingButton>();

		if (buildingButton != null)
		{
			buildingButton.Initialize(buildingData);
			buildingButton.OnClicked += OnBuildingButtonClicked;
			buildingButtons.Add(buildingButton);
		}
	}

	/// <summary>
	/// Called when a building button is clicked.
	/// </summary>
	private void OnBuildingButtonClicked(BuildingData buildingData)
	{
		if (buildingData == null)
			return;

		// Check if can afford
		if (ResourceManager.Instance != null && !ResourceManager.Instance.CanAfford(buildingData))
		{
			Debug.Log($"Cannot afford {buildingData.buildingName}");
			return;
		}

		// Enter placement mode
		if (InputSystemTowerFactory.Instance != null)
		{
			InputSystemTowerFactory.Instance.OnBuildingSelectedFromShop(buildingData);
		}
	}

	/// <summary>
	/// Called when resources change - update button states.
	/// </summary>
	private void OnResourceChanged(ResourceData resource, int amount)
	{
		UpdateButtonStates();
	}

	/// <summary>
	/// Update all button states (enabled/disabled based on resources).
	/// </summary>
	private void UpdateButtonStates()
	{
		foreach (var button in buildingButtons)
		{
			button.UpdateAffordability();
		}
	}

	/// <summary>
	/// Toggle shop panel visibility.
	/// </summary>
	public void ToggleShop()
	{
		if (shopPanel != null)
		{
			shopPanel.SetActive(!shopPanel.activeSelf);
		}
	}

	/// <summary>
	/// Add a building to the shop.
	/// </summary>
	public void AddBuilding(BuildingData buildingData)
	{
		if (!availableBuildings.Contains(buildingData))
		{
			availableBuildings.Add(buildingData);

			// Refresh if this is current category
			if (buildingData.category == currentCategory)
			{
				ShowCategory(currentCategory);
			}
		}
	}
}