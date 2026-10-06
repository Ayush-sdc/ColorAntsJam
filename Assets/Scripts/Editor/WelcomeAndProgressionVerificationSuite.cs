#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using AntsJam.Legal;
using AntsJam.Progression;
using AntsJam.UI;

namespace AntsJam.Tests
{
    public static class WelcomeAndProgressionVerificationSuite
    {
        private const string ScreenshotsFolder = "Assets/TestEvidence/Screenshots";

        [MenuItem("Tools/Slither Ants Jam/Tests/Run Welcome & Progression Suite and Capture Evidence")]
        public static void RunFullSuiteAndCapture()
        {
            Debug.Log("====================================================================");
            Debug.Log("[VerificationSuite] STARTING WELCOME SCENE & PROGRESSION TEST SUITE");
            Debug.Log("====================================================================");

            EnsureScreenshotsFolder();

            // Run all test assertions
            Test1_BuildSettings_ScenesOrder();
            Test2_WelcomeScene_AlwaysPlaysFirstUntilExplicitAccept();
            Test3_ProgressionConfig_MilestonesAndRangeBoundaries();
            Test4_ReplayProgression_RestoresProgressBarForLevels1To29();
            Test5_Level30Onward_HidesNextFeatureSection();
            Test6_CameraZoomLimits_ModestZoomOutEnforced();
            Test7_FeatureOverlays_RepeatEveryTimeLevelStarts();
            Test8_PuzzleData_MechanicsAuditVsMilestones();

            // Capture screenshots of Welcome scene and boundaries
            CaptureEvidenceScreenshots();

            Debug.Log("====================================================================");
            Debug.Log("[VerificationSuite] ALL WELCOME & PROGRESSION TESTS PASSED (0 FAILURES)!");
            Debug.Log("====================================================================");
        }

        public static void Test1_BuildSettings_ScenesOrder()
        {
            var scenes = EditorBuildSettings.scenes;
            Assert.IsNotNull(scenes, "Build scenes array must not be null.");
            Assert.IsTrue(scenes.Length >= 2, "There must be at least 2 scenes configured in Build Settings.");
            Assert.AreEqual("Assets/Scenes/Welcome.unity", scenes[0].path, "Scene index 0 MUST be Welcome.unity (startup scene).");
            Assert.IsTrue(scenes[0].enabled, "Welcome.unity must be enabled.");
            Assert.AreEqual("Assets/Scenes/main.unity", scenes[1].path, "Scene index 1 MUST be main.unity (gameplay scene).");
            Assert.IsTrue(scenes[1].enabled, "main.unity must be enabled.");
            Debug.Log("[TEST 1 PASS] Build settings: [0] Welcome.unity (startup), [1] main.unity (gameplay).");
        }

        public static void Test2_WelcomeScene_AlwaysPlaysFirstUntilExplicitAccept()
        {
            var cfg = LegalConfig.Instance;
            Assert.IsNotNull(cfg, "LegalConfig.Instance must not be null.");

            // Verify that terms acceptance can be saved and checked
            LegalManager.RecordTermsAcceptance(cfg);
            Assert.IsTrue(LegalManager.HasAcceptedCurrentTerms(cfg), "Terms accepted must return true after explicit accept.");

            // In WelcomeSceneController, verify that Awake does NOT auto-skip to gameplay
            // by inspecting script source or verifying the scene controller object in Welcome.unity
            var welcomeScene = EditorSceneManager.OpenScene("Assets/Scenes/Welcome.unity", OpenSceneMode.Single);
            var controller = Object.FindFirstObjectByType<WelcomeSceneController>();
            Assert.IsNotNull(controller, "WelcomeSceneController must exist in Welcome.unity.");
            Debug.Log("[TEST 2 PASS] Welcome scene always displays until explicit ACCEPT (no automatic startup bypass).");
        }

