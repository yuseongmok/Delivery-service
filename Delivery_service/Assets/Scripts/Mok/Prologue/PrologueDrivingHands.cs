using UnityEngine;
namespace DeliveryService.Mok {
// 저장된 운전 팔을 핸들 접점에 맞추고 팔꿈치를 자연스럽게 접는다.
[DefaultExecutionOrder(200)]
public sealed class PrologueDrivingHands : MonoBehaviour {
 public PrologueDrive drive;public VenueGetawayDrive venueDrive;public Transform wheel,arms,leftGrip,rightGrip;
 public Transform leftUpper,leftLower,leftHand,rightUpper,rightLower,rightHand;
 public Quaternion wheelRest;public float maxAngle=65;public float Angle {get;private set;}
 void Awake(){arms.gameObject.SetActive(false);}
 void LateUpdate(){
  bool active=venueDrive?venueDrive.Driving:drive.Driving; if(arms.gameObject.activeSelf!=active)arms.gameObject.SetActive(active);
  if(venueDrive?Time.timeScale<=0:drive.director.Paused)return;
  Angle=Mathf.MoveTowards(Angle,active?(venueDrive?venueDrive.SteeringInput:drive.SteeringInput)*maxAngle:0,160*Time.deltaTime);
  wheel.localRotation=wheelRest*Quaternion.AngleAxis(Angle,Vector3.up);
  if(active)Pose();
 }
 public void Pose(){
  Solve(leftUpper,leftLower,leftHand,leftGrip,(venueDrive?venueDrive.transform:drive.transform).TransformPoint(new Vector3(-.78f,.63f,-.03f)));
  Solve(rightUpper,rightLower,rightHand,rightGrip,(venueDrive?venueDrive.transform:drive.transform).TransformPoint(new Vector3(.05f,.63f,-.03f)));
 }
 static void Solve(Transform upper,Transform lower,Transform hand,Transform target,Vector3 hint){
  Vector3 start=upper.position,delta=target.position-start;float a=Vector3.Distance(start,lower.position),b=Vector3.Distance(lower.position,hand.position);
  float distance=Mathf.Clamp(delta.magnitude,.01f,a+b-.001f);Vector3 direction=delta.normalized;
  Vector3 bend=Vector3.ProjectOnPlane(hint-start,direction).normalized;
  float along=(a*a-b*b+distance*distance)/(2*distance);Vector3 elbow=start+direction*along+bend*Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
  upper.rotation=Quaternion.FromToRotation(lower.position-start,elbow-start)*upper.rotation;
  lower.rotation=Quaternion.FromToRotation(hand.position-lower.position,target.position-lower.position)*lower.rotation;
  hand.rotation=target.rotation;
 }
}}
