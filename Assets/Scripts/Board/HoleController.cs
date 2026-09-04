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

    private void Awake()
    {
        if (slotRoot == null || transform.childCount == 0)
        {
            BuildCapsuleSlotVisuals(isRightSide);
            UpdateBadgeDisplay();
        }
    }

    public void Initialize(AntColor color, Vector2Int coordinate, int capacity = 1, bool isRightSideSlot = true)
    {
        this.holeColor = color;
        this.gridCoordinate = coordinate;
        this.initialCapacity = Mathf.Max(0, capacity);
        this.remainingCount = initialCapacity;
        this.isRightSide = isRightSideSlot;

        BuildCapsuleSlotVisuals(isRightSideSlot);
        UpdateBadgeDisplay();
    }

    public void BuildCapsuleSlotVisuals(bool rightSide = true)
    {
        if (slotRoot != null)
        {
            SafeDestroy(slotRoot.gameObject);
        }

        // Also clean up any lingering child objects
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            SafeDestroy(transform.GetChild(i).gameObject);
        }

        slotRoot = new GameObject("CapsuleSlotRoot").transform;
        slotRoot.SetParent(transform, false);
        slotRoot.localPosition = Vector3.zero;

        // Rotate root so the open entrance pit is ALWAYS on the side facing the oncoming ants
        slotRoot.localRotation = Quaternion.Euler(0f, rightSide ? 0f : 180f, 0f);

        // The tray is a horizontal stadium/capsule spanning width ~2.1 units and height ~1.05 units
        float capRadius = 0.52f;
        float centerWidth = 1.0f;
        float rimHeight = 0.06f;
        float pitHeight = 0.065f;

        Material rimMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        rimMat.color = AntColorPalette.GetColor(holeColor);
        if (rimMat.HasProperty("_Smoothness")) rimMat.SetFloat("_Smoothness", 0.90f);
        if (rimMat.HasProperty("_Metallic")) rimMat.SetFloat("_Metallic", 0.04f);

        Material pitMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        pitMat.color = AntColorPalette.HolePitColor;
        if (pitMat.HasProperty("_Smoothness")) pitMat.SetFloat("_Smoothness", 0.20f);

        // 1. Glossy Colored Border (Left cylinder, Right cylinder, Center quad/box)
        GameObject rimHolder = new GameObject("SlotRim");
        rimHolder.transform.SetParent(slotRoot, false);

        // Left rim curve (Entrance pit side when unrotated)
        GameObject leftRim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        leftRim.name = "LeftRim";
        leftRim.transform.SetParent(rimHolder.transform, false);
        leftRim.transform.localPosition = new Vector3(-centerWidth * 0.5f, 0.02f, 0f);
        leftRim.transform.localScale = new Vector3(capRadius * 2f, rimHeight, capRadius * 2f);
        SafeDestroy(leftRim.GetComponent<Collider>());
        leftRim.GetComponent<MeshRenderer>().material = rimMat;

        // Right rim curve (Badge side when unrotated)
        GameObject rightRim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rightRim.name = "RightRim";
        rightRim.transform.SetParent(rimHolder.transform, false);
        rightRim.transform.localPosition = new Vector3(centerWidth * 0.5f, 0.02f, 0f);
        rightRim.transform.localScale = new Vector3(capRadius * 2f, rimHeight, capRadius * 2f);
        SafeDestroy(rightRim.GetComponent<Collider>());
        rightRim.GetComponent<MeshRenderer>().material = rimMat;

        // Center rim box
        GameObject centerRim = GameObject.CreatePrimitive(PrimitiveType.Cube);
        centerRim.name = "CenterRim";
        centerRim.transform.SetParent(rimHolder.transform, false);
        centerRim.transform.localPosition = new Vector3(0f, 0.02f, 0f);
        centerRim.transform.localScale = new Vector3(centerWidth, rimHeight, capRadius * 2f);
        SafeDestroy(centerRim.GetComponent<Collider>());
        centerRim.GetComponent<MeshRenderer>().material = rimMat;

        // 2. Dark Recessed Well / Floor (Seamless continuous dark stadium)
        float innerRadius = 0.40f;
        float innerWidth = centerWidth;

        GameObject pitHolder = new GameObject("SlotPit");
        pitHolder.transform.SetParent(slotRoot, false);

        GameObject leftPit = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        leftPit.name = "LeftPit";
        leftPit.transform.SetParent(pitHolder.transform, false);
        leftPit.transform.localPosition = new Vector3(-innerWidth * 0.5f, 0.038f, 0f);
        leftPit.transform.localScale = new Vector3(innerRadius * 2f, pitHeight, innerRadius * 2f);
        SafeDestroy(leftPit.GetComponent<Collider>());
        leftPit.GetComponent<MeshRenderer>().material = pitMat;

        GameObject rightPit = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rightPit.name = "RightPit";
        rightPit.transform.SetParent(pitHolder.transform, false);
        rightPit.transform.localPosition = new Vector3(innerWidth * 0.5f, 0.038f, 0f);
        rightPit.transform.localScale = new Vector3(innerRadius * 2f, pitHeight, innerRadius * 2f);
        SafeDestroy(rightPit.GetComponent<Collider>());
        rightPit.GetComponent<MeshRenderer>().material = pitMat;

        GameObject centerPit = GameObject.CreatePrimitive(PrimitiveType.Cube);
        centerPit.name = "CenterPit";
        centerPit.transform.SetParent(pitHolder.transform, false);
        centerPit.transform.localPosition = new Vector3(0f, 0.038f, 0f);
        centerPit.transform.localScale = new Vector3(innerWidth, pitHeight, innerRadius * 2f);
        SafeDestroy(centerPit.GetComponent<Collider>());
        if (centerPit.GetComponent<MeshRenderer>() != null) centerPit.GetComponent<MeshRenderer>().material = pitMat;

        // 3. Stylized Numeric Badge (Placed on outer end of tray)
        countBadgeObj = new GameObject("SlotCounterBadge");
        countBadgeObj.transform.SetParent(slotRoot, false);
        countBadgeObj.transform.localPosition = new Vector3(0.55f, 0.07f, 0f);

        // Badge outer colored border
        GameObject badgeBg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        badgeBg.name = "BadgeBackground";
        badgeBg.transform.SetParent(countBadgeObj.transform, false);
        badgeBg.transform.localPosition = Vector3.zero;
        badgeBg.transform.localScale = new Vector3(0.58f, 0.02f, 0.58f);
        SafeDestroy(badgeBg.GetComponent<Collider>());

        var badgeMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        badgeMat.color = AntColorPalette.GetColor(holeColor);
        if (badgeMat.HasProperty("_Smoothness")) badgeMat.SetFloat("_Smoothness", 0.90f);
        badgeBg.GetComponent<MeshRenderer>().material = badgeMat;
        badgeBackgroundRenderer = badgeBg.GetComponent<MeshRenderer>();

        // Inner dark contrast circle
        GameObject innerBadge = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        innerBadge.name = "InnerBadgeCircle";
        innerBadge.transform.SetParent(countBadgeObj.transform, false);
        innerBadge.transform.localPosition = new Vector3(0f, 0.015f, 0f);
        innerBadge.transform.localScale = new Vector3(0.48f, 0.02f, 0.48f);
        SafeDestroy(innerBadge.GetComponent<Collider>());
        var innerMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        innerMat.color = new UnityEngine.Color(0.12f, 0.14f, 0.18f);
        innerBadge.GetComponent<MeshRenderer>().material = innerMat;

        // Stylized Number Text (Always oriented upright to the top camera)
        GameObject textObj = new GameObject("BadgeCountText");
        textObj.transform.SetParent(countBadgeObj.transform, false);
        textObj.transform.localPosition = new Vector3(0f, 0.035f, 0f);
        textObj.transform.localRotation = Quaternion.Euler(90f, rightSide ? 0f : 180f, 0f);

        countTextMesh = textObj.AddComponent<TextMesh>();
        countTextMesh.alignment = TextAlignment.Center;
        countTextMesh.anchor = TextAnchor.MiddleCenter;
        countTextMesh.fontSize = 54;
        countTextMesh.characterSize = 0.095f;
        countTextMesh.fontStyle = FontStyle.Bold;
        countTextMesh.color = UnityEngine.Color.white;
    }

    private void UpdateBadgeDisplay()
    {
        if (countTextMesh == null) return;

        countTextMesh.text = remainingCount.ToString();
        countTextMesh.characterSize = remainingCount >= 10 ? 0.082f : 0.095f;
        countTextMesh.color = remainingCount > 0 ? UnityEngine.Color.white : new UnityEngine.Color(1f, 1f, 1f, 0.85f);
    }

    /// <summary>
    /// Called when an individual ant enters this hole. Decrements count by 1 and triggers swallow pulse.
    /// </summary>
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
        slotRoot.DOScale(new Vector3(1.10f, 0.90f, 1.10f), 0.08f)
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

        slotRoot.DOScale(new Vector3(1.15f, 1.0f, 1.15f), 0.2f)
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
                BuildCapsuleSlotVisuals(isRightSide);
                UpdateBadgeDisplay();
            }
        };
    }
#endif
}
