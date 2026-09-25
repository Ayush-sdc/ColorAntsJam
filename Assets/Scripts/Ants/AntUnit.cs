using UnityEngine;
using DG.Tweening;

public class AntUnit : MonoBehaviour
{
    [Header("Ant Properties")]
    [SerializeField] private AntColor antColor = AntColor.Blue;
    [SerializeField] private bool isHead = false;
    [SerializeField] private bool isTail = false;

    [Header("Components")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Transform headPivot;
    [SerializeField] private Transform modelRoot;
    [SerializeField] private MeshRenderer bodyRenderer;
    [SerializeField] private Transform leftPupil;
    [SerializeField] private Transform rightPupil;

    [Header("Walk Animation")]
    [SerializeField] private float bobHeight = 0.035f;
    [SerializeField] private float tiltAngle = 3.5f;

    private static readonly Vector3 DefaultPupilLocalPos = new Vector3(0f, 0.12f, 0.24f);

    public AntColor Color => antColor;
    public bool IsHead => isHead;
    public bool IsTail => isTail;
    public bool IsMoving { get; set; } = false;
    public Transform VisualRoot => visualRoot;
    public float BobHeight => bobHeight;
    public float TiltAngle => tiltAngle;
    public bool IsWalkTweenActive => walkSequence != null && walkSequence.IsActive();

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

    public void Setup(AntColor color, bool head = false, bool tail = false)
    {
        this.antColor = color;
        this.isHead = head;
        this.isTail = tail;
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

        // Native 3D Ant Visuals: Worker Ant Mesh or Procedural Fallback Composite Ant
        Material antMat = AntColorPalette.GetMaterial(antColor);
        Mesh workerMesh = AntColorPalette.GetWorkerAntMesh();

        if (workerMesh != null)
        {
            // 1. Compact 3D Worker Ant Mesh (Length scaled to maintain readability and separation)
            GameObject modelObj = new GameObject("AntModel_3D");
            modelObj.transform.SetParent(visualRoot, false);
            modelObj.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            modelObj.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            modelObj.transform.localScale = new Vector3(0.95f, 0.95f, 0.82f);
            modelRoot = modelObj.transform;

            var meshFilter = modelObj.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = workerMesh;

            bodyRenderer = modelObj.AddComponent<MeshRenderer>();
            bodyRenderer.sharedMaterial = antMat;
            bodyRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            bodyRenderer.receiveShadows = true;

            // 2. Head Pivot
            GameObject headPivotObj = new GameObject("HeadPivot");
            headPivotObj.transform.SetParent(visualRoot, false);
            headPivotObj.transform.localPosition = new Vector3(0f, 0.25f, 0.20f);
            headPivot = headPivotObj.transform;

            // Expressive Googly Eyes
            float eyeScale = isHead ? 0.22f : 0.18f;
            Vector3 leftEyePos = new Vector3(-0.12f, -0.02f, 0.12f);
            Vector3 rightEyePos = new Vector3(0.12f, -0.02f, 0.12f);

            leftPupil = CreateEye(headPivot, leftEyePos, "LeftEye", eyeScale);
            rightPupil = CreateEye(headPivot, rightEyePos, "RightEye", eyeScale);
        }
        else
        {
            // Procedural Fallback Composite Ant
            GameObject abdomen = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            abdomen.name = "Abdomen";
            abdomen.transform.SetParent(visualRoot, false);
            abdomen.transform.localPosition = new Vector3(0f, 0.24f, -0.14f);
            abdomen.transform.localScale = new Vector3(0.58f, 0.50f, 0.60f);
            SafeDestroy(abdomen.GetComponent<Collider>());
            var abRen = abdomen.GetComponent<MeshRenderer>();
            if (abRen != null) abRen.sharedMaterial = antMat;

            GameObject thorax = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            thorax.name = "Thorax";
            thorax.transform.SetParent(visualRoot, false);
            thorax.transform.localPosition = new Vector3(0f, 0.20f, 0.04f);
            thorax.transform.localScale = new Vector3(0.46f, 0.40f, 0.42f);
            SafeDestroy(thorax.GetComponent<Collider>());
            var thRen = thorax.GetComponent<MeshRenderer>();
            if (thRen != null) thRen.sharedMaterial = antMat;

            CreateLeg(visualRoot, new Vector3(-0.30f, 0.10f, 0.10f));
            CreateLeg(visualRoot, new Vector3(0.30f, 0.10f, 0.10f));
            CreateLeg(visualRoot, new Vector3(-0.30f, 0.10f, -0.12f));
            CreateLeg(visualRoot, new Vector3(0.30f, 0.10f, -0.12f));

            GameObject headPivotObj = new GameObject("HeadPivot");
            headPivotObj.transform.SetParent(visualRoot, false);
            headPivotObj.transform.localPosition = new Vector3(0f, 0.22f, 0.18f);
            headPivot = headPivotObj.transform;

            GameObject headSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            headSphere.name = "HeadSphere";
            headSphere.transform.SetParent(headPivot, false);
            headSphere.transform.localPosition = new Vector3(0f, 0.02f, 0.05f);
            headSphere.transform.localScale = isHead ? new Vector3(0.48f, 0.42f, 0.44f) : new Vector3(0.40f, 0.36f, 0.38f);
            SafeDestroy(headSphere.GetComponent<Collider>());
            var hdRen = headSphere.GetComponent<MeshRenderer>();
            if (hdRen != null) hdRen.sharedMaterial = antMat;

            CreateAntenna(headPivot, new Vector3(-0.07f, 0.18f, 0.10f), new Vector3(-15f, -18f, 0f));
            CreateAntenna(headPivot, new Vector3(0.07f, 0.18f, 0.10f), new Vector3(-15f, 18f, 0f));

            float eyeScale = isHead ? 0.22f : 0.18f;
            leftPupil = CreateEye(headPivot, new Vector3(-0.11f, 0.08f, 0.15f), "LeftEye", eyeScale);
            rightPupil = CreateEye(headPivot, new Vector3(0.11f, 0.08f, 0.15f), "RightEye", eyeScale);
        }

        // 3. Interaction Raycast Collider
        var mainCollider = GetComponent<SphereCollider>();
        if (mainCollider == null) mainCollider = gameObject.AddComponent<SphereCollider>();
        mainCollider.center = new Vector3(0f, 0.22f, 0f);
        mainCollider.radius = 0.42f;
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying) return;
        UpdateTrackingBehavior();
    }

