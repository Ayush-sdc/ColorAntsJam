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
using AntsJam.Legal;

public static class TutorialAndLegalSetup
{
    private const string LegalSpritesRoot = "Assets/UI/Sprites/Legal";
    private const string TutorialSpritesRoot = "Assets/UI/Sprites/Tutorials/FeatureOverlays";
    private const string LegalConfigPath = "Assets/Data/Legal/LegalConfig.asset";
    private const string ProgressionConfigPath = "Assets/Data/Progression/FeatureProgressionConfig.asset";
    private const string TutorialPrefabsFolder = "Assets/Prefabs/UI/Tutorials";
    private const string LegalPrefabsFolder = "Assets/Prefabs/UI/Legal";

    [MenuItem("Tools/Slither Ants Jam/UI/Run Full Tutorial & Legal Setup")]
    public static void RunFullSetup()
    {
        Debug.Log("[TutorialAndLegalSetup] Starting full setup of Feature Tutorials and Legal Overlays...");

        // 1. Configure texture import settings & borders
        ConfigureAllSprites();

        // 2. Setup LegalConfig ScriptableObject
        var legalConfig = CreateOrUpdateLegalConfig();

        // 3. Create Feature Overlay Prefabs
        CreateFeatureOverlayPrefabs();

        // 4. Update Feature Progression Config
        UpdateProgressionConfig();

        // 5. Create Legal and Welcome Prefabs
        var docModalPrefab = CreateLegalDocumentModalPrefab();
        var termsModalPrefab = CreateLegalTermsModalPrefab(docModalPrefab);
        var welcomePrefab = CreateWelcomeOverlayPrefab(docModalPrefab);

        // 6. Setup Scene Overlays in active scene
        SetupSceneOverlays(legalConfig, docModalPrefab, termsModalPrefab, welcomePrefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[TutorialAndLegalSetup] Full Tutorial & Legal Setup completed successfully!");
    }

    public static void ConfigureAllSprites()
    {
        // 1. Feature Tutorials
        string[] tutorialSprites = new string[]
        {
            Path.Combine(TutorialSpritesRoot, "Feature_Linked_Ants.png"),
            Path.Combine(TutorialSpritesRoot, "Feature_Stacked_Ants.png"),
            Path.Combine(TutorialSpritesRoot, "Feature_Pipe.png"),
            Path.Combine(TutorialSpritesRoot, "Feature_Locked_Ants.png")
        };

        foreach (var p in tutorialSprites)
        {
            ConfigureTextureAsSprite(p, Vector4.zero);
        }

        // 2. Legal & Welcome Sprites with 9-slice borders
        ConfigureTextureAsSprite(Path.Combine(LegalSpritesRoot, "Legal_OuterFrame.png"), new Vector4(36, 36, 36, 36));
        ConfigureTextureAsSprite(Path.Combine(LegalSpritesRoot, "Legal_InnerPanel.png"), new Vector4(28, 28, 28, 28));
        ConfigureTextureAsSprite(Path.Combine(LegalSpritesRoot, "Legal_BlueButton.png"), new Vector4(32, 32, 32, 32));
        ConfigureTextureAsSprite(Path.Combine(LegalSpritesRoot, "Legal_CloseButton.png"), Vector4.zero);
        ConfigureTextureAsSprite(Path.Combine(LegalSpritesRoot, "Legal_CopyIcon.png"), Vector4.zero);
        ConfigureTextureAsSprite(Path.Combine(LegalSpritesRoot, "Welcome_CardTop.png"), new Vector4(36, 36, 36, 36));
        ConfigureTextureAsSprite(Path.Combine(LegalSpritesRoot, "Welcome_CardBottom.png"), new Vector4(28, 28, 28, 28));
        ConfigureTextureAsSprite(Path.Combine(LegalSpritesRoot, "Welcome_Badge.png"), new Vector4(28, 28, 28, 28));
        ConfigureTextureAsSprite(Path.Combine(LegalSpritesRoot, "Welcome_AcceptButton.png"), new Vector4(32, 32, 32, 32));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void ConfigureTextureAsSprite(string path, Vector4 border)
    {
        if (!File.Exists(path))
        {
            Debug.LogWarning($"[TutorialAndLegalSetup] Sprite file not found at: {path}");
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
            if (importer.wrapMode != TextureWrapMode.Clamp)
            {
                importer.wrapMode = TextureWrapMode.Clamp;
                modified = true;
            }
            if (importer.maxTextureSize < 2048)
            {
                importer.maxTextureSize = 2048;
                modified = true;
            }
            if (border != Vector4.zero && importer.spriteBorder != border)
            {
                importer.spriteBorder = border;
                modified = true;
            }

            if (modified)
            {
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
            }
        }
    }

    public static LegalConfig CreateOrUpdateLegalConfig()
    {
        EnsureFolderExists("Assets/Data/Legal");
        LegalConfig config = AssetDatabase.LoadAssetAtPath<LegalConfig>(LegalConfigPath);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<LegalConfig>();
            AssetDatabase.CreateAsset(config, LegalConfigPath);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log($"[TutorialAndLegalSetup] Created new LegalConfig at {LegalConfigPath}");
        }
        return config;
    }

    public static void CreateFeatureOverlayPrefabs()
    {
        EnsureFolderExists(TutorialPrefabsFolder);

        var features = new (string id, string spriteName, string prefabName)[]
        {
            ("linked_ants", "Feature_Linked_Ants.png", "FeatureOverlay_LinkedAnts.prefab"),
            ("stacked_ants", "Feature_Stacked_Ants.png", "FeatureOverlay_StackedAnts.prefab"),
            ("pipes", "Feature_Pipe.png", "FeatureOverlay_Pipes.prefab"),
            ("locked_ants", "Feature_Locked_Ants.png", "FeatureOverlay_LockedAnts.prefab")
        };

        foreach (var feat in features)
        {
            string spritePath = Path.Combine(TutorialSpritesRoot, feat.spriteName);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            string prefabPath = Path.Combine(TutorialPrefabsFolder, feat.prefabName);

            GameObject root = new GameObject(Path.GetFileNameWithoutExtension(feat.prefabName), typeof(RectTransform), typeof(FeatureOverlayView));
            var rootRt = root.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            // Backdrop Dim
            GameObject dimObj = new GameObject("BackdropDim", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            dimObj.transform.SetParent(root.transform, false);
            var dimRt = dimObj.GetComponent<RectTransform>();
            dimRt.anchorMin = Vector2.zero;
            dimRt.anchorMax = Vector2.one;
            dimRt.offsetMin = Vector2.zero;
            dimRt.offsetMax = Vector2.zero;
            var dimImg = dimObj.GetComponent<Image>();
            dimImg.color = new Color(0f, 0f, 0f, 0.78f);
            dimImg.raycastTarget = true;

            // Content Container with safe margin
            GameObject contentObj = new GameObject("ContentContainer", typeof(RectTransform));
            contentObj.transform.SetParent(root.transform, false);
            var contentRt = contentObj.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0.04f, 0.04f);
            contentRt.anchorMax = new Vector2(0.96f, 0.96f);
            contentRt.offsetMin = Vector2.zero;
            contentRt.offsetMax = Vector2.zero;

            // Tutorial Artwork Image (with AspectRatioFitter)
            GameObject artObj = new GameObject("TutorialImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(AspectRatioFitter));
            artObj.transform.SetParent(contentObj.transform, false);
            var artRt = artObj.GetComponent<RectTransform>();
            artRt.anchorMin = Vector2.zero;
            artRt.anchorMax = Vector2.one;
            artRt.offsetMin = Vector2.zero;
            artRt.offsetMax = Vector2.zero;

            var artImg = artObj.GetComponent<Image>();
            artImg.sprite = sprite;
            artImg.preserveAspect = true;
            artImg.raycastTarget = false;

            var fitter = artObj.GetComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 1201f / 1309f;

            // Invisible OK Button hit area mapped precisely over the flattened OK artwork:
            // Normalized: anchorMin = (0.31, 0.031), anchorMax = (0.69, 0.115)
            GameObject okObj = new GameObject("OkButtonHitArea", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            okObj.transform.SetParent(artObj.transform, false);
            var okRt = okObj.GetComponent<RectTransform>();
            okRt.anchorMin = new Vector2(0.31f, 0.031f);
            okRt.anchorMax = new Vector2(0.69f, 0.115f);
            okRt.offsetMin = Vector2.zero;
            okRt.offsetMax = Vector2.zero;

            var okImg = okObj.GetComponent<Image>();
            okImg.color = new Color(0f, 0f, 0f, 0f);
            okImg.raycastTarget = true;

            var okBtn = okObj.GetComponent<Button>();
            var colors = okBtn.colors;
            colors.fadeDuration = 0.05f;
            okBtn.colors = colors;

            // Wire FeatureOverlayView
            var view = root.GetComponent<FeatureOverlayView>();
            var so = new SerializedObject(view);
            so.FindProperty("backdropDim").objectReferenceValue = dimImg;
            so.FindProperty("contentRoot").objectReferenceValue = contentRt;
            so.FindProperty("tutorialImage").objectReferenceValue = artImg;
            so.FindProperty("okButton").objectReferenceValue = okBtn;
            so.FindProperty("featureId").stringValue = feat.id;
            so.ApplyModifiedProperties();

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            GameObject.DestroyImmediate(root);
            Debug.Log($"[TutorialAndLegalSetup] Saved tutorial prefab: {prefabPath}");
        }
    }

    public static void UpdateProgressionConfig()
    {
        var config = AssetDatabase.LoadAssetAtPath<FeatureProgressionConfig>(ProgressionConfigPath);
        if (config == null)
        {
            Debug.LogError($"[TutorialAndLegalSetup] FeatureProgressionConfig not found at {ProgressionConfigPath}!");
            return;
        }

        var linkedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Path.Combine(TutorialPrefabsFolder, "FeatureOverlay_LinkedAnts.prefab"));
        var stackedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Path.Combine(TutorialPrefabsFolder, "FeatureOverlay_StackedAnts.prefab"));
        var pipesPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Path.Combine(TutorialPrefabsFolder, "FeatureOverlay_Pipes.prefab"));
        var lockedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Path.Combine(TutorialPrefabsFolder, "FeatureOverlay_LockedAnts.prefab"));

