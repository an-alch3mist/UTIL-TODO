using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

using SPACE_UTIL;


/// <summary>
/// Combiner belt - multiple inputs, single output with round-robin selection.
/// </summary>
public class ConveyorBeltCombiner : ConveyorBelt
{
	[Header("Combiner Settings")]
	[SerializeField] private List<BeltOrientation> inputOrientations = new List<BeltOrientation>();
	[SerializeField] private int currentInputIndex = 0;

	protected override void Awake()
	{
		base.Awake();
		pathLength = 1f;

		// Setup multiple inputs
		if (inputOrientations.Count == 0)
		{
			// Default: two inputs (North and West), one output (East)
			inputOrientations.Add(BeltOrientation.North);
			inputOrientations.Add(BeltOrientation.West);
			OutputOrientation = BeltOrientation.East;
		}
	}

	public override bool CanAcceptItem()
	{
		// Check if main belt can accept
		return base.CanAcceptItem();
	}

	/// <summary>
	/// Get next input belt using round-robin.
	/// </summary>
	public ConveyorBelt GetNextInputBelt()
	{
		GridSystem grid = GridSystem.Instance;
		if (grid == null) return null;

		// Round-robin through inputs
		for (int i = 0; i < inputOrientations.Count; i++)
		{
			currentInputIndex = (currentInputIndex + 1) % inputOrientations.Count;
			BeltOrientation inputOr = inputOrientations[currentInputIndex];

			v2 inputTile = GridPosition + GetOrientationOffset(inputOr);
			Building building = grid.GetBuildingAt(inputTile);

			if (building is ConveyorBelt belt && belt.Items.Count > 0)
			{
				return belt;
			}
		}

		return null;
	}

	protected override Vector3 GetPositionOnPath(float t)
	{
		// For combiners, items move straight from current input to output
		// In a more complex implementation, each input could have its own bezier path
		return base.GetPositionOnPath(t);
	}
}