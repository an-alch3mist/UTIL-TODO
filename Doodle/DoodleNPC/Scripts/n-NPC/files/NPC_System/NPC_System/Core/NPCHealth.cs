// ============================================================
//  NPCHealth.cs
//  Handles HP, knockouts, death, and damage routing.
//
//  Design:
//   - Pure data + events, no behaviour logic here
//   - Other systems (NPCBehaviour, NPCAnimation) subscribe
//     to events rather than being called directly
//   - Supports lethal damage → knockout (via KnockoutChance)
//   - Supports timed revival from unconscious state
// ============================================================

using System;
using UnityEngine;

namespace NPCSystem
{
    [RequireComponent(typeof(NPC))]
    public class NPCHealth : MonoBehaviour
    {
        // -------------------------------------------------------
        // Events  (Observer pattern via C# Action delegates)
        // -------------------------------------------------------
        public event Action<float, float>       OnHealthChanged;   // (current, max)
        public event Action<float, GameObject>  OnDamaged;         // (amount, source)
        public event Action<EKnockdownCause>    OnKnockedOut;
        public event Action                     OnRevived;
        public event Action<EKnockdownCause>    OnDied;

        // -------------------------------------------------------
        // State
        // -------------------------------------------------------
        public float       MaxHealth    { get; private set; }
        public float       CurrentHealth{ get; private set; }
        public EHealthState State       { get; private set; } = EHealthState.Alive;

        public bool IsAlive       => State == EHealthState.Alive;
        public bool IsUnconscious => State == EHealthState.Unconscious;
        public bool IsDead        => State == EHealthState.Dead;
        public bool IsVulnerable  => IsAlive || IsUnconscious;

        // -------------------------------------------------------
        // Private
        // -------------------------------------------------------
        private NPC            _npc;
        private NPCDefinition  _def;
        private float          _reviveTimer;
        private bool           _reviveTimerActive;

        // -------------------------------------------------------
        // Unity
        // -------------------------------------------------------
        private void Awake()
        {
            _npc = GetComponent<NPC>();
        }

        /// <summary>Called by NPC.Initialize() after definition is assigned.</summary>
        public void Initialize(NPCDefinition def)
        {
            _def       = def;
            MaxHealth  = def.MaxHealth;
            CurrentHealth = MaxHealth;
            State      = EHealthState.Alive;
        }

        private void Update()
        {
            if (_reviveTimerActive)
            {
                _reviveTimer -= Time.deltaTime;
                if (_reviveTimer <= 0f)
                {
                    _reviveTimerActive = false;
                    Revive();
                }
            }
        }

        // -------------------------------------------------------
        // Public API
        // -------------------------------------------------------

        /// <summary>Apply damage to this NPC. Source can be null.</summary>
        public void TakeDamage(float amount, GameObject source = null,
                               EKnockdownCause cause = EKnockdownCause.Melee)
        {
            if (!IsVulnerable || amount <= 0f) return;

            float clamped = Mathf.Min(amount, CurrentHealth);
            CurrentHealth -= clamped;

            OnDamaged?.Invoke(clamped, source);
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);

            if (CurrentHealth <= 0f)
                HandleLethalDamage(cause);
        }

        /// <summary>Heal the NPC. Clamps to MaxHealth.</summary>
        public void Heal(float amount)
        {
            if (!IsAlive || amount <= 0f) return;

            CurrentHealth = Mathf.Min(CurrentHealth + amount, MaxHealth);
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        /// <summary>Immediately knock out the NPC without killing.</summary>
        public void KnockOut(EKnockdownCause cause = EKnockdownCause.Melee)
        {
            if (!IsAlive) return;
            SetState(EHealthState.Unconscious);
            CurrentHealth = Mathf.Max(1f, CurrentHealth);   // leave 1 HP
            OnKnockedOut?.Invoke(cause);

            // Start revival timer if configured
            if (_def != null && _def.UnconsciousDuration > 0f)
            {
                _reviveTimer       = _def.UnconsciousDuration;
                _reviveTimerActive = true;
            }
        }

        /// <summary>Revive from unconscious state.</summary>
        public void Revive()
        {
            if (!IsUnconscious) return;
            SetState(EHealthState.Alive);
            CurrentHealth = MaxHealth * 0.25f;   // revive at 25% HP
            _reviveTimerActive = false;
            OnRevived?.Invoke();
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        /// <summary>Instantly kill the NPC.</summary>
        public void Kill(EKnockdownCause cause = EKnockdownCause.Melee)
        {
            if (IsDead) return;
            CurrentHealth = 0f;
            _reviveTimerActive = false;
            SetState(EHealthState.Dead);
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
            OnDied?.Invoke(cause);
        }

        // -------------------------------------------------------
        // Private
        // -------------------------------------------------------
        private void HandleLethalDamage(EKnockdownCause cause)
        {
            bool knockoutInstead = _def != null &&
                                   UnityEngine.Random.value < _def.KnockoutChance;

            if (knockoutInstead)
                KnockOut(cause);
            else
                Kill(cause);
        }

        private void SetState(EHealthState newState)
        {
            State = newState;
        }

        // -------------------------------------------------------
        // Normalized HP helper for UI
        // -------------------------------------------------------
        public float HealthPercent => MaxHealth > 0f ? CurrentHealth / MaxHealth : 0f;
    }
}
