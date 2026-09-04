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
        if (!System.IO.File.Exists("Assets/Prefabs/AntUnit.prefab"))
        {
            PrefabCreationUtility.GeneratePrefabs();
        }

        // 2. Ensure Database exists
        if (!System.IO.File.Exists("Assets/Data/Levels/LevelDatabase.asset"))
        {
            LevelGeneratorWindow.GenerateSampleLevelsAndDatabase();
        }

        // 3. Automatically sync Play Mode Level 1 layout into Edit Mode hierarchy
        if (!Application.isPlaying)
        {
            LevelGeneratorWindow.BuildLevel1InSceneHierarchy();
        }
    }
}
