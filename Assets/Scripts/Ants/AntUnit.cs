using UnityEngine;

public class AntUnit : MonoBehaviour
{
    [SerializeField] private AntColor antColor;
    [SerializeField] private bool isHead;
    [SerializeField] private MeshRenderer bodyRenderer;

    public AntColor Color => antColor;
    public bool IsHead => isHead;

    public void Setup(AntColor color, bool head)
    {
        this.antColor = color;
        this.isHead = head;

        BuildVisuals();
    }

    private void BuildVisuals()
    {
        // Body Sphere
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = isHead ? "AntHeadMesh" : "AntBodyMesh";
        sphere.transform.SetParent(transform, false);
        sphere.transform.localPosition = new Vector3(0f, 0.35f, 0f);
        sphere.transform.localScale = isHead ? new Vector3(0.78f, 0.65f, 0.78f) : new Vector3(0.68f, 0.58f, 0.68f);

        var col = sphere.GetComponent<Collider>();
        if (col != null) Destroy(col);

        bodyRenderer = sphere.GetComponent<MeshRenderer>();
        if (bodyRenderer != null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = AntColorPalette.GetColor(antColor);
            bodyRenderer.material = mat;
        }

        // Add cute eyes on head
        if (isHead)
        {
            CreateEye(new Vector3(-0.16f, 0.55f, 0.26f));
            CreateEye(new Vector3(0.16f, 0.55f, 0.26f));
        }

        var mainCollider = gameObject.AddComponent<SphereCollider>();
        mainCollider.center = new Vector3(0f, 0.35f, 0f);
        mainCollider.radius = 0.45f;
    }

    private void CreateEye(Vector3 localPos)
    {
        GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        eye.name = "Eye";
        eye.transform.SetParent(transform, false);
        eye.transform.localPosition = localPos;
        eye.transform.localScale = new Vector3(0.16f, 0.16f, 0.16f);

        var col = eye.GetComponent<Collider>();
        if (col != null) Destroy(col);

        var eyeRenderer = eye.GetComponent<MeshRenderer>();
        if (eyeRenderer != null)
        {
            var eyeMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            eyeMat.color = UnityEngine.Color.black;
            eyeRenderer.material = eyeMat;
        }

        // Pupil
        GameObject pupil = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pupil.name = "Pupil";
        pupil.transform.SetParent(eye.transform, false);
        pupil.transform.localPosition = new Vector3(0.2f, 0.2f, 0.4f);
        pupil.transform.localScale = Vector3.one * 0.4f;
        var pCol = pupil.GetComponent<Collider>();
        if (pCol != null) Destroy(pCol);
        var pRen = pupil.GetComponent<MeshRenderer>();
        if (pRen != null)
        {
            var pMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            pMat.color = UnityEngine.Color.white;
            pRen.material = pMat;
        }
    }
}
