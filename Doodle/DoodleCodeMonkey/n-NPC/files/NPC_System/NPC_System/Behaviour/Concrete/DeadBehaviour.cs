// ============================================================
//  RagdollBehaviour.cs
//  Activates physics ragdoll; disables NavMesh.
// ============================================================
using System;
using UnityEngine;

namespace NPCSystem
{
    public class RagdollBehaviour : Behaviour
    {
        protected override void Awake()
        {
            base.Awake();
            BehaviourName = "Ragdoll";
            Priority      = BehaviourPriority.Ragdoll;
        }

        protected override void OnActivated()
        {
            Movement?.EnableNavAgent(false);
            Animation?.SetRagdollActive(true);
        }

        protected override void OnDeactivated()
        {
            Animation?.SetRagdollActive(false);
            Movement?.EnableNavAgent(true);
        }
    }
}


// ============================================================
//  DeadBehaviour.cs
//  Terminal state. Disables agent, plays death, never exits.
// ============================================================
namespace NPCSystem
{
    public class DeadBehaviour : Behaviour
    {
        public event Action OnNPCDead;

        protected override void Awake()
        {
            base.Awake();
            BehaviourName  = "Dead";
            Priority       = BehaviourPriority.Dead;
        }

        protected override void OnActivated()
        {
            Movement?.StopWalk(false);
            Movement?.EnableNavAgent(false);
            OnNPCDead?.Invoke();
        }

        // Can never be deactivated once dead
        public override void Deactivate() { }
        public override void Disable()    { }
    }
}


// ============================================================
//  UnconsciousBehaviour.cs
//  NPC is knocked out. Plays downed animation.
//  Revived by NPCHealth timer → NPCBehaviour.OnRevived.
// ============================================================
namespace NPCSystem
{
    public class UnconsciousBehaviour : Behaviour
    {
        protected override void Awake()
        {
            base.Awake();
            BehaviourName = "Unconscious";
            Priority      = BehaviourPriority.Unconscious;
        }

        protected override void OnActivated()
        {
            Movement?.StopWalk(false);
            Movement?.EnableNavAgent(false);
        }

        protected override void OnDeactivated()
        {
            Movement?.EnableNavAgent(true);
        }
    }
}


// ============================================================
//  CallPoliceBehaviour.cs
//  NPC calls for police after witnessing a crime.
//  Stands still, plays phone-call animation, then calms down.
// ============================================================

namespace NPCSystem
{
    public class CallPoliceBehaviour : Behaviour
    {
        [Header("Call Police")]
        [SerializeField] private float _callDuration = 5f;
        [SerializeField] private float _cooldown     = 30f;

        private float _callTimer;
        private float _cooldownTimer;
        private bool  _onCooldown;

        protected override void Awake()
        {
            base.Awake();
            BehaviourName = "CallPolice";
            Priority      = BehaviourPriority.CallPolice;
        }

        public override bool WantsToBeActive()
            => Enabled && !_onCooldown;

        protected override void OnActivated()
        {
            _callTimer = 0f;
            Movement?.StopWalk(false);
            // Play phone animation — hook to Animation.TriggerTalk() or custom clip
            Animation?.TriggerTalk();
            Debug.Log($"[{Npc.name}] Calling police!");
        }

        public override void BehaviourUpdate()
        {
            _callTimer += Time.deltaTime;
            if (_callTimer >= _callDuration)
            {
                // Police called — start cooldown, disable
                _onCooldown    = true;
                _cooldownTimer = 0f;
                Disable();
                Awareness?.Calm();
            }
        }

        private void Update()
        {
            if (!_onCooldown) return;
            _cooldownTimer += Time.deltaTime;
            if (_cooldownTimer >= _cooldown)
                _onCooldown = false;
        }
    }
}


// ============================================================
//  GenericDialogueBehaviour.cs
//  NPC stops and plays a dialogue exchange.
//  Pause other behaviours, show speech bubble, then resume.
// ============================================================
namespace NPCSystem
{
    public class GenericDialogueBehaviour : Behaviour
    {
        public event Action OnDialogueStarted;
        public event Action OnDialogueEnded;

        private bool _inDialogue;
        private float _dialogueDuration;
        private float _dialogueTimer;

        protected override void Awake()
        {
            base.Awake();
            BehaviourName = "GenericDialogue";
            Priority      = BehaviourPriority.GenericDialogue;
        }

        /// <summary>Start a dialogue of the given duration. Called externally.</summary>
        public void StartDialogue(float duration)
        {
            _dialogueDuration = duration;
            _inDialogue = true;
            Enable();
            if (!Active) Activate();
        }

        protected override void OnActivated()
        {
            Movement?.StopWalk(false);
            _dialogueTimer = 0f;
            Animation?.TriggerTalk();
            OnDialogueStarted?.Invoke();
        }

        public override void BehaviourUpdate()
        {
            if (!_inDialogue) { Disable(); return; }

            _dialogueTimer += UnityEngine.Time.deltaTime;
            if (_dialogueTimer >= _dialogueDuration)
            {
                _inDialogue = false;
                OnDialogueEnded?.Invoke();
                Disable();
            }
        }
    }
}
