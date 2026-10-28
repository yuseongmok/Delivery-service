using UnityEngine;
using UnityEngine.Rendering;
namespace DeliveryService.Mok {
// 기존 플레이어 물리에는 관여하지 않고 사람형 모델과 카메라별 표시만 담당한다.
public sealed class PlayerAvatar : MonoBehaviour {
    public Animator animator;
    public PlayerRagdoll ragdoll;
    public Renderer[] upperBody;
    public PlayerCombat combat;
    // 이전 전투 API 이름은 유지하되 실제 동작은 좌우 주먹이다.
    public const float KickDuration=.52f,HitTime=.18f;
    public Transform leftFist,rightFist;bool leftPunch;
    public bool SilentStrike{get;private set;}
    public void PoseSilentStrike(float seconds){PoseKick(seconds);SilentStrike=true;leftPunch=false;}
    float kickTime;bool kicking;Vector3 previous;bool wasPhysical;
    public bool IsPhysical {get{if(ragdoll!=null&&ragdoll.IsActive)return true;var rb=GetComponentInParent<Rigidbody>();return rb!=null&&!rb.isKinematic;}}
    void OnEnable(){RenderPipelineManager.beginCameraRendering+=BeforeCamera;RenderPipelineManager.endCameraRendering+=AfterCamera;previous=transform.position;}
    public void PoseKick(float seconds){if(animator==null||!animator.isHuman)return;if(!kicking)leftPunch=!leftPunch;kickTime=seconds;kicking=true;}
    public void StopKick(){bool wasKicking=kicking;kicking=false;SilentStrike=false;if(leftFist)leftFist.gameObject.SetActive(false);if(rightFist)rightFist.gameObject.SetActive(false);if(wasKicking&&animator!=null&&animator.isActiveAndEnabled&&!IsPhysical)animator.CrossFadeInFixedTime("Idle",.1f,0);}
    void LateUpdate(){
        if(animator==null)return;if(ragdoll!=null&&ragdoll.IsActive){kicking=false;return;}
        bool physical=IsPhysical;
        if(physical){kicking=false;animator.Play("Tumble",0,0);animator.Update(0);}
        else if(kicking){animator.Play("Idle",0,0);animator.Update(0);ApplyPunch();}
        else{if(wasPhysical)animator.Play("Idle",0,0);animator.SetFloat("Speed",Vector3.ProjectOnPlane(transform.position-previous,Vector3.up).magnitude/Mathf.Max(Time.deltaTime,.001f));}
        previous=transform.position;wasPhysical=physical;
    }
    void ApplyPunch(){
        float t=kickTime;
        float thrust=t<.07f?0:t<HitTime?Mathf.SmoothStep(0,1,(t-.07f)/(HitTime-.07f)):1-Mathf.SmoothStep(0,1,(t-HitTime)/(KickDuration-HitTime));
        var eye=combat.View.transform;
        for(int side=0;side<2;side++){
            bool left=side==0;float sign=left?-1:1;float reach=left==leftPunch?thrust:0;
            var fist=left?leftFist:rightFist;
            Vector3 local=new Vector3(sign*Mathf.Lerp(.19f,.045f,reach),Mathf.Lerp(-.15f,-.045f,reach),Mathf.Lerp(.38f,.54f,reach));
            float lift=Mathf.Min(Mathf.SmoothStep(0,1,t/.065f),1-Mathf.SmoothStep(0,1,(t-(KickDuration-.10f))/.10f));local.y-=(1-lift)*.4f;
            if(SilentStrike){float wind=Mathf.SmoothStep(0,1,t/.18f),hit=Mathf.SmoothStep(0,1,(t-.18f)/.10f),recover=Mathf.SmoothStep(0,1,(t-.34f)/.38f);local=left?new Vector3(-.15f,-.1f,.45f):Vector3.Lerp(Vector3.Lerp(new Vector3(.24f,-.2f,.25f),new Vector3(.2f,.16f,.32f),wind),new Vector3(.025f,-.03f,.64f),hit);local=Vector3.Lerp(local,new Vector3(sign*.2f,-.55f,.2f),recover);}
            if(fist){
                fist.gameObject.SetActive(true);fist.localPosition=local;fist.localRotation=Quaternion.Euler(Mathf.Lerp(-20,-4,reach),-sign*reach*12,sign*Mathf.Lerp(-30,-8,reach));
                if(SilentStrike&&!left)fist.localRotation=Quaternion.Euler(45,0,-85);
                // 소매를 화면 아래 팔 위치까지 이어 주먹만 공중에 떠 보이지 않게 한다.
                var sleeve=fist.Find("Jacket cuff");if(sleeve){var wrist=new Vector3(0,-.005f,-.07f);var shoulder=fist.InverseTransformPoint(eye.TransformPoint(new Vector3(sign*.27f,-.42f,.06f)));var delta=wrist-shoulder;sleeve.localPosition=(wrist+shoulder)*.5f;sleeve.localRotation=Quaternion.FromToRotation(Vector3.up,delta.normalized);sleeve.localScale=new Vector3(.085f,delta.magnitude+.025f,.085f);}
            }
            var arm=animator.GetBoneTransform(left?HumanBodyBones.LeftUpperArm:HumanBodyBones.RightUpperArm);
            var elbow=animator.GetBoneTransform(left?HumanBodyBones.LeftLowerArm:HumanBodyBones.RightLowerArm);
            var hand=animator.GetBoneTransform(left?HumanBodyBones.LeftHand:HumanBodyBones.RightHand);
            Vector3 goal=eye.TransformPoint(local),origin=arm.position;
            float a=Vector3.Distance(origin,elbow.position),b=Vector3.Distance(elbow.position,hand.position);
            Vector3 axis=(goal-origin).normalized;float d=Mathf.Clamp(Vector3.Distance(origin,goal),.04f,a+b-.005f);goal=origin+axis*d;
            float along=(a*a-b*b+d*d)/(2*d),height=Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
            Vector3 bend=Vector3.ProjectOnPlane(transform.right*sign-transform.up,axis).normalized;
            Vector3 joint=origin+axis*along+bend*height;
            arm.rotation=Quaternion.FromToRotation(elbow.position-origin,joint-origin)*arm.rotation;
            elbow.rotation=Quaternion.FromToRotation(hand.position-elbow.position,goal-elbow.position)*elbow.rotation;
        }
    }
    void BeforeCamera(ScriptableRenderContext context,Camera camera){
        bool firstPerson=combat!=null&&camera==combat.View;
        foreach(var r in upperBody)if(r!=null)r.shadowCastingMode=firstPerson?ShadowCastingMode.ShadowsOnly:ShadowCastingMode.On;
    }
    void AfterCamera(ScriptableRenderContext context,Camera camera){foreach(var r in upperBody)if(r!=null)r.shadowCastingMode=ShadowCastingMode.On;}
    void OnDisable(){StopKick();RenderPipelineManager.beginCameraRendering-=BeforeCamera;RenderPipelineManager.endCameraRendering-=AfterCamera;foreach(var r in upperBody)if(r!=null)r.shadowCastingMode=ShadowCastingMode.On;}
}
}
