using UnityEngine;
using DG.Tweening;

public class HoleController : MonoBehaviour
{
    [SerializeField] private AntColor holeColor;
    [SerializeField] private Vector2Int gridCoordinate;
    [SerializeField] private MeshRenderer rimRenderer;
    [SerializeField] private MeshRenderer innerHoleRenderer;
    [SerializeField] private Light holeLight;

    public AntColor Color => holeColor;
    public Vector2Int GridCoordinate => gridCoordinate;

    public void Initialize(AntColor color, Vector2Int coordinate)
    {
        this.holeColor = color;
        this.gridCoordinate = coordinate;

        BuildVisuals();
    }

    private void BuildVisuals()
    {
        // Check if visuals already exist (e.g. from prefab)
        if (rimRenderer != null && innerHoleRenderer != null)
        {
            ApplyColors();
            return;
        }

        // Procedural hole visual generation if not attached to prefab
        GameObject rimObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rimObj.name = "HoleRim";
        rimObj.transform.SetParent(transform, false);
        rimObj.transform.localPosition = new Vector3(0f, 0.02f, 0f);
        rimObj.transform.localScale = new Vector3(1.15f, 0.04f, 1.15f);
        rimRenderer = rimObj.GetComponent<MeshRenderer>();

        GameObject innerObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        innerObj.name = "HolePit";
        innerObj.transform.SetParent(transform, false);
        innerObj.transform.localPosition = new Vector3(0f, 0.03f, 0f);
        innerObj.transform.localScale = new Vector3(0.85f, 0.045f, 0.85f);
        innerHoleRenderer = innerObj.GetComponent<MeshRenderer>();

        // Remove unnecessary physics colliders on decorative cylinders
        var c1 = rimObj.GetComponent<Collider>();
        if (c1 != null) Destroy(c1);
        var c2 = innerObj.GetComponent<Collider>();
        if (c2 != null) Destroy(c2);

        ApplyColors();
    }

    private void ApplyColors()
    {
        Color baseColor = AntColorPalette.GetColor(holeColor);

        if (rimRenderer != null)
        {
            var rimMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            rimMat.color = baseColor;
            rimRenderer.material = rimMat;
        }

        if (innerHoleRenderer != null)
        {
            var pitMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            pitMat.color = new Color(0.08f, 0.08f, 0.1f); // Dark pit interior
            innerHoleRenderer.material = pitMat;
        }
    }

    /// <summary>
    /// Swallows an ant segment with a pulse/swallow effect.
    /// </summary>
    public void PlaySwallowEffect()
    {
        transform.DOKill();
        transform.DOScale(new Vector3(1.2f, 0.9f, 1.2f), 0.1f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                transform.DOScale(Vector3.one, 0.15f).SetEase(Ease.InQuad);
            });
    }
}
