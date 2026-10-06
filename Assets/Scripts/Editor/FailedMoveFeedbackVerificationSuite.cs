#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using DG.Tweening;

public class FailedMoveFeedbackRunner : MonoBehaviour
{
    private void Start()
    {
        StartCoroutine(FailedMoveFeedbackVerificationSuite.RunSuiteRoutine(this.gameObject));
    }
}

public static class FailedMoveFeedbackVerificationSuite
{
    private static GameObject testRoot;
    private static BoardManager board;
    private static GameManager gameManager;
    private static SoundManager soundManager;

    public static IEnumerator RunSuiteRoutine(GameObject runnerGo)
    {
        Debug.Log("=======================================================================");
        Debug.Log("[FAILED MOVE FEEDBACK TEST SUITE] Starting comprehensive test suite in Play Mode...");
        Debug.Log("=======================================================================");

        int passed = 0;
        int failed = 0;

        yield return RunTest("Test 1: Blocker Several Cells Ahead (Approach, Contact, Ouch, 1 Life, Return)", TestBlockerSeveralCellsAhead, (p) => { if (p) passed++; else failed++; });
        yield return RunTest("Test 2: Adjacent Blocker (No Overlap, No Teleport)", TestAdjacentBlocker, (p) => { if (p) passed++; else failed++; });
        yield return RunTest("Test 3: Bent Queue (Followers Retrace Around Curve Backwards)", TestBentQueueRetrace, (p) => { if (p) passed++; else failed++; });
        yield return RunTest("Test 4: Rapid Repeated Taps (Only 1 Action, Only 1 Life Lost)", TestRapidRepeatedTaps, (p) => { if (p) passed++; else failed++; });
        yield return RunTest("Test 5: Unrelated Valid Queues Can Still Move Concurrently", TestUnrelatedQueueCanMove, (p) => { if (p) passed++; else failed++; });
        yield return RunTest("Test 6: Moving Queues Never Cause Collision", TestMovingQueuesNeverCollide, (p) => { if (p) passed++; else failed++; });
        yield return RunTest("Test 7: Blocker Starts Moving Before Impact (No False Penalty)", TestBlockerMovesBeforeImpact, (p) => { if (p) passed++; else failed++; });
        yield return RunTest("Test 8: Linked-Group Failure (Atomic Earliest Collision, 1 Ouch, 1 Life)", TestLinkedGroupFailure, (p) => { if (p) passed++; else failed++; });
        yield return RunTest("Test 9: Stacked Lower Layer Remains Hidden On Failed Move", TestStackedLayerNotRevealed, (p) => { if (p) passed++; else failed++; });
        yield return RunTest("Test 10: No Hole Capacity or Pipe Uses Consumed", TestNoResourceConsumption, (p) => { if (p) passed++; else failed++; });
        yield return RunTest("Test 11: Last-Heart Failure Lets Return Finish Before Overlay", TestLastHeartFailureDeferred, (p) => { if (p) passed++; else failed++; });
        yield return RunTest("Test 12: Pause Resumes Sequence Correctly", TestPauseAndResume, (p) => { if (p) passed++; else failed++; });

        Debug.Log("=======================================================================");
        Debug.Log($"[FAILED MOVE FEEDBACK TEST SUITE] Finished: {passed} PASSED, {failed} FAILED");
        Debug.Log("=======================================================================");

        Cleanup();
        if (runnerGo != null)
        {
            UnityEngine.Object.Destroy(runnerGo);
        }
    }

