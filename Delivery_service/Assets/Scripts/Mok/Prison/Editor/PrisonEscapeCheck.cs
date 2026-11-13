#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using Object=UnityEngine.Object;
namespace DeliveryService.Mok.Editor
{
    [InitializeOnLoad] public static class PrisonEscapeCheck
    {
        static bool running;static int shots;static int step;static float wait;static double deadline;
        const string Log="Logs/Prison-escape-check.txt";
        static PrisonEscapeCheck(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("EscapeCheck",false)){SessionState.SetBool("EscapeCheck",false);running=true;step=0;deadline=EditorApplication.timeSinceStartup+160;}};}
        public static void Run(){shots=0;File.WriteAllText(Log,"");SessionState.SetBool("EscapeCheck",true);EditorApplication.EnterPlaymode();}
        static void Check(bool b,string s){File.AppendAllText(Log,(b?"PASS ":"FAIL ")+s+"\n");if(!b)throw new Exception(s);}
        static void Set(object o,string n,object v)=>o.GetType().GetProperty(n).SetValue(o,v);
        static void Place(PlayerCombat p,Vector3 at){var cc=p.GetComponent<CharacterController>();cc.enabled=false;p.transform.position=at+Vector3.up*1.1f;cc.enabled=true;Physics.SyncTransforms();}
        public static void ContinueChapter(){File.WriteAllText(Log,"Continuous chapter test; previous mission state retained\n");running=true;step=1;wait=Time.time+.5f;deadline=EditorApplication.timeSinceStartup+200;}
        static void Tick(){if(!running||!EditorApplication.isPlaying)return;try{
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("timeout "+step);
            var m=Object.FindFirstObjectByType<PrisonEscapeMission>();if(!m)return;
            if(step==0){step=1;wait=Time.time+2;return;}if(step==4&&m.performance.Seated&&shots==0){shots++;PrisonChickenCheck.Capture(m.filmCamera,"Logs/Prison-escape-seated.png");}
            if(step==6&&m.Subtitle==m.dialogue.relief[0]&&shots<2){shots=2;PrisonChickenCheck.Capture(m.filmCamera,"Logs/Prison-escape-relief.png");}
            if(Time.time<wait)return;
            var prison=m.previous.previous.previous.prison;var p=prison.player;
            if(step==1){if(PrisonChapterCheck.Active){shots=0;step=2;return;}var intro=prison.introduction;intro.StopAllCoroutines();typeof(PrisonIntroduction).GetMethod("SetCinematic",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(intro,new object[]{false});Set(intro,"State",PrisonIntroduction.Stage.Complete);Set(m.previous.previous,"State",PrisonKeysMission.Stage.Complete);m.previous.Collect(PrisonOfficePoint.Kind.Uniforms);m.previous.Collect(PrisonOfficePoint.Kind.VehicleKey);m.previous.Collect(PrisonOfficePoint.Kind.Password);step=2;wait=Time.time+.4f;return;}
            if(step==2){Check(m.State==PrisonEscapeMission.Stage.Change,"office completion unlocks disguise without day condition");Place(p,m.changePoint.position);Check(m.Interact(),"start disguise");step=3;wait=Time.time+2;return;}
            if(step==3){Check(m.Disguised&&m.State==PrisonEscapeMission.Stage.Vehicle&&p.enabled,"disguise restores gameplay");Check(prison.introduction.mingi.GetComponentsInChildren<Renderer>().Any(r=>r.sharedMaterials.Contains(m.uniform)),"Mingi wears guard uniform");Check(p.GetComponentsInChildren<Renderer>(true).Any(r=>r.sharedMaterials.Contains(m.uniform)),"player wears guard uniform");var path=new UnityEngine.AI.NavMeshPath();Check(UnityEngine.AI.NavMesh.CalculatePath(new Vector3(-18,3.75f,-30),m.boardingPoint.position,UnityEngine.AI.NavMesh.AllAreas,path)&&path.status==UnityEngine.AI.NavMeshPathStatus.PathComplete,"office connects to transport van");Place(p,m.boardingPoint.position);prison.introduction.mingi.agent.Warp(m.boardingPoint.position+Vector3.right*2);Check(m.Interact(),"board with Mingi");step=4;wait=Time.time+25;return;}
            if(step==4){Check(m.State==PrisonEscapeMission.Stage.Checkpoint,"vehicle arrives at gate");Check(m.performance.Seated&&prison.introduction.mingi.gameObject.activeInHierarchy,"Mingi remains visible in passenger seat");Check(m.performance.InspectorReady,"inspector walks to driver window before question");PrisonChickenCheck.Capture(m.filmCamera,"Logs/Prison-escape-checkpoint.png");Check(m.Choices[1]==m.previous.dialogue.password,"password uses editable board source");Check(m.Choose(0),"wrong password accepted as attempt");step=5;wait=Time.time+m.dialogue.lineSeconds+2;return;}
            if(step==5){Check(m.State==PrisonEscapeMission.Stage.Checkpoint&&m.Attempts==1&&m.Disguised,"wrong password resets gate checkpoint retaining disguise");Check(!m.previous.GetComponent<PrisonOfficePickup>().Busy,"pickup lock released");Check(m.Choose(1),"correct password accepted");step=6;wait=Time.time+16+m.dialogue.lineSeconds*(2+m.dialogue.relief.Length);return;}
            if(step==6){Check(m.State==PrisonEscapeMission.Stage.Complete,"escape completes");Check(Vector3.Distance(m.vehicle.position,m.exitPoint.position)<.1f,"van exits prison");File.AppendAllText(Log,"NOTE travel seeded; NavMesh connectivity and full escape state flow checked, not manual traversal\n");running=false;if(PrisonChapterCheck.Active)PrisonChapterCheck.Finish(!System.IO.File.ReadAllText(Log).Contains("Exception"));else EditorApplication.ExitPlaymode();}
        }catch(Exception e){File.AppendAllText(Log,e.ToString());running=false;if(PrisonChapterCheck.Active)PrisonChapterCheck.Finish(!System.IO.File.ReadAllText(Log).Contains("Exception"));else EditorApplication.ExitPlaymode();}}
    }
}
#endif
