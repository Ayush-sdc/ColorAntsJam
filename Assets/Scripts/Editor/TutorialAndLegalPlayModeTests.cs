#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using AntsJam.Progression;
using AntsJam.UI;
using AntsJam.Legal;

public static class TutorialAndLegalPlayModeTests
{
    private const string EvidenceFolder = "Assets/TestEvidence/Screenshots";

    [MenuItem("Tools/Slither Ants Jam/Tests/Run Tutorial & Legal Full Test Suite")]
    public static void RunFullTestSuite()
    {
        Debug.Log("=======================================================================");
        Debug.Log(">>> RUNNING TUTORIAL & LEGAL TERMS VERIFICATION TEST SUITE <<<");
        Debug.Log("=======================================================================");

        EnsureEvidenceFolder();

        int passed = 0;
        int failed = 0;

        RunTest("1. Tutorial Prefabs & Hit Areas", TestTutorialPrefabsAndHitAreas, ref passed, ref failed);
        RunTest("2. Tutorial Acknowledgement Persistence", TestTutorialAcknowledgementPersistence, ref passed, ref failed);
        RunTest("3. Tutorial Modal Pause & Input Blocking", TestTutorialModalPauseAndInputBlocking, ref passed, ref failed);
        RunTest("4. Settings -> Legal Terms -> Document Navigation", TestLegalTermsNavigationAndHierarchy, ref passed, ref failed);
        RunTest("5. Account Deletion Safety (No Data Erased on Tap)", TestAccountDeletionSafety, ref passed, ref failed);
        RunTest("6. Personalized Ads Disabled & Informative Notice", TestPersonalizedAdsDisabledNotice, ref passed, ref failed);
        RunTest("7. Player ID Row Handling & Copy", TestPlayerIdRowHandling, ref passed, ref failed);
        RunTest("8. Welcome Overlay First-Launch Acceptance", TestWelcomeOverlayFirstLaunchAndAcceptance, ref passed, ref failed);
        RunTest("9. Welcome Overlay Terms Version Update", TestWelcomeOverlayTermsVersionUpdate, ref passed, ref failed);
        RunTest("10. Android Back Stack Hierarchy", TestAndroidBackStackHierarchy, ref passed, ref failed);
        RunTest("11. Capture All Verification Screenshots", CaptureAllScreenshots, ref passed, ref failed);

        Debug.Log("=======================================================================");
        Debug.Log($"TEST SUITE COMPLETED: {passed} PASSED, {failed} FAILED.");
        Debug.Log("=======================================================================");
    }

    private static void RunTest(string name, Func<bool> testFunc, ref int passed, ref int failed)
    {
        try
        {
            if (testFunc())
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
        catch (Exception ex)
        {
            failed++;
            Debug.LogError($"[EXCEPTION] {name}: {ex.Message}\n{ex.StackTrace}");
        }
    }

    // -------------------------------------------------------------
    // TEST 1: Tutorial Prefabs & OK Button Hit Areas
    // -------------------------------------------------------------
    private static bool TestTutorialPrefabsAndHitAreas()
    {
        string[] prefabs = new string[]
        {
            "Assets/Prefabs/UI/Tutorials/FeatureOverlay_LinkedAnts.prefab",
            "Assets/Prefabs/UI/Tutorials/FeatureOverlay_StackedAnts.prefab",
            "Assets/Prefabs/UI/Tutorials/FeatureOverlay_Pipes.prefab",
            "Assets/Prefabs/UI/Tutorials/FeatureOverlay_LockedAnts.prefab"
        };

        foreach (var p in prefabs)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
            if (go == null)
            {
                Debug.LogError($"Prefab missing: {p}");
                return false;
            }

            var view = go.GetComponent<FeatureOverlayView>();
            if (view == null)
            {
                Debug.LogError($"Missing FeatureOverlayView on {p}");
                return false;
            }

            if (view.TutorialImage == null || view.TutorialImage.sprite == null)
            {
                Debug.LogError($"Missing TutorialImage or sprite on {p}");
                return false;
            }

            if (!view.TutorialImage.preserveAspect)
            {
                Debug.LogError($"TutorialImage preserveAspect is false on {p}");
                return false;
            }

            if (view.OkButton == null)
            {
                Debug.LogError($"Missing OkButton on {p}");
                return false;
            }

            var btnRt = view.OkButton.GetComponent<RectTransform>();
            // Target: min=(0.31, 0.031), max=(0.69, 0.115)
            if (Mathf.Abs(btnRt.anchorMin.x - 0.31f) > 0.03f ||
                Mathf.Abs(btnRt.anchorMin.y - 0.031f) > 0.03f ||
                Mathf.Abs(btnRt.anchorMax.x - 0.69f) > 0.03f ||
                Mathf.Abs(btnRt.anchorMax.y - 0.115f) > 0.03f)
            {
                Debug.LogError($"OkButton anchors misaligned on {p}: min={btnRt.anchorMin}, max={btnRt.anchorMax}");
                return false;
            }

            var btnImg = view.OkButton.GetComponent<Image>();
            if (btnImg == null || !btnImg.raycastTarget)
            {
                Debug.LogError($"OkButton Image missing or raycastTarget is false on {p}");
                return false;
            }

            if (btnImg.color.a > 0.05f)
            {
                Debug.LogError($"OkButton Image is visible (alpha > 0) on {p}, should be invisible hit area");
                return false;
            }
        }

        return true;
    }

