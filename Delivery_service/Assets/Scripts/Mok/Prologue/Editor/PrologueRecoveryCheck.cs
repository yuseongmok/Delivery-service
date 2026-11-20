#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad] public static class PrologueRecoveryCheck {
 const string Key="PrologueRecoveryCheck",Log="Logs/Prologue-recovery-check.txt";
 static bool running; static int step,shots; static double until,deadline; static float recovered;
 static PrologueOfficer enemy; static BankPrologue d;
 static PrologueRecoveryCheck(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);running=true;step=0;until=EditorApplication.timeSinceStartup+2;deadline=until+100;}};}
 static void Check(bool ok,string msg){File.AppendAllText(Log,(ok?"PASS ":"FAIL ")+msg+"\n");if(!ok)throw new Exception(msg);}
 static void Tick(){
  if(!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling&&File.Exists("Temp/PrologueRecoveryCheck")){File.Delete("Temp/PrologueRecoveryCheck");File.WriteAllText(Log,"Prologue play-mode check\n");SessionState.SetBool(Key,true);EditorApplication.EnterPlaymode();return;}
  if(!running||!EditorApplication.isPlaying||EditorApplication.timeSinceStartup<until)return;
  try{
   if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Timed out step "+step);
   if(step==0){SceneManager.LoadScene("BankPrologue");step++;until=EditorApplication.timeSinceStartup+4;return;}
   if(step==1){d=UnityEngine.Object.FindFirstObjectByType<BankPrologue>();d.StopAllCoroutines();d.BeginRobbery();foreach(var o in d.officers)o.Stop();enemy=d.officers.First(o=>!o.roadGuard);enemy.Activate();d.player.Teleport(new Vector3(0,.05f,2));enemy.GetComponent<NavMeshAgent>().Warp(new Vector3(0,.05f,6));Check(enemy.ReceiveHit(10)&&enemy.ragdoll.IsDown,"first shot knocks enemy down");Check(enemy.ReceiveHit(10)&&enemy.Health==50,"downed enemy takes additional damage");shots=enemy.ShotsFired;step++;return;}
   if(step==2){if(enemy.ragdoll.IsDown)return;recovered=Time.time;step++;return;}
   if(step==3){if(Time.time-recovered<1.5f){CheckSilent(enemy.ShotsFired==shots,"shot fired before recovery aim delay");return;}Check(enemy.ShotsFired==shots,"no immediate shot during first 1.5 seconds after recovery");Check(enemy.ReceiveHit(10)&&enemy.ragdoll.IsDown,"second knockdown begins");Check(enemy.ReceiveHit(100)&&enemy.Dead&&enemy.ragdoll.Permanent,"lethal follow-up while down cancels recovery");step++;until=EditorApplication.timeSinceStartup+4;return;}
   if(step==4){Check(enemy.Dead&&enemy.ragdoll.IsDown,"dead enemy cannot recover");foreach(var o in d.officers)o.Stop();typeof(BankPrologue).GetProperty("Current").SetValue(d,BankPrologue.Phase.Escape);d.player.Teleport(d.exitPoint.position);var a=d.accomplice;var p=d.getawayCar.TransformPoint(new Vector3(-2.5f,0,0));Check(NavMesh.SamplePosition(p,out var nav,2,NavMesh.AllAreas),"driver-side test start on NavMesh");a.GetComponent<NavMeshAgent>().Warp(nav.position);step++;until=EditorApplication.timeSinceStartup+.5;return;}
   if(step==5){var a=d.accomplice;var box=d.getawayCar.GetComponent<BoxCollider>();var p=box.transform.InverseTransformPoint(a.transform.position)-box.center;CheckSilent(Mathf.Abs(p.x)>=box.size.x*.5f||Mathf.Abs(p.z)>=box.size.z*.5f,"companion crossed vehicle body");if(!a.ReadyToBoard)return;Check(true,"companion walks around car from driver side to passenger door");Check(Vector3.Distance(a.transform.position,a.performance.passengerStand.position)<.55f,"boarding begins at passenger door");a.performance.StartCoroutine(a.performance.EnterVehicle());step++;return;}
   if(step==6){var a=d.accomplice;if(!a.performance.EnteredCar)return;Check(a.Boarded&&!d.getawayCar.GetComponent<NavMeshObstacle>().enabled,"seated companion releases car navigation obstacle");Check(Quaternion.Angle(a.performance.passengerDoor.localRotation,Quaternion.identity)<1,"passenger door closes after boarding");File.AppendAllText(Log,"FINISHED\n");running=false;EditorApplication.ExitPlaymode();}
  }catch(Exception e){File.AppendAllText(Log,e+"\n");running=false;EditorApplication.ExitPlaymode();}
 }
 static void CheckSilent(bool ok,string msg){if(!ok)Check(false,msg);}
}
}
#endif
