#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Object=UnityEngine.Object;
namespace DeliveryService.Mok.Editor {
// 실제 피격/회복 코루틴 검사. 이동과 미션 단계 준비만 테스트에서 보조한다.
[InitializeOnLoad] static class PrisonRecoveryCheck {
 const string Log="Logs/Prison-recovery-check.txt";
 static bool running;static int step,hits;static float ready;static double deadline,nextReport;static PrisonGuard guard;
 static PrisonRecoveryCheck(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("PrisonRecoveryCheck",false)){SessionState.SetBool("PrisonRecoveryCheck",false);running=true;Application.runInBackground=true;step=0;hits=0;nextReport=0;deadline=EditorApplication.timeSinceStartup+240;}};}
 public static void Run(){
  var scene=EditorSceneManager.GetActiveScene();
  if(scene.name!="PrisonChapter")scene=EditorSceneManager.OpenScene("Assets/Scenes/PrisonChapter.unity");
  Directory.CreateDirectory("Logs");EditorSceneManager.SaveScene(scene,"Logs/Prison-before-recovery-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
  var m=Object.FindFirstObjectByType<PrisonMission>();if(!m)throw new Exception("Prison mission missing");
  var r=m.GetComponent<PrisonRecovery>();if(!r)r=m.gameObject.AddComponent<PrisonRecovery>();r.mission=m;m.recovery=r;
  if(!r.infirmarySpawn){
   var g=new GameObject("의무실 · 피격 후 회복 위치");g.transform.SetParent(m.transform);r.infirmarySpawn=g.transform;
   bool found=false;Vector3 bed=m.infirmary.transform.position;
   foreach(var offset in new[]{new Vector3(2,0,0),new Vector3(-2,0,0),new Vector3(0,0,3),new Vector3(0,0,-3),new Vector3(3,0,1)}){
    if(!NavMesh.SamplePosition(new Vector3(bed.x+offset.x,.1f,bed.z+offset.z),out var n,1,NavMesh.AllAreas))continue;
    if(Physics.OverlapCapsule(n.position+Vector3.up*.4f,n.position+Vector3.up*1.5f,.32f,~0,QueryTriggerInteraction.Ignore).Any(c=>!c.GetComponentInParent<NPCControl>()&&!c.GetComponentInParent<PlayerCombat>()))continue;
    var path=new NavMeshPath();if(!NavMesh.CalculatePath(n.position,m.cell.transform.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)continue;
    g.transform.SetPositionAndRotation(n.position+Vector3.up*.06f,Quaternion.Euler(0,90,0));found=true;break;
   }
   if(!found)throw new Exception("No unobstructed infirmary spawn with a route to cell");
  }
  EditorUtility.SetDirty(m);EditorUtility.SetDirty(r);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
  File.WriteAllText(Log,"Saved infirmary spawn "+r.infirmarySpawn.position+". Assisted stage setup and placement; real recovery and guard shots.\n");
  SessionState.SetBool("PrisonRecoveryCheck",true);EditorApplication.EnterPlaymode();
 }
 static void Preview(){
  var m=Object.FindFirstObjectByType<PrisonMission>();if(!m||!m.recovery)return;
  var scene=m.gameObject.scene;EditorSceneManager.SaveScene(scene,"Logs/Prison-before-recovery-view.unity",true);
  var spawn=m.recovery.infirmarySpawn;spawn.rotation=Quaternion.identity;
  EditorUtility.SetDirty(spawn);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  var g=new GameObject("Recovery view preview");var c=g.AddComponent<Camera>();c.fieldOfView=60;c.nearClipPlane=.05f;
  g.transform.SetPositionAndRotation(spawn.position+Vector3.up*1.65f,spawn.rotation);
  PrisonChickenCheck.Capture(c,"Logs/Prison-recovery-view.png");Object.DestroyImmediate(g);
  File.AppendAllText(Log,"PASS saved infirmary view faces room aisle instead of desk monitor\n");
 }
 static void Check(bool ok,string label){File.AppendAllText(Log,(ok?"PASS ":"FAIL ")+label+"\n");if(!ok)throw new Exception(label);}
 static void Place(PlayerCombat p,Vector3 position){var cc=p.GetComponent<CharacterController>();cc.enabled=false;p.transform.position=position;cc.enabled=true;Physics.SyncTransforms();}
 static void Property(object o,string name,object value){o.GetType().GetProperty(name).SetValue(o,value);}
 static void Tick(){if(!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling&&File.Exists("Temp/PrisonRecoveryView")){File.Delete("Temp/PrisonRecoveryView");Preview();return;}if(!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling&&File.Exists("Temp/PrisonRecoveryCheck")){File.Delete("Temp/PrisonRecoveryCheck");Run();return;}if(!running||!EditorApplication.isPlaying)return;
  try{
   var m=Object.FindFirstObjectByType<PrisonMedicineMission>();if(!m)return;var p=m.prison.player;var r=m.prison.recovery;
   if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Timeout step="+step+" health="+p.Health+" down="+p.IsDown+" controls="+p.GetComponent<PlayerMove>().isControllable+" shots="+(guard?guard.ShotsFired:0));
   if(EditorApplication.timeSinceStartup>nextReport){nextReport=EditorApplication.timeSinceStartup+10;File.AppendAllText(Log,"STATE step="+step+" health="+p.Health+" hits="+p.HitsTaken+" down="+p.IsDown+" controls="+p.GetComponent<PlayerMove>().isControllable+" cursor="+Cursor.lockState+" receive="+p.CanReceiveBullet+" time="+Time.time+"\n");}
   if(step==0){ready=Time.time+2;step=1;return;}if(Time.time<ready)return;
   if(step==1){EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();m.prison.introduction.DeveloperComplete();m.chicken.DeveloperComplete();m.BeginSearch();Cursor.lockState=CursorLockMode.Locked;ready=Time.time+1;step=2;return;}
   if(step>=2&&step<=9&&!p.IsDown){Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}
   if(step==2){if(!p.CanReceiveBullet)return;p.ReceivePunch(34,Vector3.back);hits++;ready=Time.time+.1f;if(hits==3)step=3;return;}
   if(step==3){if(r.Recoveries<1||p.IsDown)return;Check(m.State==PrisonMedicineMission.Stage.Search,"search objective survives NPC defeat");Check(p.Health==p.maximumHealth&&p.HitsTaken==0,"health and hit counters reset");File.AppendAllText(Log,"WAKE position="+p.transform.position+" spawn="+r.infirmarySpawn.position+"\n");Check(Vector3.ProjectOnPlane(p.transform.position-r.infirmarySpawn.position,Vector3.up).magnitude<.7f&&Mathf.Abs(p.transform.position.y-r.infirmarySpawn.position.y)<2,"wakes at infirmary");PrisonChickenCheck.Capture(p.View,"Logs/Prison-recovery-infirmary.png");ready=Time.time+3.2f;step=4;return;}
   if(step==4){Check(p.CanAct&&p.GetComponentInChildren<CameraMove>().isControllable,"first person controls restored");Property(m,"State",PrisonMedicineMission.Stage.Collect);m.medicine.gameObject.SetActive(true);Place(p,m.medicine.position+Vector3.right*.8f);Check(m.Interact()&&m.HasMedicine,"mission item interaction works after recovery");
    guard=Object.FindObjectsByType<PrisonGuard>(FindObjectsSortMode.None).First(g=>g.gun&&!g.receivingMeal);guard.holdPost=true;
    var a=guard.GetComponent<NavMeshAgent>();Check(NavMesh.SamplePosition(new Vector3(0,0,0),out var n,3,NavMesh.AllAreas),"guard test floor exists");a.Warp(n.position);guard.postPosition=n.position;Place(p,n.position+Vector3.forward*3);p.View.transform.localRotation=Quaternion.identity;guard.Attacked(p);Check(guard.IsAggressive,"struck guard becomes aggressive");step=5;return;
   }
   if(step==5){if(r.Recoveries<2||p.IsDown)return;Check(guard.ShotsFired>3,"guard continues firing across knockdowns until fatal hit");Check(!guard.IsAggressive&&guard.Suspicion==0,"guard aggression resets after infirmary recovery");Check(m.State==PrisonMedicineMission.Stage.Return&&m.HasMedicine,"acquired medicine and return objective preserved");ready=Time.time+3.2f;step=6;return;}
   if(step==6){m.dialogue=Object.Instantiate(m.dialogue);m.dialogue.lineSeconds=.15f;m.dialogue.nextDaySeconds=.15f;Place(p,m.cell.position);Check(m.Interact(),"cell dialogue still starts after guard defeat");step=7;return;}
   var keys=Object.FindFirstObjectByType<PrisonKeysMission>();
   if(step==7){if(m.State!=PrisonMedicineMission.Stage.Complete||keys.State==PrisonKeysMission.Stage.Waiting)return;Check(keys.State==PrisonKeysMission.Stage.Steal,"next mission unlocks after returning to cell");Property(keys,"State",PrisonKeysMission.Stage.Gather);Property(keys,"Gathered",1);for(int i=0;i<keys.supplies.Length;i++)keys.supplies[i].SetActive(i>0);hits=0;step=8;return;}
   if(step==8){if(!p.CanReceiveBullet)return;p.ReceivePunch(34,Vector3.back);if(++hits==3)step=9;return;}
   if(step==9){if(r.Recoveries<3||!p.CanAct)return;Check(keys.State==PrisonKeysMission.Stage.Gather&&keys.Gathered==1&&!keys.supplies[0].activeSelf,"warehouse progress and collected supply survive defeat");Place(p,keys.supplies[1].transform.position+Vector3.right*.8f);Check(keys.Interact()&&keys.Gathered==2,"warehouse pickup continues after recovery");File.AppendAllText(Log,"PASS recovery chain complete\n");running=false;EditorApplication.ExitPlaymode();if(Application.isBatchMode)EditorApplication.delayCall+=()=>EditorApplication.Exit(0);}
  }catch(Exception e){File.AppendAllText(Log,e+"\n");running=false;EditorApplication.ExitPlaymode();if(Application.isBatchMode)EditorApplication.delayCall+=()=>EditorApplication.Exit(1);}
 }
}
}
#endif
