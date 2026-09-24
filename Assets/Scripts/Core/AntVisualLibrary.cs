using UnityEngine;

/// <summary>
/// Centralized visual library mapping AntColor values to dedicated AI-generated prefabs (from AntQueueKit)
/// or falling back to existing procedural / 3D ant models.
/// </summary>
[CreateAssetMenu(fileName = "AntVisualLibrary", menuName = "Slither Ants/Visual Library")]
public class AntVisualLibrary : ScriptableObject
{
    [Header("AI Hole Prefabs (AntQueueKit - Investigative)")]
    [SerializeField] private GameObject holeCyanPrefab;
    [SerializeField] private GameObject holeGreenPrefab;
    [SerializeField] private GameObject holePinkPrefab;
    [SerializeField] private GameObject holeYellowPrefab;
    [SerializeField] private GameObject holeIndigoPrefab;
    [SerializeField] private GameObject holeBrownPrefab;

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
