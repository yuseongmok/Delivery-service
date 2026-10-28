using System.Collections;
using UnityEngine;
namespace DeliveryService.Mok {
// 운전석 탑승과 조수석 지원 사격. 본편 전투·수배 상태는 변경하지 않는다.
[DefaultExecutionOrder(150)]
public sealed class PrologueEscapePerformance : MonoBehaviour {
    public BankPrologue director;
    public Transform driverDoor, partnerGun, muzzle;
    public PrologueGunFeedback feedback;
    public ProloguePursuitCar[] pursuitCars;
    public float boardingSeconds=3.5f, supportDelay=3, shotSpacing=.32f;
    public string supportLine="민기: 운전에 집중해! 뒤는 내가 맡을게!";
    public string clearLine="민기: 따돌렸어. 그대로 가!";
    public int SupportShots {get;private set;}
    public bool PursuitEnded {get;private set;}
    public bool Boarding {get;private set;}
    float drivingTime,nextShot,poseWeight,recoil;
    int targetIndex,shotsAtTarget;
    ProloguePursuitCar target;
    public static bool VisibleSpawn(Camera camera,Vector3 feet){
        if(!camera)return true;
        var planes=GeometryUtility.CalculateFrustumPlanes(camera);
        if(!GeometryUtility.TestPlanesAABB(planes,new Bounds(feet+Vector3.up, new Vector3(1.4f,2.2f,1.4f))))return false;
        foreach(var offset in new[]{Vector3.up*.35f,Vector3.up,Vector3.up*1.9f,Vector3.up+camera.transform.right*.55f,Vector3.up-camera.transform.right*.55f}){
            var point=feet+offset;var delta=point-camera.transform.position;bool blocked=false;
            foreach(var hit in Physics.RaycastAll(camera.transform.position,delta.normalized,delta.magnitude,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)){
                if(hit.collider.GetComponentInParent<PrologueOfficer>()||hit.collider.GetComponentInParent<ProloguePlayer>()||hit.collider.GetComponentInParent<PrologueAccomplice>())continue;
                blocked=true;break;
            }
            if(!blocked)return true;
        }
        return false;
    }
    public IEnumerator BoardFirstPerson(){
        Boarding=true;var d=director;var cam=d.filmCamera;float near=cam.nearClipPlane;cam.nearClipPlane=.03f;
        var start=d.player.view.transform.position;var rotation=d.player.view.transform.rotation;
        var performance=d.accomplice?d.accomplice.performance:null;
        if(performance)StartCoroutine(performance.EnterVehicle());
        var source=GetComponent<AudioSource>();if(!source){source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;}
        if(performance&&performance.doorOpen)source.PlayOneShot(performance.doorOpen,.6f);
        try{
            for(float elapsed=0;elapsed<boardingSeconds;elapsed+=Time.deltaTime){
                if(d.CinematicSkipRequested)break;
                float t=elapsed/boardingSeconds;
                float open=Mathf.SmoothStep(0,1,t/.23f)*(1-Mathf.SmoothStep(0,1,(t-.78f)/.22f));
                if(driverDoor)driverDoor.localRotation=Quaternion.Euler(0,70*open,0);
                var car=d.getawayCar;var doorway=car.TransformPoint(new Vector3(-1.05f,1.12f,.16f));var eye=car.TransformPoint(d.drive.driverEye);
                var position=t<.48f?Vector3.Lerp(start,doorway,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.18f,.48f,t))):Vector3.Lerp(doorway,eye,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.48f,.83f,t)));
                position.y-=Mathf.Sin(Mathf.PI*Mathf.Clamp01(Mathf.InverseLerp(.25f,.83f,t)))*.1f;
                var look=Quaternion.LookRotation(car.TransformPoint(new Vector3(-.6f,1,.6f))-start);
                cam.transform.SetPositionAndRotation(position,t<.4f?Quaternion.Slerp(rotation,look,Mathf.SmoothStep(0,1,t/.4f)):Quaternion.Slerp(look,car.rotation,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.4f,.9f,t))));
                yield return null;
            }
            while(performance&&!performance.EnteredCar)yield return null;
        }finally{
            if(driverDoor)driverDoor.localRotation=Quaternion.identity;
            cam.transform.SetPositionAndRotation(d.getawayCar.TransformPoint(d.drive.driverEye),d.getawayCar.rotation);
            cam.nearClipPlane=near;Boarding=false;
        }
        if(performance&&performance.doorClose)source.PlayOneShot(performance.doorClose,.6f);
    }
    void Start(){if(partnerGun)partnerGun.gameObject.SetActive(false);}
    void Update(){
        if(!director||director.Paused)return;
        bool driving=director.drive&&director.drive.Driving;
        if(!driving){if(partnerGun)partnerGun.gameObject.SetActive(false);return;}
        if(director.drive.Speed>2)drivingTime+=Time.deltaTime;
        if(drivingTime<supportDelay||PursuitEnded)return;
        if(!target){
            while(targetIndex<pursuitCars.Length&&(!pursuitCars[targetIndex]||pursuitCars[targetIndex].DisabledByPartner))targetIndex++;
            if(targetIndex>=pursuitCars.Length){PursuitEnded=true;director.subtitle.text=clearLine;return;}
            target=pursuitCars[targetIndex];
            if(!target.Pursuing){target=null;return;}
            director.subtitle.text=supportLine;nextShot=Time.time+.8f;
        }
        poseWeight=Mathf.MoveTowards(poseWeight,1,Time.deltaTime*2.5f);
        if(partnerGun)partnerGun.gameObject.SetActive(true);
        if(Time.time<nextShot||poseWeight<.95f)return;
        nextShot=Time.time+shotSpacing;SupportShots++;shotsAtTarget++;recoil=1;
        if(feedback)feedback.Fire();else GunshotSound.Play(gameObject,false);
        if(muzzle)PrologueTracer.Spawn(muzzle.position,target.transform.position+Vector3.up*.8f,new Color(1,.65f,.2f));
        var hitFeedback=director.GetComponent<PrologueCombatFeedback>();
        if(hitFeedback)hitFeedback.Impact(target.transform.position+target.transform.forward*1.5f+Vector3.up*.8f,target.transform.forward,false);
        if(shotsAtTarget>=5){target.DisableByPartner();shotsAtTarget=0;targetIndex++;target=null;}
    }
    void LateUpdate(){
        if(!director||director.Paused||!director.drive.Driving||drivingTime<supportDelay)return;
        if(PursuitEnded)poseWeight=Mathf.MoveTowards(poseWeight,0,Time.deltaTime*2);
        if(poseWeight<=0){if(partnerGun)partnerGun.gameObject.SetActive(false);return;}
        var anim=director.accomplice.animator;if(!anim||!anim.isHuman||!partnerGun)return;
        var car=director.getawayCar;
        var chest=anim.GetBoneTransform(HumanBodyBones.Chest);
        if(chest)chest.rotation=Quaternion.AngleAxis(65*poseWeight,car.up)*chest.rotation;
        var head=anim.GetBoneTransform(HumanBodyBones.Head);
        if(head)head.rotation=Quaternion.AngleAxis(35*poseWeight,car.up)*head.rotation;
        var grip=car.TransformPoint(new Vector3(.98f,1.17f,-.3f));
        Vector3 aim=target?target.transform.position+Vector3.up*.8f:car.position-car.forward*15;
        partnerGun.SetPositionAndRotation(grip,Quaternion.LookRotation(aim-grip));
        recoil=Mathf.MoveTowards(recoil,0,Time.deltaTime*10);partnerGun.position-=partnerGun.forward*(recoil*.045f);
        Arm(anim,HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,HumanBodyBones.RightHand,grip,poseWeight);
        Arm(anim,HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,HumanBodyBones.LeftHand,grip+partnerGun.forward*.22f,poseWeight);
    }
    static void Arm(Animator a,HumanBodyBones upper,HumanBodyBones lower,HumanBodyBones hand,Vector3 point,float weight){
        var u=a.GetBoneTransform(upper);var l=a.GetBoneTransform(lower);var h=a.GetBoneTransform(hand);if(!u||!l||!h)return;
        point=Vector3.Lerp(h.position,point,weight);
        var origin=u.position;float aLength=Vector3.Distance(origin,l.position),bLength=Vector3.Distance(l.position,h.position);
        var direction=(point-origin).normalized;float distance=Mathf.Clamp(Vector3.Distance(origin,point),.02f,aLength+bLength-.005f);
        float along=(aLength*aLength-bLength*bLength+distance*distance)/(2*distance);
        var bend=Vector3.ProjectOnPlane(-a.transform.up+a.transform.right*(upper==HumanBodyBones.RightUpperArm?.35f:-.35f),direction).normalized;
        var elbow=origin+direction*along+bend*Mathf.Sqrt(Mathf.Max(0,aLength*aLength-along*along));
        u.rotation=Quaternion.FromToRotation(l.position-origin,elbow-origin)*u.rotation;
        l.rotation=Quaternion.FromToRotation(h.position-l.position,origin+direction*distance-l.position)*l.rotation;
    }
}
}
