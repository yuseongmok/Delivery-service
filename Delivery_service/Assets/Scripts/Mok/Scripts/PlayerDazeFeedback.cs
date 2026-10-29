using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace DeliveryService.Mok {
// 레그돌 상태를 관찰만 한다. 이동·체력·회복 시간은 변경하지 않는다.
[DisallowMultipleComponent]
public sealed class PlayerDazeFeedback:MonoBehaviour {
 ProloguePlayer prologue;PlayerCombat player;CanvasGroup group;GameObject overlay;Texture2D texture;
 readonly List<AudioLowPassFilter> owned=new();float scanAt;AudioSource ringing;AudioClip tone;
 public float Intensity{get;private set;}
 public bool IsDown=>prologue?prologue.KnockedDown:player&&player.IsDown;
 void Start(){
  prologue=GetComponent<ProloguePlayer>();player=GetComponent<PlayerCombat>();
  overlay=new GameObject("피격 혼미 효과",typeof(Canvas),typeof(CanvasGroup));overlay.transform.SetParent(transform,false);
  var canvas=overlay.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=45;
  group=overlay.GetComponent<CanvasGroup>();group.alpha=0;group.blocksRaycasts=false;group.interactable=false;
  var image=new GameObject("가장자리 안개",typeof(RectTransform),typeof(RawImage)).GetComponent<RawImage>();image.transform.SetParent(overlay.transform,false);image.raycastTarget=false;
  image.rectTransform.anchorMin=Vector2.zero;image.rectTransform.anchorMax=Vector2.one;image.rectTransform.offsetMin=image.rectTransform.offsetMax=Vector2.zero;
  texture=new Texture2D(128,128,TextureFormat.RGBA32,false);texture.wrapMode=TextureWrapMode.Clamp;
  for(int y=0;y<128;y++)for(int x=0;x<128;x++){float r=new Vector2((x-63.5f)/63.5f,(y-63.5f)/63.5f).magnitude;float haze=Mathf.SmoothStep(0,.43f,Mathf.InverseLerp(.22f,.95f,r));float dark=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.82f,1.35f,r));texture.SetPixel(x,y,new Color(Mathf.Lerp(.65f,.04f,dark),Mathf.Lerp(.69f,.05f,dark),Mathf.Lerp(.72f,.07f,dark),haze));}
  texture.Apply();image.texture=texture;
  var samples=new float[22050];for(int i=0;i<samples.Length;i++)samples[i]=Mathf.Sin(i*2*Mathf.PI*1100/22050)*.025f;
  tone=AudioClip.Create("약한 피격 이명",samples.Length,1,22050,false);tone.SetData(samples,0);
  ringing=gameObject.AddComponent<AudioSource>();ringing.playOnAwake=false;ringing.loop=true;ringing.clip=tone;ringing.volume=0;
 }
 void Update(){
  if(!group)return;if(Time.timeScale<=0){ringing.Pause();return;}
  Intensity=Mathf.MoveTowards(Intensity,IsDown?1:0,Time.deltaTime*(IsDown?5:.7f));group.alpha=Intensity;
  if(Intensity>.01f){
   ringing.UnPause();if(!ringing.isPlaying)ringing.Play();ringing.volume=.16f*Intensity*SuppliedAudio.Volume;
   if(Time.time>=scanAt){scanAt=Time.time+.25f;foreach(var s in FindObjectsByType<AudioSource>(FindObjectsSortMode.None)){if(s==ringing||s.gameObject==gameObject||!s.isPlaying||s.GetComponent<AudioLowPassFilter>())continue;owned.Add(s.gameObject.AddComponent<AudioLowPassFilter>());}}
   foreach(var f in owned)if(f)f.cutoffFrequency=Mathf.Lerp(22000,1800,Intensity);
  }else Clear();
 }
 void Clear(){if(ringing)ringing.Stop();foreach(var f in owned)if(f){f.enabled=false;Destroy(f);}owned.Clear();}
 void OnDisable(){Clear();Intensity=0;if(group)group.alpha=0;}
 void OnDestroy(){Clear();if(overlay)Destroy(overlay);if(texture)Destroy(texture);if(tone)Destroy(tone);if(ringing)Destroy(ringing);}
}
}
