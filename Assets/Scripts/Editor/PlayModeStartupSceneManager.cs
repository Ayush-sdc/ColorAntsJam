#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AntsJam.Editor
{
    /// <summary>
    /// Enforces that hitting Play in Unity Editor always starts with the Welcome scene (Welcome.unity),
    /// matching the standalone application entry point. Also provides a clearly labeled testing override
    /// to directly play the currently open scene.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayModeStartupSceneManager
    {
        private const string PrefKey_DirectPlayCurrentScene = "SlitherAnts_DirectPlayCurrentSceneOverride";
        public const string WelcomeScenePath = "Assets/Scenes/Welcome.unity";

        static PlayModeStartupSceneManager()
        {
            EditorApplication.delayCall += ApplyPlayModeStartScene;
        }

        public static bool IsDirectPlayOverrideEnabled
        {
            get => EditorPrefs.GetBool(PrefKey_DirectPlayCurrentScene, false);
            set
            {
                EditorPrefs.SetBool(PrefKey_DirectPlayCurrentScene, value);
                ApplyPlayModeStartScene();
            }
        }

        public static void ApplyPlayModeStartScene()
        {
            bool directPlay = IsDirectPlayOverrideEnabled;

            if (directPlay)
            {
                // Testing override enabled: play whatever scene is currently open in the editor
                EditorSceneManager.playModeStartScene = null;
                Debug.Log("[PlayModeStartup] Testing override ACTIVE: Play mode will start in the currently open scene.");
            }
            else
            {
                // Normal flow: always start from Welcome scene
                var welcomeSceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(WelcomeScenePath);
                if (welcomeSceneAsset != null)
                {
                    EditorSceneManager.playModeStartScene = welcomeSceneAsset;
                    Debug.Log($"[PlayModeStartup] Normal flow ACTIVE: Play mode will start in {WelcomeScenePath}.");
                }
                else
                {
                    Debug.LogWarning($"[PlayModeStartup] Could not find Welcome scene at {WelcomeScenePath}!");
                    EditorSceneManager.playModeStartScene = null;
                }
            }
        }

        [MenuItem("Tools/Slither Ants Jam/Play Mode Start Scene/Start With Welcome Scene (Normal Flow)", false, 100)]
        public static void EnableWelcomeSceneStartup()
        {
            IsDirectPlayOverrideEnabled = false;
        }

        [MenuItem("Tools/Slither Ants Jam/Play Mode Start Scene/Start With Welcome Scene (Normal Flow)", true)]
        public static bool ValidateEnableWelcomeSceneStartup()
        {
            Menu.SetChecked("Tools/Slither Ants Jam/Play Mode Start Scene/Start With Welcome Scene (Normal Flow)", !IsDirectPlayOverrideEnabled);
            return true;
        }

        [MenuItem("Tools/Slither Ants Jam/Play Mode Start Scene/Play Current Open Scene (Testing Override)", false, 101)]
        public static void EnableDirectPlayOverride()
        {
            IsDirectPlayOverrideEnabled = true;
        }

        [MenuItem("Tools/Slither Ants Jam/Play Mode Start Scene/Play Current Open Scene (Testing Override)", true)]
        public static bool ValidateEnableDirectPlayOverride()
        {
            Menu.SetChecked("Tools/Slither Ants Jam/Play Mode Start Scene/Play Current Open Scene (Testing Override)", IsDirectPlayOverrideEnabled);
            return true;
        }
    }
}
#endif
