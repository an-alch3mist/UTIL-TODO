using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using SPACE_UTIL;

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
		Debug.Log(C.method(this, "lime"));
		// Integration point for HighlightPlus or similar package:
		// var effect = GetComponent<HighlightEffect>();
		// if (effect != null) effect.SetHighlighted(true);
	}

	public virtual void DisableHighlight()
	{
		isHighlighted = false;
		Debug.Log(C.method(this, "orange"));
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
