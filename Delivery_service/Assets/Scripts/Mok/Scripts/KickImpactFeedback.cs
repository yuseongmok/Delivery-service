using UnityEngine;
using UnityEngine.Rendering;
namespace DeliveryService.Mok {
// 명중 순간에만 짧은 저음과 작은 화면 충격을 더한다. 조작용 카메라 각도는 바꾸지 않는다.
public sealed class KickImpactFeedback : MonoBehaviour {
 const float Duration=.17f;
 AudioSource source;AudioClip clip;Camera view;float began=-100;
 readonly System.Collections.Generic.Dictionary<WeaponKind,AudioClip> weaponClips=new();
 public WeaponKind LastWeapon {get;private set;}=WeaponKind.None;
 Vector3 savedPosition;Quaternion savedRotation;bool applied;
 public int HitCount {get;private set;}
 public bool IsShaking=>Time.time-began<Duration;
 void Awake(){
  source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=0;source.volume=.5f;
  const int rate=44100;var samples=new float[(int)(rate*.22f)];var random=new System.Random(739);float smoothNoise=0;
  for(int i=0;i<samples.Length;i++){
   float t=i/(float)rate;float noise=(float)(random.NextDouble()*2-1);smoothNoise=Mathf.Lerp(smoothNoise,noise,.25f);
   float bass=Mathf.Sin(2*Mathf.PI*(110*t-100*t*t))*Mathf.Exp(-t*24);
   samples[i]=Mathf.Min(1,t/.002f)*(bass*.65f+smoothNoise*Mathf.Exp(-t*45)*.5f+noise*Mathf.Exp(-t*180)*.12f);
  }
  clip=AudioClip.Create("발차기 명중",samples.Length,1,rate,false);clip.SetData(samples,0);
 }
 void OnEnable(){RenderPipelineManager.beginCameraRendering+=BeforeCamera;RenderPipelineManager.endCameraRendering+=AfterCamera;}
 public static void Play(PlayerCombat player){Play(player,WeaponKind.None);}
 public static void Play(PlayerCombat player,WeaponKind weapon){
  if(weapon==WeaponKind.Pistol)return;
  var effect=player.GetComponent<KickImpactFeedback>()??player.gameObject.AddComponent<KickImpactFeedback>();effect.view=player.View;effect.began=Time.time;effect.HitCount++;effect.LastWeapon=weapon;
  effect.source.PlayOneShot(weapon==WeaponKind.None?effect.clip:effect.WeaponClip(weapon));
 }
 AudioClip WeaponClip(WeaponKind weapon){
  if(weaponClips.TryGetValue(weapon,out var cached))return cached;
  const int rate=44100;bool tuna=weapon==WeaponKind.Tuna,brick=weapon==WeaponKind.Brick;
  float duration=tuna?.30f:brick?.20f:.24f,frequency=tuna?65:brick?180:140,decay=tuna?17:brick?38:25;
  var samples=new float[(int)(rate*duration)];var random=new System.Random(1039+(int)weapon);float filtered=0;
  for(int i=0;i<samples.Length;i++){
   float t=i/(float)rate,noise=(float)(random.NextDouble()*2-1);filtered=Mathf.Lerp(filtered,noise,tuna?.16f:brick?.65f:.3f);
   float thud=Mathf.Sin(2*Mathf.PI*(frequency*t-(tuna?55:90)*t*t))*Mathf.Exp(-t*decay);
   float texture=filtered*Mathf.Exp(-t*(brick?50:32))*(brick?.55f:.28f);
   float ring=weapon==WeaponKind.Bat?Mathf.Sin(2*Mathf.PI*380*t)*Mathf.Exp(-t*42)*.18f:0;
   samples[i]=Mathf.Clamp(Mathf.Min(1,t/.0015f)*(thud*(tuna?.8f:.6f)+texture+ring),-.95f,.95f);
  }
  var sound=AudioClip.Create(WeaponCatalog.Names[(int)weapon]+" 명중",samples.Length,1,rate,false);sound.SetData(samples,0);weaponClips.Add(weapon,sound);return sound;
 }
 void BeforeCamera(ScriptableRenderContext context,Camera camera){
  if(camera!=view||!IsShaking||Time.timeScale<=0||applied)return;
  float t=(Time.time-began)/Duration,envelope=(1-t)*(1-t);
  savedPosition=camera.transform.localPosition;savedRotation=camera.transform.localRotation;applied=true;
  camera.transform.localPosition=savedPosition+new Vector3(Mathf.Sin(t*23)*.006f,Mathf.Cos(t*19)*.008f,0)*envelope;
  camera.transform.localRotation=savedRotation*Quaternion.Euler(Mathf.Cos(t*19)*.65f*envelope,Mathf.Sin(t*23)*.35f*envelope,0);
 }
 void AfterCamera(ScriptableRenderContext context,Camera camera){if(camera==view)Restore();}
 void Restore(){if(!applied)return;if(view!=null){view.transform.localPosition=savedPosition;view.transform.localRotation=savedRotation;}applied=false;}
 void OnDisable(){Restore();began=-100;RenderPipelineManager.beginCameraRendering-=BeforeCamera;RenderPipelineManager.endCameraRendering-=AfterCamera;}
 void OnDestroy(){Restore();if(clip!=null)Destroy(clip);foreach(var sound in weaponClips.Values)if(sound!=null)Destroy(sound);if(source!=null)Destroy(source);}
}
}
