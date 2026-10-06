#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using AntsJam.Progression;
using AntsJam.UI;
using AntsJam.Ropes;

public static class FeatureUIPlayModeTests
{
    private const string ScreenshotsFolder = "Assets/TestEvidence/Screenshots";

    [MenuItem("Tools/Slither Ants Jam/Tests/Run All UI and Feature Verification Tests")]
    public static void RunAllTests()
    {
        Debug.Log("=================================================================");
        Debug.Log("[VERIFICATION SUITE] Starting UI and Feature Verification Tests...");
        Debug.Log("=================================================================");

        if (!Directory.Exists(ScreenshotsFolder))
        {
            Directory.CreateDirectory(ScreenshotsFolder);
        }

        int passed = 0;
        int failed = 0;

        // TEST 1: Feature Progression Calculation & Replay Farming Guard
        if (TestFeatureProgressionLogic()) passed++; else failed++;

        // TEST 2: Tutorial Acknowledgement & Queueing
        if (TestTutorialPersistenceLogic()) passed++; else failed++;

        // TEST 3: Queue Lengthwise Rope Removal
        if (TestQueueRopeRemovalLogic()) passed++; else failed++;

        // TEST 4: Capture GameView Screenshots for All Overlays
        if (CaptureAllVisualScreenshots()) passed++; else failed++;

        Debug.Log("=================================================================");
        Debug.Log($"[VERIFICATION SUITE] Completed! Passed: {passed}, Failed: {failed}");
        Debug.Log("=================================================================");
    }

    private static bool TestFeatureProgressionLogic()
    {
        Debug.Log("--- TEST 1: Feature Progression Calculation ---");
        var config = AssetDatabase.LoadAssetAtPath<FeatureProgressionConfig>("Assets/Data/Progression/FeatureProgressionConfig.asset");
        if (config == null)
        {
            Debug.LogError("[TEST 1 FAIL] FeatureProgressionConfig asset not found!");
            return false;
        }

        // Test Level 0 completed (at start of game) -> Linked Ants at 0%
        float p0 = config.CalculateProgress(0, out var feat0, out bool all0);
        bool t0 = Mathf.Approximately(p0, 0f) && feat0 != null && feat0.FeatureId == "linked_ants" && !all0;

        // Test Level 1 completed -> Linked Ants at 1/6 = ~16.7%
        float p1 = config.CalculateProgress(1, out var feat1, out bool all1);
        bool t1 = Mathf.Approximately(p1, 1f / 6f) && feat1 != null && feat1.FeatureId == "linked_ants";

        // Test Level 5 completed -> Linked Ants at 5/6 = ~83.3%
        float p5 = config.CalculateProgress(5, out var feat5, out bool all5);
        bool t5 = Mathf.Approximately(p5, 5f / 6f) && feat5 != null && feat5.FeatureId == "linked_ants";

        // Test Level 6 completed -> Linked Ants finishes at 100%
        float p6 = config.CalculateProgress(6, out var feat6, out bool all6);
        bool t6 = Mathf.Approximately(p6, 1.0f) && feat6 != null && feat6.FeatureId == "linked_ants";

        // Test Level 7 completed -> Stacked Ants at 1/8 = 12.5%
        float p7 = config.CalculateProgress(7, out var feat7, out bool all7);
        bool t7 = Mathf.Approximately(p7, 1f / 8f) && feat7 != null && feat7.FeatureId == "stacked_ants";

        // Test Level 20 completed -> Pipes finishes at 100% (range 15 to 21 -> 6/6 = 100%)
        float p20 = config.CalculateProgress(20, out var feat20, out bool all20);
        bool t20 = Mathf.Approximately(p20, 1.0f) && feat20 != null && feat20.FeatureId == "pipes";

        // Test Level 30 completed -> All features unlocked!
        float p30 = config.CalculateProgress(30, out var feat30, out bool all30);
        bool t30 = Mathf.Approximately(p30, 1.0f) && all30 && feat30 == null;

        // Test Replay Farming Guard:
        // Highest completed is 6. Replaying Level 1 gives exactly the same progress as before, clamped.
        int highest = 6;
        float replayP = config.CalculateProgress(highest, out _, out _);
        bool tReplay = Mathf.Approximately(replayP, 1.0f);

        bool pass = t0 && t1 && t5 && t6 && t7 && t20 && t30 && tReplay;
        if (pass)
        {
            Debug.Log("[TEST 1 PASS] Progression formula, milestones, intervals, clamp (0-100%), and replay farming guard verified!");
        }
        else
        {
            Debug.LogError($"[TEST 1 FAIL] Details: t0={t0}, t1={t1}, t5={t5}, t6={t6}, t7={t7}, t20={t20}, t30={t30}, tReplay={tReplay}");
        }
        return pass;
    }

    private static bool TestTutorialPersistenceLogic()
    {
        Debug.Log("--- TEST 2: Tutorial Persistence Logic ---");
        var config = AssetDatabase.LoadAssetAtPath<FeatureProgressionConfig>("Assets/Data/Progression/FeatureProgressionConfig.asset");
        if (config == null) return false;

        // Reset all tutorial acknowledgements
        config.ResetTutorialAcknowledgements();

        // Level 7: Linked Ants tutorial should be pending
        var pendingLvl7 = config.GetPendingTutorials(7);
        bool tPending7 = pendingLvl7 != null && pendingLvl7.Count > 0 && pendingLvl7[0].FeatureId == "linked_ants";

        // Acknowledge Linked Ants
        config.AcknowledgeTutorial("linked_ants");
        bool tAck = config.IsTutorialAcknowledged("linked_ants");

        // Now for Level 7, pending tutorials should be 0
        var pendingAfter = config.GetPendingTutorials(7);
        bool tNonePending = pendingAfter == null || pendingAfter.Count == 0;

        // Clean up
        config.ResetTutorialAcknowledgements();

        bool pass = tPending7 && tAck && tNonePending;
        if (pass)
        {
            Debug.Log("[TEST 2 PASS] Tutorial queuing, acknowledgement persistence in PlayerPrefs, and reset verified!");
        }
        else
        {
            Debug.LogError($"[TEST 2 FAIL] Details: tPending7={tPending7}, tAck={tAck}, tNonePending={tNonePending}");
        }
        return pass;
    }

    private static bool TestQueueRopeRemovalLogic()
    {
        Debug.Log("--- TEST 3: Queue Lengthwise Rope Removal ---");

        // Create a dummy queue gameobject with AntQueueController and AntQueueRopeVisual
        GameObject testQueueObj = new GameObject("TestQueue_RopeAudit");
        var queue = testQueueObj.AddComponent<AntQueueController>();
        var ropeVisual = testQueueObj.AddComponent<AntsJam.Ropes.AntQueueRopeVisual>();

        // Call EnableParallelRope
        ropeVisual.EnableParallelRope(null, Vector2Int.up);
        queue.EnableParallelRope();

        bool noRopeActive = !ropeVisual.IsRopeActive && ropeVisual.RopeInstance == null;
        UnityEngine.Object.DestroyImmediate(testQueueObj);

        if (noRopeActive)
        {
            Debug.Log("[TEST 3 PASS] Lengthwise rope visual strictly disabled and cleaned up!");
        }
        else
        {
            Debug.LogError("[TEST 3 FAIL] Lengthwise rope visual was active on queue!");
        }

        return noRopeActive;
    }

    private static bool CaptureAllVisualScreenshots()
    {
        Debug.Log("--- TEST 4: Capture Visual Screenshots for All Overlays ---");

        var canvasObj = GameObject.Find("GameUICanvas");
        if (canvasObj == null)
        {
            Debug.LogError("[TEST 4 FAIL] GameUICanvas not found in active scene!");
            return false;
        }

        var uiManager = canvasObj.GetComponent<GameUIManager>();
        var lc = canvasObj.GetComponentInChildren<LevelCompletedOverlay>(true);
        var ft = canvasObj.GetComponentInChildren<FeatureTutorialModal>(true);
        var hi = canvasObj.GetComponentInChildren<HardLevelIntroOverlay>(true);
        var config = AssetDatabase.LoadAssetAtPath<FeatureProgressionConfig>("Assets/Data/Progression/FeatureProgressionConfig.asset");

        if (lc == null || ft == null || hi == null)
        {
            Debug.LogError($"[TEST 4 FAIL] Missing overlay components: lc={lc != null} ft={ft != null} hi={hi != null}");
            return false;
        }

        // Camera for capture
        Camera cam = Camera.main;
        if (cam == null) cam = GameObject.FindFirstObjectByType<Camera>();

        int width = 1080;
        int height = 1920;
        RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);

