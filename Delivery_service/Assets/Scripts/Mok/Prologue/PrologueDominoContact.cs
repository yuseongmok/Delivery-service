using UnityEngine;
namespace DeliveryService.Mok {
// 이전 씬 참조만 유지한다. 조직원 몸의 충돌은 추가 피해나 기절을 전달하지 않는다.
public sealed class PrologueDominoContact:MonoBehaviour {public PrologueOfficer owner;}
}
