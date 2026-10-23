#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace DeliveryService.Mok.Editor {
public static class StoryTutorialBuilder {
 public const string Main="Assets/Scenes/JJinCity.unity";
 public static void Polish(){
  var scene=SceneManager.GetSceneByPath(Main);if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(Main,OpenSceneMode.Additive);
  if(!EditorSceneManager.SaveScene(scene,"Logs/JJinCity-before-tutorial-polish-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new Exception("Backup failed");
  var t=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<StoryTutorial>(true)).First();
  t.pc=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ShopPC>(true)).First().transform;
  var shop=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshFilter>(true)).First(f=>f.sharedMesh&&f.sharedMesh.name=="ShopShell_LowPoly").transform;
  var floor=shop.Find("Foundation").GetComponent<Collider>().bounds.max.y;
  t.insideStand.position=new Vector3(t.insideStand.position.x,floor+.015f,t.insideStand.position.z);
  var agent=t.actor.GetComponent<NavMeshAgent>();if(agent)agent.enabled=false;
  var body=t.actor.GetComponent<CharacterController>();if(!body)body=t.actor.gameObject.AddComponent<CharacterController>();
  body.enabled=false;t.actor.position=t.insideStand.position;body.height=1.6f;body.center=new Vector3(0,.8f,0);body.radius=.2f;body.skinWidth=.015f;body.stepOffset=.25f;body.enabled=true;
  t.actor.LookAt(new Vector3(t.day.spawnPoint.position.x,t.actor.position.y,t.day.spawnPoint.position.z));
  if(t.guideRoute==null||t.guideRoute.Length==0){
   var route=new GameObject("Mingi saved doorway route").transform;route.SetParent(t.transform,false);
   var points=new[]{t.insideStand.position,shop.TransformPoint(new Vector3(3.22f,.015f,.8f)),shop.TransformPoint(new Vector3(3.22f,.015f,3.8f)),shop.TransformPoint(new Vector3(3.22f,.015f,5.3f)),t.outsideStand.position};
   t.guideRoute=points.Select((p,i)=>{var a=new GameObject("Walk "+i).transform;a.SetParent(route,false);a.position=p;return a;}).ToArray();
  }
  t.dialogue[0]="나: 어디서부터 하는데?\n민기: 앞치마부터. 컴퓨터를 보고 F를 눌러, 개점해 봐.";
  t.dialogue[2]="민기: F로 도우를 집어서 제작대에 올려. 컴퓨터는 F로 닫으면 돼. 재료는 내가 준비했어.";
  t.dialogue[12]="민기: 첫 손님이 별 다섯 개를 줬네. 지도 앱에서 매장을 골라 돌아와. 안에서 기다릴게.";
  EditorUtility.SetDirty(t);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
  File.WriteAllText("Logs/Tutorial-polish.txt","Saved physical floor height "+floor+", PC reference, doorway route and contextual dialogue.");
 }
 public static void Inspect(){
  var scene=SceneManager.GetSceneByPath(Main);if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(Main,OpenSceneMode.Additive);
  var t=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<StoryTutorial>(true)).First();
  Physics.SyncTransforms();var report=new System.Text.StringBuilder();
  report.AppendLine("DEST "+t.destination.transform.position);
  foreach(var c in t.destination.GetComponentsInChildren<Collider>(true))report.AppendLine("dest collider "+c.name+" "+c.bounds);
  var shell=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshFilter>(true)).First(f=>f.sharedMesh&&f.sharedMesh.name=="ShopShell_LowPoly").transform;
  report.AppendLine("SHOP "+shell.position+" rotation "+shell.eulerAngles);
  foreach(var pc in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ShopPC>(true)))report.AppendLine("PC "+pc.name+" "+pc.transform.position);
  for(float z=-1;z<=5;z+=1)for(float x=0;x<=3;x+=1){var p=shell.TransformPoint(new Vector3(x,0,z));var blocks=Physics.OverlapCapsule(p+Vector3.up*.35f,p+Vector3.up*1.4f,.23f,~0,QueryTriggerInteraction.Ignore);report.AppendLine("route "+x+","+z+" "+p+": "+string.Join(",",blocks.Select(c=>c.name)));}
  foreach(var p in new[]{t.actor,t.insideStand,t.outsideStand,t.maker.transform,t.day.spawnPoint,t.pc}){
   report.AppendLine(p.name+" pos "+p.position.ToString("F3"));
   if(NavMesh.SamplePosition(p.position,out var nav,4,NavMesh.AllAreas))report.AppendLine(" nav "+nav.position.ToString("F3"));
   foreach(var h in Physics.RaycastAll(p.position+Vector3.up*2,Vector3.down,8,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance))report.AppendLine(" floor "+h.collider.name+" "+h.point.ToString("F3"));
  }
  foreach(var r in t.actor.GetComponentsInChildren<SkinnedMeshRenderer>()){
   var mesh=new Mesh();r.BakeMesh(mesh);report.AppendLine(r.name+" actual feet "+mesh.vertices.Min(v=>r.transform.TransformPoint(v).y));UnityEngine.Object.DestroyImmediate(mesh);
  }
  File.WriteAllText("Logs/Tutorial-inspect.txt",report.ToString());
 }
 public static void Tune(){
  var t=UnityEngine.Object.FindFirstObjectByType<StoryTutorial>();if(!t)throw new Exception("Open main scene");
  EditorSceneManager.SaveScene(t.gameObject.scene,"Logs/JJinCity-before-tutorial-links-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
  var list=new SerializedObject(t.orders).FindProperty("deliveryPoints");var policy=t.orders.GetComponent<DistrictDeliveryPolicy>();
  t.destination=Enumerable.Range(0,list.arraySize).Select(i=>list.GetArrayElementAtIndex(i).objectReferenceValue as DeliveryPoint)
   .Where(p=>p&&p.gameObject.activeInHierarchy&&(policy==null||policy.Region(p)=="기존 도시"))
   .OrderBy(p=>NavigationManager.TryMeasureTravelDistance(t.maker.transform.position,p.transform.position,out var meters)?meters:float.MaxValue).First();
  foreach(var view in UnityEngine.Object.FindObjectsByType<DeliveryService.Yun.Achievements.AchievementPanelView>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
   var first=view.transform.Find("First Pizza") as RectTransform;if(!first||view.transform.Find("First Honest Income"))continue;
   first.localScale=Vector3.one*.78f;first.anchoredPosition=new Vector2(0,30);
   var second=UnityEngine.Object.Instantiate(first,view.transform);second.name="First Honest Income";second.anchoredPosition=new Vector2(0,-105);
   second.Find("Name").GetComponent<Text>().text="첫 정당한 수입";
   second.Find("Description").GetComponent<Text>().text="민기와 첫 배달을 마치고 가게로 돌아오세요.";
   second.Find("Reward").GetComponent<Text>().text="첫 영업 튜토리얼 완료";
   var serialized=new SerializedObject(view);serialized.FindProperty("firstDeliveryState").objectReferenceValue=second.Find("State").GetComponent<Text>();serialized.ApplyModifiedPropertiesWithoutUndo();
   var back=view.transform.Find("Back To Settings") as RectTransform;if(back)back.anchoredPosition=new Vector2(back.anchoredPosition.x,-220);
  }
  EditorUtility.SetDirty(t);EditorSceneManager.SaveScene(t.gameObject.scene);AssetDatabase.SaveAssets();
  File.WriteAllText("Logs/StoryTutorial-build.txt","PASS: registered nearby destination "+t.destination.DeliveryPointID+" and first-income achievement card saved");
 }
 public static void Build(){
  foreach(var scene in Enumerable.Range(0,SceneManager.sceneCount).Select(SceneManager.GetSceneAt))
   if(scene.isDirty&&!EditorSceneManager.SaveScene(scene,"Logs/Tutorial-before-"+scene.name+"-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new Exception("Scene backup failed");
  var bank=UnityEngine.Object.FindFirstObjectByType<BankPrologue>();
  if(!bank)throw new Exception("Open BankPrologue before installation");
  var sceneMain=SceneManager.GetSceneByPath(Main);if(!sceneMain.isLoaded)sceneMain=EditorSceneManager.OpenScene(Main,OpenSceneMode.Additive);
  if(!EditorSceneManager.SaveScene(sceneMain,"Logs/JJinCity-before-tutorial-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new Exception("Scene backup failed");
  T[] All<T>() where T:Component=>sceneMain.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();
  if(All<StoryTutorial>().Length>0)throw new Exception("Tutorial already installed; preserve saved edits");
  SceneManager.SetActiveScene(sceneMain);
  var root=new GameObject("Story tutorial - first honest income");var t=root.AddComponent<StoryTutorial>();
  t.orders=All<OrderManager>().First();t.day=All<DayNightCycle>().First();t.inventory=t.day.player.GetComponent<ToppingInventory>();
  t.maker=All<PizzaMaker>().First();t.oven=All<Oven>().First();t.packing=All<PizzaPacking>().First();t.pickup=All<PizzaPickUpZone>().First();
  t.cargo=All<BikePizzaStorage>().First();t.navigation=All<NavigationManager>().First();
  t.pc=All<MonoBehaviour>().FirstOrDefault(c=>c.GetType().Name=="Counter")?.transform;
  if(!t.pc)t.pc=t.maker.transform;
  Topping Material(ToppingType type)=>All<Topping>().First(x=>((PizzaToppingData)new SerializedObject(x).FindProperty("toppingData").objectReferenceValue).toppingType==type);
  t.dough=Material(ToppingType.Dough);t.sauce=Material(ToppingType.Sauce);t.cheese=Material(ToppingType.Cheese);
  var policy=t.orders.GetComponent<DistrictDeliveryPolicy>();
  t.destination=(policy!=null?policy.oldCity.ToArray():All<DeliveryPoint>()).Where(p=>p&&p.gameObject.activeInHierarchy)
   .Select(p=>new {point=p,meters=NavigationManager.TryMeasureTravelDistance(t.maker.transform.position,p.transform.position,out var distance)?distance:float.MaxValue})
   .Where(p=>p.meters>20&&p.meters<float.MaxValue).OrderBy(p=>p.meters).Select(p=>p.point).FirstOrDefault();
  if(!t.destination)throw new Exception("No reachable nearby delivery point");
  Transform Point(string name,Vector3 position){var g=new GameObject(name);g.transform.SetParent(root.transform);g.transform.position=position;return g.transform;}
  var inside=t.maker.transform.position+(t.day.spawnPoint.position-t.maker.transform.position).normalized*1.8f;inside.y=t.day.spawnPoint.position.y;
  if(NavMesh.SamplePosition(inside,out var hit,4,NavMesh.AllAreas))inside=hit.position;
  t.insideStand=Point("Mingi inside",inside);t.outsideStand=Point("Mingi by bike",t.cargo.transform.position+t.cargo.transform.right*2);
  if(NavMesh.SamplePosition(t.outsideStand.position,out hit,4,NavMesh.AllAreas))t.outsideStand.position=hit.position;
  var actor=UnityEngine.Object.Instantiate(bank.shopArrival.actor.gameObject,root.transform);actor.name="Mingi - tutorial guide";
  foreach(var c in actor.GetComponentsInChildren<MonoBehaviour>(true))UnityEngine.Object.DestroyImmediate(c);
  foreach(var c in actor.GetComponentsInChildren<Joint>(true))UnityEngine.Object.DestroyImmediate(c);
  foreach(var c in actor.GetComponentsInChildren<Rigidbody>(true))UnityEngine.Object.DestroyImmediate(c);
  foreach(var c in actor.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
  t.actor=actor.transform;actor.SetActive(true);actor.transform.SetPositionAndRotation(inside,t.maker.transform.rotation);
  var animator=actor.GetComponentInChildren<Animator>();animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
  var agent=actor.AddComponent<NavMeshAgent>();agent.radius=.25f;agent.height=1.7f;agent.speed=2.4f;agent.stoppingDistance=.4f;
  t.ui=new GameObject("Tutorial UI",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));t.ui.transform.SetParent(root.transform);
  var canvas=t.ui.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=75;
  var scaler=t.ui.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);
  Text Label(string name,Vector2 min,Vector2 max,int size){var g=new GameObject(name,typeof(RectTransform),typeof(Text),typeof(Shadow));g.transform.SetParent(t.ui.transform,false);var rect=g.GetComponent<RectTransform>();rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;var label=g.GetComponent<Text>();label.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/BMJUA_ttf.ttf");label.fontSize=size;label.alignment=TextAnchor.MiddleCenter;label.color=new Color(1,.9f,.73f);label.raycastTarget=false;g.GetComponent<Shadow>().effectDistance=new Vector2(2,-2);return label;}
  t.objective=Label("Current lesson",new Vector2(.28f,.9f),new Vector2(.72f,.97f),25);
  t.subtitle=Label("Story dialogue",new Vector2(.15f,.1f),new Vector2(.85f,.22f),28);
  t.marker=Label("Target",new Vector2(.5f,.5f),new Vector2(.5f,.5f),22);t.marker.rectTransform.sizeDelta=new Vector2(180,50);
  EditorUtility.SetDirty(t);EditorSceneManager.SaveScene(sceneMain);AssetDatabase.SaveAssets();
  File.WriteAllText("Logs/StoryTutorial-build.txt",$"PASS saved guide, UI, stations and nearby reachable delivery {t.destination.DeliveryPointID}; scene backup saved.");
 }
}
}
#endif
