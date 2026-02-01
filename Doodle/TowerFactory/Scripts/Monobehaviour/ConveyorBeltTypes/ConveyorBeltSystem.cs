using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

using SPACE_UTIL;

/// <summary>
/// Global system managing all conveyor belt groups.
/// Handles belt registration, graph merging/splitting, and updates.
/// </summary>
public class ConveyorBeltSystem : MonoBehaviour
{
	private List<ConveyorBeltGroup> beltGroups = new List<ConveyorBeltGroup>();
	private static ConveyorBeltSystem instance;

	public static ConveyorBeltSystem Instance => instance;

	void Awake()
	{
		instance = this;
	}

	void Update()
	{
		float deltaTime = Time.deltaTime;

		// Update all belt groups
		foreach (var group in beltGroups)
		{
			group.UpdateAllBelts(deltaTime);
		}
	}

	/// <summary>
	/// Register a new belt (called when belt is placed).
	/// </summary>
	public void RegisterBelt(ConveyorBelt belt)
	{
		// Check if belt connects to existing groups
		List<ConveyorBeltGroup> connectedGroups = new List<ConveyorBeltGroup>();

		ConveyorBelt inputBelt = belt.GetInputBelt();
		if (inputBelt != null && inputBelt.BeltGroup != null)
		{
			if (!connectedGroups.Contains(inputBelt.BeltGroup))
				connectedGroups.Add(inputBelt.BeltGroup);
		}

		ConveyorBelt outputBelt = belt.GetOutputBelt();
		if (outputBelt != null && outputBelt.BeltGroup != null)
		{
			if (!connectedGroups.Contains(outputBelt.BeltGroup))
				connectedGroups.Add(outputBelt.BeltGroup);
		}

		if (connectedGroups.Count == 0)
		{
			// Create new group
			ConveyorBeltGroup newGroup = new ConveyorBeltGroup();
			newGroup.AddBelts(new List<ConveyorBelt> { belt });
			beltGroups.Add(newGroup);
		}
		else if (connectedGroups.Count == 1)
		{
			// Add to existing group
			connectedGroups[0].AddBelts(new List<ConveyorBelt> { belt });
		}
		else
		{
			// Merge multiple groups
			ConveyorBeltGroup mergedGroup = connectedGroups[0];
			for (int i = 1; i < connectedGroups.Count; i++)
			{
				mergedGroup.AddBelts(connectedGroups[i].belts);
				beltGroups.Remove(connectedGroups[i]);
			}
			mergedGroup.AddBelts(new List<ConveyorBelt> { belt });
		}
	}

	/// <summary>
	/// Unregister a belt (called when belt is removed).
	/// May split group into multiple groups.
	/// </summary>
	public void UnregisterBelt(ConveyorBelt belt)
	{
		if (belt.BeltGroup == null)
			return;

		ConveyorBeltGroup group = belt.BeltGroup;
		group.RemoveBelt(belt);

		if (group.belts.Count == 0)
		{
			beltGroups.Remove(group);
			return;
		}

		// Check if removal split the group
		// Use flood fill to find connected components
		SplitGroupIfNeeded(group);
	}

	private void SplitGroupIfNeeded(ConveyorBeltGroup group)
	{
		if (group.belts.Count <= 1)
			return;

		HashSet<ConveyorBelt> visited = new HashSet<ConveyorBelt>();
		List<List<ConveyorBelt>> components = new List<List<ConveyorBelt>>();

		foreach (var belt in group.belts)
		{
			if (!visited.Contains(belt))
			{
				List<ConveyorBelt> component = new List<ConveyorBelt>();
				FloodFill(belt, visited, component);
				components.Add(component);
			}
		}

		if (components.Count > 1)
		{
			// Group was split - create new groups
			beltGroups.Remove(group);

			foreach (var component in components)
			{
				ConveyorBeltGroup newGroup = new ConveyorBeltGroup();
				newGroup.AddBelts(component);
				beltGroups.Add(newGroup);
			}
		}
	}

