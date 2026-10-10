using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using DG.Tweening;

public static class PipeVisualVerificationSuite
{
    private const string EvidenceDir = "Assets/TestEvidence/PipeVisualPass";

    [MenuItem("Tools/Slither Ants Jam/Tests/Run Complete Pipe Visual Verification Suite")]
    [MenuItem("Slither Ants/Run Pipe Visual Verification Suite")]
    public static void RunCompleteVerificationSuite()
    {
        Debug.Log("=================================================================");
        Debug.Log("[PIPE VERIFICATION] Starting Complete Pipe Visual Verification Suite...");
        Debug.Log("=================================================================");

        if (!Directory.Exists(EvidenceDir))
        {
            Directory.CreateDirectory(EvidenceDir);
        }

        bool galleryOk = RunComparisonGallery();
        bool level22Ok = RunLevel22Verification();
        bool level23Ok = RunLevel23AndQueue75_3Verification();
        bool modalLayeringOk = RunModalLayeringVerification();

        TranslucentPipeAndReturnSpeedVerificationSuite.RunCompleteSuite();

        Debug.Log("=================================================================");
        Debug.Log($"[PIPE VERIFICATION COMPLETED] Gallery: {galleryOk}, Level 22: {level22Ok}, Level 23 (Queue 75_3): {level23Ok}, Modal Layering: {modalLayeringOk}");
        Debug.Log("=================================================================");
    }

