using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Controls a 3D direction-changing pipe conduit (HolderType == 2).
/// Renders glossy cyan conduits with straight rectangular runs, softly beveled cross-sections,
/// square 90-degree corners with diagonal miter seams, pale-cyan end collars, and counter badges.
/// Fully configurable via authoritative PipeVisualConfig asset.
/// </summary>
public class PipeController : MonoBehaviour
{
    [Header("Pipe Data")]
    [SerializeField] private List<Vector2Int> cells = new List<Vector2Int>();
    [SerializeField] private int pipeCount = 0;
    [SerializeField] private int remainingUses = 1;
    [SerializeField] private bool isPipeInUse = false;
    [SerializeField] private int activeReservations = 0;

    [Header("Visual Components")]
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private GameObject entranceCollarObj;
    [SerializeField] private GameObject exitCollarObj;
    [SerializeField] private GameObject shadowObj;
    [SerializeField] private RectTransform badgeRoot;
    [SerializeField] private Text counterText;
    [SerializeField] private Canvas badgeCanvas;

    [Header("Entrance Badge Inspector Controls")]
    [Tooltip("World scale of the entrance badge canvas. If <= 0, uses PipeVisualConfig.")]
    [SerializeField] private float badgeScale = 0f;
    [Tooltip("Numeral font size. If <= 0, uses PipeVisualConfig.")]
    [SerializeField] private int numeralSize = 0;
    [Tooltip("Forward offset from mouth along entry direction. If 0, uses PipeVisualConfig.")]
    [SerializeField] private float entranceForwardOffset = 0f;
    [Tooltip("Lateral offset from mouth towards outer wall. If 0, uses PipeVisualConfig.")]
    [SerializeField] private float entranceLateralOffset = 0f;
    [Tooltip("Vertical height offset above mouth. If 0, uses PipeVisualConfig.")]
    [SerializeField] private float entranceHeightOffset = 0f;

    public float ConfiguredBadgeScale => (badgeScale > 0.001f) ? badgeScale : PipeVisualConfig.Instance.badgeScale;
    public int ConfiguredNumeralSize => (numeralSize > 0) ? numeralSize : PipeVisualConfig.Instance.numeralSize;
    public float ConfiguredEntranceForwardOffset => (Mathf.Abs(entranceForwardOffset) > 0.001f) ? entranceForwardOffset : PipeVisualConfig.Instance.badgeForwardOffset;
    public float ConfiguredEntranceLateralOffset => (Mathf.Abs(entranceLateralOffset) > 0.001f) ? entranceLateralOffset : PipeVisualConfig.Instance.badgeLateralOffset;
    public float ConfiguredEntranceHeightOffset => (Mathf.Abs(entranceHeightOffset) > 0.001f) ? entranceHeightOffset : PipeVisualConfig.Instance.badgeHeightOffset;

    public IReadOnlyList<Vector2Int> Cells => cells;
    public int PipeCount => pipeCount;
    public int RemainingUses => remainingUses;
    public bool IsPipeInUse { get => isPipeInUse; set => isPipeInUse = value; }
    public bool IsExhausted => remainingUses <= 0;
    public int ActiveReservations => activeReservations;

    public Vector2Int EntranceCell => (cells != null && cells.Count > 0) ? cells[0] : Vector2Int.zero;
    public Vector2Int ExitCell => (cells != null && cells.Count > 0) ? cells[cells.Count - 1] : Vector2Int.zero;

    public Vector2Int EntryDirection
    {
        get
        {
            if (cells != null && cells.Count >= 2)
            {
                return cells[1] - cells[0];
            }
            return Vector2Int.right;
        }
    }

    public Vector2Int ExitDirection
    {
        get
        {
            if (cells != null && cells.Count >= 2)
            {
                return cells[cells.Count - 1] - cells[cells.Count - 2];
            }
            return Vector2Int.right;
        }
    }

    /// <summary>
    /// Initializes the pipe from reference level data.
    /// Documents conversion: If PipeCount > 0, displayed uses = PipeCount; otherwise 1 use.
    /// </summary>
    public void Initialize(List<Vector2Int> orderedCells, int rawPipeCount)
    {
        this.cells = orderedCells != null ? new List<Vector2Int>(orderedCells) : new List<Vector2Int>();
        this.pipeCount = rawPipeCount;

        // Authoritative conversion rule:
        // If PipeCount > 0 (e.g. Level 45 has PipeCount 2), remaining uses is that count.
        // If PipeCount <= 0 (e.g. Level 21 single-pass), remaining uses defaults to 1.
        this.remainingUses = rawPipeCount > 0 ? rawPipeCount : 1;

        BuildVisuals();
    }

    /// <summary>
    /// Rebuilds pipe geometry, collars, shadow, and badge from PipeVisualConfig settings.
    /// </summary>
    [ContextMenu("Rebuild Pipe Visuals")]
    public void BuildVisuals()
    {
        if (cells == null || cells.Count < 2) return;

        // Clean up previous children
        if (entranceCollarObj != null) DestroyImmediate(entranceCollarObj);
        if (exitCollarObj != null) DestroyImmediate(exitCollarObj);
        if (shadowObj != null) DestroyImmediate(shadowObj);
        if (badgeRoot != null && badgeRoot.gameObject != null) DestroyImmediate(badgeRoot.gameObject);

        BuildPipeMesh();
        BuildEndCollars();
        BuildContactShadow();
        CreateUsageBadge();
    }

