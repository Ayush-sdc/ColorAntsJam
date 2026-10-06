using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AntQueueVisualSettings))]
public class AntQueueVisualSettingsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawDefaultInspector();

        EditorGUILayout.Space(12);

        var settings = (AntQueueVisualSettings)target;

        GUI.backgroundColor = new Color(0.2f, 0.85f, 0.4f);
        if (GUILayout.Button("Apply & Rebuild Scene Queues", GUILayout.Height(36)))
        {
            settings.RebuildAllSceneQueues();
            EditorUtility.SetDirty(settings);
        }
        GUI.backgroundColor = Color.white;

        serializedObject.ApplyModifiedProperties();
    }
}
