// ============================================================
//  CustomerNPC.cs
//  Civilian who wants to buy a product from the player.
//  Adds: RequestProductBehaviour, deal acceptance logic.
// ============================================================

using System;
using UnityEngine;

namespace NPCSystem
{
    public class CustomerNPC : NPC
    {
        // -------------------------------------------------------
        // Events
        // -------------------------------------------------------
        public event Action<CustomerNPC> OnRequestStarted;
        public event Action<CustomerNPC> OnDealAccepted;
        public event Action<CustomerNPC> OnDealRejected;
        public event Action<CustomerNPC> OnDealLeft;

        // -------------------------------------------------------
        // Inspector
        // -------------------------------------------------------
        [Header("Customer Settings")]
        [SerializeField] private float  _requestTimeout   = 120f;   // real seconds
        [SerializeField] private float  _satisfactionBonus = 0.1f;

        // -------------------------------------------------------
        // State
        // -------------------------------------------------------
        public bool  IsRequestingProduct { get; private set; }
        public float Satisfaction        { get; private set; } = 0.5f;   // 0–1
        public float RequestTimer        { get; private set; }

        // -------------------------------------------------------
        // Unity / Init
        // -------------------------------------------------------
        public override void Initialize(NPCDefinition definition)
        {
            base.Initialize(definition);

            // Enable wander for customers by default
            BehaviourManager?.EnableBehaviour<WanderBehaviour>();
        }

        private void Update()
        {
            if (!IsRequestingProduct) return;

            RequestTimer += Time.deltaTime;
            if (RequestTimer >= _requestTimeout)
                LeaveWithoutDeal();
        }

        // -------------------------------------------------------
        // Product request flow
        // -------------------------------------------------------

        /// <summary>Called by game systems when a customer is sent to the player.</summary>
        public void BeginProductRequest()
        {
            if (IsRequestingProduct || !IsAlive) return;

            IsRequestingProduct = true;
            RequestTimer = 0f;

            // Stop wandering — wait for player
            BehaviourManager?.DisableBehaviour<WanderBehaviour>();
            BehaviourManager?.EnableBehaviour<StationaryBehaviour>();

            OnRequestStarted?.Invoke(this);
        }

        /// <summary>Called when the player offers this customer a product.</summary>
        public bool EvaluateDeal(float offeredQuality, float offeredPrice, float expectedPrice)
        {
            if (!IsRequestingProduct) return false;

            float qualityScore = Mathf.Clamp01(offeredQuality);
            float priceScore   = offeredPrice <= expectedPrice * 1.2f ? 1f :
                                 1f - Mathf.Clamp01((offeredPrice - expectedPrice) / expectedPrice);

            float acceptChance = (qualityScore * 0.5f + priceScore * 0.5f) * Satisfaction;

            bool accepted = UnityEngine.Random.value < acceptChance;

            if (accepted)
            {
                Satisfaction = Mathf.Clamp01(Satisfaction + _satisfactionBonus);
                CompleteDeal();
            }
            else
            {
                Satisfaction = Mathf.Clamp01(Satisfaction - _satisfactionBonus * 0.5f);
                OnDealRejected?.Invoke(this);
            }

            return accepted;
        }

        private void CompleteDeal()
        {
            IsRequestingProduct = false;
            OnDealAccepted?.Invoke(this);
            // Walk away after deal
            BehaviourManager?.EnableBehaviour<WanderBehaviour>();
            BehaviourManager?.DisableBehaviour<StationaryBehaviour>();
        }

        public void LeaveWithoutDeal()
        {
            IsRequestingProduct = false;
            Satisfaction = Mathf.Clamp01(Satisfaction - _satisfactionBonus);
            OnDealLeft?.Invoke(this);

            BehaviourManager?.EnableBehaviour<WanderBehaviour>();
            BehaviourManager?.DisableBehaviour<StationaryBehaviour>();
        }
    }
}


// ============================================================
//  PoliceNPC.cs
//  Extends NPC with law enforcement behaviours:
//    - Responds to crime reports
//    - Issues wanted-level escalation
//    - Can arrest the player
// ============================================================
namespace NPCSystem
{
    public class PoliceNPC : NPC
    {
        // -------------------------------------------------------
        // Events
        // -------------------------------------------------------
        public System.Action<PoliceNPC, GameObject> OnArrestAttempt;
        public System.Action<PoliceNPC>             OnPatrolStarted;

        // -------------------------------------------------------
        // Inspector
        // -------------------------------------------------------
        [Header("Police Settings")]
        [SerializeField] private float _arrestRange     = 1.5f;
        [SerializeField] private float _arrestTime      = 3f;    // seconds to complete arrest
        [SerializeField] private int   _wantedEscalation = 1;