        var linkedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(TutorialSpritesRoot, "Feature_Linked_Ants.png"));
        var stackedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(TutorialSpritesRoot, "Feature_Stacked_Ants.png"));
        var pipesSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(TutorialSpritesRoot, "Feature_Pipe.png"));
        var lockedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(TutorialSpritesRoot, "Feature_Locked_Ants.png"));

        var so = new SerializedObject(config);
        var featuresProp = so.FindProperty("features");

        for (int i = 0; i < featuresProp.arraySize; i++)
        {
            var elem = featuresProp.GetArrayElementAtIndex(i);
            string id = elem.FindPropertyRelative("featureId").stringValue;

            if (string.Equals(id, "linked_ants", StringComparison.OrdinalIgnoreCase))
            {
                if (linkedSprite != null) elem.FindPropertyRelative("tutorialArtwork").objectReferenceValue = linkedSprite;
                if (linkedPrefab != null) elem.FindPropertyRelative("tutorialPrefab").objectReferenceValue = linkedPrefab;
            }
            else if (string.Equals(id, "stacked_ants", StringComparison.OrdinalIgnoreCase))
            {
                if (stackedSprite != null) elem.FindPropertyRelative("tutorialArtwork").objectReferenceValue = stackedSprite;
                if (stackedPrefab != null) elem.FindPropertyRelative("tutorialPrefab").objectReferenceValue = stackedPrefab;
            }
            else if (string.Equals(id, "pipes", StringComparison.OrdinalIgnoreCase))
            {
                if (pipesSprite != null) elem.FindPropertyRelative("tutorialArtwork").objectReferenceValue = pipesSprite;
                if (pipesPrefab != null) elem.FindPropertyRelative("tutorialPrefab").objectReferenceValue = pipesPrefab;
            }
            else if (string.Equals(id, "locked_ants", StringComparison.OrdinalIgnoreCase))
            {
                if (lockedSprite != null) elem.FindPropertyRelative("tutorialArtwork").objectReferenceValue = lockedSprite;
                if (lockedPrefab != null) elem.FindPropertyRelative("tutorialPrefab").objectReferenceValue = lockedPrefab;
            }
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        Debug.Log("[TutorialAndLegalSetup] Updated FeatureProgressionConfig tutorial assets.");
    }

    public static GameObject CreateLegalDocumentModalPrefab()
    {
        EnsureFolderExists(LegalPrefabsFolder);
        string prefabPath = Path.Combine(LegalPrefabsFolder, "LegalDocumentModal.prefab");

        Font font = GetBestFont();
        var outerFrameSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(LegalSpritesRoot, "Legal_OuterFrame.png"));
        var innerPanelSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(LegalSpritesRoot, "Legal_InnerPanel.png"));
        var closeSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(LegalSpritesRoot, "Legal_CloseButton.png"));
        var blueBtnSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(LegalSpritesRoot, "Legal_BlueButton.png"));

        GameObject root = new GameObject("LegalDocumentModal", typeof(RectTransform), typeof(LegalDocumentModal));
        var rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        // Backdrop Dim
        GameObject dimObj = new GameObject("BackdropDim", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        dimObj.transform.SetParent(root.transform, false);
        var dimRt = dimObj.GetComponent<RectTransform>();
        dimRt.anchorMin = Vector2.zero;
        dimRt.anchorMax = Vector2.one;
        dimRt.offsetMin = Vector2.zero;
        dimRt.offsetMax = Vector2.zero;
        var dimImg = dimObj.GetComponent<Image>();
        dimImg.color = new Color(0f, 0f, 0f, 0.70f);
        dimImg.raycastTarget = true;

        // Card Root
        GameObject cardObj = new GameObject("CardRoot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        cardObj.transform.SetParent(root.transform, false);
        var cardRt = cardObj.GetComponent<RectTransform>();
        cardRt.sizeDelta = new Vector2(540, 700);
        var cardImg = cardObj.GetComponent<Image>();
        cardImg.sprite = outerFrameSprite;
        cardImg.type = Image.Type.Sliced;
        cardImg.color = new Color(0.34f, 0.40f, 0.80f); // #5767CC

        // Header Title
        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Outline));
        titleObj.transform.SetParent(cardObj.transform, false);
        var titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchoredPosition = new Vector2(0, 305);
        titleRt.sizeDelta = new Vector2(440, 50);
        var titleText = titleObj.GetComponent<Text>();
        titleText.font = font;
        titleText.fontSize = 28;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = new Color(0.99f, 0.96f, 0.93f);
        titleText.text = "DOCUMENT";
        var titleOutline = titleObj.GetComponent<Outline>();
        titleOutline.effectColor = new Color(0.08f, 0.10f, 0.22f);
        titleOutline.effectDistance = new Vector2(2, -2);

        // Close Button
        GameObject closeObj = new GameObject("CloseButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        closeObj.transform.SetParent(cardObj.transform, false);
        var closeRt = closeObj.GetComponent<RectTransform>();
        closeRt.anchoredPosition = new Vector2(240, 318);
        closeRt.sizeDelta = new Vector2(52, 52);
        var closeImg = closeObj.GetComponent<Image>();
        closeImg.sprite = closeSprite;
        closeImg.raycastTarget = true;
        var closeBtn = closeObj.GetComponent<Button>();

        // Inner Scroll View
        GameObject scrollObj = new GameObject("DocumentScrollView", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect));
        scrollObj.transform.SetParent(cardObj.transform, false);
        var scrollRt = scrollObj.GetComponent<RectTransform>();
        scrollRt.anchoredPosition = new Vector2(0, 10);
        scrollRt.sizeDelta = new Vector2(490, 520);
        var scrollImg = scrollObj.GetComponent<Image>();
        scrollImg.sprite = innerPanelSprite;
        scrollImg.type = Image.Type.Sliced;
        scrollImg.color = new Color(0.92f, 0.88f, 0.83f); // Cream inset
        var scrollRect = scrollObj.GetComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        // Viewport
        GameObject viewportObj = new GameObject("Viewport", typeof(RectTransform), typeof(Mask), typeof(Image));
        viewportObj.transform.SetParent(scrollObj.transform, false);
        var viewportRt = viewportObj.GetComponent<RectTransform>();
        viewportRt.anchorMin = Vector2.zero;
        viewportRt.anchorMax = Vector2.one;
        viewportRt.offsetMin = new Vector2(16, 16);
        viewportRt.offsetMax = new Vector2(-16, -16);
        var viewMask = viewportObj.GetComponent<Mask>();
        viewMask.showMaskGraphic = false;
        var viewImg = viewportObj.GetComponent<Image>();
        viewImg.color = Color.white;

        // Content
        GameObject contentObj = new GameObject("Content", typeof(RectTransform), typeof(ContentSizeFitter), typeof(VerticalLayoutGroup));
        contentObj.transform.SetParent(viewportObj.transform, false);
        var contentRt = contentObj.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0, 1);
        contentRt.anchorMax = new Vector2(1, 1);
        contentRt.pivot = new Vector2(0.5f, 1);
        contentRt.offsetMin = Vector2.zero;
        contentRt.offsetMax = Vector2.zero;

        var fitter = contentObj.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var vlg = contentObj.GetComponent<VerticalLayoutGroup>();
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;

        // Body Text
        GameObject bodyObj = new GameObject("DocumentBodyText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        bodyObj.transform.SetParent(contentObj.transform, false);
        var bodyText = bodyObj.GetComponent<Text>();
        bodyText.font = font;
        bodyText.fontSize = 17;
        bodyText.color = new Color(0.18f, 0.16f, 0.25f);
        bodyText.alignment = TextAnchor.UpperLeft;
        bodyText.lineSpacing = 1.25f;

        scrollRect.viewport = viewportRt;
        scrollRect.content = contentRt;

        // Open in Browser Button
        GameObject openUrlObj = new GameObject("OpenUrlButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        openUrlObj.transform.SetParent(cardObj.transform, false);
        var openUrlRt = openUrlObj.GetComponent<RectTransform>();
        openUrlRt.anchoredPosition = new Vector2(0, -300);
        openUrlRt.sizeDelta = new Vector2(280, 52);
        var openUrlImg = openUrlObj.GetComponent<Image>();
        openUrlImg.sprite = blueBtnSprite;
        openUrlImg.type = Image.Type.Sliced;
        var openUrlBtn = openUrlObj.GetComponent<Button>();

        GameObject openUrlTextObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        openUrlTextObj.transform.SetParent(openUrlObj.transform, false);
        var openUrlTextRt = openUrlTextObj.GetComponent<RectTransform>();
        openUrlTextRt.anchorMin = Vector2.zero;
        openUrlTextRt.anchorMax = Vector2.one;
        openUrlTextRt.offsetMin = Vector2.zero;
        openUrlTextRt.offsetMax = Vector2.zero;
        var openUrlText = openUrlTextObj.GetComponent<Text>();
        openUrlText.font = font;
        openUrlText.fontSize = 20;
        openUrlText.fontStyle = FontStyle.Bold;
        openUrlText.alignment = TextAnchor.MiddleCenter;
        openUrlText.color = Color.white;
        openUrlText.text = "Open in Browser";

        // Wire component
        var modalComp = root.GetComponent<LegalDocumentModal>();
        var so = new SerializedObject(modalComp);
        so.FindProperty("backdropDim").objectReferenceValue = dimImg;
        so.FindProperty("cardRoot").objectReferenceValue = cardRt;
        so.FindProperty("titleText").objectReferenceValue = titleText;
        so.FindProperty("documentBodyText").objectReferenceValue = bodyText;
        so.FindProperty("scrollRect").objectReferenceValue = scrollRect;
        so.FindProperty("closeButton").objectReferenceValue = closeBtn;
        so.FindProperty("openUrlButton").objectReferenceValue = openUrlBtn;
        so.FindProperty("openUrlButtonText").objectReferenceValue = openUrlText;
        so.ApplyModifiedProperties();

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        GameObject.DestroyImmediate(root);
        Debug.Log($"[TutorialAndLegalSetup] Saved LegalDocumentModal prefab: {prefabPath}");
        return prefab;
    }

    public static GameObject CreateLegalTermsModalPrefab(GameObject docModalPrefab)
    {
        EnsureFolderExists(LegalPrefabsFolder);
        string prefabPath = Path.Combine(LegalPrefabsFolder, "LegalTermsModal.prefab");

        Font font = GetBestFont();
        var outerFrameSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(LegalSpritesRoot, "Legal_OuterFrame.png"));
        var innerPanelSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(LegalSpritesRoot, "Legal_InnerPanel.png"));
        var closeSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(LegalSpritesRoot, "Legal_CloseButton.png"));
        var blueBtnSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(LegalSpritesRoot, "Legal_BlueButton.png"));
        var copyIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(LegalSpritesRoot, "Legal_CopyIcon.png"));

        GameObject root = new GameObject("LegalTermsModal", typeof(RectTransform), typeof(LegalTermsModal));
        var rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        // Backdrop Dim
        GameObject dimObj = new GameObject("BackdropDim", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        dimObj.transform.SetParent(root.transform, false);
        var dimRt = dimObj.GetComponent<RectTransform>();
        dimRt.anchorMin = Vector2.zero;
        dimRt.anchorMax = Vector2.one;
        dimRt.offsetMin = Vector2.zero;
        dimRt.offsetMax = Vector2.zero;
        var dimImg = dimObj.GetComponent<Image>();
        dimImg.color = new Color(0f, 0f, 0f, 0.70f);
        dimImg.raycastTarget = true;

        // Card Root (matches Privacy_policies.png frame)
        GameObject cardObj = new GameObject("CardRoot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        cardObj.transform.SetParent(root.transform, false);
        var cardRt = cardObj.GetComponent<RectTransform>();
        cardRt.sizeDelta = new Vector2(540, 680);
        var cardImg = cardObj.GetComponent<Image>();
        cardImg.sprite = outerFrameSprite;
        cardImg.type = Image.Type.Sliced;
        cardImg.color = new Color(0.34f, 0.40f, 0.80f); // Periwinkle #5767CC

        // Title
        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Outline));
        titleObj.transform.SetParent(cardObj.transform, false);
        var titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchoredPosition = new Vector2(0, 290);
        titleRt.sizeDelta = new Vector2(440, 56);
        var titleText = titleObj.GetComponent<Text>();
        titleText.font = font;
        titleText.fontSize = 32;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = new Color(0.99f, 0.96f, 0.93f);
        titleText.text = "LEGAL TERMS";
        var titleOutline = titleObj.GetComponent<Outline>();
        titleOutline.effectColor = new Color(0.08f, 0.10f, 0.22f);
        titleOutline.effectDistance = new Vector2(2, -2);

        // Close Button
        GameObject closeObj = new GameObject("CloseButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        closeObj.transform.SetParent(cardObj.transform, false);
        var closeRt = closeObj.GetComponent<RectTransform>();
        closeRt.anchoredPosition = new Vector2(240, 305);
        closeRt.sizeDelta = new Vector2(52, 52);
        var closeImg = closeObj.GetComponent<Image>();
        closeImg.sprite = closeSprite;
        closeImg.raycastTarget = true;
        var closeBtn = closeObj.GetComponent<Button>();

        // Inner Cream Inset Panel
        GameObject innerObj = new GameObject("InnerPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        innerObj.transform.SetParent(cardObj.transform, false);
        var innerRt = innerObj.GetComponent<RectTransform>();
        innerRt.anchoredPosition = new Vector2(0, -20);
        innerRt.sizeDelta = new Vector2(490, 530);
        var innerImg = innerObj.GetComponent<Image>();
        innerImg.sprite = innerPanelSprite;
        innerImg.type = Image.Type.Sliced;
        innerImg.color = new Color(0.92f, 0.88f, 0.83f); // Cream inset #E0D2C4

        // Buttons Container
        GameObject btnGroup = new GameObject("ButtonsGroup", typeof(RectTransform), typeof(VerticalLayoutGroup));
        btnGroup.transform.SetParent(innerObj.transform, false);
        var btnGroupRt = btnGroup.GetComponent<RectTransform>();
        btnGroupRt.anchoredPosition = new Vector2(0, 35);
        btnGroupRt.sizeDelta = new Vector2(450, 390);

        var vlg = btnGroup.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 18;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = false;
        vlg.childControlHeight = false;

        // 1. Privacy Policy Button
        var privacyBtn = CreatePillButton(btnGroup.transform, "PrivacyPolicyButton", "Privacy Policy", 22, blueBtnSprite, font, new Vector2(420, 68));
        // 2. Terms of Service Button
        var termsBtn = CreatePillButton(btnGroup.transform, "TermsOfServiceButton", "Terms of Service", 22, blueBtnSprite, font, new Vector2(420, 68));
        // 3. Account Deletion Button
        var deletionBtn = CreatePillButton(btnGroup.transform, "AccountDeletionButton", "Account Deletion and Data Request", 18, blueBtnSprite, font, new Vector2(420, 68));
        // 4. Personalized Ads Button (styled disabled/configurable)
        var adsBtn = CreatePillButton(btnGroup.transform, "PersonalizedAdsButton", "Personalized Ads (Unavailable)", 19, blueBtnSprite, font, new Vector2(420, 68));
        var adsImg = adsBtn.GetComponent<Image>();
        var adsTxt = adsBtn.GetComponentInChildren<Text>();

        // Player ID row near bottom
        GameObject idRow = new GameObject("PlayerIdRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        idRow.transform.SetParent(innerObj.transform, false);
        var idRowRt = idRow.GetComponent<RectTransform>();
        idRowRt.anchoredPosition = new Vector2(0, -210);
        idRowRt.sizeDelta = new Vector2(430, 48);

        var hlg = idRow.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;

        GameObject idLblObj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        idLblObj.transform.SetParent(idRow.transform, false);
        var idLbl = idLblObj.GetComponent<Text>();
        idLbl.font = font;
        idLbl.fontSize = 17;
        idLbl.color = new Color(0.35f, 0.32f, 0.45f);
        idLbl.text = "PLAYER ID: ";

        GameObject idValObj = new GameObject("Value", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        idValObj.transform.SetParent(idRow.transform, false);
        var idValRt = idValObj.GetComponent<RectTransform>();
        idValRt.sizeDelta = new Vector2(230, 40);
        var idVal = idValObj.GetComponent<Text>();
        idVal.font = font;
        idVal.fontSize = 17;
        idVal.fontStyle = FontStyle.Bold;
        idVal.color = new Color(0.18f, 0.16f, 0.28f);
        idVal.alignment = TextAnchor.MiddleLeft;
        idVal.text = "NOT CONFIGURED";

        GameObject copyBtnObj = new GameObject("CopyButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        copyBtnObj.transform.SetParent(idRow.transform, false);
        var copyBtnRt = copyBtnObj.GetComponent<RectTransform>();
        copyBtnRt.sizeDelta = new Vector2(42, 42);
        var copyBtnImg = copyBtnObj.GetComponent<Image>();
        copyBtnImg.sprite = copyIconSprite;
        var copyBtn = copyBtnObj.GetComponent<Button>();

        // Toast Container
        GameObject toastObj = new GameObject("ToastContainer", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        toastObj.transform.SetParent(cardObj.transform, false);
        var toastRt = toastObj.GetComponent<RectTransform>();
        toastRt.anchoredPosition = new Vector2(0, -280);
        toastRt.sizeDelta = new Vector2(400, 44);
        var toastImg = toastObj.GetComponent<Image>();
        toastImg.sprite = blueBtnSprite;
        toastImg.type = Image.Type.Sliced;
        toastImg.color = new Color(0.15f, 0.18f, 0.28f, 0.95f);

        GameObject toastTxtObj = new GameObject("ToastText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        toastTxtObj.transform.SetParent(toastObj.transform, false);
        var toastTxtRt = toastTxtObj.GetComponent<RectTransform>();
        toastTxtRt.anchorMin = Vector2.zero;
        toastTxtRt.anchorMax = Vector2.one;
        toastTxtRt.offsetMin = Vector2.zero;
        toastTxtRt.offsetMax = Vector2.zero;
        var toastTxt = toastTxtObj.GetComponent<Text>();
        toastTxt.font = font;
        toastTxt.fontSize = 16;
        toastTxt.alignment = TextAnchor.MiddleCenter;
        toastTxt.color = Color.white;
        toastTxt.text = "Notification";
        toastObj.SetActive(false);

        // Wire LegalTermsModal component
        var termsComp = root.GetComponent<LegalTermsModal>();
        var so = new SerializedObject(termsComp);
        so.FindProperty("backdropDim").objectReferenceValue = dimImg;
        so.FindProperty("cardRoot").objectReferenceValue = cardRt;
        so.FindProperty("closeButton").objectReferenceValue = closeBtn;
        so.FindProperty("privacyPolicyButton").objectReferenceValue = privacyBtn;
        so.FindProperty("termsOfServiceButton").objectReferenceValue = termsBtn;
        so.FindProperty("accountDeletionButton").objectReferenceValue = deletionBtn;
        so.FindProperty("personalizedAdsButton").objectReferenceValue = adsBtn;
        so.FindProperty("personalizedAdsButtonImage").objectReferenceValue = adsImg;
        so.FindProperty("personalizedAdsButtonText").objectReferenceValue = adsTxt;
        so.FindProperty("playerIdRow").objectReferenceValue = idRow;
        so.FindProperty("playerIdLabel").objectReferenceValue = idLbl;
        so.FindProperty("playerIdValueText").objectReferenceValue = idVal;
        so.FindProperty("copyPlayerIdButton").objectReferenceValue = copyBtn;
        so.FindProperty("toastContainer").objectReferenceValue = toastObj;
        so.FindProperty("toastText").objectReferenceValue = toastTxt;
        if (docModalPrefab != null)
        {
            so.FindProperty("documentModal").objectReferenceValue = docModalPrefab.GetComponent<LegalDocumentModal>();
        }
        so.ApplyModifiedProperties();

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        GameObject.DestroyImmediate(root);
        Debug.Log($"[TutorialAndLegalSetup] Saved LegalTermsModal prefab: {prefabPath}");
        return prefab;
    }

    public static GameObject CreateWelcomeOverlayPrefab(GameObject docModalPrefab)
    {
        EnsureFolderExists(LegalPrefabsFolder);
        string prefabPath = Path.Combine(LegalPrefabsFolder, "WelcomeOverlay.prefab");

        Font font = GetBestFont();
        var cardTopSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(LegalSpritesRoot, "Welcome_CardTop.png"));
        var cardBottomSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(LegalSpritesRoot, "Welcome_CardBottom.png"));
        var badgeSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(LegalSpritesRoot, "Welcome_Badge.png"));
        var acceptBtnSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(LegalSpritesRoot, "Welcome_AcceptButton.png"));

        GameObject root = new GameObject("WelcomeOverlay", typeof(RectTransform), typeof(WelcomeOverlay));
        var rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        // Dim Backdrop (Deep purple)
        GameObject dimObj = new GameObject("BackdropDim", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        dimObj.transform.SetParent(root.transform, false);
        var dimRt = dimObj.GetComponent<RectTransform>();
        dimRt.anchorMin = Vector2.zero;
        dimRt.anchorMax = Vector2.one;
        dimRt.offsetMin = Vector2.zero;
        dimRt.offsetMax = Vector2.zero;
        var dimImg = dimObj.GetComponent<Image>();
        dimImg.color = new Color(0.08f, 0.05f, 0.15f, 0.90f); // Deep purple backdrop
        dimImg.raycastTarget = true;

        // Card Root (matches Welcome.png card)
        GameObject cardObj = new GameObject("CardRoot", typeof(RectTransform));
        cardObj.transform.SetParent(root.transform, false);
        var cardRt = cardObj.GetComponent<RectTransform>();
        cardRt.sizeDelta = new Vector2(520, 620);

        // Cream Card Body
        GameObject topCard = new GameObject("CardTop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        topCard.transform.SetParent(cardObj.transform, false);
        var topRt = topCard.GetComponent<RectTransform>();
        topRt.anchorMin = new Vector2(0, 0.16f);
        topRt.anchorMax = Vector2.one;
        topRt.offsetMin = Vector2.zero;
        topRt.offsetMax = Vector2.zero;
        var topImg = topCard.GetComponent<Image>();
        topImg.sprite = cardTopSprite;
        topImg.type = Image.Type.Sliced;
        topImg.color = new Color(0.96f, 0.92f, 0.86f); // Cream card

        // Blue Card Footer
        GameObject botCard = new GameObject("CardBottom", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        botCard.transform.SetParent(cardObj.transform, false);
        var botRt = botCard.GetComponent<RectTransform>();
        botRt.anchorMin = Vector2.zero;
        botRt.anchorMax = new Vector2(1, 0.18f);
        botRt.offsetMin = Vector2.zero;
        botRt.offsetMax = Vector2.zero;
        var botImg = botCard.GetComponent<Image>();
        botImg.sprite = cardBottomSprite;
        botImg.type = Image.Type.Sliced;
        botImg.color = new Color(0.24f, 0.32f, 0.58f); // Blue footer

        // Overlapping Blue Badge
        GameObject badgeObj = new GameObject("WelcomeBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        badgeObj.transform.SetParent(cardObj.transform, false);
        var badgeRt = badgeObj.GetComponent<RectTransform>();
        badgeRt.anchoredPosition = new Vector2(0, 300);
        badgeRt.sizeDelta = new Vector2(360, 76);
        var badgeImg = badgeObj.GetComponent<Image>();
        badgeImg.sprite = badgeSprite;
        badgeImg.type = Image.Type.Sliced;
        badgeImg.color = new Color(0.28f, 0.36f, 0.65f);

        // WELCOME Title
        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Outline));
        titleObj.transform.SetParent(badgeObj.transform, false);
        var titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = Vector2.zero;
        titleRt.anchorMax = Vector2.one;
        titleRt.offsetMin = Vector2.zero;
        titleRt.offsetMax = Vector2.zero;
        var titleText = titleObj.GetComponent<Text>();
        titleText.font = font;
        titleText.fontSize = 36;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = new Color(0.99f, 0.96f, 0.92f);
        titleText.text = "WELCOME";
        var titleOutline = titleObj.GetComponent<Outline>();
        titleOutline.effectColor = new Color(0.08f, 0.12f, 0.25f);
        titleOutline.effectDistance = new Vector2(2, -2);

        // Purple Body Text
        GameObject bodyObj = new GameObject("BodyText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        bodyObj.transform.SetParent(cardObj.transform, false);
        var bodyRt = bodyObj.GetComponent<RectTransform>();
        bodyRt.anchoredPosition = new Vector2(0, 75);
        bodyRt.sizeDelta = new Vector2(440, 240);
        var bodyText = bodyObj.GetComponent<Text>();
        bodyText.font = font;
        bodyText.fontSize = 19;
        bodyText.lineSpacing = 1.30f;
        bodyText.alignment = TextAnchor.MiddleCenter;
        bodyText.color = new Color(0.25f, 0.18f, 0.36f); // Purple text
        bodyText.text = "Kindly take some time to review our Terms of Service and Privacy Policy. By clicking the \"ACCEPT\" button below, you agree to our Terms of Service and acknowledge that the Privacy Policy applies.";

        // Links Container
        GameObject linksObj = new GameObject("LinksContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
        linksObj.transform.SetParent(cardObj.transform, false);
        var linksRt = linksObj.GetComponent<RectTransform>();
        linksRt.anchoredPosition = new Vector2(0, -95);
        linksRt.sizeDelta = new Vector2(440, 76);
        var vlg = linksObj.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 6;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = false;
        vlg.childControlHeight = false;

        // Terms of Service Link Button
        GameObject termsLinkObj = new GameObject("TermsLinkButton", typeof(RectTransform), typeof(Button));
        termsLinkObj.transform.SetParent(linksObj.transform, false);
        var termsLinkRt = termsLinkObj.GetComponent<RectTransform>();
        termsLinkRt.sizeDelta = new Vector2(300, 32);
        var termsLinkBtn = termsLinkObj.GetComponent<Button>();

        GameObject termsLinkTextObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        termsLinkTextObj.transform.SetParent(termsLinkObj.transform, false);
        var termsLinkTextRt = termsLinkTextObj.GetComponent<RectTransform>();
        termsLinkTextRt.anchorMin = Vector2.zero;
        termsLinkTextRt.anchorMax = Vector2.one;
        termsLinkTextRt.offsetMin = Vector2.zero;
        termsLinkTextRt.offsetMax = Vector2.zero;
        var termsLinkText = termsLinkTextObj.GetComponent<Text>();
        termsLinkText.font = font;
        termsLinkText.fontSize = 18;
        termsLinkText.fontStyle = FontStyle.Bold;
        termsLinkText.alignment = TextAnchor.MiddleCenter;
        termsLinkText.color = new Color(0.20f, 0.40f, 0.85f);
        termsLinkText.text = "<u>Terms of Service</u>";

        // Privacy Policy Link Button
        GameObject privLinkObj = new GameObject("PrivacyLinkButton", typeof(RectTransform), typeof(Button));
        privLinkObj.transform.SetParent(linksObj.transform, false);
        var privLinkRt = privLinkObj.GetComponent<RectTransform>();
        privLinkRt.sizeDelta = new Vector2(300, 32);
        var privLinkBtn = privLinkObj.GetComponent<Button>();

        GameObject privLinkTextObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        privLinkTextObj.transform.SetParent(privLinkObj.transform, false);
        var privLinkTextRt = privLinkTextObj.GetComponent<RectTransform>();
        privLinkTextRt.anchorMin = Vector2.zero;
        privLinkTextRt.anchorMax = Vector2.one;
        privLinkTextRt.offsetMin = Vector2.zero;
        privLinkTextRt.offsetMax = Vector2.zero;
        var privLinkText = privLinkTextObj.GetComponent<Text>();
        privLinkText.font = font;
        privLinkText.fontSize = 18;
        privLinkText.fontStyle = FontStyle.Bold;
        privLinkText.alignment = TextAnchor.MiddleCenter;
        privLinkText.color = new Color(0.20f, 0.40f, 0.85f);
        privLinkText.text = "<u>Privacy Policy</u>";

        // ACCEPT Green Pill Button
        GameObject acceptObj = new GameObject("AcceptButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        acceptObj.transform.SetParent(cardObj.transform, false);
        var acceptRt = acceptObj.GetComponent<RectTransform>();
        acceptRt.anchoredPosition = new Vector2(0, -235);
        acceptRt.sizeDelta = new Vector2(280, 68);
        var acceptImg = acceptObj.GetComponent<Image>();
        acceptImg.sprite = acceptBtnSprite;
        acceptImg.type = Image.Type.Sliced;
        acceptImg.color = new Color(0.48f, 0.76f, 0.22f); // Fresh green #82BB41
        var acceptBtn = acceptObj.GetComponent<Button>();

        GameObject acceptTextObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Outline));
        acceptTextObj.transform.SetParent(acceptObj.transform, false);
        var acceptTextRt = acceptTextObj.GetComponent<RectTransform>();
        acceptTextRt.anchorMin = Vector2.zero;
        acceptTextRt.anchorMax = Vector2.one;
        acceptTextRt.offsetMin = Vector2.zero;
        acceptTextRt.offsetMax = Vector2.zero;
        var acceptText = acceptTextObj.GetComponent<Text>();
        acceptText.font = font;
        acceptText.fontSize = 24;
        acceptText.fontStyle = FontStyle.Bold;
        acceptText.alignment = TextAnchor.MiddleCenter;
        acceptText.color = Color.white;
        acceptText.text = "ACCEPT";
        var acceptOutline = acceptTextObj.GetComponent<Outline>();
        acceptOutline.effectColor = new Color(0.12f, 0.28f, 0.08f);
        acceptOutline.effectDistance = new Vector2(1, -1);

        // Wire WelcomeOverlay component
        var welcomeComp = root.GetComponent<WelcomeOverlay>();
        var so = new SerializedObject(welcomeComp);
        so.FindProperty("backdropDim").objectReferenceValue = dimImg;
        so.FindProperty("cardRoot").objectReferenceValue = cardRt;
        so.FindProperty("headingText").objectReferenceValue = titleText;
        so.FindProperty("bodyText").objectReferenceValue = bodyText;
        so.FindProperty("termsLinkButton").objectReferenceValue = termsLinkBtn;
        so.FindProperty("termsLinkText").objectReferenceValue = termsLinkText;
        so.FindProperty("privacyLinkButton").objectReferenceValue = privLinkBtn;
        so.FindProperty("privacyLinkText").objectReferenceValue = privLinkText;
        so.FindProperty("acceptButton").objectReferenceValue = acceptBtn;
        if (docModalPrefab != null)
        {
            so.FindProperty("documentModal").objectReferenceValue = docModalPrefab.GetComponent<LegalDocumentModal>();
        }
        so.ApplyModifiedProperties();

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        GameObject.DestroyImmediate(root);
        Debug.Log($"[TutorialAndLegalSetup] Saved WelcomeOverlay prefab: {prefabPath}");
        return prefab;
    }

    public static void SetupSceneOverlays(LegalConfig legalConfig, GameObject docPrefab, GameObject termsPrefab, GameObject welcomePrefab)
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.name != "main")
        {
            EditorSceneManager.OpenScene("Assets/Scenes/main.unity");
        }

        var uiMgr = UnityEngine.Object.FindFirstObjectByType<GameUIManager>();
        if (uiMgr == null)
        {
            Debug.LogError("[TutorialAndLegalSetup] GameUIManager not found in scene!");
            return;
        }

        var canvas = uiMgr.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[TutorialAndLegalSetup] Canvas not found in scene!");
            return;
        }

        // 1. Instantiate or get LegalDocumentModal in scene
        Transform existingDoc = canvas.transform.Find("LegalDocumentModal");
        GameObject docInstance = existingDoc != null ? existingDoc.gameObject : (GameObject)PrefabUtility.InstantiatePrefab(docPrefab, canvas.transform);
        docInstance.name = "LegalDocumentModal";
        docInstance.SetActive(false);

        // 2. Instantiate or get LegalTermsModal in scene
        Transform existingTerms = canvas.transform.Find("LegalTermsModal");
        GameObject termsInstance = existingTerms != null ? existingTerms.gameObject : (GameObject)PrefabUtility.InstantiatePrefab(termsPrefab, canvas.transform);
        termsInstance.name = "LegalTermsModal";
        termsInstance.SetActive(false);

        // 3. Instantiate or get WelcomeOverlay in scene
        Transform existingWelcome = canvas.transform.Find("WelcomeOverlay");
        GameObject welcomeInstance = existingWelcome != null ? existingWelcome.gameObject : (GameObject)PrefabUtility.InstantiatePrefab(welcomePrefab, canvas.transform);
        welcomeInstance.name = "WelcomeOverlay";
        welcomeInstance.SetActive(false);

        var docComp = docInstance.GetComponent<LegalDocumentModal>();
        var termsComp = termsInstance.GetComponent<LegalTermsModal>();
        var welcomeComp = welcomeInstance.GetComponent<WelcomeOverlay>();

        // Wire Document modal reference inside Terms and Welcome instances
        var termsSo = new SerializedObject(termsComp);
        termsSo.FindProperty("documentModal").objectReferenceValue = docComp;
        termsSo.FindProperty("legalConfig").objectReferenceValue = legalConfig;
        termsSo.ApplyModifiedProperties();

        var welcomeSo = new SerializedObject(welcomeComp);
        welcomeSo.FindProperty("documentModal").objectReferenceValue = docComp;
        welcomeSo.FindProperty("legalConfig").objectReferenceValue = legalConfig;
        welcomeSo.ApplyModifiedProperties();

        // 4. Update SettingsModal: Add/repurpose Legal Terms button
        Button legalTermsBtn = null;
        Transform settingsModalTrans = canvas.transform.Find("SettingsModal");
        if (settingsModalTrans != null)
        {
            Transform settingsCard = settingsModalTrans.Find("SettingsCard") ?? settingsModalTrans;
            Transform privBtnTrans = settingsCard.Find("PrivacyButton") ?? settingsCard.Find("PrivacyPolicyButton");
            if (privBtnTrans != null)
            {
                legalTermsBtn = privBtnTrans.GetComponent<Button>();
                var txt = privBtnTrans.GetComponentInChildren<Text>();
                if (txt != null) txt.text = "LEGAL TERMS";
            }
        }

        // 5. Connect all references to GameUIManager
        var uiSo = new SerializedObject(uiMgr);
        uiSo.FindProperty("legalConfig").objectReferenceValue = legalConfig;
        uiSo.FindProperty("legalTermsModal").objectReferenceValue = termsComp;
        uiSo.FindProperty("legalDocumentModal").objectReferenceValue = docComp;
        uiSo.FindProperty("welcomeOverlay").objectReferenceValue = welcomeComp;
        if (legalTermsBtn != null)
        {
            uiSo.FindProperty("legalTermsButton").objectReferenceValue = legalTermsBtn;
        }
        uiSo.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("[TutorialAndLegalSetup] Successfully wired Legal & Welcome overlays in scene!");
    }

    private static Button CreatePillButton(Transform parent, string name, string text, int fontSize, Sprite btnSprite, Font font, Vector2 size)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);
        var btnRt = btnObj.GetComponent<RectTransform>();
        btnRt.sizeDelta = size;

        var btnImg = btnObj.GetComponent<Image>();
        btnImg.sprite = btnSprite;
        btnImg.type = Image.Type.Sliced;
        btnImg.color = new Color(0.44f, 0.62f, 0.88f); // Blue gradient

        var btn = btnObj.GetComponent<Button>();

        GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        txtObj.transform.SetParent(btnObj.transform, false);
        var txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = new Vector2(12, 0);
        txtRt.offsetMax = new Vector2(-12, 0);

        var txt = txtObj.GetComponent<Text>();
        txt.font = font;
        txt.fontSize = fontSize;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.text = text;

        return btn;
    }

    private static Font GetBestFont()
    {
        Font font = AssetDatabase.LoadAssetAtPath<Font>("Assets/JMO Assets/Toony Colors Pro/Editor/Fonts/Roboto/Roboto-Bold.ttf");
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        return font;
    }

    private static void EnsureFolderExists(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = Path.GetDirectoryName(path).Replace("\\", "/");
            string folderName = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolderExists(parent);
            }
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }

    // ==========================================
    // EDITOR PREVIEW CONTROLS
    // (Inspect without altering player progression)
    // ==========================================

    [MenuItem("Tools/Slither Ants Jam/Tutorials/Preview Linked Ants Overlay")]
    public static void PreviewLinkedAnts()
    {
        ShowTutorialPreview("Assets/Prefabs/UI/Tutorials/FeatureOverlay_LinkedAnts.prefab", "linked_ants");
    }

    [MenuItem("Tools/Slither Ants Jam/Tutorials/Preview Stacked Ants Overlay")]
    public static void PreviewStackedAnts()
    {
        ShowTutorialPreview("Assets/Prefabs/UI/Tutorials/FeatureOverlay_StackedAnts.prefab", "stacked_ants");
    }

    [MenuItem("Tools/Slither Ants Jam/Tutorials/Preview Pipes Overlay")]
    public static void PreviewPipes()
    {
        ShowTutorialPreview("Assets/Prefabs/UI/Tutorials/FeatureOverlay_Pipes.prefab", "pipes");
    }

    [MenuItem("Tools/Slither Ants Jam/Tutorials/Preview Locked Ants Overlay")]
    public static void PreviewLockedAnts()
    {
        ShowTutorialPreview("Assets/Prefabs/UI/Tutorials/FeatureOverlay_LockedAnts.prefab", "locked_ants");
    }

    private static void ShowTutorialPreview(string prefabPath, string id)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"Prefab not found at {prefabPath}!");
            return;
        }

        var uiMgr = UnityEngine.Object.FindFirstObjectByType<GameUIManager>();
        var canvas = uiMgr != null ? uiMgr.GetComponentInParent<Canvas>() : UnityEngine.Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("No Canvas found in scene to preview tutorial overlay!");
            return;
        }

        DismissAllPreviews();

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas.transform);
        instance.name = "PREVIEW_" + id;
        instance.SetActive(true);
        instance.transform.SetAsLastSibling();

        var view = instance.GetComponent<FeatureOverlayView>();
        if (view != null && view.OkButton != null)
        {
            view.OkButton.onClick.AddListener(() =>
            {
                Debug.Log($"[Preview] OK tapped on {id} preview. Progression was NOT modified.");
                GameObject.DestroyImmediate(instance);
            });
        }

        Debug.Log($"[Preview] Showing preview for '{id}'. Progression untouched.");
    }

    [MenuItem("Tools/Slither Ants Jam/Legal/Preview Welcome Overlay")]
    public static void PreviewWelcome()
    {
        var uiMgr = UnityEngine.Object.FindFirstObjectByType<GameUIManager>();
        if (uiMgr != null && uiMgr.WelcomeOverlay != null)
        {
            DismissAllPreviews();
            uiMgr.WelcomeOverlay.Show(() =>
            {
                Debug.Log("[Preview] Welcome overlay accepted/dismissed in preview.");
            });
            return;
        }
        Debug.LogWarning("WelcomeOverlay reference not found in scene. Run full setup first!");
    }

    [MenuItem("Tools/Slither Ants Jam/Legal/Preview Legal Terms Modal")]
    public static void PreviewLegalTerms()
    {
        var uiMgr = UnityEngine.Object.FindFirstObjectByType<GameUIManager>();
        if (uiMgr != null && uiMgr.LegalTermsModal != null)
        {
            DismissAllPreviews();
            uiMgr.LegalTermsModal.Show(() =>
            {
                Debug.Log("[Preview] Legal Terms modal dismissed in preview.");
            });
            return;
        }
        Debug.LogWarning("LegalTermsModal reference not found in scene. Run full setup first!");
    }

    [MenuItem("Tools/Slither Ants Jam/Legal/Preview Legal Document Modal")]
    public static void PreviewLegalDocument()
    {
        var uiMgr = UnityEngine.Object.FindFirstObjectByType<GameUIManager>();
        if (uiMgr != null && uiMgr.LegalDocumentModal != null)
        {
            DismissAllPreviews();
            var cfg = LegalConfig.Instance;
            uiMgr.LegalDocumentModal.Show(
                cfg != null ? cfg.PrivacyPolicyTitle : "Privacy Policy",
                cfg != null ? cfg.PrivacyPolicyContent : "Sample policy body.",
                cfg != null ? cfg.PrivacyPolicyUrl : string.Empty,
                () => Debug.Log("[Preview] Document modal closed."));
            return;
        }
        Debug.LogWarning("LegalDocumentModal reference not found in scene. Run full setup first!");
    }

    [MenuItem("Tools/Slither Ants Jam/UI/Dismiss All Previews")]
    public static void DismissAllPreviews()
    {
        var canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        if (canvas != null)
        {
            for (int i = canvas.transform.childCount - 1; i >= 0; i--)
            {
                var child = canvas.transform.GetChild(i);
                if (child.name.StartsWith("PREVIEW_"))
                {
                    GameObject.DestroyImmediate(child.gameObject);
                }
            }
        }

        var uiMgr = UnityEngine.Object.FindFirstObjectByType<GameUIManager>();
        if (uiMgr != null)
        {
            if (uiMgr.WelcomeOverlay != null) uiMgr.WelcomeOverlay.gameObject.SetActive(false);
            if (uiMgr.LegalTermsModal != null) uiMgr.LegalTermsModal.gameObject.SetActive(false);
            if (uiMgr.LegalDocumentModal != null) uiMgr.LegalDocumentModal.gameObject.SetActive(false);
        }
        Debug.Log("[Preview] All preview overlays dismissed.");
    }
}
#endif
