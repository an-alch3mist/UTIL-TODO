// ============================================================
//  NPCDefinition.cs
//  ScriptableObject that holds all static/immutable data
//  for an NPC type. Follows the Definition/Instance split:
//    - NPCDefinition  = asset in project, never changes at runtime
//    - NPC            = MonoBehaviour instance with runtime state
//
//  Create via: Right-click → Create → NPCSystem → NPC Definition
// ============================================================

using UnityEngine;

namespace NPCSystem
{
    [CreateAssetMenu(
        fileName = "NPCDefinition_New",
        menuName  = "NPCSystem/NPC Definition",
        order     = 1)]
    public class NPCDefinition : ScriptableObject
    {
        // -------------------------------------------------------
        // Identity
        // -------------------------------------------------------
        [Header("Identity")]
        public string DisplayName   = "NPC";
        public string UniqueID      = "";       // GUID, set in Inspector
        public ENPCArchetype Archetype = ENPCArchetype.Civilian;
        public ENPCFaction   Faction   = ENPCFaction.Civilian;

        [TextArea(2, 4)]
        public string Description = "";

        // -------------------------------------------------------
        // Health
        // -------------------------------------------------------
        [Header("Health")]
        [Range(1f, 500f)]
        public float MaxHealth           = 100f;

        [Range(0f, 1f)]
        [Tooltip("Chance 0–1 that a lethal hit knocks out instead of killing.")]
        public float KnockoutChance      = 0.2f;

        [Range(0f, 300f)]
        [Tooltip("Seconds until the NPC revives from unconscious state. 0 = never revives.")]
        public float UnconsciousDuration = 30f;

        // -------------------------------------------------------
        // Movement
        // -------------------------------------------------------
        [Header("Movement")]
        [Range(0.5f, 10f)] public float WalkSpeed    = 2.5f;
        [Range(1f,  15f)]  public float RunSpeed     = 5.5f;
        [Range(1f,  15f)]  public float CombatSpeed  = 4.5f;
        [Range(0.1f, 5f)]  public float RotationSpeed = 3f;

        [Tooltip("NavMesh stopping distance when reaching a waypoint.")]
        [Range(0.05f, 2f)] public float StoppingDistance = 0.4f;

        // -------------------------------------------------------
        // Awareness / Vision
        // -------------------------------------------------------
        [Header("Awareness")]
        [Range(1f, 30f)]   public float SightRange         = 12f;
        [Range(10f, 180f)] public float SightAngle         = 90f;
        [Range(1f, 10f)]   public float HearingRange       = 5f;
        [Range(0f, 1f)]    [Tooltip("How quickly this NPC calms down after a threat disappears.")]
        public float CalmDownRate = 0.2f;

        [Tooltip("Layer mask for line-of-sight occlusion checks.")]
        public LayerMask SightOcclusionMask;

        // -------------------------------------------------------
        // Combat
        // -------------------------------------------------------
        [Header("Combat")]
        public bool  CanFight            = false;
        public bool  CanShoot            = false;
        [Range(0f, 50f)] public float AttackRange = 1.8f;
        [Range(1f, 100f)] public float AttackDamage = 15f;
        [Range(0.2f, 5f)] public float AttackCooldown = 1.0f;

        // -------------------------------------------------------
        // Civilian Behaviour
        // -------------------------------------------------------
        [Header("Civilian Behaviour")]
        public bool CallsPoliceOnCrime   = true;
        public bool FleesThreat          = true;
        [Range(0f, 1f)] public float PanicChance = 0.7f;

        // -------------------------------------------------------
        // Dialogue
        // -------------------------------------------------------
        [Header("Dialogue")]
        public AudioClip[] IdleVoiceClips;
        public AudioClip[] AlertVoiceClips;
        public AudioClip[] CombatVoiceClips;
        public AudioClip[] DeathVoiceClips;

        // -------------------------------------------------------
        // Patrol
        // -------------------------------------------------------
        [Header("Patrol")]
        public PatrolRoute DefaultPatrolRoute;

        // -------------------------------------------------------
        // Validation
        // -------------------------------------------------------
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(UniqueID))
                UniqueID = System.Guid.NewGuid().ToString();

            RunSpeed   = Mathf.Max(RunSpeed,   WalkSpeed);
            CombatSpeed = Mathf.Clamp(CombatSpeed, WalkSpeed, RunSpeed);
        }
    }
}
