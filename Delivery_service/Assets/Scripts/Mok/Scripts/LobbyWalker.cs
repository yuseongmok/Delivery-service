using UnityEngine;
namespace DeliveryService.Mok {
// 시민은 전투 AI 없이 정해진 보도만 지나간다. 양 끝은 화면 밖에 둔다.
public sealed class LobbyWalker : MonoBehaviour {
 public Vector3 from,to;public float speed=1.1f,delay;float elapsed;
 void Start(){elapsed=24;var a=GetComponentInChildren<Animator>();if(a){a.applyRootMotion=false;a.SetBool("IsWalking",true);a.cullingMode=AnimatorCullingMode.AlwaysAnimate;}}
 void Update(){elapsed+=Time.unscaledDeltaTime;if(elapsed<delay)return;float length=Vector3.Distance(from,to);if(length<.01f)return;float t=Mathf.Repeat((elapsed-delay)*speed,length)/length;transform.position=Vector3.Lerp(from,to,t);transform.rotation=Quaternion.LookRotation(to-from);}
}
}
