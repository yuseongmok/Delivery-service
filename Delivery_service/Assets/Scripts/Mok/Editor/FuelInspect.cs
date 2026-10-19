#if UNITY_EDITOR
using System.IO;using System.Linq;using System.Text;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using UnityEngine.SceneManagement;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad]public static class FuelInspect{
static FuelInspect(){EditorApplication.update+=Poll;}
static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/Mok-fuel-inspect"))return;File.Delete("Temp/Mok-fuel-inspect");var scene=SceneManager.GetSceneByPath("Assets/Scenes/JJinCity.unity");bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene("Assets/Scenes/JJinCity.unity",OpenSceneMode.Additive);try{
var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();var sb=new StringBuilder();
string P(Transform t)=>t==null?"NULL":t.parent==null?t.name:P(t.parent)+"/"+t.name;
foreach(var t in all)if(t.name=="GasPump"||t.GetComponent<FuelNozzle>()!=null||t.GetComponent<FuelCap>()!=null||t.GetComponent<PlayerInteract>()!=null){sb.AppendLine("OBJECT "+P(t)+" position="+t.position+" rotation="+t.eulerAngles+" scale="+t.lossyScale);foreach(var comp in t.GetComponents<Component>())sb.AppendLine("  "+comp.GetType().Name);
if(t.name=="GasPump")foreach(var r in t.GetComponentsInChildren<Renderer>(true))sb.AppendLine("RENDER "+P(r.transform)+" bounds="+r.bounds+" mats="+string.Join(",",r.sharedMaterials.Select(m=>m==null?"NULL":m.name+"/"+m.shader.name)));
var cap=t.GetComponent<FuelCap>();if(cap!=null)sb.AppendLine("CAP fuel="+P(cap.bikeFuelSystem?.transform)+" attach="+P(cap.NozzleAttachPoint));
var nozzle=t.GetComponent<FuelNozzle>();if(nozzle!=null)sb.AppendLine("NOZZLE reset="+P(nozzle.pumpResetPoint)+" hand="+P(nozzle.playerHandPoint)+" hose="+P(nozzle.hoseRender?.transform)+" start="+P(nozzle.hoseStartPoint)+" end="+P(nozzle.hoseEndPoint)+" rate="+nozzle.refuelSpeed+" cost="+nozzle.costPerSecond);
var interact=t.GetComponent<PlayerInteract>();if(interact!=null)sb.AppendLine("INTERACT mask="+interact.motorcycleLayer.value+" camera="+P(interact.playerCameraTransform));
foreach(var c in t.GetComponentsInChildren<Collider>(true))sb.AppendLine("COL "+P(c.transform)+" "+c.GetType().Name+" bounds="+c.bounds+" layer="+c.gameObject.layer+" tag="+c.tag+" enabled="+c.enabled);
}
File.WriteAllText("Temp/Mok-fuel-inspect.txt",sb.ToString());}finally{if(opened)EditorSceneManager.CloseScene(scene,true);}}
}}
#endif
