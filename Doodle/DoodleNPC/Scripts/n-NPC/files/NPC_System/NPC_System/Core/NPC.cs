// ============================================================
//  NPC.cs
//  Root MonoBehaviour. The central hub of an NPC character.
//
//  Design:
//   - Does NOT hold game logic — it delegates to sub-components
//   - Holds references to all sub-systems as public properties
//   - Other systems get to NPC sub-systems THROUGH this class
//   - Named NPC types (CustomerNPC, PoliceNPC) extend this class
//
//  Component architecture on NPC prefab:
//   ├── NPC.cs               ← root
//   ├── NPCHealth.cs
//   ├── NPCMovement.cs       (+ NavMeshAgent)
//   ├── NPCAwareness.cs
//   ├── NPCAnimation.cs      (+ Animator)
//   ├── NPCBehaviour.cs
//   ├── NPCScheduleManager.cs
//   └── [Behaviour components as children]
//       ├── IdleBehaviour
//       ├── WanderBehaviour
//       ├── PatrolBehaviour
//       ├── CoweringBehaviour
//       ├── FleeBehaviour
//       ├── CombatBehaviour
//       ├── RagdollBehaviour
//       ├── DeadBehaviour
//       ├── UnconsciousBehaviour
//       ├── HeavyFlinchBehaviour
//       ├── FaceTargetBehaviour
//       ├── CallPoliceBehaviour
//       └── GenericDialogueBehaviour
// ============================================================

using System;
using UnityEngine;

namespace NPCSystem
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NPCHealth))]
    [RequireComponent(typeof(NPCMovement))]
    [RequireComponent(typeof(NPCAwareness))]
    [RequireComponent(typeof(NPCAnimation))]
    [RequireComponent(typeof(NPCBehaviour))]
    [RequireComponent(typeof(NPCScheduleManager))]
    public class NPC : MonoBehaviour
    {
        // -------------------------------------------------------
        // Inspector
        // -------------------------------------------------------
        [Header("NPC Definition")]
        [SerializeField] private NPCDefinition _definition;

        [Header("Debug")]
        [SerializeField] private bool _debugMode = false;

        // -------------------------------------------------------
        // Sub-system references (read-only public)
        // -------------------------------------------------------
        public NPCDefinition    Definition      { get; private set; }
        public NPCHealth        Health          { get; private set; }
        public NPCMovement      Movement        { get; private set; }
        public NPCAwareness     Awareness       { get; private set; }
        public NPCAnimation     Animation       { get; private set; }
        public NPCBehaviour     BehaviourManager{ get; private set; }
        public NPCScheduleManager Schedule      { get; private set; }

        // -------------------------------------------------------
        // Convenience state
        // -------------------------------------------------------
        public bool IsAlive       => Health?.IsAlive       ?? false;
        public bool IsDead        => Health?.IsDead        ?? true;
        public bool IsUnconscious => Health?.IsUnconscious ?? false;
        public string DisplayName => Definition?.DisplayName ?? name;

        // -------------------------------------------------------
        // Events (bubble up from sub-systems for easy external access)
        // -------------------------------------------------------
        public event Action<NPC>              OnInitialized;
        public event Action<EKnockdownCause>  OnDied;
        public event Action<float, GameObject>OnDamaged;

        // -------------------------------------------------------
        // Unity
        // -------------------------------------------------------
        private void Awake()
        {
            // Gather all sub-components
            Health           = GetComponent<NPCHealth>();
            Movement         = GetComponent<NPCMovement>();
            Awareness        = GetComponent<NPCAwareness>();
            Animation        = GetComponent<NPCAnimation>();
            BehaviourManager = GetComponent<NPCBehaviour>();
            Schedule         = GetComponent<NPCScheduleManager>();
        }

        private void Start()
        {
            if (_definition != null)
                Initialize(_definition);
        }

        // -------------------------------------------------------
        // Initialization
        // -------------------------------------------------------
        /// <summary>
        /// Fully initializes the NPC with a definition.
        /// Can be called at runtime to swap definition (respawn, etc).
        /// </summary>
        public virtual void Initialize(NPCDefinition definition)
        {
            Definition = definition;

            // Initialize each sub-system with the definition
            Health?.Initialize(definition);
            Movement?.Initialize(definition);
            Awareness?.Initialize(definition);

            // Wire health events up to this class for external access
            if (Health != null)
            {
                Health.OnDied     += HandleDied;
                Health.OnDamaged  += HandleDamaged;
            }

            // Initialize behaviour stack
            BehaviourManager?.Initialize(this);

            // Initialize schedule
            Schedule?.Initialize(this);

            if (_debugMode)
                Debug.Log($"[NPC] {DisplayName} initialized.");

            OnInitialized?.Invoke(this);
        }

        private void OnDestroy()
        {
            if (Health != null)
            {
                Health.OnDied    -= HandleDied;
                Health.OnDamaged -= HandleDamaged;
            }
        }

        // -------------------------------------------------------
        // Public actions (external code calls these, not sub-systems directly)
        // -------------------------------------------------------

        /// <summary>Deal damage to this NPC from an external source.</summary>
        public void TakeDamage(float amount, GameObject source, EKnockdownCause cause = EKnockdownCause.Melee)
            => Health?.TakeDamage(amount, source, cause);

        /// <summary>Issue a signal to override the NPC's current schedule.</summary>
        public void IssueSignal(NPCSignal signal)
            => Schedule?.IssueSignal(signal);

        /// <summary>Force this NPC to look at a target.</summary>
        public void LookAt(Transform target)
            => BehaviourManager?.GetBehaviour<FaceTargetBehaviour>()?.SetTarget(target);

        /// <summary>Start a dialogue interaction (pauses current behaviour).</summary>
        public void StartDialogue(float duration)
        {
            BehaviourManager?.GetBehaviour<GenericDialogueBehaviour>()?.StartDialogue(duration);
        }

        /// <summary>Make this NPC immediately aware of a threat.</summary>
        public void SetThreat(GameObject threat)
            => Awareness?.SetThreat(threat);

        /// <summary>Calm the NPC and reset threat awareness.</summary>
        public void Calm() => Awareness?.Calm();

        // -------------------------------------------------------
        // Event relay
        // -------------------------------------------------------
        private void HandleDied(EKnockdownCause cause)
        {
            OnDied?.Invoke(cause);
            if (_debugMode)
                Debug.Log($"[NPC] {DisplayName} died from {cause}");
        }

        private void HandleDamaged(float amount, GameObject source)
        {
            OnDamaged?.Invoke(amount, source);

            // Trigger flinch on significant damage
            if (amount >= 10f)
                BehaviourManager?.GetBehaviour<HeavyFlinchBehaviour>()?.TriggerFlinch();

            // Aware of attacker
            if (source != null)
                Awareness?.SetThreat(source);
        }

        // -------------------------------------------------------
        // Debug
        // -------------------------------------------------------
        private void OnGUI()
        {
            if (!_debugMode) return;
            var cam = Camera.main;
            if (cam == null) return;

            Vector3 screen = cam.WorldToScreenPoint(transform.position + Vector3.up * 2.2f);
            if (screen.z < 0) return;

            GUI.Label(new Rect(screen.x - 80, Screen.height - screen.y, 160, 60),
                $"<color=yellow>{DisplayName}</color>\n" +
                $"HP:{Health?.CurrentHealth:F0}/{Health?.MaxHealth:F0}\n" +
                $"Act:{BehaviourManager?.ActiveBehaviour?.BehaviourName ?? "none"}");
        }
    }
}
