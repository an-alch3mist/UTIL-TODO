using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

using SPACE_UTIL;


/// <summary>
/// Terminator/Storage belt - accepts items and stores them in inventory.
/// </summary>
public class ConveyorBeltTerminator : ConveyorBelt
{
	[Header("Storage")]
	[SerializeField] private bool addToPlayerInventory = true;

	public override void MoveItems(float deltaTime)
	{
		if (items.Count == 0) return;

		float moveDistance = baseSpeed * deltaTime;

		for (int i = items.Count - 1; i >= 0; i--)
		{
			ItemOnBelt item = items[i];
			float targetDistance = item.distanceOnBelt + moveDistance;

			if (targetDistance >= pathLength)
			{
				// Item reached terminator - add to inventory and destroy
				if (addToPlayerInventory && ResourceManager.Instance != null)
				{
					ResourceManager.Instance.PlayerInventory.AddResource(item.resource, 1);
				}

				if (item.itemObject != null)
					Destroy(item.itemObject);

				items.RemoveAt(i);
			}
			else
			{
				item.distanceOnBelt = targetDistance;
				UpdateItemPosition(item);
			}
		}
	}

	protected override bool CanTransferToNext()
	{
		return false; // Terminators don't transfer to next belt
	}
}