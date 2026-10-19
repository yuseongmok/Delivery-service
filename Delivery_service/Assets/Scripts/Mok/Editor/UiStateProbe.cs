#if UNITY_EDITOR
using System.IO;using System.Text;using UnityEditor;using UnityEngine;using UnityEngine.SceneManagement;using UnityEngine.UI;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad] public static class UiStateProbe {
static UiStateProbe(){EditorApplication.update+=Poll;}
static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists("Temp/Mok-ui-probe"))return;File.Delete("Temp/Mok-ui-probe");var b=new StringBuilder();b.AppendLine("playing="+EditorApplication.isPlaying);
for(int i=0;i<SceneManager.sceneCount;i++){var s=SceneManager.GetSceneAt(i);b.AppendLine("SCENE "+s.path+" dirty="+s.isDirty);}
foreach(var t in Object.FindObjectsByType<Text>(FindObjectsInactive.Include,FindObjectsSortMode.None)){if(t.text!="New Text")continue;var p=t.transform;string path=p.name;while(p.parent!=null){p=p.parent;path=p.name+"/"+path;}b.AppendLine(t.gameObject.scene.path+" | "+path+" self="+t.gameObject.activeSelf+" active="+t.gameObject.activeInHierarchy);}
File.WriteAllText("Temp/Mok-ui-probe.txt",b.ToString());}
}}
#endif
