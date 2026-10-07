using System;
using UnityEngine;

/// <summary>
/// Authoritative visual configuration for pipe conduits, end collars, counter badges, and contact shadows.
/// Editable via Inspector in Assets/Resources/PipeVisualConfig.asset.
/// </summary>
[CreateAssetMenu(fileName = "PipeVisualConfig", menuName = "Slither Ants/Pipe Visual Config")]
public class PipeVisualConfig : ScriptableObject
{
    private static PipeVisualConfig s_Instance;

    public static PipeVisualConfig Instance
    {
        get
        {
            if (s_Instance == null)
            {
                s_Instance = Resources.Load<PipeVisualConfig>("PipeVisualConfig");
#if UNITY_EDITOR
                if (s_Instance == null)
                {
                    string[] guids = UnityEditor.AssetDatabase.FindAssets("t:PipeVisualConfig");
                    if (guids != null && guids.Length > 0)
                    {
                        string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                        s_Instance = UnityEditor.AssetDatabase.LoadAssetAtPath<PipeVisualConfig>(path);
                    }
                }
#endif
                if (s_Instance == null)
                {
                    s_Instance = CreateInstance<PipeVisualConfig>();
                }
            }
            return s_Instance;
        }
    }

    [Header("Conduit Cross-Section Dimensions")]
    [Tooltip("Total outer width of the pipe conduit (in grid cell units, default 0.84 for 1.0 cell)")]
    [Range(0.60f, 0.95f)]
    public float pipeWidth = 0.84f;

    [Tooltip("Total height of the pipe conduit above the board ground")]
    [Range(0.15f, 0.50f)]
    public float pipeHeight = 0.34f;

    [Tooltip("Width of the 45-degree angled chamfer bevel facets")]
    [Range(0.05f, 0.25f)]
    public float bevelWidth = 0.13f;

    [Tooltip("Vertical drop of the chamfer bevel facets")]
    [Range(0.04f, 0.20f)]
    public float bevelHeight = 0.10f;

    [Header("Conduit Materials & Colors")]
    [Tooltip("Vibrant glossy cyan base color for pipe main body")]
    public Color mainBodyColor = new Color(0.01f, 0.60f, 0.88f, 1f); // #0299E0

    [Tooltip("Bright turquoise highlight color for top/chamfer highlights")]
    public Color highlightColor = new Color(0.32f, 0.82f, 1.0f, 1f); // #52D1FF

    [Tooltip("Darker rich blue for shaded sides and bottom bevels")]
    public Color shadowColor = new Color(0.01f, 0.28f, 0.54f, 1f); // #02478A

    [Tooltip("Smooth specular reflection color")]
    public Color specularColor = new Color(0.68f, 0.94f, 1.0f, 1f); // #AEF0FE

    [Range(0.50f, 1.0f)]
    public float smoothness = 0.88f;

    [Tooltip("Optional custom material override. If null, automatically uses Toony Colors Pro 2/Hybrid Shader or URP Lit.")]
    public Material customPipeMaterial = null;

    [Header("End Collars")]
    public bool enableCollars = true;

    [Tooltip("Outer width of the pale-cyan collar flange at pipe mouths")]
    [Range(0.70f, 1.05f)]
    public float collarWidth = 0.92f;

    [Tooltip("Height of the collar flange")]
    [Range(0.20f, 0.55f)]
    public float collarHeight = 0.37f;

    [Tooltip("Thickness of the collar rim along the mouth axis")]
    [Range(0.03f, 0.15f)]
    public float collarThickness = 0.08f;

    [Tooltip("Pale cyan collar rim color")]
    public Color collarColor = new Color(0.68f, 0.94f, 1.0f, 1f); // #AEF0FE

    [Header("Counter Badge")]
    public bool enableBadge = true;

    [Tooltip("Badge plate size in world space canvas units")]
    [Range(24f, 72f)]
    public float badgeSize = 42f;

    [Tooltip("World height offset above pipe mouth for badge plate")]
    [Range(0.25f, 0.60f)]
    public float badgeHeightOffset = 0.44f;

    [Tooltip("Forward offset along pipe entry for badge placement")]
    [Range(0.0f, 0.8f)]
    public float badgeForwardOffset = 0.35f;

    [Tooltip("Lateral offset towards outer pipe wall for badge placement")]
    [Range(-0.4f, 0.4f)]
    public float badgeLateralOffset = 0.18f;

    [Tooltip("Light cyan border color for counter plate")]
    public Color badgeBorderColor = new Color(0.04f, 0.73f, 0.95f, 1f); // #0ABAFA

    [Tooltip("Deep blue face color for counter plate")]
    public Color badgeFaceColor = new Color(0.07f, 0.30f, 0.51f, 1f); // #124D82

    [Tooltip("Numeral text color")]
    public Color badgeTextColor = Color.white;

    [Tooltip("Numeral outline/shadow color")]
    public Color badgeOutlineColor = new Color(0.04f, 0.15f, 0.26f, 1f);

    [Header("Contact Shadow")]
    public bool enableShadow = true;

    [Tooltip("Width of the contact shadow ribbon beneath pipe runs")]
    [Range(0.70f, 1.20f)]
    public float shadowWidth = 0.96f;

    [Tooltip("Opacity of the soft ground contact shadow")]
    [Range(0.05f, 0.70f)]
    public float shadowAlpha = 0.30f;

    [Tooltip("Base shadow color tint")]
    public Color shadowColorRgb = new Color(0.02f, 0.04f, 0.08f, 1f);
}