    private void UpdateTrackingBehavior()
    {
        if (headPivot == null) return;

        Vector3 mouseWorld = PlayerInputHandler.MouseWorldPosition;
        Vector3 toMouse = mouseWorld - headPivot.position;
        toMouse.y = 0f;

        if (toMouse.sqrMagnitude > 0.09f && !IsMoving)
        {
            Vector3 dirToMouse = toMouse.normalized;
            float forwardDot = Vector3.Dot(transform.forward, dirToMouse);

            if (forwardDot > 0.15f)
            {
                float rawAngle = Vector3.SignedAngle(transform.forward, dirToMouse, Vector3.up);
                float subtleAngle = Mathf.Clamp(rawAngle * 0.40f, -25f, 25f);
                Quaternion targetHeadRotation = Quaternion.Euler(0f, subtleAngle, 0f);
                headPivot.localRotation = Quaternion.Slerp(headPivot.localRotation, targetHeadRotation, Time.deltaTime * 4.5f);

                Vector3 leftEyeWorld = (leftPupil != null && leftPupil.parent != null) ? leftPupil.parent.position : headPivot.position;
                Vector3 rightEyeWorld = (rightPupil != null && rightPupil.parent != null) ? rightPupil.parent.position : headPivot.position;

                UpdatePupilPosition(leftPupil, leftEyeWorld, mouseWorld, true);
                UpdatePupilPosition(rightPupil, rightEyeWorld, mouseWorld, true);
                return;
            }
        }

        headPivot.localRotation = Quaternion.Slerp(headPivot.localRotation, Quaternion.identity, Time.deltaTime * 3.5f);
        UpdatePupilPosition(leftPupil, Vector3.zero, Vector3.zero, false);
        UpdatePupilPosition(rightPupil, Vector3.zero, Vector3.zero, false);
    }

