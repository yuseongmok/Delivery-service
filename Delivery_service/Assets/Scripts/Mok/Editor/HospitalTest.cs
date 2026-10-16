#if UNITY_EDITOR
using System;using System.Collections;using System.Linq;using UnityEditor;using UnityEngine;using UnityEngine.AI;
namespace DeliveryService.Mok.Editor {
public static class HospitalTest {
static void Check(bool v,string s,Action<string> log){if(!v)throw new Exception(s);log("PASS: "+s);}
public static IEnumerator RunCore(Action<string> log){
var p=UnityEngine.Object.FindFirstObjectByType<PlayerCombat>();Check(p.hospital!=null&&p.hospital.name=="HOSPITAL","Hospital scene reference saved",log);
var move=p.GetComponent<PlayerMove>();var cc=p.GetComponent<CharacterController>();move.enabled=false;move.isControllable=true;
var camera=p.GetComponentInChildren<CameraMove>();camera.enabled=false;Cursor.lockState=CursorLockMode.Locked;
Check(NavMesh.SamplePosition(new Vector3(138,0,76),out var spot,20,NavMesh.AllAreas),"Test location on navigation",log);
cc.enabled=false;p.transform.position=spot.position+Vector3.forward*2.4f;cc.enabled=true;
var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Scripts/Min/Prefab/NPC/Male_Adult_NPC_1.prefab");
var n=UnityEngine.Object.Instantiate(prefab,spot.position,Quaternion.identity).GetComponent<CitizenCombat>();yield return null;
Check(n.attackStartRange==2.6f&&n.punchRange==2.8f&&n.combatSpacing==2.2f,"Saved citizen distances updated",log);
n.Provoke(p);float deadline=Time.time+4;while(!p.IsDown&&Time.time<deadline)yield return null;
Check(p.IsDown&&p.HitsTaken==1,"Citizen punches from larger distance and knocks player down",log);
Check(!cc.enabled&&p.GetComponent<Rigidbody>()!=null&&!p.GetComponent<Rigidbody>().isKinematic,"Hit uses dynamic rolling body and disables controller",log);
int count=p.HitsTaken;p.ReceivePunch(10,Vector3.back);Check(p.HitsTaken==count,"Downed player cannot accumulate duplicate hits",log);
Check(!p.TryKick()&&!p.TryPickpocket(),"Downed player cannot kick or interact",log);
n.enabled=false;
deadline=Time.time+6;while(p.IsDown&&Time.time<deadline)yield return null;yield return null;
Check(!p.IsDown&&p.HitsTaken==1&&cc.enabled&&p.GetComponent<Rigidbody>()==null,"First hit restores controls and retains hit count",log);
Check(n.IsAggressive,"Citizen keeps hostility through nonfatal player knockdown",log);
for(int hit=2;hit<=3;hit++){
deadline=Time.time+1.4f;while(Time.time<deadline)yield return null;
p.ReceivePunch(10,Vector3.back);Check(p.IsDown&&p.HitsTaken==hit,"Accepted hit "+hit+" rolls player",log);
deadline=Time.time+6;while(p.IsDown&&Time.time<deadline)yield return null;yield return null;
if(hit==2)Check(p.HitsTaken==2,"Second hit retained without hospital respawn",log);
}
Check(p.HitsTaken==0&&!p.IsDown&&p.Health==p.maximumHealth,"Third hit resets combat counters",log);
Check(Vector3.Distance(p.transform.position,p.hospital.position)<.1f,"Third hit respawns exactly at HOSPITAL",log);
Check(!n.IsAggressive&&cc.enabled&&p.GetComponent<Rigidbody>()==null,"Hospital clears aggression and temporary physics",log);
Check(Quaternion.Angle(p.transform.rotation,Quaternion.Euler(0,p.hospital.eulerAngles.y,0))<1,"Hospital restores upright orientation",log);
// Exercise the actual motorcycle entry point that now uses the shared rolling implementation.
var bike=UnityEngine.Object.FindFirstObjectByType<MotorcycleController>();
var method=typeof(MotorcycleController).GetMethod("BailOutRoutine",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
var routine=(IEnumerator)method.Invoke(bike,new object[]{p.gameObject,2f,Vector3.forward});bike.StartCoroutine(routine);
yield return null;Check(!cc.enabled&&p.GetComponent<Rigidbody>()!=null,"Existing bike bail-out calls shared rolling physics",log);
deadline=Time.time+3.3f;while(Time.time<deadline)yield return null;
Check(cc.enabled&&p.GetComponent<Rigidbody>()==null,"Bike bail-out still restores player controls",log);
UnityEngine.Object.Destroy(n.gameObject);
}
}}
#endif