    /// <summary>
    /// Attempts to reserve one usage slot for an ant queue traversing this pipe.
    /// Prevents concurrent queues from exceeding the remaining capacity of the pipe.
    /// </summary>
    public bool TryReserveUse()
    {
        if (IsExhausted || (remainingUses - activeReservations <= 0))
        {
            return false;
        }
        activeReservations++;
        return true;
    }

    /// <summary>
    /// Releases a previously reserved usage slot (e.g. if move was blocked or canceled).
    /// </summary>
    public void ReleaseReservedUse()
    {
        activeReservations = Mathf.Max(0, activeReservations - 1);
        if (remainingUses <= 0 && activeReservations <= 0)
        {
            PlayExhaustedAndDestroy();
        }
    }

    /// <summary>
    /// Checks whether an ant heading in forwardDir can legally enter this pipe at targetCell.
    /// Entry is only accepted at EntranceCell in EntryDirection. Side/backward entries rejected.
    /// </summary>
    public bool CanEnterFrom(Vector2Int targetCell, Vector2Int forwardDir)
    {
        if (IsExhausted || (remainingUses - activeReservations <= 0)) return false;
        return targetCell == EntranceCell && forwardDir == EntryDirection;
    }

    /// <summary>
    /// Returns true if this cell is occupied by any segment of this pipe.
    /// </summary>
    public bool Occupies(Vector2Int cell)
    {
        return cells != null && cells.Contains(cell);
    }

    /// <summary>
    /// Decreases usage by 1 after successful passage. If exhausted, removes pipe.
    /// Blocked taps do not consume uses. Does not destroy pipe if another queue is actively traversing.
    /// </summary>
    public void ConsumeUse()
    {
        activeReservations = Mathf.Max(0, activeReservations - 1);
        if (remainingUses > 0)
        {
            remainingUses--;
            UpdateBadgeUI(animate: true);
        }

        if (remainingUses <= 0 && activeReservations <= 0)
        {
            PlayExhaustedAndDestroy();
        }
    }

    private void UpdateBadgeUI(bool animate)
    {
        if (counterText != null)
        {
            counterText.text = remainingUses.ToString();
        }

        if (animate && badgeRoot != null)
        {
            badgeRoot.DOKill();
            badgeRoot.localScale = Vector3.one * 0.008f;
            badgeRoot.DOPunchScale(Vector3.one * 0.003f, 0.22f, 8, 0.5f);
        }
    }

