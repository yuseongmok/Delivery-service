#if UNITY_EDITOR
using System;using System.Collections;using System.Linq;using System.IO;using UnityEditor;using UnityEngine;using UnityEngine.AI;using UnityEngine.Rendering;using UnityEngine.Rendering.Universal;
namespace DeliveryService.Mok.Editor {
public static class CombatTest {
static void Check(bool value,string text,Action<string> log){if(!value)throw new Exception(text);log("PASS: "+text);}
static void Capture(CitizenCombat npc,string name){var obj=new GameObject("Combat capture");var c=obj.AddComponent<Camera>();c.enabled=false;c.fieldOfView=50;c.nearClipPlane=.05f;c.transform.position=npc.transform.position+new Vector3(2,1.7f,2);c.transform.LookAt(npc.transform.position+Vector3.up*.95f);var rt=new RenderTexture(1000,900,24);rt.Create();var prev=RenderTexture.active;try{RenderPipeline.SubmitRenderRequest(c,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;var tex=new Texture2D(1000,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1000,900),0,0);tex.Apply();File.WriteAllBytes("Temp/Mok-combat-"+name+".png",tex.EncodeToPNG());UnityEngine.Object.Destroy(tex);}finally{RenderTexture.active=prev;rt.Release();UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(obj);}}
static void CapturePlayer(PlayerCombat p){var c=p.View;var rt=new RenderTexture(1000,900,24);rt.Create();var prev=RenderTexture.active;try{RenderPipeline.SubmitRenderRequest(c,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;var tex=new Texture2D(1000,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1000,900),0,0);tex.Apply();File.WriteAllBytes("Temp/Mok-combat-kick.png",tex.EncodeToPNG());UnityEngine.Object.Destroy(tex);}finally{RenderTexture.active=prev;rt.Release();UnityEngine.Object.Destroy(rt);}}
public static IEnumerator RunCore(Action<string> log){
var cycle=UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();cycle.currentTime=12;cycle.isShopOpen=true;cycle.timeMultiplier=0;typeof(DayNightCycle).GetMethod("UpdateLighting",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(cycle,null);
var p=UnityEngine.Object.FindFirstObjectByType<PlayerCombat>();Check(p!=null&&p.kickVisual!=null,"Player and saved kick model installed",log);
var prefabs=AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Scripts/Min/Prefab/NPC"}).Select(AssetDatabase.GUIDToAssetPath).Where(x=>Path.GetFileName(x).Contains("_NPC_")).ToArray();
foreach(var path in prefabs){var o=AssetDatabase.LoadAssetAtPath<GameObject>(path);Check(o.GetComponent<CitizenCombat>()!=null&&o.GetComponentsInChildren<Rigidbody>().Length>5,"Combat and original ragdoll retained: "+Path.GetFileName(path),log);}
var move=p.GetComponent<PlayerMove>();move.enabled=false;move.isControllable=true;foreach(var look in p.GetComponentsInChildren<CameraMove>())look.enabled=false;
foreach(var bike in UnityEngine.Object.FindObjectsByType<MotorcycleController>(FindObjectsSortMode.None))bike.isDriven=false;
UnityEngine.Object.FindFirstObjectByType<PhoneController>()?.ClosePhone();Cursor.lockState=CursorLockMode.Locked;
Check(NavMesh.SamplePosition(new Vector3(138,0,76),out var spot,30f,NavMesh.AllAreas),"Combat test has baked navigation",log);
var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(prefabs.First(x=>x.EndsWith("Male_Adult_NPC_1.prefab")));
var citizen=UnityEngine.Object.Instantiate(prefab,spot.position,Quaternion.identity).GetComponent<CitizenCombat>();
var cc=p.GetComponent<CharacterController>();cc.enabled=false;p.transform.position=spot.position+Vector3.forward*1.2f;p.transform.rotation=Quaternion.Euler(0,180,0);p.View.transform.position=p.transform.position+Vector3.up*1.25f;p.View.transform.rotation=Quaternion.LookRotation(citizen.transform.position+Vector3.up*1.15f-p.View.transform.position);cc.enabled=true;
yield return null;Physics.SyncTransforms();
Check(p.FocusCitizen()==citizen,"E interaction can target existing civilian collider",log);
int cash=MoneyManager.Instance.currentMoney;Check(p.TryPickpocket(),"Player interaction starts theft",log);int gained=MoneyManager.Instance.currentMoney-cash;Check(gained>=100&&gained<=5000,"Random reward within 100..5000 inclusive",log);
Check(!citizen.Pickpocket(p)&&MoneyManager.Instance.currentMoney==cash+gained,"Same citizen cannot pay twice",log);Check(citizen.IsAggressive&&citizen.Npc.ExternalControl,"Theft takes over wandering to chase",log);
int health=p.Health;float until=Time.time+4;bool captured=false;float windup=0;while(Time.time<until&&p.Health==health){if(citizen.IsPunching){windup+=Time.deltaTime;if(windup>.34f&&!captured){Capture(citizen,"punch");captured=true;}}yield return null;}
Check(p.Health<health,"Punch animation applies player damage",log);
until=Time.time+4;while(p.IsDown&&Time.time<until)yield return null;
Check(!p.IsDown,"Player recovers from punch before kick regression",log);
citizen.GetComponent<NavMeshAgent>().Warp(spot.position);cc.enabled=false;p.transform.position=spot.position+Vector3.forward*1.2f;cc.enabled=true;p.View.transform.LookAt(citizen.transform.position+Vector3.up*1.1f);Physics.SyncTransforms();
Check(p.TryKick()&&!p.TryKick(),"Left-click action starts kick and rejects repeated input",log);
until=Time.time+1;bool kickCaptured=false;while(Time.time<until&&p.IsKicking){if(!kickCaptured&&p.kickVisual.localPosition.z>.34f){CapturePlayer(p);kickCaptured=true;}yield return null;}
Check(kickCaptured,"Visible player leg extends during kick",log);
Check(citizen.Health==70&&citizen.Npc.IsKnockedDown,"Kick deals 30 and enables temporary ragdoll",log);Capture(citizen,"ragdoll");
Check(!citizen.ReceiveKick(p,Vector3.back,30),"Downed NPC cannot take duplicate kick hits",log);
until=Time.time+7;while(Time.time<until&&citizen.Npc.IsKnockedDown)yield return null;
Check(!citizen.Npc.IsKnockedDown&&citizen.IsAggressive&&citizen.GetComponent<NavMeshAgent>().enabled,"Citizen stands up and resumes chase",log);
for(int i=0;i<2;i++){Check(citizen.ReceiveKick(p,Vector3.back,30),"Next kick accepted after recovery",log);until=Time.time+7;while(Time.time<until&&citizen.Npc.IsKnockedDown)yield return null;}
Check(citizen.Health==10,"Three kicks leave 10 health",log);int deaths=0;citizen.Npc.OnNPCDeath+=_=>deaths++;
Check(citizen.ReceiveKick(p,Vector3.back,30)&&citizen.Health==0&&citizen.Npc.IsDead,"Fourth kick uses existing death ragdoll",log);Check(deaths==1,"Death event fires once for existing population manager",log);
until=Time.time+5.5f;while(Time.time<until)yield return null;Check(citizen==null,"Dead citizen removed after existing five-second delay",log);
// 실제 조작과 같은 레이·구체 검사로 벽 차단을 확인한다.
citizen=UnityEngine.Object.Instantiate(prefab,spot.position,Quaternion.identity).GetComponent<CitizenCombat>();yield return null;
cc.enabled=false;p.transform.position=spot.position+Vector3.forward*1.3f;cc.enabled=true;p.View.transform.LookAt(citizen.transform.position+Vector3.up*1.1f);
var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=spot.position+new Vector3(0,1,.65f);wall.transform.localScale=new Vector3(2,2,.15f);Physics.SyncTransforms();
Check(!p.TryPickpocket()&&!p.ResolveKick()&&citizen.Health==100,"Walls block both theft and kick",log);UnityEngine.Object.Destroy(wall);
Cursor.lockState=CursorLockMode.None;Check(!p.TryKick()&&!p.TryPickpocket(),"Unlocked UI cursor blocks combat input",log);Cursor.lockState=CursorLockMode.Locked;
UnityEngine.Object.Destroy(citizen.gameObject);
// 공격 상태뿐 아니라 실제 경로 이동도 확인한다.
citizen=UnityEngine.Object.Instantiate(prefab,spot.position,Quaternion.identity).GetComponent<CitizenCombat>();yield return null;
Check(NavMesh.SamplePosition(spot.position+Vector3.forward*6,out var distant,4,NavMesh.AllAreas),"Distant chase destination exists",log);
cc.enabled=false;p.transform.position=distant.position;cc.enabled=true;citizen.Provoke(p);Vector3 original=citizen.transform.position;
until=Time.time+2;while(Time.time<until)yield return null;
Check(Vector3.Distance(original,citizen.transform.position)>.5f,"Aggressive citizen actually follows player on NavMesh",log);
citizen.EndFight();Check(!citizen.Npc.ExternalControl&&!citizen.IsAggressive,"Fight end returns control to existing wander behavior",log);
UnityEngine.Object.Destroy(citizen.gameObject);
}
}}
#endif
