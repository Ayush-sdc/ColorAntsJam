#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class TestReferenceLevel001Adapter
{
    [MenuItem("Tools/Slither Ants Jam/Tests/Test Reference Level 001 Adapter")]
    public static void RunTest()
    {
        Debug.Log("==================================================");
        Debug.Log("STARTING TASK 4B TEST: REFERENCE LEVEL 001 ADAPTER");
        Debug.Log("==================================================");

        string assetPath = "Assets/Data/ReferenceLevels/Level_001.asset";
        Level level = AssetDatabase.LoadAssetAtPath<Level>(assetPath);

        if (level == null)
        {
            Debug.LogError($"[Test] FAILED: Could not load Level asset at '{assetPath}'!");
            return;
        }

        Debug.Log($"[Test] Loaded '{level.name}': Grid={level.GridWidth}x{level.GridHeight}, Total Records in AntQueues={level.AntQueues.Count}");

        // 1. Test IsReferenceLevel
        bool isRef = ReferenceLevelRuntimeAdapter.IsReferenceLevel(level);
        Debug.Log($"[Test] ReferenceLevelRuntimeAdapter.IsReferenceLevel: {isRef} (Expected: True)");
        if (!isRef)
        {
            Debug.LogError("[Test] FAILED: IsReferenceLevel returned false!");
            return;
        }

        // 2. Test ExtractHoles
        var holes = ReferenceLevelRuntimeAdapter.ExtractHoles(level);
        Debug.Log($"[Test] Extracted {holes.Count} Holes (Expected: 5):");
        int totalHoleCapacity = 0;
        for (int i = 0; i < holes.Count; i++)
        {
            var h = holes[i];
            totalHoleCapacity += h.Capacity;
            string cellsStr = string.Join(", ", h.OccupiedCells);
            Debug.Log($"  Hole #{i + 1}: ColorID={h.ColorID} ({h.AntColor}), Capacity={h.Capacity}, Entrance={h.EntranceCell}, IsRightSide={h.IsRightSide}, Cells=[{cellsStr}]");
        }

        if (holes.Count != 5)
        {
            Debug.LogError($"[Test] FAILED: Expected 5 holes, got {holes.Count}!");
            return;
        }

        // 3. Test ExtractPlayableQueues
        var queues = ReferenceLevelRuntimeAdapter.ExtractPlayableQueues(level, holes);
        Debug.Log($"[Test] Extracted {queues.Count} Playable Ant Queues (Expected: 5):");
        int totalAnts = 0;
        for (int i = 0; i < queues.Count; i++)
        {
            var q = queues[i];
            totalAnts += q.BodyCoordinates.Count;
            string pathStr = string.Join(" -> ", q.PathToHole);
            Debug.Log($"  Queue #{i + 1}: Id={q.QueueId}, ColorID={q.ColorID} ({q.AntColor}), Ants={q.BodyCoordinates.Count}, Head={q.BodyCoordinates[0]}, Path=[{pathStr}]");
        }

        if (queues.Count != 5)
        {
            Debug.LogError($"[Test] FAILED: Expected 5 queues, got {queues.Count}!");
            return;
        }

        Debug.Log($"[Test] Summary: Total Ants = {totalAnts} (Expected: 30), Total Hole Capacity = {totalHoleCapacity} (Expected: 5)");

        // 4. Test BoardManager runtime instantiation and reconstruction
        GameObject boardObj = new GameObject("TestBoardManager");
        var boardManager = boardObj.AddComponent<BoardManager>();

        try
        {
            boardManager.SetupBoard(level, true);

            var spawnedHoles = boardObj.GetComponentsInChildren<HoleController>(true);
            var spawnedQueues = boardObj.GetComponentsInChildren<AntQueueController>(true);

            Debug.Log($"[Test] BoardManager Spawned GameObjects:");
            Debug.Log($"  HoleController instances: {spawnedHoles.Length} (Expected: 5)");
            Debug.Log($"  AntQueueController instances: {spawnedQueues.Length} (Expected: 5)");

            int spawnedAntUnits = 0;
            foreach (var sq in spawnedQueues)
            {
                var units = sq.Units;
                spawnedAntUnits += units.Count;
                Debug.Log($"    Queue '{sq.QueueId}' ({sq.Color}): {units.Count} ant units spawned. Head at {sq.CurrentBodyCoordinates[0]}");
            }
            Debug.Log($"  Total AntUnit instances: {spawnedAntUnits} (Expected: 30)");

            // Verify grid occupancy
            Debug.Log($"  HolesMap registered entries: {boardManager.HolesMap.Count} (Expected: 10 occupied cells across 5 stadium trays)");

            if (spawnedHoles.Length == 5 && spawnedQueues.Length == 5 && spawnedAntUnits == 30 && boardManager.HolesMap.Count == 10)
            {
                Debug.Log("==================================================");
                Debug.Log("SUCCESS! REFERENCE LEVEL 001 RECONSTRUCTION VERIFIED 100%!");
                Debug.Log("==================================================");
            }
            else
            {
                Debug.LogError("[Test] FAILED: Spawned count mismatch!");
            }
        }
        finally
        {
            Object.DestroyImmediate(boardObj);
        }
    }
}
#endif