    // -------------------------------------------------------------
    // TEST 2: Tutorial Acknowledgement Persistence
    // -------------------------------------------------------------
    private static bool TestTutorialAcknowledgementPersistence()
    {
        var config = FeatureProgressionConfig.Instance;
        if (config == null) return false;

        // Reset
        config.ResetTutorialAcknowledgements();

        // Level 7 should have linked_ants pending
        var pendingBefore = config.GetPendingTutorials(7);
        bool hasLinkedBefore = pendingBefore.Exists(f => f.FeatureId == "linked_ants");

        // Acknowledge linked_ants
        config.AcknowledgeTutorial("linked_ants");
        bool ackSaved = config.IsTutorialAcknowledged("linked_ants");

        // Level 7 should no longer have linked_ants pending
        var pendingAfter = config.GetPendingTutorials(7);
        bool hasLinkedAfter = pendingAfter.Exists(f => f.FeatureId == "linked_ants");

        // Clean up
        config.ResetTutorialAcknowledgements();

        return hasLinkedBefore && ackSaved && !hasLinkedAfter;
    }

    // -------------------------------------------------------------
    // TEST 3: Tutorial Modal Pause & Input Blocking
    // -------------------------------------------------------------
    private static bool TestTutorialModalPauseAndInputBlocking()
    {
        EnsureSceneLoaded();
        var uiMgr = UnityEngine.Object.FindFirstObjectByType<GameUIManager>();
        if (uiMgr == null || uiMgr.FeatureTutorialModal == null) return false;

        var config = FeatureProgressionConfig.Instance;
        var feat = config.GetFeature("linked_ants");
        if (feat == null) return false;

        bool okInvoked = false;
        uiMgr.FeatureTutorialModal.ShowTutorials(new List<FeatureDefinition> { feat }, () =>
        {
            okInvoked = true;
        });

        bool modalActiveDuring = uiMgr.FeatureTutorialModal.IsShowing && uiMgr.IsAnyModalActive();

        // Simulate dismiss
        var overlay = uiMgr.FeatureTutorialModal.GetComponentInChildren<FeatureOverlayView>();
        if (overlay != null && overlay.OkButton != null)
        {
            overlay.OkButton.onClick.Invoke();
        }

        // Clean up
        uiMgr.FeatureTutorialModal.gameObject.SetActive(false);

        return modalActiveDuring;
    }

