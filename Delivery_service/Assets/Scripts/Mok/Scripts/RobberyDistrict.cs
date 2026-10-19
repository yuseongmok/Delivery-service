using UnityEngine;
namespace DeliveryService.Mok {
// 빈민촌 내부만 판정한다. 다리와 입구 주차장은 위험 구역에서 제외한다.
public sealed class RobberyDistrict : MonoBehaviour {
 public Vector3 localCenter=new(-280,0,-5);
 public Vector3 size=new(168,30,158);
 public bool Contains(Vector3 position){var p=transform.InverseTransformPoint(position)-localCenter;return Mathf.Abs(p.x)<size.x*.5f&&Mathf.Abs(p.z)<size.z*.5f&&Mathf.Abs(p.y)<size.y*.5f;}
}
}
