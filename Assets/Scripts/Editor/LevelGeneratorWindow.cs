using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class LevelGeneratorWindow
{
    [MenuItem("SlitherAnts/Generate Sample Levels and Database")]
    public static void GenerateSampleLevelsAndDatabase()
    {
        string dataFolder = "Assets/Data/Levels";
        if (!AssetDatabase.IsValidFolder("Assets/Data"))
        {
            AssetDatabase.CreateFolder("Assets", "Data");
        }
        if (!AssetDatabase.IsValidFolder(dataFolder))
        {
            AssetDatabase.CreateFolder("Assets/Data", "Levels");
        }

        List<Level> generatedLevels = new List<Level>();

        // Level 1: 3 Queues (Blue blocked by Green, Green blocked by Purple, Purple free)
        // Order to solve: Purple -> Green -> Blue
        Level level1 = ScriptableObject.CreateInstance<Level>();
        level1.name = "Level_01";

        List<HoleData> holes1 = new List<HoleData>()
        {
            new HoleData(AntColor.Purple, new Vector2Int(2, 8)),
            new HoleData(AntColor.Green, new Vector2Int(4, 8)),
            new HoleData(AntColor.Blue, new Vector2Int(6, 8))
        };

        // Purple queue: at (2,4)-(2,2), path: (2,4) -> (2,5) -> (2,6) -> (2,7) -> (2,8) [Free]
        var purpleBody = new List<Vector2Int> { new Vector2Int(2, 4), new Vector2Int(2, 3), new Vector2Int(2, 2) };
        var purplePath = new List<Vector2Int> { new Vector2Int(2, 4), new Vector2Int(2, 5), new Vector2Int(2, 6), new Vector2Int(2, 7), new Vector2Int(2, 8) };
        var purpleQueue = new AntQueueData("Purple_Queue", AntColor.Purple, 3, purpleBody, purplePath);

        // Green queue: at (1,6)-(1,4), path moves horizontally crossing (2,6) towards its hole at (4,8)
        // Blocked at (2,6) by Purple queue's path until Purple clears!
        var greenBody = new List<Vector2Int> { new Vector2Int(1, 6), new Vector2Int(1, 5), new Vector2Int(1, 4) };
        var greenPath = new List<Vector2Int> {
            new Vector2Int(1, 6),
            new Vector2Int(2, 6), // Intersects purple queue path
            new Vector2Int(3, 6),
            new Vector2Int(4, 6),
            new Vector2Int(4, 7),
            new Vector2Int(4, 8)  // Green hole
        };
        var greenQueue = new AntQueueData("Green_Queue", AntColor.Green, 3, greenBody, greenPath);

        // Blue queue: at (3,5)-(3,3), path crosses (4,6) towards hole (6,8)
        // Blocked at (4,6) by Green queue until Green moves!
        var blueBody = new List<Vector2Int> { new Vector2Int(3, 5), new Vector2Int(3, 4), new Vector2Int(3, 3) };
        var bluePath = new List<Vector2Int> {
            new Vector2Int(3, 5),
            new Vector2Int(4, 5),
            new Vector2Int(4, 6), // Intersects green queue path
            new Vector2Int(5, 6),
            new Vector2Int(6, 6),
            new Vector2Int(6, 7),
            new Vector2Int(6, 8)  // Blue hole
        };
        var blueQueue = new AntQueueData("Blue_Queue", AntColor.Blue, 3, blueBody, bluePath);

        level1.SetLevelData(1, "Tutorial - The Chain Unblock", "Easy", 9, 10, 1.0f, holes1, new List<AntQueueData> { purpleQueue, greenQueue, blueQueue });

        AssetDatabase.CreateAsset(level1, $"{dataFolder}/Level_01.asset");
        generatedLevels.Add(level1);

        // Level 2: 4 Queues (Yellow, Blue, Green, Red)
        Level level2 = ScriptableObject.CreateInstance<Level>();
        level2.name = "Level_02";

        List<HoleData> holes2 = new List<HoleData>()
        {
            new HoleData(AntColor.Yellow, new Vector2Int(2, 9)),
            new HoleData(AntColor.Blue, new Vector2Int(5, 9)),
            new HoleData(AntColor.Green, new Vector2Int(8, 9)),
            new HoleData(AntColor.Red, new Vector2Int(5, 1))
        };

        // Red queue is free moving downward
        var redBody = new List<Vector2Int> { new Vector2Int(5, 4), new Vector2Int(5, 5), new Vector2Int(5, 6) };
        var redPath = new List<Vector2Int> { new Vector2Int(5, 4), new Vector2Int(5, 3), new Vector2Int(5, 2), new Vector2Int(5, 1) };
        var redQueue = new AntQueueData("Red_Queue", AntColor.Red, 3, redBody, redPath);

        // Yellow queue
        var yelBody = new List<Vector2Int> { new Vector2Int(2, 6), new Vector2Int(2, 5), new Vector2Int(2, 4) };
        var yelPath = new List<Vector2Int> { new Vector2Int(2, 6), new Vector2Int(2, 7), new Vector2Int(2, 8), new Vector2Int(2, 9) };
        var yelQueue = new AntQueueData("Yellow_Queue", AntColor.Yellow, 3, yelBody, yelPath);

        // Green queue crosses Yellow path
        var grnBody = new List<Vector2Int> { new Vector2Int(8, 5), new Vector2Int(8, 4), new Vector2Int(8, 3) };
        var grnPath = new List<Vector2Int> { new Vector2Int(8, 5), new Vector2Int(8, 6), new Vector2Int(8, 7), new Vector2Int(8, 8), new Vector2Int(8, 9) };
        var grnQueue = new AntQueueData("Green_Queue_2", AntColor.Green, 3, grnBody, grnPath);

        // Blue queue crosses Green
        var bluBody = new List<Vector2Int> { new Vector2Int(4, 7), new Vector2Int(3, 7), new Vector2Int(2, 7) };
        var bluPath = new List<Vector2Int> { new Vector2Int(4, 7), new Vector2Int(5, 7), new Vector2Int(5, 8), new Vector2Int(5, 9) };
        var bluQueue = new AntQueueData("Blue_Queue_2", AntColor.Blue, 3, bluBody, bluPath);

        level2.SetLevelData(2, "Crossroads of Ants", "Medium", 11, 11, 1.0f, holes2, new List<AntQueueData> { redQueue, yelQueue, grnQueue, bluQueue });
        AssetDatabase.CreateAsset(level2, $"{dataFolder}/Level_02.asset");
        generatedLevels.Add(level2);

        // Create or Update LevelDatabase
        string dbPath = "Assets/Data/Levels/LevelDatabase.asset";
        LevelDatabase db = AssetDatabase.LoadAssetAtPath<LevelDatabase>(dbPath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<LevelDatabase>();
            AssetDatabase.CreateAsset(db, dbPath);
        }

        var field = typeof(LevelDatabase).GetField("levels", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(db, generatedLevels);
        }

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[LevelGenerator] Successfully generated {generatedLevels.Count} Levels and configured LevelDatabase at {dbPath}!");
    }
}
