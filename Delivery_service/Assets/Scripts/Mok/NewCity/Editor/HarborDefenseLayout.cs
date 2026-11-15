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
 [InitializeOnLoad] internal static class HarborDefenseLayout
 {
  static Material steel,wood,green;static Transform root;
  static HarborDefenseLayout(){EditorApplication.update+=Poll;}
  static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/HarborDefenseLayout"))return;File.Delete("Temp/HarborDefenseLayout");try{Build();}catch(Exception e){File.WriteAllText("Logs/HarborDefense-error.txt",e.ToString());Debug.LogException(e);}}
  static void Build()
  {
   var scene=SceneManager.GetActiveScene();if(!scene.path.EndsWith("NewHarborCity.unity"))throw new Exception("새 도시 씬 전용입니다.");
   EditorSceneManager.SaveScene(scene,"Logs/HarborDefense-before-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);DistrictArt.Scene=scene;
   var city=scene.GetRootGameObjects().First(g=>g.name.StartsWith("NEW HARBOR")).transform;
   var shop=city.Cast<Transform>().First(t=>t.name.StartsWith("02 "));
   var old=shop.Find("디펜스 준비 • 엄폐와 셔터");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
   DistrictArt.Parent=shop;root=DistrictArt.Group("디펜스 준비 • 엄폐와 셔터");DistrictArt.Parent=root;
   steel=DistrictArt.Mat("Defense shutter steel",new Color(.25f,.29f,.28f));wood=DistrictArt.Mat("Interior walnut",Color.gray);green=DistrictArt.Mat("Deep green joinery",Color.gray);
   // 창을 총알이 막는 투명벽으로 두지 않는다. 중앙 두 창은 전투 시 열린 창으로 준비한다.
   foreach(var t in shop.Cast<Transform>().ToArray())if(t.name=="유리"&&Mathf.Abs(t.position.z)<.2f&&Mathf.Abs(t.position.x)<4){t.gameObject.SetActive(false);}
   foreach(float x in new[]{-10f,-6f,6f,10f})Shutter(new Vector3(x,1.65f,-.12f),3.65f,2.7f,0);
   Shutter(new Vector3(-12.12f,1.7f,5),9.8f,2.7f,90);
   Shutter(new Vector3(12.12f,1.7f,3),5.8f,2.7f,90);
   Shutter(new Vector3(12.12f,1.7f,9),1.8f,2.7f,90);
   foreach(float x in new[]{-2.75f,2.75f})
   {
    Box("열린 창 하부 보호판",new Vector3(x,.48f,-.06f),new Vector3(2.2f,.9f,.16f),green);
    Box("올려둔 중앙 창 셔터함",new Vector3(x,3,-.13f),new Vector3(2.35f,.24f,.3f),steel);
   }
   // 가구로 읽히는 엄폐. 부스 진입과 중앙 통로를 막지 않는다.
   Box("부스 옆 서비스 수납장",new Vector3(-6, .57f,5.1f),new Vector3(2.5f,1.06f,.65f),green);
   Box("서비스 수납장 상판",new Vector3(-6,1.13f,5.1f),new Vector3(2.6f,.1f,.75f),wood);
   DistrictArt.Prop("SM_Gen_Prop_Plate_01",new Vector3(-6.7f,1.19f,5.1f),.045f);
   Box("주방 입구 이동식 보조대",new Vector3(7, .57f,12),new Vector3(.75f,1.06f,1.9f),green);
   Box("보조대 상판",new Vector3(7,1.14f,12),new Vector3(.85f,.1f,2),wood);
   // 보조대 동쪽에 서면 측면 문을 지켜보면서 뒤쪽 후문으로 물러설 수 있다.
   Marker("방어 1 • 카운터 정면",new Vector3(0,.08f,11.1f),180);
   Marker("방어 2 • 부스 수납장 뒤",new Vector3(-6,.08f,6.2f),180);
   Marker("방어 3 • 주방 측면 엄호",new Vector3(8.1f,.08f,12),135);
   Marker("적 진입 1 • 정문 돌입",new Vector3(0,.08f,-3),0);
   Marker("적 진입 2 • 오른쪽 측면",new Vector3(15,.08f,7),270);
   Marker("야마자키파 • 정면 접근",new Vector3(-7,.08f,-8),0);
   Marker("박진웅 조직 • 우측 도착",new Vector3(22,.08f,-7),270);
   Marker("후퇴 안내 • 카운터 오른쪽",new Vector3(6.2f,.08f,9),0);
   Marker("후퇴 안내 • 직원 구역",new Vector3(9.2f,.08f,14),0);
   // 정문과 측면에 몸을 숨길 수 있는 배송 적재물을 두되 문 앞을 비워둔다.
   foreach(var p in new[]{new Vector3(-4,.55f,-4),new Vector3(4,.55f,-5.5f),new Vector3(16,.55f,9.5f)})
   {Box("야외 납품용 적재함",p,new Vector3(1.8f,1.05f,.85f),wood);Box("적재함 금속 띠",p+Vector3.up*.25f,new Vector3(1.83f,.07f,.88f),steel,false);}
   DistrictArt.Sign("직원 출입구 →",new Vector3(7.4f,2.7f,17.75f),.055f);
   foreach(var t in city.GetComponentsInChildren<Transform>())if((t.name=="외부 메뉴판"||t.name.StartsWith("FRESH PIZZA"))&&t.position.x>-4&&t.position.z<0)t.position+=Vector3.left*5;
   Physics.SyncTransforms();var report=new System.Text.StringBuilder();
   Check(report,new Vector3(0,1.65f,11.1f),new Vector3(0,1.65f,-3),"카운터 정문 사선");
   Check(report,new Vector3(-6,1.65f,6.2f),new Vector3(-2.75f,1.65f,-3),"부스 열린 창 사선");
   Check(report,new Vector3(8.1f,1.65f,12),new Vector3(13,1.65f,7),"주방 측면 사선");
   File.WriteAllText("Logs/HarborDefense-validation.txt",report.ToString());
   EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();File.WriteAllText("Temp/HarborBlockPolish","validate");File.WriteAllText("Temp/HarborFinaleAudit","audit");
  }
  static void Shutter(Vector3 p,float width,float height,float yaw)
  {
   var group=DistrictArt.Group("방어 셔터 • "+p);group.position=p;group.rotation=Quaternion.Euler(0,yaw,0);
   var old=DistrictArt.Parent;DistrictArt.Parent=group;
   var panel=Box("셔터 방탄 충돌판",Vector3.zero,new Vector3(width,height,.07f),steel);panel.transform.localPosition=Vector3.zero;panel.transform.localRotation=Quaternion.identity;
   for(float y=-height*.5f;y<height*.5f;y+=.18f){var slat=Box("셔터 가로 슬랫",Vector3.zero,new Vector3(width,.025f,.045f),wood,false);slat.transform.localPosition=new Vector3(0,y,-.06f);slat.transform.localRotation=Quaternion.identity;}
   var hood=Box("셔터 수납함",Vector3.zero,new Vector3(width+.15f,.25f,.32f),steel);hood.transform.localPosition=new Vector3(0,height*.5f+.13f,0);hood.transform.localRotation=Quaternion.identity;DistrictArt.Parent=old;
  }
  static GameObject Box(string n,Vector3 p,Vector3 s,Material m,bool solid=true)=>DistrictArt.Box(n,p,s,m,solid);
  static void Marker(string n,Vector3 p,float yaw){var g=new GameObject(n);g.transform.SetParent(root,false);g.transform.SetPositionAndRotation(p,Quaternion.Euler(0,yaw,0));}
  static void Check(System.Text.StringBuilder r,Vector3 a,Vector3 b,string name){r.AppendLine(name+": "+(Physics.Linecast(a,b,out var h)?"BLOCKED "+h.collider.name:"CLEAR"));}
 }
}
#endif

