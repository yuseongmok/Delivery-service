#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Object=UnityEngine.Object;
namespace DeliveryService.Mok.Editor
{
    [InitializeOnLoad] public static class PrisonCctvWireCheck
    {
        const string Log="Logs/Prison-wire-check.txt";static int step;static float wait;static double deadline;
        static PrisonCctvWireCheck(){EditorApplication.update+=Tick;}
        static void Check(bool value,string message){File.AppendAllText(Log,(value?"PASS ":"FAIL ")+message+"\n");if(!value)throw new Exception(message);}
        static void Place(PlayerCombat player,Transform point){var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.transform.SetPositionAndRotation(point.position+Vector3.up*1.1f,point.rotation);cc.enabled=true;player.View.transform.rotation=point.rotation;player.GetComponentInChildren<CameraMove>().SyncViewFromTransform();Physics.SyncTransforms();}
        static void Tick(){
            if(!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling&&File.Exists("Temp/PrisonCctvWireCheck")){var ready=Object.FindFirstObjectByType<PrisonCctvMission>();if(!ready||!ready.wirePanel)return;File.Delete("Temp/PrisonCctvWireCheck");File.WriteAllText(Log,"Assisted stage entry; real panel interaction/cut/retry coroutines.\n");SessionState.SetBool("PrisonWireCheck",true);EditorApplication.EnterPlaymode();return;}
            if(!EditorApplication.isPlaying||!SessionState.GetBool("PrisonWireCheck",false))return;
            try{var m=Object.FindFirstObjectByType<PrisonCctvMission>();if(!m||!m.wirePanel)return;var panel=m.wirePanel;var player=m.office.previous.previous.prison.player;
                if(step==0){step=1;wait=Time.time+3;deadline=EditorApplication.timeSinceStartup+100;return;}if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Timeout "+step);if(Time.time<wait)return;
                if(step==1){m.office.previous.previous.prison.introduction.DeveloperComplete();m.watch.postPosition=new Vector3(100,0,100);typeof(PrisonCctvMission).GetProperty("State").SetValue(m,PrisonCctvMission.Stage.Disable);Place(player,panel.checkpoint);Check(m.Interact(),"F interaction opens panel");step=2;wait=Time.time+1;return;}
                if(step==2){Check(panel.Ready&&panel.camera.enabled&&!player.View.enabled,"close-up camera and controls locked");for(int i=0;i<3;i++){var ray=panel.camera.ScreenPointToRay(panel.camera.WorldToScreenPoint(panel.wireTargets[i].transform.position));Check(Physics.Raycast(ray,out var hit,4)&&hit.collider==panel.wireTargets[i],"visible wire ray selection "+i);}PrisonChickenCheck.Capture(panel.camera,"Logs/Prison-wire-open.png");Time.timeScale=0;Check(!panel.Choose(0),"paused game cannot cut");Time.timeScale=1;panel.Cancel();Check(!panel.Playing&&player.View.enabled&&player.CanAct,"cancel restores first-person controls");Check(m.Interact(),"reopen after cancel");step=3;wait=Time.time+1;return;}
                if(step==3){Check(panel.Choose(0),"red wire selected");Check(!panel.Choose(1),"double-click cannot cut twice");step=31;wait=Time.time+.3f;return;}
                if(step==31){PrisonChickenCheck.Capture(panel.camera,"Logs/Prison-wire-cutting.png");step=4;wait=Time.time+.7f;return;}
                if(step==4){Check(panel.warning.enabled&&panel.sound.isPlaying,"wrong wire triggers visible and audible alarm");PrisonChickenCheck.Capture(panel.camera,"Logs/Prison-wire-alarm.png");step=5;wait=Time.time+3;return;}
                if(step==5){Check(m.State==PrisonCctvMission.Stage.Disable&&m.Attempts==1&&!m.CamerasDisabled&&m.monitors.All(x=>!x.Disabled),"red wire resets checkpoint, keeps cameras online");Check(!panel.Playing&&player.CanAct&&Vector3.Distance(player.transform.position,panel.checkpoint.position)<2,"retry returns to panel with control");Check(panel.wholeWires.All(x=>x.activeSelf)&&panel.cutWires.All(x=>!x.activeSelf)&&!panel.sound.isPlaying,"retry restores wires and stops alarm");Check(m.Interact(),"retry panel reopens");step=6;wait=Time.time+1;return;}
                if(step==6){Check(panel.Choose(1),"blue wire selected");step=7;wait=Time.time+5;return;}
                if(step==7){Check(m.Attempts==2&&m.State==PrisonCctvMission.Stage.Disable&&!m.CamerasDisabled,"second wrong wire also retries");Check(m.Interact(),"second retry reopens");step=8;wait=Time.time+1;return;}
                if(step==8){Check(panel.Choose(panel.correctWire),"correct CCTV wire selected");step=9;wait=Time.time+2;return;}
                if(step==9){Check(m.CamerasDisabled&&m.Channels==3&&m.monitors.All(x=>x.Disabled)&&m.State==PrisonCctvMission.Stage.Leave,"correct wire disables all CCTV feeds and enables exit");Check(player.CanAct&&player.View.enabled&&!panel.camera.enabled,"success restores camera and input");Check(Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(x=>x.enabled&&x.gameObject.activeInHierarchy)==1,"single active audio listener");Place(player,m.safePoint);step=10;wait=Time.time+.1f;return;}
                Check(m.State==PrisonCctvMission.Stage.Complete,"rendezvous continues existing evening route");File.AppendAllText(Log,"FINISHED: cancellation, pause, both wrong wires, alarm, retries, correct wire, camera/input/audio and exit verified. Travel was seeded.\n");Finish();
            }catch(Exception e){File.AppendAllText(Log,e.ToString());Finish();}
        }
        static void Finish(){Time.timeScale=1;SessionState.SetBool("PrisonWireCheck",false);step=0;EditorApplication.ExitPlaymode();}
    }
}
#endif
