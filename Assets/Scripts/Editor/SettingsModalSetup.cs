using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor.SceneManagement;
using System.Reflection;

namespace AntsJam.EditorScripts
{
    public static class SettingsModalSetup
    {
        [MenuItem("Tools/Slither Ants Jam/Setup Settings Modal")]
        public static void Setup()
        {
            var uiMgr = Object.FindFirstObjectByType<GameUIManager>();
            if (uiMgr == null)
            {
                Debug.LogError("[SettingsModalSetup] GameUIManager not found in scene!");
                return;
            }

            var canvas = uiMgr.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[SettingsModalSetup] Canvas not found!");
                return;
            }

            // Load sprites
            var panelBgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Sprites/Settings/Settings_Panel_Bg.png");
            var switchBgOn = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Sprites/Settings/Switch_Bg_On.png");
            var switchBgOff = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Sprites/Settings/Switch_Bg_Off.png");
            var switchKnobOn = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Sprites/Settings/Switch_Knob_On.png");
            var switchKnobOff = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Sprites/Settings/Switch_Knob_Off.png");
            AssetDatabase.ImportAsset("Assets/UI/Sprites/Settings/Privacy_Button_Bg.png", ImportAssetOptions.ForceUpdate);
            var privacyBtnBg = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Sprites/Settings/Privacy_Button_Bg.png");

