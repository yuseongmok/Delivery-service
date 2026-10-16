#if UNITY_EDITOR
using System;using System.Collections;using System.IO;using System.Linq;using UnityEngine;using UnityEngine.AI;using UnityEngine.Rendering;using UnityEngine.Rendering.Universal;
namespace DeliveryService.Mok.Editor {
public static class PoliceArtTest {
static void Check(bool ok,string s,Action<string> log){if(!ok)throw new Exception(s);log("PASS: "+s);}
static void Capture(Transform target,string name,bool rear=false){
var o=new GameObject("Police Art camera");var c=o.AddComponent<Camera>();c.enabled=false;c.nearClipPlane=.05f;c.farClipPlane=60;c.fieldOfView=38;c.cullingMask=~((1<<5)|(1<<17)|(1<<18)|(1<<20)|(1<<22));c.transform.position=target.position+target.rotation*new Vector3(rear?-1.7f:1.7f,1.55f,rear?-3.1f:3.1f);c.transform.LookAt(target.position+Vector3.up*1.05f);
var rt=new RenderTexture(1000,1000,24);rt.Create();c.targetTexture=rt;var previous=RenderTexture.active;try{RenderPipeline.SubmitRenderRequest(c,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;var tex=new Texture2D(1000,1000,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1000,1000),0,0);tex.Apply();File.WriteAllBytes("Temp/Mok-police-"+name+".png",tex.EncodeToPNG());UnityEngine.Object.Destroy(tex);}finally{RenderTexture.active=previous;rt.Release();UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(o);}}
public static IEnumerator RunCore(Action<string> log){
var clock=UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();clock.currentTime=12;clock.timeMultiplier=0;
var p=UnityEngine.Object.FindFirstObjectByType<PlayerCombat>();p.GetComponent<PlayerMove>().enabled=false;var cc=p.GetComponent<CharacterController>();Check(NavMesh.SamplePosition(new Vector3(7.5f,0,47.5f),out var spot,20,NavMesh.AllAreas),"Pursuit test target is reachable",log);cc.enabled=false;p.transform.position=spot.position;cc.enabled=true;
UnityEngine.Object.FindFirstObjectByType<PoliceManager>().SpawnPoliceOnAccidentRefused();yield return null;var officers=UnityEngine.Object.FindObjectsByType<PoliceControl>(FindObjectsSortMode.None);Check(officers.Length>0,"Existing PoliceManager spawns updated prefab",log);var cop=officers[0];var agent=cop.GetComponent<NavMeshAgent>();
Check(NavMesh.SamplePosition(spot.position+Vector3.forward*6,out var spawn,2,NavMesh.AllAreas),"Police spawn point reachable",log);agent.Warp(spawn.position);agent.ResetPath();agent.speed=2;agent.stoppingDistance=.1f;typeof(PoliceControl).GetField("arrestDistance",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(cop,.01f);typeof(PoliceControl).GetField("flankDistance",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(cop,0f);foreach(var other in officers.Skip(1))UnityEngine.Object.Destroy(other.gameObject);
var mesh=cop.GetComponentInChildren<SkinnedMeshRenderer>();Check(mesh.sharedMaterials.Length==5&&mesh.sharedMesh.name.Contains("Uniform"),"Uniform uses separate skin, shirt, trousers, boots and hair",log);
var cap=cop.GetComponentsInChildren<Transform>().First(t=>t.name=="Police cap crown");Check(cap.parent.name=="Head","Police cap attached to head bone",log);Check(cop.GetComponentsInChildren<Transform>().Any(t=>t.name=="POLICE lettering"),"POLICE lettering saved on rig",log);
Check(cop.GetComponentsInChildren<Rigidbody>().All(b=>b.isKinematic),"Limb physics cannot fight the running animation",log);
var anim=cop.GetComponent<Animator>();Check(!anim.applyRootMotion&&anim.isHuman,"Humanoid preserved and navigation owns root movement",log);
float until=Time.time+3;while(agent.velocity.magnitude<.5f&&Time.time<until)yield return null;
until=Time.time+.35f;while(Time.time<until)yield return null;Check(anim.GetCurrentAnimatorStateInfo(0).IsName("Chase Run"),"Moving police transition to chase run",log);
var leg=anim.GetBoneTransform(HumanBodyBones.LeftLowerLeg);var rotation=leg.localRotation;Capture(cop.transform,"running-front");until=Time.time+.12f;while(Time.time<until)yield return null;Check(Quaternion.Angle(rotation,leg.localRotation)>3,"Running clip actually animates the leg",log);Capture(cop.transform,"running-step");
agent.isStopped=true;until=Time.time+.5f;while(Time.time<until)yield return null;Check(anim.GetCurrentAnimatorStateInfo(0).IsName("Stand"),"Stopped police return to standing animation",log);Capture(cop.transform,"standing-front");Capture(cop.transform,"standing-back",true);
Check(cop.RemainingChaseSeconds>0,"Existing chase timer continues normally",log);UnityEngine.Object.Destroy(cop.gameObject);yield return null;
}
}}
#endif
