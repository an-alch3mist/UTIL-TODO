using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using SPACE_UTIL;

[DefaultExecutionOrder(-50)] // just after input system
public class GameStoreTowerFactory : MonoBehaviour
{
	
}

// GLOBAL ENUM >>
public enum TerrainType
{
	Normal,      // Regular ground
	Tree,        // Blocks placement and movement (until harvested)
	Rock,        // Blocks placement and movement (until harvested)
	Obelisk,     // Decorative, walkable, blocks placement
	Bonfire      // Decorative, NOT walkable, blocks placement
}

public enum BuildingCategory
{
	ConveyorBelt,
	Production,    // Extractors, Processors
	Tower,         // Defense towers (future)
	Decoration,    // Obelisks, bonfires
	Special        // Main tower, etc.
}

public enum ConveyorBeltType
{
	Straight,
	Curve,
	Combiner,
	Splitter,
	Extractor,
	Processor,
	Terminator
}

public enum InputMode
{
	Normal,     // Default mode - hover, select, interact
	Placement   // Building placement mode
}
// << GLOBAL ENUM