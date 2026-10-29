using System.Collections;
using UnityEngine;
using UnityEngine.AI;
namespace DeliveryService.Mok {
// 본편 시민의 관절 구성을 재사용하며 병원·수배 처리에는 연결하지 않는다.
public sealed class PrologueRagdoll : MonoBehaviour {
    public Animator animator;
    public Rigidbody[] bodies;
    public Collider[] boneColliders;
    public Transform hips;
    public bool IsDown { get; private set; }
    public bool Permanent { get; private set; }
    Transform[] bones;
    Vector3[] positions;
    Quaternion[] rotations;
    Collider mainCollider;
    NavMeshAgent agent;
    Vector3 origin;
    Quaternion heading;
    Coroutine recovery;
    void Awake() {
        mainCollider=GetComponent<Collider>();agent=GetComponent<NavMeshAgent>();
        bones=animator.GetComponentsInChildren<Transform>(true);
        positions=new Vector3[bones.Length];rotations=new Quaternion[bones.Length];
        foreach(var c in boneColliders) {
            c.enabled=false;
            if(mainCollider)Physics.IgnoreCollision(c,mainCollider);
            foreach(var other in boneColliders)if(c!=other)Physics.IgnoreCollision(c,other);
        }
        foreach(var rb in bodies)rb.isKinematic=true;
    }
    public void Knock(Vector3 velocity,bool permanent=false) {
        Permanent|=permanent;
        if(IsDown){if(Permanent&&recovery!=null)StopCoroutine(recovery);return;}
        if(SuppliedAudio.Current&&SuppliedAudio.Current.bodyFall){var sound=GetComponent<AudioSource>();if(!sound)sound=gameObject.AddComponent<AudioSource>();sound.spatialBlend=1;sound.PlayOneShot(SuppliedAudio.Current.bodyFall,.35f*SuppliedAudio.Volume);}
        IsDown=true;origin=transform.position;heading=transform.rotation;
        for(int i=0;i<bones.Length;i++){positions[i]=bones[i].localPosition;rotations[i]=bones[i].localRotation;}
        if(agent)agent.enabled=false;if(mainCollider)mainCollider.enabled=false;
        animator.enabled=false;
        foreach(var j in animator.GetComponentsInChildren<CharacterJoint>())if(j.connectedBody){
            j.autoConfigureConnectedAnchor=false;
            j.connectedAnchor=j.connectedBody.transform.InverseTransformPoint(j.transform.TransformPoint(j.anchor));
        }
        foreach(var c in boneColliders)c.enabled=true;
        Physics.SyncTransforms();
        foreach(var rb in bodies){rb.isKinematic=false;rb.useGravity=true;rb.linearVelocity=velocity;rb.angularVelocity=Vector3.zero;}
        if(!Permanent)recovery=StartCoroutine(RecoverAfter());
    }
    IEnumerator RecoverAfter(){yield return new WaitForSeconds(2);Recover();}
    public void Recover() {
        if(!IsDown||Permanent)return;
        Vector3 landing=origin;
        if(NavMesh.SamplePosition(hips.position,out var hit,2,NavMesh.AllAreas))landing=hit.position+Vector3.up*.04f;
        foreach(var rb in bodies){rb.linearVelocity=Vector3.zero;rb.angularVelocity=Vector3.zero;rb.isKinematic=true;}
        foreach(var c in boneColliders)c.enabled=false;
        transform.SetPositionAndRotation(landing,heading);
        for(int i=0;i<bones.Length;i++){bones[i].localPosition=positions[i];bones[i].localRotation=rotations[i];}
        animator.enabled=true;animator.Rebind();animator.Update(0);
        if(mainCollider)mainCollider.enabled=true;
        if(agent){agent.enabled=true;agent.Warp(landing);}
        IsDown=false;
    }
}
}
