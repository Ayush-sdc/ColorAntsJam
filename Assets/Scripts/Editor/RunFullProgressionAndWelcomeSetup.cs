#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using AntsJam.Editor;

public static class RunFullProgressionAndWelcomeSetup
{
    [MenuItem("Tools/Slither Ants Jam/Run Setup For Welcome Scene and Progression")]
    public static void Execute()
    {
        Debug.Log("[Setup] Step 1: Configuring sprites and FeatureProgressionConfig...");
        UIOverlaysSetup.ConfigureAllSprites();
        UIOverlaysSetup.CreateOrUpdateProgressionConfig();

        Debug.Log("[Setup] Step 2: Generating Welcome.unity and configuring build scenes...");
        CreateWelcomeScene.CreateAndSetupWelcomeScene();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Setup] Completed Welcome scene and Progression configuration!");
    }
}
#endif
