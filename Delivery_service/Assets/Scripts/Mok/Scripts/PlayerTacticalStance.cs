using UnityEngine;
using UnityEngine.InputSystem;
namespace DeliveryService.Mok {
// 시점 회전과 별도의 축으로 엄폐 자세를 적용해 반동·래그돌과 충돌하지 않게 한다.
[DefaultExecutionOrder(-100)]
public sealed class PlayerTacticalStance : MonoBehaviour {
    CharacterController body; Camera view; Transform pivot;
    ProloguePlayer prologue; PlayerCombat combat; PlayerWeapon weapon;
    float standingHeight, baseFov, crouchAmount, lean;
    Vector3 standingCenter, eyeRest;
    public bool IsCrouching { get; private set; }
    public float AimAmount { get; private set; }
    public float MoveMultiplier => IsCrouching ? .5f : AimAmount > .1f ? .7f : 1f;
    void Start() {
        body=GetComponent<CharacterController>();prologue=GetComponent<ProloguePlayer>();combat=GetComponent<PlayerCombat>();weapon=GetComponent<PlayerWeapon>();
        view=prologue?prologue.view:combat?combat.View:null;
        if(!body||!view){enabled=false;return;}
        standingHeight=body.height;standingCenter=body.center;baseFov=view.fieldOfView;
        pivot=new GameObject("Tactical stance pivot").transform;
        pivot.SetParent(view.transform.parent,false);eyeRest=view.transform.localPosition;pivot.localPosition=eyeRest;
        view.transform.SetParent(pivot,false);view.transform.localPosition=Vector3.zero;
    }
    bool Ready => body&&body.enabled&&view&&view.enabled&&Time.timeScale>0&&Cursor.lockState==CursorLockMode.Locked&&
        (prologue ? prologue.director&&prologue.director.CanPlay&&prologue.director.AcceptInput&&!prologue.KnockedDown : combat&&combat.CanAct);
    static bool Held(Key key)=>Keyboard.current!=null&&Keyboard.current[key].isPressed;
    bool Own(Collider c)=>c.transform.IsChildOf(transform);
    bool CanStand() {
        float radius=body.radius*.95f;
        Vector3 center=transform.TransformPoint(standingCenter);
        float half=Mathf.Max(0,standingHeight*.5f-radius);
        // 바닥은 제외하고 서 있을 때 필요한 윗부분만 검사한다.
        foreach(var c in Physics.OverlapCapsule(center,center+transform.up*half,radius,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
            if(!Own(c))return false;
        return true;
    }
    void Update() {
        if(!pivot)return;
        bool ready=Ready;
        bool requested=ready&&(Held(Key.LeftCtrl)||Held(Key.RightCtrl));
        IsCrouching=requested||(IsCrouching&&body.enabled&&!CanStand());
        float t=1-Mathf.Exp(-16*Time.unscaledDeltaTime);
        crouchAmount=Mathf.Lerp(crouchAmount,IsCrouching?1:0,t);
        float drop=Mathf.Min(.75f,standingHeight-Mathf.Max(body.radius*2+.05f,standingHeight*.58f));
        body.height=standingHeight-drop*crouchAmount;body.center=standingCenter-Vector3.up*(drop*crouchAmount*.5f);
        float requestedLean=ready?(Held(Key.E)?1:0)-(Held(Key.Q)?1:0):0;
        lean=Mathf.Lerp(lean,requestedLean,t);
        Vector3 centerLocal=eyeRest-Vector3.up*(drop*crouchAmount);
        Vector3 origin=pivot.parent.TransformPoint(centerLocal);
        Vector3 shift=pivot.parent.TransformVector(Vector3.right*(lean*.38f));
        float distance=shift.magnitude;
        if(distance>.001f){
            foreach(var hit in Physics.SphereCastAll(origin,.14f,shift.normalized,distance,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
                if(!Own(hit.collider))distance=Mathf.Min(distance,Mathf.Max(0,hit.distance-.025f));
            // 시작점이 벽과 겹치면 더 기울이지 않는다.
            foreach(var c in Physics.OverlapSphere(origin,.12f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))if(!Own(c))distance=0;
        }
        float safeLean=shift.magnitude>.001f?lean*distance/shift.magnitude:0;
        pivot.localPosition=centerLocal+Vector3.right*(safeLean*.38f);
        pivot.localRotation=Quaternion.Euler(0,0,-safeLean*12);
        bool gun=prologue?!prologue.Reloading:weapon&&weapon.HasInHand&&weapon.Equipped==WeaponKind.Pistol;
        bool aim=ready&&gun&&Mouse.current!=null&&Mouse.current.rightButton.isPressed;
        AimAmount=Mathf.Lerp(AimAmount,aim?1:0,t);
        view.fieldOfView=Mathf.Lerp(baseFov,baseFov*.72f,AimAmount);
        if(!ready){lean=0;pivot.localRotation=Quaternion.identity;pivot.localPosition=centerLocal;AimAmount=0;view.fieldOfView=baseFov;}
    }
    void OnDisable(){if(body&&standingHeight>0){body.height=standingHeight;body.center=standingCenter;}if(view&&baseFov>0)view.fieldOfView=baseFov;if(pivot){pivot.localPosition=eyeRest;pivot.localRotation=Quaternion.identity;}IsCrouching=false;AimAmount=0;}
}
}
