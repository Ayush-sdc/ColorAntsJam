using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using AntsJam.Progression;
using AntsJam.UI;

public class SlitherAntsTestingWindow : EditorWindow
{
    private const string FreezeTimerPrefKey = "SlitherAnts_FreezeTimer";

    [MenuItem("SlitherAnts/Testing & Debug Window")]
    [MenuItem("SlitherAnts/Testing Window")]
    public static void ShowWindow()
    {
        var window = GetWindow<SlitherAntsTestingWindow>("Slither Ants Testing");
        window.minSize = new Vector2(360, 420);
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        EditorGUILayout.LabelField("Slither Ants Jam — Testing Tools", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Testing controls for level validation, camera framing, and tutorial preview.", MessageType.Info);
        GUILayout.Space(10);

        // 1. Timer Freeze Toggle
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Timer & Gameplay Testing", EditorStyles.boldLabel);

        bool currentFreeze = EditorPrefs.GetBool(FreezeTimerPrefKey, false);
        if (Application.isPlaying && GameManager.Instance != null)
        {
            currentFreeze = GameManager.Instance.IsTimerFrozen;
        }

        EditorGUI.BeginChangeCheck();
        bool newFreeze = EditorGUILayout.ToggleLeft("Testing Mode: Freeze Level Timer", currentFreeze, EditorStyles.boldLabel);
        if (EditorGUI.EndChangeCheck())
        {
            EditorPrefs.SetBool(FreezeTimerPrefKey, newFreeze);
            if (Application.isPlaying && GameManager.Instance != null)
            {
                GameManager.Instance.IsTimerFrozen = newFreeze;
            }
        }

        if (newFreeze)
        {
            EditorGUILayout.HelpBox("Timer freeze active: Level timer does not count down, timeout failure is disabled, movement and input work normally, HUD indicates [TEST].", MessageType.Warning);
        }
        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

        // 2. Camera Controls
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Camera Framing", EditorStyles.boldLabel);

        if (GUILayout.Button("Fit Board Below HUD", GUILayout.Height(32)))
        {
            if (BoardCameraController.Instance != null)
            {
                BoardCameraController.Instance.FitBoard();
                Debug.Log("[TestingWindow] Board reframed below HUD with world Z baseline 2.63.");
            }
            else
            {
                var bcc = Object.FindFirstObjectByType<BoardCameraController>();
                if (bcc != null)
                {
                    bcc.FitBoard();
                    Debug.Log("[TestingWindow] Found and fitted BoardCameraController.");
                }
                else
                {
                    Debug.LogWarning("[TestingWindow] BoardCameraController not found in active scene.");
                }
            }
        }
        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

        // 3. Tutorial & Overlay Previews
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Tutorial & Overlay Previews", EditorStyles.boldLabel);

        if (GUILayout.Button("Preview Welcome Scene (No Save Reset)", GUILayout.Height(28)))
        {
            AntsJam.Editor.CreateWelcomeScene.PreviewWelcomeScene();
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Feature Tutorial Previews (Play Mode):", EditorStyles.miniLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Linked (Lvl 7)")) PreviewTutorial("linked_ants");
        if (GUILayout.Button("Stacked (Lvl 15)")) PreviewTutorial("stacked_ants");
        if (GUILayout.Button("Pipes (Lvl 21)")) PreviewTutorial("pipes");
        if (GUILayout.Button("Locked (Lvl 30)")) PreviewTutorial("locked_ants");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);
        if (GUILayout.Button("Reset Tutorial Acknowledgements (Scoped)", GUILayout.Height(24)))
        {
            var cfg = FeatureProgressionConfig.Instance;
            if (cfg != null)
            {
                cfg.ResetTutorialAcknowledgements();
            }
            PlayerPrefs.DeleteKey("TutorialSeen_locked_ants");
            PlayerPrefs.DeleteKey("TutorialSeen_linked_ants");
            PlayerPrefs.DeleteKey("TutorialSeen_stacked_ants");
            PlayerPrefs.DeleteKey("TutorialSeen_pipes");
            PlayerPrefs.Save();
            Debug.Log("[TestingWindow] Tutorial acknowledgement flags reset cleanly. (Campaign progress and legal terms preserved).");
        }
        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

        // 4. Quick Level Select at Boundary Milestones
        if (Application.isPlaying)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Milestone Boundary Quick Jumps (Play Mode)", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Lvl 1")) LevelManager.Instance?.LoadLevel(1);
            if (GUILayout.Button("Lvl 6")) LevelManager.Instance?.LoadLevel(6);
            if (GUILayout.Button("Lvl 7")) LevelManager.Instance?.LoadLevel(7);
            if (GUILayout.Button("Lvl 14")) LevelManager.Instance?.LoadLevel(14);
            if (GUILayout.Button("Lvl 15")) LevelManager.Instance?.LoadLevel(15);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Lvl 20")) LevelManager.Instance?.LoadLevel(20);
            if (GUILayout.Button("Lvl 21")) LevelManager.Instance?.LoadLevel(21);
            if (GUILayout.Button("Lvl 29")) LevelManager.Instance?.LoadLevel(29);
            if (GUILayout.Button("Lvl 30")) LevelManager.Instance?.LoadLevel(30);
            if (GUILayout.Button("Lvl 45")) LevelManager.Instance?.LoadLevel(45);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
    }

    private static void PreviewTutorial(string featureId)
    {
        if (Application.isPlaying)
        {
            var uiMgr = GameUIManager.Instance;
            var cfg = FeatureProgressionConfig.Instance;
            if (uiMgr != null && uiMgr.FeatureTutorialModal != null && cfg != null)
            {
                var feat = cfg.GetFeature(featureId);
                if (feat != null)
                {
                    GameManager.Instance?.PauseGame();
                    uiMgr.FeatureTutorialModal.ShowTutorials(new List<FeatureDefinition> { feat }, () =>
                    {
                        GameManager.Instance?.ResumeGame();
                        Debug.Log($"[TestingWindow] '{featureId}' tutorial preview dismissed.");
                    });
                }
                else
                {
                    Debug.LogError($"[TestingWindow] Could not find '{featureId}' feature definition.");
                }
            }
            else
            {
                Debug.LogWarning("[TestingWindow] GameUIManager or FeatureTutorialModal not ready in Play Mode.");
            }
        }
        else
        {
            EditorUtility.DisplayDialog("Play Mode Required", $"Please enter Play Mode in scene 'main' to preview the '{featureId}' tutorial overlay.", "OK");
        }
    }
}
