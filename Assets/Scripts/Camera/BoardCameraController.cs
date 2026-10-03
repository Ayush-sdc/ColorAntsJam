using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

public enum CameraProjectionMode
{
    Orthographic,
    Perspective
}

/// <summary>
/// Closer-view camera controller for Slither Ants Jam with smooth pan freedom and pinch/scroll zoom.
/// Starts at a comfortable closer view with larger, clearer ants instead of forcing full-board fit.
/// Calculates dynamic pan limits from board bounds and current zoom, allowing horizontal and vertical
/// panning to bring every queue, hole, and pipe comfortably into the usable screen area below the HUD.
/// </summary>
[ExecuteAlways]
public class BoardCameraController : MonoBehaviour
{
    private static BoardCameraController instance;
    public static BoardCameraController Instance => instance;

    [Header("Camera Engine Switch")]
    [Tooltip("When true, Cinemachine controls board framing. When false, Main Camera is positioned directly.")]
    [SerializeField] private bool useCinemachine = false;

    [Header("Camera Projection & Angle")]
    [SerializeField] private CameraProjectionMode projectionMode = CameraProjectionMode.Orthographic;
    [Range(60f, 90f)]
    [SerializeField] private float cameraPitch = 88f;
    [Range(30f, 90f)]
    [SerializeField] private float perspectiveFov = 78f;
    [SerializeField] private float cameraHeight = 13.04f;

    [Header("Closer View & Zoom Configuration")]
    [Tooltip("Default zoom (orthographic size) configured for comfortable closer view with large clear ants")]
    [SerializeField] private float defaultZoom = 10.0f;
    [Tooltip("Minimum allowable zoom (closest view)")]
    [SerializeField] private float minZoom = 5.5f;
    [Tooltip("Maximum allowable zoom (farthest view)")]
    [SerializeField] private float maxZoom = 26.0f;
    [Tooltip("Zoom sensitivity multiplier for mouse wheel and mobile pinch gestures")]
    [SerializeField] private float zoomSensitivity = 1.0f;
    [SerializeField] private float currentZoom = 10.0f;

    [Header("Pan Settings & Movement Margins")]
    [Tooltip("Pan movement sensitivity multiplier")]
    [SerializeField] private float panSensitivity = 1.0f;
    [Tooltip("Extra horizontal movement margin beyond board bounds (world units)")]
    [SerializeField] private float panMarginX = 2.0f;
    [Tooltip("Extra vertical movement margin beyond board bounds (world units)")]
    [SerializeField] private float panMarginZ = 2.5f;

    [Header("Usable Viewport Below HUD")]
    [Tooltip("Bottom safe area margin in viewport Y (0 = screen bottom)")]
    [SerializeField] private float minUsableViewportY = 0.04f;
    [Tooltip("Top limit below HUD in viewport Y (1 = screen top)")]
    [SerializeField] private float maxUsableViewportY = 0.83f;
    [Tooltip("Horizontal safe area margin in viewport X")]
    [SerializeField] private float horizontalMargin = 0.04f;

