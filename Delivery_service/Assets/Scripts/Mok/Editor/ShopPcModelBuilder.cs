#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
namespace DeliveryService.Mok.Editor {
public static class ShopPcModelBuilder {
const string Folder="Assets/Scripts/Mok/Art/ShopPC";
static readonly List<Vector3> vertices=new List<Vector3>();
static readonly List<Vector2> uvs=new List<Vector2>();
static List<int>[] indices;
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

[MenuItem("Tools/Delivery Service/Mok/Apply Low Poly Shop PC")]
public static void Apply() {
if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play mode first.");
var pc=UnityEngine.Object.FindObjectsByType<ShopPC>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single();
var mf=pc.GetComponent<MeshFilter>();var mr=pc.GetComponent<MeshRenderer>();var box=pc.GetComponent<BoxCollider>();
if(mf==null||mr==null||box==null||pc.pcUIPanel==null)throw new Exception("Original PC components missing.");
Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/ShopPC.mesh.asset");
string[] names={"CharcoalHousing","SlateTrim","ScreenGlass","ScreenInk","TealAccent","WarmKeys"};
Material[] mats;
if(mesh==null){
mats=new[]{MakeMaterial(names[0],"#29383F",.12f,.22f),MakeMaterial(names[1],"#52656A",.18f,.28f),MakeMaterial(names[2],"#283D48",0,.25f),MakeMaterial(names[3],"#B9C8BD",0,.15f),MakeMaterial(names[4],"#448D87",0,.22f),MakeMaterial(names[5],"#949D95",0,.16f)};
// Only the display has a restrained glow; the housing remains fully light-driven.
mats[2].EnableKeyword("_EMISSION");mats[2].SetColor("_EmissionColor",new Color(.012f,.021f,.026f));
mats[3].EnableKeyword("_EMISSION");mats[3].SetColor("_EmissionColor",new Color(.035f,.044f,.038f));
vertices.Clear();uvs.Clear();indices=Enumerable.Range(0,mats.Length).Select(_=>new List<int>()).ToArray();
// Countertop POS terminal. Authored front is local -Z; asset is rotated to original +Z; same footprint and bottom as original.
Box(0,new Vector3(0,.023f,.015f),new Vector3(.46f,.046f,.36f),.035f);
Box(1,new Vector3(0,.053f,.075f),new Vector3(.24f,.014f,.19f),.018f);
Box(0,new Vector3(0,.168f,.075f),new Vector3(.10f,.23f,.10f),.014f);
Box(1,new Vector3(0,.27f,.067f),new Vector3(.20f,.045f,.11f),.01f);
Box(0,new Vector3(0,.46f,.035f),new Vector3(.69f,.44f,.105f),.022f);
Box(1,new Vector3(0,.46f,-.022f),new Vector3(.656f,.405f,.014f),.014f);
Box(2,new Vector3(0,.471f,-.032f),new Vector3(.598f,.338f,.009f),.007f);
// Minimal order dashboard: sidebar, heading, three receipt rows and status blocks.
Box(4,new Vector3(-.258f,.471f,-.038f),new Vector3(.046f,.30f,.004f),.002f);
Box(3,new Vector3(-.11f,.60f,-.038f),new Vector3(.18f,.013f,.004f),.002f);
for(int row=0;row<3;row++){
float y=.544f-row*.071f;
Box(1,new Vector3(.025f,y,-.038f),new Vector3(.43f,.048f,.004f),.004f);
Box(3,new Vector3(-.065f,y+.007f,-.041f),new Vector3(.19f,.009f,.003f),.001f);
Box(5,new Vector3(-.096f,y-.009f,-.041f),new Vector3(.128f,.005f,.003f),.001f);
Box(4,new Vector3(.191f,y,-.041f),new Vector3(.047f,.024f,.003f),.003f);
}
Box(4,new Vector3(.278f,.273f,-.035f),new Vector3(.018f,.008f,.008f),.002f);
Box(1,new Vector3(-.20f,.274f,-.035f),new Vector3(.065f,.007f,.008f),.001f);
// Integrated compact keyboard, visibly separated keys, rear vents.
Box(1,new Vector3(0,.058f,-.095f),new Vector3(.39f,.026f,.125f),.012f);
for(int row=0;row<3;row++)for(int col=0;col<9;col++)
Box(col==8?4:5,new Vector3(-.158f+col*.0395f,.076f,-.136f+row*.035f),new Vector3(.031f,.011f,.025f),.003f);
for(int i=0;i<7;i++)Box(1,new Vector3(-.12f+i*.04f,.49f,.090f),new Vector3(.017f,.12f,.006f),.002f);
mesh=new Mesh{name="Mok_ShopPC_LowPoly"};mesh.SetVertices(vertices);mesh.SetUVs(0,uvs);mesh.subMeshCount=mats.Length;
for(int i=0;i<indices.Length;i++)mesh.SetTriangles(indices[i],i);
mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Folder+"/ShopPC.mesh.asset");
var visual=new GameObject("Mok Shop PC Visual");visual.AddComponent<MeshFilter>().sharedMesh=mesh;visual.AddComponent<MeshRenderer>().sharedMaterials=mats;
PrefabUtility.SaveAsPrefabAsset(visual,Folder+"/ShopPCVisual.prefab");UnityEngine.Object.DestroyImmediate(visual);AssetDatabase.SaveAssets();
}else mats=names.Select(n=>AssetDatabase.LoadAssetAtPath<Material>(Folder+"/"+n+".mat")).ToArray();
// Persist emissive display settings after URP material import/validation.
foreach(int slot in new[]{2,3,4}) {
var material=mats[slot];material.SetColor("_EmissionColor",slot==2?new Color(.025f,.045f,.055f):slot==3?new Color(.24f,.31f,.27f):new Color(.025f,.11f,.09f));
material.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;material.EnableKeyword("_EMISSION");EditorUtility.SetDirty(material);
}
AssetDatabase.SaveAssets();
// Match the original register's +Z screen without rotating its gameplay transform.
if(mesh.name!="Mok_ShopPC_LowPoly_FrontPositiveZ") {
var rotation=Quaternion.Euler(0,180,0);mesh.vertices=mesh.vertices.Select(v=>rotation*v).ToArray();mesh.normals=mesh.normals.Select(n=>rotation*n).ToArray();mesh.RecalculateBounds();mesh.name="Mok_ShopPC_LowPoly_FrontPositiveZ";EditorUtility.SetDirty(mesh);AssetDatabase.SaveAssets();
}
Undo.RecordObjects(new UnityEngine.Object[]{mf,mr,box},"Replace Shop PC appearance");
mf.sharedMesh=mesh;mr.sharedMaterials=mats;mr.receiveShadows=true;mr.shadowCastingMode=ShadowCastingMode.On;
box.center=mesh.bounds.center;box.size=mesh.bounds.size;
foreach(var obj in new UnityEngine.Object[]{mf,mr,box}){EditorUtility.SetDirty(obj);PrefabUtility.RecordPrefabInstancePropertyModifications(obj);}
EditorSceneManager.MarkSceneDirty(pc.gameObject.scene);Selection.activeGameObject=pc.gameObject;
SceneView.lastActiveSceneView?.FrameSelected();
Debug.Log("MOK_PC_APPLIED: same ShopPC GameObject, transform, UI references, camera references and child order managers; only mesh/material/collider changed. Save scene after review.");
}
}}
#endif
