using UnityEngine;
namespace DeliveryService.Mok {
// 충돌 방향으로 밀리면서 바깥으로 회전하고, 서스펜션 반동이 가라앉으면 멈춘다.
public sealed class PrologueRoadblockCar:MonoBehaviour {
 public bool Cleared{get;private set;}
 public Vector3 slideDirection=Vector3.forward;
 public float ImpactSpeed{get;private set;}
 Vector3 start,travel;Quaternion heading;float began,turn;Rigidbody body;
 void Awake(){body=GetComponent<Rigidbody>();}
 public bool TryRam(Vector3 velocity,Transform car){
  if(Cleared)return true;if(velocity.magnitude<3)return false;
  Cleared=true;ImpactSpeed=velocity.magnitude;start=transform.position;heading=transform.rotation;began=Time.time;
  float power=Mathf.InverseLerp(3,23,ImpactSpeed);
  travel=slideDirection.normalized*Mathf.Lerp(4.5f,5.5f,power)+velocity.normalized*Mathf.Lerp(2.2f,3.8f,power);
  turn=Vector3.Dot(Vector3.Cross(velocity.normalized,slideDirection.normalized),Vector3.up)>=0?1:-1;
  // 충돌체를 유지한다. 운전 컨트롤러가 접촉면 앞에서 기다리며 밀려나는 차를 따라간다.
  var drive=car.GetComponent<PrologueDrive>();if(drive)drive.AbsorbRam();
  var effect=car.GetComponent<PrologueDriveFeedback>();if(effect)effect.RamImpact(ImpactSpeed);
  return true;
 }
 void FixedUpdate(){if(!Cleared||Time.timeScale<=0)return;
  float t=Mathf.Clamp01((Time.time-began)/1.05f),eased=1-Mathf.Pow(1-t,3);
  float bounce=Mathf.Sin(t*Mathf.PI*3)*(1-t);
  var at=start+travel*eased+Vector3.up*Mathf.Abs(bounce)*.08f;
  var rot=heading*Quaternion.Euler(bounce*2.5f,turn*32*eased,bounce*4);
  if(body){body.MovePosition(at);body.MoveRotation(rot);}else transform.SetPositionAndRotation(at,rot);
 }
}}
