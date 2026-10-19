using UnityEngine;
namespace DeliveryService.Mok {
public sealed class ThrownBrick:MonoBehaviour,IInteractable {
 // 기존 벽돌 컴포넌트 이름은 호환성을 위해 유지하고 모든 무기에 사용한다.
 public WeaponKind kind=WeaponKind.Brick;public PlayerWeapon owner;bool hit;float born;
 void Awake(){born=Time.time;gameObject.layer=13;}
 void OnCollisionEnter(Collision c){if(hit||owner==null||c.transform.IsChildOf(owner.transform))return;hit=true;owner.Hit(c.collider,c.relativeVelocity.normalized,kind==WeaponKind.Brick?50:WeaponCatalog.Damage[(int)kind]);}
 public void Interact(ToppingInventory inventory){if(owner!=null&&inventory.gameObject==owner.gameObject&&Time.time>born+.4f)owner.Recover(this);}
 void Update(){if(owner==null){Destroy(gameObject);return;}if(transform.position.y< -50){transform.position=owner.transform.position+Vector3.up;GetComponent<Rigidbody>().linearVelocity=Vector3.zero;}}
}
}
