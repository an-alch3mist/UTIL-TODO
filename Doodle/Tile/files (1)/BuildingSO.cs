using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SPACE_UTIL;

// ═══════════════════════════════════════════════════════════════════════════════
//  BuildingPart
//  One "prefab mesh+collider" unit inside a building.
//  A single part can occupy MULTIPLE tiles (e.g. the 3-tile "B" mesh).
// ═══════════════════════════════════════════════════════════════════════════════
[System.Serializable]
public class BuildingPart
{
    [Tooltip("The actual GameObject prefab (mesh + collider). Can span multiple tiles.")]
    public GameObject prefab;

    [Tooltip("Tile offsets this part CLAIMS, relative to the building's pivot (0,0).\n" +
             "Example: a 3-tile horizontal bar at offsets (-2,0),(-1,0),(0,0).")]
    public List<v2> occupiedOffsets = new List<v2>();

    [Tooltip("Local position offset to apply when spawning this prefab,\n" +
             "relative to the pivot world position. Usually the visual centre of the part.")]
    public Vector3 prefabLocalOffset = Vector3.zero;

    [Tooltip("Optional tag / label for editor clarity (e.g. 'MainBody', 'Entrance').")]
    public string label = "";
}

// ═══════════════════════════════════════════════════════════════════════════════
//  BuildingSO
//  ScriptableObject that fully describes a building's footprint and visuals.
//
//  Coordinate convention
//  ─────────────────────
//  All offsets are in LOCAL building space where (0,0) = pivot.
//  Pivot is the rotation anchor used when placing / rotating on the grid.
//
//  Example (from the prompt):
//
//     BBE   ← y = +0 (relative)
//     BOE   ← y = -1
//     O     ← y = -2
//     ^
//     Pivot = top-right E = (0, 0)
//
//  Part layout for that building:
//     Part[0] "B-slab"   prefab occupies (-2,0),(-1,0),(-2,-1)
//     Part[1] "O-mid"    prefab occupies (-1,-1)
//     Part[2] "O-bot"    prefab occupies (-2,-2)
//     Part[3] "E-top"    prefab occupies (0, 0)   ← the pivot tile itself
//     Part[4] "E-bot"    prefab occupies (0,-1)
// ═══════════════════════════════════════════════════════════════════════════════
[CreateAssetMenu(fileName = "New Building", menuName = "TileSystem/Building")]
public class BuildingSO : ScriptableObject
{
    [Header("Identity")]
    public string buildingName = "New Building";
    [TextArea] public string description = "";
    public Sprite icon;

    [Header("Parts")]
    [Tooltip("Each entry is one prefab + the tile offsets it occupies.")]
    public List<BuildingPart> parts = new List<BuildingPart>();

    [Header("Pivot")]
    [Tooltip("Human-readable note. The pivot is always the LOCAL origin (0,0).\n" +
             "Set this for documentation only.")]
    public string pivotNote = "top-right corner";

    // ── Derived helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// All tile offsets claimed by this building (union across all parts), at rotation 0.
    /// </summary>
    public List<v2> allOffsets
    {
        get
        {
            var set = new HashSet<v2>();   // v2 is a struct so value-equality works fine
            foreach (var part in parts)
                foreach (var offset in part.occupiedOffsets)
                    set.Add(offset);
            return set.ToList();
        }
    }

    /// <summary>
    /// Returns all tile offsets after applying <paramref name="rotation"/> 90° steps CW.
    /// </summary>
    public List<v2> GetRotatedOffsets(int rotation)
    {
        return allOffsets.Select(o => RotateOffset(o, rotation)).ToList();
    }

    /// <summary>
    /// Returns per-part rotated offsets so BuildingInstance knows what each part claims.
    /// </summary>
    public List<List<v2>> GetRotatedOffsetsByPart(int rotation)
    {
        return parts.Select(p =>
            p.occupiedOffsets.Select(o => RotateOffset(o, rotation)).ToList()
        ).ToList();
    }

    // ── Static rotation math ──────────────────────────────────────────────────

    /// <summary>
    /// Rotates a local v2 offset by <paramref name="rotation"/> × 90° clockwise.
    ///
    ///   CW 90°:  (x, y) → ( y, -x)
    ///   CW 180°: (x, y) → (-x, -y)
    ///   CW 270°: (x, y) → (-y,  x)
    /// </summary>
    public static v2 RotateOffset(v2 offset, int rotation)
    {
        rotation = ((rotation % 4) + 4) % 4;   // normalise to 0-3
        v2 o = offset;
        for (int i = 0; i < rotation; i++)
            o = new v2(o.y, -o.x);             // one step CW
        return o;
    }

    /// <summary>
    /// Rotates the visual localOffset Vector3 (XZ plane, Y up) for spawning the prefab.
    /// </summary>
    public static Vector3 RotateLocalOffset(Vector3 offset, int rotation)
    {
        rotation = ((rotation % 4) + 4) % 4;
        Vector3 o = offset;
        for (int i = 0; i < rotation; i++)
            o = new Vector3(o.z, o.y, -o.x);   // CW 90° in XZ
        return o;
    }

#if UNITY_EDITOR
    // ── Editor validation ─────────────────────────────────────────────────────
    private void OnValidate()
    {
        // Warn if two parts claim the same offset
        var seen  = new HashSet<v2>();
        var dupes = new List<v2>();
        foreach (var part in parts)
            foreach (var o in part.occupiedOffsets)
                if (!seen.Add(o)) dupes.Add(o);

        if (dupes.Count > 0)
            Debug.LogWarning($"[BuildingSO] '{buildingName}' has duplicate offsets: " +
                             string.Join(", ", dupes), this);
    }
#endif
}
