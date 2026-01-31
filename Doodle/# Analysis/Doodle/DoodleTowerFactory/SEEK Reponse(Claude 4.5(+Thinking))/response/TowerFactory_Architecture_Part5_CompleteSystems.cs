/*
 * ============================================================================
 * TOWERFACTORY - COMPLETE ARCHITECTURAL SKELETON
 * Part 5: FSM, Save System, Tutorial, Animation, Audio, Camera, Full UI, Time, Utilities
 * ============================================================================
 * 
 * This part completes 100% coverage with ALL remaining systems:
 * - FSM System (Finite State Machine)
 * - Complete Save/Load System  
 * - All Tutorial Quest Types
 * - All Animation Components
 * - Complete Audio System
 * - Complete Camera System
 * - All UI Systems (Store, Settings, End Game, Cycle UI, etc.)
 * - Time Management (detailed)
 * - All Spawner Position Types
 * - All Controller/Input Modes
 * - Character System
 * - Team System
 * - Utility Classes
 * - Environment Systems
 * - VFX Systems
 * - Debug Systems
 * 
 * ============================================================================
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SPACE_UTIL;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace LightTower
{
    #region ═══════════════════════════════════════════════════════════════
    #region FSM SYSTEM - Finite State Machine
    #endregion
    #region ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// FSM Component - Finite State Machine controller
    /// </summary>
    public class FSMComponent : MonoBehaviour
    {
        #region Inspector Fields
        [SerializeField] private FSMState initialState;
        [SerializeField] private List<FSMState> states = new List<FSMState>();
        #endregion

        #region Private Fields
        private FSMState currentState;
        private Dictionary<string, FSMState> stateDict = new Dictionary<string, FSMState>();
        #endregion

        #region Properties
        public FSMState CurrentState => currentState;
        #endregion

        #region Events
        public event Action<FSMState, FSMState> OnStateChanged;
        #endregion

        private void Awake()
        {
            /* TODO:
             * Build state dictionary
             */
            foreach (var state in states)
            {
                if (state != null)
                    stateDict[state.stateName] = state;
            }
        }

        private void Start()
        {
            /* TODO:
             * Enter initial state
             */
            if (initialState != null)
                ChangeState(initialState);
        }

        private void Update()
        {
            /* TODO:
             * 1. Execute current state's tasks
             * 2. Check transition conditions
             * 3. Change state if condition met
             */
            if (currentState != null)
            {
                currentState.Execute(this);
                CheckTransitions();
            }
        }

        public void ChangeState(FSMState newState)
        {
            /* TODO:
             * 1. Exit current state
             * 2. Set new state
             * 3. Enter new state
             * 4. Fire OnStateChanged
             */
        }

        public void ChangeState(string stateName)
        {
            /* TODO:
             * Find state by name and change to it
             */
            if (stateDict.ContainsKey(stateName))
                ChangeState(stateDict[stateName]);
        }

        private void CheckTransitions()
        {
            /* TODO:
             * For each transition in current state:
             * - Check if condition is met
             * - If yes, change to target state
             */
        }
    }

    /// <summary>
    /// FSM State - represents one state in FSM
    /// </summary>
    [CreateAssetMenu(fileName = "NewFSMState", menuName = "TowerFactory/FSM/State")]
    public class FSMState : ScriptableObject
    {
        public string stateName;
        public List<FSMTask> onEnterTasks = new List<FSMTask>();
        public List<FSMTask> onUpdateTasks = new List<FSMTask>();
        public List<FSMTask> onExitTasks = new List<FSMTask>();
        public List<FSMTransition> transitions = new List<FSMTransition>();

        public void Enter(FSMComponent fsm)
        {
            /* TODO: Execute all onEnterTasks */
            foreach (var task in onEnterTasks)
                task?.Execute(fsm);
        }

        public void Execute(FSMComponent fsm)
        {
            /* TODO: Execute all onUpdateTasks */
            foreach (var task in onUpdateTasks)
                task?.Execute(fsm);
        }

        public void Exit(FSMComponent fsm)
        {
            /* TODO: Execute all onExitTasks */
            foreach (var task in onExitTasks)
                task?.Execute(fsm);
        }
    }

    /// <summary>
    /// FSM Transition - condition to move between states
    /// </summary>
    [System.Serializable]
    public class FSMTransition
    {
        public FSMCondition condition;
        public FSMState targetState;

        public bool CheckCondition(FSMComponent fsm)
        {
            return condition != null && condition.Evaluate(fsm);
        }
    }

    /// <summary>
    /// FSM Task - action to execute
    /// </summary>
    public abstract class FSMTask : ScriptableObject
    {
        public abstract void Execute(FSMComponent fsm);
    }

    /// <summary>
    /// FSM Condition - boolean check for transitions
    /// </summary>
    public abstract class FSMCondition : ScriptableObject
    {
        public abstract bool Evaluate(FSMComponent fsm);
    }

    #endregion

    #region ═══════════════════════════════════════════════════════════════
    #region SAVE SYSTEM - Complete Save/Load
    #endregion
    #region ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Save system - handles game save/load
    /// </summary>
    public class SaveSystem : MonoBehaviour
    {
        #region Singleton
        public static SaveSystem Instance { get; private set; }
        #endregion

        #region Inspector Fields
        [SerializeField] private string saveFileName = "savegame.json";
        [SerializeField] private bool useEncryption = false;
        #endregion

        #region Private Fields
        private string SavePath => Path.Combine(Application.persistentDataPath, saveFileName);
        #endregion

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void SaveGame()
        {
            /* TODO:
             * 1. Create GameSaveData object
             * 2. Collect data from all SaveComponent objects
             * 3. Serialize to JSON
             * 4. Write to file
             * 5. Fire save complete event
             */
        }

        public void LoadGame()
        {
            /* TODO:
             * 1. Read file
             * 2. Deserialize JSON
             * 3. Apply data to all SaveComponent objects
             * 4. Fire load complete event
             */
        }

        public bool SaveExists()
        {
            return File.Exists(SavePath);
        }

        public void DeleteSave()
        {
            /* TODO: Delete save file */
            if (File.Exists(SavePath))
                File.Delete(SavePath);
        }
    }

    /// <summary>
    /// SaveComponent - makes GameObject savable
    /// </summary>
    public class SaveComponent : MonoBehaviour
    {
        [SerializeField] private string uniqueID;  // Must be unique per object
        
        public string UniqueID => uniqueID;

        private void OnValidate()
        {
            /* TODO:
             * Auto-generate uniqueID if empty
             */
            if (string.IsNullOrEmpty(uniqueID))
                uniqueID = System.Guid.NewGuid().ToString();
        }

        public virtual object GetSaveData()
        {
            /* TODO:
             * Collect savable data from this object
             * - Position, rotation, scale
             * - Component-specific data
             * Return as serializable object
             */
            return null;
        }

        public virtual void LoadSaveData(object data)
        {
            /* TODO:
             * Apply loaded data to this object
             */
        }
    }

    /// <summary>
    /// Savable attribute - marks fields for auto-save
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property)]
    public class SavableAttribute : System.Attribute
    {
        public string customKey;

        public SavableAttribute(string key = null)
        {
            customKey = key;
        }
    }

    /// <summary>
    /// Storable interface - for objects that can be stored
    /// </summary>
    public interface Storable
    {
        StorableData GetStorableData();
        void SetStorableData(StorableData data);
    }

    /// <summary>
    /// Storable data - base class for storable data
    /// </summary>
    [System.Serializable]
    public class StorableData
    {
        public string objectType;
        public string uniqueID;
        public Vector3 position;
        public Quaternion rotation;
    }

    /// <summary>
    /// Vector3 serialization surrogate for binary formatter
    /// </summary>
    public class Vector3SerializationSurrogate : ISerializationSurrogate
    {
        public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
        {
            Vector3 v = (Vector3)obj;
            info.AddValue("x", v.x);
            info.AddValue("y", v.y);
            info.AddValue("z", v.z);
        }

        public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
        {
            Vector3 v = (Vector3)obj;
            v.x = (float)info.GetValue("x", typeof(float));
            v.y = (float)info.GetValue("y", typeof(float));
            v.z = (float)info.GetValue("z", typeof(float));
            return v;
        }
    }

    /// <summary>
    /// Quaternion serialization surrogate
    /// </summary>
    public class QuaternionSerializationSurrogate : ISerializationSurrogate
    {
        public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
        {
            Quaternion q = (Quaternion)obj;
            info.AddValue("x", q.x);
            info.AddValue("y", q.y);
            info.AddValue("z", q.z);
            info.AddValue("w", q.w);
        }

        public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
        {
            Quaternion q = (Quaternion)obj;
            q.x = (float)info.GetValue("x", typeof(float));
            q.y = (float)info.GetValue("y", typeof(float));
            q.z = (float)info.GetValue("z", typeof(float));
            q.w = (float)info.GetValue("w", typeof(float));
            return q;
        }
    }

    /// <summary>
    /// Storage for resource data
    /// </summary>
    [System.Serializable]
    public class Storage_ResourceData : StorableData
    {
        public ResourceType resourceType;
        public int amount;
    }

    #endregion

    #region ═══════════════════════════════════════════════════════════════
    #region TUTORIAL QUEST TYPES - All Variants
    #endregion
    #region ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Base tutorial quest data
    /// </summary>
    [CreateAssetMenu(fileName = "NewTutorialQuest", menuName = "TowerFactory/Tutorial/Base Quest")]
    public class TutorialQuestData : ScriptableObject
    {
        public string questTitle;
        [TextArea] public string questDescription;
        public Sprite questIcon;
        
        public virtual bool IsCompleted()
        {
            return false;
        }

        public virtual void Initialize()
        {
            /* TODO: Subscribe to relevant events */
        }

        public virtual void Cleanup()
        {
            /* TODO: Unsubscribe from events */
        }
    }

    /// <summary>
    /// Tutorial quest - build specific building
    /// </summary>
    [CreateAssetMenu(fileName = "NewBuildQuest", menuName = "TowerFactory/Tutorial/Build Quest")]
    public class TutorialQuestData_build : TutorialQuestData
    {
        public BuildingData targetBuilding;
        public int requiredCount = 1;
        
        private int currentCount = 0;

        public override void Initialize()
        {
            base.Initialize();
            GameEvents.OnBuildingPlaced += OnBuildingPlaced;
        }

        public override void Cleanup()
        {
            base.Cleanup();
            GameEvents.OnBuildingPlaced -= OnBuildingPlaced;
        }

        private void OnBuildingPlaced(PlacementComponent building)
        {
            /* TODO:
             * If building.Data == targetBuilding:
             * - Increment currentCount
             * - Check if completed
             */
        }

        public override bool IsCompleted()
        {
            return currentCount >= requiredCount;
        }
    }

    /// <summary>
    /// Tutorial quest - build extractor on resource node
    /// </summary>
    [CreateAssetMenu(fileName = "NewBuildExtractorQuest", menuName = "TowerFactory/Tutorial/Build Extractor Quest")]
    public class TutorialQuestData_buildExtractor : TutorialQuestData
    {
        public ResourceType requiredResourceType;
        
        private bool completed = false;

        public override void Initialize()
        {
            GameEvents.OnBuildingPlaced += OnBuildingPlaced;
        }

        private void OnBuildingPlaced(PlacementComponent building)
        {
            /* TODO:
             * If building is Extractor and on correct resource type:
             * - Set completed = true
             */
        }

        public override bool IsCompleted() => completed;
    }

    /// <summary>
    /// Tutorial quest - collect resources
    /// </summary>
    [CreateAssetMenu(fileName = "NewResourcesQuest", menuName = "TowerFactory/Tutorial/Resources Quest")]
    public class TutorialQuestData_resources : TutorialQuestData
    {
        public ResourceType resourceType;
        public int requiredAmount;
        
        public override bool IsCompleted()
        {
            /* TODO:
             * Check if PlayerData has >= requiredAmount of resourceType
             */
            return PlayerData.Instance.GetResourceAmount(resourceType) >= requiredAmount;
        }
    }

    /// <summary>
    /// Tutorial quest - store resources
    /// </summary>
    [CreateAssetMenu(fileName = "NewStoreResourcesQuest", menuName = "TowerFactory/Tutorial/Store Resources Quest")]
    public class TutorialQuestData_storeResources : TutorialQuestData
    {
        public ResourceType resourceType;
        public int requiredAmount;
        
        private int storedAmount = 0;

        public override void Initialize()
        {
            GameEvents.OnResourceStored += OnResourceStored;
        }

        private void OnResourceStored(ResourceType type, int amount)
        {
            /* TODO:
             * If type == resourceType:
             * - Add to storedAmount
             */
        }

        public override bool IsCompleted() => storedAmount >= requiredAmount;
    }

    /// <summary>
    /// Tutorial quest - pause game
    /// </summary>
    [CreateAssetMenu(fileName = "NewPauseQuest", menuName = "TowerFactory/Tutorial/Pause Quest")]
    public class TutorialQuestData_pause : TutorialQuestData
    {
        private bool paused = false;

        public override void Initialize()
        {
            GameEvents.OnGamePaused += OnGamePaused;
        }

        private void OnGamePaused()
        {
            paused = true;
        }

        public override bool IsCompleted() => paused;
    }

    /// <summary>
    /// Tutorial quest - move camera
    /// </summary>
    [CreateAssetMenu(fileName = "NewMoveCameraQuest", menuName = "TowerFactory/Tutorial/Move Camera Quest")]
    public class TutorialQuestData_moveCamera : TutorialQuestData
    {
        public float requiredDistance = 10f;
        
        private float distanceMoved = 0f;
        private Vector3 lastCameraPosition;

        public override void Initialize()
        {
            lastCameraPosition = Camera.main.transform.position;
        }

        public void Update()
        {
            /* TODO:
             * Track camera movement
             * Add to distanceMoved
             */
        }

        public override bool IsCompleted() => distanceMoved >= requiredDistance;
    }

    /// <summary>
    /// Tutorial quest - reveal position (fog of war)
    /// </summary>
    [CreateAssetMenu(fileName = "NewPositionVisibleQuest", menuName = "TowerFactory/Tutorial/Position Visible Quest")]
    public class TutorialQuestData_positionVisible : TutorialQuestData
    {
        public v2 targetPosition;
        public int requiredRadius = 5;
        
        public override bool IsCompleted()
        {
            /* TODO:
             * Check if targetPosition is visible in fog of war
             */
            return false;
        }
    }

    /// <summary>
    /// Quest position marker - visual indicator for quest objectives
    /// </summary>
    public class QuestPositionMarker : MonoBehaviour
    {
        [SerializeField] private v2 targetGridPosition;
        [SerializeField] private GameObject markerVisual;
        [SerializeField] private bool showWhenVisible = false;

        public void SetTargetPosition(v2 gridPos)
        {
            /* TODO:
             * Set targetGridPosition
             * Update marker world position
             */
        }

        private void Update()
        {
            /* TODO:
             * Update marker visibility based on fog of war
             * Animate marker (pulse, rotate, etc.)
             */
        }
    }

    #endregion

    #region ═══════════════════════════════════════════════════════════════
    #region ANIMATION COMPONENTS - All Types
    #endregion
    #region ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Generic animation component
    /// </summary>
    public class AnimationComponent : MonoBehaviour
    {
        #region Inspector Fields
        [SerializeField] protected Animator animator;
        [SerializeField] protected string idleAnimName = "Idle";
        [SerializeField] protected string attackAnimName = "Attack";
        [SerializeField] protected string dieAnimName = "Die";
        #endregion

        protected virtual void Awake()
        {
            if (animator == null)
                animator = GetComponent<Animator>();
        }

        public virtual void PlayIdle()
        {
            animator?.Play(idleAnimName);
        }

        public virtual void PlayAttack()
        {
            animator?.Play(attackAnimName);
        }

        public virtual void PlayDie()
        {
            animator?.Play(dieAnimName);
        }

        public virtual void SetAnimationSpeed(float speed)
        {
            if (animator != null)
                animator.speed = speed;
        }
    }

    /// <summary>
    /// Tower-specific animation component
    /// </summary>
    public class TowerAnimationComponent : AnimationComponent
    {
        [SerializeField] private string fireAnimName = "Fire";
        [SerializeField] private string reloadAnimName = "Reload";
        
        private Tower tower;

        protected override void Awake()
        {
            base.Awake();
            tower = GetComponent<Tower>();
        }

        public void PlayFire()
        {
            animator?.SetTrigger("Fire");
        }

        public void PlayReload()
        {
            animator?.Play(reloadAnimName);
        }
    }

    /// <summary>
    /// Enemy animation component
    /// </summary>
    public class EnemyAnimationComponent : AnimationComponent
    {
        [SerializeField] private string walkAnimName = "Walk";
        [SerializeField] private string hurtAnimName = "Hurt";
        
        private Enemy enemy;

        protected override void Awake()
        {
            base.Awake();
            enemy = GetComponent<Enemy>();
        }

        public void PlayWalk()
        {
            animator?.Play(walkAnimName);
        }

        public void PlayHurt()
        {
            animator?.SetTrigger("Hurt");
        }

        private void Update()
        {
            /* TODO:
             * Set walk speed based on enemy move speed
             */
            if (enemy != null && animator != null)
            {
                animator.SetFloat("MoveSpeed", enemy.MoveSpeed);
            }
        }
    }

    /// <summary>
    /// Combat animation component - for combat effects
    /// </summary>
    public class CombatAnimationComponent : AnimationComponent
    {
        public void PlayHitEffect(Vector3 position)
        {
            /* TODO: Play hit particle effect */
        }

        public void PlayMuzzleFlash(Transform firePoint)
        {
            /* TODO: Play muzzle flash effect */
        }
    }

    /// <summary>
    /// Processor audio component
    /// </summary>
    public class ProcessorAudioComponent : MonoBehaviour
    {
        [SerializeField] private AudioData craftingSound;
        [SerializeField] private AudioData completeSound;
        [SerializeField] private AudioSource audioSource;
        
        private Processor processor;

        private void Awake()
        {
            processor = GetComponent<Processor>();
        }

        public void PlayCraftingSound()
        {
            /* TODO: Play looping crafting sound */
        }

        public void StopCraftingSound()
        {
            /* TODO: Stop crafting sound */
        }

        public void PlayCompleteSound()
        {
            /* TODO: Play one-shot complete sound */
        }
    }

    /// <summary>
    /// Extractor audio component
    /// </summary>
    public class ExtractorAudioComponent : MonoBehaviour
    {
        [SerializeField] private AudioData extractionSound;
        [SerializeField] private AudioSource audioSource;
        
        private Extractor extractor;

        public void PlayExtractionSound()
        {
            /* TODO: Play extraction sound */
        }
    }

    #endregion

    #region ═══════════════════════════════════════════════════════════════
    #region AUDIO SYSTEM - Complete
    #endregion
    #region ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Complete audio data with variations
    /// </summary>
    [CreateAssetMenu(fileName = "NewAudioData", menuName = "TowerFactory/Audio/Audio Data")]
    public class AudioData : ScriptableObject
    {
        public AudioClip[] clips;
        
        [Range(0f, 1f)]
        public float volume = 1f;
        
        [Range(0.1f, 3f)]
        public float pitch = 1f;
        
        public Vector2 pitchRandomRange = new Vector2(0.95f, 1.05f);
        
        public bool loop = false;
        public bool is3D = true;
        public float spatialBlend = 1f;
        public float minDistance = 1f;
        public float maxDistance = 500f;

        public AudioClip GetRandomClip()
        {
            if (clips == null || clips.Length == 0) return null;
            return clips[UnityEngine.Random.Range(0, clips.Length)];
        }

        public float GetRandomPitch()
        {
            return UnityEngine.Random.Range(pitchRandomRange.x, pitchRandomRange.y);
        }
    }

    /// <summary>
    /// Complete audio system
    /// </summary>
    public class AudioSystem : MonoBehaviour
    {
        #region Singleton
        public static AudioSystem Instance { get; private set; }
        #endregion

        #region Inspector Fields
        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource ambienceSource;
        [SerializeField] private AudioSource uiSource;
        
        [Header("Audio Source Pool")]
        [SerializeField] private int poolSize = 20;
        [SerializeField] private GameObject audioSourcePrefab;
        #endregion

        #region Private Fields
        private Queue<AudioSource> audioSourcePool = new Queue<AudioSource>();
        private List<AudioSource> activeAudioSources = new List<AudioSource>();
        private Transform audioSourceParent;
        #endregion

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            
            audioSourceParent = new GameObject("AudioSources").transform;
            audioSourceParent.SetParent(transform);
        }

        public void Initialize()
        {
            /* TODO:
             * Create audio source pool
             */
            for (int i = 0; i < poolSize; i++)
            {
                CreateAudioSource();
            }
        }

        private AudioSource CreateAudioSource()
        {
            /* TODO:
             * Instantiate audio source
             * Add to pool
             */
            return null;
        }

        public void PlaySFX(AudioData data, Vector3 position)
        {
            /* TODO:
             * 1. Get audio source from pool
             * 2. Configure with AudioData settings
             * 3. Position at world position
             * 4. Play clip
             * 5. Return to pool when finished
             */
        }

        public void PlaySFX(AudioClip clip, Vector3 position, float volume = 1f)
        {
            /* TODO: Quick play without AudioData */
        }

        public void PlayMusic(AudioClip clip, bool loop = true, float fadeTime = 1f)
        {
            /* TODO:
             * Fade out current music
             * Play new music
             * Fade in
             */
        }

        public void PlayAmbience(AudioClip clip, bool loop = true)
        {
            /* TODO: Play ambience on dedicated source */
        }

        public void PlayUI(AudioClip clip)
        {
            /* TODO: Play on UI source (no 3D positioning) */
        }

        public void SetMasterVolume(float volume)
        {
            /* TODO: Set global volume */
            AudioListener.volume = volume;
        }

        public void SetMusicVolume(float volume)
        {
            musicSource.volume = volume;
        }

        public void SetSFXVolume(float volume)
        {
            /* TODO: Store and apply to future SFX */
        }

        private IEnumerator FadeMusicCoroutine(float targetVolume, float duration)
        {
            /* TODO: Smooth volume fade */
            yield return null;
        }

        private void ReturnAudioSource(AudioSource source)
        {
            /* TODO:
             * Stop source
             * Reset settings
             * Return to pool
             */
        }
    }

    /// <summary>
    /// Ambience manager - handles background sounds
    /// </summary>
    public class AmbienceManager : MonoBehaviour
    {
        [SerializeField] private AudioClip[] ambienceClips;
        [SerializeField] private float crossfadeTime = 2f;
        [SerializeField] private float clipChangeInterval = 60f;
        
        private float timer;
        private int currentClipIndex = 0;

        private void Start()
        {
            /* TODO: Play first ambience clip */
            PlayNextAmbience();
        }

        private void Update()
        {
            timer += Time.deltaTime;
            if (timer >= clipChangeInterval)
            {
                timer = 0f;
                PlayNextAmbience();
            }
        }

        private void PlayNextAmbience()
        {
            /* TODO:
             * Increment clip index
             * Crossfade to next clip
             */
        }
    }

    /// <summary>
    /// Simple sound from animation event
    /// </summary>
    public class SimpleSoundAnimationEvent : MonoBehaviour
    {
        public void PlaySound(AudioClip clip)
        {
            /* TODO: Play sound at this position */
            AudioSystem.Instance.PlaySFX(clip, transform.position);
        }
    }

    #endregion

    #region ═══════════════════════════════════════════════════════════════
    #region CAMERA SYSTEM - Complete
    #endregion
    #region ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Player camera - isometric camera controller
    /// </summary>
    public class PlayerCamera : MonoBehaviour
    {
        #region Inspector Fields
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 10f;
        [SerializeField] private float fastMoveMultiplier = 2f;
        [SerializeField] private float edgeScrollSpeed = 15f;
        [SerializeField] private float edgeScrollBorder = 10f;
        [SerializeField] private bool enableEdgeScrolling = true;
        
        [Header("Rotation")]
        [SerializeField] private float rotationSpeed = 90f;
        [SerializeField] private bool snapRotation = true;
        [SerializeField] private float[] snapAngles = { 0f, 90f, 180f, 270f };
        
        [Header("Zoom")]
        [SerializeField] private float zoomSpeed = 5f;
        [SerializeField] private float minZoom = 5f;
        [SerializeField] private float maxZoom = 30f;
        [SerializeField] private float currentZoom = 15f;
        
        [Header("Bounds")]
        [SerializeField] private bool limitCameraBounds = true;
        [SerializeField] private Bounds cameraBounds;
        
        [Header("Input")]
        [SerializeField] private KeyCode fastMoveKey = KeyCode.LeftShift;
        [SerializeField] private KeyCode rotateLeftKey = KeyCode.Q;
        [SerializeField] private KeyCode rotateRightKey = KeyCode.E;
        [SerializeField] private KeyCode resetCameraKey = KeyCode.Home;
        #endregion

        #region Private Fields
        private Camera cam;
        private Vector3 targetPosition;
        private float targetRotation;
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        #endregion

        private void Awake()
        {
            cam = GetComponent<Camera>();
            initialPosition = transform.position;
            initialRotation = transform.rotation;
            targetPosition = transform.position;
            targetRotation = transform.eulerAngles.y;
        }

        private void Update()
        {
            HandleMovement();
            HandleRotation();
            HandleZoom();
            HandleReset();
            
            ApplyMovement();
        }

        private void HandleMovement()
        {
            /* TODO:
             * 1. Get WASD input
             * 2. Check edge scrolling if enabled
             * 3. Calculate movement vector
             * 4. Apply speed modifiers (shift for fast)
             * 5. Update targetPosition
             * 6. Clamp to bounds if enabled
             */
        }

        private void HandleRotation()
        {
            /* TODO:
             * 1. Check Q/E keys for rotation
             * 2. If snapRotation:
             *    - Snap to nearest angle
             * 3. Else:
             *    - Smooth rotation
             * 4. Update targetRotation
             */
        }

        private void HandleZoom()
        {
            /* TODO:
             * 1. Get scroll wheel input
             * 2. Update currentZoom
             * 3. Clamp between min/max
             * 4. Apply to camera (orthographic size or distance)
             */
        }

        private void HandleReset()
        {
            /* TODO:
             * If reset key pressed:
             * - Return to initial position/rotation
             */
            if (Input.GetKeyDown(resetCameraKey))
            {
                targetPosition = initialPosition;
                targetRotation = initialRotation.eulerAngles.y;
            }
        }

        private void ApplyMovement()
        {
            /* TODO:
             * Smoothly lerp to target position and rotation
             */
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * moveSpeed);
            
            Vector3 euler = transform.eulerAngles;
            euler.y = Mathf.LerpAngle(euler.y, targetRotation, Time.deltaTime * rotationSpeed);
            transform.eulerAngles = euler;
        }

        private bool IsPointerNearScreenEdge(out Vector2 edgeDirection)
        {
            /* TODO:
             * Check if mouse is near screen edges
             * Return direction for scrolling
             */
            edgeDirection = Vector2.zero;
            return false;
        }

        public void FocusOnPosition(Vector3 worldPosition)
        {
            /* TODO:
             * Move camera to look at position
             */
            targetPosition = worldPosition;
        }
    }

    /// <summary>
    /// Spring arm camera - smooth camera follow
    /// </summary>
    public class SpringArmCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float springStiffness = 5f;
        [SerializeField] private float dampingRatio = 0.5f;
        [SerializeField] private Vector3 offset = new Vector3(0f, 10f, -10f);
        
        private Vector3 velocity = Vector3.zero;

        private void LateUpdate()
        {
            if (target == null) return;
            
            /* TODO:
             * Spring-based smooth follow
             * Calculate target position with offset
             * Apply spring physics for smooth movement
             */
        }
    }

    /// <summary>
    /// Top camera - orthographic top-down view
    /// </summary>
    public class TopCamera : MonoBehaviour
    {
        [SerializeField] private float height = 20f;
        [SerializeField] private float moveSpeed = 10f;
        
        private void Update()
        {
            /* TODO:
             * Simple WASD movement
             * Keep at fixed height
             */
        }
    }

    /// <summary>
    /// Camera shake effect
    /// </summary>
    public class CameraShake : MonoBehaviour
    {
        private Vector3 originalPosition;
        
        public void Shake(float duration, float magnitude)
        {
            StartCoroutine(ShakeCoroutine(duration, magnitude));
        }

        private IEnumerator ShakeCoroutine(float duration, float magnitude)
        {
            /* TODO:
             * Shake camera for duration
             * Random offset within magnitude
             * Gradually reduce intensity
             */
            originalPosition = transform.localPosition;
            
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float x = UnityEngine.Random.Range(-1f, 1f) * magnitude;
                float y = UnityEngine.Random.Range(-1f, 1f) * magnitude;
                
                transform.localPosition = originalPosition + new Vector3(x, y, 0f);
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            transform.localPosition = originalPosition;
        }
    }

    /// <summary>
    /// Cinemachine camera component
    /// </summary>
    public class CineMachineCamera : MonoBehaviour
    {
        [SerializeField] private Transform followTarget;
        [SerializeField] private Transform lookAtTarget;
        
        /* TODO: Cinemachine integration */
    }

    /// <summary>
    /// Cinemachine area - camera bounds region
    /// </summary>
    public class CineMachineArea : MonoBehaviour
    {
        [SerializeField] private Bounds areaBounds;
        
        /* TODO: Define area boundaries for camera */
    }

    /// <summary>
    /// Cinemachine spring arm
    /// </summary>
    public class CineMachineSpringArm : MonoBehaviour
    {
        [SerializeField] private float armLength = 5f;
        [SerializeField] private LayerMask collisionMask;
        
        /* TODO: Collision-avoiding camera arm */
    }

    #endregion

    #region ═══════════════════════════════════════════════════════════════
    #region TIME MANAGEMENT - Detailed
    #endregion
    #region ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Time manager - controls game speed
    /// </summary>
    public class TimeManager : MonoBehaviour
    {
        #region Singleton
        public static TimeManager Instance { get; private set; }
        #endregion

        #region Time Scales
        public enum TimeScale
        {
            Paused = 0,
            Normal = 1,
            Fast = 2,
            Faster = 4,
            VeryFast = 8
        }
        #endregion

        #region Inspector Fields
        [SerializeField] private TimeScale currentTimeScale = TimeScale.Normal;
        [SerializeField] private TimeScale[] availableTimeScales = { TimeScale.Normal, TimeScale.Fast, TimeScale.Faster };
        #endregion

        #region Properties
        public float CurrentTimeScale => (float)currentTimeScale;
        public bool IsPaused => currentTimeScale == TimeScale.Paused;
        #endregion

        #region Events
        public event Action<TimeScale> OnTimeScaleChanged;
        public event Action OnPaused;
        public event Action OnResumed;
        #endregion

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void Initialize()
        {
            SetTimeScale(TimeScale.Normal);
        }

        public void SetTimeScale(TimeScale scale)
        {
            /* TODO:
             * 1. Set currentTimeScale
             * 2. Apply to Time.timeScale
             * 3. Fire OnTimeScaleChanged
             * 4. Fire GameEvents.OnTimeScaleChanged
             */
            currentTimeScale = scale;
            Time.timeScale = (float)scale;
            OnTimeScaleChanged?.Invoke(scale);
            GameEvents.OnTimeScaleChanged?.Invoke((float)scale);
        }

        public void CycleTimeScale()
        {
            /* TODO:
             * Cycle through available time scales
             */
            int currentIndex = System.Array.IndexOf(availableTimeScales, currentTimeScale);
            int nextIndex = (currentIndex + 1) % availableTimeScales.Length;
            SetTimeScale(availableTimeScales[nextIndex]);
        }

        public void Pause()
        {
            /* TODO:
             * Set time scale to Paused
             * Fire OnPaused
             */
            SetTimeScale(TimeScale.Paused);
            OnPaused?.Invoke();
        }

        public void Resume()
        {
            /* TODO:
             * Set to Normal (or last non-paused scale)
             * Fire OnResumed
             */
            SetTimeScale(TimeScale.Normal);
            OnResumed?.Invoke();
        }

        public void TogglePause()
        {
            if (IsPaused)
                Resume();
            else
                Pause();
        }
    }

    /// <summary>
    /// Time UI - shows current time scale
    /// </summary>
    public class TimeUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI timeScaleText;
        [SerializeField] private Image timeScaleIcon;
        
        private void OnEnable()
        {
            TimeManager.Instance.OnTimeScaleChanged += UpdateTimeScaleDisplay;
        }

        private void OnDisable()
        {
            TimeManager.Instance.OnTimeScaleChanged -= UpdateTimeScaleDisplay;
        }

        private void UpdateTimeScaleDisplay(TimeManager.TimeScale scale)
        {
            /* TODO:
             * Update text to show scale (e.g., "2x", "4x")
             * Update icon/color
             */
            timeScaleText.text = $"{(int)scale}x";
        }
    }

    /// <summary>
    /// Time control button - UI button to change speed
    /// </summary>
    public class TimeControlButton : MonoBehaviour
    {
        [SerializeField] private TimeManager.TimeScale targetTimeScale;
        [SerializeField] private Button button;
        
        private void Start()
        {
            button.onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            TimeManager.Instance.SetTimeScale(targetTimeScale);
        }
    }

    /// <summary>
    /// Time spawner - spawns objects at intervals
    /// </summary>
    public class TimeSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject spawnPrefab;
        [SerializeField] private float spawnInterval = 1f;
        [SerializeField] private Transform spawnPoint;
        
        private float timer;

        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                Spawn();
                timer = spawnInterval;
            }
        }

        private void Spawn()
        {
            /* TODO: Instantiate prefab at spawn point */
            Instantiate(spawnPrefab, spawnPoint.position, spawnPoint.rotation);
        }
    }

    #endregion

    #region ═══════════════════════════════════════════════════════════════
    #region COMPLETE UI SYSTEM - All Panels
    #endregion
    #region ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Detailed UI Manager
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        #region Singleton
        public static UIManager Instance { get; private set; }
        #endregion

        #region Panels
        [Header("Main Panels")]
        [SerializeField] private GameObject hudPanel;
        [SerializeField] private GameObject buildMenuPanel;
        [SerializeField] private GameObject storePanel;
        [SerializeField] private GameObject pauseMenuPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject endGamePanel;
        [SerializeField] private GameObject tutorialPanel;
        [SerializeField] private GameObject upgradesPanel;
        
        [Header("HUD Elements")]
        [SerializeField] private HUDController hudController;
        [SerializeField] private CycleTimeUI cycleTimeUI;
        #endregion

        private GameObject currentPanel;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void Initialize()
        {
            /* TODO:
             * Show HUD by default
             * Hide all other panels
             */
            ShowHUD();
        }

        public void ShowPanel(GameObject panel)
        {
            /* TODO:
             * Hide current panel
             * Show new panel
             * Update currentPanel
             */
        }

        public void HideAllPanels()
        {
            /* TODO: Deactivate all panels except HUD */
        }

        public void ShowHUD() => ShowPanel(hudPanel);
        public void ShowBuildMenu() => ShowPanel(buildMenuPanel);
        public void ShowStore() => ShowPanel(storePanel);
        public void ShowPauseMenu() => ShowPanel(pauseMenuPanel);
        public void ShowSettings() => ShowPanel(settingsPanel);
        public void ShowEndGame() => ShowPanel(endGamePanel);
        public void ShowTutorial() => ShowPanel(tutorialPanel);
        public void ShowUpgrades() => ShowPanel(upgradesPanel);
    }

    /// <summary>
    /// HUD Controller - detailed
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("Resources")]
        [SerializeField] private Transform resourcesContainer;
        [SerializeField] private GameObject resourceUIPrefab;
        
        [Header("Player Info")]
        [SerializeField] private StatBar healthBar;
        [SerializeField] private TextMeshProUGUI moneyText;
        
        [Header("Wave Info")]
        [SerializeField] private TextMeshProUGUI waveNumberText;
        [SerializeField] private TextMeshProUGUI enemiesRemainingText;
        [SerializeField] private TextMeshProUGUI timeToNextWaveText;
        
        private Dictionary<ResourceType, TextMeshProUGUI> resourceTexts = new Dictionary<ResourceType, TextMeshProUGUI>();

        private void OnEnable()
        {
            /* TODO: Subscribe to all events */
            GameEvents.OnResourceGained += OnResourceChanged;
            GameEvents.OnResourceSpent += OnResourceChanged;
            GameEvents.OnPlayerMoneyChanged += OnMoneyChanged;
            GameEvents.OnPlayerHealthChanged += OnHealthChanged;
            GameEvents.OnWaveStarted += OnWaveStarted;
        }

        private void OnDisable()
        {
            /* TODO: Unsubscribe */
        }

        private void Start()
        {
            /* TODO: Create resource UI elements for each resource type */
            CreateResourceDisplays();
        }

        private void CreateResourceDisplays()
        {
            /* TODO:
             * For each ResourceType in enum:
             * - Create UI element
             * - Store reference in dictionary
             */
        }

        private void OnResourceChanged(ResourceType type, int amount)
        {
            /* TODO: Update resource display */
            if (resourceTexts.ContainsKey(type))
            {
                int currentAmount = PlayerData.Instance.GetResourceAmount(type);
                resourceTexts[type].text = currentAmount.ToString();
            }
        }

        private void OnMoneyChanged(int amount)
        {
            moneyText.text = $"${amount}";
        }

        private void OnHealthChanged(int current, int max)
        {
            healthBar.SetValue(current, max);
        }

        private void OnWaveStarted(int waveNumber)
        {
            waveNumberText.text = $"Wave {waveNumber}";
        }
    }

    /// <summary>
    /// Store UI - shop interface
    /// </summary>
    public class StoreUI : MonoBehaviour
    {
        [SerializeField] private StoreUIInfoPanel infoPanel;
        [SerializeField] private Transform categoriesContainer;
        [SerializeField] private Transform itemsContainer;
        [SerializeField] private StoreElementUI storeElementPrefab;
        
        private BuildingCategory currentCategory;
        private List<StoreElementUI> currentElements = new List<StoreElementUI>();

        public void ShowCategory(BuildingCategory category)
        {
            /* TODO:
             * Clear current items
             * Load buildings of category
             * Create UI elements
             */
        }

        public void SelectBuilding(BuildingData building)
        {
            /* TODO:
             * Show info panel
             * Enter build mode
             */
        }
    }

    /// <summary>
    /// Store UI info panel
    /// </summary>
    public class StoreUIInfoPanel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Image iconImage;
        [SerializeField] private Transform costContainer;
        [SerializeField] private Button buyButton;

        public void ShowBuildingInfo(BuildingData building)
        {
            /* TODO: Display building info, cost, etc. */
        }
    }

    /// <summary>
    /// Store UI tower info
    /// </summary>
    public class StoreUITowerInfo : StoreUIInfoPanel
    {
        [SerializeField] private TextMeshProUGUI damageText;
        [SerializeField] private TextMeshProUGUI rangeText;
        [SerializeField] private TextMeshProUGUI fireRateText;

        public void ShowTowerInfo(TowerData tower)
        {
            /* TODO: Show tower-specific stats */
        }
    }

    /// <summary>
    /// Store UI extractor info
    /// </summary>
    public class StoreUIExtractorInfo : StoreUIInfoPanel
    {
        [SerializeField] private Image resourceIcon;
        [SerializeField] private TextMeshProUGUI extractionRateText;

        public void ShowExtractorInfo(ExtractorData extractor)
        {
            /* TODO: Show extractor-specific info */
        }
    }

    /// <summary>
    /// Store UI processor info
    /// </summary>
    public class StoreUIProcessorInfo : StoreUIInfoPanel
    {
        [SerializeField] private Transform recipesContainer;

        public void ShowProcessorInfo(ProcessorData processor)
        {
            /* TODO: Show processor recipes */
        }
    }

    /// <summary>
    /// Store element UI - single building in shop
    /// </summary>
    public class StoreElementUI : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI priceText;
        [SerializeField] private Button button;
        
        private BuildingData building;

        public void SetBuilding(BuildingData data)
        {
            building = data;
            /* TODO: Update UI elements */
        }

        private void OnButtonClicked()
        {
            /* TODO: Notify StoreUI of selection */
        }
    }

    /// <summary>
    /// Settings controller
    /// </summary>
    public class SettingsController : MonoBehaviour
    {
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private TMP_Dropdown qualityDropdown;
        
        private void Start()
        {
            /* TODO:
             * Load saved settings
             * Setup UI elements
             * Add listeners
             */
        }

        public void OnMasterVolumeChanged(float value)
        {
            AudioSystem.Instance.SetMasterVolume(value);
        }

        public void OnMusicVolumeChanged(float value)
        {
            AudioSystem.Instance.SetMusicVolume(value);
        }

        public void OnFullscreenToggled(bool value)
        {
            Screen.fullScreen = value;
        }

        public void SaveSettings()
        {
            /* TODO: Save to PlayerPrefs */
        }

        public void LoadSettings()
        {
            /* TODO: Load from PlayerPrefs */
        }
    }

    /// <summary>
    /// Key rebind settings element
    /// </summary>
    public class SettingsElement_keyRebind : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI actionNameText;
        [SerializeField] private Button rebindButton;
        [SerializeField] private TextMeshProUGUI currentKeyText;
        
        private string actionName;
        private KeyCode currentKey;
        
        public void StartRebind()
        {
            /* TODO: Listen for key press and update binding */
        }
    }

    /// <summary>
    /// End game UI
    /// </summary>
    public class EndGameUI : MonoBehaviour
    {
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private GameObject defeatPanel;
        [SerializeField] private EndGameInfoUI infoUI;
        [SerializeField] private EndGameAnimationUI animationUI;
        
        public void ShowVictory()
        {
            victoryPanel.SetActive(true);
            defeatPanel.SetActive(false);
            /* TODO: Show victory stats, play animation */
        }

        public void ShowDefeat()
        {
            victoryPanel.SetActive(false);
            defeatPanel.SetActive(true);
            /* TODO: Show defeat stats */
        }
    }

    /// <summary>
    /// End game info UI
    /// </summary>
    public class EndGameInfoUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI wavesCompletedText;
        [SerializeField] private TextMeshProUGUI enemiesKilledText;
        [SerializeField] private TextMeshProUGUI buildingsBuiltText;
        [SerializeField] private TextMeshProUGUI timePlayedText;
        
        public void ShowStats()
        {
            /* TODO: Display end game statistics */
        }
    }

    /// <summary>
    /// End game animation UI
    /// </summary>
    public class EndGameAnimationUI : MonoBehaviour
    {
        public void PlayVictoryAnimation()
        {
            /* TODO: Play victory animation sequence */
        }
    }

    /// <summary>
    /// Cycle time UI - shows wave timer
    /// </summary>
    public class CycleTimeUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private Image progressBar;
        
        private void Update()
        {
            /* TODO: Show time until next wave */
        }
    }

    // Additional CycleTimeUI variants from source:
    public class CycleTimeUI_ticksCircle : CycleTimeUI { }
    public class CycleTimeUI_floatingBubble : CycleTimeUI { }
    public class CycleTimeUI_crystal : CycleTimeUI { }
    public class CycleTimeUI_centerController : CycleTimeUI { }

    /// <summary>
    /// Damage FX UI - floating damage numbers
    /// </summary>
    public class DamageFXUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI damageText;
        [SerializeField] private float floatSpeed = 1f;
        [SerializeField] private float lifetime = 1f;
        
        public void Show(int damage, Vector3 worldPosition, Color color)
        {
            /* TODO:
             * Set text to damage amount
             * Position at world position
             * Float upward
             * Fade out
             * Destroy after lifetime
             */
        }

        private void Update()
        {
            /* TODO: Float upward and fade */
        }
    }

    /// <summary>
    /// Resource reward UI - shows resource collection popup
    /// </summary>
    public class ResourceRewardUI : MonoBehaviour
    {
        [SerializeField] private Image resourceIcon;
        [SerializeField] private TextMeshProUGUI amountText;
        
        public void Show(ResourceType type, int amount, Vector3 worldPosition)
        {
            /* TODO: Show resource collection notification */
        }
    }

    /// <summary>
    /// Recipe element UI - displays recipe in UI
    /// </summary>
    public class RecipeElementUI : MonoBehaviour
    {
        [SerializeField] private Image recipeIcon;
        [SerializeField] private TextMeshProUGUI recipeNameText;
        [SerializeField] private Transform inputsContainer;
        [SerializeField] private Transform outputsContainer;
        
        public void SetRecipe(Recipe recipe)
        {
            /* TODO: Display recipe info */
        }
    }

    /// <summary>
    /// Player upgrade UI
    /// </summary>
    public class PlayerUpgradeUI : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Transform costContainer;
        [SerializeField] private Button purchaseButton;
        
        private PlayerUpgrade upgrade;

        public void SetUpgrade(PlayerUpgrade upgradeData)
        {
            /* TODO: Display upgrade info */
        }

        public void OnPurchaseClicked()
        {
            /* TODO: Purchase upgrade */
        }
    }

    /// <summary>
    /// Tutorial info UI
    /// </summary>
    public class TutorialInfoUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI questTitleText;
        [SerializeField] private TextMeshProUGUI questDescriptionText;
        [SerializeField] private Image questIcon;
        [SerializeField] private Image progressBar;
        
        public void ShowQuest(TutorialQuestData quest)
        {
            /* TODO: Display quest info */
        }

        public void UpdateProgress(float progress)
        {
            progressBar.fillAmount = progress;
        }
    }

    #endregion

    #region ═══════════════════════════════════════════════════════════════
    #region SPAWNER POSITION VARIANTS
    #endregion
    #region ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Base spawner position
    /// </summary>
    public abstract class SpawnerPosition : ScriptableObject
    {
        public abstract Vector3 GetSpawnPosition();
    }

    /// <summary>
    /// Circle spawner position
    /// </summary>
    [CreateAssetMenu(menuName = "TowerFactory/Spawner/Circle Position")]
    public class SpawnerPosition_Circle : SpawnerPosition
    {
        public Vector3 center;
        public float radius = 5f;
        
        public override Vector3 GetSpawnPosition()
        {
            /* TODO: Random position on circle */
            float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
            return center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
        }
    }

    /// <summary>
    /// Line spawner position
    /// </summary>
    [CreateAssetMenu(menuName = "TowerFactory/Spawner/Line Position")]
    public class SpawnerPosition_Line : SpawnerPosition
    {
        public Vector3 start;
        public Vector3 end;
        
        public override Vector3 GetSpawnPosition()
        {
            /* TODO: Random position on line */
            return Vector3.Lerp(start, end, UnityEngine.Random.Range(0f, 1f));
        }
    }

    /// <summary>
    /// Fixed positions spawner
    /// </summary>
    [CreateAssetMenu(menuName = "TowerFactory/Spawner/Fixed Positions")]
    public class SpawnerPosition_FixedPositions : SpawnerPosition
    {
        public Vector3[] positions;
        
        public override Vector3 GetSpawnPosition()
        {
            /* TODO: Random from fixed positions */
            if (positions.Length == 0) return Vector3.zero;
            return positions[UnityEngine.Random.Range(0, positions.Length)];
        }
    }

    /// <summary>
    /// Path tile spawner position
    /// </summary>
    [CreateAssetMenu(menuName = "TowerFactory/Spawner/Path Tile Position")]
    public class SpawnerPosition_PathTile : SpawnerPosition
    {
        public PathTile pathTile;
        
        public override Vector3 GetSpawnPosition()
        {
            /* TODO: Return path tile position */
            return pathTile != null ? pathTile.transform.position : Vector3.zero;
        }
    }

    #endregion

    #region ═══════════════════════════════════════════════════════════════
    #region CONTROLLER & INPUT MODES
    #endregion
    #region ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Base controller class
    /// </summary>
    public abstract class Controller : MonoBehaviour
    {
        protected bool isActive = false;
        
        public virtual void Activate()
        {
            isActive = true;
        }

        public virtual void Deactivate()
        {
            isActive = false;
        }

        protected virtual void Update()
        {
            if (isActive)
            {
                HandleInput();
            }
        }

        protected abstract void HandleInput();
    }

    /// <summary>
    /// Standard input mode - normal gameplay
    /// </summary>
    public class StandardInputMode : Controller
    {
        protected override void HandleInput()
        {
            /* TODO:
             * Handle standard gameplay input:
             * - Camera movement
             * - Building selection
             * - Resource collection
             */
        }
    }

    /// <summary>
    /// Buy mode input mode - building placement
    /// </summary>
    public class BuyModeInputMode : Controller
    {
        private BuildingData selectedBuilding;
        private GameObject ghostObject;
        
        public void SetBuilding(BuildingData building)
        {
            /* TODO: Create ghost object */
            selectedBuilding = building;
        }

        protected override void HandleInput()
        {
            /* TODO:
             * Update ghost position
             * Handle rotation (R key)
             * Handle placement (left click)
             * Handle cancel (right click/Esc)
             */
        }
    }

    /// <summary>
    /// Edit mode input mode - moving buildings
    /// </summary>
    public class EditModeInputMode : Controller
    {
        protected override void HandleInput()
        {
            /* TODO:
             * Click to drag buildings
             * Sell buildings
             * Cancel with right click
             */
        }
    }

    #endregion

    #region ═══════════════════════════════════════════════════════════════
    #region UTILITY CLASSES & HELPERS
    #endregion
    #region ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Directional mover - smooth movement in direction
    /// </summary>
    public class DirectionalMover : MonoBehaviour
    {
        [SerializeField] private Vector3 direction = Vector3.forward;
        [SerializeField] private float speed = 1f;
        
        private void Update()
        {
            transform.position += direction * speed * Time.deltaTime;
        }
    }

    /// <summary>
    /// Rotator - continuous rotation
    /// </summary>
    public class Rotator : MonoBehaviour
    {
        [SerializeField] private Vector3 rotationAxis = Vector3.up;
        [SerializeField] private float rotationSpeed = 90f;
        
        private void Update()
        {
            transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// Auto destroy - destroys GameObject after time
    /// </summary>
    public class AutoDestroy : MonoBehaviour
    {
        [SerializeField] private float lifetime = 5f;
        
        private void Start()
        {
            Destroy(gameObject, lifetime);
        }
    }

    /// <summary>
    /// Weighted random selector - pick random with weights
    /// </summary>
    public class WeightedRandomSelector<T>
    {
        [System.Serializable]
        public class WeightedItem
        {
            public T item;
            public float weight;
        }

        private List<WeightedItem> items = new List<WeightedItem>();
        private float totalWeight;

        public void AddItem(T item, float weight)
        {
            items.Add(new WeightedItem { item = item, weight = weight });
            totalWeight += weight;
        }

        public T SelectRandom()
        {
            /* TODO:
             * Random value 0 to totalWeight
             * Iterate through items, subtract weights
             * Return item when value <= 0
             */
            return default(T);
        }
    }

    /// <summary>
    /// Cursor controller - custom cursor
    /// </summary>
    public class CursorController : MonoBehaviour
    {
        [SerializeField] private Texture2D defaultCursor;
        [SerializeField] private Texture2D clickCursor;
        [SerializeField] private Texture2D buildCursor;
        [SerializeField] private Vector2 hotspot = Vector2.zero;
        
        public void SetCursor(Texture2D cursor)
        {
            Cursor.SetCursor(cursor, hotspot, CursorMode.Auto);
        }

        public void SetDefaultCursor() => SetCursor(defaultCursor);
        public void SetClickCursor() => SetCursor(clickCursor);
        public void SetBuildCursor() => SetCursor(buildCursor);
    }

    /// <summary>
    /// Character - base character class (if needed for NPCs/units)
    /// </summary>
    public class Character : MonoBehaviour
    {
        [SerializeField] protected int health = 100;
        [SerializeField] protected float moveSpeed = 3f;
        
        protected int currentHealth;

        protected virtual void Awake()
        {
            currentHealth = health;
        }

        public virtual void TakeDamage(int damage)
        {
            /* TODO: Apply damage, check death */
        }

        public virtual void Move(Vector3 direction)
        {
            /* TODO: Move character */
        }
    }

    /// <summary>
    /// Team component - for team-based gameplay
    /// </summary>
    public class TeamComponent : MonoBehaviour
    {
        public enum Team { Player, Enemy, Neutral }
        
        [SerializeField] private Team team = Team.Player;
        
        public Team CurrentTeam => team;
        
        public bool IsAlly(TeamComponent other)
        {
            return team == other.team;
        }

        public bool IsEnemy(TeamComponent other)
        {
            return team != Team.Neutral && other.team != Team.Neutral && team != other.team;
        }
    }

    /// <summary>
    /// Trigger - generic trigger component
    /// </summary>
    public class Trigger : MonoBehaviour
    {
        public event Action<Collider> OnTriggerEntered;
        public event Action<Collider> OnTriggerExited;
        
        private void OnTriggerEnter(Collider other)
        {
            OnTriggerEntered?.Invoke(other);
        }

        private void OnTriggerExit(Collider other)
        {
            OnTriggerExited?.Invoke(other);
        }
    }

    /// <summary>
    /// Debug manager
    /// </summary>
    public class DebugManager : MonoBehaviour
    {
        [SerializeField] private bool enableDebugMode = false;
        [SerializeField] private KeyCode debugToggleKey = KeyCode.F1;
        
        private void Update()
        {
            if (Input.GetKeyDown(debugToggleKey))
            {
                enableDebugMode = !enableDebugMode;
            }
        }

        public void Log(string message)
        {
            if (enableDebugMode)
                Debug.Log($"[DEBUG] {message}");
        }
    }

    /// <summary>
    /// FPS display
    /// </summary>
    public class FPSDisplay : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI fpsText;
        [SerializeField] private float updateInterval = 0.5f;
        
        private float fps;
        private float timer;

        private void Update()
        {
            timer += Time.deltaTime;
            if (timer >= updateInterval)
            {
                fps = 1f / Time.deltaTime;
                if (fpsText != null)
                    fpsText.text = $"FPS: {Mathf.RoundToInt(fps)}";
                timer = 0f;
            }
        }
    }

    #endregion

    #region ═══════════════════════════════════════════════════════════════
    #region REMAINING SPECIALIZED SYSTEMS
    #endregion
    #region ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// AI Controller - for AI-controlled units
    /// </summary>
    public class AiController : MonoBehaviour
    {
        private FSMComponent fsm;
        
        /* TODO: AI behavior control via FSM */
    }

    /// <summary>
    /// Wind controller - environmental wind
    /// </summary>
    public class WindController : MonoBehaviour
    {
        [SerializeField] private float windStrength = 1f;
        [SerializeField] private Vector3 windDirection = Vector3.right;
        
        /* TODO: Apply wind to particles, effects */
    }

    /// <summary>
    /// Day night cycle
    /// </summary>
    public class DayNightCycle : MonoBehaviour
    {
        [SerializeField] private Light directionalLight;
        [SerializeField] private float dayDuration = 120f;
        
        private float currentTime = 0f;

        private void Update()
        {
            /* TODO:
             * Update time
             * Rotate light
             * Change light color/intensity
             */
        }
    }

    /// <summary>
    /// Environment generator
    /// </summary>
    public class EnvironmentGenerator : MonoBehaviour
    {
        /* TODO: Procedural environment generation */
    }

    /// <summary>
    /// Enemy hit VFX
    /// </summary>
    public class EnemyHitVFX : MonoBehaviour
    {
        public void Play(Vector3 position, DamageType damageType)
        {
            /* TODO: Play appropriate VFX for damage type */
        }
    }

    /// <summary>
    /// Victory animation
    /// </summary>
    public class VictoryAnimation : MonoBehaviour
    {
        public void PlayVictorySequence()
        {
            /* TODO: Play victory animation sequence */
        }
    }

    /// <summary>
    /// Game over animation
    /// </summary>
    public class GameOverAnimation : MonoBehaviour
    {
        public void PlayDefeatSequence()
        {
            /* TODO: Play defeat animation */
        }
    }

    /// <summary>
    /// Reachable source indicator - shows if source is reachable by conveyors
    /// </summary>
    public class ReachableSourceIndicator : MonoBehaviour
    {
        [SerializeField] private Material reachableMaterial;
        [SerializeField] private Material unreachableMaterial;
        
        public void UpdateReachability(bool isReachable)
        {
            /* TODO: Change material based on reachability */
        }
    }

    #endregion
}
