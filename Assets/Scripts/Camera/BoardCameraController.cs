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
/// Dual-mode camera controller for Slither Ants Jam with reference-accurate framing.
/// Supports both Orthographic (reference-accurate top-down) and Perspective modes.
/// Integrates with Cinemachine 3.x (CinemachineCamera & CinemachineBrain) and fallback direct Main Camera.
/// Dynamically calculates content bounds from scratch for each level without inheriting zoom.
/// </summary>
[ExecuteAlways]
public class BoardCameraController : MonoBehaviour
{
    private static BoardCameraController instance;
    public static BoardCameraController Instance => instance;

    [Header("Camera Engine Switch")]
    [Tooltip("When true, Cinemachine controls board framing. When false, Main Camera is positioned directly.")]
    [SerializeField] private bool useCinemachine = true;

    [Header("Projection & Optics")]
    [SerializeField] private CameraProjectionMode projectionMode = CameraProjectionMode.Orthographic;
    [Range(60f, 90f)]
    [SerializeField] private float cameraPitch = 89f;
    [Range(30f, 90f)]
    [SerializeField] private float perspectiveFov = 78f;

    [Header("Camera References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private CinemachineBrain cinemachineBrain;
    [SerializeField] private CinemachineCamera gameplayCamera;

    [Header("Content Framing & Viewport Settings")]
    [Tooltip("Target screen width occupancy of the active content (0.78 to 0.86 in reference)")]
    [Range(0.5f, 0.95f)]
    [SerializeField] private float targetHorizontalOccupancy = 0.78f;

    [Tooltip("Top viewport fraction reserved for HUD / level header (0.16 - 0.22)")]
    [Range(0.05f, 0.35f)]
    [SerializeField] private float topViewportMargin = 0.18f;

    [Tooltip("Bottom viewport fraction margin (0.05 - 0.15)")]
    [Range(0.02f, 0.25f)]
    [SerializeField] private float bottomViewportMargin = 0.08f;

    [Tooltip("Extra vertical world offset fine tuning")]
    [SerializeField] private float boardVerticalOffset = 0.0f;

    [Tooltip("Extra visual padding around outer holder geometry")]
    [SerializeField] private float holderVisualMargin = 0.45f;

    [Header("Distance Limits")]
    [SerializeField] private float minimumDistance = 5f;
    [SerializeField] private float maximumDistance = 60f;
    [SerializeField] private float fixedOrthoHeight = 15f;

    // Runtime state cache for inspection & diagnostics
    [Header("Runtime Framing Diagnostics")]
    [SerializeField] private Bounds lastCalculatedContentBounds;
    [SerializeField] private float lastCalculatedOrthoSize;
    [SerializeField] private Vector3 lastCalculatedCameraPosition;
    [SerializeField] private float lastCalculatedHorizontalOccupancy;
    [SerializeField] private float lastCalculatedVerticalOccupancy;

    public bool UseCinemachine
    {
        get => useCinemachine;
        set
        {
            useCinemachine = value;
            ApplyEngineState();
        }
    }

    public CameraProjectionMode ProjectionMode
    {
        get => projectionMode;
        set => projectionMode = value;
    }

    public float CameraPitch => cameraPitch;
    public float TargetHorizontalOccupancy => targetHorizontalOccupancy;
    public float TopViewportMargin => topViewportMargin;
    public float BottomViewportMargin => bottomViewportMargin;
    public float BoardVerticalOffset => boardVerticalOffset;
    public float HolderVisualMargin => holderVisualMargin;
    public float LastCalculatedOrthoSize => lastCalculatedOrthoSize;
    public Vector3 LastCalculatedCameraPosition => lastCalculatedCameraPosition;
    public float LastCalculatedHorizontalOccupancy => lastCalculatedHorizontalOccupancy;
    public float LastCalculatedVerticalOccupancy => lastCalculatedVerticalOccupancy;

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
            if (cinemachineBrain == null)
            {
                cinemachineBrain = mainCamera.gameObject.AddComponent<CinemachineBrain>();
            }
        }

        if (gameplayCamera == null)
        {
            var gpCamObj = GameObject.Find("GameplayCamera");
            if (gpCamObj != null)
            {
                gameplayCamera = gpCamObj.GetComponent<CinemachineCamera>();
            }
            else
            {
                gpCamObj = new GameObject("GameplayCamera");
                gameplayCamera = gpCamObj.AddComponent<CinemachineCamera>();
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
    /// Calculates visual framing from scratch for any Level data without inheriting zoom from previous levels.
    /// Uses tight content bounds (ant coordinates, holder footprints, visual geometry) and aspect ratio.
    /// </summary>
    public void FrameLevel(Level level)
    {
        if (level == null) return;
        EnsureReferences();
        ApplyEngineState();

        int w = level.GridWidth > 0 ? level.GridWidth : 9;
        int h = level.GridHeight > 0 ? level.GridHeight : 6;
        float cs = level.CellSize > 0f ? level.CellSize : 1.0f;

        Vector3 GridToWorld(Vector2Int gridPos)
        {
            float x = (gridPos.x - (w - 1) * 0.5f) * cs;
            float z = (gridPos.y - (h - 1) * 0.5f) * cs;
            return new Vector3(x, 0f, z);
        }

        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minZ = float.MaxValue;
        float maxZ = float.MinValue;

        void EncapsulatePoint(float x, float z, float radiusX = 0f, float radiusZ = 0f)
        {
            minX = Mathf.Min(minX, x - radiusX);
            maxX = Mathf.Max(maxX, x + radiusX);
            minZ = Mathf.Min(minZ, z - radiusZ);
            maxZ = Mathf.Max(maxZ, z + radiusZ);
        }

        // 1. Calculate Content Bounds from Active Ants
        if (level.AntQueues != null)
        {
            foreach (var q in level.AntQueues)
            {
                // Handmade ant queue coordinates
                if (q.InitialBodyCoordinates != null && q.InitialBodyCoordinates.Count > 0)
                {
                    foreach (var cell in q.InitialBodyCoordinates)
                    {
                        Vector3 wPos = GridToWorld(cell);
                        EncapsulatePoint(wPos.x, wPos.z, 0.5f * cs, 0.5f * cs);
                    }
                }

                // Reference level ant queue coordinates (HolderType == 1)
                if (q.HolderType == 1 && q.Cells != null && q.Cells.Count > 0)
                {
                    foreach (var cell in q.Cells)
                    {
                        Vector3 wPos = GridToWorld(cell);
                        EncapsulatePoint(wPos.x, wPos.z, 0.5f * cs, 0.5f * cs);
                    }
                }
            }
        }

        // 2. Calculate Content Bounds from Holders / Holes
        if (level.Holes != null && level.Holes.Count > 0)
        {
            foreach (var hole in level.Holes)
            {
                int rowCount = hole.ParallelRows;
                bool isRight = (rowCount >= 2) ? (hole.GridCoordinate.x >= w / 2) : (hole.GridCoordinate.x >= 4);
                Vector3 hCenter;
                float hw = 1.15f * cs;
                float hh = (rowCount >= 2) ? 1.15f * cs : 0.55f * cs;

                if (rowCount >= 2)
                {
                    hCenter = GridToWorld(hole.GridCoordinate) + new Vector3(0.5f * cs, 0f, 0.5f * cs);
                }
                else
                {
                    float xOff = isRight ? 0.5f : -0.5f;
                    hCenter = GridToWorld(hole.GridCoordinate) + new Vector3(xOff * cs, 0f, 0f);
                }

                EncapsulatePoint(hCenter.x, hCenter.z, hw + holderVisualMargin * cs, hh + holderVisualMargin * cs);
            }
        }
        else if (level.AntQueues != null)
        {
            // Reference Level Holes (HolderType == 0)
            foreach (var record in level.AntQueues)
            {
                if (record.HolderType == 0 && record.Cells != null && record.Cells.Count > 0)
                {
                    if (record.Cells.Count >= 2)
                    {
                        Vector2Int entrance = record.Cells[0];
                        Vector2Int outer = record.Cells[1];
                        bool isRight = entrance.x <= outer.x;
                        int rowCount = (record.Cells.Count > 2) ? 2 : 1;
                        Vector3 ePos = GridToWorld(entrance);
                        Vector3 hCenter;
                        float hw = 1.15f * cs;
                        float hh = (rowCount >= 2) ? 1.15f * cs : 0.55f * cs;

                        if (rowCount >= 2)
                        {
                            hCenter = ePos + new Vector3(0.5f * cs, 0f, 0.5f * cs);
                        }
                        else
                        {
                            float xOff = isRight ? 0.5f : -0.5f;
                            hCenter = ePos + new Vector3(xOff * cs, 0f, 0f);
                        }

                        EncapsulatePoint(hCenter.x, hCenter.z, hw + holderVisualMargin * cs, hh + holderVisualMargin * cs);
                    }
                    else
                    {
                        foreach (var cell in record.Cells)
                        {
                            Vector3 cPos = GridToWorld(cell);
                            EncapsulatePoint(cPos.x, cPos.z, 1.15f * cs + holderVisualMargin * cs, 0.55f * cs + holderVisualMargin * cs);
                        }
                    }
                }
            }
        }

        // Fallback if level had no content records
        if (minX > maxX || minZ > maxZ)
        {
            minX = -(w - 1) * 0.5f * cs - 0.5f * cs;
            maxX = (w - 1) * 0.5f * cs + 0.5f * cs;
            minZ = -(h - 1) * 0.5f * cs - 0.5f * cs;
            maxZ = (h - 1) * 0.5f * cs + 0.5f * cs;
        }

        float contentWidth = maxX - minX;
        float contentHeight = maxZ - minZ;
        float contentCenterX = (minX + maxX) * 0.5f;
        float contentCenterZ = (minZ + maxZ) * 0.5f;

        lastCalculatedContentBounds = new Bounds(new Vector3(contentCenterX, 0f, contentCenterZ), new Vector3(contentWidth, 1f, contentHeight));

        // 3. Aspect Ratio & Screen Dimensions
        Camera activeCam = (mainCamera != null) ? mainCamera : Camera.main;
        float aspect = (activeCam != null && activeCam.aspect > 0.01f)
            ? activeCam.aspect
            : (Screen.width / (float)Mathf.Max(1, Screen.height));

        // 4. Viewport & Orthographic Size Calculations
        float targetHUsage = Mathf.Clamp(targetHorizontalOccupancy, 0.50f, 0.95f);
        float orthoForWidth = contentWidth / (2.0f * aspect * targetHUsage);

        float availVFraction = Mathf.Clamp(1.0f - topViewportMargin - bottomViewportMargin, 0.30f, 0.95f);
        float orthoForHeight = contentHeight / (2.0f * availVFraction);

        float orthoSize = Mathf.Max(orthoForWidth, orthoForHeight);
        lastCalculatedOrthoSize = orthoSize;

        float visibleWorldHeight = 2.0f * orthoSize;
        float visibleWorldWidth = visibleWorldHeight * aspect;

        lastCalculatedHorizontalOccupancy = (contentWidth / visibleWorldWidth) * 100.0f;
        lastCalculatedVerticalOccupancy = (contentHeight / visibleWorldHeight) * 100.0f;

        // Camera Z positioning centered in available gameplay vertical band
        float targetZ = (contentCenterZ + boardVerticalOffset * cs) - orthoSize * (bottomViewportMargin - topViewportMargin);
        float targetX = contentCenterX;

        Quaternion targetRot = Quaternion.Euler(cameraPitch, 0f, 0f);

        if (projectionMode == CameraProjectionMode.Orthographic)
        {
            float targetY = Mathf.Clamp(fixedOrthoHeight, minimumDistance, maximumDistance);
            Vector3 targetPos = new Vector3(targetX, targetY, targetZ);
            lastCalculatedCameraPosition = targetPos;

            if (mainCamera != null)
            {
                mainCamera.orthographic = true;
                mainCamera.orthographicSize = orthoSize;
                mainCamera.nearClipPlane = 0.1f;
                mainCamera.farClipPlane = 200f;
            }

            if (useCinemachine && gameplayCamera != null)
            {
                gameplayCamera.transform.position = targetPos;
                gameplayCamera.transform.rotation = targetRot;
                gameplayCamera.Lens.ModeOverride = LensSettings.OverrideModes.Orthographic;
                gameplayCamera.Lens.OrthographicSize = orthoSize;
                gameplayCamera.Lens.NearClipPlane = 0.1f;
                gameplayCamera.Lens.FarClipPlane = 200f;

                if (cinemachineBrain != null && cinemachineBrain.isActiveAndEnabled)
                {
                    cinemachineBrain.ManualUpdate();
                }
            }
            else if (mainCamera != null)
            {
                mainCamera.transform.position = targetPos;
                mainCamera.transform.rotation = targetRot;
            }
        }
        else // Perspective Mode
        {
            float vFovRad = perspectiveFov * Mathf.Deg2Rad;
            float tanHalfVFov = Mathf.Tan(vFovRad * 0.5f);
            float targetDistance = orthoSize / tanHalfVFov;
            float targetY = Mathf.Clamp(targetDistance, minimumDistance, maximumDistance);
            Vector3 targetPos = new Vector3(targetX, targetY, targetZ);
            lastCalculatedCameraPosition = targetPos;

            if (mainCamera != null)
            {
                mainCamera.orthographic = false;
                mainCamera.fieldOfView = perspectiveFov;
                mainCamera.nearClipPlane = 0.1f;
                mainCamera.farClipPlane = 200f;
            }

            if (useCinemachine && gameplayCamera != null)
            {
                gameplayCamera.transform.position = targetPos;
                gameplayCamera.transform.rotation = targetRot;
                gameplayCamera.Lens.ModeOverride = LensSettings.OverrideModes.Perspective;
                gameplayCamera.Lens.FieldOfView = perspectiveFov;
                gameplayCamera.Lens.NearClipPlane = 0.1f;
                gameplayCamera.Lens.FarClipPlane = 200f;

                if (cinemachineBrain != null && cinemachineBrain.isActiveAndEnabled)
                {
                    cinemachineBrain.ManualUpdate();
                }
            }
            else if (mainCamera != null)
            {
                mainCamera.transform.position = targetPos;
                mainCamera.transform.rotation = targetRot;
            }
        }
    }
}
