#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class MokRepairShopBuilder {
const string Folder="Assets/Scripts/Mok/Art/RepairShop";
static GameObject root; static Material[] mats; static Font font;
static readonly List<Vector3> v=new List<Vector3>();static List<int>[] tri;
static readonly List<Light> lights=new List<Light>();
[InitializeOnLoadMethod]static void Schedule(){if(File.Exists("Temp/Mok-repair-build-request"))EditorApplication.delayCall+=()=>{if(EditorApplication.isPlayingOrWillChangePlaymode)return;File.Delete("Temp/Mok-repair-build-request");try{Build();}catch(Exception e){File.WriteAllText("Temp/Mok-repair-result.txt",e.ToString());Debug.LogException(e);}};}
static Material Mat(string name,string hex,float smooth=.12f){var m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.name=name;ColorUtility.TryParseHtmlString(hex,out var c);m.SetColor("_BaseColor",c);m.SetFloat("_Smoothness",smooth);AssetDatabase.CreateAsset(m,Folder+"/"+name+".mat");return m;}
static void Face(int material,params Vector3[] points){int start=v.Count;v.AddRange(points);for(int i=1;i<points.Length-1;i++){tri[material].Add(start);tri[material].Add(start+i);tri[material].Add(start+i+1);}}
static void Box(int m,Vector3 c,Vector3 s){var a=c-s*.5f;var b=c+s*.5f;
Face(m,new Vector3(a.x,a.y,a.z),new Vector3(a.x,b.y,a.z),new Vector3(b.x,b.y,a.z),new Vector3(b.x,a.y,a.z));
Face(m,new Vector3(b.x,a.y,b.z),new Vector3(b.x,b.y,b.z),new Vector3(a.x,b.y,b.z),new Vector3(a.x,a.y,b.z));
Face(m,new Vector3(a.x,a.y,b.z),new Vector3(a.x,b.y,b.z),new Vector3(a.x,b.y,a.z),new Vector3(a.x,a.y,a.z));
Face(m,new Vector3(b.x,a.y,a.z),new Vector3(b.x,b.y,a.z),new Vector3(b.x,b.y,b.z),new Vector3(b.x,a.y,b.z));
Face(m,new Vector3(a.x,b.y,a.z),new Vector3(a.x,b.y,b.z),new Vector3(b.x,b.y,b.z),new Vector3(b.x,b.y,a.z));
Face(m,new Vector3(a.x,a.y,b.z),new Vector3(a.x,a.y,a.z),new Vector3(b.x,a.y,a.z),new Vector3(b.x,a.y,b.z));}
static void Solid(string name,int m,Vector3 c,Vector3 s){Box(m,c,s);var go=new GameObject(name);go.transform.SetParent(root.transform,false);var col=go.AddComponent<BoxCollider>();col.center=c;col.size=s;}
static Mesh Mesh(string name){var mesh=new Mesh{name=name};mesh.indexFormat=IndexFormat.UInt32;mesh.SetVertices(v);mesh.subMeshCount=mats.Length;for(int i=0;i<tri.Length;i++)mesh.SetTriangles(tri[i],i);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Folder+"/"+name+".asset");return mesh;}
static GameObject Cube(string name,Vector3 pos,Vector3 size,Material mat,Transform parent,bool collider=false){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=pos;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=mat;if(!collider)UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());return go;}
static TextMesh Text(string name,string value,Vector3 pos,float size,Transform parent,Vector3 rotation){var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=pos;go.transform.localEulerAngles=rotation;var t=go.AddComponent<TextMesh>();t.font=font;t.fontSize=100;t.characterSize=size;t.anchor=TextAnchor.MiddleCenter;t.alignment=TextAlignment.Center;t.text=value;t.color=new Color(.93f,.88f,.73f);go.GetComponent<MeshRenderer>().sharedMaterial=font.material;return t;}
static void Disc(int mat,Vector3 c,float radius,int sides){for(int i=0;i<sides;i++){float a=i*Mathf.PI*2/sides,b=(i+1)*Mathf.PI*2/sides;Face(mat,c,c+new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,0),c+new Vector3(Mathf.Cos(b)*radius,Mathf.Sin(b)*radius,0));}}
static Light Lamp(string name,Vector3 pos,float intensity,float range,LightType type,Vector3 rot){var go=new GameObject(name);go.transform.SetParent(root.transform,false);go.transform.localPosition=pos;go.transform.localEulerAngles=rot;var l=go.AddComponent<Light>();l.type=type;l.color=new Color(1,.85f,.65f);l.intensity=intensity;l.range=range;l.spotAngle=115;l.shadows=LightShadows.None;l.shadowBias=.035f;l.shadowNormalBias=.2f;lights.Add(l);return l;}

