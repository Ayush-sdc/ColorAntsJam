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
        // 1. Generate Prefabs if missing
        if (!System.IO.File.Exists("Assets/Prefabs/AntUnit.prefab"))
        {
            PrefabCreationUtility.GeneratePrefabs();
        }

        // 2. Generate Levels and Database if missing
        if (!System.IO.File.Exists("Assets/Data/Levels/LevelDatabase.asset"))
        {
            LevelGeneratorWindow.GenerateSampleLevelsAndDatabase();
        }

        // 3. Setup Scene GameController & Camera & GardenEnvironment if missing
        if (GameObject.Find("GameController") == null || GameObject.Find("GardenEnvironment") == null)
        {
            SceneSetupUtility.SetupScene();
        }
    }
}