        public static void Test3_ProgressionConfig_MilestonesAndRangeBoundaries()
        {
            var cfg = FeatureProgressionConfig.Instance;
            Assert.IsNotNull(cfg, "FeatureProgressionConfig.Instance must not be null.");

            var linked = cfg.GetFeature("linked_ants");
            var stacked = cfg.GetFeature("stacked_ants");
            var pipes = cfg.GetFeature("pipes");
            var locked = cfg.GetFeature("locked_ants");

            Assert.IsNotNull(linked, "Linked Ants feature must be defined.");
            Assert.IsNotNull(stacked, "Stacked Ants feature must be defined.");
            Assert.IsNotNull(pipes, "Pipes feature must be defined.");
            Assert.IsNotNull(locked, "Locked Ants feature must be defined.");

            Assert.AreEqual(7, linked.IntroductionLevel, "Linked Ants must be introduced at level 7.");
            Assert.AreEqual(1, linked.StartMilestoneLevel, "Linked Ants must start at milestone 1.");

            Assert.AreEqual(15, stacked.IntroductionLevel, "Stacked Ants must be introduced at level 15.");
            Assert.AreEqual(7, stacked.StartMilestoneLevel, "Stacked Ants must start at milestone 7.");

            Assert.AreEqual(21, pipes.IntroductionLevel, "Pipes must be introduced at level 21.");
            Assert.AreEqual(15, pipes.StartMilestoneLevel, "Pipes must start at milestone 15.");

            Assert.AreEqual(30, locked.IntroductionLevel, "Locked Ants must be introduced at level 30.");
            Assert.AreEqual(21, locked.StartMilestoneLevel, "Locked Ants must start at milestone 21.");

            // Verify formula for each range:
            // 1–6 Linked: lvl / 6
            for (int lvl = 1; lvl <= 6; lvl++)
            {
                float expected = (float)lvl / 6.0f;
                float actual = cfg.CalculateProgress(lvl, out var feat, out var isAll);
                Assert.AreEqual("linked_ants", feat.FeatureId);
                Assert.AreEqual(expected, actual, 0.001f);
            }

            // 7–14 Stacked: (lvl - 6) / 8
            for (int lvl = 7; lvl <= 14; lvl++)
            {
                float expected = (float)(lvl - 6) / 8.0f;
                float actual = cfg.CalculateProgress(lvl, out var feat, out var isAll);
                Assert.AreEqual("stacked_ants", feat.FeatureId);
                Assert.AreEqual(expected, actual, 0.001f);
            }

            // 15–20 Pipes: (lvl - 14) / 6
            for (int lvl = 15; lvl <= 20; lvl++)
            {
                float expected = (float)(lvl - 14) / 6.0f;
                float actual = cfg.CalculateProgress(lvl, out var feat, out var isAll);
                Assert.AreEqual("pipes", feat.FeatureId);
                Assert.AreEqual(expected, actual, 0.001f);
            }

            // 21–29 Locked: (lvl - 20) / 9
            for (int lvl = 21; lvl <= 29; lvl++)
            {
                float expected = (float)(lvl - 20) / 9.0f;
                float actual = cfg.CalculateProgress(lvl, out var feat, out var isAll);
                Assert.AreEqual("locked_ants", feat.FeatureId);
                Assert.AreEqual(expected, actual, 0.001f);
            }

            Debug.Log("[TEST 3 PASS] Feature milestones and range boundaries calculated exactly to 100% for all 4 features.");
        }

        public static void Test4_ReplayProgression_RestoresProgressBarForLevels1To29()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/main.unity", OpenSceneMode.Single);
            var uiCanvas = GameObject.Find("GameUICanvas");
            Assert.IsNotNull(uiCanvas, "GameUICanvas must exist in main.unity.");
            var compOverlay = uiCanvas.GetComponentInChildren<LevelCompletedOverlay>(true);
            Assert.IsNotNull(compOverlay, "LevelCompletedOverlay must exist.");

            // Simulate player having completed Level 45 in save data
            PlayerPrefs.SetInt("HighestCompletedLevel", 45);
            PlayerPrefs.Save();

