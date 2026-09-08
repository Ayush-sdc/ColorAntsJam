using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public static class SceneSetupUtility
{
    [MenuItem("SlitherAnts/Setup Main Scene Game Architecture")]
    public static void SetupScene()
    {
        // 1. Camera: Top-down studio perspective framing the board with top UI headroom
        Camera cam = Camera.main;
        if (cam == null)
        {
            var camObj = new GameObject("Main Camera");
            cam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
        }
        cam.transform.position = new Vector3(0f, 13.2f, -0.9f);
        cam.transform.rotation = Quaternion.Euler(88f, 0f, 0f);
        cam.backgroundColor = AntColorPalette.StudioBackground;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.fieldOfView = 78f;

        // 2. Soft, diffused studio lighting with gentle soft shadows
        var lightObj = GameObject.Find("Directional Light");
        if (lightObj != null)
        {
            var light = lightObj.GetComponent<Light>();
            if (light != null)
            {
                light.color = new Color(1f, 0.98f, 0.95f);
                light.intensity = 1.35f;
                light.shadows = LightShadows.Soft;
                light.shadowStrength = 0.35f;
                lightObj.transform.rotation = Quaternion.Euler(60f, -30f, 0f);
            }
        }

        // 3. GameController with core managers
        GameObject gameController = GameObject.Find("GameController");
        if (gameController == null)
        {
            gameController = new GameObject("GameController");
        }

        var levelMgr = gameController.GetComponent<LevelManager>() ?? gameController.AddComponent<LevelManager>();
        var boardMgr = gameController.GetComponent<BoardManager>() ?? gameController.AddComponent<BoardManager>();
        var gameMgr = gameController.GetComponent<GameManager>() ?? gameController.AddComponent<GameManager>();
        var inputHandler = gameController.GetComponent<PlayerInputHandler>() ?? gameController.AddComponent<PlayerInputHandler>();
        var soundMgr = gameController.GetComponent<SoundManager>() ?? gameController.AddComponent<SoundManager>();

        // Link Database
        LevelDatabase db = AssetDatabase.LoadAssetAtPath<LevelDatabase>("Assets/Data/Levels/LevelDatabase.asset");
        if (db != null)
        {
            var field = typeof(LevelManager).GetField("levelDatabase", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(levelMgr, db);
            }
        }

        // 4. Board Root
        GameObject boardObj = GameObject.Find("Board");
        if (boardObj == null)
        {
            boardObj = new GameObject("Board");
        }
        var boardParentField = typeof(BoardManager).GetField("boardParent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (boardParentField != null)
        {
            boardParentField.SetValue(boardMgr, boardObj.transform);
        }

        // 5. Ant Units Container
        GameObject antsObj = GameObject.Find("Ants");
        if (antsObj == null)
        {
            antsObj = new GameObject("Ants");
        }
        var antsParentField = typeof(BoardManager).GetField("antsParent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (antsParentField != null)
        {
            antsParentField.SetValue(boardMgr, antsObj.transform);
        }

        // 6. Food Box Container
        GameObject foodBoxesObj = GameObject.Find("FoodBoxes");
        if (foodBoxesObj == null)
        {
            foodBoxesObj = new GameObject("FoodBoxes");
        }
        var boxesParentField = typeof(BoardManager).GetField("boxesParent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (boxesParentField != null)
        {
            boxesParentField.SetValue(boardMgr, foodBoxesObj.transform);
        }

        // 7. Setup UI Canvas matching the reference visual 100%
        SetupUICanvas();

        // 8. Ensure EventSystem exists
        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        Debug.Log("<color=green><b>[SceneSetupUtility]</b> Scene Game Architecture & UI Successfully Configured!</color>");
    }

    private static void SetupUICanvas()
    {
        GameObject oldCanvas = GameObject.Find("GameUICanvas");
        if (oldCanvas != null)
        {
            GameObject.DestroyImmediate(oldCanvas);
        }

        GameObject canvasObj = new GameObject("GameUICanvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        var uiMgr = canvasObj.AddComponent<GameUIManager>();

        // Pre-generate procedural sprites for exact visual match
        Sprite roundedCardSprite = CreateRoundedRectSprite(640, 86, 32f, new Color(0.95f, 0.96f, 0.97f), new Color(0.98f, 0.62f, 0.12f), 4.5f);
        Sprite grayButtonSprite = CreateRoundedRectSprite(54, 54, 14f, new Color(0.81f, 0.83f, 0.86f), new Color(0.98f, 0.62f, 0.12f), 2.5f);
        Sprite timerPillSprite = CreateRoundedRectSprite(140, 54, 18f, new Color(0.72f, 0.74f, 0.78f), new Color(0.60f, 0.62f, 0.66f), 2f);
        Sprite heartSprite = CreateGlossyHeartSprite(128);
        Sprite stopwatchSprite = CreateStopwatchSprite(128);
        Sprite restartArrowSprite = CreateRestartArrowSprite(128);
        Sprite gearSprite = CreateGearIconSprite(128);
        Sprite modalCardSprite = CreateRoundedRectSprite(580, 520, 36f, new Color(0.96f, 0.97f, 0.98f), new Color(0.86f, 0.88f, 0.91f), 3.5f);
        Sprite pillToggleSprite = CreateRoundedRectSprite(100, 46, 23f, Color.white, Color.clear, 0f);
        Sprite greenBtnSprite = CreateRoundedRectSprite(480, 64, 20f, new Color(0.16f, 0.68f, 0.38f), Color.clear, 0f);
        Sprite orangeBtnSprite = CreateRoundedRectSprite(480, 64, 20f, new Color(0.98f, 0.62f, 0.12f), Color.clear, 0f);
        Sprite rowBgSprite = CreateRoundedRectSprite(480, 64, 18f, new Color(0.89f, 0.91f, 0.94f), Color.clear, 0f);

        // --- TOP-LEFT GEAR ICON BUTTON ---
        GameObject gearBtnObj = new GameObject("GearSettingsButton");
        gearBtnObj.transform.SetParent(canvasObj.transform, false);
        var gearRect = gearBtnObj.AddComponent<RectTransform>();
        gearRect.anchorMin = new Vector2(0f, 1f);
        gearRect.anchorMax = new Vector2(0f, 1f);
        gearRect.pivot = new Vector2(0f, 1f);
        gearRect.anchoredPosition = new Vector2(48f, -135f);
        gearRect.sizeDelta = new Vector2(68f, 68f);

        var gearImg = gearBtnObj.AddComponent<Image>();
        gearImg.sprite = gearSprite;
        var gearBtn = gearBtnObj.AddComponent<Button>();
        gearBtn.targetGraphic = gearImg;

        var gearShadow = gearBtnObj.AddComponent<Shadow>();
        gearShadow.effectColor = new Color(0f, 0f, 0f, 0.20f);
        gearShadow.effectDistance = new Vector2(0f, -3f);

        // --- TOP MAIN ROUNDED RECTANGLE HUD CARD (Anchored at -235px) ---
        GameObject topCard = new GameObject("TopCardHUD");
        topCard.transform.SetParent(canvasObj.transform, false);
        var cardRect = topCard.AddComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 1f);
        cardRect.anchorMax = new Vector2(0.5f, 1f);
        cardRect.pivot = new Vector2(0.5f, 1f);
        cardRect.anchoredPosition = new Vector2(0f, -235f);
        cardRect.sizeDelta = new Vector2(640f, 86f);
        cardRect.localScale = new Vector3(1.25f, 1.25f, 1.25f);

        var cardImg = topCard.AddComponent<Image>();
        cardImg.sprite = roundedCardSprite;
        cardImg.type = Image.Type.Simple;

        var shadow = topCard.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.16f);
        shadow.effectDistance = new Vector2(0f, -3f);

        // 1. Restart Button inside Card (Left: x = -260)
        GameObject restartBtnObj = new GameObject("RestartButton");
        restartBtnObj.transform.SetParent(topCard.transform, false);
        var rRect = restartBtnObj.AddComponent<RectTransform>();
        rRect.anchorMin = new Vector2(0.5f, 0.5f);
        rRect.anchorMax = new Vector2(0.5f, 0.5f);
        rRect.anchoredPosition = new Vector2(-260f, 0f);
        rRect.sizeDelta = new Vector2(54, 54);

        var rImg = restartBtnObj.AddComponent<Image>();
        rImg.sprite = grayButtonSprite;
        var restartBtn = restartBtnObj.AddComponent<Button>();
        restartBtn.targetGraphic = rImg;

        // Restart Icon inside button
        GameObject rIconObj = new GameObject("RestartIcon");
        rIconObj.transform.SetParent(restartBtnObj.transform, false);
        var rIconRect = rIconObj.AddComponent<RectTransform>();
        rIconRect.anchorMin = Vector2.zero;
        rIconRect.anchorMax = Vector2.one;
        rIconRect.sizeDelta = new Vector2(-12, -12);
        var rIconImg = rIconObj.AddComponent<Image>();
        rIconImg.sprite = restartArrowSprite;
        rIconImg.color = Color.white;

        // 2. Timer Widget Pill (Center-Left: x = -130)
        GameObject timerPill = new GameObject("TimerWidget");
        timerPill.transform.SetParent(topCard.transform, false);
        var timerRect = timerPill.AddComponent<RectTransform>();
        timerRect.anchorMin = new Vector2(0.5f, 0.5f);
        timerRect.anchorMax = new Vector2(0.5f, 0.5f);
        timerRect.anchoredPosition = new Vector2(-130f, 0f);
        timerRect.sizeDelta = new Vector2(140, 54);

        var timerPillImg = timerPill.AddComponent<Image>();
        timerPillImg.sprite = timerPillSprite;

        // Stopwatch Icon nested inside timer on the left
        GameObject stopwatchObj = new GameObject("StopwatchIcon");
        stopwatchObj.transform.SetParent(timerPill.transform, false);
        var swRect = stopwatchObj.AddComponent<RectTransform>();
        swRect.anchorMin = new Vector2(0f, 0.5f);
        swRect.anchorMax = new Vector2(0f, 0.5f);
        swRect.pivot = new Vector2(0f, 0.5f);
        swRect.anchoredPosition = new Vector2(6f, 0f);
        swRect.sizeDelta = new Vector2(42, 42);
        var swImg = stopwatchObj.AddComponent<Image>();
        swImg.sprite = stopwatchSprite;

        // Digital Timer Text (e.g. 2:00)
        GameObject timerTextObj = new GameObject("TimerText");
        timerTextObj.transform.SetParent(timerPill.transform, false);
        var tTextRect = timerTextObj.AddComponent<RectTransform>();
        tTextRect.anchorMin = Vector2.zero;
        tTextRect.anchorMax = Vector2.one;
        tTextRect.offsetMin = new Vector2(50f, 0f);
        tTextRect.offsetMax = new Vector2(-6f, 0f);

        var timerText = timerTextObj.AddComponent<Text>();
        timerText.text = "2:00";
        timerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        timerText.fontSize = 26;
        timerText.fontStyle = FontStyle.Bold;
        timerText.color = Color.white;
        timerText.alignment = TextAnchor.MiddleCenter;

        var tOutline = timerTextObj.AddComponent<Outline>();
        tOutline.effectColor = new Color(0.20f, 0.22f, 0.25f, 0.95f);
        tOutline.effectDistance = new Vector2(1.8f, -1.8f);

        // 3. Three 3D Glossy Candy Red Hearts in a STRICT HORIZONTAL LINE (Center-Right: x = 75)
        GameObject heartsContainer = new GameObject("HeartsContainer");
        heartsContainer.transform.SetParent(topCard.transform, false);
        var hLayoutRect = heartsContainer.AddComponent<RectTransform>();
        hLayoutRect.anchorMin = new Vector2(0.5f, 0.5f);
        hLayoutRect.anchorMax = new Vector2(0.5f, 0.5f);
        hLayoutRect.pivot = new Vector2(0.5f, 0.5f);
        hLayoutRect.anchoredPosition = new Vector2(75f, 0f);
        hLayoutRect.sizeDelta = new Vector2(150f, 50f);

        var hLayout = heartsContainer.AddComponent<HorizontalLayoutGroup>();
        hLayout.childAlignment = TextAnchor.MiddleCenter;
        hLayout.spacing = 10f;
        hLayout.childControlWidth = false;
        hLayout.childControlHeight = false;
        hLayout.childForceExpandWidth = false;
        hLayout.childForceExpandHeight = false;

        Image[] heartImages = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            GameObject heartObj = new GameObject($"Heart_{i + 1}");
            heartObj.transform.SetParent(heartsContainer.transform, false);
            var hRect = heartObj.AddComponent<RectTransform>();
            hRect.sizeDelta = new Vector2(42f, 42f);

            var hImg = heartObj.AddComponent<Image>();
            hImg.sprite = heartSprite;
            heartImages[i] = hImg;
        }

        // 4. Right Button / Badge (Level Number '1': x = 260)
        GameObject levelBox = new GameObject("LevelBadge");
        levelBox.transform.SetParent(topCard.transform, false);
        var lvlRect = levelBox.AddComponent<RectTransform>();
        lvlRect.anchorMin = new Vector2(0.5f, 0.5f);
        lvlRect.anchorMax = new Vector2(0.5f, 0.5f);
        lvlRect.anchoredPosition = new Vector2(260f, 0f);
        lvlRect.sizeDelta = new Vector2(54, 54);

        var lvlBg = levelBox.AddComponent<Image>();
        lvlBg.sprite = grayButtonSprite;

        GameObject lvlTextObj = new GameObject("LevelText");
        lvlTextObj.transform.SetParent(levelBox.transform, false);
        var lTextRect = lvlTextObj.AddComponent<RectTransform>();
        lTextRect.anchorMin = Vector2.zero;
        lTextRect.anchorMax = Vector2.one;
        lTextRect.sizeDelta = Vector2.zero;

        var lvlText = lvlTextObj.AddComponent<Text>();
        lvlText.text = "1";
        lvlText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        lvlText.fontSize = 30;
        lvlText.fontStyle = FontStyle.Bold;
        lvlText.color = Color.white;
        lvlText.alignment = TextAnchor.MiddleCenter;

        var lOutline = lvlTextObj.AddComponent<Outline>();
        lOutline.effectColor = new Color(0.20f, 0.22f, 0.25f, 0.95f);
        lOutline.effectDistance = new Vector2(2f, -2f);

        // --- SETTINGS MODAL POPUP DIALOG ---
        GameObject settingsModal = new GameObject("SettingsModal");
        settingsModal.transform.SetParent(canvasObj.transform, false);
        var modalRect = settingsModal.AddComponent<RectTransform>();
        modalRect.anchorMin = Vector2.zero;
        modalRect.anchorMax = Vector2.one;
        modalRect.offsetMin = Vector2.zero;
        modalRect.offsetMax = Vector2.zero;

        var modalBg = settingsModal.AddComponent<Image>();
        modalBg.color = new Color(0.06f, 0.08f, 0.12f, 0.65f);
        var backdropBtn = settingsModal.AddComponent<Button>();
        backdropBtn.targetGraphic = modalBg;

        // Modal Card Container (580 x 520)
        GameObject settingsCard = new GameObject("SettingsCard");
        settingsCard.transform.SetParent(settingsModal.transform, false);
        var cardModalRect = settingsCard.AddComponent<RectTransform>();
        cardModalRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardModalRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardModalRect.pivot = new Vector2(0.5f, 0.5f);
        cardModalRect.anchoredPosition = Vector2.zero;
        cardModalRect.sizeDelta = new Vector2(580f, 520f);

        var cardModalImg = settingsCard.AddComponent<Image>();
        cardModalImg.sprite = modalCardSprite;

        var cardModalShadow = settingsCard.AddComponent<Shadow>();
        cardModalShadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
        cardModalShadow.effectDistance = new Vector2(0f, -8f);

        // Header Title: "SETTINGS"
        GameObject titleObj = new GameObject("SettingsTitle");
        titleObj.transform.SetParent(settingsCard.transform, false);
        var titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -34f);
        titleRect.sizeDelta = new Vector2(400f, 50f);

        var titleText = titleObj.AddComponent<Text>();
        titleText.text = "SETTINGS";
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 34;
        titleText.fontStyle = FontStyle.Bold;
        titleText.color = new Color(0.18f, 0.20f, 0.24f);
        titleText.alignment = TextAnchor.MiddleCenter;

        // Close 'X' Button at top-right
        GameObject closeBtnObj = new GameObject("CloseButton");
        closeBtnObj.transform.SetParent(settingsCard.transform, false);
        var closeRect = closeBtnObj.AddComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-24f, -24f);
        closeRect.sizeDelta = new Vector2(46f, 46f);

        var closeBg = closeBtnObj.AddComponent<Image>();
        closeBg.sprite = grayButtonSprite;
        var closeBtn = closeBtnObj.AddComponent<Button>();
        closeBtn.targetGraphic = closeBg;

        GameObject closeTextObj = new GameObject("CloseText");
        closeTextObj.transform.SetParent(closeBtnObj.transform, false);
        var ctRect = closeTextObj.AddComponent<RectTransform>();
        ctRect.anchorMin = Vector2.zero;
        ctRect.anchorMax = Vector2.one;
        ctRect.sizeDelta = Vector2.zero;
        var closeText = closeTextObj.AddComponent<Text>();
        closeText.text = "✕";
        closeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        closeText.fontSize = 24;
        closeText.fontStyle = FontStyle.Bold;
        closeText.color = new Color(0.25f, 0.27f, 0.30f);
        closeText.alignment = TextAnchor.MiddleCenter;

        // Settings Content Container (Vertical Layout)
        GameObject contentObj = new GameObject("ContentContainer");
        contentObj.transform.SetParent(settingsCard.transform, false);
        var contentRect = contentObj.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.pivot = new Vector2(0.5f, 0.5f);
        contentRect.anchoredPosition = new Vector2(0f, -22f);
        contentRect.sizeDelta = new Vector2(480f, 360f);

        var vLayout = contentObj.AddComponent<VerticalLayoutGroup>();
        vLayout.childAlignment = TextAnchor.MiddleCenter;
        vLayout.spacing = 16f;
        vLayout.childControlWidth = true;
        vLayout.childControlHeight = false;
        vLayout.childForceExpandWidth = true;
        vLayout.childForceExpandHeight = false;

        // 1. Sound Row
        GameObject soundRow = new GameObject("SoundRow");
        soundRow.transform.SetParent(contentObj.transform, false);
        var sRowRect = soundRow.AddComponent<RectTransform>();
        sRowRect.sizeDelta = new Vector2(480f, 64f);
        var sRowBg = soundRow.AddComponent<Image>();
        sRowBg.sprite = rowBgSprite;

        GameObject soundLabelObj = new GameObject("SoundLabel");
        soundLabelObj.transform.SetParent(soundRow.transform, false);
        var sLblRect = soundLabelObj.AddComponent<RectTransform>();
        sLblRect.anchorMin = new Vector2(0f, 0f);
        sLblRect.anchorMax = new Vector2(0.65f, 1f);
        sLblRect.offsetMin = new Vector2(24f, 0f);
        sLblRect.offsetMax = Vector2.zero;
        var soundLabel = soundLabelObj.AddComponent<Text>();
        soundLabel.text = "Sound SFX";
        soundLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        soundLabel.fontSize = 24;
        soundLabel.fontStyle = FontStyle.Bold;
        soundLabel.color = new Color(0.20f, 0.22f, 0.25f);
        soundLabel.alignment = TextAnchor.MiddleLeft;

        GameObject soundToggleObj = new GameObject("SoundToggleButton");
        soundToggleObj.transform.SetParent(soundRow.transform, false);
        var sTglRect = soundToggleObj.AddComponent<RectTransform>();
        sTglRect.anchorMin = new Vector2(1f, 0.5f);
        sTglRect.anchorMax = new Vector2(1f, 0.5f);
        sTglRect.pivot = new Vector2(1f, 0.5f);
        sTglRect.anchoredPosition = new Vector2(-16f, 0f);
        sTglRect.sizeDelta = new Vector2(100f, 46f);
        var soundToggleBg = soundToggleObj.AddComponent<Image>();
        soundToggleBg.sprite = pillToggleSprite;
        soundToggleBg.color = new Color(0.18f, 0.72f, 0.32f);
        var soundToggleBtn = soundToggleObj.AddComponent<Button>();
        soundToggleBtn.targetGraphic = soundToggleBg;

        GameObject sTglTextObj = new GameObject("SoundToggleText");
        sTglTextObj.transform.SetParent(soundToggleObj.transform, false);
        var stRect = sTglTextObj.AddComponent<RectTransform>();
        stRect.anchorMin = Vector2.zero;
        stRect.anchorMax = Vector2.one;
        stRect.sizeDelta = Vector2.zero;
        var soundToggleText = sTglTextObj.AddComponent<Text>();
        soundToggleText.text = "ON";
        soundToggleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        soundToggleText.fontSize = 22;
        soundToggleText.fontStyle = FontStyle.Bold;
        soundToggleText.color = Color.white;
        soundToggleText.alignment = TextAnchor.MiddleCenter;

        // 2. Music Row
        GameObject musicRow = new GameObject("MusicRow");
        musicRow.transform.SetParent(contentObj.transform, false);
        var mRowRect = musicRow.AddComponent<RectTransform>();
        mRowRect.sizeDelta = new Vector2(480f, 64f);
        var mRowBg = musicRow.AddComponent<Image>();
        mRowBg.sprite = rowBgSprite;

        GameObject musicLabelObj = new GameObject("MusicLabel");
        musicLabelObj.transform.SetParent(musicRow.transform, false);
        var mLblRect = musicLabelObj.AddComponent<RectTransform>();
        mLblRect.anchorMin = new Vector2(0f, 0f);
        mLblRect.anchorMax = new Vector2(0.65f, 1f);
        mLblRect.offsetMin = new Vector2(24f, 0f);
        mLblRect.offsetMax = Vector2.zero;
        var musicLabel = musicLabelObj.AddComponent<Text>();
        musicLabel.text = "Music";
        musicLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        musicLabel.fontSize = 24;
        musicLabel.fontStyle = FontStyle.Bold;
        musicLabel.color = new Color(0.20f, 0.22f, 0.25f);
        musicLabel.alignment = TextAnchor.MiddleLeft;

        GameObject musicToggleObj = new GameObject("MusicToggleButton");
        musicToggleObj.transform.SetParent(musicRow.transform, false);
        var mTglRect = musicToggleObj.AddComponent<RectTransform>();
        mTglRect.anchorMin = new Vector2(1f, 0.5f);
        mTglRect.anchorMax = new Vector2(1f, 0.5f);
        mTglRect.pivot = new Vector2(1f, 0.5f);
        mTglRect.anchoredPosition = new Vector2(-16f, 0f);
        mTglRect.sizeDelta = new Vector2(100f, 46f);
        var musicToggleBg = musicToggleObj.AddComponent<Image>();
        musicToggleBg.sprite = pillToggleSprite;
        musicToggleBg.color = new Color(0.18f, 0.72f, 0.32f);
        var musicToggleBtn = musicToggleObj.AddComponent<Button>();
        musicToggleBtn.targetGraphic = musicToggleBg;

        GameObject mTglTextObj = new GameObject("MusicToggleText");
        mTglTextObj.transform.SetParent(musicToggleObj.transform, false);
        var mtRect = mTglTextObj.AddComponent<RectTransform>();
        mtRect.anchorMin = Vector2.zero;
        mtRect.anchorMax = Vector2.one;
        mtRect.sizeDelta = Vector2.zero;
        var musicToggleText = mTglTextObj.AddComponent<Text>();
        musicToggleText.text = "ON";
        musicToggleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        musicToggleText.fontSize = 22;
        musicToggleText.fontStyle = FontStyle.Bold;
        musicToggleText.color = Color.white;
        musicToggleText.alignment = TextAnchor.MiddleCenter;

        // 3. Restart Level Button inside Settings
        GameObject settingsRestartObj = new GameObject("SettingsRestartButton");
        settingsRestartObj.transform.SetParent(contentObj.transform, false);
        var srRect = settingsRestartObj.AddComponent<RectTransform>();
        srRect.sizeDelta = new Vector2(480f, 64f);
        var srBg = settingsRestartObj.AddComponent<Image>();
        srBg.sprite = orangeBtnSprite;
        var settingsRestartBtn = settingsRestartObj.AddComponent<Button>();
        settingsRestartBtn.targetGraphic = srBg;

        GameObject srTextObj = new GameObject("SettingsRestartText");
        srTextObj.transform.SetParent(settingsRestartObj.transform, false);
        var srtRect = srTextObj.AddComponent<RectTransform>();
        srtRect.anchorMin = Vector2.zero;
        srtRect.anchorMax = Vector2.one;
        srtRect.sizeDelta = Vector2.zero;
        var srText = srTextObj.AddComponent<Text>();
        srText.text = "Restart Level";
        srText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        srText.fontSize = 24;
        srText.fontStyle = FontStyle.Bold;
        srText.color = Color.white;
        srText.alignment = TextAnchor.MiddleCenter;

        // 4. Resume Game Button inside Settings
        GameObject resumeObj = new GameObject("ResumeButton");
        resumeObj.transform.SetParent(contentObj.transform, false);
        var resRect = resumeObj.AddComponent<RectTransform>();
        resRect.sizeDelta = new Vector2(480f, 64f);
        var resBg = resumeObj.AddComponent<Image>();
        resBg.sprite = greenBtnSprite;
        var resumeBtn = resumeObj.AddComponent<Button>();
        resumeBtn.targetGraphic = resBg;

        GameObject resTextObj = new GameObject("ResumeText");
        resTextObj.transform.SetParent(resumeObj.transform, false);
        var restRect = resTextObj.AddComponent<RectTransform>();
        restRect.anchorMin = Vector2.zero;
        restRect.anchorMax = Vector2.one;
        restRect.sizeDelta = Vector2.zero;
        var resText = resTextObj.AddComponent<Text>();
        resText.text = "Resume";
        resText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        resText.fontSize = 24;
        resText.fontStyle = FontStyle.Bold;
        resText.color = Color.white;
        resText.alignment = TextAnchor.MiddleCenter;

        settingsModal.SetActive(false);

        // --- OVERLAY PANELS (WIN / LOSE) ---
        GameObject winPanel = CreateOverlayPanel(canvasObj.transform, "WinPanel", "Level Complete!", "Next Level", new Color(0.12f, 0.65f, 0.22f));
        winPanel.SetActive(false);
        var nextBtn = winPanel.GetComponentInChildren<Button>();

        GameObject losePanel = CreateOverlayPanel(canvasObj.transform, "LosePanel", "Level Failed!", "Try Again", new Color(0.95f, 0.26f, 0.28f));
        losePanel.SetActive(false);
        var retryBtn = losePanel.GetComponentInChildren<Button>();

        // Wire fields via reflection
        SetField(uiMgr, "timerText", timerText);
        SetField(uiMgr, "levelNumberText", lvlText);
        SetField(uiMgr, "heartImages", heartImages);
        SetField(uiMgr, "restartButton", restartBtn);
        SetField(uiMgr, "gearSettingsButton", gearBtn);
        SetField(uiMgr, "settingsModal", settingsModal);
        SetField(uiMgr, "settingsCard", settingsCard.transform);
        SetField(uiMgr, "soundToggleButton", soundToggleBtn);
        SetField(uiMgr, "soundToggleText", soundToggleText);
        SetField(uiMgr, "soundToggleBg", soundToggleBg);
        SetField(uiMgr, "musicToggleButton", musicToggleBtn);
        SetField(uiMgr, "musicToggleText", musicToggleText);
        SetField(uiMgr, "musicToggleBg", musicToggleBg);
        SetField(uiMgr, "closeSettingsButton", closeBtn);
        SetField(uiMgr, "resumeButton", resumeBtn);
        SetField(uiMgr, "settingsRestartButton", settingsRestartBtn);
        SetField(uiMgr, "backdropCloseButton", backdropBtn);
        SetField(uiMgr, "winPanel", winPanel);
        SetField(uiMgr, "losePanel", losePanel);
        SetField(uiMgr, "nextLevelButton", nextBtn);
        SetField(uiMgr, "retryButton", retryBtn);
    }

    private static Sprite CreateRoundedRectSprite(int width, int height, float cornerRadius, Color fillColor, Color borderColor, float borderWidth)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color[] cols = new Color[width * height];

        float halfW = width * 0.5f;
        float halfH = height * 0.5f;
        float innerRadius = Mathf.Max(1f, cornerRadius - borderWidth);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float px = x - halfW;
                float py = y - halfH;

                float qx = Mathf.Abs(px) - (halfW - cornerRadius);
                float qy = Mathf.Abs(py) - (halfH - cornerRadius);

                float dx = Mathf.Max(qx, 0f);
                float dy = Mathf.Max(qy, 0f);
                float distOuter = Mathf.Sqrt(dx * dx + dy * dy) + Mathf.Min(Mathf.Max(qx, qy), 0f) - cornerRadius;

                float qix = Mathf.Abs(px) - (halfW - borderWidth - innerRadius);
                float qiy = Mathf.Abs(py) - (halfH - borderWidth - innerRadius);
                float dix = Mathf.Max(qix, 0f);
                float diy = Mathf.Max(qiy, 0f);
                float distInner = Mathf.Sqrt(dix * dix + diy * diy) + Mathf.Min(Mathf.Max(qix, qiy), 0f) - innerRadius;

                float alpha = 1f - Mathf.Clamp01(distOuter + 0.5f);
                if (alpha <= 0.001f)
                {
                    cols[y * width + x] = Color.clear;
                    continue;
                }

                float borderT = Mathf.Clamp01(distInner + 0.5f);
                Color pixelCol = Color.Lerp(fillColor, borderColor, borderT);
                pixelCol.a *= alpha;
                cols[y * width + x] = pixelCol;
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
    }

    private static Sprite CreateGearIconSprite(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color[] cols = new Color[size * size];

        float cx = size * 0.5f;
        float cy = size * 0.5f;
        int numTeeth = 8;
        float period = Mathf.PI * 2f / numTeeth;
        float rOuter = size * 0.44f;
        float rInner = size * 0.33f;
        float rHole = size * 0.13f;
        float borderW = size * 0.045f;

        float toothW = period * 0.42f;
        float transitionW = period * 0.10f;

        Color fillCol = new Color(0.96f, 0.97f, 0.98f);
        Color borderCol = new Color(0.20f, 0.22f, 0.25f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) - cx;
                float dy = (y + 0.5f) - cy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float theta = Mathf.Atan2(dy, dx);
                if (theta < 0f) theta += Mathf.PI * 2f;

                float normAngle = theta % period;
                float dAngle = Mathf.Abs(normAngle - (period * 0.5f));

                float currentR;
                if (dAngle <= toothW * 0.5f)
                {
                    currentR = rOuter;
                }
                else if (dAngle <= toothW * 0.5f + transitionW)
                {
                    float t = (dAngle - toothW * 0.5f) / transitionW;
                    currentR = Mathf.Lerp(rOuter, rInner, t);
                }
                else
                {
                    currentR = rInner;
                }

                float outerDist = dist - currentR;
                float holeDist = rHole - dist;
                float sdf = Mathf.Max(outerDist, holeDist);

                if (sdf > 1.2f)
                {
                    cols[y * size + x] = Color.clear;
                    continue;
                }

                float alpha = Mathf.Clamp01(0.5f - sdf);
                if (alpha <= 0.01f)
                {
                    cols[y * size + x] = Color.clear;
                    continue;
                }

                Color col;
                if (outerDist > -borderW || holeDist > -borderW)
                {
                    col = borderCol;
                }
                else
                {
                    col = fillCol;
                }
                col.a *= alpha;
                cols[y * size + x] = col;
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private static Sprite CreateGlossyHeartSprite(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color[] cols = new Color[size * size];

        Color heartRedTop = new Color(0.98f, 0.18f, 0.22f);
        Color heartRedBottom = new Color(0.85f, 0.08f, 0.14f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x - size * 0.5f) / (size * 0.40f);
                float ny = (y - size * 0.45f) / (size * 0.40f);

                float x2 = nx * nx;
                float y2 = ny * ny;
                float heartVal = Mathf.Pow(x2 + y2 - 1f, 3) - x2 * ny * y2;

                if (heartVal <= 0.05f)
                {
                    float alpha = Mathf.Clamp01(-heartVal * 20f + 1f);
                    float gradY = (float)y / size;
                    Color baseCol = Color.Lerp(heartRedBottom, heartRedTop, gradY);

                    float hx = nx + 0.42f;
                    float hy = ny - 0.42f;
                    float hDist = Mathf.Sqrt(hx * hx + hy * hy);
                    if (hDist < 0.35f)
                    {
                        float shine = Mathf.Pow(1f - (hDist / 0.35f), 2f) * 0.70f;
                        baseCol = Color.Lerp(baseCol, Color.white, shine);
                    }

                    baseCol.a *= alpha;
                    cols[y * size + x] = baseCol;
                }
                else
                {
                    cols[y * size + x] = Color.clear;
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private static Sprite CreateStopwatchSprite(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color[] cols = new Color[size * size];

        Color goldRim = new Color(0.97f, 0.62f, 0.08f);
        Color whiteFace = new Color(0.96f, 0.98f, 1.0f);
        Color needleBlue = new Color(0.15f, 0.30f, 0.65f);

        float center = size * 0.46f;
        float radius = size * 0.36f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                bool isCrown = (Mathf.Abs(dx) < 6f && y > center + radius - 2f && y < center + radius + 12f);

                if (dist <= radius || isCrown)
                {
                    float alpha = 1f;
                    if (dist > radius - 1f && !isCrown) alpha = Mathf.Clamp01(radius - dist + 1f);

                    Color col = goldRim;
                    if (dist < radius - 6f)
                    {
                        col = whiteFace;

                        if (Mathf.Abs(dx) < 3.5f && dy > -2f && dy < radius - 10f)
                        {
                            col = needleBlue;
                        }
                    }

                    col.a *= alpha;
                    cols[y * size + x] = col;
                }
                else
                {
                    cols[y * size + x] = Color.clear;
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private static Sprite CreateRestartArrowSprite(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color[] cols = new Color[size * size];

        float center = size * 0.5f;
        float ringRadius = size * 0.28f;
        float ringThick = 7f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                if (angle < 0) angle += 360f;

                bool inArc = (dist >= ringRadius - ringThick && dist <= ringRadius + ringThick && (angle > 45f && angle < 320f));

                float ax = dx + 12f;
                float ay = dy - 20f;
                bool inHead = (Mathf.Abs(ax) + Mathf.Abs(ay) < 14f && dx < 0);

                if (inArc || inHead)
                {
                    cols[y * size + x] = Color.white;
                }
                else
                {
                    cols[y * size + x] = Color.clear;
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private static GameObject CreateOverlayPanel(Transform parent, string name, string header, string buttonLabel, Color headerColor)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        var rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.15f, 0.35f);
        rect.anchorMax = new Vector2(0.85f, 0.65f);
        rect.sizeDelta = Vector2.zero;

        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.10f, 0.14f, 0.88f);

        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panel.transform, false);
        var tRect = titleObj.AddComponent<RectTransform>();
        tRect.anchorMin = new Vector2(0.1f, 0.55f);
        tRect.anchorMax = new Vector2(0.9f, 0.9f);
        tRect.sizeDelta = Vector2.zero;

        var titleText = titleObj.AddComponent<Text>();
        titleText.text = header;
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 38;
        titleText.fontStyle = FontStyle.Bold;
        titleText.color = headerColor;
        titleText.alignment = TextAnchor.MiddleCenter;

        GameObject btnObj = new GameObject("ActionButton");
        btnObj.transform.SetParent(panel.transform, false);
        var bRect = btnObj.AddComponent<RectTransform>();
        bRect.anchorMin = new Vector2(0.2f, 0.15f);
        bRect.anchorMax = new Vector2(0.8f, 0.45f);
        bRect.sizeDelta = Vector2.zero;

        var btnImg = btnObj.AddComponent<Image>();
        btnImg.color = headerColor;

        var btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImg;

        GameObject bTextObj = new GameObject("ButtonText");
        bTextObj.transform.SetParent(btnObj.transform, false);
        var btRect = bTextObj.AddComponent<RectTransform>();
        btRect.anchorMin = Vector2.zero;
        btRect.anchorMax = Vector2.one;
        btRect.sizeDelta = Vector2.zero;

        var btnText = bTextObj.AddComponent<Text>();
        btnText.text = buttonLabel;
        btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        btnText.fontSize = 30;
        btnText.fontStyle = FontStyle.Bold;
        btnText.color = Color.white;
        btnText.alignment = TextAnchor.MiddleCenter;

        return panel;
    }

    private static void SetField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        if (field != null)
        {
            field.SetValue(target, value);
        }
    }
}
