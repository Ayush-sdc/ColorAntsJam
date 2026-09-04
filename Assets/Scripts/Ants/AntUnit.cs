using UnityEngine;
using DG.Tweening;

[ExecuteAlways]
public class AntUnit : MonoBehaviour
{
    [Header("Ant Properties")]
    [SerializeField] private AntColor antColor = AntColor.Blue;
    [SerializeField] private bool isHead = false;
    [SerializeField] private bool isTail = false;

    [Header("Components")]
    [SerializeField] private Transform headPivot;
    [SerializeField] private Transform modelRoot;
    [SerializeField] private MeshRenderer bodyRenderer;
    [SerializeField] private Transform leftPupil;
    [SerializeField] private Transform rightPupil;
    [SerializeField] private GameObject groundDotObj;

    private static readonly Vector3 DefaultPupilLocalPos = new Vector3(0f, 0.12f, 0.24f);

    public AntColor Color => antColor;
    public bool IsHead => isHead;
    public bool IsTail => isTail;
    public bool IsMoving { get; set; } = false;

    private void Awake()
    {
        if (transform.childCount == 0)
        {
            BuildVisuals();
        }
    }

    private void Start()
    {
        BuildVisuals();
    }

    public void Setup(AntColor color, bool head = false, bool tail = false)
    {
        this.antColor = color;
        this.isHead = head;
        this.isTail = tail;

        BuildVisuals();
    }

    [ContextMenu("Rebuild Ant Visuals")]
    public void BuildVisuals()
    {
        ClearVisuals();

        Material antMat = AntColorPalette.GetMaterial(antColor);
        Mesh workerMesh = AntColorPalette.GetWorkerAntMesh();

        if (workerMesh != null)
        {
            // 1. Compact 3D Worker Ant Mesh (Length scaled to leave clear gap between queue ants)
            GameObject modelObj = new GameObject("AntModel_3D");
            modelObj.transform.SetParent(transform, false);
            modelObj.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            modelObj.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); // Aligns mesh facing forward (+Z)
            modelObj.transform.localScale = new Vector3(0.95f, 0.95f, 0.82f); // Compact length leaves clear ~0.3 unit gap between adjacent ants
            modelRoot = modelObj.transform;

            var meshFilter = modelObj.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = workerMesh;

            bodyRenderer = modelObj.AddComponent<MeshRenderer>();
            bodyRenderer.material = antMat;
            bodyRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            bodyRenderer.receiveShadows = true;

            // 2. Head Pivot (Positioned on the compact head)
            GameObject headPivotObj = new GameObject("HeadPivot");
            headPivotObj.transform.SetParent(transform, false);
            headPivotObj.transform.localPosition = new Vector3(0f, 0.25f, 0.20f);
            headPivot = headPivotObj.transform;

            // Expressive Googly Eyes (Sockets positioned in front of antennae roots)
            float eyeScale = isHead ? 0.22f : 0.18f;
            Vector3 leftEyePos = new Vector3(-0.12f, -0.02f, 0.12f);
            Vector3 rightEyePos = new Vector3(0.12f, -0.02f, 0.12f);

            leftPupil = CreateEye(headPivot, leftEyePos, "LeftEye", eyeScale);
            rightPupil = CreateEye(headPivot, rightEyePos, "RightEye", eyeScale);
        }
        else
        {
            // Fallback Procedural Composite Ant
            GameObject abdomen = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            abdomen.name = "Abdomen";
            abdomen.transform.SetParent(transform, false);
            abdomen.transform.localPosition = new Vector3(0f, 0.24f, -0.14f);
            abdomen.transform.localScale = new Vector3(0.58f, 0.50f, 0.60f);
            SafeDestroy(abdomen.GetComponent<Collider>());
            var abRen = abdomen.GetComponent<MeshRenderer>();
            if (abRen != null) abRen.material = antMat;

            GameObject thorax = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            thorax.name = "Thorax";
            thorax.transform.SetParent(transform, false);
            thorax.transform.localPosition = new Vector3(0f, 0.20f, 0.04f);
            thorax.transform.localScale = new Vector3(0.46f, 0.40f, 0.42f);
            SafeDestroy(thorax.GetComponent<Collider>());
            var thRen = thorax.GetComponent<MeshRenderer>();
            if (thRen != null) thRen.material = antMat;

            CreateLeg(new Vector3(-0.30f, 0.10f, 0.10f));
            CreateLeg(new Vector3(0.30f, 0.10f, 0.10f));
            CreateLeg(new Vector3(-0.30f, 0.10f, -0.12f));
            CreateLeg(new Vector3(0.30f, 0.10f, -0.12f));

            GameObject headPivotObj = new GameObject("HeadPivot");
            headPivotObj.transform.SetParent(transform, false);
            headPivotObj.transform.localPosition = new Vector3(0f, 0.22f, 0.18f);
            headPivot = headPivotObj.transform;

            GameObject headSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            headSphere.name = "HeadSphere";
            headSphere.transform.SetParent(headPivot, false);
            headSphere.transform.localPosition = new Vector3(0f, 0.02f, 0.05f);
            headSphere.transform.localScale = isHead ? new Vector3(0.48f, 0.42f, 0.44f) : new Vector3(0.40f, 0.36f, 0.38f);
            SafeDestroy(headSphere.GetComponent<Collider>());
            var hdRen = headSphere.GetComponent<MeshRenderer>();
            if (hdRen != null) hdRen.material = antMat;

            CreateAntenna(headPivot, new Vector3(-0.07f, 0.18f, 0.10f), new Vector3(-15f, -18f, 0f));
            CreateAntenna(headPivot, new Vector3(0.07f, 0.18f, 0.10f), new Vector3(-15f, 18f, 0f));

            float eyeScale = isHead ? 0.22f : 0.18f;
            leftPupil = CreateEye(headPivot, new Vector3(-0.11f, 0.08f, 0.15f), "LeftEye", eyeScale);
            rightPupil = CreateEye(headPivot, new Vector3(0.11f, 0.08f, 0.15f), "RightEye", eyeScale);
        }

