using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

using SPACE_UTIL;


/// <summary>
/// Straight conveyor belt - simplest belt type.
/// </summary>
public class ConveyorBeltStraight : ConveyorBelt
{
	protected override void Awake()
	{
		base.Awake();
		pathLength = 1f; // 1 tile length
	}
}
