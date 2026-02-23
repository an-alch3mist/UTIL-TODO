using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using SPACE_UTIL;

/// <summary>
/// Handles placement mode for NEW buildings (not yet on the board).
/// Moving already-placed buildings is handled by BuildingSelector (hold-to-drag).
///
/// Activate:  BuildingPlacer.I.StartPlacing(someSO)
/// Deactivate: Right-click or Escape
///
/// Wire up:
///   1. Attach to a Manager GameObject in scene.
///   2. Assign ghostValidMaterial and ghostInvalidMaterial.
///   3. Assign placementCamera (or leave null → Camera.main).
///   4. Set groundLayerMask to your ground plane layer.
/// </summary>
public class BuildingPlacer : MonoBehaviour
{
    public static BuildingPlacer I { get; private set; }

    [Header("Ghost / Preview")]
    [Tooltip("Transparent green material for valid placement preview tiles.")]
    public Material ghostValidMaterial;
    [Tooltip("Transparent red material for invalid placement preview tiles.")]
    public Material ghostInvalidMaterial;

    [Header("Raycast")]
    public Camera    placementCamera;
    public LayerMask groundLayerMask = ~0;

    // ── State ─────────────────────────────────────────────────────────────────
    public BuildingSO selectedBuilding { get; private set; }
    public bool       isPlacing        { get; private set; }

    private int              _rotation = 0;
    private List<GameObject> _ghostTiles = new List<GameObject>();
    private v2               _lastHoveredCoord;
    private bool             _lastWasValid;

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
    }

    private void Update()
    {
        if (!isPlacing) return;

        if (!TryGetGroundCoord(out v2 hovered)) return;

        bool coordChanged = hovered != _lastHoveredCoord;
        _lastHoveredCoord = hovered;

        bool valid = GridManager.I.CanPlace(selectedBuilding, hovered, _rotation);

        if (coordChanged || valid != _lastWasValid)
        {
            _lastWasValid = valid;
            RefreshGhost(hovered, valid);
        }

        // Rotate
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            _rotation = (_rotation + 1) % 4;
            RefreshGhost(hovered, GridManager.I.CanPlace(selectedBuilding, hovered, _rotation));
        }

        // Confirm
        if (Mouse.current.leftButton.wasPressedThisFrame && valid)
        {
            GridManager.I.Place(selectedBuilding, hovered, _rotation);
            // Stay in placement mode so the player can stamp multiple buildings.
            // Call StopPlacing() from UI if needed.
        }

        // Cancel
        if (Mouse.current.rightButton.wasPressedThisFrame ||
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            StopPlacing();
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Public API
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Enter placement mode for a new building type.</summary>
    public void StartPlacing(BuildingSO buildingData)
    {
        selectedBuilding = buildingData;
        _rotation        = 0;
        isPlacing        = true;
        SpawnGhostTiles();
    }

    /// <summary>Exit placement mode. Ghost tiles are destroyed.</summary>
    public void StopPlacing()
    {
        isPlacing        = false;
        selectedBuilding = null;
        DestroyGhost();
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Ghost
    // ═════════════════════════════════════════════════════════════════════════

    private void SpawnGhostTiles()
    {
        DestroyGhost();
        foreach (var _ in selectedBuilding.allOffsets)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(cube.GetComponent<Collider>());
            cube.transform.localScale = Vector3.one * (GridManager.I.cellSize * 0.9f);
            _ghostTiles.Add(cube);
        }
    }

    private void RefreshGhost(v2 pivotCoord, bool valid)
    {
        var rotatedOffsets = selectedBuilding.GetRotatedOffsets(_rotation);
        var mat            = valid ? ghostValidMaterial : ghostInvalidMaterial;

        for (int i = 0; i < _ghostTiles.Count && i < rotatedOffsets.Count; i++)
        {
            v2      coord = pivotCoord + rotatedOffsets[i];
            Vector3 world = GridManager.I.GridToWorld(coord);
            _ghostTiles[i].transform.position = world + Vector3.up * 0.05f;
            if (mat != null)
                _ghostTiles[i].GetComponent<Renderer>().sharedMaterial = mat;
        }
    }

    private void DestroyGhost()
    {
        foreach (var go in _ghostTiles)
            if (go != null) Destroy(go);
        _ghostTiles.Clear();
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Helpers
    // ═════════════════════════════════════════════════════════════════════════

    private bool TryGetGroundCoord(out v2 coord)
    {
        coord = default;
        var cam = placementCamera != null ? placementCamera : Camera.main;
        if (cam == null) return false;

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit, 500f, groundLayerMask))
            return false;

        coord = GridManager.I.WorldToGrid(hit.point);
        return GridManager.I.InBounds(coord);
    }
}
