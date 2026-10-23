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
public static class SpecialOrderBuilder {
 const string Dir="Assets/Scripts/Mok/Art/StoryCharacters/";
 static Material Mat(string name,Color color){string path=Dir+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.color=color;m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*.18f);EditorUtility.SetDirty(m);return m;}
 public static void Preview(){
  var m=UnityEngine.Object.FindFirstObjectByType<SpecialOrderMission>();var go=new GameObject("Temporary reception preview");var cam=go.AddComponent<Camera>();var rt=new RenderTexture(1280,720,24);var prior=RenderTexture.active;
  try{cam.transform.position=m.approach.position+new Vector3(-3,2,-4);cam.transform.LookAt(m.attendant.position+Vector3.up*1.2f);cam.fieldOfView=55;cam.farClipPlane=500;cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();File.WriteAllBytes("Logs/Special-order-site.png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);}
  finally{RenderTexture.active=prior;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);}
 }
 public static void Install(){
  var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!=StoryTutorialBuilder.Main||EditorApplication.isPlaying)throw new Exception("Open JJinCity in edit mode");
  if(UnityEngine.Object.FindFirstObjectByType<SpecialOrderMission>())throw new Exception("Already installed; preserve existing scene");
  if(!EditorSceneManager.SaveScene(scene,"Logs/JJinCity-before-special-order-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new Exception("Backup failed");
  var broker=UnityEngine.Object.FindFirstObjectByType<BrokerMission>();var tutorial=UnityEngine.Object.FindFirstObjectByType<StoryTutorial>();var policy=UnityEngine.Object.FindFirstObjectByType<DistrictDeliveryPolicy>();Physics.SyncTransforms();
  // 기존 배달 지점 인근의 보행 가능한 빈 공간에 접수대를 놓는다. 도로와 건물을 재생성하지 않는다.
  Vector3 spot=Vector3.zero;float best=float.MaxValue;bool found=false;
  foreach(var point in policy.oldCity){if(!point)continue;for(int i=0;i<12;i++){
   Vector3 wish=point.transform.position+new Vector3(Mathf.Cos(i*Mathf.PI/6),0,Mathf.Sin(i*Mathf.PI/6))*4;
   if(!NavMesh.SamplePosition(wish,out var hit,2,NavMesh.AllAreas))continue;
   if(Physics.OverlapBox(hit.position+Vector3.up*1.5f,new Vector3(2.7f,1.3f,2.7f),Quaternion.identity,~0,QueryTriggerInteraction.Ignore).Any(c=>!c.isTrigger))continue;
   var path=new NavMeshPath();if(!NavMesh.CalculatePath(broker.exit.position,hit.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)continue;
   float length=0;for(int n=1;n<path.corners.Length;n++)length+=Vector3.Distance(path.corners[n-1],path.corners[n]);if(length<45||length>220||Vector3.Distance(hit.position,broker.meeting.position)<12)continue;
   float score=Mathf.Abs(length-100);if(score<best){best=score;spot=hit.position;found=true;}
  }}
  if(!found)throw new Exception("No reachable clear reception site");
  var root=new GameObject("Night Mission - Special Order");root.transform.position=spot;var m=root.AddComponent<SpecialOrderMission>();m.broker=broker;m.kitchen=tutorial;
  Transform Point(string name,Vector3 p){var t=new GameObject(name).transform;t.SetParent(root.transform);t.position=p;return t;}
  m.approach=Point("Reception approach",spot);m.returnPoint=Point("Mingi report position",tutorial.insideStand.position);
  m.sound=root.AddComponent<AudioSource>();m.sound.spatialBlend=1;m.sound.minDistance=2;m.sound.maxDistance=12;m.sound.playOnAwake=false;
  m.boxSound=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Scripts/Mok/Prologue/Art/PartnerPerformance/BagCloth.wav");m.paperSound=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Scripts/Mok/Prologue/Art/PartnerPerformance/CashRustle.wav");
  var actor=UnityEngine.Object.Instantiate(broker.broker.gameObject,root.transform);actor.name="Event catering coordinator";
  foreach(var c in actor.GetComponentsInChildren<MonoBehaviour>(true))UnityEngine.Object.DestroyImmediate(c);
  foreach(var nav in actor.GetComponentsInChildren<NavMeshAgent>(true))UnityEngine.Object.DestroyImmediate(nav);
  foreach(var t in actor.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Broker ")).ToArray())if(t)UnityEngine.Object.DestroyImmediate(t.gameObject);
  actor.transform.SetPositionAndRotation(spot+Vector3.forward*1.7f,Quaternion.Euler(0,180,0));actor.SetActive(true);m.attendant=actor.transform;
  var teal=Mat("Catering teal uniform",new Color(.12f,.47f,.49f));var dark=Mat("Catering navy",new Color(.08f,.12f,.16f));var cream=Mat("Catering paper",new Color(.94f,.86f,.67f));var orange=Mat("Catering orange",new Color(.95f,.42f,.1f));
  foreach(var renderer in actor.GetComponentsInChildren<SkinnedMeshRenderer>(true)){var mats=renderer.sharedMaterials;if(mats.Length>0)mats[0]=teal;renderer.sharedMaterials=mats;}
  GameObject Cube(string name,Vector3 position,Vector3 size,Material mat,bool solid=true){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(root.transform);g.transform.position=position;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=mat;if(!solid)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;}
  Cube("Catering counter",spot+new Vector3(0,.85f,.7f),new Vector3(2,.12f,.85f),dark);
  foreach(float x in new[]{-.8f,.8f})Cube("Counter leg",spot+new Vector3(x,.4f,.7f),new Vector3(.09f,.8f,.6f),dark);
  Cube("Orange reception stripe",spot+new Vector3(0,.8f,.26f),new Vector3(2,.13f,.03f),orange,false);
  Cube("Reception canopy",spot+new Vector3(0,2.75f,1.05f),new Vector3(3,.16f,2.2f),teal);
  foreach(float x in new[]{-1.3f,1.3f})Cube("Canopy upright",spot+new Vector3(x,1.35f,1.9f),new Vector3(.07f,2.7f,.07f),dark);
  var sign=new GameObject("Catering reception sign",typeof(TextMesh));sign.transform.SetParent(root.transform);sign.transform.SetPositionAndRotation(spot+new Vector3(0,2.5f,.02f),Quaternion.identity);var tm=sign.GetComponent<TextMesh>();tm.text="행사 납품 접수";tm.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/BMJUA_ttf.ttf");tm.fontSize=64;tm.characterSize=.055f;tm.anchor=TextAnchor.MiddleCenter;tm.color=new Color(1,.85f,.57f);sign.GetComponent<MeshRenderer>().sharedMaterial=tm.font.material;
  var light=new GameObject("Warm reception light",typeof(Light));light.transform.SetParent(root.transform);light.transform.position=spot+new Vector3(0,2.5f,.7f);var l=light.GetComponent<Light>();l.type=LightType.Point;l.range=6;l.intensity=2;l.color=new Color(1,.82f,.6f);l.shadows=LightShadows.None;
  var animator=actor.GetComponentInChildren<Animator>();animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
  var chest=animator.GetBoneTransform(HumanBodyBones.Chest)??animator.GetBoneTransform(HumanBodyBones.Spine);var badge=Cube("Catering staff badge",chest.position+actor.transform.forward*.16f,new Vector3(.12f,.17f,.025f),cream,false);badge.transform.SetParent(chest,true);badge.transform.rotation=actor.transform.rotation;
  m.boxRest=Point("Accepted pizza position",spot+new Vector3(-.35f,.92f,.7f));
  m.box=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Scripts/Mok/Resources/MokPizzaArt/Box.prefab"),root.transform);m.box.name="Visible sample handover";m.box.transform.position=m.boxRest.position;m.box.transform.localScale=Vector3.one*.55f;foreach(var c in m.box.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
  m.approvalPaper=Cube("Catering approval document",spot+new Vector3(.55f,.93f,.7f),new Vector3(.28f,.015f,.38f),cream,false);
  Cube("Document seal",spot+new Vector3(.57f,.94f,.58f),new Vector3(.08f,.008f,.07f),orange,false).transform.SetParent(m.approvalPaper.transform,true);
  m.ui=UnityEngine.Object.Instantiate(broker.missionUI,root.transform);m.ui.name="Special order story UI";m.objective=m.ui.transform.Find("Objective").GetComponent<Text>();m.subtitle=m.ui.transform.Find("Dialogue").GetComponent<Text>();m.marker=m.ui.transform.Find("Destination").GetComponent<Text>();m.objective.text=m.subtitle.text=m.marker.text="";
  // 기존 이야기 자막의 폰트와 화자별 색을 그대로 이어 쓴다.
  if(!m.subtitle.GetComponent<StorySpeakerColors>())m.subtitle.gameObject.AddComponent<StorySpeakerColors>();m.ui.SetActive(false);
  m.box.SetActive(false);m.approvalPaper.SetActive(false);
  var plan=AssetDatabase.LoadAssetAtPath<NightPreparationPlan>("Assets/Scripts/Mok/ShopUI/FirstChapterPreparation.asset");var entry=plan.missions.First(x=>x.id=="special_order");entry.description="실장이 소개한 행사 납품 담당자를 만나세요. 구운 치즈피자 한 판을 포장해 전달하고, 정식 납품 승인을 받아 민기에게 돌아오세요. 재료비는 평소와 같으며 제한 시간은 없습니다.";entry.result="행사 납품 명단 등록 · 서비스 출입구 이용 자격\n시식 납품 대금 5,000원";EditorUtility.SetDirty(plan);
  var inbox=broker.GetComponent<StoryPhoneInbox>();inbox.followup="실장이 납품 담당자에게 우리 가게를 소개했어. 박진웅의 행사에 들어갈 명분을 만들 기회야. 휴대폰의 ‘특별 주문’을 시작해서 담당자를 만나 봐. 피자 맛으로 정식 납품 명단에 이름을 올리는 거야.";EditorUtility.SetDirty(inbox);
  broker.meetingLines[2]="실장: 나도 버림받았어. 박진웅… 그 사람이 곧 비공개 행사에 나타나.";EditorUtility.SetDirty(broker);
  EditorUtility.SetDirty(m);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();File.WriteAllText("Logs/Special-order-install.txt","PASS saved reception, actor, props, UI and dialogue\nreception="+spot+" return="+m.returnPoint.position);
 }
}
}
#endif
