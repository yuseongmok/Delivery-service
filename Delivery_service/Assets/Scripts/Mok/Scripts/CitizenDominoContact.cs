using UnityEngine;
namespace DeliveryService.Mok {
// 기존 참조와 호출은 보존하되 연쇄 기절은 사용하지 않는다.
public sealed class CitizenDominoContact:MonoBehaviour {
 public CitizenCombat owner;public PlayerCombat attacker;public float until;
 public static void Arm(CitizenCombat target,PlayerCombat player,float expiry){}
}
}
