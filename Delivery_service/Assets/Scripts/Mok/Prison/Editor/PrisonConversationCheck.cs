#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
namespace DeliveryService.Mok.Editor
{
    [InitializeOnLoad]
    public static class PrisonConversationCheck
    {
        static bool running;static int stage;static float wait;static double deadline;
        const string Log="Logs/Prison-conversation-check.txt";
        static PrisonConversationCheck(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("ConversationCheck",false)){SessionState.SetBool("ConversationCheck",false);running=true;stage=0;deadline=EditorApplication.timeSinceStartup+60;}};}
        public static void Run(){EditorSceneManager.OpenScene("Assets/Scenes/PrisonChapter.unity");File.WriteAllText(Log,"Conversation / population / chef checks\n");SessionState.SetBool("ConversationCheck",true);EditorApplication.EnterPlaymode();}
        static void Check(bool value,string text){File.AppendAllText(Log,(value?"PASS ":"FAIL ")+text+"\n");if(!value)throw new Exception(text);}
        static void Tick(){if(!running||!EditorApplication.isPlaying)return;try{
            var intro=Object.FindFirstObjectByType<PrisonIntroduction>();if(!intro)return;
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("timeout "+stage);
            if(stage==0){stage=1;wait=Time.time+2;return;}if(Time.time<wait)return;
            if(stage==1){intro.StopAllCoroutines();typeof(PrisonIntroduction).GetMethod("SetCinematic",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(intro,new object[]{false});intro.mugshotActor.gameObject.SetActive(false);
                var pop=Object.FindFirstObjectByType<PrisonPopulation>();Check(pop.slots.Count(s=>!s.guard)==30,"30 inmate slots");Check(pop.slots.Count(s=>s.guard)==20,"20 guards preserved");
                intro.meetingPresentation.Begin(intro.filmCamera);PrisonChickenCheck.Capture(intro.filmCamera,"Logs/Conversation-wide.png");stage=2;wait=Time.time+.2f;return;}
            if(stage>=2&&stage<=4){string line=stage==2?"최형욱: 검사":stage==3?"민기: 검사":"나: 검사";intro.meetingPresentation.ShowLine(line);PrisonChickenCheck.Capture(intro.filmCamera,"Logs/Conversation-speaker-"+stage+".png");Check(intro.meetingPresentation.playerActor.gameObject.activeSelf,"player actor visible for conversation "+stage);stage++;wait=Time.time+.2f;return;}
            if(stage==5){intro.meetingPresentation.End();Check(!intro.meetingPresentation.playerActor.gameObject.activeSelf,"conversation actor hidden after dialogue");
                var m=Object.FindFirstObjectByType<PrisonChickenMission>();var cc=intro.mission.player.GetComponent<CharacterController>();cc.enabled=false;intro.mission.player.transform.position=m.checkpoint.position;cc.enabled=true;
                m.StartCoroutine(m.chefReaction.Wake(m.camera));stage=6;wait=Time.time+.8f;return;}
            if(stage==6){var m=Object.FindFirstObjectByType<PrisonChickenMission>();Check(Vector3.Distance(m.camera.transform.position,m.chefReaction.playerView.position)<.05f,"chef rises at player eye position before zoom");PrisonChickenCheck.Capture(m.camera,"Logs/Chef-player-view.png");stage=7;wait=Time.time+.9f;return;}
            if(stage==7){var m=Object.FindFirstObjectByType<PrisonChickenMission>();Check(m.camera.fieldOfView==42,"face zoom finishes");PrisonChickenCheck.Capture(m.camera,"Logs/Chef-player-closeup.png");m.chefReaction.Sleep();
                var cc=intro.mission.player.GetComponent<CharacterController>();cc.enabled=false;intro.mission.player.transform.position=intro.informant.position+intro.informant.forward*2;cc.enabled=true;intro.mingi.agent.Warp(intro.mission.player.transform.position+intro.informant.right);
                intro.dialogue=Object.Instantiate(intro.dialogue);intro.dialogue.lineSeconds=.05f;typeof(PrisonIntroduction).GetProperty("State").SetValue(intro,PrisonIntroduction.Stage.Yard);
                Check(intro.BeginMeeting(),"meeting starts at relocated informant");stage=8;wait=Time.time+3.5f;return;}
            if(stage==8){Check(intro.State==PrisonIntroduction.Stage.Complete&&intro.mission.player.CanAct,"meeting completes and restores gameplay");Check(!intro.meetingPresentation.playerActor.gameObject.activeSelf&&intro.mingi.Following,"conversation actors and companion restored");running=false;EditorApplication.ExitPlaymode();}
        }catch(Exception e){File.AppendAllText(Log,e.ToString());running=false;EditorApplication.ExitPlaymode();}}
    }
}
#endif
