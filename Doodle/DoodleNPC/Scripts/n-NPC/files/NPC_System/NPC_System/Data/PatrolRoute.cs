// ============================================================
//  PatrolRoute.cs
//  ScriptableObject that stores a list of patrol waypoints.
//  Shared by multiple NPCs that walk the same route.
//  Transform-based waypoints are resolved at runtime from
//  the scene via named GameObjects or direct references.
//
//  Create via: Right-click → Create → NPCSystem → Patrol Route
// ============================================================

using System.Collections.Generic;
using UnityEngine;

namespace NPCSystem
{
    [CreateAssetMenu(
        fileName = "PatrolRoute_New",
        menuName  = "NPCSystem/Patrol Route",
        order     = 2)]
    public class PatrolRoute : ScriptableObject
    {
        // -------------------------------------------------------
        // Settings
        // -------------------------------------------------------
        [Header("Settings")]
        [Tooltip("Loop back to the start, or ping-pong back and forth.")]
        public EPatrolMode Mode = EPatrolMode.Loop;

        [Tooltip("How long the NPC waits at each waypoint (seconds).")]
        [Range(0f, 30f)]
        public float WaypointWaitTime = 2f;

        [Tooltip("Whether NPC walks or runs between waypoints.")]
        public bool RunBetweenWaypoints = false;

        // -------------------------------------------------------
        // Waypoints
        // -------------------------------------------------------
        [Header("Waypoints")]
        [Tooltip("Scene object names to resolve at runtime.")]
        public string[] WaypointObjectNames;

        // Runtime-resolved transforms (filled by PatrolBehaviour.Awake)
        [System.NonSerialized]
        public List<Transform> ResolvedWaypoints = new List<Transform>();

        // -------------------------------------------------------
        // Enum
        // -------------------------------------------------------
        public enum EPatrolMode { Loop, PingPong, Random }

        // -------------------------------------------------------
        // API
        // -------------------------------------------------------
        /// <summary>
        /// Resolve waypoint names into scene transforms.
        /// Call once from PatrolBehaviour.Initialize().
        /// </summary>
        public void Resolve()
        {
            ResolvedWaypoints.Clear();
            if (WaypointObjectNames == null) return;

            foreach (var n in WaypointObjectNames)
            {
                var go = GameObject.Find(n);
                if (go != null)
                    ResolvedWaypoints.Add(go.transform);
                else
                    Debug.LogWarning($"[PatrolRoute] Could not find waypoint object: '{n}'");
            }
        }

        /// <summary>Get next waypoint index given current index and direction.</summary>
        public int GetNextIndex(int current, ref int direction)
        {
            if (ResolvedWaypoints.Count == 0) return 0;

            switch (Mode)
            {
                case EPatrolMode.Loop:
                    return (current + 1) % ResolvedWaypoints.Count;

                case EPatrolMode.PingPong:
                    int next = current + direction;
                    if (next >= ResolvedWaypoints.Count) { direction = -1; next = current - 1; }
                    if (next < 0)                        { direction =  1; next = current + 1; }
                    return Mathf.Clamp(next, 0, ResolvedWaypoints.Count - 1);

                case EPatrolMode.Random:
                    int r = Random.Range(0, ResolvedWaypoints.Count);
                    return (r == current && ResolvedWaypoints.Count > 1)
                        ? (r + 1) % ResolvedWaypoints.Count : r;

                default: return 0;
            }
        }
    }
}
