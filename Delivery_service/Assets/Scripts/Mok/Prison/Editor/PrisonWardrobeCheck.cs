#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Object=UnityEngine.Object;
namespace DeliveryService.Mok.Editor
{
    [InitializeOnLoad]
    public static class PrisonWardrobeCheck
    {
        static bool running;static int step;static float wait;static double deadline;static NPCControl subject;
        const string Log="Logs/Prison-wardrobe-check.txt";
        static PrisonWardrobeCheck(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("WardrobeCheck",false)){running=true;step=0;deadline=EditorApplication.timeSinceStartup+90;SessionState.SetBool("WardrobeCheck",false);}};}
        public static void Run(){EditorSceneManager.OpenScene("Assets/Scenes/PrisonChapter.unity");File.WriteAllText(Log,"Prison visual and horizon play test\n");SessionState.SetBool("WardrobeCheck",true);EditorApplication.EnterPlaymode();}
        static void Check(bool okay,string text){File.AppendAllText(Log,(okay?"PASS ":"FAIL ")+text+"\n");if(!okay)throw new Exception(text);}
        static void Tick(){if(!running||!EditorApplication.isPlaying)return;try{
            var intro=Object.FindFirstObjectByType<PrisonIntroduction>();if(!intro)return;if(EditorApplication.timeSinceStartup>deadline)throw new Exception("timeout");
            if(step==0){intro.dialogue=Object.Instantiate(intro.dialogue);intro.dialogue.lineSeconds=.1f;step=1;}
            if(step==1&&intro.State==PrisonIntroduction.Stage.Yard&&intro.Fade==0){
                var player=intro.mission.player.transform;Check(Vector3.Dot(player.up,Vector3.up)>.99999f,"player root upright after cinematic handoff");
                var start=player.rotation;for(int i=0;i<8;i++){player.Rotate(Vector3.up*45);Check(Mathf.Abs(intro.mission.player.View.transform.right.y)<.015f,"level horizon at turn "+(i*45));}player.rotation=start;
                var population=Object.FindFirstObjectByType<PrisonPopulation>();Check(population.slots.Count(s=>!s.guard)==50,"50 civilian population slots retained");Check(population.slots.Count(s=>s.guard)==20,"20 guards retained");
                foreach(var slot in population.slots.Where(s=>!s.guard)){var r=slot.actor.GetComponentInChildren<SkinnedMeshRenderer>();Check(r.sharedMaterials.Any(m=>m.name.Contains("주황색"))&&r.bounds.size.y<3&&r.bounds.size.y>.8f,"uniform and bounds "+slot.actor.name);}
                subject=population.slots.First(s=>!s.guard).actor;subject.KnockDown(Vector3.forward,2);step=2;wait=Time.time+2;
                PrisonChickenCheck.Capture(intro.mission.player.View,"Logs/Prison-level-gameplay.png");
            }
            if(step==2&&Time.time>wait){Check(subject.IsKnockedDown,"city model ragdoll activates");Check(subject.RecoverFromKnockDown(),"city model recovers on navigation");step=3;wait=Time.time+3;}
            if(step==3&&Time.time>wait){Check(subject.GetComponentInChildren<Animator>().enabled&&subject.GetComponent<NavMeshAgent>().isOnNavMesh,"animation and navigation restored");running=false;EditorApplication.ExitPlaymode();}
        }catch(Exception e){File.AppendAllText(Log,e.ToString());running=false;EditorApplication.ExitPlaymode();}}
    }
}
#endif
