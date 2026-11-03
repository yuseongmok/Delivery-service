using UnityEngine;
namespace DeliveryService.Mok {
[DefaultExecutionOrder(2100)]
public sealed class HeadHitbox:MonoBehaviour {
 Transform head;SphereCollider hitbox;
 public static void Attach(GameObject actor){if(actor.GetComponentInChildren<HeadHitbox>(true))return;var a=actor.GetComponentInChildren<Animator>(true);if(!a||!a.isHuman)return;var head=a.GetBoneTransform(HumanBodyBones.Head);if(!head)return;var go=new GameObject("머리 피격 판정");go.transform.SetParent(actor.transform,false);var box=go.AddComponent<HeadHitbox>();box.head=head;box.hitbox=go.AddComponent<SphereCollider>();box.hitbox.radius=.22f;box.hitbox.isTrigger=true;go.transform.position=head.position;}
 void LateUpdate(){if(!head){var a=transform.parent.GetComponentInChildren<Animator>();if(a&&a.isHuman)head=a.GetBoneTransform(HumanBodyBones.Head);}if(head)transform.position=head.position;}
}
}
