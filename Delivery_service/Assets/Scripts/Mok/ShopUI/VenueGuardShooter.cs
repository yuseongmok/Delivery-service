using UnityEngine;
namespace DeliveryService.Mok {
// 이전 씬의 직렬화 참조를 보존한다. 차량 도주 중 사격은 폐기했다.
public sealed class VenueGuardShooter:MonoBehaviour {
 public VenueRoadInterception owner;
 public float OriginalHandScale=>1; public float WeaponSize=>0;
 void Awake(){enabled=false;}
}}
