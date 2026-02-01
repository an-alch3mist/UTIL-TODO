using System;
using System.Collections.Generic;
using UnityEngine;
using SPACE_UTIL;

/// <summary>
/// Base class for all conveyor belt types.
/// Handles item movement, connections, and belt graph integration.
/// </summary>
public abstract class ConveyorBelt : Building
{
    [Header("Belt Configuration")]
    [SerializeField] protected float baseSpeed = 1f; // 1 tile per second
    [SerializeField] protected float minItemSpacing = 0.33f; // Minimum distance between items
    [SerializeField] protected float height = 0.5f; // Height above ground for items

    // Belt state
    protected List<ItemOnBelt> items = new List<ItemOnBelt>();
    protected ConveyorBeltGroup beltGroup;
    protected float pathLength = 1f; // Length of belt path (for speed normalization)

    // Orientation
    public BeltOrientation InputOrientation { get; protected set; }
    public BeltOrientation OutputOrientation { get; protected set; }

    public ConveyorBeltGroup BeltGroup
    {
        get => beltGroup;
        set => beltGroup = value;
    }

    public List<ItemOnBelt> Items => items;
    public float Speed => baseSpeed;
    public float PathLength => pathLength;

    protected override void Awake()
    {
        base.Awake();
        
        if (buildingData != null)
        {
            InputOrientation = buildingData.inputOrientation;
            OutputOrientation = buildingData.outputOrientation;
            baseSpeed = buildingData.speed;
        }
    }

    public override void OnPlace()
    {
        // Register with belt system
        ConveyorBeltSystem.Instance?.RegisterBelt(this);
        
        // Update belt type based on neighbors (for curves)
        UpdateBeltType();
    }

    public override void OnRemove()
    {
        // Remove all items from belt
        foreach (var item in items)
        {
            if (item.itemObject != null)
                Destroy(item.itemObject);
        }
        items.Clear();

        // Unregister from belt system
        ConveyorBeltSystem.Instance?.UnregisterBelt(this);
    }

    public override void OnRotate()
    {
        // Rotate orientations
        InputOrientation = RotateOrientation(InputOrientation, 1);
        OutputOrientation = RotateOrientation(OutputOrientation, 1);
        
        UpdateBeltType();
    }

    /// <summary>
    /// Update belt type based on adjacent belts (for auto-curve detection).
    /// </summary>
    protected virtual void UpdateBeltType()
    {
        // Override in subclasses that need auto-detection (e.g., curves)
    }

    /// <summary>
    /// Move items along this belt.
    /// Called by ConveyorBeltGroup in topological order.
    /// </summary>
    public virtual void MoveItems(float deltaTime)
    {
        if (items.Count == 0) return;

        float moveDistance = baseSpeed * deltaTime;

        // Move items backward-to-forward to prevent collisions
        for (int i = items.Count - 1; i >= 0; i--)
        {
            ItemOnBelt item = items[i];
            float maxMove = moveDistance;

            // Check spacing with item ahead
            if (i < items.Count - 1)
            {
                ItemOnBelt itemAhead = items[i + 1];
                float distToAhead = itemAhead.distanceOnBelt - item.distanceOnBelt;
                float availableSpace = distToAhead - minItemSpacing;
                maxMove = Mathf.Min(maxMove, Mathf.Max(0, availableSpace));
            }

            // Try to move
            float targetDistance = item.distanceOnBelt + maxMove;

            if (targetDistance >= pathLength)
            {
                // Item reached end of belt
                float overshoot = targetDistance - pathLength;
                
                if (CanTransferToNext())
                {
                    // Transfer to next belt
                    ConveyorBelt nextBelt = GetOutputBelt();
                    if (nextBelt != null && nextBelt.CanAcceptItem())
                    {
                        nextBelt.AcceptItem(item, overshoot);
                        items.RemoveAt(i);
                        continue;
                    }
                }

                // Can't transfer - backloading (stop at end)
                item.distanceOnBelt = pathLength;
            }
            else
            {
                item.distanceOnBelt = targetDistance;
            }

            // Update visual position
            UpdateItemPosition(item);
        }
    }

    /// <summary>
    /// Check if this belt can accept a new item.
    /// </summary>
    public virtual bool CanAcceptItem()
    {
        if (items.Count == 0)
            return true;

        // Check if first item is far enough from start
        return items[0].distanceOnBelt >= minItemSpacing;
    }

    /// <summary>
    /// Accept an item from another belt.
    /// </summary>
    public virtual void AcceptItem(ItemOnBelt item, float startDistance = 0f)
    {
        item.distanceOnBelt = startDistance;
        item.currentBelt = this;
        items.Insert(0, item); // Add at start
        
        UpdateItemPosition(item);
    }

    /// <summary>
    /// Spawn a new item on this belt (for extractors).
    /// </summary>
    public virtual bool SpawnItem(ResourceData resource)
    {
        if (!CanAcceptItem())
            return false;

        // Create item GameObject
        GameObject itemObj = null;
        if (resource.itemPrefab != null)
        {
            itemObj = Instantiate(resource.itemPrefab);
        }
        else
        {
            // Fallback: create simple cube
            itemObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            itemObj.transform.localScale = Vector3.one * 0.3f;
            Destroy(itemObj.GetComponent<Collider>()); // Remove collider
        }

        ItemOnBelt newItem = new ItemOnBelt
        {
            resource = resource,
            itemObject = itemObj,
            currentBelt = this,
            distanceOnBelt = 0f
        };

        items.Insert(0, newItem);
        UpdateItemPosition(newItem);

        return true;
    }

