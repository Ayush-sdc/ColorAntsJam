using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class AutoAssetInitializer
{
    static AutoAssetInitializer()
    {
        EditorApplication.delayCall += Initialize;
    }

    private static void Initialize()
    {
        // 1. Ensure Prefabs exist
        if (!System.IO.File.Exists("Assets/Prefabs/AntUnit.prefab") || !System.IO.File.Exists("Assets/Prefabs/Hole.prefab"))
        {
            PrefabCreationUtility.GeneratePrefabs();
        }

        // 2. Ensure Database exists
        if (!System.IO.File.Exists("Assets/Data/Levels/LevelDatabase.asset"))
        {
            LevelGeneratorWindow.GenerateSampleLevelsAndDatabase();
        }

        // Automatic scene wipe on reload has been disabled to preserve user's manual Edit Mode changes.
        // If you want to rebuild the scene layout from data at any time, use the top menu:
        // SlitherAnts -> Sync Play Mode Layout to Edit Mode
    }
}
