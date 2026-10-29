using UnityEngine;
namespace DeliveryService.Mok {
// 프롤로그 운전 사운드와 짧은 충돌 흔들림. 지속 접촉은 반복 재생하지 않는다.
[DefaultExecutionOrder(250)]
public sealed class PrologueDriveFeedback : MonoBehaviour {
 public PrologueDrive drive;public AudioSource engine,impact;
 public float impactCooldown=.65f,shakeDuration=.28f;
 public int ImpactCount {get;private set;}
 public float ShakeRemaining => shake;
 float nextImpact,shake,strength;bool paused;
 void Start(){if(SuppliedAudio.Current&&SuppliedAudio.Current.carCrash)impact.clip=SuppliedAudio.Current.carCrash;if(SuppliedAudio.Current&&SuppliedAudio.Current.playerEngine)engine.clip=SuppliedAudio.Current.playerEngine;}
 void Update(){
  bool cityRide=drive.director.Current==BankPrologue.Phase.CityReveal&&(!drive.director.shopArrival||drive.director.shopArrival.Stage<2);
  if(!drive.Driving&&!cityRide){StopEffects();return;}
  if(drive.director.Paused){if(!paused){engine.Pause();impact.Pause();paused=true;}return;}
  if(paused){engine.UnPause();impact.UnPause();paused=false;}
  if(cityRide){
   if(impact)impact.Stop();shake=0;
   bool conversation=drive.director.shopArrival&&drive.director.shopArrival.Stage==1;
   engine.volume=Mathf.MoveTowards(engine.volume,(conversation?.055f:.085f)*SuppliedAudio.Volume,Time.deltaTime*.3f);
   engine.pitch=Mathf.MoveTowards(engine.pitch,conversation?.76f:.9f,Time.deltaTime);
   if(!engine.isPlaying)engine.Play();return;
  }
  if(!engine.isPlaying)engine.Play();
  float speed=Mathf.Clamp01(Mathf.Abs(drive.Speed)/Mathf.Max(1,drive.maxSpeed));
  engine.pitch=Mathf.Lerp(engine.pitch,Mathf.Lerp(.75f,1.65f,speed),1-Mathf.Exp(-5*Time.deltaTime));
  engine.volume=Mathf.Lerp(engine.volume,Mathf.Lerp(.12f,.28f,speed)*SuppliedAudio.Volume,1-Mathf.Exp(-4*Time.deltaTime));
 }
 public void Collision(float speed){
  if(!drive.Driving||drive.director.Paused||speed<2.5f||Time.time<nextImpact)return;
  nextImpact=Time.time+impactCooldown;strength=Mathf.InverseLerp(2,23,speed);shake=shakeDuration;
  impact.pitch=Mathf.Lerp(1.1f,.85f,strength);StorySoundMix.Play(impact,impact.clip,Mathf.Lerp(.18f,.5f,strength));ImpactCount++;
 }
 void LateUpdate(){
  if(!drive.Driving||drive.director.Paused||shake<=0)return;
  shake=Mathf.Max(0,shake-Time.deltaTime);float amount=shake/shakeDuration*Mathf.Lerp(.25f,1,strength),t=(shakeDuration-shake)*65;
  var cam=drive.director.filmCamera.transform;
  cam.localPosition+=cam.right*(Mathf.Sin(t)*.012f*amount)+cam.up*(Mathf.Sin(t*1.3f)*.008f*amount);
  cam.rotation*=Quaternion.Euler(Mathf.Sin(t)*.55f*amount,Mathf.Sin(t*.7f)*.25f*amount,Mathf.Cos(t)*.35f*amount);
 }
 public void StopEffects(){if(engine)engine.Stop();if(impact)impact.Stop();shake=0;paused=false;}
 void OnDisable(){StopEffects();}
}}
