using UnityEditor;
using UnityEngine;

public static class AutoAssetInitializer
{
    // Auto-initialization disabled to prevent silent asset regenerations on domain reload.
    // [MenuItem("Tools/Slither Ants Jam/Initialize Assets Manually")]
    public static void ManualInitialize()
    {
        Initialize();
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
    }
}