    [InitializeOnLoadMethod]
    private static void RegisterPlayModeCallback()
    {
        EditorApplication.playModeStateChanged += (state) =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && EditorPrefs.GetBool("AutoRunFailedMoveSuite", false))
            {
                EditorPrefs.DeleteKey("AutoRunFailedMoveSuite");
                SpawnRunnerInPlayMode();
            }
        };
    }

    [MenuItem("Tools/Slither Ants Jam/Tests/Run Failed Move Feedback Suite")]
    public static void RunSuite()
    {
        if (!EditorApplication.isPlaying)
        {
            EditorPrefs.SetBool("AutoRunFailedMoveSuite", true);
            EditorApplication.isPlaying = true;
            return;
        }

        SpawnRunnerInPlayMode();
    }

    public static void SpawnRunnerInPlayMode()
    {
        var existing = UnityEngine.Object.FindAnyObjectByType<FailedMoveFeedbackRunner>();
        if (existing != null) UnityEngine.Object.Destroy(existing.gameObject);

        var runnerGo = new GameObject("FailedMoveFeedbackRunner");
        UnityEngine.Object.DontDestroyOnLoad(runnerGo);
        runnerGo.AddComponent<FailedMoveFeedbackRunner>();
    }

    private static IEnumerator RunTest(string testName, Func<IEnumerator> testFunc, Action<bool> recordResult)
    {
        Debug.Log($"---> RUNNING: {testName}");
        SetupTestEnvironment();

        bool hasError = false;
        Application.LogCallback onLog = (condition, stackTrace, type) =>
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                hasError = true;
            }
        };
        Application.logMessageReceived += onLog;

        IEnumerator routine = null;
        try
        {
            routine = testFunc();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FAIL] {testName} setup exception: {ex.Message}\n{ex.StackTrace}");
            hasError = true;
        }

        if (routine != null)
        {
            bool hasNext = true;
            while (hasNext)
            {
                object current = null;
                try
                {
                    hasNext = routine.MoveNext();
                    if (hasNext) current = routine.Current;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[FAIL] {testName} step exception: {ex.Message}\n{ex.StackTrace}");
                    hasError = true;
                    break;
                }

                if (hasNext) yield return current;
            }
        }

        Application.logMessageReceived -= onLog;

        if (!hasError)
        {
            Debug.Log($"[SUITE PASS] {testName}");
            recordResult(true);
        }
        else
        {
            Debug.LogError($"[SUITE FAIL] {testName}");
            recordResult(false);
        }

        Cleanup();
    }

    private static void SetupTestEnvironment()
    {
        Cleanup();

        testRoot = new GameObject("TestRoot_FailedMoveFeedback");

        var gmGo = new GameObject("Test_GameManager");
        gmGo.transform.SetParent(testRoot.transform);
        gameManager = gmGo.AddComponent<GameManager>();
        gameManager.SetTestingState(GameState.Playing, 3);

        var smGo = new GameObject("Test_SoundManager");
        smGo.transform.SetParent(testRoot.transform);
        soundManager = smGo.AddComponent<SoundManager>();

        var bmGo = new GameObject("Test_BoardManager");
        bmGo.transform.SetParent(testRoot.transform);
        board = bmGo.AddComponent<BoardManager>();
        board.SetGridDimensions(20, 20);
        BoardManager.SetTestInstance(board);
    }

    private static void Cleanup()
    {
        if (testRoot != null)
        {
            UnityEngine.Object.DestroyImmediate(testRoot);
            testRoot = null;
        }
        board = null;
        gameManager = null;
        soundManager = null;
    }

    private static AntQueueController CreateTestQueue(string id, int color, List<Vector2Int> coords, bool connected = false, int connIdx = -1)
    {
        var qGo = new GameObject($"Queue_{id}");
        qGo.transform.SetParent(testRoot.transform);
        var q = qGo.AddComponent<AntQueueController>();
        var queueData = new AntQueueData(
            id,
            color,
            0,
            coords,
            connected,
            connIdx,
            false,
            0,
            0,
            false,
            0
        );
        q.Initialize(queueData);
        if (coords != null && coords.Count >= 2)
        {
            q.ForwardDirection = coords[0] - coords[1];
        }
        else
        {
            q.ForwardDirection = Vector2Int.up;
        }
        board.RegisterActiveQueue(q);
        return q;
    }

    private static HoleController CreateTestHole(int color, Vector2Int cell, int capacity)
    {
        var hGo = new GameObject($"Hole_{color}");
        hGo.transform.SetParent(testRoot.transform);
        hGo.transform.position = board.GridToWorldPosition(cell);
        var h = hGo.AddComponent<HoleController>();
        h.Initialize(color, cell, capacity);
        board.RegisterActiveHole(h);
        return h;
    }

    // -----------------------------------------------------------------------------------------------------------------
    // TEST 1: Blocker Several Cells Ahead
    // -----------------------------------------------------------------------------------------------------------------
    private static IEnumerator TestBlockerSeveralCellsAhead()
    {
        // Destination hole at (5, 12) Color 1
        var hole = CreateTestHole(1, new Vector2Int(5, 12), 5);

        // Queue A at (5, 5)-(5, 4), forward is UP (towards (5, 6))
        var qA = CreateTestQueue("QueueA", 1, new List<Vector2Int> { new Vector2Int(5, 5), new Vector2Int(5, 4) });

        // Blocker Queue B stationary at (5, 9)-(5, 8), Color 2
        var qB = CreateTestQueue("QueueB", 2, new List<Vector2Int> { new Vector2Int(5, 9), new Vector2Int(5, 8) });

        int initialLives = gameManager.PlayerLives;
        Vector3 startHeadPos = qA.Units[0].transform.position;

        // Validation test: must return structured info with blocker queue
        var validation = board.ValidateQueueMove(qA);
        if (!validation.IsBlockedByStationaryQueue || validation.BlockerQueue != qB)
        {
            Debug.LogError($"[TEST 1 FAIL] Expected validation IsBlockedByStationaryQueue with blocker QueueB, got {validation.FailureReason}");
            yield break;
        }

        // Tap queue A
        bool started = qA.TryStartMove();
        if (started)
        {
            Debug.LogError("[TEST 1 FAIL] TryStartMove should return false for blocked rescue attempt");
            yield break;
        }

        // Immediately after tap: must NOT lose life immediately!
        if (gameManager.PlayerLives != initialLives)
        {
            Debug.LogError($"[TEST 1 FAIL] Life was deducted immediately on tap! Expected {initialLives}, got {gameManager.PlayerLives}");
            yield break;
        }

        // Must be in FailedMoveApproach state
        if (qA.MoveState != AntQueueController.QueueMoveState.FailedMoveApproach)
        {
            Debug.LogError($"[TEST 1 FAIL] Expected MoveState FailedMoveApproach, got {qA.MoveState}");
            yield break;
        }

        // Wait until queue A reaches impact
        float timeout = 3.0f;
        while (qA.MoveState == AntQueueController.QueueMoveState.FailedMoveApproach && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        if (qA.MoveState != AntQueueController.QueueMoveState.Impact && qA.MoveState != AntQueueController.QueueMoveState.Returning)
        {
            Debug.LogError($"[TEST 1 FAIL] Queue did not transition to Impact or Returning: {qA.MoveState}");
            yield break;
        }

        // At impact, exactly one life must be lost
        if (gameManager.PlayerLives != initialLives - 1)
        {
            Debug.LogError($"[TEST 1 FAIL] Expected lives {initialLives - 1} at impact, got {gameManager.PlayerLives}");
            yield break;
        }

        // Wait until queue A returns to Idle
        timeout = 4.0f;
        while (qA.MoveState != AntQueueController.QueueMoveState.Idle && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        if (qA.MoveState != AntQueueController.QueueMoveState.Idle)
        {
            Debug.LogError($"[TEST 1 FAIL] Queue did not return to Idle: {qA.MoveState}");
            yield break;
        }

        // Verify head returned to exact starting position
        Vector3 finalHeadPos = qA.Units[0].transform.position;
        float diff = Vector3.Distance(startHeadPos, finalHeadPos);
        if (diff > 0.05f)
        {
            Debug.LogError($"[TEST 1 FAIL] Queue head did not return to starting position! Diff={diff:F4}");
            yield break;
        }

        // Blocker Queue B should remain completely stationary at original position
        if (qB.CurrentBodyCoordinates[0] != new Vector2Int(5, 9))
        {
            Debug.LogError("[TEST 1 FAIL] Blocker queue logical position was altered!");
            yield break;
        }

        Debug.Log("[PASS] Test 1: Blocker Several Cells Ahead handled cleanly (approach, impact, 1 life, exact return).");
    }

    // -----------------------------------------------------------------------------------------------------------------
    // TEST 2: Adjacent Blocker
    // -----------------------------------------------------------------------------------------------------------------
    private static IEnumerator TestAdjacentBlocker()
    {
        CreateTestHole(1, new Vector2Int(5, 12), 5);

        // Queue A head at (5, 5), forward is UP
        var qA = CreateTestQueue("QueueA_Adj", 1, new List<Vector2Int> { new Vector2Int(5, 5), new Vector2Int(5, 4) });

        // Blocker Queue B in immediately adjacent cell (5, 6)
        var qB = CreateTestQueue("QueueB_Adj", 2, new List<Vector2Int> { new Vector2Int(5, 6), new Vector2Int(5, 7) });

        Vector3 startHeadPos = qA.Units[0].transform.position;
        Vector3 blockerPos = qB.Units[0].transform.position;

        qA.TryStartMove();

        // Wait until returning or idle
        float timeout = 3.0f;
        float maxHeadZ = startHeadPos.z;
        while (qA.MoveState != AntQueueController.QueueMoveState.Idle && timeout > 0f)
        {
            maxHeadZ = Mathf.Max(maxHeadZ, qA.Units[0].transform.position.z);
            timeout -= Time.deltaTime;
            yield return null;
        }

        // Ensure head did NOT penetrate blocker (blockerPos.z is 6.0, head must stop before ~5.35)
        if (maxHeadZ > blockerPos.z - 0.5f)
        {
            Debug.LogError($"[TEST 2 FAIL] Ant penetrated blocker mesh! maxHeadZ={maxHeadZ}, blockerZ={blockerPos.z}");
            yield break;
        }

        // Check restored position
        float diff = Vector3.Distance(startHeadPos, qA.Units[0].transform.position);
        if (diff > 0.05f)
        {
            Debug.LogError($"[TEST 2 FAIL] Adjacent queue did not return to exact start: diff={diff:F4}");
            yield break;
        }

        Debug.Log("[PASS] Test 2: Adjacent blocker stopped with visible spacing and restored exactly without teleportation.");
    }

    // -----------------------------------------------------------------------------------------------------------------
    // TEST 3: Bent Queue Retrace
    // -----------------------------------------------------------------------------------------------------------------
    private static IEnumerator TestBentQueueRetrace()
    {
        CreateTestHole(1, new Vector2Int(5, 15), 5);

        // Queue bent in an L: head at (5, 8), neck at (5, 7), body at (4, 7), tail at (3, 7)
        var bentCoords = new List<Vector2Int>
        {
            new Vector2Int(5, 8),
            new Vector2Int(5, 7),
            new Vector2Int(4, 7),
            new Vector2Int(3, 7)
        };
        var qBent = CreateTestQueue("QueueBent", 1, bentCoords);

        // Blocker ahead at (5, 11)
        var qBlock = CreateTestQueue("QueueBlock", 2, new List<Vector2Int> { new Vector2Int(5, 11), new Vector2Int(5, 12) });

        List<Vector3> startUnitPositions = new List<Vector3>();
        foreach (var u in qBent.Units) startUnitPositions.Add(u.transform.position);

        qBent.TryStartMove();

        // While moving forward, tail and followers must follow body curve, not translate rigidly
        yield return new WaitForSeconds(0.2f);
        if (qBent.Units.Count >= 4)
        {
            // Tail should not move laterally; it must follow along the body line (x >= 3.0, y=7)
            Vector3 tailPos = qBent.Units[3].transform.position;
            if (Mathf.Abs(tailPos.z - startUnitPositions[3].z) > 0.5f && Mathf.Abs(tailPos.x - startUnitPositions[3].x) < 0.01f)
            {
                Debug.LogError("[TEST 3 FAIL] Tail did not follow bent route!");
                yield break;
            }
        }

        // Wait for return to complete
        float timeout = 4.0f;
        while (qBent.MoveState != AntQueueController.QueueMoveState.Idle && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        // Verify all 4 units returned to exact initial positions
        for (int i = 0; i < qBent.Units.Count; i++)
        {
            float d = Vector3.Distance(startUnitPositions[i], qBent.Units[i].transform.position);
            if (d > 0.05f)
            {
                Debug.LogError($"[TEST 3 FAIL] Unit {i} did not restore position! diff={d:F4}");
                yield break;
            }
        }

        Debug.Log("[PASS] Test 3: Bent queue followers naturally followed curve forward and retraced backwards to start.");
    }

    // -----------------------------------------------------------------------------------------------------------------
    // TEST 4: Rapid Repeated Taps
    // -----------------------------------------------------------------------------------------------------------------
    private static IEnumerator TestRapidRepeatedTaps()
    {
        CreateTestHole(1, new Vector2Int(5, 12), 5);
        var qA = CreateTestQueue("QueueA_Rapid", 1, new List<Vector2Int> { new Vector2Int(5, 5), new Vector2Int(5, 4) });
        CreateTestQueue("QueueB_Rapid", 2, new List<Vector2Int> { new Vector2Int(5, 8), new Vector2Int(5, 9) });

        int startLives = gameManager.PlayerLives;

        // First tap
        qA.TryStartMove();

        // Rapid subsequent taps while moving
        for (int i = 0; i < 5; i++)
        {
            yield return new WaitForSeconds(0.05f);
            qA.TryStartMove();
        }

        // Wait until return is finished
        float timeout = 4.0f;
        while (qA.MoveState != AntQueueController.QueueMoveState.Idle && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        // Exactly ONE heart lost
        if (gameManager.PlayerLives != startLives - 1)
        {
            Debug.LogError($"[TEST 4 FAIL] Expected lives {startLives - 1}, but got {gameManager.PlayerLives}");
            yield break;
        }

        Debug.Log("[PASS] Test 4: Rapid repeated taps ignored; exactly 1 failed action and 1 life lost.");
    }

    // -----------------------------------------------------------------------------------------------------------------
    // TEST 5: Unrelated Valid Queues Can Move Concurrently
    // -----------------------------------------------------------------------------------------------------------------
    private static IEnumerator TestUnrelatedQueueCanMove()
    {
        CreateTestHole(1, new Vector2Int(2, 12), 5);
        CreateTestHole(2, new Vector2Int(8, 12), 5);

        // Queue 1 (blocked)
        var q1 = CreateTestQueue("Queue1_Blocked", 1, new List<Vector2Int> { new Vector2Int(2, 5), new Vector2Int(2, 4) });
        CreateTestQueue("Queue1_Blocker", 3, new List<Vector2Int> { new Vector2Int(2, 8), new Vector2Int(2, 9) });

        // Queue 2 (unblocked)
        var q2 = CreateTestQueue("Queue2_Valid", 2, new List<Vector2Int> { new Vector2Int(8, 5), new Vector2Int(8, 4) });

        // Launch Queue 1 failed move
        q1.TryStartMove();

        yield return new WaitForSeconds(0.1f);

        // Queue 1 is in failed move approach
        if (q1.MoveState != AntQueueController.QueueMoveState.FailedMoveApproach)
        {
            Debug.LogError($"[TEST 5 FAIL] Queue 1 not in approach state: {q1.MoveState}");
            yield break;
        }

        // Queue 2 should be completely responsive and able to launch!
        bool q2Started = q2.TryStartMove();
        if (!q2Started || !q2.IsMoving)
        {
            Debug.LogError($"[TEST 5 FAIL] Unrelated valid queue 2 could not start moving while queue 1 is in failed move! started={q2Started}, isMoving={q2.IsMoving}");
            yield break;
        }

        Debug.Log("[PASS] Test 5: Unrelated valid queues remain fully responsive and can move concurrently.");
    }

    // -----------------------------------------------------------------------------------------------------------------
    // TEST 6: Moving Queues Never Cause Collision
    // -----------------------------------------------------------------------------------------------------------------
    private static IEnumerator TestMovingQueuesNeverCollide()
    {
        // Both queues share line x=5 moving to Hole at (5, 15)
        CreateTestHole(1, new Vector2Int(5, 15), 5);

        var q1 = CreateTestQueue("Queue1_Cross", 1, new List<Vector2Int> { new Vector2Int(5, 8), new Vector2Int(5, 7) });
        var q2 = CreateTestQueue("Queue2_Cross", 1, new List<Vector2Int> { new Vector2Int(5, 4), new Vector2Int(5, 3) });

        int startLives = gameManager.PlayerLives;

        // Launch Queue 1 first
        bool s1 = q1.TryStartMove();
        yield return new WaitForSeconds(0.15f);

        // Now launch Queue 2 along the same route while Queue 1 is moving
        bool s2 = q2.TryStartMove();

        if (!s1 || !s2)
        {
            Debug.LogError($"[TEST 6 FAIL] Both queues should start moving legally: s1={s1}, s2={s2}");
            yield break;
        }

        // Moving queues must not collide or lose lives
        if (gameManager.PlayerLives != startLives)
        {
            Debug.LogError($"[TEST 6 FAIL] Lives were lost between moving queues! start={startLives}, current={gameManager.PlayerLives}");
            yield break;
        }

        Debug.Log("[PASS] Test 6: Moving queues overlap and share paths without collision or life loss.");
    }

    // -----------------------------------------------------------------------------------------------------------------
    // TEST 7: Blocker Moves Before Impact -> No False Penalty
    // -----------------------------------------------------------------------------------------------------------------
    private static IEnumerator TestBlockerMovesBeforeImpact()
    {
        CreateTestHole(1, new Vector2Int(5, 15), 5);
        CreateTestHole(2, new Vector2Int(8, 15), 5);

        // Queue A at (5, 4), blocked by Queue B at (5, 9)
        var qA = CreateTestQueue("QueueA_Avoid", 1, new List<Vector2Int> { new Vector2Int(5, 4), new Vector2Int(5, 3) });
        var qB = CreateTestQueue("QueueB_Moving", 2, new List<Vector2Int> { new Vector2Int(5, 9), new Vector2Int(5, 8) });

        int startLives = gameManager.PlayerLives;

        // Launch Queue A into failed move approach towards Q B
        qA.TryStartMove();
        yield return new WaitForSeconds(0.1f);

        // While Q A is traveling, Queue B starts moving away!
        qB.Units[0].transform.position += Vector3.right * 10f; // moves away
        qB.StartSmoothMovement(new List<Vector3> { qB.transform.position, qB.transform.position + Vector3.right * 10f }, null, Vector2Int.zero);

        // Wait until Queue A completes its attempt
        float timeout = 4.0f;
        while (qA.MoveState != AntQueueController.QueueMoveState.Idle && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        // NO heart should be lost because blocker moved before contact!
        if (gameManager.PlayerLives != startLives)
        {
            Debug.LogError($"[TEST 7 FAIL] False collision penalty occurred when blocker moved! Expected {startLives}, got {gameManager.PlayerLives}");
            yield break;
        }

        Debug.Log("[PASS] Test 7: Blocker moved before impact -> no false collision penalty, returned safely.");
    }

    // -----------------------------------------------------------------------------------------------------------------
    // TEST 8: Linked-Group Failure
    // -----------------------------------------------------------------------------------------------------------------
    private static IEnumerator TestLinkedGroupFailure()
    {
        CreateTestHole(1, new Vector2Int(5, 15), 5);
        CreateTestHole(1, new Vector2Int(6, 15), 5);

        // Linked group: Q1 at (5, 5), Q2 at (6, 5)
        var q1 = CreateTestQueue("Link1", 1, new List<Vector2Int> { new Vector2Int(5, 5), new Vector2Int(5, 4) }, true, 0);
        var q2 = CreateTestQueue("Link2", 1, new List<Vector2Int> { new Vector2Int(6, 5), new Vector2Int(6, 4) }, true, 0);

        // Only Q1 has a blocker ahead at (5, 8) (distance ~2.28); Q2 has a clear path to hole (distance ~9)
        CreateTestQueue("BlockerForQ1", 2, new List<Vector2Int> { new Vector2Int(5, 8), new Vector2Int(5, 9) });

        int startLives = gameManager.PlayerLives;
        Vector3 startQ1 = q1.Units[0].transform.position;
        Vector3 startQ2 = q2.Units[0].transform.position;

        // Tap either member of the linked pair
        q1.TryStartMove();

        // Both members must enter FailedMoveApproach together
        if (q1.MoveState != AntQueueController.QueueMoveState.FailedMoveApproach || q2.MoveState != AntQueueController.QueueMoveState.FailedMoveApproach)
        {
            Debug.LogError($"[TEST 8 FAIL] Both members should be in FailedMoveApproach: q1={q1.MoveState}, q2={q2.MoveState}");
            yield break;
        }

        // Wait until return finishes
        float timeout = 4.0f;
        while ((q1.MoveState != AntQueueController.QueueMoveState.Idle || q2.MoveState != AntQueueController.QueueMoveState.Idle) && timeout > 0f)
        {
            // Verify heads remain side-by-side (equal Z coordinates throughout motion)
            float diffZ = Mathf.Abs(q1.Units[0].transform.position.z - q2.Units[0].transform.position.z);
            if (diffZ > 0.35f)
            {
                Debug.LogError($"[TEST 8 FAIL] Linked group lost parallel alignment during movement! diffZ={diffZ}");
                yield break;
            }
            timeout -= Time.deltaTime;
            yield return null;
        }

        // Exactly ONE heart lost for the entire group action
        if (gameManager.PlayerLives != startLives - 1)
        {
            Debug.LogError($"[TEST 8 FAIL] Expected 1 heart lost for linked group, got {startLives - gameManager.PlayerLives}");
            yield break;
        }

        // Both returned to exact starting positions
        if (Vector3.Distance(startQ1, q1.Units[0].transform.position) > 0.05f || Vector3.Distance(startQ2, q2.Units[0].transform.position) > 0.05f)
        {
            Debug.LogError("[TEST 8 FAIL] Linked group members did not return to initial coordinates!");
            yield break;
        }

        Debug.Log("[PASS] Test 8: Linked-group failed move animated atomically, maintained alignment, and cost exactly 1 life.");
    }

    // -----------------------------------------------------------------------------------------------------------------
    // TEST 9: Stacked Layer Not Revealed On Failed Move
    // -----------------------------------------------------------------------------------------------------------------
    private static IEnumerator TestStackedLayerNotRevealed()
    {
        CreateTestHole(1, new Vector2Int(5, 12), 5);

        var qUpper = CreateTestQueue("UpperQueue", 1, new List<Vector2Int> { new Vector2Int(5, 5), new Vector2Int(5, 4) });
        var qLower = CreateTestQueue("LowerQueue", 2, new List<Vector2Int> { new Vector2Int(5, 5), new Vector2Int(5, 4) });
        qUpper.SetLowerQueueLayer(qLower);

        // Blocker
        CreateTestQueue("BlockerStacked", 3, new List<Vector2Int> { new Vector2Int(5, 8), new Vector2Int(5, 9) });

        // Lower layer should be dormant
        if (!qLower.IsDormantStacked || qLower.gameObject.activeSelf)
        {
            Debug.LogError("[TEST 9 FAIL] Lower layer should be dormant and inactive initially");
            yield break;
        }

        qUpper.TryStartMove();

        // During and after failed move: lower layer must remain dormant and inactive!
        yield return new WaitForSeconds(0.2f);
        if (!qLower.IsDormantStacked || qLower.gameObject.activeSelf)
        {
            Debug.LogError("[TEST 9 FAIL] Lower layer was revealed during failed move attempt!");
            yield break;
        }

        float timeout = 3.0f;
        while (qUpper.MoveState != AntQueueController.QueueMoveState.Idle && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        if (!qLower.IsDormantStacked || qLower.gameObject.activeSelf)
        {
            Debug.LogError("[TEST 9 FAIL] Lower layer was revealed after failed move returned!");
            yield break;
        }

        Debug.Log("[PASS] Test 9: Stacked lower layer remained safely dormant and unrevealed throughout failed move.");
    }

    // -----------------------------------------------------------------------------------------------------------------
    // TEST 10: No Resource Consumption (Hole / Pipe)
    // -----------------------------------------------------------------------------------------------------------------
    private static IEnumerator TestNoResourceConsumption()
    {
        var hole = CreateTestHole(1, new Vector2Int(5, 12), 3);
        int initialHoleAvailable = hole.AvailableCapacity;

        var pGo = new GameObject("TestPipe");
        pGo.transform.SetParent(testRoot.transform);
        var pipe = pGo.AddComponent<PipeController>();
        pipe.Initialize(new List<Vector2Int> { new Vector2Int(5, 6), new Vector2Int(5, 7), new Vector2Int(5, 8) }, 2);
        board.RegisterActivePipe(pipe);

        int initialPipeUses = pipe.RemainingUses;

        // Blocker after pipe at (5, 10)
        CreateTestQueue("BlockerPipe", 2, new List<Vector2Int> { new Vector2Int(5, 10), new Vector2Int(5, 11) });

        var q = CreateTestQueue("QueuePipeTest", 1, new List<Vector2Int> { new Vector2Int(5, 4), new Vector2Int(5, 3) });

        q.TryStartMove();

        float timeout = 4.0f;
        while (q.MoveState != AntQueueController.QueueMoveState.Idle && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        if (hole.AvailableCapacity != initialHoleAvailable)
        {
            Debug.LogError($"[TEST 10 FAIL] Hole capacity was consumed! Initial={initialHoleAvailable}, Current={hole.AvailableCapacity}");
            yield break;
        }

        if (pipe.RemainingUses != initialPipeUses)
        {
            Debug.LogError($"[TEST 10 FAIL] Pipe uses were consumed! Initial={initialPipeUses}, Current={pipe.RemainingUses}");
            yield break;
        }

        if (pipe.IsPipeInUse)
        {
            Debug.LogError("[TEST 10 FAIL] Pipe is still marked in use after queue returned!");
            yield break;
        }

        Debug.Log("[PASS] Test 10: Hole capacity and pipe uses were NOT consumed by failed attempt; pipe released.");
    }

    // -----------------------------------------------------------------------------------------------------------------
    // TEST 11: Last-Heart Failure Deferred Until Return Completes
    // -----------------------------------------------------------------------------------------------------------------
    private static IEnumerator TestLastHeartFailureDeferred()
    {
        CreateTestHole(1, new Vector2Int(5, 12), 5);
        var qA = CreateTestQueue("QueueA_LastLife", 1, new List<Vector2Int> { new Vector2Int(5, 5), new Vector2Int(5, 4) });
        CreateTestQueue("QueueB_LastLife", 2, new List<Vector2Int> { new Vector2Int(5, 8), new Vector2Int(5, 9) });

        // Deduct lives so exactly 1 remains
        while (gameManager.PlayerLives > 1)
        {
            gameManager.LoseLife(InvalidMoveReason.NoValidPath, deferGameOver: false);
        }

        if (gameManager.PlayerLives != 1)
        {
            Debug.LogError($"[TEST 11 FAIL] Failed to prepare 1 life state, got {gameManager.PlayerLives}");
            yield break;
        }

        // Tap queue A -> impact will remove the last heart
        qA.TryStartMove();

        // Wait until impact
        while (qA.MoveState == AntQueueController.QueueMoveState.FailedMoveApproach)
        {
            yield return null;
        }

        // At impact: lives = 0, but GameState must NOT be LevelLost yet!
        if (gameManager.PlayerLives != 0)
        {
            Debug.LogError($"[TEST 11 FAIL] Expected lives=0 at impact, got {gameManager.PlayerLives}");
            yield break;
        }

        if (gameManager.CurrentState == GameState.LevelLost)
        {
            Debug.LogError("[TEST 11 FAIL] Game over triggered prematurely during impact before return finished!");
            yield break;
        }

        // While returning, game over should still be deferred
        yield return new WaitForSeconds(0.1f);
        if (qA.MoveState == AntQueueController.QueueMoveState.Returning && gameManager.CurrentState == GameState.LevelLost)
        {
            Debug.LogError("[TEST 11 FAIL] Game over triggered prematurely while queue was returning!");
            yield break;
        }

        // Wait until return is completely finished
        float timeout = 4.0f;
        while (qA.MoveState != AntQueueController.QueueMoveState.Idle && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        // Now that return has finished, GameState must be LevelLost!
        if (gameManager.CurrentState != GameState.LevelLost)
        {
            Debug.LogError($"[TEST 11 FAIL] Expected LevelLost after return finished, got {gameManager.CurrentState}");
            yield break;
        }

        Debug.Log("[PASS] Test 11: Last-heart failure cleanly deferred game over until return fully finished.");
    }

    // -----------------------------------------------------------------------------------------------------------------
    // TEST 12: Pause and Resume
    // -----------------------------------------------------------------------------------------------------------------
    private static IEnumerator TestPauseAndResume()
    {
        CreateTestHole(1, new Vector2Int(5, 12), 5);
        var qA = CreateTestQueue("QueueA_Pause", 1, new List<Vector2Int> { new Vector2Int(5, 5), new Vector2Int(5, 4) });
        CreateTestQueue("QueueB_Pause", 2, new List<Vector2Int> { new Vector2Int(5, 8), new Vector2Int(5, 9) });

        qA.TryStartMove();
        yield return new WaitForSeconds(0.1f);

        // Pause
        gameManager.PauseGame();
        Vector3 pausedPos = qA.Units[0].transform.position;

        yield return new WaitForSeconds(0.25f);
        Vector3 afterPausePos = qA.Units[0].transform.position;

        // Movement should have frozen
        if (Vector3.Distance(pausedPos, afterPausePos) > 0.01f)
        {
            Debug.LogError("[TEST 12 FAIL] Queue continued moving while game was paused!");
            yield break;
        }

        // Resume
        gameManager.ResumeGame();
        yield return new WaitForSeconds(0.2f);
        Vector3 afterResumePos = qA.Units[0].transform.position;

        if (Vector3.Distance(afterPausePos, afterResumePos) < 0.02f)
        {
            Debug.LogError("[TEST 12 FAIL] Queue did not resume movement after game resumed!");
            yield break;
        }

        // Wait for finish
        float timeout = 4.0f;
        while (qA.MoveState != AntQueueController.QueueMoveState.Idle && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        Debug.Log("[PASS] Test 12: Pause correctly froze failed-move sequence and resumed cleanly.");
    }
}
#endif
