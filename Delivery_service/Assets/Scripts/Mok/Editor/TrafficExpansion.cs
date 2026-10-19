#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Text;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad]public static class TrafficExpansion {
static TrafficExpansion(){EditorApplication.update+=Poll;}
static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/Mok-traffic-build"))return;File.Delete("Temp/Mok-traffic-build");try{Run();}catch(Exception e){File.WriteAllText("Temp/Mok-traffic-build.txt",e.ToString());}}
static void Run(){
string stamp=DateTime.Now.ToString("yyyyMMdd-HHmmss");for(int i=0;i<SceneManager.sceneCount;i++){var s=SceneManager.GetSceneAt(i);if(s.isLoaded&&!EditorSceneManager.SaveScene(s,"Temp/Before-traffic-"+stamp+"-"+i+".unity",true))throw new Exception("Backup failed");}
var scene=SceneManager.GetSceneByPath("Assets/Scenes/JJinCity.unity");bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene("Assets/Scenes/JJinCity.unity",OpenSceneMode.Additive);
try{
if(!EditorSceneManager.SaveScene(scene,"Temp/JJinCity-before-traffic-"+stamp+".unity",true))throw new Exception("Backup failed");
var cars=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<CarMov>(true)).ToList();if(cars.Count!=3)throw new Exception("Expected original three cars; inspect before rebuilding");
var way=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<WayPoint>(true)).ToDictionary(w=>w.name);
string[] types={"Sedan","Small","Muscle","Taxi","Medium","Sedan","Small","Muscle","Van"};
int[] from={1,10,12,16,18,24,29,32,53},to={7,11,13,17,19,25,31,33,61};
float[] speeds={6.6f,7.1f,8f,8.4f,7.5f,9.3f,8.9f,7.8f,8.1f,7.3f,9.6f,6.9f};
var parent=scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="Road_Cars");
for(int i=0;i<types.Length;i++){
var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonCity/Prefabs/Vehicles/SM_Veh_Car_"+types[i]+"_01.prefab");var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);go.name="Traffic "+(i+4).ToString("00")+" "+types[i];if(parent!=null)go.transform.SetParent(parent.transform,true);
var a=way["Point"+from[i]];var b=way["Point"+to[i]];if(!a.nextWaypoints.Contains(b))throw new Exception("Invalid road link");
go.transform.position=Vector3.Lerp(a.transform.position,b.transform.position,.5f)+Vector3.up*.05f;go.transform.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(b.transform.position-a.transform.position,Vector3.up));
var car=go.AddComponent<CarMov>();var so=new SerializedObject(car);so.FindProperty("currentTarget").objectReferenceValue=b;so.FindProperty("rotationSpeed").floatValue=5;so.FindProperty("reachDistance").floatValue=.3f;so.FindProperty("lockYPosition").boolValue=true;so.FindProperty("obstacleLayer").intValue=(1<<11)|(1<<16);so.ApplyModifiedPropertiesWithoutUndo();cars.Add(car);
}
var log=new StringBuilder();for(int i=0;i<cars.Count;i++){
var car=cars[i];var so=new SerializedObject(car);so.FindProperty("speed").floatValue=speeds[i];so.ApplyModifiedPropertiesWithoutUndo();
var bounds=new Bounds();bool first=true;foreach(var r in car.GetComponentsInChildren<Renderer>()){
var local=car.transform.worldToLocalMatrix*r.localToWorldMatrix;var b=r.localBounds;for(int n=0;n<8;n++){var p=local.MultiplyPoint3x4(b.center+Vector3.Scale(b.extents,new Vector3((n&1)==0?-1:1,(n&2)==0?-1:1,(n&4)==0?-1:1)));if(first){bounds=new Bounds(p,Vector3.zero);first=false;}else bounds.Encapsulate(p);}
// 에셋에 포함된 팔레트만 교체하고 유리 재질은 유지한다.
var mats=r.sharedMaterials;for(int m=0;m<mats.Length;m++)if(mats[m]!=null&&mats[m].name.StartsWith("PolygonCity_01_"))mats[m]=AssetDatabase.LoadAssetAtPath<Material>("Assets/Synty/PolygonCity/Materials/Alts/PolygonCity_01_"+(char)('A'+i%3)+".mat");r.sharedMaterials=mats;PrefabUtility.RecordPrefabInstancePropertyModifications(r);
}
var trigger=car.GetComponent<BoxCollider>();if(trigger==null)trigger=car.gameObject.AddComponent<BoxCollider>();trigger.center=bounds.center;trigger.size=bounds.size;trigger.isTrigger=true;
var rb=car.GetComponent<Rigidbody>();if(rb==null)rb=car.gameObject.AddComponent<Rigidbody>();rb.isKinematic=true;rb.useGravity=false;
var spacing=car.GetComponent<TrafficSpacing>();if(spacing==null)spacing=car.gameObject.AddComponent<TrafficSpacing>();spacing.bodyLength=bounds.size.z;
var rolling=car.GetComponent<TrafficWheelVisual>();if(rolling==null)rolling=car.gameObject.AddComponent<TrafficWheelVisual>();rolling.wheels=car.GetComponentsInChildren<Transform>().Where(t=>t.name.Contains("_Wheel_")).ToArray();rolling.radii=rolling.wheels.Select(t=>t.GetComponent<Renderer>().bounds.extents.y).ToArray();if(rolling.wheels.Length!=4)throw new Exception("Wheel count");
PrefabUtility.RecordPrefabInstancePropertyModifications(car);PrefabUtility.RecordPrefabInstancePropertyModifications(trigger);PrefabUtility.RecordPrefabInstancePropertyModifications(rb);
log.AppendLine(car.name+" speed="+speeds[i]+" palette="+(char)('A'+i%3)+" position="+car.transform.position);
}
for(int i=0;i<cars.Count;i++)for(int j=i+1;j<cars.Count;j++)if(Vector3.Distance(cars[i].transform.position,cars[j].transform.position)<9)throw new Exception("Overlapping starts");
EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Save failed");File.WriteAllText("Temp/Mok-traffic-build.txt","PASS: 12 vehicles, 6 models, 3 palettes, distinct speeds, spaced starts.\n"+log);
}finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
}
}}
#endif
