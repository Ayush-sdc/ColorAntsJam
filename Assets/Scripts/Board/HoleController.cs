using UnityEngine;
using DG.Tweening;

[ExecuteAlways]
public class HoleController : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private AntColor holeColor;
    [SerializeField] private Vector2Int gridCoordinate;
    [SerializeField] private int initialCapacity = 1;
    [SerializeField] private int remainingCount = 1;
    [SerializeField] private bool isRightSide = true;
    [SerializeField] private int parallelRows = 1;

    [Header("Visual Components")]
    [SerializeField] private Transform slotRoot;
    [SerializeField] private GameObject countBadgeObj;
    [SerializeField] private TextMesh countTextMesh;
    [SerializeField] private MeshRenderer badgeBackgroundRenderer;

    private static GameObject swallowVfxPrefab;

    public AntColor Color => holeColor;
    public Vector2Int GridCoordinate => gridCoordinate;
    public int RemainingCount => remainingCount;
    public int InitialCapacity => initialCapacity;
    public bool IsFull => remainingCount <= 0;
    public int ParallelRows => parallelRows;

    private void Awake()
    {
        if (slotRoot == null || transform.childCount == 0)
        {
            BuildRoundedRectangleSlotVisuals(isRightSide, parallelRows);
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

        BuildRoundedRectangleSlotVisuals(isRightSideSlot, this.parallelRows);
        UpdateBadgeDisplay();
    }

    /// <summary>
    /// Builds a glossy rounded-corner rectangle food box with a dark recessed pit and a small square counter badge.
    /// </summary>
    public void BuildRoundedRectangleSlotVisuals(bool rightSide = true, int rows = 1)
    {
        if (slotRoot != null)
        {
            SafeDestroy(slotRoot.gameObject);
        }

        // Clean up lingering child objects
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            SafeDestroy(transform.GetChild(i).gameObject);
        }

        slotRoot = new GameObject("RoundedSlotRoot").transform;
        slotRoot.SetParent(transform, false);
        slotRoot.localPosition = Vector3.zero;
        slotRoot.localRotation = Quaternion.identity;

        // Dimensions
        float outerW = (rows >= 2) ? 2.05f : 1.95f;
        float outerH = (rows >= 2) ? 2.05f : 0.95f;
        float cornerR = 0.35f;
        float rimThick = 0.14f;
        float rimH = 0.06f;
        float pitH = 0.065f;

        float innerW = outerW - 2f * rimThick;
        float innerH = outerH - 2f * rimThick;
        float innerR = Mathf.Max(0.06f, cornerR - rimThick);

        Material rimMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        rimMat.color = AntColorPalette.GetColor(holeColor);
        if (rimMat.HasProperty("_Smoothness")) rimMat.SetFloat("_Smoothness", 0.90f);
        if (rimMat.HasProperty("_Metallic")) rimMat.SetFloat("_Metallic", 0.04f);

        Material pitMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        pitMat.color = AntColorPalette.HolePitColor;
        if (pitMat.HasProperty("_Smoothness")) pitMat.SetFloat("_Smoothness", 0.20f);

        // 1. Glossy Colored Border (SlotRim - Rounded Corner Rectangle)
        GameObject rimHolder = new GameObject("SlotRim");
        rimHolder.transform.SetParent(slotRoot, false);

        float cx = outerW * 0.5f - cornerR;
        float cz = outerH * 0.5f - cornerR;

        // 4 Corner Cylinders for smooth rounded corners
        Vector3[] cornerPositions = new Vector3[]
        {
            new Vector3(-cx, 0.02f,  cz), // Top-Left
            new Vector3( cx, 0.02f,  cz), // Top-Right
            new Vector3(-cx, 0.02f, -cz), // Bottom-Left
            new Vector3( cx, 0.02f, -cz)  // Bottom-Right
        };

        for (int i = 0; i < 4; i++)
        {
            GameObject corner = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            corner.name = $"RimCorner_{i}";
            corner.transform.SetParent(rimHolder.transform, false);
            corner.transform.localPosition = cornerPositions[i];
            corner.transform.localScale = new Vector3(cornerR * 2f, rimH, cornerR * 2f);
            SafeDestroy(corner.GetComponent<Collider>());
            corner.GetComponent<MeshRenderer>().material = rimMat;
        }

        // Horizontal center slab
        GameObject hBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
        hBox.name = "RimCenterH";
        hBox.transform.SetParent(rimHolder.transform, false);
        hBox.transform.localPosition = new Vector3(0f, 0.02f, 0f);
        hBox.transform.localScale = new Vector3(outerW, rimH, outerH - 2f * cornerR);
        SafeDestroy(hBox.GetComponent<Collider>());
        hBox.GetComponent<MeshRenderer>().material = rimMat;

        // Vertical center slab
        GameObject vBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
        vBox.name = "RimCenterV";
        vBox.transform.SetParent(rimHolder.transform, false);
        vBox.transform.localPosition = new Vector3(0f, 0.02f, 0f);
        vBox.transform.localScale = new Vector3(outerW - 2f * cornerR, rimH, outerH);
        SafeDestroy(vBox.GetComponent<Collider>());
        vBox.GetComponent<MeshRenderer>().material = rimMat;

        // 2. Dark Recessed Well / Floor (SlotPit - Clean Flat Square/Rectangular Pit Floor)
        GameObject pitHolder = new GameObject("SlotPit");
        pitHolder.transform.SetParent(slotRoot, false);

        GameObject pitSquare = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pitSquare.name = "PitCenterSquare";
        pitSquare.transform.SetParent(pitHolder.transform, false);
        pitSquare.transform.localPosition = new Vector3(0f, 0.038f, 0f);
        pitSquare.transform.localScale = new Vector3(innerW, pitH, innerH);
        SafeDestroy(pitSquare.GetComponent<Collider>());
        pitSquare.GetComponent<MeshRenderer>().material = pitMat;

        // 3. Small Square Counter Badge at the bottom-right corner of the rectangle
        countBadgeObj = new GameObject("SlotCounterBadge");
        countBadgeObj.transform.SetParent(slotRoot, false);

        float badgeX = (rows >= 2) ? (outerW * 0.5f - 0.32f) : (outerW * 0.5f - 0.28f);
        float badgeZ = (rows >= 2) ? (-outerH * 0.5f + 0.32f) : 0f;
        countBadgeObj.transform.localPosition = new Vector3(badgeX, 0.07f, badgeZ);

        // Small Square Outer Border
        GameObject badgeBg = GameObject.CreatePrimitive(PrimitiveType.Cube);
        badgeBg.name = "BadgeSquareBackground";
        badgeBg.transform.SetParent(countBadgeObj.transform, false);
        badgeBg.transform.localPosition = Vector3.zero;
        badgeBg.transform.localScale = new Vector3(0.48f, 0.025f, 0.48f);
        SafeDestroy(badgeBg.GetComponent<Collider>());

        var badgeMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        badgeMat.color = AntColorPalette.GetColor(holeColor);
        if (badgeMat.HasProperty("_Smoothness")) badgeMat.SetFloat("_Smoothness", 0.90f);
        badgeBg.GetComponent<MeshRenderer>().material = badgeMat;
        badgeBackgroundRenderer = badgeBg.GetComponent<MeshRenderer>();

        // Inner Dark Square Contrast
        GameObject innerBadge = GameObject.CreatePrimitive(PrimitiveType.Cube);
        innerBadge.name = "InnerBadgeSquare";
        innerBadge.transform.SetParent(countBadgeObj.transform, false);
        innerBadge.transform.localPosition = new Vector3(0f, 0.012f, 0f);
        innerBadge.transform.localScale = new Vector3(0.40f, 0.025f, 0.40f);
        SafeDestroy(innerBadge.GetComponent<Collider>());
        var innerMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        innerMat.color = new UnityEngine.Color(0.12f, 0.14f, 0.18f);
        innerBadge.GetComponent<MeshRenderer>().material = innerMat;

        // Stylized Number Text (Always oriented upright facing top camera)
        GameObject textObj = new GameObject("BadgeCountText");
        textObj.transform.SetParent(countBadgeObj.transform, false);
        textObj.transform.localPosition = new Vector3(0f, 0.025f, 0f);
        textObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        countTextMesh = textObj.AddComponent<TextMesh>();
        countTextMesh.alignment = TextAlignment.Center;
        countTextMesh.anchor = TextAnchor.MiddleCenter;
        countTextMesh.fontSize = 54;
        countTextMesh.characterSize = 0.090f;
        countTextMesh.fontStyle = FontStyle.Bold;
        countTextMesh.color = UnityEngine.Color.white;
    }

    private void UpdateBadgeDisplay()
    {
        if (countTextMesh == null) return;

        countTextMesh.text = remainingCount.ToString();
        countTextMesh.characterSize = remainingCount >= 10 ? 0.078f : 0.090f;
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
            countBadgeObj.transform.DOPunchScale(new Vector3(0.25f, 0.25f, 0.25f), 0.18f, 5, 0.5f);
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
        if (Application.isPlaying) Destroy(obj);
        else DestroyImmediate(obj);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null && !Application.isPlaying)
            {
                BuildRoundedRectangleSlotVisuals(isRightSide, parallelRows);
                UpdateBadgeDisplay();
            }
        };
    }
#endif
}
