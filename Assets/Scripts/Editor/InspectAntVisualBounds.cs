#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class InspectAntVisualBounds
{
    [MenuItem("Tools/Slither Ants Jam/Tests/Inspect Ant Visual Bounds")]
    public static void Inspect()
    {
        var go = new GameObject("TempAntUnit");
        var unit = go.AddComponent<AntUnit>();
        unit.Setup(AntColor.Red, head: true, tail: false);

        Bounds leaderBounds = new Bounds(go.transform.position, Vector3.zero);
        bool first = true;
        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            if (first) { leaderBounds = r.bounds; first = false; }
            else leaderBounds.Encapsulate(r.bounds);
        }

        Debug.Log($"[LEADER BOUNDS] Center={leaderBounds.center}, Size={leaderBounds.size}, Extents={leaderBounds.extents}");

        unit.Setup(AntColor.Red, head: false, tail: false);
        Bounds workerBounds = new Bounds(go.transform.position, Vector3.zero);
        first = true;
        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            if (first) { workerBounds = r.bounds; first = false; }
            else workerBounds.Encapsulate(r.bounds);
        }

        Debug.Log($"[WORKER BOUNDS] Center={workerBounds.center}, Size={workerBounds.size}, Extents={workerBounds.extents}");

        Object.DestroyImmediate(go);
    }
}
#endif
