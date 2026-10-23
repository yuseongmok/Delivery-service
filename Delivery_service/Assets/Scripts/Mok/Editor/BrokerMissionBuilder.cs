#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
namespace DeliveryService.Mok.Editor {
public static class BrokerMissionBuilder {
 public static void Tune(){var m=UnityEngine.Object.FindFirstObjectByType<BrokerMission>();if(!m)throw new Exception("Missing mission");EditorSceneManager.SaveScene(m.gameObject.scene,"Logs/JJinCity-before-broker-exit-"+DateTime.Now.ToString("HHmmss")+".unity",true);if(NavMesh.SamplePosition(m.exit.position+Vector3.forward*4,out var hit,3,NavMesh.AllAreas))m.exit.position=hit.position;EditorUtility.SetDirty(m);EditorSceneManager.SaveScene(m.gameObject.scene);File.WriteAllText("Logs/Broker-exit.txt",m.exit.position.ToString());}
 public static void Repair(){
  var m=UnityEngine.Object.FindFirstObjectByType<BrokerMission>();EditorSceneManager.SaveScene(m.gameObject.scene,"Logs/JJinCity-before-broker-space-"+DateTime.Now.ToString("HHmmss")+".unity",true);Physics.SyncTransforms();
  Vector3 Clear(Vector3 wish){for(float radius=0;radius<=16;radius+=1.5f)for(int a=0;a<16;a++){var v=wish+new Vector3(Mathf.Cos(a*Mathf.PI/8),0,Mathf.Sin(a*Mathf.PI/8))*radius;if(!NavMesh.SamplePosition(v,out var hit,1.5f,NavMesh.AllAreas))continue;if(Physics.OverlapCapsule(hit.position+Vector3.up*.5f,hit.position+Vector3.up*1.4f,.38f,~0,QueryTriggerInteraction.Ignore).Any(c=>!c.transform.IsChildOf(m.transform)))continue;var path=new NavMeshPath();if(NavMesh.CalculatePath(m.exit.position,hit.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete)return hit.position;}throw new Exception("No physically clear point near "+wish);}
  m.broker.position=m.meeting.position=Clear(m.meeting.position+Vector3.right*5.5f);
  var path=new NavMeshPath();NavMesh.CalculatePath(m.exit.position,m.meeting.position,NavMesh.AllAreas,path);
  for(int i=0;i<4;i++){var wish=i<2?m.meeting.position+Vector3.back*9+Vector3.right*(i==0?-3:3):path.corners[path.corners.Length/2]+Vector3.right*(i==2?-5:5);m.ambushPosts[i].position=Clear(wish);}
  EditorUtility.SetDirty(m);EditorSceneManager.SaveScene(m.gameObject.scene);File.WriteAllText("Logs/Broker-space.txt","meeting "+m.meeting.position+"\nposts "+string.Join(",",m.ambushPosts.Select(p=>p.position.ToString())));
 }
 public static void Install(){
  var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(StoryTutorialBuilder.Main);
  if(!scene.isLoaded)throw new Exception("Open JJinCity first");
  if(UnityEngine.Object.FindFirstObjectByType<BrokerMission>())throw new Exception("Already installed; preserve scene edits");
  if(!EditorSceneManager.SaveScene(scene,"Logs/JJinCity-before-broker-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new Exception("Backup failed");
  var tutorial=UnityEngine.Object.FindFirstObjectByType<StoryTutorial>();var day=UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();
  var view=day.pcUIPanel.GetComponent<NightPreparationView>();
  if(!NavMesh.SamplePosition(tutorial.outsideStand.position,out var start,5,NavMesh.AllAreas))throw new Exception("No safe exit NavMesh");
  // 기존 도시의 실제 완주 가능한 보행 경로에서 접선 지점을 고른다.
  var policy=UnityEngine.Object.FindFirstObjectByType<DistrictDeliveryPolicy>();Vector3 meeting=Vector3.zero;NavMeshPath selected=null;float best=float.MaxValue;
  foreach(var point in policy.oldCity){if(!point||!NavMesh.SamplePosition(point.transform.position,out var hit,8,NavMesh.AllAreas))continue;var path=new NavMeshPath();if(!NavMesh.CalculatePath(start.position,hit.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)continue;float length=0;for(int i=1;i<path.corners.Length;i++)length+=Vector3.Distance(path.corners[i-1],path.corners[i]);if(length<45||length>160)continue;float score=Mathf.Abs(length-90);if(score<best){best=score;meeting=hit.position;selected=path;}}
  if(selected==null)throw new Exception("No reachable 45–160m broker site");
  var root=new GameObject("Night Mission - Missing Broker");var m=root.AddComponent<BrokerMission>();m.day=day;m.player=UnityEngine.Object.FindFirstObjectByType<PlayerCombat>();m.orders=view.orders;
  Transform Point(string name,Vector3 p){var t=new GameObject(name).transform;t.SetParent(root.transform);t.position=p;return t;}
  m.meeting=Point("Broker meeting",meeting);m.exit=Point("Safe escape by shop",start.position);
  var actor=UnityEngine.Object.Instantiate(tutorial.actor.gameObject,root.transform);actor.name="Broker - saved actor";actor.SetActive(true);
  foreach(var c in actor.GetComponentsInChildren<MonoBehaviour>(true))UnityEngine.Object.DestroyImmediate(c);
  foreach(var c in actor.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
  var nav=actor.GetComponent<NavMeshAgent>();if(!nav)nav=actor.AddComponent<NavMeshAgent>();nav.enabled=true;nav.radius=.25f;nav.height=1.7f;nav.speed=m.followSpeed;nav.areaMask=NavMesh.AllAreas;nav.baseOffset=0;
  actor.transform.position=meeting;actor.transform.rotation=Quaternion.LookRotation((start.position-meeting).normalized);m.broker=actor.transform;
  var capsule=actor.AddComponent<CapsuleCollider>();capsule.height=1.7f;capsule.radius=.23f;capsule.center=Vector3.up*.85f;
  // 새 재질을 공유 에셋으로 저장하여 원본 민기의 옷은 변경하지 않는다.
  var renderer=actor.GetComponentsInChildren<SkinnedMeshRenderer>().FirstOrDefault();if(renderer&&renderer.sharedMaterial){var material=new Material(renderer.sharedMaterial);material.name="Broker Coat";if(material.HasProperty("_BaseColor"))material.SetColor("_BaseColor",new Color(.38f,.34f,.28f));AssetDatabase.CreateAsset(material,"Assets/Scripts/Mok/ShopUI/BrokerCoat.mat");renderer.sharedMaterial=material;}
  var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Scripts/Min/Prefab/NPC/Male_Adult_NPC_1.prefab");
  var enemy=UnityEngine.Object.Instantiate(prefab,root.transform);enemy.name="Mission pursuer template";enemy.SetActive(false);
  var combat=enemy.GetComponent<CitizenCombat>();if(!combat)combat=enemy.AddComponent<CitizenCombat>();combat.missionEnemy=true;combat.chaseSpeed=m.pursuitSpeed;combat.maximumHealth=100;m.enemyTemplate=combat;
  m.ambushPosts=new Transform[4];Vector3 direction=(meeting-selected.corners[Mathf.Max(0,selected.corners.Length-2)]).normalized;Vector3 side=Vector3.Cross(Vector3.up,direction);
  for(int i=0;i<4;i++){Vector3 wish=i<2?meeting+direction*10+side*(i==0?-5:5):selected.corners[selected.corners.Length/2]+side*(i==2?-7:7);if(!NavMesh.SamplePosition(wish,out var post,12,NavMesh.AllAreas))throw new Exception("No ambush post "+i);m.ambushPosts[i]=Point("Pursuer post "+i,post.position);}
  m.missionUI=new GameObject("Broker Mission UI",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));m.missionUI.transform.SetParent(root.transform,false);var canvas=m.missionUI.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=74;
  var scaler=m.missionUI.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);
  Text Label(string name,Transform parent,int size,float x,float y,float xx,float yy){var g=new GameObject(name,typeof(RectTransform),typeof(Text),typeof(Shadow));g.transform.SetParent(parent,false);var t=g.GetComponent<Text>();t.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/BMJUA_ttf.ttf");t.fontSize=size;t.color=new Color(1,.9f,.73f);t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;t.rectTransform.anchorMin=new Vector2(x,y);t.rectTransform.anchorMax=new Vector2(xx,yy);t.rectTransform.offsetMin=t.rectTransform.offsetMax=Vector2.zero;g.GetComponent<Shadow>().effectDistance=new Vector2(2,-2);return t;}
  m.objective=Label("Objective",m.missionUI.transform,25,.26f,.86f,.74f,.97f);m.subtitle=Label("Dialogue",m.missionUI.transform,28,.16f,.08f,.84f,.24f);m.marker=Label("Destination",m.missionUI.transform,22,.5f,.5f,.5f,.5f);m.marker.rectTransform.sizeDelta=new Vector2(260,70);m.missionUI.SetActive(false);
  view.brokerMission=m;var button=UnityEngine.Object.Instantiate(view.restButton,view.detailResult.transform.parent);button.name="Start broker mission";button.onClick=new Button.ButtonClickedEvent();var rect=(RectTransform)button.transform;rect.anchorMin=new Vector2(.055f,.025f);rect.anchorMax=new Vector2(.945f,.18f);rect.offsetMin=rect.offsetMax=Vector2.zero;
  view.startButton=button;view.startLabel=button.GetComponentInChildren<Text>();view.startLabel.text="미션 시작";
  view.detailResult.rectTransform.anchorMin=new Vector2(.055f,.20f);view.detailResult.rectTransform.anchorMax=new Vector2(.945f,.39f);view.detailResult.fontSize=20;
  EditorUtility.SetDirty(view);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
  File.WriteAllText("Logs/Broker-install.txt","PASS scene backup; PC start button; saved broker and pursuer template; complete walking path\nmeeting "+meeting+" exit "+start.position+"\nroute "+string.Join(" -> ",selected.corners.Select(p=>p.ToString())));
 }
}
}
#endif
