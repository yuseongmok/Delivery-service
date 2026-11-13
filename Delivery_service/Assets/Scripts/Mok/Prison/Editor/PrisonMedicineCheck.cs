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
namespace DeliveryService.Mok.Editor
{
    [InitializeOnLoad]
    public static class PrisonMedicineCheck
    {
        static bool running;static int stage;static float wait;static double deadline;
        const string Log="Logs/Prison-medicine-check.txt";
        static PrisonMedicineCheck(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("MedicineCheck",false)){SessionState.SetBool("MedicineCheck",false);running=true;stage=0;deadline=EditorApplication.timeSinceStartup+90;}};}
        public static void Run(){if(!PrisonChapterCheck.Active)EditorSceneManager.OpenScene("Assets/Scenes/PrisonChapter.unity");File.WriteAllText(Log,"Medicine mission / range / ragdoll checks\n");SessionState.SetBool("MedicineCheck",true);EditorApplication.EnterPlaymode();}
        static void Check(bool value,string text){File.AppendAllText(Log,(value?"PASS ":"FAIL ")+text+"\n");if(!value)throw new Exception(text);}
        static void Set(object target,string name,object value)=>target.GetType().GetProperty(name).SetValue(target,value);
        static void Place(PrisonMedicineMission m,Vector3 p){var cc=m.prison.player.GetComponent<CharacterController>();if(p.y<.5f)p.y+=1.1f;cc.enabled=false;m.prison.player.transform.position=p;cc.enabled=true;Physics.SyncTransforms();}
        static void Face(PrisonMedicineMission m,Transform t){m.prison.player.View.transform.LookAt(t.position+Vector3.up*1.3f);Physics.SyncTransforms();}
        public static void ContinueChapter(){File.WriteAllText(Log,"Continuous chapter test; previous mission state retained\n");running=true;stage=1;wait=Time.time+.5f;deadline=EditorApplication.timeSinceStartup+200;}
        static void Tick(){if(!running||!EditorApplication.isPlaying)return;try{
            var m=Object.FindFirstObjectByType<PrisonMedicineMission>();if(!m)return;if(EditorApplication.timeSinceStartup>deadline)throw new Exception("timeout stage "+stage);
            if(stage==0){stage=1;wait=Time.time+2;return;}if(Time.time<wait)return;
            if(stage==1){if(PrisonChapterCheck.Active){m.dialogue=Object.Instantiate(m.dialogue);m.dialogue.lineSeconds=.05f;stage=2;return;}var intro=m.prison.introduction;intro.StopAllCoroutines();typeof(PrisonIntroduction).GetMethod("SetCinematic",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(intro,new object[]{false});Set(intro,"State",PrisonIntroduction.Stage.Complete);Set(intro,"Fade",0f);intro.mugshotActor.gameObject.SetActive(false);Set(m.chicken,"State",PrisonChickenMission.Stage.Complete);m.dialogue=Object.Instantiate(m.dialogue);m.dialogue.lineSeconds=.05f;stage=2;wait=Time.time+.5f;return;}
            if(stage==2){Check(m.State==PrisonMedicineMission.Stage.Search,"chicken completion unlocks medicine mission without day gate");
                var source=m.prison.introduction.mugshotActor.GetComponentsInChildren<SkinnedMeshRenderer>(true);foreach(var r in m.prison.player.GetComponentsInChildren<SkinnedMeshRenderer>(true))Check(source.Any(s=>s.name==r.name&&s.sharedMesh==r.sharedMesh),"ragdoll uses prison uniform "+r.name);
                foreach(var target in m.enemies.Select(e=>e.transform).Concat(new[]{m.cell})){NavMesh.SamplePosition(m.checkpoint.position,out var a,3,NavMesh.AllAreas);NavMesh.SamplePosition(target.position,out var b,3,NavMesh.AllAreas);var path=new NavMeshPath();Check(NavMesh.CalculatePath(a.position,b.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete,"route to "+target.name);}
                var actor=m.enemies[0].combat.transform;Place(m,actor.position+actor.forward*2.2f);Check(!m.enemies[0].CanReach(),"punch cannot reach 2.2 metres");Place(m,actor.position+actor.forward*1.1f);Check(m.enemies[0].CanReach(),"punch reaches close target");
                var decoy=m.decoys[0];Place(m,decoy.position+decoy.forward*1.8f);stage=21;wait=Time.time+.2f;return;}
            if(stage==21){var decoy=m.decoys[0];Face(m,decoy);Physics.Raycast(m.prison.player.View.transform.position,m.prison.player.View.transform.forward,out var ray,3.2f,~0,QueryTriggerInteraction.Ignore);File.AppendAllText(Log,"DECOY hit="+ray.transform?.name+" canAct="+m.prison.player.CanAct+" state="+m.State+"\n");Check(m.Interact()&&m.State==PrisonMedicineMission.Stage.Search,"wrong inmate gives clue without combat");var actor=m.enemies[0].combat.transform;Place(m,actor.position+actor.forward*2);stage=22;wait=Time.time+.2f;return;}
            if(stage==22){var actor=m.enemies[0].combat.transform;
                Face(m,actor);PrisonChickenCheck.Capture(m.prison.player.View,"Logs/Medicine-target.png");Check(m.Interact(),"correct inmate starts confrontation");stage=3;wait=Time.time+1;return;}
            if(stage==3){Check(m.State==PrisonMedicineMission.Stage.Fight,"dialogue starts three-enemy fight");foreach(var e in m.enemies)e.Hold=true;Place(m,m.checkpoint.position);m.Hit(Vector3.back);Check(m.prison.player.GetComponentInChildren<PlayerRagdoll>().IsActive,"hit launches player ragdoll");stage=4;wait=Time.time+.4f;return;}
            if(stage==4){var camera=Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).First(c=>c.name=="Player ragdoll camera");PrisonChickenCheck.Capture(camera,"Logs/Medicine-prison-ragdoll.png");stage=5;wait=Time.time+3.5f;return;}
            if(stage==5){m.Hit(Vector3.back);stage=6;wait=Time.time+3.6f;return;}
            if(stage==6){m.Hit(Vector3.back);stage=7;wait=Time.time+4;return;}
            if(stage==7){if(m.Failing)return;Check(m.State==PrisonMedicineMission.Stage.Search&&m.Hits==0&&m.prison.player.CanAct,"three hits retry at workshop and restore input");Check(m.chicken.State==PrisonChickenMission.Stage.Complete,"retry preserves chicken completion");var actor=m.enemies[0].combat.transform;Place(m,actor.position+actor.forward*2);Face(m,actor);Check(m.Interact(),"confrontation can restart");stage=8;wait=Time.time+1;return;}
            if(stage==8){foreach(var e in m.enemies)e.Hold=true;if(!m.prison.player.CanAct)return;foreach(var e in m.enemies){e.combat.ReceiveBullet(m.prison.player,Vector3.forward,1000);}stage=9;wait=Time.time+.2f;return;}
            if(stage==9){if(!m.prison.player.CanAct)return;Check(m.State==PrisonMedicineMission.Stage.Collect&&m.medicine.gameObject.activeSelf,"defeating group reveals medicine");Place(m,m.medicine.position+Vector3.back);Check(m.Interact()&&m.HasMedicine&&m.State==PrisonMedicineMission.Stage.Return,"medicine pickup changes objective to cell");Place(m,m.cell.position);Check(m.Interact(),"cell interaction starts Mingi conversation");stage=10;wait=Time.time+1;return;}
            if(stage==10){Check(m.State==PrisonMedicineMission.Stage.Complete&&m.prison.player.CanAct,"mission completes and restores gameplay");Check(!m.medicine.gameObject.activeSelf&&m.HasMedicine,"medicine retained without duplicate pickup");File.AppendAllText(Log,"NOTE navigation checked; travel seeded; combat damage injected for progression and retry validation\n");running=false;if(PrisonChapterCheck.Active)PrisonChapterCheck.Finish(!System.IO.File.ReadAllText(Log).Contains("Exception"));else EditorApplication.ExitPlaymode();}
        }catch(Exception e){File.AppendAllText(Log,e.ToString());running=false;if(PrisonChapterCheck.Active)PrisonChapterCheck.Finish(!System.IO.File.ReadAllText(Log).Contains("Exception"));else EditorApplication.ExitPlaymode();}}
    }
}
#endif


