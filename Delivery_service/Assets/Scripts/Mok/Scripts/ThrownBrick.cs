using UnityEngine;
namespace DeliveryService.Mok {
public sealed class ThrownBrick:MonoBehaviour,IInteractable {
 // 기존 벽돌 컴포넌트 이름은 호환성을 위해 유지하고 모든 무기에 사용한다.
 public WeaponKind kind=WeaponKind.Brick;public PlayerWeapon owner;bool hit;float born;
 void Awake(){born=Time.time;gameObject.layer=13;}
 void OnCollisionEnter(Collision c){if(hit||owner==null||c.transform.IsChildOf(owner.transform))return;hit=true;owner.Hit(c.collider,c.relativeVelocity.normalized,kind==WeaponKind.Brick?50:WeaponCatalog.Damage[(int)kind]);}
 public bool CanRecover(GameObject player)=>owner!=null&&owner.gameObject==player&&owner.Thrown==this&&Time.time>born+.4f;
 public void Interact(ToppingInventory inventory){if(inventory!=null&&CanRecover(inventory.gameObject))owner.Recover(this);}
 void OnGUI(){
  if(owner==null||owner.Thrown!=this||owner.Combat==null||!owner.Combat.CanAct||Time.time<born+.4f)return;
  var cam=owner.Combat.View;if(cam==null)return;var point=cam.WorldToViewportPoint(transform.position+Vector3.up*.28f);
  if(point.z<=0)return;
  float x=Mathf.Clamp(point.x*Screen.width,100,Screen.width-100),y=Mathf.Clamp((1-point.y)*Screen.height,55,Screen.height-140);
  var style=new GUIStyle(GUI.skin.label){font=MokUIFont.Font,fontSize=Mathf.Max(16,Screen.height/48),alignment=TextAnchor.MiddleCenter};style.normal.textColor=new Color(1,.72f,.3f);
  GUI.Label(new Rect(x-110,y-25,220,62),$"▼ {WeaponCatalog.Names[(int)kind]}\n{Vector3.Distance(cam.transform.position,transform.position):0}m",style);
  // 공통 안내 UI가 없는 씬에서도 회수 문구를 표시한다.
  var input=owner.GetComponent<PlayerInteraction>();var hover=owner.GetComponentInChildren<InteractHoverManager>();
  if(input!=null&&(hover==null||hover.interactText==null)&&InteractionFocus.Find(input)==transform){style.normal.textColor=Color.white;GUI.Label(new Rect(0,Screen.height-125,Screen.width,40),"E를 눌러 줍기",style);}
 }
 void Update(){if(owner==null){Destroy(gameObject);return;}if(transform.position.y< -50){transform.position=owner.transform.position+Vector3.up;GetComponent<Rigidbody>().linearVelocity=Vector3.zero;}}
}
}