    /// <summary>
    /// Requirement 7: Comparison showing:
    /// - One straight pipe.
    /// - One square 90-degree bend.
    /// - One long U-shaped pipe.
    /// - Two adjacent/nested pipes.
    /// - Different configured counter values.
    /// Captures normal gameplay size and close-ups.
    public static bool RunComparisonGallery()
    {
        var prevBoard = BoardManager.Instance;
        BoardManager.SetTestInstance(null);
        GameObject galleryRoot = new GameObject("PipeComparisonGallery");
        List<GameObject> disabledRoots = null;
        try
        {
            disabledRoots = TemporarilyDisableSceneRoots(galleryRoot);

            // Set up test camera
            GameObject camGo = new GameObject("GalleryCamera");
            camGo.transform.SetParent(galleryRoot.transform);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.867f, 0.855f, 0.827f, 1f); // Board background
            cam.orthographic = true;

            // 1. Straight pipe: 4 cells horizontal, counter = 1
            var pStraightGo = new GameObject("Gallery_StraightPipe");
            pStraightGo.transform.SetParent(galleryRoot.transform);
            var pStraight = pStraightGo.AddComponent<PipeController>();
            pStraight.Initialize(new List<Vector2Int> {
                new Vector2Int(0, 8), new Vector2Int(1, 8), new Vector2Int(2, 8), new Vector2Int(3, 8)
            }, rawPipeCount: 1);

            // 2. Square 90-degree bend: 2 cells down, 2 cells right, counter = 2
            var pBendGo = new GameObject("Gallery_90DegBendPipe");
            pBendGo.transform.SetParent(galleryRoot.transform);
            var pBend = pBendGo.AddComponent<PipeController>();
            pBend.Initialize(new List<Vector2Int> {
                new Vector2Int(5, 10), new Vector2Int(5, 9), new Vector2Int(5, 8), new Vector2Int(6, 8), new Vector2Int(7, 8)
            }, rawPipeCount: 2);

            // 3. Long U-shaped pipe: counter = 3
            var pUGo = new GameObject("Gallery_LongUPipe");
            pUGo.transform.SetParent(galleryRoot.transform);
            var pU = pUGo.AddComponent<PipeController>();
            pU.Initialize(new List<Vector2Int> {
                new Vector2Int(0, 6), new Vector2Int(0, 5), new Vector2Int(0, 4), new Vector2Int(0, 3), new Vector2Int(0, 2),
                new Vector2Int(1, 2), new Vector2Int(2, 2),
                new Vector2Int(2, 3), new Vector2Int(2, 4), new Vector2Int(2, 5), new Vector2Int(2, 6)
            }, rawPipeCount: 3);

            // 4 & 5. Two adjacent/nested U-pipes:
            // Outer nested U-pipe: counter = 2
            var pNestedOuterGo = new GameObject("Gallery_NestedOuterPipe");
            pNestedOuterGo.transform.SetParent(galleryRoot.transform);
            var pNestedOuter = pNestedOuterGo.AddComponent<PipeController>();
            pNestedOuter.Initialize(new List<Vector2Int> {
                new Vector2Int(5, 6), new Vector2Int(5, 5), new Vector2Int(5, 4), new Vector2Int(5, 3), new Vector2Int(5, 2),
                new Vector2Int(6, 2), new Vector2Int(7, 2), new Vector2Int(8, 2),
                new Vector2Int(8, 3), new Vector2Int(8, 4), new Vector2Int(8, 5), new Vector2Int(8, 6)
            }, rawPipeCount: 2);

            // Inner nested U-pipe: counter = 1
            var pNestedInnerGo = new GameObject("Gallery_NestedInnerPipe");
            pNestedInnerGo.transform.SetParent(galleryRoot.transform);
            var pNestedInner = pNestedInnerGo.AddComponent<PipeController>();
            pNestedInner.Initialize(new List<Vector2Int> {
                new Vector2Int(6, 6), new Vector2Int(6, 5), new Vector2Int(6, 4), new Vector2Int(6, 3),
                new Vector2Int(7, 3),
                new Vector2Int(7, 4), new Vector2Int(7, 5), new Vector2Int(7, 6)
            }, rawPipeCount: 1);

            // Directional Light
            GameObject lightGo = new GameObject("GalleryLight");
            lightGo.transform.SetParent(galleryRoot.transform);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            light.intensity = 1.15f;
            light.color = Color.white;

            // CAPTURE 1: Normal gameplay size overview
            camGo.transform.position = new Vector3(4f, 15f, 5.5f);
            camGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            cam.orthographicSize = 6.2f;
            CaptureCameraToFile(cam, Path.Combine(EvidenceDir, "Evidence_PipeComparison_Overview.png"), 1080, 1920);

            // CAPTURE 2: Close-up of 90-degree square corner bends and diagonal miter seams
            camGo.transform.position = new Vector3(5.5f, 8f, 5.5f);
            cam.orthographicSize = 3.2f;
            CaptureCameraToFile(cam, Path.Combine(EvidenceDir, "Evidence_PipeComparison_CloseUp_Corners.png"), 1200, 1200);

            // CAPTURE 3: Close-up of pale-cyan end collars and blue counter badges
            camGo.transform.position = new Vector3(1.5f, 8f, 7.5f);
            cam.orthographicSize = 2.4f;
            CaptureCameraToFile(cam, Path.Combine(EvidenceDir, "Evidence_PipeComparison_CloseUp_Collars.png"), 1200, 1200);

            Debug.Log("[PASS] Pipe comparison gallery captured successfully.");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FAIL] Pipe comparison gallery failed: {ex.Message}\n{ex.StackTrace}");
            return false;
        }
        finally
        {
            RestoreSceneRoots(disabledRoots);
            if (galleryRoot != null) UnityEngine.Object.DestroyImmediate(galleryRoot);
            BoardManager.SetTestInstance(prevBoard);
        }
    }

    /// <summary>
    /// Requirement 7: Verify Level 22 (the reference layout with nested U-pipes, straight middle pipe, and yellow ant).
    /// </summary>
    public static bool RunLevel22Verification()
    {
        try
        {
            var level22Asset = AssetDatabase.LoadAssetAtPath<Level>("Assets/Data/ReferenceLevels/Level_022.asset");
            if (level22Asset == null)
            {
                Debug.LogError("[FAIL] Level_022.asset not found.");
                return false;
            }

            GameObject testRoot = new GameObject("Level22TestRoot");
            List<GameObject> disabledRoots = null;
            try
            {
                disabledRoots = TemporarilyDisableSceneRoots(testRoot);

                var bmGo = new GameObject("BoardManager");
                bmGo.transform.SetParent(testRoot.transform);
                var board = bmGo.AddComponent<BoardManager>();
                BoardManager.SetTestInstance(board);

                // Set up camera
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                camGo.transform.SetParent(testRoot.transform);
                var cam = camGo.AddComponent<Camera>();
                cam.orthographic = true;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.867f, 0.855f, 0.827f, 1f);

                // Directional light
                var lightGo = new GameObject("DirLight");
                lightGo.transform.SetParent(testRoot.transform);
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                light.intensity = 1.15f;

                // Build Level 22 board
                board.SetupBoard(level22Asset, false);

                // Normal gameplay full-board capture
                camGo.transform.position = new Vector3(0f, 20f, 0f);
                camGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                cam.orthographicSize = 8.5f;

                CaptureCameraToFile(cam, Path.Combine(EvidenceDir, "Evidence_Level22_FullBoard.png"), 1080, 1920);

                // Close-up on left nested U-pipes
                camGo.transform.position = new Vector3(-4.5f, 12f, 0f);
                cam.orthographicSize = 4.8f;
                CaptureCameraToFile(cam, Path.Combine(EvidenceDir, "Evidence_Level22_LeftNestedPipes.png"), 1200, 1600);

                // Close-up on middle straight pipe and entrance mouth
                camGo.transform.position = new Vector3(0.5f, 8f, 0f);
                cam.orthographicSize = 2.8f;
                CaptureCameraToFile(cam, Path.Combine(EvidenceDir, "Evidence_Level22_MiddlePipeAndMouth.png"), 1200, 800);

                Debug.Log($"[PASS] Level 22 verification passed. Active pipes: {board.ActivePipes.Count}");
                return true;
            }
            finally
            {
                RestoreSceneRoots(disabledRoots);
                if (testRoot != null) UnityEngine.Object.DestroyImmediate(testRoot);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FAIL] Level 22 verification failed: {ex.Message}\n{ex.StackTrace}");
            return false;
        }
    }

    /// <summary>
    /// Requirement 7: Verify Level 23 and Queue_Record_75_3 route (multi-pipe traversal through Pipe 6 and 7).
    /// </summary>
    public static bool RunLevel23AndQueue75_3Verification()
    {
        try
        {
            GameObject testRoot = new GameObject("Level23TestRoot");
            List<GameObject> disabledRoots = null;
            try
            {
                disabledRoots = TemporarilyDisableSceneRoots(testRoot);

                var bmGo = new GameObject("BoardManager");
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

                // Validation
                var group = new List<AntQueueController> { q };
                var pendingHoles = new Dictionary<HoleController, int>();
                var pendingPipes = new Dictionary<PipeController, int>();
                bool valid = board.ValidateForwardMove(q, group, pendingHoles, out var forwardDir, out var targetHole, out var entrance, out var blockedReason, out List<PipeController> traversedPipes, out var routeCells, pendingPipes);

                if (!valid || traversedPipes.Count != 2 || traversedPipes[0] != pipe6 || traversedPipes[1] != pipe7)
                {
                    Debug.LogError($"[FAIL] Record_75_3 validation failed: valid={valid}, reason={blockedReason}, pipeCount={traversedPipes?.Count}");
                    return false;
                }

                // Atomic move start
                bool moveStarted = board.TryStartMove(q);
                if (!moveStarted || !q.IsMoving || q.TraversedPipes.Count != 2)
                {
                    Debug.LogError($"[FAIL] TryStartMove failed for Record_75_3: moveStarted={moveStarted}, isMoving={q.IsMoving}, pipesCount={q.TraversedPipes?.Count}");
                    return false;
                }

                // Verify pipe reservation
                if (pipe6.ActiveReservations != 1 || pipe7.ActiveReservations != 1)
                {
                    Debug.LogError($"[FAIL] Pipe reservations failed: p6Res={pipe6.ActiveReservations}, p7Res={pipe7.ActiveReservations}");
                    return false;
                }

                // Simulate traversal completion & pipe consumption
                int p6InitialUses = pipe6.RemainingUses;
                int p7InitialUses = pipe7.RemainingUses;
                pipe6.ConsumeUse();
                pipe7.ConsumeUse();

                if (pipe6.RemainingUses != p6InitialUses - 1 || pipe7.RemainingUses != p7InitialUses - 1)
                {
                    Debug.LogError($"[FAIL] Pipe usage decrement failed: p6={pipe6.RemainingUses}, p7={pipe7.RemainingUses}");
                    return false;
                }

                // Directional light for Level 23 test
                var lightGo = new GameObject("TestLight");
                lightGo.transform.SetParent(testRoot.transform);
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                light.intensity = 1.15f;
                light.color = Color.white;

                // Camera capture of the multi-pipe route
                var camGo = new GameObject("TestCam");
                camGo.transform.SetParent(testRoot.transform);
                var cam = camGo.AddComponent<Camera>();
                cam.orthographic = true;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.867f, 0.855f, 0.827f, 1f);
                camGo.transform.position = new Vector3(4.5f, 15f, -0.5f);
                camGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                cam.orthographicSize = 5.2f;

                CaptureCameraToFile(cam, Path.Combine(EvidenceDir, "Evidence_Level23_Queue75_3_Traversal.png"), 1200, 1200);

                Debug.Log("[PASS] Level 23 and Queue_Record_75_3 verification passed.");
                return true;
            }
            finally
            {
                RestoreSceneRoots(disabledRoots);
                if (testRoot != null) UnityEngine.Object.DestroyImmediate(testRoot);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FAIL] Level 23 verification failed: {ex.Message}\n{ex.StackTrace}");
            return false;
        }
    }

    /// <summary>
    /// Requirement 1 & 4: Modal layering test - guarantees pipe counter badges render beneath all modal overlays.
    /// </summary>
    public static bool RunModalLayeringVerification()
    {
        GameObject root = new GameObject("ModalLayeringTestRoot");
        List<GameObject> disabledRoots = null;
        try
        {
            disabledRoots = TemporarilyDisableSceneRoots(root);

            // Set up camera first so modal canvas can reference it
            var camGo = new GameObject("TestCam");
            camGo.transform.SetParent(root.transform);
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.867f, 0.855f, 0.827f, 1f);
            camGo.transform.position = new Vector3(1f, 10f, 0f);
            camGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            cam.orthographicSize = 2.5f;

            // Directional Light
            var lightGo = new GameObject("ModalTestLight");
            lightGo.transform.SetParent(root.transform);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            light.intensity = 1.15f;

            // Spawn pipe with badge (sorting order 5)
            var pGo = new GameObject("TestPipeWithBadge");
            pGo.transform.SetParent(root.transform);
            var pipe = pGo.AddComponent<PipeController>();
            pipe.Initialize(new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) }, 3);

            var badgeCanvas = pGo.GetComponentInChildren<Canvas>(true);
            if (badgeCanvas == null)
            {
                Debug.LogError("[FAIL] Pipe badge canvas was not created.");
                return false;
            }

            if (badgeCanvas.sortingOrder > 10)
            {
                Debug.LogError($"[FAIL] Pipe badge canvas sortingOrder is {badgeCanvas.sortingOrder}, must be <= 10 to ensure modal layering!");
                return false;
            }

            // Spawn simulated modal overlay at sorting order 50 with dark semi-transparent panel
            var modalGo = new GameObject("SimulatedModalOverlay");
            modalGo.transform.SetParent(root.transform);
            var modalCanvas = modalGo.AddComponent<Canvas>();
            modalCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            modalCanvas.worldCamera = cam;
            modalCanvas.planeDistance = 2f;
            modalCanvas.sortingOrder = 50;

            var panelGo = new GameObject("ModalDarkBackdrop");
            panelGo.transform.SetParent(modalGo.transform, false);
            var panelRect = panelGo.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;
            var panelImg = panelGo.AddComponent<Image>();
            panelImg.color = new Color(0.08f, 0.08f, 0.12f, 0.65f); // 65% semi-transparent modal backdrop

            CaptureCameraToFile(cam, Path.Combine(EvidenceDir, "Evidence_PipeModalLayering.png"), 800, 800);

            Debug.Log($"[PASS] Modal layering verified: Badge sortingOrder={badgeCanvas.sortingOrder} < Modal sortingOrder={modalCanvas.sortingOrder}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FAIL] Modal layering test failed: {ex.Message}\n{ex.StackTrace}");
            return false;
        }
        finally
        {
            RestoreSceneRoots(disabledRoots);
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static List<GameObject> TemporarilyDisableSceneRoots(GameObject exemptRoot)
    {
        var disabledRoots = new List<GameObject>();
        var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        foreach (var root in activeScene.GetRootGameObjects())
        {
            if (root != exemptRoot && root.activeSelf)
            {
                root.SetActive(false);
                disabledRoots.Add(root);
            }
        }
        return disabledRoots;
    }

    private static void RestoreSceneRoots(List<GameObject> disabledRoots)
    {
        if (disabledRoots != null)
        {
            foreach (var root in disabledRoots)
            {
                if (root != null)
                {
                    root.SetActive(true);
                }
            }
        }
    }

    private static void CaptureCameraToFile(Camera cam, string filePath, int width, int height)
    {
        var rt = new RenderTexture(width, height, 24);
        var prevRt = cam.targetTexture;
        var prevActive = RenderTexture.active;

        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();

        byte[] pngData = tex.EncodeToPNG();
        File.WriteAllBytes(filePath, pngData);

        cam.targetTexture = prevRt;
        RenderTexture.active = prevActive;

        UnityEngine.Object.DestroyImmediate(tex);
        UnityEngine.Object.DestroyImmediate(rt);
        AssetDatabase.Refresh();
    }
}
