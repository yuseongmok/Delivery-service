#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Object=UnityEngine.Object;
namespace DeliveryService.Mok.Editor
{
    [InitializeOnLoad]
    public static class PrisonKitchenArtCheck
    {
        static bool running,closeup;static int stage;static float wait;static double deadline;
        const string Log="Logs/Prison-kitchen-art-check.txt";
        static PrisonKitchenArtCheck(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("KitchenArtCheck",false)){SessionState.SetBool("KitchenArtCheck",false);running=true;stage=0;closeup=false;deadline=EditorApplication.timeSinceStartup+60;}};}
        public static void Run(){EditorSceneManager.OpenScene("Assets/Scenes/PrisonChapter.unity");File.WriteAllText(Log,"Kitchen art / wake reaction play-mode checks\n");SessionState.SetBool("KitchenArtCheck",true);EditorApplication.EnterPlaymode();}
        static void Check(bool okay,string message){File.AppendAllText(Log,(okay?"PASS ":"FAIL ")+message+"\n");if(!okay)throw new Exception(message);}
        static void Set(object target,string property,object value)=>target.GetType().GetProperty(property).SetValue(target,value);
        static void Tick(){if(!running||!EditorApplication.isPlaying)return;try{
            var m=Object.FindFirstObjectByType<PrisonChickenMission>();if(!m)return;if(EditorApplication.timeSinceStartup>deadline)throw new Exception("timeout at "+stage);
            if(stage==0){stage=1;wait=Time.time+2;return;}if(Time.time<wait)return;
            if(stage==1){var intro=m.prison.introduction;intro.StopAllCoroutines();typeof(PrisonIntroduction).GetMethod("SetCinematic",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(intro,new object[]{false});Set(intro,"State",PrisonIntroduction.Stage.Complete);Set(intro,"Card","");Set(intro,"Fade",0f);intro.mugshotActor.gameObject.SetActive(false);m.ResetKitchen();Check(Mathf.Abs(Vector3.Dot(m.chef.up,Vector3.up))<.05f,"chef lies down on cot");Check(m.GetComponentsInChildren<ChickenNoiseHazard>().Length==16,"sixteen readable floor hazards");
                foreach(var p in m.ingredients){NavMesh.SamplePosition(m.checkpoint.position,out var from,3,NavMesh.AllAreas);NavMesh.SamplePosition(new Vector3(p.transform.position.x,0,p.transform.position.z)+Vector3.forward,out var to,3,NavMesh.AllAreas);var path=new NavMeshPath();Check(NavMesh.CalculatePath(from.position,to.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete,"navigation to "+p.name);}
                stage=2;wait=Time.time+1.1f;return;
            }
            if(stage>=2&&stage<=4){m.MakeNoise(false);stage++;wait=Time.time+1.1f;return;}
            if(stage==5){if(m.Failing){if(!closeup&&m.camera.fieldOfView<43){Check(Vector3.Dot(m.chef.up,Vector3.up)>.99f,"chef stands upright");Check(Vector3.Distance(m.camera.transform.position,m.chefReaction.head.position)<1,"face closeup camera");PrisonChickenCheck.Capture(m.camera,"Logs/Kitchen-chef-wake-closeup.png");closeup=true;}return;}Check(closeup,"wake closeup was shown before retry");Check(m.State==PrisonChickenMission.Stage.Cook&&m.Noise==0&&m.prison.player.CanAct,"failure resets mission and input");Check(Mathf.Abs(Vector3.Dot(m.chef.up,Vector3.up))<.05f,"retry restores sleeping pose");running=false;EditorApplication.ExitPlaymode();}
        }catch(Exception e){File.AppendAllText(Log,e.ToString());running=false;EditorApplication.ExitPlaymode();}}
    }
}
#endif