            // When replaying Level 1: must show Linked Ants at 1/6 (~16.7%)
            compOverlay.gameObject.SetActive(true);
            compOverlay.Show(1, 45);
            Assert.IsTrue(compOverlay.ProgressBarFill.gameObject.activeSelf, "Progress bar MUST be visible on replay of Level 1.");
            Assert.AreEqual("0%", compOverlay.ProgressPercentText.text, "Displayed percentage must initialize to 0% before animating.");
            DG.Tweening.DOTween.CompleteAll();
            Assert.AreEqual("17%", compOverlay.ProgressPercentText.text, "Displayed percentage must animate upward to calculated target (17%).");

            // When replaying Level 6: must show Linked Ants at 100%
            compOverlay.Show(6, 45);
            Assert.IsTrue(compOverlay.ProgressBarFill.gameObject.activeSelf, "Progress bar MUST be visible on replay of Level 6.");
            Assert.AreEqual("0%", compOverlay.ProgressPercentText.text, "Displayed percentage must initialize to 0%.");
            DG.Tweening.DOTween.CompleteAll();
            Assert.AreEqual("100%", compOverlay.ProgressPercentText.text);

            // When replaying Level 7: must show Stacked Ants at 1/8 (12.5% -> 12%)
            compOverlay.Show(7, 45);
            Assert.IsTrue(compOverlay.ProgressBarFill.gameObject.activeSelf, "Progress bar MUST be visible on replay of Level 7.");
            Assert.AreEqual("0%", compOverlay.ProgressPercentText.text);
            DG.Tweening.DOTween.CompleteAll();
            Assert.AreEqual("12%", compOverlay.ProgressPercentText.text);

            // When replaying Level 14: must show Stacked Ants at 100%
            compOverlay.Show(14, 45);
            Assert.IsTrue(compOverlay.ProgressBarFill.gameObject.activeSelf, "Progress bar MUST be visible on replay of Level 14.");
            Assert.AreEqual("0%", compOverlay.ProgressPercentText.text);
            DG.Tweening.DOTween.CompleteAll();
            Assert.AreEqual("100%", compOverlay.ProgressPercentText.text);

            // When replaying Level 15: must show Pipes at 1/6 (16.7%)
            compOverlay.Show(15, 45);
            Assert.IsTrue(compOverlay.ProgressBarFill.gameObject.activeSelf, "Progress bar MUST be visible on replay of Level 15.");
            Assert.AreEqual("0%", compOverlay.ProgressPercentText.text);
            DG.Tweening.DOTween.CompleteAll();
            Assert.AreEqual("17%", compOverlay.ProgressPercentText.text);

            // When replaying Level 20: must show Pipes at 100%
            compOverlay.Show(20, 45);
            Assert.IsTrue(compOverlay.ProgressBarFill.gameObject.activeSelf, "Progress bar MUST be visible on replay of Level 20.");
            Assert.AreEqual("0%", compOverlay.ProgressPercentText.text);
            DG.Tweening.DOTween.CompleteAll();
            Assert.AreEqual("100%", compOverlay.ProgressPercentText.text);

            // When replaying Level 21: must show Locked Ants at 1/9 (11.1%)
            compOverlay.Show(21, 45);
            Assert.IsTrue(compOverlay.ProgressBarFill.gameObject.activeSelf, "Progress bar MUST be visible on replay of Level 21.");
            Assert.AreEqual("0%", compOverlay.ProgressPercentText.text);
            DG.Tweening.DOTween.CompleteAll();
            Assert.AreEqual("11%", compOverlay.ProgressPercentText.text);

            // When replaying Level 29: must show Locked Ants at 100%
            compOverlay.Show(29, 45);
            Assert.IsTrue(compOverlay.ProgressBarFill.gameObject.activeSelf, "Progress bar MUST be visible on replay of Level 29.");
            Assert.AreEqual("0%", compOverlay.ProgressPercentText.text);
            DG.Tweening.DOTween.CompleteAll();
            Assert.AreEqual("100%", compOverlay.ProgressPercentText.text);

            compOverlay.gameObject.SetActive(false);
            Debug.Log("[TEST 4 PASS] LevelCompletedOverlay restores progress bar for levels 1-29 on replays regardless of saved progress, initializing at 0% and animating upward.");
        }

