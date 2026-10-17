using UnityEngine;
using UnityEngine.AI;
namespace DeliveryService.Mok {
public sealed class RobberPresentation : MonoBehaviour {
    RobberAI robber;NPCControl npc;NavMeshAgent agent;Animator animator;
    Transform leftUpper,leftLower,leftHand,rightUpper,rightLower,rightHand,package;
    void Start(){
        robber=GetComponent<RobberAI>();npc=GetComponent<NPCControl>();agent=GetComponent<NavMeshAgent>();animator=GetComponent<Animator>();
        if(animator==null||!animator.isHuman)return;
        leftUpper=animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);leftLower=animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);leftHand=animator.GetBoneTransform(HumanBodyBones.LeftHand);
        rightUpper=animator.GetBoneTransform(HumanBodyBones.RightUpperArm);rightLower=animator.GetBoneTransform(HumanBodyBones.RightLowerArm);rightHand=animator.GetBoneTransform(HumanBodyBones.RightHand);
        package=transform.Find("Stolen pizza visual");
    }
    void Update(){
        if(animator==null)return;
        animator.SetFloat("MoveSpeed",agent.enabled&&agent.isOnNavMesh&&!agent.isStopped?agent.velocity.magnitude:0);
        animator.SetFloat("RunRate",Mathf.Clamp(agent.speed/4.5f,.8f,1.3f));
        if(package!=null)package.gameObject.SetActive(robber.StolenCount>0);
    }
    void LateUpdate(){
        if(animator==null||!animator.enabled||npc.IsKnockedDown||npc.IsDead)return;
        if(robber.StolenCount>0){
            Aim(leftUpper,leftLower,leftHand,transform.TransformPoint(new Vector3(-.24f,1.12f,.39f)),-1);
            Aim(rightUpper,rightLower,rightHand,transform.TransformPoint(new Vector3(.24f,1.12f,.39f)),1);
        }else if(robber.State==RobberAI.Phase.Windup){
            float reach=Mathf.SmoothStep(.25f,.75f,Mathf.Clamp01(robber.StateAge));
            Aim(rightUpper,rightLower,rightHand,transform.TransformPoint(new Vector3(.15f,1.15f,reach)),1);
        }
    }
    // 기존 뼈대의 팔 두 마디를 목표점에 맞춘다.
    void Aim(Transform a,Transform b,Transform c,Vector3 goal,float side){
        if(a==null||b==null||c==null)return;
        float l1=Vector3.Distance(a.position,b.position),l2=Vector3.Distance(b.position,c.position);
        Vector3 d=goal-a.position;float distance=Mathf.Clamp(d.magnitude,.03f,l1+l2-.005f);Vector3 axis=d.normalized;
        float x=(l1*l1-l2*l2+distance*distance)/(2*distance),h=Mathf.Sqrt(Mathf.Max(0,l1*l1-x*x));
        Vector3 bend=Vector3.ProjectOnPlane(transform.right*side-Vector3.up*.7f,axis).normalized;
        Vector3 elbow=a.position+axis*x+bend*h;
        a.rotation=Quaternion.FromToRotation(b.position-a.position,elbow-a.position)*a.rotation;
        b.rotation=Quaternion.FromToRotation(c.position-b.position,goal-b.position)*b.rotation;
    }
}}
