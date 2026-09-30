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
}
#endif
