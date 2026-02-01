using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

using SPACE_UTIL;

/*
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

/// <summary>
/// Curved conveyor belt with 90° bezier turn.
/// Can auto-detect curve direction based on adjacent belts.
/// </summary>
public class ConveyorBeltCurve : ConveyorBelt
{
    [Header("Curve Settings")]
    [SerializeField] private bool isClockwise = true; // +90° or -90°

    protected override void Awake()
    {
        base.Awake();
        // Arc length of 90° curve is approximately 1.57 (π/2)
        pathLength = 1.57f;
    }

    protected override Vector3 GetPositionOnPath(float t)
    {
        // Cubic bezier for smooth 90° turn
        Vector3 p0 = GetStartPosition();
        Vector3 p3 = GetEndPosition();
        
        // Control points for smooth curve
        Vector3 p1 = p0 + GetOrientationDirection(InputOrientation) * 0.5f;
        Vector3 p2 = p3 - GetOrientationDirection(OutputOrientation) * 0.5f;

        return GetBezierPoint(p0, p1, p2, p3, t);
    }

    private Vector3 GetBezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;

        Vector3 p = uuu * p0;
        p += 3 * uu * t * p1;
        p += 3 * u * tt * p2;
        p += ttt * p3;

        return p;
    }

    protected override void UpdateBeltType()
    {
        // Auto-detect curve direction based on input/output orientations
        // If input is North and output is East, it's a clockwise curve
        int inputInt = (int)InputOrientation;
        int outputInt = (int)OutputOrientation;
        int diff = (outputInt - inputInt + 4) % 4;
        
        isClockwise = (diff == 1); // 90° clockwise
    }
}

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
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
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
*/