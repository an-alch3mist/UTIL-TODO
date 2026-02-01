using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/*
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

/// <summary>
/// Individual resource display entry showing icon and amount.
/// </summary>
public class ResourceDisplayEntry : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI amountText;

    private ResourceData resource;

    public void Initialize(ResourceData resourceData, int amount)
    {
        resource = resourceData;

        if (iconImage != null && resourceData.icon != null)
        {
            iconImage.sprite = resourceData.icon;
        }

        UpdateAmount(amount);
    }

    public void UpdateAmount(int amount)
    {
        if (amountText != null)
        {
            amountText.text = amount.ToString();
        }

        // Hide if amount is 0
        if (amount == 0)
        {
            gameObject.SetActive(false);
        }
        else
        {
            gameObject.SetActive(true);
        }
    }
}

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

*/