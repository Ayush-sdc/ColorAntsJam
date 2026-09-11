#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class TestLevels1To5Validation
{
    [MenuItem("Tools/Slither Ants Jam/Tests/Validate Levels 1 to 5")]
    public static void RunValidation()
    {
        Debug.Log("==================================================");
        Debug.Log("STARTING VALIDATION: LEVELS 1-5 FROM NORMAL LEVELDATABASE");
        Debug.Log("==================================================");

        string dbPath = "Assets/Data/Levels/LevelDatabase.asset";
        LevelDatabase database = AssetDatabase.LoadAssetAtPath<LevelDatabase>(dbPath);
        if (database == null)
        {
            Debug.LogError($"[Validation] FAILED: Could not load LevelDatabase at '{dbPath}'!");
            return;
        }

        int levelCount = database.LevelCount;
        Debug.Log($"[Validation] Loaded LevelDatabase: {levelCount} total levels.");
        if (levelCount < 5)
        {
            Debug.LogError($"[Validation] FAILED: LevelDatabase contains only {levelCount} levels, expected >= 5!");
            return;
        }

        int[] expectedGridW = { 8, 12, 14, 14, 14 };
        int[] expectedGridH = { 10, 10, 10, 12, 12 };
        int[] expectedHoles = { 5, 5, 5, 9, 6 };
        int[] expectedQueues = { 5, 10, 15, 14, 23 };

        bool allPassed = true;

        for (int i = 0; i < 5; i++)
        {
            int levelNumber = i + 1;
            Level level = database.GetLevel(levelNumber);
            if (level == null)
            {
                Debug.LogError($"[Validation] FAILED: Level {levelNumber} is null!");
                allPassed = false;
                continue;
            }

            string assetPath = AssetDatabase.GetAssetPath(level);
            Debug.Log($"--- Level {levelNumber} ('{level.name}') at '{assetPath}' ---");

            if (level.GridWidth != expectedGridW[i] || level.GridHeight != expectedGridH[i])
            {
                Debug.LogError($"[Validation] Level {levelNumber}: Grid mismatch! Got {level.GridWidth}x{level.GridHeight}, expected {expectedGridW[i]}x{expectedGridH[i]}");
                allPassed = false;
            }
            else
            {
                Debug.Log($"[Validation] Level {levelNumber}: Grid size verified: {level.GridWidth}x{level.GridHeight}");
            }

            bool isRef = ReferenceLevelRuntimeAdapter.IsReferenceLevel(level);
            if (!isRef)
            {
                Debug.LogError($"[Validation] Level {levelNumber}: IsReferenceLevel returned false!");
                allPassed = false;
                continue;
            }

            var holes = ReferenceLevelRuntimeAdapter.ExtractHoles(level);
            var queues = ReferenceLevelRuntimeAdapter.ExtractPlayableQueues(level, holes);

            if (holes.Count != expectedHoles[i])
            {
                Debug.LogError($"[Validation] Level {levelNumber}: Holes count mismatch! Got {holes.Count}, expected {expectedHoles[i]}");
                allPassed = false;
            }
            if (queues.Count != expectedQueues[i])
            {
                Debug.LogError($"[Validation] Level {levelNumber}: Queues count mismatch! Got {queues.Count}, expected {expectedQueues[i]}");
                allPassed = false;
            }

            int sumCapacity = 0;
            Debug.Log($"[Validation] Level {levelNumber}: Holders Details ({holes.Count} total):");
            foreach (var h in holes)
            {
                sumCapacity += h.Capacity;
                Debug.Log($"   Holder: ColorID={h.ColorID} ({h.AntColor}) | Count={h.Capacity} | Footprint={h.WidthCells}x{h.HeightCells} | Bounds=[({h.MinX},{h.MinY}) to ({h.MaxX},{h.MaxY})] | Entrance=({h.EntranceCell.x},{h.EntranceCell.y})");
            }

            if (sumCapacity != queues.Count)
            {
                Debug.LogError($"[Validation] Level {levelNumber}: Capacity sum ({sumCapacity}) != Queue count ({queues.Count})!");
                allPassed = false;
            }

            // Test BoardManager instantiation
            GameObject boardObj = new GameObject($"TestBoard_Level_{levelNumber}");
            var boardManager = boardObj.AddComponent<BoardManager>();
            try
            {
                boardManager.SetupBoard(level, true);
                var spawnedHoles = boardObj.GetComponentsInChildren<HoleController>(true);
                var spawnedQueues = boardObj.GetComponentsInChildren<AntQueueController>(true);

                if (spawnedHoles.Length != expectedHoles[i])
                {
                    Debug.LogError($"[Validation] Level {levelNumber}: Spawned HoleControllers mismatch! Got {spawnedHoles.Length}, expected {expectedHoles[i]}");
                    allPassed = false;
                }
                if (spawnedQueues.Length != expectedQueues[i])
                {
                    Debug.LogError($"[Validation] Level {levelNumber}: Spawned AntQueueControllers mismatch! Got {spawnedQueues.Length}, expected {expectedQueues[i]}");
                    allPassed = false;
                }

                foreach (var sh in spawnedHoles)
                {
                    if (sh.RemainingCount <= 0)
                    {
                        Debug.LogError($"[Validation] Level {levelNumber}: Hole {sh.name} has invalid initial remainingCount: {sh.RemainingCount}!");
                        allPassed = false;
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(boardObj);
            }
        }

        Debug.Log("==================================================");
        if (allPassed)
        {
            Debug.Log("ALL VALIDATIONS PASSED SUCCESSFULLY FOR LEVELS 1 TO 5!");
        }
        else
        {
            Debug.LogError("VALIDATION FAILED ON ONE OR MORE CHECKS!");
        }
        Debug.Log("==================================================");
    }
}
#endif
