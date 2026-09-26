using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class FailScreenSetup
{
    private const string TitlePath = "Assets/UI/FailScreen/LevelFailed_Title.png";
    private const string HeartPath = "Assets/UI/FailScreen/LevelFailed_Heart.png";
    private const string ButtonPath = "Assets/UI/FailScreen/LevelFailed_RestartButton.png";

    [MenuItem("Tools/Slither Ants Jam/Setup Fail Screen")]
    public static void Setup()
    {
        Debug.Log("[FailScreenSetup] Starting Level Failed UI setup...");

        // 1. Configure and import textures as Sprites
        ConfigureTextureAsSprite(TitlePath);
        ConfigureTextureAsSprite(HeartPath);
        ConfigureTextureAsSprite(ButtonPath);

        Sprite titleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TitlePath);
        Sprite heartSprite = AssetDatabase.LoadAssetAtPath<Sprite>(HeartPath);
        Sprite buttonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ButtonPath);

        if (titleSprite == null || heartSprite == null || buttonSprite == null)
        {
            Debug.LogError($"[FailScreenSetup] Failed to load sprites! title={titleSprite != null} heart={heartSprite != null} button={buttonSprite != null}");
            return;
        }

        // 2. Locate GameUICanvas in active scene
        var canvasObj = GameObject.Find("GameUICanvas");
        if (canvasObj == null)
        {
            Debug.LogError("[FailScreenSetup] GameUICanvas not found in active scene!");
            return;
        }

        var uiManager = canvasObj.GetComponent<GameUIManager>();
        if (uiManager == null)
        {
            Debug.LogError("[FailScreenSetup] GameUIManager component not found on GameUICanvas!");
            return;
        }

        // 3. Locate or create LosePanel under GameUICanvas
        Transform losePanelTrans = canvasObj.transform.Find("LosePanel");
        GameObject losePanelObj;
        if (losePanelTrans == null)
        {
            losePanelObj = new GameObject("LosePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            losePanelObj.transform.SetParent(canvasObj.transform, false);
            losePanelTrans = losePanelObj.transform;
        }
        else
        {
            losePanelObj = losePanelTrans.gameObject;
        }

        // Configure LosePanel full-screen RectTransform
        var losePanelRect = losePanelObj.GetComponent<RectTransform>();
        losePanelRect.anchorMin = Vector2.zero;
        losePanelRect.anchorMax = Vector2.one;
        losePanelRect.offsetMin = Vector2.zero;
        losePanelRect.offsetMax = Vector2.zero;
        losePanelRect.pivot = new Vector2(0.5f, 0.5f);
        losePanelRect.localScale = Vector3.one;
        losePanelRect.localPosition = Vector3.zero;

        // Dim background image on LosePanel
        var dimImg = losePanelObj.GetComponent<Image>();
        if (dimImg == null) dimImg = losePanelObj.AddComponent<Image>();
        dimImg.sprite = null;
        dimImg.color = new Color(0f, 0f, 0f, 0.82f);
        dimImg.raycastTarget = true;

        // Clean up legacy children if present
        for (int i = losePanelTrans.childCount - 1; i >= 0; i--)
        {
            var child = losePanelTrans.GetChild(i);
            if (child.name == "Title" || child.name == "ActionButton" || child.name == "FailPopupRoot")
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }

        // 4. Create FailPopupRoot
        GameObject popupRootObj = new GameObject("FailPopupRoot", typeof(RectTransform));
        popupRootObj.transform.SetParent(losePanelTrans, false);
        var popupRect = popupRootObj.GetComponent<RectTransform>();
        popupRect.anchorMin = new Vector2(0.5f, 0.5f);
        popupRect.anchorMax = new Vector2(0.5f, 0.5f);
        popupRect.pivot = new Vector2(0.5f, 0.5f);
        popupRect.anchoredPosition = Vector2.zero;
        popupRect.sizeDelta = new Vector2(1080f, 1920f);
        popupRect.localScale = Vector3.one;

        // 5. Title Image
        GameObject titleObj = new GameObject("FailedTitleImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        titleObj.transform.SetParent(popupRect, false);
        var titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = new Vector2(0f, 520f);
        titleRect.sizeDelta = new Vector2(780f, 408f);
        titleRect.localScale = Vector3.one;

        var titleImg = titleObj.GetComponent<Image>();
        titleImg.sprite = titleSprite;
        titleImg.preserveAspect = true;
        titleImg.raycastTarget = false;

        // 6. Broken Heart Image
        GameObject heartObj = new GameObject("BrokenHeartImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        heartObj.transform.SetParent(popupRect, false);
        var heartRect = heartObj.GetComponent<RectTransform>();
        heartRect.anchorMin = new Vector2(0.5f, 0.5f);
        heartRect.anchorMax = new Vector2(0.5f, 0.5f);
        heartRect.pivot = new Vector2(0.5f, 0.5f);
        heartRect.anchoredPosition = new Vector2(0f, -70f);
        heartRect.sizeDelta = new Vector2(680f, 452f);
        heartRect.localScale = Vector3.one;

        var heartImg = heartObj.GetComponent<Image>();
        heartImg.sprite = heartSprite;
        heartImg.preserveAspect = true;
        heartImg.raycastTarget = false;

        // 7. Restart Button
        GameObject buttonObj = new GameObject("RestartButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObj.transform.SetParent(popupRect, false);
        var buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = new Vector2(0f, -470f);
        buttonRect.sizeDelta = new Vector2(560f, 190f);
        buttonRect.localScale = Vector3.one;

        var buttonImg = buttonObj.GetComponent<Image>();
        buttonImg.sprite = buttonSprite;
        buttonImg.preserveAspect = true;
        buttonImg.raycastTarget = true;

        var buttonComp = buttonObj.GetComponent<Button>();
        buttonComp.targetGraphic = buttonImg;
        buttonComp.transition = Selectable.Transition.ColorTint;
        var colors = buttonComp.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        colors.selectedColor = Color.white;
        colors.fadeDuration = 0.08f;
        buttonComp.colors = colors;

        // 8. Serialize references into GameUIManager
        SerializedObject so = new SerializedObject(uiManager);
        so.Update();
        so.FindProperty("losePanel").objectReferenceValue = losePanelObj;
        so.FindProperty("retryButton").objectReferenceValue = buttonComp;
        so.FindProperty("failDimBackground").objectReferenceValue = dimImg;
        so.FindProperty("failPopupRoot").objectReferenceValue = popupRect;
        so.FindProperty("failTitleTransform").objectReferenceValue = titleRect;
        so.FindProperty("failHeartTransform").objectReferenceValue = heartRect;
        so.FindProperty("failRestartButtonTransform").objectReferenceValue = buttonRect;
        so.ApplyModifiedProperties();

        // Ensure LosePanel is inactive by default in the saved scene
        losePanelObj.SetActive(false);

        // Mark scene dirty and save
        EditorUtility.SetDirty(uiManager);
        EditorUtility.SetDirty(losePanelObj);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("[FailScreenSetup] Level Failed UI setup completed successfully and saved to scene!");
    }

    private static void ConfigureTextureAsSprite(string path)
    {
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
            if (importer.filterMode != FilterMode.Bilinear)
            {
                importer.filterMode = FilterMode.Bilinear;
                modified = true;
            }

            var defaultSettings = importer.GetDefaultPlatformTextureSettings();
            if (defaultSettings.format != TextureImporterFormat.RGBA32 && defaultSettings.format != TextureImporterFormat.Automatic)
            {
                defaultSettings.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SetPlatformTextureSettings(defaultSettings);
                modified = true;
            }

            if (modified)
            {
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
            }
        }
    }
}
