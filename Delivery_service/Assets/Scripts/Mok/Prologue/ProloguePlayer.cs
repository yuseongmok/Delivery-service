using System.Collections;
using UnityEngine;
namespace DeliveryService.Mok {
// 본편 전투와 분리하여 프롤로그 실패가 병원·벌금으로 이어지지 않게 한다.
[RequireComponent(typeof(CharacterController))]
public sealed class ProloguePlayer : MonoBehaviour {
    public BankPrologue director;
    public Camera view;
    public GameObject weapon;
    public Light muzzle;
    public Transform magazine, bolt;
    public PrologueRagdoll ragdoll;
    public Renderer[] avatarRenderers;
    public bool KnockedDown => ragdoll && ragdoll.IsDown;
    [Min(1)] public int magazineSize = 30;
    [Min(.05f)] public float shotInterval = .10f;
    [Min(.3f)] public float reloadSeconds = 2.2f;
    public int Health { get; private set; } = 100;
    public int Ammo { get; private set; } = 30;
    public bool Reloading { get; private set; }
    CharacterController body;
    float pitch, vertical, nextShot, immuneUntil, recoil;
    Vector3 rest;
    Vector3 magazineRest, boltRest;
    Quaternion restRotation;
    void Awake() {
        body = GetComponent<CharacterController>(); rest = weapon.transform.localPosition;
        restRotation=weapon.transform.localRotation;Ammo=magazineSize;
        if(magazine)magazineRest=magazine.localPosition;
        if(bolt)boltRest=bolt.localPosition;
        if(avatarRenderers!=null)foreach(var r in avatarRenderers)r.enabled=false;
    }
    void Update() {
        if (director == null || !director.CanPlay || KnockedDown) return;
        // 포커스를 되찾는 클릭으로 바로 발사하지 않는다.
        if (Cursor.lockState != CursorLockMode.Locked) {
            if (Input.GetMouseButtonDown(0)) Cursor.lockState = CursorLockMode.Locked;
            return;
        }
        transform.Rotate(0, Input.GetAxis("Mouse X") * 2, 0);
        pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * 2, -75, 75);
        recoil = Mathf.MoveTowards(recoil, 0, Time.deltaTime * 12);
        view.transform.localRotation = Quaternion.Euler(pitch - recoil, 0, 0);
        var input = Vector3.ClampMagnitude(new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")), 1);
        vertical = body.isGrounded && vertical < 0 ? -2 : vertical + Physics.gravity.y * Time.deltaTime;
        body.Move((transform.TransformDirection(input) * (Input.GetKey(KeyCode.LeftShift) ? 5.5f : 3.8f) + Vector3.up * vertical) * Time.deltaTime);
        if(!Reloading) {
            weapon.transform.localPosition = rest + Vector3.back * recoil * .015f;
            weapon.transform.localRotation=restRotation;
        }
        if (Input.GetMouseButton(0)) Fire();
        if (Input.GetKeyDown(KeyCode.R)) Reload();
    }
    public bool Fire() {
        if (director == null || !director.CanPlay || KnockedDown || Reloading || Ammo <= 0 || Time.time < nextShot) return false;
        Ammo--; nextShot = Time.time + shotInterval; recoil = 1.25f;
        GunshotSound.Play(gameObject, false); StartCoroutine(Flash());
        Vector3 end=view.transform.position+view.transform.forward*90;
        bool hitSomething=Physics.Raycast(view.transform.position, view.transform.forward, out var hit, 90, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        if(hitSomething)end=hit.point;
        // 카메라가 엄폐물 밖이어도 총구가 벽에 막혔다면 관통하지 않는다.
        Vector3 start=muzzle.transform.position;
        if(Physics.Raycast(start,(end-start).normalized,out var obstruction,Vector3.Distance(start,end),Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)){
            hit=obstruction;end=hit.point;hitSomething=true;
        }
        PrologueTracer.Spawn(start,end,new Color(1,.8f,.3f));
        if (hitSomething) {
            var target = hit.collider.GetComponentInParent<PrologueOfficer>();
            if (target != null) target.ReceiveHit(35);
        }
        return true;
    }
    IEnumerator Flash() { muzzle.enabled = true; yield return new WaitForSeconds(.05f); muzzle.enabled = false; }
    public bool Reload() {
        if (director == null || !director.CanPlay || KnockedDown || Reloading || Ammo == magazineSize) return false;
        StartCoroutine(ReloadRoutine()); return true;
    }
    IEnumerator ReloadRoutine() {
        Reloading = true;
        for(float elapsed=0;elapsed<reloadSeconds;elapsed+=Time.deltaTime) {
            if(KnockedDown||(!director.CanPlay&&!director.Paused))break;
            float t=elapsed/reloadSeconds;
            float tilt=Mathf.SmoothStep(0,1,Mathf.Clamp01(t/.2f))*Mathf.SmoothStep(0,1,Mathf.Clamp01((1-t)/.2f));
            weapon.transform.localRotation=restRotation*Quaternion.Euler(10*tilt,15*tilt,-35*tilt);
            weapon.transform.localPosition=rest+new Vector3(-.07f,.07f,-.08f)*tilt;
            if(magazine) {
                float pull=t<.52f?Mathf.InverseLerp(.22f,.48f,t):1-Mathf.InverseLerp(.56f,.76f,t);
                magazine.localPosition=magazineRest+Vector3.down*(Mathf.Clamp01(pull)*.24f);
            }
            if(bolt)bolt.localPosition=boltRest+Vector3.forward*(Mathf.Sin(Mathf.Clamp01((t-.78f)/.13f)*Mathf.PI)*.06f);
            yield return null;
        }
        weapon.transform.SetLocalPositionAndRotation(rest,restRotation);
        if(magazine)magazine.localPosition=magazineRest;if(bolt)bolt.localPosition=boltRest;
        if(director.CanPlay&&!KnockedDown)Ammo=magazineSize;
        Reloading = false;
    }
    public bool ReceiveHit(int damage) {
        if (director == null || !director.CanPlay || KnockedDown || Time.time < immuneUntil || damage <= 0) return false;
        Health = Mathf.Max(0, Health - damage); immuneUntil = Time.time + 3.2f;
        director.HitFeedback();
        if (Health == 0)director.Fail();
        if(ragdoll)StartCoroutine(FallAndRise());
        return true;
    }
    IEnumerator FallAndRise(){
        // 머리 안쪽 메시가 시야를 가리지 않도록 몸은 숨기고 물리 뼈만 추적한다.
        foreach(var r in avatarRenderers)r.enabled=false;
        var head=ragdoll.animator.GetBoneTransform(HumanBodyBones.Head);
        var eyeRotation=Quaternion.Inverse(head.rotation)*view.transform.rotation;
        var eyeOffset=Quaternion.Inverse(head.rotation)*(transform.forward*.09f+Vector3.up*.05f);
        ragdoll.Knock(-transform.forward*2+Vector3.up*1.3f,Health==0);
        view.enabled=false;weapon.SetActive(false);director.filmCamera.enabled=true;
        view.GetComponent<AudioListener>().enabled=false;director.filmCamera.GetComponent<AudioListener>().enabled=true;
        director.crosshair.SetActive(false);
        while(ragdoll.IsDown){
            Vector3 destination=head.position+head.rotation*eyeOffset;
            destination.y=Mathf.Max(.12f,destination.y);
            director.filmCamera.transform.SetPositionAndRotation(destination,head.rotation*eyeRotation);
            director.filmCamera.fieldOfView=view.fieldOfView;
            director.filmCamera.nearClipPlane=.04f;
            yield return null;
        }
        foreach(var r in avatarRenderers)r.enabled=false;
        if(director.CanPlay){view.enabled=true;weapon.SetActive(true);director.filmCamera.enabled=false;
            view.GetComponent<AudioListener>().enabled=true;director.filmCamera.GetComponent<AudioListener>().enabled=false;director.crosshair.SetActive(true);}
    }
    public void Teleport(Vector3 position) { body.enabled = false; transform.position = position; body.enabled = true; vertical = 0; }
}
}