            Font defaultFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/JMO Assets/Toony Colors Pro/Editor/Fonts/Roboto/Roboto-Bold.ttf")
                               ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Find or recreate SettingsModal
            Transform settingsModalTrans = canvas.transform.Find("SettingsModal");
            GameObject modalObj;
            if (settingsModalTrans != null)
            {
                modalObj = settingsModalTrans.gameObject;
            }
            else
            {
                modalObj = new GameObject("SettingsModal", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                modalObj.transform.SetParent(canvas.transform, false);
            }

            var modalRt = modalObj.GetComponent<RectTransform>();
            modalRt.anchorMin = Vector2.zero;
            modalRt.anchorMax = Vector2.one;
            modalRt.offsetMin = Vector2.zero;
            modalRt.offsetMax = Vector2.zero;

            var modalImg = modalObj.GetComponent<Image>();
            modalImg.color = new Color(0f, 0f, 0f, 0.55f);
            modalImg.raycastTarget = true;

            var backdropBtn = modalObj.GetComponent<Button>();
            backdropBtn.targetGraphic = modalImg;

            // Clear old children of SettingsModal
            for (int i = modalObj.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(modalObj.transform.GetChild(i).gameObject);
            }

            // Create SettingsCard
            GameObject cardObj = new GameObject("SettingsCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            cardObj.transform.SetParent(modalObj.transform, false);
            var cardRt = cardObj.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(520f, 440f);
            cardRt.anchoredPosition = Vector2.zero;

            var cardImg = cardObj.GetComponent<Image>();
            cardImg.sprite = panelBgSprite;
            cardImg.type = Image.Type.Simple;
            cardImg.color = Color.white;
            cardImg.raycastTarget = true;

            // 1. Heading "Settings"
            GameObject titleObj = new GameObject("SettingsHeading", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Outline));
            titleObj.transform.SetParent(cardObj.transform, false);
            var titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.5f, 1f);
            titleRt.anchorMax = new Vector2(0.5f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.sizeDelta = new Vector2(400f, 60f);
            titleRt.anchoredPosition = new Vector2(0f, -32f);

            var titleTxt = titleObj.GetComponent<Text>();
            titleTxt.font = defaultFont;
            titleTxt.fontSize = 42;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.color = new Color(1f, 0.88f, 0.12f, 1f); // Vibrant Yellow
            titleTxt.text = "Settings";

            var titleOutline = titleObj.GetComponent<Outline>();
            titleOutline.effectColor = new Color(0.12f, 0.08f, 0.18f, 1f);
            titleOutline.effectDistance = new Vector2(2.5f, -2.5f);

            // 2. Row: Vibration
            GameObject vibRow = new GameObject("VibrationRow", typeof(RectTransform));
            vibRow.transform.SetParent(cardObj.transform, false);
            var vibRowRt = vibRow.GetComponent<RectTransform>();
            vibRowRt.anchorMin = new Vector2(0.5f, 0.5f);
            vibRowRt.anchorMax = new Vector2(0.5f, 0.5f);
            vibRowRt.pivot = new Vector2(0.5f, 0.5f);
            vibRowRt.sizeDelta = new Vector2(420f, 60f);
            vibRowRt.anchoredPosition = new Vector2(0f, 40f);

            GameObject vibLabelObj = new GameObject("VibrationLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Outline));
            vibLabelObj.transform.SetParent(vibRow.transform, false);
            var vibLabelRt = vibLabelObj.GetComponent<RectTransform>();
            vibLabelRt.anchorMin = new Vector2(0f, 0.5f);
            vibLabelRt.anchorMax = new Vector2(0f, 0.5f);
            vibLabelRt.pivot = new Vector2(0f, 0.5f);
            vibLabelRt.sizeDelta = new Vector2(240f, 50f);
            vibLabelRt.anchoredPosition = new Vector2(10f, 0f);

            var vibLabelTxt = vibLabelObj.GetComponent<Text>();
            vibLabelTxt.font = defaultFont;
            vibLabelTxt.fontSize = 32;
            vibLabelTxt.fontStyle = FontStyle.Bold;
            vibLabelTxt.alignment = TextAnchor.MiddleLeft;
            vibLabelTxt.color = Color.white;
            vibLabelTxt.text = "Vibration";

            var vibOutline = vibLabelObj.GetComponent<Outline>();
            vibOutline.effectColor = new Color(0.12f, 0.08f, 0.18f, 1f);
            vibOutline.effectDistance = new Vector2(2f, -2f);

            // Vibration Switch
            GameObject vibSwitchObj = new GameObject("VibrationSwitch", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            vibSwitchObj.transform.SetParent(vibRow.transform, false);
            var vibSwitchRt = vibSwitchObj.GetComponent<RectTransform>();
            vibSwitchRt.anchorMin = new Vector2(1f, 0.5f);
            vibSwitchRt.anchorMax = new Vector2(1f, 0.5f);
            vibSwitchRt.pivot = new Vector2(1f, 0.5f);
            vibSwitchRt.sizeDelta = new Vector2(120f, 52f);
            vibSwitchRt.anchoredPosition = new Vector2(-10f, 0f);

            var vibSwitchImg = vibSwitchObj.GetComponent<Image>();
            vibSwitchImg.sprite = switchBgOn;
            vibSwitchImg.type = Image.Type.Simple;
            var vibSwitchBtn = vibSwitchObj.GetComponent<Button>();
            vibSwitchBtn.targetGraphic = vibSwitchImg;

            GameObject vibKnobObj = new GameObject("VibrationKnob", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            vibKnobObj.transform.SetParent(vibSwitchObj.transform, false);
            var vibKnobRt = vibKnobObj.GetComponent<RectTransform>();
            vibKnobRt.anchorMin = new Vector2(0.5f, 0.5f);
            vibKnobRt.anchorMax = new Vector2(0.5f, 0.5f);
            vibKnobRt.pivot = new Vector2(0.5f, 0.5f);
            vibKnobRt.sizeDelta = new Vector2(44f, 44f);
            vibKnobRt.anchoredPosition = new Vector2(28f, 0f);

            var vibKnobImg = vibKnobObj.GetComponent<Image>();
            vibKnobImg.sprite = switchKnobOn;
            vibKnobImg.raycastTarget = false;

            // 3. Row: Sounds
            GameObject sndRow = new GameObject("SoundsRow", typeof(RectTransform));
            sndRow.transform.SetParent(cardObj.transform, false);
            var sndRowRt = sndRow.GetComponent<RectTransform>();
            sndRowRt.anchorMin = new Vector2(0.5f, 0.5f);
            sndRowRt.anchorMax = new Vector2(0.5f, 0.5f);
            sndRowRt.pivot = new Vector2(0.5f, 0.5f);
            sndRowRt.sizeDelta = new Vector2(420f, 60f);
            sndRowRt.anchoredPosition = new Vector2(0f, -32f);

            GameObject sndLabelObj = new GameObject("SoundsLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Outline));
            sndLabelObj.transform.SetParent(sndRow.transform, false);
            var sndLabelRt = sndLabelObj.GetComponent<RectTransform>();
            sndLabelRt.anchorMin = new Vector2(0f, 0.5f);
            sndLabelRt.anchorMax = new Vector2(0f, 0.5f);
            sndLabelRt.pivot = new Vector2(0f, 0.5f);
            sndLabelRt.sizeDelta = new Vector2(240f, 50f);
            sndLabelRt.anchoredPosition = new Vector2(10f, 0f);

            var sndLabelTxt = sndLabelObj.GetComponent<Text>();
            sndLabelTxt.font = defaultFont;
            sndLabelTxt.fontSize = 32;
            sndLabelTxt.fontStyle = FontStyle.Bold;
            sndLabelTxt.alignment = TextAnchor.MiddleLeft;
            sndLabelTxt.color = Color.white;
            sndLabelTxt.text = "Sounds";

            var sndOutline = sndLabelObj.GetComponent<Outline>();
            sndOutline.effectColor = new Color(0.12f, 0.08f, 0.18f, 1f);
            sndOutline.effectDistance = new Vector2(2f, -2f);

            // Sounds Switch
            GameObject sndSwitchObj = new GameObject("SoundsSwitch", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            sndSwitchObj.transform.SetParent(sndRow.transform, false);
            var sndSwitchRt = sndSwitchObj.GetComponent<RectTransform>();
            sndSwitchRt.anchorMin = new Vector2(1f, 0.5f);
            sndSwitchRt.anchorMax = new Vector2(1f, 0.5f);
            sndSwitchRt.pivot = new Vector2(1f, 0.5f);
            sndSwitchRt.sizeDelta = new Vector2(120f, 52f);
            sndSwitchRt.anchoredPosition = new Vector2(-10f, 0f);

            var sndSwitchImg = sndSwitchObj.GetComponent<Image>();
            sndSwitchImg.sprite = switchBgOn;
            sndSwitchImg.type = Image.Type.Simple;
            var sndSwitchBtn = sndSwitchObj.GetComponent<Button>();
            sndSwitchBtn.targetGraphic = sndSwitchImg;

            GameObject sndKnobObj = new GameObject("SoundsKnob", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            sndKnobObj.transform.SetParent(sndSwitchObj.transform, false);
            var sndKnobRt = sndKnobObj.GetComponent<RectTransform>();
            sndKnobRt.anchorMin = new Vector2(0.5f, 0.5f);
            sndKnobRt.anchorMax = new Vector2(0.5f, 0.5f);
            sndKnobRt.pivot = new Vector2(0.5f, 0.5f);
            sndKnobRt.sizeDelta = new Vector2(44f, 44f);
            sndKnobRt.anchoredPosition = new Vector2(28f, 0f);

            var sndKnobImg = sndKnobObj.GetComponent<Image>();
            sndKnobImg.sprite = switchKnobOn;
            sndKnobImg.raycastTarget = false;

            // 4. Privacy Policy Button
            GameObject privBtnObj = new GameObject("PrivacyPolicyButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            privBtnObj.transform.SetParent(cardObj.transform, false);
            var privBtnRt = privBtnObj.GetComponent<RectTransform>();
            privBtnRt.anchorMin = new Vector2(0.5f, 0f);
            privBtnRt.anchorMax = new Vector2(0.5f, 0f);
            privBtnRt.pivot = new Vector2(0.5f, 0f);
            privBtnRt.sizeDelta = new Vector2(240f, 52f);
            privBtnRt.anchoredPosition = new Vector2(0f, 32f);

            var privBtnImg = privBtnObj.GetComponent<Image>();
            if (privacyBtnBg != null)
            {
                privBtnImg.sprite = privacyBtnBg;
                privBtnImg.type = Image.Type.Sliced;
            }
            privBtnImg.color = new Color(0.29f, 0.13f, 0.48f, 1f); // Darker purple matching reference
            var privBtn = privBtnObj.GetComponent<Button>();
            privBtn.targetGraphic = privBtnImg;

            GameObject privTxtObj = new GameObject("PrivacyPolicyText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            privTxtObj.transform.SetParent(privBtnObj.transform, false);
            var privTxtRt = privTxtObj.GetComponent<RectTransform>();
            privTxtRt.anchorMin = Vector2.zero;
            privTxtRt.anchorMax = Vector2.one;
            privTxtRt.offsetMin = Vector2.zero;
            privTxtRt.offsetMax = Vector2.zero;

            var privTxt = privTxtObj.GetComponent<Text>();
            privTxt.font = defaultFont;
            privTxt.fontSize = 20;
            privTxt.fontStyle = FontStyle.Bold;
            privTxt.alignment = TextAnchor.MiddleCenter;
            privTxt.color = Color.white;
            privTxt.text = "Privacy Policy";
            privTxt.raycastTarget = false;

            // Wire all fields on GameUIManager via reflection / SerializedObject
            var so = new SerializedObject(uiMgr);
            so.FindProperty("settingsModal").objectReferenceValue = modalObj;
            so.FindProperty("settingsCard").objectReferenceValue = cardObj.transform;
            so.FindProperty("backdropCloseButton").objectReferenceValue = backdropBtn;
            so.FindProperty("soundToggleButton").objectReferenceValue = sndSwitchBtn;
            so.FindProperty("soundToggleBg").objectReferenceValue = sndSwitchImg;
            so.FindProperty("soundKnobTransform").objectReferenceValue = sndKnobRt;
            so.FindProperty("soundKnobImage").objectReferenceValue = sndKnobImg;
            so.FindProperty("vibrationToggleButton").objectReferenceValue = vibSwitchBtn;
            so.FindProperty("vibrationToggleBg").objectReferenceValue = vibSwitchImg;
            so.FindProperty("vibrationKnobTransform").objectReferenceValue = vibKnobRt;
            so.FindProperty("vibrationKnobImage").objectReferenceValue = vibKnobImg;
            so.FindProperty("privacyPolicyButton").objectReferenceValue = privBtn;
            so.FindProperty("switchBgOn").objectReferenceValue = switchBgOn;
            so.FindProperty("switchBgOff").objectReferenceValue = switchBgOff;
            so.FindProperty("switchKnobOn").objectReferenceValue = switchKnobOn;
            so.FindProperty("switchKnobOff").objectReferenceValue = switchKnobOff;
            so.ApplyModifiedProperties();

            modalObj.SetActive(false);
            EditorUtility.SetDirty(uiMgr);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            EditorSceneManager.SaveScene(canvas.gameObject.scene);

            Debug.Log("[SettingsModalSetup] Settings modal successfully created and configured to match reference design!");
        }
    }
}
