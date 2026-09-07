using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

[ExecuteAlways]
public class HoleController : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private AntColor holeColor = AntColor.Red;
    [SerializeField] private Vector2Int gridCoordinate;
    [SerializeField] private int initialCapacity = 1;
    [SerializeField] private int remainingCount = 1;
    [Tooltip("True if hole is on the right side (ants enter from left, badge on right). False if hole is on the left side (ants enter from right, badge on left).")]
    [SerializeField] private bool isRightSide = true;
    [SerializeField] private int parallelRows = 1;

    [Header("Stadium Dimensions")]
    [SerializeField] private float singleRowWidth = 2.05f;
    [SerializeField] private float singleRowHeight = 0.88f;
    [SerializeField] private float multiRowWidth = 2.05f;
    [SerializeField] private float multiRowHeight = 1.90f;
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

    public AntColor Color => holeColor;
    public Vector2Int GridCoordinate => gridCoordinate;
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

        bool hasCapsule = slotRoot != null && slotRoot.Find("SlotPit/Capsule") != null; bool needsRebuild = !hasCapsule || slotRoot == null || 
                            slotRoot.childCount == 0 || 
                            rimMeshRenderer == null || 
                            rimMeshRenderer.sharedMaterial == null ||
                            countTextMesh == null;

        if (needsRebuild)
        {
            BuildStadiumSlotVisuals(isRightSide, parallelRows);
        }
        else
        {
            UpdateBadgeDisplay();
        }
    }

    public void Initialize(AntColor color, Vector2Int coordinate, int capacity = 1, bool isRightSideSlot = true, int rowCount = 1)
    {
        this.holeColor = color;
        this.gridCoordinate = coordinate;
        this.initialCapacity = Mathf.Max(0, capacity);
        this.remainingCount = initialCapacity;
        this.isRightSide = isRightSideSlot;
        this.parallelRows = Mathf.Max(1, rowCount);

        if (!IsPrefabAsset())
        {
            BuildStadiumSlotVisuals(isRightSideSlot, this.parallelRows);
            UpdateBadgeDisplay();
        }
    }

    /// <summary>
    /// Returns or generates a crisp anti-aliased 2D capsule sprite with normalized PPU (256) -> width 1.0, height 2.0.
    /// </summary>
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

    /// <summary>
    /// Builds the sleek horizontal stadium capsule slot matching HOLEE and reference photo:
    /// 1. Smooth tubular torus rim (SlotRim) in glossy color.
    /// 2. Recessed dark stadium pit cavity floor (SlotPit) in pure black.
    /// 3. Inner Capsule Shadow Sprite (SlotPit > Capsule) with transform (0.02, 0, 0), rotation (90, 0, 90), scale (0.64208, 0.92615, 1).
    /// 4. Integrated counter badge always positioned OPPOSITE to the ant-facing entrance side of the hole.
    /// 5. Crisp bold white number text.
    /// </summary>
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

        // Clean up lingering child objects
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            SafeDestroy(transform.GetChild(i).gameObject);
        }

        slotRoot = new GameObject("HoleSlotRoot").transform;
        slotRoot.SetParent(transform, false);
        slotRoot.localPosition = Vector3.zero;
        slotRoot.localRotation = Quaternion.identity;

        // Dimensions
        float outerW = (rows >= 2) ? multiRowWidth : singleRowWidth;
        float outerH = (rows >= 2) ? multiRowHeight : singleRowHeight;

        Material rimMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        rimMat.color = AntColorPalette.GetColor(holeColor);
        if (rimMat.HasProperty("_Smoothness")) rimMat.SetFloat("_Smoothness", 0.94f);
        if (rimMat.HasProperty("_Metallic")) rimMat.SetFloat("_Metallic", 0.05f);

        Material pitMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        pitMat.color = UnityEngine.Color.black;
        if (pitMat.HasProperty("_BaseColor")) pitMat.SetColor("_BaseColor", UnityEngine.Color.black);
        if (pitMat.HasProperty("_Smoothness")) pitMat.SetFloat("_Smoothness", 0.12f);

        // 1. Tubular Stadium Pill Rim
        GameObject rimObj = new GameObject("SlotRim");
        rimObj.transform.SetParent(slotRoot, false);
        rimObj.transform.localPosition = Vector3.zero;

        MeshFilter rimFilter = rimObj.AddComponent<MeshFilter>();
        rimFilter.sharedMesh = CreateStadiumTubeMesh(outerW, outerH, tubeRadius, tubeHeight, 64, 16);
        rimMeshRenderer = rimObj.AddComponent<MeshRenderer>();
        rimMeshRenderer.material = rimMat;

        // 2. Dark Recessed Stadium Pit Floor
        GameObject pitObj = new GameObject("SlotPit");
        pitObj.transform.SetParent(slotRoot, false);
        pitObj.transform.localPosition = Vector3.zero;

        MeshFilter pitFilter = pitObj.AddComponent<MeshFilter>();
        pitFilter.sharedMesh = CreateStadiumPitMesh(outerW, outerH, tubeRadius, 0.012f, 24);
        var pitRenderer = pitObj.AddComponent<MeshRenderer>();
        pitRenderer.material = pitMat;

        // 2b. Inner Capsule Shadow / Pit Sprite (SlotPit > Capsule) matching HOLEE
        GameObject capsuleObj = new GameObject("Capsule");
        capsuleObj.transform.SetParent(pitObj.transform, false);
        capsuleObj.transform.localPosition = new Vector3(0.02f, 0.014f, 0f);
        capsuleObj.transform.localRotation = Quaternion.Euler(90f, 0f, 90f);

        float capScaleX = (rows >= 2) ? 1.45f : 0.64208f;
        float capScaleY = 0.92615f;
        capsuleObj.transform.localScale = new Vector3(capScaleX, capScaleY, 1f);

        SpriteRenderer sr = capsuleObj.AddComponent<SpriteRenderer>();
        sr.sprite = GetCapsuleSprite();
        sr.color = UnityEngine.Color.black;
        sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        sr.receiveShadows = false;

        // 3. Integrated Counter Badge (Placed opposite to the ant facing entrance)
        countBadgeObj = new GameObject("SlotCounterBadge");
        countBadgeObj.transform.SetParent(slotRoot, false);
        countBadgeObj.transform.localPosition = Vector3.zero;

        // Badge patch mesh
        GameObject badgeMeshObj = new GameObject("BadgeMesh");
        badgeMeshObj.transform.SetParent(countBadgeObj.transform, false);
        badgeMeshObj.transform.localPosition = Vector3.zero;

        MeshFilter badgeFilter = badgeMeshObj.AddComponent<MeshFilter>();
        badgeFilter.sharedMesh = CreateStadiumBadgeMesh(outerW, outerH, tubeRadius, 0.018f, rightSide, rows, 24);
        badgeBackgroundRenderer = badgeMeshObj.AddComponent<MeshRenderer>();
        badgeBackgroundRenderer.material = rimMat;

        // Number Text (Facing top-down camera)
        GameObject textObj = new GameObject("BadgeCountText");
        textObj.transform.SetParent(countBadgeObj.transform, false);

        float rCap = Mathf.Max(0.08f, (outerH * 0.5f) - tubeRadius);
        float straightLen = Mathf.Max(0f, outerW - 2f * (rCap + tubeRadius));
        float halfL = straightLen * 0.5f;
        float rInner = Mathf.Max(0.04f, rCap - tubeRadius);

        float badgeLeftX = (rows >= 2) ? (halfL - 0.20f) : (halfL - 0.10f);
        float centerBadgeX = (badgeLeftX + (halfL + rInner)) * 0.5f;

        float textX = rightSide ? centerBadgeX : -centerBadgeX;
        float textZ = 0f;
        textObj.transform.localPosition = new Vector3(textX, 0.035f, textZ);
        textObj.transform.localRotation = Quaternion.Euler(90f, 0f, 90f);

        countTextMesh = textObj.AddComponent<TextMesh>();
        countTextMesh.alignment = TextAlignment.Center;
        countTextMesh.anchor = TextAnchor.MiddleCenter;
        countTextMesh.fontSize = 62;
        countTextMesh.characterSize = (rows >= 2) ? 0.080f : 0.092f;
        countTextMesh.fontStyle = FontStyle.Bold;
        countTextMesh.color = UnityEngine.Color.white;
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(gameObject);
        if (slotRoot != null)
        {
            UnityEditor.EditorUtility.SetDirty(slotRoot.gameObject);
            foreach (Transform t in slotRoot.GetComponentsInChildren<Transform>(true))
            {
                UnityEditor.EditorUtility.SetDirty(t.gameObject);
            }
        }
