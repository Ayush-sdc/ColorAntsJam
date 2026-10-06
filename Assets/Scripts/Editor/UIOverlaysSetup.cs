#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using AntsJam.Progression;
using AntsJam.UI;

public static class UIOverlaysSetup
{
    private const string SpritesRoot = "Assets/UI/Sprites";
    private const string ProgressionAssetPath = "Assets/Data/Progression/FeatureProgressionConfig.asset";
    private const string PrefabsFolder = "Assets/Prefabs/UI";

    [MenuItem("Tools/Slither Ants Jam/UI/Configure Sprites and Setup Overlays")]
    public static void RunFullSetup()
    {
        Debug.Log("[UIOverlaysSetup] Starting full UI and Sprites setup...");

        // 1. Configure all new sprites
        ConfigureAllSprites();

        // 2. Create or update FeatureProgressionConfig ScriptableObject
        var config = CreateOrUpdateProgressionConfig();

        // 3. Setup Scene Overlays in active scene
        SetupSceneOverlays(config);

        // 4. Run Tutorial & Legal Setup
        TutorialAndLegalSetup.RunFullSetup();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[UIOverlaysSetup] Full UI setup completed successfully!");
    }

    [MenuItem("Tools/Slither Ants Jam/Tests/Run Tutorial and Legal Tests")]
    public static void RunTutorialAndLegalTests()
    {
        TutorialAndLegalPlayModeTests.RunFullTestSuite();
    }

