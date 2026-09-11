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

                int levelOverrides = 0;
                int[] expectedOverrides = { 5, 0, 1, 4, 0 };

                foreach (var sh in spawnedHoles)
                {
                    if (sh.RemainingCount <= 0)
                    {
                        Debug.LogError($"[Validation] Level {levelNumber}: Hole {sh.name} has invalid initial remainingCount: {sh.RemainingCount}!");
                        allPassed = false;
                    }

                    if (sh.InitialQueueCount == 1)
                    {
                        levelOverrides++;
                        if (!sh.SingleQueueVisualOverrideApplied)
                        {
                            Debug.LogError($"[Validation] Level {levelNumber}: Hole {sh.name} (Color={sh.Color}) has InitialQueueCount=1 but override was not applied!");
                            allPassed = false;
                        }
                    }
                    else
                    {
                        if (sh.SingleQueueVisualOverrideApplied)
                        {
                            Debug.LogError($"[Validation] Level {levelNumber}: Hole {sh.name} (Color={sh.Color}) has InitialQueueCount={sh.InitialQueueCount} but override was unexpectedly applied!");
                            allPassed = false;
                        }
                    }
                }

                if (levelOverrides != expectedOverrides[i])
                {
                    Debug.LogError($"[Validation] Level {levelNumber}: Override count mismatch! Got {levelOverrides}, expected {expectedOverrides[i]}");
                    allPassed = false;
                }
                else
                {
                    Debug.Log($"[Validation] Level {levelNumber}: Single-queue visual overrides verified ({levelOverrides} applied).");
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

    [MenuItem("Tools/Slither Ants Jam/Tests/Validate All 45 Levels in Database")]
    public static void ValidateAll45Levels()
    {
        var db = AssetDatabase.LoadAssetAtPath<LevelDatabase>("Assets/Data/Levels/LevelDatabase.asset");
        if (db == null)
        {
            Debug.LogError("[LevelDatabaseVerification] FAILED: LevelDatabase is null at Assets/Data/Levels/LevelDatabase.asset!");
            return;
        }
        int count = db.LevelCount;
        Debug.Log($"[LevelDatabaseVerification] Successfully loaded LevelDatabase at Assets/Data/Levels/LevelDatabase.asset: Total Levels = {count}");
        if (count != 45)
        {
            Debug.LogError($"[LevelDatabaseVerification] FAILED: Expected 45 levels, found {count}!");
            return;
        }
        for (int i = 0; i < count; i++)
        {
            var lvl = db.GetLevel(i + 1);
            if (lvl == null)
            {
                Debug.LogError($"[LevelDatabaseVerification] FAILED: Level at 1-based index {i + 1} is null!");
                return;
            }
        }
        Debug.Log($"[LevelDatabaseVerification] PASSED: All 45 levels are non-null and valid in LevelDatabase.asset (Level 1: '{db.GetLevel(1).name}', Level 45: '{db.GetLevel(45).name}')");
    }

    [MenuItem("Tools/Slither Ants Jam/Tests/Validate Color Consistency Levels 1 to 5")]
    public static void ValidateColorConsistency()
    {
        Debug.Log("==================================================");
        Debug.Log("STARTING COLOR CONSISTENCY VALIDATION (LEVELS 1-5)");
        Debug.Log("==================================================");

        string dbPath = "Assets/Data/Levels/LevelDatabase.asset";
        LevelDatabase database = AssetDatabase.LoadAssetAtPath<LevelDatabase>(dbPath);
        if (database == null)
        {
            Debug.LogError($"[ColorConsistency] FAILED: Could not load LevelDatabase at '{dbPath}'!");
            return;
        }

        HashSet<int> loggedColorIds = new HashSet<int>();
        bool allMatch = true;

        for (int l = 0; l < 5; l++)
        {
            int levelNum = l + 1;
            Level level = database.GetLevel(levelNum);
            if (level == null) continue;

            GameObject boardObj = new GameObject($"TestBoard_ColorDiag_{levelNum}");
            var bm = boardObj.AddComponent<BoardManager>();

            try
            {
                bm.SetupBoard(level, true);

                var spawnedHoles = boardObj.GetComponentsInChildren<HoleController>(true);
                var spawnedQueues = boardObj.GetComponentsInChildren<AntQueueController>(true);

                Dictionary<int, HoleController> holeByColorId = new Dictionary<int, HoleController>();
                foreach (var h in spawnedHoles)
                {
                    if (!holeByColorId.ContainsKey(h.ColorID))
                    {
                        holeByColorId[h.ColorID] = h;
                    }
                }

                foreach (var q in spawnedQueues)
                {
                    int rawId = q.ColorID;
                    if (loggedColorIds.Add(rawId))
                    {
                        AntColor resolvedColor = ReferenceColorMapper.ToAntColor(rawId);
                        Material antMat = AntColorPalette.GetMaterial(resolvedColor);
                        Material rimMat = AntColorPalette.GetHoleMaterial(resolvedColor);

                        Color antBaseColor = antMat.HasProperty("_BaseColor") ? antMat.GetColor("_BaseColor") : antMat.color;

                        HoleController hole = holeByColorId.ContainsKey(rawId) ? holeByColorId[rawId] : null;
                        Color rimBaseColor = Color.clear;
                        string rimMatName = rimMat.name;

                        if (hole != null)
                        {
                            Transform slotRoot = hole.transform.Find("HoleSlotRoot");
                            Transform rimTr = slotRoot != null ? slotRoot.Find("SlotRim") : null;
                            var rimRen = rimTr != null ? rimTr.GetComponent<MeshRenderer>() : null;
                            if (rimRen != null)
                            {
                                Material actualMat = rimRen.sharedMaterial != null ? rimRen.sharedMaterial : rimRen.material;
                                rimMatName = actualMat.name;
                                rimBaseColor = actualMat.HasProperty("_BaseColor") ? actualMat.GetColor("_BaseColor") : actualMat.color;
                            }
                        }

                        bool rgbMatch = Mathf.Approximately(antBaseColor.r, rimBaseColor.r) &&
                                        Mathf.Approximately(antBaseColor.g, rimBaseColor.g) &&
                                        Mathf.Approximately(antBaseColor.b, rimBaseColor.b);

                        if (!rgbMatch) allMatch = false;

                        Debug.Log($"[ColorConsistency]\nRawID={rawId}\nResolved={resolvedColor}\nQueueMaterial={antMat.name}\nHolderBaseColor=RGBA({rimBaseColor.r:F3}, {rimBaseColor.g:F3}, {rimBaseColor.b:F3}, {rimBaseColor.a:F3})\nHolderRimMaterial={rimMatName}\nMATCH={rgbMatch}");
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(boardObj);
            }
        }

        Debug.Log("==================================================");
        Debug.Log(allMatch ? "COLOR CONSISTENCY VALIDATION PASSED!" : "COLOR CONSISTENCY VALIDATION HAD MISMATCHES!");
        Debug.Log("==================================================");
    }

    [MenuItem("Tools/Slither Ants Jam/Tests/Validate Holder Removal Level 1")]
    public static void ValidateHolderRemovalLevel1()
    {
        Debug.Log("==================================================");
        Debug.Log("VALIDATING HOLDER REMOVAL & FOOTPRINT RELEASE (LEVEL 1)");
        Debug.Log("==================================================");

        string dbPath = "Assets/Data/Levels/LevelDatabase.asset";
        LevelDatabase db = AssetDatabase.LoadAssetAtPath<LevelDatabase>(dbPath);
        if (db == null)
        {
            Debug.LogError($"[Validation] Failed to load LevelDatabase at '{dbPath}'");
            return;
        }

        Level level1 = db.GetLevel(1);
        if (level1 == null)
        {
            Debug.LogError("[Validation] Failed to load Level 1");
            return;
        }

        GameObject boardObj = new GameObject("ValidationBoard_Level1");
        var bm = boardObj.AddComponent<BoardManager>();

        Dictionary<string, bool> results = new Dictionary<string, bool>();
        string[] colorNames = { "Yellow", "Green", "SkyBlue", "Brown", "Red" };
        int[] rawIds = { 2, 7, 0, 8, 10 };

        try
        {
            bm.SetupBoard(level1, true);

            var spawnedHoles = boardObj.GetComponentsInChildren<HoleController>(true);
            var spawnedQueues = boardObj.GetComponentsInChildren<AntQueueController>(true);

            Dictionary<int, HoleController> holesByColor = new Dictionary<int, HoleController>();
            Dictionary<int, AntQueueController> queuesByColor = new Dictionary<int, AntQueueController>();

            foreach (var h in spawnedHoles) holesByColor[h.ColorID] = h;
            foreach (var q in spawnedQueues) queuesByColor[q.ColorID] = q;

            for (int i = 0; i < rawIds.Length; i++)
            {
                int rawId = rawIds[i];
                string name = colorNames[i];
                bool pass = true;

                if (!holesByColor.ContainsKey(rawId) || !queuesByColor.ContainsKey(rawId))
                {
                    Debug.LogError($"[Validation] Missing holder or queue for {name} (RawID {rawId})");
                    results[name] = false;
                    continue;
                }

                HoleController hole = holesByColor[rawId];
                AntQueueController queue = queuesByColor[rawId];
                var footprint = new List<Vector2Int>(hole.OccupiedCells);

                // Check before
                if (hole.RemainingCount != 1 || hole.IsCompleted || footprint.Count != 2)
                {
                    Debug.LogError($"[Validation] {name}: Invalid BEFORE state! Count={hole.RemainingCount}, Completed={hole.IsCompleted}, FootprintCount={footprint.Count}");
                    pass = false;
                }

                foreach (var cell in footprint)
                {
                    if (!bm.HolesMap.ContainsKey(cell) || bm.GetHoleAt(cell) != hole || bm.IsCellFree(cell))
                    {
                        Debug.LogError($"[Validation] {name}: Cell ({cell.x},{cell.y}) is not properly blocked before completion!");
                        pass = false;
                    }
                }

                // Complete queue
                bm.OnQueueCompleted(queue);

                // Check after
                if (hole.RemainingCount != 0 || !hole.IsCompleted)
                {
                    Debug.LogError($"[Validation] {name}: Invalid AFTER state! Count={hole.RemainingCount}, Completed={hole.IsCompleted}");
                    pass = false;
                }

                foreach (var cell in footprint)
                {
                    if (bm.HolesMap.ContainsKey(cell))
                    {
                        Debug.LogError($"[Validation] {name}: Cell ({cell.x},{cell.y}) still present in HolesMap!");
                        pass = false;
                    }
                    if (bm.GetHoleAt(cell) != null)
                    {
                        Debug.LogError($"[Validation] {name}: GetHoleAt(({cell.x},{cell.y})) did not return null!");
                        pass = false;
                    }
                    if (!bm.IsCellFree(cell))
                    {
                        Debug.LogError($"[Validation] {name}: Cell ({cell.x},{cell.y}) is not marked free!");
                        pass = false;
                    }
                }

                results[name] = pass;
                Debug.Log($"Holder {name}: {(pass ? "PASS" : "FAIL")}");
            }

            bool winPass = (bm.ActiveQueues.Count == 0 && bm.ActiveHoles.Count == 0 && bm.CompletedHoleCount == 5);
            Debug.Log($"WIN CONDITION: {(winPass ? "PASS" : "FAIL")}");

            Debug.Log("==================================================");
            Debug.Log("LEVEL 1 SUMMARY:");
            foreach (var kvp in results)
            {
                Debug.Log($"  {kvp.Key} = {(kvp.Value ? "PASS" : "FAIL")}");
            }
            Debug.Log($"  Win Condition = {(winPass ? "PASS" : "FAIL")}");
            Debug.Log("==================================================");
        }
        finally
        {
            Object.DestroyImmediate(boardObj);
        }
    }
}
#endif