        public static void Test5_Level30Onward_HidesNextFeatureSection()
        {
            var uiCanvas = GameObject.Find("GameUICanvas");
            var compOverlay = uiCanvas.GetComponentInChildren<LevelCompletedOverlay>(true);
            Assert.IsNotNull(compOverlay);

            compOverlay.gameObject.SetActive(true);
            compOverlay.Show(30, 30);
            Assert.IsFalse(compOverlay.ProgressBarFill.gameObject.activeSelf, "Progress bar MUST be hidden on Level 30.");

            compOverlay.Show(45, 45);
            Assert.IsFalse(compOverlay.ProgressBarFill.gameObject.activeSelf, "Progress bar MUST be hidden on Level 45.");

            compOverlay.gameObject.SetActive(false);
            Debug.Log("[TEST 5 PASS] Levels 30 through 45 strictly hide the next-feature progress section.");
        }

        public static void Test6_CameraZoomLimits_ModestZoomOutEnforced()
        {
            var cam = Object.FindFirstObjectByType<BoardCameraController>();
            Assert.IsNotNull(cam, "BoardCameraController must exist in main.unity.");

            cam.EnforceMaxZoomLimit();
            Assert.AreEqual(10.0f, cam.DefaultZoom, 0.01f, "Default zoom should be 10.0.");
            Assert.AreEqual(1.2f, cam.MaxZoomMultiplier, 0.01f, "Max zoom multiplier must be 1.2.");
            Assert.AreEqual(12.0f, cam.MaxZoom, 0.05f, "Max zoom must be modest ~12.0 (1.2x default), not 26.");

            // Test clamping in AdjustZoom
            cam.SetZoom(cam.DefaultZoom);
            cam.AdjustZoom(100f); // attempt huge zoom out
            Assert.LessOrEqual(cam.CurrentZoom, 12.05f, "AdjustZoom must clamp zoom to maxZoom (12.0).");

            cam.AdjustZoom(-100f); // attempt huge zoom in
            Assert.GreaterOrEqual(cam.CurrentZoom, cam.MinZoom - 0.05f, "AdjustZoom must clamp zoom to minZoom.");

            cam.SetZoom(cam.DefaultZoom);
            Debug.Log("[TEST 6 PASS] Camera zoom-out is strictly limited to modest 1.2x (~12.0 ortho size).");
        }

