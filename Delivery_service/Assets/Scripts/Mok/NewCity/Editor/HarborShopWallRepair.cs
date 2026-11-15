#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace DeliveryService.Mok.NewCity.Editor
{
 [InitializeOnLoad] internal static class HarborShopWallRepair
 {
  static HarborShopWallRepair(){EditorApplication.update+=Poll;}
  static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlaying||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/HarborShopWallRepair"))return;File.Delete("Temp/HarborShopWallRepair");try{Repair();}catch(Exception e){File.WriteAllText("Logs/HarborShopWallRepair-error.txt",e.ToString());Debug.LogException(e);}}
  static void Repair()
  {
   var scene=SceneManager.GetActiveScene();if(!scene.path.EndsWith("NewHarborCity.unity"))throw new Exception("새 도시 씬 전용입니다.");
   EditorSceneManager.SaveScene(scene,"Logs/ShopWall-before-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
   DistrictArt.Scene=scene;var city=scene.GetRootGameObjects().First(g=>g.name.StartsWith("NEW HARBOR")).transform;var shop=city.Cast<Transform>().First(t=>t.name.StartsWith("02 "));
   var ceiling=shop.Find("지붕과 천장").GetComponent<Renderer>().bounds;float top=ceiling.min.y+.02f,bottom=3.03f;
   var previous=shop.Find("창 위 상부벽과 천장 마감");if(previous)UnityEngine.Object.DestroyImmediate(previous.gameObject);
   DistrictArt.Parent=shop;DistrictArt.Parent=DistrictArt.Group("창 위 상부벽과 천장 마감");
   var plaster=AssetDatabase.LoadAssetAtPath<Material>(DistrictArt.Folder+"/Materials/Warm plaster.mat");var green=AssetDatabase.LoadAssetAtPath<Material>(DistrictArt.Folder+"/Materials/Deep green joinery.mat");
   foreach(float x in new[]{-12f,12f})
   {
    DistrictArt.Box("측면 창 위 연속 상부벽",new Vector3(x,(top+bottom)*.5f,5),new Vector3(.32f,top-bottom,10.25f),plaster);
    DistrictArt.Box("측면 창 상부 녹색 몰딩",new Vector3(x,3.10f,5),new Vector3(.38f,.14f,10.25f),green);
    DistrictArt.Box("측면 천장 접합 몰딩",new Vector3(x,top-.10f,5),new Vector3(.45f,.18f,10.25f),green,false);
   }
   DistrictArt.Box("정면 간판 뒤 연속 상부벽",new Vector3(0,(top+bottom)*.5f,0),new Vector3(24.25f,top-bottom,.32f),plaster);
   DistrictArt.Box("정면 창 상부 녹색 몰딩",new Vector3(0,3.10f,0),new Vector3(24.25f,.14f,.38f),green);
   DistrictArt.Box("정면 천장 접합 몰딩",new Vector3(0,top-.10f,0),new Vector3(24.25f,.18f,.45f),green,false);
   foreach(var t in shop.Cast<Transform>())if(t.name=="우측 창 기둥"&&Mathf.Abs(t.position.z-7)<.1f)t.position=new Vector3(t.position.x,t.position.y,8.05f);
   Physics.SyncTransforms();var log=new System.Text.StringBuilder();
   foreach(float x in new[]{-12f,12f})foreach(float z in new[]{1f,5f,7f,9f}){
    bool closed=Physics.Linecast(new Vector3(x-Mathf.Sign(x),3.7f,z),new Vector3(x+Mathf.Sign(x),3.7f,z));log.AppendLine((closed?"PASS":"FAIL")+" 상부 틈 "+x+","+z);}
   foreach(var p in new[]{new Vector3(0,0,0),new Vector3(12,0,7),new Vector3(9.2f,0,18)}){
    bool clear=!Physics.CheckCapsule(p+Vector3.up*.42f,p+Vector3.up*1.62f,.28f,~0,QueryTriggerInteraction.Ignore);log.AppendLine((clear?"PASS":"FAIL")+" 출입구 "+p);}
   File.WriteAllText("Logs/HarborShopWallRepair-validation.txt",log.ToString());EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();File.WriteAllText("Temp/ShopInteriorPolish","capture");
  }
 }
}
#endif

