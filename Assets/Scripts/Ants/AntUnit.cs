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

        // Half-step duration inversely scaled by move speed
        float halfStepDuration = Mathf.Clamp(0.50f / Mathf.Max(1f, moveSpeed), 0.07f, 0.16f);
        float fullCycle = halfStepDuration * 4f;

        walkSequence = DOTween.Sequence();
        walkSequence.SetTarget(visualRoot);

        // Step 1: Left Step (Bob up & down on local Y, tilt left on local Z)
        walkSequence.Append(visualRoot.DOLocalMoveY(bobHeight, halfStepDuration).SetEase(Ease.OutSine));
        walkSequence.Join(visualRoot.DOLocalRotate(new Vector3(0f, 0f, tiltAngle), halfStepDuration).SetEase(Ease.OutSine));

        walkSequence.Append(visualRoot.DOLocalMoveY(0f, halfStepDuration).SetEase(Ease.InSine));
        walkSequence.Join(visualRoot.DOLocalRotate(Vector3.zero, halfStepDuration).SetEase(Ease.InSine));

        // Step 2: Right Step (Bob up & down on local Y, tilt right on local Z)
        walkSequence.Append(visualRoot.DOLocalMoveY(bobHeight, halfStepDuration).SetEase(Ease.OutSine));
        walkSequence.Join(visualRoot.DOLocalRotate(new Vector3(0f, 0f, -tiltAngle), halfStepDuration).SetEase(Ease.OutSine));

        walkSequence.Append(visualRoot.DOLocalMoveY(0f, halfStepDuration).SetEase(Ease.InSine));
        walkSequence.Join(visualRoot.DOLocalRotate(Vector3.zero, halfStepDuration).SetEase(Ease.InSine));

        walkSequence.SetLoops(-1, LoopType.Restart);

        // Deterministic staggering based on queue index
        float phaseOffset = (queueIndex * 0.04f) % fullCycle;
        if (phaseOffset > 0.001f)
        {
            walkSequence.Goto(phaseOffset, true);
        }
    }

    public void StopWalkAnimation()
    {
        KillWalkTween();
        ResetWalkVisual();
    }

    public void KillWalkTween()
    {
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
        if (visualRoot != null)
        {
            visualRoot.localPosition = Vector3.zero;
            visualRoot.localRotation = Quaternion.identity;
            visualRoot.localScale = Vector3.one;
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
        visualInstance.transform.localPosition = Vector3.zero;
        visualInstance.transform.localRotation = Quaternion.identity;

        float scale = isHead ? config.LeaderScale : config.WorkerScale;
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
