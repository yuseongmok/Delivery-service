#if UNITY_EDITOR
using System.IO;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad] public static class UiSceneCleanup {
static UiSceneCleanup(){EditorApplication.update+=Poll;}
static void Poll(){if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists("Temp/Mok-ui-scene-cleanup"))return;File.Delete("Temp/Mok-ui-scene-cleanup");
var main=SceneManager.GetSceneByPath("Assets/Scenes/JJinCity.unity");var extra=SceneManager.GetSceneByPath("Assets/Scenes/Woong1.unity");
if(!main.isLoaded||!extra.isLoaded){File.WriteAllText("Temp/Mok-ui-scene-cleanup.txt","Expected scenes not loaded");return;}
if(!EditorSceneManager.SaveScene(main,"Temp/JJinCity-before-ui-cleanup.unity",true)||!EditorSceneManager.SaveScene(extra,"Temp/Woong1-before-ui-cleanup.unity",true))return;
SceneManager.SetActiveScene(main);EditorSceneManager.CloseScene(extra,true);
File.WriteAllText("Temp/Mok-ui-scene-cleanup.txt","Backed up both scenes including unsaved changes; closed Woong1 without changing scene assets. Main scene preserved.");
}}
}
#endif
