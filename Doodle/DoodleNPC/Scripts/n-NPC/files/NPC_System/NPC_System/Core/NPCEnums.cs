// ============================================================
//  NPCEnums.cs
//  All enumerations used across the NPC system.
// ============================================================

namespace NPCSystem
{
    // -------------------------------------------------------
    // Movement
    // -------------------------------------------------------

    /// <summary>Result of a pathfinding walk request.</summary>
    public enum EWalkResult
    {
        Success,        // NPC reached the destination
        Failed,         // Pathfinding could not find a route
        Interrupted,    // Walk was cancelled before completion
        Timeout         // NPC took too long and gave up
    }

    /// <summary>Current locomotion state of the NPC.</summary>
    public enum ELocomotionState
    {
        Idle,
        Walking,
        Running,
        Crouching,
        Ragdoll
    }

    // -------------------------------------------------------
    // Health / Combat
    // -------------------------------------------------------

    /// <summary>Broad health state of the NPC.</summary>
    public enum EHealthState
    {
        Alive,
        Unconscious,    // Knocked out — can be revived
        Dead
    }

    /// <summary>How the NPC died or fell unconscious.</summary>
    public enum EKnockdownCause
    {
        None,
        Melee,
        Gunshot,
        Explosion,
        Taser,
        Fall
    }

    // -------------------------------------------------------
    // Awareness / Detection
    // -------------------------------------------------------

    /// <summary>How alert the NPC currently is.</summary>
    public enum EAlertLevel
    {
        Calm,           // Normal routine, nothing suspicious
        Suspicious,     // Noticed something — investigating
        Alert,          // Confirmed threat — heightened state
        Combat          // Actively fighting or fleeing
    }

    /// <summary>The player's legal status from this NPC's perspective.</summary>
    public enum EPlayerThreatLevel
    {
        None,
        Suspicious,
        Wanted,
        HighlyDangerous
    }

    // -------------------------------------------------------
    // NPC Type / Faction
    // -------------------------------------------------------

    /// <summary>High-level faction for NPC behaviour decisions.</summary>
    public enum ENPCFaction
    {
        Civilian,
        Customer,
        Employee,
        Police,
        Cartel,
        Gang
    }

    /// <summary>Broad NPC archetype, drives default behaviour stack.</summary>
    public enum ENPCArchetype
    {
        Civilian,       // Idle/Wander/Cower/CallPolice
        Customer,       // Same as civilian + RequestProduct
        Employee,       // Work tasks + wander
        CombatNPC,      // Combat + Patrol
        Police          // Patrol + Combat + Arrest
    }

    // -------------------------------------------------------
    // Schedule
    // -------------------------------------------------------

    /// <summary>Current state of a scheduled action.</summary>
    public enum EActionState
    {
        Pending,        // Waiting for its start time
        Active,         // Currently executing
        Completed,      // Finished normally
        Interrupted,    // Stopped before completion
        Skipped         // Start time passed without activation
    }

    // -------------------------------------------------------
    // Dialogue / Interaction
    // -------------------------------------------------------

    /// <summary>The mood the NPC expresses in dialogue.</summary>
    public enum ENPCMood
    {
        Neutral,
        Happy,
        Angry,
        Scared,
        Suspicious,
        Grateful
    }

    // -------------------------------------------------------
    // Behaviour priority constants (reference values)
    // -------------------------------------------------------
    public static class BehaviourPriority
    {
        public const int Dead               = 10000;
        public const int Ragdoll            = 9000;
        public const int Unconscious        = 8500;
        public const int HeavyFlinch        = 8000;
        public const int Combat             = 7000;
        public const int Flee               = 6000;
        public const int CallPolice         = 5000;
        public const int Cowering           = 4000;
        public const int GenericDialogue    = 3000;
        public const int RequestProduct     = 2500;
        public const int ConsumeProduct     = 2000;
        public const int Summon             = 1500;
        public const int FaceTarget         = 1000;
        public const int Work               = 500;
        public const int Stationary         = 400;
        public const int Patrol             = 300;
        public const int Wander             = 200;
        public const int Idle               = 100;
    }
}
