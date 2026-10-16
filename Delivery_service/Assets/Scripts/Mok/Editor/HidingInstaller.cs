#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Text;using UnityEngine;using UnityEngine.AI;using UnityEngine.UI;using UnityEditor;using UnityEditor.SceneManagement;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad]public static class HidingInstaller {
static HidingInstaller(){EditorApplication.update+=Poll;}
static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/Mok-hiding-install"))return;File.Delete("Temp/Mok-hiding-install");try{Run();}catch(Exception e){File.WriteAllText("Temp/Mok-hiding-install.txt",e.ToString());Debug.LogException(e);}}
static Material Mat(string name,Color color){string path="Assets/Scripts/Mok/Art/Hiding/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.color=color;m.SetFloat("_Smoothness",.18f);return m;}
static Transform Box(Transform parent,string name,Vector3 pos,Vector3 size,Material m){var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.SetParent(parent,false);o.transform.localPosition=pos;o.transform.localScale=size;o.GetComponent<Renderer>().sharedMaterial=m;return o.transform;}
static bool Clear(Vector3 point,Vector3 half)=>Physics.OverlapBox(point,half,Quaternion.identity,~0,QueryTriggerInteraction.Ignore).Length==0;
static void Gauge(Image image,Vector2 position){var r=image.rectTransform;r.anchorMin=r.anchorMax=r.pivot=Vector2.zero;r.anchoredPosition=position;r.sizeDelta=new Vector2(260,14);r.localScale=Vector3.one;image.enabled=true;image.gameObject.SetActive(true);}
static void Run(){
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/JJinCity.unity")throw new Exception("Open JJinCity");if(!EditorSceneManager.SaveScene(scene,"Temp/JJinCity-before-hiding.unity",true))throw new Exception("Backup failed");
bool installed=UnityEngine.Object.FindObjectsByType<HidingDumpster>(FindObjectsSortMode.None).Length==6;
Directory.CreateDirectory("Assets/Scripts/Mok/Art/Hiding");AssetDatabase.Refresh();var green=Mat("Paint",new Color(.16f,.25f,.21f));var dark=Mat("Lid",new Color(.065f,.085f,.085f));var steel=Mat("Hardware",new Color(.32f,.36f,.34f));var stripe=Mat("Marking",new Color(.68f,.59f,.28f));
var root=(GameObject.Find("Mok Hiding Dumpsters")??new GameObject("Mok Hiding Dumpsters"));var targets=new[]{new Vector3(-70,0,-105),new Vector3(-70,0,40),new Vector3(0,0,-45),new Vector3(20,0,105),new Vector3(110,0,90),new Vector3(100,0,0)};
var sidewalks=UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(x=>x.name.StartsWith("SM_Env_Sidewalk")&&x.bounds.max.y<.2f).ToArray();var log=new StringBuilder();Physics.SyncTransforms();
for(int i=0;i<(installed?0:targets.Length);i++){
Vector3 pos=default;float yaw=0;bool found=false;
foreach(var tile in sidewalks.OrderBy(t=>(t.bounds.center-targets[i]).sqrMagnitude)){
var candidate=new Vector3(tile.bounds.center.x,.32f,tile.bounds.center.z);if(Vector3.Distance(candidate,targets[i])>35)break;
if(!Clear(candidate+Vector3.up*.9f,new Vector3(1.22f,.73f,.92f)))continue;
for(int angle=0;angle<360;angle+=90){var exit=candidate+Quaternion.Euler(0,angle,0)*Vector3.forward*1.8f;if(!NavMesh.SamplePosition(exit,out var hit,.5f,NavMesh.AllAreas)||!Clear(exit+Vector3.up, new Vector3(.4f,.85f,.4f)))continue;pos=candidate;yaw=angle;found=true;break;}if(found)break;
}
if(!found){File.WriteAllText("Temp/Mok-hide-placement.txt",string.Join("\n",sidewalks.OrderBy(t=>(t.bounds.center-targets[i]).sqrMagnitude).Take(12).Select(t=>{var q=new Vector3(t.bounds.center.x,.08f,t.bounds.center.z);return t.name+" "+q+" blockers="+string.Join(",",Physics.OverlapBox(q+Vector3.up*.9f,new Vector3(1.22f,.73f,.92f)).Select(c=>c.name+" "+c.bounds))+" nav="+NavMesh.SamplePosition(q,out var h,4,NavMesh.AllAreas);})));throw new Exception("No clear sidewalk near "+targets[i]);}
var o=new GameObject("Hide Dumpster "+(i+1).ToString("00"));o.transform.SetParent(root.transform);o.transform.SetPositionAndRotation(pos,Quaternion.Euler(0,yaw,0));var t=o.transform;var bin=o.AddComponent<HidingDumpster>();
Box(t,"Floor",new Vector3(0,.23f,0),new Vector3(2.2f,.18f,1.5f),green);
Box(t,"Back",new Vector3(0,.9f,-.72f),new Vector3(2.2f,1.3f,.12f),green);
Box(t,"Left",new Vector3(-1.04f,.9f,0),new Vector3(.12f,1.3f,1.5f),green);Box(t,"Right",new Vector3(1.04f,.9f,0),new Vector3(.12f,1.3f,1.5f),green);
Box(t,"Front",new Vector3(0,.63f,.72f),new Vector3(2.2f,.78f,.12f),green);Box(t,"Peek rim",new Vector3(0,1.45f,.72f),new Vector3(2.2f,.2f,.12f),green);
for(int j=-1;j<=1;j+=2){Box(t,"Foot",new Vector3(j*.8f,.1f,0),new Vector3(.24f,.2f,1.2f),dark);Box(t,"Front brace",new Vector3(j*.78f,.68f,.80f),new Vector3(.065f,.65f,.05f),steel);Box(t,"Reflector",new Vector3(j*.52f,.72f,.80f),new Vector3(.45f,.12f,.04f),stripe);}
var hinge=new GameObject("Lid hinge").transform;hinge.SetParent(t,false);hinge.localPosition=new Vector3(0,1.58f,-.78f);bin.lid=hinge;Box(hinge,"Lid",new Vector3(0,0,.78f),new Vector3(2.32f,.12f,1.64f),dark);for(int rib=-2;rib<=2;rib++)Box(hinge,"Rib",new Vector3(rib*.4f,.08f,.78f),new Vector3(.065f,.045f,1.4f),steel);
Box(t,"Handle",new Vector3(0,1.45f,.84f),new Vector3(.55f,.065f,.08f),steel);
var obstacle=o.AddComponent<NavMeshObstacle>();obstacle.shape=NavMeshObstacleShape.Box;obstacle.center=new Vector3(0,.85f,0);obstacle.size=new Vector3(2.2f,1.7f,1.6f);obstacle.carving=true;
log.AppendLine(o.name+" position="+pos+" exit="+bin.ExitPosition);Physics.SyncTransforms();
}
var player=UnityEngine.Object.FindFirstObjectByType<PlayerCombat>();if(player.GetComponent<PlayerHiding>()==null)player.gameObject.AddComponent<PlayerHiding>();
var bike=UnityEngine.Object.FindFirstObjectByType<MotorcycleController>();var fuel=bike.GetComponent<MotorcycleFuel>();var durability=UnityEngine.Object.FindFirstObjectByType<MotorcycleDurability>();var panel=fuel.fuelGaugeUI.transform.parent.GetComponent<RectTransform>();panel.anchorMin=panel.anchorMax=panel.pivot=Vector2.zero;panel.anchoredPosition=new Vector2(20,24);panel.sizeDelta=new Vector2(280,110);panel.localScale=Vector3.one;
Gauge(fuel.fuelGaugeUI,new Vector2(0,52));Gauge(durability.durabilityGaugeUI,new Vector2(0,12));
var font=UnityEngine.Object.FindObjectsByType<WantedHUD>(FindObjectsInactive.Include,FindObjectsSortMode.None).First().label.font;
foreach(var entry in new[]{("Fuel label","연료",72f),("Repair label","내구도",32f)}){var label=new GameObject(entry.Item1,typeof(RectTransform),typeof(Text)).GetComponent<Text>();label.transform.SetParent(panel,false);label.font=font;label.fontSize=14;label.text=entry.Item2;label.raycastTarget=false;label.rectTransform.anchorMin=label.rectTransform.anchorMax=label.rectTransform.pivot=Vector2.zero;label.rectTransform.anchoredPosition=new Vector2(0,entry.Item3);label.rectTransform.sizeDelta=new Vector2(260,20);}
EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();File.WriteAllText("Temp/Mok-hiding-install.txt","PASS: Six dumpsters, player hiding and gauge positions saved.\n"+log);
}
}}
#endif
