#if UNITY_EDITOR
using System;using System.IO;using System.Linq;
using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using UnityEngine.AI;using UnityEngine.UI;
namespace DeliveryService.Mok.Editor {
public static class BackdoorBuilder {
 const string Dir="Assets/Scripts/Mok/Art/StoryCharacters/";
 static Material Mat(string name,Color color){string path=Dir+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}mat.color=color;mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",color*.15f);EditorUtility.SetDirty(mat);return mat;}
 static bool Clear(Vector3 p,float radius=.45f)=>!Physics.OverlapCapsule(p+Vector3.up*.55f,p+Vector3.up*1.5f,radius,~0,QueryTriggerInteraction.Ignore).Any(c=>!c.isTrigger);
 static bool Point(Vector3 wish,out Vector3 point){point=default;if(!NavMesh.SamplePosition(wish,out var h,2,NavMesh.AllAreas)||!Clear(h.position))return false;point=h.position;return true;}
 static NavMeshPath Path(Vector3 a,Vector3 b){var path=new NavMeshPath();return NavMesh.CalculatePath(a,b,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete?path:null;}
 static float Length(NavMeshPath path){float total=0;for(int i=1;i<path.corners.Length;i++)total+=Vector3.Distance(path.corners[i-1],path.corners[i]);return total;}
 public static void Sightlines(){var m=UnityEngine.Object.FindFirstObjectByType<BackdoorMission>();Physics.SyncTransforms();var lines=new System.Collections.Generic.List<string>();for(int i=0;i<4;i++){var from=m.standPoints[i].position+Vector3.up*1.7f;var d=m.surveyTargets[i].position-from;lines.Add(i+" "+from+" -> "+m.surveyTargets[i].position+" "+string.Join("; ",Physics.RaycastAll(from,d.normalized,d.magnitude,~0,QueryTriggerInteraction.Ignore).Select(h=>h.collider.name+" "+h.point)));}File.WriteAllLines("Logs/Backdoor-sightlines.txt",lines);}
 public static void ReceptionNavigation(){
  var m=UnityEngine.Object.FindFirstObjectByType<SpecialOrderMission>();var scene=m.gameObject.scene;
  if(!EditorSceneManager.SaveScene(scene,"Logs/JJinCity-before-reception-navigation-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new Exception("Backup failed");
  var t=m.transform.Find("Reception navigation footprint");if(!t){t=new GameObject("Reception navigation footprint").transform;t.SetParent(m.transform);t.localPosition=new Vector3(0,1,1);}
  var obstacle=t.GetComponent<NavMeshObstacle>();if(!obstacle)obstacle=t.gameObject.AddComponent<NavMeshObstacle>();obstacle.shape=NavMeshObstacleShape.Box;obstacle.size=new Vector3(3.2f,2,2.2f);obstacle.carving=true;
  m.approach.position=m.transform.position+Vector3.back*.9f;EditorUtility.SetDirty(m);
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);File.WriteAllText("Logs/Backdoor-navigation.txt","PASS reception footprint registered for navigation");
 }
 public static void Refine(){
  var m=UnityEngine.Object.FindFirstObjectByType<BackdoorMission>();var scene=m.gameObject.scene;
  if(!EditorSceneManager.SaveScene(scene,"Logs/JJinCity-before-backdoor-route-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new Exception("Backup failed");
  var path=Path(m.standPoints[1].position,m.standPoints[3].position);if(path==null)throw new Exception("Escape route missing");Vector3 chosen=default;bool found=false;
  var from=m.standPoints[0].position+Vector3.up*1.4f;
  for(int i=1;i<path.corners.Length&&!found;i++){float length=Vector3.Distance(path.corners[i-1],path.corners[i]);for(float t=1;t<length;t+=1){var candidate=Vector3.Lerp(path.corners[i-1],path.corners[i],t/length);if(Vector3.Distance(candidate,m.standPoints[0].position)<20||Vector3.Distance(candidate,m.standPoints[3].position)<10||!Point(candidate,out var at))continue;var d=at+Vector3.up*1.4f-from;if(!Physics.RaycastAll(from,d.normalized,d.magnitude,~0,QueryTriggerInteraction.Ignore).Any(h=>h.collider.name.StartsWith("SM_Bld_")))continue;chosen=at;found=true;break;}}
  if(!found)throw new Exception("No building occluded corner found");m.standPoints[2].position=chosen;m.surveyTargets[2].position=chosen+Vector3.up*1.1f;m.transform.Find("Escape route ground stripe").position=chosen+Vector3.up*.035f;EditorUtility.SetDirty(m);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);File.WriteAllText("Logs/Backdoor-route.txt","PASS building occludes escape waypoint from service approach: "+chosen);
 }
 public static void Preview(){var m=UnityEngine.Object.FindFirstObjectByType<BackdoorMission>();var go=new GameObject("Temporary backdoor preview");var cam=go.AddComponent<Camera>();var rt=new RenderTexture(1280,720,24);var prior=RenderTexture.active;try{var d=(m.standPoints[0].position-m.surveyTargets[0].position);d.y=0;cam.transform.position=m.standPoints[0].position+d.normalized*3+Vector3.up*2;cam.transform.LookAt(m.surveyTargets[0].position);cam.fieldOfView=65;cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();File.WriteAllBytes("Logs/Backdoor-site.png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);}finally{RenderTexture.active=prior;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);}}
 public static void Inspect(){var d=UnityEngine.Object.FindFirstObjectByType<SpecialOrderMission>();var lines=new System.Collections.Generic.List<string>();for(int i=0;i<24;i++){var direction=new Vector3(Mathf.Cos(i*Mathf.PI/12),0,Mathf.Sin(i*Mathf.PI/12));foreach(var h in Physics.RaycastAll(d.approach.position+Vector3.up*1.5f,direction,45,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance).Take(3)){bool clear=Point(h.point+h.normal*4-Vector3.up*1.5f,out var at);lines.Add(i+" "+h.collider.name+" distance="+h.distance+" size="+h.collider.bounds.size+" normal="+h.normal+" point="+h.point+" stand="+clear+" "+at);}}File.WriteAllLines("Logs/Backdoor-facades.txt",lines);}
 public static void Install(){
  var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!=StoryTutorialBuilder.Main||EditorApplication.isPlaying)throw new Exception("Open JJinCity in edit mode");
  if(UnityEngine.Object.FindFirstObjectByType<BackdoorMission>())throw new Exception("Already installed");
  if(!EditorSceneManager.SaveScene(scene,"Logs/JJinCity-before-backdoor-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new Exception("Backup failed");
  var delivery=UnityEngine.Object.FindFirstObjectByType<SpecialOrderMission>();Physics.SyncTransforms();
  Vector3 gate=default,normal=default,stand=default,blind=default,postA=default,postB=default;bool found=false;float score=float.MaxValue;
  // 납품 접수대 근처의 실제 건물 벽과 연결된 보행 공간을 함께 찾는다.
  for(int i=0;i<72;i++){
   var direction=new Vector3(Mathf.Cos(i*Mathf.PI/36),0,Mathf.Sin(i*Mathf.PI/36));
   if(!Physics.Raycast(delivery.approach.position+Vector3.up*1.5f,direction,out var hit,42,~0,QueryTriggerInteraction.Ignore)||Mathf.Abs(hit.normal.y)>.15f||hit.collider.bounds.size.y<2.2f||!hit.collider.name.StartsWith("SM_Bld_")||hit.distance<3)continue;
   var n=new Vector3(hit.normal.x,0,hit.normal.z).normalized;var side=Vector3.Cross(Vector3.up,n);if(!Point(hit.point+n*4-Vector3.up*1.5f,out var at))continue;
   if(!Point(at+side*6,out var hidden)||!Point(at+side*2.4f,out var pa)||!Point(at-side*2.4f,out var pb))continue;
   if(Path(delivery.approach.position,at)==null||Path(at,hidden)==null||Path(pa,pb)==null)continue;
   float value=Mathf.Abs(hit.distance-14);if(value>=score)continue;score=value;gate=new Vector3(hit.point.x,at.y,hit.point.z)+n*.12f;normal=n;stand=at;blind=hidden;postA=pa;postB=pb;found=true;
  }
  if(!found)throw new Exception("No accessible service facade near reception");
  var policy=UnityEngine.Object.FindFirstObjectByType<DistrictDeliveryPolicy>();NavMeshPath route=null;Vector3 finish=default;score=float.MaxValue;
  foreach(var point in policy.oldCity){if(!point)continue;for(int a=0;a<8;a++){if(!Point(point.transform.position+new Vector3(Mathf.Cos(a*Mathf.PI/4),0,Mathf.Sin(a*Mathf.PI/4))*4,out var at))continue;var path=Path(blind,at);if(path==null)continue;float length=Length(path);if(length<45||length>170||Vector3.Distance(stand,at)<30||path.corners.Length<3)continue;float value=Mathf.Abs(length-85)-Mathf.Min(20,length-Vector3.Distance(blind,at));if(value<score){score=value;route=path;finish=at;}}}
  if(route==null)throw new Exception("No connected escape route");
  Vector3 alley=default;found=false;float travelled=0;
  for(int i=1;i<route.corners.Length;i++){float segment=Vector3.Distance(route.corners[i-1],route.corners[i]);for(float t=1;t<segment;t+=1){if(travelled+t<18||travelled+t>Length(route)-15)continue;var candidate=Vector3.Lerp(route.corners[i-1],route.corners[i],t/segment);if(Point(candidate,out alley)){found=true;break;}}if(found)break;travelled+=segment;}
  if(!found)throw new Exception("No clear escape waypoint");
  var root=new GameObject("Night Mission - City Backdoor");var m=root.AddComponent<BackdoorMission>();m.delivery=delivery;m.reportPoint=delivery.returnPoint;
  Transform Anchor(string name,Vector3 p){var t=new GameObject(name).transform;t.SetParent(root.transform);t.position=p;return t;}
  m.surveyTargets=new[]{Anchor("Service door record",gate+Vector3.up*1.5f+normal*.12f),Anchor("Blind spot record",blind+Vector3.up*1.1f),Anchor("Escape alley record",alley+Vector3.up*1.1f),Anchor("Vehicle rendezvous record",finish+Vector3.up*1.1f)};
  m.standPoints=new[]{Anchor("Service observation stand",stand),Anchor("Blind observation stand",blind+normal*1.5f),Anchor("Escape observation stand",alley),Anchor("Rendezvous stand",finish)};
  var dark=Mat("Backdoor metal",new Color(.10f,.16f,.20f));var orange=Mat("Backdoor orange",new Color(.9f,.42f,.12f));var cream=Mat("Backdoor lettering",new Color(.95f,.88f,.71f));var wood=Mat("Backdoor crates",new Color(.32f,.23f,.14f));var sideAxis=Vector3.Cross(Vector3.up,normal);var facing=Quaternion.LookRotation(normal);
  GameObject Cube(string name,Vector3 p,Vector3 size,Material mat,Quaternion rotation,bool solid=true){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(root.transform);g.transform.SetPositionAndRotation(p,rotation);g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=mat;if(!solid)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;}
  Cube("Event service door",gate+Vector3.up*1.2f,new Vector3(1.5f,2.4f,.12f),dark,facing);
  for(int i=-1;i<=1;i+=2)Cube("Service door jamb",gate+Vector3.up*1.3f+sideAxis*i*.85f,new Vector3(.12f,2.6f,.18f),orange,facing);
  Cube("Service door header",gate+Vector3.up*2.6f,new Vector3(1.82f,.12f,.18f),orange,facing);
  Cube("Push handle",gate+Vector3.up*1.05f+normal*.13f,new Vector3(1,.07f,.08f),cream,facing,false);
  void Sign(string name,string text,Vector3 p,Quaternion r,float size){var g=new GameObject(name,typeof(TextMesh));g.transform.SetParent(root.transform);g.transform.SetPositionAndRotation(p,r);var tm=g.GetComponent<TextMesh>();tm.text=text;tm.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/BMJUA_ttf.ttf");tm.fontSize=64;tm.characterSize=size;tm.anchor=TextAnchor.MiddleCenter;tm.color=new Color(1,.85f,.6f);g.GetComponent<MeshRenderer>().sharedMaterial=tm.font.material;}
  Sign("Service entrance sign","서비스 출입구\n승인 업체 전용",gate+Vector3.up*2.9f+normal*.15f,Quaternion.LookRotation(-normal),.027f);
  var cover=Cube("Loading crates cover",blind-sideAxis*1.15f+Vector3.up*.9f,new Vector3(1.4f,1.8f,1.5f),wood,facing);
  for(int i=-1;i<=1;i++)Cube("Crate band",cover.transform.position+Vector3.up*i*.55f,new Vector3(1.43f,.07f,1.53f),dark,facing,false).transform.SetParent(cover.transform,true);
  var obstacle=cover.AddComponent<NavMeshObstacle>();obstacle.shape=NavMeshObstacleShape.Box;obstacle.carving=true;obstacle.size=Vector3.one;
  Sign("Loading bay label","물품 적재 구역",blind+Vector3.up*1.7f,Quaternion.LookRotation(-normal),.023f);
  // 경로 표식은 작고 얇게 배치하여 보행과 오토바이 통행을 막지 않는다.
  Cube("Escape route ground stripe",alley+Vector3.up*.035f,new Vector3(1.3f,.015f,.12f),orange,Quaternion.identity,false);
  Cube("Rendezvous ground stripe",finish+Vector3.up*.035f,new Vector3(2,.015f,.15f),orange,Quaternion.identity,false);
  var lamp=new GameObject("Service doorway light",typeof(Light));lamp.transform.SetParent(root.transform);lamp.transform.position=gate+normal+Vector3.up*2.7f;var light=lamp.GetComponent<Light>();light.type=LightType.Point;light.range=7;light.intensity=1.8f;light.color=new Color(1,.82f,.57f);light.shadows=LightShadows.None;
  m.sound=root.AddComponent<AudioSource>();m.sound.spatialBlend=0;m.sound.playOnAwake=false;m.recordedSound=delivery.paperSound;m.warningSound=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Scripts/Mok/Prologue/Art/PartnerPerformance/VaultLatch.wav");
  var template=UnityEngine.Object.Instantiate(delivery.broker.enemyTemplate.gameObject,root.transform);template.name="Event security patrol template";template.SetActive(false);template.transform.position=postA;
  var combat=template.GetComponent<CitizenCombat>();combat.missionEnemy=true;combat.chaseSpeed=3.7f;combat.punchCooldown=2;combat.maximumHealth=100;
  foreach(var t in template.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Gang ")).ToArray())if(t)UnityEngine.Object.DestroyImmediate(t.gameObject);
  var security=Mat("Backdoor security blue",new Color(.12f,.22f,.35f));foreach(var renderer in template.GetComponentsInChildren<SkinnedMeshRenderer>(true)){var mats=renderer.sharedMaterials;if(mats.Length>0)mats[0]=security;renderer.sharedMaterials=mats;}
  var guard=template.AddComponent<BackdoorGuard>();guard.mission=m;guard.posts=new[]{Anchor("Security patrol A",postA),Anchor("Security patrol B",postB)};m.guards=new[]{guard};
  m.ui=UnityEngine.Object.Instantiate(delivery.ui,root.transform);m.ui.name="Backdoor mission UI";m.objective=m.ui.transform.Find("Objective").GetComponent<Text>();m.subtitle=m.ui.transform.Find("Dialogue").GetComponent<Text>();m.marker=m.ui.transform.Find("Destination").GetComponent<Text>();m.objective.text=m.subtitle.text=m.marker.text="";m.ui.SetActive(false);
  var plan=AssetDatabase.LoadAssetAtPath<NightPreparationPlan>("Assets/Scripts/Mok/ShopUI/FirstChapterPreparation.asset");var entry=plan.missions.First(x=>x.id=="backdoor");entry.description="납품 승인을 받은 뒤 민기와 상의하세요. 행사 서비스 출입구와 경비 사각지대를 조사하고 골목부터 차량 합류 지점까지 직접 이동해 탈출 동선을 확보하세요. 경비에게 들키면 시야를 끊고 조사를 이어갈 수 있습니다.";entry.result="서비스 출입구 · 사각지대 · 골목 탈출로 · 차량 합류 지점 확보";EditorUtility.SetDirty(plan);
  delivery.ending[2]="나: 다음은 뒷문인가.\n민기: 맞아. 승인서는 잘 챙겨. 휴대폰의 ‘도시의 뒷문’에서 현장 조사를 시작하자.";EditorUtility.SetDirty(delivery);
  EditorUtility.SetDirty(m);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();File.WriteAllText("Logs/Backdoor-install.txt","PASS saved service entrance, cover, patrol template, four objectives and connected route\n"+string.Join("\n",m.standPoints.Select(t=>t.name+" "+t.position))+"\nroute "+string.Join(" -> ",route.corners.Select(p=>p.ToString())));
 }
}
}
#endif
