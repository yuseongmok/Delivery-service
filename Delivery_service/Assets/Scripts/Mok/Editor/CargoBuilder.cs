#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Collections.Generic;
using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad] public static class CargoBuilder {
const string Folder="Assets/Scripts/Mok/Resources/MokCargoArt";
static readonly List<Vector3> vertices=new List<Vector3>();static List<int>[] triangles;static Material[] palette;
static CargoBuilder(){EditorApplication.update+=Poll;}
static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/Mok-cargo-build"))return;File.Delete("Temp/Mok-cargo-build");try{Build();File.WriteAllText("Temp/Mok-cargo-build.txt","PASS: Cargo installed and scene saved.");}catch(Exception e){File.WriteAllText("Temp/Mok-cargo-build.txt",e.ToString());Debug.LogException(e);}}
        static Material Mat(string name, string hex, float gloss = .15f)
        {
            string path = Folder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
            ColorUtility.TryParseHtmlString(hex, out var color);
            material.SetColor("_BaseColor", color); material.SetFloat("_Metallic", 0); material.SetFloat("_Smoothness", gloss);
            material.SetColor("_EmissionColor", Color.black); material.DisableKeyword("_EMISSION");
            return material;
        }
        static void Begin() { vertices.Clear(); triangles = Enumerable.Range(0, palette.Length).Select(_ => new List<int>()).ToArray(); }
        static void Face(int mat, params Vector3[] p)
        {
            int start = vertices.Count; vertices.AddRange(p);
            for (int i = 1; i < p.Length - 1; i++) { triangles[mat].Add(start); triangles[mat].Add(start + i); triangles[mat].Add(start + i + 1); }
        }
        static Vector3 Ring(float r, float y, float angle) => new Vector3(Mathf.Cos(angle) * r, y, Mathf.Sin(angle) * r);
        static void Disc(int mat, Vector3 center, float radius, float aspect = 1, int sides = 32, float noise = 0)
        {
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2 / sides, b = (i + 1) * Mathf.PI * 2 / sides;
                Vector3 p = Ring(radius * (1 + noise * Mathf.Sin(i * 2.3f)), 0, a), q = Ring(radius * (1 + noise * Mathf.Sin((i + 1) * 2.3f)), 0, b);
                p.z *= aspect; q.z *= aspect; Face(mat, center, center + q, center + p);
            }
        }
        static void Box(int mat, Vector3 center, Vector3 size, float yaw = 0)
        {
            Vector3 s = size / 2; Quaternion rotation = Quaternion.Euler(0, yaw, 0);
            Vector3 V(float x, float y, float z) => center + rotation * new Vector3(x * s.x, y * s.y, z * s.z);
            Face(mat,V(-1,1,-1),V(-1,1,1),V(1,1,1),V(1,1,-1));
            Face(mat,V(-1,-1,-1),V(1,-1,-1),V(1,-1,1),V(-1,-1,1));
            Face(mat,V(-1,-1,-1),V(-1,1,-1),V(1,1,-1),V(1,-1,-1));
            Face(mat,V(1,-1,1),V(1,1,1),V(-1,1,1),V(-1,-1,1));
            Face(mat,V(-1,-1,1),V(-1,1,1),V(-1,1,-1),V(-1,-1,-1));
            Face(mat,V(1,-1,-1),V(1,1,-1),V(1,1,1),V(1,-1,1));
        }
        static GameObject Finish(string name, Transform parent)
        {
            var mesh = new Mesh { name = name }; mesh.SetVertices(vertices); mesh.subMeshCount = palette.Length;
            for (int i = 0; i < palette.Length; i++) mesh.SetTriangles(triangles[i], i);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            string path = Folder + "/" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null) AssetDatabase.CreateAsset(mesh, path);
            else
            {
                // Use Mesh setters so Unity also refreshes the GPU buffers of loaded prefabs.
                existing.Clear(); existing.vertices = mesh.vertices; existing.subMeshCount = mesh.subMeshCount;
                for (int i = 0; i < mesh.subMeshCount; i++) existing.SetTriangles(mesh.GetTriangles(i), i);
                existing.normals = mesh.normals; existing.bounds = mesh.bounds;
                EditorUtility.SetDirty(existing); UnityEngine.Object.DestroyImmediate(mesh); mesh = existing;
            }
            var obj = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); obj.transform.SetParent(parent, false);
            obj.GetComponent<MeshFilter>().sharedMesh = mesh; obj.GetComponent<MeshRenderer>().sharedMaterials = palette;
            return obj;
        }

