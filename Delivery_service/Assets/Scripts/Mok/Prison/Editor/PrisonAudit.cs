#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Reflection;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using Object=UnityEngine.Object;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad] public static class PrisonAudit {
static PrisonAudit(){EditorApplication.update+=Poll;}
static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/PrisonAudit"))return;var a=File.ReadAllText("Temp/PrisonAudit").Trim();File.Delete("Temp/PrisonAudit");try{switch(a){case "fix":Fix();break;case "intro":PrisonIntroductionCheck.Run();break;case "conversation":PrisonConversationCheck.Run();break;case "chicken":PrisonChickenCheck.Run();break;case "medicine":PrisonMedicineCheck.Run();break;case "keys":PrisonKeysCheck.Run();break;case "office":PrisonOfficeCheck.Run();break;}}catch(Exception e){File.WriteAllText("Logs/Prison-audit.txt",e.ToString());}}
static void Fix(){var m=Object.FindFirstObjectByType<PrisonKeysMission>();var scene=m.gameObject.scene;if(scene.name!="PrisonChapter")throw new Exception("감옥 씬만 수정");EditorSceneManager.SaveScene(scene,"Logs/Prison-before-audit-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
const string folder="Assets/DeliveryPrisonChapter/OfficeSearch";var mat=AssetDatabase.LoadAssetAtPath<Material>(folder+"/벽에 가려지는 글자.mat");if(!mat){mat=new Material(Shader.Find("Mok/GarageDepthText"));mat.mainTexture=MokUIFont.Font.material.mainTexture;AssetDatabase.CreateAsset(mat,folder+"/벽에 가려지는 글자.mat");}int count=0;foreach(var t in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<TextMesh>(true))){t.GetComponent<Renderer>().sharedMaterial=mat;count++;}
var w=m.warden;SyntyStoryUpgrade.Skin(w.transform,"Assets/Synty/PolygonCity/Prefabs/Characters/Character_Male_Police.prefab","PrisonWardenUniform",false,true);
Material Cloth(string name,Color c){var x=AssetDatabase.LoadAssetAtPath<Material>(folder+"/"+name+".mat");if(!x){x=new Material(Shader.Find("Universal Render Pipeline/Lit")){color=c};AssetDatabase.CreateAsset(x,folder+"/"+name+".mat");}return x;}
var type=typeof(PrisonWardrobeSetup);type.GetField("uniform",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,Cloth("간수장 상아색 제복",new Color(.8f,.77f,.64f)));type.GetField("shoes",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,Cloth("간수장 구두",new Color(.045f,.055f,.065f)));type.GetMethod("Dress",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{w.transform,"YeomWardenDress"});
if(w.gun)w.gun.gameObject.SetActive(false);
EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);File.WriteAllText("Logs/Prison-audit.txt","PASS depth-tested text="+count+"; hidden gun no longer forces armed pose; warden cream uniform and police cap saved\n");
var cam=m.camera;bool active=w.gameObject.activeSelf;w.gameObject.SetActive(true);cam.transform.position=w.transform.position+w.transform.forward*2.8f+Vector3.up*1.3f;cam.transform.LookAt(w.transform.position+Vector3.up*1.1f);PrisonChickenCheck.Capture(cam,"Logs/Warden-uniform.png");w.gameObject.SetActive(active);
}
}}
#endif
