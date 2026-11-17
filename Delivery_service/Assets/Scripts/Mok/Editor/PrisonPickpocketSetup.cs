#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace DeliveryService.Mok.Editor
{
 [InitializeOnLoad] public static class PrisonPickpocketSetup
 {
  static PrisonPickpocketSetup(){EditorApplication.update+=Poll;}
  static void Poll()
  {
   if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists("Temp/PrisonPickpocketSetup"))return;
   File.Delete("Temp/PrisonPickpocketSetup");
   try
   {
    var scene=EditorSceneManager.GetActiveScene();if(scene.name!="PrisonChapter")throw new Exception("PrisonChapter must be active");
    EditorSceneManager.SaveScene(scene,"Logs/Prison-before-pickpocket-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
    var mission=UnityEngine.Object.FindFirstObjectByType<PrisonKeysMission>();
    if(!mission.GetComponent<PrisonPickpocket>())mission.gameObject.AddComponent<PrisonPickpocket>();
    mission.GetComponent<PrisonPickpocket>().handMesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Scripts/Mok/Art/StoryCharacters/FacetedHand.asset");
    var ring=mission.keyRing.transform;ring.localPosition=new Vector3(-.24f,.98f,-.29f);
    if(!ring.Find("금속 열쇠고리"))
    {
     var renderer=ring.GetComponent<Renderer>();if(renderer)renderer.enabled=false;
     var root=new GameObject("금속 열쇠고리").transform;root.SetParent(ring,false);
     root.localScale=new Vector3(1/ring.localScale.x,1/ring.localScale.y,1/ring.localScale.z);
     var material=renderer.sharedMaterial;
     for(int i=0;i<12;i++)
     {
      float a=i*Mathf.PI*2/12,b=(i+1)*Mathf.PI*2/12;
      Vector3 p=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0)*.045f,q=new Vector3(Mathf.Cos(b),Mathf.Sin(b),0)*.045f;
      var part=Box(root,"リング",(p+q)*.5f,new Vector3(.009f,(q-p).magnitude,.009f),material);
      part.localRotation=Quaternion.FromToRotation(Vector3.up,q-p);
     }
     for(int i=0;i<3;i++)
     {
      var key=new GameObject("열쇠 "+i).transform;key.SetParent(root,false);key.localPosition=new Vector3((i-1)*.022f,-.045f,0);key.localRotation=Quaternion.Euler(0,0,(i-1)*18);
      Box(key,"머리",Vector3.zero,new Vector3(.027f,.028f,.009f),material);
      Box(key,"몸통",new Vector3(0,-.055f,0),new Vector3(.01f,.09f,.009f),material);
      Box(key,"이빨",new Vector3(.01f,-.09f,0),new Vector3(.025f,.014f,.009f),material);
     }
    }
    EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    File.WriteAllText("Logs/PrisonPickpocketSetup.txt","PASS scene backed up; key ring saved; interaction component attached; only PrisonChapter saved.");
   }catch(Exception e){File.WriteAllText("Logs/PrisonPickpocketSetup.txt",e.ToString());}
  }
  static Transform Box(Transform parent,string name,Vector3 position,Vector3 scale,Material material)
  {var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=material;return go.transform;}
 }
}
#endif