        // -------------------------------------------------------
        // State
        // -------------------------------------------------------
        public bool IsOnDuty    { get; private set; } = true;
        public bool IsArrestin  { get; private set; }

        private float _arrestTimer;
        private UnityEngine.GameObject _arrestTarget;

        // -------------------------------------------------------
        // Init
        // -------------------------------------------------------
        public override void Initialize(NPCDefinition definition)
        {
            base.Initialize(definition);

            // Police always have combat and patrol enabled
            BehaviourManager?.EnableBehaviour<PatrolBehaviour>();
            BehaviourManager?.EnableBehaviour<CombatBehaviour>();
        }

        private void Update()
        {
            if (!IsArrestin || _arrestTarget == null) return;

            float dist = UnityEngine.Vector3.Distance(
                transform.position, _arrestTarget.transform.position);

            if (dist > _arrestRange) { CancelArrest(); return; }

            _arrestTimer += Time.deltaTime;
            if (_arrestTimer >= _arrestTime)
                CompleteArrest();
        }

        // -------------------------------------------------------
        // Crime response
        // -------------------------------------------------------

        /// <summary>Respond to a crime report — pursue the target.</summary>
        public void RespondToCrime(UnityEngine.GameObject criminal)
        {
            if (!IsOnDuty || !IsAlive) return;
            SetThreat(criminal);
            BehaviourManager?.GetBehaviour<CombatBehaviour>()?.Enable();
        }

        // -------------------------------------------------------
        // Arrest
        // -------------------------------------------------------
        public void AttemptArrest(UnityEngine.GameObject target)
        {
            if (IsArrestin || !IsAlive) return;
            IsArrestin   = true;
            _arrestTarget = target;
            _arrestTimer  = 0f;
            Movement?.WalkTo(target.transform.position, null, run: false);
            OnArrestAttempt?.Invoke(this, target);
        }

        private void CompleteArrest()
        {
            IsArrestin = false;
            // Signal game systems that player is arrested
            UnityEngine.Debug.Log($"[Police:{name}] Arrest completed on {_arrestTarget?.name}");
            _arrestTarget = null;
        }

        private void CancelArrest()
        {
            IsArrestin    = false;
            _arrestTarget = null;
        }
    }
}


// ============================================================
//  EmployeeNPC.cs
//  NPC assigned to work at a station or property.
//  Drives work tasks via schedule and responds to management.
// ============================================================
namespace NPCSystem
{
    public class EmployeeNPC : NPC
    {
        // -------------------------------------------------------
        // Events
        // -------------------------------------------------------
        public System.Action<EmployeeNPC, string> OnTaskAssigned;
        public System.Action<EmployeeNPC>         OnTaskCompleted;

        // -------------------------------------------------------
        // Inspector
        // -------------------------------------------------------
        [Header("Employee Settings")]
        [SerializeField] private float _workEfficiency = 1f;   // multiplier on work speed

        // -------------------------------------------------------
        // State
        // -------------------------------------------------------
        public string CurrentTaskName   { get; private set; } = "None";
        public bool   IsWorking         { get; private set; }
        public float  WorkEfficiency    => _workEfficiency;

        // The station/location this employee is assigned to
        private UnityEngine.Transform _assignedStation;

        // -------------------------------------------------------
        // Init
        // -------------------------------------------------------
        public override void Initialize(NPCDefinition definition)
        {
            base.Initialize(definition);
            // Employees start with stationary behaviour enabled
            BehaviourManager?.EnableBehaviour<StationaryBehaviour>();
        }

        // -------------------------------------------------------
        // Task management
        // -------------------------------------------------------

        /// <summary>
        /// Assign the employee to a station. They will walk there and begin working.
        /// </summary>
        public void AssignToStation(UnityEngine.Transform station, string taskName)
        {
            _assignedStation = station;
            CurrentTaskName  = taskName;
            IsWorking        = false;

            var stationary = BehaviourManager?.GetBehaviour<StationaryBehaviour>();
            if (stationary != null)
            {
                stationary.StationPoint = station;
                stationary.Enable();
            }

            OnTaskAssigned?.Invoke(this, taskName);
        }

        /// <summary>Stop working and go idle.</summary>
        public void Unassign()
        {
            _assignedStation = null;
            CurrentTaskName  = "None";
            IsWorking        = false;

            BehaviourManager?.DisableBehaviour<StationaryBehaviour>();
            BehaviourManager?.EnableBehaviour<WanderBehaviour>();
        }

        public void NotifyTaskComplete()
        {
            IsWorking = false;
            OnTaskCompleted?.Invoke(this);
        }
    }
}
