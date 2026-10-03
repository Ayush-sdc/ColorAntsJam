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

        // 3. Tutorial Preview
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Tutorial & Overlay Previews", EditorStyles.boldLabel);

        if (GUILayout.Button("Preview Locked Ants Tutorial", GUILayout.Height(30)))
        {
            if (Application.isPlaying)
            {
                var uiMgr = GameUIManager.Instance;
                var cfg = FeatureProgressionConfig.Instance;
                if (uiMgr != null && uiMgr.FeatureTutorialModal != null && cfg != null)
                {
                    var feat = cfg.GetFeature("locked_ants");
                    if (feat != null)
                    {
                        GameManager.Instance?.PauseGame();
                        uiMgr.FeatureTutorialModal.ShowTutorials(new List<FeatureDefinition> { feat }, () =>
                        {
                            GameManager.Instance?.ResumeGame();
                            Debug.Log("[TestingWindow] Locked Ants tutorial preview dismissed.");
                        });
                    }
                    else
                    {
                        Debug.LogError("[TestingWindow] Could not find 'locked_ants' feature definition.");
                    }
                }
                else
                {
                    Debug.LogWarning("[TestingWindow] GameUIManager or FeatureTutorialModal not ready in Play Mode.");
                }
            }
            else
            {
                EditorUtility.DisplayDialog("Play Mode Required", "Please enter Play Mode to preview the Locked Ants Tutorial overlay in the Game View.", "OK");
            }
        }

        if (GUILayout.Button("Reset Tutorial Acknowledgements", GUILayout.Height(24)))
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
            Debug.Log("[TestingWindow] All tutorial acknowledgements reset.");
        }
        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

        // 4. Quick Level Select
        if (Application.isPlaying)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Quick Level Jump (Play Mode)", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Lvl 7")) LevelManager.Instance?.LoadLevel(7);
            if (GUILayout.Button("Lvl 8")) LevelManager.Instance?.LoadLevel(8);
            if (GUILayout.Button("Lvl 11")) LevelManager.Instance?.LoadLevel(11);
            if (GUILayout.Button("Lvl 23")) LevelManager.Instance?.LoadLevel(23);
            if (GUILayout.Button("Lvl 25")) LevelManager.Instance?.LoadLevel(25);
            if (GUILayout.Button("Lvl 29")) LevelManager.Instance?.LoadLevel(29);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
    }
}
