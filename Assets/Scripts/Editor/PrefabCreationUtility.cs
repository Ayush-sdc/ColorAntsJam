using UnityEditor;
using UnityEngine;

public static class PrefabCreationUtility
{
    [MenuItem("SlitherAnts/Generate Prefabs")]
    public static void GeneratePrefabs()
    {
        string prefabsFolder = "Assets/Prefabs";
        if (!AssetDatabase.IsValidFolder(prefabsFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        // 1. Ant Unit Prefab
        GameObject antUnitObj = new GameObject("AntUnit");
        antUnitObj.AddComponent<AntUnit>();
        string antPrefabPath = $"{prefabsFolder}/AntUnit.prefab";
        PrefabUtility.SaveAsPrefabAsset(antUnitObj, antPrefabPath);
        GameObject.DestroyImmediate(antUnitObj);

        // 2. Hole Prefab
        GameObject holeObj = new GameObject("Hole");
        holeObj.AddComponent<HoleController>();
        string holePrefabPath = $"{prefabsFolder}/Hole.prefab";
        PrefabUtility.SaveAsPrefabAsset(holeObj, holePrefabPath);
        GameObject.DestroyImmediate(holeObj);

        // 3. Ant Queue Prefab
        GameObject queueObj = new GameObject("AntQueue");
        queueObj.AddComponent<AntQueueController>();
        string queuePrefabPath = $"{prefabsFolder}/AntQueue.prefab";
        PrefabUtility.SaveAsPrefabAsset(queueObj, queuePrefabPath);
        GameObject.DestroyImmediate(queueObj);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[PrefabCreationUtility] Prefabs generated successfully in Assets/Prefabs!");
    }
}
