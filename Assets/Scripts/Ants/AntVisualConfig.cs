using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Centralized authoritative visual configuration for ant prefabs and visual appearance.
/// Queue index 0 (leader) uses ChubbyAntColors; all followers and tail (index >= 1) use WorkerAnt.
/// Colors are applied using MaterialPropertyBlock to achieve 100% exact parity with Hole SlotRim.
/// </summary>
[CreateAssetMenu(fileName = "AntVisualConfig", menuName = "Slither Ants/Ant Visual Config")]
public class AntVisualConfig : ScriptableObject
{
    private static AntVisualConfig s_Instance;

    public static AntVisualConfig Instance
    {
        get
        {
            if (s_Instance == null)
            {
                s_Instance = Resources.Load<AntVisualConfig>("AntVisualConfig");
                if (s_Instance == null)
                {
#if UNITY_EDITOR
                    string[] guids = UnityEditor.AssetDatabase.FindAssets("t:AntVisualConfig");
                    if (guids != null && guids.Length > 0)
                    {
                        string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                        s_Instance = UnityEditor.AssetDatabase.LoadAssetAtPath<AntVisualConfig>(path);
                    }
#endif
                }

                if (s_Instance == null)
                {
                    s_Instance = CreateInstance<AntVisualConfig>();
                    s_Instance.InitializeDefaults();
                }
            }
            return s_Instance;
        }
    }

    [Header("Default Prefabs")]
    [Tooltip("Default ChubbyAntColors prefab used for leading ant (index 0)")]
    [SerializeField] private GameObject defaultLeaderPrefab;

    [Tooltip("Default WorkerAnt prefab used for followers and tail (index >= 1)")]
    [SerializeField] private GameObject defaultWorkerPrefab;

    [Header("Color-Mapped Leader Prefabs (ChubbyAntColors)")]
    [SerializeField] private List<ColorPrefabMapping> leaderPrefabs = new List<ColorPrefabMapping>();

    [Header("Color-Mapped Worker Prefabs (WorkerAnt)")]
    [SerializeField] private List<ColorPrefabMapping> workerPrefabs = new List<ColorPrefabMapping>();

    [Header("Visual Scales")]
    [Range(0.20f, 0.45f)]
    [SerializeField] private float leaderScale = 0.37f;
    [Range(0.20f, 0.45f)]
    [SerializeField] private float workerScale = 0.315f;

    [Header("Visual Lengths (for hole entry & despawn depth)")]
    [SerializeField] private float leaderVisualLength = 1.10f;
    [SerializeField] private float workerVisualLength = 0.92f;

    [Header("Walk Bob & Tilt Animation")]
    [SerializeField] private float leaderBobHeight = 0.028f;
    [SerializeField] private float leaderTiltAngle = 2.8f;
    [SerializeField] private float workerBobHeight = 0.035f;
    [SerializeField] private float workerTiltAngle = 3.5f;

    public float LeaderScale => leaderScale;
    public float WorkerScale => workerScale;
    public float LeaderVisualLength => leaderVisualLength;
    public float WorkerVisualLength => workerVisualLength;
    public float LeaderBobHeight => leaderBobHeight;
    public float LeaderTiltAngle => leaderTiltAngle;
    public float WorkerBobHeight => workerBobHeight;
    public float WorkerTiltAngle => workerTiltAngle;

    [Serializable]
    public struct ColorPrefabMapping
    {
        public AntColor color;
        public GameObject prefab;
    }

    private static readonly int PropColor = Shader.PropertyToID("_Color");
    private static readonly int PropShadowColor = Shader.PropertyToID("_ShadowColor");
    private static readonly int PropBaseColor = Shader.PropertyToID("_BaseColor");

    private static MaterialPropertyBlock s_BodyMpb;
    private static MaterialPropertyBlock s_AccentMpb;
    private static MaterialPropertyBlock s_IrisMpb;

