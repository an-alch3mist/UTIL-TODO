using System.Collections.Generic;
using UnityEngine;
using SPACE_UTIL;

/// <summary>
/// Live placed building on the grid.
///
/// Indicator child GO naming convention:
///   The prefab root must have a child named "Indicator" (case-insensitive).
///   That GO holds your green/red shadow mesh renderer.
///   BuildingInstance enables/disables it and swaps between two materials.
///   You author it entirely in the editor — this script just drives it.
/// </summary>
public class BuildingInstance : MonoBehaviour
{
    // ── Identity ──────────────────────────────────────────────────────────────
    public BuildingSO data        { get; private set; }
    public v2         pivotCoord  { get; private set; }
    public int        rotation    { get; private set; }

    public IReadOnlyList<v2> occupiedCoords => _occupiedCoords;
    private readonly List<v2> _occupiedCoords = new List<v2>();

    // ── Spawned part GOs ──────────────────────────────────────────────────────
    private readonly List<GameObject> _partGOs = new List<GameObject>();

    // ── Indicator ─────────────────────────────────────────────────────────────
    [Header("Indicator (auto-found by name 'Indicator')")]
    public Material indicatorValidMat;
    public Material indicatorInvalidMat;

    private GameObject  _indicatorGO;
    private Renderer[]  _indicatorRenderers;

    // ── Initialise ────────────────────────────────────────────────────────────
    public void Initialise(BuildingSO buildingData, v2 pivot, int rot,
                           float cellSize, Vector3 worldOrigin)
    {
        data       = buildingData;
        pivotCoord = pivot;
        rotation   = rot;

        CacheIndicator();
        SetIndicatorActive(false);   // hidden until dragged

        RebuildOccupied();
        SpawnParts(cellSize, worldOrigin);

        // Subscribe to events so parts react without being directly called
        BuildingEvents.onMoved   += OnBuildingMoved;
        BuildingEvents.onRemoved += OnBuildingRemoved;
    }

    // ── Visual preview (no board write) ───────────────────────────────────────

    /// <summary>
    /// Move the visual root to a candidate grid position during drag.
    /// Does NOT touch Board&lt;Tile&gt; — call GridManager.LandOn() to commit.
    /// </summary>
    public void PreviewAt(v2 candidatePivot, int candidateRot)
    {
        Vector3 world = GridManager.I.GridToWorld(candidatePivot);
        transform.position = world;
        transform.rotation = Quaternion.Euler(0, candidateRot * 90f, 0);
    }

    // ── Apply confirmed placement ─────────────────────────────────────────────
    public void ApplyNewPlacement(v2 newPivot, int newRot, float cellSize, Vector3 worldOrigin)
    {
        pivotCoord = newPivot;
        rotation   = newRot;
        RebuildOccupied();
        RepositionParts(cellSize, worldOrigin);
    }

    // ── Indicator ─────────────────────────────────────────────────────────────

    /// <summary>Enable or disable the indicator child GO.</summary>
    public void SetIndicatorActive(bool active)
    {
        if (_indicatorGO != null) _indicatorGO.SetActive(active);
    }

    /// <summary>Swap indicator to green (valid) or red (invalid) material.</summary>
    public void SetIndicator(bool valid)
    {
        if (_indicatorRenderers == null) return;
        Material mat = valid ? indicatorValidMat : indicatorInvalidMat;
        if (mat == null) return;
        foreach (var r in _indicatorRenderers)
            r.sharedMaterial = mat;
    }

    // ── Cleanup ───────────────────────────────────────────────────────────────
    public void Despawn()
    {
        BuildingEvents.onMoved   -= OnBuildingMoved;
        BuildingEvents.onRemoved -= OnBuildingRemoved;

        foreach (var go in _partGOs)
            if (go != null) Destroy(go);
        _partGOs.Clear();
        _occupiedCoords.Clear();
    }

    // ── Hover highlight ───────────────────────────────────────────────────────

    /// <summary>
    /// Called by BuildingHoverProxy when the cursor enters ANY part collider on this building.
    /// <paramref name="sourceProxy"/> is the part that was entered — use it if you need
    /// per-part highlighting, otherwise apply highlight to all parts here.
    ///
    /// TODO: implement your outline / emissive / shader-swap logic here.
    /// </summary>
    public void OnHighlight(BuildingHoverProxy sourceProxy)
    {
        // TODO: enable outline effect, set emissive, etc.
    }

    /// <summary>
    /// Called by BuildingHoverProxy when the cursor exits ANY part collider on this building.
    /// Only fires the unhighlight when ALL parts have been exited (hover-count tracking).
    ///
    /// TODO: implement removal of highlight here.
    /// </summary>
    public void OnUnhighlight(BuildingHoverProxy sourceProxy)
    {
        // TODO: disable outline effect, reset emissive, etc.
    }

