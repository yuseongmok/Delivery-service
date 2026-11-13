#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
namespace DeliveryService.Mok.NewCity.Editor
{
    internal static class DistrictArt
    {
        public const string Folder="Assets/DeliveryNewCity";
        public static Scene Scene;
        public static Transform Parent;
        static Dictionary<string,string> prefabs;
        static readonly Dictionary<Material,Material> converted=new();
        public static Material Mat(string name,Color color)
        {
            string path=Folder+"/Materials/"+name+".mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m)return m;
            m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,color=color};m.SetFloat("_Smoothness",.12f);AssetDatabase.CreateAsset(m,path);return m;
        }
        public static Material Pattern(string name,Color a,Color b,bool brick)
        {
            var m=Mat(name,a);if(m.mainTexture)return m;
            var t=new Texture2D(128,128,TextureFormat.RGBA32,false){name=name,filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Repeat};
            for(int y=0;y<128;y++)for(int x=0;x<128;x++){
                bool seam=brick?(y%16<1||(x+(y/16%2)*32)%64<1):(x%32<1||y%32<1);
                float n=((x*37+y*17)%13-6)*.002f;
                t.SetPixel(x,y,seam?b:new Color(a.r+n,a.g+n,a.b+n));
            }t.Apply();File.WriteAllBytes(Folder+"/Materials/"+name+".png",t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(Folder+"/Materials/"+name+".png");m.color=Color.white;m.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Materials/"+name+".png");m.mainTextureScale=new Vector2(4,4);return m;
        }
        public static Transform Group(string name){var g=new GameObject(name);g.transform.SetParent(Parent,false);return g.transform;}
        public static GameObject Box(string name,Vector3 p,Vector3 size,Material m,bool solid=true,float yaw=0)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(Parent,false);g.transform.SetPositionAndRotation(p,Quaternion.Euler(0,yaw,0));g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=m;
            if(!solid)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());g.isStatic=true;return g;
        }
        public static Bounds Bounds(GameObject g){var rr=g.GetComponentsInChildren<Renderer>();var b=rr[0].bounds;foreach(var r in rr)b.Encapsulate(r.bounds);return b;}
        public static GameObject Prop(string name,Vector3 ground,float height=0,float yaw=0,bool solid=false)
        {
            prefabs??=AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Synty"}).Select(AssetDatabase.GUIDToAssetPath).GroupBy(Path.GetFileNameWithoutExtension).ToDictionary(g=>g.Key,g=>g.First());
            if(!prefabs.TryGetValue(name,out var path))throw new Exception("Missing prefab: "+name);
            var g=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),Scene);g.transform.SetParent(Parent,true);g.transform.rotation=Quaternion.Euler(0,yaw,0);
            var b=Bounds(g);if(height>0){g.transform.localScale*=height/b.size.y;b=Bounds(g);}g.transform.position+=ground-new Vector3(b.center.x,b.min.y,b.center.z);
            foreach(var c in g.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(c);
            foreach(var mb in g.GetComponentsInChildren<MonoBehaviour>())if(mb)UnityEngine.Object.DestroyImmediate(mb);
            foreach(var r in g.GetComponentsInChildren<Renderer>()){
                var mats=r.sharedMaterials;for(int i=0;i<mats.Length;i++){
                    var src=mats[i];if(!src||src.shader.name.Contains("Universal"))continue;
                    if(!converted.TryGetValue(src,out var dst)){dst=Mat("Synty_"+src.name,src.HasProperty("_Color")?src.color:Color.white);var texture=src.HasProperty("_Albedo_Map")?src.GetTexture("_Albedo_Map"):src.mainTexture;if(texture)dst.mainTexture=texture;EditorUtility.SetDirty(dst);converted[src]=dst;}mats[i]=dst;
                }r.sharedMaterials=mats;
            }
            if(solid){b=Bounds(g);var c=new GameObject("簡易衝突");c.name="외곽 충돌";c.transform.SetParent(Parent,false);c.transform.position=b.center;c.AddComponent<BoxCollider>().size=b.size;c.isStatic=true;}
            foreach(var tr in g.GetComponentsInChildren<Transform>())tr.gameObject.isStatic=true;
            PrefabUtility.RecordPrefabInstancePropertyModifications(g.transform);return g;
        }
        public static void Sign(string value,Vector3 p,float size,float yaw=0,Color? color=null)
        {
            var g=new GameObject(value);g.transform.SetParent(Parent,false);g.transform.SetPositionAndRotation(p,Quaternion.Euler(0,yaw,0));var t=g.AddComponent<TextMesh>();
            t.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/BMJUA_ttf.ttf");t.fontSize=100;t.characterSize=size*.4f;t.anchor=TextAnchor.MiddleCenter;t.alignment=TextAlignment.Center;t.text=value;t.color=color??new Color(1,.88f,.66f);
            // 표지판이 벽 뒤로 비치지 않도록 깊이 검사가 있는 텍스트 재질을 쓴다.
            var mat=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/Sign font.mat");if(!mat){mat=new Material(Shader.Find("Delivery/NewCitySign"));mat.mainTexture=t.font.material.mainTexture;AssetDatabase.CreateAsset(mat,Folder+"/Materials/Sign font.mat");}
            g.GetComponent<Renderer>().sharedMaterial=mat;
        }
        public static void Lamp(Vector3 p,float range,float intensity,Color color){var g=new GameObject("따뜻한 실내 조명");g.transform.SetParent(Parent,false);g.transform.position=p;var l=g.AddComponent<Light>();l.type=LightType.Point;l.range=range;l.intensity=intensity;l.color=color;l.shadows=LightShadows.None;}
    }
}
#endif



