using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ModelCreationUtility
{
    [MenuItem("SlitherAnts/Refresh All Holes In Scene")]
    public static void RefreshAllHolesInScene()
    {
        HoleController[] holes = Object.FindObjectsByType<HoleController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (HoleController hole in holes)
        {
            hole.BuildStadiumSlotVisuals();
            EditorUtility.SetDirty(hole.gameObject);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("[ModelCreationUtility] Successfully refreshed " + holes.Length + " holes in scene!");
    }

    [MenuItem("SlitherAnts/Create Hole_Red_New in Scene")]
    public static void CreateHoleRedNewInScene()
    {
        GameObject existing = GameObject.Find("Hole_Red_New");
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }

        GameObject holeRedNew = new GameObject("Hole_Red_New");
        holeRedNew.transform.position = new Vector3(-1.62f, 0.0f, 8.23f);
        holeRedNew.transform.rotation = Quaternion.identity;
        holeRedNew.transform.localScale = Vector3.one;

        HoleController holeCtrl = holeRedNew.AddComponent<HoleController>();
        holeCtrl.Initialize(AntColor.Red, new Vector2Int(1, 0), 1, true, 1);

        string prefabPath = "Assets/Prefabs/Hole_Red_New.prefab";
        PrefabUtility.SaveAsPrefabAsset(holeRedNew, prefabPath);

        EditorUtility.SetDirty(holeRedNew);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[ModelCreationUtility] Successfully created Hole_Red_New in Scene and saved to Assets/Prefabs/Hole_Red_New.prefab!");
    }
}