        // 3. Smooth Circular Ground Dot Disc (Positioned directly below each ant on the board floor)
        GameObject dotObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        dotObj.name = "GroundDot";
        dotObj.transform.SetParent(transform, false);
        dotObj.transform.localPosition = new Vector3(0f, 0.01f, 0f);
        dotObj.transform.localScale = new Vector3(0.68f, 0.006f, 0.68f);
        SafeDestroy(dotObj.GetComponent<Collider>());

        var dotRen = dotObj.GetComponent<MeshRenderer>();
        if (dotRen != null)
        {
            dotRen.material = AntColorPalette.GetGroundDotMaterial();
            dotRen.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            dotRen.receiveShadows = true;
        }
        groundDotObj = dotObj;

        // 4. Interaction Raycast Collider
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

    /// <summary>
    /// Subtle sensitivity head pivot tracking:
    /// - Turns gently when the cursor is in front (clamped to +/- 25 degrees).
    /// - Smooth interpolation rate (4.5f) to avoid sudden snapping.
    /// </summary>
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

            // In front of the ant (within frontal cone)
            if (forwardDot > 0.15f)
            {
                // Gentle head turning (clamped to +/- 25 degrees)
                float rawAngle = Vector3.SignedAngle(transform.forward, dirToMouse, Vector3.up);
                float subtleAngle = Mathf.Clamp(rawAngle * 0.40f, -25f, 25f);
                Quaternion targetHeadRotation = Quaternion.Euler(0f, subtleAngle, 0f);
                headPivot.localRotation = Quaternion.Slerp(headPivot.localRotation, targetHeadRotation, Time.deltaTime * 4.5f);

                // Expressive eye gaze tracking
                Vector3 leftEyeWorld = (leftPupil != null && leftPupil.parent != null) ? leftPupil.parent.position : headPivot.position;
                Vector3 rightEyeWorld = (rightPupil != null && rightPupil.parent != null) ? rightPupil.parent.position : headPivot.position;

                UpdatePupilPosition(leftPupil, leftEyeWorld, mouseWorld, true);
                UpdatePupilPosition(rightPupil, rightEyeWorld, mouseWorld, true);
                return;
            }
        }

        // Reset gently to center forward
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

        // Return to neutral center
        pupil.localPosition = Vector3.Lerp(pupil.localPosition, DefaultPupilLocalPos, Time.deltaTime * 8f);
    }

    private void ClearVisuals()
    {
        leftPupil = null;
        rightPupil = null;
        headPivot = null;
        modelRoot = null;
        groundDotObj = null;

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            SafeDestroy(transform.GetChild(i).gameObject);
        }
    }

    private void CreateLeg(Vector3 localPos)
    {
        GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        leg.name = "FootBead";
        leg.transform.SetParent(transform, false);
        leg.transform.localPosition = localPos;
        leg.transform.localScale = new Vector3(0.16f, 0.12f, 0.16f);

        SafeDestroy(leg.GetComponent<Collider>());

        var legRenderer = leg.GetComponent<MeshRenderer>();
        if (legRenderer != null)
        {
            var legMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            legMat.color = AntColorPalette.LegColor;
            if (legMat.HasProperty("_Smoothness")) legMat.SetFloat("_Smoothness", 0.5f);
            legRenderer.material = legMat;
        }
    }

    private void CreateAntenna(Transform parent, Vector3 localPos, Vector3 localEuler)
    {
        GameObject antenna = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        antenna.name = "Antenna";
        antenna.transform.SetParent(parent, false);
        antenna.transform.localPosition = localPos;
        antenna.transform.localRotation = Quaternion.Euler(localEuler);
        antenna.transform.localScale = new Vector3(0.02f, 0.06f, 0.02f);
        SafeDestroy(antenna.GetComponent<Collider>());

        var ren = antenna.GetComponent<MeshRenderer>();
        if (ren != null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = AntColorPalette.LegColor;
            ren.material = mat;
        }

        GameObject tip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        tip.name = "Tip";
        tip.transform.SetParent(antenna.transform, false);
        tip.transform.localPosition = new Vector3(0f, 1.0f, 0f);
        tip.transform.localScale = new Vector3(2.0f, 0.8f, 2.0f);
        SafeDestroy(tip.GetComponent<Collider>());
        var tipRen = tip.GetComponent<MeshRenderer>();
        if (tipRen != null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = AntColorPalette.GetColor(antColor);
            tipRen.material = mat;
        }
    }

    private Transform CreateEye(Transform parent, Vector3 localPos, string eyeName, float scale = 0.18f)
    {
        // 1. Full Expressive Glossy White Eyeball Sphere
        GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        eye.name = eyeName;
        eye.transform.SetParent(parent, false);
        eye.transform.localPosition = localPos;
        eye.transform.localScale = new Vector3(scale, scale * 1.08f, scale);

        SafeDestroy(eye.GetComponent<Collider>());

        var eyeRenderer = eye.GetComponent<MeshRenderer>();
        if (eyeRenderer != null)
        {
            var eyeMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            eyeMat.color = UnityEngine.Color.white;
            if (eyeMat.HasProperty("_Smoothness")) eyeMat.SetFloat("_Smoothness", 0.95f);
            eyeRenderer.material = eyeMat;
        }

        // 2. Jet-Black Pupil Sphere (Expressive Gaze Tracking)
        GameObject pupil = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pupil.name = "Pupil";
        pupil.transform.SetParent(eye.transform, false);
        pupil.transform.localPosition = DefaultPupilLocalPos;
        pupil.transform.localScale = new Vector3(0.65f, 0.65f, 0.55f);

        SafeDestroy(pupil.GetComponent<Collider>());

        var pRen = pupil.GetComponent<MeshRenderer>();
        if (pRen != null)
        {
            var pMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            pMat.color = new UnityEngine.Color(0.04f, 0.04f, 0.06f);
            if (pMat.HasProperty("_Smoothness")) pMat.SetFloat("_Smoothness", 0.98f);
            pRen.material = pMat;
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
