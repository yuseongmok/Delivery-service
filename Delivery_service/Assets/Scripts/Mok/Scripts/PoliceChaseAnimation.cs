using UnityEngine;
using UnityEngine.AI;
namespace DeliveryService.Mok {
[RequireComponent(typeof(Animator),typeof(NavMeshAgent))]
public sealed class PoliceChaseAnimation : MonoBehaviour {
    Animator animator;NavMeshAgent agent;
    public float MovementSpeed {get;private set;}
    void Awake(){animator=GetComponent<Animator>();agent=GetComponent<NavMeshAgent>();animator.applyRootMotion=false;}
    Transform badge,plate,tie,letters,hips,neck;
    void Start(){
        hips=animator.GetBoneTransform(HumanBodyBones.Hips);neck=animator.GetBoneTransform(HumanBodyBones.Neck);
        foreach(var t in GetComponentsInChildren<Transform>())switch(t.name){case "Chest badge":badge=t;break;case "Nameplate":plate=t;break;case "Tie":tie=t;break;case "POLICE lettering":letters=t;break;}
    }
    void LateUpdate(){
        if(hips==null||neck==null)return;
        Vector3 up=(neck.position-hips.position).normalized,right=Vector3.ProjectOnPlane(transform.right,up).normalized,forward=Vector3.Cross(right,up).normalized;
        Vector3 center=Vector3.Lerp(hips.position,neck.position,.8f);float scale=transform.lossyScale.x;var rotation=Quaternion.LookRotation(forward,up);
        void Place(Transform t,float x,float y,float z){if(t!=null)t.SetPositionAndRotation(center+(right*x+up*y+forward*z)*scale,rotation);}
        Place(badge,-.095f,0,.16f);Place(plate,.085f,0,.16f);Place(tie,0,-.01f,.15f);Place(letters,0,0,-.15f);
    }
    void Update(){MovementSpeed=agent.enabled&&agent.isOnNavMesh&&!agent.isStopped&&!PoliceControl.IsArrestInProgress?agent.velocity.magnitude:0;
        animator.SetFloat("MoveSpeed",MovementSpeed);animator.SetFloat("RunRate",Mathf.Clamp(MovementSpeed/4f,.7f,2f));}
}
}