    /// <summary>
    /// Returns the ChubbyAntColors prefab for the leading ant (queue index 0).
    /// </summary>
    public GameObject GetLeaderPrefab(AntColor color)
    {
        if (leaderPrefabs != null)
        {
            for (int i = 0; i < leaderPrefabs.Count; i++)
            {
                if (leaderPrefabs[i].color == color && leaderPrefabs[i].prefab != null)
                {
                    return leaderPrefabs[i].prefab;
                }
            }
        }

        if (defaultLeaderPrefab != null)
        {
            return defaultLeaderPrefab;
        }

#if UNITY_EDITOR
        var loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ChubbyAntColors/Prefabs/ChubbyAnt_Red.prefab");
        if (loaded != null) return loaded;
#endif

        Debug.LogError("[AntVisualConfig] ChubbyAntColors leader prefab reference is missing! Fallback to Snake/Slither visual is STRICTLY FORBIDDEN.");
        return null;
    }

    /// <summary>
    /// Returns the WorkerAnt prefab for all followers and tail (queue index >= 1).
    /// </summary>
    public GameObject GetWorkerPrefab(AntColor color)
    {
        if (workerPrefabs != null)
        {
            for (int i = 0; i < workerPrefabs.Count; i++)
            {
                if (workerPrefabs[i].color == color && workerPrefabs[i].prefab != null)
                {
                    return workerPrefabs[i].prefab;
                }
            }
        }

        if (defaultWorkerPrefab != null)
        {
            return defaultWorkerPrefab;
        }

#if UNITY_EDITOR
        var loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/WorkerAnt/Prefabs/WorkerAnt_Red.prefab");
        if (loaded != null) return loaded;
#endif

        Debug.LogError("[AntVisualConfig] WorkerAnt follower prefab reference is missing! Fallback to Snake/Slither visual is STRICTLY FORBIDDEN.");
        return null;
    }