    // ── Query ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets a behavior component of type T on a specific part GO.
    /// Used by neighbor queries: "does the tile next to me have a ConveyorBelt?"
    /// </summary>
    public T GetPartBehavior<T>(int partIndex) where T : class
    {
        if (partIndex < 0 || partIndex >= _partGOs.Count) return null;
        var go = _partGOs[partIndex];
        return go != null ? go.GetComponent<T>() : null;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Event handlers — parts subscribe via BuildingEvents, not direct calls
    // ═════════════════════════════════════════════════════════════════════════

    private void OnBuildingMoved(BuildingInstance b, BuildingMoveArgs args)
    {
        // Only care about ourselves
        if (b != this) return;

        // Notify all IPartBehavior components on our parts
        foreach (var go in _partGOs)
        {
            if (go == null) continue;
            foreach (var behavior in go.GetComponents<IPartBehavior>())
                behavior.OnMoved();
        }
    }

    private void OnBuildingRemoved(BuildingInstance b)
    {
        if (b != this) return;
        // Parts that subscribed to global events via IPartBehavior should
        // unsubscribe in their own OnRemoved(). We call it here as the trigger.
        foreach (var go in _partGOs)
        {
            if (go == null) continue;
            foreach (var behavior in go.GetComponents<IPartBehavior>())
                behavior.OnRemoved();
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Private helpers
    // ═════════════════════════════════════════════════════════════════════════

    private void RebuildOccupied()
    {
        _occupiedCoords.Clear();
        foreach (var offset in data.GetRotatedOffsets(rotation))
            _occupiedCoords.Add(pivotCoord + offset);
    }

    private void CacheIndicator()
    {
        // Q() search for a child named "indicator" (case-insensitive via your UTIL default)
        var indicatorGO = gameObject.Q().downNamed("indicator").gf();
        if (indicatorGO == null)
        {
            Debug.LogWarning($"[BuildingInstance] '{data?.buildingName}' has no 'Indicator' child GO. " +
                              "Add one to the prefab root with a mesh renderer for drag feedback.");
            return;
        }
        _indicatorGO        = indicatorGO;
        _indicatorRenderers = indicatorGO.GetComponentsInChildren<Renderer>();
    }

    private void SpawnParts(float cellSize, Vector3 worldOrigin)
    {
        foreach (var go in _partGOs)
            if (go != null) Destroy(go);
        _partGOs.Clear();

        for (int i = 0; i < data.parts.Count; i++)
        {
            var part = data.parts[i];
            if (part.prefab == null) { _partGOs.Add(null); continue; }

            var go = Instantiate(part.prefab, transform);
            go.name = $"{data.buildingName}_Part{i}_{part.label}";

            // Proxy for O(1) part-index lookup on collider hit
            var proxy = go.GetComponent<BuildingPartProxy>();
            if (proxy == null) proxy = go.AddComponent<BuildingPartProxy>();
            proxy.Init(this, i);

            // Hover proxy — auto-attached so every part collider reports hover to us
            // Requires a Collider on the part prefab (which it must have anyway for selection)
            if (go.GetComponent<Collider>() != null)
            {
                var hoverProxy = go.GetComponent<BuildingHoverProxy>();
                if (hoverProxy == null) go.AddComponent<BuildingHoverProxy>();
                // BuildingHoverProxy.Awake() calls Q().up<BuildingInstance>() to cache _owner
            }

            // IPartBehavior.OnPlaced() — parts set up their own event subscriptions here
            foreach (var behavior in go.GetComponents<IPartBehavior>())
                behavior.OnPlaced(this, i);

            _partGOs.Add(go);
        }

        RepositionParts(cellSize, worldOrigin);
    }

    private void RepositionParts(float cellSize, Vector3 worldOrigin)
    {
        transform.position = GridManager.I.GridToWorld(pivotCoord);
        transform.rotation = Quaternion.Euler(0, rotation * 90f, 0);

        for (int i = 0; i < data.parts.Count && i < _partGOs.Count; i++)
        {
            if (_partGOs[i] == null) continue;
            _partGOs[i].transform.localPosition = data.parts[i].prefabLocalOffset;
        }
    }

    // ── Static grid-to-world (used before GridManager may exist at init) ──────
    public static Vector3 GridToWorld(v2 coord, float cellSize, Vector3 worldOrigin) =>
        worldOrigin + new Vector3(coord.x * cellSize, 0, coord.y * cellSize);
}
