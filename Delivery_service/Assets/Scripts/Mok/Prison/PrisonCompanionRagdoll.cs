using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace DeliveryService.Mok
{
    // 민기만 사용한다. 쓰러지기 전 층의 도달 가능한 바닥으로 복귀한다.
    public sealed class PrisonCompanionRagdoll : MonoBehaviour
    {
        public PrisonCompanion companion;
        public Rigidbody[] bodies;
        public Collider[] colliders;
        public Transform hips;
        public bool IsDown { get; private set; }
        Transform[] bones;
        Vector3[] positions;
        Quaternion[] rotations;
        Vector3 origin;
        Quaternion heading;
        void Awake()
        {
            bones=companion.animator.GetComponentsInChildren<Transform>(true);
            positions=new Vector3[bones.Length];rotations=new Quaternion[bones.Length];
            foreach(var body in bodies)body.isKinematic=true;
            foreach(var c in colliders){c.enabled=false;foreach(var other in colliders)if(c!=other)Physics.IgnoreCollision(c,other);}
        }
        public void Knock(Vector3 direction)
        {
            if(IsDown)return;
            IsDown=true;origin=transform.position;heading=transform.rotation;
            for(int i=0;i<bones.Length;i++){positions[i]=bones[i].localPosition;rotations[i]=bones[i].localRotation;}
            companion.agent.enabled=false;companion.animator.enabled=false;
            foreach(var joint in GetComponentsInChildren<CharacterJoint>())if(joint.connectedBody){joint.autoConfigureConnectedAnchor=false;joint.connectedAnchor=joint.connectedBody.transform.InverseTransformPoint(joint.transform.TransformPoint(joint.anchor));}
            foreach(var c in colliders)c.enabled=true;
            Physics.SyncTransforms();
            Vector3 velocity=Vector3.ProjectOnPlane(direction,Vector3.up).normalized*3.2f+Vector3.up*1.5f;
            foreach(var body in bodies){body.isKinematic=false;body.useGravity=true;body.linearVelocity=velocity;body.angularVelocity=Vector3.zero;}
            if(SuppliedAudio.Current&&SuppliedAudio.Current.bodyFall)AudioSource.PlayClipAtPoint(SuppliedAudio.Current.bodyFall,origin,.35f*SuppliedAudio.Volume);
            StartCoroutine(GetUp());
        }
        IEnumerator GetUp(){yield return new WaitForSeconds(2.2f);Restore();}
        public void Restore()
        {
            if(!IsDown)return;
            StopAllCoroutines();
            Vector3 landing=origin;Vector3 query=hips.position;query.y=origin.y;
            var agent=companion.agent;
            var filter=new NavMeshQueryFilter{agentTypeID=agent.agentTypeID,areaMask=agent.areaMask};
            if(NavMesh.SamplePosition(query,out var hit,1f,filter)&&Mathf.Abs(hit.position.y-origin.y)<.6f){
                var path=new NavMeshPath();
                if(NavMesh.CalculatePath(origin,hit.position,filter,path)&&path.status==NavMeshPathStatus.PathComplete)landing=hit.position;
            }
            foreach(var body in bodies){body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;body.isKinematic=true;}
            foreach(var c in colliders)c.enabled=false;
            transform.SetPositionAndRotation(landing,heading);
            for(int i=0;i<bones.Length;i++){bones[i].localPosition=positions[i];bones[i].localRotation=rotations[i];}
            companion.animator.enabled=true;companion.animator.Rebind();companion.animator.Update(0);
            agent.enabled=true;agent.Warp(landing);IsDown=false;
        }
    }
}
