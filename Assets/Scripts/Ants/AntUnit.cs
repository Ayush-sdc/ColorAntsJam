using UnityEngine;

public class AntUnit : MonoBehaviour
{
    [SerializeField] private AntColor antColor;
    [SerializeField] private bool isHead;
    [SerializeField] private bool isTail;
    [SerializeField] private MeshRenderer bodyRenderer;
    [SerializeField] private Transform leftPupil;
    [SerializeField] private Transform rightPupil;

    public AntColor Color => antColor;
    public bool IsHead => isHead;
    public bool IsTail => isTail;

    private void Start()
    {
        // Rebuild cleanly at runtime to ensure no old baked child objects persist
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

        // 1. Reduced-size Glossy Ant Body Sphere (72% scale, maintaining cell-to-cell distance)
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "AntBodySphere";
        sphere.transform.SetParent(transform, false);
        sphere.transform.localPosition = new Vector3(0f, 0.26f, 0f);
        sphere.transform.localScale = new Vector3(0.72f, 0.60f, 0.72f);

        var col = sphere.GetComponent<Collider>();
        if (col != null) SafeDestroy(col);

        bodyRenderer = sphere.GetComponent<MeshRenderer>();
        if (bodyRenderer != null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = AntColorPalette.GetColor(antColor);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.90f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.04f);
            bodyRenderer.material = mat;
        }

        // 2. Bilateral Side Legs (4 proportional dark gray feet along sides)
        CreateLeg(new Vector3(-0.36f, 0.12f, 0.10f));
        CreateLeg(new Vector3(0.36f, 0.12f, 0.10f));
        CreateLeg(new Vector3(-0.36f, 0.12f, -0.10f));
        CreateLeg(new Vector3(0.36f, 0.12f, -0.10f));

        // 3. Proportional Clean Cartoon Googly Eyes (White + Black Pupil)
        leftPupil = CreateEye(new Vector3(-0.13f, 0.48f, 0.13f), "LeftEye");
        rightPupil = CreateEye(new Vector3(0.13f, 0.48f, 0.13f), "RightEye");

        // Collider for raycasting & interactions
        var mainCollider = GetComponent<SphereCollider>();
        if (mainCollider == null) mainCollider = gameObject.AddComponent<SphereCollider>();
        mainCollider.center = new Vector3(0f, 0.26f, 0f);
        mainCollider.radius = 0.38f;
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying) return;
        UpdateEyeGaze();
    }

    /// <summary>
    /// Smoothly moves black pupils across the white eyeball to gaze at the mouse cursor.
    /// </summary>
    private void UpdateEyeGaze()
    {
        if (leftPupil == null || rightPupil == null)
        {
            FindPupilTransforms();
            if (leftPupil == null || rightPupil == null) return;
        }

        Vector3 mouseWorld = PlayerInputHandler.MouseWorldPosition;

        UpdatePupilPosition(leftPupil, transform.TransformPoint(new Vector3(-0.13f, 0.48f, 0.13f)), mouseWorld);
        UpdatePupilPosition(rightPupil, transform.TransformPoint(new Vector3(0.13f, 0.48f, 0.13f)), mouseWorld);
    }

    private void UpdatePupilPosition(Transform pupil, Vector3 eyeCenterWorld, Vector3 targetWorld)
    {
        if (pupil == null) return;

        Vector3 toTarget = targetWorld - eyeCenterWorld;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude < 0.001f) return;

        // Convert world direction to ant's local coordinate space
        Vector3 localDir = transform.InverseTransformDirection(toTarget.normalized);

        // Maximum pupil displacement on eyeball surface
        float maxOffset = 0.065f;
        float targetX = Mathf.Clamp(localDir.x * maxOffset, -maxOffset, maxOffset);
        float targetZ = 0.26f + Mathf.Clamp(localDir.z * maxOffset, -maxOffset * 0.4f, maxOffset * 0.4f);
        float targetY = 0.15f;

        Vector3 targetPos = new Vector3(targetX, targetY, targetZ);
        pupil.localPosition = Vector3.Lerp(pupil.localPosition, targetPos, Time.deltaTime * 14f);
    }

    private void FindPupilTransforms()
    {
        var pupils = GetComponentsInChildren<MeshRenderer>();
        foreach (var r in pupils)
        {
            if (r.gameObject.name == "Pupil")
            {
                if (r.transform.parent != null && r.transform.parent.localPosition.x < 0)
                {
                    leftPupil = r.transform;
                }
                else
                {
                    rightPupil = r.transform;
                }
            }
        }
    }

    private void ClearVisuals()
    {
        leftPupil = null;
        rightPupil = null;

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
        leg.transform.localScale = new Vector3(0.16f, 0.13f, 0.16f);

        var col = leg.GetComponent<Collider>();
        if (col != null) SafeDestroy(col);

        var legRenderer = leg.GetComponent<MeshRenderer>();
        if (legRenderer != null)
        {
            var legMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            legMat.color = AntColorPalette.LegColor;
            if (legMat.HasProperty("_Smoothness")) legMat.SetFloat("_Smoothness", 0.5f);
            legRenderer.material = legMat;
        }
    }

    private Transform CreateEye(Vector3 localPos, string eyeName)
    {
        // 1. Pure White Sclera Eyeball Sphere
        GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        eye.name = eyeName;
        eye.transform.SetParent(transform, false);
        eye.transform.localPosition = localPos;
        eye.transform.localScale = new Vector3(0.23f, 0.25f, 0.23f);

        var col = eye.GetComponent<Collider>();
        if (col != null) SafeDestroy(col);

        var eyeRenderer = eye.GetComponent<MeshRenderer>();
        if (eyeRenderer != null)
        {
            var eyeMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            eyeMat.color = UnityEngine.Color.white;
            if (eyeMat.HasProperty("_Smoothness")) eyeMat.SetFloat("_Smoothness", 0.95f);
            eyeRenderer.material = eyeMat;
        }

        // 2. Pure Jet-Black Pupil Sphere (Tracks Cursor)
        GameObject pupil = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pupil.name = "Pupil";
        pupil.transform.SetParent(eye.transform, false);
        pupil.transform.localPosition = new Vector3(0f, 0.15f, 0.26f);
        pupil.transform.localScale = new Vector3(0.65f, 0.65f, 0.55f);

        var pCol = pupil.GetComponent<Collider>();
        if (pCol != null) SafeDestroy(pCol);

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
        if (bodyRenderer != null && bodyRenderer.sharedMaterial != null)
        {
            bodyRenderer.sharedMaterial.color = AntColorPalette.GetColor(antColor);
        }
    }
#endif
}
