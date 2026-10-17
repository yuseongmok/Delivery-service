#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
public static class MokRepairGarageTest {
const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
static string log;
static void Check(bool ok,string t){if(!ok)throw new Exception(log+"FAIL: "+t);log+="PASS: "+t+"\n";}
static void Invoke(object o,string n)=>o.GetType().GetMethod(n,F).Invoke(o,null);
static T Get<T>(object o,string n)=>(T)o.GetType().GetField(n,F).GetValue(o);
static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
static void Capture(string name,Vector3 pos,Vector3 target){var go=new GameObject("Building preview camera");var c=go.AddComponent<Camera>();c.transform.position=pos;c.transform.LookAt(target);c.fieldOfView=48;c.nearClipPlane=.05f;c.farClipPlane=400;
var rt=new RenderTexture(1400,1000,24,RenderTextureFormat.ARGB32);rt.Create();var old=RenderTexture.active;Texture2D tex=null;try{RenderPipeline.SubmitRenderRequest(c,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;tex=new Texture2D(1400,1000,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1400,1000),0,0);tex.Apply();File.WriteAllBytes("Temp/Mok-repair-"+name+".png",tex.EncodeToPNG());}finally{RenderTexture.active=old;if(tex!=null)UnityEngine.Object.DestroyImmediate(tex);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);}}

public static string RunCore(){log="Repair garage Play Mode verification\n";
var marker=GameObject.Find("repair_shop");var root=marker.transform.Find("Mok Repair Garage");Check(root!=null,"garage loaded from saved scene at repair_shop");Check(!marker.GetComponent<Collider>().enabled,"original solid cube collider disabled");Check(root.GetComponent<Renderer>().sharedMaterials.All(m=>m.shader.name=="Universal Render Pipeline/Lit"),"garage materials respond to city lighting");
// 일정한 주행 시험을 위해 테스트에서만 임의 보행자를 제거한다.
foreach(var agent in UnityEngine.Object.FindObjectsByType<UnityEngine.AI.NavMeshAgent>(FindObjectsSortMode.None))agent.gameObject.SetActive(false);
Physics.SyncTransforms();
foreach(float x in new[]{-3f,0f,3f}){var start=root.TransformPoint(new Vector3(x,1.4f,7));bool hit=Physics.BoxCast(start,new Vector3(.6f,1.2f,1.45f),-root.forward,out var h,root.rotation,6.7f,~0,QueryTriggerInteraction.Ignore);Check(!hit,"bike-and-rider clearance from street into garage at lane "+x+(hit?" blocked by "+h.collider.name:""));}
foreach(int angle in new[]{0,45,90,135,180}){var overlaps=Physics.OverlapBox(root.TransformPoint(new Vector3(0,1.6f,-.45f)),new Vector3(.6f,1.2f,1.45f),Quaternion.Euler(0,angle,0),~0,QueryTriggerInteraction.Ignore);Check(overlaps.Length==0,"service pad clearance at heading "+angle+" hits="+string.Join(",",overlaps.Select(c=>c.name+"/"+c.GetType().Name+"/"+c.bounds)));}
// 기존 Movement/LateUpdate와 실제 물리로 오토바이를 주행한다.
var bike=UnityEngine.Object.FindFirstObjectByType<MotorcycleController>();Check(bike!=null&&bike.sphereRB!=null,"existing motorcycle controller available");
var oldMode=Physics.simulationMode;bool wasEnabled=bike.enabled;try{
Physics.simulationMode=SimulationMode.Script;bike.enabled=false;bike.maxSpeed=8f;bike.reverseSpeed=8f;Set(bike,"currentSpeed",0f);Set(bike,"moveInput",1f);Set(bike,"isBraking",false);
bike.transform.rotation=root.rotation*Quaternion.Euler(0,180,0);var start=root.TransformPoint(new Vector3(0,.52f,8));bike.transform.position=start;var offset=Get<Vector3>(bike,"localOffset");bike.sphereRB.position=start-bike.transform.rotation*offset;bike.sphereRB.transform.position=bike.sphereRB.position;bike.sphereRB.linearVelocity=Vector3.zero;bike.sphereRB.angularVelocity=Vector3.zero;Physics.SyncTransforms();
int steps=0;while(root.InverseTransformPoint(bike.transform.position).z>.5f&&steps++<450){Invoke(bike,"FixedUpdate");Physics.Simulate(.02f);Invoke(bike,"LateUpdate");Physics.SyncTransforms();}
var parked=root.InverseTransformPoint(bike.transform.position);Check(parked.z<.65f&&parked.y>-.1f&&parked.y<2,"actual motorcycle drives from street across ramp into service bay: "+parked.ToString("F2")+" speed="+bike.CurrentSpeed+" input="+Get<float>(bike,"moveInput")+" fuel="+bike.GetComponent<MotorcycleFuel>().currentFuel+" durability="+(bike.durabilitySystem==null?-1:bike.durabilitySystem.currentDurability)+" velocity="+bike.sphereRB.linearVelocity+" constraints="+bike.sphereRB.constraints);
Set(bike,"moveInput",0f);Set(bike,"isBraking",true);for(int i=0;i<60;i++){Invoke(bike,"FixedUpdate");Physics.Simulate(.02f);Invoke(bike,"LateUpdate");}
Check(root.InverseTransformPoint(bike.transform.position).z>-1.7f,"bike can stop inside service area without reaching rear bench");
var cycle=UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();cycle.currentTime=12;Invoke(cycle,"UpdateLighting");
Capture("exterior",root.TransformPoint(new Vector3(13,7,19)),root.TransformPoint(new Vector3(0,2.1f,0)));
Capture("interior",root.TransformPoint(new Vector3(-4.1f,2.7f,3.8f)),root.TransformPoint(new Vector3(1,1.4f,-1.7f)));
cycle.currentTime=22;Invoke(cycle,"UpdateLighting");Capture("night",root.TransformPoint(new Vector3(10,5,17)),root.TransformPoint(new Vector3(0,2.1f,0)));
Set(bike,"isBraking",false);Set(bike,"moveInput",-1f);steps=0;while(root.InverseTransformPoint(bike.transform.position).z<7&&steps++<450){Invoke(bike,"FixedUpdate");Physics.Simulate(.02f);Invoke(bike,"LateUpdate");Physics.SyncTransforms();}
Check(root.InverseTransformPoint(bike.transform.position).z>=7,"actual motorcycle reverses out of garage to street");
}finally{Physics.simulationMode=oldMode;bike.enabled=wasEnabled;}
return log+"RESULT: PASS\n";}
}
#endif
