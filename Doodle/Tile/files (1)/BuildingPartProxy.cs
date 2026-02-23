using UnityEngine;

/// <summary>
/// Sits on every spawned part GameObject (the ones with colliders).
/// When a raycast hits a part's collider, grab this component to reach
/// the owning BuildingInstance without any GetComponentInParent() gambling.
///
/// Wired up automatically by BuildingInstance.SpawnParts().
/// </summary>
public class BuildingPartProxy : MonoBehaviour
{
    public BuildingInstance owner     { get; private set; }
    public int              partIndex { get; private set; }

    public void Init(BuildingInstance ownerInstance, int idx)
    {
        owner     = ownerInstance;
        partIndex = idx;
    }
}
