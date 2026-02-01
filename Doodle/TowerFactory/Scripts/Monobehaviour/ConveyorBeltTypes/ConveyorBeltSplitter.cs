using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

using SPACE_UTIL;


/// <summary>
/// Splitter belt - single input, multiple outputs with round-robin distribution.
/// </summary>
public class ConveyorBeltSplitter : ConveyorBelt
{
	[Header("Splitter Settings")]
	[SerializeField] private List<BeltOrientation> outputOrientations = new List<BeltOrientation>();
	[SerializeField] private int currentOutputIndex = 0;

	protected override void Awake()
	{
		base.Awake();
		pathLength = 1f;

		// Setup multiple outputs
		if (outputOrientations.Count == 0)
		{
			// Default: one input (West), two outputs (North and East)
			InputOrientation = BeltOrientation.West;
			outputOrientations.Add(BeltOrientation.North);
			outputOrientations.Add(BeltOrientation.East);
		}
	}

	public override void MoveItems(float deltaTime)
	{
		// Override to handle round-robin output selection
		if (items.Count == 0) return;

		float moveDistance = baseSpeed * deltaTime;

		for (int i = items.Count - 1; i >= 0; i--)
		{
			ItemOnBelt item = items[i];
			float targetDistance = item.distanceOnBelt + moveDistance;

			if (targetDistance >= pathLength)
			{
				// Item reached end - select output using round-robin
				float overshoot = targetDistance - pathLength;

				ConveyorBelt outputBelt = GetNextAvailableOutput();
				if (outputBelt != null && outputBelt.CanAcceptItem())
				{
					outputBelt.AcceptItem(item, overshoot);
					items.RemoveAt(i);
					currentOutputIndex = (currentOutputIndex + 1) % outputOrientations.Count;
					continue;
				}

				// Can't transfer - stop at end
				item.distanceOnBelt = pathLength;
			}
			else
			{
				item.distanceOnBelt = targetDistance;
			}

			UpdateItemPosition(item);
		}
	}

	private ConveyorBelt GetNextAvailableOutput()
	{
		GridSystem grid = GridSystem.Instance;
		if (grid == null) return null;

		// Try each output in round-robin order
		for (int i = 0; i < outputOrientations.Count; i++)
		{
			int tryIndex = (currentOutputIndex + i) % outputOrientations.Count;
			BeltOrientation outputOr = outputOrientations[tryIndex];

			v2 outputTile = GridPosition + GetOrientationOffset(outputOr);
			Building building = grid.GetBuildingAt(outputTile);

			if (building is ConveyorBelt belt && belt.CanAcceptItem())
			{
				currentOutputIndex = tryIndex;
				return belt;
			}
		}

		return null;
	}
}
