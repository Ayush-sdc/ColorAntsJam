using System;
using UnityEngine;

/// <summary>
/// Authoritative shared visual settings for ant queues.
/// Controls visual scale, pivot offsets, and movement segment spacing
/// independently from the underlying logical grid layout.
/// </summary>
[CreateAssetMenu(fileName = "AntQueueVisualSettings", menuName = "Slither Ants/Ant Queue Visual Settings")]
public class AntQueueVisualSettings : ScriptableObject
{
    private static AntQueueVisualSettings s_Instance;

    public static AntQueueVisualSettings Instance
    {
        get
        {
            if (s_Instance == null)
            {
                s_Instance = Resources.Load<AntQueueVisualSettings>("AntQueueVisualSettings");
#if UNITY_EDITOR
                if (s_Instance == null)
                {
                    string[] guids = UnityEditor.AssetDatabase.FindAssets("t:AntQueueVisualSettings");
                    if (guids != null && guids.Length > 0)
                    {
                        string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                        s_Instance = UnityEditor.AssetDatabase.LoadAssetAtPath<AntQueueVisualSettings>(path);
                    }
                }
#endif
                if (s_Instance == null)
                {
                    s_Instance = CreateInstance<AntQueueVisualSettings>();
                }
            }
            return s_Instance;
        }
    }

    [Header("Visual Scales")]
    [Tooltip("Visual scale of the leading ant (queue index 0, ChubbyAntColors).")]
    [Range(0.20f, 0.50f)]
    [SerializeField] private float leaderVisualScale = 0.38f;

    [Tooltip("Visual scale of follower ants (queue index >= 1, WorkerAnt). Tuned to 0.35 to bring ants close together without lateral merge.")]
    [Range(0.20f, 0.50f)]
    [SerializeField] private float workerVisualScale = 0.35f;

    [Header("Visual Pivot Offsets")]
    [Tooltip("Local offset applied to leader visual model relative to ant unit pivot.")]
    [SerializeField] private Vector3 leaderPivotOffset = Vector3.zero;

    [Tooltip("Local offset applied to worker visual model relative to ant unit pivot.")]
    [SerializeField] private Vector3 workerPivotOffset = Vector3.zero;

    [Header("Movement Spacing & Speed")]
    [Tooltip("Distance along recorded trail between consecutive ants during motion. Defaults to 1.0 to eliminate jump/spreading/bunching.")]
    [Range(0.5f, 1.5f)]
    [SerializeField] private float movementSpacingMultiplier = 1.0f;

    [Tooltip("Movement speed multiplier for all ant queues. Tunable in Inspector. Defaults to 1.5x for noticeably faster movement.")]
    [Range(0.5f, 4.0f)]
    [SerializeField] private float movementSpeedMultiplier = 1.5f;

    public float LeaderVisualScale => leaderVisualScale;
    public float WorkerVisualScale => workerVisualScale;
    public Vector3 LeaderPivotOffset => leaderPivotOffset;
    public Vector3 WorkerPivotOffset => workerPivotOffset;
    public float MovementSpacingMultiplier => movementSpacingMultiplier;
    public float MovementSpeedMultiplier => movementSpeedMultiplier;

    /// <summary>
    /// Applies visual settings to all ant queues currently instantiated in the active scene.
    /// </summary>
    [ContextMenu("Apply & Rebuild Scene Queues")]
    public void RebuildAllSceneQueues()
    {
        var queues = UnityEngine.Object.FindObjectsByType<AntQueueController>(FindObjectsSortMode.None);
        int count = 0;
        foreach (var q in queues)
        {
            if (q != null && !q.IsMoving)
            {
                q.SpawnAnts();
                count++;
            }
        }
        Debug.Log($"[AntQueueVisualSettings] Rebuilt visuals for {count} active queues in scene.");
    }
}
