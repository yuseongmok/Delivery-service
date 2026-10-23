using System.Collections;using System.Collections.Generic;using System.Linq;using UnityEngine;
namespace DeliveryService.Mok {
// 피자 인벤토리와 독립적인 한 칸짜리 무기 슬롯이다.
public sealed class PlayerWeapon : MonoBehaviour {
 public WeaponKind Equipped {get;private set;}=WeaponKind.None;
 public int Ammo {get;private set;}
 public ThrownBrick Thrown {get;private set;}
 public bool HasWeapon=>Equipped!=WeaponKind.None;
 public bool HasInHand=>HasWeapon&&Thrown==null;
 public PlayerCombat Combat {get;private set;}
 PlayerTacticalStance stance;
 Transform held; float nextAttack,animationEnd,animationStart; Quaternion restRotation; Vector3 restPosition;
 void Awake(){Combat=GetComponent<PlayerCombat>();}
 void Start(){stance=GetComponent<PlayerTacticalStance>();}
 public bool Purchase(WeaponKind kind){int i=(int)kind;if(i<0||i>3||MoneyManager.Instance==null)return false;
  if(!MoneyManager.Instance.SpendMoney(WeaponCatalog.Prices[i]))return false;
  Equip(kind);return true;}
 public bool BuyAmmo(){if(Equipped!=WeaponKind.Pistol||Ammo>989||MoneyManager.Instance==null||!MoneyManager.Instance.SpendMoney(3000))return false;Ammo+=10;return true;}
 public void Equip(WeaponKind kind){if(Thrown!=null)Destroy(Thrown.gameObject);Thrown=null;if(held!=null)Destroy(held.gameObject);Equipped=kind;Ammo=0;nextAttack=Time.time+.25f;
  if(kind==WeaponKind.None)return;var prefab=WeaponCatalog.Model(kind);if(prefab==null)return;
  held=Instantiate(prefab,Combat.View.transform).transform;held.gameObject.name="Held "+kind;restPosition=kind==WeaponKind.Pistol?new Vector3(.23f,-.33f,.45f):new Vector3(.32f,-.35f,.55f);restRotation=kind==WeaponKind.Pistol?Quaternion.Euler(0,-4,0):Quaternion.Euler(-20,0,-18);held.localPosition=restPosition;held.localRotation=restRotation;
  foreach(var c in held.GetComponentsInChildren<Collider>())Destroy(c);foreach(var t in held.GetComponentsInChildren<Transform>())t.gameObject.layer=2;}
 void Update(){if(held!=null){held.gameObject.SetActive(HasInHand&&Combat.CanAct);float p=animationEnd>Time.time?Mathf.Clamp01((Time.time-animationStart)/(animationEnd-animationStart)):1;float arc=Mathf.Sin(p*Mathf.PI);float aim=stance?stance.AimAmount:0;if(Equipped==WeaponKind.Pistol)arc*=.06f;
   held.localRotation=Quaternion.Slerp(restRotation,Quaternion.identity,aim)*Quaternion.Euler(-arc*85,arc*25,arc*35);held.localPosition=Vector3.Lerp(restPosition,new Vector3(0,-.345f,.55f),aim)+new Vector3(-arc*.2f,arc*.12f,arc*.18f);}
  if(!Combat.CanAct||!HasInHand)return;if(Input.GetMouseButtonDown(0))Attack(false);else if(Equipped!=WeaponKind.Pistol&&Input.GetMouseButtonDown(1))Attack(true);}
 public bool Attack(bool throwing){if(!Combat.CanAct||!HasInHand||Combat.IsKicking||Time.time<nextAttack)return false;int i=(int)Equipped;
  if(!throwing&&Equipped==WeaponKind.Pistol&&Ammo<=0){DeliveryRequestNotice.Show("탄약이 없습니다. 무기점에서 구매하세요.");return false;}
  nextAttack=Time.time+WeaponCatalog.Cooldown[i];animationStart=Time.time;animationEnd=Time.time+(Equipped==WeaponKind.Pistol?.15f:WeaponCatalog.Cooldown[i]*.65f);
  if(throwing){Throw();return true;}
  if(Equipped==WeaponKind.Pistol){Ammo--;GunshotSound.Play(gameObject,false);Resolve();StartCoroutine(ShotFlash());}else StartCoroutine(Swing(Equipped));return true;}
 IEnumerator Swing(WeaponKind kind){yield return new WaitForSeconds(WeaponCatalog.Cooldown[(int)kind]*.23f);if(Combat.CanAct&&Equipped==kind&&HasInHand)Resolve();}
 bool Ignore(Transform t){var inv=GetComponent<ToppingInventory>();return t.IsChildOf(transform)||(inv!=null&&((inv.HoldPoint!=null&&t.IsChildOf(inv.HoldPoint))||(inv.HeldPizza!=null&&t.IsChildOf(inv.HeldPizza.transform))));}
 public int Resolve(){if(Equipped==WeaponKind.None)return 0;var cam=Combat.View;int i=(int)Equipped;var seen=new HashSet<int>();int count=0;
  // 참치는 부채꼴 여러 광선으로 쓸어 치고, 각 광선은 첫 장애물에서 멈춘다.
  int rays=Equipped==WeaponKind.Tuna?9:1;for(int r=0;r<rays;r++){var dir=Quaternion.AngleAxis(rays==1?0:(r-4)*9,cam.transform.up)*cam.transform.forward;
   var hits=Physics.SphereCastAll(cam.transform.position,Equipped==WeaponKind.Pistol?.015f:.18f,dir,WeaponCatalog.Reach[i],~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance);
   foreach(var h in hits){if(Ignore(h.transform))continue;var citizen=h.collider.GetComponentInParent<CitizenCombat>();var robber=h.collider.GetComponentInParent<RobberAI>();var police=h.collider.GetComponentInParent<PoliceCombat>();var target=police!=null?(Component)police:citizen!=null?(Component)citizen:robber;
    if(target!=null&&seen.Add(target.GetInstanceID())&&Hit(h.collider,dir,WeaponCatalog.Damage[i]))count++;break;}}
  // 광역 무기가 여러 명을 맞혀도 한 번 휘두른 충격음과 흔들림은 한 번만 준다.
  if(count>0&&Equipped!=WeaponKind.Pistol)KickImpactFeedback.Play(Combat,Equipped);
  return count;}
 public bool Hit(Collider collider,Vector3 direction,int damage){var police=collider.GetComponentInParent<PoliceCombat>();if(police!=null)return police.ReceiveHit(Combat,direction,damage);var citizen=collider.GetComponentInParent<CitizenCombat>();var robber=collider.GetComponentInParent<RobberAI>();
  if(citizen!=null){if(robber!=null)robber.DropLoot();return citizen.ReceiveKick(Combat,direction,damage);}return robber!=null&&robber.ReceiveKick(direction);}
 void Throw(){var cam=Combat.View;var pos=cam.transform.position;
  var go=Instantiate(WeaponCatalog.Model(Equipped),pos,cam.transform.rotation);go.name="Thrown "+Equipped;
  // 각 무기의 실제 외형을 감싸는 충돌체를 만들고 원본 장식 충돌체는 제거한다.
  var bounds=new Bounds(Vector3.zero,Vector3.zero);bool first=true;
  foreach(var renderer in go.GetComponentsInChildren<Renderer>()){
   var local=renderer.localBounds;
   for(int n=0;n<8;n++){var v=local.center+Vector3.Scale(local.extents,new Vector3((n&1)==0?-1:1,(n&2)==0?-1:1,(n&4)==0?-1:1));var point=go.transform.InverseTransformPoint(renderer.transform.TransformPoint(v));if(first){bounds=new Bounds(point,Vector3.zero);first=false;}else bounds.Encapsulate(point);}
  }
  foreach(var old in go.GetComponentsInChildren<Collider>()){old.enabled=false;Destroy(old);}
  var col=go.AddComponent<BoxCollider>();col.center=bounds.center;col.size=Vector3.Max(bounds.size,Vector3.one*.08f);
  var rb=go.AddComponent<Rigidbody>();rb.mass=Equipped==WeaponKind.Tuna?3:1.2f;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;rb.interpolation=RigidbodyInterpolation.Interpolate;
  rb.linearVelocity=cam.transform.forward*(Equipped==WeaponKind.Tuna?12:16)+Vector3.up*2;rb.angularVelocity=cam.transform.right*10;
  Thrown=go.AddComponent<ThrownBrick>();Thrown.owner=this;Thrown.kind=Equipped;
  foreach(var t in go.GetComponentsInChildren<Transform>())t.gameObject.layer=13;
  foreach(var own in GetComponentsInChildren<Collider>())Physics.IgnoreCollision(col,own);
  var inv=GetComponent<ToppingInventory>();if(inv!=null&&inv.HeldPizza!=null)foreach(var own in inv.HeldPizza.GetComponentsInChildren<Collider>())Physics.IgnoreCollision(col,own);
  if(held!=null)held.gameObject.SetActive(false);
 }
 public bool Recover(ThrownBrick brick){if(brick==null||brick!=Thrown||brick.kind!=Equipped)return false;Thrown=null;Destroy(brick.gameObject);return true;}
 IEnumerator ShotFlash(){var light=new GameObject("Muzzle flash",typeof(Light));light.transform.position=Combat.View.transform.position+Combat.View.transform.forward*.65f;var l=light.GetComponent<Light>();l.color=new Color(1,.6f,.2f);l.range=3;l.intensity=3;yield return new WaitForSeconds(.045f);Destroy(light);}
 void OnGUI(){if(!HasWeapon||!Combat.CanAct)return;var s=new GUIStyle(GUI.skin.label){font=MokUIFont.Font,fontSize=18,alignment=TextAnchor.MiddleRight};GUI.Label(new Rect(Screen.width-320,Screen.height-85,290,35),WeaponCatalog.Names[(int)Equipped]+(Thrown!=null?" · 회수 필요":Equipped==WeaponKind.Pistol?$"  {Ammo}발":""),s);}
}
}
