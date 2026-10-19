using UnityEngine;
namespace DeliveryService.Mok {
// 화면 밖에서만 시작과 종료를 반복하는 로비 전용 이동. 게임 AI는 사용하지 않는다.
public sealed class LobbyPasser : MonoBehaviour {
 public Vector3 from,to; public float speed=3,period=48,delay=18;public bool running;
 Renderer[] visuals;Animator animator;
 void Awake(){visuals=GetComponentsInChildren<Renderer>(true);animator=GetComponentInChildren<Animator>();}
 void Update(){float age=Time.unscaledTime-delay;float length=Vector3.Distance(from,to);float t=age<0?-1:Mathf.Repeat(age,period)*speed/Mathf.Max(.1f,length);bool visible=t>=0&&t<1;
 foreach(var r in visuals)if(r)r.enabled=visible;
 if(!visible)return;transform.position=Vector3.Lerp(from,to,t);transform.rotation=Quaternion.LookRotation(to-from);
 if(animator){foreach(var p in animator.parameters){if(p.name=="IsWalking"&&p.type==AnimatorControllerParameterType.Bool)animator.SetBool(p.name,true);if(p.name=="MoveSpeed"&&p.type==AnimatorControllerParameterType.Float)animator.SetFloat(p.name,speed);if(p.name=="RunRate"&&p.type==AnimatorControllerParameterType.Float)animator.SetFloat(p.name,1);} }
 }
}
}
