using UnityEngine;
using DG.Tweening;

/// <summary>
/// AntUnit manages a single ant segment within an AntQueueController.
/// Queue index 0 (isHead) strictly uses ChubbyAntColors as its visual child.
/// All followers and tail (index >= 1) strictly use WorkerAnt as their visual child.
/// AntUnit remains responsible for logical gameplay, queue tracking, movement, and raycast interaction.
/// </summary>
public class AntUnit : MonoBehaviour
{
    [Header("Ant Properties")]
    [SerializeField] private AntColor antColor = AntColor.Blue;
    [SerializeField] private bool isHead = false;
    [SerializeField] private bool isTail = false;
    private Color? overrideExactColor = null;

    [Header("Components")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Transform modelRoot;
    [SerializeField] private MeshRenderer bodyRenderer;

    [Header("Walk Animation")]
    [SerializeField] private float bobHeight = 0.035f;
    [SerializeField] private float tiltAngle = 3.5f;

    public AntColor Color => antColor;
    public bool IsHead => isHead;
    public bool IsTail => isTail;
    public bool IsMoving { get; set; } = false;
    public Transform VisualRoot => visualRoot;
    public float BobHeight => bobHeight;
    public float TiltAngle => tiltAngle;
    public bool IsWalkTweenActive => walkSequence != null && walkSequence.IsActive();
    public float VisualLength => isHead 
        ? AntVisualConfig.Instance.LeaderVisualLength 
        : AntVisualConfig.Instance.WorkerVisualLength;

    private Sequence walkSequence;
    private bool isInitialized = false;

    private void Awake()
    {
        EnsureVisualRoot();
        if (!isInitialized && transform.childCount == 0 && modelRoot == null)
        {
            BuildVisuals();
        }
    }

    private void Start()
    {
        EnsureVisualRoot();
        if (!isInitialized && transform.childCount == 0 && modelRoot == null)
        {
            BuildVisuals();
        }
    }

    public bool IsVisuallyHidden => isVisuallyHidden;
    private bool isVisuallyHidden = false;

    public void SetVisualVisibility(bool visible)
    {
        isVisuallyHidden = !visible;
        EnsureVisualRoot();
        if (visualRoot != null)
        {
            visualRoot.gameObject.SetActive(visible);
        }
    }

    private void OnEnable()
    {
        SetVisualVisibility(true);
    }

    private void OnDisable()
    {
        StopWalkAnimation();
    }

    private void OnDestroy()
    {
        KillWalkTween();
    }

    private void EnsureVisualRoot()
    {
        if (visualRoot == null)
        {
            Transform existing = transform.Find("VisualRoot");
            if (existing != null)
            {
                visualRoot = existing;
            }
        }
    }

    public void Setup(AntColor color, bool head = false, bool tail = false, Color? exactColor = null)
    {
        this.antColor = color;
        this.isHead = head;
        this.isTail = tail;
        this.overrideExactColor = exactColor;
        this.isInitialized = true;

        BuildVisuals();
        SetVisualVisibility(true);
    }

    private float walkTimer = 0f;
    private float walkCycleFreq = 0f;

    private void Update()
    {
        if (IsMoving && visualRoot != null && walkCycleFreq > 0f)
        {
            walkTimer += Time.deltaTime;
            float phase = walkTimer * walkCycleFreq;
            float yOffset = Mathf.Abs(Mathf.Sin(phase)) * bobHeight;
            float zTilt = Mathf.Sin(phase * 0.5f) * tiltAngle;

            visualRoot.localPosition = new Vector3(0f, yOffset, 0f);
            visualRoot.localRotation = Quaternion.Euler(0f, 0f, zTilt);
        }
    }

    public void StartWalkAnimation(int queueIndex = 0, float moveSpeed = 6.8f)
    {
        KillWalkTween();
        ResetWalkVisual();

        if (visualRoot == null)
        {
            EnsureVisualRoot();
            if (visualRoot == null) return;
        }

        var config = AntVisualConfig.Instance;
        bobHeight = isHead ? config.LeaderBobHeight : config.WorkerBobHeight;
        tiltAngle = isHead ? config.LeaderTiltAngle : config.WorkerTiltAngle;

        float halfStepDuration = Mathf.Clamp(0.50f / Mathf.Max(1f, moveSpeed), 0.07f, 0.16f);
        walkCycleFreq = (Mathf.PI * 2f) / (halfStepDuration * 4f);
        walkTimer = (queueIndex * 0.04f);
        IsMoving = true;
    }

    public void StopWalkAnimation()
    {
        IsMoving = false;
        KillWalkTween();
        ResetWalkVisual();
    }

    public void KillWalkTween()
    {
        IsMoving = false;
        if (walkSequence != null)
        {
            if (walkSequence.IsActive())
            {
                walkSequence.Kill();
            }
            walkSequence = null;
        }
        else if (visualRoot != null)
        {
            visualRoot.DOKill();
        }
    }

    public void ResetWalkVisual()
    {
        SetVisualVisibility(true);
        transform.localScale = Vector3.one;
        if (visualRoot != null)
        {
            visualRoot.localPosition = Vector3.zero;
            visualRoot.localRotation = Quaternion.identity;
            visualRoot.localScale = Vector3.one;
        }
        if (modelRoot != null)
        {
            var visualSettings = AntQueueVisualSettings.Instance;
            float scale = isHead ? visualSettings.LeaderVisualScale : visualSettings.WorkerVisualScale;
            Vector3 offset = isHead ? visualSettings.LeaderPivotOffset : visualSettings.WorkerPivotOffset;
            modelRoot.localPosition = offset;
            modelRoot.localScale = Vector3.one * scale;
        }
    }

    [ContextMenu("Rebuild Ant Visuals")]
    public void BuildVisuals()
    {
        KillWalkTween();
        ClearVisuals();

        GameObject vRootObj = new GameObject("VisualRoot");
        vRootObj.transform.SetParent(transform, false);
        vRootObj.transform.localPosition = Vector3.zero;
        vRootObj.transform.localRotation = Quaternion.identity;
        vRootObj.transform.localScale = Vector3.one;
        visualRoot = vRootObj.transform;

        // Authoritative Rule: Leader (index 0 / isHead) uses ChubbyAntColors; all followers & tail use WorkerAnt
        var config = AntVisualConfig.Instance;
        GameObject prefab = isHead ? config.GetLeaderPrefab(antColor) : config.GetWorkerPrefab(antColor);

        if (prefab == null)
        {
            Debug.LogError($"[AntUnit] Missing visual prefab for {(isHead ? "Leader (ChubbyAntColors)" : "Worker (WorkerAnt)")}! Snake/Slither visual fallback is STRICTLY FORBIDDEN.");
            return;
        }

        GameObject visualInstance = Instantiate(prefab, visualRoot);
        visualInstance.name = isHead ? "ChubbyAntColors" : "WorkerAnt";
        var visualSettings = AntQueueVisualSettings.Instance;
        float scale = isHead ? visualSettings.LeaderVisualScale : visualSettings.WorkerVisualScale;
        Vector3 offset = isHead ? visualSettings.LeaderPivotOffset : visualSettings.WorkerPivotOffset;

        visualInstance.transform.localPosition = offset;
        visualInstance.transform.localScale = Vector3.one * scale;
        modelRoot = visualInstance.transform;

        // Remove conflicting colliders/rigidbodies from visual instance so only AntUnit handles gameplay
        AntVisualConfig.StripConflictingComponents(visualInstance);

        // Apply authoritative resolved color (exact match to Hole SlotRim)
        Color resolvedColor = overrideExactColor ?? AntColorPalette.GetColor(antColor);
        AntVisualConfig.ApplyAntColor(visualInstance, resolvedColor);

        // Cache body renderer reference
        Transform bodyTransform = visualInstance.transform.Find("body");
        if (bodyTransform != null)
        {
            bodyRenderer = bodyTransform.GetComponent<MeshRenderer>();
        }

        // Gameplay Interaction Raycast Collider on AntUnit root
        var mainCollider = GetComponent<SphereCollider>();
        if (mainCollider == null) mainCollider = gameObject.AddComponent<SphereCollider>();
        mainCollider.center = new Vector3(0f, 0.22f, 0f);
        mainCollider.radius = 0.42f;
    }

    private void ClearVisuals()
    {
        KillWalkTween();

        modelRoot = null;
        bodyRenderer = null;
        visualRoot = null;

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child != null)
            {
                SafeDestroy(child.gameObject);
            }
        }
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
                BuildVisuals();
            }
        };
    }
#endif
}
