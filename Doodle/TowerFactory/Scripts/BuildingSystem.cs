using System;
using UnityEngine;
using SPACE_UTIL;

/// <summary>
/// Interface for external highlight package integration (e.g., HighlightPlus).
/// </summary>
public interface IHighlightable
{
    void EnableHighlight();
    void DisableHighlight();
}

/*
/// <summary>
/// Base class for ALL buildings: conveyor belts, extractors, processors, towers, decorations.
/// Provides common functionality for placement, rotation, removal, and visual feedback.
/// </summary>
[RequireComponent(typeof(PlacementComponent))]
public abstract class Building : MonoBehaviour, IHighlightable
{
    [Header("Building Data")]
    public BuildingData buildingData;

    [Header("Placement Indicators")]
    [Tooltip("Child GameObject shown when placement is valid (green)")]
    public GameObject greenIndicator;
    [Tooltip("Child GameObject shown when placement is invalid (red)")]
    public GameObject redIndicator;

    [Header("Collider")]
    [Tooltip("Collider for mouse raycasting and interaction")]
    public Collider buildingCollider;

    // Components
    protected PlacementComponent placementComponent;
    protected bool isHighlighted;

    public PlacementComponent PlacementComponent => placementComponent;
    public v2 GridPosition => placementComponent.gridPosition;
    public v2 Size => buildingData.size;
    public v2[] OccupiedTiles => buildingData.GetOccupiedTiles();

    protected virtual void Awake()
    {
        placementComponent = GetComponent<PlacementComponent>();
        if (placementComponent != null)
        {
            placementComponent.building = this;
        }

        // Ensure collider exists
        if (buildingCollider == null)
        {
            buildingCollider = GetComponentInChildren<Collider>();
        }

        HidePlacementIndicators();
    }

    protected virtual void Start()
    {
        if (placementComponent != null)
        {
            placementComponent.onPlace += OnPlaceInternal;
            placementComponent.onUnplace += OnUnplaceInternal;
            placementComponent.onRotate += OnRotateInternal;
        }
    }

    #region Placement Indicators
    public void ShowValidPlacementIndicator()
    {
        if (greenIndicator != null) greenIndicator.SetActive(true);
        if (redIndicator != null) redIndicator.SetActive(false);
    }

    public void ShowInvalidPlacementIndicator()
    {
        if (greenIndicator != null) greenIndicator.SetActive(false);
        if (redIndicator != null) redIndicator.SetActive(true);
    }

    public void HidePlacementIndicators()
    {
        if (greenIndicator != null) greenIndicator.SetActive(false);
        if (redIndicator != null) redIndicator.SetActive(false);
    }
    #endregion

    #region Highlighting (for external package integration)
    public virtual void EnableHighlight()
    {
        isHighlighted = true;
        // Integration point for HighlightPlus or similar package:
        // var effect = GetComponent<HighlightEffect>();
        // if (effect != null) effect.SetHighlighted(true);
    }

    public virtual void DisableHighlight()
    {
        isHighlighted = false;
        // Integration point for HighlightPlus or similar package:
        // var effect = GetComponent<HighlightEffect>();
        // if (effect != null) effect.SetHighlighted(false);
    }
    #endregion

    #region Placement Callbacks
    private void OnPlaceInternal(PlacementComponent component)
    {
        HidePlacementIndicators();
        OnPlace();
    }

    private void OnUnplaceInternal(PlacementComponent component)
    {
        OnRemove();
    }

    private void OnRotateInternal(PlacementComponent component)
    {
        OnRotate();
    }

    /// <summary>
    /// Called when building is successfully placed on grid.
    /// Override in subclasses for specific behavior.
    /// </summary>
    public abstract void OnPlace();

    /// <summary>
    /// Called when building is removed from grid.
    /// Override in subclasses for specific behavior.
    /// </summary>
    public abstract void OnRemove();

    /// <summary>
    /// Called when building is rotated.
    /// Override in subclasses for specific behavior.
    /// </summary>
    public virtual void OnRotate() { }
    #endregion

    #region Update Loop (for buildings that need it)
    /// <summary>
    /// Override this for buildings that need per-frame updates (e.g., conveyor belts moving items).
    /// </summary>
    public virtual void UpdateBuilding(float deltaTime) { }
    #endregion
}

/// <summary>
/// Component handling grid placement, rotation, and position management for buildings.
/// Attached to all building GameObjects.
/// </summary>
public class PlacementComponent : MonoBehaviour
{
    [Header("Placement State")]
    public bool isPlaced = false;
    public bool isGhost = false; // True when in placement preview mode
    public v2 gridPosition;
    public int rotationIndex = 0; // 0=0°, 1=90°, 2=180°, 3=270°

    [Header("References")]
    public Building building;

    // Events
    public event Action<PlacementComponent> onPlace;
    public event Action<PlacementComponent> onUnplace;
    public event Action<PlacementComponent> onRotate;

    /// <summary>
    /// Place building on grid at current position.
    /// </summary>
    public bool Place()
    {
        if (isPlaced || building == null || building.buildingData == null)
            return false;

        GridSystem grid = GridSystem.Instance;
        if (grid == null)
            return false;

        // Check if position is valid
        v2[] occupiedTiles = building.buildingData.GetRotatedOccupiedTiles(rotationIndex);
        if (!grid.IsPositionValid(gridPosition, building.Size, occupiedTiles))
            return false;

        // Occupy tiles
        grid.OccupyTiles(gridPosition, building.Size, building, occupiedTiles);

        // Update placement state
        isPlaced = true;
        isGhost = false;

        // Update world position
        UpdateWorldPosition();

        // Fire event
        onPlace?.Invoke(this);

        return true;
    }

    /// <summary>
    /// Remove building from grid.
    /// </summary>
    public void Unplace()
    {
        if (!isPlaced || building == null)
            return;

        GridSystem grid = GridSystem.Instance;
        if (grid != null)
        {
            v2[] occupiedTiles = building.buildingData.GetRotatedOccupiedTiles(rotationIndex);
            grid.FreeTiles(gridPosition, building.Size, occupiedTiles);
        }

        isPlaced = false;
        onUnplace?.Invoke(this);
    }

    /// <summary>
    /// Rotate building 90 degrees clockwise.
    /// </summary>
    public void Rotate()
    {
        rotationIndex = (rotationIndex + 1) % 4;

        // Update visual rotation
        float targetRotation = rotationIndex * 90f;
        transform.rotation = Quaternion.Euler(0, targetRotation, 0);

        onRotate?.Invoke(this);
    }

    /// <summary>
    /// Set grid position and update world position.
    /// </summary>
    public void SetGridPosition(v2 newGridPos)
    {
        gridPosition = newGridPos;
        UpdateWorldPosition();
    }

    /// <summary>
    /// Update world position based on grid position.
    /// </summary>
    public void UpdateWorldPosition()
    {
        GridSystem grid = GridSystem.Instance;
        if (grid != null)
        {
            Vector3 worldPos = grid.GridToWorld(gridPosition);
            transform.position = worldPos;
        }
    }

    /// <summary>
    /// Check if building can be placed at current grid position with current rotation.
    /// </summary>
    public bool CanPlaceAtCurrentPosition()
    {
        if (building == null || building.buildingData == null)
            return false;

        GridSystem grid = GridSystem.Instance;
        if (grid == null)
            return false;

        v2[] occupiedTiles = building.buildingData.GetRotatedOccupiedTiles(rotationIndex);
        return grid.IsPositionValid(gridPosition, building.Size, occupiedTiles);
    }
}

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

/// <summary>
/// Resource source building (tree, rock) that can be harvested.
/// </summary>
public class ResourceSource : StaticBuilding
{
    [Header("Resource Source")]
    [SerializeField] private ResourceData providedResource;
    [SerializeField] private int resourceAmount = 50;
    [SerializeField] private int currentAmount;

    void Start()
    {
        currentAmount = resourceAmount;
    }

    /// <summary>
    /// Harvest resources from this source.
    /// </summary>
    public bool Harvest(int amount)
    {
        if (currentAmount <= 0)
            return false;

        int harvested = Mathf.Min(amount, currentAmount);
        currentAmount -= harvested;

        // Add to player inventory
        if (ResourceManager.Instance != null && providedResource != null)
        {
            ResourceManager.Instance.PlayerInventory.AddResource(providedResource, harvested);
        }

        // Destroy if depleted
        if (currentAmount <= 0)
        {
            OnDepleted();
        }

        return true;
    }

    /// <summary>
    /// Called when resource is fully harvested.
    /// </summary>
    protected virtual void OnDepleted()
    {
        // Free grid tiles
        if (placementComponent != null)
        {
            placementComponent.Unplace();
        }

        // Clear terrain in grid
        GridSystem grid = GridSystem.Instance;
        if (grid != null)
        {
            grid.ClearTerrain(GridPosition);
        }

        Destroy(gameObject);
    }

    public int GetRemainingAmount() => currentAmount;
    public ResourceData GetResourceData() => providedResource;
}
*/