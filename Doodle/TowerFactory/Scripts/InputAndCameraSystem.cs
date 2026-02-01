using System;
using UnityEngine;
using UnityEngine.InputSystem;
using SPACE_UTIL;

/*
/// <summary>
/// Input system managing two modes: Normal and Placement.
/// Handles building placement, rotation, removal, and interaction.
/// </summary>
public class InputSystemTowerFactory : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Camera mainCamera;

    [Header("Layers")]
    [SerializeField] private LayerMask groundLayerMask;
    [SerializeField] private LayerMask buildingLayerMask;

    [Header("Settings")]
    [SerializeField] private float raycastMaxDistance = 1000f;

    // Input mode
    private InputMode currentMode = InputMode.Normal;
    private Building ghostBuilding; // Preview building in placement mode
    private BuildingData selectedBuildingData;

    // Hover tracking
    private Building currentHoveredBuilding;

    private static InputSystemTowerFactory instance;
    public static InputSystemTowerFactory Instance => instance;

    public enum InputMode
    {
        Normal,     // Default mode - hover, select, interact
        Placement   // Building placement mode
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    void Update()
    {
        if (currentMode == InputMode.Normal)
        {
            UpdateNormalMode();
        }
        else if (currentMode == InputMode.Placement)
        {
            UpdatePlacementMode();
        }
    }

    #region Normal Mode
    private void UpdateNormalMode()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // Raycast to buildings
        if (Physics.Raycast(ray, out hit, raycastMaxDistance, buildingLayerMask))
        {
            Building building = hit.collider.GetComponentInParent<Building>();
            
            if (building != currentHoveredBuilding)
            {
                // Unhighlight previous
                if (currentHoveredBuilding != null)
                {
                    currentHoveredBuilding.DisableHighlight();
                }

                // Highlight new
                currentHoveredBuilding = building;
                if (currentHoveredBuilding != null)
                {
                    currentHoveredBuilding.EnableHighlight();
                }
            }

            // Handle input on hovered building
            if (currentHoveredBuilding != null)
            {
                // F key - remove building
                if (Input.GetKeyDown(KeyCode.F))
                {
                    RemoveBuilding(currentHoveredBuilding);
                }

                // C key - copy building
                if (Input.GetKeyDown(KeyCode.C))
                {
                    CopyBuilding(currentHoveredBuilding);
                }

                // Left click - select building (for future features)
                if (Input.GetMouseButtonDown(0))
                {
                    SelectBuilding(currentHoveredBuilding);
                }
            }
        }
        else
        {
            // No building hovered
            if (currentHoveredBuilding != null)
            {
                currentHoveredBuilding.DisableHighlight();
                currentHoveredBuilding = null;
            }
        }
    }

    private void RemoveBuilding(Building building)
    {
        if (building == null || building.PlacementComponent == null)
            return;

        // Refund resources
        if (ResourceManager.Instance != null && building.buildingData != null)
        {
            ResourceManager.Instance.RefundResources(building.buildingData, 0.5f);
        }

        // Unplace and destroy
        building.PlacementComponent.Unplace();
        Destroy(building.gameObject);

        currentHoveredBuilding = null;
    }

    private void CopyBuilding(Building building)
    {
        if (building == null || building.buildingData == null)
            return;

        // Enter placement mode with same building type
        EnterPlacementMode(building.buildingData);
    }

    private void SelectBuilding(Building building)
    {
        // For future features: show building info UI, allow upgrades, etc.
        Debug.Log($"Selected building: {building.buildingData?.buildingName}");
    }
    #endregion

    #region Placement Mode
    /// <summary>
    /// Enter placement mode with selected building.
    /// Called from shop UI or when copying a building.
    /// </summary>
    public void EnterPlacementMode(BuildingData buildingData)
    {
        if (buildingData == null || buildingData.prefab == null)
            return;

        // Check if can afford
        if (ResourceManager.Instance != null && !ResourceManager.Instance.CanAfford(buildingData))
        {
            Debug.Log($"Cannot afford {buildingData.buildingName}");
            return;
        }

        selectedBuildingData = buildingData;
        currentMode = InputMode.Placement;

        // Create ghost building
        GameObject ghostObj = Instantiate(buildingData.prefab);
        ghostBuilding = ghostObj.GetComponent<Building>();
        
        if (ghostBuilding != null && ghostBuilding.PlacementComponent != null)
        {
            ghostBuilding.PlacementComponent.isGhost = true;
            
            // Make ghost semi-transparent (would need material setup)
            SetGhostMaterial(ghostBuilding);
        }

        Debug.Log($"Entered placement mode for: {buildingData.buildingName}");
    }

    private void UpdatePlacementMode()
    {
        if (ghostBuilding == null)
        {
            ExitPlacementMode();
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // Raycast to ground to find placement position
        if (Physics.Raycast(ray, out hit, raycastMaxDistance, groundLayerMask))
        {
            GridSystem grid = GridSystem.Instance;
            if (grid != null)
            {
                // Convert hit point to grid position
                v2 gridPos = grid.WorldToGrid(hit.point);
                
                // Update ghost position
                ghostBuilding.PlacementComponent.SetGridPosition(gridPos);

                // Check if placement is valid
                bool isValid = ghostBuilding.PlacementComponent.CanPlaceAtCurrentPosition();

                // Check if can afford
                if (isValid && ResourceManager.Instance != null)
                {
                    isValid = ResourceManager.Instance.CanAfford(selectedBuildingData);
                }

                // Update indicator
                if (isValid)
                {
                    ghostBuilding.ShowValidPlacementIndicator();
                }
                else
                {
                    ghostBuilding.ShowInvalidPlacementIndicator();
                }

                // Left click - place building
                if (Input.GetMouseButtonDown(0) && isValid)
                {
                    PlaceBuilding();
                    return;
                }
            }
        }

        // R key - rotate
        if (Input.GetKeyDown(KeyCode.R))
        {
            RotateGhostBuilding();
        }

        // Right click or ESC - cancel
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            ExitPlacementMode();
        }
    }

    private void PlaceBuilding()
    {
        if (ghostBuilding == null || selectedBuildingData == null)
            return;

        // Spend resources
        if (ResourceManager.Instance != null)
        {
            if (!ResourceManager.Instance.SpendResources(selectedBuildingData))
            {
                Debug.Log("Cannot afford building!");
                return;
            }
        }

        // Place the building
        if (ghostBuilding.PlacementComponent.Place())
        {
            Debug.Log($"Placed: {selectedBuildingData.buildingName}");

            // Keep in placement mode for continuous placement
            // Create new ghost for next placement
            GameObject newGhostObj = Instantiate(selectedBuildingData.prefab);
            Building newGhost = newGhostObj.GetComponent<Building>();
            
            if (newGhost != null && newGhost.PlacementComponent != null)
            {
                newGhost.PlacementComponent.isGhost = true;
                newGhost.PlacementComponent.rotationIndex = ghostBuilding.PlacementComponent.rotationIndex;
                SetGhostMaterial(newGhost);
                
                ghostBuilding = newGhost;
            }
        }
        else
        {
            Debug.Log("Failed to place building!");
        }
    }

    private void RotateGhostBuilding()
    {
        if (ghostBuilding == null || ghostBuilding.PlacementComponent == null)
            return;

        ghostBuilding.PlacementComponent.Rotate();
    }

    private void ExitPlacementMode()
    {
        if (ghostBuilding != null)
        {
            Destroy(ghostBuilding.gameObject);
            ghostBuilding = null;
        }

        selectedBuildingData = null;
        currentMode = InputMode.Normal;

        Debug.Log("Exited placement mode");
    }

    private void SetGhostMaterial(Building building)
    {
        // Make ghost semi-transparent
        // This would require proper material setup with transparent shader
        // For now, just a placeholder

        Renderer[] renderers = building.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            foreach (var mat in renderer.materials)
            {
                Color color = mat.color;
                color.a = 0.5f;
                mat.color = color;
                
                // Would need to set material to transparent mode:
                // mat.SetFloat("_Mode", 3);
                // mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                // mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                // mat.SetInt("_ZWrite", 0);
                // mat.DisableKeyword("_ALPHATEST_ON");
                // mat.EnableKeyword("_ALPHABLEND_ON");
                // mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                // mat.renderQueue = 3000;
            }
        }
    }
    #endregion

    #region Public API for UI
    /// <summary>
    /// Called by shop UI when building is selected.
    /// </summary>
    public void OnBuildingSelectedFromShop(BuildingData buildingData)
    {
        EnterPlacementMode(buildingData);
    }

    /// <summary>
    /// Check current input mode.
    /// </summary>
    public InputMode GetCurrentMode() => currentMode;

    /// <summary>
    /// Force exit placement mode (called by UI cancel button).
    /// </summary>
    public void CancelPlacement()
    {
        if (currentMode == InputMode.Placement)
        {
            ExitPlacementMode();
        }
    }
    #endregion
}

/// <summary>
/// Simple camera controller for grid-based game.
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float edgeScrollBorder = 10f;

    [Header("Zoom")]
    [SerializeField] private float zoomSpeed = 5f;
    [SerializeField] private float minZoom = 5f;
    [SerializeField] private float maxZoom = 30f;

    [Header("Rotation")]
    [SerializeField] private float rotateSpeed = 100f;

    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void Update()
    {
        HandleMovement();
        HandleZoom();
        HandleRotation();
    }

    private void HandleMovement()
    {
        Vector3 moveDir = Vector3.zero;

        // WASD keys
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            moveDir += Vector3.forward;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            moveDir += Vector3.back;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            moveDir += Vector3.left;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            moveDir += Vector3.right;

        // Edge scrolling
        Vector3 mousePos = Input.mousePosition;
        if (mousePos.x < edgeScrollBorder)
            moveDir += Vector3.left;
        if (mousePos.x > Screen.width - edgeScrollBorder)
            moveDir += Vector3.right;
        if (mousePos.y < edgeScrollBorder)
            moveDir += Vector3.back;
        if (mousePos.y > Screen.height - edgeScrollBorder)
            moveDir += Vector3.forward;

        // Middle mouse button drag
        if (Input.GetMouseButton(2))
        {
            float h = -Input.GetAxis("Mouse X");
            float v = -Input.GetAxis("Mouse Y");
            moveDir += new Vector3(h, 0, v) * 2f;
        }

        if (moveDir != Vector3.zero)
        {
            transform.position += moveDir.normalized * moveSpeed * Time.deltaTime;
        }
    }

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            Vector3 pos = transform.position;
            pos.y -= scroll * zoomSpeed;
            pos.y = Mathf.Clamp(pos.y, minZoom, maxZoom);
            transform.position = pos;
        }
    }

    private void HandleRotation()
    {
        // Q and E keys to rotate camera
        if (Input.GetKey(KeyCode.Q))
        {
            transform.RotateAround(transform.position, Vector3.up, rotateSpeed * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.E))
        {
            transform.RotateAround(transform.position, Vector3.up, -rotateSpeed * Time.deltaTime);
        }
    }
}

	*/