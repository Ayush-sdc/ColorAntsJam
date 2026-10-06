#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using AntsJam.Legal;
using AntsJam.UI;

namespace AntsJam.Editor
{
    public static class CreateWelcomeScene
    {
        private const string WelcomeScenePath = "Assets/Scenes/Welcome.unity";
        private const string GameplayScenePath = "Assets/Scenes/main.unity";

        [MenuItem("Tools/Slither Ants Jam/Legal/Create and Configure Welcome Scene")]
        public static void CreateAndSetupWelcomeScene()
        {
            Debug.Log("[CreateWelcomeScene] Generating Welcome scene...");

            // 1. Create a brand new scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 2. Main Camera
            var camObj = new GameObject("Main Camera");
            var cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.055f, 0.031f, 0.094f, 1f); // Deep purple
            cam.orthographic = true;
            cam.orthographicSize = 10f;
            camObj.tag = "MainCamera";

            // 3. EventSystem
            var eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<StandaloneInputModule>();

            // 4. Canvas
            var canvasObj = new GameObject("WelcomeCanvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // 5. Background Fill (Deep purple)
            var bgObj = new GameObject("DeepPurpleBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bgObj.transform.SetParent(canvasObj.transform, false);
            var bgRt = bgObj.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            var bgImg = bgObj.GetComponent<Image>();
            bgImg.color = new Color(0.055f, 0.031f, 0.094f, 1f);

            // 6. Instantiate WelcomeOverlay Prefab & DocumentModal Prefab
            var welcomePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Legal/WelcomeOverlay.prefab");
            var docModalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Legal/LegalDocumentModal.prefab");
            var legalConfig = AssetDatabase.LoadAssetAtPath<LegalConfig>("Assets/Data/Legal/LegalConfig.asset");

            if (welcomePrefab == null)
            {
                Debug.LogError("[CreateWelcomeScene] WelcomeOverlay.prefab not found at Assets/Prefabs/UI/Legal/WelcomeOverlay.prefab!");
                return;
            }

            var welcomeInstance = (GameObject)PrefabUtility.InstantiatePrefab(welcomePrefab, canvasObj.transform);
            welcomeInstance.name = "WelcomeCardRoot";
            var welcomeRt = welcomeInstance.GetComponent<RectTransform>();
            welcomeRt.anchorMin = Vector2.zero;
            welcomeRt.anchorMax = Vector2.one;
            welcomeRt.offsetMin = Vector2.zero;
            welcomeRt.offsetMax = Vector2.zero;

            // In standalone scene, disable the inner backdrop dim since the whole background is deep purple
            var innerDim = welcomeInstance.transform.Find("BackdropDim");
            if (innerDim != null)
            {
                var dimImg = innerDim.GetComponent<Image>();
                if (dimImg != null)
                {
                    dimImg.color = new Color(0f, 0f, 0f, 0.25f);
                }
            }

            // Document modal instance
            LegalDocumentModal docModalComp = null;
            if (docModalPrefab != null)
            {
                var docModalInstance = (GameObject)PrefabUtility.InstantiatePrefab(docModalPrefab, canvasObj.transform);
                docModalInstance.name = "LegalDocumentModal";
                docModalComp = docModalInstance.GetComponent<LegalDocumentModal>();
                docModalInstance.SetActive(false);
            }

            // 7. Add & wire WelcomeSceneController
            var controller = canvasObj.AddComponent<WelcomeSceneController>();

            // Find visual components inside welcomeInstance
            var cardRoot = welcomeInstance.transform.Find("CardRoot") as RectTransform;
            var badge = cardRoot != null ? cardRoot.Find("WelcomeBadge") : null;
            var titleText = badge != null ? badge.Find("TitleText")?.GetComponent<Text>() : null;
            var bodyText = cardRoot != null ? cardRoot.Find("BodyText")?.GetComponent<Text>() : null;
            var linksContainer = cardRoot != null ? cardRoot.Find("LinksContainer") : null;
            var termsBtn = linksContainer != null ? linksContainer.Find("TermsLinkButton")?.GetComponent<Button>() : null;
            var termsTxt = termsBtn != null ? termsBtn.transform.Find("Text")?.GetComponent<Text>() : null;
            var privacyBtn = linksContainer != null ? linksContainer.Find("PrivacyLinkButton")?.GetComponent<Button>() : null;
            var privacyTxt = privacyBtn != null ? privacyBtn.transform.Find("Text")?.GetComponent<Text>() : null;
            var acceptBtn = cardRoot != null ? cardRoot.Find("AcceptButton")?.GetComponent<Button>() : null;

            var so = new SerializedObject(controller);
            so.FindProperty("legalConfig").objectReferenceValue = legalConfig;
            so.FindProperty("gameplaySceneName").stringValue = "main";
            so.FindProperty("backgroundFill").objectReferenceValue = bgImg;
            so.FindProperty("cardRoot").objectReferenceValue = cardRoot;
            so.FindProperty("headingText").objectReferenceValue = titleText;
            so.FindProperty("bodyText").objectReferenceValue = bodyText;
            so.FindProperty("termsLinkButton").objectReferenceValue = termsBtn;
            so.FindProperty("termsLinkText").objectReferenceValue = termsTxt;
            so.FindProperty("privacyLinkButton").objectReferenceValue = privacyBtn;
            so.FindProperty("privacyLinkText").objectReferenceValue = privacyTxt;
            so.FindProperty("acceptButton").objectReferenceValue = acceptBtn;
            so.FindProperty("documentModal").objectReferenceValue = docModalComp;
            so.ApplyModifiedProperties();

            // Also configure welcomeOverlay component on the instance if present
            var welcomeComp = welcomeInstance.GetComponent<WelcomeOverlay>();
            if (welcomeComp != null)
            {
                var soWelcome = new SerializedObject(welcomeComp);
                soWelcome.FindProperty("legalConfig").objectReferenceValue = legalConfig;
                soWelcome.FindProperty("documentModal").objectReferenceValue = docModalComp;
                soWelcome.ApplyModifiedProperties();
            }

            // 8. Save the scene
            EditorSceneManager.SaveScene(scene, WelcomeScenePath);
            Debug.Log($"[CreateWelcomeScene] Scene saved successfully to {WelcomeScenePath}");

            // 9. Configure Build Settings: Scene 0 = Welcome, Scene 1 = main
            ConfigureBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void ConfigureBuildSettings()
        {
            var scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(WelcomeScenePath, true),
                new EditorBuildSettingsScene(GameplayScenePath, true)
            };

            EditorBuildSettings.scenes = scenes;
            Debug.Log($"[CreateWelcomeScene] Configured active build settings: [0] {WelcomeScenePath}, [1] {GameplayScenePath}");
            PlayModeStartupSceneManager.ApplyPlayModeStartScene();
        }

        [MenuItem("Tools/Slither Ants Jam/Legal/Preview Welcome Scene (No Data Reset)")]
        public static void PreviewWelcomeScene()
        {
            WelcomeSceneController.ForcePreviewMode = true;
            if (!Application.isPlaying)
            {
                EditorSceneManager.OpenScene(WelcomeScenePath);
                EditorApplication.isPlaying = true;
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("Welcome");
            }
        }
    }
}
#endif
