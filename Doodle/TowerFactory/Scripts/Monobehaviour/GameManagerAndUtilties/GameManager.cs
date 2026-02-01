using System.Collections.Generic;
using UnityEngine;

using SPACE_UTIL;

/// <summary>
/// Main game manager coordinating all systems.
/// Manages game state, updates, and system initialization.
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("Systems")]
    [SerializeField] private GridSystem gridSystem;
    [SerializeField] private ConveyorBeltSystem beltSystem;
    [SerializeField] private ResourceManager resourceManager;
    [SerializeField] private InputSystemTowerFactory inputSystem;

    [Header("Game State")]
    [SerializeField] private bool isPaused = false;
    [SerializeField] private float gameSpeed = 1f;

    private List<Building> allBuildings = new List<Building>();
    private static GameManager instance;

    public static GameManager Instance => instance;
    public bool IsPaused => isPaused;
    public float GameSpeed => gameSpeed;

    void Awake()
    {
        instance = this;

        // Find systems if not assigned
        if (gridSystem == null)
            gridSystem = FindObjectOfType<GridSystem>();
        if (beltSystem == null)
            beltSystem = FindObjectOfType<ConveyorBeltSystem>();
        if (resourceManager == null)
            resourceManager = FindObjectOfType<ResourceManager>();
        if (inputSystem == null)
            inputSystem = FindObjectOfType<InputSystemTowerFactory>();
    }

    void Start()
    {
        InitializeGame();
    }

    void Update()
    {
        if (isPaused)
            return;

        float deltaTime = Time.deltaTime * gameSpeed;

        // Update all buildings that need per-frame updates
        UpdateAllBuildings(deltaTime);

        // Handle pause key
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TogglePause();
        }

        // Handle speed controls
        if (Input.GetKeyDown(KeyCode.Plus) || Input.GetKeyDown(KeyCode.Equals))
        {
            IncreaseGameSpeed();
        }
        if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.Underscore))
        {
            DecreaseGameSpeed();
        }
    }

    private void InitializeGame()
    {
        Debug.Log("Game initialized");

        // Find all existing buildings in scene
        Building[] existingBuildings = FindObjectsOfType<Building>();
        foreach (var building in existingBuildings)
        {
            RegisterBuilding(building);
        }
    }

    /// <summary>
    /// Register a building for updates.
    /// </summary>
    public void RegisterBuilding(Building building)
    {
        if (building != null && !allBuildings.Contains(building))
        {
            allBuildings.Add(building);
        }
    }

    /// <summary>
    /// Unregister a building.
    /// </summary>
    public void UnregisterBuilding(Building building)
    {
        allBuildings.Remove(building);
    }

    /// <summary>
    /// Update all buildings that need it (extractors, processors, etc.).
    /// </summary>
    private void UpdateAllBuildings(float deltaTime)
    {
        // Note: ConveyorBelts are updated by ConveyorBeltSystem
        // This is for other buildings that need updates
        foreach (var building in allBuildings)
        {
            if (building != null && !(building is ConveyorBelt))
            {
                building.UpdateBuilding(deltaTime);
            }
        }
    }

    #region Game Controls
    public void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : gameSpeed;
        Debug.Log(isPaused ? "Game Paused" : "Game Resumed");
    }

    public void SetPause(bool pause)
    {
        isPaused = pause;
        Time.timeScale = isPaused ? 0f : gameSpeed;
    }

    public void SetGameSpeed(float speed)
    {
        gameSpeed = Mathf.Clamp(speed, 0.25f, 4f);
        if (!isPaused)
        {
            Time.timeScale = gameSpeed;
        }
        Debug.Log($"Game Speed: {gameSpeed}x");
    }

    public void IncreaseGameSpeed()
    {
        if (gameSpeed < 1f)
            SetGameSpeed(1f);
        else
            SetGameSpeed(gameSpeed * 2f);
    }

    public void DecreaseGameSpeed()
    {
        SetGameSpeed(gameSpeed * 0.5f);
    }
    #endregion

    #region Utility Methods
    /// <summary>
    /// Get all buildings of specific type.
    /// </summary>
    public List<T> GetBuildingsOfType<T>() where T : Building
    {
        List<T> result = new List<T>();
        foreach (var building in allBuildings)
        {
            if (building is T typed)
            {
                result.Add(typed);
            }
        }
        return result;
    }

    /// <summary>
    /// Get building at grid position.
    /// </summary>
    public Building GetBuildingAt(v2 gridPos)
    {
        if (gridSystem != null)
        {
            return gridSystem.GetBuildingAt(gridPos);
        }
        return null;
    }
    #endregion
}
