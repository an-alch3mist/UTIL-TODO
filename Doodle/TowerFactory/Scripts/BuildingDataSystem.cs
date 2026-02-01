using System;
using System.Collections.Generic;
using UnityEngine;
using SPACE_UTIL;

/*
/// <summary>
/// ScriptableObject defining building properties, costs, and configuration.
/// Used for ALL building types: conveyor belts, extractors, processors, towers, decorations.
/// </summary>
[CreateAssetMenu(fileName = "new-BuildingData", menuName = "TowerFactory/BuildingData")]
public class BuildingData : ScriptableObject
{
    [Header("Basic Info")]
    public string buildingName = "buildingName";
    [TextArea(2, 4)] public string description = "buildingDecr";
    public Sprite icon;
    public GameObject prefab;
    public BuildingCategory category;

    [Header("Grid Placement")]
    public v2 size = new v2(1, 1); // Grid size (width, height)
    public v2 pivotOffset = new v2(0, 0); // Pivot point for rotation
    
    [Tooltip("For jagged/irregular shapes, list relative tile positions. Empty = use rectangular size.")]
    public v2[] occupiedTiles; // For irregular shapes

    [Header("Building Costs")]
    public ResourceCost[] buildCosts;

    [Header("Stats (Optional)")]
    public float speed = 1f; // For conveyor belts
    public float range = 5f; // For extractors and towers
    public float processingTime = 2f; // For processors

    [Header("Belt-Specific (if conveyor)")]
    public ConveyorBeltType beltType = ConveyorBeltType.Straight;
    public BeltOrientation inputOrientation = BeltOrientation.North;
    public BeltOrientation outputOrientation = BeltOrientation.South;

    [Header("Extractor-Specific")]
    public ResourceData extractedResource; // What resource this extractor produces
    [Header("Processor-Specific")]
    public ProcessingRecipe[] recipes; // Processing recipes

	#region Public API
	/// <summary>
	/// Get all occupied tile positions relative to pivot.
	/// </summary>
	public v2[] GetOccupiedTiles()
	{
		if (occupiedTiles != null && occupiedTiles.Length > 0)
			return occupiedTiles;

		// Generate rectangular tiles
		List<v2> tiles = new List<v2>();
		for (int x = 0; x < size.x; x++)
		{
			for (int z = 0; z < size.y; z++)
			{
				tiles.Add(new v2(x, z));
			}
		}
		return tiles.ToArray();
	}
	/// <summary>
	/// Get occupied tiles rotated by rotation index (0=0°, 1=90°, 2=180°, 3=270°).
	/// </summary>
	public v2[] GetRotatedOccupiedTiles(int rotationIndex)
	{
		v2[] tiles = GetOccupiedTiles();
		v2[] rotated = new v2[tiles.Length];

		for (int i = 0; i < tiles.Length; i++)
		{
			rotated[i] = RotateTile(tiles[i] - pivotOffset, rotationIndex) + pivotOffset;
		}

		return rotated;
	}
	#endregion

	#region Private API
	private v2 RotateTile(v2 tile, int rotationIndex)
	{
		v2 result = tile;
		for (int i = 0; i < rotationIndex; i++)
		{
			// Rotate 90° clockwise: (x, y) -> (y, -x)
			result = new v2(result.y, -result.x);
		}
		return result;
	} 
	#endregion
}
*/
/*
/// <summary>
/// ScriptableObject defining a resource type (wood, stone, processed items, etc.).
/// </summary>
[CreateAssetMenu(fileName = "new-ResourceData", menuName = "TowerFactory/ResourceData")]
public class ResourceData : ScriptableObject
{
    public string resourceName = "resourceName";
    public Sprite icon;
    public GameObject itemPrefab; // 3D model for items on belts
    public int maxStackSize = 100;
    public Color resourceColor = Color.white;
}
*/

/// <summary>
/// Resource cost entry for building construction.
/// </summary>
[Serializable]
public class ResourceCost
{
    public ResourceData resource;
    public int amount;
}

/// <summary>
/// Processing recipe for Processor buildings.
/// </summary>
[Serializable]
public class ProcessingRecipe
{
    public string recipeName;
    public ResourceCost[] inputs;
    public ResourceData output;
    public int outputAmount = 1;
    public float processingTime = 2f;
}

/// <summary>
/// Player inventory system for tracking resources.
/// </summary>
[Serializable]
public class Inventory
{
    [SerializeField] private List<ResourceStack> resources = new List<ResourceStack>();

    /// <summary>
    /// Check if player can afford building costs.
    /// </summary>
    public bool CanAfford(BuildingData building)
    {
        if (building.buildCosts == null || building.buildCosts.Length == 0)
            return true;

        foreach (var cost in building.buildCosts)
        {
            if (!HasResource(cost.resource, cost.amount))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Spend resources to build something.
    /// </summary>
    public bool SpendResources(BuildingData building)
    {
        if (!CanAfford(building))
            return false;

        foreach (var cost in building.buildCosts)
        {
            RemoveResource(cost.resource, cost.amount);
        }
        return true;
    }

    /// <summary>
    /// Refund resources when building is removed.
    /// </summary>
    public void RefundResources(BuildingData building, float refundPercentage = 0.5f)
    {
        if (building.buildCosts == null)
            return;

        foreach (var cost in building.buildCosts)
        {
            int refundAmount = Mathf.FloorToInt(cost.amount * refundPercentage);
            AddResource(cost.resource, refundAmount);
        }
    }

    /// <summary>
    /// Add resource to inventory.
    /// </summary>
    public void AddResource(ResourceData resource, int amount)
    {
        if (resource == null || amount <= 0)
            return;

        ResourceStack stack = resources.Find(r => r.resource == resource);
        if (stack == null)
        {
            resources.Add(new ResourceStack { resource = resource, amount = amount });
        }
        else
        {
            stack.amount += amount;
        }

        OnResourceChanged?.Invoke(resource, GetResourceAmount(resource));
    }

    /// <summary>
    /// Remove resource from inventory.
    /// </summary>
    public void RemoveResource(ResourceData resource, int amount)
    {
        if (resource == null || amount <= 0)
            return;

        ResourceStack stack = resources.Find(r => r.resource == resource);
        if (stack != null)
        {
            stack.amount = Mathf.Max(0, stack.amount - amount);
            OnResourceChanged?.Invoke(resource, stack.amount);
        }
    }

    /// <summary>
    /// Check if player has enough of a resource.
    /// </summary>
    public bool HasResource(ResourceData resource, int amount)
    {
        return GetResourceAmount(resource) >= amount;
    }

    /// <summary>
    /// Get current amount of a resource.
    /// </summary>
    public int GetResourceAmount(ResourceData resource)
    {
        ResourceStack stack = resources.Find(r => r.resource == resource);
        return stack?.amount ?? 0;
    }

    /// <summary>
    /// Get all resources in inventory.
    /// </summary>
    public List<ResourceStack> GetAllResources()
    {
        return new List<ResourceStack>(resources);
    }

    /// <summary>
    /// Event fired when resource amount changes.
    /// </summary>
    public event Action<ResourceData, int> OnResourceChanged;

    [Serializable]
    public class ResourceStack
    {
        public ResourceData resource;
        public int amount;
    }
}
