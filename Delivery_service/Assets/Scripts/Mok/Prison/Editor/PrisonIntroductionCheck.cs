#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Object=UnityEngine.Object;

namespace DeliveryService.Mok.Editor
{
    [InitializeOnLoad]
    public static class PrisonIntroductionCheck
    {
        static bool running, captured, cellCaptured, sleepCaptured, sawYard, sawPlan, sawPetitioner;
        static int stage, corner, workStep; static double workUntil;
        static Vector3[] route; static Vector3 walkLast; static double walkProgress;
        static double deadline;
        const string Log="Logs/Prison-introduction-check.txt";
        static PrisonIntroductionCheck(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("PrisonIntroCheck",false)){running=true;stage=0;workStep=0;captured=cellCaptured=sleepCaptured=sawYard=sawPlan=sawPetitioner=false;deadline=EditorApplication.timeSinceStartup+300;SessionState.SetBool("PrisonIntroCheck",false);}};}
        public static void Run(){if(!PrisonChapterCheck.Active)EditorSceneManager.OpenScene("Assets/Scenes/PrisonChapter.unity");File.WriteAllText(Log,"Prison introduction play-mode check\n");SessionState.SetBool("PrisonIntroCheck",true);EditorApplication.EnterPlaymode();}
        static void Check(bool ok,string text){File.AppendAllText(Log,(ok?"PASS ":"FAIL ")+text+"\n");if(!ok)throw new Exception(text);}
        static void Tick()
        {
            if(!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling&&File.Exists("Temp/PrisonIntroOnlyCheck")){
                File.Delete("Temp/PrisonIntroOnlyCheck");
                var scene=EditorSceneManager.GetActiveScene();
                if(scene.name!="PrisonChapter")return;
                EditorSceneManager.SaveScene(scene,"Logs/Prison-before-intro-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
                File.WriteAllText(Log,"Prison introduction play-mode check\n");
                SessionState.SetBool("PrisonIntroCheck",true);EditorApplication.EnterPlaymode();return;
            }
            if(!running||!EditorApplication.isPlaying)return;
            try{
                var intro=Object.FindFirstObjectByType<PrisonIntroduction>();if(!intro)return;
                if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Timed out at "+intro.State+" player="+intro.mission.player.transform.position);
                if(System.Array.IndexOf(intro.dialogue.yardWalk,intro.Subtitle)>=0)sawYard=true;
                if(System.Array.IndexOf(intro.dialogue.kitchenPlan,intro.Subtitle)>=0)sawPlan=true;
                if(System.Array.IndexOf(intro.dialogue.informantEntrance,intro.Subtitle)>=0&&!sawPetitioner){sawPetitioner=true;Capture(intro.filmCamera,"Logs/Prison-informant-entrance.png");}
                var notices=Object.FindFirstObjectByType<MissionMilestoneHUD>();if(notices){var card=notices.transform.Find("Mission notice") as RectTransform;if(card&&card.anchorMax.y<.65f)throw new Exception("Mission notice covers screen center");}
                if(stage==0){intro.dialogue=Object.Instantiate(intro.dialogue);intro.dialogue.lineSeconds=1;stage=1;}
                if(intro.State==PrisonIntroduction.Stage.Mugshot&&intro.Fade<.01f&&!captured){Capture(intro.filmCamera,"Logs/Prison-intro-mugshot.png");captured=true;Check(intro.Cinematic&&!intro.mission.player.CanAct,"mugshot blocks player actions");}
                if(intro.State==PrisonIntroduction.Stage.Cell&&intro.Fade<.01f&&!cellCaptured){Capture(intro.filmCamera,"Logs/Prison-intro-cell.png");cellCaptured=true;Check(Vector3.Distance(intro.mingi.transform.position,intro.mission.player.transform.position)<4,"Mingi and player share cell");}
                if(stage==1&&intro.State==PrisonIntroduction.Stage.Work){
                    var work=intro.firstWork;var p=intro.mission.player;
                    if(workStep==0){Landscape(intro,"Logs/Prison-daylight.png");StartWalk(intro,work.approach.position);workStep=10;}
                    else if(workStep==10){if(WalkRoute(intro)){p.GetComponent<PlayerMove>().enabled=true;workStep=1;workUntil=EditorApplication.timeSinceStartup+.3;}}
                    else if(workStep==1&&EditorApplication.timeSinceStartup>workUntil){PressF(true);workStep=2;workUntil=EditorApplication.timeSinceStartup+1.7;}
                    else if(workStep==2){PressF(false);Mouse(true);if(EditorApplication.timeSinceStartup>workUntil){Capture(intro.filmCamera,"Logs/Prison-work-raised.png");var grip=work.GetComponent<PrisonWoodFeedback>();Check(grip.hands[0].gameObject.activeSelf&&grip.hands[1].gameObject.activeSelf,"both grips visible while charging");Mouse(false);workStep=3;workUntil=EditorApplication.timeSinceStartup+1.2;}}
                    else if(workStep==3&&EditorApplication.timeSinceStartup>workUntil){
                        Check(Vector3.Distance(work.BladeWorld,work.station.position)<.015f,"axe blade contacts board cutting line");
                        Capture(intro.filmCamera,"Logs/Prison-work-contact.png");
                        if(work.Completed<work.Required){workStep=2;workUntil=EditorApplication.timeSinceStartup+1.7;}
                        else{Check(work.Completed==5&&work.Returning&&!work.Working,"five strikes complete work");var fx=work.GetComponent<PrisonWoodFeedback>();Check(fx&&fx.ImpactCount==5&&fx.hands.Length==2&&!fx.hands[0].gameObject.activeSelf&&!fx.hands[1].gameObject.activeSelf,"five single impacts and hands hidden after work");workStep=4;}
                    }
                    else if(workStep==4&&work.ReadyToSleep){Check(intro.GetComponent<PrisonEvening>().IsEvening&&RenderSettings.sun.intensity<1,"evening changes actual sun and sky");Landscape(intro,"Logs/Prison-evening.png");StartWalk(intro,intro.cellSpawn.position);workStep=12;}
                    else if(workStep==12){if(WalkRoute(intro)){p.GetComponent<PlayerMove>().enabled=true;workStep=5;}}
                    else if(workStep==5){PressF(true);workStep=6;}
                    else if(workStep==6){PressF(false);Check(work.Sleeping,"collision walk back to cell and sleep");workStep=7;}
                    else if(workStep==7&&intro.Subtitle==intro.dialogue.bedtime){Check(Vector3.Distance(intro.mingi.transform.position,intro.cellMingi.position)<.2f,"bedtime starts with Mingi inside cell");Capture(intro.filmCamera,"Logs/Prison-bedtime-cell.png");workStep=8;}
                }
                var bed=intro.GetComponent<PrisonBedtimePresentation>();
                if(bed&&bed.Lying&&!sleepCaptured){Check(Vector3.Distance(intro.filmCamera.transform.position,bed.pillow)<.02f&&intro.Fade<.1f,"lying view reaches pillow before slow fade");Capture(intro.filmCamera,"Logs/Prison-lying-view.png");sleepCaptured=true;}
                if(stage==1&&intro.State==PrisonIntroduction.Stage.Yard&&intro.Fade==0){
                    Check(sleepCaptured,"bedtime presentation completed before time jump");Check(!intro.Cinematic&&intro.mission.player.CanAct,"first-person handoff restores controls");Check(!intro.GetComponent<PrisonEvening>().IsEvening,"morning restores original lighting");
                    var panel=Object.FindFirstObjectByType<DeliveryService.Yun.Settings.GameSettingsPanel>(FindObjectsInactive.Include);panel.gameObject.SetActive(true);Check(Time.timeScale==0,"shared ESC menu pauses");panel.Close();
                    NavMesh.SamplePosition(intro.mission.player.transform.position,out var from,2,NavMesh.AllAreas);NavMesh.SamplePosition(intro.informant.position+intro.informant.forward*2,out var to,2,NavMesh.AllAreas);
                    var path=new NavMeshPath();Check(NavMesh.CalculatePath(from.position,to.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete,"cell to yard connected route");route=path.corners;corner=1;
                    intro.mission.player.GetComponent<PlayerMove>().enabled=false;stage=2;
                }
                if(stage==2){
                    var player=intro.mission.player;var body=player.GetComponent<CharacterController>();
                    if(corner<route.Length){var delta=Vector3.ProjectOnPlane(route[corner]-player.transform.position,Vector3.up);if(delta.magnitude<.45f)corner++;else{player.transform.rotation=Quaternion.LookRotation(delta);body.Move((delta.normalized*4+Vector3.down*2)*Mathf.Min(Time.deltaTime,.04f));}}
                    else{if(Vector3.Distance(intro.mingi.transform.position,player.transform.position)>6)return;Check(intro.mingi.animator.enabled&&intro.mingi.agent.isOnNavMesh,"Mingi follows player to yard with animator");player.GetComponent<PlayerMove>().enabled=true;Check(intro.BeginMeeting(),"walking route reaches interactable informant; player="+player.transform.position+" target="+intro.informant.position+" failing="+intro.mission.Failing+" stage="+intro.State+" scale="+Time.timeScale);stage=3;}
                }
                if(stage==3&&intro.State==PrisonIntroduction.Stage.Conversation&&intro.Subtitle.Length>0){Capture(intro.filmCamera,"Logs/Prison-intro-meeting.png");stage=4;}
                if(stage==4&&intro.State==PrisonIntroduction.Stage.Complete){Check(!intro.Cinematic&&intro.mission.player.CanAct,"meeting ends with restored player controls");Check(intro.Target==null,"completed mission clears target");Check(sawYard&&sawPlan&&sawPetitioner,"walking dialogue, petitioner entrance and kitchen plan all played");running=false;if(PrisonChapterCheck.Active)PrisonChapterCheck.Finish(!System.IO.File.ReadAllText(Log).Contains("Exception"));else EditorApplication.ExitPlaymode();}
            }catch(Exception e){File.AppendAllText(Log,e+"\n");running=false;if(PrisonChapterCheck.Active)PrisonChapterCheck.Finish(!System.IO.File.ReadAllText(Log).Contains("Exception"));else EditorApplication.ExitPlaymode();}
        }
        static void Landscape(PrisonIntroduction intro,string file){var c=intro.filmCamera;var p=c.transform.position;var r=c.transform.rotation;c.transform.position=new Vector3(14,2,10);c.transform.LookAt(new Vector3(0,6,38));Capture(c,file);c.transform.SetPositionAndRotation(p,r);}
        static void PressF(bool down){UnityEngine.InputSystem.InputSystem.QueueStateEvent(UnityEngine.InputSystem.Keyboard.current,down?new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.F):new UnityEngine.InputSystem.LowLevel.KeyboardState());}
        static void Mouse(bool down){var state=new UnityEngine.InputSystem.LowLevel.MouseState();if(down)state=state.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left);UnityEngine.InputSystem.InputSystem.QueueStateEvent(UnityEngine.InputSystem.Mouse.current,state);}
        static void StartWalk(PrisonIntroduction intro,Vector3 target){var p=intro.mission.player;p.GetComponent<PlayerMove>().enabled=false;NavMesh.SamplePosition(p.transform.position,out var a,2,NavMesh.AllAreas);NavMesh.SamplePosition(target,out var b,2,NavMesh.AllAreas);var path=new NavMeshPath();Check(NavMesh.CalculatePath(a.position,b.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete,"connected route to "+target);route=path.corners;corner=1;walkLast=p.transform.position;walkProgress=EditorApplication.timeSinceStartup;}
        static bool WalkRoute(PrisonIntroduction intro){var p=intro.mission.player;var body=p.GetComponent<CharacterController>();if(corner>=route.Length)return true;var delta=Vector3.ProjectOnPlane(route[corner]-p.transform.position,Vector3.up);if(delta.magnitude<.4f){corner++;return false;}p.transform.rotation=Quaternion.LookRotation(delta);body.Move((delta.normalized*4+Vector3.down*2)*Mathf.Min(Time.deltaTime,.04f));if(Vector3.Distance(walkLast,p.transform.position)>.2f){walkLast=p.transform.position;walkProgress=EditorApplication.timeSinceStartup;}if(EditorApplication.timeSinceStartup-walkProgress>8)throw new Exception("Collision walking blocked at "+p.transform.position+" toward "+route[corner]);return false;}
        static void Capture(Camera camera,string path){var previous=camera.targetTexture;var rt=new RenderTexture(1280,720,24);var active=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());camera.targetTexture=previous;RenderTexture.active=active;Object.DestroyImmediate(image);Object.DestroyImmediate(rt);}
    }
}
#endif











