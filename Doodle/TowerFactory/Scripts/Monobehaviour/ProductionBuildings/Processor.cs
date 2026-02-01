using System;
using System.Collections.Generic;
using UnityEngine;

using SPACE_UTIL;


/// <summary>
/// Processor building - accepts input resources, processes them, outputs processed resources.
/// Multi-tile building: Input belt + Processing animation + Output belt.
/// </summary>
public class Processor : Building
{
	[Header("Processor Configuration")]
	[SerializeField] private v2 inputTileOffset = new v2(0, 0);
	[SerializeField] private v2 outputTileOffset = new v2(2, 0);
	[SerializeField] private int maxInputStorage = 10;
	[SerializeField] private int maxOutputStorage = 10;

	[Header("Runtime State")]
	private ConveyorBelt inputBelt;
	private ConveyorBelt outputBelt;
	private List<ResourceData> inputStorage = new List<ResourceData>();
	private List<ResourceData> outputStorage = new List<ResourceData>();
	private ProcessingRecipe currentRecipe;
	private float processingTimer = 0f;
	private bool isProcessing = false;

	protected override void Awake()
	{
		base.Awake();
	}

	public override void OnPlace()
	{
		FindInputOutputBelts();

		// Select first recipe if available
		if (buildingData.recipes != null && buildingData.recipes.Length > 0)
		{
			currentRecipe = buildingData.recipes[0];
		}
	}

	public override void OnRemove()
	{
		// Drop stored items
		if (ResourceManager.Instance != null)
		{
			foreach (var resource in inputStorage)
			{
				ResourceManager.Instance.PlayerInventory.AddResource(resource, 1);
			}
			foreach (var resource in outputStorage)
			{
				ResourceManager.Instance.PlayerInventory.AddResource(resource, 1);
			}
		}

		inputStorage.Clear();
		outputStorage.Clear();
		inputBelt = null;
		outputBelt = null;
	}

	private void FindInputOutputBelts()
	{
		GridSystem grid = GridSystem.Instance;
		if (grid == null) return;

		// Input belt
		v2 inputPos = GridPosition + inputTileOffset;
		Building inputBuilding = grid.GetBuildingAt(inputPos);
		if (inputBuilding is ConveyorBelt belt)
		{
			inputBelt = belt;
		}

		// Output belt
		v2 outputPos = GridPosition + outputTileOffset;
		Building outputBuilding = grid.GetBuildingAt(outputPos);
		if (outputBuilding is ConveyorBelt outBelt)
		{
			outputBelt = outBelt;
		}
	}

	public override void UpdateBuilding(float deltaTime)
	{
		// Accept items from input belt
		AcceptInputItems();

		// Process items if not processing
		if (!isProcessing && CanStartProcessing())
		{
			StartProcessing();
		}

		// Continue processing
		if (isProcessing)
		{
			processingTimer += deltaTime;

			if (processingTimer >= currentRecipe.processingTime)
			{
				FinishProcessing();
			}
		}

		// Output items to output belt
		OutputItems();
	}

	private void AcceptInputItems()
	{
		if (inputBelt == null || inputStorage.Count >= maxInputStorage)
			return;

		// Check if input belt has items at the end
		if (inputBelt.Items.Count > 0)
		{
			ItemOnBelt lastItem = inputBelt.Items[inputBelt.Items.Count - 1];

			// Check if item reached end of belt
			if (lastItem.distanceOnBelt >= inputBelt.PathLength * 0.9f)
			{
				// Accept item into storage
				inputStorage.Add(lastItem.resource);

				// Remove from belt
				inputBelt.Items.Remove(lastItem);
				if (lastItem.itemObject != null)
					Destroy(lastItem.itemObject);
			}
		}
	}

	private bool CanStartProcessing()
	{
		if (currentRecipe == null)
			return false;

		if (outputStorage.Count >= maxOutputStorage)
			return false;

		// Check if we have all required inputs
		if (currentRecipe.inputs == null || currentRecipe.inputs.Length == 0)
			return inputStorage.Count > 0;

		foreach (var inputCost in currentRecipe.inputs)
		{
			int required = inputCost.amount;
			int available = inputStorage.FindAll(r => r == inputCost.resource).Count;

			if (available < required)
				return false;
		}

		return true;
	}

	private void StartProcessing()
	{
		// Consume inputs
		if (currentRecipe.inputs != null)
		{
			foreach (var inputCost in currentRecipe.inputs)
			{
				for (int i = 0; i < inputCost.amount; i++)
				{
					inputStorage.Remove(inputCost.resource);
				}
			}
		}
		else
		{
			// Generic processing - consume one item
			if (inputStorage.Count > 0)
				inputStorage.RemoveAt(0);
		}

		isProcessing = true;
		processingTimer = 0f;
	}

	private void FinishProcessing()
	{
		// Add output to storage
		for (int i = 0; i < currentRecipe.outputAmount; i++)
		{
			outputStorage.Add(currentRecipe.output);
		}

		isProcessing = false;
		processingTimer = 0f;
	}

	private void OutputItems()
	{
		if (outputBelt == null || outputStorage.Count == 0)
			return;

		// Try to spawn item on output belt
		if (outputBelt.CanAcceptItem())
		{
			ResourceData outputResource = outputStorage[0];
			if (outputBelt.SpawnItem(outputResource))
			{
				outputStorage.RemoveAt(0);
			}
		}
	}

	/// <summary>
	/// Change active recipe (if multiple recipes available).
	/// </summary>
	public void ChangeRecipe(int recipeIndex)
	{
		if (buildingData.recipes != null &&
			recipeIndex >= 0 &&
			recipeIndex < buildingData.recipes.Length)
		{
			currentRecipe = buildingData.recipes[recipeIndex];
		}
	}

	public int GetInputStorageCount() => inputStorage.Count;
	public int GetOutputStorageCount() => outputStorage.Count;
	public bool IsProcessing() => isProcessing;
	public float GetProcessingProgress() => processingTimer / currentRecipe.processingTime;
}
