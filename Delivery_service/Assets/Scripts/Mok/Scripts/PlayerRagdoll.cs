using System.Collections;
using UnityEngine;
namespace DeliveryService.Mok {
// 시민과 같은 관절 물리를 사용하고, 충돌이 끝나면 조작 가능한 몸으로 복구한다.
public sealed class PlayerRagdoll : MonoBehaviour {
    public Rigidbody[] bodies;
    public Collider[] colliders;
    public Transform hips;
    public PlayerAvatar avatar;
    public bool IsActive {get;private set;}
    public bool IsReady => bodies!=null&&bodies.Length>5&&hips!=null;
    Transform root;CharacterController controller;PlayerMove move;CameraMove look;Camera view,actionCamera;
    bool moveEnabled,lookEnabled,controllerEnabled,viewEnabled,animatorEnabled;
    Transform[] bones;Vector3[] positions;Quaternion[] rotations;Quaternion rootRotation;
    Vector3 cameraOffset;
    void Awake(){
        root=GetComponentInParent<PlayerCombat>().transform;
        controller=root.GetComponent<CharacterController>();move=root.GetComponent<PlayerMove>();
        view=root.GetComponent<PlayerInteraction>().InteractionCamera;look=view.GetComponent<CameraMove>();
        bones=GetComponentsInChildren<Transform>();positions=new Vector3[bones.Length];rotations=new Quaternion[bones.Length];
        for(int i=0;i<colliders.Length;i++)for(int j=i+1;j<colliders.Length;j++)Physics.IgnoreCollision(colliders[i],colliders[j]);
    }
    public void Launch(Vector3 velocity,float duration=2.5f){if(!IsReady||IsActive||!isActiveAndEnabled)return;StartCoroutine(Fly(velocity,duration));}
    IEnumerator Fly(Vector3 velocity,float duration){
        IsActive=true;rootRotation=root.rotation;
        moveEnabled=move!=null&&move.enabled;lookEnabled=look!=null&&look.enabled;controllerEnabled=controller!=null&&controller.enabled;
        viewEnabled=view!=null&&view.enabled;animatorEnabled=avatar.animator.enabled;
        avatar.StopKick();
        for(int i=0;i<bones.Length;i++){positions[i]=bones[i].localPosition;rotations[i]=bones[i].localRotation;}
        if(move!=null)move.enabled=false;if(look!=null)look.enabled=false;if(controller!=null)controller.enabled=false;
        avatar.animator.enabled=false;
        // 현재 애니메이션 자세에서 연결점을 맞춰 전환 순간 관절이 폭발하지 않게 한다.
        foreach(var joint in GetComponentsInChildren<CharacterJoint>())if(joint.connectedBody!=null){joint.autoConfigureConnectedAnchor=false;joint.connectedAnchor=joint.connectedBody.transform.InverseTransformPoint(joint.transform.TransformPoint(joint.anchor));}
        Physics.SyncTransforms();
        foreach(var col in colliders)col.enabled=true;
        foreach(var body in bodies){body.isKinematic=false;body.useGravity=true;body.linearVelocity=velocity;body.angularVelocity=Vector3.zero;}
        hips.GetComponent<Rigidbody>().AddTorque(root.right*3f,ForceMode.VelocityChange);
        Vector3 horizontal=Vector3.ProjectOnPlane(velocity,Vector3.up).normalized;
        if(horizontal.sqrMagnitude<.1f)horizontal=root.forward;
        cameraOffset=-horizontal*4f+Vector3.up*2f;
        actionCamera=new GameObject("Player ragdoll camera").AddComponent<Camera>();
        actionCamera.fieldOfView=60;actionCamera.nearClipPlane=.05f;actionCamera.depth=99;
        if(view!=null){actionCamera.cullingMask=view.cullingMask;view.enabled=false;}
        UpdateCamera(true);
        try{yield return new WaitForSeconds(duration);}
        finally{Restore();}
    }
    void LateUpdate(){if(IsActive)UpdateCamera(false);}
    void UpdateCamera(bool instant){
        if(actionCamera==null)return;
        Vector3 target=hips.position,desired=target+cameraOffset;
        // 벽 뒤로 카메라가 넘어가지 않도록 몸과 카메라 사이를 검사한다.
        foreach(var hit in Physics.RaycastAll(target,cameraOffset.normalized,cameraOffset.magnitude,~0,QueryTriggerInteraction.Ignore)){
            if(hit.collider.transform.IsChildOf(root)||hit.distance<.15f)continue;
            if(Vector3.Distance(target,desired)>hit.distance)desired=target+cameraOffset.normalized*Mathf.Max(.2f,hit.distance-.15f);
        }
        actionCamera.transform.position=instant?desired:Vector3.Lerp(actionCamera.transform.position,desired,Time.deltaTime*10f);
        actionCamera.transform.LookAt(target);
    }
    void Restore(){
        if(!IsActive)return;
        Vector3 landing=hips.position;
        foreach(var body in bodies){body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;body.isKinematic=true;}
        foreach(var col in colliders)col.enabled=false;
        float closest=float.PositiveInfinity;
        foreach(var hit in Physics.RaycastAll(landing+Vector3.up,Vector3.down,8f,~0,QueryTriggerInteraction.Ignore)){
            if(hit.collider.transform.IsChildOf(root)||hit.normal.y<.55f||hit.distance>=closest)continue;
            closest=hit.distance;landing=hit.point;
        }
        // 발이 땅 밑에 들어가지 않도록 컨트롤러 중심 높이를 되돌린다.
        if(closest<float.PositiveInfinity)landing+=Vector3.up*(controller.height*.5f-controller.center.y+.06f);
        root.SetPositionAndRotation(landing,rootRotation);
        for(int i=0;i<bones.Length;i++){bones[i].localPosition=positions[i];bones[i].localRotation=rotations[i];}
        avatar.animator.enabled=animatorEnabled;
        if(animatorEnabled){avatar.animator.Rebind();avatar.animator.Play("Idle",0,0);avatar.animator.Update(0);}
        if(actionCamera!=null)Destroy(actionCamera.gameObject);
        if(view!=null)view.enabled=viewEnabled;
        if(controller!=null)controller.enabled=controllerEnabled;if(move!=null)move.enabled=moveEnabled;if(look!=null)look.enabled=lookEnabled;
        IsActive=false;
    }
    void OnDisable(){StopAllCoroutines();Restore();}
}
}