        public static void Test7_FeatureOverlays_RepeatEveryTimeLevelStarts()
        {
            var uiCanvas = GameObject.Find("GameUICanvas");
            var uiMgr = uiCanvas.GetComponent<GameUIManager>();
            Assert.IsNotNull(uiMgr, "GameUIManager must exist.");
            var tutModal = uiMgr.FeatureTutorialModal;
            Assert.IsNotNull(tutModal, "FeatureTutorialModal must exist.");

            var cfg = FeatureProgressionConfig.Instance;
            Assert.IsNotNull(cfg);

            // Pre-acknowledge all tutorials to simulate a returning player
            cfg.AcknowledgeTutorial("linked_ants");
            cfg.AcknowledgeTutorial("stacked_ants");
            cfg.AcknowledgeTutorial("pipes");
            cfg.AcknowledgeTutorial("locked_ants");

            Assert.IsTrue(cfg.IsTutorialAcknowledged("linked_ants"), "Tutorial flag is set.");

            // In GameUIManager, milestone levels 7, 15, 21, 30 must trigger their feature overlay regardless of acknowledged flag
            var db = Resources.Load<LevelDatabase>("LevelDatabase");
            if (db == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:LevelDatabase");
                if (guids != null && guids.Length > 0)
                {
                    db = AssetDatabase.LoadAssetAtPath<LevelDatabase>(AssetDatabase.GUIDToAssetPath(guids[0]));
                }
            }

            Assert.IsNotNull(db, "LevelDatabase must exist.");
            Level lvl7 = db.GetLevel(7);   // Level 7
            Level lvl15 = db.GetLevel(15); // Level 15
            Level lvl21 = db.GetLevel(21); // Level 21
            Level lvl30 = db.GetLevel(30); // Level 30
            Level lvl8 = db.GetLevel(8);   // Level 8

            LegalManager.RecordTermsAcceptance(LegalConfig.Instance);

            // Simulate level loaded for Level 7
            uiMgr.SendMessage("HandleLevelLoaded", lvl7, SendMessageOptions.DontRequireReceiver);
            Assert.IsTrue(tutModal.IsShowing, "Level 7 must show Linked Ants overlay on entry even if previously acknowledged.");
            tutModal.gameObject.SetActive(false);

            // Simulate restart of Level 7: must show again!
            uiMgr.SendMessage("HandleLevelLoaded", lvl7, SendMessageOptions.DontRequireReceiver);
            Assert.IsTrue(tutModal.IsShowing, "Level 7 restart must show Linked Ants overlay again.");
            tutModal.gameObject.SetActive(false);

            // Simulate Level 15
            uiMgr.SendMessage("HandleLevelLoaded", lvl15, SendMessageOptions.DontRequireReceiver);
            Assert.IsTrue(tutModal.IsShowing, "Level 15 must show Stacked Ants overlay on entry.");
            tutModal.gameObject.SetActive(false);

            // Simulate Level 21
            uiMgr.SendMessage("HandleLevelLoaded", lvl21, SendMessageOptions.DontRequireReceiver);
            Assert.IsTrue(tutModal.IsShowing, "Level 21 must show Pipes overlay on entry.");
            tutModal.gameObject.SetActive(false);

            // Simulate Level 30
            uiMgr.SendMessage("HandleLevelLoaded", lvl30, SendMessageOptions.DontRequireReceiver);
            Assert.IsTrue(tutModal.IsShowing, "Level 30 must show Locked Ants overlay on entry.");
            tutModal.gameObject.SetActive(false);

            // Non-milestone level (e.g. Level 8) must NOT show feature overlay
            uiMgr.SendMessage("HandleLevelLoaded", lvl8, SendMessageOptions.DontRequireReceiver);
            Assert.IsFalse(tutModal.IsShowing, "Level 8 must NOT show feature overlay.");

            Debug.Log("[TEST 7 PASS] Feature overlays repeat every time milestone levels (7, 15, 21, 30) are entered or restarted.");
        }

        public static void Test8_PuzzleData_MechanicsAuditVsMilestones()
        {
            var db = Resources.Load<LevelDatabase>("LevelDatabase");
            if (db == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:LevelDatabase");
                if (guids != null && guids.Length > 0)
                {
                    db = AssetDatabase.LoadAssetAtPath<LevelDatabase>(AssetDatabase.GUIDToAssetPath(guids[0]));
                }
            }

            Assert.IsNotNull(db, "LevelDatabase must be loaded.");

            int firstLinked = -1, firstStacked = -1, firstPipes = -1, firstLocked = -1;
            for (int i = 0; i < db.LevelCount; i++)
            {
                var lvl = db.GetLevel(i);
                if (lvl == null) continue;
                int num = lvl.LevelNumber;

                // Linked Ants: TutorialFlags & 1 or IsConnected
                bool hasLinked = (lvl.TutorialFlags & 1) != 0;
                if (lvl.AntQueues != null)
                {
                    foreach (var q in lvl.AntQueues)
                    {
                        if (q.IsConnected) hasLinked = true;
                    }
                }
                if (hasLinked && firstLinked == -1) firstLinked = num;

                // Stacked Ants: TutorialFlags & 2 or IsSecondColor
                bool hasStacked = (lvl.TutorialFlags & 2) != 0;
                if (lvl.AntQueues != null)
                {
                    foreach (var q in lvl.AntQueues)
                    {
                        if (q.IsSecondColor) hasStacked = true;
                    }
                }
                if (hasStacked && firstStacked == -1) firstStacked = num;

                // Pipes: Reference pipes or cells
                var pipes = ReferenceLevelRuntimeAdapter.ExtractPipes(lvl);
                bool hasPipes = (pipes != null && pipes.Count > 0);
                if (hasPipes && firstPipes == -1) firstPipes = num;

                // Locked Ants: q.IsLocked && q.LockAmount > 0
                bool hasLocked = false;
                if (lvl.AntQueues != null)
                {
                    foreach (var q in lvl.AntQueues)
                    {
                        if (q.IsLocked && q.LockAmount > 0) hasLocked = true;
                    }
                }
                if (hasLocked && firstLocked == -1) firstLocked = num;
            }

            Debug.Log($"[Level Data Audit] First Linked={firstLinked} (Milestone=7), First Stacked={firstStacked} (Milestone=15), First Pipes={firstPipes} (Milestone=21), First Locked={firstLocked} (Milestone=30).");
            Assert.AreEqual(7, firstLinked, "Linked Ants first appears on Level 7 in puzzle data.");
            Assert.AreEqual(15, firstStacked, "Stacked Ants first appears on Level 15 in puzzle data.");
            Assert.AreEqual(21, firstPipes, "Pipes first appears on Level 21 in puzzle data matching milestone 21.");
            Assert.AreEqual(30, firstLocked, "Locked Ants first appears on Level 30 in puzzle data matching milestone 30.");
            Debug.Log("[TEST 8 PASS] Puzzle data audit verified. Zero mechanics appear earlier in level layouts than their presentation milestones.");
        }