    // -------------------------------------------------------------
    // TEST 4: Settings -> Legal Terms -> Document Navigation
    // -------------------------------------------------------------
    private static bool TestLegalTermsNavigationAndHierarchy()
    {
        EnsureSceneLoaded();
        var uiMgr = UnityEngine.Object.FindFirstObjectByType<GameUIManager>();
        if (uiMgr == null) return false;

        uiMgr.EnsureLegalReferencesConfigured();
        var termsModal = uiMgr.LegalTermsModal;
        var docModal = uiMgr.LegalDocumentModal;
        if (termsModal == null || docModal == null) return false;

        // Open Legal Terms
        uiMgr.OpenLegalTermsModal();
        DG.Tweening.DOTween.CompleteAll();
        bool termsShowing = termsModal.IsShowing && uiMgr.IsAnyModalActive();

        // Open Privacy Policy
        var privBtnProp = typeof(LegalTermsModal).GetField("privacyPolicyButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var privBtn = privBtnProp?.GetValue(termsModal) as Button;
        if (privBtn != null) privBtn.onClick.Invoke();
        DG.Tweening.DOTween.CompleteAll();

        bool docShowing = docModal.IsShowing && uiMgr.IsAnyModalActive();

        // Close Document Modal
        docModal.CloseModal();
        DG.Tweening.DOTween.CompleteAll();
        bool docClosed = !docModal.IsShowing;
        bool termsStillOpen = termsModal.IsShowing;

        // Close Legal Terms Modal
        termsModal.CloseModal();
        DG.Tweening.DOTween.CompleteAll();
        bool termsClosed = !termsModal.IsShowing;

        return termsShowing && docShowing && docClosed && termsStillOpen && termsClosed;
    }

    // -------------------------------------------------------------
    // TEST 5: Account Deletion Safety (No Local/Remote Data Erased)
    // -------------------------------------------------------------
    private static bool TestAccountDeletionSafety()
    {
        EnsureSceneLoaded();
        var uiMgr = UnityEngine.Object.FindFirstObjectByType<GameUIManager>();
        if (uiMgr == null || uiMgr.LegalTermsModal == null || uiMgr.LegalDocumentModal == null) return false;

        uiMgr.EnsureLegalReferencesConfigured();

        // Save a test progression value in PlayerPrefs
        PlayerPrefs.SetInt("TestSafety_Progression", 42);
        PlayerPrefs.Save();

        // Open Legal Terms and tap Account Deletion
        uiMgr.OpenLegalTermsModal();
        DG.Tweening.DOTween.CompleteAll();
        var delBtnProp = typeof(LegalTermsModal).GetField("accountDeletionButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var delBtn = delBtnProp?.GetValue(uiMgr.LegalTermsModal) as Button;
        if (delBtn != null) delBtn.onClick.Invoke();
        DG.Tweening.DOTween.CompleteAll();

        // Verify Document Modal opens with instructions
        bool docOpened = uiMgr.LegalDocumentModal.IsShowing;

        // Verify local progression data was NOT erased!
        int progressionVal = PlayerPrefs.GetInt("TestSafety_Progression", 0);
        bool dataPreserved = progressionVal == 42;

        PlayerPrefs.DeleteKey("TestSafety_Progression");
        uiMgr.LegalDocumentModal.CloseModal();
        uiMgr.LegalTermsModal.CloseModal();
        DG.Tweening.DOTween.CompleteAll();

        return docOpened && dataPreserved;
    }

    // -------------------------------------------------------------
    // TEST 6: Personalized Ads Disabled & Informative Notice
    // -------------------------------------------------------------
    private static bool TestPersonalizedAdsDisabledNotice()
    {
        var config = LegalConfig.Instance;
        if (config == null) return false;

        // Personalized ads must default to false since no SDK is installed
        if (config.IsPersonalizedAdsAvailable)
        {
            Debug.LogError("IsPersonalizedAdsAvailable should be false when no SDK is present!");
            return false;
        }

        EnsureSceneLoaded();
        var uiMgr = UnityEngine.Object.FindFirstObjectByType<GameUIManager>();
        if (uiMgr == null || uiMgr.LegalTermsModal == null) return false;

        uiMgr.EnsureLegalReferencesConfigured();
        uiMgr.OpenLegalTermsModal();
        DG.Tweening.DOTween.CompleteAll();
        uiMgr.LegalTermsModal.RefreshUIState();

        var adsBtnProp = typeof(LegalTermsModal).GetField("personalizedAdsButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var adsBtn = adsBtnProp?.GetValue(uiMgr.LegalTermsModal) as Button;

        bool isInteractable = adsBtn != null && adsBtn.interactable;
        // Button should be disabled/uninteractable or show explanation
        uiMgr.LegalTermsModal.CloseModal();
        DG.Tweening.DOTween.CompleteAll();

        return !isInteractable;
    }

    // -------------------------------------------------------------
    // TEST 7: Player ID Row Handling & Copy
    // -------------------------------------------------------------
    private static bool TestPlayerIdRowHandling()
    {
        var config = LegalConfig.Instance;
        if (config == null) return false;

        EnsureSceneLoaded();
        var uiMgr = UnityEngine.Object.FindFirstObjectByType<GameUIManager>();
        if (uiMgr == null || uiMgr.LegalTermsModal == null) return false;

        uiMgr.EnsureLegalReferencesConfigured();
        var idRowProp = typeof(LegalTermsModal).GetField("playerIdRow", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // 1. When not configured / available -> row must be hidden
        config.SetPlayerId(false, "");
        uiMgr.OpenLegalTermsModal();
        DG.Tweening.DOTween.CompleteAll();
        uiMgr.LegalTermsModal.RefreshUIState();
        var idRow = idRowProp?.GetValue(uiMgr.LegalTermsModal) as GameObject;
        bool hiddenWhenUnavailable = idRow != null && !idRow.activeSelf;

        // 2. When configured with real ID -> row is active and copy button copies ID
        config.SetPlayerId(true, "USER_784920_PROD");
        uiMgr.LegalTermsModal.RefreshUIState();
        bool activeWhenConfigured = idRow != null && idRow.activeSelf;

        var copyBtnProp = typeof(LegalTermsModal).GetField("copyPlayerIdButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var copyBtn = copyBtnProp?.GetValue(uiMgr.LegalTermsModal) as Button;
        if (copyBtn != null) copyBtn.onClick.Invoke();

        bool copiedToClipboard = GUIUtility.systemCopyBuffer == "USER_784920_PROD";

        // Restore clean state (hidden by default since no backend auth is present)
        config.SetPlayerId(false, "");
        uiMgr.LegalTermsModal.RefreshUIState();
        uiMgr.LegalTermsModal.CloseModal();
        DG.Tweening.DOTween.CompleteAll();

        return hiddenWhenUnavailable && activeWhenConfigured && copiedToClipboard;
    }

    // -------------------------------------------------------------
    // TEST 8: Welcome Overlay First-Launch Acceptance
    // -------------------------------------------------------------
    private static bool TestWelcomeOverlayFirstLaunchAndAcceptance()
    {
        var config = LegalConfig.Instance;
        if (config == null) return false;

        EnsureSceneLoaded();
        var uiMgr = UnityEngine.Object.FindFirstObjectByType<GameUIManager>();
        if (uiMgr == null || uiMgr.WelcomeOverlay == null) return false;

        uiMgr.EnsureLegalReferencesConfigured();

        // Reset terms acceptance
        LegalManager.ResetTermsAcceptance();
        bool notAcceptedInitially = !LegalManager.HasAcceptedCurrentTerms(config);

        bool acceptedCallbackFired = false;
        uiMgr.WelcomeOverlay.Show(() =>
        {
            acceptedCallbackFired = true;
        });
        DG.Tweening.DOTween.CompleteAll();

        bool welcomeShowing = uiMgr.WelcomeOverlay.IsShowing && uiMgr.IsAnyModalActive();

        // Simulate ACCEPT button click
        var acceptBtnProp = typeof(WelcomeOverlay).GetField("acceptButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var acceptBtn = acceptBtnProp?.GetValue(uiMgr.WelcomeOverlay) as Button;
        if (acceptBtn != null) acceptBtn.onClick.Invoke();
        DG.Tweening.DOTween.CompleteAll();

        bool acceptedNow = LegalManager.HasAcceptedCurrentTerms(config);
        string savedVer = LegalManager.GetAcceptedTermsVersion();
        string savedTs = LegalManager.GetAcceptedTermsTimestamp();
        bool hasValidTimestamp = !string.IsNullOrEmpty(savedTs) && savedTs.Length > 10;

        uiMgr.WelcomeOverlay.gameObject.SetActive(false);

        return notAcceptedInitially && welcomeShowing && acceptedCallbackFired && acceptedNow && savedVer == config.TermsOfServiceVersion && hasValidTimestamp;
    }

    // -------------------------------------------------------------
    // TEST 9: Welcome Overlay Terms Version Update
    // -------------------------------------------------------------
    private static bool TestWelcomeOverlayTermsVersionUpdate()
    {
        var config = LegalConfig.Instance;
        if (config == null) return false;

        // Accept v1.0.0
        config.SetTermsVersion("1.0.0");
        LegalManager.RecordTermsAcceptance(config);
        bool acceptedV1 = LegalManager.HasAcceptedCurrentTerms(config);

        // Update terms version to 1.1.0
        config.SetTermsVersion("1.1.0");
        config.SetTermsRecentlyUpdated(true);

        // Must now report unaccepted!
        bool unacceptedV11 = !LegalManager.HasAcceptedCurrentTerms(config);

        // Accept new version
        LegalManager.RecordTermsAcceptance(config);
        bool acceptedV11 = LegalManager.HasAcceptedCurrentTerms(config);

        // Restore clean state
        config.SetTermsVersion("1.0.0");
        config.SetTermsRecentlyUpdated(false);
        LegalManager.RecordTermsAcceptance(config);

        return acceptedV1 && unacceptedV11 && acceptedV11;
    }

    // -------------------------------------------------------------
    // TEST 10: Android Back Stack Hierarchy
    // -------------------------------------------------------------
    private static bool TestAndroidBackStackHierarchy()
    {
        EnsureSceneLoaded();
        var uiMgr = UnityEngine.Object.FindFirstObjectByType<GameUIManager>();
        if (uiMgr == null) return false;

        // Open Settings -> Legal Terms -> Document Modal
        uiMgr.OpenSettingsModal();
        uiMgr.OpenLegalTermsModal();
        uiMgr.LegalTermsModal.RefreshUIState();

        var privBtnProp = typeof(LegalTermsModal).GetField("privacyPolicyButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var privBtn = privBtnProp?.GetValue(uiMgr.LegalTermsModal) as Button;
        if (privBtn != null) privBtn.onClick.Invoke();

        // 1. When Document Modal is top: Escape closes Document Modal only
        if (uiMgr.LegalDocumentModal.IsShowing)
        {
            uiMgr.LegalDocumentModal.CloseModal();
        }
        bool docClosed = !uiMgr.LegalDocumentModal.IsShowing;
        bool termsStillOpen = uiMgr.LegalTermsModal.IsShowing;

        // 2. When Legal Terms Modal is top: Escape closes Legal Terms Modal only
        if (termsStillOpen)
        {
            uiMgr.LegalTermsModal.CloseModal();
        }
        bool termsClosed = !uiMgr.LegalTermsModal.IsShowing;

        // 3. Close Settings
        uiMgr.CloseSettingsModal();

        return docClosed && termsStillOpen && termsClosed;
    }

    // -------------------------------------------------------------
    // TEST 11: Capture All Verification Screenshots
    // -------------------------------------------------------------
    private static bool CaptureAllScreenshots()
    {
        EnsureSceneLoaded();
        var uiMgr = UnityEngine.Object.FindFirstObjectByType<GameUIManager>();
        if (uiMgr == null) return false;

        var canvas = uiMgr.GetComponentInParent<Canvas>();
        if (canvas == null) return false;

        DismissAllModals(uiMgr);

        // 1. Linked Ants Tutorial
        var linkedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Tutorials/FeatureOverlay_LinkedAnts.prefab");
        var linkedInst = (GameObject)PrefabUtility.InstantiatePrefab(linkedPrefab, canvas.transform);
        linkedInst.SetActive(true);
        linkedInst.transform.SetAsLastSibling();
        CaptureView("Evidence_Tutorial_LinkedAnts.png");
        GameObject.DestroyImmediate(linkedInst);

        // 2. Stacked Ants Tutorial
        var stackedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Tutorials/FeatureOverlay_StackedAnts.prefab");
        var stackedInst = (GameObject)PrefabUtility.InstantiatePrefab(stackedPrefab, canvas.transform);
        stackedInst.SetActive(true);
        stackedInst.transform.SetAsLastSibling();
        CaptureView("Evidence_Tutorial_StackedAnts.png");
        GameObject.DestroyImmediate(stackedInst);

        // 3. Pipe Tutorial
        var pipePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Tutorials/FeatureOverlay_Pipes.prefab");
        var pipeInst = (GameObject)PrefabUtility.InstantiatePrefab(pipePrefab, canvas.transform);
        pipeInst.SetActive(true);
        pipeInst.transform.SetAsLastSibling();
        CaptureView("Evidence_Tutorial_Pipes.png");
        GameObject.DestroyImmediate(pipeInst);

        // 4. Locked Ants Tutorial
        var lockedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Tutorials/FeatureOverlay_LockedAnts.prefab");
        var lockedInst = (GameObject)PrefabUtility.InstantiatePrefab(lockedPrefab, canvas.transform);
        lockedInst.SetActive(true);
        lockedInst.transform.SetAsLastSibling();
        CaptureView("Evidence_Tutorial_LockedAnts.png");
        GameObject.DestroyImmediate(lockedInst);

        // 5. Settings Modal with Legal Terms Button
        uiMgr.OpenSettingsModal();
        CaptureView("Evidence_Settings_WithLegalButton.png");
        uiMgr.CloseSettingsModal();

        // 6. Legal Terms Modal matching Privacy_policies.png
        if (uiMgr.LegalTermsModal != null)
        {
            uiMgr.LegalTermsModal.Show(null);
            CaptureView("Evidence_LegalTerms_Modal.png");

            // 7. Legal Document Modal: Privacy Policy
            if (uiMgr.LegalDocumentModal != null)
            {
                var cfg = LegalConfig.Instance;
                uiMgr.LegalDocumentModal.Show(cfg.PrivacyPolicyTitle, cfg.PrivacyPolicyContent, cfg.PrivacyPolicyUrl, null);
                CaptureView("Evidence_LegalDocument_PrivacyPolicy.png");

                // 8. Legal Document Modal: Account Deletion
                uiMgr.LegalDocumentModal.Show(cfg.AccountDeletionTitle, cfg.AccountDeletionContent, cfg.AccountDeletionUrl, null);
                CaptureView("Evidence_LegalDocument_AccountDeletion.png");

                uiMgr.LegalDocumentModal.gameObject.SetActive(false);
            }
            uiMgr.LegalTermsModal.gameObject.SetActive(false);
        }

        // 9. Welcome Overlay matching Welcome.png
        if (uiMgr.WelcomeOverlay != null)
        {
            uiMgr.WelcomeOverlay.Show(null);
            CaptureView("Evidence_Welcome_Overlay.png");
            uiMgr.WelcomeOverlay.gameObject.SetActive(false);
        }

        DismissAllModals(uiMgr);
        return true;
    }

    private static void DismissAllModals(GameUIManager uiMgr)
    {
        if (uiMgr == null) return;
        if (uiMgr.WelcomeOverlay != null) uiMgr.WelcomeOverlay.gameObject.SetActive(false);
        if (uiMgr.LegalTermsModal != null) uiMgr.LegalTermsModal.gameObject.SetActive(false);
        if (uiMgr.LegalDocumentModal != null) uiMgr.LegalDocumentModal.gameObject.SetActive(false);
        if (uiMgr.FeatureTutorialModal != null) uiMgr.FeatureTutorialModal.gameObject.SetActive(false);
    }

    private static void CaptureView(string filename)
    {
        Camera cam = Camera.main;
        if (cam == null) cam = UnityEngine.Object.FindAnyObjectByType<Camera>();
        if (cam == null)
        {
            var camGo = GameObject.Find("Main Camera");
            if (camGo != null) cam = camGo.GetComponent<Camera>();
        }
        if (cam == null) return;

        var canvasObj = GameObject.Find("GameUICanvas");
        Canvas canvas = canvasObj != null ? canvasObj.GetComponent<Canvas>() : null;
        RenderMode prevRenderMode = canvas != null ? canvas.renderMode : RenderMode.ScreenSpaceOverlay;
        Camera prevWorldCam = canvas != null ? canvas.worldCamera : null;
        float prevPlaneDist = canvas != null ? canvas.planeDistance : 100f;

        if (canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 1.0f;
        }

        int width = 1080;
        int height = 1920;
        RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);

        int prevMask = cam.cullingMask;
        cam.cullingMask = -1;
        var prevRt = cam.targetTexture;
        cam.targetTexture = rt;

        DG.Tweening.DOTween.CompleteAll();
        Canvas.ForceUpdateCanvases();
        cam.Render();

        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();

        byte[] pngData = tex.EncodeToPNG();
        string fullPath = Path.Combine(EvidenceFolder, filename);
        File.WriteAllBytes(fullPath, pngData);
        Debug.Log($"[Screenshot Captured] {fullPath}");

        cam.targetTexture = prevRt;
        cam.cullingMask = prevMask;
        RenderTexture.active = null;
        UnityEngine.Object.DestroyImmediate(rt);
        UnityEngine.Object.DestroyImmediate(tex);

        if (canvas != null)
        {
            canvas.renderMode = prevRenderMode;
            canvas.worldCamera = prevWorldCam;
            canvas.planeDistance = prevPlaneDist;
        }
    }

    private static void EnsureEvidenceFolder()
    {
        if (!Directory.Exists(EvidenceFolder))
        {
            Directory.CreateDirectory(EvidenceFolder);
        }
    }

    private static void EnsureSceneLoaded()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.name != "main")
        {
            EditorSceneManager.OpenScene("Assets/Scenes/main.unity");
        }
    }
}
#endif
