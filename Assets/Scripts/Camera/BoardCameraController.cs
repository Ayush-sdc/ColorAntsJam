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
/// Fixed-view camera controller for Slither Ants Jam with manual drag-panning.
/// Maintains constant camera height/distance, rotation, and FOV across all levels (no dynamic zoom).
/// Centers initial view on the board and allows horizontal/vertical drag-panning only when the level
/// layout exceeds the visible Game View footprint. Clamps panning to playable board boundaries.
/// </summary>
[ExecuteAlways]
public class BoardCameraController : MonoBehaviour
{
    private static BoardCameraController instance;
    public static BoardCameraController Instance => instance;

    [Header("Camera Engine Switch")]
    [Tooltip("When true, Cinemachine controls board framing. When false, Main Camera is positioned directly.")]
    [SerializeField] private bool useCinemachine = false;

    [Header("Fixed Baseline Optics (Constant across levels)")]
    [SerializeField] private CameraProjectionMode projectionMode = CameraProjectionMode.Perspective;
    [Range(60f, 90f)]
    [SerializeField] private float cameraPitch = 88f;
    [Range(30f, 90f)]
    [SerializeField] private float perspectiveFov = 78f;
    [SerializeField] private float cameraHeight = 13.04f;
    [SerializeField] private float baselineZOffset = -0.17f;