// 기계 소품은 면 수가 적은 원통으로 만들고 면별 음영을 유지한다.
static void Cylinder(int m,Vector3 center,float radius,float length,Quaternion rot,int sides=12){
for(int i=0;i<sides;i++){float a=i*2*Mathf.PI/sides,b=(i+1)*2*Mathf.PI/sides;
var p=new Vector3(Mathf.Cos(a)*radius,-length/2,Mathf.Sin(a)*radius);var q=new Vector3(Mathf.Cos(b)*radius,-length/2,Mathf.Sin(b)*radius);var u=p+Vector3.up*length;var w=q+Vector3.up*length;
Face(m,center+rot*p,center+rot*u,center+rot*w,center+rot*q);
Face(m,center+rot*(Vector3.up*length/2),center+rot*w,center+rot*u);
Face(m,center-rot*(Vector3.up*length/2),center+rot*p,center+rot*q);}}
static void Tire(Vector3 c){Cylinder(4,c,.43f,.24f,Quaternion.identity);Cylinder(6,c+Vector3.up*.125f,.21f,.015f,Quaternion.identity);Cylinder(4,c+Vector3.up*.136f,.125f,.018f,Quaternion.identity);}
[MenuItem("Tools/Delivery Service/Mok/Build Repair Shop")]
public static void Build(){
var marker=GameObject.Find("repair_shop");if(marker==null)throw new Exception("repair_shop marker missing");if(marker.transform.Find("Mok Repair Garage")!=null||Directory.Exists(Folder))throw new Exception("Garage already exists; preserve manual edits.");
if(marker.GetComponents<MonoBehaviour>().Any())throw new Exception("Marker has behaviour; inspect before replacement.");
EditorSceneManager.SaveScene(marker.scene);File.Copy(marker.scene.path,"Temp/Mok-repair-before.unity",true);
var existing=UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(b=>b!=null).ToArray();var positions=existing.ToDictionary(b=>b,b=>b.transform.position);
Directory.CreateDirectory(Folder);AssetDatabase.Refresh();font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/BMJUA_ttf.ttf");
mats=new[]{Mat("Concrete","#87938D"),Mat("PetrolSteel","#324D51"),Mat("SafetyOchre","#C4A058"),Mat("WorkshopFloor","#626E6C"),Mat("Graphite","#263238"),Mat("WarmWhite","#D4D3BD"),Mat("Steel","#7B8D8C"),Mat("ToolRed","#9B5442")};v.Clear();tri=mats.Select(m=>new List<int>()).ToArray();lights.Clear();
var bounds=marker.GetComponent<Renderer>().bounds;root=new GameObject("Mok Repair Garage");root.transform.position=new Vector3(marker.transform.position.x,bounds.min.y+.08f,marker.transform.position.z);root.transform.rotation=marker.transform.rotation;root.transform.SetParent(marker.transform,true);Undo.RegisterCreatedObjectUndo(root,"Install repair garage");
// 너비 15m, 깊이 8m. 도로 쪽 정면은 +Z이며 내부 전체를 막는 콜라이더는 없다.
Solid("WorkshopFloor",3,new Vector3(0,-.09f,0),new Vector3(15,.18f,8));
Solid("RearWall",0,new Vector3(0,2.55f,-3.86f),new Vector3(15,5.1f,.28f));
foreach(float x in new[]{-7.36f,7.36f}){Solid("SideWall",0,new Vector3(x,2.55f,0),new Vector3(.28f,5.1f,8));Box(1,new Vector3(x,1.1f,0),new Vector3(.31f,1.9f,8));Box(2,new Vector3(x,2.12f,0),new Vector3(.32f,.09f,8));
foreach(float z in new[]{-3.8f,-1.25f,1.25f,3.8f})Box(1,new Vector3(x,2.6f,z),new Vector3(.42f,5.2f,.23f));}
Box(1,new Vector3(0,1,-3.7f),new Vector3(14.45f,2,.07f));Box(2,new Vector3(0,2.12f,-3.65f),new Vector3(14.45f,.09f,.06f));
// 입구 너비 9.2m, 통과 높이 4.35m를 확보한다.
foreach(float x in new[]{-6.05f,6.05f}){Solid("FrontShoulder",0,new Vector3(x,2.55f,3.84f),new Vector3(2.9f,5.1f,.32f));Box(1,new Vector3(x,.55f,4.02f),new Vector3(2.9f,1.1f,.08f));Box(1,new Vector3(Mathf.Sign(x)*4.64f,2.18f,3.98f),new Vector3(.18f,4.36f,.36f));
// 노란 띠는 주행 입구 바깥에 배치한다.
for(int y=0;y<5;y++)Box(2,new Vector3(Mathf.Sign(x)*4.77f,.26f+y*.28f,4.17f),new Vector3(.23f,.11f,.02f));}
Solid("EntranceHeader",1,new Vector3(0,4.74f,3.85f),new Vector3(15,.78f,.4f));
Solid("Roof",4,new Vector3(0,5.22f,0),new Vector3(15.6f,.24f,8.6f));Box(6,new Vector3(0,5.38f,0),new Vector3(15.3f,.08f,8.3f));
foreach(float z in new[]{-4.23f,4.23f})Box(1,new Vector3(0,5.49f,z),new Vector3(15.6f,.34f,.16f));
foreach(float x in new[]{-7.72f,7.72f})Box(1,new Vector3(x,5.49f,0),new Vector3(.16f,.34f,8.5f));
// 간판과 셔터 덮개를 만들고 셔터는 통과 높이 위에 둔다.
Box(1,new Vector3(0,4.86f,4.13f),new Vector3(14.5f,.76f,.18f));Box(2,new Vector3(0,4.42f,4.25f),new Vector3(14.5f,.08f,.14f));
Text("GarageName","바이크 정비소",new Vector3(.1f,4.89f,4.239f),.069f,root.transform,new Vector3(0,180,0));
Text("GarageEnglish","MOTO / SERVICE",new Vector3(5.23f,4.88f,4.24f),.026f,root.transform,new Vector3(0,180,0));
Text("BayNumber","01",new Vector3(-6.06f,3.55f,4.025f),.08f,root.transform,new Vector3(0,180,0));
Text("BayLabel","REPAIR BAY",new Vector3(-6.06f,2.93f,4.026f),.024f,root.transform,new Vector3(0,180,0));
// 넓은 블록으로 스패너 모양을 만든다.
Box(2,new Vector3(-5.5f,4.87f,4.25f),new Vector3(.55f,.12f,.03f));Box(2,new Vector3(-5.85f,5.02f,4.25f),new Vector3(.15f,.26f,.03f));Box(2,new Vector3(-5.85f,4.72f,4.25f),new Vector3(.15f,.26f,.03f));
Box(4,new Vector3(0,4.47f,3.56f),new Vector3(9,.17f,.4f));
// 천장 보와 작업장 설비를 배치한다.
foreach(float z in new[]{-2.7f,0f,2.7f})Box(1,new Vector3(0,4.97f,z),new Vector3(14.7f,.25f,.2f));
var glow=Mat("LightLens","#DDD9BF");glow.EnableKeyword("_EMISSION");glow.SetColor("_EmissionColor",new Color(.95f,.9f,.72f)*1.4f);
foreach(float x in new[]{-3.4f,3.4f})foreach(float z in new[]{-1.8f,1.9f}){Cube("CeilingFixture",new Vector3(x,4.89f,z),new Vector3(2,.12f,.36f),mats[4],root.transform);Cube("CeilingLens",new Vector3(x,4.819f,z),new Vector3(1.8f,.025f,.25f),glow,root.transform);Lamp("WorkLight",new Vector3(x,4.65f,z),22f,7,LightType.Spot,new Vector3(90,0,0));}
// 정비 바닥은 평평하게 만들어 바퀴가 걸리지 않게 한다.
Box(4,new Vector3(0,.003f,-.45f),new Vector3(4.5f,.006f,4.7f));
foreach(float x in new[]{-2.3f,2.3f})for(float z=-2.8f;z<2;z+=.65f)Box(2,new Vector3(x,.008f,z),new Vector3(.09f,.008f,.42f));
foreach(float z in new[]{-2.85f,1.95f})Box(2,new Vector3(0,.008f,z),new Vector3(4.7f,.008f,.09f));
foreach(float x in new[]{-.55f,.55f})Box(6,new Vector3(x,.012f,-.5f),new Vector3(.08f,.008f,3.3f));
Text("ServiceFloorLabel","SERVICE 01",new Vector3(0,.018f,2.6f),.045f,root.transform,new Vector3(90,180,0));
var bay=new GameObject("RepairBayCenter");bay.transform.SetParent(root.transform,false);bay.transform.localPosition=new Vector3(0,0,-.45f);bay.transform.localRotation=Quaternion.Euler(0,180,0);
// 주행과 하차 공간을 확보하도록 장비는 가장자리에 배치한다.
Solid("RearToolBench",1,new Vector3(0,.5f,-3.28f),new Vector3(5.8f,1,.65f));Box(6,new Vector3(0,1.05f,-3.25f),new Vector3(6,.1f,.85f));
for(int x=-2;x<=2;x++){Box(7,new Vector3(x,.59f,-2.943f),new Vector3(.85f,.62f,.025f));for(int y=0;y<3;y++)Box(6,new Vector3(x,.39f+y*.19f,-2.92f),new Vector3(.58f,.035f,.035f));}
Box(4,new Vector3(0,1.94f,-3.65f),new Vector3(4.3f,1.35f,.06f));
for(int x=-3;x<=3;x++){float px=x*.45f;Box(6,new Vector3(px,1.92f,-3.59f),new Vector3(.065f,.5f,.05f));Box(6,new Vector3(px-.07f,2.2f,-3.59f),new Vector3(.08f,.18f,.05f));Box(6,new Vector3(px+.07f,2.2f,-3.59f),new Vector3(.08f,.18f,.05f));}
Text("RearLabel","MOTO WORKSHOP",new Vector3(0,3.45f,-3.7f),.064f,root.transform,new Vector3(0,180,0));
// 왼쪽 타이어 선반과 부품 선반을 만든다.
foreach(float x in new[]{-6.9f,-5.3f})foreach(float z in new[]{-3.15f,-1.9f})Box(1,new Vector3(x,1.2f,z),new Vector3(.09f,2.4f,.09f));
foreach(float y in new[]{.16f,1.15f,2.15f}){Box(6,new Vector3(-6.1f,y,-2.53f),new Vector3(1.7f,.08f,1.35f));Tire(new Vector3(-6.45f,y+.19f,-2.55f));Tire(new Vector3(-5.65f,y+.19f,-2.55f));}
Solid("TireRackCollision",0,new Vector3(-6.1f,1.15f,-2.53f),new Vector3(1.7f,2.3f,1.35f));
// 충돌 전용 상자가 보이지 않도록 마지막 여섯 면을 제거한다.
for(int i=0;i<6;i++){v.RemoveRange(v.Count-4,4);tri[0].RemoveRange(tri[0].Count-6,6);}
Solid("ToolCabinet",7,new Vector3(6.4f,.68f,-2.75f),new Vector3(1.45f,1.36f,.85f));Box(4,new Vector3(6.4f,1.4f,-2.75f),new Vector3(1.55f,.09f,.95f));
for(int i=0;i<5;i++)Box(6,new Vector3(6.4f,.2f+i*.23f,-2.31f),new Vector3(1.15f,.045f,.035f));
Cylinder(7,new Vector3(6.4f,.55f,-.8f),.4f,1,Quaternion.Euler(0,0,90));Box(4,new Vector3(6.4f,1,-.8f),new Vector3(.7f,.35f,.45f));
foreach(float x in new[]{6.05f,6.75f})Cylinder(4,new Vector3(x,.16f,-.8f),.17f,.12f,Quaternion.Euler(90,0,0));
var compressor=new GameObject("CompressorCollision");compressor.transform.SetParent(root.transform,false);var cc=compressor.AddComponent<BoxCollider>();cc.center=new Vector3(6.4f,.6f,-.8f);cc.size=new Vector3(1.35f,1.2f,1);
// 지붕 환기구와 뒤쪽 배관을 만든다.
Box(6,new Vector3(-4.7f,5.82f,-1.8f),new Vector3(2.2f,.85f,1.6f));for(int i=0;i<7;i++)Box(4,new Vector3(-5.55f+i*.28f,5.82f,-.985f),new Vector3(.1f,.53f,.035f));Cylinder(6,new Vector3(5.5f,5.8f,-2.3f),.3f,1,Quaternion.identity);Cylinder(4,new Vector3(5.5f,6.34f,-2.3f),.44f,.12f,Quaternion.identity);
var nav=root.AddComponent<UnityEngine.AI.NavMeshObstacle>();nav.shape=UnityEngine.AI.NavMeshObstacleShape.Box;nav.center=new Vector3(0,2.5f,0);nav.size=new Vector3(15,5,8);nav.carving=true;
var mesh=Mesh("RepairGarage_LowPoly");root.AddComponent<MeshFilter>().sharedMesh=mesh;root.AddComponent<MeshRenderer>().sharedMaterials=mats;
// 보도 턱을 넘는 완만한 진입 경사로를 만든다.
var ramp=new GameObject("EntranceRamp");ramp.transform.SetParent(root.transform,false);var rm=new Mesh{name="GarageRamp"};rm.vertices=new[]{new Vector3(-4.6f,0,4),new Vector3(4.6f,0,4),new Vector3(-4.6f,-.04f,5.6f),new Vector3(4.6f,-.04f,5.6f),new Vector3(-4.6f,-.12f,4),new Vector3(4.6f,-.12f,4),new Vector3(-4.6f,-.12f,5.6f),new Vector3(4.6f,-.12f,5.6f)};rm.triangles=new[]{0,2,1,1,2,3,0,1,4,1,5,4,2,6,3,3,6,7,0,4,2,2,4,6,1,3,5,3,7,5,4,5,6,5,7,6};rm.RecalculateNormals();AssetDatabase.CreateAsset(rm,Folder+"/GarageRamp.asset");ramp.AddComponent<MeshFilter>().sharedMesh=rm;ramp.AddComponent<MeshRenderer>().sharedMaterial=mats[3];ramp.AddComponent<MeshCollider>().sharedMesh=rm;
foreach(var r in marker.GetComponents<Renderer>()){Undo.RecordObject(r,"Replace repair placeholder");r.enabled=false;}foreach(var c in marker.GetComponents<Collider>()){Undo.RecordObject(c,"Remove solid placeholder collision");c.enabled=false;}
foreach(var p in positions)if(p.Key==null||Vector3.Distance(p.Key.transform.position,p.Value)>.0001f)throw new Exception("Existing gameplay transform changed");
AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(marker.scene);EditorSceneManager.SaveScene(marker.scene);Selection.activeGameObject=root;SceneView.lastActiveSceneView?.FrameSelected();File.WriteAllText("Temp/Mok-repair-result.txt","PASS: Garage installed at repair_shop; footprint 15x8 m, entrance 9.2x4.35 m, service pad 4.5x4.7 m. Existing gameplay transforms unchanged. Scene saved.");
}
}
#endif