    public static void ConfigureAllSprites()
    {
        string[] spritePaths = new string[]
        {
            "Assets/UI/Sprites/HardLevel/HardLevel_Overlay.png",
            "Assets/UI/Sprites/Features/Illustration_LinkedAnts.png",
            "Assets/UI/Sprites/Features/Illustration_StackedAnts.png",
            "Assets/UI/Sprites/Features/Illustration_Pipe.png",
            "Assets/UI/Sprites/Features/Illustration_LockedAnts.png",
            "Assets/UI/Sprites/Features/Icon_LinkedAnts.png",
            "Assets/UI/Sprites/Features/Icon_StackedAnts.png",
            "Assets/UI/Sprites/Features/Icon_Pipe.png",
            "Assets/UI/Sprites/Features/Icon_LockedAnts.png",
            "Assets/UI/Sprites/Features/Icon_AllFeaturesCompleted.png",
            "Assets/UI/Sprites/Features/Preview_LinkedAnts.png",
            "Assets/UI/Sprites/Features/Preview_StackedAnts.png",
            "Assets/UI/Sprites/Features/Preview_Pipes.png",
            "Assets/UI/Sprites/Features/Preview_LockedAnts.png",
            "Assets/UI/Sprites/Tutorials/Tutorial_RadialRays.png",
            "Assets/UI/Sprites/Tutorials/Tutorial_Panel.png",
            "Assets/UI/Sprites/Tutorials/Tutorial_OkButton.png",
            "Assets/UI/Sprites/LevelCompleted/LevelCompleted_Title.png",
            "Assets/UI/Sprites/LevelCompleted/Next_Button.png",
            "Assets/UI/Sprites/LevelCompleted/ProgressBar_Bg.png",
            "Assets/UI/Sprites/LevelCompleted/ProgressBar_Fill.png",
            "Assets/UI/Sprites/LevelCompleted/Icon_Glow.png",
            "Assets/UI/Sprites/CampaignCompleted/PlayAgain_Button_Bg.png"
        };

        foreach (var path in spritePaths)
        {
            ConfigureTextureAsSprite(path);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void ConfigureTextureAsSprite(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogWarning($"[UIOverlaysSetup] Texture file not found at: {path}");
            return;
        }

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            bool modified = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                modified = true;
            }
            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                modified = true;
            }
            if (!importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = true;
                modified = true;
            }
            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                modified = true;
            }

            if (modified)
            {
                importer.SaveAndReimport();
                Debug.Log($"[UIOverlaysSetup] Configured as Sprite: {path}");
            }
        }
    }

    public static FeatureProgressionConfig CreateOrUpdateProgressionConfig()
    {
        if (!Directory.Exists("Assets/Data/Progression"))
        {
            Directory.CreateDirectory("Assets/Data/Progression");
        }

        FeatureProgressionConfig config = AssetDatabase.LoadAssetAtPath<FeatureProgressionConfig>(ProgressionAssetPath);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<FeatureProgressionConfig>();
            AssetDatabase.CreateAsset(config, ProgressionAssetPath);
            Debug.Log($"[UIOverlaysSetup] Created new FeatureProgressionConfig at {ProgressionAssetPath}");
        }

        Sprite iconLinked = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Sprites/Features/Preview_LinkedAnts.png") 
            ?? AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Sprites/Features/Icon_LinkedAnts.png");
        Sprite illLinked = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Sprites/Tutorials/FeatureOverlays/Feature_Linked_Ants.png");
        GameObject prefabLinked = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Tutorials/FeatureOverlay_LinkedAnts.prefab");

        Sprite iconStacked = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Sprites/Features/Preview_StackedAnts.png") 
            ?? AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Sprites/Features/Icon_StackedAnts.png");
        Sprite illStacked = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Sprites/Tutorials/FeatureOverlays/Feature_Stacked_Ants.png");
        GameObject prefabStacked = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Tutorials/FeatureOverlay_StackedAnts.prefab");

        Sprite iconPipe = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Sprites/Features/Preview_Pipes.png") 
            ?? AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Sprites/Features/Icon_Pipe.png");
        Sprite illPipe = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Sprites/Tutorials/FeatureOverlays/Feature_Pipe.png");
        GameObject prefabPipe = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Tutorials/FeatureOverlay_Pipes.prefab");

        Sprite iconLocked = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Sprites/Features/Preview_LockedAnts.png") 
            ?? AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Sprites/Features/Icon_LockedAnts.png");
        Sprite illLocked = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Sprites/Tutorials/FeatureOverlays/Feature_Locked_Ants.png");
        GameObject prefabLocked = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Tutorials/FeatureOverlay_LockedAnts.prefab");

        Sprite iconCompleted = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Sprites/Features/Icon_AllFeaturesCompleted.png");

        var list = new List<FeatureDefinition>
        {
            new FeatureDefinition(
                "linked_ants",
                "Linked Ants",
                startMilestoneLevel: 1,
                introductionLevel: 7,
                displayOrder: 1,
                tutorialTitle: "Linked Ants!",
                tutorialDescription: "Linked ant queues move together.",
                previewIcon: iconLinked,
                tutorialArtwork: illLinked,
                tutorialPrefab: prefabLinked
            ),
            new FeatureDefinition(
                "stacked_ants",
                "Stacked Ants",
                startMilestoneLevel: 7,
                introductionLevel: 15,
                displayOrder: 2,
                tutorialTitle: "Stacked Ants!",
                tutorialDescription: "Send stacked ant queues to the holes in order.",
                previewIcon: iconStacked,
                tutorialArtwork: illStacked,
                tutorialPrefab: prefabStacked
            ),
            new FeatureDefinition(
                "pipes",
                "Pipes",
                startMilestoneLevel: 15,
                introductionLevel: 21,
                displayOrder: 3,
                tutorialTitle: "Pipe!",
                tutorialDescription: "Pass ant queues through the pipe to break it.",
                previewIcon: iconPipe,
                tutorialArtwork: illPipe,
                tutorialPrefab: prefabPipe
            ),
            new FeatureDefinition(
                "locked_ants",
                "Locked Ants",
                startMilestoneLevel: 21,
                introductionLevel: 30,
                displayOrder: 4,
                tutorialTitle: "Locked Ants!",
                tutorialDescription: "Rescue the required number of ant queues to unlock it.",
                previewIcon: iconLocked,
                tutorialArtwork: illLocked,
                tutorialPrefab: prefabLocked
            )
        };

        config.SetFeaturesList(list, iconCompleted);
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();

        return config;
    }

    public static void SetupSceneOverlays(FeatureProgressionConfig config)
    {
        var canvasObj = GameObject.Find("GameUICanvas");
        if (canvasObj == null)
        {
            Debug.LogError("[UIOverlaysSetup] GameUICanvas not found in active scene!");
            return;
        }

        var uiManager = canvasObj.GetComponent<GameUIManager>();
        if (uiManager == null)
        {
            Debug.LogError("[UIOverlaysSetup] GameUIManager not found on GameUICanvas!");
            return;
        }

        if (!Directory.Exists(PrefabsFolder))
        {
            Directory.CreateDirectory(PrefabsFolder);
        }

        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // 1. LevelCompletedOverlay
        var lcOverlay = SetupLevelCompletedOverlay(canvasObj.transform, defaultFont, config);

        // 2. FeatureTutorialModal
        var ftModal = SetupFeatureTutorialModal(canvasObj.transform, defaultFont);

        // 3. HardLevelIntroOverlay
        var hiOverlay = SetupHardLevelIntroOverlay(canvasObj.transform);

        // 4. CampaignCompletedOverlay
        var ccOverlay = SetupCampaignCompletedOverlay(canvasObj.transform, defaultFont);

        // Wire references into GameUIManager
        uiManager.SetOverlayReferences(lcOverlay, ftModal, hiOverlay, config, ccOverlay);
        EditorUtility.SetDirty(uiManager);

        // Save prefabs
        SaveAsPrefab(lcOverlay.gameObject, $"{PrefabsFolder}/LevelCompletedOverlay.prefab");
        SaveAsPrefab(ftModal.gameObject, $"{PrefabsFolder}/FeatureTutorialModal.prefab");
        SaveAsPrefab(hiOverlay.gameObject, $"{PrefabsFolder}/HardLevelIntroOverlay.prefab");
        SaveAsPrefab(ccOverlay.gameObject, $"{PrefabsFolder}/CampaignCompletedOverlay.prefab");

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("[UIOverlaysSetup] Scene saved with all overlays wired!");
    }

    private static LevelCompletedOverlay SetupLevelCompletedOverlay(Transform canvasTrans, Font font, FeatureProgressionConfig config)
    {
        Transform existing = canvasTrans.Find("LevelCompletedOverlay");
        GameObject obj;
        if (existing != null)
        {
            obj = existing.gameObject;
        }
        else
        {
            obj = new GameObject("LevelCompletedOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            obj.transform.SetParent(canvasTrans, false);
        }

        // Full screen rect
        var rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var bgImg = obj.GetComponent<Image>();
        bgImg.color = new Color(0.02f, 0.03f, 0.06f, 0.88f);
        bgImg.raycastTarget = true;

        // Clean existing children
        for (int i = obj.transform.childCount - 1; i >= 0; i--)
        {
            UnityEngine.Object.DestroyImmediate(obj.transform.GetChild(i).gameObject);
        }

        // ContentRoot
        GameObject contentRoot = new GameObject("ContentRoot", typeof(RectTransform));
        contentRoot.transform.SetParent(obj.transform, false);
        var contentRt = contentRoot.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0.5f, 0.5f);
        contentRt.anchorMax = new Vector2(0.5f, 0.5f);
        contentRt.sizeDelta = new Vector2(800f, 1200f);
        contentRt.anchoredPosition = Vector2.zero;

        // Title Image
        GameObject titleObj = new GameObject("TitleImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        titleObj.transform.SetParent(contentRoot.transform, false);
        var titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchoredPosition = new Vector2(0f, 290f);
        titleRt.sizeDelta = new Vector2(720f, 298f);
        var titleImg = titleObj.GetComponent<Image>();
        titleImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Sprites/LevelCompleted/LevelCompleted_Title.png");
        titleImg.preserveAspect = true;
        titleImg.raycastTarget = false;

        // NextFeatureLabel
        GameObject labelObj = new GameObject("NextFeatureLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        labelObj.transform.SetParent(contentRoot.transform, false);
        var labelRt = labelObj.GetComponent<RectTransform>();
        labelRt.anchoredPosition = new Vector2(0f, 100f);
        labelRt.sizeDelta = new Vector2(600f, 50f);
        var labelTxt = labelObj.GetComponent<Text>();
        labelTxt.font = font;
        labelTxt.fontSize = 26;
        labelTxt.fontStyle = FontStyle.Bold;
        labelTxt.alignment = TextAnchor.MiddleCenter;
        labelTxt.color = Color.white;
        labelTxt.text = "Next Feature: Linked Ants";
        var labelOutline = labelObj.AddComponent<Outline>();
        labelOutline.effectColor = new Color(0.10f, 0.35f, 0.75f, 0.9f);
        labelOutline.effectDistance = new Vector2(1.5f, -1.5f);

        // ProgressBar Container
        GameObject progContainer = new GameObject("ProgressBarContainer", typeof(RectTransform));
        progContainer.transform.SetParent(contentRoot.transform, false);
        var progContainerRt = progContainer.GetComponent<RectTransform>();
        progContainerRt.anchoredPosition = new Vector2(0f, 20f);
        progContainerRt.sizeDelta = new Vector2(640f, 90f);

        // Progress Bg
        GameObject progBgObj = new GameObject("ProgressBarBg", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        progBgObj.transform.SetParent(progContainer.transform, false);
        var progBgRt = progBgObj.GetComponent<RectTransform>();
        progBgRt.anchoredPosition = new Vector2(-40f, 0f);
        progBgRt.sizeDelta = new Vector2(500f, 75f);
        var progBgImg = progBgObj.GetComponent<Image>();
        progBgImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Sprites/LevelCompleted/ProgressBar_Bg.png");
        progBgImg.type = Image.Type.Sliced;
        progBgImg.raycastTarget = false;

        // Progress Fill
        GameObject progFillObj = new GameObject("ProgressBarFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        progFillObj.transform.SetParent(progBgObj.transform, false);
        var progFillRt = progFillObj.GetComponent<RectTransform>();
        progFillRt.anchorMin = new Vector2(0.02f, 0.12f);
        progFillRt.anchorMax = new Vector2(0.98f, 0.88f);
        progFillRt.offsetMin = Vector2.zero;
        progFillRt.offsetMax = Vector2.zero;
        var progFillImg = progFillObj.GetComponent<Image>();
        progFillImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Sprites/LevelCompleted/ProgressBar_Fill.png");
        progFillImg.type = Image.Type.Filled;
        progFillImg.fillMethod = Image.FillMethod.Horizontal;
        progFillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
        progFillImg.fillAmount = 0f;
        progFillImg.raycastTarget = false;

        // Percent Text
        GameObject percentObj = new GameObject("PercentText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        percentObj.transform.SetParent(progBgObj.transform, false);
        var percentRt = percentObj.GetComponent<RectTransform>();
        percentRt.anchorMin = Vector2.zero;
        percentRt.anchorMax = Vector2.one;
        percentRt.offsetMin = Vector2.zero;
        percentRt.offsetMax = Vector2.zero;
        var percentTxt = percentObj.GetComponent<Text>();
        percentTxt.font = font;
        percentTxt.fontSize = 28;
        percentTxt.fontStyle = FontStyle.Bold;
        percentTxt.alignment = TextAnchor.MiddleCenter;
        percentTxt.color = Color.white;
        percentTxt.text = "0%";
        var percentOutline = percentObj.AddComponent<Outline>();
        percentOutline.effectColor = new Color(0.05f, 0.15f, 0.35f, 0.9f);
        percentOutline.effectDistance = new Vector2(1.5f, -1.5f);

        // Feature Icon Glow
        GameObject glowObj = new GameObject("FeatureIconGlow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        glowObj.transform.SetParent(progContainer.transform, false);
        var glowRt = glowObj.GetComponent<RectTransform>();
        glowRt.anchoredPosition = new Vector2(250f, 0f);
        glowRt.sizeDelta = new Vector2(130f, 130f);
        var glowImg = glowObj.GetComponent<Image>();
        glowImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Sprites/LevelCompleted/Icon_Glow.png");
        glowImg.raycastTarget = false;

        // Feature Icon
        GameObject iconObj = new GameObject("FeatureIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconObj.transform.SetParent(progContainer.transform, false);
        var iconRt = iconObj.GetComponent<RectTransform>();
        iconRt.anchoredPosition = new Vector2(250f, 0f);
        iconRt.sizeDelta = new Vector2(100f, 100f);
        var iconImg = iconObj.GetComponent<Image>();
        iconImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Sprites/Features/Icon_LinkedAnts.png");
        iconImg.preserveAspect = true;
        iconImg.raycastTarget = false;

        // NEXT Button
        GameObject nextBtnObj = new GameObject("NextButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        nextBtnObj.transform.SetParent(contentRoot.transform, false);
        var nextBtnRt = nextBtnObj.GetComponent<RectTransform>();
        nextBtnRt.anchoredPosition = new Vector2(0f, -220f);
        nextBtnRt.sizeDelta = new Vector2(376f, 128f);
        var nextBtnImg = nextBtnObj.GetComponent<Image>();
        nextBtnImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Sprites/LevelCompleted/Next_Button.png");
        nextBtnImg.preserveAspect = true;
        nextBtnImg.raycastTarget = true;
        var nextButton = nextBtnObj.GetComponent<Button>();

        GameObject nextTxtObj = new GameObject("ButtonText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        nextTxtObj.transform.SetParent(nextBtnObj.transform, false);
        var nextTxtRt = nextTxtObj.GetComponent<RectTransform>();
        nextTxtRt.anchorMin = Vector2.zero;
        nextTxtRt.anchorMax = Vector2.one;
        nextTxtRt.offsetMin = Vector2.zero;
        nextTxtRt.offsetMax = Vector2.zero;
        var nextTxt = nextTxtObj.GetComponent<Text>();
        nextTxt.font = font;
        nextTxt.fontSize = 34;
        nextTxt.fontStyle = FontStyle.Bold;
        nextTxt.alignment = TextAnchor.MiddleCenter;
        nextTxt.color = Color.white;
        nextTxt.text = "NEXT";
        var nextOutline = nextTxtObj.AddComponent<Outline>();
        nextOutline.effectColor = new Color(0.12f, 0.40f, 0.15f, 0.9f);
        nextOutline.effectDistance = new Vector2(1.8f, -1.8f);

        // Attach LevelCompletedOverlay component and wire fields
        var controller = obj.GetComponent<LevelCompletedOverlay>();
        if (controller == null) controller = obj.AddComponent<LevelCompletedOverlay>();

        // Wire serialized fields via SerializedObject
        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("backdropDim").objectReferenceValue = bgImg;
        so.FindProperty("contentRoot").objectReferenceValue = contentRt;
        so.FindProperty("titleImage").objectReferenceValue = titleImg;
        so.FindProperty("nextFeatureLabel").objectReferenceValue = labelTxt;
        so.FindProperty("progressBarBg").objectReferenceValue = progBgImg;
        so.FindProperty("progressBarFill").objectReferenceValue = progFillImg;
        so.FindProperty("progressPercentText").objectReferenceValue = percentTxt;
        so.FindProperty("featureIconGlow").objectReferenceValue = glowImg;
        so.FindProperty("featureIcon").objectReferenceValue = iconImg;
        so.FindProperty("nextButton").objectReferenceValue = nextButton;
        so.FindProperty("nextButtonText").objectReferenceValue = nextTxt;
        so.FindProperty("progressionConfig").objectReferenceValue = config;
        so.ApplyModifiedProperties();

        obj.SetActive(false);
        return controller;
    }

    private static FeatureTutorialModal SetupFeatureTutorialModal(Transform canvasTrans, Font font)
    {
        Transform existing = canvasTrans.Find("FeatureTutorialModal");
        GameObject obj;
        if (existing != null)
        {
            obj = existing.gameObject;
        }
        else
        {
            obj = new GameObject("FeatureTutorialModal", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            obj.transform.SetParent(canvasTrans, false);
        }

        var rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var bgImg = obj.GetComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.78f);
        bgImg.raycastTarget = true;

        for (int i = obj.transform.childCount - 1; i >= 0; i--)
        {
            UnityEngine.Object.DestroyImmediate(obj.transform.GetChild(i).gameObject);
        }

        // ModalRoot
        GameObject modalRoot = new GameObject("ModalRoot", typeof(RectTransform));
        modalRoot.transform.SetParent(obj.transform, false);
        var modalRt = modalRoot.GetComponent<RectTransform>();
        modalRt.anchorMin = new Vector2(0.5f, 0.5f);
        modalRt.anchorMax = new Vector2(0.5f, 0.5f);
        modalRt.sizeDelta = new Vector2(800f, 1200f);
        modalRt.anchoredPosition = Vector2.zero;

        // TitleText
        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        titleObj.transform.SetParent(modalRoot.transform, false);
        var titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchoredPosition = new Vector2(0f, 400f);
        titleRt.sizeDelta = new Vector2(700f, 75f);
        var titleTxt = titleObj.GetComponent<Text>();
        titleTxt.font = font;
        titleTxt.fontSize = 42;
        titleTxt.fontStyle = FontStyle.Bold;
        titleTxt.alignment = TextAnchor.MiddleCenter;
        titleTxt.color = Color.white;
        titleTxt.text = "Linked Ants!";
        var titleOutline = titleObj.AddComponent<Outline>();
        titleOutline.effectColor = new Color(0.12f, 0.45f, 0.90f, 0.95f);
        titleOutline.effectDistance = new Vector2(2f, -2f);

        // Illustration Area
        GameObject illArea = new GameObject("IllustrationArea", typeof(RectTransform));
        illArea.transform.SetParent(modalRoot.transform, false);
        var illAreaRt = illArea.GetComponent<RectTransform>();
        illAreaRt.anchoredPosition = new Vector2(0f, 150f);
        illAreaRt.sizeDelta = new Vector2(520f, 520f);

        // Radial Rays
        GameObject raysObj = new GameObject("RadialRays", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        raysObj.transform.SetParent(illArea.transform, false);
        var raysRt = raysObj.GetComponent<RectTransform>();
        raysRt.anchorMin = Vector2.zero;
        raysRt.anchorMax = Vector2.one;
        raysRt.offsetMin = Vector2.zero;
        raysRt.offsetMax = Vector2.zero;
        var raysImg = raysObj.GetComponent<Image>();
        raysImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Sprites/Tutorials/Tutorial_RadialRays.png");
        raysImg.raycastTarget = false;

        // Illustration Image
        GameObject illObj = new GameObject("IllustrationImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        illObj.transform.SetParent(illArea.transform, false);
        var illRt = illObj.GetComponent<RectTransform>();
        illRt.anchorMin = new Vector2(0.1f, 0.1f);
        illRt.anchorMax = new Vector2(0.9f, 0.9f);
        illRt.offsetMin = Vector2.zero;
        illRt.offsetMax = Vector2.zero;
        var illImg = illObj.GetComponent<Image>();
        illImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Sprites/Features/Illustration_LinkedAnts.png");
        illImg.preserveAspect = true;
        illImg.raycastTarget = false;

        // Description Panel
        GameObject panelObj = new GameObject("DescriptionPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelObj.transform.SetParent(modalRoot.transform, false);
        var panelRt = panelObj.GetComponent<RectTransform>();
        panelRt.anchoredPosition = new Vector2(0f, -160f);
        panelRt.sizeDelta = new Vector2(600f, 180f);
        var panelImg = panelObj.GetComponent<Image>();
        panelImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Sprites/Tutorials/Tutorial_Panel.png");
        panelImg.type = Image.Type.Sliced;
        panelImg.raycastTarget = false;

        // Description Text
        GameObject descTxtObj = new GameObject("DescriptionText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        descTxtObj.transform.SetParent(panelObj.transform, false);
        var descTxtRt = descTxtObj.GetComponent<RectTransform>();
        descTxtRt.anchorMin = new Vector2(0.06f, 0.1f);
        descTxtRt.anchorMax = new Vector2(0.94f, 0.9f);
        descTxtRt.offsetMin = Vector2.zero;
        descTxtRt.offsetMax = Vector2.zero;
        var descTxt = descTxtObj.GetComponent<Text>();
        descTxt.font = font;
        descTxt.fontSize = 24;
        descTxt.fontStyle = FontStyle.Bold;
        descTxt.alignment = TextAnchor.MiddleCenter;
        descTxt.color = Color.white;
        descTxt.text = "Linked ant queues move together.";

        // OK Button
        GameObject okBtnObj = new GameObject("OkButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        okBtnObj.transform.SetParent(modalRoot.transform, false);
        var okBtnRt = okBtnObj.GetComponent<RectTransform>();
        okBtnRt.anchoredPosition = new Vector2(0f, -320f);
        okBtnRt.sizeDelta = new Vector2(340f, 110f);
        var okBtnImg = okBtnObj.GetComponent<Image>();
        okBtnImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Sprites/Tutorials/Tutorial_OkButton.png");
        okBtnImg.preserveAspect = true;
        okBtnImg.raycastTarget = true;
        var okBtn = okBtnObj.GetComponent<Button>();

        GameObject okTxtObj = new GameObject("OkText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        okTxtObj.transform.SetParent(okBtnObj.transform, false);
        var okTxtRt = okTxtObj.GetComponent<RectTransform>();
        okTxtRt.anchorMin = Vector2.zero;
        okTxtRt.anchorMax = Vector2.one;
        okTxtRt.offsetMin = Vector2.zero;
        okTxtRt.offsetMax = Vector2.zero;
        var okTxt = okTxtObj.GetComponent<Text>();
        okTxt.font = font;
        okTxt.fontSize = 32;
        okTxt.fontStyle = FontStyle.Bold;
        okTxt.alignment = TextAnchor.MiddleCenter;
        okTxt.color = Color.white;
        okTxt.text = "OK";
        var okOutline = okTxtObj.AddComponent<Outline>();
        okOutline.effectColor = new Color(0.12f, 0.40f, 0.15f, 0.9f);
        okOutline.effectDistance = new Vector2(1.8f, -1.8f);

        // Custom Prefab Container (for full flattened tutorial compositions)
        GameObject containerObj = new GameObject("CustomPrefabContainer", typeof(RectTransform));
        containerObj.transform.SetParent(obj.transform, false);
        var containerRt = containerObj.GetComponent<RectTransform>();
        containerRt.anchorMin = Vector2.zero;
        containerRt.anchorMax = Vector2.one;
        containerRt.offsetMin = Vector2.zero;
        containerRt.offsetMax = Vector2.zero;
        containerRt.localScale = Vector3.one;
        containerRt.SetAsLastSibling();

        // Attach FeatureTutorialModal component
        var controller = obj.GetComponent<FeatureTutorialModal>();
        if (controller == null) controller = obj.AddComponent<FeatureTutorialModal>();

        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("backdropDim").objectReferenceValue = bgImg;
        so.FindProperty("modalRoot").objectReferenceValue = modalRt;
        so.FindProperty("titleText").objectReferenceValue = titleTxt;
        so.FindProperty("titleOutline").objectReferenceValue = titleOutline;
        so.FindProperty("radialRaysImage").objectReferenceValue = raysImg;
        so.FindProperty("illustrationImage").objectReferenceValue = illImg;
        so.FindProperty("customPrefabContainer").objectReferenceValue = containerRt;
        so.FindProperty("descriptionPanel").objectReferenceValue = panelImg;
        so.FindProperty("descriptionText").objectReferenceValue = descTxt;
        so.FindProperty("okButton").objectReferenceValue = okBtn;
        so.FindProperty("okButtonText").objectReferenceValue = okTxt;
        so.ApplyModifiedProperties();

        obj.SetActive(false);
        return controller;
    }

    private static HardLevelIntroOverlay SetupHardLevelIntroOverlay(Transform canvasTrans)
    {
        Transform existing = canvasTrans.Find("HardLevelIntroOverlay");
        GameObject obj;
        if (existing != null)
        {
            obj = existing.gameObject;
        }
        else
        {
            obj = new GameObject("HardLevelIntroOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            obj.transform.SetParent(canvasTrans, false);
        }

        var rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var bgImg = obj.GetComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.72f);
        bgImg.raycastTarget = true;

        for (int i = obj.transform.childCount - 1; i >= 0; i--)
        {
            UnityEngine.Object.DestroyImmediate(obj.transform.GetChild(i).gameObject);
        }

        // EmblemRoot
        GameObject emblemRoot = new GameObject("EmblemRoot", typeof(RectTransform));
        emblemRoot.transform.SetParent(obj.transform, false);
        var emblemRt = emblemRoot.GetComponent<RectTransform>();
        emblemRt.anchorMin = new Vector2(0.5f, 0.5f);
        emblemRt.anchorMax = new Vector2(0.5f, 0.5f);
        emblemRt.anchoredPosition = new Vector2(0f, 20f);
        emblemRt.sizeDelta = new Vector2(680f, 550f);

        // EmblemImage
        GameObject emblemImgObj = new GameObject("EmblemImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        emblemImgObj.transform.SetParent(emblemRoot.transform, false);
        var emblemImgRt = emblemImgObj.GetComponent<RectTransform>();
        emblemImgRt.anchorMin = Vector2.zero;
        emblemImgRt.anchorMax = Vector2.one;
        emblemImgRt.offsetMin = Vector2.zero;
        emblemImgRt.offsetMax = Vector2.zero;
        var emblemImg = emblemImgObj.GetComponent<Image>();
        emblemImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Sprites/HardLevel/HardLevel_Overlay.png");
        emblemImg.preserveAspect = true;
        emblemImg.raycastTarget = false;

        var controller = obj.GetComponent<HardLevelIntroOverlay>();
        if (controller == null) controller = obj.AddComponent<HardLevelIntroOverlay>();

        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("backdropDim").objectReferenceValue = bgImg;
        so.FindProperty("emblemRoot").objectReferenceValue = emblemRt;
        so.FindProperty("emblemImage").objectReferenceValue = emblemImg;
        so.FindProperty("popInDuration").floatValue = 0.35f;
        so.FindProperty("holdDuration").floatValue = 1.15f;
        so.FindProperty("fadeOutDuration").floatValue = 0.30f;
        so.ApplyModifiedProperties();

        obj.SetActive(false);
        return controller;
    }

    private static CampaignCompletedOverlay SetupCampaignCompletedOverlay(Transform canvasTrans, Font font)
    {
        Transform existing = canvasTrans.Find("CampaignCompletedOverlay");
        GameObject obj;
        if (existing != null)
        {
            obj = existing.gameObject;
        }
        else
        {
            obj = new GameObject("CampaignCompletedOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            obj.transform.SetParent(canvasTrans, false);
        }

        // Full screen rect
        var rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var bgImg = obj.GetComponent<Image>();
        bgImg.color = new Color(0.02f, 0.03f, 0.06f, 0.92f);
        bgImg.raycastTarget = true;

        // Clean existing children
        for (int i = obj.transform.childCount - 1; i >= 0; i--)
        {
            UnityEngine.Object.DestroyImmediate(obj.transform.GetChild(i).gameObject);
        }

        // ContentRoot
        GameObject contentRoot = new GameObject("ContentRoot", typeof(RectTransform));
        contentRoot.transform.SetParent(obj.transform, false);
        var contentRt = contentRoot.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0.5f, 0.5f);
        contentRt.anchorMax = new Vector2(0.5f, 0.5f);
        contentRt.sizeDelta = new Vector2(800f, 1200f);
        contentRt.anchoredPosition = Vector2.zero;

        // Title Text: "ALL LEVELS COMPLETED!"
        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        titleObj.transform.SetParent(contentRoot.transform, false);
        var titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchoredPosition = new Vector2(0f, 320f);
        titleRt.sizeDelta = new Vector2(740f, 100f);
        var titleTxt = titleObj.GetComponent<Text>();
        titleTxt.font = font;
        titleTxt.fontSize = 42;
        titleTxt.fontStyle = FontStyle.Bold;
        titleTxt.alignment = TextAnchor.MiddleCenter;
        titleTxt.color = new Color(1.0f, 0.85f, 0.25f, 1.0f); // Bright celebratory gold
        titleTxt.text = "ALL LEVELS COMPLETED!";
        var titleOutline = titleObj.AddComponent<Outline>();
        titleOutline.effectColor = new Color(0.70f, 0.28f, 0.02f, 0.95f);
        titleOutline.effectDistance = new Vector2(2.5f, -2.5f);

        // Radial Rays behind the cute ants
        GameObject raysObj = new GameObject("RadialRays", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        raysObj.transform.SetParent(contentRoot.transform, false);
        var raysRt = raysObj.GetComponent<RectTransform>();
        raysRt.anchoredPosition = new Vector2(0f, 80f);
        raysRt.sizeDelta = new Vector2(560f, 560f);
        var raysImg = raysObj.GetComponent<Image>();
        raysImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Sprites/Tutorials/Tutorial_RadialRays.png");
        raysImg.color = new Color(1f, 0.9f, 0.5f, 0.65f);
        raysImg.raycastTarget = false;

        // Celebratory Illustration using existing cute ants
        GameObject illustObj = new GameObject("Illustration", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        illustObj.transform.SetParent(contentRoot.transform, false);
        var illustRt = illustObj.GetComponent<RectTransform>();
        illustRt.anchoredPosition = new Vector2(0f, 80f);
        illustRt.sizeDelta = new Vector2(460f, 380f);
        var illustImg = illustObj.GetComponent<Image>();
        illustImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Sprites/Features/Illustration_LinkedAnts.png");
        illustImg.preserveAspect = true;
        illustImg.raycastTarget = false;

        // Message Text: "You rescued every ant queue!"
        GameObject msgObj = new GameObject("MessageText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        msgObj.transform.SetParent(contentRoot.transform, false);
        var msgRt = msgObj.GetComponent<RectTransform>();
        msgRt.anchoredPosition = new Vector2(0f, -140f);
        msgRt.sizeDelta = new Vector2(650f, 60f);
        var msgTxt = msgObj.GetComponent<Text>();
        msgTxt.font = font;
        msgTxt.fontSize = 28;
        msgTxt.fontStyle = FontStyle.Bold;
        msgTxt.alignment = TextAnchor.MiddleCenter;
        msgTxt.color = Color.white;
        msgTxt.text = "You rescued every ant queue!";
        var msgOutline = msgObj.AddComponent<Outline>();
        msgOutline.effectColor = new Color(0.08f, 0.25f, 0.55f, 0.9f);
        msgOutline.effectDistance = new Vector2(1.8f, -1.8f);

        // PLAY AGAIN Button
        GameObject playAgainBtnObj = new GameObject("PlayAgainButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        playAgainBtnObj.transform.SetParent(contentRoot.transform, false);
        var playAgainBtnRt = playAgainBtnObj.GetComponent<RectTransform>();
        playAgainBtnRt.anchoredPosition = new Vector2(0f, -250f);
        playAgainBtnRt.sizeDelta = new Vector2(376f, 128f);
        var playAgainImg = playAgainBtnObj.GetComponent<Image>();
        playAgainImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Sprites/CampaignCompleted/PlayAgain_Button_Bg.png");
        playAgainImg.preserveAspect = true;
        playAgainImg.raycastTarget = true;
        var playAgainBtn = playAgainBtnObj.GetComponent<Button>();

        GameObject playAgainTxtObj = new GameObject("ButtonText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        playAgainTxtObj.transform.SetParent(playAgainBtnObj.transform, false);
        var playAgainTxtRt = playAgainTxtObj.GetComponent<RectTransform>();
        playAgainTxtRt.anchorMin = Vector2.zero;
        playAgainTxtRt.anchorMax = Vector2.one;
        playAgainTxtRt.offsetMin = Vector2.zero;
        playAgainTxtRt.offsetMax = Vector2.zero;
        var playAgainTxt = playAgainTxtObj.GetComponent<Text>();
        playAgainTxt.font = font;
        playAgainTxt.fontSize = 32;
        playAgainTxt.fontStyle = FontStyle.Bold;
        playAgainTxt.alignment = TextAnchor.MiddleCenter;
        playAgainTxt.color = Color.white;
        playAgainTxt.text = "PLAY AGAIN";
        var playAgainOutline = playAgainTxtObj.AddComponent<Outline>();
        playAgainOutline.effectColor = new Color(0.10f, 0.40f, 0.12f, 0.95f);
        playAgainOutline.effectDistance = new Vector2(2f, -2f);

        var controller = obj.GetComponent<CampaignCompletedOverlay>();
        if (controller == null) controller = obj.AddComponent<CampaignCompletedOverlay>();

        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("backdropDim").objectReferenceValue = bgImg;
        so.FindProperty("contentRoot").objectReferenceValue = contentRt;
        so.FindProperty("titleText").objectReferenceValue = titleTxt;
        so.FindProperty("radialRays").objectReferenceValue = raysImg;
        so.FindProperty("illustrationImage").objectReferenceValue = illustImg;
        so.FindProperty("messageText").objectReferenceValue = msgTxt;
        so.FindProperty("playAgainButton").objectReferenceValue = playAgainBtn;
        so.FindProperty("playAgainButtonText").objectReferenceValue = playAgainTxt;
        so.ApplyModifiedProperties();

        obj.SetActive(false);
        return controller;
    }

    private static void SaveAsPrefab(GameObject instance, string prefabPath)
    {
        try
        {
            PrefabUtility.SaveAsPrefabAssetAndConnect(instance, prefabPath, InteractionMode.AutomatedAction);
            Debug.Log($"[UIOverlaysSetup] Saved prefab: {prefabPath}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[UIOverlaysSetup] Could not save prefab at {prefabPath}: {ex.Message}");
        }
    }
}
#endif
