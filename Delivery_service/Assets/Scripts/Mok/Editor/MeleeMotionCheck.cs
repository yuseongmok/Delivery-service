#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Collections;using UnityEngine;using UnityEngine.AI;
namespace DeliveryService.Mok.Editor {
public static class MeleeMotionCheck {
 static string log;static void Check(bool ok,string text){log+=(ok?"PASS ":"FAIL ")+text+"\n";File.WriteAllText("Logs/Melee-motion-check.txt",log);if(!ok)throw new Exception(text);}
 static IEnumerator Wait(float seconds){float end=Time.time+seconds;while(Time.time<end)yield return null;}
 public static IEnumerator Verify(){log="First-person melee motion and damage timing\n";yield return Wait(.5f);var m=VenueFinale.Current;var p=m.Player;var weapon=p.GetComponent<PlayerWeapon>();StoryPhoneInbox.Current.AutoOpenEnabled=false;UnityEngine.Object.FindFirstObjectByType<PhoneController>()?.ClosePhone();m.PlacePlayer(m.observeStand);p.GetComponent<PlayerMove>().enabled=false;p.View.GetComponent<CameraMove>().enabled=false;p.View.transform.rotation=Quaternion.identity;Cursor.lockState=CursorLockMode.Locked;yield return null;
 foreach(var kind in new[]{WeaponKind.Bat,WeaponKind.Tuna,WeaponKind.Brick}){
 weapon.Equip(kind);yield return Wait(.3f);var held=p.View.transform.Cast<Transform>().First(t=>t.name=="Held "+kind);
 for(int frame=0;frame<=100;frame++){PlayerWeapon.MeleePose(kind,frame/100f,out var pos,out var rot);held.SetLocalPositionAndRotation(pos,rot);foreach(var filter in held.GetComponentsInChildren<MeshFilter>())foreach(var v in filter.sharedMesh.vertices)if(p.View.transform.InverseTransformPoint(filter.transform.TransformPoint(v)).z<.10f)throw new Exception(kind+" behind camera at phase "+frame);}
 Check(true,kind+" entire mesh remains ahead of camera through swing");weapon.UpdateHeldPose();
 var enemy=UnityEngine.Object.Instantiate(m.preparation.enemyTemplate,p.transform.position+Vector3.forward*2,Quaternion.identity);enemy.gameObject.SetActive(true);yield return null;var agent=enemy.GetComponent<NavMeshAgent>();if(NavMesh.SamplePosition(m.observeStand.position+Vector3.forward*2,out var hit,2,NavMesh.AllAreas))agent.Warp(hit.position);enemy.enabled=false;enemy.Npc.ExternalControl=true;agent.isStopped=true;Physics.SyncTransforms();
 Check(weapon.Attack(false),kind+" attack starts");yield return Wait(WeaponCatalog.Cooldown[(int)kind]*.65f*.35f);Check(enemy.Health==100,kind+" windup does not deal early damage");BrokerMissionCheck.Capture(m.preparation.preparation.delivery.broker,"Melee-"+kind+"-windup");yield return Wait(WeaponCatalog.Cooldown[(int)kind]*.65f*.28f);Check(enemy.Health==100-WeaponCatalog.Damage[(int)kind],kind+" damage occurs once during forward strike");weapon.enabled=false;PlayerWeapon.MeleePose(kind,.52f,out var strikePosition,out var strikeRotation);held.SetLocalPositionAndRotation(strikePosition,strikeRotation);BrokerMissionCheck.Capture(m.preparation.preparation.delivery.broker,"Melee-"+kind+"-strike");weapon.enabled=true;yield return Wait(WeaponCatalog.Cooldown[(int)kind]);Check((held.localPosition-new Vector3(.32f,-.35f,.65f)).sqrMagnitude<.00001f,kind+" returns to stable rest pose");UnityEngine.Object.Destroy(enemy.gameObject);yield return null;
 }
 weapon.Equip(WeaponKind.None);p.GetComponent<PlayerMove>().enabled=true;p.View.GetComponent<CameraMove>().enabled=true;
 }
}
}
#endif
