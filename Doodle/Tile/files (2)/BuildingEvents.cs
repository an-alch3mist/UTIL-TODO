using System;
using UnityEngine;
using SPACE_UTIL;

/// <summary>
/// Static event bus for the building placement system.
///
/// Any system that cares about buildings subscribes here.
/// No direct references needed between systems.
///
/// Subscribe:   BuildingEvents.onPlaced += MyHandler;
/// Unsubscribe: BuildingEvents.onPlaced -= MyHandler;
///
/// Rule: always unsubscribe in OnDisable/OnDestroy.
/// </summary>
public static class BuildingEvents
{
    // ── Placement lifecycle ───────────────────────────────────────────────────

    /// <summary>A building was successfully placed on the grid.</summary>
    public static event Action<BuildingInstance> onPlaced;

    /// <summary>
    /// A building was removed from the grid.
    /// Fired BEFORE the GameObject is destroyed — still safe to read .data / .pivotCoord.
    /// </summary>
    public static event Action<BuildingInstance> onRemoved;

    /// <summary>
    /// A building's grid position or rotation changed (move + rotate both fire this).
    /// Payload carries old state so neighbors can react properly.
    /// </summary>
    public static event Action<BuildingInstance, BuildingMoveArgs> onMoved;

    // ── Selection ─────────────────────────────────────────────────────────────

    /// <summary>Player selected a building (single selection only for now).</summary>
    public static event Action<BuildingInstance> onSelected;

    /// <summary>Player deselected a building.</summary>
    public static event Action<BuildingInstance> onDeselected;

    // ── Drag / placement preview ──────────────────────────────────────────────

    /// <summary>
    /// Fires every frame while dragging, with the current candidate position.
    /// Systems can use this to show connection previews (conveyor links etc.)
    /// without committing anything.
    /// </summary>
    public static event Action<BuildingInstance, BuildingDragArgs> onDragUpdated;

    // ═════════════════════════════════════════════════════════════════════════
    //  Internal fire methods (called only by GridManager / BuildingSelector)
    // ═════════════════════════════════════════════════════════════════════════

    internal static void FirePlaced(BuildingInstance b)           => onPlaced?.Invoke(b);
    internal static void FireRemoved(BuildingInstance b)          => onRemoved?.Invoke(b);
    internal static void FireSelected(BuildingInstance b)         => onSelected?.Invoke(b);
    internal static void FireDeselected(BuildingInstance b)       => onDeselected?.Invoke(b);

    internal static void FireMoved(BuildingInstance b, v2 oldPivot, int oldRot)
        => onMoved?.Invoke(b, new BuildingMoveArgs(oldPivot, oldRot));

    internal static void FireDragUpdated(BuildingInstance b, v2 candidatePivot, int candidateRot, bool isValid)
        => onDragUpdated?.Invoke(b, new BuildingDragArgs(candidatePivot, candidateRot, isValid));
}

// ── Payload structs ───────────────────────────────────────────────────────────

public readonly struct BuildingMoveArgs
{
    public readonly v2  oldPivot;
    public readonly int oldRotation;
    public BuildingMoveArgs(v2 pivot, int rot) { oldPivot = pivot; oldRotation = rot; }
}

public readonly struct BuildingDragArgs
{
    public readonly v2   candidatePivot;
    public readonly int  candidateRotation;
    public readonly bool isValid;
    public BuildingDragArgs(v2 pivot, int rot, bool valid)
    { candidatePivot = pivot; candidateRotation = rot; isValid = valid; }
}
