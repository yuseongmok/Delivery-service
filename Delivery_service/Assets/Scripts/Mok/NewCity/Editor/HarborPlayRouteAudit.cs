#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace DeliveryService.Mok.NewCity.Editor
{
 [InitializeOnLoad] internal static class HarborPlayRouteAudit
 {
  static GameObject car;static Rigidbody body;static CharacterController walker;static Camera camera;static List<Vector3> points;static int index,phase;static double last,began;static string report;
  static HarborPlayRouteAudit(){EditorApplication.update+=Tick;}
  static void Tick()
  {
   if(!EditorApplication.isPlaying){if(SessionState.GetBool("HarborAuditRunning",false)){SessionState.SetBool("HarborAuditRunning",false);EditorSceneManager.OpenScene("Assets/Scenes/NewHarborCity.unity");}if(File.Exists("Temp/HarborPlayRouteAudit")&&!EditorApplication.isCompiling){File.Delete("Temp/HarborPlayRouteAudit");SessionState.SetBool("HarborAuditRunning",true);EditorApplication.isPlaying=true;}return;}
   if(!SessionState.GetBool("HarborAuditRunning",false))return;
   if(SceneManager.GetActiveScene().name=="JJinCity"){File.AppendAllText("Logs/HarborPlayRouteAudit.txt","PASS 터널 트리거 → JJinCity 로드\n");EditorApplication.isPlaying=false;return;}
   if(!walker){var survey=Object.FindFirstObjectByType<NewCitySurvey>();if(!survey)return;survey.enabled=false;walker=survey.GetComponent<CharacterController>();camera=survey.view;phase=0;index=0;report="자동 이동 검증: 검사 차량은 본편 미션 차량과 별개\n";File.WriteAllText("Logs/HarborPlayRouteAudit.txt",report);began=last=EditorApplication.timeSinceStartup;
    car=new GameObject("검사 차량 • 1.8 x 3.8m");car.transform.position=new Vector3(60,.8f,-16);car.transform.rotation=Quaternion.Euler(0,90,0);car.AddComponent<BoxCollider>().size=new Vector3(1.8f,1.3f,3.8f);body=car.AddComponent<Rigidbody>();body.isKinematic=true;body.useGravity=false;
    var source=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshFilter>()).FirstOrDefault(g=>g.name.Contains("Car_Small"));
    if(source){var visual=new GameObject("차량 시각화");visual.transform.SetParent(car.transform,false);visual.AddComponent<MeshFilter>().sharedMesh=source.sharedMesh;visual.AddComponent<MeshRenderer>().sharedMaterials=source.GetComponent<MeshRenderer>().sharedMaterials;}
    walker.enabled=false;walker.transform.position=new Vector3(0,15,0);points=new List<Vector3>{new Vector3(72,.8f,-16)};
    for(int i=1;i<=32;i++){float a=(-90+i*90/32f)*Mathf.Deg2Rad;points.Add(new Vector3(72+10*Mathf.Cos(a),.8f,-6+10*Mathf.Sin(a)));}points.Add(new Vector3(82,.8f,30));for(int i=1;i<=32;i++){float a=i*90/32f*Mathf.Deg2Rad;points.Add(new Vector3(72+10*Mathf.Cos(a),.8f,30+10*Mathf.Sin(a)));}points.Add(new Vector3(-72,.8f,40));for(int i=1;i<=32;i++){float a=(90-i*90/32f)*Mathf.Deg2Rad;points.Add(new Vector3(-72-10*Mathf.Cos(a),.8f,30+10*Mathf.Sin(a)));}points.Add(new Vector3(-82,.8f,-6));for(int i=1;i<=32;i++){float a=-i*90/32f*Mathf.Deg2Rad;points.Add(new Vector3(-72-10*Mathf.Cos(a),.8f,-6+10*Mathf.Sin(a)));}points.Add(new Vector3(0,.8f,-16));return;}
   float dt=Mathf.Clamp((float)(EditorApplication.timeSinceStartup-last),0,.05f);last=EditorApplication.timeSinceStartup;
   if(EditorApplication.timeSinceStartup-began>180){File.AppendAllText("Logs/HarborPlayRouteAudit.txt","FAIL 검사 시간 초과\n");EditorApplication.isPlaying=false;return;}
   if(phase==0){var delta=points[index]-body.position;if(delta.magnitude<.25f){index++;if(index==points.Count){File.AppendAllText("Logs/HarborPlayRouteAudit.txt","PASS 차량 규격 이동체로 외곽 도로 1바퀴\n");Object.Destroy(car);phase=1;index=0;walker.transform.position=new Vector3(-88,1,-76.8f);walker.enabled=true;points=new List<Vector3>{new Vector3(90,1,-76.8f),new Vector3(90,1,76.8f),new Vector3(-90,1,76.8f),new Vector3(-90,1,-76.8f)};}return;}
    var direction=delta.normalized;var rotation=Quaternion.LookRotation(direction);body.rotation=rotation;Physics.SyncTransforms();float travel=Mathf.Min(delta.magnitude,12*dt);if(body.SweepTest(direction,out var hit,travel+.02f,QueryTriggerInteraction.Ignore)){File.AppendAllText("Logs/HarborPlayRouteAudit.txt","FAIL CAR "+hit.collider.name+" "+body.position+"\n");EditorApplication.isPlaying=false;return;}body.position+=direction*travel;camera.transform.position=body.position-direction*6+Vector3.up*3;camera.transform.LookAt(body.position+direction*4);}
   else if(phase==1){var delta=points[index]-walker.transform.position;delta.y=0;if(delta.magnitude<.4f){index++;if(index==points.Count){File.AppendAllText("Logs/HarborPlayRouteAudit.txt","PASS CharacterController 해안 산책로 1바퀴\n");phase=2;walker.enabled=false;walker.transform.position=new Vector3(-58,1,-70);walker.enabled=true;began=EditorApplication.timeSinceStartup;}return;}walker.Move(delta.normalized*10*dt+Vector3.down*2*dt);camera.transform.localPosition=new Vector3(0,.6f,0);camera.transform.rotation=Quaternion.LookRotation(delta.normalized);}
   else{walker.Move(Vector3.left*4*dt+Vector3.down*2*dt);camera.transform.localPosition=new Vector3(0,.6f,0);camera.transform.rotation=Quaternion.LookRotation(Vector3.left);}
  }
 }
}
#endif
