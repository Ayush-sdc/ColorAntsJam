#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class RecordFailedMoveMenuItem
{
    [MenuItem("Tools/Slither Ants Jam/Tests/Record Failed Move Sequence Video")]
    public static void RecordVideo()
    {
        if (!EditorApplication.isPlaying)
        {
            EditorPrefs.SetBool("AutoRecordFailedMoveVideo", true);
            EditorApplication.isPlaying = true;
            return;
        }

        SpawnRecorder();
    }

    [InitializeOnLoadMethod]
    private static void CheckAutoRecord()
    {
        EditorApplication.playModeStateChanged += (state) =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && EditorPrefs.GetBool("AutoRecordFailedMoveVideo", false))
            {
                EditorPrefs.DeleteKey("AutoRecordFailedMoveVideo");
                SpawnRecorder();
            }
        };
    }

    private static void SpawnRecorder()
    {
        var existing = UnityEngine.Object.FindAnyObjectByType<FailedMoveVideoRecorder>();
        if (existing != null) UnityEngine.Object.Destroy(existing.gameObject);

        var go = new GameObject("FailedMoveVideoRecorder");
        UnityEngine.Object.DontDestroyOnLoad(go);
        var recorder = go.AddComponent<FailedMoveVideoRecorder>();
        recorder.StartRecording();
    }
}
#endif
