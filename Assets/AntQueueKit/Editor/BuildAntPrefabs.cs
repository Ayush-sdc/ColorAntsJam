using UnityEngine;
using UnityEditor;
using System.IO;

namespace AntQueueKit {
    public static class BuildAntPrefabs {
        const string Root="Assets/AntQueueKit/";
        static readonly string[] Names={"Cyan","Green","Pink","Yellow","Indigo","Brown"};
        static readonly Color[] Colors={new Color(0,.7f,1),new Color(.1f,.65f,.06f),new Color(.92f,.16f,.83f),new Color(1,.73f,0),new Color(.23f,.16f,.85f),new Color(.57f,.28f,.06f)};
        [MenuItem("Tools/Ant Queue/Build Prefabs")]
        public static void Build() {
            Directory.CreateDirectory(Root+"Prefabs");
            Directory.CreateDirectory(Root+"Generated");
            AssetDatabase.Refresh();
            Texture2D atlas=LoadTexture("ants.png");
            Texture2D hole=LoadTexture("hole.png");
            if(!atlas || !hole) { Debug.LogError("Place ants.png and hole.png in AntQueueKit/Art."); return; }
            for(int i=0;i<Names.Length;i++) {
                // Brown shares the yellow ant geometry and is tinted at runtime.
                int cell=i==5?3:i;
                Rect cellRect=new Rect(Mathf.RoundToInt(cell*atlas.width/5f),0,Mathf.RoundToInt((cell+1)*atlas.width/5f)-Mathf.RoundToInt(cell*atlas.width/5f),atlas.height);
                Sprite antSprite=SaveSprite(atlas,TightBounds(atlas,cellRect),Names[i]+"Ant",256);
                GameObject ant=new GameObject("Ant_"+Names[i]);
                var sr=ant.AddComponent<SpriteRenderer>(); sr.sprite=antSprite; sr.sortingOrder=5;
                if(i==5) sr.color=new Color(.57f,.40f,.2f);
                ant.transform.localScale=Vector3.one*.36f;
                var antAsset=PrefabUtility.SaveAsPrefabAsset(ant,Root+"Prefabs/Ant_"+Names[i]+".prefab");
                Object.DestroyImmediate(ant);
                GameObject queue=new GameObject("Queue_"+Names[i]);
                queue.AddComponent<AntQueue>().antPrefab=antAsset;
                PrefabUtility.SaveAsPrefabAsset(queue,Root+"Prefabs/Queue_"+Names[i]+".prefab"); Object.DestroyImmediate(queue);
                GameObject goal=new GameObject("Hole_"+Names[i]);
                sr=goal.AddComponent<SpriteRenderer>();
                sr.sprite=SaveSprite(hole,TightBounds(hole,new Rect(0,0,hole.width,hole.height)),Names[i]+"Hole",512);
                sr.color=Colors[i];
                var collider=goal.AddComponent<BoxCollider2D>(); collider.isTrigger=true; collider.size=sr.sprite.bounds.size*.8f;
                var component=goal.AddComponent<AntHole>(); component.colorId=Names[i]; component.remaining=i==5?7:(i>=3?2:4);
                GameObject label=new GameObject("Count"); label.transform.SetParent(goal.transform,false);
                Vector3 bounds=sr.sprite.bounds.extents;
                label.transform.localPosition=new Vector3(bounds.x-.14f,-bounds.y+.16f,-.1f);
                var tm=label.AddComponent<TextMesh>(); tm.anchor=TextAnchor.MiddleRight; tm.alignment=TextAlignment.Right; tm.fontSize=64; tm.characterSize=.055f; tm.color=Color.white; tm.text=component.remaining.ToString();
                tm.GetComponent<MeshRenderer>().sortingOrder=10; component.label=tm;
                PrefabUtility.SaveAsPrefabAsset(goal,Root+"Prefabs/Hole_"+Names[i]+".prefab"); Object.DestroyImmediate(goal);
            }
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            Debug.Log("Built 18 Ant Queue prefabs in "+Root+"Prefabs");
        }
        static Texture2D LoadTexture(string name) {
            string p=Root+"Art/"+name;
            var imp=AssetImporter.GetAtPath(p) as TextureImporter;
            if(!imp) return null;
            imp.textureType=TextureImporterType.Default; imp.isReadable=true; imp.alphaIsTransparency=true; imp.textureCompression=TextureImporterCompression.Uncompressed; imp.maxTextureSize=4096; imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
        }
        static Rect TightBounds(Texture2D tex,Rect region) {
            int minX=(int)region.xMax,minY=(int)region.yMax,maxX=(int)region.xMin,maxY=(int)region.yMin;
            bool found=false;
            for(int y=(int)region.yMin;y<(int)region.yMax;y++) for(int x=(int)region.xMin;x<(int)region.xMax;x++) {
                if(tex.GetPixel(x,y).a<.05f) continue;
                found=true; minX=Mathf.Min(minX,x);minY=Mathf.Min(minY,y);maxX=Mathf.Max(maxX,x);maxY=Mathf.Max(maxY,y);
            }
            return found?new Rect(minX,minY,maxX-minX+1,maxY-minY+1):region;
        }
        static Sprite SaveSprite(Texture2D tex,Rect rect,string name,float ppu) {
            string p=Root+"Generated/"+name+".asset";
            var old=AssetDatabase.LoadAssetAtPath<Sprite>(p);
            var sprite=Sprite.Create(tex,rect,new Vector2(.5f,.5f),ppu); sprite.name=name;
            if(old) { EditorUtility.CopySerialized(sprite,old); Object.DestroyImmediate(sprite); return old; }
            AssetDatabase.CreateAsset(sprite,p); return sprite;
        }
    }
}