	private void FloodFill(ConveyorBelt start, HashSet<ConveyorBelt> visited, List<ConveyorBelt> component)
	{
		if (visited.Contains(start))
			return;

		visited.Add(start);
		component.Add(start);

		// Check neighbors
		ConveyorBelt inputBelt = start.GetInputBelt();
		if (inputBelt != null && !visited.Contains(inputBelt))
		{
			FloodFill(inputBelt, visited, component);
		}

		ConveyorBelt outputBelt = start.GetOutputBelt();
		if (outputBelt != null && !visited.Contains(outputBelt))
		{
			FloodFill(outputBelt, visited, component);
		}
	}
}


/// <summary>
/// Manages a connected network of conveyor belts.
/// Handles topological sorting and efficient updates.
/// </summary>
public class ConveyorBeltGroup
{
	public List<ConveyorBelt> belts = new List<ConveyorBelt>();
	public List<ConveyorBelt> topologicalOrder = new List<ConveyorBelt>();
	public bool isLoop = false;
	public bool isDirty = true;

	/// <summary>
	/// Add belts to this group.
	/// </summary>
	public void AddBelts(List<ConveyorBelt> newBelts)
	{
		foreach (var belt in newBelts)
		{
			if (!belts.Contains(belt))
			{
				belts.Add(belt);
				belt.BeltGroup = this;
			}
		}
		isDirty = true;
	}

	/// <summary>
	/// Remove belt from this group.
	/// </summary>
	public void RemoveBelt(ConveyorBelt belt)
	{
		belts.Remove(belt);
		belt.BeltGroup = null;
		isDirty = true;
	}

	/// <summary>
	/// Update all belts in topological order.
	/// </summary>
	public void UpdateAllBelts(float deltaTime)
	{
		if (isDirty)
		{
			RecalculateTopology();
		}

		// Update belts in topological order
		foreach (var belt in topologicalOrder)
		{
			belt.MoveItems(deltaTime);
		}
	}

	/// <summary>
	/// Recalculate topological order using Kahn's algorithm.
	/// Handles loops and cycles gracefully.
	/// </summary>
	public void RecalculateTopology()
	{
		if (belts.Count == 0)
		{
			topologicalOrder.Clear();
			isDirty = false;
			return;
		}

		// Build adjacency graph
		Dictionary<ConveyorBelt, int> inDegree = new Dictionary<ConveyorBelt, int>();
		Dictionary<ConveyorBelt, List<ConveyorBelt>> adj = new Dictionary<ConveyorBelt, List<ConveyorBelt>>();

		foreach (var belt in belts)
		{
			inDegree[belt] = 0;
			adj[belt] = new List<ConveyorBelt>();
		}

		// Build edges (output -> input connections)
		foreach (var belt in belts)
		{
			ConveyorBelt outputBelt = belt.GetOutputBelt();
			if (outputBelt != null && belts.Contains(outputBelt))
			{
				adj[belt].Add(outputBelt);
				inDegree[outputBelt]++;
			}
		}

		// Kahn's algorithm
		Queue<ConveyorBelt> queue = new Queue<ConveyorBelt>();
		foreach (var belt in belts)
		{
			if (inDegree[belt] == 0)
				queue.Enqueue(belt);
		}

		topologicalOrder.Clear();

		while (queue.Count > 0)
		{
			ConveyorBelt belt = queue.Dequeue();
			topologicalOrder.Add(belt);

			foreach (var neighbor in adj[belt])
			{
				inDegree[neighbor]--;
				if (inDegree[neighbor] == 0)
					queue.Enqueue(neighbor);
			}
		}

		// Handle cycles/loops
		var remaining = belts.Except(topologicalOrder).ToList();
		if (remaining.Count > 0)
		{
			isLoop = true;
			// Prioritize belts with items
			remaining.Sort((a, b) => b.Items.Count.CompareTo(a.Items.Count));
			topologicalOrder.AddRange(remaining);
		}
		else
		{
			isLoop = false;
		}

		isDirty = false;
	}
}