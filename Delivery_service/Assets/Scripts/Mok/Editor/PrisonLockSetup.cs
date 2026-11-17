#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace DeliveryService.Mok.Editor
{
 [InitializeOnLoad] public static class PrisonLockSetup
 {
  static PrisonLockSetup(){EditorApplication.update+=Poll;}
  static void Poll()
  {
   if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists("Temp/PrisonLockSetup"))return;
   File.Delete("Temp/PrisonLockSetup");
   try
   {
    var scene=EditorSceneManager.GetActiveScene();if(scene.name!="PrisonChapter")throw new Exception("Open PrisonChapter first");
    EditorSceneManager.SaveScene(scene,"Logs/Prison-before-lock-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
    var mission=UnityEngine.Object.FindFirstObjectByType<PrisonKeysMission>();
    if(!mission.GetComponent<PrisonLockInteraction>())mission.gameObject.AddComponent<PrisonLockInteraction>();
    var door=mission.keyLock.door;
    if(!door.Find("창고 열쇠구멍"))
    {
     string path="Assets/DeliveryPrisonChapter/KitchenArt/LockKeyhole.mat";
     var material=AssetDatabase.LoadAssetAtPath<Material>(path);
     if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Lit")??Shader.Find("Standard"));material.color=new Color(.035f,.04f,.045f);AssetDatabase.CreateAsset(material,path);}
     var hole=GameObject.CreatePrimitive(PrimitiveType.Cube);hole.name="창고 열쇠구멍";hole.transform.SetParent(door,true);hole.transform.SetPositionAndRotation(mission.keyLock.transform.position-mission.keyLock.transform.forward*.045f,mission.keyLock.transform.rotation);hole.transform.localScale=new Vector3(.015f,.055f,.008f);UnityEngine.Object.DestroyImmediate(hole.GetComponent<Collider>());hole.GetComponent<Renderer>().sharedMaterial=material;
    }
    EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
    File.WriteAllText("Logs/PrisonLockSetup.txt","PASS backed up unsaved scene and saved lock socket; PrisonChapter only");
   }catch(Exception e){File.WriteAllText("Logs/PrisonLockSetup.txt",e.ToString());}
  }
 }
}
#endif
