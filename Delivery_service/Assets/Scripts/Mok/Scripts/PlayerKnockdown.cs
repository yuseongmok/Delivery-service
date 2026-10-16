using System.Collections;
using UnityEngine;
namespace DeliveryService.Mok {
// Shared by motorcycle bail-out and combat: the original capsule/body rolling motion.
public static class PlayerKnockdown {
    public static IEnumerator Roll(GameObject player,float speed,Vector3 direction,Transform destination=null){
        var cc=player.GetComponent<CharacterController>();var move=player.GetComponent<PlayerMove>();
        var look=player.GetComponentInChildren<CameraMove>();
        bool moveEnabled=move!=null&&move.enabled,ccEnabled=cc!=null&&cc.enabled,lookEnabled=look!=null&&look.enabled;
        var rotation=player.transform.rotation;
        var col=player.GetComponent<CapsuleCollider>();bool addedCol=col==null;bool colEnabled=col!=null&&col.enabled;
        var rb=player.GetComponent<Rigidbody>();bool addedRb=rb==null;
        bool kinematic=rb!=null&&rb.isKinematic,gravity=rb!=null&&rb.useGravity;var constraints=rb!=null?rb.constraints:RigidbodyConstraints.None;
        var detection=rb!=null?rb.collisionDetectionMode:CollisionDetectionMode.Discrete;
        PhysicsMaterial material=null;
        if(move!=null)move.enabled=false;if(look!=null)look.enabled=false;if(cc!=null)cc.enabled=false;
        if(addedCol){col=player.AddComponent<CapsuleCollider>();col.height=cc!=null?cc.height:2f;col.radius=cc!=null?cc.radius:.5f;col.center=cc!=null?cc.center:Vector3.zero;material=new PhysicsMaterial{dynamicFriction=.3f,bounciness=.1f};col.material=material;}
        col.enabled=true;if(addedRb)rb=player.AddComponent<Rigidbody>();
        rb.isKinematic=false;rb.useGravity=true;rb.constraints=RigidbodyConstraints.None;rb.collisionDetectionMode=CollisionDetectionMode.Continuous;
        rb.linearVelocity=direction.normalized*speed*.9f+Vector3.up*.6f;
        rb.AddTorque(player.transform.right*speed*1.5f+Random.insideUnitSphere*speed,ForceMode.VelocityChange);
        try{yield return new WaitForSeconds(3f);}
        finally{
            if(rb!=null){rb.linearVelocity=Vector3.zero;rb.angularVelocity=Vector3.zero;rb.isKinematic=true;if(addedRb)Object.Destroy(rb);else{rb.constraints=constraints;rb.useGravity=gravity;rb.collisionDetectionMode=detection;rb.isKinematic=kinematic;}}
            if(col!=null){col.enabled=addedCol?false:colEnabled;if(addedCol)Object.Destroy(col);}if(material!=null)Object.Destroy(material);
            if(player!=null){player.transform.rotation=Quaternion.Euler(0,rotation.eulerAngles.y,0);if(destination!=null)player.transform.SetPositionAndRotation(destination.position,Quaternion.Euler(0,destination.eulerAngles.y,0));}
            if(cc!=null)cc.enabled=ccEnabled;if(move!=null)move.enabled=moveEnabled;if(look!=null)look.enabled=lookEnabled;
        }
    }
}
}
