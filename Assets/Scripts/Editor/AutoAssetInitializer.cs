using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class AutoAssetInitializer
{
    static AutoAssetInitializer()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

        EditorApplication.delayCall += Initialize;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            // Automatically capture and bake any manual Scene Hierarchy changes into Level_01.asset before entering Play Mode!
            LevelGeneratorWindow.SaveSceneHierarchyToLevel01Asset();
        }
    }

    private static void Initialize()
    {
        // 1. Generate Prefabs if missing
        if (!System.IO.File.Exists("Assets/Prefabs/AntUnit.prefab"))
        {
            PrefabCreationUtility.GeneratePrefabs();
        }

        // 2. Generate Sample Levels & Database if missing
        if (!System.IO.File.Exists("Assets/Data/Levels/Level_01.asset"))
        {
            LevelGeneratorWindow.GenerateSampleLevelsAndDatabase();
            LevelGeneratorWindow.BuildLevel1InSceneHierarchy();
        }

        // 3. Ensure UI Canvas HUD is up to date
        if (GameObject.Find("HeartsContainer") == null)
        {
            SceneSetupUtility.SetupScene();
        }
    }
}
