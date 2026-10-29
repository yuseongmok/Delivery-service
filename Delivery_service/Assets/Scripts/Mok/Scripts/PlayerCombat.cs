using System.Collections;
using System.Linq;
using UnityEngine;
namespace DeliveryService.Mok {
public sealed class PlayerCombat : MonoBehaviour {
    public int maximumHealth=100, kickDamage=30;
    public float kickReach=2.1f, kickCooldown=.9f;
    public Transform kickVisual;
    public PlayerAvatar avatar;
    public Transform hospital;
    public HospitalFade hospitalFade;
    public int HitsTaken {get;private set;}
    public int BulletHitsTaken {get;private set;}
    public bool CanReceiveBullet=>CanReceiveHit&&Time.time>=recoveryUntil;
    public bool ReceiveBullet(Vector3 direction){if(DeveloperCheats.Invincible||!CanReceiveBullet)return false;BulletHitsTaken++;if(BulletHitsTaken>=3)HitsTaken=2;ReceivePunch(34,direction);return true;}
    public int Health {get;private set;}
    public bool IsDown {get;private set;}
    public bool IsKicking {get;private set;}
    public bool CanAct => isActiveAndEnabled && RiddenBike==null && CanReceiveHit && !IsDown && (avatar==null||!avatar.IsPhysical) && Time.timeScale>0 && Cursor.lockState==CursorLockMode.Locked && (move==null||move.isControllable) && (phone==null||!phone.IsPhoneOpen());
    public bool WaterRescueActive {get;set;}
    public void ResetAfterWaterRescue(){HitsTaken=0;BulletHitsTaken=0;Health=maximumHealth;recoveryUntil=Time.time+2;}
    public bool CanReceiveHit => !(VenueFinale.Active&&VenueFinale.Current.Cinematic) && !WaterRescueActive && (isActiveAndEnabled||RiddenBike!=null) && (avatar==null||!avatar.IsPhysical) && !(GetComponent<PlayerHiding>()!=null&&GetComponent<PlayerHiding>().IsHidden) && !PoliceControl.IsArrestInProgress && !IsDown;
    Camera view;PlayerMove move;PhoneController phone;MotorcycleController[] bikes;CharacterController controller;
    float nextKick, recoveryUntil; Font font;
    public MotorcycleController RiddenBike => bikes==null?null:bikes.FirstOrDefault(b=>b&&b.isDriven);
    public Camera View => RiddenBike&&RiddenBike.bikeCamera?RiddenBike.bikeCamera.GetComponentInChildren<Camera>():view;
    void Awake(){gameObject.AddComponent<PlayerDazeFeedback>();Health=maximumHealth;view=GetComponent<PlayerInteraction>().InteractionCamera;if(avatar!=null&&view!=null)view.nearClipPlane=Mathf.Min(view.nearClipPlane,.05f);move=GetComponent<PlayerMove>();controller=GetComponent<CharacterController>();}
    void Start(){phone=FindFirstObjectByType<PhoneController>();bikes=FindObjectsByType<MotorcycleController>(FindObjectsSortMode.None);font=MokUIFont.Font;if(kickVisual!=null)kickVisual.gameObject.SetActive(false);}
    void Update(){if(CanAct&&Input.GetMouseButtonDown(0)&&(GetComponent<PlayerWeapon>()==null||!GetComponent<PlayerWeapon>().HasInHand))TryKick();}

