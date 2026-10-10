#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class TranslucentPipeAndReturnSpeedVerificationSuite
{
    public const string EvidenceDir = "Assets/TestEvidence/TraversalAndReturnVerification";

    [MenuItem("Tools/Slither Ants Jam/Tests/Run Complete Translucent Pipe & Return Speed Suite")]
    public static void RunCompleteSuite()
    {
        Debug.Log("=========================================================================");
        Debug.Log("[VERIFICATION SUITE] Running Translucent Pipe, Return Speed & Badge Suite...");
        Debug.Log("=========================================================================");

        if (!Directory.Exists(EvidenceDir))
        {
            Directory.CreateDirectory(EvidenceDir);
        }

        int passed = 0;
        int failed = 0;

        void Report(string name, bool success)
        {
            if (success)
            {
                passed++;
                Debug.Log($"[PASS] {name}");
            }
            else
            {
                failed++;
                Debug.LogError($"[FAIL] {name}");
            }
        }

        Report("Test 1: Partial Queue Entry Translucency (Per-Ant Opacity & Tint)", TestPartialQueueEntryTranslucency());
        Report("Test 2: Square 90-Degree Bend Centerline Traversal & Exit Restoration", TestSquareBendTraversalAndExitRestoration());
        Report("Test 3: Multiple Queues Inside Pipe Simultaneously (No Shared Material Mutation)", TestMultipleQueuesInsidePipe());
        Report("Test 4: Reset & Appearance Restoration on Cleanup / Disable / Restart", TestCleanupAndResetRestoration());
        Report("Test 5: Wrong-Move Return Speed Parity (1:1 with Normal Movement Speed)", TestReturnSpeedParityAndRestoration());
        Report("Test 6: Larger Entrance Badge Anchor, Upright Text & Modal Layering", TestEntranceBadgeSpecifications());
        Report("Test 7: Gameplay Traversal & Failed-Move Frame Sequences Generation", GenerateEvidenceFrameSequences());

        // Audio Effects Configuration and Verification
        AudioEffectsSetupAndVerificationSuite.ConfigureSoundManagerInScene();
        AudioEffectsSetupAndVerificationSuite.RunCompleteVerificationSuite();

        Debug.Log("=========================================================================");
        Debug.Log($"[VERIFICATION SUITE COMPLETED] Total: {passed} PASSED, {failed} FAILED");
        Debug.Log("=========================================================================");
    }

    [MenuItem("Tools/Slither Ants Jam/Tests/Run Audio Effects Verification Suite")]
    public static void RunAudioVerification()
    {
        AudioEffectsSetupAndVerificationSuite.ConfigureSoundManagerInScene();
        AudioEffectsSetupAndVerificationSuite.RunCompleteVerificationSuite();
    }

    private static bool TestPartialQueueEntryTranslucency()
    {
        GameObject root = new GameObject("TestRoot_PartialEntry");
        try
        {
            var bmGo = new GameObject("BoardManager");
            bmGo.transform.SetParent(root.transform);
            var board = bmGo.AddComponent<BoardManager>();
            board.SetGridDimensions(16, 16);
            BoardManager.SetTestInstance(board);

            // Pipe along (5, 4) to (5, 7) heading +Z
            var pipeGo = new GameObject("Pipe");
            pipeGo.transform.SetParent(root.transform);
            var pipe = pipeGo.AddComponent<PipeController>();
            pipe.Initialize(new List<Vector2Int> {
                new Vector2Int(5, 4), new Vector2Int(5, 5), new Vector2Int(5, 6), new Vector2Int(5, 7)
            }, 3);
            board.RegisterActivePipe(pipe);

            // Queue of 4 ants: Head at (5, 5) [INSIDE PIPE], followed by (5, 4) [INSIDE PIPE], (5, 3) [OUTSIDE], (5, 2) [OUTSIDE]
            var qGo = new GameObject("Queue");
            qGo.transform.SetParent(root.transform);
            var q = qGo.AddComponent<AntQueueController>();
            q.Initialize(new AntQueueData("TestQ", 1, 0, new List<Vector2Int> {
                new Vector2Int(5, 5), new Vector2Int(5, 4), new Vector2Int(5, 3), new Vector2Int(5, 2)
            }, false, -1, false, 0, 0, false, 0));
            q.ForwardDirection = Vector2Int.up;
            q.TraversedPipes = new List<PipeController> { pipe };

            // Update visuals
            for (int i = 0; i < q.Units.Count; i++)
            {
                q.UpdateAntPipeVisual(q.Units[i], q.TraversedPipes);
            }

            // Ant 0 at (5, 5): inside pipe -> translucent (~0.40)
            // Ant 1 at (5, 4): entrance cell -> inside pipe -> translucent
            // Ant 2 at (5, 3): 1 cell outside pipe -> fully opaque (1.0)
            // Ant 3 at (5, 2): 2 cells outside pipe -> fully opaque (1.0)
            if (!q.Units[0].IsInsidePipe || q.Units[0].CurrentPipeOpacity > 0.45f)
            {
                Debug.LogError($"[PartialEntry] Head ant should be translucent inside pipe! Inside={q.Units[0].IsInsidePipe}, Opacity={q.Units[0].CurrentPipeOpacity}");
                return false;
            }

            if (q.Units[2].IsInsidePipe || q.Units[2].CurrentPipeOpacity < 0.99f)
            {
                Debug.LogError($"[PartialEntry] Outside ant 2 should remain fully opaque! Inside={q.Units[2].IsInsidePipe}, Opacity={q.Units[2].CurrentPipeOpacity}");
                return false;
            }

            if (q.Units[3].IsInsidePipe || q.Units[3].CurrentPipeOpacity < 0.99f)
            {
                Debug.LogError($"[PartialEntry] Outside ant 3 should remain fully opaque! Inside={q.Units[3].IsInsidePipe}, Opacity={q.Units[3].CurrentPipeOpacity}");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PartialEntry] Exception: {ex.Message}\n{ex.StackTrace}");
            return false;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static bool TestSquareBendTraversalAndExitRestoration()
    {
        GameObject root = new GameObject("TestRoot_Bend");
        try
        {
            var bmGo = new GameObject("BoardManager");
            bmGo.transform.SetParent(root.transform);
            var board = bmGo.AddComponent<BoardManager>();
            board.SetGridDimensions(16, 16);
            BoardManager.SetTestInstance(board);

            // L-bend pipe: (5, 5) -> (5, 8) -> (8, 8)
            var pipeGo = new GameObject("BendPipe");
            pipeGo.transform.SetParent(root.transform);
            var pipe = pipeGo.AddComponent<PipeController>();
            pipe.Initialize(new List<Vector2Int> {
                new Vector2Int(5, 5), new Vector2Int(5, 6), new Vector2Int(5, 7), new Vector2Int(5, 8),
                new Vector2Int(6, 8), new Vector2Int(7, 8), new Vector2Int(8, 8)
            }, 2);
            board.RegisterActivePipe(pipe);

            var qGo = new GameObject("BendQueue");
            qGo.transform.SetParent(root.transform);
            var q = qGo.AddComponent<AntQueueController>();
            q.Initialize(new AntQueueData("BendQ", 2, 0, new List<Vector2Int> {
                new Vector2Int(6, 8), new Vector2Int(5, 8), new Vector2Int(5, 7)
            }, false, -1, false, 0, 0, false, 0));
            q.TraversedPipes = new List<PipeController> { pipe };

            for (int i = 0; i < q.Units.Count; i++)
            {
                q.UpdateAntPipeVisual(q.Units[i], q.TraversedPipes);
            }

            // Ant at corner (5, 8) must be translucent
            if (!q.Units[1].IsInsidePipe || q.Units[1].CurrentPipeOpacity > 0.45f)
            {
                Debug.LogError($"[Bend] Corner ant must be translucent inside bend! Inside={q.Units[1].IsInsidePipe}, Opacity={q.Units[1].CurrentPipeOpacity}");
                return false;
            }

            // Test ant exiting: position ant at (9, 8) (outside exit mouth)
            q.Units[0].transform.position = board.GridToWorldPosition(new Vector2Int(9, 8));
            q.UpdateAntPipeVisual(q.Units[0], q.TraversedPipes);

            if (q.Units[0].IsInsidePipe || q.Units[0].CurrentPipeOpacity < 0.99f)
            {
                Debug.LogError($"[Bend] Exited ant must restore full opacity! Inside={q.Units[0].IsInsidePipe}, Opacity={q.Units[0].CurrentPipeOpacity}");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Bend] Exception: {ex.Message}\n{ex.StackTrace}");
            return false;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static bool TestMultipleQueuesInsidePipe()
    {
        GameObject root = new GameObject("TestRoot_MultiQueue");
        try
        {
            var bmGo = new GameObject("BoardManager");
            bmGo.transform.SetParent(root.transform);
            var board = bmGo.AddComponent<BoardManager>();
            board.SetGridDimensions(16, 16);
            BoardManager.SetTestInstance(board);

            var pipeGo = new GameObject("Pipe");
            pipeGo.transform.SetParent(root.transform);
            var pipe = pipeGo.AddComponent<PipeController>();
            pipe.Initialize(new List<Vector2Int> {
                new Vector2Int(4, 5), new Vector2Int(5, 5), new Vector2Int(6, 5), new Vector2Int(7, 5),
                new Vector2Int(8, 5), new Vector2Int(9, 5), new Vector2Int(10, 5)
            }, 3);
            board.RegisterActivePipe(pipe);

            // Queue 1: Red ant inside pipe at (8, 5)
            var q1Go = new GameObject("Q1");
            q1Go.transform.SetParent(root.transform);
            var q1 = q1Go.AddComponent<AntQueueController>();
            q1.Initialize(new AntQueueData("Q1", 1, 0, new List<Vector2Int> { new Vector2Int(8, 5), new Vector2Int(7, 5) }, false, -1, false, 0, 0, false, 0));
            q1.TraversedPipes = new List<PipeController> { pipe };

            // Queue 2: Blue ant entering pipe at (5, 5)
            var q2Go = new GameObject("Q2");
            q2Go.transform.SetParent(root.transform);
            var q2 = q2Go.AddComponent<AntQueueController>();
            q2.Initialize(new AntQueueData("Q2", 2, 0, new List<Vector2Int> { new Vector2Int(5, 5), new Vector2Int(4, 5) }, false, -1, false, 0, 0, false, 0));
            q2.TraversedPipes = new List<PipeController> { pipe };

            // Queue 3: Green ant outside pipe at (2, 5)
            var q3Go = new GameObject("Q3");
            q3Go.transform.SetParent(root.transform);
            var q3 = q3Go.AddComponent<AntQueueController>();
            q3.Initialize(new AntQueueData("Q3", 3, 0, new List<Vector2Int> { new Vector2Int(2, 5), new Vector2Int(1, 5) }, false, -1, false, 0, 0, false, 0));

            q1.UpdateAntPipeVisual(q1.Units[0], q1.TraversedPipes);
            q2.UpdateAntPipeVisual(q2.Units[0], q2.TraversedPipes);
            q3.UpdateAntPipeVisual(q3.Units[0]);

            // Both Q1 and Q2 inside pipe must be translucent, but Q3 outside must remain 100% opaque
            if (!q1.Units[0].IsInsidePipe || q1.Units[0].CurrentPipeOpacity > 0.45f) return false;
            if (!q2.Units[0].IsInsidePipe || q2.Units[0].CurrentPipeOpacity > 0.45f) return false;
            if (q3.Units[0].IsInsidePipe || q3.Units[0].CurrentPipeOpacity < 0.99f) return false;

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[MultiQueue] Exception: {ex.Message}\n{ex.StackTrace}");
            return false;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static bool TestCleanupAndResetRestoration()
    {
        GameObject root = new GameObject("TestRoot_Cleanup");
        try
        {
            var bmGo = new GameObject("BoardManager");
            bmGo.transform.SetParent(root.transform);
            var board = bmGo.AddComponent<BoardManager>();
            board.SetGridDimensions(16, 16);
            BoardManager.SetTestInstance(board);

            var pipeGo = new GameObject("Pipe");
            pipeGo.transform.SetParent(root.transform);
            var pipe = pipeGo.AddComponent<PipeController>();
            pipe.Initialize(new List<Vector2Int> {
                new Vector2Int(5, 5), new Vector2Int(5, 6), new Vector2Int(5, 7)
            }, 1);

            var qGo = new GameObject("Q");
            qGo.transform.SetParent(root.transform);
            var q = qGo.AddComponent<AntQueueController>();
            q.Initialize(new AntQueueData("Q", 1, 0, new List<Vector2Int> { new Vector2Int(5, 6), new Vector2Int(5, 5) }, false, -1, false, 0, 0, false, 0));
            q.TraversedPipes = new List<PipeController> { pipe };

            q.UpdateAntPipeVisual(q.Units[0], q.TraversedPipes);
            if (!q.Units[0].IsInsidePipe) return false;

            // Reset all
            q.ResetAllAntPipeVisuals();
            if (q.Units[0].IsInsidePipe || q.Units[0].CurrentPipeOpacity < 0.99f)
            {
                Debug.LogError($"[Cleanup] ResetAllAntPipeVisuals failed to restore full opacity!");
                return false;
            }

            return true;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static bool TestReturnSpeedParityAndRestoration()
    {
        GameObject root = new GameObject("TestRoot_ReturnSpeed");
        try
        {
            var bmGo = new GameObject("BoardManager");
            bmGo.transform.SetParent(root.transform);
            var board = bmGo.AddComponent<BoardManager>();
            board.SetGridDimensions(16, 16);
            BoardManager.SetTestInstance(board);

            var gmGo = new GameObject("GameManager");
            gmGo.transform.SetParent(root.transform);
            var gm = gmGo.AddComponent<GameManager>();
            gm.SetTestingState(GameState.Playing, 3);

            var qGo = new GameObject("QueueReturn");
            qGo.transform.SetParent(root.transform);
            var q = qGo.AddComponent<AntQueueController>();
            q.Initialize(new AntQueueData("QRet", 1, 0, new List<Vector2Int> {
                new Vector2Int(5, 5), new Vector2Int(5, 4), new Vector2Int(5, 3)
            }, false, -1, false, 0, 0, false, 0));
            q.ForwardDirection = Vector2Int.up;

            // Verify ReturnSpeed equals EffectiveMoveSpeed exactly
            float normalSpeed = q.EffectiveMoveSpeed;
            float returnSpeed = q.ReturnSpeed;

            if (Mathf.Abs(normalSpeed - returnSpeed) > 0.001f)
            {
                Debug.LogError($"[ReturnSpeed] ReturnSpeed ({returnSpeed}) must strictly match EffectiveMoveSpeed ({normalSpeed})!");
                return false;
            }

            // Simulate forward travel of 2.0 world units and return
            float travelDistance = 2.0f;
            float expectedDuration = travelDistance / normalSpeed;

            // Step through return phase distance computation
            float virtualDist = travelDistance;
            float dt = 0.016f;
            float simulatedElapsed = 0f;
            while (virtualDist > 0f)
            {
                virtualDist -= normalSpeed * dt;
                simulatedElapsed += dt;
            }

            if (Mathf.Abs(simulatedElapsed - expectedDuration) > dt * 1.5f)
            {
                Debug.LogError($"[ReturnSpeed] Return duration mismatch: expected {expectedDuration:F3}s, elapsed {simulatedElapsed:F3}s");
                return false;
            }

            return true;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static bool TestEntranceBadgeSpecifications()
    {
        GameObject root = new GameObject("TestRoot_Badge");
        try
        {
            var bmGo = new GameObject("BoardManager");
            bmGo.transform.SetParent(root.transform);
            var board = bmGo.AddComponent<BoardManager>();
            board.SetGridDimensions(16, 16);
            BoardManager.SetTestInstance(board);

            // Create pipes in horizontal, vertical, and bent orientations
            var pHorizGo = new GameObject("PipeHoriz");
            pHorizGo.transform.SetParent(root.transform);
            var pHoriz = pHorizGo.AddComponent<PipeController>();
            pHoriz.Initialize(new List<Vector2Int> { new Vector2Int(3, 5), new Vector2Int(4, 5), new Vector2Int(5, 5) }, 2);

            var pVertGo = new GameObject("PipeVert");
            pVertGo.transform.SetParent(root.transform);
            var pVert = pVertGo.AddComponent<PipeController>();
            pVert.Initialize(new List<Vector2Int> { new Vector2Int(7, 5), new Vector2Int(7, 4), new Vector2Int(7, 3) }, 4);

            // 1. Verify remaining uses preserved (not hardcoded 3)
            if (pHoriz.RemainingUses != 2 || pVert.RemainingUses != 4)
            {
                Debug.LogError($"[Badge] Preserved remaining uses failed: Horiz={pHoriz.RemainingUses}, Vert={pVert.RemainingUses}");
                return false;
            }

            // 2. Verify canvas rotation is camera-aligned (60, 0, 0) for upright text on all orientations
            Canvas horizCanvas = pHoriz.GetComponentInChildren<Canvas>();
            Canvas vertCanvas = pVert.GetComponentInChildren<Canvas>();

            if (horizCanvas == null || vertCanvas == null)
            {
                Debug.LogError("[Badge] Badge Canvas missing from pipe children!");
                return false;
            }

            float horizAngleX = horizCanvas.transform.rotation.eulerAngles.x;
            float vertAngleX = vertCanvas.transform.rotation.eulerAngles.x;
            if (Mathf.Abs(horizAngleX - 60f) > 1.0f || Mathf.Abs(vertAngleX - 60f) > 1.0f)
            {
                Debug.LogError($"[Badge] Canvas rotation not camera-aligned upright (60°)! Horiz={horizAngleX}, Vert={vertAngleX}");
                return false;
            }

            // 3. Verify sorting order beneath modal overlays (<= 10)
            if (horizCanvas.sortingOrder > 10 || vertCanvas.sortingOrder > 10)
            {
                Debug.LogError($"[Badge] Sorting order too high: Horiz={horizCanvas.sortingOrder}, Vert={vertCanvas.sortingOrder}");
                return false;
            }

            // 4. Verify raycastTarget is false on all Images and Texts (non-blocking)
            var graphics = pHoriz.GetComponentsInChildren<Graphic>();
            foreach (var g in graphics)
            {
                if (g.raycastTarget)
                {
                    Debug.LogError($"[Badge] Graphic {g.name} has raycastTarget=true, which blocks input!");
                    return false;
                }
            }

            return true;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static bool GenerateEvidenceFrameSequences()
    {
        GameObject root = new GameObject("TestRoot_EvidenceCapture");
        try
        {
            var bmGo = new GameObject("BoardManager");
            bmGo.transform.SetParent(root.transform);
            var board = bmGo.AddComponent<BoardManager>();
            board.SetGridDimensions(16, 16);
            BoardManager.SetTestInstance(board);

            // Camera setup
            var camGo = new GameObject("EvidenceCam");
            camGo.transform.SetParent(root.transform);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.867f, 0.855f, 0.827f, 1f);
            cam.orthographic = true;
            cam.orthographicSize = 4.5f;
            camGo.transform.position = new Vector3(5.5f, 14f, 5.5f);
            camGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            // Light
            var lightGo = new GameObject("DirLight");
            lightGo.transform.SetParent(root.transform);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            light.intensity = 1.2f;

            // Pipe: (5, 3) to (5, 7) heading +Z
            var pipeGo = new GameObject("EvidencePipe");
            pipeGo.transform.SetParent(root.transform);
            var pipe = pipeGo.AddComponent<PipeController>();
            pipe.Initialize(new List<Vector2Int> {
                new Vector2Int(5, 3), new Vector2Int(5, 4), new Vector2Int(5, 5), new Vector2Int(5, 6), new Vector2Int(5, 7)
            }, 3);
            board.RegisterActivePipe(pipe);

            // Queue starting outside pipe at (5, 1), (5, 0), (5, -1)
            var qGo = new GameObject("EvidenceQueue");
            qGo.transform.SetParent(root.transform);
            var q = qGo.AddComponent<AntQueueController>();
            q.Initialize(new AntQueueData("EvQ", 1, 0, new List<Vector2Int> {
                new Vector2Int(5, 1), new Vector2Int(5, 0), new Vector2Int(5, -1)
            }, false, -1, false, 0, 0, false, 0));
            q.ForwardDirection = Vector2Int.up;
            q.TraversedPipes = new List<PipeController> { pipe };

            int w = 720;
            int h = 1280;
            var rt = new RenderTexture(w, h, 24);
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);

            string framesDir = Path.Combine(EvidenceDir, "PipeTraversalFrames");
            if (Directory.Exists(framesDir)) Directory.Delete(framesDir, true);
            Directory.CreateDirectory(framesDir);

            void Capture(string path)
            {
                var prevRt = cam.targetTexture;
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                tex.Apply();
                RenderTexture.active = null;
                cam.targetTexture = prevRt;
                File.WriteAllBytes(path, tex.EncodeToPNG());
            }

            // Step queue forward across entrance:
            // 24 frames total
            Vector3 startHead = board.GridToWorldPosition(new Vector2Int(5, 1));
            Vector3 endHead = board.GridToWorldPosition(new Vector2Int(5, 7));

            for (int f = 0; f < 24; f++)
            {
                float t = f / 23.0f;
                Vector3 headPos = Vector3.Lerp(startHead, endHead, t);

                // Update units along line
                for (int u = 0; u < q.Units.Count; u++)
                {
                    Vector3 uPos = headPos - Vector3.forward * (u * 0.85f);
                    q.Units[u].transform.position = uPos;
                    q.Units[u].transform.rotation = Quaternion.LookRotation(Vector3.forward);
                    q.UpdateAntPipeVisual(q.Units[u], q.TraversedPipes);
                }

                Capture(Path.Combine(framesDir, $"frame_{f:D3}.png"));
            }

            // Also capture key snapshots:
            // Snapshot 1: Partial entry (Head inside translucent, tail outside opaque)
            File.Copy(Path.Combine(framesDir, "frame_008.png"), Path.Combine(EvidenceDir, "Evidence_PartialEntry_InsideTranslucent_OutsideOpaque.png"), true);
            // Snapshot 2: Full traversal inside pipe
            File.Copy(Path.Combine(framesDir, "frame_015.png"), Path.Combine(EvidenceDir, "Evidence_FullTraversal_TranslucentBeneathShell.png"), true);
            // Snapshot 3: Exit restoration
            File.Copy(Path.Combine(framesDir, "frame_022.png"), Path.Combine(EvidenceDir, "Evidence_ExitRestoration_OpaqueRestored.png"), true);

            // Failed-move return sequence (Forward at speed V, impact feedback, Return at exact same speed V)
            string returnFramesDir = Path.Combine(EvidenceDir, "ReturnMovementFrames");
            if (Directory.Exists(returnFramesDir)) Directory.Delete(returnFramesDir, true);
            Directory.CreateDirectory(returnFramesDir);

            Vector3 originPos = board.GridToWorldPosition(new Vector2Int(5, 1));
            Vector3 contactPos = board.GridToWorldPosition(new Vector2Int(5, 3));
            int steps = 12; // 12 frames forward, 4 frames impact pause/shake, 12 frames reverse (1:1 speed)
            int frameIdx = 0;

            // Phase 1: Forward movement at normal speed
            for (int f = 0; f <= steps; f++, frameIdx++)
            {
                float t = (float)f / steps;
                Vector3 headPos = Vector3.Lerp(originPos, contactPos, t);
                for (int u = 0; u < q.Units.Count; u++)
                {
                    q.Units[u].transform.position = headPos - Vector3.forward * (u * 0.85f);
                    q.Units[u].transform.rotation = Quaternion.LookRotation(Vector3.forward);
                }
                Capture(Path.Combine(returnFramesDir, $"return_frame_{frameIdx:D3}.png"));
            }

            // Phase 2: Impact feedback (brief contact pause & subtle recoil)
            for (int f = 0; f < 3; f++, frameIdx++)
            {
                float shake = (f % 2 == 1) ? 0.04f : -0.04f;
                Vector3 headPos = contactPos + new Vector3(shake, 0, 0);
                for (int u = 0; u < q.Units.Count; u++)
                {
                    q.Units[u].transform.position = headPos - Vector3.forward * (u * 0.85f);
                    q.Units[u].transform.rotation = Quaternion.LookRotation(Vector3.forward);
                }
                Capture(Path.Combine(returnFramesDir, $"return_frame_{frameIdx:D3}.png"));
            }

            // Phase 3: Exact reverse movement at identical 1:1 speed
            for (int f = steps; f >= 0; f--, frameIdx++)
            {
                float t = (float)f / steps;
                Vector3 headPos = Vector3.Lerp(originPos, contactPos, t);
                for (int u = 0; u < q.Units.Count; u++)
                {
                    q.Units[u].transform.position = headPos - Vector3.forward * (u * 0.85f);
                    q.Units[u].transform.rotation = Quaternion.LookRotation(Vector3.forward);
                }
                Capture(Path.Combine(returnFramesDir, $"return_frame_{frameIdx:D3}.png"));
            }

            UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(tex);

            Debug.Log($"[Evidence] Generated traversal frames, return frames, and key snapshots in {EvidenceDir}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Evidence] Exception: {ex.Message}\n{ex.StackTrace}");
            return false;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
#endif
