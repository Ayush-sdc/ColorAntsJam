using UnityEngine;
using UnityEditor;

public class AntBoardGizmoPreview : MonoBehaviour
{
    [SerializeField] private Level levelData;

    private void OnDrawGizmos()
    {
        if (levelData == null) return;

        // Draw grid
        Gizmos.color = new Color(0.8f, 0.8f, 0.8f, 0.4f);
        int w = levelData.GridWidth;
        int h = levelData.GridHeight;
        float cs = levelData.CellSize;

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                float px = (x - (w - 1) * 0.5f) * cs;
                float pz = (y - (h - 1) * 0.5f) * cs;
                Gizmos.DrawWireCube(new Vector3(px, 0, pz), new Vector3(cs * 0.9f, 0.05f, cs * 0.9f));
            }
        }

        // Draw Holes
        foreach (var hole in levelData.Holes)
        {
            Gizmos.color = AntColorPalette.GetColor(hole.HoleColor);
            float hx = (hole.GridCoordinate.x - (w - 1) * 0.5f) * cs;
            float hz = (hole.GridCoordinate.y - (h - 1) * 0.5f) * cs;
            Gizmos.DrawSphere(new Vector3(hx, 0.05f, hz), cs * 0.45f);
        }

        // Draw Queues and Paths
        foreach (var queue in levelData.AntQueues)
        {
            Color c = AntColorPalette.GetColor(queue.AntColor);
            Gizmos.color = c;

            // Draw initial body
            for (int i = 0; i < queue.InitialBodyCoordinates.Count; i++)
            {
                var coord = queue.InitialBodyCoordinates[i];
                float bx = (coord.x - (w - 1) * 0.5f) * cs;
                float bz = (coord.y - (h - 1) * 0.5f) * cs;
                Gizmos.DrawWireSphere(new Vector3(bx, 0.3f, bz), i == 0 ? cs * 0.35f : cs * 0.28f);
            }

            // Draw predefined path line
            for (int p = 0; p < queue.FullPathToHole.Count - 1; p++)
            {
                var p1 = queue.FullPathToHole[p];
                var p2 = queue.FullPathToHole[p + 1];
                Vector3 v1 = new Vector3((p1.x - (w - 1) * 0.5f) * cs, 0.15f, (p1.y - (h - 1) * 0.5f) * cs);
                Vector3 v2 = new Vector3((p2.x - (w - 1) * 0.5f) * cs, 0.15f, (p2.y - (h - 1) * 0.5f) * cs);
                Gizmos.DrawLine(v1, v2);
            }
        }
    }
}
