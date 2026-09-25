#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class BuildColorSlitherJamAllPrefabsNoSlither
{
    const string Root = "Assets/ColorSlitherJamAllPrefabs";
    [Serializable] class ComponentInfo { public long pathID; public int classID; public long meshPathID; public int meshFileID; public long[] materialPathIDs; public long spritePathID; }
    [Serializable] class Node { public string sourceFile; public long sourceGameObjectPathID; public long sourceTransformPathID; public string name; public bool active=true; public bool isRectTransform; public float[] localPosition; public float[] localRotationQuaternion; public float[] localScale; public float[] anchorMin; public float[] anchorMax; public float[] anchoredPosition; public float[] sizeDelta; public float[] pivot; public ComponentInfo[] components; public Node[] children; }
    [Serializable] class MatSpec { public long pathID; public string name; public float[] baseColor; public float metallic; public float smoothness; public float[] outlineColor; public float outlineWidth; }
    [Serializable] class MatList { public MatSpec[] materials; }

    [MenuItem("Tools/Color Slither Jam/Build ALL Extracted Prefabs (No Slither)")]
    public static void BuildAll()
    {
        Directory.CreateDirectory(Root + "/Materials/Generated");
        Directory.CreateDirectory(Root + "/Prefabs/Reconstructed/GamePrefabs");
        Directory.CreateDirectory(Root + "/Prefabs/Reconstructed/ResourcePrefabs");
        BuildMaterials();
        BuildFolder("Hierarchy/GamePrefabs", "Prefabs/Reconstructed/GamePrefabs", false);
        BuildFolder("Hierarchy/ResourcePrefabs", "Prefabs/Reconstructed/ResourcePrefabs", true);
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        Debug.Log("[CSJ Extract] Built all reconstructable prefab roots except Head/Mid/Tail Snake. 3D MeshFilter/MeshRenderer dependencies are restored when parseable. Stripped UI/MonoBehaviour/particle logic remains a hierarchy/layout skeleton; see README.");
    }
    static void BuildMaterials()
    {
        var ta=AssetDatabase.LoadAssetAtPath<TextAsset>(Root+"/Materials/material_specs.json"); if(!ta)return;
        var list=JsonUtility.FromJson<MatList>(ta.text); if(list?.materials==null)return;
        Shader sh=Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        foreach(var s in list.materials){
            string p=$"{Root}/Materials/Generated/Mat_{s.pathID}_{Clean(s.name)}.mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(p); if(!m){m=new Material(sh);AssetDatabase.CreateAsset(m,p);}
            m.name=s.name;
            if(s.baseColor?.Length>=4){var c=new Color(s.baseColor[0],s.baseColor[1],s.baseColor[2],s.baseColor[3]); if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",c); if(m.HasProperty("_Color"))m.SetColor("_Color",c);}
            if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",s.metallic); if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",s.smoothness); EditorUtility.SetDirty(m);
        }
    }
    static void BuildFolder(string rel,string outRel,bool hud)
    {
        string abs=Path.Combine(Application.dataPath,"ColorSlitherJamAllPrefabs",rel.Replace('/',Path.DirectorySeparatorChar)); if(!Directory.Exists(abs))return;
        foreach(string file in Directory.GetFiles(abs,"*.json")){
            string ap="Assets"+file.Substring(Application.dataPath.Length).Replace('\\','/'); var ta=AssetDatabase.LoadAssetAtPath<TextAsset>(ap); if(!ta)continue;
            Node n=JsonUtility.FromJson<Node>(ta.text); if(n==null)continue; GameObject go=BuildNode(n,hud);
            string outp=$"{Root}/{outRel}/{Path.GetFileNameWithoutExtension(file)}.prefab"; PrefabUtility.SaveAsPrefabAsset(go,outp); UnityEngine.Object.DestroyImmediate(go);
        }
    }
    static GameObject BuildNode(Node n,bool hud)
    {
        GameObject go=(hud||n.isRectTransform)?new GameObject(n.name,typeof(RectTransform)):new GameObject(n.name); go.SetActive(n.active); Transform t=go.transform;
        if(n.localPosition?.Length>=3)t.localPosition=new Vector3(n.localPosition[0],n.localPosition[1],n.localPosition[2]);
        if(n.localRotationQuaternion?.Length>=4)t.localRotation=new Quaternion(n.localRotationQuaternion[0],n.localRotationQuaternion[1],n.localRotationQuaternion[2],n.localRotationQuaternion[3]);
        if(n.localScale?.Length>=3)t.localScale=new Vector3(n.localScale[0],n.localScale[1],n.localScale[2]);
        if(t is RectTransform rt){if(n.anchorMin?.Length>=2)rt.anchorMin=new Vector2(n.anchorMin[0],n.anchorMin[1]);if(n.anchorMax?.Length>=2)rt.anchorMax=new Vector2(n.anchorMax[0],n.anchorMax[1]);if(n.anchoredPosition?.Length>=2)rt.anchoredPosition=new Vector2(n.anchoredPosition[0],n.anchoredPosition[1]);if(n.sizeDelta?.Length>=2)rt.sizeDelta=new Vector2(n.sizeDelta[0],n.sizeDelta[1]);if(n.pivot?.Length>=2)rt.pivot=new Vector2(n.pivot[0],n.pivot[1]);}
        if(!hud && n.components!=null){
            ComponentInfo mfInfo=null,mrInfo=null,srInfo=null; foreach(var c in n.components){if(c.classID==33&&c.meshPathID!=0)mfInfo=c;if(c.classID==23)mrInfo=c;if(c.classID==212)srInfo=c;}
            if(mfInfo!=null){var mf=go.AddComponent<MeshFilter>();mf.sharedMesh=FindMesh(mfInfo.meshPathID,mfInfo.meshFileID);var mr=go.AddComponent<MeshRenderer>();if(mrInfo?.materialPathIDs!=null)mr.sharedMaterials=FindMaterials(mrInfo.materialPathIDs);}
            else if(srInfo!=null){var sr=go.AddComponent<SpriteRenderer>();if(srInfo.materialPathIDs!=null){var mm=FindMaterials(srInfo.materialPathIDs);if(mm.Length>0)sr.sharedMaterial=mm[0];} /* source Sprite pixels are preserved separately; ASTC cannot be losslessly auto-wired here */ }
        }
        if(n.children!=null)foreach(var c in n.children){var ch=BuildNode(c,hud);ch.transform.SetParent(t,false);} return go;
    }
    static Material[] FindMaterials(long[] ids){var list=new List<Material>();foreach(var id in ids){var g=AssetDatabase.FindAssets($"Mat_{id}_",new[]{Root+"/Materials/Generated"});if(g.Length>0)list.Add(AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g[0])));}return list.ToArray();}
    static Mesh FindMesh(long id,int fileID){string folder=fileID==3?Root+"/Models/Builtin":Root+"/Models/Game";var guids=AssetDatabase.FindAssets($"Mesh_{id}_",new[]{folder});if(guids.Length==0)return null;string p=AssetDatabase.GUIDToAssetPath(guids[0]);foreach(var o in AssetDatabase.LoadAllAssetsAtPath(p))if(o is Mesh m)return m;return null;}
    static string Clean(string s){if(string.IsNullOrEmpty(s))return "Material";foreach(char c in Path.GetInvalidFileNameChars())s=s.Replace(c,'_');return s.Replace(' ','_');}
}
#endif
