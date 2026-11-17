using UnityEngine;
namespace DeliveryService.Mok {
// 판정과 독립된 결투 표시. 화면 중앙에는 체력판이나 타이밍 계기를 두지 않는다.
public sealed class PrisonDuelHUD : MonoBehaviour {
 public PrisonMedicineDuel duel;
 Texture2D plate;float delayedHealth,damageHold;int previousHealth;bool showing;
 void Update(){if(!duel||!duel.Active){showing=false;return;}if(!showing){previousHealth=duel.BossHealth;delayedHealth=previousHealth;showing=true;}if(Time.timeScale<=0)return;if(duel.BossHealth>previousHealth)delayedHealth=duel.BossHealth;if(duel.BossHealth<previousHealth)damageHold=.24f;previousHealth=duel.BossHealth;damageHold-=Time.unscaledDeltaTime;if(damageHold<=0)delayedHealth=Mathf.MoveTowards(delayedHealth,duel.BossHealth,duel.settings.bossHealth*Time.unscaledDeltaTime*1.2f);}
 void Init(){if(plate)return;plate=new Texture2D(128,32,TextureFormat.RGBA32,false){name="결투 HUD 모서리",filterMode=FilterMode.Bilinear};var pixels=new Color[4096];for(int y=0;y<32;y++)for(int x=0;x<128;x++){int edge=Mathf.Min(x,127-x);int vertical=Mathf.Min(y,31-y);pixels[y*128+x]=edge+vertical<6?Color.clear:Color.white;}plate.SetPixels(pixels);plate.Apply();}
 void OnGUI(){if(!duel||!duel.Active||duel.Current==PrisonMedicineDuel.Phase.Inactive||duel.Retrying||Time.timeScale<=0)return;Init();var old=GUI.color;float s=Screen.height/900f;float w=Mathf.Min(520*s,Screen.width*.52f),x=(Screen.width-w)*.5f;var ink=new Color(.06f,.075f,.09f,.9f);var cream=new Color(1,.94f,.82f);var orange=new Color(.93f,.38f,.15f);GUIStyle Style(int size,TextAnchor alignment){var t=new GUIStyle(GUI.skin.label){font=MokUIFont.Font,fontSize=Mathf.RoundToInt(size*s),alignment=alignment};return t;}
 void Label(Rect r,string text,int size,Color color,TextAnchor align=TextAnchor.MiddleLeft){GUI.color=color;GUI.Label(r,text,Style(size,align));GUI.color=Color.white;}
 void Fill(Rect r,Color color,bool clipped=false){GUI.color=color;GUI.DrawTexture(r,clipped?plate:Texture2D.whiteTexture);GUI.color=Color.white;}
 Fill(new Rect(x-18*s,26*s,w+36*s,86*s),ink,true);
 Label(new Rect(x,30*s,w,22*s),"수감동  /  맨주먹 결투",12,new Color(.67f,.7f,.69f));
 Label(new Rect(x,49*s,w,31*s),duel.settings.bossName,26,cream);
 Label(new Rect(x,51*s,w,26*s),duel.BossHealth<=duel.settings.bossHealth/2?"분노":"운동장의 주먹",14,orange,TextAnchor.MiddleRight);
 Fill(new Rect(x,88*s,w,8*s),new Color(.2f,.21f,.22f));
 Fill(new Rect(x,88*s,w*Mathf.Clamp01(delayedHealth/duel.settings.bossHealth),8*s),new Color(1,.8f,.48f));
 Fill(new Rect(x,88*s,w*Mathf.Clamp01((float)duel.BossHealth/duel.settings.bossHealth),8*s),orange);
 for(int i=1;i<4;i++)Fill(new Rect(x+w*i/4,88*s,2*s,8*s),ink);
 float bottom=Screen.height-82*s;Label(new Rect(38*s,bottom-29*s,180*s,25*s),"버틸 수 있는 체력",14,cream);
 int count=Mathf.CeilToInt((float)duel.settings.playerHealth/duel.settings.enemyDamage);count=Mathf.Clamp(count,1,12);
 for(int i=0;i<count;i++){float remaining=Mathf.Clamp01((float)duel.PlayerHealth/duel.settings.enemyDamage-i);Fill(new Rect((38+i*32)*s,bottom,26*s,12*s),ink,true);if(remaining>0)Fill(new Rect((40+i*32)*s,bottom+2*s,22*s*remaining,8*s),new Color(.77f,.86f,.73f),true);}
 if(duel.Current==PrisonMedicineDuel.Phase.Ready){float alpha=Mathf.Clamp01((2-duel.Age)*2);Label(new Rect(Screen.width*.23f,Screen.height*.76f,Screen.width*.54f,50*s),"← Q       회피       R →",25,new Color(cream.r,cream.g,cream.b,alpha),TextAnchor.MiddleCenter);}
 string message="";Color highlight=cream;
 if(duel.SlowRemaining>0)DrawPerfect(s);
 else if(duel.Current==PrisonMedicineDuel.Phase.Won)message="김정민 제압";
 else if(duel.Current==PrisonMedicineDuel.Phase.Lost)message="다시 일어설 시간이다\nEnter  ·  재도전";
 if(message!="")Label(new Rect(Screen.width*.3f,Screen.height*.69f,Screen.width*.4f,78*s),message,duel.Current==PrisonMedicineDuel.Phase.Lost?23:21,highlight,TextAnchor.MiddleCenter);
 if(duel.HurtFlash>0){var red=new Color(.7f,.04f,.02f,duel.HurtFlash*.2f);Fill(new Rect(0,0,Screen.width,Screen.height),red);}GUI.color=old;
 }
 // SlowRemaining은 실제 시간 기준이며 일시정지 시 멈춘다. 극저속 중에도 글자는 경쾌하게 튀어나온다.
 void DrawPerfect(float scale){
  float duration=Mathf.Max(.01f,duel.settings.slowSeconds);
  float progress=Mathf.Clamp01(1-duel.SlowRemaining/duration);
  float pop=progress<.14f?Mathf.Lerp(.88f,1.18f,Mathf.SmoothStep(0,1,progress/.14f))
   :Mathf.Lerp(1.18f,1,Mathf.SmoothStep(0,1,(progress-.14f)/.2f));
  float fade=Mathf.SmoothStep(0,1,Mathf.Clamp01((progress-.58f)/.42f));
  float alpha=Mathf.SmoothStep(0,1,Mathf.Clamp01(progress/.06f))*(1-fade);
  var style=new GUIStyle(GUI.skin.label){font=MokUIFont.Font,fontSize=Mathf.RoundToInt(27*scale*pop),alignment=TextAnchor.MiddleCenter};
  style.normal.textColor=Color.white;
  var rect=new Rect(Screen.width*.3f,Screen.height*.69f-fade*12*scale,Screen.width*.4f,78*scale);
  var old=GUI.color;
  GUI.color=new Color(.025f,.055f,.07f,.75f*alpha);
  GUI.Label(new Rect(rect.x,rect.y+2*scale,rect.width,rect.height),"PERFECT",style);
  GUI.color=new Color(.75f,.94f,1,alpha);GUI.Label(rect,"PERFECT",style);
  GUI.color=old;
 }
 void OnDestroy(){if(plate)Destroy(plate);}
}
}
