#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
[InitializeOnLoad] public static class CargoInspection {
static CargoInspection(){EditorApplication.update+=Poll;}
static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/Mok-cargo-inspect"))return;File.Delete("Temp/Mok-cargo-inspect");var s=new StringBuilder();
foreach(var storage in Object.FindObjectsByType<BikePizzaStorage>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
s.AppendLine("STORAGE "+storage.name+" world="+storage.transform.position+" local="+storage.transform.localPosition+" scale="+storage.transform.lossyScale);
s.AppendLine("PARENT "+storage.transform.parent.name+" world="+storage.transform.parent.position+" rot="+storage.transform.parent.eulerAngles+" scale="+storage.transform.parent.lossyScale);
foreach(var t in storage.transform.parent.GetComponentsInChildren<Transform>(true)){
var r=t.GetComponent<Renderer>();if(r!=null)s.AppendLine(t.name+" local="+t.localPosition+" world="+t.position+" bounds="+r.bounds+" enabled="+r.enabled);}
s.AppendLine("visual="+(storage.pizzaBoxVisual!=null?storage.pizzaBoxVisual.name:"null"));}
File.WriteAllText("Temp/Mok-cargo-inspect.txt",s.ToString());}
}
#endif
