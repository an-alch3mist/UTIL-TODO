// ============================================================
//  GameClock.cs
//  Drives the in-game clock in minutes.
//  All time-based NPC systems (scheduling, timed actions)
//  subscribe to OnMinutePassed and OnDayStarted.
//
//  Design: Singleton MonoBehaviour. Decoupled from NPC code.
//          NPCScheduleManager subscribes to events.
// ============================================================

using System;
using UnityEngine;

namespace NPCSystem
{
    public class GameClock : MonoBehaviour
    {
        // -------------------------------------------------------
        // Singleton
        // -------------------------------------------------------
        private static GameClock _instance;
        public static GameClock Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindObjectOfType<GameClock>();
                return _instance;
            }
        }

        // -------------------------------------------------------
        // Inspector
        // -------------------------------------------------------
        [Header("Clock Settings")]
        [Tooltip("How many real-world seconds equal one in-game minute.")]
        [SerializeField] private float _secondsPerMinute = 1f;

        [Tooltip("Starting time in minutes from midnight (0–1440). 480 = 8 AM.")]
        [SerializeField] private int _startTimeMinutes = 480;

        [Header("Debug")]
        [SerializeField] private bool _debugLog = false;

        // -------------------------------------------------------
        // State
        // -------------------------------------------------------
        private float _timer;
        private bool _paused;

        // Current day minute 0–1439 (0 = midnight)
        public int CurrentMinute { get; private set; }
        public int CurrentDay    { get; private set; }

        // Human-readable helpers
        public int Hour   => CurrentMinute / 60;
        public int Minute => CurrentMinute % 60;

        // -------------------------------------------------------
        // Events
        // -------------------------------------------------------
        /// <summary>Fired every in-game minute. Arg = current minute of the day (0–1439).</summary>
        public event Action<int> OnMinutePassed;

        /// <summary>Fired when the clock rolls over midnight. Arg = new day index.</summary>
        public event Action<int> OnDayStarted;

        // -------------------------------------------------------
        // Unity
        // -------------------------------------------------------
        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            CurrentMinute = _startTimeMinutes;
            CurrentDay    = 0;
        }

        private void Update()
        {
            if (_paused) return;

            _timer += Time.deltaTime;
            if (_timer >= _secondsPerMinute)
            {
                _timer -= _secondsPerMinute;
                AdvanceMinute();
            }
        }

        // -------------------------------------------------------
        // Advance
        // -------------------------------------------------------
        private void AdvanceMinute()
        {
            CurrentMinute++;

            if (CurrentMinute >= 1440)
            {
                CurrentMinute = 0;
                CurrentDay++;
                if (_debugLog) Debug.Log($"[GameClock] Day {CurrentDay} started.");
                OnDayStarted?.Invoke(CurrentDay);
            }

            if (_debugLog && CurrentMinute % 60 == 0)
                Debug.Log($"[GameClock] {Hour:D2}:00");

            OnMinutePassed?.Invoke(CurrentMinute);
        }

        // -------------------------------------------------------
        // Control
        // -------------------------------------------------------
        public void Pause()  => _paused = true;
        public void Resume() => _paused = false;

        /// <summary>Jump the clock forward by N minutes (fires events for each).</summary>
        public void AdvanceBy(int minutes)
        {
            for (int i = 0; i < minutes; i++)
                AdvanceMinute();
        }

        /// <summary>Set the clock to a specific minute (0–1439) without firing events.</summary>
        public void SetTime(int minute) => CurrentMinute = Mathf.Clamp(minute, 0, 1439);

        // -------------------------------------------------------
        // Utility
        // -------------------------------------------------------
        /// <summary>Returns a formatted time string "HH:MM".</summary>
        public string GetTimeString() => $"{Hour:D2}:{Minute:D2}";

        /// <summary>Minutes remaining until a target minute in the day.</summary>
        public int MinutesUntil(int targetMinute)
        {
            if (targetMinute >= CurrentMinute) return targetMinute - CurrentMinute;
            return (1440 - CurrentMinute) + targetMinute;   // wraps midnight
        }
    }
}
