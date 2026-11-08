using UnityEngine;
namespace DeliveryService.Mok {
// 최종 탄착점 한 곳에서만 재생한다. 프롤로그의 작은 효과 풀과 기존 음원을 공유한다.
public static class BulletImpactFeedback {
 static PrologueCombatFeedback instance;
 public static PrologueCombatFeedback Current=>instance;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset(){instance=null;}
 public static void Play(RaycastHit hit){if(!hit.collider)return;bool person=hit.collider.GetComponentInParent<CitizenCombat>()||hit.collider.GetComponentInParent<PoliceCombat>()||hit.collider.GetComponentInParent<RobberAI>()||hit.collider.GetComponentInParent<PlayerCombat>();
  if(!instance){var prefab=Resources.Load<GameObject>("BulletImpactFeedback");if(!prefab)return;instance=Object.Instantiate(prefab).GetComponent<PrologueCombatFeedback>();}
  instance.Impact(hit.point,hit.normal,person);
 }
}
}
