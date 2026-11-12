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
        static bool running, captured, cellCaptured;
        static int stage, corner;
        static Vector3[] route;
        static double deadline;
        const string Log="Logs/Prison-introduction-check.txt";
        static PrisonIntroductionCheck(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("PrisonIntroCheck",false)){running=true;stage=0;captured=cellCaptured=false;deadline=EditorApplication.timeSinceStartup+160;SessionState.SetBool("PrisonIntroCheck",false);}};}
        public static void Run(){EditorSceneManager.OpenScene("Assets/Scenes/PrisonChapter.unity");File.WriteAllText(Log,"Prison introduction play-mode check\n");SessionState.SetBool("PrisonIntroCheck",true);EditorApplication.EnterPlaymode();}
        static void Check(bool ok,string text){File.AppendAllText(Log,(ok?"PASS ":"FAIL ")+text+"\n");if(!ok)throw new Exception(text);}
        static void Tick()
        {
            if(!running||!EditorApplication.isPlaying)return;
            try{
                var intro=Object.FindFirstObjectByType<PrisonIntroduction>();if(!intro)return;
                if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Timed out at "+intro.State+" player="+intro.mission.player.transform.position);
                if(stage==0){intro.dialogue=Object.Instantiate(intro.dialogue);intro.dialogue.lineSeconds=1;stage=1;}
                if(intro.State==PrisonIntroduction.Stage.Mugshot&&intro.Fade<.01f&&!captured){Capture(intro.filmCamera,"Logs/Prison-intro-mugshot.png");captured=true;Check(intro.Cinematic&&!intro.mission.player.CanAct,"mugshot blocks player actions");}
                if(intro.State==PrisonIntroduction.Stage.Cell&&intro.Fade<.01f&&!cellCaptured){Capture(intro.filmCamera,"Logs/Prison-intro-cell.png");cellCaptured=true;Check(Vector3.Distance(intro.mingi.transform.position,intro.mission.player.transform.position)<4,"Mingi and player share cell");}
                if(stage==1&&intro.State==PrisonIntroduction.Stage.Yard&&intro.Fade==0){
                    Check(!intro.Cinematic&&intro.mission.player.CanAct,"first-person handoff restores controls");
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
                if(stage==4&&intro.State==PrisonIntroduction.Stage.Complete){Check(!intro.Cinematic&&intro.mission.player.CanAct,"meeting ends with restored player controls");Check(intro.Target==null,"completed mission clears target");running=false;EditorApplication.ExitPlaymode();}
            }catch(Exception e){File.AppendAllText(Log,e+"\n");running=false;EditorApplication.ExitPlaymode();}
        }
        static void Capture(Camera camera,string path){var previous=camera.targetTexture;var rt=new RenderTexture(1280,720,24);var active=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());camera.targetTexture=previous;RenderTexture.active=active;Object.DestroyImmediate(image);Object.DestroyImmediate(rt);}
    }
}
#endif



