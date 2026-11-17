#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using Object=UnityEngine.Object;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad] public static class PrisonWarehousePolish {
static PrisonWarehousePolish(){EditorApplication.update+=Poll;}
static void Poll(){const string f="Temp/WarehousePolish";if(EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(f))return;var mode=File.ReadAllText(f).Trim();File.Delete(f);try{if(EditorSceneManager.GetActiveScene().name!="PrisonChapter")throw new Exception("Prison scene only");if(mode=="build")Build();else if(mode=="finish")Finish();else if(mode=="detail")Detail();else if(mode=="cover")Cover();else Inspect();}catch(Exception e){File.WriteAllText("Logs/WarehousePolish.txt",e.ToString());}}
static void Build(){
var m=Object.FindFirstObjectByType<PrisonKeysMission>();var scene=m.gameObject.scene;
if(m.transform.Find("골목 끝 식료품 창고 마감"))throw new Exception("Already polished");
EditorSceneManager.SaveScene(scene,"Logs/Prison-before-warehouse-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
var root=new GameObject("골목 끝 식료품 창고 마감").transform;root.SetParent(m.transform,false);
string folder="Assets/DeliveryPrisonChapter/KeysMission";
Material Mat(string name,Color c){var path=folder+"/"+name+".mat";var a=AssetDatabase.LoadAssetAtPath<Material>(path);if(!a){a=new Material(Shader.Find("Universal Render Pipeline/Lit"));a.color=c;a.SetFloat("_Smoothness",.12f);AssetDatabase.CreateAsset(a,path);}return a;}
var trim=Mat("창고 짙은 금속 마감",new Color(.12f,.18f,.19f));var concrete=Mat("창고 기단",new Color(.37f,.39f,.36f));var amber=Mat("창고 황동 표식",new Color(.69f,.51f,.22f));
Transform Box(string n,Vector3 p,Vector3 sz,Material mat,Transform parent=null){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(parent?parent:root,true);g.transform.position=p;g.transform.localScale=sz;g.GetComponent<Renderer>().sharedMaterial=mat;Object.DestroyImmediate(g.GetComponent<Collider>());return g.transform;}
Transform Fit(string path,string n,Vector3 p,Vector3 sz,float yaw=0){var g=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));g.name=n;g.transform.SetParent(root,false);g.transform.rotation=Quaternion.Euler(0,yaw,0);var rr=g.GetComponentsInChildren<Renderer>();var b=rr[0].bounds;foreach(var r in rr)b.Encapsulate(r.bounds);g.transform.localScale=new Vector3(sz.x/Mathf.Max(.01f,b.size.x),sz.y/Mathf.Max(.01f,b.size.y),sz.z/Mathf.Max(.01f,b.size.z));b=rr[0].bounds;foreach(var r in rr)b.Encapsulate(r.bounds);g.transform.position+=p-b.center;foreach(var c in g.GetComponentsInChildren<Collider>())Object.DestroyImmediate(c);return g.transform;}
const string wall="Assets/Synty/PolygonHeist/Prefab/Environment/SM_Env_Wall_Exterior_01.prefab";
foreach(string n in new[]{"창고 칸막이 왼쪽","창고 칸막이 오른쪽","창고 앞 벽 왼쪽","창고 앞 벽 오른쪽","창고 뒤 벽"}){
var old=m.transform.Find(n);var b=old.GetComponent<Renderer>().bounds;old.GetComponent<Renderer>().enabled=false;
bool side=b.size.z>b.size.x;int count=side?2:n=="창고 뒤 벽"?3:1;
for(int i=0;i<count;i++){var size=side?new Vector3(.20f,2.8f,b.size.z/count):new Vector3(b.size.x/count,2.8f,.20f);var p=b.center+(side?Vector3.forward:Vector3.right)*((i+.5f)/count-.5f)*(side?b.size.z:b.size.x);Fit(wall,n+" Synty "+i,p,size,side?90:0);}
}
m.transform.Find("창고 지붕").GetComponent<Renderer>().sharedMaterial=trim;
Box("문 위 인방",new Vector3(-48.5f,2.72f,13),new Vector3(1.8f,.18f,.24f),trim);
foreach(float x in new[]{-52.05f,-49.35f,-47.65f,-44.95f})Box("정면 금속 기둥",new Vector3(x,1.4f,12.86f),new Vector3(.12f,2.8f,.18f),trim);
foreach(float x in new[]{-52f,-45f})Box("벽 기단",new Vector3(x,.14f,15),new Vector3(.28f,.28f,4.15f),concrete);
Box("후면 기단",new Vector3(-48.5f,.14f,17),new Vector3(7.2f,.28f,.28f),concrete);
foreach(float z in new[]{12.8f,17.2f})Box("지붕 처마",new Vector3(-48.5f,2.9f,z),new Vector3(7.5f,.20f,.18f),trim);
for(int i=0;i<16;i++)Box("금속 지붕 이음",new Vector3(-52+i*.47f,2.96f,15),new Vector3(.035f,.06f,4.4f),concrete);
var door=m.keyLock.door;
foreach(float x in new[]{-49.23f,-47.77f})Box("문 테두리",new Vector3(x,1.3f,12.91f),new Vector3(.07f,2.5f,.07f),trim,door);
foreach(float y in new[]{.12f,1.95f,2.48f})Box("문 보강대",new Vector3(-48.5f,y,12.91f),new Vector3(1.45f,.065f,.08f),trim,door);
foreach(float y in new[]{.45f,2.1f})Box("철문 경첩",new Vector3(-49.29f,y,12.86f),new Vector3(.12f,.22f,.12f),amber,door);
Box("철문 손잡이",new Vector3(-47.95f,1.45f,12.80f),new Vector3(.06f,.24f,.08f),trim,door);
Fit("Assets/Synty/PolygonHeist/Prefab/Props/SM_Prop_AirVent_01.prefab","창고 환기구",new Vector3(-50.7f,2.15f,12.76f),new Vector3(1.05f,.55f,.18f));
Fit("Assets/Synty/PolygonCity/Prefabs/Props/SM_Prop_Pallet_01.prefab","창고 납품 팔레트",new Vector3(-46,.12f,9),new Vector3(1.5f,.22f,1.4f));
var cover=m.transform.Find("숨을 수 있는 식료품 상자");cover.GetComponent<Renderer>().enabled=false;
Fit("Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Crate_Preset_01.prefab","숨는 적재 상자",new Vector3(-46,.91f,9),new Vector3(1.22f,1.6f,1.22f));
for(int i=0;i<4;i++)Box("선반 받침",new Vector3(-50.8f+i*1.5f,.35f,16),new Vector3(.07f,.7f,.7f),trim);
var lightGo=new GameObject("창고 입구 조명");lightGo.transform.SetParent(root);lightGo.transform.position=new Vector3(-48.5f,2.45f,12.4f);var l=lightGo.AddComponent<Light>();l.type=LightType.Point;l.range=5;l.intensity=1.2f;l.color=new Color(1,.83f,.58f);l.shadows=LightShadows.None;
Box("조명 차양",new Vector3(-48.5f,2.5f,12.55f),new Vector3(.6f,.12f,.38f),trim);
var turn=Quaternion.Euler(0,180,0);Vector3 origin=new Vector3(-48.5f,0,15),target=new Vector3(-48.5f,0,-30);
var names=new[]{"창고 칸막이 왼쪽","창고 칸막이 오른쪽","창고 앞 벽 왼쪽","창고 앞 벽 오른쪽","창고 뒤 벽","창고 지붕","창고 식료품 선반","숨을 수 있는 식료품 상자"};
var moved=names.Select(n=>m.transform.Find(n)).Concat(new[]{m.keyLock.door,m.keyLock.transform,m.checkpoint,m.keyGuard.transform}).Concat(m.keyGuard.route).Concat(m.keyLock.keyModels.Select(g=>g.transform)).Concat(m.supplies.Select(g=>g.transform)).Concat(root.Cast<Transform>()).Distinct().ToArray();
foreach(var t in moved){if(t.IsChildOf(door)&&t!=door)continue;t.SetPositionAndRotation(target+turn*(t.position-origin),turn*t.rotation);}
EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);File.WriteAllText("Logs/WarehousePolish.txt","PASS saved prison only; backup before edits; warehouse front "+m.keyLock.transform.position+"; guard "+m.keyGuard.transform.position);
Capture(m);
}
static void Cover(){
var m=Object.FindFirstObjectByType<PrisonKeysMission>();var scene=m.gameObject.scene;EditorSceneManager.SaveScene(scene,"Logs/Prison-before-alley-cover-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
var root=m.transform.Find("골목 끝 식료품 창고 마감");
if(root.Find("골목 접근 엄폐 상자 1"))throw new Exception("Cover already placed");
var source=root.Find("숨는 적재 상자");var pallet=root.Find("창고 납품 팔레트");
var positions=new[]{new Vector3(-51.7f,.91f,-10),new Vector3(-45.8f,.91f,-17),new Vector3(-51.7f,.91f,-20.5f)};
for(int i=0;i<positions.Length;i++){
var g=Object.Instantiate(source.gameObject,root);g.name="골목 접근 엄폐 상자 "+(i+1);g.transform.position=positions[i];
var pp=Object.Instantiate(pallet.gameObject,root);pp.name="골목 팔레트 "+(i+1);pp.transform.position=new Vector3(positions[i].x,.12f,positions[i].z);
var solid=new GameObject("엄폐 충돌 "+(i+1));solid.transform.SetParent(g.transform,true);solid.transform.position=positions[i];solid.transform.rotation=Quaternion.identity;solid.transform.localScale=new Vector3(1/g.transform.lossyScale.x,1/g.transform.lossyScale.y,1/g.transform.lossyScale.z);
var col=solid.AddComponent<BoxCollider>();col.size=new Vector3(1.22f,1.6f,1.22f);
var obstacle=solid.AddComponent<UnityEngine.AI.NavMeshObstacle>();obstacle.carving=true;obstacle.shape=UnityEngine.AI.NavMeshObstacleShape.Box;obstacle.size=col.size;
}
m.keyGuard.sightRange=14;m.keyGuard.sightAngle=90;m.approachNoticeSeconds=1;m.closeApproachNoticeSeconds=.65f;
EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Capture(m);File.WriteAllText("Logs/WarehousePolish.txt","PASS 3 additional cover stacks; front sight 90 degrees / 14m; approach exposure 1s, close .65s; saved prison only");
}
static void Detail(){var m=Object.FindFirstObjectByType<PrisonKeysMission>();var root=m.transform.Find("골목 끝 식료품 창고 마감");if(!root.Find("식료품 창고 간판")){
var mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/DeliveryPrisonChapter/KeysMission/창고 짙은 금속 마감.mat");
void Part(string n,Vector3 p,Vector3 scale){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(root,true);g.transform.position=p;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=mat;Object.DestroyImmediate(g.GetComponent<Collider>());}
Part("식료품 창고 간판",new Vector3(-48.5f,3.18f,-27.88f),new Vector3(2.6f,.46f,.12f));
for(int i=0;i<2;i++)Part("간판 지지대",new Vector3(-49.45f+i*1.9f,2.96f,-27.95f),new Vector3(.06f,.5f,.06f));
var text=new GameObject("식료품 창고 표기").AddComponent<TMPro.TextMeshPro>();text.transform.SetParent(root,true);text.transform.position=new Vector3(-48.5f,3.18f,-27.80f);text.transform.rotation=Quaternion.Euler(0,180,0);text.rectTransform.sizeDelta=new Vector2(2.4f,.35f);text.font=AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/Fonts/Jua_SDF.asset");text.text="식료품 창고  03";text.fontSize=2.5f;text.alignment=TMPro.TextAlignmentOptions.Center;text.color=new Color(.93f,.86f,.67f);
foreach(float x in new[]{-51.4f,-50.65f,-46.35f,-45.6f})Part("외벽 세로 이음",new Vector3(x,1.42f,-27.87f),new Vector3(.035f,2.6f,.035f));
var vent=root.Find("창고 환기구");if(vent)vent.Rotate(0,180,0,Space.Self);
}EditorSceneManager.MarkSceneDirty(m.gameObject.scene);EditorSceneManager.SaveScene(m.gameObject.scene);Capture(m);File.WriteAllText("Logs/WarehousePolish.txt","PASS facade details saved");}
static void Finish(){var m=Object.FindFirstObjectByType<PrisonKeysMission>();foreach(var t in m.transform.Find("골목 끝 식료품 창고 마감").Cast<Transform>().Where(t=>t.name.Contains("칸막이")&&t.name.Contains("Synty"))){var rr=t.GetComponentsInChildren<Renderer>();var b=rr[0].bounds;foreach(var r in rr)b.Encapsulate(r.bounds);var center=b.center;var sc=t.localScale;sc.x*=2/b.size.z;sc.z*=.2f/b.size.x;t.localScale=sc;b=rr[0].bounds;foreach(var r in rr)b.Encapsulate(r.bounds);t.position+=center-b.center;}EditorSceneManager.MarkSceneDirty(m.gameObject.scene);EditorSceneManager.SaveScene(m.gameObject.scene);Capture(m);File.WriteAllText("Logs/WarehousePolish.txt","PASS: warehouse saved at alley end; lock="+m.keyLock.transform.position+"; guard="+m.keyGuard.transform.position);}
static void Capture(PrisonKeysMission m){GameObject go=null;RenderTexture rt=null;Texture2D tex=null;var prev=RenderTexture.active;try{go=new GameObject("Warehouse still camera"){hideFlags=HideFlags.HideAndDontSave};var c=go.AddComponent<Camera>();c.enabled=false;c.nearClipPlane=.05f;c.fieldOfView=65;c.cullingMask=m.previous.prison.player.GetComponentsInChildren<Camera>(true).First().cullingMask;go.transform.position=new Vector3(-48.5f,2,-19);go.transform.LookAt(new Vector3(-48.5f,1.5f,-30));rt=new RenderTexture(1280,720,24);c.targetTexture=rt;c.Render();RenderTexture.active=rt;tex=new Texture2D(1280,720,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();File.WriteAllBytes("Logs/Warehouse-front.png",tex.EncodeToPNG());}finally{RenderTexture.active=prev;if(go)Object.DestroyImmediate(go);if(rt){rt.Release();Object.DestroyImmediate(rt);}if(tex)Object.DestroyImmediate(tex);}}

static void Inspect(){var m=Object.FindFirstObjectByType<PrisonKeysMission>();var s="";foreach(Transform t in m.transform)s+=t.name+" "+t.position+" scale="+t.lossyScale+"\n";foreach(var c in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None)){var b=c.bounds;if(b.center.x < -40 && b.size.y>1 && !c.isTrigger)s+="COL "+c.name+" "+b+"\n";}File.WriteAllText("Logs/WarehousePolish.txt",s);}
}}
#endif