    private void UpdatePupilPosition(Transform pupil, Vector3 eyeCenterWorld, Vector3 targetWorld, bool trackCursor)
    {
        if (pupil == null) return;

        if (trackCursor)
        {
            Vector3 toTarget = targetWorld - eyeCenterWorld;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude > 0.001f)
            {
                Vector3 localDir = pupil.parent != null
                    ? pupil.parent.InverseTransformDirection(toTarget.normalized)
                    : transform.InverseTransformDirection(toTarget.normalized);

                float maxOffset = 0.055f;
                float targetX = Mathf.Clamp(localDir.x * maxOffset, -maxOffset, maxOffset);
                float targetZ = 0.24f + Mathf.Clamp(localDir.z * maxOffset, -maxOffset * 0.35f, maxOffset * 0.35f);
                float targetY = 0.12f;

                Vector3 targetPos = new Vector3(targetX, targetY, targetZ);
                pupil.localPosition = Vector3.Lerp(pupil.localPosition, targetPos, Time.deltaTime * 12f);
                return;
            }
        }

        pupil.localPosition = Vector3.Lerp(pupil.localPosition, DefaultPupilLocalPos, Time.deltaTime * 8f);
    }

    private void ClearVisuals()
    {
        KillWalkTween();

        leftPupil = null;
        rightPupil = null;
        headPivot = null;
        modelRoot = null;
        bodyRenderer = null;
        visualRoot = null;

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }
    }

    private void CreateLeg(Transform parent, Vector3 localPos)
    {
        GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        leg.name = "FootBead";
        leg.transform.SetParent(parent, false);
        leg.transform.localPosition = localPos;
        leg.transform.localScale = new Vector3(0.16f, 0.12f, 0.16f);

        SafeDestroy(leg.GetComponent<Collider>());

        var legRenderer = leg.GetComponent<MeshRenderer>();
        if (legRenderer != null)
        {
            legRenderer.sharedMaterial = AntColorPalette.GetLegMaterial();
        }
    }

    private void CreateAntenna(Transform parent, Vector3 localPos, Vector3 localRot)
    {
        GameObject antenna = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        antenna.name = "Antenna";
        antenna.transform.SetParent(parent, false);
        antenna.transform.localPosition = localPos;
        antenna.transform.localRotation = Quaternion.Euler(localRot);
        antenna.transform.localScale = new Vector3(0.024f, 0.09f, 0.024f);

        SafeDestroy(antenna.GetComponent<Collider>());

        var antRenderer = antenna.GetComponent<MeshRenderer>();
        if (antRenderer != null)
        {
            antRenderer.sharedMaterial = AntColorPalette.GetLegMaterial();
        }
    }

    private Transform CreateEye(Transform parent, Vector3 localPos, string eyeName, float scale = 0.20f)
    {
        GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        eye.name = eyeName;
        eye.transform.SetParent(parent, false);
        eye.transform.localPosition = localPos;
        eye.transform.localScale = new Vector3(scale, scale * 1.08f, scale);

        SafeDestroy(eye.GetComponent<Collider>());

        var eyeRenderer = eye.GetComponent<MeshRenderer>();
        if (eyeRenderer != null)
        {
            eyeRenderer.sharedMaterial = AntColorPalette.GetEyeWhiteMaterial();
        }

        GameObject pupil = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pupil.name = "Pupil";
        pupil.transform.SetParent(eye.transform, false);
        pupil.transform.localPosition = DefaultPupilLocalPos;
        pupil.transform.localScale = new Vector3(0.65f, 0.65f, 0.55f);

        SafeDestroy(pupil.GetComponent<Collider>());

        var pRen = pupil.GetComponent<MeshRenderer>();
        if (pRen != null)
        {
            pRen.sharedMaterial = AntColorPalette.GetEyePupilMaterial();
        }

        return pupil.transform;
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
