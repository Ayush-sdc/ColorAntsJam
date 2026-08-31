using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelDatabase", menuName = "SlitherAnts/Level Database", order = 0)]
public class LevelDatabase : ScriptableObject
{
    [SerializeField] private List<Level> levels = new List<Level>();

    public IReadOnlyList<Level> Levels => levels;
    public int LevelCount => levels != null ? levels.Count : 0;

    /// <summary>
    /// Gets a level by 1-based level index (e.g. Level 1, 2, ...).
    /// Clamps or loops depending on looping preference.
    /// </summary>
    public Level GetLevel(int levelIndex, bool loopIfExceeded = true)
    {
        if (levels == null || levels.Count == 0)
        {
            Debug.LogWarning("[LevelDatabase] No levels configured in database!");
            return null;
        }

        int index = levelIndex - 1; // Convert 1-based level to 0-based index

        if (index < 0)
        {
            index = 0;
        }
        else if (index >= levels.Count)
        {
            if (loopIfExceeded)
            {
                index = index % levels.Count;
            }
            else
            {
                index = levels.Count - 1;
            }
        }

        return levels[index];
    }
}
