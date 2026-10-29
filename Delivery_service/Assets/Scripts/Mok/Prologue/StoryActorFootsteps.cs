using UnityEngine;
namespace DeliveryService.Mok {
// 실제 보행 거리와 애니메이션이 함께 움직일 때만 발소리를 낸다. 순간이동·차량 이동은 제외한다.
[DefaultExecutionOrder(400)]
public sealed class StoryActorFootsteps:MonoBehaviour {
 public Animator animator; public BankPrologue director; public bool street;
 AudioSource source; Vector3 previous; float distance; int last=-1; bool paused,hasMove;
 public int Steps{get;private set;}
 public static void Attach(Transform actor,Animator animator,BankPrologue director,bool street=false){if(!actor||!animator)return;var f=actor.GetComponent<StoryActorFootsteps>();if(!f)f=actor.gameObject.AddComponent<StoryActorFootsteps>();f.animator=animator;f.director=director;f.street=street;f.hasMove=false;foreach(var p in animator.parameters)if(p.name=="MoveSpeed")f.hasMove=true;}
 void Awake(){source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=1;source.rolloffMode=AudioRolloffMode.Linear;source.minDistance=1.5f;source.maxDistance=18;previous=transform.position;}
 void OnEnable(){previous=transform.position;distance=0;}
 void LateUpdate(){
  var delta=transform.position-previous;previous=transform.position;delta.y=0;
  bool stop=Time.timeScale<=0||(director&&director.Paused);
  if(stop){if(!paused){source.Pause();paused=true;}return;}if(paused){source.UnPause();paused=false;}
  if(!animator||!animator.enabled||!hasMove||animator.GetFloat("MoveSpeed")<.1f||delta.magnitude>1){distance=0;return;}
  if(delta.sqrMagnitude<.000001f)return;
  distance+=delta.magnitude;if(distance<.75f)return;distance%=.75f;
  var bank=SuppliedAudio.Current;var clips=bank?(street?bank.streetSteps:bank.indoorSteps):null;if(clips==null||clips.Length==0)return;
  int index=Random.Range(0,clips.Length);if(clips.Length>1&&index==last)index=(index+1)%clips.Length;last=index;
  source.pitch=Random.Range(.96f,1.04f);source.PlayOneShot(clips[index],.24f*SuppliedAudio.Volume);Steps++;
 }
 void OnDisable(){if(source)source.Stop();distance=0;paused=false;}
}
}