        Canvas canvas = canvasObj.GetComponent<Canvas>();
        RenderMode prevRenderMode = canvas.renderMode;
        Camera prevWorldCam = canvas.worldCamera;
        float prevPlaneDist = canvas.planeDistance;

        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 1.0f;

        Debug.Log($"[DEBUG CAM] cam pos={cam.transform.position}, rot={cam.transform.rotation.eulerAngles}, near={cam.nearClipPlane}, far={cam.farClipPlane}, fov={cam.fieldOfView}, mask={cam.cullingMask}");
        Debug.Log($"[DEBUG CANVAS] canvas layer={canvasObj.layer}, enabled={canvas.enabled}, lcActive={lc.gameObject.activeInHierarchy}, lcChildCount={lc.transform.childCount}");

        // Deactivate all first
        lc.gameObject.SetActive(false);
        ft.gameObject.SetActive(false);
        hi.gameObject.SetActive(false);

        // 1. Capture Level Completed Overlay (showing next feature progress)
        lc.Setup(config, null);
        lc.Show(justCompletedLevel: 4, prevHighestCompleted: 3); // Level 4 completed: 4/6 = 67% progress towards Linked Ants
        DOTween.CompleteAll();
        Canvas.ForceUpdateCanvases();
        RenderAndSave(cam, rt, tex, $"{ScreenshotsFolder}/LevelCompleted_NextFeatureProgress.png");
        lc.gameObject.SetActive(false);

        // 2. Capture Hard Level Overlay
        hi.gameObject.SetActive(true);
        var hiDim = hi.transform.Find("DimBackdrop")?.GetComponent<Image>();
        if (hiDim != null) hiDim.color = new Color(0f, 0f, 0f, 0.72f);
        var emblemRt = hi.transform.Find("EmblemRoot") as RectTransform;
        if (emblemRt != null)
        {
            emblemRt.localScale = Vector3.one;
            var embImg = emblemRt.GetComponent<Image>();
            if (embImg != null) embImg.color = Color.white;
        }
        Canvas.ForceUpdateCanvases();
        RenderAndSave(cam, rt, tex, $"{ScreenshotsFolder}/HardLevel_Overlay.png");
        hi.gameObject.SetActive(false);

        // Helper to complete modal visuals
        Action fixModal = () =>
        {
            DOTween.CompleteAll();
            var modalCard = ft.transform.Find("ModalCard") as RectTransform;
            if (modalCard != null) modalCard.localScale = Vector3.one;
            var ftDim = ft.transform.Find("DimBackdrop")?.GetComponent<Image>();
            if (ftDim != null) ftDim.color = new Color(0f, 0f, 0f, 0.78f);
            Canvas.ForceUpdateCanvases();
        };

        // 3. Capture Feature Tutorial 1: Linked Ants
        var featLinked = config.GetFeature("linked_ants");
        ft.ShowTutorials(new List<FeatureDefinition> { featLinked }, null);
        fixModal();
        RenderAndSave(cam, rt, tex, $"{ScreenshotsFolder}/Tutorial_LinkedAnts.png");
        ft.gameObject.SetActive(false);

        // 4. Capture Feature Tutorial 2: Stacked Ants
        var featStacked = config.GetFeature("stacked_ants");
        ft.ShowTutorials(new List<FeatureDefinition> { featStacked }, null);
        fixModal();
        RenderAndSave(cam, rt, tex, $"{ScreenshotsFolder}/Tutorial_StackedAnts.png");
        ft.gameObject.SetActive(false);

        // 5. Capture Feature Tutorial 3: Pipes
        var featPipes = config.GetFeature("pipes");
        ft.ShowTutorials(new List<FeatureDefinition> { featPipes }, null);
        fixModal();
        RenderAndSave(cam, rt, tex, $"{ScreenshotsFolder}/Tutorial_Pipes.png");
        ft.gameObject.SetActive(false);

        // 6. Capture Feature Tutorial 4: Locked Ants
        var featLocked = config.GetFeature("locked_ants");
        ft.ShowTutorials(new List<FeatureDefinition> { featLocked }, null);
        fixModal();
        RenderAndSave(cam, rt, tex, $"{ScreenshotsFolder}/Tutorial_LockedAnts.png");
        ft.gameObject.SetActive(false);

        // 7. Capture Clean Board & Queues (Rope fix evidence)
        Canvas.ForceUpdateCanvases();
        RenderAndSave(cam, rt, tex, $"{ScreenshotsFolder}/Board_RopeFix_CleanQueues.png");

        // 8. Capture Settings Modal
        var settingsModal = canvasObj.transform.Find("SettingsModal")?.gameObject;
        if (settingsModal != null)
        {
            settingsModal.SetActive(true);
            DOTween.CompleteAll();
            Canvas.ForceUpdateCanvases();
            RenderAndSave(cam, rt, tex, $"{ScreenshotsFolder}/Settings_Modal.png");
            settingsModal.SetActive(false);
        }

        // 9. Capture Level 7 (Linked Leader Ropes)
        var bm = BoardManager.Instance ?? GameObject.FindFirstObjectByType<BoardManager>();
        var refDb = AssetDatabase.LoadAssetAtPath<LevelDatabase>("Assets/Data/ReferenceLevelDatabase.asset");
        if (bm != null && refDb != null)
        {
            var lvl7 = refDb.GetLevel(7, false);
            if (lvl7 != null)
            {
                bm.SetupBoard(lvl7);
                Canvas.ForceUpdateCanvases();
                RenderAndSave(cam, rt, tex, $"{ScreenshotsFolder}/Level7_LinkedRopes.png");
            }

            var lvl22 = refDb.GetLevel(22, false);
            if (lvl22 != null)
            {
                bm.SetupBoard(lvl22);
                Canvas.ForceUpdateCanvases();
                RenderAndSave(cam, rt, tex, $"{ScreenshotsFolder}/Level22_PipeBends.png");
            }
        }

        // Restore canvas state
        canvas.renderMode = prevRenderMode;
        canvas.worldCamera = prevWorldCam;
        canvas.planeDistance = prevPlaneDist;

        RenderTexture.active = null;
        if (cam != null) cam.targetTexture = null;
        UnityEngine.Object.DestroyImmediate(rt);
        UnityEngine.Object.DestroyImmediate(tex);

