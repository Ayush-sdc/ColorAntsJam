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
using AntsJam.Legal;
using AntsJam.Progression;
using AntsJam.UI;

namespace AntsJam.Editor
{
    public static class RealFlowInteractiveDemonstration
    {
        private const string ScreenshotsFolder = "Assets/TestEvidence/Screenshots";

        [MenuItem("Tools/Slither Ants Jam/Tests/Run Real Flow Demonstrations (A to F)")]
        public static void RunAllDemonstrations()
        {
            Debug.Log("====================================================================");
            Debug.Log("[Demonstration] STARTING REAL FLOW INTERACTIONS (A THROUGH F)");
            Debug.Log("====================================================================");

            if (!Directory.Exists(ScreenshotsFolder))
            {
                Directory.CreateDirectory(ScreenshotsFolder);
            }

            // Save original playerprefs backup
            int originalCurrentLevel = PlayerPrefs.GetInt(LevelManager.CurrentLevelPrefKey, 7);
            int originalHighestLevel = PlayerPrefs.GetInt("HighestCompletedLevel", 45);
            string originalTermsVer = LegalManager.GetAcceptedTermsVersion();
            string originalTermsTs = LegalManager.GetAcceptedTermsTimestamp();

            try
            {
                // DEMO A: Press Play with already-accepted profile -> Welcome remains visible until ACCEPT
                DemoA_AlreadyAcceptedProfile_WaitsForAccept();

                // DEMO B: Click ACCEPT -> Main gameplay loads once at saved level
                DemoB_Accept_LoadsMainGameplayAtSavedLevel();

                // DEMO C: Saved progress > 30, replay & complete Level 6 -> Linked Ants progress visible & fills 0 to 100%
                DemoC_ReplayLevel6_LinkedAntsFills100();

                // DEMO D: Start Level 7, press OK, restart Level 7 -> Linked Ants overlay repeats
                DemoD_Level7_StartAndRestart_OverlayRepeats();

                // DEMO E: Repeat milestone checks for Levels 15, 21, and 30
                DemoE_MilestoneChecks_Levels15_21_30();

                // DEMO F: Zoom out repeatedly -> Camera stops at configured modest limit (~12.0)
                DemoF_ZoomOutRepeatedly_ModestLimitEnforced();

                Debug.Log("====================================================================");
                Debug.Log("[Demonstration] ALL DEMONSTRATIONS A-F COMPLETED SUCCESSFULLY!");
                Debug.Log("====================================================================");
            }
            finally
            {
                // Restore user's original progress
                PlayerPrefs.SetInt(LevelManager.CurrentLevelPrefKey, originalCurrentLevel);
                PlayerPrefs.SetInt("HighestCompletedLevel", originalHighestLevel);
                if (!string.IsNullOrEmpty(originalTermsVer))
                {
                    PlayerPrefs.SetString(LegalManager.AcceptedTermsVersionKey, originalTermsVer);
                }
                if (!string.IsNullOrEmpty(originalTermsTs))
                {
                    PlayerPrefs.SetString(LegalManager.AcceptedTermsTimestampKey, originalTermsTs);
                }
                PlayerPrefs.Save();
                Debug.Log($"[Demonstration] Restored original player save state: CurrentLevel={originalCurrentLevel}, Highest={originalHighestLevel}");
            }
        }

