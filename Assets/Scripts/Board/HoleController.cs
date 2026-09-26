using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class HoleController : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private int colorId = 0;
    [SerializeField] private AntColor holeColor = AntColor.Red;
    [SerializeField] private Vector2Int gridCoordinate;
    [SerializeField] private List<Vector2Int> occupiedCells = new List<Vector2Int>();
    [SerializeField] private int initialCapacity = 1;
    [SerializeField] private int remainingCount = 1;
    [Tooltip("True if hole is on the right side (ants enter from left, badge on right). False if hole is on the left side (ants enter from right, badge on left).")]
    [SerializeField] private bool isRightSide = true;
    [SerializeField] private int parallelRows = 1;

    [Header("Stadium Dimensions")]
    [SerializeField] private float singleRowWidth = 2.05f;
    [SerializeField] private float singleRowHeight = 0.88f;
    [SerializeField] private float multiRowWidth = 2.05f;
    [SerializeField] private float multiRowHeight = 2.05f;
    [SerializeField] private float tubeRadius = 0.095f;
    [SerializeField] private float tubeHeight = 0.085f;
    [SerializeField] private int widthCells = 1;
    [SerializeField] private int heightCells = 1;
    [SerializeField] private bool isQueueBasedCount = true;
    [SerializeField] private int initialQueueCount = 1;
    [SerializeField] private bool singleQueueVisualOverrideApplied = false;

    [Header("Visual Components")]
    [SerializeField] private Transform slotRoot;
    [SerializeField] private GameObject countBadgeObj;
    [SerializeField] private TextMesh countTextMesh;
    [SerializeField] private MeshRenderer badgeBackgroundRenderer;
    [SerializeField] private MeshRenderer rimMeshRenderer;
    [SerializeField] private MeshRenderer innerBevelRenderer;
    [SerializeField] private MeshRenderer pitRenderer;

    private static GameObject swallowVfxPrefab;
    private static Sprite capsuleSprite;

    [SerializeField] private string holeId = "";
    [SerializeField] private bool isCompleted = false;
    private bool hasDisappeared = false;

    public string HoleId => !string.IsNullOrEmpty(holeId) ? holeId : $"{holeColor}_{gridCoordinate.x}_{gridCoordinate.y}";
    public bool IsCompleted => isCompleted;
    public int ColorID => colorId;
    public AntColor Color => holeColor;
    public Vector2Int GridCoordinate => gridCoordinate;
    public IReadOnlyList<Vector2Int> OccupiedCells => (occupiedCells != null && occupiedCells.Count > 0) ? occupiedCells : new List<Vector2Int> { gridCoordinate };
    public int RemainingCount => remainingCount;
    public int InitialCapacity => initialCapacity;
    public int InitialQueueCount => initialQueueCount;
    public bool SingleQueueVisualOverrideApplied => singleQueueVisualOverrideApplied;

    [SerializeField] private int inboundReservations = 0;
    private readonly HashSet<AntQueueController> reservedQueues = new HashSet<AntQueueController>();
    private readonly HashSet<AntQueueController> enteringQueues = new HashSet<AntQueueController>();
    private readonly Dictionary<AntQueueController, int> queueLaneIndices = new Dictionary<AntQueueController, int>();
    private int nextLaneCounter = 0;

    public int InboundReservations => reservedQueues.Count + inboundReservations;
    public int AvailableCapacity => Mathf.Max(0, remainingCount - InboundReservations);
    public bool HasAvailableCapacity => !isCompleted && AvailableCapacity > 0;
    public bool IsFull => isCompleted || AvailableCapacity <= 0;
    public int EnteringQueueCount => enteringQueues.Count;

    public bool TryReserveSlot(AntQueueController queue = null)
    {
        if (!HasAvailableCapacity) return false;

        if (queue != null)
        {
            reservedQueues.Add(queue);
            if (!queueLaneIndices.ContainsKey(queue))
            {
                queueLaneIndices[queue] = (nextLaneCounter++) % 4;
            }
        }
        else
        {
            inboundReservations++;
        }

        Debug.Log($"[HolderReservation] Color={holeColor} Remaining={remainingCount} ReservedQueues={reservedQueues.Count} Reservations={InboundReservations} Available={AvailableCapacity}");
        return true;
    }

    public void ReleaseReservation(AntQueueController queue = null)
    {
        if (queue != null)
        {
            reservedQueues.Remove(queue);
            queueLaneIndices.Remove(queue);
        }
        else if (inboundReservations > 0)
        {
            inboundReservations--;
        }

        Debug.Log($"[HolderReservationReleased] Color={holeColor} Remaining={remainingCount} ReservedQueues={reservedQueues.Count} Reservations={InboundReservations} Available={AvailableCapacity}");
        CheckCompletion();
    }

    public bool RequestHoleEntry(AntQueueController queue)
    {
        if (queue != null)
        {
            enteringQueues.Add(queue);
        }
        return true;
    }

    public void ReleaseHoleEntry(AntQueueController queue)
    {
        if (queue != null)
        {
            enteringQueues.Remove(queue);
        }
        CheckCompletion();
    }

    public void ResetState()
    {
        inboundReservations = 0;
        reservedQueues.Clear();
        enteringQueues.Clear();
        queueLaneIndices.Clear();
        nextLaneCounter = 0;
        isCompleted = false;
        hasDisappeared = false;
        remainingCount = initialCapacity;
        UpdateBadgeDisplay();
    }

    [Header("Parallel Lanes")]
    [SerializeField] private float laneSpacing = 0.30f; // Lateral distance between parallel queue paths in cellSize units
    public float LaneSpacing => laneSpacing;

    public float GetLaneOffset(AntQueueController queue, float cs)
    {
        if (queue == null) return 0f;
        if (!queueLaneIndices.TryGetValue(queue, out int lane))
        {
            lane = (nextLaneCounter++) % 4;
            queueLaneIndices[queue] = lane;
        }

        float spacing = laneSpacing * cs;

        // When capacity is 1, single center lane
        if (initialCapacity <= 1 && reservedQueues.Count <= 1 && queueLaneIndices.Count <= 1)
        {
            return 0f;
        }

        // Deterministic symmetric parallel lanes:
        // Lane 0: Left lane  (-0.5 * laneSpacing)
        // Lane 1: Right lane (+0.5 * laneSpacing)
        // Lane 2: Outer Left (-1.15 * laneSpacing) for 3/4 queue capacity
        // Lane 3: Outer Right (+1.15 * laneSpacing) for 3/4 queue capacity
        switch (lane)
        {
            case 0: return -0.5f * spacing;
            case 1: return  0.5f * spacing;
            case 2: return -1.15f * spacing;
            case 3: return  1.15f * spacing;
            default: return 0f;
        }
    }

    /// <summary>
    /// Calculates the point where an ant begins entering the hole (outer rim boundary along approach direction).
    /// Uses board-plane entryForward and entryRight to position the parallel entry lane accurately.
    /// </summary>
    public Vector3 GetHoleEntryPoint(Vector2Int entranceCell, Vector2Int approachDir, AntQueueController queue = null)
    {
        Vector3 cellCenter = BoardManager.Instance != null 
            ? BoardManager.Instance.GridToWorldPosition(entranceCell)
            : transform.position;
        float cs = BoardManager.Instance != null ? BoardManager.Instance.CellSize : 1.0f;
        Vector3 entryForward = new Vector3(approachDir.x, 0f, approachDir.y).normalized;
        Vector3 entryRight = Vector3.Cross(Vector3.up, entryForward).normalized;

        float lateralOffset = GetLaneOffset(queue, cs);
        Vector3 point = cellCenter - entryForward * (0.50f * cs) + entryRight * lateralOffset;
        point.y = transform.position.y;
        return point;
    }

    /// <summary>
    /// Calculates the deep despawn point inside the hole, past the point where the COMPLETE ant has cleared the visible opening.
    /// Preserves the exact same parallel lane lateral offset so ants remain side-by-side inside the pit.
    /// </summary>
    public Vector3 GetHoleDespawnPoint(Vector2Int entranceCell, Vector2Int approachDir, float antVisualLength = 0.85f, AntQueueController queue = null)
    {
        Vector3 entryPoint = GetHoleEntryPoint(entranceCell, approachDir, queue);
        Vector3 entryForward = new Vector3(approachDir.x, 0f, approachDir.y).normalized;
        float cs = BoardManager.Instance != null ? BoardManager.Instance.CellSize : 1.0f;
        
        // Ant must clear the entry rim by its full visual length + safe margin
        float minDespawnDist = antVisualLength + 0.25f * cs;
        Vector3 despawnPoint = entryPoint + entryForward * minDespawnDist;
        
        // Recessed down into the dark pit floor
        despawnPoint.y = transform.position.y - 0.20f;
        return despawnPoint;
    }

    /// <summary>
    /// Calculates combined visual mesh bounds of this hole for overlap validation.
    /// </summary>
    public Bounds CalculateVisualBounds()
    {
        var renderers = GetComponentsInChildren<MeshRenderer>();
        Bounds totalBounds = new Bounds(transform.position, Vector3.zero);
        bool first = true;
        foreach (var r in renderers)
        {
            if (r == null || !r.enabled) continue;
            if (first) { totalBounds = r.bounds; first = false; }
            else totalBounds.Encapsulate(r.bounds);
        }
        return totalBounds;
    }

    public int ParallelRows => parallelRows;
    public int WidthCells => widthCells;
    public int HeightCells => heightCells;
    public bool IsQueueBasedCount { get => isQueueBasedCount; set => isQueueBasedCount = value; }
    public bool IsRightSide
    {
        get => isRightSide;
        set
        {
            isRightSide = value;
            if (!IsPrefabAsset())
            {
                BuildStadiumSlotVisuals(isRightSide, parallelRows, this.widthCells, this.heightCells);
                UpdateBadgeDisplay();
            }
        }
    }

    private bool IsPrefabAsset()
    {
#if UNITY_EDITOR
        return UnityEditor.PrefabUtility.IsPartOfPrefabAsset(gameObject) || !gameObject.scene.IsValid();
#else
        return false;
#endif
    }

    private void Awake()
    {
        if (IsPrefabAsset()) return;
        EnsureVisuals();
    }

    private void Start()
    {
        if (IsPrefabAsset()) return;
        EnsureVisuals();
    }

    private void OnEnable()
    {
        if (IsPrefabAsset()) return;
        EnsureVisuals();
    }

    public void EnsureVisuals()
    {
        if (IsPrefabAsset()) return;

        bool hasVisuals = slotRoot != null && 
                          slotRoot.childCount >= 3 && 
                          rimMeshRenderer != null && 
                          rimMeshRenderer.sharedMaterial != null &&
                          countTextMesh != null;

        if (!hasVisuals)
        {
            BuildStadiumSlotVisuals(isRightSide, parallelRows, this.widthCells, this.heightCells);
        }
        UpdateBadgeDisplay();
    }

    public void Initialize(int rawColorId, Vector2Int coordinate, List<Vector2Int> allCells, int capacity = 1, bool isRightSideSlot = true, int rowCount = 1, int widthInCells = 1, int heightInCells = 1, bool queueBased = true)
    {
        this.colorId = rawColorId;
        this.holeColor = ReferenceColorMapper.ToAntColor(rawColorId);
        this.gridCoordinate = coordinate;
        this.holeId = $"Hole_{rawColorId}_{coordinate.x}_{coordinate.y}";
        this.isCompleted = false;
        this.hasDisappeared = false;
        this.initialCapacity = Mathf.Max(0, capacity);
        this.initialQueueCount = this.initialCapacity;
        this.remainingCount = initialCapacity;
        this.inboundReservations = 0;
        this.isRightSide = isRightSideSlot;
        this.parallelRows = Mathf.Max(1, rowCount);
        this.isQueueBasedCount = queueBased;

        if (allCells != null && allCells.Count > 0)
        {
            this.occupiedCells = new List<Vector2Int>(allCells);
            int minX = int.MaxValue, maxX = int.MinValue;
            int minY = int.MaxValue, maxY = int.MinValue;
            foreach (var cell in allCells)
            {
                if (cell.x < minX) minX = cell.x;
                if (cell.x > maxX) maxX = cell.x;
                if (cell.y < minY) minY = cell.y;
                if (cell.y > maxY) maxY = cell.y;
            }
            this.widthCells = (widthInCells > 1) ? widthInCells : (maxX - minX + 1);
            this.heightCells = (heightInCells > 1) ? heightInCells : (maxY - minY + 1);
        }
        else
        {
            this.occupiedCells = new List<Vector2Int> { coordinate };
            this.widthCells = Mathf.Max(1, widthInCells);
            this.heightCells = Mathf.Max(1, heightInCells);
            if (this.parallelRows >= 2)
            {
                this.widthCells = Mathf.Max(this.widthCells, 2);
                this.heightCells = Mathf.Max(this.heightCells, 2);
            }
            else
            {
                this.widthCells = Mathf.Max(this.widthCells, 2);
                this.heightCells = 1;
            }
        }

        if (!IsPrefabAsset())
        {
            BuildStadiumSlotVisuals(isRightSideSlot, this.parallelRows, this.widthCells, this.heightCells);
            UpdateBadgeDisplay();
        }
    }

    public void Initialize(int rawColorId, Vector2Int coordinate, int capacity = 1, bool isRightSideSlot = true, int rowCount = 1)
    {
        Initialize(rawColorId, coordinate, new List<Vector2Int> { coordinate }, capacity, isRightSideSlot, rowCount, 2, rowCount >= 2 ? 2 : 1, false);
    }

    public void Initialize(AntColor color, Vector2Int coordinate, int capacity = 1, bool isRightSideSlot = true, int rowCount = 1)
    {
        int rawId = ReferenceColorMapper.ToRawColorId(color);
        Initialize(rawId, coordinate, new List<Vector2Int> { coordinate }, capacity, isRightSideSlot, rowCount, 2, rowCount >= 2 ? 2 : 1, false);
        this.holeColor = color;
    }

    public static Sprite GetCapsuleSprite()
    {
        if (capsuleSprite != null) return capsuleSprite;

        int w = 256;
        int h = 512;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.name = "CapsuleShadowTexture";
        UnityEngine.Color[] colors = new UnityEngine.Color[w * h];
        float radius = (w - 2) * 0.5f;
        float centerX = w * 0.5f;
        float topCenterY = h - radius - 1;
        float botCenterY = radius + 1;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = x - centerX;
                float dy = 0f;
                if (y > topCenterY) dy = y - topCenterY;
                else if (y < botCenterY) dy = y - botCenterY;

                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01((radius - dist) + 0.5f);
                colors[y * w + x] = new UnityEngine.Color(1f, 1f, 1f, alpha);
            }
        }
        tex.SetPixels(colors);
        tex.Apply();

        capsuleSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 256f);
        return capsuleSprite;
    }

    [ContextMenu("Rebuild Stadium Visuals")]
    public void BuildStadiumSlotVisuals()
    {
        BuildStadiumSlotVisuals(isRightSide, parallelRows, this.widthCells, this.heightCells);
        UpdateBadgeDisplay();
    }

    public void BuildStadiumSlotVisuals(bool rightSide, int rows, int wCells = -1, int hCells = -1)
    {
        if (IsPrefabAsset()) return;

        singleQueueVisualOverrideApplied = false;

        if (slotRoot != null)
        {
            SafeDestroy(slotRoot.gameObject);
        }

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            SafeDestroy(transform.GetChild(i).gameObject);
        }

        // Hierarchy root: HoleVisualRoot
        slotRoot = new GameObject("HoleVisualRoot").transform;
        slotRoot.SetParent(transform, false);
        slotRoot.localPosition = Vector3.zero;
        slotRoot.localRotation = Quaternion.identity;

        if (wCells > 0) this.widthCells = wCells;
        if (hCells > 0) this.heightCells = hCells;
        if (rows > 0) this.parallelRows = rows;
        this.isRightSide = rightSide;

        int effectiveW = (wCells > 0) ? wCells : this.widthCells;
        int effectiveH = (hCells > 0) ? hCells : this.heightCells;

        float cs = (BoardManager.Instance != null && BoardManager.Instance.CellSize > 0f) ? BoardManager.Instance.CellSize : 1.0f;
        float safeMargin = 0.08f * cs;
        float outerW = Mathf.Max(0.50f * cs, effectiveW * cs - safeMargin);
        float outerH = Mathf.Max(0.50f * cs, effectiveH * cs - safeMargin);

        // Rounded rectangle radius: clearly rectangular body with rounded corners
        float cornerRadius = Mathf.Min(
            0.22f * cs,
            Mathf.Min(outerW, outerH) * 0.28f
        );

        // Visible colored plastic rim border
        float baseRim = (initialCapacity >= 2 && effectiveW == 1 && effectiveH == 1) ? 0.09f * cs : 0.12f * cs;
        float rimThickness = Mathf.Clamp(baseRim, 0.08f * cs, Mathf.Min(outerW, outerH) * 0.24f);
        float innerW = outerW - (rimThickness * 2f);
        float innerH = outerH - (rimThickness * 2f);
        float innerCornerRadius = Mathf.Max(0.04f * cs, cornerRadius - rimThickness);

        float pitInset = (initialCapacity >= 2 && effectiveW == 1 && effectiveH == 1) ? 0.025f * cs : 0.035f * cs;
        float pitW = innerW - (pitInset * 2f);
        float pitH = innerH - (pitInset * 2f);
        float pitCornerRadius = Mathf.Max(0.02f * cs, innerCornerRadius - pitInset);

        Material rimMat = AntColorPalette.GetHoleMaterial(holeColor);
        Material bevelMat = AntColorPalette.GetHoleBevelMaterial(holeColor);
        Material pitMat = AntColorPalette.GetHolePitMaterial();

        // 1. Layer 1: Outer Rim (glossy colored rounded-rectangle ring, Y = 0.032)
        GameObject rimObj = new GameObject("SlotRim");
        rimObj.transform.SetParent(slotRoot, false);
        rimObj.transform.localPosition = Vector3.zero;

        MeshFilter rimFilter = rimObj.AddComponent<MeshFilter>();
        rimFilter.sharedMesh = CreateRoundedRectRingMesh(outerW, outerH, innerW, innerH, cornerRadius, innerCornerRadius, 0.032f, 0.005f, 0.020f, 8);
        rimMeshRenderer = rimObj.AddComponent<MeshRenderer>();
        rimMeshRenderer.material = rimMat;

        // 2. Layer 2: Inner Bevel / Inset Wall (darker shaded colored inset, sloping Y = 0.030 down to 0.016)
        GameObject bevelObj = new GameObject("SlotInnerBevel");
        bevelObj.transform.SetParent(slotRoot, false);
        bevelObj.transform.localPosition = Vector3.zero;

        MeshFilter bevelFilter = bevelObj.AddComponent<MeshFilter>();
        bevelFilter.sharedMesh = CreateRoundedRectBevelMesh(innerW, innerH, pitW, pitH, innerCornerRadius, pitCornerRadius, 0.030f, 0.016f, 8);
        innerBevelRenderer = bevelObj.AddComponent<MeshRenderer>();
        innerBevelRenderer.material = bevelMat;

        // 3. Layer 3: Inner Pit (dark charcoal/black recessed rounded-rectangle floor, Y = 0.010)
        GameObject pitObj = new GameObject("SlotPit");
        pitObj.transform.SetParent(slotRoot, false);
        pitObj.transform.localPosition = Vector3.zero;

        MeshFilter pitFilter = pitObj.AddComponent<MeshFilter>();
        pitFilter.sharedMesh = CreateRoundedRectFilledMesh(pitW, pitH, pitCornerRadius, 0.010f, 8, "StadiumPitFloorMesh");
        pitRenderer = pitObj.AddComponent<MeshRenderer>();
        pitRenderer.material = pitMat;

        // 4. Integrated Counter Badge — sticks to the nearest side of the colored SlotRim, inside holder bounds
        countBadgeObj = new GameObject("SlotCounterBadge");
        countBadgeObj.transform.SetParent(slotRoot, false);

        // BadgeMesh size: compact and clear padding without obstructing parallel entry lanes
        float badgeH = (effectiveW == 1 && effectiveH == 1)
            ? Mathf.Clamp((initialCapacity >= 2 ? 0.32f : 0.38f) * cs, 0.24f * cs, pitH * 0.58f)
            : Mathf.Clamp(0.42f * cs, 0.32f * cs, Mathf.Min(pitW, pitH) * 0.72f);
        float badgeW = (remainingCount >= 10) ? badgeH * 1.25f : badgeH;
        float badgeRadius = Mathf.Min(0.09f * cs, Mathf.Min(badgeW, badgeH) * 0.35f);

        Vector3 badgePos;
        float insetFromRim = 0.04f * cs;

        if (effectiveW > effectiveH)
        {
            // Horizontal holder: badge sticks to the end of the slot (right or left depending on isRightSide)
            float endSignX = isRightSide ? 1f : -1f;
            float badgeCenterX = endSignX * (outerW * 0.5f - badgeW * 0.5f - insetFromRim);
            badgePos = new Vector3(badgeCenterX, 0f, 0f);
        }
        else if (effectiveH > effectiveW)
        {
            // Vertical holder: badge sticks to the nearest end along vertical axis (bottom or top)
            float endSignZ = -1f;
            if (gridCoordinate.y > 0 && occupiedCells != null && occupiedCells.Count > 1)
            {
                int minY = int.MaxValue, maxY = int.MinValue;
                foreach (var c in occupiedCells)
                {
                    if (c.y < minY) minY = c.y;
                    if (c.y > maxY) maxY = c.y;
                }
                // If entrance is at top (maxY), badge is at bottom (-Z); if entrance is at bottom (minY), badge is at top (+Z)
                endSignZ = (gridCoordinate.y >= (minY + maxY) * 0.5f) ? -1f : 1f;
            }
            float badgeCenterZ = endSignZ * (outerH * 0.5f - badgeH * 0.5f - insetFromRim);
            badgePos = new Vector3(0f, 0f, badgeCenterZ);
        }
        else
        {
            // 1x1 or square holder
            if (effectiveW == 1 && effectiveH == 1)
            {
                float endSignX = isRightSide ? 1f : -1f;
                float badgeCenterX = endSignX * (outerW * 0.5f - badgeW * 0.5f - insetFromRim);
                badgePos = new Vector3(badgeCenterX, 0f, 0f);
            }
            else
            {
                // Multi-row square (e.g. 2x2): tuck against bottom-right inner rim
                float endSignX = isRightSide ? 1f : -1f;
                float badgeCenterX = endSignX * (outerW * 0.5f - badgeW * 0.5f - insetFromRim);
                float badgeCenterZ = -(outerH * 0.5f - badgeH * 0.5f - insetFromRim);
                badgePos = new Vector3(badgeCenterX, 0f, badgeCenterZ);
            }
        }
        countBadgeObj.transform.localPosition = badgePos;

        GameObject badgeMeshObj = new GameObject("BadgeMesh");
        badgeMeshObj.transform.SetParent(countBadgeObj.transform, false);
        badgeMeshObj.transform.localPosition = new Vector3(0f, 0.022f, 0f);

        MeshFilter badgeFilter = badgeMeshObj.AddComponent<MeshFilter>();
        badgeFilter.sharedMesh = CreateRoundedRectFilledMesh(badgeW, badgeH, badgeRadius, 0.016f, 8, "StadiumBadgeTabMesh");
        badgeBackgroundRenderer = badgeMeshObj.AddComponent<MeshRenderer>();
        badgeBackgroundRenderer.material = rimMat;

        // Number Text: centered relative to BadgeMesh, clear padding, no z-fighting
        GameObject textObj = new GameObject("BadgeCountText");
        textObj.transform.SetParent(countBadgeObj.transform, false);
        textObj.transform.localPosition = new Vector3(0f, 0.042f, 0f);
        textObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        countTextMesh = textObj.AddComponent<TextMesh>();
        countTextMesh.alignment = TextAlignment.Center;
        countTextMesh.anchor = TextAnchor.MiddleCenter;
        countTextMesh.fontSize = 64;
        countTextMesh.characterSize = (remainingCount >= 10) ? 0.034f : 0.035f;
        countTextMesh.fontStyle = FontStyle.Bold;
        countTextMesh.color = UnityEngine.Color.white;
        countTextMesh.text = remainingCount.ToString();
    }

    [ContextMenu("Apply Single Queue Holder Visual Override")]
    public void ApplySingleQueueHolderVisualOverride(int levelNumber = -1)
    {
        // Visual dimensions are authoritatively bounded by OccupiedCells footprint.
        // Scaling beyond the footprint is disabled to strictly prevent hole overlaps.
        singleQueueVisualOverrideApplied = true;
    }

    /// <summary>
    /// Generates 2D perimeter points of a rounded rectangle in CLOCKWISE order starting from Top-Left.
    /// </summary>
    private static List<Vector2> GenerateRoundedRectPerimeter(float w, float h, float r, int pointsPerCorner = 8)
    {
        var points = new List<Vector2>();
        float halfW = w * 0.5f;
        float halfH = h * 0.5f;
        r = Mathf.Clamp(r, 0.005f, Mathf.Min(halfW, halfH) - 0.001f);

        float straightW = Mathf.Max(0f, halfW - r);
        float straightH = Mathf.Max(0f, halfH - r);

        // 1. Top Edge (moving right: -straightW to +straightW at +halfH)
        points.Add(new Vector2(-straightW, halfH));
        points.Add(new Vector2(straightW, halfH));

        // 2. Top-Right Corner (arc from PI/2 down to 0)
        Vector2 cTR = new Vector2(straightW, straightH);
        for (int i = 1; i <= pointsPerCorner; i++)
        {
            float ang = Mathf.PI * 0.5f - ((float)i / pointsPerCorner) * Mathf.PI * 0.5f;
            points.Add(cTR + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r);
        }

        // 3. Right Edge (moving down: +straightH to -straightH at +halfW)
        points.Add(new Vector2(halfW, -straightH));

        // 4. Bottom-Right Corner (arc from 0 down to -PI/2)
        Vector2 cBR = new Vector2(straightW, -straightH);
        for (int i = 1; i <= pointsPerCorner; i++)
        {
            float ang = 0f - ((float)i / pointsPerCorner) * Mathf.PI * 0.5f;
            points.Add(cBR + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r);
        }

        // 5. Bottom Edge (moving left: +straightW to -straightW at -halfH)
        points.Add(new Vector2(-straightW, -halfH));

        // 6. Bottom-Left Corner (arc from -PI/2 down to -PI)
        Vector2 cBL = new Vector2(-straightW, -straightH);
        for (int i = 1; i <= pointsPerCorner; i++)
        {
            float ang = -Mathf.PI * 0.5f - ((float)i / pointsPerCorner) * Mathf.PI * 0.5f;
            points.Add(cBL + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r);
        }

        // 7. Left Edge (moving up: -straightH to +straightH at -halfW)
        points.Add(new Vector2(-halfW, straightH));

        // 8. Top-Left Corner (arc from PI down to PI/2)
        Vector2 cTL = new Vector2(-straightW, straightH);
        for (int i = 1; i < pointsPerCorner; i++)
        {
            float ang = Mathf.PI - ((float)i / pointsPerCorner) * Mathf.PI * 0.5f;
            points.Add(cTL + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r);
        }

        return points;
    }

    /// <summary>
    /// Generates a glossy 3D rounded-rectangle rim ring with top face and outer bevel skirt.
    /// All triangles are wound clockwise looking from above for proper top-down camera rendering.
    /// </summary>
    public static Mesh CreateRoundedRectRingMesh(float outerW, float outerH, float innerW, float innerH, float rOuter, float rInner, float topY = 0.032f, float outerBaseY = 0.005f, float innerBaseY = 0.020f, int pointsPerCorner = 8)
    {
        Mesh mesh = new Mesh();
        mesh.name = "StadiumRimRingMesh";

        List<Vector2> pOut = GenerateRoundedRectPerimeter(outerW, outerH, rOuter, pointsPerCorner);
        List<Vector2> pIn = GenerateRoundedRectPerimeter(innerW, innerH, rInner, pointsPerCorner);
        float baseOutW = outerW + 0.035f;
        float baseOutH = outerH + 0.035f;
        float baseROuter = rOuter + 0.0175f;
        List<Vector2> pBase = GenerateRoundedRectPerimeter(baseOutW, baseOutH, baseROuter, pointsPerCorner);

        int N = pOut.Count;
        List<Vector3> verts = new List<Vector3>();
        List<Vector3> norms = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> tris = new List<int>();

        // 1. Top Flat Face of the Rim Ring
        int topStart = verts.Count;
        for (int i = 0; i < N; i++)
        {
            verts.Add(new Vector3(pOut[i].x, topY, pOut[i].y));
            norms.Add(Vector3.up);
            uvs.Add(new Vector2((pOut[i].x / outerW) + 0.5f, (pOut[i].y / outerH) + 0.5f));
        }
        for (int i = 0; i < N; i++)
        {
            verts.Add(new Vector3(pIn[i].x, topY, pIn[i].y));
            norms.Add(Vector3.up);
            uvs.Add(new Vector2((pIn[i].x / outerW) + 0.5f, (pIn[i].y / outerH) + 0.5f));
        }

        for (int i = 0; i < N; i++)
        {
            int nxt = (i + 1) % N;
            int outCurr = topStart + i;
            int outNxt = topStart + nxt;
            int inNxt = topStart + N + nxt;
            int inCurr = topStart + N + i;

            // Clockwise from above
            tris.Add(outCurr);
            tris.Add(outNxt);
            tris.Add(inNxt);

            tris.Add(outCurr);
            tris.Add(inNxt);
            tris.Add(inCurr);
        }

        // 2. Outer Beveled Skirt (slopes down from top outer to base outer)
        int skirtStart = verts.Count;
        for (int i = 0; i < N; i++)
        {
            Vector3 topPt = new Vector3(pOut[i].x, topY, pOut[i].y);
            Vector3 basePt = new Vector3(pBase[i].x, outerBaseY, pBase[i].y);
            Vector3 norm = (new Vector3(pOut[i].x, 0f, pOut[i].y).normalized + Vector3.up * 0.5f).normalized;

            verts.Add(topPt);
            norms.Add(norm);
            uvs.Add(new Vector2(0f, (float)i / N));

            verts.Add(basePt);
            norms.Add(norm);
            uvs.Add(new Vector2(1f, (float)i / N));
        }

        for (int i = 0; i < N; i++)
        {
            int nxt = (i + 1) % N;
            int topCurr = skirtStart + (i * 2);
            int baseCurr = skirtStart + (i * 2) + 1;
            int topNxt = skirtStart + (nxt * 2);
            int baseNxt = skirtStart + (nxt * 2) + 1;

            // Clockwise
            tris.Add(topCurr);
            tris.Add(topNxt);
            tris.Add(baseNxt);

            tris.Add(topCurr);
            tris.Add(baseNxt);
            tris.Add(baseCurr);
        }

        mesh.vertices = verts.ToArray();
        mesh.normals = norms.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.triangles = tris.ToArray();
        mesh.RecalculateBounds();
        return mesh;
    }

    /// <summary>
    /// Generates a darker shaded inner bevel wall dropping from the inner rim edge into the pit.
    /// </summary>
    public static Mesh CreateRoundedRectBevelMesh(float topW, float topH, float botW, float botH, float rTop, float rBot, float topY = 0.030f, float botY = 0.016f, int pointsPerCorner = 8)
    {
        Mesh mesh = new Mesh();
        mesh.name = "StadiumInnerBevelMesh";

        List<Vector2> pTop = GenerateRoundedRectPerimeter(topW, topH, rTop, pointsPerCorner);
        List<Vector2> pBot = GenerateRoundedRectPerimeter(botW, botH, rBot, pointsPerCorner);

        int N = pTop.Count;
        List<Vector3> verts = new List<Vector3>();
        List<Vector3> norms = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> tris = new List<int>();

        for (int i = 0; i < N; i++)
        {
            Vector3 topPt = new Vector3(pTop[i].x, topY, pTop[i].y);
            Vector3 botPt = new Vector3(pBot[i].x, botY, pBot[i].y);
            Vector3 norm = (new Vector3(-pTop[i].x, 0f, -pTop[i].y).normalized + Vector3.up * 0.7f).normalized;

            verts.Add(topPt);
            norms.Add(norm);
            uvs.Add(new Vector2(0f, (float)i / N));

            verts.Add(botPt);
            norms.Add(norm);
            uvs.Add(new Vector2(1f, (float)i / N));
        }

        for (int i = 0; i < N; i++)
        {
            int nxt = (i + 1) % N;
            int topCurr = i * 2;
            int botCurr = i * 2 + 1;
            int topNxt = nxt * 2;
            int botNxt = nxt * 2 + 1;

            // Clockwise from top
            tris.Add(topCurr);
            tris.Add(topNxt);
            tris.Add(botNxt);

            tris.Add(topCurr);
            tris.Add(botNxt);
            tris.Add(botCurr);
        }

        mesh.vertices = verts.ToArray();
        mesh.normals = norms.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.triangles = tris.ToArray();
        mesh.RecalculateBounds();
        return mesh;
    }

    /// <summary>
    /// Generates a flat filled rounded-rectangle mesh (used for recessed pit floor and badge tab).
    /// Triangles are wound clockwise from above, normal pointing straight up.
    /// </summary>
    public static Mesh CreateRoundedRectFilledMesh(float w, float h, float r, float yOffset = 0.010f, int pointsPerCorner = 8, string meshName = "StadiumFilledMesh")
    {
        Mesh mesh = new Mesh();
        mesh.name = meshName;

        List<Vector2> pts = GenerateRoundedRectPerimeter(w, h, r, pointsPerCorner);
        int N = pts.Count;

        List<Vector3> verts = new List<Vector3>();
        List<Vector3> norms = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> tris = new List<int>();

        // Center vertex
        verts.Add(new Vector3(0f, yOffset, 0f));
        norms.Add(Vector3.up);
        uvs.Add(new Vector2(0.5f, 0.5f));

        for (int i = 0; i < N; i++)
        {
            verts.Add(new Vector3(pts[i].x, yOffset, pts[i].y));
            norms.Add(Vector3.up);
            uvs.Add(new Vector2((pts[i].x / w) + 0.5f, (pts[i].y / h) + 0.5f));
        }

        for (int i = 0; i < N; i++)
        {
            int nxt = (i + 1) % N;
            // Center -> pts[i] -> pts[nxt] is clockwise from above
            tris.Add(0);
            tris.Add(1 + i);
            tris.Add(1 + nxt);
        }

        mesh.vertices = verts.ToArray();
        mesh.normals = norms.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.triangles = tris.ToArray();
        mesh.RecalculateBounds();
        return mesh;
    }

    public static Mesh CreateStadiumTubeMesh(float outerW, float outerH, float tubeRadius, float tubeHeight, int pathResolution = 64, int crossResolution = 16, float rCorner = 0.36f)
    {
        float rimTh = Mathf.Clamp(tubeRadius * 1.5f, 0.08f, 0.20f);
        float inW = outerW - rimTh * 2f;
        float inH = outerH - rimTh * 2f;
        float rIn = Mathf.Max(0.04f, rCorner - rimTh);
        return CreateRoundedRectRingMesh(outerW, outerH, inW, inH, rCorner, rIn, 0.032f, 0.005f, 0.020f, 8);
    }

    public static Mesh CreateStadiumPitMesh(float outerW, float outerH, float tubeRadius, float yOffset = 0.012f, int arcSegments = 24, float rCorner = 0.36f)
    {
        float inW = outerW - tubeRadius * 2f;
        float inH = outerH - tubeRadius * 2f;
        float rIn = Mathf.Max(0.04f, rCorner - tubeRadius);
        return CreateRoundedRectFilledMesh(inW, inH, rIn, yOffset, 8, "StadiumPitMesh");
    }

    public static Mesh CreateStadiumBadgeTabMesh(Vector3 center, float w, float h, float r, float yOffset = 0.018f, int arcRes = 12)
    {
        return CreateRoundedRectFilledMesh(w, h, r, yOffset, 8, "StadiumBadgeTabMesh");
    }

    private void UpdateBadgeDisplay()
    {
        if (countTextMesh == null) return;

        countTextMesh.text = remainingCount.ToString();
        countTextMesh.characterSize = (remainingCount >= 10) ? 0.034f : 0.035f;
        countTextMesh.color = remainingCount > 0 ? UnityEngine.Color.white : new UnityEngine.Color(1f, 1f, 1f, 0.85f);
    }

    public void OnAntEnteredHole()
    {
        OnAntSwallowed();
    }

    public void OnAntSwallowed()
    {
        // Visual swallow only. DO NOT decrement queue count per ant.
        PlaySwallowEffect();
        SpawnSwallowVFX();

        if (SoundManager.Instance != null && Application.isPlaying)
        {
            SoundManager.Instance.PlaySwallow();
        }
    }

    public void OnEntireQueueEnteredHole(AntQueueController queue = null)
    {
        OnQueueSwallowed(queue);
    }

    public void OnQueueSwallowed(AntQueueController queue = null)
    {
        if (isCompleted) return;

        if (queue != null)
        {
            enteringQueues.Remove(queue);
            reservedQueues.Remove(queue);
            queueLaneIndices.Remove(queue);
        }
        else if (inboundReservations > 0)
        {
            inboundReservations--;
        }

        remainingCount = Mathf.Max(0, remainingCount - 1);
        UpdateBadgeDisplay();

        CheckCompletion();

        if (SoundManager.Instance != null && Application.isPlaying)
        {
            SoundManager.Instance.PlaySwallow();
        }
    }

    private void CheckCompletion()
    {
        if (isCompleted || hasDisappeared) return;

        // Hole disappears only when:
        // 1. remainingCount == 0
        // 2. inboundReservations == 0
        // 3. enteringQueues.Count == 0
        // 4. every ant belonging to every final reserved queue has fully crossed the despawn threshold
        if (remainingCount <= 0 && InboundReservations <= 0 && enteringQueues.Count <= 0)
        {
            remainingCount = 0;
            isCompleted = true;

            if (BoardManager.Instance != null)
            {
                BoardManager.Instance.RemoveCompletedHole(this);
            }
            else
            {
                PlayCompletedEffectAndDisappear();
            }
        }
    }

    public void PlayCompletedEffectAndDisappear(int releasedCells = -1, bool removedFromHoleMap = true)
    {
        if (hasDisappeared) return;
        hasDisappeared = true;
        isCompleted = true;

        // Immediately disable any colliders so they cannot interact with clicks
        var colliders = GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null) colliders[i].enabled = false;
        }

        var cells = OccupiedCells;
        if (releasedCells < 0)
        {
            releasedCells = cells != null ? cells.Count : 0;
        }

        string cellsFormatted = "[";
        if (cells != null && cells.Count > 0)
        {
            List<string> cellStrings = new List<string>();
            foreach (var c in cells)
            {
                cellStrings.Add($"({c.x},{c.y})");
            }
            cellsFormatted += string.Join(",", cellStrings);
        }
        cellsFormatted += "]";

        Debug.Log($"[HoleCompleted]\n" +
                  $"Color={holeColor}\n" +
                  $"RemainingCount={remainingCount}\n" +
                  $"OccupiedCells={cellsFormatted}\n" +
                  $"ReleasedCells={releasedCells}\n" +
                  $"RemovedFromHoleMap={removedFromHoleMap}\n" +
                  $"GameObjectRemoval=True");

        if (Application.isPlaying)
        {
            KillAllTweens();
            transform.DOScale(Vector3.zero, 0.20f)
                .SetEase(Ease.InBack)
                .OnComplete(() =>
                {
                    SafeDestroy(gameObject);
                });
        }
        else
        {
            SafeDestroy(gameObject);
        }
    }

    private void OnDisable()
    {
        KillAllTweens();
    }

    private void OnDestroy()
    {
        KillAllTweens();
    }

    private void KillAllTweens()
    {
        if (slotRoot != null) slotRoot.DOKill();
        if (countBadgeObj != null) countBadgeObj.transform.DOKill();
        transform.DOKill();
    }

    public void PlaySwallowEffect()
    {
        if (!Application.isPlaying) return;
        if (slotRoot == null) return;

        slotRoot.DOKill();
        slotRoot.localScale = Vector3.one;
        slotRoot.DOScale(new Vector3(1.08f, 0.92f, 1.08f), 0.08f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                if (slotRoot != null)
                {
                    slotRoot.DOScale(Vector3.one, 0.12f).SetEase(Ease.OutBounce);
                }
            });

        if (countBadgeObj != null)
        {
            countBadgeObj.transform.DOKill();
            countBadgeObj.transform.DOPunchScale(new Vector3(0.22f, 0.22f, 0.22f), 0.18f, 5, 0.5f);
        }
    }

    private void PlayCompletedEffect()
    {
        if (!Application.isPlaying) return;
        if (slotRoot == null) return;

        slotRoot.DOScale(new Vector3(1.12f, 1.0f, 1.12f), 0.2f)
            .SetLoops(2, LoopType.Yoyo)
            .SetEase(Ease.OutBack);
    }

    private void SpawnSwallowVFX()
    {
        if (!Application.isPlaying)
            return;

        if (swallowVfxPrefab == null)
        {
            swallowVfxPrefab = Resources.Load<GameObject>("Ants/Prefabs/ant_dis");
        }

        if (swallowVfxPrefab != null)
        {
            Vector3 spawnPos = transform.position + new Vector3(0f, 0.15f, 0f);
            GameObject fx = Instantiate(swallowVfxPrefab, spawnPos, Quaternion.identity);
            Destroy(fx, 1.2f);
        }
    }

    private void SafeDestroy(UnityEngine.Object obj)
    {
        if (obj == null) return;
#if UNITY_EDITOR
        if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(obj)) return;
#endif
        if (Application.isPlaying) Destroy(obj);
        else DestroyImmediate(obj);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (IsPrefabAsset()) return;
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null && !Application.isPlaying && !IsPrefabAsset())
            {
                BuildStadiumSlotVisuals(isRightSide, parallelRows, this.widthCells, this.heightCells);
                UpdateBadgeDisplay();
            }
        };
    }
#endif
}