    private void PlayExhaustedAndDestroy()
    {
        // Unregister cells from board immediately so footprints are not left as obstacles
        if (BoardManager.Instance != null)
        {
            BoardManager.Instance.RemovePipe(this);
        }

        transform.DOKill();
        transform.DOScale(transform.localScale * 1.15f, 0.12f).SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                transform.DOScale(Vector3.zero, 0.22f).SetEase(Ease.InBack)
                    .OnComplete(() =>
                    {
                        Destroy(gameObject);
                    });
            });
    }

    /// <summary>
    /// Returns the continuous world-space centerline waypoints with smooth circular elbow curves at corners.
    /// Preserved for ant queue path generation to maintain identical traversal movement and speed.
    /// </summary>
    public List<Vector3> GetCenterlineWaypoints(float yOffset = 0f)
    {
        List<Vector3> points = new List<Vector3>();
        if (cells == null || cells.Count < 2) return points;

        float cs = 1.0f;
        if (BoardManager.Instance != null && BoardManager.Instance.CellSize > 0)
        {
            cs = BoardManager.Instance.CellSize;
        }

        float rElbow = 0.35f * cs;
        int arcSubdivisions = 8;

        List<Vector3> gridWorldPoints = new List<Vector3>();
        for (int i = 0; i < cells.Count; i++)
        {
            Vector3 w = BoardManager.Instance != null
                ? BoardManager.Instance.GridToWorldPosition(cells[i], yOffset)
                : new Vector3(cells[i].x, yOffset, cells[i].y);
            gridWorldPoints.Add(w);
        }

        points.Add(gridWorldPoints[0]);

        for (int i = 1; i < gridWorldPoints.Count - 1; i++)
        {
            Vector3 prev = gridWorldPoints[i - 1];
            Vector3 curr = gridWorldPoints[i];
            Vector3 next = gridWorldPoints[i + 1];

            Vector3 u = (curr - prev).normalized;
            Vector3 v = (next - curr).normalized;

            if (Vector3.Dot(u, v) > 0.99f)
            {
                points.Add(curr);
            }
            else
            {
                Vector3 pStart = curr - u * rElbow;
                Vector3 pEnd = curr + v * rElbow;
                Vector3 cArc = curr - u * rElbow + v * rElbow;

                points.Add(pStart);
                for (int step = 1; step < arcSubdivisions; step++)
                {
                    float theta = (float)step / arcSubdivisions * (Mathf.PI * 0.5f);
                    Vector3 pt = cArc - v * (rElbow * Mathf.Cos(theta)) + u * (rElbow * Mathf.Sin(theta));
                    pt.y = gridWorldPoints[0].y;
                    points.Add(pt);
                }
                points.Add(pEnd);
            }
        }

        points.Add(gridWorldPoints[gridWorldPoints.Count - 1]);
        return points;
    }

    private void BuildPipeMesh()
    {
        meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null) meshFilter = gameObject.AddComponent<MeshFilter>();

        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null) meshRenderer = gameObject.AddComponent<MeshRenderer>();

        PipeVisualConfig config = PipeVisualConfig.Instance;
        float cs = (BoardManager.Instance != null && BoardManager.Instance.CellSize > 0) ? BoardManager.Instance.CellSize : 1.0f;

        Vector3 rootWorldPos = BoardManager.Instance != null
            ? BoardManager.Instance.GridToWorldPosition(cells[0])
            : new Vector3(cells[0].x, 0f, cells[0].y);

        transform.position = rootWorldPos;

        // Material setup
        Material pipeMat = config.customPipeMaterial;
        if (pipeMat == null)
        {
#if UNITY_EDITOR
            pipeMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Ants/Materials/PipeConduit_URPLit.mat");
#endif
            if (pipeMat == null)
            {
                pipeMat = Resources.Load<Material>("PipeConduit_URPLit");
            }
        }
        if (pipeMat == null)
        {
            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            pipeMat = new Material(urpShader);
            Texture2D gradTex = null;
#if UNITY_EDITOR
            gradTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Ants/Textures/PipeConduit_Gradient.png");
#endif
            if (gradTex == null) gradTex = Resources.Load<Texture2D>("PipeConduit_Gradient");
            if (gradTex != null) pipeMat.SetTexture("_BaseMap", gradTex);
            pipeMat.SetColor("_BaseColor", Color.white);
            if (pipeMat.HasProperty("_Smoothness")) pipeMat.SetFloat("_Smoothness", config.smoothness);
            if (pipeMat.HasProperty("_Metallic")) pipeMat.SetFloat("_Metallic", 0.05f);
        }

        // Configure controlled transparent surface for translucent inside-ant visibility
        if (pipeMat != null)
        {
            Material runMat = Application.isPlaying ? new Material(pipeMat) : pipeMat;
            runMat.SetFloat("_Surface", 1.0f); // Transparent
            runMat.SetFloat("_Blend", 0.0f);   // Alpha
            runMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            runMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            runMat.SetFloat("_ZWrite", 0.0f);
            runMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent; // 3000
            runMat.SetOverrideTag("RenderType", "Transparent");
            Color baseCol = runMat.HasProperty("_BaseColor") ? runMat.GetColor("_BaseColor") : Color.white;
            baseCol.a = config.pipeConduitOpacity;
            runMat.SetColor("_BaseColor", baseCol);
            runMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            pipeMat = runMat;
        }
        meshRenderer.sharedMaterial = pipeMat;

        // Generate procedural square mitered beveled conduit mesh
        meshFilter.sharedMesh = GenerateSquareMiteredPipeMesh(cells, cs, config.pipeWidth, config.pipeHeight, config.bevelWidth, config.bevelHeight, rootWorldPos);
    }

    private UnityEngine.Mesh GenerateSquareMiteredPipeMesh(List<Vector2Int> cellList, float cs, float pipeW, float pipeH, float bevelW, float bevelH, Vector3 rootWorldPos)
    {
        UnityEngine.Mesh mesh = new UnityEngine.Mesh { name = "SquareMiteredConduitMesh" };
        float w = pipeW * 0.5f;
        float yTop = pipeH;
        float ySide = pipeH - bevelH;
        float fChamfer = Mathf.Clamp01((w - bevelW) / w);

        int n = cellList.Count;
        List<Vector3> segDirs = new List<Vector3>();
        List<Vector3> worldCellPositions = new List<Vector3>();

        for (int i = 0; i < n; i++)
        {
            Vector3 wPos = BoardManager.Instance != null
                ? BoardManager.Instance.GridToWorldPosition(cellList[i])
                : new Vector3(cellList[i].x * cs, 0f, cellList[i].y * cs);
            worldCellPositions.Add(wPos - rootWorldPos);
        }

        for (int i = 0; i < n - 1; i++)
        {
            segDirs.Add((worldCellPositions[i + 1] - worldCellPositions[i]).normalized);
        }

        // Build path slices
        List<Vector3> sliceCenters = new List<Vector3>();
        List<Vector3> sliceMiters = new List<Vector3>();

        // 1. Entrance mouth slice (extended by 0.5 cell backwards from cell 0)
        Vector3 p0 = worldCellPositions[0] - 0.5f * cs * segDirs[0];
        Vector3 n0 = new Vector3(-segDirs[0].z, 0f, segDirs[0].x);
        sliceCenters.Add(p0);
        sliceMiters.Add(n0);

        // 2. Interior cell slices
        for (int i = 0; i < n; i++)
        {
            Vector3 wi = worldCellPositions[i];
            if (i == 0)
            {
                sliceCenters.Add(wi);
                sliceMiters.Add(n0);
            }
            else if (i == n - 1)
            {
                Vector3 nLast = new Vector3(-segDirs[n - 2].z, 0f, segDirs[n - 2].x);
                sliceCenters.Add(wi);
                sliceMiters.Add(nLast);
            }
            else
            {
                Vector3 u = segDirs[i - 1];
                Vector3 v = segDirs[i];
                if (Vector3.Dot(u, v) > 0.99f)
                {
                    // Straight run
                    Vector3 nStraight = new Vector3(-u.z, 0f, u.x);
                    sliceCenters.Add(wi);
                    sliceMiters.Add(nStraight);
                }
                else
                {
                    // 90-degree corner: exact angle bisector miter vector
                    Vector3 nu = new Vector3(-u.z, 0f, u.x);
                    Vector3 nv = new Vector3(-v.z, 0f, v.x);
                    Vector3 miter = nu + nv;
                    sliceCenters.Add(wi);
                    sliceMiters.Add(miter);
                }
            }
        }

        // 3. Exit mouth slice (extended by 0.5 cell forwards from last cell)
        Vector3 pEnd = worldCellPositions[n - 1] + 0.5f * cs * segDirs[n - 2];
        Vector3 nExit = new Vector3(-segDirs[n - 2].z, 0f, segDirs[n - 2].x);
        sliceCenters.Add(pEnd);
        sliceMiters.Add(nExit);

        // Calculate continuous longitudinal V distance along conduit
        List<float> sliceV = new List<float>();
        float accumV = 0f;
        sliceV.Add(0f);
        for (int s = 0; s < sliceCenters.Count - 1; s++)
        {
            accumV += Vector3.Distance(sliceCenters[s], sliceCenters[s + 1]) / cs;
            sliceV.Add(accumV);
        }

        // Generate faceted quads across slices
        List<Vector3> verts = new List<Vector3>();
        List<Vector3> norms = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<Color> colors = new List<Color>();
        List<int> tris = new List<int>();

        // Lateral profile: 6 points defining 5 longitudinal facets
        float[] fCoords = new float[] { -1.0f, -1.0f, -fChamfer, fChamfer, 1.0f, 1.0f };
        float[] yCoords = new float[] { 0.0f, ySide, yTop, yTop, ySide, 0.0f };
        float[] uFacetBorders = new float[] { 0.00f, 0.18f, 0.38f, 0.62f, 0.82f, 1.00f };

        // 5 facets:
        // 0: Left vertical side wall (pt 0 -> 1)
        // 1: Left chamfer bevel facet (pt 1 -> 2)
        // 2: Top horizontal surface (pt 2 -> 3)
        // 3: Right chamfer bevel facet (pt 3 -> 4)
        // 4: Right vertical side wall (pt 4 -> 5)

        for (int s = 0; s < sliceCenters.Count - 1; s++)
        {
            Vector3 cA = sliceCenters[s];
            Vector3 mA = sliceMiters[s];
            Vector3 cB = sliceCenters[s + 1];
            Vector3 mB = sliceMiters[s + 1];

            Vector3 segDir = (cB - cA).normalized;
            Vector3 segLeft = new Vector3(-segDir.z, 0f, segDir.x);

            float vA = sliceV[s];
            float vB = sliceV[s + 1];

            for (int f = 0; f < 5; f++)
            {
                int pA1 = f;
                int pA2 = f + 1;

                Vector3 v0 = cA + fCoords[pA1] * w * mA + Vector3.up * yCoords[pA1];
                Vector3 v1 = cA + fCoords[pA2] * w * mA + Vector3.up * yCoords[pA2];
                Vector3 v2 = cB + fCoords[pA2] * w * mB + Vector3.up * yCoords[pA2];
                Vector3 v3 = cB + fCoords[pA1] * w * mB + Vector3.up * yCoords[pA1];

                // Face normal
                Vector3 norm;
                if (f == 2) norm = Vector3.up; // top face strictly up
                else if (f == 0) norm = -segLeft; // left wall strictly left
                else if (f == 4) norm = segLeft;  // right wall strictly right
                else if (f == 1) norm = (-segLeft + Vector3.up).normalized; // left chamfer
                else norm = (segLeft + Vector3.up).normalized; // right chamfer

                Color col = (f == 2) ? new Color(0.08f, 0.72f, 0.92f, 1f) :
                            (f == 1 || f == 3) ? new Color(0.28f, 0.88f, 1.0f, 1f) :
                            new Color(0.02f, 0.35f, 0.65f, 1f);

                int idx = verts.Count;
                verts.Add(v0); verts.Add(v1); verts.Add(v2); verts.Add(v3);
                norms.Add(norm); norms.Add(norm); norms.Add(norm); norms.Add(norm);
                colors.Add(col); colors.Add(col); colors.Add(col); colors.Add(col);

                float u0 = uFacetBorders[f];
                float u1 = uFacetBorders[f + 1];
                uvs.Add(new Vector2(u0, vA));
                uvs.Add(new Vector2(u1, vA));
                uvs.Add(new Vector2(u1, vB));
                uvs.Add(new Vector2(u0, vB));

                // Front-facing clockwise winding (upward/outward)
                tris.Add(idx); tris.Add(idx + 1); tris.Add(idx + 2);
                tris.Add(idx); tris.Add(idx + 2); tris.Add(idx + 3);
            }
        }

        mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetColors(colors);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    private void BuildEndCollars()
    {
        PipeVisualConfig config = PipeVisualConfig.Instance;
        if (!config.enableCollars || cells == null || cells.Count < 2) return;

        float cs = (BoardManager.Instance != null && BoardManager.Instance.CellSize > 0) ? BoardManager.Instance.CellSize : 1.0f;
        Vector3 rootWorldPos = transform.position;

        Vector3 w0 = BoardManager.Instance != null
            ? BoardManager.Instance.GridToWorldPosition(cells[0])
            : new Vector3(cells[0].x * cs, 0f, cells[0].y * cs);
        Vector3 w1 = BoardManager.Instance != null
            ? BoardManager.Instance.GridToWorldPosition(cells[1])
            : new Vector3(cells[1].x * cs, 0f, cells[1].y * cs);
        Vector3 d0 = (w1 - w0).normalized;

        Vector3 wLast = BoardManager.Instance != null
            ? BoardManager.Instance.GridToWorldPosition(cells[cells.Count - 1])
            : new Vector3(cells[cells.Count - 1].x * cs, 0f, cells[cells.Count - 1].y * cs);
        Vector3 wPrev = BoardManager.Instance != null
            ? BoardManager.Instance.GridToWorldPosition(cells[cells.Count - 2])
            : new Vector3(cells[cells.Count - 2].x * cs, 0f, cells[cells.Count - 2].y * cs);
        Vector3 dLast = (wLast - wPrev).normalized;

        // Entrance mouth collar: placed at outer mouth boundary, oriented perpendicular to d0
        Vector3 entPos = (w0 - 0.5f * cs * d0) - rootWorldPos;
        entranceCollarObj = CreateCollarObject("EntranceCollar", entPos, d0, config);

        // Exit mouth collar: placed at outer mouth boundary, oriented perpendicular to dLast
        Vector3 exitPos = (wLast + 0.5f * cs * dLast) - rootWorldPos;
        exitCollarObj = CreateCollarObject("ExitCollar", exitPos, -dLast, config);
    }

    private GameObject CreateCollarObject(string name, Vector3 localPos, Vector3 forwardDir, PipeVisualConfig config)
    {
        GameObject collar = new GameObject(name);
        collar.transform.SetParent(transform, false);
        collar.transform.localPosition = localPos;
        collar.transform.localRotation = Quaternion.LookRotation(forwardDir, Vector3.up);

        var mf = collar.AddComponent<MeshFilter>();
        var mr = collar.AddComponent<MeshRenderer>();

        // Collar material: pale-cyan toon material matching reference
        Shader shader = Shader.Find("Toony Colors Pro 2/Hybrid Shader") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material mat = new Material(shader);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", config.collarColor);
        else mat.color = config.collarColor;
        if (mat.HasProperty("_HColor")) mat.SetColor("_HColor", Color.white);
        if (mat.HasProperty("_SColor")) mat.SetColor("_SColor", config.highlightColor);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.92f);
        mr.sharedMaterial = mat;

        // Narrow rectangular collar rim mesh
        mf.sharedMesh = GenerateCollarMesh(config.collarWidth, config.collarHeight, config.collarThickness);
        return collar;
    }

    private UnityEngine.Mesh GenerateCollarMesh(float cW, float cH, float cThickness)
    {
        UnityEngine.Mesh mesh = new UnityEngine.Mesh { name = "PipeCollarRimMesh" };
        float hw = cW * 0.5f;
        float hInW = hw * 0.76f;
        float hInH = cH * 0.76f;
        float rEnd = cThickness * 0.45f;

        List<Vector3> verts = new List<Vector3>();
        List<Vector3> norms = new List<Vector3>();
        List<int> tris = new List<int>();

        void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 n)
        {
            int idx = verts.Count;
            verts.Add(a); verts.Add(b); verts.Add(c); verts.Add(d);
            norms.Add(n); norms.Add(n); norms.Add(n); norms.Add(n);
            tris.Add(idx); tris.Add(idx + 1); tris.Add(idx + 2);
            tris.Add(idx); tris.Add(idx + 2); tris.Add(idx + 3);
        }

        float zFront = 0f;
        float zBack = cThickness;

        // Front face: framing the opening
        Vector3 oBL = new Vector3(-hw + rEnd, 0f, zFront);
        Vector3 oTL = new Vector3(-hw + rEnd, cH, zFront);
        Vector3 oTR = new Vector3(hw - rEnd, cH, zFront);
        Vector3 oBR = new Vector3(hw - rEnd, 0f, zFront);

        Vector3 iBL = new Vector3(-hInW, 0f, zFront);
        Vector3 iTL = new Vector3(-hInW, hInH, zFront);
        Vector3 iTR = new Vector3(hInW, hInH, zFront);
        Vector3 iBR = new Vector3(hInW, 0f, zFront);

        Vector3 nFwd = -Vector3.forward;
        AddQuad(oBL, oTL, iTL, iBL, nFwd);
        AddQuad(iTL, oTL, oTR, iTR, nFwd);
        AddQuad(iBR, iTR, oTR, oBR, nFwd);

        // Top surface (rounded pill contour with chamfered top)
        Vector3 bTL = new Vector3(-hw + rEnd, cH, zBack);
        Vector3 bTR = new Vector3(hw - rEnd, cH, zBack);
        AddQuad(oTL, bTL, bTR, oTR, Vector3.up);

        // Rounded left cap
        Vector3 oLC = new Vector3(-hw, cH * 0.5f, zFront + rEnd);
        Vector3 bLC = new Vector3(-hw, cH * 0.5f, zBack - rEnd);
        AddQuad(new Vector3(-hw, 0f, zFront + rEnd), new Vector3(-hw, cH, zFront + rEnd), oTL, oBL, -Vector3.right);
        AddQuad(new Vector3(-hw, 0f, zBack - rEnd), bTL, new Vector3(-hw, cH, zBack - rEnd), new Vector3(-hw, 0f, zBack), -Vector3.right);
        AddQuad(new Vector3(-hw, 0f, zFront + rEnd), new Vector3(-hw, cH, zFront + rEnd), new Vector3(-hw, cH, zBack - rEnd), new Vector3(-hw, 0f, zBack - rEnd), -Vector3.right);

        // Rounded right cap
        AddQuad(oBR, oTR, new Vector3(hw, cH, zFront + rEnd), new Vector3(hw, 0f, zFront + rEnd), Vector3.right);
        AddQuad(new Vector3(hw, 0f, zBack - rEnd), new Vector3(hw, cH, zBack - rEnd), bTR, new Vector3(hw, 0f, zBack), Vector3.right);
        AddQuad(new Vector3(hw, 0f, zFront + rEnd), new Vector3(hw, cH, zFront + rEnd), new Vector3(hw, cH, zBack - rEnd), new Vector3(hw, 0f, zBack - rEnd), Vector3.right);

        mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    private void BuildContactShadow()
    {
        PipeVisualConfig config = PipeVisualConfig.Instance;
        if (!config.enableShadow || cells == null || cells.Count < 2) return;

        shadowObj = new GameObject("ContactShadow");
        shadowObj.transform.SetParent(transform, false);
        shadowObj.transform.localPosition = new Vector3(0f, 0.003f, 0f);

        var mf = shadowObj.AddComponent<MeshFilter>();
        var mr = shadowObj.AddComponent<MeshRenderer>();

        Shader shadowShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
        Material shadowMat = new Material(shadowShader);
        Color sCol = config.shadowColorRgb;
        sCol.a = config.shadowAlpha;
        if (shadowMat.HasProperty("_BaseColor")) shadowMat.SetColor("_BaseColor", sCol);
        else shadowMat.color = sCol;

        Texture2D shadowTex = null;
#if UNITY_EDITOR
        shadowTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Ants/Textures/PipeShadow_Ribbon.png");
#endif
        if (shadowTex == null) shadowTex = Resources.Load<Texture2D>("PipeShadow_Ribbon");
        if (shadowTex != null)
        {
            if (shadowMat.HasProperty("_BaseMap")) shadowMat.SetTexture("_BaseMap", shadowTex);
            else shadowMat.mainTexture = shadowTex;
        }

        // Enable transparency blending
        shadowMat.SetFloat("_Surface", 1); // Transparent in URP
        shadowMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        shadowMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        shadowMat.SetInt("_ZWrite", 0);
        shadowMat.renderQueue = 2999;
        mr.sharedMaterial = shadowMat;

        // Procedural flat shadow ribbon along pipe centerline
        float cs = (BoardManager.Instance != null && BoardManager.Instance.CellSize > 0) ? BoardManager.Instance.CellSize : 1.0f;
        mf.sharedMesh = GenerateContactShadowMesh(cells, cs, config.shadowWidth, transform.position);
    }

    private UnityEngine.Mesh GenerateContactShadowMesh(List<Vector2Int> cellList, float cs, float shadowW, Vector3 rootWorldPos)
    {
        UnityEngine.Mesh mesh = new UnityEngine.Mesh { name = "PipeContactShadowMesh" };
        float hw = shadowW * 0.5f;
        int n = cellList.Count;

        List<Vector3> worldCellPositions = new List<Vector3>();
        for (int i = 0; i < n; i++)
        {
            Vector3 wPos = BoardManager.Instance != null
                ? BoardManager.Instance.GridToWorldPosition(cellList[i])
                : new Vector3(cellList[i].x * cs, 0f, cellList[i].y * cs);
            worldCellPositions.Add(wPos - rootWorldPos);
        }

        List<Vector3> segDirs = new List<Vector3>();
        for (int i = 0; i < n - 1; i++)
        {
            segDirs.Add((worldCellPositions[i + 1] - worldCellPositions[i]).normalized);
        }

        List<Vector3> sliceCenters = new List<Vector3>();
        List<Vector3> sliceMiters = new List<Vector3>();

        Vector3 p0 = worldCellPositions[0] - 0.52f * cs * segDirs[0];
        Vector3 n0 = new Vector3(-segDirs[0].z, 0f, segDirs[0].x);
        sliceCenters.Add(p0);
        sliceMiters.Add(n0);

        for (int i = 0; i < n; i++)
        {
            Vector3 wi = worldCellPositions[i];
            if (i == 0)
            {
                sliceCenters.Add(wi); sliceMiters.Add(n0);
            }
            else if (i == n - 1)
            {
                Vector3 nLast = new Vector3(-segDirs[n - 2].z, 0f, segDirs[n - 2].x);
                sliceCenters.Add(wi); sliceMiters.Add(nLast);
            }
            else
            {
                Vector3 u = segDirs[i - 1];
                Vector3 v = segDirs[i];
                if (Vector3.Dot(u, v) > 0.99f)
                {
                    sliceCenters.Add(wi);
                    sliceMiters.Add(new Vector3(-u.z, 0f, u.x));
                }
                else
                {
                    Vector3 nu = new Vector3(-u.z, 0f, u.x);
                    Vector3 nv = new Vector3(-v.z, 0f, v.x);
                    sliceCenters.Add(wi);
                    sliceMiters.Add(nu + nv);
                }
            }
        }

        Vector3 pEnd = worldCellPositions[n - 1] + 0.52f * cs * segDirs[n - 2];
        Vector3 nExit = new Vector3(-segDirs[n - 2].z, 0f, segDirs[n - 2].x);
        sliceCenters.Add(pEnd);
        sliceMiters.Add(nExit);

        List<Vector3> verts = new List<Vector3>();
        List<Vector3> norms = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> tris = new List<int>();

        for (int s = 0; s < sliceCenters.Count - 1; s++)
        {
            Vector3 cA = sliceCenters[s];
            Vector3 mA = sliceMiters[s];
            Vector3 cB = sliceCenters[s + 1];
            Vector3 mB = sliceMiters[s + 1];

            Vector3 v0 = cA - hw * mA;
            Vector3 v1 = cA + hw * mA;
            Vector3 v2 = cB + hw * mB;
            Vector3 v3 = cB - hw * mB;

            int idx = verts.Count;
            verts.Add(v0); verts.Add(v1); verts.Add(v2); verts.Add(v3);
            norms.Add(Vector3.up); norms.Add(Vector3.up); norms.Add(Vector3.up); norms.Add(Vector3.up);
            uvs.Add(new Vector2(0f, 0f));
            uvs.Add(new Vector2(1f, 0f));
            uvs.Add(new Vector2(1f, 1f));
            uvs.Add(new Vector2(0f, 1f));

            tris.Add(idx); tris.Add(idx + 1); tris.Add(idx + 2);
            tris.Add(idx); tris.Add(idx + 2); tris.Add(idx + 3);
        }

        mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    private void CreateUsageBadge()
    {
        PipeVisualConfig config = PipeVisualConfig.Instance;
        if (!config.enableBadge || cells == null || cells.Count == 0) return;

        float cs = (BoardManager.Instance != null && BoardManager.Instance.CellSize > 0) ? BoardManager.Instance.CellSize : 1.0f;
        Vector3 rootWorldPos = transform.position;

        // Logical configured entrance is strictly cells[0] (EntranceCell)
        Vector3 w0 = BoardManager.Instance != null
            ? BoardManager.Instance.GridToWorldPosition(EntranceCell)
            : new Vector3(EntranceCell.x * cs, 0f, EntranceCell.y * cs);

        Vector3 d0 = new Vector3(EntryDirection.x, 0f, EntryDirection.y).normalized;
        if (d0.sqrMagnitude < 0.001f) d0 = Vector3.right;

        // Entrance mouth is at w0 - 0.5 * cs * d0 (at the entrance collar)
        Vector3 mouthPos = w0 - 0.5f * cs * d0;

        // Lateral side perpendicular to entry direction (outer edge beside collar)
        Vector3 lateralDir = Vector3.Cross(Vector3.up, d0).normalized;
        if (lateralDir.sqrMagnitude < 0.001f) lateralDir = Vector3.forward;

        // Position beside entrance collar: slightly forward from mouth into pipe, and lateral to outer side
        // so it never covers entering ants in the center, and never blocks the visible mouth opening
        float fwdOffset = ConfiguredEntranceForwardOffset * cs;
        float latOffset = ConfiguredEntranceLateralOffset * cs;
        float hOffset = ConfiguredEntranceHeightOffset;

        Vector3 badgeWorldPos = mouthPos + d0 * fwdOffset + lateralDir * latOffset + Vector3.up * hOffset;
        Vector3 badgeLocalPos = badgeWorldPos - rootWorldPos;

        GameObject canvasObj = new GameObject("PipeUsageBadge");
        canvasObj.transform.SetParent(transform, false);
        canvasObj.transform.localPosition = badgeLocalPos;

        badgeCanvas = canvasObj.AddComponent<Canvas>();
        badgeCanvas.renderMode = RenderMode.WorldSpace;
        badgeCanvas.sortingOrder = 5; // Guaranteed beneath modals (sorting order 50+)

        badgeRoot = canvasObj.GetComponent<RectTransform>();
        badgeRoot.sizeDelta = new Vector2(config.badgeSize, config.badgeSize);

        // Independent world scale, unaffected by pipe length or parent scaling
        float targetScale = ConfiguredBadgeScale;
        Vector3 parentLossy = transform.lossyScale;
        badgeRoot.localScale = new Vector3(
            parentLossy.x > 0.0001f ? targetScale / parentLossy.x : targetScale,
            parentLossy.y > 0.0001f ? targetScale / parentLossy.y : targetScale,
            parentLossy.z > 0.0001f ? targetScale / parentLossy.z : targetScale
        );

        // Keep text upright and readable for every pipe orientation (aligned with camera view)
        canvasObj.transform.rotation = Quaternion.Euler(60f, 0f, 0f);

        // Load 9-slice squircle sprite for soft rounded corners
        Sprite squircleSprite = Resources.Load<Sprite>("Badge_Squircle");
        if (squircleSprite == null)
        {
            Texture2D squircleTex = Resources.Load<Texture2D>("Badge_Squircle");
#if UNITY_EDITOR
            if (squircleTex == null)
                squircleTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Badge_Squircle.png");
#endif
            if (squircleTex != null)
            {
                squircleSprite = Sprite.Create(squircleTex, new Rect(0, 0, squircleTex.width, squircleTex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(24, 24, 24, 24));
            }
        }

        // 1. Light cyan border plate
        GameObject borderObj = new GameObject("BadgeBorder");
        borderObj.transform.SetParent(badgeRoot, false);
        var borderRect = borderObj.AddComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.sizeDelta = Vector2.zero;
        var borderImg = borderObj.AddComponent<Image>();
        borderImg.raycastTarget = false; // Non-blocking to gameplay input
        if (squircleSprite != null)
        {
            borderImg.sprite = squircleSprite;
            borderImg.type = Image.Type.Sliced;
        }
        borderImg.color = config.badgeBorderColor;

        // 2. Deep blue inner face plate (inset 4px)
        GameObject faceObj = new GameObject("BadgeFace");
        faceObj.transform.SetParent(badgeRoot, false);
        var faceRect = faceObj.AddComponent<RectTransform>();
        faceRect.anchorMin = Vector2.zero;
        faceRect.anchorMax = Vector2.one;
        faceRect.offsetMin = new Vector2(4f, 4f);
        faceRect.offsetMax = new Vector2(-4f, -4f);
        var faceImg = faceObj.AddComponent<Image>();
        faceImg.raycastTarget = false; // Non-blocking to gameplay input
        if (squircleSprite != null)
        {
            faceImg.sprite = squircleSprite;
            faceImg.type = Image.Type.Sliced;
        }
        faceImg.color = config.badgeFaceColor;

        // 3. Crisp white numeral with strong dark outline matching reference
        GameObject textObj = new GameObject("CounterText");
        textObj.transform.SetParent(badgeRoot, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        counterText = textObj.AddComponent<Text>();
        counterText.raycastTarget = false; // Non-blocking to gameplay input
        counterText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        int fs = ConfiguredNumeralSize;
        counterText.fontSize = (remainingUses >= 10) ? Mathf.RoundToInt(fs * 0.82f) : fs;
        counterText.fontStyle = FontStyle.Bold;
        counterText.alignment = TextAnchor.MiddleCenter;
        counterText.color = config.badgeTextColor;
        counterText.text = remainingUses.ToString();

        // Strong dark outline for high-contrast readability
        var outline = textObj.AddComponent<Outline>();
        outline.effectColor = config.badgeOutlineColor;
        outline.effectDistance = new Vector2(2.5f, -2.5f);
    }

    /// <summary>
    /// Calculates the visual opacity and tint for an ant at world position traversing this pipe.
    /// Returns 1.0f if the ant is outside the pipe, or smoothly transitions to insidePipeAntOpacity with insidePipeAntTint.
    /// </summary>
    public float GetAntVisualOpacity(Vector3 antWorldPos, out Color tint)
    {
        PipeVisualConfig config = PipeVisualConfig.Instance;
        tint = (config != null) ? config.insidePipeAntTint : Color.white;
        float baseInsideOpacity = (config != null) ? config.insidePipeAntOpacity : 0.40f;

        if (cells == null || cells.Count < 2)
        {
            return 1.0f;
        }

        float cs = (BoardManager.Instance != null && BoardManager.Instance.CellSize > 0) ? BoardManager.Instance.CellSize : 1.0f;
        Vector3 w0 = BoardManager.Instance != null
            ? BoardManager.Instance.GridToWorldPosition(cells[0])
            : new Vector3(cells[0].x * cs, 0f, cells[0].y * cs);
        Vector3 wLast = BoardManager.Instance != null
            ? BoardManager.Instance.GridToWorldPosition(cells[cells.Count - 1])
            : new Vector3(cells[cells.Count - 1].x * cs, 0f, cells[cells.Count - 1].y * cs);

        Vector3 entFwd = new Vector3(EntryDirection.x, 0f, EntryDirection.y).normalized;
        Vector3 exitFwd = new Vector3(ExitDirection.x, 0f, ExitDirection.y).normalized;

        Vector3 entMouth = w0 - 0.5f * cs * entFwd;
        Vector3 exitMouth = wLast + 0.5f * cs * exitFwd;

        // Check if ant is within lateral conduit envelope
        float minCenterlineDistSq = float.MaxValue;
        for (int i = 0; i < cells.Count - 1; i++)
        {
            Vector3 a = BoardManager.Instance != null
                ? BoardManager.Instance.GridToWorldPosition(cells[i])
                : new Vector3(cells[i].x * cs, 0f, cells[i].y * cs);
            Vector3 b = BoardManager.Instance != null
                ? BoardManager.Instance.GridToWorldPosition(cells[i + 1])
                : new Vector3(cells[i + 1].x * cs, 0f, cells[i + 1].y * cs);
            a.y = antWorldPos.y;
            b.y = antWorldPos.y;
            Vector3 ab = b - a;
            float tSeg = Mathf.Clamp01(Vector3.Dot(antWorldPos - a, ab) / Mathf.Max(0.001f, ab.sqrMagnitude));
            Vector3 proj = a + ab * tSeg;
            float distSq = (antWorldPos - proj).sqrMagnitude;
            if (distSq < minCenterlineDistSq) minCenterlineDistSq = distSq;
        }

        float maxLateralReach = (config != null ? config.pipeWidth : 0.84f) * cs;
        if (minCenterlineDistSq > maxLateralReach * maxLateralReach)
        {
            return 1.0f;
        }

        float distPastEntrance = Vector3.Dot(antWorldPos - entMouth, entFwd);
        float distToExit = Vector3.Dot(exitMouth - antWorldPos, exitFwd);

        if (distPastEntrance < -0.05f) return 1.0f;
        if (distToExit < -0.05f) return 1.0f;

        float transitionDist = 0.35f * cs;
        if (distPastEntrance < transitionDist)
        {
            float t = Mathf.Clamp01(distPastEntrance / transitionDist);
            return Mathf.Lerp(1.0f, baseInsideOpacity, t);
        }

        if (distToExit < transitionDist)
        {
            float t = Mathf.Clamp01(distToExit / transitionDist);
            return Mathf.Lerp(1.0f, baseInsideOpacity, t);
        }

        return baseInsideOpacity;
    }
}