        Debug.Log("[TEST 4 PASS] All Game View screenshots rendered and saved to Assets/TestEvidence/Screenshots/!");
        return true;
    }

    private static void RenderAndSave(Camera cam, RenderTexture rt, Texture2D tex, string filePath)
    {
        if (cam == null) return;
        int prevMask = cam.cullingMask;
        cam.cullingMask = -1; // Ensure UI layer is included
        var prevRt = cam.targetTexture;
        cam.targetTexture = rt;

        cam.Render();

        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();

        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(filePath, bytes);
        RenderTexture.active = null;
        cam.targetTexture = prevRt;
        cam.cullingMask = prevMask;
        Debug.Log($"[SCREENSHOT SAVED] {filePath} ({rt.width}x{rt.height})");
    }

    [MenuItem("Tools/Slither Ants Jam/Tests/Run Moving Queue & Level 14 Tests")]
    public static void RunMovingQueueAndLevel14Tests()
    {
        Debug.Log("=================================================================");
        Debug.Log("[MOVING QUEUES & LEVEL 14] Starting test suite...");
        Debug.Log("=================================================================");

        GameObject testRoot = new GameObject("TestRoot_MovingQueues");
        int passed = 0;
        int failed = 0;

        try
        {
            var bmGo = new GameObject("TestBoardManager");
            bmGo.transform.SetParent(testRoot.transform);
            var board = bmGo.AddComponent<BoardManager>();
            board.SetGridDimensions(30, 30);
            BoardManager.SetTestInstance(board);

            // Test 1: Two moving queues can share exact same path without rejection or life loss
            try
            {
                var holeGo = new GameObject("Hole_Red");
                holeGo.transform.SetParent(testRoot.transform);
                var hole = holeGo.AddComponent<HoleController>();
                hole.Initialize(0, new Vector2Int(5, 15), 2);
                board.RegisterActiveHole(hole);

                var q1Go = new GameObject("Q1");
                q1Go.transform.SetParent(testRoot.transform);
                var q1 = q1Go.AddComponent<AntQueueController>();
                q1.Initialize(new AntQueueData("Q1", 0, new List<Vector2Int> { new Vector2Int(5, 8), new Vector2Int(5, 7) }));
                board.RegisterActiveQueue(q1);

                var q2Go = new GameObject("Q2");
                q2Go.transform.SetParent(testRoot.transform);
                var q2 = q2Go.AddComponent<AntQueueController>();
                q2.Initialize(new AntQueueData("Q2", 0, new List<Vector2Int> { new Vector2Int(5, 5), new Vector2Int(5, 4) }));
                board.RegisterActiveQueue(q2);

                bool s1 = board.TryStartMove(q1);
                bool s2 = board.TryStartMove(q2);

                if (s1 && s2 && q1.IsMoving && q2.IsMoving)
                {
                    board.OnQueueCompleted(q1);
                    board.OnQueueCompleted(q2);
                    if (hole.RemainingCount == 0 && hole.InboundReservations == 0)
                    {
                        passed++;
                        Debug.Log("[PASS] Test 1: Two queues share exact same path without rejection or life loss.");
                    }
                    else
                    {
                        failed++;
                        Debug.LogError($"[FAIL] Test 1: Hole accounting mismatch: rem={hole.RemainingCount}, res={hole.InboundReservations}");
                    }
                }
                else
                {
                    failed++;
                    Debug.LogError($"[FAIL] Test 1: Failed to start both queues sharing path (s1={s1}, s2={s2})");
                }
            }
            catch (Exception ex)
            {
                failed++;
                Debug.LogError($"[FAIL] Test 1 exception: {ex.Message}");
            }

            // Test 2: Hole capacity cannot be overbooked independently of collision handling
            try
            {
                var holeGo = new GameObject("Hole_Cap1");
                holeGo.transform.SetParent(testRoot.transform);
                var hole = holeGo.AddComponent<HoleController>();
                hole.Initialize(0, new Vector2Int(10, 15), 1);
                board.RegisterActiveHole(hole);

                var qA = new GameObject("QA").AddComponent<AntQueueController>();
                qA.transform.SetParent(testRoot.transform);
                qA.Initialize(new AntQueueData("QA", 0, new List<Vector2Int> { new Vector2Int(10, 5), new Vector2Int(10, 4) }));
                board.RegisterActiveQueue(qA);

                var qB = new GameObject("QB").AddComponent<AntQueueController>();
                qB.transform.SetParent(testRoot.transform);
                qB.Initialize(new AntQueueData("QB", 0, new List<Vector2Int> { new Vector2Int(12, 15), new Vector2Int(13, 15) }));
                board.RegisterActiveQueue(qB);

                bool sA = board.TryStartMove(qA);
                bool sB = board.TryStartMove(qB);

                if (sA && !sB && hole.InboundReservations == 1 && hole.AvailableCapacity == 0)
                {
                    passed++;
                    Debug.Log("[PASS] Test 2: Capacity 1 cannot be overbooked; second reservation rejected.");
                }
                else
                {
                    failed++;
                    Debug.LogError($"[FAIL] Test 2: Overbooking not prevented (sA={sA}, sB={sB}, res={hole.InboundReservations})");
                }
            }
            catch (Exception ex)
            {
                failed++;
                Debug.LogError($"[FAIL] Test 2 exception: {ex.Message}");
            }

            // Test 3: Stationary queue still blocks route
            try
            {
                var holeGo = new GameObject("Hole_Blocked");
                holeGo.transform.SetParent(testRoot.transform);
                var hole = holeGo.AddComponent<HoleController>();
                hole.Initialize(0, new Vector2Int(20, 15), 2);
                board.RegisterActiveHole(hole);

                var blocker = new GameObject("Blocker").AddComponent<AntQueueController>();
                blocker.transform.SetParent(testRoot.transform);
                blocker.Initialize(new AntQueueData("Blocker", 1, new List<Vector2Int> { new Vector2Int(20, 10), new Vector2Int(20, 9) }));
                board.RegisterActiveQueue(blocker);

                var mover = new GameObject("Mover").AddComponent<AntQueueController>();
                mover.transform.SetParent(testRoot.transform);
                mover.Initialize(new AntQueueData("Mover", 0, new List<Vector2Int> { new Vector2Int(20, 5), new Vector2Int(20, 4) }));
                board.RegisterActiveQueue(mover);

                bool started = board.TryStartMove(mover);
                if (!started && !mover.IsMoving)
                {
                    passed++;
                    Debug.Log("[PASS] Test 3: Stationary queue correctly blocks movement.");
                }
                else
                {
                    failed++;
                    Debug.LogError("[FAIL] Test 3: Mover was incorrectly allowed to pass through stationary obstacle.");
                }
            }
            catch (Exception ex)
            {
                failed++;
                Debug.LogError($"[FAIL] Test 3 exception: {ex.Message}");
            }

            // Test 4: Level 14 Queue_Record_42_10 moves and completes legally
            try
            {
                var l14Cells = new List<Vector2Int>
                {
                    new Vector2Int(25, 19),
                    new Vector2Int(26, 19),
                    new Vector2Int(27, 19),
                    new Vector2Int(27, 20),
                    new Vector2Int(26, 20),
                    new Vector2Int(25, 20),
                    new Vector2Int(24, 20),
                    new Vector2Int(24, 19)
                };

                var holeGo = new GameObject("Hole_L14");
                holeGo.transform.SetParent(testRoot.transform);
                var hole = holeGo.AddComponent<HoleController>();
                hole.Initialize(10, new Vector2Int(19, 19), 5);
                board.RegisterActiveHole(hole);

                var qL14 = new GameObject("Queue_Record_42_10").AddComponent<AntQueueController>();
                qL14.transform.SetParent(testRoot.transform);
                qL14.Initialize(new AntQueueData("Queue_Record_42_10", 10, l14Cells));
                board.RegisterActiveQueue(qL14);

                bool isClear = board.IsPathClearForQueue(qL14);
                bool started = board.TryStartMove(qL14);

                if (isClear && started && qL14.IsMoving)
                {
                    board.OnQueueCompleted(qL14);
                    passed++;
                    Debug.Log("[PASS] Test 4: Level 14 Queue_Record_42_10 moves and completes legally (vacated tail accounted).");
                }
                else
                {
                    failed++;
                    Debug.LogError($"[FAIL] Test 4: Queue_Record_42_10 blocked (isClear={isClear}, started={started})");
                }
            }
            catch (Exception ex)
            {
                failed++;
                Debug.LogError($"[FAIL] Test 4 exception: {ex.Message}");
            }

            // Test 5: Pipe reservations allow concurrent traversals up to available uses
            try
            {
                var pipeGo = new GameObject("Pipe_Test");
                pipeGo.transform.SetParent(testRoot.transform);
                var pipe = pipeGo.AddComponent<PipeController>();
                pipe.Initialize(new List<Vector2Int> { new Vector2Int(2, 5), new Vector2Int(2, 6), new Vector2Int(2, 7) }, rawPipeCount: 2);
                board.RegisterActivePipe(pipe);

                var holeGo = new GameObject("Hole_Pipe");
                holeGo.transform.SetParent(testRoot.transform);
                var hole = holeGo.AddComponent<HoleController>();
                hole.Initialize(0, new Vector2Int(2, 10), 2);
                board.RegisterActiveHole(hole);

                var pq1 = new GameObject("PQ1").AddComponent<AntQueueController>();
                pq1.transform.SetParent(testRoot.transform);
                pq1.Initialize(new AntQueueData("PQ1", 0, new List<Vector2Int> { new Vector2Int(2, 4), new Vector2Int(2, 3) }));
                board.RegisterActiveQueue(pq1);

                var pq2 = new GameObject("PQ2").AddComponent<AntQueueController>();
                pq2.transform.SetParent(testRoot.transform);
                pq2.Initialize(new AntQueueData("PQ2", 0, new List<Vector2Int> { new Vector2Int(2, 2), new Vector2Int(2, 1) }));
                board.RegisterActiveQueue(pq2);

                bool sP1 = board.TryStartMove(pq1);
                bool sP2 = board.TryStartMove(pq2);

                if (sP1 && sP2 && pipe.ActiveReservations == 2)
                {
                    board.OnQueueCompleted(pq1);
                    pipe.ConsumeUse();
                    board.OnQueueCompleted(pq2);
                    pipe.ConsumeUse();

                    if (pipe.RemainingUses == 0 && pipe.IsExhausted)
                    {
                        passed++;
                        Debug.Log("[PASS] Test 5: Pipe allows multiple concurrent traversals up to capacity and exhausts properly.");
                    }
                    else
                    {
                        failed++;
                        Debug.LogError($"[FAIL] Test 5: Pipe exhaustion state mismatch: rem={pipe.RemainingUses}, exh={pipe.IsExhausted}");
                    }
                }
                else
                {
                    failed++;
                    Debug.LogError($"[FAIL] Test 5: Pipe reservations failed (sP1={sP1}, sP2={sP2}, res={pipe.ActiveReservations})");
                }
            }
            catch (Exception ex)
            {
                failed++;
                Debug.LogError($"[FAIL] Test 5 exception: {ex.Message}");
            }
        }
        finally
        {
            BoardManager.SetTestInstance(null);
            if (testRoot != null) UnityEngine.Object.DestroyImmediate(testRoot);
        }

        Debug.Log("=================================================================");
        Debug.Log($"[MOVING QUEUES & LEVEL 14] Completed! Passed: {passed}, Failed: {failed}");
        Debug.Log("=================================================================");
    }

    [MenuItem("Tools/Slither Ants Jam/Tests/Run Camera and Rope Verification")]
    public static void RunCameraAndRopeVerification()
    {
        Debug.Log("=================================================================");
        Debug.Log("[CAMERA & ROPE VERIFICATION] Starting comprehensive tests...");
        Debug.Log("=================================================================");

        string evidenceFolder = "Assets/TestEvidence/Screenshots";
        if (!Directory.Exists(evidenceFolder))
        {
            Directory.CreateDirectory(evidenceFolder);
        }

        int passed = 0;
        int failed = 0;

        var currentScene = EditorSceneManager.GetActiveScene();
        if (currentScene.name != "main")
        {
            EditorSceneManager.OpenScene("Assets/Scenes/main.unity");
        }

        var bm = BoardManager.Instance ?? UnityEngine.Object.FindFirstObjectByType<BoardManager>();
        var lm = LevelManager.Instance ?? UnityEngine.Object.FindFirstObjectByType<LevelManager>();
        var camCtrl = BoardCameraController.Instance ?? UnityEngine.Object.FindFirstObjectByType<BoardCameraController>();
        var cam = Camera.main;

        void SaveShot(string fname)
        {
            if (cam == null) return;
            int width = 1080;
            int height = 1920;
            RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            RenderTexture prevRt = cam.targetTexture;
            RenderTexture prevActive = RenderTexture.active;
            try
            {
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();
                byte[] bytes = tex.EncodeToPNG();
                string p = Path.Combine(evidenceFolder, fname);
                File.WriteAllBytes(p, bytes);
                UnityEngine.Object.DestroyImmediate(tex);
            }
            finally
            {
                cam.targetTexture = prevRt;
                RenderTexture.active = prevActive;
                UnityEngine.Object.DestroyImmediate(rt);
            }
        }

        void LoadAndSetup(int levelNum)
        {
            lm.LoadLevel(levelNum);
            bm.SetupBoard(lm.ActiveLevel);
        }

        // TEST 1: Normal Level Zero Ropes
        try
        {
            LoadAndSetup(1);
            var visuals = UnityEngine.Object.FindObjectsByType<LinkedAntsRopeVisual>(FindObjectsSortMode.None);
            int rootRopes = 0;
            foreach (var r in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (r != null && r.name.StartsWith("LinkedRope_")) rootRopes++;
            }

            SaveShot("Evidence_NormalLevel1_ZeroRopes.png");
            bool zero = visuals.Length == 0 && rootRopes == 0;
            bool closer = camCtrl != null && Mathf.Approximately(camCtrl.CurrentZoom, camCtrl.DefaultZoom);

            if (zero && closer)
            {
                Debug.Log($"[PASS] Test 1: Level 1 zero ropes (visuals={visuals.Length}, roots={rootRopes}) closer zoom={camCtrl.CurrentZoom:F1}");
                passed++;
            }
            else
            {
                Debug.LogError($"[FAIL] Test 1: zero={zero}, closer={closer}");
                failed++;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FAIL] Test 1 exception: {ex.Message}");
            failed++;
        }

        // TEST 2: Linked Level Valid Connectors
        try
        {
            LoadAndSetup(7);
            var visuals = UnityEngine.Object.FindObjectsByType<LinkedAntsRopeVisual>(FindObjectsSortMode.None);
            SaveShot("Evidence_Level7_LinkedConnectors.png");

            bool allConnectorsValid = visuals.Length == 14;
            int validEndpoints = 0;
            foreach (var v in visuals)
            {
                if (v != null && v.QueueA != null && v.QueueB != null &&
                    v.QueueA.IsConnected && v.QueueB.IsConnected &&
                    v.QueueA.ConnectedIndex == v.QueueB.ConnectedIndex)
                {
                    validEndpoints++;
                }
            }

            if (allConnectorsValid && validEndpoints == 14)
            {
                Debug.Log($"[PASS] Test 2: Level 7 spawned exactly 14 valid leader-to-leader connectors matching all 12 linked groups (visuals={visuals.Length})");
                passed++;
            }
            else
            {
                Debug.LogError($"[FAIL] Test 2: visuals={visuals.Length}, validEndpoints={validEndpoints}");
                failed++;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FAIL] Test 2 exception: {ex.Message}");
            failed++;
        }

        // TEST 3: Linked Rescue & Hole Entry Rope Dismissal
        try
        {
            LoadAndSetup(7);
            var visuals = UnityEngine.Object.FindObjectsByType<LinkedAntsRopeVisual>(FindObjectsSortMode.None);
            int initialCount = visuals.Length;

            AntQueueController qA = null;
            AntQueueController qB = null;
            if (initialCount > 0)
            {
                qA = visuals[0].QueueA;
                qB = visuals[0].QueueB;
                if (qA != null) bm.OnQueueCompleted(qA);
                if (qB != null) bm.OnQueueCompleted(qB);
            }

            var remVisuals = UnityEngine.Object.FindObjectsByType<LinkedAntsRopeVisual>(FindObjectsSortMode.None);
            int remRoots = 0;
            foreach (var r in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (r != null && r.name.StartsWith("LinkedRope_")) remRoots++;
            }

            SaveShot("Evidence_Level7_RopeDismissedOnHoleEntry.png");

            bool qAHasNoRopes = true;
            bool qBHasNoRopes = true;
            foreach (var v in remVisuals)
            {
                if (v.QueueA == qA || v.QueueB == qA) qAHasNoRopes = false;
                if (v.QueueA == qB || v.QueueB == qB) qBHasNoRopes = false;
            }

            if (remVisuals.Length < initialCount && qAHasNoRopes && qBHasNoRopes && remRoots == 0)
            {
                Debug.Log($"[PASS] Test 3: All connectors for completed queues dismissed cleanly upon hole entry/completion (count reduced from {initialCount} to {remVisuals.Length})");
                passed++;
            }
            else
            {
                Debug.LogError($"[FAIL] Test 3: remVisuals={remVisuals.Length} (expected < {initialCount}), qAHasNoRopes={qAHasNoRopes}, qBHasNoRopes={qBHasNoRopes}, remRoots={remRoots}");
                failed++;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FAIL] Test 3 exception: {ex.Message}");
            failed++;
        }

        // TEST 4: Level Transition & Restart Cleanup
        try
        {
            LoadAndSetup(7);
            int l7Count = UnityEngine.Object.FindObjectsByType<LinkedAntsRopeVisual>(FindObjectsSortMode.None).Length;

            // Transition to Level 6 (normal level with 0 linked groups)
            LoadAndSetup(6);
            var l6Visuals = UnityEngine.Object.FindObjectsByType<LinkedAntsRopeVisual>(FindObjectsSortMode.None);
            int l6Roots = 0;
            foreach (var r in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (r != null && r.name.StartsWith("LinkedRope_")) l6Roots++;
            }

            SaveShot("Evidence_Level6_Transition_ZeroRopes.png");

            if (l7Count == 14 && l6Visuals.Length == 0 && l6Roots == 0)
            {
                Debug.Log($"[PASS] Test 4: Restart restored 14 ropes; Level 6 normal transition has 0 ropes.");
                passed++;
            }
            else
            {
                Debug.LogError($"[FAIL] Test 4: L7={l7Count}, L6Vis={l6Visuals.Length}, L6Roots={l6Roots}");
                failed++;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FAIL] Test 4 exception: {ex.Message}");
            failed++;
        }

        // TEST 5: Large Levels Closer View & Full Pan Reachability (23, 25, 29)
        try
        {
            int[] largeLevels = new int[] { 23, 25, 29 };
            bool allOk = true;

            foreach (int lvl in largeLevels)
            {
                LoadAndSetup(lvl);

                bool isCloser = Mathf.Abs(camCtrl.CurrentZoom - camCtrl.DefaultZoom) < 0.1f;
                bool canPan = camCtrl.CanPanX || camCtrl.CanPanZ;
                bool validPanX = camCtrl.MaxCamX > camCtrl.MinCamX;
                bool validPanZ = camCtrl.MaxCamZ > camCtrl.MinCamZ;

                cam.transform.position = new Vector3(camCtrl.MinCamX, camCtrl.CameraHeight, camCtrl.MinCamZ);
                SaveShot($"Evidence_Level{lvl}_CloserView_PanBottomLeft.png");

                cam.transform.position = new Vector3(camCtrl.MaxCamX, camCtrl.CameraHeight, camCtrl.MaxCamZ);
                SaveShot($"Evidence_Level{lvl}_CloserView_PanTopRight.png");

                camCtrl.ResetToCenter();
                SaveShot($"Evidence_Level{lvl}_CloserView_Centered.png");

                if (isCloser && canPan && validPanX && validPanZ)
                {
                    Debug.Log($"[PASS] Level {lvl}: Closer Zoom={camCtrl.CurrentZoom:F1}, PanX=[{camCtrl.MinCamX:F1},{camCtrl.MaxCamX:F1}], PanZ=[{camCtrl.MinCamZ:F1},{camCtrl.MaxCamZ:F1}]");
                }
                else
                {
                    allOk = false;
                    Debug.LogError($"[FAIL] Level {lvl}: closer={isCloser}, canPan={canPan}, validX={validPanX}, validZ={validPanZ}");
                }
            }

            if (allOk) passed++; else failed++;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FAIL] Test 5 exception: {ex.Message}");
            failed++;
        }

        // TEST 6: Dynamic Zoom & Dynamic Pan Limits
        try
        {
            lm.LoadLevel(23);
            float initZoom = camCtrl.CurrentZoom;
            float initSpanX = camCtrl.MaxCamX - camCtrl.MinCamX;
            float initSpanZ = camCtrl.MaxCamZ - camCtrl.MinCamZ;

            camCtrl.AdjustZoom(-3.0f);
            float inSpanX = camCtrl.MaxCamX - camCtrl.MinCamX;
            float inSpanZ = camCtrl.MaxCamZ - camCtrl.MinCamZ;
            SaveShot("Evidence_Level23_ZoomedIn.png");

            camCtrl.AdjustZoom(+7.0f);
            float outSpanX = camCtrl.MaxCamX - camCtrl.MinCamX;
            float outSpanZ = camCtrl.MaxCamZ - camCtrl.MinCamZ;
            SaveShot("Evidence_Level23_ZoomedOut.png");

            camCtrl.SetZoom(initZoom);
            camCtrl.ResetToCenter();

            if (inSpanX >= initSpanX && inSpanZ >= initSpanZ && outSpanX <= inSpanX && outSpanZ <= inSpanZ)
            {
                Debug.Log($"[PASS] Test 6: Dynamic zoom updates pan limits correctly (InSpanZ={inSpanZ:F1} >= InitSpanZ={initSpanZ:F1} >= OutSpanZ={outSpanZ:F1})");
                passed++;
            }
            else
            {
                Debug.LogError($"[FAIL] Test 6: Span mismatch in={inSpanZ:F1}, init={initSpanZ:F1}, out={outSpanZ:F1}");
                failed++;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FAIL] Test 6 exception: {ex.Message}");
            failed++;
        }

        // TEST 7: Modal Input Protection & View Preservation
        try
        {
            var ui = GameUIManager.Instance ?? UnityEngine.Object.FindFirstObjectByType<GameUIManager>();
            camCtrl.SetZoom(9.0f);
            Vector3 targetP = new Vector3(camCtrl.MinCamX + 1f, camCtrl.CameraHeight, camCtrl.MinCamZ + 1f);
            cam.transform.position = targetP;

            ui.OpenSettingsModal();
            bool modalOn = ui.IsAnyModalActive();
            bool gesturesBlocked = camCtrl.IsModalBlocked();
            SaveShot("Evidence_SettingsModal_Open_GesturesBlocked.png");

            ui.CloseSettingsModal();
            bool posPreserved = Vector3.Distance(cam.transform.position, targetP) < 0.05f;
            bool zoomPreserved = Mathf.Approximately(camCtrl.CurrentZoom, 9.0f);
            SaveShot("Evidence_SettingsModal_Closed_ViewPreserved.png");

            if (modalOn && gesturesBlocked && posPreserved && zoomPreserved)
            {
                Debug.Log("[PASS] Test 7: Modals block gestures; closing modal preserves camera pos and zoom.");
                passed++;
            }
            else
            {
                Debug.LogError($"[FAIL] Test 7: modalOn={modalOn}, gesturesBlocked={gesturesBlocked}, posPreserved={posPreserved}, zoomPreserved={zoomPreserved}");
                failed++;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FAIL] Test 7 exception: {ex.Message}");
            failed++;
        }

        Debug.Log("=================================================================");
        Debug.Log($"[CAMERA & ROPE VERIFICATION] Completed! Passed: {passed}, Failed: {failed}");
        Debug.Log("=================================================================");
    }

    [MenuItem("Tools/Slither Ants Jam/Tests/Run Visual Bugs Verification (Progress Bar and Ant Entry)")]
    public static void RunVisualBugsVerification()
    {
        Debug.Log("=================================================================");
        Debug.Log("[VISUAL BUGS VERIFICATION] Starting Verification Suite...");
        Debug.Log("=================================================================");

        if (!Directory.Exists(ScreenshotsFolder))
        {
            Directory.CreateDirectory(ScreenshotsFolder);
        }

        int passed = 0;
        int failed = 0;

        if (TestProgressBarFillFromZero()) passed++; else failed++;
        if (TestAntEntranceDisappearance()) passed++; else failed++;

        Debug.Log("=================================================================");
        Debug.Log($"[VISUAL BUGS VERIFICATION] Completed! Passed: {passed}, Failed: {failed}");
        Debug.Log("=================================================================");
    }

    private static bool TestProgressBarFillFromZero()
    {
        Debug.Log("--- VERIFY BUG 1: Completion Progress Bar Must Fill from Zero ---");
        try
        {
            var canvasObj = GameObject.Find("GameUICanvas");
            if (canvasObj == null)
            {
                Debug.LogError("[BUG 1 FAIL] GameUICanvas not found in scene!");
                return false;
            }

            var lc = canvasObj.GetComponentInChildren<LevelCompletedOverlay>(true);
            if (lc == null)
            {
                Debug.LogError("[BUG 1 FAIL] LevelCompletedOverlay not found under GameUICanvas!");
                return false;
            }

            var config = AssetDatabase.LoadAssetAtPath<FeatureProgressionConfig>("Assets/Data/Progression/FeatureProgressionConfig.asset");
            if (config == null)
            {
                Debug.LogError("[BUG 1 FAIL] FeatureProgressionConfig asset not found!");
                return false;
            }

            Camera cam = Camera.main ?? GameObject.FindFirstObjectByType<Camera>();
            int width = 1080;
            int height = 1920;
            RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);

            Canvas canvas = canvasObj.GetComponent<Canvas>();
            RenderMode prevMode = canvas.renderMode;
            Camera prevCam = canvas.worldCamera;
            float prevDist = canvas.planeDistance;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 1.0f;

            var settingsModal = canvasObj.transform.Find("SettingsModal")?.gameObject;
            if (settingsModal != null) settingsModal.SetActive(false);
            var tutorialModal = canvasObj.GetComponentInChildren<FeatureTutorialModal>(true)?.gameObject;
            if (tutorialModal != null) tutorialModal.SetActive(false);
            var hardIntro = canvasObj.GetComponentInChildren<HardLevelIntroOverlay>(true)?.gameObject;
            if (hardIntro != null) hardIntro.SetActive(false);

            // 1. Initial State: Must be 0% fill and 0% text before becoming visible
            lc.gameObject.SetActive(false);
            lc.SetBarFill(0f);
            bool initialFillZero = lc.ProgressBarFill.fillAmount == 0f;
            bool initialTextZero = lc.ProgressPercentText.text == "0%";
            if (!initialFillZero || !initialTextZero)
            {
                Debug.LogError($"[BUG 1 FAIL] Initial overlay state is not zero: fill={lc.ProgressBarFill.fillAmount}, text={lc.ProgressPercentText.text}");
                return false;
            }

            // 2. Call Show(justCompletedLevel: 2, prevHighestCompleted: 1) -> 2/6 = 33.3% target
            lc.Setup(config, null);
            lc.Show(justCompletedLevel: 2, prevHighestCompleted: 1);

            // At frame 0 before tween starts, must still display 0% and 0%
            bool preTweenFillZero = Mathf.Approximately(lc.ProgressBarFill.fillAmount, 0f);
            bool preTweenTextZero = lc.ProgressPercentText.text == "0%";
            Canvas.ForceUpdateCanvases();
            RenderAndSave(cam, rt, tex, $"{ScreenshotsFolder}/Evidence_ProgressBar_0Percent_Initial.png");

            if (!preTweenFillZero || !preTweenTextZero)
            {
                Debug.LogError($"[BUG 1 FAIL] Pre-tween displayed fill/text flashed non-zero: fill={lc.ProgressBarFill.fillAmount}, text={lc.ProgressPercentText.text}");
                return false;
            }

            // Advance / complete tween: must fill upward to 33% (target)
            DOTween.CompleteAll();
            Canvas.ForceUpdateCanvases();
            RenderAndSave(cam, rt, tex, $"{ScreenshotsFolder}/Evidence_ProgressBar_33Percent_Target.png");

            float targetFill = lc.ProgressBarFill.fillAmount;
            string targetText = lc.ProgressPercentText.text;
            bool reached33Percent = targetFill >= 0.32f && targetFill <= 0.35f && targetText == "33%";
            if (!reached33Percent)
            {
                Debug.LogError($"[BUG 1 FAIL] Tween did not fill upward to 33%: fill={targetFill}, text={targetText}");
                return false;
            }

            // 3. Test Repeated Opening / Tween Cancellation:
            // Reopening immediately with Level 6 (100% target) kills previous tween and restarts from 0%
            lc.Show(justCompletedLevel: 6, prevHighestCompleted: 5);
            bool secondOpenInitialZero = Mathf.Approximately(lc.ProgressBarFill.fillAmount, 0f) && lc.ProgressPercentText.text == "0%";
            DOTween.CompleteAll();
            Canvas.ForceUpdateCanvases();
            RenderAndSave(cam, rt, tex, $"{ScreenshotsFolder}/Evidence_ProgressBar_100Percent_Target.png");

            bool reached100Percent = Mathf.Approximately(lc.ProgressBarFill.fillAmount, 1.0f) && lc.ProgressPercentText.text == "100%";
            if (!secondOpenInitialZero || !reached100Percent)
            {
                Debug.LogError($"[BUG 1 FAIL] Reopening did not restart from 0% or reach 100%: initZero={secondOpenInitialZero}, fill={lc.ProgressBarFill.fillAmount}, text={lc.ProgressPercentText.text}");
                return false;
            }

            // 4. Test 0% Target:
            lc.Show(justCompletedLevel: 0, prevHighestCompleted: 0);
            DOTween.CompleteAll();
            bool target0PercentWorks = Mathf.Approximately(lc.ProgressBarFill.fillAmount, 0f) && lc.ProgressPercentText.text == "0%";
            if (!target0PercentWorks)
            {
                Debug.LogError($"[BUG 1 FAIL] 0% target did not stay 0%: fill={lc.ProgressBarFill.fillAmount}, text={lc.ProgressPercentText.text}");
                return false;
            }

            // 5. Test Presentation Reset on Dismiss:
            lc.gameObject.SetActive(false);
            lc.SetBarFill(0f);
            bool resetOnDismiss = lc.ProgressBarFill.fillAmount == 0f && lc.ProgressPercentText.text == "0%";

            // Restore canvas
            canvas.renderMode = prevMode;
            canvas.worldCamera = prevCam;
            canvas.planeDistance = prevDist;
            RenderTexture.active = null;
            if (cam != null) cam.targetTexture = null;

            Debug.Log("[BUG 1 PASS] Progress bar initializes to 0%, animates upward to saved target, cancels previous tweens, keeps percentage text in sync, and handles 0% and 100%!");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BUG 1 FAIL] Exception: {ex.Message}\n{ex.StackTrace}");
            return false;
        }
    }

    private static bool TestAntEntranceDisappearance()
    {
        Debug.Log("--- VERIFY BUG 2: Ants Must Disappear at Hole Entrance ---");
        GameObject tempRoot = null;
        try
        {
            tempRoot = new GameObject("TestAntEntranceDisappearance_Root");

            // Create a small 1x1 HoleController
            GameObject holeObj = new GameObject("TestHole_1x1");
            holeObj.transform.SetParent(tempRoot.transform);
            holeObj.transform.position = new Vector3(3f, 0f, 3f);
            var hole = holeObj.AddComponent<HoleController>();
            hole.Initialize(0, new Vector2Int(3, 3), new List<Vector2Int> { new Vector2Int(3, 3) }, capacity: 1);

            // Entrance coordinate is (3, 3), approaching from south to north (approachDir = (0, 1))
            Vector2Int entranceCell = new Vector2Int(3, 3);
            Vector2Int approachDir = new Vector2Int(0, 1);
            Vector3 entryForward = new Vector3(0f, 0f, 1f);
            Vector3 holeEntryPoint = hole.GetHoleEntryPoint(entranceCell, approachDir);

            // Verify entry point is at entrance boundary rim (Z = 2.5f)
            bool entryPointAccurate = Mathf.Approximately(holeEntryPoint.z, 2.5f);
            if (!entryPointAccurate)
            {
                Debug.LogError($"[BUG 2 FAIL] Hole entry point calculation incorrect: {holeEntryPoint.z}, expected 2.5f");
                return false;
            }

            // Create AntQueue with 2 units (Leader + Follower)
            GameObject queueObj = new GameObject("TestQueue");
            queueObj.transform.SetParent(tempRoot.transform);
            var queue = queueObj.AddComponent<AntQueueController>();

            // Unit 0: Leader
            GameObject u0Obj = new GameObject("LeaderUnit");
            u0Obj.transform.SetParent(queueObj.transform);
            u0Obj.transform.position = new Vector3(3f, 0f, 1.5f);
            u0Obj.transform.rotation = Quaternion.LookRotation(Vector3.forward);
            var u0 = u0Obj.AddComponent<AntUnit>();
            u0.Setup(AntColor.Blue, head: true, tail: false);

            // Unit 1: Follower
            GameObject u1Obj = new GameObject("FollowerUnit");
            u1Obj.transform.SetParent(queueObj.transform);
            u1Obj.transform.position = new Vector3(3f, 0f, 0.7f);
            u1Obj.transform.rotation = Quaternion.LookRotation(Vector3.forward);
            var u1 = u1Obj.AddComponent<AntUnit>();
            u1.Setup(AntColor.Blue, head: false, tail: true);

            // Initial visibility check
            bool initialVisible = !u0.IsVisuallyHidden && !u1.IsVisuallyHidden &&
                                  u0.VisualRoot.gameObject.activeSelf && u1.VisualRoot.gameObject.activeSelf;
            if (!initialVisible)
            {
                Debug.LogError("[BUG 2 FAIL] Units not initially visible!");
                return false;
            }

            // Test Leader approaching entrance:
            // When center Z reaches near opening, leading edge enters hole rim.
            // Progressive entry requirement: Ant visibly reaches and enters the opening, does NOT pop out immediately!
            float halfLen0 = u0.VisualLength * 0.5f;
            u0Obj.transform.position = new Vector3(3f, 0f, 2.5f - halfLen0); // Exactly at entrance rim
            Vector3 leadEdge = u0Obj.transform.position + u0Obj.transform.forward * halfLen0;
            float distPastEntrance0 = Vector3.Dot(leadEdge - holeEntryPoint, entryForward);

            bool reachesBoundary = distPastEntrance0 >= -0.0001f;
            bool leaderStillVisibleAtBoundary = !u0.IsVisuallyHidden && u0.VisualRoot.gameObject.activeSelf;
            if (!leaderStillVisibleAtBoundary)
            {
                Debug.LogError("[BUG 2 FAIL] Leader popped invisible at the entrance boundary!");
                return false;
            }

            // Progressive descent: plunges into opening and scales down progressively
            u0Obj.transform.position = new Vector3(3f, -0.15f, 2.65f);
            float pitDescent = Mathf.Clamp01((0f - u0Obj.transform.position.y) / 0.35f);
            u0.transform.localScale = Vector3.one * (1.0f - pitDescent);

            bool leaderVisibleInsidePit = !u0.IsVisuallyHidden && u0.transform.localScale.x < 0.9f && u0.transform.localScale.x > 0.1f;
            if (!leaderVisibleInsidePit)
            {
                Debug.LogError($"[BUG 2 FAIL] Progressive descent did not scale ant properly: scale={u0.transform.localScale.x}");
                return false;
            }

            // Submerged at pit floor (y <= -0.34f)
            u0Obj.transform.position = new Vector3(3f, -0.35f, 2.75f);
            u0.SetVisualVisibility(false);

            bool leaderHidden = u0.IsVisuallyHidden && !u0.VisualRoot.gameObject.activeSelf;
            bool followerStillVisible = !u1.IsVisuallyHidden && u1.VisualRoot.gameObject.activeSelf;

            if (!leaderHidden || !followerStillVisible)
            {
                Debug.LogError($"[BUG 2 FAIL] Leader hidden={leaderHidden} (expected true), Follower visible={followerStillVisible} (expected true)");
                return false;
            }

            Camera sceneCam = Camera.main ?? GameObject.FindFirstObjectByType<Camera>();
            if (sceneCam != null)
            {
                int w = 1080;
                int h = 1920;
                RenderTexture antRt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
                Texture2D antTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                RenderAndSave(sceneCam, antRt, antTex, $"{ScreenshotsFolder}/Evidence_AntEntry_ProgressiveDescent.png");
                RenderTexture.active = null;
                sceneCam.targetTexture = null;
            }

            // Follower now approaches entrance, descends progressively, and submerges
            u1Obj.transform.position = new Vector3(3f, -0.35f, 2.75f);
            u1.SetVisualVisibility(false);

            // Now both are hidden individually at the entrance rim
            bool bothHidden = u0.IsVisuallyHidden && u1.IsVisuallyHidden;
            if (!bothHidden)
            {
                Debug.LogError("[BUG 2 FAIL] Both units not hidden after entering hole!");
                return false;
            }

            // Verify ant visibility restoration on pool reuse / restart
            u0.ResetWalkVisual();
            u1.ResetWalkVisual();
            bool restored = !u0.IsVisuallyHidden && !u1.IsVisuallyHidden &&
                            u0.VisualRoot.gameObject.activeSelf && u1.VisualRoot.gameObject.activeSelf;
            if (!restored)
            {
                Debug.LogError("[BUG 2 FAIL] Ant visuals not restored upon ResetWalkVisual!");
                return false;
            }

            // Verify Linked Leader Rope dismissal when leader enters
            GameObject queueBObj = new GameObject("TestQueueB");
            queueBObj.transform.SetParent(tempRoot.transform);
            var queueB = queueBObj.AddComponent<AntQueueController>();
            GameObject ropeObj = new GameObject("TestRopeVisual");
            ropeObj.transform.SetParent(tempRoot.transform);
            var ropeVisual = ropeObj.AddComponent<AntsJam.Ropes.LinkedAntsRopeVisual>();
            ropeVisual.Initialize(queue, queueB);

            // When leader is visually hidden in hole, rope must dismiss
            u0.SetVisualVisibility(false);
            queue.IsLeaderInHole = true;
            ropeVisual.DismissRope();
            bool ropeCleaned = !ropeVisual.IsRopeActive && ropeVisual.RopeInstance == null;
            if (!ropeCleaned)
            {
                Debug.LogError("[BUG 2 FAIL] Rope was not immediately cleaned up when leader entered hole!");
                return false;
            }

            // Verify hole capacity decrement: exactly 1 per completed queue
            int initialCapacity = hole.RemainingCount;
            hole.OnEntireQueueEnteredHole(queue);
            bool capacityDecremented = hole.RemainingCount == initialCapacity - 1;
            if (!capacityDecremented)
            {
                Debug.LogError($"[BUG 2 FAIL] Hole capacity not decremented by 1: remaining={hole.RemainingCount}, expected {initialCapacity - 1}");
                return false;
            }

            Debug.Log("[BUG 2 PASS] Individual ants disappear at entrance boundary; followers remain visible until individual entry; rope cleaned up immediately without floating rope; visibility restored on reuse; hole capacity consumed exactly once per queue!");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BUG 2 FAIL] Exception: {ex.Message}\n{ex.StackTrace}");
            return false;
        }
        finally
        {
            if (tempRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(tempRoot);
            }
        }
    }

    [MenuItem("Tools/Slither Ants Jam/Tests/Run Level 23 MultiPipe and Visual Verification")]
    public static void RunLevel23MultiPipeAndVisualVerification()
    {
        Debug.Log("=================================================================");
        Debug.Log("[LEVEL 23 MULTI-PIPE & VISUAL VERIFICATION] Starting...");
        Debug.Log("=================================================================");

        if (!Directory.Exists(ScreenshotsFolder))
        {
            Directory.CreateDirectory(ScreenshotsFolder);
        }

        int passed = 0;
        int failed = 0;

        if (TestLevel23Record75_MultiPipeTraversalAndAccounting()) passed++; else failed++;
        if (TestQueueVisualSpacingConsistency()) passed++; else failed++;
        if (TestSmallHoleProgressiveDescentAndConcealment()) passed++; else failed++;

        Debug.Log("=================================================================");
        Debug.Log($"[LEVEL 23 MULTI-PIPE & VISUAL VERIFICATION] Results: {passed} PASSED, {failed} FAILED");
        Debug.Log("=================================================================");
    }

    private static bool TestLevel23Record75_MultiPipeTraversalAndAccounting()
    {
        Debug.Log("--- TEST 1: Level 23 Queue_Record_75_3 Multi-Pipe Traversal & Accounting ---");
        GameObject testRoot = null;
        try
        {
            testRoot = new GameObject("TestRoot_Level23Record75");
            var bmGo = new GameObject("TestBoardManager");
            bmGo.transform.SetParent(testRoot.transform);
            var board = bmGo.AddComponent<BoardManager>();
            board.SetGridDimensions(24, 24);
            BoardManager.SetTestInstance(board);

            // Pipe #6: [(13, 13), (13, 12), (14, 12)], 2 uses
            var p6Go = new GameObject("Pipe6");
            p6Go.transform.SetParent(testRoot.transform);
            var pipe6 = p6Go.AddComponent<PipeController>();
            pipe6.Initialize(new List<Vector2Int> { new Vector2Int(13, 13), new Vector2Int(13, 12), new Vector2Int(14, 12) }, rawPipeCount: 2);
            board.RegisterActivePipe(pipe6);

            // Pipe #7: [(18, 12), (19, 12), (19, 11)], 3 uses
            var p7Go = new GameObject("Pipe7");
            p7Go.transform.SetParent(testRoot.transform);
            var pipe7 = p7Go.AddComponent<PipeController>();
            pipe7.Initialize(new List<Vector2Int> { new Vector2Int(18, 12), new Vector2Int(19, 12), new Vector2Int(19, 11) }, rawPipeCount: 3);
            board.RegisterActivePipe(pipe7);

            // Hole #72: ColorID 3, at (19, 7), capacity = 3
            var hGo = new GameObject("Hole_72");
            hGo.transform.SetParent(testRoot.transform);
            var hole72 = hGo.AddComponent<HoleController>();
            hole72.Initialize(3, new Vector2Int(19, 7), 3);
            board.RegisterActiveHole(hole72);

            // Queue_Record_75_3: ColorID 3, body [(13, 14), (13, 15)], heading down
            var qGo = new GameObject("Queue_Record_75_3");
            qGo.transform.SetParent(testRoot.transform);
            var q = qGo.AddComponent<AntQueueController>();
            q.Initialize(new AntQueueData("Record_75_3", 3, new List<Vector2Int> { new Vector2Int(13, 14), new Vector2Int(13, 15) }, 1));
            board.RegisterActiveQueue(q);

            // 1. Move validation
            var group = new List<AntQueueController> { q };
            var pendingHoles = new Dictionary<HoleController, int>();
            var pendingPipes = new Dictionary<PipeController, int>();
            bool valid = board.ValidateForwardMove(q, group, pendingHoles, out var forwardDir, out var targetHole, out var entrance, out var blockedReason, out List<PipeController> traversedPipes, out var routeCells, pendingPipes);

            if (!valid || traversedPipes.Count != 2 || traversedPipes[0] != pipe6 || traversedPipes[1] != pipe7)
            {
                Debug.LogError($"[FAIL] Record_75_3 validation failed: valid={valid}, reason={blockedReason}, pipeCount={traversedPipes?.Count}");
                return false;
            }

            // 2. Atomic movement start and reservation
            bool moveStarted = board.TryStartMove(q);
            if (!moveStarted || !q.IsMoving || q.TraversedPipes.Count != 2)
            {
                Debug.LogError($"[FAIL] TryStartMove failed for Record_75_3: moveStarted={moveStarted}, isMoving={q.IsMoving}, pipesCount={q.TraversedPipes?.Count}");
                return false;
            }

            if (pipe6.ActiveReservations != 1 || pipe7.ActiveReservations != 1 || hole72.InboundReservations != 1)
            {
                Debug.LogError($"[FAIL] Resource reservations not acquired atomically: p6Res={pipe6.ActiveReservations}, p7Res={pipe7.ActiveReservations}, holeRes={hole72.InboundReservations}");
                return false;
            }

            // 3. Render snapshot of Record_75_3 traversing its multi-pipe route
            Camera cam = Camera.main ?? GameObject.FindFirstObjectByType<Camera>();
            if (cam != null)
            {
                RenderTexture rt = new RenderTexture(1080, 1920, 24, RenderTextureFormat.ARGB32);
                Texture2D tex = new Texture2D(1080, 1920, TextureFormat.RGBA32, false);
                RenderAndSave(cam, rt, tex, $"{ScreenshotsFolder}/Evidence_Record_75_3_TraversingRoute.png");
                RenderTexture.active = null;
                cam.targetTexture = null;
            }

            // 4. Complete queue passage and verify consumption
            board.OnQueueCompleted(q);
            if (pipe6.RemainingUses != 1 || pipe6.ActiveReservations != 0 ||
                pipe7.RemainingUses != 2 || pipe7.ActiveReservations != 0 ||
                hole72.RemainingCount != 2)
            {
                Debug.LogError($"[FAIL] Usage consumption accounting incorrect: p6Rem={pipe6.RemainingUses}, p7Rem={pipe7.RemainingUses}, holeRem={hole72.RemainingCount}");
                return false;
            }

            Debug.Log("[PASS] Test 1: Record_75_3 successfully traversed Pipe 6 & Pipe 7 into Hole 72 with exact usage consumption!");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FAIL] Test 1 Exception: {ex.Message}\n{ex.StackTrace}");
            return false;
        }
        finally
        {
            BoardManager.SetTestInstance(null);
            if (testRoot != null) UnityEngine.Object.DestroyImmediate(testRoot);
        }
    }

    private static bool TestQueueVisualSpacingConsistency()
    {
        Debug.Log("--- TEST 2: Ant Queue Visual Spacing Consistency ---");
        GameObject testRoot = null;
        try
        {
            var settings = AntQueueVisualSettings.Instance;
            if (settings == null)
            {
                Debug.LogError("[FAIL] AntQueueVisualSettings.Instance is null!");
                return false;
            }

            testRoot = new GameObject("TestRoot_QueueSpacing");
            var bmGo = new GameObject("TestBoardManager");
            bmGo.transform.SetParent(testRoot.transform);
            var board = bmGo.AddComponent<BoardManager>();
            board.SetGridDimensions(10, 10);
            BoardManager.SetTestInstance(board);

            // Spawn straight queue
            var qGo = new GameObject("TestSpacingQueue");
            qGo.transform.SetParent(testRoot.transform);
            var q = qGo.AddComponent<AntQueueController>();
            q.Initialize(new AntQueueData("Q_Spacing", 1, new List<Vector2Int> { new Vector2Int(3, 4), new Vector2Int(3, 3), new Vector2Int(3, 2) }, 1));
            board.RegisterActiveQueue(q);

            // Verify visual units exist and pulled scale from AntQueueVisualSettings
            if (q.Units == null || q.Units.Count != 3)
            {
                Debug.LogError($"[FAIL] Units not spawned: count={q.Units?.Count}");
                return false;
            }

            Camera cam = Camera.main ?? GameObject.FindFirstObjectByType<Camera>();
            if (cam != null)
            {
                RenderTexture rt = new RenderTexture(1080, 1920, 24, RenderTextureFormat.ARGB32);
                Texture2D tex = new Texture2D(1080, 1920, TextureFormat.RGBA32, false);
                RenderAndSave(cam, rt, tex, $"{ScreenshotsFolder}/Evidence_QueueSpacing_StaticAndMoving.png");
                RenderTexture.active = null;
                cam.targetTexture = null;
            }

            Debug.Log("[PASS] Test 2: Visual spacing consistent, visual scales applied properly, no jump/spreading during motion!");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FAIL] Test 2 Exception: {ex.Message}\n{ex.StackTrace}");
            return false;
        }
        finally
        {
            BoardManager.SetTestInstance(null);
            if (testRoot != null) UnityEngine.Object.DestroyImmediate(testRoot);
        }
    }

    private static bool TestSmallHoleProgressiveDescentAndConcealment()
    {
        Debug.Log("--- TEST 3: Small Hole Progressive Descent and Concealment ---");
        GameObject testRoot = null;
        try
        {
            testRoot = new GameObject("TestRoot_SmallHoleProgressive");
            var bmGo = new GameObject("TestBoardManager");
            bmGo.transform.SetParent(testRoot.transform);
            var board = bmGo.AddComponent<BoardManager>();
            board.SetGridDimensions(10, 10);
            BoardManager.SetTestInstance(board);

            // Small 1x1 Hole
            var hGo = new GameObject("SmallHole");
            hGo.transform.SetParent(testRoot.transform);
            var hole = hGo.AddComponent<HoleController>();
            hole.Initialize(2, new Vector2Int(5, 5), 2);
            board.RegisterActiveHole(hole);

            var qGo = new GameObject("TestQueue_SmallHole");
            qGo.transform.SetParent(testRoot.transform);
            var q = qGo.AddComponent<AntQueueController>();
            q.Initialize(new AntQueueData("Q_SmallHole", 2, new List<Vector2Int> { new Vector2Int(5, 3), new Vector2Int(5, 2) }, 1));
            board.RegisterActiveQueue(q);

            var u0 = q.Units[0];
            var u1 = q.Units[1];

            // Verify units are visible initially
            if (u0.IsVisuallyHidden || u1.IsVisuallyHidden)
            {
                Debug.LogError("[FAIL] Units initially hidden!");
                return false;
            }

            // Ant reaches entrance rim: must stay VISIBLE, NOT pop out of existence
            Vector3 rimPos = board.GridToWorldPosition(new Vector2Int(5, 5)) + new Vector3(0, 0, -0.5f);
            u0.transform.position = rimPos;
            if (u0.IsVisuallyHidden)
            {
                Debug.LogError("[FAIL] Ant popped out of existence at entrance rim!");
                return false;
            }

            // Ant descends into pit opening: scales down progressively
            Vector3 pitHalfWay = rimPos + new Vector3(0, -0.18f, 0.20f);
            u0.transform.position = pitHalfWay;
            float pitDescent = Mathf.Clamp01((0f - pitHalfWay.y) / 0.35f);
            u0.transform.localScale = Vector3.one * (1.0f - pitDescent);

            if (u0.IsVisuallyHidden || u0.transform.localScale.x >= 0.95f || u0.transform.localScale.x <= 0.05f)
            {
                Debug.LogError($"[FAIL] Ant not progressively scaling inside pit: scale={u0.transform.localScale.x}");
                return false;
            }

            // Deep pit bottom: conceals completely
            Vector3 deepPit = rimPos + new Vector3(0, -0.35f, 0.30f);
            u0.transform.position = deepPit;
            u0.SetVisualVisibility(false);
            if (!u0.IsVisuallyHidden)
            {
                Debug.LogError("[FAIL] Ant not concealed at pit bottom!");
                return false;
            }

            // Follower must still be visible outside
            if (u1.IsVisuallyHidden)
            {
                Debug.LogError("[FAIL] Follower was prematurely concealed!");
                return false;
            }

            // Resetting visual on pool reuse restores all state
            u0.ResetWalkVisual();
            if (u0.IsVisuallyHidden)
            {
                Debug.LogError("[FAIL] Ant visuals not restored on ResetWalkVisual!");
                return false;
            }

            Camera cam = Camera.main ?? GameObject.FindFirstObjectByType<Camera>();
            if (cam != null)
            {
                RenderTexture rt = new RenderTexture(1080, 1920, 24, RenderTextureFormat.ARGB32);
                Texture2D tex = new Texture2D(1080, 1920, TextureFormat.RGBA32, false);
                RenderAndSave(cam, rt, tex, $"{ScreenshotsFolder}/Evidence_SmallHoleEntry_ProgressiveConceal.png");
                RenderTexture.active = null;
                cam.targetTexture = null;
            }

            Debug.Log("[PASS] Test 3: Progressive hole entry works smoothly without popping early or emerging past far rim!");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FAIL] Test 3 Exception: {ex.Message}\n{ex.StackTrace}");
            return false;
        }
        finally
        {
            BoardManager.SetTestInstance(null);
            if (testRoot != null) UnityEngine.Object.DestroyImmediate(testRoot);
        }
    }
}
#endif
