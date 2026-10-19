using UnityEngine;
using UnityEngine.Rendering;
namespace DeliveryService.Mok {
// 기존 플레이어 물리에는 관여하지 않고 사람형 모델과 카메라별 표시만 담당한다.
public sealed class PlayerAvatar : MonoBehaviour {
    public Animator animator;
    public PlayerRagdoll ragdoll;
    public Renderer[] upperBody;
    public PlayerCombat combat;
    public const float KickDuration=.65f,HitTime=.22f;
    Vector3 restFoot;Quaternion restFootRotation;float kickTime;bool kicking;Vector3 previous;bool wasPhysical;
    public bool IsPhysical {get{if(ragdoll!=null&&ragdoll.IsActive)return true;var rb=GetComponentInParent<Rigidbody>();return rb!=null&&!rb.isKinematic;}}
    void OnEnable(){RenderPipelineManager.beginCameraRendering+=BeforeCamera;RenderPipelineManager.endCameraRendering+=AfterCamera;previous=transform.position;}
    public void PoseKick(float seconds){if(animator==null||!animator.isHuman)return;if(!kicking){var foot=animator.GetBoneTransform(HumanBodyBones.RightFoot);restFoot=transform.InverseTransformPoint(foot.position);restFootRotation=Quaternion.Inverse(transform.rotation)*foot.rotation;}kickTime=seconds;kicking=true;}
    public void StopKick(){bool wasKicking=kicking;kicking=false;if(wasKicking&&animator!=null&&animator.isActiveAndEnabled&&!IsPhysical)animator.CrossFadeInFixedTime("Idle",.1f,0);}
    void LateUpdate(){
        if(animator==null)return;if(ragdoll!=null&&ragdoll.IsActive){kicking=false;return;}
        bool physical=IsPhysical;
        if(physical){kicking=false;animator.Play("Tumble",0,0);animator.Update(0);}
        else if(kicking){animator.Play("Kick",0,Mathf.Clamp01(kickTime/KickDuration));animator.Update(0);ApplyKickLeg();}
        else{if(wasPhysical)animator.Play("Idle",0,0);animator.SetFloat("Speed",Vector3.ProjectOnPlane(transform.position-previous,Vector3.up).magnitude/Mathf.Max(Time.deltaTime,.001f));}
        previous=transform.position;wasPhysical=physical;
    }
    // 두 관절 IK로 무릎을 접었다 펴며 실제 발을 타격 시점에 앞으로 보낸다.
    void ApplyKickLeg(){
        var hip=animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
        var knee=animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
        var foot=animator.GetBoneTransform(HumanBodyBones.RightFoot);
        // 체형 기준으로 무릎 들기 → 뻗기 → 다시 접기 → 착지한다.
        float length=Vector3.Distance(hip.position,knee.position)+Vector3.Distance(knee.position,foot.position);
        float scale=Mathf.Max(.001f,transform.lossyScale.y);float leg=length/scale;
        Vector3 hipLocal=transform.InverseTransformPoint(hip.position);
        // 정면 시야의 아래쪽에 신발이 들어오도록 무릎을 높이고 상단으로 뻗는다.
        float t=kickTime;Vector3 tuck=new Vector3(restFoot.x,hipLocal.y-leg*.05f,hipLocal.z+leg*.40f);
        Vector3 extended=new Vector3(restFoot.x,hipLocal.y+leg*.52f,hipLocal.z+leg*.84f);
        Vector3 local=t<.12f?Vector3.Lerp(restFoot,tuck,Mathf.SmoothStep(0,1,t/.12f)):
            t<HitTime?Vector3.Lerp(tuck,extended,Mathf.SmoothStep(0,1,(t-.12f)/(HitTime-.12f))):
            t<.34f?extended:t<.49f?Vector3.Lerp(extended,tuck,Mathf.SmoothStep(0,1,(t-.34f)/.15f)):
            Vector3.Lerp(tuck,restFoot,Mathf.SmoothStep(0,1,(t-.49f)/(KickDuration-.49f)));
        Vector3 target=transform.TransformPoint(local),origin=hip.position;
        float a=Vector3.Distance(origin,knee.position),b=Vector3.Distance(knee.position,foot.position);
        Vector3 direction=(target-origin).normalized;float d=Mathf.Clamp(Vector3.Distance(target,origin),.02f,a+b-.001f);
        target=origin+direction*d;
        float along=(a*a-b*b+d*d)/(2*d),height=Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
        Vector3 bend=Vector3.ProjectOnPlane(transform.forward+transform.up,direction);
        if(bend.sqrMagnitude<.001f)bend=Vector3.ProjectOnPlane(transform.up,direction);
        bend.Normalize();
        Vector3 joint=origin+direction*along+bend*height;
        hip.rotation=Quaternion.FromToRotation(knee.position-origin,joint-origin)*hip.rotation;
        knee.rotation=Quaternion.FromToRotation(foot.position-knee.position,target-knee.position)*knee.rotation;
        foot.rotation=transform.rotation*restFootRotation;
    }
    void BeforeCamera(ScriptableRenderContext context,Camera camera){
        bool firstPerson=combat!=null&&camera==combat.View;
        foreach(var r in upperBody)if(r!=null)r.shadowCastingMode=firstPerson?ShadowCastingMode.ShadowsOnly:ShadowCastingMode.On;
    }
    void AfterCamera(ScriptableRenderContext context,Camera camera){foreach(var r in upperBody)if(r!=null)r.shadowCastingMode=ShadowCastingMode.On;}
    void OnDisable(){kicking=false;RenderPipelineManager.beginCameraRendering-=BeforeCamera;RenderPipelineManager.endCameraRendering-=AfterCamera;foreach(var r in upperBody)if(r!=null)r.shadowCastingMode=ShadowCastingMode.On;}
}
}
