using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Controls a 3D direction-changing pipe (HolderType == 2).
/// Follows ordered centerline Cells, provides directional entry gating,
/// renders a glossy blue tubular body with rounded ends, and tracks usage.
/// </summary>
public class PipeController : MonoBehaviour
{
    [Header("Pipe Data")]
    [SerializeField] private List<Vector2Int> cells = new List<Vector2Int>();
    [SerializeField] private int pipeCount = 0;
    [SerializeField] private int remainingUses = 1;
    [SerializeField] private bool isPipeInUse = false;

    [Header("Visuals")]
    [SerializeField] private float pipeRadius = 0.42f;
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private MeshRenderer meshRenderer;

    private Canvas badgeCanvas;
    private Text counterText;
    private RectTransform badgeRoot;

    public IReadOnlyList<Vector2Int> Cells => cells;
    public int PipeCount => pipeCount;
    public int RemainingUses => remainingUses;
    public bool IsPipeInUse { get => isPipeInUse; set => isPipeInUse = value; }
    public bool IsExhausted => remainingUses <= 0;

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
        // If PipeCount <= 0 (e.g. Level 21 has single-pass obstacles), remaining uses defaults to 1.
        this.remainingUses = rawPipeCount > 0 ? rawPipeCount : 1;

        BuildPipeMesh();
        CreateUsageBadge();
    }

    [SerializeField] private int activeReservations = 0;
    public int ActiveReservations => activeReservations;

    /// <summary>
    /// Attempts to reserve one usage slot for an ant queue traversing this pipe.
    /// Prevents concurrent queues from exceeding the remaining capacity of the pipe.
    /// Note: PipeCount conversion and entrance direction are reference assumptions until verified from reference footage.
    /// </summary>
    public bool TryReserveUse()
    {
        if (IsExhausted || isPipeInUse || (remainingUses - activeReservations <= 0))
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
    }

    /// <summary>
    /// Checks whether an ant heading in forwardDir can legally enter this pipe at targetCell.
    /// Entry is only accepted at EntranceCell in EntryDirection. Side/backward entries rejected.
    /// </summary>
    public bool CanEnterFrom(Vector2Int targetCell, Vector2Int forwardDir)
    {
        if (IsExhausted || isPipeInUse || (remainingUses - activeReservations <= 0)) return false;
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
    /// Blocked taps do not consume uses.
    /// </summary>
    public void ConsumeUse()
    {
        activeReservations = Mathf.Max(0, activeReservations - 1);
        if (remainingUses > 0)
        {
            remainingUses--;
            UpdateBadgeUI(animate: true);

            if (remainingUses <= 0)
            {
                PlayExhaustedAndDestroy();
            }
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
        // Unregister cells from board immediately
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

    private void BuildPipeMesh()
    {
        if (cells == null || cells.Count < 2) return;

        meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null) meshFilter = gameObject.AddComponent<MeshFilter>();

        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null) meshRenderer = gameObject.AddComponent<MeshRenderer>();

        // Material: Glossy blue URP Lit
        Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material pipeMat = new Material(litShader);
        pipeMat.color = new Color(0.12f, 0.58f, 0.95f, 1f); // Vibrant cyan-blue
        if (pipeMat.HasProperty("_Smoothness")) pipeMat.SetFloat("_Smoothness", 0.85f);
        if (pipeMat.HasProperty("_Metallic")) pipeMat.SetFloat("_Metallic", 0.25f);
        meshRenderer.sharedMaterial = pipeMat;

        // Generate procedural tube mesh along centerline
        Mesh mesh = new Mesh { name = "ProceduralPipeMesh" };

        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();

        int radialSegments = 16;
        float radius = pipeRadius;

        // Convert grid cells to local points relative to pipe root
        Vector3 rootWorldPos = BoardManager.Instance != null
            ? BoardManager.Instance.GridToWorldPosition(cells[0])
            : new Vector3(cells[0].x, 0f, cells[0].y);

        transform.position = rootWorldPos;

        List<Vector3> centerPoints = new List<Vector3>();
        for (int i = 0; i < cells.Count; i++)
        {
            Vector3 worldPt = BoardManager.Instance != null
                ? BoardManager.Instance.GridToWorldPosition(cells[i], yOffset: 0.22f)
                : new Vector3(cells[i].x, 0.22f, cells[i].y);
            centerPoints.Add(worldPt - rootWorldPos);
        }

        // Generate rings along centerline
        for (int i = 0; i < centerPoints.Count; i++)
        {
            Vector3 pt = centerPoints[i];
            Vector3 fwd;
            if (i < centerPoints.Count - 1)
            {
                fwd = (centerPoints[i + 1] - pt).normalized;
            }
            else
            {
                fwd = (pt - centerPoints[i - 1]).normalized;
            }

            Vector3 up = Vector3.up;
            Vector3 right = Vector3.Cross(up, fwd).normalized;
            if (right.sqrMagnitude < 0.001f)
            {
                right = Vector3.right;
                up = Vector3.Cross(fwd, right).normalized;
            }
            else
            {
                up = Vector3.Cross(fwd, right).normalized;
            }

            for (int r = 0; r <= radialSegments; r++)
            {
                float angle = (float)r / radialSegments * Mathf.PI * 2f;
                Vector3 offsetDir = (right * Mathf.Cos(angle) + up * Mathf.Sin(angle));
                Vector3 vert = pt + offsetDir * radius;

                vertices.Add(vert);
                normals.Add(offsetDir.normalized);
                uvs.Add(new Vector2((float)r / radialSegments, (float)i / (centerPoints.Count - 1)));
            }

            if (i > 0)
            {
                int prevRingStart = (i - 1) * (radialSegments + 1);
                int currRingStart = i * (radialSegments + 1);

                for (int r = 0; r < radialSegments; r++)
                {
                    int p0 = prevRingStart + r;
                    int p1 = prevRingStart + r + 1;
                    int c0 = currRingStart + r;
                    int c1 = currRingStart + r + 1;

                    triangles.Add(p0);
                    triangles.Add(c0);
                    triangles.Add(p1);

                    triangles.Add(p1);
                    triangles.Add(c0);
                    triangles.Add(c1);
                }
            }
        }

        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();

        meshFilter.sharedMesh = mesh;
    }

    private void CreateUsageBadge()
    {
        // Compute center point of pipe in local space
        Vector3 center = Vector3.zero;
        if (cells != null && cells.Count > 0)
        {
            Vector3 sum = Vector3.zero;
            Vector3 root = BoardManager.Instance != null ? BoardManager.Instance.GridToWorldPosition(cells[0]) : Vector3.zero;
            foreach (var c in cells)
            {
                Vector3 w = BoardManager.Instance != null ? BoardManager.Instance.GridToWorldPosition(c) : new Vector3(c.x, 0, c.y);
                sum += (w - root);
            }
            center = sum / cells.Count;
        }

        GameObject canvasObj = new GameObject("PipeUsageBadge");
        canvasObj.transform.SetParent(transform, false);
        canvasObj.transform.localPosition = center + new Vector3(0f, 0.72f, 0f);

        badgeCanvas = canvasObj.AddComponent<Canvas>();
        badgeCanvas.renderMode = RenderMode.WorldSpace;
        badgeCanvas.sortingOrder = 55;

        badgeRoot = canvasObj.GetComponent<RectTransform>();
        badgeRoot.sizeDelta = new Vector2(60, 60);
        badgeRoot.localScale = Vector3.one * 0.008f;
        badgeRoot.localRotation = Quaternion.Euler(60f, 0f, 0f);

        // Circular background
        GameObject bgObj = new GameObject("BadgeBg");
        bgObj.transform.SetParent(badgeRoot, false);
        var bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        var bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.06f, 0.28f, 0.65f, 0.95f); // Deep blue backing

        // Text
        GameObject textObj = new GameObject("CounterText");
        textObj.transform.SetParent(badgeRoot, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        counterText = textObj.AddComponent<Text>();
        counterText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        counterText.fontSize = 32;
        counterText.fontStyle = FontStyle.Bold;
        counterText.alignment = TextAnchor.MiddleCenter;
        counterText.color = Color.white;
        counterText.text = remainingUses.ToString();
    }
}
