using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class LevelGeneratorWindow
{
    private const string DataFolder = "Assets/Data/Levels";
    private const string Level01AssetPath = "Assets/Data/Levels/Level_01.asset";
    private const string DatabaseAssetPath = "Assets/Data/Levels/LevelDatabase.asset";

    [MenuItem("SlitherAnts/Build Level 1 in Scene (Edit Mode)")]
    public static void BuildLevel1InSceneHierarchy()
    {
        EnsureDataFolders();
        Level level1 = CreateOrUpdateLevel1Asset();
        SceneSetupUtility.SetupScene();

        BoardManager boardMgr = Object.FindFirstObjectByType<BoardManager>();
        if (boardMgr == null)
        {
            GameObject gameCtrl = GameObject.Find("GameController") ?? new GameObject("GameController");
            boardMgr = gameCtrl.GetComponent<BoardManager>() ?? gameCtrl.AddComponent<BoardManager>();
        }

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

        boardMgr.SetupBoard(level1);

        EditorUtility.SetDirty(level1);
        EditorUtility.SetDirty(boardMgr);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();

        Debug.Log("[LevelGenerator] Successfully instantiated and permanently saved Level 1 in Scene Hierarchy in Edit Mode!");
    }

    [MenuItem("SlitherAnts/Generate All Levels and Database")]
    public static void GenerateSampleLevelsAndDatabase()
    {
        EnsureDataFolders();

        List<Level> generatedLevels = new List<Level>();

        Level level1 = CreateOrUpdateLevel1Asset();
        generatedLevels.Add(level1);

        Level level2 = CreateLevel2Asset();
        generatedLevels.Add(level2);

        Level level3 = CreateLevel3Asset();
        generatedLevels.Add(level3);

        LevelDatabase db = AssetDatabase.LoadAssetAtPath<LevelDatabase>(DatabaseAssetPath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<LevelDatabase>();
            AssetDatabase.CreateAsset(db, DatabaseAssetPath);
        }

        var field = typeof(LevelDatabase).GetField("levels", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(db, generatedLevels);
        }

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[LevelGenerator] Successfully generated {generatedLevels.Count} Levels and configured LevelDatabase!");
    }

    [MenuItem("SlitherAnts/Save Scene Hierarchy to Level_01 Asset")]
    public static void SaveSceneHierarchyToLevel01Asset()
    {
        Level level1 = AssetDatabase.LoadAssetAtPath<Level>(Level01AssetPath);
        if (level1 == null)
        {
            level1 = ScriptableObject.CreateInstance<Level>();
            AssetDatabase.CreateAsset(level1, Level01AssetPath);
        }

        BoardManager boardMgr = Object.FindFirstObjectByType<BoardManager>();
        if (boardMgr == null)
        {
            return;
        }

        var holesInScene = Object.FindObjectsByType<HoleController>(FindObjectsSortMode.None);
        List<HoleData> holeDataList = new List<HoleData>();
        foreach (var h in holesInScene)
        {
            if (h == null) continue;
            holeDataList.Add(new HoleData(h.Color, h.GridCoordinate, h.InitialCapacity));
        }

        var queuesInScene = Object.FindObjectsByType<AntQueueController>(FindObjectsSortMode.None);
        List<AntQueueData> queueDataList = new List<AntQueueData>();
        foreach (var q in queuesInScene)
        {
            if (q == null) continue;

            List<Vector2Int> bodyCoords = new List<Vector2Int>();
            var units = q.GetComponentsInChildren<AntUnit>();
            if (units != null && units.Length > 0)
            {
                foreach (var u in units)
                {
                    if (u != null)
                    {
                        bodyCoords.Add(boardMgr.WorldToGridPosition(u.transform.position));
                    }
                }
            }
            else
            {
                bodyCoords = new List<Vector2Int>(q.CurrentBodyCoordinates);
            }

            List<Vector2Int> pathCoords = new List<Vector2Int>(q.RemainingPath);
            if (bodyCoords.Count > 0 && pathCoords.Count > 0)
            {
                pathCoords[0] = bodyCoords[0];
            }

            queueDataList.Add(new AntQueueData(q.QueueId, q.Color, bodyCoords.Count, bodyCoords, pathCoords));
        }

        if (holeDataList.Count > 0 && queueDataList.Count > 0)
        {
            level1.SetLevelData(1, "Color Slither Jam", "Easy", boardMgr.Width > 0 ? boardMgr.Width : 9, boardMgr.Height > 0 ? boardMgr.Height : 6, boardMgr.CellSize > 0 ? boardMgr.CellSize : 1.0f, holeDataList, queueDataList);

            EditorUtility.SetDirty(level1);
            AssetDatabase.SaveAssets();
            Debug.Log($"[LevelGenerator] Auto-saved {holeDataList.Count} Holes and {queueDataList.Count} Queues from Scene Hierarchy into {Level01AssetPath}!");
        }
    }

    private static Level CreateOrUpdateLevel1Asset()
    {
        Level level1 = AssetDatabase.LoadAssetAtPath<Level>(Level01AssetPath);
        if (level1 == null)
        {
            level1 = ScriptableObject.CreateInstance<Level>();
            level1.name = "Level_01";
            AssetDatabase.CreateAsset(level1, Level01AssetPath);
        }

        // All 5 Goal Trays initialized with capacity equal to the ant segment counts
        List<HoleData> holes1 = new List<HoleData>()
        {
            new HoleData(AntColor.Yellow, new Vector2Int(6, 4), 9),  // Yellow tray (9 ants)
            new HoleData(AntColor.Green, new Vector2Int(6, 3), 7),   // Green tray (7 ants)
            new HoleData(AntColor.Blue, new Vector2Int(6, 2), 5),    // Blue tray (5 ants)
            new HoleData(AntColor.Brown, new Vector2Int(6, 1), 3),   // Brown tray (3 ants)
            new HoleData(AntColor.Red, new Vector2Int(1, 0), 11)     // Red tray (11 ants)
        };

        // 1. Yellow Slither (Length 9): Runs down column 0 and across row 4 to (5,4) facing Yellow tray marked '0'
        var yellowBody1 = new List<Vector2Int> {
            new Vector2Int(5, 4), // Head
            new Vector2Int(4, 4),
            new Vector2Int(3, 4),
            new Vector2Int(2, 4),
            new Vector2Int(1, 4),
            new Vector2Int(0, 4), // Top-Left Corner
            new Vector2Int(0, 3),
            new Vector2Int(0, 2),
            new Vector2Int(0, 1)  // Tail
        };
        var yellowPath1 = new List<Vector2Int> { new Vector2Int(5, 4), new Vector2Int(6, 4) };
        var yellowQueue1 = new AntQueueData("Yellow_Slither", AntColor.Yellow, yellowBody1.Count, yellowBody1, yellowPath1);

        // 2. Green Slither (Length 7): Nested inside Yellow, runs down column 1 and across row 3 to (5,3) facing Green tray
        var greenBody1 = new List<Vector2Int> {
            new Vector2Int(5, 3), // Head
            new Vector2Int(4, 3),
            new Vector2Int(3, 3),
            new Vector2Int(2, 3),
            new Vector2Int(1, 3), // Corner
            new Vector2Int(1, 2),
            new Vector2Int(1, 1)  // Tail
        };
        var greenPath1 = new List<Vector2Int> { new Vector2Int(5, 3), new Vector2Int(6, 3) };
        var greenQueue1 = new AntQueueData("Green_Slither", AntColor.Green, greenBody1.Count, greenBody1, greenPath1);

        // 3. Blue Slither (Length 5): Nested inside Green, runs down column 2 and across row 2 to (5,2) facing Blue tray
        var blueBody1 = new List<Vector2Int> {
            new Vector2Int(5, 2), // Head
            new Vector2Int(4, 2),
            new Vector2Int(3, 2),
            new Vector2Int(2, 2), // Corner
            new Vector2Int(2, 1)  // Tail
        };
        var bluePath1 = new List<Vector2Int> { new Vector2Int(5, 2), new Vector2Int(6, 2) };
        var blueQueue1 = new AntQueueData("Blue_Slither", AntColor.Blue, blueBody1.Count, blueBody1, bluePath1);

        // 4. Brown Slither (Length 3): Short worm nested inside Blue, straight across row 1 to (5,1) facing Brown tray
        var brownBody1 = new List<Vector2Int> {
            new Vector2Int(5, 1), // Head
            new Vector2Int(4, 1),
            new Vector2Int(3, 1)  // Tail
        };
        var brownPath1 = new List<Vector2Int> { new Vector2Int(5, 1), new Vector2Int(6, 1) };
        var brownQueue1 = new AntQueueData("Brown_Slither", AntColor.Brown, brownBody1.Count, brownBody1, brownPath1);

        // 5. Red Slither (Length 11): Outer perimeter wrapping row 0 (bottom) and column 8 (right) behind all trays
        var redBody1 = new List<Vector2Int> {
            new Vector2Int(2, 0), // Head entering Red tray at (1,0)
            new Vector2Int(3, 0),
            new Vector2Int(4, 0),
            new Vector2Int(5, 0),
            new Vector2Int(6, 0),
            new Vector2Int(7, 0),
            new Vector2Int(8, 0), // Bottom-Right Corner
            new Vector2Int(8, 1),
            new Vector2Int(8, 2),
            new Vector2Int(8, 3),
            new Vector2Int(8, 4)  // Tail
        };
        var redPath1 = new List<Vector2Int> { new Vector2Int(2, 0), new Vector2Int(1, 0) };
        var redQueue1 = new AntQueueData("Red_Slither", AntColor.Red, redBody1.Count, redBody1, redPath1);

        level1.SetLevelData(1, "Color Slither Jam", "Easy", 9, 6, 1.0f, holes1, new List<AntQueueData> { yellowQueue1, greenQueue1, blueQueue1, brownQueue1, redQueue1 });

        EditorUtility.SetDirty(level1);
        AssetDatabase.SaveAssets();

        return level1;
    }

    private static Level CreateLevel2Asset()
    {
        string path = $"{DataFolder}/Level_02.asset";
        Level level2 = AssetDatabase.LoadAssetAtPath<Level>(path);
        if (level2 == null)
        {
            level2 = ScriptableObject.CreateInstance<Level>();
            level2.name = "Level_02";
            AssetDatabase.CreateAsset(level2, path);
        }

        List<HoleData> holes2 = new List<HoleData>()
        {
            new HoleData(AntColor.Yellow, new Vector2Int(6, 4), 4),
            new HoleData(AntColor.Blue, new Vector2Int(6, 2), 4),
            new HoleData(AntColor.Green, new Vector2Int(1, 4), 4),
            new HoleData(AntColor.Red, new Vector2Int(1, 1), 4)
        };

        var yelBody2 = new List<Vector2Int> { new Vector2Int(5, 4), new Vector2Int(4, 4), new Vector2Int(3, 4), new Vector2Int(2, 4) };
        var yelPath2 = new List<Vector2Int> { new Vector2Int(5, 4), new Vector2Int(6, 4) };
        var yelQueue2 = new AntQueueData("Yellow_2", AntColor.Yellow, yelBody2.Count, yelBody2, yelPath2);

        var bluBody2 = new List<Vector2Int> { new Vector2Int(5, 2), new Vector2Int(4, 2), new Vector2Int(3, 2), new Vector2Int(2, 2) };
        var bluPath2 = new List<Vector2Int> { new Vector2Int(5, 2), new Vector2Int(6, 2) };
        var bluQueue2 = new AntQueueData("Blue_2", AntColor.Blue, bluBody2.Count, bluBody2, bluPath2);

        var grnBody2 = new List<Vector2Int> { new Vector2Int(2, 3), new Vector2Int(3, 3), new Vector2Int(4, 3), new Vector2Int(5, 3) };
        var grnPath2 = new List<Vector2Int> { new Vector2Int(2, 3), new Vector2Int(2, 4), new Vector2Int(1, 4) };
        var grnQueue2 = new AntQueueData("Green_2", AntColor.Green, grnBody2.Count, grnBody2, grnPath2);

        var redBody2 = new List<Vector2Int> { new Vector2Int(2, 1), new Vector2Int(3, 1), new Vector2Int(4, 1), new Vector2Int(5, 1) };
        var redPath2 = new List<Vector2Int> { new Vector2Int(2, 1), new Vector2Int(1, 1) };
        var redQueue2 = new AntQueueData("Red_2", AntColor.Red, redBody2.Count, redBody2, redPath2);

        level2.SetLevelData(2, "S-Curve Interlock", "Medium", 9, 6, 1.0f, holes2, new List<AntQueueData> { yelQueue2, bluQueue2, grnQueue2, redQueue2 });

        EditorUtility.SetDirty(level2);
        AssetDatabase.SaveAssets();
        return level2;
    }

    private static Level CreateLevel3Asset()
    {
        string path = $"{DataFolder}/Level_03.asset";
        Level level3 = AssetDatabase.LoadAssetAtPath<Level>(path);
        if (level3 == null)
        {
            level3 = ScriptableObject.CreateInstance<Level>();
            level3.name = "Level_03";
            AssetDatabase.CreateAsset(level3, path);
        }

        List<HoleData> holes3 = new List<HoleData>()
        {
            new HoleData(AntColor.Yellow, new Vector2Int(6, 4), 6),
            new HoleData(AntColor.Blue, new Vector2Int(6, 2), 6),
            new HoleData(AntColor.Red, new Vector2Int(1, 0), 11)
        };

        // Yellow 3A (Front): (5,4), (4,4), (3,4)
        var yelBody3A = new List<Vector2Int> { new Vector2Int(5, 4), new Vector2Int(4, 4), new Vector2Int(3, 4) };
        var yelPath3A = new List<Vector2Int> { new Vector2Int(5, 4), new Vector2Int(6, 4) };
        var yelQ3A = new AntQueueData("Yellow_3A", AntColor.Yellow, 3, yelBody3A, yelPath3A);

        // Yellow 3B (Back - blocked by 3A): (2,4), (1,4), (0,4) - on row 4, safely away from holes
        var yelBody3B = new List<Vector2Int> { new Vector2Int(2, 4), new Vector2Int(1, 4), new Vector2Int(0, 4) };
        var yelPath3B = new List<Vector2Int> { new Vector2Int(2, 4), new Vector2Int(3, 4), new Vector2Int(4, 4), new Vector2Int(5, 4), new Vector2Int(6, 4) };
        var yelQ3B = new AntQueueData("Yellow_3B", AntColor.Yellow, 3, yelBody3B, yelPath3B);

        // Blue 3A (Front): (5,2), (4,2), (3,2)
        var bluBody3A = new List<Vector2Int> { new Vector2Int(5, 2), new Vector2Int(4, 2), new Vector2Int(3, 2) };
        var bluPath3A = new List<Vector2Int> { new Vector2Int(5, 2), new Vector2Int(6, 2) };
        var bluQ3A = new AntQueueData("Blue_3A", AntColor.Blue, 3, bluBody3A, bluPath3A);

        // Blue 3B (Back - blocked by 3A): (2,2), (1,2), (0,2) - on row 2, safely away from Red hole at row 0
        var bluBody3B = new List<Vector2Int> { new Vector2Int(2, 2), new Vector2Int(1, 2), new Vector2Int(0, 2) };
        var bluPath3B = new List<Vector2Int> { new Vector2Int(2, 2), new Vector2Int(3, 2), new Vector2Int(4, 2), new Vector2Int(5, 2), new Vector2Int(6, 2) };
        var bluQ3B = new AntQueueData("Blue_3B", AntColor.Blue, 3, bluBody3B, bluPath3B);

        // Red Slither (Outer perimeter): (2,0) to (8,4)
        var redBody3 = new List<Vector2Int> { new Vector2Int(2, 0), new Vector2Int(3, 0), new Vector2Int(4, 0), new Vector2Int(5, 0), new Vector2Int(6, 0), new Vector2Int(7, 0), new Vector2Int(8, 0), new Vector2Int(8, 1), new Vector2Int(8, 2), new Vector2Int(8, 3), new Vector2Int(8, 4) };
        var redPath3 = new List<Vector2Int> { new Vector2Int(2, 0), new Vector2Int(1, 0) };
        var redQ3 = new AntQueueData("Red_3", AntColor.Red, redBody3.Count, redBody3, redPath3);

        level3.SetLevelData(3, "Twin Worms Jam", "Medium", 9, 6, 1.0f, holes3, new List<AntQueueData> { yelQ3A, bluQ3A, yelQ3B, bluQ3B, redQ3 });

        EditorUtility.SetDirty(level3);
        AssetDatabase.SaveAssets();
        return level3;
    }

    private static void EnsureDataFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Data"))
        {
            AssetDatabase.CreateFolder("Assets", "Data");
        }
        if (!AssetDatabase.IsValidFolder(DataFolder))
        {
            AssetDatabase.CreateFolder("Assets/Data", "Levels");
        }
    }
}