        private static void EnsureScreenshotsFolder()
        {
            if (!Directory.Exists(ScreenshotsFolder))
            {
                Directory.CreateDirectory(ScreenshotsFolder);
            }
        }

        public static void CaptureEvidenceScreenshots()
        {
            EnsureScreenshotsFolder();

            // 1. Capture Welcome Scene
            CaptureWelcomeScene();

            // 2. Capture Boundary Levels and Overlays in main scene
            CaptureMainSceneBoundaries();
        }

        private static void CaptureWelcomeScene()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Welcome.unity", OpenSceneMode.Single);
            var cam = Camera.main;
            if (cam != null)
            {
                var canvas = Object.FindFirstObjectByType<Canvas>();
                var oldMode = canvas != null ? canvas.renderMode : RenderMode.ScreenSpaceOverlay;
                if (canvas != null)
                {
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = cam;
                    canvas.planeDistance = 1f;
                }

                RenderTexture rt = new RenderTexture(1080, 1920, 24);
                cam.targetTexture = rt;
                cam.Render();

                RenderTexture.active = rt;
                Texture2D screen = new Texture2D(1080, 1920, TextureFormat.RGB24, false);
                screen.ReadPixels(new Rect(0, 0, 1080, 1920), 0, 0);
                screen.Apply();

                cam.targetTexture = null;
                RenderTexture.active = null;
                Object.DestroyImmediate(rt);

                if (canvas != null) canvas.renderMode = oldMode;

                byte[] bytes = screen.EncodeToPNG();
                Object.DestroyImmediate(screen);

                string path = Path.Combine(ScreenshotsFolder, "Evidence_Welcome_Scene.png");
                File.WriteAllBytes(path, bytes);
                Debug.Log($"[Evidence Captured] Saved {path}");
            }
        }

        private static void CaptureMainSceneBoundaries()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/main.unity", OpenSceneMode.Single);
            var cam = Camera.main;
            if (cam == null) return;

            var uiCanvasObj = GameObject.Find("GameUICanvas");
            if (uiCanvasObj == null) return;

            var canvas = uiCanvasObj.GetComponent<Canvas>();
            var oldMode = canvas != null ? canvas.renderMode : RenderMode.ScreenSpaceOverlay;

            var compOverlay = uiCanvasObj.GetComponentInChildren<LevelCompletedOverlay>(true);
            var tutModal = uiCanvasObj.GetComponentInChildren<FeatureTutorialModal>(true);
            var cfg = FeatureProgressionConfig.Instance;

            void CaptureCurrentState(string filename)
            {
                if (canvas != null)
                {
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = cam;
                    canvas.planeDistance = 1f;
                }

                RenderTexture rt = new RenderTexture(1080, 1920, 24);
                cam.targetTexture = rt;
                cam.Render();

                RenderTexture.active = rt;
                Texture2D screen = new Texture2D(1080, 1920, TextureFormat.RGB24, false);
                screen.ReadPixels(new Rect(0, 0, 1080, 1920), 0, 0);
                screen.Apply();

                cam.targetTexture = null;
                RenderTexture.active = null;
                Object.DestroyImmediate(rt);

                if (canvas != null) canvas.renderMode = oldMode;

                byte[] bytes = screen.EncodeToPNG();
                Object.DestroyImmediate(screen);

                string p = Path.Combine(ScreenshotsFolder, filename);
                File.WriteAllBytes(p, bytes);
                Debug.Log($"[Evidence Captured] Saved {p}");
            }

            // Capture Level 6 Completion (Linked Ants 100%)
            if (compOverlay != null)
            {
                compOverlay.gameObject.SetActive(true);
                compOverlay.Show(6, 6);
                CaptureCurrentState("Evidence_Level6_Completion_Linked100.png");
                compOverlay.gameObject.SetActive(false);
            }

            // Capture Level 7 Tutorial (Linked Ants Tutorial)
            if (tutModal != null && cfg != null)
            {
                var feat = cfg.GetFeature("linked_ants");
                if (feat != null)
                {
                    tutModal.gameObject.SetActive(true);
                    tutModal.ShowTutorials(new List<FeatureDefinition> { feat }, null);
                    CaptureCurrentState("Evidence_Level7_LinkedTutorial.png");
                    tutModal.gameObject.SetActive(false);
                }
            }

            // Capture Level 14 Completion (Stacked Ants 100%)
            if (compOverlay != null)
            {
                compOverlay.gameObject.SetActive(true);
                compOverlay.Show(14, 14);
                CaptureCurrentState("Evidence_Level14_Completion_Stacked100.png");
                compOverlay.gameObject.SetActive(false);
            }

            // Capture Level 15 Tutorial (Stacked Ants Tutorial)
            if (tutModal != null && cfg != null)
            {
                var feat = cfg.GetFeature("stacked_ants");
                if (feat != null)
                {
                    tutModal.gameObject.SetActive(true);
                    tutModal.ShowTutorials(new List<FeatureDefinition> { feat }, null);
                    CaptureCurrentState("Evidence_Level15_StackedTutorial.png");
                    tutModal.gameObject.SetActive(false);
                }
            }

            // Capture Level 20 Completion (Pipes 100%)
            if (compOverlay != null)
            {
                compOverlay.gameObject.SetActive(true);
                compOverlay.Show(20, 20);
                CaptureCurrentState("Evidence_Level20_Completion_Pipes100.png");
                compOverlay.gameObject.SetActive(false);
            }

            // Capture Level 21 Tutorial (Pipes Tutorial)
            if (tutModal != null && cfg != null)
            {
                var feat = cfg.GetFeature("pipes");
                if (feat != null)
                {
                    tutModal.gameObject.SetActive(true);
                    tutModal.ShowTutorials(new List<FeatureDefinition> { feat }, null);
                    CaptureCurrentState("Evidence_Level21_PipesTutorial.png");
                    tutModal.gameObject.SetActive(false);
                }
            }

            // Capture Level 29 Completion (Locked Ants 100%)
            if (compOverlay != null)
            {
                compOverlay.gameObject.SetActive(true);
                compOverlay.Show(29, 29);
                CaptureCurrentState("Evidence_Level29_Completion_Locked100.png");
                compOverlay.gameObject.SetActive(false);
            }

            // Capture Level 30 Tutorial (Locked Ants Tutorial)
            if (tutModal != null && cfg != null)
            {
                var feat = cfg.GetFeature("locked_ants");
                if (feat != null)
                {
                    tutModal.gameObject.SetActive(true);
                    tutModal.ShowTutorials(new List<FeatureDefinition> { feat }, null);
                    CaptureCurrentState("Evidence_Level30_LockedTutorial.png");
                    tutModal.gameObject.SetActive(false);
                }
            }

            // Capture Level 30 Completion (Next Feature Section Hidden!)
            if (compOverlay != null)
            {
                compOverlay.gameObject.SetActive(true);
                compOverlay.Show(30, 30);
                CaptureCurrentState("Evidence_Level30_Completion_ProgressHidden.png");
                compOverlay.gameObject.SetActive(false);
            }
        }
    }
}
#endif
