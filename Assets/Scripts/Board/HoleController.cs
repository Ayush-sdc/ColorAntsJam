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

    [Header("Visual Components")]
    [SerializeField] private Transform slotRoot;
    [SerializeField] private GameObject countBadgeObj;
    [SerializeField] private TextMesh countTextMesh;
    [SerializeField] private MeshRenderer badgeBackgroundRenderer;
    [SerializeField] private MeshRenderer rimMeshRenderer;

    private static GameObject swallowVfxPrefab;
    private static Sprite capsuleSprite;

    public int ColorID => colorId;
    public AntColor Color => holeColor;
    public Vector2Int GridCoordinate => gridCoordinate;
    public IReadOnlyList<Vector2Int> OccupiedCells => (occupiedCells != null && occupiedCells.Count > 0) ? occupiedCells : new List<Vector2Int> { gridCoordinate };
    public int RemainingCount => remainingCount;
    public int InitialCapacity => initialCapacity;
    public bool IsFull => remainingCount <= 0;
    public int ParallelRows => parallelRows;
    public bool IsRightSide
    {
        get => isRightSide;
        set
        {
            isRightSide = value;
            if (!IsPrefabAsset())
            {
                BuildStadiumSlotVisuals(isRightSide, parallelRows);
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
                          slotRoot.childCount > 0 && 
                          rimMeshRenderer != null && 
                          rimMeshRenderer.sharedMaterial != null &&
                          countTextMesh != null;

        if (!hasVisuals)
        {
            BuildStadiumSlotVisuals(isRightSide, parallelRows);
        }
        else
        {
            UpdateBadgeDisplay();
        }
    }

    public void Initialize(int rawColorId, Vector2Int coordinate, List<Vector2Int> allCells, int capacity = 1, bool isRightSideSlot = true, int rowCount = 1)
    {
        this.colorId = rawColorId;
        this.holeColor = ReferenceColorMapper.ToAntColor(rawColorId);
        this.gridCoordinate = coordinate;
        this.initialCapacity = Mathf.Max(0, capacity);
        this.remainingCount = initialCapacity;
        this.isRightSide = isRightSideSlot;
        this.parallelRows = Mathf.Max(1, rowCount);

        if (allCells != null && allCells.Count > 1)
        {
            this.occupiedCells = new List<Vector2Int>(allCells);
        }
        else
        {
            this.occupiedCells = new List<Vector2Int>();
            this.occupiedCells.Add(coordinate);
            if (this.parallelRows >= 2)
            {
                this.occupiedCells.Add(coordinate + new Vector2Int(1, 0));
                this.occupiedCells.Add(coordinate + new Vector2Int(0, 1));
                this.occupiedCells.Add(coordinate + new Vector2Int(1, 1));
            }
            else
            {
                int xDir = this.isRightSide ? 1 : -1;
                this.occupiedCells.Add(coordinate + new Vector2Int(xDir, 0));
            }
        }

        if (!IsPrefabAsset())
        {
            BuildStadiumSlotVisuals(isRightSideSlot, this.parallelRows);
            UpdateBadgeDisplay();
        }
    }

    public void Initialize(AntColor color, Vector2Int coordinate, int capacity = 1, bool isRightSideSlot = true, int rowCount = 1)
    {
        Initialize((int)color, coordinate, new List<Vector2Int> { coordinate }, capacity, isRightSideSlot, rowCount);
    }

    public static Sprite GetCapsuleSprite()
    {
        if (capsuleSprite != null) return capsuleSprite;

        int w = 256;
        int h = 512;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.name = "CapsuleShadowTexture";
        Color[] colors = new Color[w * h];
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
                colors[y * w + x] = new Color(1f, 1f, 1f, alpha);
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
        BuildStadiumSlotVisuals(isRightSide, parallelRows);
        UpdateBadgeDisplay();
    }

    public void BuildStadiumSlotVisuals(bool rightSide, int rows)
    {
        if (IsPrefabAsset()) return;

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

        float outerW = (rows >= 2) ? multiRowWidth : singleRowWidth;
        float outerH = (rows >= 2) ? multiRowHeight : singleRowHeight;

        Material rimMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        rimMat.color = AntColorPalette.GetColor(holeColor);
        if (rimMat.HasProperty("_BaseColor")) rimMat.SetColor("_BaseColor", AntColorPalette.GetColor(holeColor));
        if (rimMat.HasProperty("_Smoothness")) rimMat.SetFloat("_Smoothness", 0.94f);
        if (rimMat.HasProperty("_Metallic")) rimMat.SetFloat("_Metallic", 0.05f);

        Material pitMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        pitMat.color = UnityEngine.Color.black;
        if (pitMat.HasProperty("_BaseColor")) pitMat.SetColor("_BaseColor", UnityEngine.Color.black);
        if (pitMat.HasProperty("_Smoothness")) pitMat.SetFloat("_Smoothness", 0.12f);

        // 1. Tubular Stadium Pill / Rounded-Rectangle Rim
        GameObject rimObj = new GameObject("SlotRim");
        rimObj.transform.SetParent(slotRoot, false);
        rimObj.transform.localPosition = Vector3.zero;

        MeshFilter rimFilter = rimObj.AddComponent<MeshFilter>();
        rimFilter.sharedMesh = CreateStadiumTubeMesh(outerW, outerH, tubeRadius, tubeHeight, 64, 16, rows);
        rimMeshRenderer = rimObj.AddComponent<MeshRenderer>();
        rimMeshRenderer.material = rimMat;

        // 2. Dark Recessed Stadium Pit Floor
        GameObject pitObj = new GameObject("SlotPit");
        pitObj.transform.SetParent(slotRoot, false);
        pitObj.transform.localPosition = Vector3.zero;

        MeshFilter pitFilter = pitObj.AddComponent<MeshFilter>();
        pitFilter.sharedMesh = CreateStadiumPitMesh(outerW, outerH, tubeRadius, 0.012f, 24, rows);
        var pitRenderer = pitObj.AddComponent<MeshRenderer>();
        pitRenderer.material = pitMat;

        // 2b. Inner Capsule Shadow / Pit Sprite (Single-row only)
        if (rows < 2)
        {
            GameObject capsuleObj = new GameObject("Capsule");
            capsuleObj.transform.SetParent(pitObj.transform, false);
            capsuleObj.transform.localPosition = new Vector3(0.02f, 0.014f, 0f);
            capsuleObj.transform.localRotation = Quaternion.Euler(90f, 0f, 90f);

            float capScaleX = 0.64208f;
            float capScaleY = 0.92615f;
            capsuleObj.transform.localScale = new Vector3(capScaleX, capScaleY, 1f);

            SpriteRenderer sr = capsuleObj.AddComponent<SpriteRenderer>();
            sr.sprite = GetCapsuleSprite();
            sr.color = UnityEngine.Color.black;
            sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            sr.receiveShadows = false;
        }

        // 3. Integrated Counter Badge
        countBadgeObj = new GameObject("SlotCounterBadge");
        countBadgeObj.transform.SetParent(slotRoot, false);
        countBadgeObj.transform.localPosition = Vector3.zero;

        GameObject badgeMeshObj = new GameObject("BadgeMesh");
        badgeMeshObj.transform.SetParent(countBadgeObj.transform, false);
        badgeMeshObj.transform.localPosition = Vector3.zero;

        MeshFilter badgeFilter = badgeMeshObj.AddComponent<MeshFilter>();
        badgeFilter.sharedMesh = CreateStadiumBadgeMesh(outerW, outerH, tubeRadius, 0.018f, rightSide, rows, 24);
        badgeBackgroundRenderer = badgeMeshObj.AddComponent<MeshRenderer>();
        badgeBackgroundRenderer.material = rimMat;

        // Number Text
        GameObject textObj = new GameObject("BadgeCountText");
        textObj.transform.SetParent(countBadgeObj.transform, false);

        float rCap = (rows >= 2) ? 0.36f : Mathf.Max(0.08f, (outerH * 0.5f) - tubeRadius);
        float straightLen = (rows >= 2) ? Mathf.Max(0.1f, outerW - 2f * rCap) : Mathf.Max(0f, outerW - 2f * (rCap + tubeRadius));
        float halfL = straightLen * 0.5f;
        float rInner = Mathf.Max(0.04f, rCap - tubeRadius);

        float badgeLeftX = (rows >= 2) ? (halfL - 0.20f) : (halfL - 0.10f);
        float centerBadgeX = (badgeLeftX + (halfL + rInner)) * 0.5f;

        float textX = rightSide ? centerBadgeX : -centerBadgeX;
        float textY = 0.22f;
        float textZ = 0f;
        textObj.transform.localPosition = new Vector3(textX, textY, textZ);
        textObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        countTextMesh = textObj.AddComponent<TextMesh>();
        countTextMesh.alignment = TextAlignment.Center;
        countTextMesh.anchor = TextAnchor.MiddleCenter;
        countTextMesh.fontSize = 62;
        countTextMesh.characterSize = (rows >= 2) ? 0.080f : 0.092f;
        countTextMesh.fontStyle = FontStyle.Bold;
        countTextMesh.color = UnityEngine.Color.white;
    }

    public static Mesh CreateStadiumTubeMesh(float outerW, float outerH, float tubeRadius, float tubeHeight, int pathResolution = 64, int crossResolution = 16, int rows = 1)
    {
        Mesh mesh = new Mesh();
        mesh.name = (rows >= 2) ? "RoundedRectTubeMesh" : "StadiumTubeMesh";

        List<Vector3> pathPoints = new List<Vector3>();
        List<Vector3> pathNormals = new List<Vector3>();

        if (rows >= 2)
        {
            // Proper 4-sided rounded-corner rectangle for 2-row multi-queue holders
            float rCorner = 0.36f;
            float straightW = Mathf.Max(0.1f, outerW - 2f * rCorner);
            float straightH = Mathf.Max(0.1f, outerH - 2f * rCorner);
            float halfLx = straightW * 0.5f;
            float halfLz = straightH * 0.5f;
            int segRes = Mathf.Max(3, pathResolution / 16);
            int arcRes = Mathf.Max(6, pathResolution / 8);

            // 1. Top straight (from left to right)
            for (int i = 0; i < segRes; i++)
            {
                float t = (float)i / segRes;
                float x = Mathf.Lerp(-halfLx, halfLx, t);
                pathPoints.Add(new Vector3(x, 0f, halfLz + rCorner));
                pathNormals.Add(Vector3.forward);
            }
            // 2. Top-Right corner (PI/2 -> 0)
            for (int i = 0; i < arcRes; i++)
            {
                float t = (float)i / arcRes;
                float ang = Mathf.PI * 0.5f - t * Mathf.PI * 0.5f;
                pathPoints.Add(new Vector3(halfLx + rCorner * Mathf.Cos(ang), 0f, halfLz + rCorner * Mathf.Sin(ang)));
                pathNormals.Add(new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)).normalized);
            }
            // 3. Right straight (from top to bottom)
            for (int i = 0; i < segRes; i++)
            {
                float t = (float)i / segRes;
                float z = Mathf.Lerp(halfLz, -halfLz, t);
                pathPoints.Add(new Vector3(halfLx + rCorner, 0f, z));
                pathNormals.Add(Vector3.right);
            }
            // 4. Bottom-Right corner (0 -> -PI/2)
            for (int i = 0; i < arcRes; i++)
            {
                float t = (float)i / arcRes;
                float ang = 0f - t * Mathf.PI * 0.5f;
                pathPoints.Add(new Vector3(halfLx + rCorner * Mathf.Cos(ang), 0f, -halfLz + rCorner * Mathf.Sin(ang)));
                pathNormals.Add(new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)).normalized);
            }
            // 5. Bottom straight (from right to left)
            for (int i = 0; i < segRes; i++)
            {
                float t = (float)i / segRes;
                float x = Mathf.Lerp(halfLx, -halfLx, t);
                pathPoints.Add(new Vector3(x, 0f, -halfLz - rCorner));
                pathNormals.Add(Vector3.back);
            }
            // 6. Bottom-Left corner (-PI/2 -> -PI)
            for (int i = 0; i < arcRes; i++)
            {
                float t = (float)i / arcRes;
                float ang = -Mathf.PI * 0.5f - t * Mathf.PI * 0.5f;
                pathPoints.Add(new Vector3(-halfLx + rCorner * Mathf.Cos(ang), 0f, -halfLz + rCorner * Mathf.Sin(ang)));
                pathNormals.Add(new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)).normalized);
            }
            // 7. Left straight (from bottom to top)
            for (int i = 0; i < segRes; i++)
            {
                float t = (float)i / segRes;
                float z = Mathf.Lerp(-halfLz, halfLz, t);
                pathPoints.Add(new Vector3(-halfLx - rCorner, 0f, z));
                pathNormals.Add(Vector3.left);
            }
            // 8. Top-Left corner (PI -> PI/2)
            for (int i = 0; i < arcRes; i++)
            {
                float t = (float)i / arcRes;
                float ang = Mathf.PI - t * Mathf.PI * 0.5f;
                pathPoints.Add(new Vector3(-halfLx + rCorner * Mathf.Cos(ang), 0f, halfLz + rCorner * Mathf.Sin(ang)));
                pathNormals.Add(new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)).normalized);
            }
        }
        else
        {
            // Standard single-row horizontal stadium pill
            float rCap = Mathf.Max(0.08f, (outerH * 0.5f) - tubeRadius);
            float straightLen = Mathf.Max(0f, outerW - 2f * (rCap + tubeRadius));
            float halfL = straightLen * 0.5f;

            int straightRes = Mathf.Max(4, pathResolution / 8);
            int arcRes = Mathf.Max(12, (pathResolution - 2 * straightRes) / 2);

            for (int i = 0; i < straightRes; i++)
            {
                float t = (float)i / straightRes;
                float x = Mathf.Lerp(-halfL, halfL, t);
                pathPoints.Add(new Vector3(x, 0f, rCap));
                pathNormals.Add(Vector3.forward);
            }

            for (int i = 0; i < arcRes; i++)
            {
                float t = (float)i / arcRes;
                float ang = Mathf.PI * 0.5f - t * Mathf.PI;
                pathPoints.Add(new Vector3(halfL + rCap * Mathf.Cos(ang), 0f, rCap * Mathf.Sin(ang)));
                pathNormals.Add(new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)).normalized);
            }

            for (int i = 0; i < straightRes; i++)
            {
                float t = (float)i / straightRes;
                float x = Mathf.Lerp(halfL, -halfL, t);
                pathPoints.Add(new Vector3(x, 0f, -rCap));
                pathNormals.Add(Vector3.back);
            }

            for (int i = 0; i < arcRes; i++)
            {
                float t = (float)i / arcRes;
                float ang = -Mathf.PI * 0.5f - t * Mathf.PI;
                pathPoints.Add(new Vector3(-halfL + rCap * Mathf.Cos(ang), 0f, rCap * Mathf.Sin(ang)));
                pathNormals.Add(new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)).normalized);
            }
        }

        int M = pathPoints.Count;
        int K = crossResolution;
        Vector3[] vertices = new Vector3[M * (K + 1)];
        Vector3[] normals = new Vector3[M * (K + 1)];
        Vector2[] uvs = new Vector2[M * (K + 1)];

        for (int i = 0; i < M; i++)
        {
            Vector3 center = pathPoints[i];
            Vector3 norm2D = pathNormals[i];

            for (int j = 0; j <= K; j++)
            {
                float phi = ((float)j / K) * Mathf.PI;
                float cosPhi = Mathf.Cos(phi);
                float sinPhi = Mathf.Sin(phi);

                Vector3 pos = center + norm2D * (-cosPhi * tubeRadius) + Vector3.up * (sinPhi * tubeHeight);
                Vector3 n = (norm2D * (-cosPhi) + Vector3.up * sinPhi).normalized;

                int vIdx = i * (K + 1) + j;
                vertices[vIdx] = pos;
                normals[vIdx] = n;
                uvs[vIdx] = new Vector2((float)i / M, (float)j / K);
            }
        }

        int[] triangles = new int[M * K * 6];
        int tIdx = 0;

        for (int i = 0; i < M; i++)
        {
            int nextI = (i + 1) % M;
            for (int j = 0; j < K; j++)
            {
                int v00 = i * (K + 1) + j;
                int v01 = i * (K + 1) + j + 1;
                int v10 = nextI * (K + 1) + j;
                int v11 = nextI * (K + 1) + j + 1;

                triangles[tIdx++] = v00;
                triangles[tIdx++] = v01;
                triangles[tIdx++] = v10;

                triangles[tIdx++] = v01;
                triangles[tIdx++] = v11;
                triangles[tIdx++] = v10;
            }
        }

        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        return mesh;
    }

    public static Mesh CreateStadiumPitMesh(float outerW, float outerH, float tubeRadius, float yOffset = 0.012f, int arcSegments = 24, int rows = 1)
    {
        Mesh mesh = new Mesh();
        mesh.name = (rows >= 2) ? "RoundedRectPitMesh" : "StadiumPitMesh";

        List<Vector3> verts = new List<Vector3>();
        List<Vector3> norms = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> tris = new List<int>();

        verts.Add(new Vector3(0f, yOffset, 0f));
        norms.Add(Vector3.up);
        uvs.Add(new Vector2(0.5f, 0.5f));

        List<Vector3> perimeter = new List<Vector3>();

        if (rows >= 2)
        {
            float rCorner = 0.36f;
            float rInner = Mathf.Max(0.04f, rCorner - tubeRadius);
            float straightW = Mathf.Max(0.1f, outerW - 2f * rCorner);
            float straightH = Mathf.Max(0.1f, outerH - 2f * rCorner);
            float halfLx = straightW * 0.5f;
            float halfLz = straightH * 0.5f;
            int arcRes = Mathf.Max(4, arcSegments / 4);

            perimeter.Add(new Vector3(-halfLx, yOffset, halfLz + rInner));
            perimeter.Add(new Vector3(halfLx, yOffset, halfLz + rInner));

            for (int i = 0; i <= arcRes; i++)
            {
                float ang = Mathf.PI * 0.5f - ((float)i / arcRes) * Mathf.PI * 0.5f;
                perimeter.Add(new Vector3(halfLx + rInner * Mathf.Cos(ang), yOffset, halfLz + rInner * Mathf.Sin(ang)));
            }

            perimeter.Add(new Vector3(halfLx + rInner, yOffset, -halfLz));

            for (int i = 0; i <= arcRes; i++)
            {
                float ang = 0f - ((float)i / arcRes) * Mathf.PI * 0.5f;
                perimeter.Add(new Vector3(halfLx + rInner * Mathf.Cos(ang), yOffset, -halfLz + rInner * Mathf.Sin(ang)));
            }

            perimeter.Add(new Vector3(-halfLx, yOffset, -halfLz - rInner));

            for (int i = 0; i <= arcRes; i++)
            {
                float ang = -Mathf.PI * 0.5f - ((float)i / arcRes) * Mathf.PI * 0.5f;
                perimeter.Add(new Vector3(-halfLx + rInner * Mathf.Cos(ang), yOffset, -halfLz + rInner * Mathf.Sin(ang)));
            }

            perimeter.Add(new Vector3(-halfLx - rInner, yOffset, halfLz));

            for (int i = 0; i <= arcRes; i++)
            {
                float ang = Mathf.PI - ((float)i / arcRes) * Mathf.PI * 0.5f;
                perimeter.Add(new Vector3(-halfLx + rInner * Mathf.Cos(ang), yOffset, halfLz + rInner * Mathf.Sin(ang)));
            }
        }
        else
        {
            float rCap = Mathf.Max(0.08f, (outerH * 0.5f) - tubeRadius);
            float rInner = Mathf.Max(0.04f, rCap - tubeRadius);
            float straightLen = Mathf.Max(0f, outerW - 2f * (rCap + tubeRadius));
            float halfL = straightLen * 0.5f;

            perimeter.Add(new Vector3(halfL, yOffset, rInner));
            perimeter.Add(new Vector3(-halfL, yOffset, rInner));

            for (int i = 0; i <= arcSegments; i++)
            {
                float ang = Mathf.PI * 0.5f + ((float)i / arcSegments) * Mathf.PI;
                perimeter.Add(new Vector3(-halfL + rInner * Mathf.Cos(ang), yOffset, rInner * Mathf.Sin(ang)));
            }

            perimeter.Add(new Vector3(-halfL, yOffset, -rInner));
            perimeter.Add(new Vector3(halfL, yOffset, -rInner));

            for (int i = 0; i <= arcSegments; i++)
            {
                float ang = -Mathf.PI * 0.5f + ((float)i / arcSegments) * Mathf.PI;
                perimeter.Add(new Vector3(halfL + rInner * Mathf.Cos(ang), yOffset, rInner * Mathf.Sin(ang)));
            }
        }

        int perimStartIdx = verts.Count;
        for (int i = 0; i < perimeter.Count; i++)
        {
            verts.Add(perimeter[i]);
            norms.Add(Vector3.up);
            uvs.Add(new Vector2((perimeter[i].x + outerW * 0.5f) / outerW, (perimeter[i].z + outerH * 0.5f) / outerH));
        }

        for (int i = 0; i < perimeter.Count - 1; i++)
        {
            tris.Add(0);
            tris.Add(perimStartIdx + i);
            tris.Add(perimStartIdx + i + 1);
        }
        tris.Add(0);
        tris.Add(perimStartIdx + perimeter.Count - 1);
        tris.Add(perimStartIdx);

        mesh.vertices = verts.ToArray();
        mesh.normals = norms.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.triangles = tris.ToArray();
        mesh.RecalculateBounds();
        return mesh;
    }

    public static Mesh CreateStadiumBadgeMesh(float outerW, float outerH, float tubeRadius, float yOffset = 0.018f, bool rightSide = true, int rows = 1, int arcSegments = 24)
    {
        Mesh mesh = new Mesh();
        mesh.name = rightSide ? "StadiumBadgeMesh_Right" : "StadiumBadgeMesh_Left";

        float rCap = (rows >= 2) ? 0.36f : Mathf.Max(0.08f, (outerH * 0.5f) - tubeRadius);
        float rInner = Mathf.Max(0.04f, rCap - tubeRadius);
        float straightLen = (rows >= 2) ? Mathf.Max(0.1f, outerW - 2f * rCap) : Mathf.Max(0f, outerW - 2f * (rCap + tubeRadius));
        float halfL = straightLen * 0.5f;

        float badgeLeftX = (rows >= 2) ? (halfL - 0.20f) : (halfL - 0.10f);
        float cornerR = 0.06f;
        float centerBadgeX = (badgeLeftX + (halfL + rInner)) * 0.5f;

        List<Vector3> verts = new List<Vector3>();
        List<Vector3> norms = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> tris = new List<int>();

        List<Vector3> perimeter = new List<Vector3>();

        if (rightSide)
        {
            verts.Add(new Vector3(centerBadgeX, yOffset, 0f));
            norms.Add(Vector3.up);
            uvs.Add(new Vector2(0.5f, 0.5f));

            perimeter.Add(new Vector3(halfL, yOffset, rInner));
            perimeter.Add(new Vector3(badgeLeftX + cornerR, yOffset, rInner));

            for (int i = 0; i <= 6; i++)
            {
                float ang = Mathf.PI * 0.5f + ((float)i / 6) * Mathf.PI * 0.5f;
                perimeter.Add(new Vector3(badgeLeftX + cornerR + cornerR * Mathf.Cos(ang), yOffset, rInner - cornerR + cornerR * Mathf.Sin(ang)));
            }

            perimeter.Add(new Vector3(badgeLeftX, yOffset, -rInner + cornerR));

            for (int i = 0; i <= 6; i++)
            {
                float ang = Mathf.PI + ((float)i / 6) * Mathf.PI * 0.5f;
                perimeter.Add(new Vector3(badgeLeftX + cornerR + cornerR * Mathf.Cos(ang), yOffset, -rInner + cornerR + cornerR * Mathf.Sin(ang)));
            }

            perimeter.Add(new Vector3(halfL, yOffset, -rInner));

            for (int i = 0; i <= arcSegments; i++)
            {
                float ang = -Mathf.PI * 0.5f + ((float)i / arcSegments) * Mathf.PI;
                perimeter.Add(new Vector3(halfL + rInner * Mathf.Cos(ang), yOffset, rInner * Mathf.Sin(ang)));
            }

            int pStart = verts.Count;
            for (int i = 0; i < perimeter.Count; i++)
            {
                verts.Add(perimeter[i]);
                norms.Add(Vector3.up);
                uvs.Add(new Vector2(perimeter[i].x, perimeter[i].z));
            }

            for (int i = 0; i < perimeter.Count - 1; i++)
            {
                tris.Add(0);
                tris.Add(pStart + i);
                tris.Add(pStart + i + 1);
            }
            tris.Add(0);
            tris.Add(pStart + perimeter.Count - 1);
            tris.Add(pStart);
        }
        else
        {
            verts.Add(new Vector3(-centerBadgeX, yOffset, 0f));
            norms.Add(Vector3.up);
            uvs.Add(new Vector2(0.5f, 0.5f));

            perimeter.Add(new Vector3(-halfL, yOffset, rInner));
            perimeter.Add(new Vector3(-badgeLeftX - cornerR, yOffset, rInner));

            for (int i = 0; i <= 6; i++)
            {
                float ang = Mathf.PI * 0.5f - ((float)i / 6) * Mathf.PI * 0.5f;
                perimeter.Add(new Vector3(-badgeLeftX - cornerR + cornerR * Mathf.Cos(ang), yOffset, rInner - cornerR + cornerR * Mathf.Sin(ang)));
            }

            perimeter.Add(new Vector3(-badgeLeftX, yOffset, -rInner + cornerR));

            for (int i = 0; i <= 6; i++)
            {
                float ang = 0f - ((float)i / 6) * Mathf.PI * 0.5f;
                perimeter.Add(new Vector3(-badgeLeftX - cornerR + cornerR * Mathf.Cos(ang), yOffset, -rInner + cornerR + cornerR * Mathf.Sin(ang)));
            }

            perimeter.Add(new Vector3(-halfL, yOffset, -rInner));

            for (int i = 0; i <= arcSegments; i++)
            {
                float ang = -Mathf.PI * 0.5f - ((float)i / arcSegments) * Mathf.PI;
                perimeter.Add(new Vector3(-halfL + rInner * Mathf.Cos(ang), yOffset, rInner * Mathf.Sin(ang)));
            }

            int pStart = verts.Count;
            for (int i = 0; i < perimeter.Count; i++)
            {
                verts.Add(perimeter[i]);
                norms.Add(Vector3.up);
                uvs.Add(new Vector2(perimeter[i].x, perimeter[i].z));
            }

            for (int i = 0; i < perimeter.Count - 1; i++)
            {
                tris.Add(0);
                tris.Add(pStart + i + 1);
                tris.Add(pStart + i);
            }
            tris.Add(0);
            tris.Add(pStart);
            tris.Add(pStart + perimeter.Count - 1);
        }

        mesh.vertices = verts.ToArray();
        mesh.normals = norms.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.triangles = tris.ToArray();
        mesh.RecalculateBounds();
        return mesh;
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
        remainingCount = Mathf.Max(0, remainingCount - 1);
        UpdateBadgeDisplay();
        PlaySwallowEffect();
        SpawnSwallowVFX();

        if (remainingCount == 0)
        {
            PlayCompletedEffect();
        }

        if (SoundManager.Instance != null && Application.isPlaying)
        {
            SoundManager.Instance.PlaySwallow();
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
                BuildStadiumSlotVisuals(isRightSide, parallelRows);
                UpdateBadgeDisplay();
            }
        };
    }
#endif
}