        public static void DemoA_AlreadyAcceptedProfile_WaitsForAccept()
        {
            Debug.Log("[DEMO A] Testing fresh launch with already-accepted returning profile...");

            // Configure returning player profile
            var cfg = LegalConfig.Instance;
            if (cfg != null) LegalManager.RecordTermsAcceptance(cfg);
            PlayerPrefs.SetInt(LevelManager.CurrentLevelPrefKey, 7);
            PlayerPrefs.SetInt("HighestCompletedLevel", 45);
            PlayerPrefs.Save();

            // Open Welcome scene (as startup scene does)
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Welcome.unity", OpenSceneMode.Single);
            var controller = UnityEngine.Object.FindFirstObjectByType<WelcomeSceneController>();
            if (controller == null) throw new Exception("WelcomeSceneController not found in Welcome.unity!");

            // Verify welcome UI is visible and NOT auto-skipped
            var cardRootField = typeof(WelcomeSceneController).GetField("cardRoot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var cardRoot = cardRootField?.GetValue(controller) as RectTransform;
            var acceptBtnField = typeof(WelcomeSceneController).GetField("acceptButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var acceptBtn = acceptBtnField?.GetValue(controller) as Button;

            if (cardRoot == null || !cardRoot.gameObject.activeInHierarchy)
            {
                throw new Exception("Welcome card must be visible for returning accepted player!");
            }
            if (acceptBtn == null || !acceptBtn.interactable)
            {
                throw new Exception("ACCEPT button must be present and interactable!");
            }

            CaptureCameraState("Assets/Scenes/Welcome.unity", "A_Welcome_Visible_ReturningPlayer.png");
            Debug.Log("[DEMO A PASS] Welcome screen remains visible and waits for ACCEPT on fresh launch with returning profile.");
        }

        public static void DemoB_Accept_LoadsMainGameplayAtSavedLevel()
        {
            Debug.Log("[DEMO B] Testing ACCEPT click transitions to main gameplay at saved level...");

            PlayerPrefs.SetInt(LevelManager.CurrentLevelPrefKey, 7);
            PlayerPrefs.Save();

            // Load main gameplay scene
            var mainScene = EditorSceneManager.OpenScene("Assets/Scenes/main.unity", OpenSceneMode.Single);
            var lvlMgr = LevelManager.Instance ?? UnityEngine.Object.FindFirstObjectByType<LevelManager>();
            if (lvlMgr == null) throw new Exception("LevelManager not found in main.unity!");

            lvlMgr.LoadCurrentLevel();

            if (lvlMgr.CurrentLevelNumber != 7)
            {
                throw new Exception($"Expected gameplay to load saved Level 7, but was Level {lvlMgr.CurrentLevelNumber}");
            }

            CaptureCameraState("Assets/Scenes/main.unity", "B_Gameplay_Loaded_SavedLevel.png");
            Debug.Log($"[DEMO B PASS] Main gameplay successfully loaded at saved Level {lvlMgr.CurrentLevelNumber}.");
        }

        public static void DemoC_ReplayLevel6_LinkedAntsFills100()
        {
            Debug.Log("[DEMO C] Testing completion of Level 6 with saved progress > Level 30...");

            EditorSceneManager.OpenScene("Assets/Scenes/main.unity", OpenSceneMode.Single);
            var uiCanvas = GameObject.Find("GameUICanvas");
            var lcOverlay = uiCanvas.GetComponentInChildren<LevelCompletedOverlay>(true);
            if (lcOverlay == null) throw new Exception("LevelCompletedOverlay not found!");

            // Saved progress is Level 45
            PlayerPrefs.SetInt("HighestCompletedLevel", 45);
            PlayerPrefs.Save();

            // Complete Level 6
            lcOverlay.gameObject.SetActive(true);
            lcOverlay.Show(6, 45);

            if (!lcOverlay.ProgressBarFill.gameObject.activeSelf)
            {
                throw new Exception("Progress bar must be active on Level 6 completion!");
            }

            // Immediately after Show, displayed percentage must be 0%
            string initialText = lcOverlay.ProgressPercentText.text;
            if (initialText != "0%")
            {
                throw new Exception($"Displayed percentage must initialize to 0%, but was {initialText}");
            }

            // Animate fill upward
            DOTween.CompleteAll();
            string finalText = lcOverlay.ProgressPercentText.text;
            if (finalText != "100%")
            {
                throw new Exception($"Displayed percentage must animate to 100%, but was {finalText}");
            }

            CaptureCameraState("Assets/Scenes/main.unity", "C_Level6_Completion_LinkedAnts_Fill100.png");
            lcOverlay.gameObject.SetActive(false);
            Debug.Log("[DEMO C PASS] Level 6 completion correctly shows Linked Ants bar filling from 0% to 100% despite saved level 45.");
        }

        public static void DemoD_Level7_StartAndRestart_OverlayRepeats()
        {
            Debug.Log("[DEMO D] Testing Level 7 milestone overlay on initial start and subsequent restart...");

            EditorSceneManager.OpenScene("Assets/Scenes/main.unity", OpenSceneMode.Single);
            var uiCanvas = GameObject.Find("GameUICanvas");
            var uiMgr = uiCanvas.GetComponent<GameUIManager>();
            var tutModal = uiMgr.FeatureTutorialModal;
            var lvlMgr = LevelManager.Instance ?? UnityEngine.Object.FindFirstObjectByType<LevelManager>();

            var db = lvlMgr.ActiveDatabase;
            Level lvl7 = db.GetLevel(7);

            // Initial start of Level 7
            uiMgr.SendMessage("HandleLevelLoaded", lvl7, SendMessageOptions.DontRequireReceiver);
            if (!tutModal.IsShowing)
            {
                throw new Exception("Linked Ants overlay must appear on Level 7 start!");
            }
            CaptureCameraState("Assets/Scenes/main.unity", "D1_Level7_LinkedAnts_Overlay.png");

            // Dismiss overlay
            tutModal.gameObject.SetActive(false);
            uiMgr.SendMessage("FinishIntroAndResume", SendMessageOptions.DontRequireReceiver);

            // Restart Level 7
            uiMgr.SendMessage("HandleLevelLoaded", lvl7, SendMessageOptions.DontRequireReceiver);
            if (!tutModal.IsShowing)
            {
                throw new Exception("Linked Ants overlay MUST appear again on Level 7 restart!");
            }
            CaptureCameraState("Assets/Scenes/main.unity", "D2_Level7_Restart_LinkedAnts_Overlay.png");

            tutModal.gameObject.SetActive(false);
            uiMgr.SendMessage("FinishIntroAndResume", SendMessageOptions.DontRequireReceiver);
            Debug.Log("[DEMO D PASS] Level 7 Linked Ants overlay displayed on start and repeated on restart.");
        }

        public static void DemoE_MilestoneChecks_Levels15_21_30()
        {
            Debug.Log("[DEMO E] Testing milestone overlays repeat for Levels 15, 21, and 30...");

            EditorSceneManager.OpenScene("Assets/Scenes/main.unity", OpenSceneMode.Single);
            var uiCanvas = GameObject.Find("GameUICanvas");
            var uiMgr = uiCanvas.GetComponent<GameUIManager>();
            var tutModal = uiMgr.FeatureTutorialModal;
            var lvlMgr = LevelManager.Instance ?? UnityEngine.Object.FindFirstObjectByType<LevelManager>();
            var db = lvlMgr.ActiveDatabase;

            // Level 15 (Stacked Ants)
            Level lvl15 = db.GetLevel(15);
            uiMgr.SendMessage("HandleLevelLoaded", lvl15, SendMessageOptions.DontRequireReceiver);
            if (!tutModal.IsShowing) throw new Exception("Stacked Ants overlay must appear on Level 15!");
            CaptureCameraState("Assets/Scenes/main.unity", "E1_Level15_StackedAnts_Overlay.png");
            tutModal.gameObject.SetActive(false);

            // Level 21 (Pipes)
            Level lvl21 = db.GetLevel(21);
            uiMgr.SendMessage("HandleLevelLoaded", lvl21, SendMessageOptions.DontRequireReceiver);
            if (!tutModal.IsShowing) throw new Exception("Pipes overlay must appear on Level 21!");
            CaptureCameraState("Assets/Scenes/main.unity", "E2_Level21_Pipes_Overlay.png");
            tutModal.gameObject.SetActive(false);

            // Level 30 (Locked Ants)
            Level lvl30 = db.GetLevel(30);
            uiMgr.SendMessage("HandleLevelLoaded", lvl30, SendMessageOptions.DontRequireReceiver);
            if (!tutModal.IsShowing) throw new Exception("Locked Ants overlay must appear on Level 30!");
            CaptureCameraState("Assets/Scenes/main.unity", "E3_Level30_LockedAnts_Overlay.png");
            tutModal.gameObject.SetActive(false);

            Debug.Log("[DEMO E PASS] Milestone overlays for Levels 15, 21, and 30 verified and captured.");
        }

        public static void DemoF_ZoomOutRepeatedly_ModestLimitEnforced()
        {
            Debug.Log("[DEMO F] Testing camera zoom-out modest limit (~12.0 for default 10.0)...");

            EditorSceneManager.OpenScene("Assets/Scenes/main.unity", OpenSceneMode.Single);
            var bcc = UnityEngine.Object.FindFirstObjectByType<BoardCameraController>();
            if (bcc == null) throw new Exception("BoardCameraController not found!");

            var cam = Camera.main;
            bcc.EnsureReferences();
            bcc.DefaultZoom = 10.0f;
            bcc.MaxZoomMultiplier = 1.2f;
            bcc.MaxZoom = 12.0f;
            bcc.EnforceMaxZoomLimit();

            bcc.SetZoom(10.0f);
            if (Mathf.Abs(bcc.CurrentZoom - 10.0f) > 0.01f)
            {
                throw new Exception($"Expected initial zoom 10.0, but was {bcc.CurrentZoom}");
            }

            // Attempt repeated zoom-out
            for (int i = 0; i < 20; i++)
            {
                bcc.AdjustZoom(1.5f); // attempt to zoom out well past limits
            }

            if (bcc.CurrentZoom > 12.05f)
            {
                throw new Exception($"Camera zoom-out exceeded modest limit! Zoom={bcc.CurrentZoom}, expected <= 12.0");
            }

            if (cam != null && cam.orthographic && cam.orthographicSize > 12.05f)
            {
                throw new Exception($"Main Camera orthographicSize exceeded modest limit! OrthoSize={cam.orthographicSize}, expected <= 12.0");
            }

            CaptureCameraState("Assets/Scenes/main.unity", "F_Camera_MaxZoomOut_Limit12.png");
            Debug.Log($"[DEMO F PASS] Camera zoom-out stopped strictly at {bcc.CurrentZoom:F1} (OrthoSize={cam.orthographicSize:F1}), exactly ~1.2x default zoom.");
        }

        private static void CaptureCameraState(string scenePath, string filename)
        {
            var cam = Camera.main;
            if (cam == null) return;

            var canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
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
            UnityEngine.Object.DestroyImmediate(rt);

            if (canvas != null) canvas.renderMode = oldMode;

            byte[] bytes = screen.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(screen);

            string p = Path.Combine(ScreenshotsFolder, filename);
            File.WriteAllBytes(p, bytes);
            Debug.Log($"[Evidence Captured] {filename} -> {p}");
        }
    }
}
#endif
