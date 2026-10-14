#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeliveryService.Mok.Editor
{
    // Editor-only authoring tool. Does not save or overwrite the gameplay scene.
    public static class PizzaWorkbenchBuilder
    {
        const string Folder = "Assets/Scripts/Mok/Art/PizzaWorkbench";
        static readonly string Output = Environment.GetEnvironmentVariable("MOK_WORKBENCH_OUTPUT");
        static readonly List<Vector3> vertices = new List<Vector3>();
        static readonly List<Vector2> uvs = new List<Vector2>();
        static List<int>[] indices;
        [Serializable] public class Report {
            public string meshGuid; public string[] materialGuids;
            public Vector3 colliderCenter, colliderSize, anchor, worldScale;
            public int triangles; public string shader; public bool sceneSaved;
        }
        static void Face(int material, params Vector3[] points) {
            int start = vertices.Count;
            vertices.AddRange(points);
            for(int i=0;i<points.Length;i++) uvs.Add(new Vector2(points[i].x, points[i].z));
            for(int i=1;i<points.Length-1;i++) {indices[material].Add(start); indices[material].Add(start+i); indices[material].Add(start+i+1);}
        }
        static void Box(int mat, Vector3 c, Vector3 s, float bevel=0) {
            float x=s.x/2, z=s.z/2, y=s.y/2;
            float b=Mathf.Min(bevel, Mathf.Min(x,z)*0.45f);
            var ring = new [] {new Vector2(-x+b,-z),new Vector2(x-b,-z),new Vector2(x,-z+b),new Vector2(x,z-b),new Vector2(x-b,z),new Vector2(-x+b,z),new Vector2(-x,z-b),new Vector2(-x,-z+b)};
            var bottom=ring.Select(p=>c+new Vector3(p.x,-y,p.y)).ToArray();
            var top=ring.Select(p=>c+new Vector3(p.x,y,p.y)).ToArray();
            Face(mat,bottom); Face(mat,top.Reverse().ToArray());
            for(int i=0;i<8;i++) {int n=(i+1)%8; Face(mat,bottom[i],top[i],top[n],bottom[n]);}
        }
        static Material MakeMaterial(string name, string hex, float metallic, float smoothness) {
            var m=new Material(Shader.Find("Universal Render Pipeline/Lit")); m.name=name;
            ColorUtility.TryParseHtmlString(hex,out var color);
            m.SetColor("_BaseColor",color); m.SetFloat("_Metallic",metallic); m.SetFloat("_Smoothness",smoothness);
            m.SetColor("_EmissionColor",Color.black); m.DisableKeyword("_EMISSION");
            AssetDatabase.CreateAsset(m,Folder+"/"+name+".mat"); return m;
        }
        public static void Build() {
            try {
                Directory.CreateDirectory(Output); Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
                if(File.Exists(Folder+"/PizzaWorkbench.mesh.asset")) throw new Exception("Prototype already exists. Preserve edits; use a new version folder.");
                var materials=new [] {
                    MakeMaterial("WarmStone", "#BBB6A4",0,0.16f),
                    MakeMaterial("PaintedSteel", "#405355",0.12f,0.22f),
                    MakeMaterial("BrushedSteel", "#7E8C8B",0.28f,0.3f),
                    MakeMaterial("Rubber", "#242E32",0,0.12f),
                    MakeMaterial("OxideRed", "#823E32",0,0.19f)
                };
                vertices.Clear();uvs.Clear();indices=Enumerable.Range(0,materials.Length).Select(_=>new List<int>()).ToArray();
                // Dimensions preserve original countertop and PizzaPoint height.
                Box(2,new Vector3(0,.765f,0),new Vector3(1.26f,.05f,1.06f),.035f);
                Box(0,new Vector3(0,.8105f,0),new Vector3(1.24f,.041f,1.04f),.035f);
                Box(1,new Vector3(0,.678f,0),new Vector3(1.12f,.125f,.91f),.015f);
                // Two shallow drawer faces, mirrored so either approach works.
                foreach(float front in new[]{-1f,1f}) {
                    Box(4,new Vector3(0,.728f,front*.462f),new Vector3(1.09f,.015f,.015f),.003f);
                    foreach(float side in new[]{-1f,1f}) {
                        Box(2,new Vector3(side*.276f,.676f,front*.467f),new Vector3(.53f,.072f,.018f),.006f);
                        Box(3,new Vector3(side*.276f,.684f,front*.484f),new Vector3(.17f,.016f,.025f),.004f);
                    }
                }
                foreach(float x in new[]{-.51f,.51f}) foreach(float z in new[]{-.4f,.4f}) {
                    Box(1,new Vector3(x,.333f,z),new Vector3(.065f,.58f,.065f),.009f);
                    Box(3,new Vector3(x,.027f,z),new Vector3(.082f,.054f,.082f),.012f);
                    Box(2,new Vector3(x,.072f,z),new Vector3(.07f,.026f,.07f),.008f);
                }
                Box(1,new Vector3(0,.184f,0),new Vector3(1.08f,.04f,.87f),.012f);
                foreach(float z in new[]{-.28f,0,.28f}) Box(2,new Vector3(0,.211f,z),new Vector3(1.01f,.014f,.24f),.009f);
                var mesh=new Mesh{name="PizzaWorkbench_LowPoly"}; mesh.SetVertices(vertices);mesh.SetUVs(0,uvs);mesh.subMeshCount=materials.Length;
                for(int i=0;i<indices.Length;i++)mesh.SetTriangles(indices[i],i);
                mesh.RecalculateNormals();mesh.RecalculateBounds();
                AssetDatabase.CreateAsset(mesh,Folder+"/PizzaWorkbench.mesh.asset");
                var model=new GameObject("Mok Pizza Workbench");model.AddComponent<MeshFilter>().sharedMesh=mesh;
                var mr=model.AddComponent<MeshRenderer>();mr.sharedMaterials=materials;mr.shadowCastingMode=ShadowCastingMode.On;mr.receiveShadows=true;
                var collider=model.AddComponent<BoxCollider>();collider.center=new Vector3(0,.4155f,0);collider.size=new Vector3(1.26f,.831f,1.06f);
                PrefabUtility.SaveAsPrefabAsset(model,Folder+"/PizzaWorkbenchVisual.prefab");UnityEngine.Object.DestroyImmediate(model);AssetDatabase.SaveAssets();
                var scene=EditorSceneManager.OpenScene("Assets/Scenes/JJinCity.unity");
                var maker=UnityEngine.Object.FindObjectsByType<PizzaMaker>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single();
                var point=new SerializedObject(maker).FindProperty("doughPlacePoint").objectReferenceValue as Transform;
                var tr=maker.transform;var oldBounds=maker.GetComponent<MeshRenderer>().bounds;
                var report=new Report{meshGuid=AssetDatabase.AssetPathToGUID(Folder+"/PizzaWorkbench.mesh.asset"),materialGuids=materials.Select(m=>AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(m))).ToArray(),colliderCenter=collider==null?new Vector3(0,.4155f,0):Vector3.zero,colliderSize=new Vector3(1.26f,.831f,1.06f),anchor=point.position,worldScale=tr.lossyScale,triangles=indices.Sum(i=>i.Count)/3,shader=materials[0].shader.name,sceneSaved=false};
                report.colliderCenter=new Vector3(0,.4155f,0);
                File.WriteAllText(Output+"/integration.json",JsonUtility.ToJson(report,true));
                File.WriteAllText(Output+"/dimensions.txt",$"Original world bounds: {oldBounds}\nTransform world scale: {tr.lossyScale}\nPizzaPoint local: {point.localPosition}\nTriangles: {report.triangles}");
                // In-memory visual substitution only. The narrowly scoped scene patch is applied separately.
                maker.GetComponent<MeshFilter>().sharedMesh=mesh;maker.GetComponent<MeshRenderer>().sharedMaterials=materials;
                var box=maker.GetComponent<BoxCollider>();box.center=report.colliderCenter;box.size=report.colliderSize;
                Physics.SyncTransforms();
                if(point!=new SerializedObject(maker).FindProperty("doughPlacePoint").objectReferenceValue)throw new Exception("Anchor changed");
                var day=UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();
                var player=UnityEngine.Object.FindFirstObjectByType<PlayerMove>();
                Vector3 towardPlayer=player==null?tr.forward:(player.transform.position-tr.position);towardPlayer.y=0;towardPlayer.Normalize();
                float scale=Mathf.Max(Mathf.Abs(tr.lossyScale.x),Mathf.Abs(tr.lossyScale.z));
                var target=tr.TransformPoint(new Vector3(0,.44f,0));
                Vector3 view=target+towardPlayer*(2.3f*scale)+tr.right*(.65f*scale)+Vector3.up*(1.25f*scale);
                var cam=CreateCamera(view,target);
                foreach(float time in new[]{6f,12f}) {
                    day.currentTime=time;typeof(DayNightCycle).GetMethod("UpdateLighting",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(day,null);
                    Capture(cam,Output+(time==6?"/workbench-dawn.png":"/workbench-noon.png"));
                }
                UnityEngine.Object.DestroyImmediate(cam.gameObject);
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.34f,.39f,.43f);RenderSettings.ambientIntensity=1;
                var studio=new GameObject("Workbench");studio.AddComponent<MeshFilter>().sharedMesh=mesh;studio.AddComponent<MeshRenderer>().sharedMaterials=materials;
                var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(0,-.05f,0);floor.transform.localScale=new Vector3(200,.1f,200);
                var floorMat=new Material(Shader.Find("Universal Render Pipeline/Lit"));floorMat.SetColor("_BaseColor",new Color(.12f,.16f,.19f));floor.GetComponent<Renderer>().sharedMaterial=floorMat;
                var light=new GameObject("Key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=2.5f;light.color=new Color(1,.93f,.82f);light.transform.rotation=Quaternion.Euler(48,-35,0);light.shadows=LightShadows.Soft;
                var fill=new GameObject("Fill").AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.55f;fill.color=new Color(.62f,.78f,1);fill.transform.rotation=Quaternion.Euler(32,145,0);
                cam=CreateCamera(new Vector3(1.75f,1.6f,-2.1f),new Vector3(0,.43f,0));Capture(cam,Output+"/workbench-preview.png");
                File.WriteAllText(Output+"/build-success.txt","Assets generated; gameplay scene was not saved.\n");
                EditorApplication.Exit(0);
            }catch(Exception e){Debug.LogException(e);File.WriteAllText(Output+"/build-error.txt",e.ToString());EditorApplication.Exit(1);}
        }
        static Camera CreateCamera(Vector3 pos,Vector3 target) {
            var cam=new GameObject("Mok Preview Camera").AddComponent<Camera>();cam.transform.position=pos;cam.transform.LookAt(target);cam.fieldOfView=42;cam.nearClipPlane=.03f;cam.farClipPlane=350;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.08f,.12f,.16f);cam.allowHDR=true;return cam;
        }
        static void Capture(Camera cam,string path) {
            var rt=new RenderTexture(1400,1000,24,RenderTextureFormat.ARGB32);rt.antiAliasing=4;cam.targetTexture=rt;
            cam.Render();RenderTexture.active=rt;
            var tex=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());
            cam.targetTexture=null;RenderTexture.active=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);
        }
    }
}
#endif
