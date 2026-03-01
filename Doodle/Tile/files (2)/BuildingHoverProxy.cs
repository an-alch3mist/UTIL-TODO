using UnityEngine;
using SPACE_UTIL;

/// <summary>
/// Attach to every spawned part GameObject (done automatically by BuildingInstance.SpawnParts).
/// Unity's OnMouseEnter/Exit fire per-collider, so this component bridges those events
/// up to the whole BuildingInstance via Q().up().
///
/// When any part is hovered, the ENTIRE building is highlighted — not just the part.
///
/// OnHighlight() and OnUnhighlight() are intentionally empty.
/// Implement your outline / emissive / shader swap logic there.
/// </summary>
[RequireComponent(typeof(Collider))]
public class BuildingHoverProxy : MonoBehaviour
{
    // Cached on first hover — avoids Q() traversal every frame
    private BuildingInstance _owner;

    private void Awake()
    {
        // Walk up the hierarchy to find the owning BuildingInstance
        // Uses your UTIL Q() fluent API — no GetComponentInParent
        _owner = gameObject.Q().up<BuildingInstance>().gf<BuildingInstance>();

        if (_owner == null)
            Debug.LogWarning($"[BuildingHoverProxy] '{name}' could not find a parent BuildingInstance.");
    }

    // Unity message — fires when the cursor enters this part's collider
    private void OnMouseEnter()
    {
        if (_owner == null) return;
        _owner.OnHighlight(this);
    }

    // Unity message — fires when the cursor leaves this part's collider
    private void OnMouseExit()
    {
        if (_owner == null) return;
        _owner.OnUnhighlight(this);
    }
}
