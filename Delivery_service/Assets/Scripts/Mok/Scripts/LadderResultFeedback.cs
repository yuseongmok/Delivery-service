using UnityEngine;
using UnityEngine.UI;
namespace DeliveryService.Mok {
// 정산은 건드리지 않고 결과를 처음 확인할 때만 소리와 화면을 재생한다.
public sealed class LadderResultFeedback : MonoBehaviour {
 Text heading,amount;RectTransform board;Vector2 origin;Image background;Color baseColor;
 AudioSource speaker;AudioClip winSound,loseSound;int[] seen;float began=-10;bool won,active;
 RectTransform[] confetti=new RectTransform[20];Image[] pieces=new Image[20];
 public int PlayCount {get;private set;}
 public bool LastWon=>won;
 public void Initialize(Text title,Text value,RectTransform paths){
  heading=title;amount=value;board=paths;origin=board.anchoredPosition;background=GetComponent<Image>();baseColor=background.color;
  speaker=gameObject.AddComponent<AudioSource>();speaker.playOnAwake=false;speaker.spatialBlend=0;speaker.volume=.32f;
  winSound=MakeSound(true);loseSound=MakeSound(false);
  for(int i=0;i<confetti.Length;i++){
   var g=new GameObject("당첨 색종이",typeof(RectTransform),typeof(Image));g.layer=gameObject.layer;g.transform.SetParent(transform,false);
   var r=(RectTransform)g.transform;r.anchorMin=r.anchorMax=new Vector2(0,1);r.sizeDelta=new Vector2(7+i%3*3,12+i%2*5);
   confetti[i]=r;pieces[i]=g.GetComponent<Image>();pieces[i].raycastTarget=false;g.SetActive(false);
  }
 }
 public void Show(LadderGame game){
  if(!game.HasResult||ReferenceEquals(seen,game.Bridges))return;
  Clear();seen=game.Bridges;won=game.Won;began=Time.unscaledTime;active=true;PlayCount++;
  speaker.PlayOneShot(won?winSound:loseSound);
 }
 public void Clear(){
  active=false;if(heading==null)return;
  heading.rectTransform.localScale=amount.rectTransform.localScale=Vector3.one;board.anchoredPosition=origin;
  heading.color=amount.color=new Color(.10f,.17f,.23f);background.color=baseColor;
  foreach(var r in confetti)if(r!=null)r.gameObject.SetActive(false);
 }
 void LateUpdate(){
  if(!active)return;float t=Time.unscaledTime-began;
  if(t>2.2f){Clear();return;}
  float pulse=Mathf.Sin(Mathf.Clamp01(t/.55f)*Mathf.PI);
  heading.rectTransform.localScale=Vector3.one*(1+(won?.12f:.045f)*pulse);
  amount.rectTransform.localScale=Vector3.one*(1+(won?.08f:.025f)*pulse);
  var accent=won?new Color(.72f,.34f,.05f):new Color(.70f,.20f,.19f);
  heading.color=amount.color=Color.Lerp(accent,new Color(.10f,.17f,.23f),Mathf.Clamp01((t-1.5f)/.7f));
  background.color=Color.Lerp(baseColor,won?new Color(1,.86f,.55f):new Color(.87f,.74f,.73f),.32f*Mathf.Clamp01(1-t/.7f));
  if(!won)board.anchoredPosition=origin+Vector2.right*(Mathf.Sin(t*65)*9*Mathf.Clamp01(1-t/.45f));
  for(int i=0;i<confetti.Length;i++){
   confetti[i].gameObject.SetActive(won);if(!won)continue;
   float life=Mathf.Clamp01(t/2.2f);float x=30+(i*113%480);
   confetti[i].anchoredPosition=new Vector2(x+Mathf.Sin(t*4+i)*12,-(160+(i*29%120)+life*420));
   confetti[i].localRotation=Quaternion.Euler(0,0,i*37+t*(i%2==0?150:-130));
   Color c=i%3==0?new Color(1,.63f,.16f):i%3==1?new Color(.23f,.66f,.55f):new Color(1,.82f,.36f);c.a=Mathf.Clamp01((1-life)*2);pieces[i].color=c;
  }
 }
 // 외부 음원 없이 부드러운 상승 화음과 하강음을 생성한다.
 static AudioClip MakeSound(bool win){
  const int rate=44100;float duration=win?1.25f:.75f;var samples=new float[(int)(rate*duration)];
  float[] notes=win?new[]{523.25f,659.25f,783.99f,1046.5f}:new[]{330f,261.63f,196f};
  for(int i=0;i<samples.Length;i++){
   float t=i/(float)rate;float value=0;
   for(int n=0;n<notes.Length;n++){
    float age=t-n*(win?.14f:.16f);float length=win?.65f:.38f;if(age<0||age>length)continue;
    float env=Mathf.Min(1,age/.012f)*Mathf.Pow(1-age/length,2);
    value+=(Mathf.Sin(age*notes[n]*Mathf.PI*2)+.18f*Mathf.Sin(age*notes[n]*Mathf.PI*4))*env*.25f;
   }samples[i]=Mathf.Clamp(value,-.8f,.8f);
  }
  var clip=AudioClip.Create(win?"사다리 당첨 화음":"사다리 실패 하강음",samples.Length,1,rate,false);clip.SetData(samples,0);return clip;
 }
 void OnDisable(){if(speaker!=null)speaker.Stop();Clear();}
 void OnDestroy(){if(winSound!=null)Destroy(winSound);if(loseSound!=null)Destroy(loseSound);}
}
}
