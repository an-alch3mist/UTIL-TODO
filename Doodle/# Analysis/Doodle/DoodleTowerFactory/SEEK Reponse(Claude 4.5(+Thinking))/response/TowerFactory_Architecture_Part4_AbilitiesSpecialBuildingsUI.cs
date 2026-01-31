/*
 * ============================================================================
 * TOWERFACTORY - COMPLETE ARCHITECTURAL SKELETON
 * Part 4: Abilities, Special Buildings, Beacons, Detailed UI
 * ============================================================================
 * 
 * This part covers ALL systems missing from Part 3:
 * - Complete Abilities System (ActiveAbility, Ability, AbilityManager, etc.)
 * - All Special Building Types (Source, Producer, Chest, Obelisk, etc.)
 * - All Beacon Variants (Circle, Cone, Squared)
 * - Complete UI System (all SelectableUI, TooltipUI variants)
 * - Detailed Stat System
 * - Save System Components
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

namespace LightTower
{
    #region ═══════════════════════════════════════════════════════════════
    #region ABILITIES SYSTEM - COMPLETE
    #endregion
    #region ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Active ability data - defines player-activated special abilities
    /// </summary>
    [CreateAssetMenu(fileName = "NewActiveAbility", menuName = "TowerFactory/Abilities/Active Ability")]
    public class ActiveAbility : ScriptableObject
    {
        [Header("Identity")]
        public string abilityName;
        public string description;
        public Sprite icon;
        
        [Header("Costs")]
        public Cost activationCost;
        public float cooldownTime = 10f;
        
        [Header("Targeting")]
        public bool requiresTargetPosition = false;
        public float targetRadius = 5f;
        public LayerMask targetLayers;
        
        [Header("Effects")]
        public GameObject effectPrefab;
        public AudioData activationSound;
        
        [Header("Unlock")]
        public AbilityUnlockCondition unlockCondition;

        /// <summary>
        /// Execute the ability at target position
        /// </summary>
        public virtual void Execute(Vector3 targetPosition)
        {
            /* TODO:
             * 1. Instantiate effect prefab
             * 2. Apply ability effects
             * 3. Play sound
             * 4. Start cooldown
             */
        }

        /// <summary>
        /// Check if ability can be activated
        /// </summary>
        public virtual bool CanActivate()
        {
            /* TODO:
             * 1. Check if on cooldown
             * 2. Check if player has resources
             * 3. Check unlock condition
             */
            return false;
        }
    }

    /// <summary>
    /// Base ability class - passive or active
    /// </summary>
    [CreateAssetMenu(fileName = "NewAbility", menuName = "TowerFactory/Abilities/Ability")]
    public class Ability : ScriptableObject
    {
        public string abilityName;
        public string description;
        public Sprite icon;
        public bool isPassive = true;
        
        [Header("Effects")]
        public StatModifier[] statModifiers;
        
        public virtual void Apply()
        {
            /* TODO: Apply ability effects */
        }

        public virtual void Remove()
        {
            /* TODO: Remove ability effects */
        }
    }

    /// <summary>
    /// Unlock condition for abilities
    /// </summary>
    [System.Serializable]
    public class AbilityUnlockCondition
    {
        public enum ConditionType
        {
            AlwaysUnlocked,
            PlayerLevel,
            BuildingBuilt,
            EnemiesKilled,
            WaveCompleted,
            ResourceCollected,
            UpgradePurchased
        }

        public ConditionType type;
        public int requiredValue;
        public string requiredID;  // Building name, upgrade ID, etc.

        public bool IsMet()
        {
            /* TODO:
             * Check condition based on type
             * - AlwaysUnlocked: return true
             * - PlayerLevel: check PlayerData.Level >= requiredValue
             * - BuildingBuilt: check if building exists
             * - EnemiesKilled: check kill count
             * - WaveCompleted: check wave index
             * - ResourceCollected: check resource amount
             * - UpgradePurchased: check if upgrade owned
             */
            return false;
        }
    }

    /// <summary>
    /// Queued ability for execution
    /// </summary>
    [System.Serializable]
    public class QueuedAbility
    {
        public ActiveAbility ability;
        public Vector3 targetPosition;
        public float delayTime;
        public bool executed = false;

        public QueuedAbility(ActiveAbility ability, Vector3 target, float delay = 0f)
        {
            this.ability = ability;
            this.targetPosition = target;
            this.delayTime = delay;
        }
    }

    /// <summary>
    /// Ability queue - manages queued abilities
    /// </summary>
    public class AbilityQueue : MonoBehaviour
    {
        private List<QueuedAbility> queue = new List<QueuedAbility>();

        public void EnqueueAbility(ActiveAbility ability, Vector3 target, float delay = 0f)
        {
            /* TODO: Add to queue */
            queue.Add(new QueuedAbility(ability, target, delay));
        }

        private void Update()
        {
            /* TODO:
             * For each queued ability:
             * - Reduce delay
             * - If delay <= 0 and not executed:
             *   - Execute ability
             *   - Mark as executed
             * - Remove executed abilities
             */
        }
    }

    /// <summary>
    /// Ability manager - tracks unlocked and active abilities
    /// </summary>
    public class AbilityManager : MonoBehaviour
    {
        #region Singleton
        public static AbilityManager Instance { get; private set; }
        #endregion

        #region Inspector Fields
        [Header("Available Abilities")]
        [SerializeField] private ActiveAbility[] allAbilities;
        
        [Header("Ability Queue")]
        [SerializeField] private AbilityQueue abilityQueue;
        #endregion

        #region Private Fields
        private HashSet<string> unlockedAbilities = new HashSet<string>();
        private Dictionary<ActiveAbility, float> cooldowns = new Dictionary<ActiveAbility, float>();
        private List<Ability> activePassiveAbilities = new List<Ability>();
        #endregion

        #region Events
        public event Action<ActiveAbility> OnAbilityUnlocked;
        public event Action<ActiveAbility> OnAbilityActivated;
        public event Action<ActiveAbility, float> OnAbilityCooldownChanged;
        #endregion

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void Initialize()
        {
            /* TODO: Check unlock conditions for all abilities */
        }

        private void Update()
        {
            /* TODO: Update cooldowns */
        }

        #region Ability Activation
        public bool ActivateAbility(ActiveAbility ability, Vector3 targetPosition)
        {
            /* TODO:
             * 1. Check if ability is unlocked
             * 2. Check if can activate (cooldown, cost)
             * 3. Spend cost
             * 4. Execute ability
             * 5. Start cooldown
             * 6. Fire events
             */
            return false;
        }

        public void QueueAbility(ActiveAbility ability, Vector3 target, float delay)
        {
            /* TODO: Add to ability queue */
        }
        #endregion

        #region Ability Management
        public void UnlockAbility(ActiveAbility ability)
        {
            /* TODO:
             * 1. Add to unlockedAbilities
             * 2. Fire OnAbilityUnlocked
             */
        }

        public bool IsAbilityUnlocked(ActiveAbility ability)
        {
            return unlockedAbilities.Contains(ability.abilityName);
        }

        public float GetCooldownRemaining(ActiveAbility ability)
        {
            return cooldowns.ContainsKey(ability) ? cooldowns[ability] : 0f;
        }

        public void ApplyPassiveAbility(Ability ability)
        {
            /* TODO:
             * 1. Add to activePassiveAbilities
             * 2. Apply stat modifiers
             */
        }

        public void RemovePassiveAbility(Ability ability)
        {
            /* TODO:
             * 1. Remove from activePassiveAbilities
             * 2. Remove stat modifiers
             */
        }
        #endregion

        #region Cooldown Management
        private void StartCooldown(ActiveAbility ability)
        {
            /* TODO: Set cooldown timer */
            cooldowns[ability] = ability.cooldownTime;
        }

        private void UpdateCooldowns()
        {
            /* TODO:
             * For each ability in cooldowns:
             * - Reduce by Time.deltaTime
             * - Fire OnAbilityCooldownChanged
             * - Remove if <= 0
             */
        }
        #endregion
    }

    /// <summary>
    /// Active ability input data - for ability activation
    /// </summary>
    [System.Serializable]
    public struct FActiveAbilityInputData
    {
        public ActiveAbility ability;
        public Vector3 targetPosition;
        public bool requiresTarget;

        public FActiveAbilityInputData(ActiveAbility ability, Vector3 target)
        {
            this.ability = ability;
            this.targetPosition = target;
            this.requiresTarget = ability.requiresTargetPosition;
        }
    }

    #endregion

    #region ═══════════════════════════════════════════════════════════════
    #region SPECIAL BUILDINGS - ALL TYPES
    #endregion
    #region ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Source building - generates resources over time (not from nodes)
    /// </summary>
    public class Source : MonoBehaviour, ISelectable
    {
        #region Inspector Fields
        [SerializeField] private BuildingData buildingData;
        [SerializeField] private ResourceType generatedResource;
        [SerializeField] private float generationRate = 1f;  // Per second
        [SerializeField] private int amountPerGeneration = 1;
        [SerializeField] private bool outputToConveyor = false;
        #endregion

        #region Private Fields
        private float generationTimer;
        private ConveyorBelt outputBelt;
        private PlacementComponent placement;
        #endregion

        #region Properties
        public GameObject GameObject => gameObject;
        #endregion

        private void Start()
        {
            /* TODO:
             * If outputToConveyor, find output belt
             */
        }

        private void Update()
        {
            generationTimer -= Time.deltaTime;
            if (generationTimer <= 0f)
            {
                GenerateResource();
                generationTimer = 1f / generationRate;
            }
        }

        private void GenerateResource()
        {
            /* TODO:
             * If outputToConveyor:
             * - Create item and add to belt
             * Else:
             * - Add to player inventory
             * Fire event
             */
        }

        #region ISelectable
        public string GetDisplayName() => buildingData.buildingName;
        public Sprite GetIcon() => buildingData.icon;
        public string GetDescription() => $"Generating: {generatedResource} ({generationRate}/s)";
        public void OnSelected() { }
        public void OnDeselected() { }
        #endregion
    }

    /// <summary>
    /// Producer building - similar to Source but with more complex logic
    /// </summary>
    public class Producer : MonoBehaviour, ISelectable
    {
        [SerializeField] private BuildingData buildingData;
        [SerializeField] private Recipe productionRecipe;
        [SerializeField] private float productionRate = 1f;
        
        private float productionTimer;
        private ConveyorBelt outputBelt;

        private void Update()
        {
            /* TODO: Similar to Source but can have input requirements from recipe */
        }

        public GameObject GameObject => gameObject;
        public string GetDisplayName() => buildingData.buildingName;
        public Sprite GetIcon() => buildingData.icon;
        public string GetDescription() => $"Producing: {productionRecipe?.recipeName}";
        public void OnSelected() { }
        public void OnDeselected() { }
    }

    /// <summary>
    /// Chest - collectible resource container
    /// </summary>
    public class Chest : MonoBehaviour, ISelectable
    {
        #region Inspector Fields
        [SerializeField] private ResourceAmount[] contents;
        [SerializeField] private bool oneTimeUse = true;
        [SerializeField] private GameObject openedVisual;
        [SerializeField] private GameObject closedVisual;
        #endregion

        #region Private Fields
        private bool isOpened = false;
        #endregion

        public void Open()
        {
            /* TODO:
             * 1. If already opened and oneTimeUse, return
             * 2. Add all contents to player inventory
             * 3. Set isOpened = true
             * 4. Swap visuals (closed → opened)
             * 5. Play sound/VFX
             * 6. If oneTimeUse, disable interaction
             */
        }

        public GameObject GameObject => gameObject;
        public string GetDisplayName() => "Chest";
        public Sprite GetIcon() => null;
        public string GetDescription() => isOpened ? "Empty" : "Click to open";
        public void OnSelected() { }
        public void OnDeselected() { }
    }

    /// <summary>
    /// Golden coins chest - special reward chest
    /// </summary>
    public class GoldenCoinsChest : Chest
    {
        [SerializeField] private int goldAmount = 100;

        public new void Open()
        {
            /* TODO:
             * Add gold to player
             * Call base.Open()
             */
        }
    }

    /// <summary>
    /// Crystal Altar - special building for crystal collection/interaction
    /// </summary>
    public class CrystalAltar : MonoBehaviour, ISelectable
    {
        #region Inspector Fields
        [SerializeField] private int requiredCrystals = 5;
        [SerializeField] private GameObject activatedVisual;
        [SerializeField] private GameObject inactiveVisual;
        #endregion

        #region Private Fields
        private int currentCrystals = 0;
        private bool isActivated = false;
        #endregion

        public void AddCrystal()
        {
            /* TODO:
             * 1. Increment currentCrystals
             * 2. Update visual
             * 3. If currentCrystals >= requiredCrystals:
             *    - Activate altar
             *    - Trigger special event (quest completion, unlock, etc.)
             */
        }

        private void Activate()
        {
            /* TODO:
             * 1. Set isActivated = true
             * 2. Swap visuals
             * 3. Play activation VFX
             * 4. Fire quest/victory event
             */
        }

        public GameObject GameObject => gameObject;
        public string GetDisplayName() => "Crystal Altar";
        public Sprite GetIcon() => null;
        public string GetDescription() => $"Crystals: {currentCrystals}/{requiredCrystals}";
        public void OnSelected() { }
        public void OnDeselected() { }
    }

    /// <summary>
    /// Crystal Finder - reveals location of crystals
    /// </summary>
    public class CrystalFinder : MonoBehaviour, ISelectable
    {
        [SerializeField] private float detectionRadius = 20f;
        [SerializeField] private GameObject indicatorPrefab;
        
        private List<Transform> crystalLocations = new List<Transform>();
        private List<GameObject> indicators = new List<GameObject>();

        private void Start()
        {
            /* TODO:
             * Find all crystals in radius
             * Create indicator for each
             */
        }

        public GameObject GameObject => gameObject;
        public string GetDisplayName() => "Crystal Finder";
        public Sprite GetIcon() => null;
        public string GetDescription() => $"Detecting {crystalLocations.Count} crystals";
        public void OnSelected() { }
        public void OnDeselected() { }
    }

    /// <summary>
    /// Enemy Tower - hostile tower that attacks player buildings
    /// </summary>
    public class EnemyTower : MonoBehaviour, ISelectable
    {
        #region Inspector Fields
        [SerializeField] private TowerData towerData;
        [SerializeField] private int health = 100;
        #endregion

        #region Private Fields
        private int currentHealth;
        private Tower currentTarget;
        private float fireCooldown;
        #endregion

        private void Start()
        {
            currentHealth = health;
            /* TODO: Find player towers in range */
        }

        private void Update()
        {
            /* TODO:
             * Similar to Tower but targets player buildings instead of enemies
             */
        }

        public void TakeDamage(int damage)
        {
            /* TODO:
             * Reduce health
             * If health <= 0, destroy and drop rewards
             */
        }

        public GameObject GameObject => gameObject;
        public string GetDisplayName() => "Enemy Tower";
        public Sprite GetIcon() => towerData.icon;
        public string GetDescription() => $"HP: {currentHealth}/{health}";
        public void OnSelected() { }
        public void OnDeselected() { }
    }

    /// <summary>
    /// Player Tower - main base/objective to defend
    /// </summary>
    public class PlayerTower : MonoBehaviour, ISelectable
    {
        #region Inspector Fields
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private GameObject healthBarPrefab;
        #endregion

        #region Private Fields
        private int currentHealth;
        private PlayerTowerHealthBar healthBar;
        #endregion

        #region Events
        public event Action<int, int> OnHealthChanged;
        public event Action OnDestroyed;
        #endregion

        private void Start()
        {
            currentHealth = maxHealth;
            /* TODO: Create health bar */
        }

        public void TakeDamage(int damage)
        {
            /* TODO:
             * 1. Reduce currentHealth
             * 2. Update health bar
             * 3. Fire OnHealthChanged
             * 4. If health <= 0:
             *    - Fire OnDestroyed
             *    - Trigger game defeat
             */
        }

        public void Heal(int amount)
        {
            /* TODO: Restore health, update bar */
        }

        public GameObject GameObject => gameObject;
        public string GetDisplayName() => "Player Tower";
        public Sprite GetIcon() => null;
        public string GetDescription() => $"HP: {currentHealth}/{maxHealth}";
        public void OnSelected() { }
        public void OnDeselected() { }
    }

    /// <summary>
    /// Obelisk - special beacon-like building with unique effects
    /// </summary>
    public class Obelisk : MonoBehaviour, ISelectable
    {
        [SerializeField] private BuildingData buildingData;
        [SerializeField] private float effectRadius = 10f;
        [SerializeField] private StatModifier[] modifiers;
        
        private List<Tower> affectedTowers = new List<Tower>();

        private void Start()
        {
            /* TODO: Find and buff towers in radius */
        }

        private void Update()
        {
            /* TODO: Update affected towers (in case new towers built) */
        }

        public GameObject GameObject => gameObject;
        public string GetDisplayName() => buildingData.buildingName;
        public Sprite GetIcon() => buildingData.icon;
        public string GetDescription() => $"Buffing {affectedTowers.Count} towers";
        public void OnSelected() { }
        public void OnDeselected() { }
    }

    #endregion

    #region ═══════════════════════════════════════════════════════════════
    #region BEACON VARIANTS - ALL SHAPES
    #endregion
    #region ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Base beacon class
    /// </summary>
    public abstract class Beacon : MonoBehaviour, ISelectable
    {
        #region Inspector Fields
        [SerializeField] protected BeaconData data;
        [SerializeField] protected GameObject rangeIndicator;
        #endregion

        #region Protected Fields
        protected List<Tower> affectedTowers = new List<Tower>();
        protected List<Extractor> affectedExtractors = new List<Extractor>();
        protected List<Processor> affectedProcessors = new List<Processor>();
        #endregion

        #region Properties
        public BeaconData Data => data;
        public GameObject GameObject => gameObject;
        #endregion

        protected virtual void Start()
        {
            /* TODO: Find and apply effects to buildings in range */
            FindAndApplyEffects();
        }

        protected virtual void Update()
        {
            /* TODO: Periodically check for new buildings */
        }

        protected abstract bool IsInRange(Vector3 position);

        protected virtual void FindAndApplyEffects()
        {
            /* TODO:
             * 1. Find all buildings of affected categories
             * 2. Check if in range via IsInRange()
             * 3. Apply stat modifiers
             * 4. Add to affected lists
             */
        }

        protected virtual void RemoveEffects()
        {
            /* TODO:
             * Remove stat modifiers from all affected buildings
             */
        }

        protected virtual void OnDestroy()
        {
            RemoveEffects();
        }

        #region ISelectable
        public string GetDisplayName() => data.buildingName;
        public Sprite GetIcon() => data.icon;
        public string GetDescription() => $"Buffing {affectedTowers.Count + affectedExtractors.Count + affectedProcessors.Count} buildings";
        public void OnSelected() { if (rangeIndicator) rangeIndicator.SetActive(true); }
        public void OnDeselected() { if (rangeIndicator) rangeIndicator.SetActive(false); }
        #endregion
    }

    /// <summary>
    /// Circle beacon - affects buildings in circular radius
    /// </summary>
    public class CircleBeacon : Beacon
    {
        protected override bool IsInRange(Vector3 position)
        {
            /* TODO:
             * Check if distance to position <= data.effectRadius
             */
            return Vector3.Distance(transform.position, position) <= data.effectRadius;
        }
    }

    /// <summary>
    /// Cone beacon - affects buildings in cone shape (directional)
    /// </summary>
    public class ConeBeacon : Beacon
    {
        [SerializeField] private float coneAngle = 90f;  // Total angle of cone
        [SerializeField] private Transform coneDirection;

        protected override bool IsInRange(Vector3 position)
        {
            /* TODO:
             * 1. Check if within effectRadius
             * 2. Calculate angle between coneDirection and position
             * 3. Return true if angle <= coneAngle/2
             */
            
            Vector3 dirToTarget = (position - transform.position).normalized;
            float angle = Vector3.Angle(coneDirection.forward, dirToTarget);
            float distance = Vector3.Distance(transform.position, position);
            
            return distance <= data.effectRadius && angle <= coneAngle / 2f;
        }
    }

    /// <summary>
    /// Squared beacon - affects buildings in square area
    /// </summary>
    public class SquaredBeacon : Beacon
    {
        [SerializeField] private v2 squareSize = (5, 5);

        protected override bool IsInRange(Vector3 position)
        {
            /* TODO:
             * Check if position is within square bounds
             * Square centered on beacon position
             */
            
            Vector3 localPos = position - transform.position;
            return Mathf.Abs(localPos.x) <= squareSize.x / 2f &&
                   Mathf.Abs(localPos.z) <= squareSize.y / 2f;
        }
    }

    /// <summary>
    /// Beacon_01 - First beacon type (specific implementation from TowerFactory)
    /// </summary>
    public class Beacon_01 : CircleBeacon
    {
        // Specific behavior for Beacon_01 type
        protected override void Start()
        {
            base.Start();
            /* TODO: Any beacon-01 specific initialization */
        }
    }

    /// <summary>
    /// Beacon_02 - Second beacon type
    /// </summary>
    public class Beacon_02 : CircleBeacon
    {
        // Specific behavior for Beacon_02 type
        protected override void Start()
        {
            base.Start();
            /* TODO: Any beacon-02 specific initialization */
        }
    }

    #endregion

    #region ═══════════════════════════════════════════════════════════════
    #region DETAILED STAT SYSTEM
    #endregion
    #region ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Stat configuration ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "NewStatConfig", menuName = "TowerFactory/Stats/Stat Config")]
    public class StatConfig : ScriptableObject
    {
        public string statName;
        public float baseValue;
        public float minValue = float.MinValue;
        public float maxValue = float.MaxValue;
        public bool canBeNegative = false;
    }

    /// <summary>
    /// Individual stat with modifiers
    /// </summary>
    [System.Serializable]
    public class Stat
    {
        public string statName;
        public float baseValue;
        public float minValue = float.MinValue;
        public float maxValue = float.MaxValue;

        private List<StatModifier> modifiers = new List<StatModifier>();
        private float cachedFinalValue;
        private bool isDirty = true;

        public float Value
        {
            get
            {
                if (isDirty)
                {
                    cachedFinalValue = CalculateFinalValue();
                    isDirty = false;
                }
                return cachedFinalValue;
            }
        }

        public Stat(string name, float baseVal)
        {
            statName = name;
            baseValue = baseVal;
        }

        public void AddModifier(StatModifier modifier)
        {
            /* TODO:
             * 1. Add to modifiers list
             * 2. Sort by priority
             * 3. Mark dirty
             */
            modifiers.Add(modifier);
            modifiers.Sort((a, b) => a.priority.CompareTo(b.priority));
            isDirty = true;
        }

        public void RemoveModifier(StatModifier modifier)
        {
            /* TODO:
             * 1. Remove from list
             * 2. Mark dirty
             */
            modifiers.Remove(modifier);
            isDirty = true;
        }

        public void RemoveAllModifiers()
        {
            modifiers.Clear();
            isDirty = true;
        }

        private float CalculateFinalValue()
        {
            /* TODO:
             * 1. Start with baseValue
             * 2. Apply Add modifiers (sum all Add operations)
             * 3. Apply Multiply modifiers (product of all Multiply operations)
             * 4. Check for Override modifiers (last Override wins)
             * 5. Clamp to min/max
             */
            
            float finalValue = baseValue;
            float additiveSum = 0f;
            float multiplicativeProduct = 1f;
            float? overrideValue = null;

            foreach (var mod in modifiers)
            {
                switch (mod.operation)
                {
                    case ModifierOperation.Add:
                        additiveSum += mod.value;
                        break;
                    case ModifierOperation.Multiply:
                        multiplicativeProduct *= (1f + mod.value);
                        break;
                    case ModifierOperation.Override:
                        overrideValue = mod.value;
                        break;
                }
            }

            if (overrideValue.HasValue)
            {
                finalValue = overrideValue.Value;
            }
            else
            {
                finalValue = (baseValue + additiveSum) * multiplicativeProduct;
            }

            return Mathf.Clamp(finalValue, minValue, maxValue);
        }
    }

    /// <summary>
    /// UI bar for displaying stats (health, mana, etc.)
    /// </summary>
    public class StatBar : MonoBehaviour
    {
        #region Inspector Fields
        [SerializeField] private Image fillImage;
        [SerializeField] private TextMeshProUGUI valueText;
        [SerializeField] private Gradient colorGradient;
        [SerializeField] private bool showText = true;
        #endregion

        #region Private Fields
        private float currentValue;
        private float maxValue;
        #endregion

        public void SetValue(float current, float max)
        {
            /* TODO:
             * 1. Store values
             * 2. Calculate fill amount (current / max)
             * 3. Update fillImage.fillAmount
             * 4. Evaluate color from gradient
             * 5. Update text if showText
             */
            
            currentValue = current;
            maxValue = max;
            
            float fillAmount = maxValue > 0 ? currentValue / maxValue : 0f;
            fillImage.fillAmount = fillAmount;
            fillImage.color = colorGradient.Evaluate(fillAmount);
            
            if (showText && valueText != null)
            {
                valueText.text = $"{Mathf.RoundToInt(currentValue)}/{Mathf.RoundToInt(maxValue)}";
            }
        }

        public void AnimateChange(float newCurrent, float duration = 0.3f)
        {
            /* TODO: Smooth animation from current to new value */
            StartCoroutine(AnimateChangeCoroutine(newCurrent, duration));
        }

        private IEnumerator AnimateChangeCoroutine(float target, float duration)
        {
            /* TODO:
             * Lerp currentValue to target over duration
             * Update bar each frame
             */
            yield return null;
        }
    }

    /// <summary>
    /// Enum for stat types
    /// </summary>
    public enum EStats
    {
        Health,
        Armor,
        Shield,
        Damage,
        Range,
        FireRate,
        MoveSpeed,
        AttackSpeed,
        CritChance,
        CritDamage,
        // Add more as needed
    }

    #endregion

    #region ═══════════════════════════════════════════════════════════════
    #region UI COMPONENTS - ALL SELECTABLE UI VARIANTS
    #endregion
    #region ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Base UI for selectable objects
    /// </summary>
    public abstract class SelectableUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] protected GameObject panel;
        [SerializeField] protected TextMeshProUGUI titleText;
        [SerializeField] protected Image iconImage;
        [SerializeField] protected TextMeshProUGUI descriptionText;

        protected ISelectable currentSelection;

        protected virtual void OnEnable()
        {
            GameEvents.OnObjectSelected += OnObjectSelected;
            GameEvents.OnObjectDeselected += OnObjectDeselected;
        }

        protected virtual void OnDisable()
        {
            GameEvents.OnObjectSelected -= OnObjectSelected;
            GameEvents.OnObjectDeselected -= OnObjectDeselected;
        }

        protected virtual void OnObjectSelected(ISelectable selectable)
        {
            /* TODO:
             * If selectable is correct type:
             * - Show panel
             * - Update UI elements
             */
        }

        protected virtual void OnObjectDeselected()
        {
            /* TODO: Hide panel */
            if (panel) panel.SetActive(false);
        }

        protected abstract void UpdateUI();
    }

    /// <summary>
    /// UI for selected towers
    /// </summary>
    public class SelectableUI_tower : SelectableUI
    {
        [Header("Tower Specific")]
        [SerializeField] private StatBar healthBar;
        [SerializeField] private TextMeshProUGUI damageText;
        [SerializeField] private TextMeshProUGUI rangeText;
        [SerializeField] private TextMeshProUGUI fireRateText;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private Button sellButton;

        private Tower selectedTower;

        protected override void OnObjectSelected(ISelectable selectable)
        {
            if (selectable.GameObject.TryGetComponent<Tower>(out var tower))
            {
                selectedTower = tower;
                currentSelection = selectable;
                UpdateUI();
                panel.SetActive(true);
            }
        }

        protected override void UpdateUI()
        {
            /* TODO:
             * Update all tower-specific UI elements
             * - Name, icon, description
             * - Health bar (if tower has health)
             * - Stats (damage, range, fire rate)
             * - Upgrade button (enabled if can upgrade)
             * - Sell button
             */
        }

        public void OnUpgradeClicked()
        {
            /* TODO:
             * Upgrade tower if possible
             */
        }

        public void OnSellClicked()
        {
            /* TODO:
             * Sell tower
             */
        }
    }

    /// <summary>
    /// UI for selected processors
    /// </summary>
    public class SelectableUI_processor : SelectableUI
    {
        [Header("Processor Specific")]
        [SerializeField] private Image recipeIcon;
        [SerializeField] private TextMeshProUGUI recipeNameText;
        [SerializeField] private Image progressBar;
        [SerializeField] private Transform inputsContainer;
        [SerializeField] private Transform outputsContainer;
        [SerializeField] private Button changeRecipeButton;

        private Processor selectedProcessor;

        protected override void OnObjectSelected(ISelectable selectable)
        {
            if (selectable.GameObject.TryGetComponent<Processor>(out var processor))
            {
                selectedProcessor = processor;
                UpdateUI();
                panel.SetActive(true);
            }
        }

        protected override void UpdateUI()
        {
            /* TODO:
             * Update processor UI:
             * - Current recipe
             * - Crafting progress
             * - Input buffer status
             * - Output buffer status
             */
        }

        private void Update()
        {
            if (selectedProcessor != null && selectedProcessor.IsCrafting)
            {
                /* TODO: Update progress bar */
                progressBar.fillAmount = selectedProcessor.CraftingProgress;
            }
        }
    }

    /// <summary>
    /// UI for selected extractors
    /// </summary>
    public class SelectableUI_extractor : SelectableUI
    {
        [Header("Extractor Specific")]
        [SerializeField] private Image resourceIcon;
        [SerializeField] private TextMeshProUGUI resourceNameText;
        [SerializeField] private TextMeshProUGUI extractionRateText;
        [SerializeField] private TextMeshProUGUI resourceRemainingText;

        private Extractor selectedExtractor;

        protected override void UpdateUI()
        {
            /* TODO:
             * Show extracted resource type
             * Show extraction rate
             * Show resource node remaining amount (if applicable)
             */
        }
    }

    /// <summary>
    /// UI for selected obelisks
    /// </summary>
    public class SelectableUI_obelisk : SelectableUI
    {
        [SerializeField] private TextMeshProUGUI affectedCountText;
        [SerializeField] private Transform modifiersContainer;

        protected override void UpdateUI()
        {
            /* TODO:
             * Show number of affected buildings
             * Show active modifiers
             */
        }
    }

    /// <summary>
    /// UI for selected chests
    /// </summary>
    public class SelectableUI_chest : SelectableUI
    {
        [SerializeField] private Button openButton;
        [SerializeField] private Transform contentsContainer;

        protected override void UpdateUI()
        {
            /* TODO:
             * Show chest contents
             * Enable/disable open button based on state
             */
        }
    }

    /// <summary>
    /// UI for selected golden coins chest
    /// </summary>
    public class SelectableUI_goldenCoinsChest : SelectableUI_chest
    {
        [SerializeField] private TextMeshProUGUI goldAmountText;

        protected override void UpdateUI()
        {
            base.UpdateUI();
            /* TODO: Show gold amount */
        }
    }

    /// <summary>
    /// UI for selected crystal altar
    /// </summary>
    public class SelectableUI_crystalAlatar : SelectableUI
    {
        [SerializeField] private TextMeshProUGUI crystalCountText;
        [SerializeField] private Image progressBar;

        protected override void UpdateUI()
        {
            /* TODO:
             * Show crystal count / required
             * Show progress bar
             */
        }
    }

    /// <summary>
    /// UI for selected crystal finder
    /// </summary>
    public class SelectableUI_crystalFinder : SelectableUI
    {
        [SerializeField] private TextMeshProUGUI detectionCountText;

        protected override void UpdateUI()
        {
            /* TODO: Show number of crystals detected */
        }
    }

    /// <summary>
    /// UI for selected enemy towers
    /// </summary>
    public class SelectableUI_enemyTower : SelectableUI
    {
        [SerializeField] private StatBar healthBar;
        [SerializeField] private TextMeshProUGUI damageText;

        protected override void UpdateUI()
        {
            /* TODO:
             * Show enemy tower health
             * Show damage it deals
             */
        }
    }

    #endregion

    #region ═══════════════════════════════════════════════════════════════
    #region TOOLTIP SYSTEM - ALL VARIANTS
    #endregion
    #region ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Base tooltip UI
    /// </summary>
    public abstract class TooltipUI : MonoBehaviour
    {
        [SerializeField] protected RectTransform tooltipRect;
        [SerializeField] protected CanvasGroup canvasGroup;
        [SerializeField] protected float fadeSpeed = 5f;

        protected bool isVisible = false;

        public virtual void Show(Vector3 position)
        {
            /* TODO:
             * 1. Position tooltip near cursor/object
             * 2. Fade in canvasGroup
             * 3. Set isVisible = true
             */
        }

        public virtual void Hide()
        {
            /* TODO:
             * 1. Fade out canvasGroup
             * 2. Set isVisible = false
             */
        }

        protected virtual void PositionTooltip(Vector3 worldPosition)
        {
            /* TODO:
             * Convert world position to screen space
             * Position tooltip rect
             * Keep within screen bounds
             */
        }
    }

    /// <summary>
    /// Tooltip for towers
    /// </summary>
    public class TooltipUI_tower : TooltipUI
    {
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI statsText;
        [SerializeField] private Transform modifiersContainer;

        public void SetTowerData(Tower tower)
        {
            /* TODO:
             * Display tower name, stats, active modifiers
             */
        }
    }

    /// <summary>
    /// Tooltip for processors
    /// </summary>
    public class TooltipUI_processor : TooltipUI
    {
        [SerializeField] private TextMeshProUGUI recipeText;
        [SerializeField] private Transform inputsContainer;
        [SerializeField] private Transform outputsContainer;

        public void SetProcessorData(Processor processor)
        {
            /* TODO:
             * Display recipe, inputs, outputs
             */
        }
    }

    /// <summary>
    /// Tooltip for sources
    /// </summary>
    public class TooltipUI_source : TooltipUI
    {
        [SerializeField] private Image resourceIcon;
        [SerializeField] private TextMeshProUGUI generationRateText;

        public void SetSourceData(Source source)
        {
            /* TODO:
             * Display generated resource and rate
             */
        }
    }

    /// <summary>
    /// Tooltip for recipes
    /// </summary>
    public class TooltipUI_recipe : TooltipUI
    {
        [SerializeField] private TextMeshProUGUI recipeNameText;
        [SerializeField] private Transform ingredientsContainer;
        [SerializeField] private Transform resultsContainer;
        [SerializeField] private TextMeshProUGUI craftTimeText;

        public void SetRecipeData(Recipe recipe)
        {
            /* TODO:
             * Display recipe name, ingredients, results, craft time
             */
        }
    }

    /// <summary>
    /// Simple text tooltip
    /// </summary>
    public class TooltipUI_text : TooltipUI
    {
        [SerializeField] private TextMeshProUGUI contentText;

        public void SetText(string text)
        {
            contentText.text = text;
        }
    }

    /// <summary>
    /// Detailed text tooltip with title
    /// </summary>
    public class TooltipUI_detailedText : TooltipUI_text
    {
        [SerializeField] private TextMeshProUGUI titleText;

        public void SetContent(string title, string content)
        {
            titleText.text = title;
            SetText(content);
        }
    }

    /// <summary>
    /// Hotbar tooltip (for building selection)
    /// </summary>
    public class TooltipUI_hotbar : TooltipUI
    {
        [SerializeField] private TextMeshProUGUI buildingNameText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Transform costContainer;
        [SerializeField] private TooltipHotbar_CostUI costUIPrefab;

        public void SetBuildingData(BuildingData building)
        {
            /* TODO:
             * Display building name, description, cost
             * Create cost UI elements
             */
        }
    }

    /// <summary>
    /// Cost UI element for tooltips
    /// </summary>
    public class TooltipHotbar_CostUI : MonoBehaviour
    {
        [SerializeField] private Image resourceIcon;
        [SerializeField] private TextMeshProUGUI amountText;

        public void SetCost(ResourceType type, int amount, bool canAfford)
        {
            /* TODO:
             * Set icon and amount
             * Color red if can't afford
             */
        }
    }

    /// <summary>
    /// Recipe resource display in tooltip
    /// </summary>
    public class TooltipUI_recipe_resource : MonoBehaviour
    {
        [SerializeField] private Image resourceIcon;
        [SerializeField] private TextMeshProUGUI amountText;

        public void SetResource(ResourceType type, int amount)
        {
            /* TODO: Set icon and amount text */
        }
    }

    #endregion

    #region ═══════════════════════════════════════════════════════════════
    #region TOOLTIP COMPONENTS - Attachable Components
    #endregion
    #region ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Base tooltip trigger component
    /// </summary>
    public abstract class TooltipComponent : MonoBehaviour
    {
        [SerializeField] protected bool showOnHover = true;
        [SerializeField] protected float showDelay = 0.5f;

        protected bool isHovering = false;
        protected float hoverTimer = 0f;

        protected virtual void OnMouseEnter()
        {
            isHovering = true;
            hoverTimer = 0f;
        }

        protected virtual void OnMouseExit()
        {
            isHovering = false;
            hoverTimer = 0f;
            HideTooltip();
        }

        protected virtual void Update()
        {
            if (isHovering && showOnHover)
            {
                hoverTimer += Time.deltaTime;
                if (hoverTimer >= showDelay)
                {
                    ShowTooltip();
                }
            }
        }

        protected abstract void ShowTooltip();
        protected abstract void HideTooltip();
    }

    public class TooltipComponent_tower : TooltipComponent
    {
        private Tower tower;

        private void Awake()
        {
            tower = GetComponent<Tower>();
        }

        protected override void ShowTooltip()
        {
            /* TODO: Show tower tooltip via TooltipManager */
        }

        protected override void HideTooltip()
        {
            /* TODO: Hide tooltip */
        }
    }

    public class TooltipComponent_processor : TooltipComponent
    {
        private Processor processor;
        
        private void Awake()
        {
            processor = GetComponent<Processor>();
        }

        protected override void ShowTooltip()
        {
            /* TODO: Show processor tooltip */
        }

        protected override void HideTooltip()
        {
            /* TODO: Hide tooltip */
        }
    }

    public class TooltipComponent_source : TooltipComponent
    {
        private Source source;

        protected override void ShowTooltip()
        {
            /* TODO: Show source tooltip */
        }

        protected override void HideTooltip() { }
    }

    public class TooltipComponent_recipe : TooltipComponent
    {
        [SerializeField] private Recipe recipe;

        protected override void ShowTooltip()
        {
            /* TODO: Show recipe tooltip */
        }

        protected override void HideTooltip() { }
    }

    public class TooltipComponent_text : TooltipComponent
    {
        [SerializeField] [TextArea] private string tooltipText;

        protected override void ShowTooltip()
        {
            /* TODO: Show text tooltip */
        }

        protected override void HideTooltip() { }
    }

    public class TooltipComponent_detailedText : TooltipComponent_text
    {
        [SerializeField] private string title;
        
        protected override void ShowTooltip()
        {
            /* TODO: Show detailed text tooltip with title */
        }
    }

    public class TooltipComponent_hotbar : TooltipComponent
    {
        [SerializeField] private BuildingData buildingData;

        protected override void ShowTooltip()
        {
            /* TODO: Show hotbar tooltip */
        }

        protected override void HideTooltip() { }
    }

    public class TooltipComponent_playerUpgrade_building : TooltipComponent
    {
        [SerializeField] private PlayerUpgrade upgrade;

        protected override void ShowTooltip()
        {
            /* TODO: Show upgrade tooltip */
        }

        protected override void HideTooltip() { }
    }

    public class TooltipComponent_pooled : TooltipComponent
    {
        // Special tooltip component for pooled objects
        protected override void ShowTooltip() { }
        protected override void HideTooltip() { }
    }

    #endregion
}
