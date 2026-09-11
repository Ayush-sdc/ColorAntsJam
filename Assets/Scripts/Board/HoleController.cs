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
    public bool IsFull => remainingCount <= 0;
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
                int visualW = (this.initialQueueCount == 1) ? 1 : this.widthCells;
                int visualH = (this.initialQueueCount == 1) ? 1 : this.heightCells;
                BuildStadiumSlotVisuals(isRightSide, parallelRows, visualW, visualH);
                UpdateBadgeDisplay();
                if (this.initialQueueCount == 1)
                {
                    ApplySingleQueueHolderVisualOverride();
                }
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
            int visualW = (this.initialQueueCount == 1) ? 1 : this.widthCells;
            int visualH = (this.initialQueueCount == 1) ? 1 : this.heightCells;
            BuildStadiumSlotVisuals(isRightSide, parallelRows, visualW, visualH);
            if (this.initialQueueCount == 1)
            {
                ApplySingleQueueHolderVisualOverride();
            }
        }
        else
        {
            UpdateBadgeDisplay();
        }
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
            int visualW = (this.initialQueueCount == 1) ? 1 : this.widthCells;
            int visualH = (this.initialQueueCount == 1) ? 1 : this.heightCells;
            BuildStadiumSlotVisuals(isRightSideSlot, this.parallelRows, visualW, visualH);
            UpdateBadgeDisplay();

            if (this.initialQueueCount == 1)
            {
                ApplySingleQueueHolderVisualOverride();
            }
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
        int visualW = (this.initialQueueCount == 1) ? 1 : this.widthCells;
        int visualH = (this.initialQueueCount == 1) ? 1 : this.heightCells;
        BuildStadiumSlotVisuals(isRightSide, parallelRows, visualW, visualH);
        UpdateBadgeDisplay();
        if (this.initialQueueCount == 1)
        {
            ApplySingleQueueHolderVisualOverride();
        }
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

        slotRoot = new GameObject("HoleSlotRoot").transform;
        slotRoot.SetParent(transform, false);
        slotRoot.localPosition = Vector3.zero;
        slotRoot.localRotation = Quaternion.identity;

        if (wCells > 0 && this.widthCells <= 1) this.widthCells = wCells;
        if (hCells > 0 && this.heightCells <= 1) this.heightCells = hCells;
        if (rows > 0) this.parallelRows = rows;
        this.isRightSide = rightSide;

        int effectiveW = (wCells > 0) ? wCells : this.widthCells;
        int effectiveH = (hCells > 0) ? hCells : this.heightCells;

        float cs = (BoardManager.Instance != null && BoardManager.Instance.CellSize > 0f) ? BoardManager.Instance.CellSize : 1.0f;
        float smallMargin = 0.06f * cs;
        float outerW = Mathf.Max(0.60f * cs, effectiveW * cs - smallMargin);
        float outerH = Mathf.Max(0.60f * cs, effectiveH * cs - smallMargin);

        // Moderate rounded rectangle radius: clearly rectangular body, NOT a pill or capsule!
        float cornerRadius = Mathf.Min(
            0.22f * cs,
            Mathf.Min(outerW, outerH) * 0.28f
        );

        // Visible colored plastic rim border
        float rimThickness = Mathf.Clamp(0.12f * cs, 0.08f * cs, Mathf.Min(outerW, outerH) * 0.24f);
        float innerW = outerW - (rimThickness * 2f);
        float innerH = outerH - (rimThickness * 2f);
        float innerCornerRadius = Mathf.Max(0.04f * cs, cornerRadius - rimThickness);

        float pitInset = 0.035f * cs;
        float pitW = innerW - (pitInset * 2f);
        float pitH = innerH - (pitInset * 2f);
        float pitCornerRadius = Mathf.Max(0.02f * cs, innerCornerRadius - pitInset);

        Material rimMat = AntColorPalette.GetHoleMaterial(holeColor);
        UnityEngine.Color rimCol = AntColorPalette.GetColor(holeColor);

        Material bevelMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        bevelMat.name = $"HoleInnerBevelMat_{holeColor}";
        UnityEngine.Color bevelCol = UnityEngine.Color.Lerp(rimCol, UnityEngine.Color.black, 0.38f);
        bevelMat.color = bevelCol;
        if (bevelMat.HasProperty("_BaseColor")) bevelMat.SetColor("_BaseColor", bevelCol);
        if (bevelMat.HasProperty("_Smoothness")) bevelMat.SetFloat("_Smoothness", 0.70f);
        if (bevelMat.HasProperty("_Metallic")) bevelMat.SetFloat("_Metallic", 0.05f);

        Material pitMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        pitMat.name = "HolePitFloorMat";
        UnityEngine.Color pitCol = new UnityEngine.Color(0.08f, 0.08f, 0.09f);
        pitMat.color = pitCol;
        if (pitMat.HasProperty("_BaseColor")) pitMat.SetColor("_BaseColor", pitCol);
        if (pitMat.HasProperty("_Smoothness")) pitMat.SetFloat("_Smoothness", 0.12f);

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

        // 4. Integrated Counter Badge
        countBadgeObj = new GameObject("SlotCounterBadge");
        countBadgeObj.transform.SetParent(slotRoot, false);
        countBadgeObj.transform.localPosition = Vector3.zero;

        Vector3 badgePos;
        if (this.widthCells > this.heightCells)
        {
            // Horizontal holder: badge on right edge of rim
            badgePos = new Vector3(outerW * 0.5f - 0.20f * cs, 0f, 0f);
        }
        else if (this.heightCells > this.widthCells)
        {
            // Vertical holder: badge at bottom edge of rim
            badgePos = new Vector3(0f, 0f, -(outerH * 0.5f - 0.20f * cs));
        }
        else
        {
            // Box / rectangular holder: badge at bottom-right
            badgePos = new Vector3(outerW * 0.5f - 0.22f * cs, 0f, -(outerH * 0.5f - 0.22f * cs));
        }

        GameObject badgeMeshObj = new GameObject("BadgeMesh");
        badgeMeshObj.transform.SetParent(countBadgeObj.transform, false);
        badgeMeshObj.transform.localPosition = badgePos;

        MeshFilter badgeFilter = badgeMeshObj.AddComponent<MeshFilter>();
        float badgeSize = 0.38f * cs;
        badgeFilter.sharedMesh = CreateRoundedRectFilledMesh(badgeSize, badgeSize, 0.08f * cs, 0.038f, 8, "StadiumBadgeTabMesh");
        badgeBackgroundRenderer = badgeMeshObj.AddComponent<MeshRenderer>();
        badgeBackgroundRenderer.material = rimMat;

        // Number Text
        GameObject textObj = new GameObject("BadgeCountText");
        textObj.transform.SetParent(countBadgeObj.transform, false);
        textObj.transform.localPosition = new Vector3(badgePos.x, 0.22f, badgePos.z);
        textObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        countTextMesh = textObj.AddComponent<TextMesh>();
        countTextMesh.alignment = TextAlignment.Center;
        countTextMesh.anchor = TextAnchor.MiddleCenter;
        countTextMesh.fontSize = 62;
        countTextMesh.characterSize = (this.widthCells >= 2 || this.heightCells >= 2) ? 0.080f : 0.092f;
        countTextMesh.fontStyle = FontStyle.Bold;
        countTextMesh.color = UnityEngine.Color.white;
    }

    [ContextMenu("Apply Single Queue Holder Visual Override")]
    public void ApplySingleQueueHolderVisualOverride(int levelNumber = -1)
    {
        if (slotRoot == null) return;

        // SlotRim: LocalScale X = 1.9205, Y = keep existing, Z = keep existing
        Transform rimTr = rimMeshRenderer != null ? rimMeshRenderer.transform : slotRoot.Find("SlotRim");
        if (rimTr != null)
        {
            Vector3 s = rimTr.localScale;
            s.x = 1.9205f;
            rimTr.localScale = s;
        }

        // SlotInnerBevel: LocalScale X = 2.3861, Y = keep existing, Z = keep existing
        Transform bevelTr = innerBevelRenderer != null ? innerBevelRenderer.transform : slotRoot.Find("SlotInnerBevel");
        if (bevelTr != null)
        {
            Vector3 s = bevelTr.localScale;
            s.x = 2.3861f;
            bevelTr.localScale = s;
        }

        // SlotPit: LocalScale X = 2.6535, Y = keep existing, Z = keep existing
        Transform pitTr = pitRenderer != null ? pitRenderer.transform : slotRoot.Find("SlotPit");
        if (pitTr != null)
        {
            Vector3 s = pitTr.localScale;
            s.x = 2.6535f;
            pitTr.localScale = s;
        }

        // SlotCounterBadge: LocalPosition X = 0.35, Y = 0, Z = 0
        Transform badgeTr = countBadgeObj != null ? countBadgeObj.transform : slotRoot.Find("SlotCounterBadge");
        if (badgeTr != null)
        {
            badgeTr.localPosition = new Vector3(0.35f, 0f, 0f);

            // BadgeMesh: LocalPosition X = 0.25, Y = 0, Z = -0.25
            Transform badgeMeshTr = badgeBackgroundRenderer != null ? badgeBackgroundRenderer.transform : badgeTr.Find("BadgeMesh");
            if (badgeMeshTr != null)
            {
                badgeMeshTr.localPosition = new Vector3(0.25f, 0f, -0.25f);
            }

            // BadgeCountText: LocalPosition X = 0.25, Y = 0.22, Z = -0.25
            Transform badgeTextTr = countTextMesh != null ? countTextMesh.transform : badgeTr.Find("BadgeCountText");
            if (badgeTextTr != null)
            {
                badgeTextTr.localPosition = new Vector3(0.25f, 0.22f, -0.25f);
            }
        }

        if (!singleQueueVisualOverrideApplied)
        {
            singleQueueVisualOverrideApplied = true;
            int lvl = levelNumber > 0 ? levelNumber : (BoardManager.Instance?.CurrentLevel?.LevelNumber ?? (LevelManager.Instance != null ? LevelManager.Instance.CurrentLevelNumber : 1));
            Debug.Log($"[SingleQueueHolderVisual] Level={lvl} Hole={gameObject.name} Color={holeColor} InitialQueueCount={initialQueueCount} OverrideApplied=True");
        }
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
        countTextMesh.characterSize = remainingCount >= 10 ? 0.080f : 0.092f;
        countTextMesh.color = remainingCount > 0 ? UnityEngine.Color.white : new UnityEngine.Color(1f, 1f, 1f, 0.85f);
    }

    public void OnAntSwallowed()
    {
        if (!isQueueBasedCount)
        {
            remainingCount = Mathf.Max(0, remainingCount - 1);
            UpdateBadgeDisplay();
        }

        PlaySwallowEffect();
        SpawnSwallowVFX();

        if (SoundManager.Instance != null && Application.isPlaying)
        {
            SoundManager.Instance.PlaySwallow();
        }
    }

    public void OnQueueSwallowed()
    {
        if (isCompleted) return;

        if (isQueueBasedCount)
        {
            remainingCount--;
        }

        UpdateBadgeDisplay();

        if (remainingCount <= 0)
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

        if (SoundManager.Instance != null && Application.isPlaying)
        {
            SoundManager.Instance.PlaySwallow();
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
                int visualW = (this.initialQueueCount == 1) ? 1 : this.widthCells;
                int visualH = (this.initialQueueCount == 1) ? 1 : this.heightCells;
                BuildStadiumSlotVisuals(isRightSide, parallelRows, visualW, visualH);
                UpdateBadgeDisplay();
                if (this.initialQueueCount == 1)
                {
                    ApplySingleQueueHolderVisualOverride();
                }
            }
        };
    }
#endif
}
