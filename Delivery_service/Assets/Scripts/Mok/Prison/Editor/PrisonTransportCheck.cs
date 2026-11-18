#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Object=UnityEngine.Object;
namespace DeliveryService.Mok.Editor
{
    [InitializeOnLoad] public static class PrisonTransportCheck
    {
        const string Log="Logs/Prison-transport-check.txt";
        static bool running,visual;static int step;static float wait,nextTap,savedGrip;static double deadline,pauseEnd;static Vector3 pausedVehicle;
        static PrisonTransportCheck(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("TransportCheck",false)){SessionState.SetBool("TransportCheck",false);running=true;visual=SessionState.GetBool("TransportVisual",false);step=0;deadline=EditorApplication.timeSinceStartup+240;}};}
        static void Check(bool b,string text){File.AppendAllText(Log,(b?"PASS ":"FAIL ")+text+"\n");if(!b)throw new Exception(text);}
        static void Tick()
        {
            if(!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling&&File.Exists("Temp/PrisonTransportCheck")){SessionState.SetBool("TransportVisual",File.ReadAllText("Temp/PrisonTransportCheck").Trim()=="visual");File.Delete("Temp/PrisonTransportCheck");File.WriteAllText(Log,"Assisted previous missions/travel; real board/ride/fall/retry/success coroutines. Tap API used, not physical keyboard.\n");SessionState.SetBool("TransportCheck",true);EditorApplication.EnterPlaymode();return;}
            if(!running||!EditorApplication.isPlaying)return;
            try
            {
                if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Timeout step "+step);
                var m=Object.FindFirstObjectByType<PrisonTransportEscape>();if(!m)return;
                if(step==0){step=1;wait=Time.time+2;return;}
                if(step==31)
                {
                    if(EditorApplication.timeSinceStartup<pauseEnd)return;
                    Check(m.Grip==savedGrip&&m.vehicle.position==pausedVehicle&&!m.Tap(),"pause stops travel, grip and tapping");Time.timeScale=1;step=4;return;
                }
                if(Time.time<wait)return;
                if(step==1)
                {
                    var o=m.previous.previous.office;o.previous.previous.prison.introduction.DeveloperComplete();Object.FindFirstObjectByType<PrisonChickenMission>().DeveloperComplete();typeof(PrisonKeysMission).GetProperty("State").SetValue(o.previous,PrisonKeysMission.Stage.Complete);o.evidence.DeveloperComplete();o.Collect(PrisonOfficePoint.Kind.Uniforms);m.previous.previous.DeveloperComplete();m.previous.DeveloperComplete();
                    var cc=m.Player.GetComponent<CharacterController>();cc.enabled=false;m.Player.transform.position=m.previous.vehiclePoint.position+Vector3.up*1.1f;cc.enabled=true;m.Friend.agent.Warp(m.previous.vehiclePoint.position+Vector3.back*1.5f);
                    m.dialogue=Object.Instantiate(m.dialogue);m.dialogue.lineSeconds=.15f;step=2;wait=Time.time+.4f;return;
                }
                if(step==2){Check(m.State==PrisonTransportEscape.Stage.Ready&&m.Interact(),"night arrival -> interact -> crawl under van");step=3;wait=Time.time+2.5f;return;}
                if(step==3)
                {
                    if(m.State!=PrisonTransportEscape.Stage.Riding)return;
                    PrisonChickenCheck.Capture(m.film,"Logs/Prison-transport-underbody.png");
                    Check(!m.Player.CanAct&&m.film.enabled&&!m.Player.View.enabled,"riding camera and normal controls locked");
                    Check(m.playerActor.Hanging&&m.friendActor.Hanging,"both actors attached and gripping");
                    var hand=m.playerActor.animator.GetBoneTransform(HumanBodyBones.RightHand);File.AppendAllText(Log,"Hand distance "+Vector3.Distance(hand.position,m.playerActor.rightGrip.position)+"\n");
                    Check(m.hands&&m.hands.gameObject.activeSelf,"first-person arms shown");
                    Check(Vector3.Distance(m.hands.rig.rightHand.position,m.hands.rightGrip.position)<.12f,"visible right hand contacts bar");
                    if(visual){File.AppendAllText(Log,"VISUAL CHECK FINISHED\n");running=false;EditorApplication.ExitPlaymode();return;}
                    savedGrip=m.Grip;pausedVehicle=m.vehicle.position;Time.timeScale=0;pauseEnd=EditorApplication.timeSinceStartup+.5;step=31;return;
                }
                if(step==4)
                {
                    if(m.Attempts==0||m.State!=PrisonTransportEscape.Stage.Riding)return;
                    Check(m.Attempts==1&&m.Progress<.15f&&m.Grip>.85f,"no taps -> fall -> departure checkpoint reset");
                    Check(m.previous.Complete&&m.previous.previous.CamerasDisabled,"retry preserves night/CCTV completion");step=5;nextTap=Time.time;return;
                }
                if(step==5)
                {
                    if(m.State==PrisonTransportEscape.Stage.Riding&&Time.time>=nextTap){Check(m.Tap(),"tap accepted");nextTap=Time.time+.25f;}
                    if(m.State!=PrisonTransportEscape.Stage.Complete)return;
                    Check(m.Attempts==1&&m.Progress==1,"repeated taps complete escape without extra failure");
                    Check(m.Player.CanAct&&m.Player.View.enabled&&!m.film.enabled,"success restores first-person controls");
                    Check(!m.playerActor.gameObject.activeSelf&&!m.friendActor.gameObject.activeSelf,"cinematic doubles hidden");
                    Check(m.Friend.Following&&m.Friend.agent.enabled&&m.Friend.agent.isOnNavMesh,"Mingi resumes on navigation mesh");
                    Check(Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(x=>x.isActiveAndEnabled)==1,"single audio listener restored");
                    PrisonChickenCheck.Capture(m.Player.View,"Logs/Prison-transport-escaped.png");
                    m.DeveloperComplete();Check(m.Complete&&m.Player.CanAct,"developer skip idempotent");File.AppendAllText(Log,"FINISHED\n");running=false;EditorApplication.ExitPlaymode();
                }
            }
            catch(Exception e){Time.timeScale=1;File.AppendAllText(Log,e.ToString());running=false;EditorApplication.ExitPlaymode();}
        }
    }
}
#endif
