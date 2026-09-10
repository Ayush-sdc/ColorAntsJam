#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class TestReferenceLevelDatabase
{
    private const string RefDatabasePath = "Assets/Data/ReferenceLevelDatabase.asset";

    [MenuItem("Tools/Slither Ants Jam/Tests/Validate Reference Level Database")]
    public static bool ValidateDatabase()
    {
        Debug.Log("==================================================");
        Debug.Log("VALIDATING: ReferenceLevelDatabase.asset");
        Debug.Log("==================================================");

        LevelDatabase db = AssetDatabase.LoadAssetAtPath<LevelDatabase>(RefDatabasePath);
        if (db == null)
        {
            Debug.LogError($"[RefDB Validation] FAILED: Could not load LevelDatabase at '{RefDatabasePath}'!");
            return false;
        }

        var levels = db.Levels;
        if (levels == null || levels.Count != 45)
        {
            Debug.LogError($"[RefDB Validation] FAILED: Expected 45 levels, found {levels?.Count ?? 0}!");
            return false;
        }

        HashSet<Level> seenLevels = new HashSet<Level>();
        bool allSequential = true;
        bool hasNulls = false;

        for (int i = 0; i < 45; i++)
        {
            int expectedNum = i + 1;
            string expectedName = $"Level_{expectedNum:000}";
            Level lvl = levels[i];

            if (lvl == null)
            {
                Debug.LogError($"[RefDB Validation] Entry [{i}] (Expected {expectedName}) is NULL!");
                hasNulls = true;
                continue;
            }

            if (seenLevels.Contains(lvl))
            {
                Debug.LogError($"[RefDB Validation] Duplicate reference detected for level '{lvl.name}' at index [{i}]!");
                allSequential = false;
            }
            seenLevels.Add(lvl);

            if (lvl.name != expectedName && lvl.LevelNumber != expectedNum)
            {
                Debug.LogError($"[RefDB Validation] Order mismatch at index [{i}]: Expected {expectedName} / {expectedNum}, found {lvl.name} / {lvl.LevelNumber}!");
                allSequential = false;
            }
        }

        if (hasNulls || !allSequential || seenLevels.Count != 45)
        {
            Debug.LogError($"[RefDB Validation] FAILED: Validation failed! Unique valid levels: {seenLevels.Count}/45");
            return false;
        }

        string firstAsset = levels[0] != null ? levels[0].name : "null";
        string lastAsset = levels[44] != null ? levels[44].name : "null";

        Debug.Log($"[RefDB Validation] First Asset: {firstAsset}");
        Debug.Log($"[RefDB Validation] Last Asset: {lastAsset}");
        Debug.Log($"[RefDB Validation] Null Entries: 0");
        Debug.Log($"[RefDB Validation] Duplicate References: 0");
        Debug.Log($"[RefDB Validation] Sequential Order: Confirmed (1 to 45)");
        Debug.Log("==================================================");
        Debug.Log("Reference Level Database: 45/45 VALID");
        Debug.Log("==================================================");

        return true;
    }
}
#endif