    [Header("Camera References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private CinemachineBrain cinemachineBrain;
    [SerializeField] private CinemachineCamera gameplayCamera;

    [Header("Content Framing & Visual Margins")]
    [Tooltip("Small extra visual padding around outer geometry in world units")]
    [SerializeField] private float visualPadding = 0.35f;

    [Header("Runtime Pan Diagnostics")]
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
    private Vector2 camToVisOffset;
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
    public bool CanPanX => canPanX;
    public bool CanPanZ => canPanZ;
    public Bounds BoardBounds => boardBounds;
    public float VisibleWidth => visibleWidth;
    public float VisibleHeight => visibleHeight;
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

    /// <summary>
    /// Alias for SetupCameraForLevel to preserve backwards compatibility.
    /// </summary>
    public void FrameLevel(Level level)
    {
        SetupCameraForLevel(level);
    }

    /// <summary>
    /// Configures the fixed-view camera for the given level.
    /// Calculates board bounds, visible camera footprint, overflow, and clamps camera to centered starting position.
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
        RecalculateFramingAndPanLimits();

        // Place camera at centered starting position
        currentCamPos = initialCamPos;
        ApplyCameraPosition(currentCamPos);

        // Required diagnostic logs
        if (!canPanX && !canPanZ)
        {
            Debug.Log($"[CameraPan] Level={level.LevelNumber} FitsViewport=True CanPanX=False CanPanY=False");
        }
        else
        {
            Debug.Log($"[CameraPan] Level={level.LevelNumber} BoardBounds={boardBounds.size.x:F2}x{boardBounds.size.z:F2} " +
                      $"VisibleBounds={visibleWidth:F2}x{visibleHeight:F2} CanPanX={canPanX} CanPanY={canPanZ} " +
                      $"PanX=[{minCamX:F2},{maxCamX:F2}] PanY=[{minCamZ:F2},{maxCamZ:F2}] FOV={perspectiveFov:F0}");
        }
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

    private void RecalculateFramingAndPanLimits()
    {
        if (currentActiveLevel == null) return;

        // 1. Calculate complete playable level footprint
        CalculateBoardBounds(currentActiveLevel);

        // 2. Calculate visible camera world footprint on board plane (Y = 0)
        CalculateVisibleFootprint();

        // 3. Calculate initial center position and pan clamps
        CalculatePanLimits();
    }

    private void CalculateBoardBounds(Level level)
    {
        int w = level.GridWidth > 0 ? level.GridWidth : 9;
        int h = level.GridHeight > 0 ? level.GridHeight : 6;
        float cs = level.CellSize > 0f ? level.CellSize : 1.0f;

        float minX = -(w - 1) * 0.5f * cs - 0.5f * cs;
        float maxX = (w - 1) * 0.5f * cs + 0.5f * cs;
        float minZ = -(h - 1) * 0.5f * cs - 0.5f * cs;
        float maxZ = (h - 1) * 0.5f * cs + 0.5f * cs;

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

        if (level.AntQueues != null)
        {
            foreach (var q in level.AntQueues)
            {
                if (q.Cells != null && q.Cells.Count > 0)
                {
                    foreach (var cell in q.Cells)
                    {
                        Vector3 pos = GridToWorld(cell.x, cell.y);
                        Encapsulate(pos.x, pos.z, 0.5f * cs, 0.5f * cs);
                    }
                }
                if (q.InitialBodyCoordinates != null && q.InitialBodyCoordinates.Count > 0)
                {
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
                Vector3 pos = GridToWorld(hole.GridCoordinate.x, hole.GridCoordinate.y);
                Encapsulate(pos.x, pos.z, 1.0f * cs, 1.0f * cs);
            }
        }

        // Apply intentional visual padding for outer holder geometries
        minX -= visualPadding * cs;
        maxX += visualPadding * cs;
        minZ -= visualPadding * cs;
        maxZ += visualPadding * cs;

        float boundsWidth = maxX - minX;
        float boundsHeight = maxZ - minZ;
        Vector3 center = new Vector3((minX + maxX) * 0.5f, 0f, (minZ + maxZ) * 0.5f);

        boardBounds = new Bounds(center, new Vector3(boundsWidth, 1f, boundsHeight));
    }

    private void CalculateVisibleFootprint()
    {
        if (mainCamera == null) return;

        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        // Evaluate visible footprint with camera at reference position
        Vector3 refCamPos = new Vector3(boardBounds.center.x, cameraHeight, boardBounds.center.z + baselineZOffset);
        Quaternion refRot = Quaternion.Euler(cameraPitch, 0f, 0f);

        // Temporarily position camera for raycasting
        Vector3 prevPos = mainCamera.transform.position;
        Quaternion prevRot = mainCamera.transform.rotation;
        mainCamera.transform.position = refCamPos;
        mainCamera.transform.rotation = refRot;

        Vector2[] viewports = new Vector2[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f)
        };

        float minX = float.MaxValue, maxX = float.MinValue;
        float minZ = float.MaxValue, maxZ = float.MinValue;

        for (int i = 0; i < 4; i++)
        {
            Ray ray = mainCamera.ViewportPointToRay(new Vector3(viewports[i].x, viewports[i].y, 0f));
            if (groundPlane.Raycast(ray, out float enter))
            {
                Vector3 hit = ray.GetPoint(enter);
                minX = Mathf.Min(minX, hit.x);
                maxX = Mathf.Max(maxX, hit.x);
                minZ = Mathf.Min(minZ, hit.z);
                maxZ = Mathf.Max(maxZ, hit.z);
            }
        }

        mainCamera.transform.position = prevPos;
        mainCamera.transform.rotation = prevRot;

        visibleWidth = maxX - minX;
        visibleHeight = maxZ - minZ;

        // Relative offset between reference camera position and visible footprint center
        float visCenterX = (minX + maxX) * 0.5f;
        float visCenterZ = (minZ + maxZ) * 0.5f;
        camToVisOffset = new Vector2(visCenterX - refCamPos.x, visCenterZ - refCamPos.z);
    }

    private void CalculatePanLimits()
    {
        float boardWidth = boardBounds.size.x;
        float boardHeight = boardBounds.size.z;
        Vector3 boardCenter = boardBounds.center;

        // Initial camera position centers the visible footprint on the board
        initialCamPos = new Vector3(
            boardCenter.x,
            cameraHeight,
            boardCenter.z + baselineZOffset
        );

        // Overflow detection
        canPanX = boardWidth > visibleWidth;
        canPanZ = boardHeight > visibleHeight;

        // When camera is at initialCamPos:
        float visMinX = initialCamPos.x + camToVisOffset.x - visibleWidth * 0.5f;
        float visMaxX = initialCamPos.x + camToVisOffset.x + visibleWidth * 0.5f;
        float visMinZ = initialCamPos.z + camToVisOffset.y - visibleHeight * 0.5f;
        float visMaxZ = initialCamPos.z + camToVisOffset.y + visibleHeight * 0.5f;

        if (canPanX)
        {
            // Clamping bounds: screen left edge cannot go past board min.x, screen right edge cannot go past board max.x
            minCamX = initialCamPos.x + (boardBounds.min.x - visMinX);
            maxCamX = initialCamPos.x + (boardBounds.max.x - visMaxX);
            if (minCamX > maxCamX)
            {
                minCamX = maxCamX = initialCamPos.x;
                canPanX = false;
            }
        }
        else
        {
            minCamX = maxCamX = initialCamPos.x;
        }

        if (canPanZ)
        {
            minCamZ = initialCamPos.z + (boardBounds.min.z - visMinZ);
            maxCamZ = initialCamPos.z + (boardBounds.max.z - visMaxZ);
            if (minCamZ > maxCamZ)
            {
                minCamZ = maxCamZ = initialCamPos.z;
                canPanZ = false;
            }
        }
        else
        {
            minCamZ = maxCamZ = initialCamPos.z;
        }
    }

    public void OnDragStart(Vector2 screenPos)
    {
        isDragging = true;
        dragStartPointerPos = screenPos;
        dragStartCamPos = (mainCamera != null) ? mainCamera.transform.position : currentCamPos;
    }

    public void OnDrag(Vector2 currentScreenPos)
    {
        if (!isDragging) return;
        if (!canPanX && !canPanZ) return;
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

            Vector3 delta = pCurr - pStart;

            float targetX = canPanX
                ? Mathf.Clamp(dragStartCamPos.x - delta.x, minCamX, maxCamX)
                : initialCamPos.x;

            float targetZ = canPanZ
                ? Mathf.Clamp(dragStartCamPos.z - delta.z, minCamZ, maxCamZ)
                : initialCamPos.z;

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
