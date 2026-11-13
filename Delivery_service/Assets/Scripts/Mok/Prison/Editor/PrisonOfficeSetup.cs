#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad] public static class PrisonOfficeSetup {
static PrisonOfficeSetup(){EditorApplication.update+=Poll;}
static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/PrisonOffice-request"))return;var a=File.ReadAllText("Temp/PrisonOffice-request").Trim();File.Delete("Temp/PrisonOffice-request");try{if(a=="build")Build();else if(a=="test")PrisonOfficeCheck.Run();else if(a=="polish")Polish();else if(a=="acting")Acting();}catch(Exception e){File.WriteAllText("Logs/Prison-office.txt",e.ToString());Debug.LogException(e);}}
static void Acting(){
 var m=Object.FindFirstObjectByType<PrisonOfficeSearch>();
 EditorSceneManager.SaveScene(m.gameObject.scene,"Logs/Prison-before-office-acting.unity",true);
 var pickup=m.GetComponent<PrisonOfficePickup>()??m.gameObject.AddComponent<PrisonOfficePickup>();pickup.mission=m;
 var friend=m.GetComponent<PrisonOfficeCompanionSearch>()??m.gameObject.AddComponent<PrisonOfficeCompanionSearch>();friend.mission=m;
 foreach(var p in m.points){if(p.kind==PrisonOfficePoint.Kind.Password)continue;
 foreach(var c in p.contents.GetComponentsInChildren<Collider>())Object.DestroyImmediate(c);
 if(p.movingPart.Find("서랍 왼쪽 테두리"))continue;
 var mat=p.movingPart.GetComponentInChildren<Renderer>().sharedMaterial;
 for(int i=0;i<3;i++){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=i==0?"서랍 왼쪽 테두리":i==1?"서랍 오른쪽 테두리":"서랍 뒷판";g.transform.SetParent(p.movingPart,false);g.transform.localPosition=new Vector3(i==0?-.44f:i==1?.44f:0,-.07f,i==2?.20f:0);g.transform.localScale=i==2?new Vector3(.88f,.20f,.03f):new Vector3(.03f,.20f,.43f);g.GetComponent<Renderer>().sharedMaterial=mat;Object.DestroyImmediate(g.GetComponent<Collider>());}
 if(p.kind==PrisonOfficePoint.Kind.Uniforms){foreach(Transform shirt in p.contents.transform){var collar=GameObject.CreatePrimitive(PrimitiveType.Cube);collar.name="접힌 제복 깃";collar.transform.SetParent(shirt,false);collar.transform.localPosition=new Vector3(0,.52f,.22f);collar.transform.localScale=new Vector3(.35f,.12f,.25f);collar.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/DeliveryPrisonChapter/OfficeSearch/간수장 상아색 제복.mat");Object.DestroyImmediate(collar.GetComponent<Collider>());}}
 }
 EditorSceneManager.MarkSceneDirty(m.gameObject.scene);EditorSceneManager.SaveScene(m.gameObject.scene);File.WriteAllText("Logs/Prison-office-acting.txt","PASS saved drawer sides, clothing detail and search presentations");
}
static void Polish(){var m=Object.FindFirstObjectByType<PrisonOfficeSearch>();var scene=m.gameObject.scene;EditorSceneManager.SaveScene(scene,"Logs/Prison-before-office-polish.unity",true);m.passwordBoard.transform.rotation=Quaternion.identity;m.points[0].transform.position=new Vector3(-19.5f,3.75f,-23);foreach(var p in m.points){if(p.kind==PrisonOfficePoint.Kind.Password)continue;var label=new GameObject("보관함 이름");label.transform.SetParent(p.movingPart,false);label.transform.localPosition=new Vector3(0,.10f,-.29f);var t=label.AddComponent<TextMesh>();t.font=MokUIFont.Font;t.fontSize=48;t.characterSize=.018f;t.anchor=TextAnchor.MiddleCenter;t.text=p.kind==PrisonOfficePoint.Kind.Uniforms?"예비 간수복":"호송차량 열쇠";label.GetComponent<Renderer>().sharedMaterial=t.font.material;}EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);var cam=m.previous.camera;cam.transform.position=new Vector3(-24,5.6f,-26);cam.transform.LookAt(new Vector3(-23,4.8f,-21));PrisonChickenCheck.Capture(cam,"Logs/Prison-office-search.png");}
static void Build(){
 var previous=Object.FindFirstObjectByType<PrisonKeysMission>();var scene=previous.gameObject.scene;if(scene.name!="PrisonChapter")throw new Exception("감옥 씬에서 실행하세요.");if(Object.FindFirstObjectByType<PrisonOfficeSearch>())throw new Exception("이미 배치됨");
 EditorSceneManager.SaveScene(scene,"Logs/Prison-before-office-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
 const string folder="Assets/DeliveryPrisonChapter/OfficeSearch";Directory.CreateDirectory(folder);AssetDatabase.Refresh();
 var root=new GameObject("26_간수장실 조사").transform;var m=root.gameObject.AddComponent<PrisonOfficeSearch>();m.previous=previous;m.dialogue=ScriptableObject.CreateInstance<PrisonOfficeText>();AssetDatabase.CreateAsset(m.dialogue,folder+"/간수장실 조사 대사.asset");
 Material Mat(string n,Color c){var mat=new Material(Shader.Find("Universal Render Pipeline/Lit")){color=c};AssetDatabase.CreateAsset(mat,folder+"/"+n+".mat");return mat;}
 var metal=Mat("보관함",new Color(.19f,.25f,.28f));var blue=Mat("접힌 간수복",new Color(.14f,.24f,.42f));var paper=Mat("암구호 게시판",new Color(.85f,.80f,.66f));var gold=AssetDatabase.LoadAssetAtPath<Material>("Assets/DeliveryPrisonChapter/KeysMission/열쇠.mat");
 GameObject Box(string n,Transform parent,Vector3 p,Vector3 size,Material mat){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=mat;return g;}
 PrisonOfficePoint Chest(string n,Vector3 pos,PrisonOfficePoint.Kind kind){var g=new GameObject(n);g.transform.SetParent(root);g.transform.position=pos;var p=g.AddComponent<PrisonOfficePoint>();p.kind=kind;p.mission=m;
 Box("보관함 뒤판",g.transform,new Vector3(0,.45f,.22f),new Vector3(1,.9f,.08f),metal);Box("왼쪽 측판",g.transform,new Vector3(-.48f,.45f,0),new Vector3(.06f,.9f,.5f),metal);Box("오른쪽 측판",g.transform,new Vector3(.48f,.45f,0),new Vector3(.06f,.9f,.5f),metal);Box("상판",g.transform,new Vector3(0,.92f,0),new Vector3(1,.08f,.55f),metal);
 var drawer=new GameObject("열리는 서랍").transform;drawer.SetParent(g.transform,false);drawer.localPosition=Vector3.up*.62f;p.movingPart=drawer;Box("서랍 앞판",drawer,new Vector3(0,0,-.25f),new Vector3(.93f,.35f,.06f),metal);Box("서랍 바닥",drawer,new Vector3(0,-.17f,0),new Vector3(.9f,.04f,.45f),metal);Box("손잡이",drawer,new Vector3(0,.02f,-.30f),new Vector3(.3f,.04f,.04f),gold);
 p.contents=new GameObject("확보할 물건");p.contents.transform.SetParent(drawer,false);return p;}
 var uniform=Chest("예비 간수복 보관함",new Vector3(-22,3.75f,-21),PrisonOfficePoint.Kind.Uniforms);
 for(int i=0;i<2;i++)Box("접힌 간수복 "+(i+1),uniform.contents.transform,new Vector3((i-.5f)*.4f,-.09f,0),new Vector3(.34f,.13f,.32f),blue);
 var key=Chest("호송차량 열쇠 서랍",new Vector3(-25,3.75f,-21),PrisonOfficePoint.Kind.VehicleKey);
 var keyPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Key_01.prefab");var keyObj=(GameObject)PrefabUtility.InstantiatePrefab(keyPrefab,scene);keyObj.transform.SetParent(key.contents.transform,false);keyObj.transform.localPosition=Vector3.up*-.10f;var bounds=new Bounds(keyObj.transform.position,Vector3.zero);foreach(var r in keyObj.GetComponentsInChildren<Renderer>())bounds.Encapsulate(r.bounds);keyObj.transform.localScale*=.3f/Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z);
 var board=Box("오늘의 암구호",root,new Vector3(-27,5.05f,-20.22f),new Vector3(1.6f,.85f,.05f),paper);var code=board.AddComponent<PrisonOfficePoint>();code.kind=PrisonOfficePoint.Kind.Password;code.mission=m;
 var label=new GameObject("암구호 글씨");label.transform.SetParent(root);label.transform.position=board.transform.position+Vector3.back*.035f;label.transform.rotation=Quaternion.Euler(0,180,0);m.passwordBoard=label.AddComponent<TextMesh>();m.passwordBoard.font=MokUIFont.Font;m.passwordBoard.fontSize=60;m.passwordBoard.characterSize=.035f;m.passwordBoard.anchor=TextAnchor.MiddleCenter;m.passwordBoard.color=new Color(.08f,.12f,.15f);m.passwordBoard.text="오늘의 암구호\n"+m.dialogue.password;label.GetComponent<Renderer>().sharedMaterial=m.passwordBoard.font.material;
 m.points=new[]{uniform,key,code};Object.FindFirstObjectByType<PrisonSharedPresentation>().office=m;
 EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);File.WriteAllText("Logs/Prison-office.txt","PASS office props and editable dialogue saved");
 var cam=previous.camera;cam.transform.position=new Vector3(-24.5f,5.6f,-25);cam.transform.LookAt(new Vector3(-24.5f,4.8f,-21));PrisonChickenCheck.Capture(cam,"Logs/Prison-office-search.png");
}
}}
#endif
