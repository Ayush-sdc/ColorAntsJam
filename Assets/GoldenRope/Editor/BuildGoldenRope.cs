using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

namespace AntsJam.Ropes
{
    public static class BuildGoldenRope
    {
        const string Root="Assets/GoldenRope";
        const float Pitch=.40f;
        const float Orbit=.068f;
        const float StrandRadius=.063f;
        [MenuItem("Tools/Ant Queue/Build Golden Rope Prefabs")]
        public static void Build()
        {
            foreach(string d in new[]{"Meshes","Materials","Prefabs","Scenes"})Directory.CreateDirectory(Root+"/"+d);
            AssetDatabase.Refresh();
            Shader shader=Shader.Find("AntsJam/Golden Rope");
            if(!shader || ShaderUtil.ShaderHasError(shader))throw new Exception("Rope shader did not compile.");
            string matPath=Root+"/Materials/GoldenRope.mat";
            Material mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if(!mat){mat=new Material(shader);AssetDatabase.CreateAsset(mat,matPath);}
            mat.SetColor("_Color",new Color(.88f,.65f,.32f));mat.SetColor("_ShadowColor",new Color(.36f,.21f,.075f));mat.SetColor("_HighlightColor",new Color(1,.91f,.66f));
            mat.enableInstancing=true;EditorUtility.SetDirty(mat);
            foreach(int length in new[]{2,3,4,5})
            {
                Mesh mesh=CreateMesh(length);string meshPath=Root+"/Meshes/Rope_"+length+"Cells.asset";
                var old=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if(old){EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);mesh=old;}else AssetDatabase.CreateAsset(mesh,meshPath);
                var rope=new GameObject("GoldenRope_"+length+"Cells");
                rope.AddComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=rope.AddComponent<MeshRenderer>();renderer.sharedMaterial=mat;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
                var link=rope.AddComponent<RopeBetweenPoints>();link.meshLength=length;
                PrefabUtility.SaveAsPrefabAsset(rope,Root+"/Prefabs/GoldenRope_"+length+"Cells.prefab");Object.DestroyImmediate(rope);
                Debug.Log("ROPE_MESH length="+length+" triangles="+mesh.triangles.Length/3+" vertices="+mesh.vertexCount+" submeshes="+mesh.subMeshCount);
            }
            Preview();
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();
            foreach(int length in new[]{2,3,4,5})
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/GoldenRope_"+length+"Cells.prefab");
                if(!prefab || prefab.GetComponentsInChildren<Renderer>().Length!=1 || !prefab.GetComponent<MeshFilter>().sharedMesh || !prefab.GetComponent<Renderer>().sharedMaterial)throw new Exception("Invalid rope prefab");
                Mesh m=prefab.GetComponent<MeshFilter>().sharedMesh;
                if(m.subMeshCount!=1 || Math.Abs(m.bounds.size.z-length)>.02f)throw new Exception("Incorrect rope dimensions");
            }
            AssetDatabase.ExportPackage(Root,Path.GetFullPath("../GoldenRope.unitypackage"),ExportPackageOptions.Recurse);
            Debug.Log("GOLDEN_ROPE_SUCCESS: 4 prefabs, shader, material, endpoint component and preview scene exported.");
        }
        static Mesh CreateMesh(float length)
        {
            int rings=Mathf.CeilToInt(length/Pitch*24),sides=10;
            var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var colors=new List<Color>();var tris=new List<int>();
            for(int strand=0;strand<3;strand++)
            {
                int offset=vertices.Count;
                float phase=strand*Mathf.PI*2/3;
                Color strandTint=Color.white*(strand==0?1f:strand==1?.97f:.92f);strandTint.a=1;
                for(int i=0;i<=rings;i++)
                {
                    float z=-length*.5f+length*i/rings;
                    float angle=z/Pitch*Mathf.PI*2+phase;
                    Vector3 radial=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0);
                    Vector3 center=radial*Orbit+Vector3.forward*z;
                    // Rings lie in XY for clean perpendicular cut ends and exact bounds.
                    for(int j=0;j<=sides;j++)
                    {
                        float a=j*Mathf.PI*2/sides;
                        Vector3 n=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);
                        vertices.Add(center+n*StrandRadius);normals.Add(n);uv.Add(new Vector2((float)j/sides,(z+length*.5f)/Pitch));colors.Add(strandTint);
                        if(i<rings && j<sides)
                        {
                            int k=offset+i*(sides+1)+j;
                            tris.Add(k);tris.Add(k+1);tris.Add(k+sides+1);
                            tris.Add(k+1);tris.Add(k+sides+2);tris.Add(k+sides+1);
                        }
                    }
                }
                for(int end=0;end<2;end++)
                {
                    float z=(end==0?-.5f:.5f)*length;float angle=z/Pitch*Mathf.PI*2+phase;
                    Vector3 c=new Vector3(Mathf.Cos(angle)*Orbit,Mathf.Sin(angle)*Orbit,z);
                    int middle=vertices.Count;vertices.Add(c);normals.Add(end==0?Vector3.back:Vector3.forward);uv.Add(Vector2.zero);colors.Add(strandTint*.87f);
                    for(int j=0;j<=sides;j++){float a=j*Mathf.PI*2/sides;vertices.Add(c+new Vector3(Mathf.Cos(a),Mathf.Sin(a),0)*StrandRadius);normals.Add(end==0?Vector3.back:Vector3.forward);uv.Add(Vector2.one*.5f);colors.Add(strandTint*.87f);}
                    for(int j=0;j<sides;j++){tris.Add(middle);tris.Add(middle+1+(end==0?j+1:j));tris.Add(middle+1+(end==0?j:j+1));}
                }
            }
            Mesh mesh=new Mesh{name="GoldenRope_"+length+"Cells"};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetColors(colors);mesh.SetTriangles(tris,0);mesh.RecalculateBounds();return mesh;
        }
        static void Preview()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=Color.gray;
            var camera=new GameObject("Camera").AddComponent<Camera>();camera.tag="MainCamera";camera.orthographic=true;camera.orthographicSize=3.0f;camera.transform.position=new Vector3(0,8,0);camera.transform.rotation=Quaternion.Euler(90,0,0);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.69f,.68f,.64f);
            int i=0;
            foreach(int length in new[]{2,3,4,5}){var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/GoldenRope_"+length+"Cells.prefab"));go.transform.position=new Vector3((i++-1.5f)*.95f,0,0);}
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),Root+"/Scenes/RopeShowcase.unity");
            var rt=new RenderTexture(1200,1200,24);rt.antiAliasing=8;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            var tex=new Texture2D(1200,1200,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1200,1200),0,0);tex.Apply();File.WriteAllBytes(Path.GetFullPath("../GoldenRope-preview.png"),tex.EncodeToPNG());
            camera.targetTexture=null;RenderTexture.active=null;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(tex);
        }
    }
}
