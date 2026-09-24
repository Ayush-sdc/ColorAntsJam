using UnityEngine;

/// <summary>
/// Centralized visual library mapping AntColor values to dedicated AI-generated prefabs (from AntQueueKit)
/// or falling back to existing procedural / 3D ant models.
/// </summary>
[CreateAssetMenu(fileName = "AntVisualLibrary", menuName = "Slither Ants/Visual Library")]
public class AntVisualLibrary : ScriptableObject
{
    [Header("AI Ant Prefabs (AntQueueKit)")]
    [Tooltip("Dedicated Cyan/SkyBlue ant sprite prefab")]
    [SerializeField] private GameObject antCyanPrefab;

    [Tooltip("Dedicated Green ant sprite prefab")]
    [SerializeField] private GameObject antGreenPrefab;

    [Tooltip("Dedicated Pink ant sprite prefab")]
    [SerializeField] private GameObject antPinkPrefab;

    [Tooltip("Dedicated Yellow ant sprite prefab")]
    [SerializeField] private GameObject antYellowPrefab;

    [Tooltip("Dedicated Indigo ant sprite prefab")]
    [SerializeField] private GameObject antIndigoPrefab;

    [Tooltip("Dedicated Brown ant sprite prefab")]
    [SerializeField] private GameObject antBrownPrefab;

    [Header("Visual Configuration")]
    [Tooltip("Visual scale factor applied to the 2D ant sprite inside the 1x1 grid cell (approx 0.55 - 0.60 fits cell cleanly with readable spacing)")]
    [Range(0.2f, 1.2f)]
    [SerializeField] private float antScale = 0.58f;

    [Header("AI Hole Prefabs (AntQueueKit - Investigative)")]
    [SerializeField] private GameObject holeCyanPrefab;
    [SerializeField] private GameObject holeGreenPrefab;
    [SerializeField] private GameObject holePinkPrefab;
    [SerializeField] private GameObject holeYellowPrefab;
    [SerializeField] private GameObject holeIndigoPrefab;
    [SerializeField] private GameObject holeBrownPrefab;

    public float AntScale => antScale;

    /// <summary>
    /// Returns the dedicated AI ant visual prefab for the specified AntColor if available,
    /// or null to use existing fallback 3D visuals.
    /// Dedicated kit colors: SkyBlue/Cyan, Green, Pink, Yellow, Indigo, Brown.
    /// Fallback colors: Lime, Purple, Orange, White, Red, Turquoise.
    /// </summary>
    public GameObject GetAntPrefab(AntColor color)
    {
        return color switch
        {
            AntColor.SkyBlue or AntColor.Blue or AntColor.Cyan => antCyanPrefab,
            AntColor.Yellow => antYellowPrefab,
            AntColor.Pink => antPinkPrefab,
            AntColor.Indigo => antIndigoPrefab,
            AntColor.Green => antGreenPrefab,
            AntColor.Brown => antBrownPrefab,
            _ => null // Fallback: Lime, Purple, Orange, White, Red, Turquoise, etc.
        };
    }

    /// <summary>
    /// Returns the AI hole visual prefab if configured.
    /// </summary>
    public GameObject GetHolePrefab(AntColor color)
    {
        return color switch
        {
            AntColor.SkyBlue or AntColor.Blue or AntColor.Cyan => holeCyanPrefab,
            AntColor.Yellow => holeYellowPrefab,
            AntColor.Pink => holePinkPrefab,
            AntColor.Indigo => holeIndigoPrefab,
            AntColor.Green => holeGreenPrefab,
            AntColor.Brown => holeBrownPrefab,
            _ => null
        };
    }

    public void SetAntPrefabs(GameObject cyan, GameObject green, GameObject pink, GameObject yellow, GameObject indigo, GameObject brown)
    {
        antCyanPrefab = cyan;
        antGreenPrefab = green;
        antPinkPrefab = pink;
        antYellowPrefab = yellow;
        antIndigoPrefab = indigo;
        antBrownPrefab = brown;
    }

    public void SetHolePrefabs(GameObject cyan, GameObject green, GameObject pink, GameObject yellow, GameObject indigo, GameObject brown)
    {
        holeCyanPrefab = cyan;
        holeGreenPrefab = green;
        holePinkPrefab = pink;
        holeYellowPrefab = yellow;
        holeIndigoPrefab = indigo;
        holeBrownPrefab = brown;
    }
}
