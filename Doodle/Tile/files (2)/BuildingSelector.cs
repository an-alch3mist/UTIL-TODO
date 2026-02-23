using UnityEngine;
using UnityEngine.InputSystem;
using SPACE_UTIL;

/// <summary>
/// Handles building selection (click any part collider → selects whole building)
/// and hold-to-drag movement with a green/red shadow indicator.
///
/// Flow:
///   1. Left-click any part collider
///      → Q().up&lt;BuildingInstance&gt;() finds the owner
///      → BuildingEvents.onSelected fires
///   2. While holding the mouse button down
///      → building lifts off its old grid tiles
///      → indicator GO enabled, green or red based on CanPlace
///      → building snaps to cursor grid position each frame
///      → R rotates the dragged building
///   3. Mouse button released
///      → if valid: commit move (fires BuildingEvents.onMoved)
///      → if invalid: snap back to original position
///      → indicator GO disabled
/// </summary>
public class BuildingSelector : MonoBehaviour
{
    [Header("Raycast")]
    public Camera    selectionCamera;
    public LayerMask buildingLayerMask = ~0;
    public LayerMask groundLayerMask   = ~0;
    public float     maxDistance       = 200f;

    [Header("Drag")]
    [Tooltip("How many pixels the mouse must move after click before drag mode begins.")]
    public float dragThresholdPx = 6f;

    // ── Public state ──────────────────────────────────────────────────────────
    public BuildingInstance selected   { get; private set; }
    public bool             isDragging { get; private set; }

    // ── Private drag state ────────────────────────────────────────────────────
    private bool    _mouseDownOnBuilding;
    private Vector2 _mouseDownScreenPos;

    private v2  _dragOriginPivot;
    private int _dragOriginRot;
    private v2  _dragCandidatePivot;
    private int _dragCandidateRot;
    private bool _dragLastValid;

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    private void Update()
    {
        HandleMouseDown();

        // Check if held mouse has moved far enough to start dragging
        if (!isDragging && _mouseDownOnBuilding && selected != null)
        {
            float moved = Vector2.Distance(Mouse.current.position.ReadValue(), _mouseDownScreenPos);
            if (moved >= dragThresholdPx)
                BeginDrag();
        }

        if (isDragging)
            HandleDrag();

        HandleMouseUp();
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Input phases
    // ═════════════════════════════════════════════════════════════════════════

    private void HandleMouseDown()
    {
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        var cam = Cam();
        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, buildingLayerMask))
        {
            if (!isDragging) Deselect();
            return;
        }

        // ── Q() traversal — walks up the hierarchy from the hit collider's GO ──
        // No GetComponentInParent, no manual loop — your UTIL fluent API.
        // Part GO → (parent) BuildingInstance root → gf<BuildingInstance>()
        BuildingInstance instance =
            hit.collider.gameObject.Q().up<BuildingInstance>().gf<BuildingInstance>();

        if (instance == null)
        {
            if (!isDragging) Deselect();
            return;
        }

        SelectInternal(instance);
        _mouseDownOnBuilding = true;
        _mouseDownScreenPos  = Mouse.current.position.ReadValue();
    }

    private void HandleDrag()
    {
        // Rotation
        if (Keyboard.current.rKey.wasPressedThisFrame)
            _dragCandidateRot = (_dragCandidateRot + 1) % 4;

        // Follow cursor on ground plane
        if (!TryGetGroundCoord(out v2 hoveredCoord)) return;

        bool valid = GridManager.I.CanPlace(
            selected.data, hoveredCoord, _dragCandidateRot, ignoreInstance: selected);

        bool changed = hoveredCoord != _dragCandidatePivot || valid != _dragLastValid;
        _dragCandidatePivot = hoveredCoord;
        _dragLastValid      = valid;

        if (changed)
        {
            // Move visual without touching the board
            selected.PreviewAt(hoveredCoord, _dragCandidateRot);
            // Swap indicator material green/red
            selected.SetIndicator(valid);
        }

        BuildingEvents.FireDragUpdated(selected, _dragCandidatePivot, _dragCandidateRot, valid);
    }

    private void HandleMouseUp()
    {
        if (!Mouse.current.leftButton.wasReleasedThisFrame) return;

        if (!isDragging)
        {
            _mouseDownOnBuilding = false;
            return;
        }

        if (_dragLastValid)
            CommitDrag();
        else
            CancelDrag();

        EndDrag();
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Drag lifecycle
    // ═════════════════════════════════════════════════════════════════════════

    private void BeginDrag()
    {
        isDragging          = true;
        _dragOriginPivot    = selected.pivotCoord;
        _dragOriginRot      = selected.rotation;
        _dragCandidatePivot = selected.pivotCoord;
        _dragCandidateRot   = selected.rotation;
        _dragLastValid      = true;

        // Lift tiles so the building doesn't block its own footprint check
        GridManager.I.LiftOff(selected);

        // Show indicator
        selected.SetIndicatorActive(true);
        selected.SetIndicator(valid: true);
    }

    private void CommitDrag()
    {
        GridManager.I.LandOn(selected, _dragCandidatePivot, _dragCandidateRot);

        bool moved = _dragCandidatePivot != _dragOriginPivot
                  || _dragCandidateRot   != _dragOriginRot;
        if (moved)
            BuildingEvents.FireMoved(selected, _dragOriginPivot, _dragOriginRot);
    }

    private void CancelDrag()
    {
        // Snap back — board tiles restored at original position
        GridManager.I.LandOn(selected, _dragOriginPivot, _dragOriginRot);
    }

    private void EndDrag()
    {
        selected?.SetIndicatorActive(false);
        isDragging           = false;
        _mouseDownOnBuilding = false;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Selection
    // ═════════════════════════════════════════════════════════════════════════

    private void SelectInternal(BuildingInstance instance)
    {
        if (instance == selected) return;
        Deselect();
        selected = instance;
        BuildingEvents.FireSelected(selected);
    }

    public void Deselect()
    {
        if (selected == null) return;
        if (isDragging) { CancelDrag(); EndDrag(); }
        var prev = selected;
        selected = null;
        BuildingEvents.FireDeselected(prev);
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Helpers
    // ═════════════════════════════════════════════════════════════════════════

    private Camera Cam() => selectionCamera != null ? selectionCamera : Camera.main;

    private bool TryGetGroundCoord(out v2 coord)
    {
        coord = default;
        Ray ray = Cam().ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, groundLayerMask))
            return false;
        coord = GridManager.I.WorldToGrid(hit.point);
        return GridManager.I.InBounds(coord);
    }
}
