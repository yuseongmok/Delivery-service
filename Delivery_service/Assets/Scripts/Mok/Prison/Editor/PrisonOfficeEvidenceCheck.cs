#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
namespace DeliveryService.Mok.Editor
{
    [InitializeOnLoad] public static class PrisonOfficeEvidenceCheck
    {
        const string Log="Logs/Prison-office-evidence-check.txt";
        static bool running;static bool visual;static int step;static float wait;static double deadline;
        static PrisonOfficeEvidenceCheck(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("EvidenceCheck",false)){SessionState.SetBool("EvidenceCheck",false);running=true;visual=SessionState.GetBool("EvidenceVisual",false);step=0;deadline=EditorApplication.timeSinceStartup+240;}};}
        static void Check(bool b,string text){File.AppendAllText(Log,(b?"PASS ":"FAIL ")+text+"\n");if(!b)throw new Exception(text);}
        static void Place(PlayerCombat p,Vector3 at){var cc=p.GetComponent<CharacterController>();cc.enabled=false;p.transform.position=at;cc.enabled=true;Physics.SyncTransforms();}
        static bool Use(PrisonOfficeEvidence e,int index)
        {
            var p=e.office.previous.previous.prison.player;var target=e.evidencePoints[index];
            p.View.transform.LookAt(target.position+Vector3.up*.05f);
            Physics.SyncTransforms();
            Check(e.TryInteraction(),"F interaction ray selects evidence prop "+index);
            return e.Busy;
        }
        static void Tick()
        {
            if(!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling&&File.Exists("Temp/PrisonOfficeEvidenceCheck")){visual=File.Exists("Temp/PrisonOfficeEvidenceVisual");File.Delete("Temp/PrisonOfficeEvidenceVisual");SessionState.SetBool("EvidenceVisual",visual);File.Delete("Temp/PrisonOfficeEvidenceCheck");File.WriteAllText(Log,"Assisted stage entry and travel; real interactions/coroutines and vehicle checkpoint.\n");SessionState.SetBool("EvidenceCheck",true);EditorApplication.EnterPlaymode();return;}
            if(!running||!EditorApplication.isPlaying)return;
            try{
                if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Timeout "+step);
                var e=Object.FindFirstObjectByType<PrisonOfficeEvidence>();if(!e)return;var o=e.office;var p=o.previous.previous.prison.player;
                if(step==0){step=1;wait=Time.time+2;return;}if(Time.time<wait)return;
                if(step==1){
                    o.previous.previous.prison.introduction.DeveloperComplete();
                    Object.FindFirstObjectByType<PrisonChickenMission>().DeveloperComplete();
                    typeof(PrisonKeysMission).GetProperty("State").SetValue(o.previous,PrisonKeysMission.Stage.Complete);
                    o.previous.warden.gameObject.SetActive(true);o.previous.warden.enabled=false;
                    o.previous.warden.GetComponent<NPCControl>().ExternalControl=true;
                    o.previous.warden.GetComponent<NPCControl>().KnockDown(Vector3.zero,0);
                    e.dialogue=Object.Instantiate(e.dialogue);e.dialogue.lineSeconds=1.2f;
                    step=2;wait=Time.time+.5f;return;
                }
                if(step==2){
                    if(visual){typeof(PrisonOfficeEvidence).GetProperty("Discovered").SetValue(e,3);step=5;return;}
                    Check(o.Active,"office starts after pizza mission");
                    Place(p,o.points[0].transform.position+new Vector3(0,1.1f,-1));
                    Check(!o.points[0].TryInteract(),"supplies locked before evidence and approval");
                    Place(p,e.evidencePoints[0].position+new Vector3(0,.2f,-1));
                    Check(!e.Inspect(2),"phone cannot skip discovery order");Check(Use(e,0),"ledger interaction starts");step=3;wait=Time.time+4;return;
                }
                if(step==3){Check(!e.Busy&&e.Discovered==1&&p.CanAct,"ledger returns control");Place(p,e.evidencePoints[1].position+new Vector3(0,.2f,-1));Check(Use(e,1),"hostage transfer document");step=4;wait=Time.time+5;return;}
                if(step==4){Check(e.Discovered==2&&!e.Busy,"transfer recorded");Place(p,e.evidencePoints[2].position+new Vector3(0,.2f,-1));Check(Use(e,2),"Jinwoong message");step=5;wait=Time.time+5;return;}
                if(step==5){Check(e.Discovered==3&&!e.Authorized&&!o.Complete,"evidence does not skip confrontation");Place(p,e.wardenStand.position+e.wardenStand.forward*1.7f+Vector3.up*1.1f);File.AppendAllText(Log,"Confront state busy="+e.Busy+" canAct="+p.CanAct+" distance="+Vector3.Distance(p.transform.position,e.Target.position)+" target="+e.Target.position+"\n");Check(e.Inspect(3),"confront warden");step=6;wait=Time.time+4;return;}
                if(step==6){PrisonChickenCheck.Capture(o.previous.camera,"Logs/Prison-evidence-confrontation.png");step=61;wait=Time.time+8;return;}
                if(step==61){var a=o.previous.warden.GetComponentInChildren<Animator>();File.AppendAllText(Log,"PHONE "+e.phone.position+" HAND "+a.GetBoneTransform(HumanBodyBones.RightHand).position+" HEAD "+a.GetBoneTransform(HumanBodyBones.Head).position+"\n");PrisonChickenCheck.Capture(o.previous.camera,"Logs/Prison-evidence-call.png");step=7;wait=Time.time+7;return;}
                if(step==7){Check(e.Authorized&&!e.Busy&&p.CanAct&&p.View.enabled,"approval restores player view and control");Check(!o.Complete,"still requires uniform, key and password");if(visual){running=false;EditorApplication.ExitPlaymode();return;}Place(p,o.points[0].transform.position+new Vector3(0,1.1f,-1));Check(o.points[0].TryInteract(),"unlocked uniform drawer");step=8;wait=Time.time+.7f;return;}
                if(step==8){Check(o.points[0].TryInteract(),"take uniform");step=9;wait=Time.time+3;return;}
                if(step==9){Check(o.HasUniforms,"uniform animation completes");Place(p,o.points[1].transform.position+new Vector3(0,1.1f,-1));Check(o.points[1].TryInteract(),"open vehicle key drawer");step=10;wait=Time.time+.7f;return;}
                if(step==10){Check(o.points[1].TryInteract(),"take vehicle key");step=11;wait=Time.time+3;return;}
                if(step==11){Check(o.HasVehicleKey,"vehicle key animation completes");Place(p,o.points[2].transform.position+Vector3.back);Check(o.points[2].TryInteract(),"read password");Check(o.Complete,"evidence and all supplies complete");running=false;PrisonEscapeCheck.ContinueChapter();return;}

            }catch(Exception ex){File.AppendAllText(Log,ex.ToString());running=false;EditorApplication.ExitPlaymode();}
        }
    }
}
#endif
