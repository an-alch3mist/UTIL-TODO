/// <summary>
/// Contract for any MonoBehaviour that lives on a BuildingPart prefab
/// and participates in the grid system.
///
/// Lifecycle:
///   OnPlaced()  → called once after instantiation. Subscribe to BuildingEvents here.
///   OnMoved()   → called via BuildingEvents.onMoved when this building moves/rotates.
///   OnRemoved() → called via BuildingEvents.onRemoved. Unsubscribe from events here.
///
/// Rule: never cache a direct ref to other BuildingInstances across frames.
///       Query the board via GridManager.I.board[coord] instead — it's always current.
/// </summary>
public interface IPartBehavior
{
    void OnPlaced(BuildingInstance owner, int partIndex);
    void OnRemoved();
    void OnMoved() { }   // default-implemented — simple parts can ignore it
}
