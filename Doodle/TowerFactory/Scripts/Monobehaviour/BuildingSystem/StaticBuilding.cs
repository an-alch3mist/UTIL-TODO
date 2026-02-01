using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using SPACE_UTIL;
/// <summary>
/// Simple building implementation for static objects (trees, rocks, obelisks).
/// </summary>
public class StaticBuilding : Building
{
	public override void OnPlace()
	{
		// Static buildings don't do anything special on placement
	}

	public override void OnRemove()
	{
		// Handle resource dropping if this is a tree/rock
		if (buildingData.category == BuildingCategory.Decoration)
		{
			// Could drop resources here
		}
	}
}