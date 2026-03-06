// ============================================================
//  NPCScheduleManager.cs
//  Drives the NPC's daily schedule using the GameClock.
//
//  Design:
//   - Holds a sorted list of NPCAction components
//   - Subscribes to GameClock.OnMinutePassed
//   - Checks each minute: should the next action start?
//   - Signals can be injected at any time (higher priority
//     than the current action immediately takes over)
//   - At day rollover, resets and re-sorts the action list
//
//  Action priority:
//   1. Active NPCSignals (priority-sorted)
//   2. Scheduled NPCActions (time-sorted)
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace NPCSystem
{
    [RequireComponent(typeof(NPC))]
    public class NPCScheduleManager : MonoBehaviour
    {
        // -------------------------------------------------------
        // Events
        // -------------------------------------------------------
        public event Action<NPCAction> OnActionStarted;
        public event Action<NPCAction> OnActionEnded;

        // -------------------------------------------------------
        // State
        // -------------------------------------------------------
        public NPCAction  CurrentAction  { get; private set; }
        public NPCSignal  ActiveSignal   { get; private set; }

        // -------------------------------------------------------
        // Private
        // -------------------------------------------------------
        private NPC               _npc;
        private List<NPCAction>   _scheduledActions = new List<NPCAction>();
        private List<NPCSignal>   _signals          = new List<NPCSignal>();
        private int               _currentActionIndex = -1;
        private bool              _initialized;

        // -------------------------------------------------------
        // Unity
        // -------------------------------------------------------
        private void Awake()
        {
            _npc = GetComponent<NPC>();
        }

        public void Initialize(NPC npc)
        {
            _npc = npc;

            // Collect all NPCAction (and NPCSignal) children
            var all = GetComponentsInChildren<NPCAction>(true);
            foreach (var a in all)
            {
                a.SetNPC(npc);

                if (a is NPCSignal sig)
                    _signals.Add(sig);
                else
                    _scheduledActions.Add(a);
            }

            SortActions();

            // Subscribe to clock
            if (GameClock.Instance != null)
            {
                GameClock.Instance.OnMinutePassed += OnMinutePassed;
                GameClock.Instance.OnDayStarted   += OnDayStarted;
            }

            _initialized = true;

            // Start whatever action is appropriate for current time
            JumpToCurrentTime(GameClock.Instance?.CurrentMinute ?? 0);
        }

        private void OnDestroy()
        {
            if (GameClock.Instance != null)
            {
                GameClock.Instance.OnMinutePassed -= OnMinutePassed;
                GameClock.Instance.OnDayStarted   -= OnDayStarted;
            }
        }

        private void Update()
        {
            if (!_initialized) return;

            // Tick active signal or action each frame
            if (ActiveSignal != null && ActiveSignal.IsActive)
                ActiveSignal.ActiveUpdate();
            else if (CurrentAction != null && CurrentAction.IsActive)
                CurrentAction.ActiveUpdate();
        }

        // -------------------------------------------------------
        // Clock callbacks
        // -------------------------------------------------------
        private void OnMinutePassed(int currentMinute)
        {
            // Tick current action
            if (ActiveSignal != null && ActiveSignal.IsActive)
                ActiveSignal.MinPassed();
            else if (CurrentAction != null && CurrentAction.IsActive)
                CurrentAction.MinPassed();

            // Check if current action should end
            if (CurrentAction != null && CurrentAction.IsActive)
            {
                int endTime = CurrentAction.GetEndTime();
                if (endTime > 0 && currentMinute >= endTime)
                    EndCurrentAction();
            }

            // Check if next action should start
            TryStartNextAction(currentMinute);
        }

        private void OnDayStarted(int day)
        {
            // Reset all signals for a new day
            foreach (var sig in _signals)
                sig.ResetCycle();

            // End current action
            if (CurrentAction != null && CurrentAction.IsActive)
                EndCurrentAction();

            _currentActionIndex = -1;
            JumpToCurrentTime(0);
        }

        // -------------------------------------------------------
        // Action control
        // -------------------------------------------------------
        private void TryStartNextAction(int currentMinute)
        {
            if (_scheduledActions.Count == 0) return;

            int nextIndex = _currentActionIndex + 1;
            if (nextIndex >= _scheduledActions.Count) return;

            var next = _scheduledActions[nextIndex];
            if (currentMinute < next.StartTime) return;
            if (!next.ShouldStart()) return;

            StartAction(next, nextIndex);
        }

        private void StartAction(NPCAction action, int index)
        {
            // End the current action first
            if (CurrentAction != null && CurrentAction.IsActive)
                EndCurrentAction();

            _currentActionIndex = index;
            CurrentAction       = action;

            action.Started();
            action.LateStarted();

            OnActionStarted?.Invoke(action);
        }

        private void EndCurrentAction()
        {
            if (CurrentAction == null) return;
            var a = CurrentAction;

            if (a.IsActive)
                a.End();

            CurrentAction = null;
            OnActionEnded?.Invoke(a);
        }

        // -------------------------------------------------------
        // Signal injection (external code calls this)
        // -------------------------------------------------------
        /// <summary>
        /// Inject a signal that overrides the current schedule action.
        /// Signals are priority-sorted; highest runs immediately.
        /// </summary>
        public void IssueSignal(NPCSignal signal)
        {
            if (signal == null || signal.StartedThisCycle) return;

            // Interrupt lower-priority signal or current action
            if (ActiveSignal != null)
            {
                if (signal.Priority <= ActiveSignal.Priority) return;
                ActiveSignal.Interrupt();
            }
            else if (CurrentAction != null && CurrentAction.IsActive)
            {
                if (signal.Priority <= CurrentAction.Priority) return;
                CurrentAction.Interrupt();
            }

            ActiveSignal = signal;
            signal.Started();
            signal.LateStarted();
            signal.OnEnded += OnSignalEnded;

            OnActionStarted?.Invoke(signal);
        }

        private void OnSignalEnded()
        {
            if (ActiveSignal != null)
            {
                ActiveSignal.OnEnded -= OnSignalEnded;
                OnActionEnded?.Invoke(ActiveSignal);
                ActiveSignal = null;
            }

            // Resume the scheduled action if it was interrupted
            if (CurrentAction != null && !CurrentAction.IsActive)
            {
                CurrentAction.JumpTo();
            }
        }

        // -------------------------------------------------------
        // Jump-to-current-time (loading / scene start)
        // -------------------------------------------------------
        private void JumpToCurrentTime(int currentMinute)
        {
            if (_scheduledActions.Count == 0) return;

            // Find the most recent action that should have started by now
            int bestIndex = -1;
            for (int i = 0; i < _scheduledActions.Count; i++)
            {
                if (_scheduledActions[i].StartTime <= currentMinute)
                    bestIndex = i;
            }

            if (bestIndex < 0) return;

            var action = _scheduledActions[bestIndex];
            if (!action.ShouldStart()) return;

            _currentActionIndex = bestIndex;
            CurrentAction       = action;
            action.JumpTo();
            OnActionStarted?.Invoke(action);
        }

        // -------------------------------------------------------
        // Action list management
        // -------------------------------------------------------
        private void SortActions()
        {
            _scheduledActions.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));
            _signals.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        }

        /// <summary>Add an action at runtime (e.g. quest-assigned task).</summary>
        public void AddAction(NPCAction action)
        {
            action.SetNPC(_npc);
            if (action is NPCSignal sig)
                _signals.Add(sig);
            else
                _scheduledActions.Add(action);

            SortActions();
        }

        /// <summary>Remove an action at runtime.</summary>
        public void RemoveAction(NPCAction action)
        {
            if (action == CurrentAction) EndCurrentAction();
            _scheduledActions.Remove(action);
        }

        // -------------------------------------------------------
        // Debug
        // -------------------------------------------------------
        public void LogSchedule()
        {
            Debug.Log($"=== Schedule for {_npc.name} ===");
            foreach (var a in _scheduledActions)
                Debug.Log($"  {a}");
        }
    }
}
