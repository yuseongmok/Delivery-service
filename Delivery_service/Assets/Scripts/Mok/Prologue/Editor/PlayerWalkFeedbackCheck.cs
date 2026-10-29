#if UNITY_EDITOR
using System;using System.Collections;using System.IO;using UnityEngine;
namespace DeliveryService.Mok.Editor {
public static class PlayerWalkFeedbackCheck {
    static string log;
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);log+="PASS "+message+"\n";File.WriteAllText("Logs/Player-walk-feedback-check.txt",log);}
    public static IEnumerator Verify(){
        log="Walking feedback\n";yield return null;yield return null;
        var d=UnityEngine.Object.FindFirstObjectByType<BankPrologue>();
        var stance=d.player.GetComponent<PlayerTacticalStance>();Check(stance&&stance.WalkFeedback,"prologue stance installs shared walking feedback");
        Check(Resources.Load<AudioClip>("MokWalk/BootStep"),"footstep audio available in build resources");
        var go=new GameObject("Walking feedback test");var f=go.AddComponent<PlayerWalkFeedback>();
        try{
            f.Tick(true,true,false,0,.02f);float max=0;
            for(int i=0;i<100;i++){go.transform.position+=Vector3.forward*.08f;f.Tick(true,true,false,0,.02f);max=Mathf.Max(max,f.CameraOffset.magnitude);}
            Check(f.Footsteps>=4&&f.Footsteps<=5,"walk cadence follows real distance");Check(max>.008f&&max<.025f,"camera movement stays subtle");
            int count=f.Footsteps;for(int i=0;i<100;i++)f.Tick(true,true,false,0,.02f);
            Check(f.Footsteps==count&&f.CameraOffset.magnitude<.0001f&&Quaternion.Angle(f.HandRotation,Quaternion.identity)<.01f,"standing still settles camera and weapon without footsteps");
            float normal=0,aimed=0;
            f.Tick(false,false,false,0,.02f);f.Tick(true,true,false,0,.02f);
            for(int i=0;i<50;i++){go.transform.position+=Vector3.forward*.08f;f.Tick(true,true,false,0,.02f);normal=Mathf.Max(normal,f.CameraOffset.magnitude);}
            f.Tick(false,false,false,0,.02f);f.Tick(true,true,false,1,.02f);
            for(int i=0;i<50;i++){go.transform.position+=Vector3.forward*.08f;f.Tick(true,true,false,1,.02f);aimed=Mathf.Max(aimed,f.CameraOffset.magnitude);}
            Check(aimed<normal*.2f,"aim reduces walking motion");
            count=f.Footsteps;go.transform.position+=Vector3.forward*20;f.Tick(true,true,false,0,.02f);Check(f.Footsteps==count&&f.CameraOffset==Vector3.zero,"teleport does not trigger a step");
            f.Tick(false,false,false,0,0);Check(f.CameraOffset==Vector3.zero&&f.HandOffset==Vector3.zero&&!go.GetComponent<AudioSource>().isPlaying,"cinematic pause vehicle and ragdoll gate clears feedback");
            f.cameraAmount=0;f.Tick(true,true,false,0,.02f);for(int i=0;i<30;i++){go.transform.position+=Vector3.forward*.08f;f.Tick(true,true,false,0,.02f);}Check(f.CameraOffset==Vector3.zero,"zero camera intensity disables bob");
        }finally{UnityEngine.Object.Destroy(go);}
    }
}
}
#endif