    [Header("Camera References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private CinemachineBrain cinemachineBrain;
    [SerializeField] private CinemachineCamera gameplayCamera;

    [Header("Content Framing & Visual Padding")]
    [Tooltip("Small extra visual padding around outer geometry in world units")]
    [SerializeField] private float visualPadding = 0.35f;

    [Header("Runtime Pan & Zoom Diagnostics")]
    [SerializeField] private Bounds boardBounds;
    [SerializeField] private float visibleWidth;
    [SerializeField] private float visibleHeight;
    [SerializeField] private bool canPanX;
    [SerializeField] private bool canPanZ;
    [SerializeField] private float minCamX;
    [SerializeField] private float maxCamX;
    [SerializeField] private float minCamZ;
    [SerializeField] private float maxCamZ;
    [SerializeField] private Vector3 initialCamPos;
    [SerializeField] private Vector3 currentCamPos;
    [SerializeField] private bool isDragging;

    // Pointer drag tracking
    private Vector2 dragStartPointerPos;
    private Vector3 dragStartCamPos;
    private Level currentActiveLevel;
    private int lastScreenWidth = -1;
    private int lastScreenHeight = -1;

    // Public properties
    public bool UseCinemachine
    {
        get => useCinemachine;
        set
        {
            useCinemachine = value;
            ApplyEngineState();
        }
    }

    public CameraProjectionMode ProjectionMode => projectionMode;
    public float CameraPitch => cameraPitch;
    public float PerspectiveFov => perspectiveFov;
    public float CameraHeight => cameraHeight;
    public float DefaultZoom { get => defaultZoom; set => defaultZoom = value; }
    public float MinZoom { get => minZoom; set => minZoom = value; }
    public float MaxZoom { get => maxZoom; set => maxZoom = value; }
    public float ZoomSensitivity { get => zoomSensitivity; set => zoomSensitivity = value; }
    public float PanSensitivity { get => panSensitivity; set => panSensitivity = value; }
    public float PanMarginX { get => panMarginX; set => panMarginX = value; }
    public float PanMarginZ { get => panMarginZ; set => panMarginZ = value; }
    public float CurrentZoom => currentZoom;

    public bool CanPanX => canPanX;
    public bool CanPanZ => canPanZ;
    public Bounds BoardBounds => boardBounds;
    public float VisibleWidth => visibleWidth;
    public float VisibleHeight => visibleHeight;
    public float MinCamX => minCamX;
    public float MaxCamX => maxCamX;
    public float MinCamZ => minCamZ;
    public float MaxCamZ => maxCamZ;
    public Vector3 InitialCamPos => initialCamPos;
    public Vector3 CurrentCamPos => currentCamPos;
    public bool IsDragging => isDragging;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        EnsureReferences();
        ApplyEngineState();
    }

    private void OnEnable()
    {
        if (instance == null) instance = this;
        EnsureReferences();
        ApplyEngineState();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void Update()
    {
        // Handle runtime Game View resize or aspect ratio changes
        int curW = Screen.width;
        int curH = Screen.height;
        if (curW > 0 && curH > 0 && (curW != lastScreenWidth || curH != lastScreenHeight))
        {
            lastScreenWidth = curW;
            lastScreenHeight = curH;
            if (currentActiveLevel != null)
            {
                RecalculateFramingAndPanLimits();
            }
        }

        // Mouse wheel zoom in Editor / Desktop (blocked while any modal owns input)
        if (!IsModalBlocked())
        {
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                // Scroll up (positive) = zoom in (decrease ortho size)
                // Scroll down (negative) = zoom out (increase ortho size)
                AdjustZoom(-scroll * zoomSensitivity * 0.8f);
            }
        }
    }

    public void EnsureReferences()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                var camObj = GameObject.FindWithTag("MainCamera");
                if (camObj != null) mainCamera = camObj.GetComponent<Camera>();
            }
        }

        if (mainCamera != null && cinemachineBrain == null)
        {
            cinemachineBrain = mainCamera.GetComponent<CinemachineBrain>();
        }

        if (gameplayCamera == null)
        {
            var gpCamObj = GameObject.Find("GameplayCamera");
            if (gpCamObj != null)
            {
                gameplayCamera = gpCamObj.GetComponent<CinemachineCamera>();
            }
        }
    }

    public void ApplyEngineState()
    {
        EnsureReferences();

        if (cinemachineBrain != null)
        {
            cinemachineBrain.enabled = useCinemachine;
        }

        if (gameplayCamera != null)
        {
            gameplayCamera.gameObject.SetActive(useCinemachine);
        }
    }

    public bool IsModalBlocked()
    {
        var ui = GameUIManager.Instance ?? FindFirstObjectByType<GameUIManager>();
        if (ui != null && ui.IsAnyModalActive())
        {
            return true;
        }
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Alias for SetupCameraForLevel to preserve backwards compatibility.
    /// </summary>
    public void FrameLevel(Level level)
    {
        SetupCameraForLevel(level);
    }

    /// <summary>
    /// Configures the camera for the given level with a comfortable closer view.
    /// Resets zoom to defaultZoom (large, clear ants) and centers the board within the usable viewport below HUD.
    /// Calculates full pan limits allowing reach to every queue, hole, and pipe.
    /// </summary>
    public void SetupCameraForLevel(Level level)
    {
        if (level == null) return;
        EnsureReferences();
        ApplyEngineState();

        currentActiveLevel = level;
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

        ApplyFixedCameraOptics();

        // Reset to configured closer view zoom on level load
        currentZoom = defaultZoom;
        ApplyZoomToCamera(currentZoom);

        RecalculateFramingAndPanLimits();

        // Place camera at centered starting position
        currentCamPos = initialCamPos;
        ApplyCameraPosition(currentCamPos);

        Debug.Log($"[BoardCameraController] Level={level.LevelNumber} Zoom={currentZoom:F1} " +
                  $"BoardBounds={boardBounds.size.x:F1}x{boardBounds.size.z:F1} " +
                  $"PanX=[{minCamX:F1},{maxCamX:F1}] PanZ=[{minCamZ:F1},{maxCamZ:F1}] CloserStartingView=Active");
    }

    private void ApplyFixedCameraOptics()
    {
        if (mainCamera == null) return;

        mainCamera.orthographic = (projectionMode == CameraProjectionMode.Orthographic);
        if (!mainCamera.orthographic)
        {
            mainCamera.fieldOfView = perspectiveFov;
        }
        mainCamera.nearClipPlane = 0.1f;
        mainCamera.farClipPlane = 200f;

        Quaternion fixedRot = Quaternion.Euler(cameraPitch, 0f, 0f);
        mainCamera.transform.rotation = fixedRot;

        if (useCinemachine && gameplayCamera != null)
        {
            gameplayCamera.transform.rotation = fixedRot;
            gameplayCamera.Lens.ModeOverride = (projectionMode == CameraProjectionMode.Orthographic)
                ? LensSettings.OverrideModes.Orthographic
                : LensSettings.OverrideModes.Perspective;
            gameplayCamera.Lens.FieldOfView = perspectiveFov;
            gameplayCamera.Lens.NearClipPlane = 0.1f;
            gameplayCamera.Lens.FarClipPlane = 200f;
        }
    }

    private void ApplyZoomToCamera(float zoom)
    {
        if (mainCamera != null && mainCamera.orthographic)
        {
            mainCamera.orthographicSize = zoom;
        }

        if (useCinemachine && gameplayCamera != null)
        {
            gameplayCamera.Lens.OrthographicSize = zoom;
        }
    }

    /// <summary>
    /// Adjusts camera zoom by delta and dynamically recalculates pan clamps.
    /// Clamps zoom between minZoom and maxZoom.
    /// </summary>
    public void AdjustZoom(float zoomDelta)
    {
        if (IsModalBlocked()) return;

        float targetZoom = Mathf.Clamp(currentZoom + zoomDelta, minZoom, maxZoom);
        if (Mathf.Abs(targetZoom - currentZoom) > 0.001f)
        {
            SetZoom(targetZoom);
        }
    }

    /// <summary>
    /// Sets camera zoom directly, updating optics and pan clamp bounds.
    /// </summary>
    public void SetZoom(float newZoom)
    {
        currentZoom = Mathf.Clamp(newZoom, minZoom, maxZoom);
        ApplyZoomToCamera(currentZoom);

        RecalculateFramingAndPanLimits();

        // Clamp camera position to newly updated pan boundaries
        float actualMinX = Mathf.Min(minCamX, maxCamX);
        float actualMaxX = Mathf.Max(minCamX, maxCamX);
        float actualMinZ = Mathf.Min(minCamZ, maxCamZ);
        float actualMaxZ = Mathf.Max(minCamZ, maxCamZ);

        currentCamPos.x = Mathf.Clamp(currentCamPos.x, actualMinX, actualMaxX);
        currentCamPos.z = Mathf.Clamp(currentCamPos.z, actualMinZ, actualMaxZ);
        ApplyCameraPosition(currentCamPos);
    }

    private void RecalculateFramingAndPanLimits()
    {
        if (currentActiveLevel == null) return;

        // 1. Calculate complete playable level footprint
        CalculateBoardBounds(currentActiveLevel);

        // 2. Calculate dynamic pan limits for current zoom level
        UpdatePanLimitsForCurrentZoom();
    }

    private void CalculateBoardBounds(Level level)
    {
        int w = level.GridWidth > 0 ? level.GridWidth : 9;
        int h = level.GridHeight > 0 ? level.GridHeight : 6;
        float cs = level.CellSize > 0f ? level.CellSize : 1.0f;

        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minZ = float.MaxValue;
        float maxZ = float.MinValue;

        Vector3 GridToWorld(int gx, int gy)
        {
            float x = (gx - (w - 1) * 0.5f) * cs;
            float z = (gy - (h - 1) * 0.5f) * cs;
            return new Vector3(x, 0f, z);
        }

        void Encapsulate(float x, float z, float radX, float radZ)
        {
            minX = Mathf.Min(minX, x - radX);
            maxX = Mathf.Max(maxX, x + radX);
            minZ = Mathf.Min(minZ, z - radZ);
            maxZ = Mathf.Max(maxZ, z + radZ);
        }

        bool hasAnyContent = false;

        if (level.AntQueues != null)
        {
            foreach (var q in level.AntQueues)
            {
                if (q.Cells != null && q.Cells.Count > 0)
                {
                    hasAnyContent = true;
                    foreach (var cell in q.Cells)
                    {
                        Vector3 pos = GridToWorld(cell.x, cell.y);
                        Encapsulate(pos.x, pos.z, 0.5f * cs, 0.5f * cs);
                    }
                }
                if (q.InitialBodyCoordinates != null && q.InitialBodyCoordinates.Count > 0)
                {
                    hasAnyContent = true;
                    foreach (var cell in q.InitialBodyCoordinates)
                    {
                        Vector3 pos = GridToWorld(cell.x, cell.y);
                        Encapsulate(pos.x, pos.z, 0.5f * cs, 0.5f * cs);
                    }
                }
            }
        }

        if (level.Holes != null)
        {
            foreach (var hole in level.Holes)
            {
                hasAnyContent = true;
                Vector3 pos = GridToWorld(hole.GridCoordinate.x, hole.GridCoordinate.y);
                Encapsulate(pos.x, pos.z, 1.0f * cs, 1.0f * cs);
            }
        }

        var refHoles = ReferenceLevelRuntimeAdapter.ExtractHoles(level);
        if (refHoles != null)
        {
            foreach (var rH in refHoles)
            {
                if (rH.OccupiedCells != null)
                {
                    hasAnyContent = true;
                    foreach (var cell in rH.OccupiedCells)
                    {
                        Vector3 pos = GridToWorld(cell.x, cell.y);
                        Encapsulate(pos.x, pos.z, 0.6f * cs, 0.6f * cs);
                    }
                }
            }
        }

        var refPipes = ReferenceLevelRuntimeAdapter.ExtractPipes(level);
        if (refPipes != null)
        {
            foreach (var rP in refPipes)
            {
                if (rP.Cells != null)
                {
                    hasAnyContent = true;
                    foreach (var cell in rP.Cells)
                    {
                        Vector3 pos = GridToWorld(cell.x, cell.y);
                        Encapsulate(pos.x, pos.z, 0.6f * cs, 0.6f * cs);
                    }
                }
            }
        }

        if (!hasAnyContent)
        {
            minX = -(w - 1) * 0.5f * cs - 0.5f * cs;
            maxX = (w - 1) * 0.5f * cs + 0.5f * cs;
            minZ = -(h - 1) * 0.5f * cs - 0.5f * cs;
            maxZ = (h - 1) * 0.5f * cs + 0.5f * cs;
        }

        // Apply visual padding for outer holder geometries
        minX -= visualPadding * cs;
        maxX += visualPadding * cs;
        minZ -= visualPadding * cs;
        maxZ += visualPadding * cs;

        float boundsWidth = maxX - minX;
        float boundsHeight = maxZ - minZ;
        Vector3 center = new Vector3((minX + maxX) * 0.5f, 0f, (minZ + maxZ) * 0.5f);

        boardBounds = new Bounds(center, new Vector3(boundsWidth, 1f, boundsHeight));
    }

    private void UpdatePanLimitsForCurrentZoom()
    {
        if (mainCamera == null) return;

        float aspect = mainCamera.aspect > 0.01f ? mainCamera.aspect : ((float)Mathf.Max(1, Screen.width) / Mathf.Max(1, Screen.height));
        visibleHeight = 2f * currentZoom;
        visibleWidth = visibleHeight * aspect;

        float usableFracW = Mathf.Clamp01(1.0f - horizontalMargin * 2f);
        float usableHalfW = currentZoom * aspect * usableFracW;

        float pitchRad = cameraPitch * Mathf.Deg2Rad;
        float sinPitch = Mathf.Sin(pitchRad);
        float cotPitch = Mathf.Cos(pitchRad) / Mathf.Sin(pitchRad);
        float pitchZOffset = cameraHeight * cotPitch;

        // Viewport vertical safe area in camera Y units
        float yCamMin = (minUsableViewportY - 0.5f) * 2f * currentZoom;
        float yCamMax = (maxUsableViewportY - 0.5f) * 2f * currentZoom;
        float yCamMid = (yCamMin + yCamMax) * 0.5f;

        // Centered camera position so board center aligns with center of usable viewport below HUD
        float initialZ = boardBounds.center.z - pitchZOffset - (yCamMid / sinPitch);
        initialCamPos = new Vector3(boardBounds.center.x, cameraHeight, initialZ);

        // Horizontal pan limits: allow bringing min.x and max.x comfortably inside usable horizontal bounds
        float camXLeftLimit = boardBounds.min.x - panMarginX + usableHalfW;
        float camXRightLimit = boardBounds.max.x + panMarginX - usableHalfW;
        minCamX = Mathf.Min(camXLeftLimit, boardBounds.center.x - panMarginX);
        maxCamX = Mathf.Max(camXRightLimit, boardBounds.center.x + panMarginX);
        canPanX = (maxCamX - minCamX) > 0.05f;

        // Vertical pan limits:
        // Panning camera +Z moves viewport content down, bringing board top (max.z) into usable view below HUD.
        // Panning camera -Z moves viewport content up, bringing board bottom (min.z) into usable view above bottom HUD.
        float camZBottomLimit = boardBounds.min.z - panMarginZ - pitchZOffset - (yCamMin / sinPitch);
        float camZTopLimit = boardBounds.max.z + panMarginZ - pitchZOffset - (yCamMax / sinPitch);

        minCamZ = Mathf.Min(camZBottomLimit, initialZ - panMarginZ);
        maxCamZ = Mathf.Max(camZTopLimit, initialZ + panMarginZ);
        canPanZ = (maxCamZ - minCamZ) > 0.05f;
    }

    /// <summary>
    /// Optional testing/debug action: calculates the zoom required to fit the entire board on screen
    /// below the HUD at once, without altering the normal gameplay default starting view.
    /// </summary>
    [ContextMenu("Fit Board Below HUD (Debug Action)")]
    public void FitBoard()
    {
        if (currentActiveLevel == null && BoardManager.Instance != null && BoardManager.Instance.CurrentLevel != null)
        {
            currentActiveLevel = BoardManager.Instance.CurrentLevel;
        }

        if (currentActiveLevel != null)
        {
            CalculateBoardBounds(currentActiveLevel);
            float aspect = mainCamera.aspect > 0.01f ? mainCamera.aspect : ((float)Mathf.Max(1, Screen.width) / Mathf.Max(1, Screen.height));
            float usableFracW = Mathf.Clamp01(1.0f - horizontalMargin * 2f);
            float usableFracH = Mathf.Clamp01(maxUsableViewportY - minUsableViewportY);

            float requiredOrthoW = boardBounds.size.x / (2f * aspect * usableFracW);
            float requiredOrthoH = boardBounds.size.z / (2f * usableFracH);
            float fitOrtho = Mathf.Clamp(Mathf.Max(requiredOrthoW, requiredOrthoH), minZoom, maxZoom);

            SetZoom(fitOrtho);
            ResetToCenter();
        }
    }

    public void OnDragStart(Vector2 screenPos)
    {
        if (IsModalBlocked()) return;

        isDragging = true;
        dragStartPointerPos = screenPos;
        dragStartCamPos = (mainCamera != null) ? mainCamera.transform.position : currentCamPos;
    }

    public void OnDrag(Vector2 currentScreenPos)
    {
        if (!isDragging) return;
        if (IsModalBlocked())
        {
            OnDragEnd();
            return;
        }
        if (mainCamera == null) return;

        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        Vector3 oldCamPos = mainCamera.transform.position;
        mainCamera.transform.position = dragStartCamPos;

        Ray startRay = mainCamera.ScreenPointToRay(dragStartPointerPos);
        Ray currRay = mainCamera.ScreenPointToRay(currentScreenPos);

        mainCamera.transform.position = oldCamPos;

        if (groundPlane.Raycast(startRay, out float d1) && groundPlane.Raycast(currRay, out float d2))
        {
            Vector3 pStart = startRay.GetPoint(d1);
            Vector3 pCurr = currRay.GetPoint(d2);

            Vector3 delta = (pCurr - pStart) * panSensitivity;

            float actualMinX = Mathf.Min(minCamX, maxCamX);
            float actualMaxX = Mathf.Max(minCamX, maxCamX);
            float actualMinZ = Mathf.Min(minCamZ, maxCamZ);
            float actualMaxZ = Mathf.Max(minCamZ, maxCamZ);

            float targetX = Mathf.Clamp(dragStartCamPos.x - delta.x, actualMinX, actualMaxX);
            float targetZ = Mathf.Clamp(dragStartCamPos.z - delta.z, actualMinZ, actualMaxZ);

            currentCamPos = new Vector3(targetX, cameraHeight, targetZ);
            ApplyCameraPosition(currentCamPos);
        }
    }

    public void OnDragEnd()
    {
        isDragging = false;
    }

    public void ResetToCenter()
    {
        isDragging = false;
        currentCamPos = initialCamPos;
        ApplyCameraPosition(currentCamPos);
    }

    private void ApplyCameraPosition(Vector3 pos)
    {
        Quaternion rot = Quaternion.Euler(cameraPitch, 0f, 0f);

        if (mainCamera != null)
        {
            mainCamera.transform.position = pos;
            mainCamera.transform.rotation = rot;
        }

        if (useCinemachine && gameplayCamera != null)
        {
            gameplayCamera.transform.position = pos;
            gameplayCamera.transform.rotation = rot;
        }
    }
}
