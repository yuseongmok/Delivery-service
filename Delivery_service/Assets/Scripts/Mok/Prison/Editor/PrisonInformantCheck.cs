#if UNITY_EDITOR
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace DeliveryService.Mok.Editor
{
    [InitializeOnLoad] public static class PrisonInformantCheck
    {
        static bool running, bowed, replied, firstLine; static int step; static double until, deadline, replyAt;
        const string Log = "Logs/Prison-informant-check.txt";
        static PrisonInformantCheck()
        {
            EditorApplication.update += Run;
            EditorApplication.playModeStateChanged += s => { if (s == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("PrisonInformantCheck", false)) { SessionState.SetBool("PrisonInformantCheck", false); running = true; step = 0; bowed = replied = firstLine = false; replyAt=0; until = EditorApplication.timeSinceStartup + 2; deadline = EditorApplication.timeSinceStartup + 120; } };
        }
        static void Check(bool good, string message) { File.AppendAllText(Log, (good ? "PASS " : "FAIL ") + message + "\n"); if (!good) throw new System.Exception(message); }
        static void Run()
        {
            if(Application.isBatchMode&&!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling&&File.Exists("Temp/InformantEditorExit")){File.Delete("Temp/InformantEditorExit");EditorApplication.Exit(0);return;}
            if (!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && File.Exists("Temp/PrisonInformantCheck")) { File.Delete("Temp/PrisonInformantCheck"); File.WriteAllText(Log, "Informant entrance play-mode/API check\n"); SessionState.SetBool("PrisonInformantCheck", true); EditorApplication.EnterPlaymode(); return; }
            if (!running || !EditorApplication.isPlaying || EditorApplication.timeSinceStartup < until) return;
            try
            {
                if (EditorApplication.timeSinceStartup > deadline) throw new System.Exception("Timed out");
                if (step == 0) { SceneManager.LoadScene("PrisonChapter"); step = 1; until = EditorApplication.timeSinceStartup + 5; return; }
                var intro = Object.FindFirstObjectByType<PrisonIntroduction>(); var scene = intro.informantEntrance;
                if (step == 1)
                {
                    intro.DeveloperComplete();
                    intro.dialogue = Object.Instantiate(intro.dialogue); intro.dialogue.lineSeconds = 1;
                    typeof(PrisonIntroduction).GetProperty("State").SetValue(intro, PrisonIntroduction.Stage.Yard);
                    var player = intro.mission.player; var body = player.GetComponent<CharacterController>(); body.enabled = false; player.transform.position = intro.informant.position + intro.informant.forward * 2.5f + Vector3.up; body.enabled = true;
                    intro.mingi.agent.Warp(intro.informant.position + intro.informant.forward * 2.4f - intro.informant.right);
                    Check(intro.BeginMeeting(), "meeting begins from player interaction range"); step = 2; return;
                }
                if (!bowed && scene.BowAmount > .95f)
                {
                    Check(scene.petitioner.gameObject.activeInHierarchy, "petitioner visible during bow");
                    foreach (var actor in new[] { scene.petitioner, intro.informant }) { var p = intro.filmCamera.WorldToViewportPoint(actor.position + Vector3.up * 1.3f); Check(p.z > 0 && p.x > .05f && p.x < .95f && p.y > .05f && p.y < .95f, "both speakers framed: " + actor.name); }
                    PrisonChickenCheck.Capture(intro.filmCamera, "Logs/Prison-informant-bow.png"); bowed = true;
                }
                if (!replied && intro.Subtitle.StartsWith("최형욱: 됐어")) { if(replyAt==0)replyAt=EditorApplication.timeSinceStartup+1.2; if(EditorApplication.timeSinceStartup>=replyAt){PrisonChickenCheck.Capture(intro.filmCamera, "Logs/Prison-informant-response.png"); replied = true;} }
                if (!firstLine && intro.Subtitle == "최형욱: 이번엔 또 뭐야? 공무원 준비해야 하니까 꺼져") { Check(scene.Departed && !scene.Playing && !scene.petitioner.gameObject.activeSelf, "petitioner walks entire exit route before handoff"); firstLine = true; }
                if (step == 2 && intro.State == PrisonIntroduction.Stage.Complete)
                {
                    Check(bowed && replied && firstLine, "bow, response, requested line all played");
                    var move=intro.mission.player.GetComponent<PlayerMove>();var look=intro.mission.player.View.GetComponent<CameraMove>();
                    Check(!intro.Cinematic && move.enabled && move.isControllable && look.enabled && look.isControllable && intro.mission.player.View.enabled && intro.mingi.Following, "player controls and companion follow restored; move="+move.enabled+"/"+move.isControllable+" look="+look.enabled+"/"+look.isControllable+" follow="+intro.mingi.Following+" cursor="+Cursor.lockState);
                    File.AppendAllText(Log, "FINISHED\n"); running = false; EditorApplication.ExitPlaymode();
                }
            }
            catch (System.Exception e) { File.AppendAllText(Log, e + "\n"); running = false; EditorApplication.ExitPlaymode(); }
        }
    }
}
#endif
