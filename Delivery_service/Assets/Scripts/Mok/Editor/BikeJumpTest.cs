#if UNITY_EDITOR
using System;using System.Collections;using System.Linq;using System.Reflection;using UnityEngine;
namespace DeliveryService.Mok.Editor {
public static class BikeJumpTest {
static readonly BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
public static IEnumerator RunCore(Action<string> log){
foreach(var car in UnityEngine.Object.FindObjectsByType<CarMov>(FindObjectsSortMode.None)){car.enabled=false;foreach(var c in car.GetComponentsInChildren<Collider>())c.enabled=false;}
foreach(var npc in UnityEngine.Object.FindObjectsByType<NPCControl>(FindObjectsSortMode.None))npc.gameObject.SetActive(false);
UnityEngine.Object.FindFirstObjectByType<RobberyManager>().enabled=false;
var bike=UnityEngine.Object.FindFirstObjectByType<MotorcycleController>();bike.enabled=false;bike.sphereRB.gameObject.AddComponent<BikeContactProbe>();BikeContactProbe.Report.Clear();
log("Sphere radius/bounds="+bike.sphereCollider.bounds.size+" material="+(bike.sphereCollider.sharedMaterial?bike.sphereCollider.sharedMaterial.bounciness.ToString():"none")+" constraints="+bike.sphereRB.constraints+" collision="+bike.sphereRB.collisionDetectionMode);
foreach(var start in new[]{new Vector3(145,.65f,-5),new Vector3(293,.65f,-95),new Vector3(-80,.65f,-7.5f),new Vector3(-80,.65f,-5)}){
bool vertical=start.x==293;bike.transform.rotation=Quaternion.Euler(0,vertical?0:90,0);bike.sphereRB.position=start;bike.sphereRB.linearVelocity=Vector3.zero;bike.sphereRB.angularVelocity=Vector3.zero;typeof(MotorcycleController).GetField("currentSpeed",F).SetValue(bike,0f);typeof(MotorcycleController).GetField("moveInput",F).SetValue(bike,1f);bike.maxSpeed=30;Physics.SyncTransforms();float until=Time.time+8,maxY=0,maxV=0;
while(Time.time<until&&!(start.x==-80&&start.z==-5&&bike.sphereRB.position.x>8)){typeof(MotorcycleController).GetMethod("Movement",F).Invoke(bike,null);typeof(MotorcycleController).GetMethod("LateUpdate",F).Invoke(bike,null);maxY=Mathf.Max(maxY,bike.sphereRB.position.y);maxV=Mathf.Max(maxV,bike.sphereRB.linearVelocity.y);yield return new WaitForFixedUpdate();}
log("DRIVE "+start+" maxY="+maxY+" maxUpVelocity="+maxV+" end="+bike.sphereRB.position);if(maxY>.8f)throw new Exception("Road-only drive unexpectedly launched the bike");
}
log(BikeContactProbe.Report.ToString());
}
}
}
#endif
