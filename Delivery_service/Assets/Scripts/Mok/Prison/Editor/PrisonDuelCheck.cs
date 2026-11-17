#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object=UnityEngine.Object;
namespace DeliveryService.Mok.Editor
{
    [InitializeOnLoad] static class PrisonDuelCheck
    {
        const string Log="Logs/Prison-duel-check.txt";
        static bool running,sawSlow,sawNormal,captured,paused,skipRun,hudCaptured,nearCaptured,talkCaptured,shoutCaptured;
        static int step,pressedRound,health;
        static double deadline,pauseUntil;
        static float readyAt,pausedAge,pausedSlow,savedScale;
        static Key? held;
        static UnityEngine.AI.NavMeshPath route;static int corner;static float frameTime;
        static PrisonDuelCheck(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("PrisonDuelCheck",false)){SessionState.SetBool("PrisonDuelCheck",false);running=true;step=0;pressedRound=0;sawSlow=sawNormal=captured=paused=skipRun=hudCaptured=nearCaptured=talkCaptured=shoutCaptured=false;held=null;deadline=EditorApplication.timeSinceStartup+300;}};}
        static void Check(bool ok,string text){File.AppendAllText(Log,(ok?"PASS ":"FAIL ")+text+"\n");if(!ok)throw new Exception(text);}
        static void Keys(Key? key){if(Keyboard.current!=null)InputSystem.QueueStateEvent(Keyboard.current,key.HasValue?new KeyboardState(key.Value):new KeyboardState());}
        static void Click(bool down){if(Mouse.current!=null)InputSystem.QueueStateEvent(Mouse.current,down?new MouseState().WithButton(MouseButton.Left):new MouseState());}
        static void Place(PrisonMedicineMission m,Vector3 pos){var cc=m.prison.player.GetComponent<CharacterController>();cc.enabled=false;m.prison.player.transform.position=pos;cc.enabled=true;}
        static void Demand(PrisonMedicineMission m){m.StartCoroutine((System.Collections.IEnumerator)typeof(PrisonMedicineMission).GetMethod("Demand",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(m,null));}
        static void Tick()
        {
            if(!EditorApplication.isCompiling&&!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists("Temp/PrisonDuelCheck")){
                File.Delete("Temp/PrisonDuelCheck");if(EditorSceneManager.GetActiveScene().name!="PrisonChapter")return;
                File.WriteAllText(Log,"Play-mode duel check. Travel and conversation trigger assisted; real Input System Q/R events drive the fight; no attack clicks.\n");SessionState.SetBool("PrisonDuelCheck",true);EditorApplication.EnterPlaymode();return;
            }
            if(!running||!EditorApplication.isPlaying)return;
            try{
                var m=Object.FindFirstObjectByType<PrisonMedicineMission>();if(!m||!m.duel)return;var d=m.duel;
                if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Timeout step="+step+" phase="+d.Current+" mission="+m.State);
                if(step==0){readyAt=Time.time+2;step=1;return;}
                if(step==1){if(Time.time<readyAt)return;m.prison.introduction.DeveloperComplete();m.chicken.DeveloperComplete();m.BeginSearch();
                    d.settings=Object.Instantiate(d.settings);d.settings.lineSeconds=1.2f;m.dialogue=Object.Instantiate(m.dialogue);m.dialogue.lineSeconds=1.2f;m.dialogue.nextDaySeconds=.5f;
                    Check(d.boss.name.Contains("김정민")&&d.presentation.bossAnimator.isHuman,"named humanoid boss saved");Check(d.crowd.childCount==16,"16 saved circle spectators");Check(d.crowd.GetComponentsInChildren<NPCControl>(true).Length==0,"spectators cannot wander into fight");
                    Place(m,d.boss.position+d.boss.forward*2);m.prison.player.View.transform.position=m.prison.player.transform.position+Vector3.up*1.6f;m.prison.player.View.transform.LookAt(d.boss.position+Vector3.up*1.25f);Physics.SyncTransforms();Check(m.Interact(),"looking at Kim opens conversation through normal interaction");step=2;return;
                }
                if(m.State==PrisonMedicineMission.Stage.Conversation&&d.introduction){if(!talkCaptured&&m.Subtitle.StartsWith("나:")){PrisonChickenCheck.Capture(m.camera,"Logs/Prison-duel-conversation.png");Check(d.introduction.playerActor.gameObject.activeInHierarchy,"player visible in three-person workshop conversation");talkCaptured=true;}if(!shoutCaptured&&m.Subtitle.StartsWith("죄수:")){PrisonChickenCheck.Capture(m.camera,"Logs/Prison-duel-shout.png");Check(d.introduction.herald.gameObject.activeInHierarchy,"herald visible for fight announcement");shoutCaptured=true;}}
                Click(false);
                if(step==2){
                    if(d.Active&&!captured){Check(d.introduction.playerActor.gameObject.activeInHierarchy,"player visible in arena establishing shot");PrisonChickenCheck.Capture(m.camera,"Logs/Prison-duel-arena.png");captured=true;}
                    if(d.Current==PrisonMedicineDuel.Phase.Tell){
                        if(d.Round==1&&pressedRound==0){held=d.DangerSide<0?Key.R:Key.Q;pressedRound=1;Check(d.Age<d.TellDuration-d.settings.dodgeWindow,"early-input case");Check(d.presentation.duelAudio&&d.presentation.duelAudio.PlayingMusic,"new battle music plays during duel");}
                        if(d.Round>=3)held=null;
                        if(d.Round==3&&pressedRound<3&&d.TellDuration-d.Age<.09f){held=d.DangerSide<0?Key.Q:Key.R;pressedRound=3;}
                    }
                    Keys(held);
                    if(d.Current==PrisonMedicineDuel.Phase.Lost){Check(d.PlayerHealth==0&&Mathf.Approximately(Time.timeScale,1),"early / held / wrong / absent dodge inputs fail, normal speed restored");Keys(Key.Enter);pressedRound=0;step=3;}
                    return;
                }
                if(step==3){Keys(null);if(d.Current==PrisonMedicineDuel.Phase.Ready&&!d.Retrying){Check(d.PlayerHealth==d.settings.playerHealth&&d.BossHealth==d.settings.bossHealth,"Enter retries directly in arena with full health");step=4;}return;}
                if(step==4||step==8){
                    if(paused){if(EditorApplication.timeSinceStartup<pauseUntil){CheckOncePause(d);return;}Time.timeScale=savedScale;paused=false;}
                    Keys(null);
                    if(d.Current==PrisonMedicineDuel.Phase.Tell&&pressedRound!=d.Round){float remaining=d.TellDuration-d.Age;float target=!sawNormal&&step==4?.23f:.18f;if(remaining<target&&remaining>0){Keys(d.DangerSide<0?Key.R:Key.Q);pressedRound=d.Round;}}
                    if(d.SlowRemaining>0&&!sawSlow){sawSlow=true;Check(Time.timeScale<=.05f&&d.Age>=.11f,"near-face dodge triggers extreme slow motion");PrisonChickenCheck.Capture(m.camera,"Logs/Prison-duel-perfect.png");ScreenCapture.CaptureScreenshot("Logs/Prison-duel-hud.png");savedScale=Time.timeScale;pausedAge=d.Age;pausedSlow=d.SlowRemaining;Time.timeScale=0;pauseUntil=EditorApplication.timeSinceStartup+.2;paused=true;return;}
                    if(!nearCaptured&&d.SlowRemaining>.3f&&d.SlowRemaining<.5f){PrisonChickenCheck.Capture(m.camera,"Logs/Prison-duel-near-face.png");ScreenCapture.CaptureScreenshot("Logs/Prison-duel-new-hud.png");nearCaptured=true;}
                    if(step==8&&d.SlowRemaining>0){savedScale=Time.timeScale;Time.timeScale=0;m.DeveloperComplete();Check(Time.timeScale==0&&!d.Active,"mission skip preserves pause");Time.timeScale=savedScale;step=9;return;}
                    if(d.Current==PrisonMedicineDuel.Phase.Counter){if(sawSlow&&!hudCaptured&&Time.timeScale>.9f){ScreenCapture.CaptureScreenshot("Logs/Prison-duel-hud.png");PrisonChickenCheck.Capture(m.camera,"Logs/Prison-duel-counter.png");hudCaptured=true;}if(!sawNormal){Check(!d.Perfect,"ordinary correctly timed dodge opens counter");sawNormal=true;}Click(false);}
                    if(m.State==PrisonMedicineMission.Stage.Collect){Check(sawSlow&&sawNormal&&d.BossHealth==0&&!d.boss.gameObject.activeSelf&&d.ragdoll.Down,"Q/R alone triggers automatic counters and defeats boss");Check(d.presentation.duelAudio.Hits>0&&d.presentation.duelAudio.Swings>d.presentation.duelAudio.Hits,"separate attack and impact events, dodges do not play hit sound");Check(m.prison.player.CanAct&&m.prison.player.View.enabled&&!m.camera.enabled&&Mathf.Approximately(Time.timeScale,1),"victory restores first person, controls and time");Check(m.Interact()&&m.HasMedicine,"collect medicine after boss fight without teleport");route=new UnityEngine.AI.NavMeshPath();Check(UnityEngine.AI.NavMesh.CalculatePath(m.prison.player.transform.position,m.cell.position,UnityEngine.AI.NavMesh.AllAreas,route)&&route.status==UnityEngine.AI.NavMeshPathStatus.PathComplete,"walkable route from arena to cell");corner=0;frameTime=Time.time;step=6;}return;
                }
                if(step==6){float dt=Mathf.Min(.05f,Time.time-frameTime);frameTime=Time.time;var p=m.prison.player;Vector3 goal=route.corners[Mathf.Min(corner,route.corners.Length-1)];Vector3 delta=Vector3.ProjectOnPlane(goal-p.transform.position,Vector3.up);if(delta.magnitude<.35f&&corner<route.corners.Length-1){corner++;return;}if(Vector3.Distance(p.transform.position,m.cell.position)<2.3f){Check(m.Interact(),"walked courtyard and corridors, cell dialogue starts");step=5;return;}p.GetComponent<CharacterController>().Move(delta.normalized*3*dt+Vector3.down*dt);return;}
                if(step==5&&m.State==PrisonMedicineMission.Stage.Complete){Check(!d.presentation.duelAudio.PlayingMusic,"battle music stops after victory");Check(m.prison.player.CanAct,"cell dialogue and next-day transition restore controls");m.BeginSearch();Place(m,d.boss.position+d.boss.forward*2);Demand(m);pressedRound=0;step=8;return;}
                if(step==9&&m.State==PrisonMedicineMission.Stage.Complete){Keys(null);Click(false);Check(Mathf.Approximately(Time.timeScale,1)&&Mathf.Approximately(Time.fixedDeltaTime,.02f),"paused slow-motion skip restores speed and physics after resume");File.AppendAllText(Log,"PASS complete boss chain, retry, pause and skip checks.\n");running=false;EditorApplication.ExitPlaymode();}
            }catch(Exception e){Keys(null);Click(false);Time.timeScale=1;Time.fixedDeltaTime=.02f;File.AppendAllText(Log,e+"\n");running=false;EditorApplication.ExitPlaymode();}
        }
        static void CheckOncePause(PrisonMedicineDuel d){if(Mathf.Abs(d.Age-pausedAge)>.001f||Mathf.Abs(d.SlowRemaining-pausedSlow)>.001f)throw new Exception("Pause advanced duel timing");}
    }
}
#endif











