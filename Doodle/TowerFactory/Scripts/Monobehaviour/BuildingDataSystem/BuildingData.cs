using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using SPACE_UTIL;

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

