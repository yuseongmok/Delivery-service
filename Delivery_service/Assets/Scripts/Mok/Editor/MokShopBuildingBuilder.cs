#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using DeliveryService.Mok.ShopUI;

public static class MokShopBuildingBuilder
{
const string Folder="Assets/Scripts/Mok/Art/ShopBuilding";
static GameObject root; static Material[] mats; static Font font;
static readonly List<Vector3> v=new List<Vector3>();static List<int>[] tri;
static readonly List<Light> lights=new List<Light>();static readonly List<Renderer> lamps=new List<Renderer>();static readonly List<TextMesh> signs=new List<TextMesh>();
static Vector3 Anchor=new Vector3(138f,.23f,60f);
[InitializeOnLoadMethod]static void Schedule(){if(File.Exists("Temp/Mok-building-build-request"))EditorApplication.delayCall+=()=>{if(EditorApplication.isPlayingOrWillChangePlaymode){Debug.LogWarning("Exit play mode before building shop.");return;}File.Delete("Temp/Mok-building-build-request");try{Build();}catch(Exception e){File.WriteAllText("Temp/Mok-building-result.txt",e.ToString());Debug.LogException(e);}};}
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
static Light Lamp(string name,Vector3 pos,float intensity,float range,LightType type,Vector3 rot){var go=new GameObject(name);go.transform.SetParent(root.transform,false);go.transform.localPosition=pos;go.transform.localEulerAngles=rot;var l=go.AddComponent<Light>();l.type=type;l.color=new Color(1,.85f,.65f);l.intensity=intensity;l.range=range;l.spotAngle=115;l.shadows=name=="SignLight"?LightShadows.None:LightShadows.Soft;l.shadowBias=.035f;l.shadowNormalBias=.2f;lights.Add(l);return l;}
[MenuItem("Tools/Delivery Service/Mok/Build and Install Pizza Shop")]
public static void Build(){
var shop=GameObject.Find("PizzaStore");if(shop==null)throw new Exception("PizzaStore missing");if(shop.transform.Find("Mok Pizza Shop")!=null)throw new Exception("Building already installed. Preserve edits.");if(Directory.Exists(Folder))throw new Exception("Asset folder exists; do not overwrite.");
EditorSceneManager.SaveScene(shop.scene);File.Copy(shop.scene.path,"Temp/Mok-building-before.unity",true);
// 기존 게임 오브젝트의 Transform과 저장된 참조를 유지한다.
var behaviours=shop.GetComponentsInChildren<MonoBehaviour>(true).Where(b=>b!=null).ToArray();var positions=behaviours.ToDictionary(b=>b,b=>b.transform.position);
var refs=new List<Tuple<MonoBehaviour,string,UnityEngine.Object>>();foreach(var b in behaviours){var so=new SerializedObject(b);var p=so.GetIterator();while(p.Next(true))if(p.propertyType==SerializedPropertyType.ObjectReference)refs.Add(Tuple.Create(b,p.propertyPath,p.objectReferenceValue));}
Directory.CreateDirectory(Folder);AssetDatabase.Refresh();font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/BMJUA_ttf.ttf");
mats=new[]{Mat("Limestone","#999F93"),Mat("DeepTeal","#30484A"),Mat("OxideRed","#884C3E"),Mat("StoneFloor","#747D78"),Mat("RoofSlate","#3D4849"),Mat("WarmTrim","#B6B7A3"),Mat("Steel","#687A77")};v.Clear();tri=mats.Select(m=>new List<int>()).ToArray();lights.Clear();lamps.Clear();signs.Clear();
root=new GameObject("Mok Pizza Shop");root.transform.position=Anchor;root.transform.SetParent(shop.transform,true);Undo.RegisterCreatedObjectUndo(root,"Install low-poly pizza shop");
Solid("Foundation",3,new Vector3(0,-.14f,0),new Vector3(10.98f,.28f,9.62f));
// 사진 텍스처 대신 큼직하고 단순한 타일을 만든다.
for(int x=-5;x<=5;x++)Box(4,new Vector3(x,-.001f,0),new Vector3(.012f,.006f,9.4f));
for(int z=-4;z<=4;z++)Box(4,new Vector3(0,-.001f,z),new Vector3(10.8f,.006f,.012f));
Solid("BackWall",0,new Vector3(0,1.75f,-3.34f),new Vector3(8.98f,3.5f,.28f));
foreach(float x in new[]{-4.4f,4.4f}){
Solid("SideLowerWall",0,new Vector3(x,.53f,-.2f),new Vector3(.28f,1.06f,6.55f));
Solid("SideUpperWall",0,new Vector3(x,3.13f,-.2f),new Vector3(.28f,.74f,6.55f));
foreach(float z in new[]{-3.3f,-1.2f,.9f,2.9f}){Solid("SidePier",0,new Vector3(x,1.76f,z),new Vector3(.38f,3.52f,.32f));Box(5,new Vector3(x,.15f,z),new Vector3(.43f,.3f,.38f));}
foreach(float z in new[]{-2.25f,-.15f,1.9f}){
Box(1,new Vector3(x,1.95f,z),new Vector3(.32f,.065f,1.85f));
Box(1,new Vector3(x,1.07f,z),new Vector3(.36f,.10f,1.85f));Box(1,new Vector3(x,2.76f,z),new Vector3(.36f,.10f,1.85f));}
Box(5,new Vector3(x,1.08f,-.2f),new Vector3(.39f,.09f,6.65f));}
// 기존 오른쪽 출입 동선을 막지 않도록 전면 창을 배치한다.
Solid("FrontLowWall",0,new Vector3(-1.2f,.43f,2.91f),new Vector3(6.35f,.86f,.22f));
foreach(float x in new[]{-4.4f,-1.9f,2.05f,4.4f}){
Solid("FrontPier",0,new Vector3(x,1.65f,2.91f),new Vector3(.28f,3.3f,.3f));Box(5,new Vector3(x,.15f,2.91f),new Vector3(.38f,.3f,.38f));Box(5,new Vector3(x,3.19f,2.91f),new Vector3(.44f,.17f,.44f));}
foreach(float x in new[]{-3.15f,.05f}){
float width=x< -1?2.18f:3.62f;Box(1,new Vector3(x,.91f,2.94f),new Vector3(width,.10f,.18f));Box(1,new Vector3(x,2.92f,2.94f),new Vector3(width,.10f,.18f));Box(1,new Vector3(x,1.96f,2.94f),new Vector3(.07f,2.02f,.12f));}
Solid("FrontHeader",1,new Vector3(0,3.29f,2.91f),new Vector3(9.1f,.3f,.35f));
Solid("RoofSlab",4,new Vector3(0,3.57f,-.05f),new Vector3(9.5f,.25f,7.2f));
Box(5,new Vector3(0,3.42f,-.05f),new Vector3(9.65f,.10f,7.35f));
Box(2,new Vector3(0,3.78f,3.49f),new Vector3(9.65f,.58f,.18f));Box(1,new Vector3(0,4.09f,3.49f),new Vector3(9.8f,.09f,.24f));Box(5,new Vector3(0,3.47f,3.49f),new Vector3(9.8f,.06f,.24f));
foreach(float x in new[]{-4.72f,4.72f})Box(1,new Vector3(x,3.88f,-.05f),new Vector3(.16f,.42f,7.25f));
Box(1,new Vector3(0,3.88f,-3.62f),new Vector3(9.5f,.42f,.16f));
// 지붕 설비는 단순한 다면체로 만들고 간판 뒤쪽에 둔다.
Box(6,new Vector3(-2.8f,3.95f,-1.9f),new Vector3(1.1f,.45f,.95f));for(int i=0;i<5;i++)Box(4,new Vector3(-3.2f+i*.2f,4.19f,-1.9f),new Vector3(.08f,.025f,.8f));
Box(4,new Vector3(2.8f,4.12f,-2.2f),new Vector3(.5f,.8f,.5f));Box(6,new Vector3(2.8f,4.55f,-2.2f),new Vector3(.7f,.1f,.7f));
// 굵은 벽돌 띠와 모서리 블록을 만든다.
for(float y=.5f;y<3.3f;y+=.52f){Box(5,new Vector3(0,y,-3.49f),new Vector3(8.95f,.022f,.025f));foreach(float x in new[]{-4.4f,4.4f})foreach(float z in new[]{-3.3f,2.91f})Box(5,new Vector3(x,y,z),new Vector3(.39f,.18f,.36f));}
// 전면 띠에 로우폴리 피자 문양을 배치한다.
Disc(5,new Vector3(-3.48f,3.79f,3.595f),.25f,12);Disc(2,new Vector3(-3.48f,3.79f,3.601f),.19f,12);Disc(5,new Vector3(-3.48f,3.79f,3.605f),.16f,12);
foreach(var p in new[]{new Vector2(-.07f,.05f),new Vector2(.08f,.02f),new Vector2(0,-.09f)})Disc(2,new Vector3(-3.48f+p.x,3.79f+p.y,3.61f),.035f,6);
var shell=root.AddComponent<MeshFilter>();shell.sharedMesh=Mesh("ShopShell_LowPoly");var renderer=root.AddComponent<MeshRenderer>();renderer.sharedMaterials=mats;
// 창은 조명을 받는 투명 재질로 만들어 내부가 보이게 한다.
var glass=Mat("WindowGlass","#78948D",.45f);glass.SetFloat("_Surface",1);glass.SetFloat("_Blend",0);glass.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha);glass.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);glass.SetInt("_ZWrite",0);glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");glass.renderQueue=3000;glass.SetColor("_BaseColor",new Color(.47f,.58f,.55f,.16f));
foreach(float x in new[]{-4.4f,4.4f})foreach(float z in new[]{-2.25f,-.15f,1.9f})Cube("SideGlass",new Vector3(x,1.94f,z),new Vector3(.035f,1.6f,1.82f),glass,root.transform,true);
Cube("FrontGlassLeft",new Vector3(-3.15f,1.93f,2.93f),new Vector3(2.17f,1.93f,.035f),glass,root.transform,true);Cube("FrontGlassMain",new Vector3(.05f,1.93f,2.93f),new Vector3(3.61f,1.93f,.035f),glass,root.transform,true);
signs.Add(Text("ShopName","이얏호 피자",new Vector3(-.45f,3.79f,3.602f),.075f,root.transform,new Vector3(0,180,0)));
signs.Add(Text("ShopSubtitle","PIZZA  /  DELIVERY",new Vector3(2.7f,3.79f,3.602f),.027f,root.transform,new Vector3(0,180,0)));
var glow=Mat("LampDiffuser","#EDDBAE");glow.EnableKeyword("_EMISSION");glow.SetColor("_EmissionColor",new Color(1,.79f,.49f)*2);
foreach(float x in new[]{-2.65f,2.25f})foreach(float z in new[]{-1.7f,1.5f}){
Cube("CeilingLampHousing",new Vector3(x,3.36f,z),new Vector3(1.1f,.10f,.32f),mats[1],root.transform);
lamps.Add(Cube("CeilingLampDiffuser",new Vector3(x,3.299f,z),new Vector3(1f,.025f,.24f),glow,root.transform).GetComponent<Renderer>());
Lamp("InteriorLight",new Vector3(x,3.18f,z),4.5f,5.5f,LightType.Spot,new Vector3(90,0,0));}
foreach(float x in new[]{-2.8f,2.8f}){Cube("SignLampArm",new Vector3(x,4.13f,3.75f),new Vector3(.06f,.07f,.58f),mats[1],root.transform);Cube("SignLampHousing",new Vector3(x,4.09f,4.02f),new Vector3(.34f,.12f,.24f),mats[1],root.transform);lamps.Add(Cube("SignLampLens",new Vector3(x,4.02f,4.02f),new Vector3(.28f,.025f,.18f),glow,root.transform).GetComponent<Renderer>());Lamp("SignLight",new Vector3(x,3.99f,3.99f),2.2f,3,LightType.Spot,new Vector3(52,180,0));}
var switchGo=Cube("LightSwitch",new Vector3(4.22f,1.35f,2.25f),new Vector3(.1f,.24f,.18f),mats[5],root.transform,true);switchGo.layer=3;
// 글자와 상호작용 범위가 맞도록 스위치 스케일은 1로 유지한다.
var switchWorld=switchGo.transform.position;switchGo.GetComponent<BoxCollider>().size=new Vector3(1,1,1);
var controller=switchGo.AddComponent<ShopLightSwitch>();controller.lights=lights.ToArray();controller.lampSurfaces=lamps.ToArray();controller.signText=signs.ToArray();
Cube("SwitchRocker",new Vector3(4.159f,1.35f,2.25f),new Vector3(.025f,.13f,.10f),mats[1],root.transform);
controller.label=Text("SwitchLabel","E  조명 끄기",new Vector3(4.15f,1.59f,2.25f),.025f,root.transform,new Vector3(0,90,0));controller.SetLights(true);
// 불러온 부모 오브젝트에도 기존 건물 메시가 있다.
foreach(var r in shop.GetComponents<Renderer>()){Undo.RecordObject(r,"Hide original shop mesh");r.enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(r);}
foreach(var c in shop.GetComponents<Collider>()){Undo.RecordObject(c,"Disable original shop collision");c.enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(c);}
// 기존 외벽만 교체하고 복구용으로 비활성화해 보관한다.
foreach(string name in new[]{"Ground","Ground (1)","Wall_L","Wall_R","Wall_B","Wall_F","Wall_F (1)","Signboard"}){var old=shop.transform.Find(name);if(old==null)throw new Exception("Missing shell part: "+name);if(old.GetComponentsInChildren<MonoBehaviour>(true).Any(b=>b!=null))throw new Exception("Gameplay script on shell: "+name);Undo.RecordObject(old.gameObject,"Replace original shop shell");old.gameObject.SetActive(false);PrefabUtility.RecordPrefabInstancePropertyModifications(old.gameObject);}
// 이 매장의 Unlit 소품 재질만 변환하고 텍스처는 유지한다.
var converted=new Dictionary<Material,Material>();foreach(var r in shop.GetComponentsInChildren<MeshRenderer>(true)){
if(r.transform.IsChildOf(root.transform)||!r.gameObject.activeInHierarchy)continue;var materials=r.sharedMaterials;bool changed=false;
for(int i=0;i<materials.Length;i++){var old=materials[i];if(old==null||old.shader.name.IndexOf("Unlit",StringComparison.OrdinalIgnoreCase)<0)continue;if(!converted.TryGetValue(old,out var lit)){lit=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="PropLit_"+converted.Count};var texture=old.HasProperty("_BaseMap")?old.GetTexture("_BaseMap"):old.HasProperty("_MainTex")?old.GetTexture("_MainTex"):null;lit.SetTexture("_BaseMap",texture);lit.SetColor("_BaseColor",old.HasProperty("_BaseColor")?old.GetColor("_BaseColor"):old.HasProperty("_Color")?old.GetColor("_Color"):Color.white);lit.SetFloat("_Smoothness",.12f);AssetDatabase.CreateAsset(lit,Folder+"/"+lit.name+".mat");converted.Add(old,lit);}materials[i]=lit;changed=true;}
if(changed){Undo.RecordObject(r,"Make store props respond to lights");r.sharedMaterials=materials;PrefabUtility.RecordPrefabInstancePropertyModifications(r);}}
foreach(var pair in positions)if(pair.Key==null||Vector3.Distance(pair.Key.transform.position,pair.Value)>.0001f)throw new Exception("Gameplay transform changed");foreach(var entry in refs)if(new SerializedObject(entry.Item1).FindProperty(entry.Item2).objectReferenceValue!=entry.Item3)throw new Exception("Gameplay reference changed: "+entry.Item2);
AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(shop.scene);EditorSceneManager.SaveScene(shop.scene);Selection.activeGameObject=root;SceneView.lastActiveSceneView?.FrameSelected();
File.WriteAllText("Temp/Mok-building-result.txt",$"PASS: low-poly shell installed at original footprint. {positions.Count} gameplay components and {refs.Count} references preserved. {lights.Count} switched lights. Original shell retained inactive. {converted.Count} local unlit prop materials converted to Lit. Scene saved.");
}
}
#endif
