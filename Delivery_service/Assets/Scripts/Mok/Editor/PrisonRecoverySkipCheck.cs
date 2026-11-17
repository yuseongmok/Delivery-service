#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
namespace DeliveryService.Mok.Editor
{
    [InitializeOnLoad]
    public static class PrisonRecoverySkipCheck
    {
        const string Trigger="Temp/PrisonRecoverySkipCheck", Log="Logs/PrisonRecoverySkipCheck.txt";
        static int stage;static double until,deadline;static bool running;static PrisonMedicineMission medicine;static PrisonBrawler enemy;static float floor;
        static PrisonRecoverySkipCheck(){EditorApplication.update+=Tick;}
        static void Check(bool pass,string label){File.AppendAllText(Log,(pass?"PASS ":"FAIL ")+label+"\n");if(!pass)throw new Exception(label);}
        static void Tick(){
            if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            if(!running){if(!File.Exists(Trigger)||EditorApplication.isPlaying)return;File.Delete(Trigger);File.WriteAllText(Log,"Prison recovery / mission skip / next morning integration\n");SessionState.SetBool("PrisonRecoveryCheck",true);EditorApplication.EnterPlaymode();return;}
            if(!EditorApplication.isPlaying)return;
            try{
                if(EditorApplication.timeSinceStartup>deadline)throw new Exception("timeout stage "+stage);
                if(EditorApplication.timeSinceStartup<until)return;
                var cheats=DeveloperCheats.Current;
                if(stage==0){Check(cheats.SkipMission(),"skip introduction");stage++;until=EditorApplication.timeSinceStartup+1;}
                else if(stage==1){Check(cheats.SkipTarget=="갓 튀긴 치킨"&&cheats.SkipMission(),"skip chicken");stage++;until=EditorApplication.timeSinceStartup+1;}
                else if(stage==2){medicine=Object.FindFirstObjectByType<PrisonMedicineMission>();enemy=medicine.enemies[0];floor=enemy.combat.transform.position.y;enemy.Hold=true;enemy.combat.Npc.KnockDown(Vector3.up,7);stage++;until=EditorApplication.timeSinceStartup+.3;}
                else if(stage==3){enemy.combat.Npc.RagdollCenter.position+=Vector3.up*8;Check(enemy.RecoverOnSameFloor(),"recover even when ragdoll is above roof");Check(Mathf.Abs(enemy.combat.transform.position.y-floor)<.7f,"recovery stays on original floor");medicine.dialogue=Object.Instantiate(medicine.dialogue);medicine.dialogue.lineSeconds=.05f;medicine.dialogue.nextDaySeconds=.6f;Check(cheats.SkipMission(),"skip robbery starts home transition");stage++;until=EditorApplication.timeSinceStartup+.9;}
                else if(stage==4){Check(medicine.State==PrisonMedicineMission.Stage.Conversation&&medicine.Fade>.9f,"next morning fades to black before unlocking keys");Check(Object.FindFirstObjectByType<PrisonKeysMission>().State==PrisonKeysMission.Stage.Waiting,"keys do not start during overnight transition");stage++;until=EditorApplication.timeSinceStartup+2;}
                else if(stage==5){Check(medicine.State==PrisonMedicineMission.Stage.Complete&&medicine.prison.player.CanAct,"overnight returns control and completes robbery");Check(cheats.SkipTarget=="소매치기와 배식"&&cheats.SkipMission(),"skip keys and meals");stage++;until=EditorApplication.timeSinceStartup+3;}
                else if(stage==6){Check(cheats.SkipTarget=="간수장실 조사"&&cheats.SkipMission(),"skip office grants all clues");stage++;until=EditorApplication.timeSinceStartup+1;}
                else if(stage==7){Check(cheats.SkipTarget=="탈옥"&&cheats.SkipMission(),"skip escape");Check(cheats.SkipTarget=="스킵할 미션 없음","finished prison chain has no remaining skip");File.AppendAllText(Log,"DONE: automated state checks, not a manual visual playthrough.\n");running=false;EditorApplication.ExitPlaymode();}
            }catch(Exception e){File.AppendAllText(Log,e+"\n");running=false;EditorApplication.ExitPlaymode();}
        }
        [InitializeOnLoadMethod]static void Resume(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("PrisonRecoveryCheck",false)){SessionState.SetBool("PrisonRecoveryCheck",false);running=true;stage=0;until=EditorApplication.timeSinceStartup+3;deadline=until+65;}};}
    }
}
#endif
