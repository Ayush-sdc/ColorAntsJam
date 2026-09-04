using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class LevelGeneratorWindow
{
    private const string DataFolder = "Assets/Data/Levels";
    private const string Level01AssetPath = "Assets/Data/Levels/Level_01.asset";
    private const string DatabaseAssetPath = "Assets/Data/Levels/LevelDatabase.asset";

    [MenuItem("SlitherAnts/Sync Play Mode Layout to Edit Mode")]
    [MenuItem("SlitherAnts/Build Level 1 in Scene (Edit Mode)")]
    public static void BuildLevel1InSceneHierarchy()
    {
        EnsureDataFolders();

        Level level1 = AssetDatabase.LoadAssetAtPath<Level>(Level01AssetPath);
        if (level1 == null)
        {
            level1 = CreateOrUpdateLevel1Asset();
        }

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

        // Setup the board using the exact same code that runs in Play Mode!
        boardMgr.SetupBoard(level1);

        // Mark all generated scene objects dirty so Unity persists them in Edit Mode
        EditorUtility.SetDirty(boardMgr);
        if (boardObj != null)
        {
            EditorUtility.SetDirty(boardObj);
            foreach (Transform child in boardObj.GetComponentsInChildren<Transform>(true))
            {
                EditorUtility.SetDirty(child.gameObject);
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();

        Debug.Log("[LevelGenerator] Successfully synced and baked Play Mode Level 1 layout into Edit Mode Scene!");
    }

    [MenuItem("SlitherAnts/Generate All Levels and Database")]
    public static void GenerateSampleLevelsAndDatabase()
    {
        EnsureDataFolders();

        List<Level> generatedLevels = new List<Level>();

        Level level1 = AssetDatabase.LoadAssetAtPath<Level>(Level01AssetPath);
        if (level1 == null)
        {
            level1 = CreateOrUpdateLevel1Asset();
        }
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
        var yellowPath1 = new List<Vector2Int> {
            new Vector2Int(5, 4),
            new Vector2Int(6, 4)  // Into Yellow hole (6,4)
        };

        // 2. Green Slither (Length 7): S-turn around column 1 up to (5,3) facing Green tray
        var greenBody1 = new List<Vector2Int> {
            new Vector2Int(5, 3), // Head
            new Vector2Int(4, 3),
            new Vector2Int(3, 3),
            new Vector2Int(2, 3),
            new Vector2Int(1, 3),
            new Vector2Int(1, 2),
            new Vector2Int(1, 1)  // Tail
        };
        var greenPath1 = new List<Vector2Int> {
            new Vector2Int(5, 3),
            new Vector2Int(6, 3)  // Into Green hole (6,3)
        };

        // 3. Blue Slither (Length 5): Straight across row 2 up to (5,2) facing Blue tray
        var blueBody1 = new List<Vector2Int> {
            new Vector2Int(5, 2), // Head
            new Vector2Int(4, 2),
            new Vector2Int(3, 2),
            new Vector2Int(2, 2),
            new Vector2Int(2, 1)  // Tail
        };
        var bluePath1 = new List<Vector2Int> {
            new Vector2Int(5, 2),
            new Vector2Int(6, 2)  // Into Blue hole (6,2)
        };

        // 4. Brown Slither (Length 3): Compact 3-ant line across row 1 to (5,1) facing Brown tray
        var brownBody1 = new List<Vector2Int> {
            new Vector2Int(5, 1), // Head
            new Vector2Int(4, 1),
            new Vector2Int(3, 1)  // Tail
        };
        var brownPath1 = new List<Vector2Int> {
            new Vector2Int(5, 1),
            new Vector2Int(6, 1)  // Into Brown hole (6,1)
        };

        // 5. Red Slither (Length 11): Perimeter blocker starting at (2,0) facing LEFT into Red tray at (1,0)
        var redBody1 = new List<Vector2Int> {
            new Vector2Int(2, 0), // Head
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
        var redPath1 = new List<Vector2Int> {
            new Vector2Int(2, 0),
            new Vector2Int(1, 0)  // Into Red hole (1,0)
        };

        List<AntQueueData> queues1 = new List<AntQueueData>()
        {
            new AntQueueData("Green_Slither", AntColor.Green, greenBody1.Count, greenBody1, greenPath1),
            new AntQueueData("Red_Slither", AntColor.Red, redBody1.Count, redBody1, redPath1),
            new AntQueueData("Yellow_Slither", AntColor.Yellow, yellowBody1.Count, yellowBody1, yellowPath1),
            new AntQueueData("Brown_Slither", AntColor.Brown, brownBody1.Count, brownBody1, brownPath1),
            new AntQueueData("Blue_Slither", AntColor.Blue, blueBody1.Count, blueBody1, bluePath1)
        };

        level1.SetLevelData(1, "Color Slither Jam", "Easy", 9, 6, 1.0f, holes1, queues1);
        EditorUtility.SetDirty(level1);
        AssetDatabase.SaveAssets();

        return level1;
    }

    private static Level CreateLevel2Asset()
    {
        string path = $"{DataFolder}/Level_02.asset";
        Level level = AssetDatabase.LoadAssetAtPath<Level>(path);
        if (level == null)
        {
            level = ScriptableObject.CreateInstance<Level>();
            level.name = "Level_02";
            AssetDatabase.CreateAsset(level, path);
        }

        // 5 Food Boxes matching reference photo (2 parallel ants capacity each)
        List<HoleData> holes = new List<HoleData>()
        {
            new HoleData(AntColor.Blue, new Vector2Int(1, 9), 2),    // Top-Left Blue Box
            new HoleData(AntColor.Green, new Vector2Int(10, 9), 2),  // Top-Right Green Box
            new HoleData(AntColor.Red, new Vector2Int(1, 0), 2),     // Bottom-Left Red Box
            new HoleData(AntColor.Orange, new Vector2Int(5, 0), 2),  // Bottom-Center Orange Box
            new HoleData(AntColor.Cyan, new Vector2Int(10, 2), 2)    // Bottom-Right Cyan Box
        };

        // 10 Ant Queues (5 Pairs of Parallel Ants)
        // 1. Dark Blue (Top Horizontal, facing LEFT into Blue Box)
        var blueTopBody = new List<Vector2Int> {
            new Vector2Int(3, 10), new Vector2Int(4, 10), new Vector2Int(5, 10), new Vector2Int(6, 10),
            new Vector2Int(7, 10), new Vector2Int(8, 10), new Vector2Int(9, 10)
        };
        var blueTopPath = new List<Vector2Int> { new Vector2Int(3, 10), new Vector2Int(2, 10), new Vector2Int(1, 10) };

        var blueBottomBody = new List<Vector2Int> {
            new Vector2Int(3, 9), new Vector2Int(4, 9), new Vector2Int(5, 9), new Vector2Int(6, 9),
            new Vector2Int(7, 9), new Vector2Int(8, 9), new Vector2Int(9, 9)
        };
        var blueBottomPath = new List<Vector2Int> { new Vector2Int(3, 9), new Vector2Int(2, 9), new Vector2Int(1, 9) };

        // 2. Green (Right Vertical, facing UP into Green Box)
        var greenLeftBody = new List<Vector2Int> {
            new Vector2Int(10, 8), new Vector2Int(10, 7), new Vector2Int(10, 6), new Vector2Int(10, 5), new Vector2Int(10, 4)
        };
        var greenLeftPath = new List<Vector2Int> { new Vector2Int(10, 8), new Vector2Int(10, 9) };

        var greenRightBody = new List<Vector2Int> {
            new Vector2Int(11, 8), new Vector2Int(11, 7), new Vector2Int(11, 6), new Vector2Int(11, 5), new Vector2Int(11, 4)
        };
        var greenRightPath = new List<Vector2Int> { new Vector2Int(11, 8), new Vector2Int(11, 9) };

        // 3. Red (Left Vertical, facing DOWN into Red Box)
        var redLeftBody = new List<Vector2Int> {
            new Vector2Int(1, 2), new Vector2Int(1, 3), new Vector2Int(1, 4), new Vector2Int(1, 5),
            new Vector2Int(1, 6), new Vector2Int(1, 7), new Vector2Int(1, 8)
        };
        var redLeftPath = new List<Vector2Int> { new Vector2Int(1, 2), new Vector2Int(1, 1), new Vector2Int(1, 0) };

        var redRightBody = new List<Vector2Int> {
            new Vector2Int(2, 2), new Vector2Int(2, 3), new Vector2Int(2, 4), new Vector2Int(2, 5),
            new Vector2Int(2, 6), new Vector2Int(2, 7), new Vector2Int(2, 8)
        };
        var redRightPath = new List<Vector2Int> { new Vector2Int(2, 2), new Vector2Int(2, 1), new Vector2Int(2, 0) };

        // 4. Cyan (Bottom Horizontal, facing RIGHT into Cyan Box)
        var cyanTopBody = new List<Vector2Int> {
            new Vector2Int(9, 3), new Vector2Int(8, 3), new Vector2Int(7, 3), new Vector2Int(6, 3),
            new Vector2Int(5, 3), new Vector2Int(4, 3), new Vector2Int(3, 3)
        };
        var cyanTopPath = new List<Vector2Int> { new Vector2Int(9, 3), new Vector2Int(10, 3) };

        var cyanBottomBody = new List<Vector2Int> {
            new Vector2Int(9, 2), new Vector2Int(8, 2), new Vector2Int(7, 2), new Vector2Int(6, 2),
            new Vector2Int(5, 2), new Vector2Int(4, 2), new Vector2Int(3, 2)
        };
        var cyanBottomPath = new List<Vector2Int> { new Vector2Int(9, 2), new Vector2Int(10, 2) };

        // 5. Orange (Center Vertical, facing DOWN into Orange Box)
        var orangeLeftBody = new List<Vector2Int> {
            new Vector2Int(5, 4), new Vector2Int(5, 5), new Vector2Int(5, 6), new Vector2Int(5, 7)
        };
        var orangeLeftPath = new List<Vector2Int> {
            new Vector2Int(5, 4), new Vector2Int(5, 3), new Vector2Int(5, 2), new Vector2Int(5, 1), new Vector2Int(5, 0)
        };

        var orangeRightBody = new List<Vector2Int> {
            new Vector2Int(6, 4), new Vector2Int(6, 5), new Vector2Int(6, 6), new Vector2Int(6, 7)
        };
        var orangeRightPath = new List<Vector2Int> {
            new Vector2Int(6, 4), new Vector2Int(6, 3), new Vector2Int(6, 2), new Vector2Int(6, 1), new Vector2Int(6, 0)
        };

        List<AntQueueData> queues = new List<AntQueueData>()
        {
            new AntQueueData("Blue_Top", AntColor.Blue, blueTopBody.Count, blueTopBody, blueTopPath),
            new AntQueueData("Blue_Bottom", AntColor.Blue, blueBottomBody.Count, blueBottomBody, blueBottomPath),
            new AntQueueData("Green_Left", AntColor.Green, greenLeftBody.Count, greenLeftBody, greenLeftPath),
            new AntQueueData("Green_Right", AntColor.Green, greenRightBody.Count, greenRightBody, greenRightPath),
            new AntQueueData("Red_Left", AntColor.Red, redLeftBody.Count, redLeftBody, redLeftPath),
            new AntQueueData("Red_Right", AntColor.Red, redRightBody.Count, redRightBody, redRightPath),
            new AntQueueData("Cyan_Top", AntColor.Cyan, cyanTopBody.Count, cyanTopBody, cyanTopPath),
            new AntQueueData("Cyan_Bottom", AntColor.Cyan, cyanBottomBody.Count, cyanBottomBody, cyanBottomPath),
            new AntQueueData("Orange_Left", AntColor.Orange, orangeLeftBody.Count, orangeLeftBody, orangeLeftPath),
            new AntQueueData("Orange_Right", AntColor.Orange, orangeRightBody.Count, orangeRightBody, orangeRightPath)
        };

        level.SetLevelData(2, "Color Slither Jam", "Medium", 13, 12, 1.0f, holes, queues);
        EditorUtility.SetDirty(level);
        AssetDatabase.SaveAssets();
        return level;
    }

    private static Level CreateLevel3Asset()
    {
        string path = $"{DataFolder}/Level_03.asset";
        Level level = AssetDatabase.LoadAssetAtPath<Level>(path);
        if (level == null)
        {
            level = ScriptableObject.CreateInstance<Level>();
            level.name = "Level_03";
            AssetDatabase.CreateAsset(level, path);
        }

        List<HoleData> holes = new List<HoleData>()
        {
            new HoleData(AntColor.Yellow, new Vector2Int(6, 3), 4),
            new HoleData(AntColor.Red, new Vector2Int(6, 1), 4)
        };

        var yBody = new List<Vector2Int> { new Vector2Int(5, 3), new Vector2Int(4, 3), new Vector2Int(3, 3), new Vector2Int(2, 3) };
        var rBody = new List<Vector2Int> { new Vector2Int(5, 1), new Vector2Int(4, 1), new Vector2Int(3, 1), new Vector2Int(2, 1) };

        List<AntQueueData> queues = new List<AntQueueData>()
        {
            new AntQueueData("Y_Slither", AntColor.Yellow, yBody.Count, yBody, new List<Vector2Int> { new Vector2Int(5, 3), new Vector2Int(6, 3) }),
            new AntQueueData("R_Slither", AntColor.Red, rBody.Count, rBody, new List<Vector2Int> { new Vector2Int(5, 1), new Vector2Int(6, 1) })
        };

        level.SetLevelData(3, "Crossroad Jam", "Hard", 8, 5, 1.0f, holes, queues);
        EditorUtility.SetDirty(level);
        return level;
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
