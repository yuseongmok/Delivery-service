#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using UnityEngine;using UnityEngine.AI;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;using Unity.AI.Navigation;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad] public static class DistrictRefine {
static DistrictRefine(){EditorApplication.update+=Poll;}
static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/Mok-district-refine"))return;File.Delete("Temp/Mok-district-refine");try{Run();}catch(Exception e){File.WriteAllText("Temp/Mok-district-refine.txt",e.ToString());}}
static Bounds B(GameObject g){var rr=g.GetComponentsInChildren<Renderer>();var b=rr[0].bounds;foreach(var r in rr)b.Encapsulate(r.bounds);return b;}
static void Box(Transform p,string n,Vector3 pos,Vector3 size,Material m){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(p);g.transform.position=pos;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=m;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());g.isStatic=true;}
static void Run(){var old=SceneManager.GetActiveScene();for(int i=0;i<SceneManager.sceneCount;i++)EditorSceneManager.SaveScene(SceneManager.GetSceneAt(i),"Temp/Before-refine-"+DateTime.Now.ToString("HHmmss")+"-"+i+".unity",true);var s=SceneManager.GetSceneByPath("Assets/Scenes/JJinCity.unity");bool opened=!s.isLoaded;if(opened)s=EditorSceneManager.OpenScene("Assets/Scenes/JJinCity.unity",OpenSceneMode.Additive);SceneManager.SetActiveScene(s);EditorSceneManager.SaveScene(s,"Temp/JJinCity-before-refine.unity",true);
var w=s.GetRootGameObjects().First(g=>g.name=="Mok West Quarter");string report="";for(int x=-210;x<=-80;x+=5){NavMeshHit hit;report+=x+": "+NavMesh.SamplePosition(new Vector3(x,0,-5),out hit,2,NavMesh.AllAreas)+" "+hit.position+"\n";}
var link=w.GetComponentInChildren<NavMeshLink>();link.startPoint=new Vector3(-100,.1f,-5);link.UpdateLink();
foreach(var tm in w.GetComponentsInChildren<TextMesh>()){tm.transform.rotation=Quaternion.Euler(0,tm.text.Contains("오토바이 주차")?0:270,0);}
var metal=AssetDatabase.LoadAssetAtPath<Material>("Assets/Scripts/Mok/Art/Districts/Dark steel.mat");var glass=AssetDatabase.LoadAssetAtPath<Material>("Assets/Scripts/Mok/Art/Districts/Window dark.mat");var roof=AssetDatabase.LoadAssetAtPath<Material>("Assets/Scripts/Mok/Art/Districts/Weathered roof.mat");
foreach(var house in w.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("West house")).ToArray()){
var core=house.Find("House walls");var p=core.position;float h=core.localScale.y;float face=p.z< -5?1:-1;
foreach(var t in house.GetComponentsInChildren<Transform>().Where(t=>t.name=="SM_Bld_Apartment_01").ToArray()){var b=B(t.gameObject);t.RotateAround(b.center,Vector3.up,180);t.position+=new Vector3(0,0,face*.18f);PrefabUtility.RecordPrefabInstancePropertyModifications(t);}
// 앞뒤 골목에서 창문과 출입구가 읽히도록 외벽에 입체 디테일을 더한다.
foreach(int side in new[]{-1,1}){for(int floor=0;floor<h/3;floor++)foreach(float x in new[]{-4.5f,0,4.5f}){var q=new Vector3(p.x+x,floor*3+1.7f,p.z+side*7.55f);Box(house,"Alley window frame",q,new Vector3(1.8f,1.5f,.15f),metal);Box(house,"Alley window glass",q+new Vector3(0,0,side*.09f),new Vector3(1.5f,1.22f,.04f),glass);Box(house,"Window sill",q+new Vector3(0,-.8f,side*.16f),new Vector3(2,.13f,.45f),roof);}Box(house,"Alley door",new Vector3(p.x,1.05f,p.z+side*7.58f),new Vector3(1.2f,2.1f,.18f),metal);}
}
foreach(var t in w.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("SM_Bld_Shop_")).ToArray()){var b=B(t.gameObject);t.RotateAround(b.center,Vector3.up,180);PrefabUtility.RecordPrefabInstancePropertyModifications(t);}
foreach(var c in w.GetComponentsInChildren<Collider>(true))if(!c.enabled&&PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);
EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);SceneManager.SetActiveScene(old);if(opened)EditorSceneManager.CloseScene(s,true);File.WriteAllText("Temp/Mok-district-refine.txt",report+"Saved");}
}}
#endif
