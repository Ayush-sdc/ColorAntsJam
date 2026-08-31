using UnityEditor;
using UnityEngine;

public static class SceneSetupUtility
{
    [MenuItem("SlitherAnts/Setup Main Scene Game Architecture")]
    public static void SetupScene()
    {
        // 1. Setup Camera for Top-Down View like reference with pleasant garden sky
        Camera cam = Camera.main;
        if (cam == null)
        {
            var camObj = new GameObject("Main Camera");
            cam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
        }
        cam.transform.position = new Vector3(0f, 13.5f, -0.5f);
        cam.transform.rotation = Quaternion.Euler(85f, 0f, 0f);
        // Fresh outdoor ambient tint
        cam.backgroundColor = new Color(0.85f, 0.92f, 0.86f);
        cam.clearFlags = CameraClearFlags.SolidColor;

        // 2. Ensure Directional Light casts soft warm outdoor shadows
        var lightObj = GameObject.Find("Directional Light");
        if (lightObj != null)
        {
            var light = lightObj.GetComponent<Light>();
            if (light != null)
            {
                light.color = new Color(1f, 0.97f, 0.92f);
                light.intensity = 1.25f;
                light.shadows = LightShadows.Soft;
                light.shadowStrength = 0.55f;
                lightObj.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
            }
        }

        // 3. Create GameController with core managers
        GameObject gameController = GameObject.Find("GameController");
        if (gameController == null)
        {
            gameController = new GameObject("GameController");
        }

        var levelMgr = gameController.GetComponent<LevelManager>() ?? gameController.AddComponent<LevelManager>();
        var boardMgr = gameController.GetComponent<BoardManager>() ?? gameController.AddComponent<BoardManager>();
        var gameMgr = gameController.GetComponent<GameManager>() ?? gameController.AddComponent<GameManager>();
        var inputHandler = gameController.GetComponent<PlayerInputHandler>() ?? gameController.AddComponent<PlayerInputHandler>();

        // Link Database
        LevelDatabase db = AssetDatabase.LoadAssetAtPath<LevelDatabase>("Assets/Data/Levels/LevelDatabase.asset");
        if (db != null)
        {
            var field = typeof(LevelManager).GetField("levelDatabase", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(levelMgr, db);
            }
        }

        // 4. Create Board Root
        GameObject boardObj = GameObject.Find("Board");
        if (boardObj == null)
        {
            boardObj = new GameObject("Board");
        }
        var boardParentField = typeof(BoardManager).GetField("boardParent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (boardParentField != null)
        {
            boardParentField.SetValue(boardMgr, boardObj.transform);
        }

        // 5. Create Garden Background Root
        GameObject envObj = GameObject.Find("GardenEnvironment");
        if (envObj == null)
        {
            envObj = new GameObject("GardenEnvironment");
        }
        var bgMgr = envObj.GetComponent<GardenBackgroundManager>() ?? envObj.AddComponent<GardenBackgroundManager>();
        bgMgr.BuildEnvironment(11, 15, 1.0f);

        EditorUtility.SetDirty(gameController);
        EditorUtility.SetDirty(envObj);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("[SceneSetup] Main scene architecture and Garden Environment successfully configured!");
    }
}