    public CitizenCombat FocusCitizen(float distance=2.5f){
        if(view==null)return null;
        var hits=Physics.RaycastAll(view.transform.position,view.transform.forward,distance,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance);
        foreach(var h in hits){if(h.transform.IsChildOf(transform))continue;return h.collider.GetComponentInParent<CitizenCombat>();}return null;
    }
    public bool TryPickpocket(){
        if(!CanAct||IsKicking)return false;var npc=FocusCitizen();if(npc==null)return false;
        npc.Pickpocket(this);return true;
    }
    public bool TryKick(){if(!CanAct||IsKicking||Time.time<nextKick)return false;nextKick=Time.time+kickCooldown;var target=FocusCitizen(2);var guard=target?target.GetComponent<VenuePatrolGuard>():null;if(guard&&guard.CanSilentStrike(this))StartCoroutine(SilentTakedown(guard));else StartCoroutine(Kick());return true;}
    IEnumerator SilentTakedown(VenuePatrolGuard guard){
        IsKicking=true;bool struck=false;
        for(float t=0;t<.75f;t+=Time.deltaTime){if(Time.timeScale<=0){yield return null;continue;}if(!CanAct||!guard||!guard.mission.Active)break;if(avatar)avatar.PoseSilentStrike(t);
            if(!struck&&t>=.28f){struck=true;if(guard.CanSilentStrike(this)&&guard.Combat.ReceiveKick(this,view.transform.forward,kickDamage))KickImpactFeedback.PlaySilent(this);}
            yield return null;
        }
        if(avatar)avatar.StopKick();IsKicking=false;
    }
    IEnumerator Kick(){
        IsKicking=true;if(avatar==null&&kickVisual!=null)kickVisual.gameObject.SetActive(true);bool hit=false;
        for(float t=0;t<PlayerAvatar.KickDuration;t+=Time.deltaTime){
            if(!CanAct)break;if(avatar!=null)avatar.PoseKick(t);
            float extension=t<.22f?Mathf.SmoothStep(0,1,t/.22f):1-Mathf.SmoothStep(0,1,(t-.28f)/.37f);
            if(avatar==null&&kickVisual!=null){kickVisual.localPosition=new Vector3(.18f,Mathf.Lerp(-.48f,-.22f,extension),Mathf.Lerp(.12f,.38f,extension));kickVisual.localRotation=Quaternion.Euler(Mathf.Lerp(65,0,extension),0,0);}
            if(!hit&&t>=PlayerAvatar.HitTime){hit=true;if(CanAct)ResolveKick();}
            yield return null;
        }
        if(kickVisual!=null)kickVisual.gameObject.SetActive(false);if(avatar!=null)avatar.StopKick();IsKicking=false;
    }
    bool IsHeldObject(Transform target){var inv=GetComponent<ToppingInventory>();return target.IsChildOf(transform)||(inv!=null&&((inv.HoldPoint!=null&&target.IsChildOf(inv.HoldPoint))||(inv.HeldPizza!=null&&target.IsChildOf(inv.HeldPizza.transform))));}
    public bool ResolveKick(){
        // 가장 가까운 물체를 우선해 벽 너머로 주먹 공격을 하지 못하게 한다.
        var hits=Physics.SphereCastAll(view.transform.position,.22f,view.transform.forward,kickReach,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance);
        foreach(var h in hits){
            if(IsHeldObject(h.transform))continue;
            var police=h.collider.GetComponentInParent<PoliceCombat>();
            var robber=h.collider.GetComponentInParent<RobberAI>();
            var npc=h.collider.GetComponentInParent<CitizenCombat>();
            bool landed=police!=null?police.ReceiveHit(this,view.transform.forward,kickDamage):robber!=null?robber.ReceiveKick(view.transform.forward):npc!=null&&npc.ReceiveKick(this,view.transform.forward,kickDamage);
            if(landed)KickImpactFeedback.Play(this);
            return landed;
        }return false;
    }
    public void ReceivePunch(int damage,Vector3 direction,bool slumRobber=false){
        if(DeveloperCheats.Invincible||!CanReceiveHit||Time.time<recoveryUntil)return;
        var mounted=RiddenBike;if(mounted){mounted.ApplyImpactDeceleration(.1f);mounted.ExitBike();}
        HitsTaken++;Health=Mathf.Max(0,maximumHealth-HitsTaken*34);
        if(HitsTaken>=3&&slumRobber)SlumRobberCombat.ApplyDefeatLoss(this);
        StartCoroutine(RecoverPlayer(direction));
    }
    IEnumerator RecoverPlayer(Vector3 direction){
        IsDown=true;if(avatar!=null)avatar.StopKick();
        if(kickVisual!=null)kickVisual.gameObject.SetActive(false);
        bool respawn=HitsTaken>=3;
        if(respawn)FindFirstObjectByType<PoliceManager>()?.CancelReplacements();
        if(respawn)foreach(var citizen in FindObjectsByType<CitizenCombat>(FindObjectsSortMode.None))citizen.EndFight();
        Coroutine blackout=respawn&&hospitalFade!=null?StartCoroutine(hospitalFade.DarkenAfter(1.5f)):null;
        yield return PlayerKnockdown.Roll(gameObject,3.5f,direction,respawn?hospital:null);
        // 치료비가 부족해도 병원 도착 후 한 번만 청구한다.
        if(respawn&&hospital!=null&&MoneyManager.Instance!=null)MoneyManager.Instance.AccidentMoney(20000);
        if(respawn&&hospitalFade!=null){
            bool wasMoving=move!=null&&move.enabled;var look=GetComponentInChildren<CameraMove>();bool wasLooking=look!=null&&look.enabled;
            if(move!=null)move.enabled=false;if(look!=null)look.enabled=false;
            if(blackout!=null)yield return blackout;
            yield return hospitalFade.WakeUp();
            if(move!=null)move.enabled=wasMoving;if(look!=null)look.enabled=wasLooking;
        }
        if(respawn){HitsTaken=0;BulletHitsTaken=0;Health=maximumHealth;foreach(var officer in SceneActors.Police)Destroy(officer.gameObject);}
        IsDown=false;recoveryUntil=Time.time+1.25f;
    }
    void OnDisable(){if(avatar!=null)avatar.StopKick();StopAllCoroutines();IsKicking=false;if(kickVisual!=null)kickVisual.gameObject.SetActive(false);}
    void OnDestroy(){}
    void OnGUI(){
        if(Time.timeScale<=0)return;
        var style=new GUIStyle(GUI.skin.label){font=font,fontSize=Mathf.Max(16,Screen.height/45),alignment=TextAnchor.MiddleCenter};style.normal.textColor=Color.white;
        if(CanAct){var npc=FocusCitizen();if(npc!=null&&npc.CanBePickpocketed)
            GUI.Label(new Rect(Screen.width*.25f,Screen.height-95,Screen.width*.5f,40),"F를 눌러 소매치기",style);}
    }
}
}