    /// <summary>
    /// Applies authoritative resolved queue color to ant body, limbs, and iris using MaterialPropertyBlock.
    /// Preserves neutral eye white, pupils, glints, and cheeks.
    /// </summary>
    public static void ApplyAntColor(GameObject visualInstance, Color resolvedColor)
    {
        if (visualInstance == null) return;

        if (s_BodyMpb == null) s_BodyMpb = new MaterialPropertyBlock();
        if (s_AccentMpb == null) s_AccentMpb = new MaterialPropertyBlock();
        if (s_IrisMpb == null) s_IrisMpb = new MaterialPropertyBlock();

        Color shadowCol = resolvedColor * 0.28f;
        Color limbCol = resolvedColor * 0.64f;
        Color limbShadowCol = limbCol * 0.28f;
        Color irisCol = resolvedColor * 0.30f;
        Color irisShadowCol = irisCol * 0.28f;

        var renderers = visualInstance.GetComponentsInChildren<MeshRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (r == null) continue;

            string partName = r.gameObject.name.ToLowerInvariant();

            if (partName == "body")
            {
                r.GetPropertyBlock(s_BodyMpb);
                s_BodyMpb.SetColor(PropColor, resolvedColor);
                s_BodyMpb.SetColor(PropShadowColor, shadowCol);
                s_BodyMpb.SetColor(PropBaseColor, resolvedColor);
                r.SetPropertyBlock(s_BodyMpb);
            }
            else if (partName == "accent")
            {
                r.GetPropertyBlock(s_AccentMpb);
                s_AccentMpb.SetColor(PropColor, limbCol);
                s_AccentMpb.SetColor(PropShadowColor, limbShadowCol);
                s_AccentMpb.SetColor(PropBaseColor, limbCol);
                r.SetPropertyBlock(s_AccentMpb);
            }
            else if (partName == "iris")
            {
                r.GetPropertyBlock(s_IrisMpb);
                s_IrisMpb.SetColor(PropColor, irisCol);
                s_IrisMpb.SetColor(PropShadowColor, irisShadowCol);
                s_IrisMpb.SetColor(PropBaseColor, irisCol);
                r.SetPropertyBlock(s_IrisMpb);
            }
            else if (partName == "rope")
            {
                r.gameObject.SetActive(false);
            }
            // "eyes", "pupils", "glints", "blush" remain untouched preserving neutral features
        }
    }

    /// <summary>
    /// Strips gameplay colliders, rigidbodies, and unwanted scripts from the runtime visual instance.
    /// </summary>
    public static void StripConflictingComponents(GameObject visualInstance)
    {
        if (visualInstance == null) return;

        var colliders = visualInstance.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = false;
                SafeDestroy(colliders[i]);
            }
        }

        var rigidbodies = visualInstance.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < rigidbodies.Length; i++)
        {
            if (rigidbodies[i] != null)
            {
                SafeDestroy(rigidbodies[i]);
            }
        }

        Transform rope = visualInstance.transform.Find("rope");
        if (rope != null)
        {
            rope.gameObject.SetActive(false);
        }
    }

    private static void SafeDestroy(UnityEngine.Object obj)
    {
        if (obj == null) return;
        if (Application.isPlaying) Destroy(obj);
        else DestroyImmediate(obj);
    }

    public void InitializeDefaults()
    {
#if UNITY_EDITOR
        defaultLeaderPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ChubbyAntColors/Prefabs/ChubbyAnt_Red.prefab");
        defaultWorkerPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/WorkerAnt/Prefabs/WorkerAnt_Red.prefab");

        leaderPrefabs = new List<ColorPrefabMapping>
        {
            new ColorPrefabMapping { color = AntColor.Red, prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ChubbyAntColors/Prefabs/ChubbyAnt_Red.prefab") },
            new ColorPrefabMapping { color = AntColor.Yellow, prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ChubbyAntColors/Prefabs/ChubbyAnt_Yellow.prefab") },
            new ColorPrefabMapping { color = AntColor.SkyBlue, prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ChubbyAntColors/Prefabs/ChubbyAnt_Blue.prefab") },
            new ColorPrefabMapping { color = AntColor.Blue, prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ChubbyAntColors/Prefabs/ChubbyAnt_Blue.prefab") },
            new ColorPrefabMapping { color = AntColor.Green, prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ChubbyAntColors/Prefabs/ChubbyAnt_Green.prefab") },
            new ColorPrefabMapping { color = AntColor.Orange, prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ChubbyAntColors/Prefabs/ChubbyAnt_Orange.prefab") },
            new ColorPrefabMapping { color = AntColor.Pink, prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ChubbyAntColors/Prefabs/ChubbyAnt_Pink.prefab") },
            new ColorPrefabMapping { color = AntColor.Purple, prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ChubbyAntColors/Prefabs/ChubbyAnt_Purple.prefab") },
            new ColorPrefabMapping { color = AntColor.Brown, prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ChubbyAntColors/Prefabs/ChubbyAnt_Brown.prefab") },
            new ColorPrefabMapping { color = AntColor.Turquoise, prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ChubbyAntColors/Prefabs/ChubbyAnt_Cyan.prefab") },
            new ColorPrefabMapping { color = AntColor.Cyan, prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ChubbyAntColors/Prefabs/ChubbyAnt_Cyan.prefab") }
        };

        workerPrefabs = new List<ColorPrefabMapping>
        {
            new ColorPrefabMapping { color = AntColor.Red, prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/WorkerAnt/Prefabs/WorkerAnt_Red.prefab") },
            new ColorPrefabMapping { color = AntColor.Yellow, prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/WorkerAnt/Prefabs/WorkerAnt_Yellow.prefab") },
            new ColorPrefabMapping { color = AntColor.SkyBlue, prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/WorkerAnt/Prefabs/WorkerAnt_Blue.prefab") },
            new ColorPrefabMapping { color = AntColor.Blue, prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/WorkerAnt/Prefabs/WorkerAnt_Blue.prefab") },
            new ColorPrefabMapping { color = AntColor.Green, prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/WorkerAnt/Prefabs/WorkerAnt_Green.prefab") },
            new ColorPrefabMapping { color = AntColor.Orange, prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/WorkerAnt/Prefabs/WorkerAnt_Orange.prefab") },
            new ColorPrefabMapping { color = AntColor.Pink, prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/WorkerAnt/Prefabs/WorkerAnt_Pink.prefab") },
            new ColorPrefabMapping { color = AntColor.Purple, prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/WorkerAnt/Prefabs/WorkerAnt_Purple.prefab") },
            new ColorPrefabMapping { color = AntColor.Brown, prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/WorkerAnt/Prefabs/WorkerAnt_Brown.prefab") },
            new ColorPrefabMapping { color = AntColor.Turquoise, prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/WorkerAnt/Prefabs/WorkerAnt_Cyan.prefab") },
            new ColorPrefabMapping { color = AntColor.Cyan, prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/WorkerAnt/Prefabs/WorkerAnt_Cyan.prefab") }
        };
#endif
    }
}
