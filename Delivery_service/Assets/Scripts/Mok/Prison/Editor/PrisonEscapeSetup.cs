#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad] public static class PrisonEscapeSetup {
static PrisonEscapeSetup(){EditorApplication.update+=Poll;}
static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/PrisonEscape"))return;var action=File.ReadAllText("Temp/PrisonEscape").Trim();File.Delete("Temp/PrisonEscape");try{if(action=="acting")Acting();else if(action=="polish")Polish();else if(action=="test")PrisonEscapeCheck.Run();else if(action=="build")Build();else Inspect();}catch(System.Exception e){File.WriteAllText("Logs/Prison-escape-setup.txt",e.ToString());}}
static void Build(){
var office=Object.FindFirstObjectByType<PrisonOfficeSearch>();var scene=office.gameObject.scene;
if(scene.name!="PrisonChapter"||Object.FindFirstObjectByType<PrisonEscapeMission>())throw new System.Exception("감옥 씬 / 중복 확인 필요");
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,"Logs/Prison-before-escape.unity",true);
const string folder="Assets/DeliveryPrisonChapter/Escape";Directory.CreateDirectory(folder);AssetDatabase.Refresh();
var root=new GameObject("27_간수복 변장과 탈옥");var m=root.AddComponent<PrisonEscapeMission>();m.previous=office;
m.dialogue=ScriptableObject.CreateInstance<PrisonEscapeText>();AssetDatabase.CreateAsset(m.dialogue,folder+"/탈옥 대사.asset");
m.uniform=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.uniform.color=new Color(.12f,.22f,.34f);AssetDatabase.CreateAsset(m.uniform,folder+"/변장 간수복.mat");
Transform Anchor(string n,Vector3 p){var t=new GameObject(n).transform;t.SetParent(root.transform,false);t.position=p;return t;}
m.changePoint=Anchor("간수복 갈아입기",office.points[0].transform.position+Vector3.back);
var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonCity/Prefabs/Vehicles/SM_Veh_Car_Van_01.prefab");
var van=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);van.name="죄수 호송차량";van.transform.SetParent(root.transform,true);van.transform.SetPositionAndRotation(new Vector3(0,.15f,39),Quaternion.identity);m.vehicle=van.transform;
foreach(var r in van.GetComponentsInChildren<Renderer>()){var materials=r.sharedMaterials;for(int i=0;i<materials.Length;i++){if(!materials[i])continue;var mat=new Material(materials[i]);if(mat.shader.name.Contains("Standard")){mat.shader=Shader.Find("Universal Render Pipeline/Lit");}AssetDatabase.CreateAsset(mat,folder+"/차량 재질 "+r.GetInstanceID()+" "+i+".mat");materials[i]=mat;}r.sharedMaterials=materials;}
m.boardingPoint=Anchor("호송차량 탑승",new Vector3(-1.8f,.2f,39));m.gateStop=Anchor("정문 검문",new Vector3(0,.15f,47));m.exitPoint=Anchor("탈옥 도착",new Vector3(0,.15f,63));
var all=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None);m.gateLeft=all.First(t=>t.name=="GateLeaf_Left");m.gateRight=all.First(t=>t.name=="GateLeaf_Right");
m.filmCamera=Anchor("탈옥 연출 카메라",Vector3.zero).gameObject.AddComponent<Camera>();m.filmCamera.enabled=false;m.filmCamera.fieldOfView=55;
var guard=Object.Instantiate(office.previous.diners[0].gameObject,root.transform);guard.name="정문 검문 간수";guard.transform.position=new Vector3(-3.4f,0,48);guard.transform.rotation=Quaternion.Euler(0,180,0);var watch=guard.GetComponent<PrisonGuard>();watch.holdPost=true;watch.postPosition=guard.transform.position;watch.postYaw=180;Object.DestroyImmediate(guard.GetComponent<PrisonPizzaEating>());
Object.FindFirstObjectByType<PrisonSharedPresentation>().escape=m;
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
var cam=office.previous.camera;cam.transform.position=new Vector3(-9,5,34);cam.transform.LookAt(new Vector3(0,1.5f,45));PrisonChickenCheck.Capture(cam,"Logs/Prison-escape-vehicle.png");
File.WriteAllText("Logs/Prison-escape-setup.txt","PASS saved escape mission, transport van, gate guard and editable dialogue");
}
static void Polish(){var m=Object.FindFirstObjectByType<PrisonEscapeMission>();var scene=m.gameObject.scene;UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,"Logs/Prison-before-escape-polish.unity",true);var guard=m.transform.Find("정문 검문 간수");guard.gameObject.SetActive(true);guard.position=new Vector3(-2.5f,0,49);guard.GetComponent<PrisonGuard>().postPosition=guard.position;
if(!m.vehicle.Find("호송차량 표시")){var label=new GameObject("호송차량 표시");label.transform.SetParent(m.vehicle,false);label.transform.localPosition=new Vector3(-1.05f,1.5f,-.5f);label.transform.localRotation=Quaternion.Euler(0,90,0);var text=label.AddComponent<TextMesh>();text.font=MokUIFont.Font;text.fontSize=48;text.characterSize=.035f;text.anchor=TextAnchor.MiddleCenter;text.text="교정시설\n호송차량";var mat=new Material(Shader.Find("Mok/GarageDepthText"));mat.mainTexture=text.font.material.mainTexture;AssetDatabase.CreateAsset(mat,"Assets/DeliveryPrisonChapter/Escape/호송 표시.mat");label.GetComponent<Renderer>().sharedMaterial=mat;}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);File.WriteAllText("Logs/Prison-escape-polish.txt","PASS gate guard active and transport label saved");}
static void Acting(){
var m=Object.FindFirstObjectByType<PrisonEscapeMission>();var scene=m.gameObject.scene;UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,"Logs/Prison-before-escape-acting.unity",true);
var p=m.GetComponent<PrisonEscapePerformance>();if(!p)p=m.gameObject.AddComponent<PrisonEscapePerformance>();p.mission=m;m.performance=p;
Transform Anchor(string n,Vector3 pos){var t=m.vehicle.Find(n);if(!t){t=new GameObject(n).transform;t.SetParent(m.vehicle,false);}t.localPosition=pos;t.localRotation=Quaternion.identity;return t;}
var all=m.vehicle.GetComponentsInChildren<Transform>(true);var report=new System.Text.StringBuilder();foreach(var t in all){var r=t.GetComponent<Renderer>();if(r)report.AppendLine(t.name+" pos="+t.localPosition+" bounds="+r.bounds);}
Transform Door(string side){var pivot=m.vehicle.Find(side+" 앞문 경첩");if(pivot)return pivot;pivot=Anchor(side+" 앞문 경첩",new Vector3(side=="l"?-.9f:.9f,.55f,1.55f));foreach(var filter in m.vehicle.GetComponentsInChildren<MeshFilter>().Where(f=>f.transform==m.vehicle||f.name=="SM_Veh_Car_Van_Glass").ToArray())PrisonVanDoors.Split(filter,m.vehicle,pivot,side=="l");return pivot;}
p.driverDoor=Door("l");p.passengerDoor=Door("r");
p.passengerStand=Anchor("민기 조수석 문 앞",new Vector3(1.7f,0,1.05f));p.passengerStand.localRotation=Quaternion.Euler(0,-90,0);
p.passengerSeat=Anchor("민기 조수석 앉는 위치",new Vector3(.48f,0f,1.05f));p.driverEye=Anchor("운전석 눈 위치",new Vector3(-.48f,1.45f,.65f));
EditorUtility.SetDirty(m.dialogue);p.inspector=m.transform.Find("정문 검문 간수").GetComponent<PrisonGuard>();if(p.inspector.gun)p.inspector.gun.gameObject.SetActive(false);
m.boardingPoint.position=m.vehicle.TransformPoint(new Vector3(-1.6f,0,1.05f));
AssetDatabase.SaveAssets();UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);File.WriteAllText("Logs/Prison-escape-acting.txt",report.ToString());
}
static void Inspect(){var all=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None);File.WriteAllLines("Logs/Prison-escape-layout.txt",all.Where(t=>t.name.Contains("정문")||t.name.Contains("Gate")||t.name.Contains("호송")||t.name.Contains("출입")).Select(t=>t.name+" pos="+t.position+" rot="+t.eulerAngles+" scale="+t.lossyScale));}
}}
#endif
