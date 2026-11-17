#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.AI;
using Object=UnityEngine.Object;
namespace DeliveryService.Mok.Editor
{
    // 조리는 완료 상태에서 시작한다. 실제 AI 접근/공격, 관절 물리, 실패 복구와 후속 전달을 검증한다.
    [InitializeOnLoad] static class PrisonChickenDefenseCheck
    {
        static bool visualOnly;
        static string Log=>visualOnly?"Logs/Prison-chicken-defense-visual-check.txt":"Logs/Prison-chicken-defense-check.txt";
        static bool running,handoffCaptured;
        static int step,lastHits;
        static double deadline;
        static float until,handoffAt;
        static PrisonChickenDefenseCheck(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("ChickenDefenseCheck",false)){SessionState.SetBool("ChickenDefenseCheck",false);visualOnly=SessionState.GetBool("ChickenDefenseVisualOnly",false);running=true;step=lastHits=0;deadline=EditorApplication.timeSinceStartup+180;}};}
        static void Check(bool ok,string message){File.AppendAllText(Log,(ok?"PASS ":"FAIL ")+message+"\n");if(!ok)throw new Exception(message);}
        static void Set(object owner,string property,object value)=>owner.GetType().GetProperty(property).SetValue(owner,value);
        static void Place(PrisonChickenMission m,Vector3 position){var cc=m.prison.player.GetComponent<CharacterController>();cc.enabled=false;m.prison.player.transform.position=position;cc.enabled=true;}
        static void Tick()
        {
            if(!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling&&(File.Exists("Temp/PrisonChickenDefenseCheck")||File.Exists("Temp/PrisonChickenDefenseVisualCheck"))){
                visualOnly=File.Exists("Temp/PrisonChickenDefenseVisualCheck");SessionState.SetBool("ChickenDefenseVisualOnly",visualOnly);File.Delete("Temp/PrisonChickenDefenseVisualCheck");File.Delete("Temp/PrisonChickenDefenseCheck");if(EditorSceneManager.GetActiveScene().name!="PrisonChapter")return;
                File.WriteAllText(Log,"Chicken defense play mode validation; cooking seeded, combat AI unmodified.\n");SessionState.SetBool("ChickenDefenseCheck",true);EditorApplication.EnterPlaymode();return;
            }
            if(!running||!EditorApplication.isPlaying)return;
            try{
                var m=Object.FindFirstObjectByType<PrisonChickenMission>();if(!m)return;
                var d=m.defense;var intro=m.prison.introduction;
                if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Timeout step="+step+" state="+m.State+" ready="+d.Ready+" hits="+d.MingiHits+" player="+m.prison.player.transform.position+" mingi="+intro.mingi.transform.position);
                if(step==0){until=Time.time+2;step=1;return;}
                if(Time.time<until)return;
                if(step==2&&m.camera.enabled&&!handoffCaptured&&m.State==PrisonChickenMission.Stage.Fight){if(handoffAt==0)handoffAt=Time.time;if(Time.time-handoffAt>1.4f){PrisonChickenCheck.Capture(m.camera,"Logs/Prison-chicken-defense-receive.png");handoffCaptured=true;}}
                if(step==1){
                    intro.DeveloperComplete();Check(d&&d.ragdoll.bodies.Length>=6,"saved Mingi physical rig");
                    m.dialogue=Object.Instantiate(m.dialogue);m.dialogue.lineSeconds=.15f;m.dialogue.encounterLineSeconds=.7f;
                    NavMesh.SamplePosition(m.ambushPoint.position,out var spot,2,NavMesh.AllAreas);Place(m,spot.position);
                    intro.mingi.agent.enabled=true;intro.mingi.agent.Warp(spot.position+Vector3.back*2);
                    handoffAt=0;handoffCaptured=false;m.FinishCooking();step=2;return;
                }
                if(step==2&&d.Ready){
                    Check(d.HiddenSpawns==m.enemies.Length,"every enemy entered through offscreen spawn selection");
                    Check(m.carrying.activeInHierarchy&&m.carrying.transform.parent!=m.prison.player.View.transform,"Mingi takes cooked chicken");
                    if(visualOnly){Check(m.prison.player.CanAct&&!m.camera.enabled,"handoff restores controls and camera");running=false;EditorApplication.ExitPlaymode();return;}
                    // 플레이어 담당은 대기시켜 민기 담당 AI의 실제 공격을 관찰한다.
                    foreach(var enemy in m.enemies)if(!enemy.huntsMingi)enemy.Hold=true;
                    PrisonChickenCheck.Capture(m.prison.player.View,"Logs/Prison-chicken-defense-handoff.png");step=3;return;
                }
                if(step==3){
                    if(d.MingiHits>lastHits){lastHits=d.MingiHits;Check(d.MingiDown,"AI punch "+lastHits+" starts Mingi ragdoll");if(lastHits==1)PrisonChickenCheck.Capture(m.prison.player.View,"Logs/Prison-chicken-defense-ragdoll.png");}
                    if(m.Failing){Check(d.MingiHits==3,"third Mingi hit fails mission");step=4;}return;
                }
                if(step==4&&!m.Failing){Check(m.carrying.activeInHierarchy&&m.State!=PrisonChickenMission.Stage.Cook,"retry preserves cooked chicken");Check(d.MingiHits==0&&!d.MingiDown&&m.prison.player.CanAct,"retry restores Mingi and player controls");step=5;return;}
                if(step==5&&d.Ready){foreach(var e in m.enemies)e.combat.ReceiveBullet(m.prison.player,Vector3.forward,1000);step=6;return;}
                if(step==6&&m.State==PrisonChickenMission.Stage.Deliver){Check(intro.mingi.Following,"victory resumes companion follow");Place(m,intro.informant.position+intro.informant.forward*2);m.StartCoroutine((System.Collections.IEnumerator)typeof(PrisonChickenMission).GetMethod("Deliver",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(m,null));step=7;return;}
                if(step==7&&m.State==PrisonChickenMission.Stage.Complete){Check(m.prison.player.CanAct&&!m.carrying.activeSelf,"delivery completes with controls restored");m.DeveloperComplete();Check(!d.Ready&&!d.EncounterActive,"skip cleanup is safe after completion");File.AppendAllText(Log,"PASS all checks. Victory damage and travel assisted; real AI performed all Mingi attacks.\n");running=false;EditorApplication.ExitPlaymode();}
            }catch(Exception e){File.AppendAllText(Log,e+"\n");running=false;EditorApplication.ExitPlaymode();}
        }
    }
}
#endif


