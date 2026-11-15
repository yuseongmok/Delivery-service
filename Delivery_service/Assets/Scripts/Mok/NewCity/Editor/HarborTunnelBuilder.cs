#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace DeliveryService.Mok.NewCity.Editor
{
 [InitializeOnLoad] internal static class HarborTunnelBuilder
 {
  static HarborTunnelBuilder(){EditorApplication.update+=Poll;}
  static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlaying||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/HarborTunnelBuilder"))return;File.Delete("Temp/HarborTunnelBuilder");try{Build();}catch(Exception e){File.WriteAllText("Logs/HarborTunnel-error.txt",e.ToString());Debug.LogException(e);}}
  static void Build()
  {
   var scene=SceneManager.GetActiveScene();if(!scene.path.EndsWith("NewHarborCity.unity"))return;
   EditorSceneManager.SaveScene(scene,"Logs/HarborTunnel-before-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);DistrictArt.Scene=scene;
   var city=scene.GetRootGameObjects().First(g=>g.name.StartsWith("NEW HARBOR")).transform;
   var old=city.Find("09 기존 도시 연결 터널");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
   DistrictArt.Parent=city;DistrictArt.Parent=DistrictArt.Group("09 기존 도시 연결 터널");
   var stone=DistrictArt.Mat("Boundary seawall",Color.gray);var road=DistrictArt.Mat("Asphalt",Color.gray);var iron=DistrictArt.Mat("Street iron",Color.gray);
   // 서남쪽 물류 출입구를 터널로 대체. 매장과 피날레는 건드리지 않는다.
   var boundary=city.Find("08 해안 산책로와 외곽 도로");
   foreach(var t in boundary.Cast<Transform>().ToArray())
   {
    var p=t.position;
    if(p.x< -53&&p.z< -63&&(t.name=="항만 차량 게이트"||t.name=="게이트 반사판"||t.name=="SM_Bld_Shop_02"||t.name=="외곽 상가 충돌"))UnityEngine.Object.DestroyImmediate(t.gameObject);
   }
   DistrictArt.Box("터널 노면",new Vector3(-66,-.025f,-70),new Vector3(28,.04f,10),road,false);
   HarborTunnelArt.Build();
   var trigger=new GameObject("터널 중간 • 기존 도시 이동");trigger.transform.SetParent(DistrictArt.Parent,false);trigger.transform.position=new Vector3(-65,2.5f,-70);
   var box=trigger.AddComponent<BoxCollider>();box.isTrigger=true;box.size=new Vector3(3,5,9.5f);
   var rb=trigger.AddComponent<Rigidbody>();rb.isKinematic=true;rb.useGravity=false;trigger.AddComponent<HarborTunnelTravel>();
   Physics.SyncTransforms();Audit(city);
   EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();File.WriteAllText("Temp/HarborBlockPolish","validate");File.WriteAllText("Temp/HarborFinaleAudit","audit");
  }
  static void Audit(Transform city)
  {
   var report=new StringBuilder("차량 외곽 충돌 검사: 폭 1.8m / 길이 3.8m (실제 운전 테스트 아님)\n");
   foreach(float side in new[]{-1f,1f})
   {
    for(int i=0;i<=24;i++){
     float angle=(-90+i*90/24f)*Mathf.Deg2Rad;var p=new Vector3(side*(72+10*Mathf.Cos(angle)),.85f,-6+10*Mathf.Sin(angle));
     CheckCar(report,p,new Vector3(-side*Mathf.Sin(angle),0,Mathf.Cos(angle)));
     angle=i*90/24f*Mathf.Deg2Rad;p=new Vector3(side*(72+10*Mathf.Cos(angle)),.85f,30+10*Mathf.Sin(angle));CheckCar(report,p,new Vector3(-side*Mathf.Sin(angle),0,Mathf.Cos(angle)));
    }
    for(float z=-6;z<=30;z+=2)CheckCar(report,new Vector3(side*82,.85f,z),Vector3.forward);
   }
   // 보행로 중앙을 따라 난간/가구/건물의 실제 충돌을 검사한다.
   foreach(float z in new[]{-76.8f,76.8f})for(float x=-90;x<91;x+=2)
   {var p=new Vector3(x,0,z);var hits=Physics.OverlapCapsule(p+Vector3.up*.42f,p+Vector3.up*1.62f,.28f,~0,QueryTriggerInteraction.Ignore);if(hits.Length>0)report.AppendLine("WALK BLOCK "+p+" "+string.Join(",",hits.Select(h=>h.name)));}
   for(float x=-54;x>=-67;x-=.5f)CheckCar(report,new Vector3(x,.85f,-70),Vector3.left);
   report.AppendLine("터널 중심부 차량 규격 검사 완료 (BLOCK 행이 없으면 통과)");
   File.WriteAllText("Logs/HarborTravelAudit.txt",report.ToString());
  }
  static void CheckCar(StringBuilder report,Vector3 p,Vector3 forward){var hits=Physics.OverlapBox(p,new Vector3(.9f,.65f,1.9f),Quaternion.LookRotation(forward),~0,QueryTriggerInteraction.Ignore);if(hits.Length>0)report.AppendLine("CAR BLOCK "+p+" "+string.Join(",",hits.Select(h=>h.name)));}
 }
}
#endif


