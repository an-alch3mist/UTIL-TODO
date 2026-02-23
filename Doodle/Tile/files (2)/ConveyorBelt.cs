using UnityEngine;
using SPACE_UTIL;

/// <summary>
/// Conveyor belt part behavior — STUB.
///
/// Neighbor detection and topological sort are not yet implemented.
/// This file exists so the prefab wiring, IPartBehavior lifecycle,
/// and BuildingEvents subscriptions are in place and ready to extend.
///
/// TODO (future sprint):
///   - Item queue and Tick() logic
///   - FindNextConveyor() neighbor query
///   - ConveyorTickSystem registration
///   - Topological sort across all active conveyors
/// </summary>
public class ConveyorBelt : MonoBehaviour, IPartBehavior
{
    [Header("Config")]
    [Tooltip("Flow direction in this prefab's local space (before building rotation).")]
    public v2 localFlowDir = new v2(1, 0);

    // ── Runtime (populated in OnPlaced) ──────────────────────────────────────
    private BuildingInstance _owner;
    private int              _partIndex;

    // ═════════════════════════════════════════════════════════════════════════
    //  IPartBehavior — lifecycle hooks (wired by BuildingInstance.SpawnParts)
    // ═════════════════════════════════════════════════════════════════════════

    public void OnPlaced(BuildingInstance owner, int partIndex)
    {
        _owner     = owner;
        _partIndex = partIndex;

        // TODO: Subscribe to BuildingEvents for neighbor relinking
        // TODO: Register with ConveyorTickSystem
    }

    public void OnRemoved()
    {
        // TODO: Unsubscribe from BuildingEvents
        // TODO: Unregister from ConveyorTickSystem
    }

    public void OnMoved()
    {
        // TODO: Recalculate world coord + flow dir, re-find next conveyor
    }
}
