#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad] static class PrisonSceneRecovery {
 static PrisonSceneRecovery(){EditorApplication.delayCall+=Recover;}
 static void Recover(){if(EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/PrisonRecoveryCloseCity"))return;File.Delete("Temp/PrisonRecoveryCloseCity");var prison=SceneManager.GetSceneByName("PrisonChapter");var city=SceneManager.GetSceneByName("JJinCity");if(!prison.IsValid()||!city.IsValid())return;Directory.CreateDirectory("Logs/SceneRecovery");if(!EditorSceneManager.SaveScene(city,"Logs/SceneRecovery/CityBeforeUnloading.unity",true))throw new System.Exception("City backup failed");EditorSceneManager.CloseScene(city,true);SceneManager.SetActiveScene(prison);Selection.objects=new Object[0];if(SceneView.lastActiveSceneView)SceneView.lastActiveSceneView.LookAt(new Vector3(0,0,28),Quaternion.Euler(35,30,0),65);File.WriteAllText("Logs/SceneRecovery/recovered.txt","Closed extra city scene; prison dirty="+prison.isDirty+" scenes="+SceneManager.sceneCount);}
}
}
#endif
