using System.Collections.Generic;
using UnityEngine;
using SPACE_UTIL;

/*
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
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
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
*/

/// <summary>
/// Bezier path utility for curved belts.
/// </summary>
public static class BezierUtility
{
    /// <summary>
    /// Get point on cubic bezier curve.
    /// </summary>
    public static Vector3 GetBezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;

        Vector3 p = uuu * p0;
        p += 3 * uu * t * p1;
        p += 3 * u * tt * p2;
        p += ttt * p3;

        return p;
    }

    /// <summary>
    /// Calculate approximate arc length of bezier curve.
    /// </summary>
    public static float CalculateBezierLength(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, int segments = 20)
    {
        float length = 0f;
        Vector3 previousPoint = p0;

        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector3 currentPoint = GetBezierPoint(p0, p1, p2, p3, t);
            length += Vector3.Distance(previousPoint, currentPoint);
            previousPoint = currentPoint;
        }

        return length;
    }

    /// <summary>
    /// Get normalized speed to maintain 1 unit/second along path.
    /// </summary>
    public static float GetNormalizedSpeed(float pathLength)
    {
        return pathLength; // Speed multiplier to traverse in correct time
    }
}

/// <summary>
/// Debug utilities for visualization and testing.
/// </summary>
public static class DebugUtility
{
    /// <summary>
    /// Draw a grid cell outline in world space.
    /// </summary>
    public static void DrawGridCell(Vector3 center, float size, Color color, float duration = 0f)
    {
        Vector3 halfSize = new Vector3(size * 0.5f, 0, size * 0.5f);
        
        Vector3 bl = center - halfSize;
        Vector3 br = center + new Vector3(halfSize.x, 0, -halfSize.z);
        Vector3 tl = center + new Vector3(-halfSize.x, 0, halfSize.z);
        Vector3 tr = center + halfSize;

        Debug.DrawLine(bl, br, color, duration);
        Debug.DrawLine(br, tr, color, duration);
        Debug.DrawLine(tr, tl, color, duration);
        Debug.DrawLine(tl, bl, color, duration);
    }

    /// <summary>
    /// Draw arrow in world space.
    /// </summary>
    public static void DrawArrow(Vector3 start, Vector3 end, Color color, float duration = 0f)
    {
        Debug.DrawLine(start, end, color, duration);

        Vector3 direction = (end - start).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;

        Vector3 arrowTip = end - direction * 0.2f;
        Debug.DrawLine(end, arrowTip + right * 0.1f, color, duration);
        Debug.DrawLine(end, arrowTip - right * 0.1f, color, duration);
    }

    /// <summary>
    /// Log building info to console.
    /// </summary>
    public static void LogBuildingInfo(Building building)
    {
        if (building == null)
        {
            Debug.Log("Building is null");
            return;
        }

        string info = $"Building: {building.buildingData?.buildingName}\n";
        info += $"Position: {building.GridPosition}\n";
        info += $"Size: {building.Size}\n";
        info += $"Placed: {building.PlacementComponent?.isPlaced}\n";

        if (building is ConveyorBelt belt)
        {
            info += $"Belt Type: {building.buildingData?.beltType}\n";
            info += $"Items on belt: {belt.Items.Count}\n";
            info += $"Belt Group: {(belt.BeltGroup != null ? "Yes" : "No")}\n";
        }

        Debug.Log(info);
    }
}

/// <summary>
/// Extension methods for Unity types.
/// </summary>
public static class UnityExtensions
{
    /// <summary>
    /// Get XZ component of Vector3 as Vector2.
    /// </summary>
    public static Vector2 XZ(this Vector3 vector)
    {
        return new Vector2(vector.x, vector.z);
    }

    /// <summary>
    /// Check if Vector2 is within bounds.
    /// </summary>
    public static bool IsInBounds(this Vector2 pos, Vector2 min, Vector2 max)
    {
        return pos.x >= min.x && pos.x <= max.x &&
               pos.y >= min.y && pos.y <= max.y;
    }

    /// <summary>
    /// Get distance between two v2 positions.
    /// </summary>
    public static float Distance(this v2 a, v2 b)
    {
        int dx = b.x - a.x;
        int dy = b.y - a.y;
        return Mathf.Sqrt(dx * dx + dy * dy);
    }
}
/*
/// <summary>
/// Stats component for buildings with stats (speed, range, etc.).
/// Optional component for buildings that need dynamic stats.
/// </summary>
public class StatsComponent : MonoBehaviour
{
    [System.Serializable]
    public class Stat
    {
        public string statName;
        public float baseValue;
        public float currentValue;
        public float multiplier = 1f;

        public void RecalculateValue()
        {
            currentValue = baseValue * multiplier;
        }
    }

    [SerializeField] private List<Stat> stats = new List<Stat>();

    public float GetStat(string statName)
    {
        Stat stat = stats.Find(s => s.statName == statName);
        return stat?.currentValue ?? 0f;
    }

    public void SetStatBase(string statName, float value)
    {
        Stat stat = stats.Find(s => s.statName == statName);
        if (stat != null)
        {
            stat.baseValue = value;
            stat.RecalculateValue();
        }
    }

    public void SetStatMultiplier(string statName, float multiplier)
    {
        Stat stat = stats.Find(s => s.statName == statName);
        if (stat != null)
        {
            stat.multiplier = multiplier;
            stat.RecalculateValue();
        }
    }

    public void AddStat(string statName, float baseValue)
    {
        if (stats.Find(s => s.statName == statName) == null)
        {
            Stat newStat = new Stat
            {
                statName = statName,
                baseValue = baseValue,
                currentValue = baseValue,
                multiplier = 1f
            };
            stats.Add(newStat);
        }
    }
}
*/