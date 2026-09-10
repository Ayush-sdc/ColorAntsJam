#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class TestEditPlayParity
{
    [MenuItem("Tools/Slither Ants Jam/Tests/Run Parity Test Manually")]
    public static void RunParityTest()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        void Log(string msg) { Debug.Log(msg); sb.AppendLine(msg); }
        void LogError(string msg) { Debug.LogError(msg); sb.AppendLine("[ERROR] " + msg); }

        Log("==================================================");
        Log("RUNNING TEST: HANDMADE (1-3) & REFERENCE LEVELS (001, 002, 005, 006, 007)");
        Log("==================================================");

        string[] levelPaths = new string[]
        {
            "Assets/Data/Levels/Level_01.asset",
            "Assets/Data/Levels/Level_02.asset",
            "Assets/Data/Levels/Level_03.asset",
            "Assets/Data/ReferenceLevels/Level_001.asset",
            "Assets/Data/ReferenceLevels/Level_002.asset",
            "Assets/Data/ReferenceLevels/Level_005.asset",
            "Assets/Data/ReferenceLevels/Level_006.asset",
            "Assets/Data/ReferenceLevels/Level_007.asset"
        };

        foreach (var levelPath in levelPaths)
        {
            Level level = AssetDatabase.LoadAssetAtPath<Level>(levelPath);
            if (level == null)
            {
                Debug.LogError($"[ParityTest] FAILED: Could not load Level asset at '{levelPath}'!");
                continue;
            }

            GameObject boardObj = new GameObject($"TestBoard_{level.name}");
            BoardManager boardMgr = boardObj.AddComponent<BoardManager>();

            try
            {
                boardMgr.SetupBoard(level, true);

                var holes = boardObj.GetComponentsInChildren<HoleController>(true);
                var queues = boardObj.GetComponentsInChildren<AntQueueController>(true);
                var antUnits = boardObj.GetComponentsInChildren<AntUnit>(true);

                int expectedHoleCount = (level.Holes != null && level.Holes.Count > 0) 
                    ? level.Holes.Count 
                    : ReferenceLevelRuntimeAdapter.ExtractHoles(level).Count;

                int expectedQueueCount = 0;
                int expectedAntCount = 0;

                if (ReferenceLevelRuntimeAdapter.IsReferenceLevel(level))
                {
                    var refQueues = ReferenceLevelRuntimeAdapter.ExtractPlayableQueues(level, ReferenceLevelRuntimeAdapter.ExtractHoles(level));
                    expectedQueueCount = refQueues.Count;
                    foreach (var rq in refQueues) expectedAntCount += rq.BodyCoordinates.Count;
                }
                else
                {
                    expectedQueueCount = level.AntQueues?.Count ?? 0;
                    if (level.AntQueues != null)
                    {
                        foreach (var q in level.AntQueues) expectedAntCount += q.AntCount;
                    }
                }

                // Disabled cells validation
                HashSet<Vector2Int> disabledSet = level.GetCombinedDisabledCells();

                Debug.Log($"[ParityTest] Level '{level.name}' (Grid={boardMgr.GridWidth}x{boardMgr.GridHeight}):");
                Debug.Log($"  Disabled Cells (Combined Unique): {disabledSet.Count}");
                Debug.Log($"  Holes: {holes.Length} (Expected: {expectedHoleCount})");
                Debug.Log($"  Queues: {queues.Length} (Expected: {expectedQueueCount})");
                Debug.Log($"  Ant Units: {antUnits.Length} (Expected: {expectedAntCount})");
                Debug.Log($"  HolesMap entries: {boardMgr.HolesMap.Count}");

                // Overlap checks
                int antOverlaps = 0;
                foreach (var q in queues)
                {
                    foreach (var c in q.CurrentBodyCoordinates)
                    {
                        if (disabledSet.Contains(c)) antOverlaps++;
                    }
                }

                int holeEntranceOverlaps = 0;
                int holeFootprintOverlaps = 0;
                foreach (var h in holes)
                {
                    // A. Check Playable Entrance Coordinate (where ants enter and pathfind into)
                    if (disabledSet.Contains(h.GridCoordinate))
                    {
                        holeEntranceOverlaps++;
                    }

                    // B. Check Non-Playable Decorative Footprint Coordinates (outer tray / badge backing)
                    foreach (var c in h.OccupiedCells)
                    {
                        if (c != h.GridCoordinate && disabledSet.Contains(c))
                        {
                            holeFootprintOverlaps++;
                        }
                    }
                }

                Debug.Log($"  Overlaps: Ant-Disabled={antOverlaps}, HoleEntrance-Disabled={holeEntranceOverlaps}, HoleFootprint-Disabled={holeFootprintOverlaps}");
                if (antOverlaps > 0) Debug.LogError($"  [Conflict] {antOverlaps} ant coordinates overlap with disabled cells!");
                if (holeEntranceOverlaps > 0) Debug.LogError($"  [Conflict] {holeEntranceOverlaps} playable hole entrance coordinates overlap with disabled cells!");
                if (holeFootprintOverlaps > 0) Debug.Log($"  [Info] {holeFootprintOverlaps} outer hole footprint cells extend into non-playable disabled border area (valid reference design).");

                // Validate BFS Pathfinding on initial state & ensure NO path touches disabled cells
                int moveableQueues = 0;
                int disabledTouches = 0;

                foreach (var q in queues)
                {
                    var path = AntPathfinder.FindPath(boardMgr, q);
                    if (path != null && path.Count >= 2)
                    {
                        moveableQueues++;
                        foreach (var step in path)
                        {
                            if (disabledSet.Contains(step)) disabledTouches++;
                        }
                    }
                }

                Debug.Log($"  [BFS Path] Moveable queues: {moveableQueues}/{queues.Length}, Paths touching disabled: {disabledTouches}");
                if (disabledTouches > 0) LogError($"  [BFS Error] Path traversed disabled cells!");

                // Reference Level 001 Sequential Simulation Test
                if (level.name == "Level_001")
                {
                    RunReference001SequentialSolveSimulation(boardMgr, Log, LogError);
                }

                // Reference Level 007 Connected Groups Test
                if (level.name == "Level_007")
                {
                    RunLevel007ConnectedGroupsTest(boardMgr, Log, LogError);
                }

                if (holes.Length == expectedHoleCount && queues.Length == expectedQueueCount && antUnits.Length == expectedAntCount && antOverlaps == 0 && holeEntranceOverlaps == 0 && disabledTouches == 0)
                {
                    Log($"[ParityTest] SUCCESS: '{level.name}' Verified 100% without errors!");
                }
                else
                {
                    LogError($"[ParityTest] FAILED: '{level.name}' validation errors detected!");
                }
            }
            finally
            {
                if (boardMgr != null)
                {
                    boardMgr.ClearBoard();
                }
                if (boardObj != null)
                {
                    Object.DestroyImmediate(boardObj);
                }
            }
        }

        Log("==================================================");
        Log("ALL PARITY, PATHFINDING & DISABLED-CELL TESTS COMPLETED");
        Log("==================================================");

        try
        {
            System.IO.File.WriteAllText("test_parity_output.txt", sb.ToString());
        }
        catch { }
    }

    [MenuItem("Tools/Slither Ants Jam/Tests/Test Level 007 Connected Groups")]
    public static void RunLevel007TestDirect()
    {
        string levelPath = "Assets/Data/ReferenceLevels/Level_007.asset";
        Level level = AssetDatabase.LoadAssetAtPath<Level>(levelPath);
        if (level == null)
        {
            Debug.LogError($"[TestLevel007] FAILED: Could not load Level asset at '{levelPath}'!");
            return;
        }

        GameObject boardObj = new GameObject("TestBoard_Level_007");
        BoardManager boardMgr = boardObj.AddComponent<BoardManager>();

        try
        {
            boardMgr.SetupBoard(level, true);
            RunLevel007ConnectedGroupsTest(boardMgr);
        }
        finally
        {
            if (boardMgr != null)
            {
                boardMgr.ClearBoard();
            }
            if (boardObj != null)
            {
                Object.DestroyImmediate(boardObj);
            }
        }
    }

    private static void RunLevel007ConnectedGroupsTest(BoardManager boardMgr, System.Action<string> log = null, System.Action<string> logError = null)
    {
        if (log == null) log = Debug.Log;
        if (logError == null) logError = Debug.LogError;

        log("==================================================");
        log("--- STARTING LEVEL 007 CONNECTED GROUPS VALIDATION & SOLVE ---");
        log("==================================================");

        var activeQueues = boardMgr.ActiveQueues;
        int initialQueueCount = activeQueues.Count;

        // Group active queues by ConnectedIndex
        var connectedGroups = new Dictionary<int, List<AntQueueController>>();
        var standaloneQueues = new List<AntQueueController>();

        foreach (var q in activeQueues)
        {
            if (q.IsConnected && q.ConnectedIndex >= 0)
            {
                if (!connectedGroups.ContainsKey(q.ConnectedIndex))
                {
                    connectedGroups[q.ConnectedIndex] = new List<AntQueueController>();
                }
                connectedGroups[q.ConnectedIndex].Add(q);
            }
            else
            {
                standaloneQueues.Add(q);
            }
        }

        log($"[Level007 Test] Found {connectedGroups.Count} Connected Groups and {standaloneQueues.Count} Standalone Queues across {initialQueueCount} Total Queues.");

        // Validate GetConnectedGroup consistency
        foreach (var kvp in connectedGroups)
        {
            int cIdx = kvp.Key;
            var members = kvp.Value;
            foreach (var m in members)
            {
                var groupFromManager = boardMgr.GetConnectedGroup(m);
                if (groupFromManager.Count != members.Count)
                {
                    logError($"[Level007 Test] Mismatch in GetConnectedGroup for Queue '{m.QueueId}' (ConnectedIndex={cIdx}): expected {members.Count} members, got {groupFromManager.Count}!");
                }
            }
        }

        // Test 1: Validate Atomic Group Blocked Rejection on initially blocked groups (e.g. Group 5 or 9)
        if (connectedGroups.TryGetValue(5, out var group5Members))
        {
            var blockedMember = group5Members[0];
            bool canMove = boardMgr.IsPathClearForQueue(blockedMember);
            log($"[Level007 Test] Initial state check for Group 5 (ConnectedIndex=5, blocked): IsPathClearForQueue = {canMove} (Expected: False)");
            if (canMove)
            {
                logError($"[Level007 Test] FAILED: Group 5 should be blocked initially!");
            }
        }

        // Test 2: Sequential Solve Simulation
        log("--- Starting Full Sequential Solve Simulation for Level 007 ---");

        int rounds = 0;
        int maxRounds = 20;

        while (boardMgr.ActiveQueues.Count > 0 && rounds < maxRounds)
        {
            rounds++;

            // Find an unblocked queue (or group) to move
            AntQueueController queueToTrigger = null;
            List<AntQueueController> groupToMove = null;
            Dictionary<AntQueueController, List<Vector2Int>> memberPaths = new Dictionary<AntQueueController, List<Vector2Int>>();

            foreach (var q in boardMgr.ActiveQueues)
            {
                var group = boardMgr.GetConnectedGroup(q);
                bool groupAllValid = true;
                var currentPaths = new Dictionary<AntQueueController, List<Vector2Int>>();

                foreach (var member in group)
                {
                    var path = AntPathfinder.FindPath(boardMgr, member);
                    if (path == null || path.Count < 2)
                    {
                        groupAllValid = false;
                        break;
                    }
                    currentPaths[member] = path;
                }

                if (groupAllValid)
                {
                    queueToTrigger = q;
                    groupToMove = group;
                    memberPaths = currentPaths;
                    break;
                }
            }

            if (queueToTrigger == null)
            {
                logError($"[Level007 Sim] Deadlock at Round {rounds}! Remaining Queues: {boardMgr.ActiveQueues.Count}");
                return;
            }

            string groupInfo = queueToTrigger.IsConnected 
                ? $"Connected Group (Index={queueToTrigger.ConnectedIndex}, {groupToMove.Count} queues)" 
                : $"Standalone Queue '{queueToTrigger.QueueId}'";

            log($"[Level007 Sim] Round {rounds}: Moving {groupInfo} simultaneously:");

            // Simulate atomic lockstep movement into holes for each member
            foreach (var member in groupToMove)
            {
                var path = memberPaths[member];
                Vector2Int targetEntrance = path[path.Count - 1];
                HoleController targetHole = boardMgr.GetHoleAt(targetEntrance);

                log($"   -> Member '{member.QueueId}' (ColorID={member.ColorID}, Ants={member.AntCount}) -> Hole Entrance at {targetEntrance}");

                // Swallow ants
                int count = member.AntCount;
                for (int a = 0; a < count; a++)
                {
                    if (targetHole != null)
                    {
                        targetHole.OnAntSwallowed();
                    }
                }
            }

            // Unregister all members of the group
            var membersList = new List<AntQueueController>(groupToMove);
            foreach (var member in membersList)
            {
                boardMgr.OnQueueCompleted(member);
            }
        }

        // Verify completion state
        if (boardMgr.ActiveQueues.Count == 0)
        {
            log($"[Level007 Sim] SUCCESS! All 28 Level 007 Queues solved in {rounds} rounds. Win condition achieved!");

            // Verify all holes are full / remaining count is 0
            foreach (var kvp in boardMgr.HolesMap)
            {
                var hole = kvp.Value;
                if (hole != null && hole.RemainingCount > 0)
                {
                    logError($"[Level007 Sim] FAILED: Hole ColorID={hole.ColorID} still has {hole.RemainingCount} remaining capacity!");
                }
            }
        }
        else
        {
            logError($"[Level007 Sim] FAILED: Unsolved queues remaining: {boardMgr.ActiveQueues.Count}");
        }
    }

    private static void RunReference001SequentialSolveSimulation(BoardManager boardMgr, System.Action<string> log = null, System.Action<string> logError = null)
    {
        if (log == null) log = Debug.Log;
        if (logError == null) logError = Debug.LogError;

        log("--- Starting Reference Level 001 Sequential Solve Simulation ---");

        int clearedCount = 0;
        int maxRounds = 10;
        int round = 0;

        while (boardMgr.ActiveQueues.Count > 0 && round < maxRounds)
        {
            round++;
            AntQueueController queueToMove = null;
            List<Vector2Int> foundPath = null;

            foreach (var q in boardMgr.ActiveQueues)
            {
                var path = AntPathfinder.FindPath(boardMgr, q);
                if (path != null && path.Count >= 2)
                {
                    queueToMove = q;
                    foundPath = path;
                    break;
                }
            }

            if (queueToMove == null)
            {
                logError($"[Ref001 Sim] Deadlock at round {round}! Remaining queues: {boardMgr.ActiveQueues.Count}");
                return;
            }

            Vector2Int destinationHole = foundPath[foundPath.Count - 1];
            HoleController targetHole = boardMgr.GetHoleAt(destinationHole);

            log($"[Ref001 Sim] Round {round}: Clearing Queue '{queueToMove.QueueId}' (ColorID={queueToMove.ColorID}, Ants={queueToMove.AntCount}) -> Hole at {destinationHole}");

            // Simulate sequential swallow into target hole
            int antCount = queueToMove.AntCount;
            for (int a = 0; a < antCount; a++)
            {
                if (targetHole != null)
                {
                    targetHole.OnAntSwallowed();
                }
            }

            boardMgr.OnQueueCompleted(queueToMove);
            clearedCount++;
        }

        if (boardMgr.ActiveQueues.Count == 0 && clearedCount == 5)
        {
            log($"[Ref001 Sim] SUCCESS! All 5 Reference Level 001 queues cleared sequentially and level completed.");
        }
        else
        {
            logError($"[Ref001 Sim] FAILED: Cleared {clearedCount}/5 queues, remaining: {boardMgr.ActiveQueues.Count}");
        }
    }
}
#endif
