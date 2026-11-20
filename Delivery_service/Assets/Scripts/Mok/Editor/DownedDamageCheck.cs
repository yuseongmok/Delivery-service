#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad] public static class DownedDamageCheck {
 const string Log="Logs/Downed-damage-check.txt",Key="DownedDamageCheck";
 static readonly string[] Scenes={"JJinCity","PrisonChapter","NewHarborCity"};
 static bool running;static int stage,sceneIndex;static double until,deadline;static CitizenCombat survivor;static float recoverAt;
 static DownedDamageCheck(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);running=true;stage=0;sceneIndex=SessionState.GetInt(Key+"Start",0);until=EditorApplication.timeSinceStartup+2;deadline=until+180;}};}
 static void Check(bool ok,string msg){File.AppendAllText(Log,(ok?"PASS ":"FAIL ")+Scenes[sceneIndex]+": "+msg+"\n");if(!ok)throw new Exception(msg);}
 static void Tick(){
  if(!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling&&File.Exists("Temp/DownedDamageCheck")){SessionState.SetInt(Key+"Start",File.ReadAllText("Temp/DownedDamageCheck").Trim()=="harbor"?2:0);File.Delete("Temp/DownedDamageCheck");File.WriteAllText(Log,"Shared enemy damage play-mode check\n");SessionState.SetBool(Key,true);EditorApplication.EnterPlaymode();return;}
  if(!running||!EditorApplication.isPlaying||EditorApplication.timeSinceStartup<until)return;
  try{
   if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Timed out");
   if(stage==0){SceneManager.LoadScene(Scenes[sceneIndex]);stage=1;until=EditorApplication.timeSinceStartup+5;return;}
   if(stage==1){
    var p=UnityEngine.Object.FindFirstObjectByType<PlayerCombat>();Check(p,"shared player found");
    var point=p.transform.position;Check(NavMesh.SamplePosition(point,out var nav,30,NavMesh.AllAreas),"test floor found");
    if(sceneIndex==2){bool found=false;for(int x=-80;x<=80&&!found;x+=10)for(int z=-80;z<=80&&!found;z+=10){if(!NavMesh.SamplePosition(new Vector3(x,0,z),out var candidate,1,NavMesh.AllAreas)||!NavMesh.FindClosestEdge(candidate.position,out var edge,NavMesh.AllAreas)||edge.distance<3)continue;if(Physics.CheckCapsule(candidate.position+Vector3.up*.6f,candidate.position+Vector3.up*1.6f,.4f,~0,QueryTriggerInteraction.Ignore))continue;nav=candidate;found=true;}Check(found,"open recovery test floor away from furniture and navigation edges");}
    CitizenCombat Spawn(){var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Scripts/Min/Prefab/NPC/Male_Adult_NPC_1.prefab");var c=UnityEngine.Object.Instantiate(prefab,nav.position,Quaternion.identity).GetComponent<CitizenCombat>();c.missionEnemy=true;return c;}
    var previous=p.GetComponent<CombatHitMarker>();int confirmations=previous?previous.ConfirmedHits:0;
    var c=Spawn();int deaths=0;c.Npc.OnNPCDeath+=_=>deaths++;
    Check(c.ReceiveKick(p,Vector3.zero,10)&&c.Npc.IsKnockedDown,"first melee hit creates ragdoll");
    Check(c.ReceiveKick(p,Vector3.zero,20)&&c.Health==70,"unarmed follow-up damages downed enemy");
    var w=p.GetComponent<PlayerWeapon>();if(!w)w=p.gameObject.AddComponent<PlayerWeapon>();
    var collider=c.GetComponentsInChildren<Collider>().First(x=>x.enabled&&!x.isTrigger&&x.attachedRigidbody&&!x.attachedRigidbody.isKinematic);
    Check(w.Hit(collider,Vector3.zero,15)&&c.Health==55,"weapon / thrown-brick routing reaches ragdoll bone");
    Check(c.ReceiveBullet(p,Vector3.zero,15)&&c.Health==40,"bullet follow-up damages downed enemy");
    Check(!c.ReceiveKick(p,Vector3.zero,0)&&!c.ReceiveBullet(p,Vector3.zero,-10)&&c.Health==40,"invalid damage rejected");Check(p.GetComponent<CombatHitMarker>().ConfirmedHits==confirmations+4&&p.GetComponent<CombatHitMarker>().Remaining>0&&!p.GetComponent<CombatHitMarker>().Fatal,"standing and downed hits each refresh confirmation, invalid damage does not");
    Check(c.ReceiveBullet(p,Vector3.zero,100)&&c.Npc.IsDead&&deaths==1,"lethal follow-up invokes death once");
    Check(!c.ReceiveKick(p,Vector3.zero,20)&&!c.ReceiveBullet(p,Vector3.zero,20)&&deaths==1,"dead enemy rejects further damage");Check(p.GetComponent<CombatHitMarker>().ConfirmedHits==confirmations+5&&p.GetComponent<CombatHitMarker>().Fatal,"fatal marker appears once; corpse hits do not confirm");
    foreach(var bodyCollider in c.GetComponentsInChildren<Collider>())bodyCollider.enabled=false;UnityEngine.Object.Destroy(c.gameObject);
    survivor=Spawn();File.AppendAllText(Log,"SPAWN "+nav.position+"\n");survivor.ReceiveKick(p,Vector3.zero,10);survivor.ReceiveBullet(p,Vector3.zero,10);survivor.ReceiveKick(p,Vector3.zero,10);
    var police=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Scripts/Min/Prefab/NPC/Police.prefab"),nav.position+Vector3.right*3,Quaternion.identity);police.GetComponent<PoliceControl>().enabled=false;
    var pc=police.GetComponent<PoliceCombat>();if(!pc)pc=police.AddComponent<PoliceCombat>();
    Check(pc.ReceiveHit(p,Vector3.zero,10)&&pc.IsDown,"police knocked down");Check(w.Hit(police.GetComponent<Collider>(),Vector3.zero,20)&&pc.Health==70,"police takes downed follow-up");UnityEngine.Object.Destroy(police);
    stage=2;recoverAt=Time.time+6;return;
   }
   if(stage==2){if(Time.time<recoverAt)return;File.AppendAllText(Log,"RECOVERY nav="+NavMesh.SamplePosition(survivor.Npc.RagdollCenter.position,out var landing,5,NavMesh.AllAreas)+" closest="+landing.position+" health="+(survivor?survivor.Health:-1)+" down="+(survivor&&survivor.Npc.IsKnockedDown)+" position="+(survivor?survivor.Npc.RagdollCenter.position:Vector3.zero)+"\n");Check(survivor&&survivor.Health==70&&!survivor.Npc.IsKnockedDown,"multiple hits preserve health and single normal recovery");UnityEngine.Object.Destroy(survivor.gameObject);sceneIndex++;if(sceneIndex<Scenes.Length){stage=0;return;}File.AppendAllText(Log,"FINISHED\n");running=false;EditorApplication.ExitPlaymode();}
  }catch(Exception e){File.AppendAllText(Log,e+"\n");running=false;EditorApplication.ExitPlaymode();}
 }
}
}
#endif