static void Beam(Vector3 a,Vector3 b,float width){
var direction=b-a;var up=direction.normalized;var right=Vector3.Cross(up,Vector3.forward).normalized;if(right.sqrMagnitude<.01f)right=Vector3.right;var forward=Vector3.Cross(right,up).normalized;
Vector3 P(Vector3 c,int x,int z)=>c+right*x*width/2+forward*z*width/2;
Face(2,P(a,-1,-1),P(b,-1,-1),P(b,1,-1),P(a,1,-1));Face(2,P(a,1,1),P(b,1,1),P(b,-1,1),P(a,-1,1));
Face(2,P(a,-1,1),P(b,-1,1),P(b,-1,-1),P(a,-1,-1));Face(2,P(a,1,-1),P(b,1,-1),P(b,1,1),P(a,1,1));}
[MenuItem("Tools/Delivery Service/Mok/Build and Install Bike Cargo")]
public static void Build(){
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/JJinCity.unity")throw new Exception("Open JJinCity");
if(!EditorSceneManager.SaveScene(scene,"Temp/JJinCity-before-cargo.unity",true))throw new Exception("Scene backup failed");
Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
palette=new[]{Mat("CargoRed","#833D32"),Mat("CargoCream","#D8C8A8"),Mat("CargoSteel","#334548"),Mat("CargoInner","#253335"),Mat("CargoReflector","#C86643")};
var root=new GameObject("Mok Bike Cargo");
Begin();Box(3,new Vector3(0,.018f,0),new Vector3(.66f,.036f,.64f));
foreach(float side in new[]{-1f,1f}){
Box(0,new Vector3(side*.317f,.25f,0),new Vector3(.026f,.46f,.64f));
Box(0,new Vector3(0,.25f,side*.307f),new Vector3(.61f,.46f,.026f));
Box(1,new Vector3(side*.332f,.24f,0),new Vector3(.004f,.065f,.60f));
Box(2,new Vector3(side*.318f,.483f,0),new Vector3(.035f,.016f,.66f));
Box(2,new Vector3(0,.483f,side*.31f),new Vector3(.65f,.016f,.035f));
Box(2,new Vector3(side*.22f,-.045f,0),new Vector3(.04f,.04f,.84f));
Beam(new Vector3(side*.22f,-.045f,-.23f),new Vector3(side*.17f,-.27f,.27f),.035f);
Box(4,new Vector3(side*.23f,.12f,-.322f),new Vector3(.10f,.045f,.006f));}
Box(2,new Vector3(0,-.045f,0),new Vector3(.58f,.04f,.04f));
Box(2,new Vector3(0,.40f,-.328f),new Vector3(.12f,.07f,.02f));
// Large cream pizza-slice emblem faces the rear.
Face(1,new Vector3(-.10f,.31f,-.322f),new Vector3(.10f,.31f,-.322f),new Vector3(0,.16f,-.322f));
Box(1,new Vector3(0,.33f,-.324f),new Vector3(.23f,.024f,.004f));
Finish("CargoShell",root.transform);
var hinge=new GameObject("LidHinge").transform;hinge.SetParent(root.transform,false);hinge.localPosition=new Vector3(0,.49f,.32f);
Begin();Box(0,new Vector3(0,.017f,-.32f),new Vector3(.70f,.034f,.68f));Box(3,new Vector3(0,-.004f,-.32f),new Vector3(.61f,.008f,.60f));Box(1,new Vector3(0,.035f,-.32f),new Vector3(.06f,.003f,.57f));Finish("CargoLid",hinge);
var inside=new GameObject("Contents").transform;inside.SetParent(root.transform,false);inside.localPosition=new Vector3(0,.04f,0);
var prefab=PrefabUtility.SaveAsPrefabAsset(root,Folder+"/BikeCargo.prefab");UnityEngine.Object.DestroyImmediate(root);
foreach(var storage in UnityEngine.Object.FindObjectsByType<BikePizzaStorage>(FindObjectsSortMode.None)){
if(storage.GetComponent<BikeCargoVisual>()!=null)continue;
Undo.RegisterFullObjectHierarchyUndo(storage.gameObject,"Install bike cargo");
foreach(var r in storage.GetComponents<Renderer>())UnityEngine.Object.DestroyImmediate(r);foreach(var f in storage.GetComponents<MeshFilter>())UnityEngine.Object.DestroyImmediate(f);
storage.transform.localPosition=new Vector3(0,.74f,-1.10f);storage.transform.localRotation=Quaternion.identity;storage.transform.localScale=Vector3.one;
var model=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);model.transform.SetParent(storage.transform,false);model.layer=storage.gameObject.layer;
var col=storage.GetComponent<BoxCollider>();if(col!=null){col.center=new Vector3(0,.25f,0);col.size=new Vector3(.70f,.52f,.68f);}
var visual=storage.gameObject.AddComponent<BikeCargoVisual>();visual.Configure(model.transform.Find("LidHinge"),model.transform.Find("Contents"));
storage.pizzaBoxVisual=model;EditorUtility.SetDirty(storage);EditorUtility.SetDirty(visual);
PrefabUtility.RecordPrefabInstancePropertyModifications(storage);PrefabUtility.RecordPrefabInstancePropertyModifications(storage.transform);if(col!=null)PrefabUtility.RecordPrefabInstancePropertyModifications(col);
}
AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
}
}}
#endif