    /// <summary>
    /// Update item's 3D position based on distance on belt.
    /// Uses bezier path for curved belts.
    /// </summary>
    protected virtual void UpdateItemPosition(ItemOnBelt item)
    {
        if (item.itemObject == null) return;

        float t = item.distanceOnBelt / pathLength;
        Vector3 position = GetPositionOnPath(t);
        item.itemObject.transform.position = position;
    }

    /// <summary>
    /// Get 3D position at normalized distance t (0 to 1) along belt path.
    /// Override in subclasses for bezier curves.
    /// </summary>
    protected virtual Vector3 GetPositionOnPath(float t)
    {
        // Default: straight line from input to output
        Vector3 start = GetStartPosition();
        Vector3 end = GetEndPosition();
        return Vector3.Lerp(start, end, t);
    }

    /// <summary>
    /// Get start position of belt in world space.
    /// </summary>
    public virtual Vector3 GetStartPosition()
    {
        Vector3 offset = GetOrientationDirection(InputOrientation) * 0.5f;
        return transform.position + Vector3.up * height + offset;
    }

    /// <summary>
    /// Get end position of belt in world space.
    /// </summary>
    public virtual Vector3 GetEndPosition()
    {
        Vector3 offset = GetOrientationDirection(OutputOrientation) * 0.5f;
        return transform.position + Vector3.up * height + offset;
    }

    /// <summary>
    /// Check if can transfer item to next belt.
    /// </summary>
    protected virtual bool CanTransferToNext()
    {
        ConveyorBelt nextBelt = GetOutputBelt();
        return nextBelt != null && nextBelt.CanAcceptItem();
    }

    /// <summary>
    /// Get the belt connected to this belt's output.
    /// </summary>
    public ConveyorBelt GetOutputBelt()
    {
        GridSystem grid = GridSystem.Instance;
        if (grid == null) return null;

        v2 outputTile = GridPosition + GetOrientationOffset(OutputOrientation);
        Building building = grid.GetBuildingAt(outputTile);
        
        if (building is ConveyorBelt belt)
        {
            // Check if the belt's input aligns with our output
            v2 theirInputOffset = GetOrientationOffset(belt.InputOrientation);
            if (belt.GridPosition + theirInputOffset == GridPosition + GetOrientationOffset(OutputOrientation))
            {
                return belt;
            }
        }

        return null;
    }

    /// <summary>
    /// Get the belt connected to this belt's input.
    /// </summary>
    public ConveyorBelt GetInputBelt()
    {
        GridSystem grid = GridSystem.Instance;
        if (grid == null) return null;

        v2 inputTile = GridPosition + GetOrientationOffset(InputOrientation);
        Building building = grid.GetBuildingAt(inputTile);
        
        if (building is ConveyorBelt belt)
        {
            v2 theirOutputOffset = GetOrientationOffset(belt.OutputOrientation);
            if (belt.GridPosition + theirOutputOffset == GridPosition + GetOrientationOffset(InputOrientation))
            {
                return belt;
            }
        }

        return null;
    }

    #region Orientation Helpers
    protected Vector3 GetOrientationDirection(BeltOrientation orientation)
    {
        switch (orientation)
        {
            case BeltOrientation.North: return Vector3.forward;
            case BeltOrientation.East: return Vector3.right;
            case BeltOrientation.South: return Vector3.back;
            case BeltOrientation.West: return Vector3.left;
            default: return Vector3.forward;
        }
    }

    protected v2 GetOrientationOffset(BeltOrientation orientation)
    {
        switch (orientation)
        {
            case BeltOrientation.North: return new v2(0, 1);
            case BeltOrientation.East: return new v2(1, 0);
            case BeltOrientation.South: return new v2(0, -1);
            case BeltOrientation.West: return new v2(-1, 0);
            default: return new v2(0, 1);
        }
    }

    protected BeltOrientation RotateOrientation(BeltOrientation orientation, int steps)
    {
        int index = (int)orientation;
        index = (index + steps) % 4;
        if (index < 0) index += 4;
        return (BeltOrientation)index;
    }
    #endregion

    public override void UpdateBuilding(float deltaTime)
    {
        // Note: Items are moved by ConveyorBeltGroup, not individual belts
        // This ensures proper topological order
    }
}

/// <summary>
/// Represents an item moving on a conveyor belt.
/// </summary>
public class ItemOnBelt
{
    public ResourceData resource;
    public GameObject itemObject;
    public ConveyorBelt currentBelt;
    public float distanceOnBelt; // 0 = start, pathLength = end
}

/// <summary>
/// Belt orientation enum matching BuildingData.
/// </summary>
public enum BeltOrientation
{
    North = 0,
    East = 1,
    South = 2,
    West = 3
}
