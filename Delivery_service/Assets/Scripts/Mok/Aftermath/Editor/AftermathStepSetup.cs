#if UNITY_EDITOR
using System;using System.IO;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using Object=UnityEngine.Object;
namespace DeliveryService.Mok.Editor
{
 public static class AftermathStepSetup
 {
 public static void Apply(){try{
 var scene=EditorSceneManager.OpenScene("Assets/Scenes/PrisonAftermath.unity");EditorSceneManager.SaveScene(scene,"Logs/Aftermath-before-shortsteps-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
 var f=Object.FindFirstObjectByType<AftermathFilm>();var h=f.hideout;var steps=h.mingi.GetComponent<AftermathCabinetSteps>();if(!steps)steps=h.mingi.gameObject.AddComponent<AftermathCabinetSteps>();steps.animator=h.mingi.animator;steps.stepOffset=new Vector3(.28f,0,-.08f);h.steps=steps;
 EditorSceneManager.SaveScene(scene);File.WriteAllText("Logs/Aftermath-shortsteps.txt","Cabinet walk cycle disabled. Two planted-foot steps, 0.29m maximum body offset and 0.035m foot lift. Camera, dialogue and bag endpoints unchanged.");AftermathReview.Record();
 }catch(Exception e){File.WriteAllText("Logs/Aftermath-shortsteps.txt",e.ToString());EditorApplication.Exit(1);}}
 }
}
#endif
