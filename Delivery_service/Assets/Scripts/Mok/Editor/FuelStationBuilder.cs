#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using UnityEngine;using UnityEngine.AI;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad]public static class FuelStationBuilder{
const string Dir="Assets/Scripts/Mok/Art/FuelStation";static Material cream,orange,dark,metal,black;
static FuelStationBuilder(){EditorApplication.update+=Poll;}
static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/Mok-fuel-build"))return;File.Delete("Temp/Mok-fuel-build");try{Build();}catch(Exception e){File.WriteAllText("Temp/Mok-fuel-build.txt",e.ToString());}}
static Material Mat(string name,Color color){var path=Dir+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.color=color;m.SetFloat("_Smoothness",.12f);return m;}
static GameObject Box(Transform parent,string name,Vector3 p,Vector3 size,Material mat,bool collider=false){var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.SetParent(parent,false);o.transform.localPosition=p;o.transform.localScale=size;o.GetComponent<Renderer>().sharedMaterial=mat;if(!collider)UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());return o;}
static void Label(Transform parent,string text,Vector3 p,float size,Color color){var o=new GameObject(text);o.transform.SetParent(parent,false);o.transform.localPosition=p;o.transform.localRotation=Quaternion.identity;var t=o.AddComponent<TextMesh>();t.text=text;t.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/BMJUA_ttf.ttf");t.fontSize=64;t.characterSize=size;t.anchor=TextAnchor.MiddleCenter;t.alignment=TextAlignment.Center;t.color=color;o.GetComponent<MeshRenderer>().sharedMaterial=t.font.material;}
static Transform Anchor(Transform parent,string name,Vector3 p){var o=new GameObject(name);o.transform.SetParent(parent,false);o.transform.localPosition=p;return o.transform;}
static void Build(){
string stamp=DateTime.Now.ToString("yyyyMMdd-HHmmss");for(int i=0;i<SceneManager.sceneCount;i++){var s=SceneManager.GetSceneAt(i);if(s.isLoaded&&!EditorSceneManager.SaveScene(s,"Temp/Before-fuel-"+stamp+"-"+i+".unity",true))throw new Exception("Backup failed");}
var scene=SceneManager.GetSceneByPath("Assets/Scenes/JJinCity.unity");bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene("Assets/Scenes/JJinCity.unity",OpenSceneMode.Additive);
try{
if(!EditorSceneManager.SaveScene(scene,"Temp/JJinCity-before-fuel-"+stamp+".unity",true))throw new Exception("Backup failed");
var pump=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Single(t=>t.name=="GasPump");if(pump.Find("Mok Fuel Station")!=null)throw new Exception("Already built");var nozzle=pump.GetComponentInChildren<FuelNozzle>();
Directory.CreateDirectory(Dir);AssetDatabase.Refresh();cream=Mat("Warm concrete",new Color(.66f,.65f,.57f));orange=Mat("Station orange",new Color(.75f,.30f,.09f));dark=Mat("Station navy",new Color(.08f,.13f,.16f));metal=Mat("Pump steel",new Color(.35f,.40f,.41f));black=Mat("Hose rubber",new Color(.025f,.035f,.04f));
foreach(var r in pump.GetComponentsInChildren<Renderer>(true))if(!(r is LineRenderer)){r.enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(r);}
var art=new GameObject("Mok Fuel Station");SceneManager.MoveGameObjectToScene(art,scene);art.transform.SetPositionAndRotation(new Vector3(pump.position.x,pump.position.y-2.5f,pump.position.z),pump.rotation);
var a=art.transform;
Box(a,"Forecourt",new Vector3(0,.025f,0),new Vector3(6.4f,.05f,5),cream);
Box(a,"Pump island",new Vector3(0,.10f,0),new Vector3(2.1f,.15f,1.35f),metal);
Box(a,"Pump base",new Vector3(0,.28f,0),new Vector3(1.35f,.35f,.84f),dark);
Box(a,"Pump cabinet",new Vector3(0,1.08f,0),new Vector3(1.25f,1.4f,.72f),cream);
Box(a,"Orange lower panel",new Vector3(0,.75f,-.37f),new Vector3(1.24f,.85f,.035f),orange);
Box(a,"Top cap",new Vector3(0,1.94f,0),new Vector3(1.4f,.2f,.85f),dark);
Box(a,"Display frame",new Vector3(0,1.51f,-.39f),new Vector3(1.03f,.51f,.08f),metal);
Box(a,"Display",new Vector3(0,1.53f,-.44f),new Vector3(.91f,.37f,.025f),dark);
Label(a,"4,000",new Vector3(0,1.55f,-.46f),.065f,new Color(.88f,.92f,.82f));Label(a,"완충 요금",new Vector3(0,1.20f,-.40f),.035f,Color.white);
Label(a,"F  주유",new Vector3(0,.78f,-.40f),.055f,Color.white);
for(int side=-1;side<=1;side+=2){Box(a,"Canopy column",new Vector3(side*2.8f,1.86f,1.5f),new Vector3(.24f,3.72f,.24f),dark,true);Box(a,"Column base",new Vector3(side*2.8f,.22f,1.5f),new Vector3(.42f,.44f,.42f),metal,true);Box(a,"Parking stripe",new Vector3(side*2,.056f,-.8f),new Vector3(.07f,.012f,2.2f),orange);}
Box(a,"Canopy roof",new Vector3(0,3.78f,0),new Vector3(6.4f,.25f,4.4f),dark,true);
Box(a,"Canopy fascia",new Vector3(0,3.76f,-2.23f),new Vector3(6.4f,.43f,.12f),orange);
Box(a,"Canopy underside",new Vector3(0,3.62f,0),new Vector3(6,.07f,4.05f),cream);
Label(a,"CITY FUEL",new Vector3(-.6f,3.77f,-2.30f),.095f,Color.white);Label(a,"주유소",new Vector3(2.15f,3.77f,-2.30f),.075f,Color.white);
for(int side=-1;side<=1;side+=2){Box(a,"Ceiling lamp",new Vector3(side*1.6f,3.56f,0),new Vector3(.9f,.045f,.3f),cream);var lamp=Anchor(a,"Warm light",new Vector3(side*1.6f,3.25f,0)).gameObject.AddComponent<Light>();lamp.type=LightType.Point;lamp.color=new Color(1,.82f,.58f);lamp.range=4;lamp.intensity=1.3f;lamp.shadows=LightShadows.None;}
var reset=Anchor(a,"Nozzle dock",new Vector3(.72f,1.18f,-.49f));var start=Anchor(a,"Fixed hose outlet",new Vector3(.61f,.70f,-.42f));
// 시제품을 에셋으로 저장하고 씬에도 같은 위치에 남긴다.
var prefab=PrefabUtility.SaveAsPrefabAsset(art,Dir+"/FuelStation.prefab");UnityEngine.Object.DestroyImmediate(art);var placed=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);placed.name="Mok Fuel Station";placed.transform.SetParent(pump,true);a=placed.transform;
nozzle.pumpResetPoint=a.Find("Nozzle dock");nozzle.hoseStartPoint=a.Find("Fixed hose outlet");nozzle.hoseSag=.35f;
nozzle.transform.SetParent(nozzle.pumpResetPoint,false);nozzle.transform.localPosition=Vector3.zero;nozzle.transform.localRotation=Quaternion.identity;var scale=nozzle.pumpResetPoint.lossyScale;nozzle.transform.localScale=new Vector3(1/scale.x,1/scale.y,1/scale.z);
var grip=new GameObject("Mok Fuel Nozzle");grip.transform.SetParent(nozzle.transform,false);Box(grip.transform,"Handle",new Vector3(0,0,0),new Vector3(.16f,.25f,.11f),orange);Box(grip.transform,"Grip cutout",new Vector3(0,-.025f,-.064f),new Vector3(.08f,.12f,.025f),black);Box(grip.transform,"Spout",new Vector3(0,.14f,.12f),new Vector3(.06f,.06f,.27f),metal);Box(grip.transform,"Hose coupling",new Vector3(0,-.16f,0),new Vector3(.08f,.09f,.08f),black);
nozzle.hoseEndPoint=Anchor(nozzle.transform,"Mok hose endpoint",new Vector3(0,-.2f,0));var collider=nozzle.GetComponent<BoxCollider>();collider.center=new Vector3(0,.01f,.04f);collider.size=new Vector3(.24f,.42f,.34f);
nozzle.hoseRender.sharedMaterial=black;nozzle.hoseRender.startWidth=.045f;nozzle.hoseRender.endWidth=.045f;nozzle.hoseRender.useWorldSpace=true;nozzle.hoseRender.positionCount=20;
for(int i=0;i<20;i++){float t=i/19f;var x=nozzle.hoseStartPoint.position;var y=nozzle.hoseEndPoint.position;nozzle.hoseRender.SetPosition(i,Vector3.Lerp(x,y,t)+Vector3.down*Mathf.Sin(t*Mathf.PI)*.2f);}
nozzle.refuelSpeed=15;nozzle.costPerSecond=600;
var pc=pump.GetComponent<BoxCollider>();pc.center=pump.InverseTransformPoint(a.TransformPoint(new Vector3(0,1.1f,0)));pc.size=new Vector3(1.4f/pump.lossyScale.x,2.2f/pump.lossyScale.y,.9f/pump.lossyScale.z);
var obstacle=pump.GetComponent<NavMeshObstacle>();if(obstacle!=null){obstacle.center=pc.center;obstacle.size=pc.size;PrefabUtility.RecordPrefabInstancePropertyModifications(obstacle);}
foreach(var component in new Component[]{nozzle,nozzle.transform,nozzle.hoseRender,collider,pc}){EditorUtility.SetDirty(component);PrefabUtility.RecordPrefabInstancePropertyModifications(component);}
EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Save failed");AssetDatabase.SaveAssets();File.WriteAllText("Temp/Mok-fuel-build.txt","PASS: lit fuel station installed, fixed hose anchor and material, nozzle and interaction references retained.");
}finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
}
}}
#endif