#endif
    }

    /// <summary>
    /// Generates a perfectly smooth continuous tubular stadium/capsule rim mesh.
    /// </summary>
    public static Mesh CreateStadiumTubeMesh(float outerW, float outerH, float tubeRadius, float tubeHeight, int pathResolution = 64, int crossResolution = 16)
    {
        Mesh mesh = new Mesh();
        mesh.name = "StadiumTubeMesh";

        float rCap = Mathf.Max(0.08f, (outerH * 0.5f) - tubeRadius);
        float straightLen = Mathf.Max(0f, outerW - 2f * (rCap + tubeRadius));
        float halfL = straightLen * 0.5f;

        int straightRes = Mathf.Max(4, pathResolution / 8);
        int arcRes = Mathf.Max(12, (pathResolution - 2 * straightRes) / 2);
        int totalPathPoints = 2 * straightRes + 2 * arcRes;

        List<Vector3> pathPoints = new List<Vector3>(totalPathPoints);
        List<Vector3> pathNormals = new List<Vector3>(totalPathPoints);

        // 1. Top straight line (-halfL -> +halfL)
        for (int i = 0; i < straightRes; i++)
        {
            float t = (float)i / straightRes;
            float x = Mathf.Lerp(-halfL, halfL, t);
            pathPoints.Add(new Vector3(x, 0f, rCap));
            pathNormals.Add(Vector3.forward);
        }

        // 2. Right semicircle (+pi/2 -> -pi/2)
        for (int i = 0; i < arcRes; i++)
        {
            float t = (float)i / arcRes;
            float ang = Mathf.PI * 0.5f - t * Mathf.PI;
            float x = halfL + rCap * Mathf.Cos(ang);
            float z = rCap * Mathf.Sin(ang);
            pathPoints.Add(new Vector3(x, 0f, z));
            pathNormals.Add(new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)).normalized);
        }

        // 3. Bottom straight line (+halfL -> -halfL)
        for (int i = 0; i < straightRes; i++)
        {
            float t = (float)i / straightRes;
            float x = Mathf.Lerp(halfL, -halfL, t);
            pathPoints.Add(new Vector3(x, 0f, -rCap));
            pathNormals.Add(Vector3.back);
        }

        // 4. Left semicircle (-pi/2 -> -3pi/2)
        for (int i = 0; i < arcRes; i++)
        {
            float t = (float)i / arcRes;
            float ang = -Mathf.PI * 0.5f - t * Mathf.PI;
            float x = -halfL + rCap * Mathf.Cos(ang);
            float z = rCap * Mathf.Sin(ang);
            pathPoints.Add(new Vector3(x, 0f, z));
            pathNormals.Add(new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)).normalized);
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
                float phi = ((float)j / K) * Mathf.PI; // 0 to PI
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

    /// <summary>
    /// Generates the flat inner stadium pit floor mesh.
    /// </summary>
    public static Mesh CreateStadiumPitMesh(float outerW, float outerH, float tubeRadius, float yOffset = 0.012f, int arcSegments = 24)
    {
        Mesh mesh = new Mesh();
        mesh.name = "StadiumPitMesh";

        float rCap = Mathf.Max(0.08f, (outerH * 0.5f) - tubeRadius);
        float rInner = Mathf.Max(0.04f, rCap - tubeRadius);
        float straightLen = Mathf.Max(0f, outerW - 2f * (rCap + tubeRadius));
        float halfL = straightLen * 0.5f;

        List<Vector3> verts = new List<Vector3>();
        List<Vector3> norms = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> tris = new List<int>();

        // Center origin vertex
        verts.Add(new Vector3(0f, yOffset, 0f));
        norms.Add(Vector3.up);
        uvs.Add(new Vector2(0.5f, 0.5f));

        List<Vector3> perimeter = new List<Vector3>();

        // Top line (+halfL -> -halfL)
        perimeter.Add(new Vector3(halfL, yOffset, rInner));
        perimeter.Add(new Vector3(-halfL, yOffset, rInner));

        // Left semicircle (pi/2 -> 3pi/2)
        for (int i = 0; i <= arcSegments; i++)
        {
            float ang = Mathf.PI * 0.5f + ((float)i / arcSegments) * Mathf.PI;
            perimeter.Add(new Vector3(-halfL + rInner * Mathf.Cos(ang), yOffset, rInner * Mathf.Sin(ang)));
        }

        // Bottom line (-halfL -> +halfL)
        perimeter.Add(new Vector3(-halfL, yOffset, -rInner));
        perimeter.Add(new Vector3(halfL, yOffset, -rInner));

        // Right semicircle (-pi/2 -> +pi/2)
        for (int i = 0; i <= arcSegments; i++)
        {
            float ang = -Mathf.PI * 0.5f + ((float)i / arcSegments) * Mathf.PI;
            perimeter.Add(new Vector3(halfL + rInner * Mathf.Cos(ang), yOffset, rInner * Mathf.Sin(ang)));
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

    /// <summary>
    /// Generates the integrated badge mesh sitting inside the end cap of the stadium cavity
    /// (placed strictly opposite to the ant facing entrance side).
    /// </summary>
    public static Mesh CreateStadiumBadgeMesh(float outerW, float outerH, float tubeRadius, float yOffset = 0.018f, bool rightSide = true, int rows = 1, int arcSegments = 24)
    {
        Mesh mesh = new Mesh();
        mesh.name = rightSide ? "StadiumBadgeMesh_Right" : "StadiumBadgeMesh_Left";

        float rCap = Mathf.Max(0.08f, (outerH * 0.5f) - tubeRadius);
        float rInner = Mathf.Max(0.04f, rCap - tubeRadius);
        float straightLen = Mathf.Max(0f, outerW - 2f * (rCap + tubeRadius));
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
            // Badge on RIGHT side of hole
            verts.Add(new Vector3(centerBadgeX, yOffset, 0f));
            norms.Add(Vector3.up);
            uvs.Add(new Vector2(0.5f, 0.5f));

            // 1. Top line from right arc entry to top-left corner
            perimeter.Add(new Vector3(halfL, yOffset, rInner));
            perimeter.Add(new Vector3(badgeLeftX + cornerR, yOffset, rInner));

            // 2. Top-left rounded corner (pi/2 to pi)
            for (int i = 0; i <= 6; i++)
            {
                float ang = Mathf.PI * 0.5f + ((float)i / 6) * Mathf.PI * 0.5f;
                perimeter.Add(new Vector3(badgeLeftX + cornerR + cornerR * Mathf.Cos(ang), yOffset, rInner - cornerR + cornerR * Mathf.Sin(ang)));
            }

            // 3. Left vertical line
            perimeter.Add(new Vector3(badgeLeftX, yOffset, -rInner + cornerR));

            // 4. Bottom-left rounded corner (pi to 3pi/2)
            for (int i = 0; i <= 6; i++)
            {
                float ang = Mathf.PI + ((float)i / 6) * Mathf.PI * 0.5f;
                perimeter.Add(new Vector3(badgeLeftX + cornerR + cornerR * Mathf.Cos(ang), yOffset, -rInner + cornerR + cornerR * Mathf.Sin(ang)));
            }

            // 5. Bottom line to right arc entry
            perimeter.Add(new Vector3(halfL, yOffset, -rInner));

            // 6. Right semicircle arc (-pi/2 to +pi/2)
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
            // Badge on LEFT side of hole
            verts.Add(new Vector3(-centerBadgeX, yOffset, 0f));
            norms.Add(Vector3.up);
            uvs.Add(new Vector2(0.5f, 0.5f));

            // 1. Top line from left arc entry to top-right corner
            perimeter.Add(new Vector3(-halfL, yOffset, rInner));
            perimeter.Add(new Vector3(-badgeLeftX - cornerR, yOffset, rInner));

            // 2. Top-right rounded corner (pi/2 to 0)
            for (int i = 0; i <= 6; i++)
            {
                float ang = Mathf.PI * 0.5f - ((float)i / 6) * Mathf.PI * 0.5f;
                perimeter.Add(new Vector3(-badgeLeftX - cornerR + cornerR * Mathf.Cos(ang), yOffset, rInner - cornerR + cornerR * Mathf.Sin(ang)));
            }

            // 3. Right vertical line
            perimeter.Add(new Vector3(-badgeLeftX, yOffset, -rInner + cornerR));

            // 4. Bottom-right rounded corner (0 to -pi/2)
            for (int i = 0; i <= 6; i++)
            {
                float ang = 0f - ((float)i / 6) * Mathf.PI * 0.5f;
                perimeter.Add(new Vector3(-badgeLeftX - cornerR + cornerR * Mathf.Cos(ang), yOffset, -rInner + cornerR + cornerR * Mathf.Sin(ang)));
            }

            // 5. Bottom line to left arc entry
            perimeter.Add(new Vector3(-halfL, yOffset, -rInner));

            // 6. Left semicircle arc (-pi/2 to -3pi/2)
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

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySwallow();
        }
    }

    private void SpawnSwallowVFX()
    {
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

    public void PlaySwallowEffect()
    {
        if (slotRoot == null) return;

        slotRoot.DOKill();
        slotRoot.DOScale(new Vector3(1.08f, 0.92f, 1.08f), 0.08f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                slotRoot.DOScale(Vector3.one, 0.12f).SetEase(Ease.OutBounce);
            });

        if (countBadgeObj != null)
        {
            countBadgeObj.transform.DOKill();
            countBadgeObj.transform.DOPunchScale(new Vector3(0.22f, 0.22f, 0.22f), 0.18f, 5, 0.5f);
        }
    }

    private void PlayCompletedEffect()
    {
        if (slotRoot == null) return;

        slotRoot.DOScale(new Vector3(1.12f, 1.0f, 1.12f), 0.2f)
            .SetLoops(2, LoopType.Yoyo)
            .SetEase(Ease.OutBack);
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
