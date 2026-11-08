using UnityEngine;
namespace DeliveryService.Mok {
public sealed class ThrownBrick:MonoBehaviour,IInteractable {
 public WeaponKind kind=WeaponKind.Brick;public PlayerWeapon owner;bool hit;float born;
 
 public float noiseRadius = 15f; 
 public int noisePriority = 2; // [중요] 발소리(1)보다 벽돌(2)이 더 높아 1순위로 어그로가 끌림
 private bool hasMadeNoise = false;

 void Awake(){born=Time.time;gameObject.layer=13;}
 
 void OnCollisionEnter(Collision c){
  if (!hasMadeNoise && c.relativeVelocity.magnitude > 2f) {
   hasMadeNoise = true;
   Collider[] colliders = Physics.OverlapSphere(transform.position, noiseRadius);
   foreach (Collider col in colliders) {
    StealthCitizenAI npc = col.GetComponentInParent<StealthCitizenAI>();
    // ★ 투척물 자신의 현재 위치(transform.position)를 정확하게 AI에게 전달!
    if (npc != null) npc.HearSound(transform.position, noisePriority);
   }
  }

  if(hit||(owner!=null&&c.transform.IsChildOf(owner.transform)))return;
  hit=true;
  if(owner!=null) owner.Hit(c.collider,c.relativeVelocity.normalized,kind==WeaponKind.Brick?50:WeaponCatalog.Damage[(int)kind]);
 }
 
 public bool CanRecover(GameObject player)=>owner==null||(owner.gameObject==player&&owner.Thrown==this&&Time.time>born+.4f);
 
 public void Interact(ToppingInventory inventory){
  if(inventory==null)return;
  
  if(owner!=null&&CanRecover(inventory.gameObject)) {
   owner.Recover(this); 
  }
  else if(owner==null) {
   PlayerWeapon pw = inventory.GetComponent<PlayerWeapon>();
   if(pw!=null){ 
    pw.Equip(kind); 
    Destroy(gameObject); 
   }
  }
 }
 
 void OnGUI(){
  if(owner==null||owner.Thrown!=this||owner.Combat==null||!owner.Combat.CanAct||Time.time<born+.4f)return;
  var cam=owner.Combat.View;if(cam==null)return;var point=cam.WorldToViewportPoint(transform.position+Vector3.up*.28f);
  if(point.z<=0)return;
  float x=Mathf.Clamp(point.x*Screen.width,100,Screen.width-100),y=Mathf.Clamp((1-point.y)*Screen.height,55,Screen.height-140);
  var style=new GUIStyle(GUI.skin.label){font=MokUIFont.Font,fontSize=Mathf.Max(16,Screen.height/48),alignment=TextAnchor.MiddleCenter};style.normal.textColor=new Color(1,.72f,.3f);
  GUI.Label(new Rect(x-110,y-25,220,62),$"▼ {WeaponCatalog.Names[(int)kind]}\n{Vector3.Distance(cam.transform.position,transform.position):0}m",style);
  var input=owner.GetComponent<PlayerInteraction>();var hover=owner.GetComponentInChildren<InteractHoverManager>();
  if(input!=null&&(hover==null||hover.interactText==null)&&InteractionFocus.Find(input)==transform){style.normal.textColor=Color.white;GUI.Label(new Rect(0,Screen.height-125,Screen.width,40),"F를 눌러 줍기",style);}
 }
 
 void Update(){
  if(transform.position.y< -50){
   if(owner!=null){transform.position=owner.transform.position+Vector3.up;GetComponent<Rigidbody>().linearVelocity=Vector3.zero;}
   else Destroy(gameObject);  
  }
 }
}
}