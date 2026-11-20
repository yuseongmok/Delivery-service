#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad] public static class RaceSceneSurvey {
 static RaceSceneSurvey(){EditorApplication.update+=Run;}
 static void Run(){
 if(!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling&&File.Exists("Temp/RaceRefresh")){File.Delete("Temp/RaceRefresh");AssetDatabase.Refresh();return;}
 if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||!File.Exists("Temp/RaceSceneSurvey"))return;
 File.Delete("Temp/RaceSceneSurvey");
 var scene=SceneManager.GetSceneByPath("Assets/Scenes/JJinCity.unity");bool added=!scene.isLoaded;
 if(added)scene=EditorSceneManager.OpenScene("Assets/Scenes/JJinCity.unity",OpenSceneMode.Additive);
 var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
 var s=new StringBuilder();var race=all.Select(t=>t.GetComponent<RaceManager>()).First(r=>r);
 foreach(var t in all.Where(t=>t.name.Contains("신도시")||t.name=="RaceObject"||t.name=="Motorcycle"))s.AppendLine(t.name+" "+t.position);
 foreach(var c in race.objects.GetComponentsInChildren<Collider>(true))s.AppendLine("BLOCK "+c.name+" pos="+c.transform.position+" scale="+c.transform.lossyScale+" type="+c.GetType().Name);
 foreach(var b in all.Select(t=>t.GetComponent<MotorcycleController>()).Where(b=>b))s.AppendLine("BIKE "+b.name+" sphere="+b.sphereRB+" collider="+b.sphereCollider+" layer="+b.sphereCollider.gameObject.layer+" trigger="+b.sphereCollider.isTrigger);
 foreach(var n in all.Select(t=>t.GetComponent<NPCManager>()).Where(n=>n)){var o=new SerializedObject(n);s.AppendLine("NPC manager "+n.name+" prefab="+AssetDatabase.GetAssetPath(o.FindProperty("racerNPCPrefab").objectReferenceValue));}
 foreach(var cp in race.PlayerCourse){var p=cp.transform.position; var hits=Physics.RaycastAll(p+Vector3.up*2,Vector3.down,10,~0,QueryTriggerInteraction.Ignore);s.AppendLine("CP "+cp.name+" "+p+" floors="+string.Join(";",hits.Select(h=>h.collider.name+"@"+h.point.y)));}
 s.AppendLine("START "+race.PlayerStart.position);
 foreach(var h in Physics.RaycastAll(race.PlayerStart.position+Vector3.up*10,Vector3.down,40))s.AppendLine("GROUND "+h.collider.name+" layer="+h.collider.gameObject.layer+" point="+h.point+" trigger="+h.collider.isTrigger);
 File.WriteAllText("Logs/Race-scene-survey.txt",s.ToString());
 if(added)EditorSceneManager.CloseScene(scene,true);
 }
}}
#endif
