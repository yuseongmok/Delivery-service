#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
namespace DeliveryService.Mok.Editor
{
 public static class AftermathDoorContact
 {
  public static void Apply(){try{
   var scene=EditorSceneManager.OpenScene("Assets/Scenes/PrisonAftermath.unity");
   EditorSceneManager.SaveScene(scene,"Logs/Aftermath-before-door-contact-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
   var film=Object.FindFirstObjectByType<AftermathFilm>();var h=film.hideout;
   h.cabinetDoor.localRotation=Quaternion.identity;
   if(h.openAngle<0){
    var set=film.shots[2].set.transform;
    foreach(var t in set.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="보관함 뒤판"||t.name=="보관함 측판"||t.name=="보관함 선반"))t.position+=Vector3.right*.35f;
    var children=h.cabinetDoor.Cast<Transform>().ToArray();var positions=children.Select(t=>t.position+Vector3.right*.35f).ToArray();
    h.cabinetDoor.position=new Vector3(500.2f,.25f,1.5f);
    for(int i=0;i<children.Length;i++)children[i].position=positions[i];
    var handle=children.First(t=>t.name=="문 손잡이");handle.position=new Vector3(500.65f,1.13f,1.42f);
    h.doorGrip.position=new Vector3(500.65f,1.13f,1.4f);
   }
   h.openAngle=165;
   EditorSceneManager.SaveScene(scene);AftermathReview.Record();
  }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 }
}
#endif
