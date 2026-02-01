using System;
using System.Collections.Generic;
using UnityEngine;

using SPACE_UTIL;


/// <summary>
/// Extractor building - extracts resources from nearby sources (trees/rocks) and spawns items on belt.
/// Multi-tile building: Animation tile + Belt tile.
/// </summary>
public class Extractor : Building
{
	[Header("Extractor Configuration")]
	[SerializeField] private float extractionInterval = 2f; // Seconds between resource spawns
	[SerializeField] private float extractionRange = 2f; // Tiles
	[SerializeField] private v2 beltTileOffset = new v2(1, 0); // Which tile is the belt

	[Header("Runtime State")]
	private float extractionTimer = 0f;
	private ResourceSourceProducer nearbySource;
	private ConveyorBelt outputBelt;

	protected override void Awake()
	{
		base.Awake();

		if (buildingData != null)
		{
			extractionInterval = 1f / buildingData.speed; // Speed determines extraction rate
			extractionRange = buildingData.range;
		}
	}

	public override void OnPlace()
	{
		// Find nearby resource source
		FindNearbyResourceSource();

		// Get the belt tile
		FindOutputBelt();

		if (nearbySource == null)
		{
			Debug.LogWarning($"Extractor at {GridPosition} has no nearby resource source!");
		}
	}

	public override void OnRemove()
	{
		nearbySource = null;
		outputBelt = null;
	}

	private void FindNearbyResourceSource()
	{
		GridSystem grid = GridSystem.Instance;
		if (grid == null) return;

		// Search in radius for resource sources
		int searchRadius = Mathf.CeilToInt(extractionRange);

		for (int x = -searchRadius; x <= searchRadius; x++)
		{
			for (int z = -searchRadius; z <= searchRadius; z++)
			{
				v2 checkPos = GridPosition + new v2(x, z);

				if (Vector2.Distance(GridPosition, checkPos) > extractionRange)
					continue;

				Building building = grid.GetBuildingAt(checkPos);
				if (building is ResourceSourceProducer source)
				{
					// Check if source has resources and matches extractor type
					if (source.GetRemainingAmount() > 0)
					{
						if (buildingData.extractedResource == null ||
							buildingData.extractedResource == source.GetResourceData())
						{
							nearbySource = source;
							return;
						}
					}
				}
			}
		}
	}

	private void FindOutputBelt()
	{
		GridSystem grid = GridSystem.Instance;
		if (grid == null) return;

		// The belt tile is at beltTileOffset from main position
		v2 beltPos = GridPosition + beltTileOffset;
		Building building = grid.GetBuildingAt(beltPos);

		if (building is ConveyorBelt belt)
		{
			outputBelt = belt;
		}
		else
		{
			Debug.LogWarning($"Extractor at {GridPosition} has no output belt at {beltPos}!");
		}
	}

	public override void UpdateBuilding(float deltaTime)
	{
		if (nearbySource == null || outputBelt == null)
			return;

		// Check if source is depleted
		if (nearbySource.GetRemainingAmount() <= 0)
		{
			FindNearbyResourceSource(); // Try to find another source
			return;
		}

		extractionTimer += deltaTime;

		if (extractionTimer >= extractionInterval)
		{
			extractionTimer = 0f;

			// Try to spawn item on belt
			ResourceData resource = nearbySource.GetResourceData();
			if (resource != null && outputBelt.CanAcceptItem())
			{
				outputBelt.SpawnItem(resource);

				// Optionally decrease source amount (auto-harvest)
				// nearbySource.Harvest(1);
			}
		}
	}
}