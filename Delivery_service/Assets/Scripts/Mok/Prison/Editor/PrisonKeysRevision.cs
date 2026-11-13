#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad] public static class PrisonKeysRevision {
static PrisonKeysRevision(){EditorApplication.update+=Poll;}
static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/KeysRevision"))return;var a=File.ReadAllText("Temp/KeysRevision").Trim();File.Delete("Temp/KeysRevision");try{if(a=="inspect")Inspect();else if(a=="apply")Apply();}catch(Exception e){File.WriteAllText("Logs/KeysRevision.txt",e.ToString());}}
static void Apply(){
 var m=Object.FindFirstObjectByType<PrisonKeysMission>();var scene=m.gameObject.scene;if(scene.name!="PrisonChapter")throw new Exception("감옥 씬만 수정합니다.");
 EditorSceneManager.SaveScene(scene,"Logs/Prison-before-alley-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
 if(!m.transform.Find("골목 창고 배치 완료")){
  var delta=new Vector3(-9,0,0);
  foreach(var name in new[]{"창고 칸막이 왼쪽","창고 칸막이 오른쪽","창고 앞 벽 왼쪽","창고 앞 벽 오른쪽","식료품 창고 문 경첩","식료품 창고 자물쇠","창고 식료품 선반"})m.transform.Find(name).position+=delta;
  foreach(var key in m.keyLock.keyModels)key.transform.position+=delta;
  foreach(var item in m.supplies)item.transform.position+=delta;
  m.transform.Find("숨을 수 있는 식료품 상자").position=new Vector3(-46, .8f,9);
  m.checkpoint.position=new Vector3(-49,1.1f,-1);m.keyGuard.transform.position=new Vector3(-49,0,5);m.keyGuard.postPosition=m.keyGuard.transform.position;
  m.keyGuard.route[0].position=new Vector3(-49,0,2);m.keyGuard.route[1].position=new Vector3(-49,0,11);
  var wall=AssetDatabase.LoadAssetAtPath<Material>("Assets/DeliveryPrisonChapter/KeysMission/창고 벽.mat");
  Box(m.transform,"창고 뒤 벽",new Vector3(-48.5f,1.4f,17),new Vector3(7,2.8f,.12f),wall);
  Box(m.transform,"창고 지붕",new Vector3(-48.5f,2.85f,15),new Vector3(7.2f,.15f,4.2f),wall);
  new GameObject("골목 창고 배치 완료").transform.SetParent(m.transform);
 }
 // 조리대 실제 상판 위에 도시에서 쓰는 원본 트레이를 배치한다.
 int index=0;foreach(var supply in m.cookingSupplies.GetComponentsInChildren<PrisonPizzaSupply>(true)){
  supply.transform.position=new Vector3(28.8f+index*.83f,.94f,-27.63f);supply.transform.localScale=Vector3.one;
  supply.GetComponent<Renderer>().enabled=false;supply.GetComponent<BoxCollider>().size=new Vector3(.72f,.28f,.55f);
  supply.GetComponent<BoxCollider>().center=new Vector3(0,.13f,0);
  if(!supply.transform.Find("도시 재료 트레이")){var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Scripts/Mok/Resources/MokPizzaArt/Tray"+supply.ingredient.toppingType+".prefab");var tray=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);tray.name="도시 재료 트레이";tray.transform.SetParent(supply.transform,false);tray.transform.localScale=Vector3.one*1.3f;}
  var label=m.transform.Find(supply.ingredient.toppingName+" 안내");if(label)label.position=supply.transform.position+Vector3.up*.48f;index++;
 }
 m.transform.Find("피자 재료 폐기통").position=new Vector3(34.4f,.5f,-30.6f);
  for(int i=0;i<m.supplies.Length;i++){var item=m.supplies[i];item.GetComponent<Renderer>().enabled=false;item.transform.position=new Vector3(-50.3f+i*1.2f,.77f,16);item.transform.localScale=Vector3.one;item.GetComponent<BoxCollider>().size=new Vector3(.65f,.25f,.55f);item.GetComponent<BoxCollider>().center=Vector3.up*.12f;if(!item.transform.Find("도시 재료 트레이")){var p=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Scripts/Mok/Resources/MokPizzaArt/Tray"+m.ingredients[i].toppingType+".prefab");var tray=(GameObject)PrefabUtility.InstantiatePrefab(p,scene);tray.name="도시 재료 트레이";tray.transform.SetParent(item.transform,false);tray.transform.localScale=Vector3.one*1.3f;}}
 var space=m.GetComponent<PrisonMissionSpace>()??m.gameObject.AddComponent<PrisonMissionSpace>();space.population=Object.FindFirstObjectByType<PrisonPopulation>();space.missions=Object.FindFirstObjectByType<PrisonSharedPresentation>();
 if(!m.warden.transform.Find("염윤태 전용 외형")){
  SyntyStoryUpgrade.Skin(m.warden.transform,"Assets/Synty/PolygonCity/Prefabs/Characters/Character_BusinessMan_Suit.prefab","PrisonWardenYeom",false,true);
  var anim=m.warden.GetComponentInChildren<Animator>(true);var chest=anim.GetBoneTransform(HumanBodyBones.Chest);var marker=new GameObject("염윤태 전용 외형").transform;marker.SetParent(m.warden.transform,false);
  var gold=AssetDatabase.LoadAssetAtPath<Material>("Assets/DeliveryPrisonChapter/KeysMission/열쇠.mat");
  foreach(float side in new[]{-1f,1f}){var p=m.warden.transform.position+m.warden.transform.right*.23f*side+Vector3.up*1.42f;var badge=Box(chest,"간수장 금색 견장",p,new Vector3(.15f,.035f,.13f),gold);Object.DestroyImmediate(badge.GetComponent<Collider>());}
  m.warden.gun.gameObject.SetActive(false);
 }
 m.dialogue.start="민기: 의무실 건물 옆 골목에 창고가 있어. 간수가 등을 돌렸을 때 열쇠를 빼 오자. 나는 망을 볼게.";EditorUtility.SetDirty(m.dialogue);
 EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
 Inspect();File.AppendAllText("Logs/KeysRevision.txt","\nAPPLIED alley pantry / city trays / mission space / unique warden\n");
 var cam=m.camera;cam.transform.position=new Vector3(-49,2.3f,7);cam.transform.LookAt(new Vector3(-48.5f,1.2f,14));PrisonChickenCheck.Capture(cam,"Logs/Keys-alley.png");
 cam.transform.position=new Vector3(30,2.2f,-24.5f);cam.transform.LookAt(new Vector3(30,.95f,-27.63f));PrisonChickenCheck.Capture(cam,"Logs/Keys-trays.png");
 var t=m.warden.transform;bool active=t.gameObject.activeSelf;t.gameObject.SetActive(true);cam.transform.position=t.position+t.forward*2.8f+Vector3.up*1.25f;cam.transform.LookAt(t.position+Vector3.up*1.1f);PrisonChickenCheck.Capture(cam,"Logs/Keys-warden.png");t.gameObject.SetActive(active);
}
static GameObject Box(Transform parent,string name,Vector3 position,Vector3 size,Material material){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,true);g.transform.position=position;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=material;return g;}

static void Inspect(){var m=Object.FindFirstObjectByType<PrisonKeysMission>();var b=new System.Text.StringBuilder();foreach(var c in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))if(c.bounds.size.x>3||c.bounds.size.z>3)b.AppendLine(c.name+" "+c.bounds);foreach(var r in m.warden.GetComponentsInChildren<SkinnedMeshRenderer>(true))b.AppendLine("WARDEN "+r.name+" "+AssetDatabase.GetAssetPath(r.sharedMesh));foreach(var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))if(Vector3.Distance(r.bounds.center,new Vector3(30,1,-32.7f))<3)b.AppendLine("KITCHEN "+r.name+" parent="+r.transform.parent?.name+" "+r.bounds);File.WriteAllText("Logs/KeysRevision.txt",b.ToString());var c1=m.camera;c1.orthographic=true;c1.orthographicSize=58;c1.transform.position=new Vector3(0,90,0);c1.transform.rotation=Quaternion.Euler(90,0,0);PrisonChickenCheck.Capture(c1,"Logs/Prison-top.png");c1.orthographic=false;}
}}
#endif
