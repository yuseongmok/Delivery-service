using UnityEngine;
namespace DeliveryService.Mok {
public sealed class ThrownBrick:MonoBehaviour,IInteractable {
 public PlayerWeapon owner;bool hit;float born;
 void Awake(){born=Time.time;gameObject.layer=13;}
 void OnCollisionEnter(Collision c){if(hit||owner==null||c.transform.IsChildOf(owner.transform))return;hit=true;owner.Hit(c.collider,GetComponent<Rigidbody>().linearVelocity.normalized,50);}
 public void Interact(ToppingInventory inventory){if(owner!=null&&inventory.gameObject==owner.gameObject&&Time.time>born+.4f)owner.Recover(this);}
 void Update(){if(owner==null){Destroy(gameObject);return;}if(transform.position.y< -50){transform.position=owner.transform.position+Vector3.up;GetComponent<Rigidbody>().linearVelocity=Vector3.zero;}}
}
}
