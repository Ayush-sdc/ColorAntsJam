using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SlitherAnts.Editor
{
    public static class LeaderHeadLookMeshSplitter
    {
        public const string MeshesDir = "Assets/ChubbyAntColors/Meshes";
        public const string PrefabsDir = "Assets/ChubbyAntColors/Prefabs";

        public const string HeadBodyMeshPath = MeshesDir + "/Chubby_Head_Body.asset";
        public const string TorsoBodyMeshPath = MeshesDir + "/Chubby_Torso_Body.asset";
        public const string HeadAccentMeshPath = MeshesDir + "/Chubby_Head_Accent.asset";
        public const string TorsoAccentMeshPath = MeshesDir + "/Chubby_Torso_Accent.asset";

        public static readonly Vector3 NeckPivotLocalPosition = new Vector3(0f, 0.50f, 0.15f);
        public static readonly Vector3 HeadChildrenLocalPosition = -NeckPivotLocalPosition; // (0f, -0.50f, -0.15f)


        [MenuItem("Tools/Slither Ants Jam/Head Look/Split Chubby Ant Meshes and Setup Prefabs")]
        public static void SplitMeshesAndSetupPrefabs()
        {
            Debug.Log("[LeaderHeadLookMeshSplitter] Starting mesh splitting and prefab setup...");

            // 1. Split body.asset
            Mesh bodySrc = AssetDatabase.LoadAssetAtPath<Mesh>(MeshesDir + "/body.asset");
            if (bodySrc == null)
            {
                Debug.LogError("[LeaderHeadLookMeshSplitter] Could not load " + MeshesDir + "/body.asset");
                return;
            }
            SplitMeshByZ(bodySrc, 0.30f, out Mesh headBodyMesh, out Mesh torsoBodyMesh, "Chubby_Head_Body", "Chubby_Torso_Body");
            SaveOrCreateAsset(headBodyMesh, HeadBodyMeshPath);
            SaveOrCreateAsset(torsoBodyMesh, TorsoBodyMeshPath);

            // 2. Split accent.asset
            Mesh accentSrc = AssetDatabase.LoadAssetAtPath<Mesh>(MeshesDir + "/accent.asset");
            if (accentSrc == null)
            {
                Debug.LogError("[LeaderHeadLookMeshSplitter] Could not load " + MeshesDir + "/accent.asset");
                return;
            }
            SplitMeshByZ(accentSrc, 0.30f, out Mesh headAccentMesh, out Mesh torsoAccentMesh, "Chubby_Head_Accent", "Chubby_Torso_Accent");
            SaveOrCreateAsset(headAccentMesh, HeadAccentMeshPath);
            SaveOrCreateAsset(torsoAccentMesh, TorsoAccentMeshPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 3. Configure all ChubbyAnt prefabs
            ConfigureAllPrefabs();

            Debug.Log("[LeaderHeadLookMeshSplitter] Successfully split meshes and configured ChubbyAnt prefabs!");
        }

        public static void SplitMeshByZ(
            Mesh source,
            float zThreshold,
            out Mesh headMesh,
            out Mesh torsoMesh,
            string headMeshName,
            string torsoMeshName)
        {
            Vector3[] verts = source.vertices;
            Vector3[] normals = source.normals;
            Vector2[] uvs = source.uv;
            Color32[] colors = source.colors32;
            int[] triangles = source.triangles;

            int vertCount = verts.Length;
            List<HashSet<int>> adj = new List<HashSet<int>>(vertCount);
            for (int i = 0; i < vertCount; i++) adj.Add(new HashSet<int>());

            for (int t = 0; t < triangles.Length; t += 3)
            {
                int i0 = triangles[t];
                int i1 = triangles[t + 1];
                int i2 = triangles[t + 2];
                adj[i0].Add(i1); adj[i0].Add(i2);
                adj[i1].Add(i0); adj[i1].Add(i2);
                adj[i2].Add(i0); adj[i2].Add(i1);
            }

            bool[] visited = new bool[vertCount];
            HashSet<int> headVertIndices = new HashSet<int>();
            HashSet<int> torsoVertIndices = new HashSet<int>();

            for (int i = 0; i < vertCount; i++)
            {
                if (visited[i]) continue;
                List<int> comp = new List<int>();
                Queue<int> q = new Queue<int>();
                q.Enqueue(i);
                visited[i] = true;

                while (q.Count > 0)
                {
                    int curr = q.Dequeue();
                    comp.Add(curr);
                    foreach (int n in adj[curr])
                    {
                        if (!visited[n])
                        {
                            visited[n] = true;
                            q.Enqueue(n);
                        }
                    }
                }

                float sumZ = 0f;
                for (int c = 0; c < comp.Count; c++) sumZ += verts[comp[c]].z;
                float centerZ = sumZ / comp.Count;

                if (centerZ > zThreshold)
                {
                    for (int c = 0; c < comp.Count; c++) headVertIndices.Add(comp[c]);
                }
                else
                {
                    for (int c = 0; c < comp.Count; c++) torsoVertIndices.Add(comp[c]);
                }
            }

            headMesh = BuildSubmesh(source, verts, normals, uvs, colors, triangles, headVertIndices, headMeshName);
            torsoMesh = BuildSubmesh(source, verts, normals, uvs, colors, triangles, torsoVertIndices, torsoMeshName);
        }

        private static Mesh BuildSubmesh(
            Mesh source,
            Vector3[] verts,
            Vector3[] normals,
            Vector2[] uvs,
            Color32[] colors,
            int[] triangles,
            HashSet<int> targetVertIndices,
            string meshName)
        {
            Mesh submesh = new Mesh { name = meshName };

            Dictionary<int, int> oldToNew = new Dictionary<int, int>();
            List<Vector3> newVerts = new List<Vector3>();
            List<Vector3> newNormals = new List<Vector3>();
            List<Vector2> newUvs = new List<Vector2>();
            List<Color32> newColors = new List<Color32>();

            foreach (int oldIdx in targetVertIndices)
            {
                oldToNew[oldIdx] = newVerts.Count;
                newVerts.Add(verts[oldIdx]);
                if (normals != null && normals.Length > oldIdx) newNormals.Add(normals[oldIdx]);
                if (uvs != null && uvs.Length > oldIdx) newUvs.Add(uvs[oldIdx]);
                if (colors != null && colors.Length > oldIdx) newColors.Add(colors[oldIdx]);
            }

            List<int> newTris = new List<int>();
            for (int t = 0; t < triangles.Length; t += 3)
            {
                int i0 = triangles[t];
                int i1 = triangles[t + 1];
                int i2 = triangles[t + 2];

                if (oldToNew.TryGetValue(i0, out int n0) &&
                    oldToNew.TryGetValue(i1, out int n1) &&
                    oldToNew.TryGetValue(i2, out int n2))
                {
                    newTris.Add(n0);
                    newTris.Add(n1);
                    newTris.Add(n2);
                }
            }

            submesh.SetVertices(newVerts);
            if (newNormals.Count == newVerts.Count) submesh.SetNormals(newNormals);
            if (newUvs.Count == newVerts.Count) submesh.SetUVs(0, newUvs);
            if (newColors.Count == newVerts.Count) submesh.SetColors(newColors);
            submesh.SetTriangles(newTris, 0);

            submesh.RecalculateBounds();
            return submesh;
        }

        private static void SaveOrCreateAsset(Mesh mesh, string assetPath)
        {
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
            if (existing != null)
            {
                existing.Clear();
                EditorUtility.CopySerialized(mesh, existing);
                EditorUtility.SetDirty(existing);
            }
            else
            {
                AssetDatabase.CreateAsset(mesh, assetPath);
            }
        }

        public static void ConfigureAllPrefabs()
        {
            Mesh headBodyMesh = AssetDatabase.LoadAssetAtPath<Mesh>(HeadBodyMeshPath);
            Mesh torsoBodyMesh = AssetDatabase.LoadAssetAtPath<Mesh>(TorsoBodyMeshPath);
            Mesh headAccentMesh = AssetDatabase.LoadAssetAtPath<Mesh>(HeadAccentMeshPath);
            Mesh torsoAccentMesh = AssetDatabase.LoadAssetAtPath<Mesh>(TorsoAccentMeshPath);

            if (headBodyMesh == null || torsoBodyMesh == null || headAccentMesh == null || torsoAccentMesh == null)
            {
                Debug.LogError("[LeaderHeadLookMeshSplitter] Split meshes are missing!");
                return;
            }

            string[] prefabFiles = Directory.GetFiles(PrefabsDir, "ChubbyAnt_*.prefab");
            foreach (string path in prefabFiles)
            {
                ConfigurePrefab(path, headBodyMesh, torsoBodyMesh, headAccentMesh, torsoAccentMesh);
            }
        }

        private static void ConfigurePrefab(
            string prefabPath,
            Mesh headBodyMesh,
            Mesh torsoBodyMesh,
            Mesh headAccentMesh,
            Mesh torsoAccentMesh)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                Transform bodyT = root.transform.Find("body");
                Transform accentT = root.transform.Find("accent");

                if (bodyT == null || accentT == null)
                {
                    Debug.LogWarning($"[LeaderHeadLookMeshSplitter] Skipping {prefabPath}: missing body or accent");
                    return;
                }

                Material bodyMat = bodyT.GetComponent<MeshRenderer>()?.sharedMaterial;
                Material limbMat = accentT.GetComponent<MeshRenderer>()?.sharedMaterial;

                // 1. Update root body to torso mesh
                MeshFilter bodyMf = bodyT.GetComponent<MeshFilter>();
                if (bodyMf != null) bodyMf.sharedMesh = torsoBodyMesh;

                // 2. Update root accent to torso mesh
                MeshFilter accentMf = accentT.GetComponent<MeshFilter>();
                if (accentMf != null) accentMf.sharedMesh = torsoAccentMesh;

                // 3. Find or create HeadLookPivot
                Transform pivotT = root.transform.Find("HeadLookPivot");
                if (pivotT == null)
                {
                    GameObject pivotObj = new GameObject("HeadLookPivot");
                    pivotT = pivotObj.transform;
                    pivotT.SetParent(root.transform, false);
                }
                pivotT.localPosition = NeckPivotLocalPosition;
                pivotT.localRotation = Quaternion.identity;
                pivotT.localScale = Vector3.one;

                // 4. Create/update head in HeadLookPivot
                Transform headT = pivotT.Find("head");
                if (headT == null)
                {
                    GameObject headObj = new GameObject("head");
                    headT = headObj.transform;
                    headT.SetParent(pivotT, false);
                }
                headT.localPosition = HeadChildrenLocalPosition;
                headT.localRotation = Quaternion.identity;
                MeshFilter headMf = headT.GetComponent<MeshFilter>();
                if (headMf == null) headMf = headT.gameObject.AddComponent<MeshFilter>();
                headMf.sharedMesh = headBodyMesh;

                MeshRenderer headMr = headT.GetComponent<MeshRenderer>();
                if (headMr == null) headMr = headT.gameObject.AddComponent<MeshRenderer>();
                headMr.sharedMaterial = bodyMat;

                // 5. Create/update head_accent in HeadLookPivot
                Transform headAccentT = pivotT.Find("head_accent");
                if (headAccentT == null)
                {
                    GameObject headAccentObj = new GameObject("head_accent");
                    headAccentT = headAccentObj.transform;
                    headAccentT.SetParent(pivotT, false);
                }
                headAccentT.localPosition = HeadChildrenLocalPosition;
                headAccentT.localRotation = Quaternion.identity;
                headAccentT.localScale = Vector3.one;

                MeshFilter headAccentMf = headAccentT.GetComponent<MeshFilter>();
                if (headAccentMf == null) headAccentMf = headAccentT.gameObject.AddComponent<MeshFilter>();
                headAccentMf.sharedMesh = headAccentMesh;

                MeshRenderer headAccentMr = headAccentT.GetComponent<MeshRenderer>();
                if (headAccentMr == null) headAccentMr = headAccentT.gameObject.AddComponent<MeshRenderer>();
                headAccentMr.sharedMaterial = limbMat;

                // 6. Move facial children into HeadLookPivot
                string[] faceParts = new string[] { "eyes", "pupils", "iris", "glints", "blush" };
                foreach (string part in faceParts)
                {
                    Transform partT = root.transform.Find(part);
                    if (partT != null)
                    {
                        partT.SetParent(pivotT, false);
                        partT.localPosition = HeadChildrenLocalPosition;
                        partT.localRotation = Quaternion.identity;
                        partT.localScale = Vector3.one;
                    }
                    else
                    {
                        // Check if already under pivot
                        Transform underPivot = pivotT.Find(part);
                        if (underPivot != null)
                        {
                            underPivot.localPosition = HeadChildrenLocalPosition;
                            underPivot.localRotation = Quaternion.identity;
                            underPivot.localScale = Vector3.one;
                        }
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Debug.Log($"[LeaderHeadLookMeshSplitter] Configured prefab: {Path.GetFileName(prefabPath)}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
