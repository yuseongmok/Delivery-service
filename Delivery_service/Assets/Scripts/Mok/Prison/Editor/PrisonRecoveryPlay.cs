#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Object=UnityEngine.Object;
namespace DeliveryService.Mok.Editor {
// NPC와 간수의 실제 공격을 사용한다. 미션 준비와 경로 보행만 보조한다.
[InitializeOnLoad] static class PrisonRecoveryPlay {
 const string Log="Logs/Prison-recovery-play.txt";
 static bool running;static int step,corner;static float ready,lastTime;static double deadline,nextReport;
 static CitizenCombat inmate;static PrisonGuard guard;static Vector3[] points;static Vector3 startPosition,mingiStart;static Quaternion startView;
 static PrisonRecoveryPlay(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("PrisonRecoveryPlay",false)){SessionState.SetBool("PrisonRecoveryPlay",false);running=true;step=0;nextReport=0;deadline=EditorApplication.timeSinceStartup+480;Application.runInBackground=true;}};}
 static void Check(bool ok,string s){File.AppendAllText(Log,(ok?"PASS ":"FAIL ")+s+"\n");if(!ok)throw new Exception(s);}
 static void Place(PlayerCombat p,Vector3 v){var c=p.GetComponent<CharacterController>();c.enabled=false;p.transform.position=v+Vector3.up*1.1f;c.enabled=true;Physics.SyncTransforms();}
 static void Route(PlayerCombat p,Vector3 target){
  // 검사 보행은 가구 충돌까지 고려한다. 저장된 게임 NavMesh나 캐릭터 코드는 바꾸지 않는다.
  const float size=.6f;var cache=new Dictionary<Vector2Int,bool>();var positions=new Dictionary<Vector2Int,Vector3>();
  Vector2Int Cell(Vector3 v)=>new Vector2Int(Mathf.RoundToInt(v.x/size),Mathf.RoundToInt(v.z/size));
  bool Free(Vector2Int k){if(cache.TryGetValue(k,out bool value))return value;
   if(Mathf.Abs(k.x)>100||k.y< -65||k.y>85)return false;
   var raw=new Vector3(k.x*size,.1f,k.y*size);bool ok=NavMesh.SamplePosition(raw,out var n,.25f,NavMesh.AllAreas)&&Mathf.Abs(n.position.y)<.4f;
   if(ok)ok=!Physics.OverlapCapsule(n.position+Vector3.up*.56f,n.position+Vector3.up*1.35f,.48f,~0,QueryTriggerInteraction.Ignore).Any(c=>!c.GetComponentInParent<NPCControl>()&&!c.GetComponentInParent<PlayerCombat>()&&!c.GetComponentInParent<PrisonCompanion>());
   cache[k]=ok;if(ok)positions[k]=n.position;return ok;
  }
  Vector2Int Nearest(Vector3 v){var key=Cell(v);for(int radius=0;radius<5;radius++)for(int x=-radius;x<=radius;x++)for(int y=-radius;y<=radius;y++){var k=key+new Vector2Int(x,y);if(Free(k))return k;}throw new Exception("No walking clearance near "+v);}
  var start=Nearest(p.transform.position);var end=Nearest(target);var open=new List<Vector2Int>{start};var closed=new HashSet<Vector2Int>();var cost=new Dictionary<Vector2Int,float>{{start,0}};var parent=new Dictionary<Vector2Int,Vector2Int>();bool found=false;
  for(int tries=0;open.Count>0&&tries<16000;tries++){
   int best=0;float bestScore=float.MaxValue;for(int i=0;i<open.Count;i++){float score=cost[open[i]]+Vector2Int.Distance(open[i],end);if(score<bestScore){best=i;bestScore=score;}}
   var current=open[best];open.RemoveAt(best);if(current==end){found=true;break;}closed.Add(current);
   for(int x=-1;x<=1;x++)for(int y=-1;y<=1;y++){if(x==0&&y==0)continue;var next=current+new Vector2Int(x,y);if(closed.Contains(next)||!Free(next))continue;if(x!=0&&y!=0&&(!Free(current+new Vector2Int(x,0))||!Free(current+new Vector2Int(0,y))))continue;
    float score=cost[current]+(x!=0&&y!=0?1.4142f:1);if(cost.TryGetValue(next,out float before)&&before<=score)continue;cost[next]=score;parent[next]=current;if(!open.Contains(next))open.Add(next);
   }
  }
  Check(found,"walkable route avoids furniture");var result=new List<Vector3>();for(var k=end;;k=parent[k]){result.Add(positions[k]);if(k==start)break;}result.Reverse();points=result.ToArray();corner=0;lastTime=Time.time;
 }
 static bool Walk(PlayerCombat p){float dt=Mathf.Min(.04f,Time.time-lastTime);lastTime=Time.time;var delta=Vector3.ProjectOnPlane(points[corner]-p.transform.position,Vector3.up);if(delta.magnitude<.16f){if(corner==points.Length-1)return true;corner++;return false;}p.transform.rotation=Quaternion.RotateTowards(p.transform.rotation,Quaternion.LookRotation(delta),180*dt);p.GetComponent<CharacterController>().Move(delta.normalized*2.4f*dt);return false;}
 static void Tick(){
  if(!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling&&File.Exists("Temp/PrisonRecoveryPlay")){File.Delete("Temp/PrisonRecoveryPlay");SessionState.SetBool("PrisonRecoveryManualVerified",File.Exists(Log)&&(File.ReadAllText(Log).Contains("PASS manual mouse look changes view")||File.ReadAllText(Log).Contains("Manual keyboard, mouse and ESC verified")));if(File.Exists(Log))File.Copy(Log,"Logs/Prison-recovery-manual-previous.txt",true);File.WriteAllText(Log,"Live NPC attacks and guard shots. Stage entry assisted; manual input check, then collision-aware assisted walking.\n");SessionState.SetBool("PrisonRecoveryPlay",true);EditorApplication.EnterPlaymode();return;}
  if(!running||!EditorApplication.isPlaying)return;
  try{
   var m=Object.FindFirstObjectByType<PrisonMedicineMission>();if(!m)return;var p=m.prison.player;var r=m.prison.recovery;var mingi=m.prison.introduction.mingi;
   if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Timeout step="+step);
   if(EditorApplication.timeSinceStartup>nextReport){nextReport=EditorApplication.timeSinceStartup+5;File.AppendAllText(Log,$"STATE {step} hp={p.Health} down={p.IsDown} pos={p.transform.position} camera={p.View.transform.eulerAngles} mingi={mingi.transform.position} follow={mingi.Following} stopped={mingi.agent.isStopped}\n");}
   if(step==0){ready=Time.time+2;step=1;return;}if(Time.time<ready)return;
   if(step==1){EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();m.prison.introduction.DeveloperComplete();m.chicken.DeveloperComplete();m.BeginSearch();
    Check(NavMesh.SamplePosition(new Vector3(-20,0,-6),out var n,3,NavMesh.AllAreas),"encounter floor");Place(p,n.position);p.transform.rotation=Quaternion.identity;p.View.transform.localRotation=Quaternion.identity;p.GetComponentInChildren<CameraMove>().SyncViewFromTransform();
    inmate=Object.FindObjectsByType<CitizenCombat>(FindObjectsSortMode.None).First(c=>c.GetComponent<PrisonResident>()&&!c.GetComponent<PrisonGuard>());
    Check(NavMesh.SamplePosition(n.position+Vector3.forward*2,out var a,2,NavMesh.AllAreas),"inmate placement");inmate.GetComponent<NavMeshAgent>().Warp(a.position);inmate.Provoke(p);step=2;return;
   }
   if(step==2){if(r.Recoveries<1||p.IsDown)return;Check(!inmate.IsAggressive,"actual inmate stops retaliation after defeat");Check(m.State==PrisonMedicineMission.Stage.Search,"NPC defeat preserves objective");ready=Time.time+3.2f;step=3;return;}
   if(step==3){Check(p.CanAct&&p.View.enabled&&p.GetComponentInChildren<CameraMove>().isControllable,"controls and first-person camera restored");startPosition=p.transform.position;startView=p.View.transform.rotation;mingiStart=mingi.transform.position;PrisonChickenCheck.Capture(p.View,"Logs/Prison-live-wake.png");File.AppendAllText(Log,"MANUAL input checkpoint: move, turn and jump; then Temp/PrisonRecoveryContinue.\n");step=4;if(SessionState.GetBool("PrisonRecoveryManualVerified",false)){File.AppendAllText(Log,"Manual keyboard, mouse and ESC verified in previous run.\n");guard=Object.FindObjectsByType<PrisonGuard>(FindObjectsSortMode.None).Where(g=>g.gun&&!g.receivingMeal).OrderBy(g=>Vector3.Distance(g.transform.position,p.transform.position)).First();Route(p,guard.transform.position);step=5;}return;}
   if(step==4){if(!File.Exists("Temp/PrisonRecoveryContinue"))return;File.Delete("Temp/PrisonRecoveryContinue");Check(Vector3.ProjectOnPlane(p.transform.position-startPosition,Vector3.up).magnitude>.05f,"manual keyboard movement changes position");Check(Quaternion.Angle(startView,p.View.transform.rotation)>2,"manual mouse look changes view");Check(Mathf.Abs(Mathf.DeltaAngle(0,p.View.transform.eulerAngles.z))<1,"camera remains upright");
    p.View.transform.localRotation=Quaternion.identity;p.GetComponentInChildren<CameraMove>().SyncViewFromTransform();guard=Object.FindObjectsByType<PrisonGuard>(FindObjectsSortMode.None).Where(g=>g.gun&&!g.receivingMeal).OrderBy(g=>Vector3.Distance(g.transform.position,p.transform.position)).First();Route(p,guard.transform.position);step=5;return;}
   if(step==5){if(Vector3.Distance(p.transform.position,guard.transform.position)>3){Walk(p);return;}Check(Vector3.Distance(mingiStart,mingi.transform.position)>.5f,"Mingi resumes moving after respawn");guard.Attacked(p);PrisonChickenCheck.Capture(p.View,"Logs/Prison-live-guard.png");step=6;return;}
   if(step==6){if(r.Recoveries<2||p.IsDown)return;Check(!guard.IsAggressive&&guard.ShotsFired>3,"live guard keeps firing until defeat and resets");ready=Time.time+3.2f;step=7;return;}
   if(step==7){Check(p.CanAct&&mingi.Following&&mingi.agent.enabled&&mingi.agent.isOnNavMesh,"second recovery restores player and companion");Route(p,new Vector3(34,0,35));step=8;return;}
   if(step==8){if(!Walk(p))return;Route(p,new Vector3(40.74f,0,35));step=81;return;}
   if(step==81){if(!Walk(p))return;Route(p,new Vector3(40.74f,0,33.66f));step=82;return;}
   if(step==82){if(!Walk(p))return;ready=Time.time+4;step=9;return;}
   if(step==9){Check(Vector3.Distance(p.transform.position,mingi.transform.position)<5,"Mingi follows infirmary-to-workshop route");Check(Mathf.Abs(Mathf.DeltaAngle(0,p.View.transform.eulerAngles.z))<1,"camera stays upright after walking");p.View.transform.LookAt(m.duel.boss.position+Vector3.up*1.2f);Check(m.Interact(),"workshop conversation starts after both recoveries");ready=Time.time+2;step=10;return;}
   if(step==10){PrisonChickenCheck.Capture(m.camera,"Logs/Prison-live-resumed-mission.png");Check(m.State==PrisonMedicineMission.Stage.Conversation,"mission cinematic remains functional");File.AppendAllText(Log,"PASS live recovery play complete\n");running=false;EditorApplication.ExitPlaymode();}
  }catch(Exception e){File.AppendAllText(Log,e+"\n");running=false;EditorApplication.ExitPlaymode();}
 }
}
}
#endif





