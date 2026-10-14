#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
public static class MokRepairInspect {
[InitializeOnLoadMethod] static void Init(){EditorApplication.delayCall+=Run;}
static string P(Transform t)=>t.parent==null?t.name:P(t.parent)+"/"+t.name;
static void Run(){if(EditorApplication.isPlayingOrWillChangePlaymode)return;var s=new StringBuilder();var all=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None);var marker=all.FirstOrDefault(t=>t.name.ToLower()=="repair_shop");if(marker==null){File.WriteAllText("Temp/Mok-repair-inspect.txt","marker missing");return;}s.AppendLine("MARKER "+P(marker)+" pos="+marker.position.ToString("F3")+" rot="+marker.eulerAngles+" scale="+marker.lossyScale+" scene="+marker.gameObject.scene.path);
foreach(var c in marker.GetComponents<Component>())s.AppendLine("MARKERCOMP "+c.GetType().Name);
foreach(var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))if(Vector3.Distance(r.bounds.center,marker.position)<35)s.AppendLine("RENDER "+P(r.transform)+" bounds="+r.bounds);
foreach(var c in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))if(Vector3.Distance(c.bounds.center,marker.position)<25 || P(c.transform).ToLower().Contains("bike"))s.AppendLine("COLLIDER "+P(c.transform)+" type="+c.GetType().Name+" trigger="+c.isTrigger+" bounds="+c.bounds+" layer="+c.gameObject.layer);
foreach(var t in all.Where(t=>t.name.ToLower().Contains("bike")))s.AppendLine("BIKE "+P(t)+" pos="+t.position+" rot="+t.eulerAngles+" scripts="+string.Join(",",t.GetComponents<MonoBehaviour>().Where(b=>b!=null).Select(b=>b.GetType().Name)));
File.WriteAllText("Temp/Mok-repair-inspect.txt",s.ToString());}
}
#endif
