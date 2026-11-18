#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using Object=UnityEngine.Object;
namespace DeliveryService.Mok.Editor
{
    [InitializeOnLoad] public static class PrisonNightCheck
    {
        static bool running; static int step; static float wait; static double deadline;
        const string Log="Logs/Prison-night-check.txt";
        static PrisonNightCheck(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("NightCheck",false)){SessionState.SetBool("NightCheck",false);running=true;step=0;deadline=EditorApplication.timeSinceStartup+200;}};}
        static void Check(bool b,string text){File.AppendAllText(Log,(b?"PASS ":"FAIL ")+text+"\n");if(!b)throw new Exception(text);}
        static void Place(PlayerCombat p,Vector3 point){var cc=p.GetComponent<CharacterController>();cc.enabled=false;p.transform.position=point+Vector3.up*1.1f;cc.enabled=true;Physics.SyncTransforms();}
        static void Tick()
        {
            if(!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling&&File.Exists("Temp/PrisonNightCheck"))
            {File.Delete("Temp/PrisonNightCheck");File.WriteAllText(Log,"Assisted CCTV completion and player placement; real evening, exposure/retry, companion navigation.\n");SessionState.SetBool("NightCheck",true);EditorApplication.EnterPlaymode();return;}
            if(!running||!EditorApplication.isPlaying)return;
            try
            {
                if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Timeout step "+step);
                var m=Object.FindFirstObjectByType<PrisonNightMission>();if(!m)return;
                if(step==0){step=1;wait=Time.time+2;return;}if(Time.time<wait)return;
                if(step==1){var o=m.previous.office;o.previous.previous.prison.introduction.DeveloperComplete();Object.FindFirstObjectByType<PrisonChickenMission>().DeveloperComplete();typeof(PrisonKeysMission).GetProperty("State").SetValue(o.previous,PrisonKeysMission.Stage.Complete);o.evidence.DeveloperComplete();o.Collect(PrisonOfficePoint.Kind.Uniforms);m.dialogue=Object.Instantiate(m.dialogue);m.dialogue.lineSeconds=.15f;m.previous.DeveloperComplete();step=2;wait=Time.time+8;return;}
                if(step==2)
                {
                    Check(m.State==PrisonNightMission.Stage.Sneak&&m.evening.IsEvening,"CCTV -> evening -> player control");
                    Check(m.Player.CanAct&&m.Player.View.enabled&&!m.film.enabled,"camera and controls restored");
                    var film=m.film;film.transform.SetPositionAndRotation(m.establishingShot.position,m.establishingShot.rotation);PrisonChickenCheck.Capture(film,"Logs/Prison-night-yard.png");
                    var path=new NavMeshPath();Vector3 from=m.checkpoint.position;
                    foreach(var point in m.route){Check(NavMesh.CalculatePath(from,point.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete,"walkable route to "+point.name);from=point.position;}
                    Check(NavMesh.CalculatePath(from,m.vehiclePoint.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete,"walkable route to van");
                    Place(m.Player,new Vector3(-18,0,32));
                    var light=m.searchlights[0];light.sweepFrom=light.sweepTo=new Vector3(-18,0,32);light.ResetSweep();
                    Check(light.Illuminates(new Vector3(-18,.15f,32),m.Player.transform),"spotlight cone reaches player");
                    var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);blocker.transform.position=Vector3.Lerp(light.transform.position,new Vector3(-18,.15f,32),.8f);blocker.transform.localScale=Vector3.one*3;Physics.SyncTransforms();
                    Check(!light.Illuminates(new Vector3(-18,.15f,32),m.Player.transform),"solid cover blocks spotlight detection");Object.DestroyImmediate(blocker);
                    step=3;wait=Time.time+4;return;
                }
                if(step==3)
                {
                    Check(m.Attempts==1&&m.State==PrisonNightMission.Stage.Sneak,"light exposure retries checkpoint");
                    Check(m.previous.CamerasDisabled&&m.previous.restraint.Bound,"CCTV and warden state preserved");
                    Check(m.Player.CanAct&&m.Player.View.enabled&&Vector3.Distance(m.Player.transform.position,m.checkpoint.position)<2,"retry camera control and player position");
                    Check(Vector3.Distance(m.Friend.transform.position,m.companionStart.position)<3,"Mingi restored to checkpoint");
                    foreach(var light in m.searchlights)light.SetRunning(false);
                    // Separate navigation check: disable hazards, move player ahead, let Mingi traverse the real mesh.
                    Place(m.Player,m.vehiclePoint.position);step=4;wait=Time.time+1;return;
                }
                if(step==4)
                {
                    if(!m.Complete)return;
                    Check(Vector3.Distance(m.Friend.transform.position,m.vehiclePoint.position)<4,"Mingi physically follows route to vehicle");
                    Check(m.Player.CanAct&&!m.Busy&&m.Target==null,"arrival completion and handoff");
                    Check(m.previous.legacyEscape.State==PrisonEscapeMission.Stage.Waiting,"old drive/password sequence remains suspended");
                    m.DeveloperComplete();Check(m.Complete&&m.Player.CanAct,"night skip safe and idempotent");
                    File.AppendAllText(Log,"FINISHED\n");running=false;EditorApplication.ExitPlaymode();
                }
            }
            catch(Exception e){File.AppendAllText(Log,e.ToString());running=false;Time.timeScale=1;EditorApplication.ExitPlaymode();}
        }
    }
}
#endif
