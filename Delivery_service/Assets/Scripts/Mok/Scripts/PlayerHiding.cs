using UnityEngine;
namespace DeliveryService.Mok {
public sealed class PlayerHiding : MonoBehaviour {
    public bool IsHidden=>bin!=null;
    HidingDumpster bin;PlayerCombat combat;PlayerMove move;PlayerInteraction interact;CharacterController cc;Camera cam;CameraMove look;PhoneController phone;DayNightCycle clock;
    bool moveEnabled,interactEnabled,ccEnabled,lookEnabled,phoneEnabled;Vector3 savedPosition,camPosition;Quaternion savedRotation,camRotation;int enteredDay;Font font;
    void Start(){combat=GetComponent<PlayerCombat>();move=GetComponent<PlayerMove>();interact=GetComponent<PlayerInteraction>();cc=GetComponent<CharacterController>();cam=interact.InteractionCamera;look=cam.GetComponent<CameraMove>();phone=FindFirstObjectByType<PhoneController>();clock=FindFirstObjectByType<DayNightCycle>();font=MokUIFont.Font;}
    public HidingDumpster Focus(){if(cam==null)return null;return Physics.Raycast(cam.transform.position,cam.transform.forward,out var hit,3f,~0,QueryTriggerInteraction.Ignore)?hit.collider.GetComponentInParent<HidingDumpster>():null;}
    void Update(){if(IsHidden&&clock!=null&&clock.currentDay!=enteredDay){Leave(false);return;}if(Time.timeScale<=0||Cursor.lockState!=CursorLockMode.Locked)return;if(DeliveryService.Mok.PrologueKeyboard.Pressed(UnityEngine.InputSystem.Key.F)){if(IsHidden)Leave();else if(combat!=null&&combat.CanAct){var target=Focus();if(target!=null)Enter(target);}}}
    public bool Enter(HidingDumpster target){
        if(IsHidden||target==null||combat==null||!combat.CanAct||Vector3.Distance(transform.position,target.transform.position)>3.5f)return false;
        bin=target;savedPosition=transform.position;savedRotation=transform.rotation;camPosition=cam.transform.localPosition;camRotation=cam.transform.localRotation;enteredDay=clock!=null?clock.currentDay:0;
        moveEnabled=move.enabled;interactEnabled=interact.enabled;ccEnabled=cc.enabled;lookEnabled=look!=null&&look.enabled;phoneEnabled=phone!=null&&phone.enabled;
        move.enabled=false;interact.enabled=false;cc.enabled=false;if(look!=null)look.enabled=false;if(phone!=null){phone.ClosePhone();phone.enabled=false;}
        transform.SetPositionAndRotation(bin.transform.position,bin.transform.rotation);cam.transform.localPosition=new Vector3(0,1.16f,0);cam.transform.localRotation=Quaternion.identity;bin.Open();return true;
    }
    public void Leave(bool relocate=true){if(!IsHidden)return;var old=bin;bin=null;old.Open();
        if(relocate)transform.SetPositionAndRotation(savedPosition,savedRotation);
        cam.transform.localPosition=camPosition;cam.transform.localRotation=camRotation;if(FindFirstObjectByType<BankruptcyGameOver>()!=null)return;move.enabled=moveEnabled;interact.enabled=interactEnabled;cc.enabled=ccEnabled;if(look!=null)look.enabled=lookEnabled;if(phone!=null)phone.enabled=phoneEnabled;
    }
    void OnDisable(){if(IsHidden)Leave();}
    void OnDestroy(){}
    void OnGUI(){if(Time.timeScale<=0||cam==null||Cursor.lockState!=CursorLockMode.Locked)return;string text=IsHidden?"숨어 있는 중 · F를 눌러 나오기":combat!=null&&combat.CanAct&&Focus()!=null?"F를 눌러 쓰레기통에 숨기":"";
        var style=new GUIStyle(GUI.skin.label){font=font,fontSize=Mathf.Max(16,Screen.height/45),alignment=TextAnchor.MiddleCenter};GUI.Label(new Rect(0,Screen.height-140,Screen.width,40),text,style);}
}
}
