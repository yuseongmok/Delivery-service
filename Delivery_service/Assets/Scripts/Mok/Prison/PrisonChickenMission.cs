using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI;
namespace DeliveryService.Mok {
 public sealed class PrisonChickenMission:MonoBehaviour {
  public enum Stage { Waiting, Kitchen, Cook, Carry, Fight, Deliver, Conversation, Complete }
  public PrisonMission prison;
  public PrisonChickenText dialogue;
  public ChickenCooking cooking;
  public ChickenInteraction entrance,station;
  public ChickenInteraction[] ingredients;
  public Transform checkpoint,chef,chefView,cookView,ambushPoint;
  public PrisonChefReaction chefReaction;
  public Camera camera;
  public GameObject carrying;
  public ChickenAmbusher[] enemies;
  public Stage State{get;private set;}
  public bool Active=>State!=Stage.Waiting;
  public bool CookingActive=>State==Stage.Cook&&!Failing;
  public bool Failing{get;private set;}
  public int Noise{get;private set;}
  public int Hits{get;private set;}
  public float Remaining{get;private set;}
  public float Fade{get;private set;}
  public string Subtitle{get;private set;}="";
  public readonly bool[] Collected=new bool[5];
  public string Objective=>Failing?"주방 입구에서 다시 준비합니다":State switch {
   Stage.Kitchen=>"갓 튀긴 치킨 · 주방으로 가기",
   Stage.Cook=>Collected.All(v=>v)?"갓 튀긴 치킨 · 조리대에서 치킨 만들기":"갓 튀긴 치킨 · 재료 모으기 ("+Collected.Count(v=>v)+"/5)",
   Stage.Carry=>"갓 튀긴 치킨 · 최형욱에게 전달하기",
   Stage.Fight=>"치킨을 지키세요 · 죄수들을 제압하기",
   Stage.Deliver=>"최형욱에게 치킨 전달하기",
   Stage.Conversation=>"최형욱과 정보 거래",
   Stage.Complete=>"갓 튀긴 치킨 완료 · 다음 단서: 작업실의 죄수",_=>""
  };
  public Transform Target=>State==Stage.Kitchen?entrance.transform:State==Stage.Cook?(Collected.All(v=>v)?station.transform:ingredients[System.Array.FindIndex(Collected,v=>!v)].transform):State==Stage.Carry||State==Stage.Deliver?prison.introduction.informant:null;
  PlayerCombat Player=>prison.player;
  float nextNoise,noticeUntil,nextHit;
  bool wasMoving,wasLooking,wasInteraction,wasPhone;
  bool locked;
  Quaternion chefRest;
  void Start(){chefRest=chef.localRotation;carrying.SetActive(false);foreach(var e in enemies)e.gameObject.SetActive(false);}
  void Update(){
   if(Time.timeScale<=0)return;
   if(State==Stage.Waiting){if(prison.introduction.State==PrisonIntroduction.Stage.Complete){State=Stage.Kitchen;Say(dialogue.start,8);}return;}
   if(Time.time>noticeUntil&&State!=Stage.Conversation&&!Failing)Subtitle="";
   if(CookingActive){Remaining=Mathf.Max(0,Remaining-Time.deltaTime);if(!chefReaction)chef.localRotation=chefRest*Quaternion.Euler(Mathf.Sin(Time.time)*3,0,0);if(Remaining==0)Fail(false);}
   if(State==Stage.Carry&&(Vector3.Distance(Player.transform.position,ambushPoint.position)<5||Vector3.Distance(Player.transform.position,prison.introduction.informant.position)<7))StartAmbush();
   if(State==Stage.Fight&&enemies.All(e=>!e||!e.combat||e.combat.Health<=0||e.combat.Npc.IsDead)){State=Stage.Deliver;Say(dialogue.fightCleared,6);}
   if(State==Stage.Deliver&&!Failing&&Player.CanAct&&Vector3.Distance(Player.transform.position,prison.introduction.informant.position)<3.2f&&Keyboard.current!=null&&Keyboard.current.fKey.wasPressedThisFrame)StartCoroutine(Deliver());
  }
  public void Say(string text,float seconds=4){Subtitle=text;noticeUntil=Time.time+seconds;}
  public bool Use(ChickenInteraction point){
   if(!Active||Failing||!Player.CanAct||Vector3.Distance(Player.transform.position,point.transform.position)>3.6f)return false;
   if(point.entrance&&State==Stage.Kitchen){ResetKitchen();return true;}
   if(!CookingActive)return false;
   if(point.ingredient>=0){Collected[point.ingredient]=true;point.gameObject.SetActive(false);PrisonAudio.Clink(point.transform.position,.1f);return true;}
   if(point==station){if(!Collected.All(v=>v)){Say("재료 다섯 가지를 먼저 준비하자.");return false;}Lock(true,cookView);cooking.Begin();return true;}
   return false;
  }
  public void ResetKitchen(){
   State=Stage.Cook;Noise=Hits=0;Remaining=dialogue.kitchenSeconds;nextNoise=Time.time+1;nextHit=0;
   for(int i=0;i<5;i++){Collected[i]=false;ingredients[i].gameObject.SetActive(true);}
   cooking.ResetRecipe();carrying.SetActive(false);chef.localRotation=chefRest;if(chefReaction)chefReaction.Sleep();
   prison.introduction.mingi.Following=false;
   foreach(var enemy in enemies)enemy.gameObject.SetActive(false);
   Say(dialogue.start,7);
  }
  public void DiscardIngredient(int index){Collected[index]=false;ingredients[index].gameObject.SetActive(true);Say("정량 초과 · "+ingredients[index].name+" 다시 가져오기");}
  public void LeaveCooking(){cooking.Close();Lock(false,null);}
  public void FinishCooking(){LeaveCooking();State=Stage.Carry;carrying.SetActive(true);prison.introduction.mingi.Following=true;Say(dialogue.cooked);}
  public void MakeNoise(bool oil){
   if(!CookingActive||Time.time<nextNoise)return;nextNoise=Time.time+1;Noise++;PrisonAudio.Clink(Player.transform.position,.65f);Say("주방장이 뒤척인다… 소음 "+Noise+"/3",3);
   if(oil){var rig=Player.GetComponentInChildren<PlayerRagdoll>();if(rig)rig.Launch(Player.transform.forward*2+Vector3.up*1.2f,2.2f);}
   if(Noise>=3)Fail(false);
  }
  public void StartAmbush(){
   if(State!=Stage.Carry)return;State=Stage.Fight;Say(dialogue.ambush,6);
   foreach(var enemy in enemies){enemy.gameObject.SetActive(true);enemy.Begin();}
  }
  public void Hit(Vector3 direction){
   if(State!=Stage.Fight||Failing||Time.time<nextHit||DeveloperCheats.Invincible)return;
   var rig=Player.GetComponentInChildren<PlayerRagdoll>();if(rig&&rig.IsActive)return;
   nextHit=Time.time+3.5f;Hits++;if(rig)rig.Launch(direction*2+Vector3.up,2.2f);if(Hits>=3)Fail(true);
  }
  public void Fail(bool fight){if(Failing)return;StartCoroutine(Retry(fight));}
  IEnumerator Retry(bool fight){
   Failing=true;cooking.Close();foreach(var e in enemies)if(e)e.Hold=true;
   var rig=Player.GetComponentInChildren<PlayerRagdoll>();while(rig&&rig.IsActive)yield return null;
   Lock(true,chefView);if(!fight&&chefReaction)yield return chefReaction.Wake(camera);Say(fight?dialogue.defeated:dialogue.caught,10);
   yield return new WaitForSeconds(2);
   yield return Blackout(1);
   var cc=Player.GetComponent<CharacterController>();cc.enabled=false;Player.transform.SetPositionAndRotation(checkpoint.position,Quaternion.Euler(0,checkpoint.eulerAngles.y,0));cc.enabled=true;
   foreach(var e in enemies)if(e)e.ResetActor();
   ResetKitchen();Lock(false,null);yield return Blackout(0);Failing=false;
  }
  IEnumerator Blackout(float target){float from=Fade;for(float t=0;t<.55f;t+=Time.deltaTime){Fade=Mathf.Lerp(from,target,t/.55f);yield return null;}Fade=target;}
  IEnumerator Deliver(){
   State=Stage.Conversation;carrying.SetActive(false);prison.introduction.mingi.Following=false;
   Lock(true,prison.introduction.portraitCamera);
   var meeting=prison.introduction.meetingPresentation;if(meeting){meeting.Begin(camera);yield return new WaitForSeconds(1.2f);}
   foreach(var line in dialogue.delivery){Subtitle=line;if(meeting)meeting.ShowLine(line);for(float t=0;t<dialogue.lineSeconds;t+=Time.deltaTime){if(Time.timeScale>0&&Keyboard.current!=null&&Keyboard.current.spaceKey.wasPressedThisFrame)break;yield return null;}}
   if(meeting)meeting.End();
   Subtitle="";Lock(false,null);State=Stage.Complete;prison.introduction.mingi.Following=true;
  }
  void Lock(bool value,Transform shot){
   var move=Player.GetComponent<PlayerMove>();var look=Player.GetComponentInChildren<CameraMove>();var interaction=Player.GetComponent<PlayerInteraction>();var phone=Player.GetComponentInChildren<PhoneController>(true);
   if(value&&!locked){wasMoving=move.isControllable;wasLooking=look.isControllable;wasInteraction=interaction.enabled;wasPhone=phone&&phone.enabled;}
   if(!value&&!locked)return;locked=value;
   move.isControllable=value?false:wasMoving;look.isControllable=value?false:wasLooking;interaction.enabled=value?false:wasInteraction;
   if(phone)phone.enabled=value?false:wasPhone;Player.View.enabled=!value;camera.enabled=value;camera.fieldOfView=55;if(shot)camera.transform.SetPositionAndRotation(shot.position,shot.rotation);
   Player.View.GetComponent<AudioListener>().enabled=!value;camera.GetComponent<AudioListener>().enabled=value;
   Cursor.lockState=value?CursorLockMode.None:CursorLockMode.Locked;Cursor.visible=value;
  }
  void OnGUI(){
   if(!Active||Time.timeScale<=0)return;var old=GUI.color;
   var style=new GUIStyle(GUI.skin.label){font=MokUIFont.Font,fontSize=Mathf.RoundToInt(Screen.height*.025f),alignment=TextAnchor.MiddleCenter,wordWrap=true};
   if(CookingActive){GUI.Label(new Rect(Screen.width*.25f,Screen.height*.17f,Screen.width*.5f,Screen.height*.065f),$"주방장이 깨어나기까지 {Mathf.FloorToInt(Remaining/60):00}:{Mathf.FloorToInt(Remaining%60):00}   소음 {Noise}/3",style);}
   if(cooking.Open&&CookingActive){
    GUI.color=new Color(.03f,.07f,.09f,.92f);GUI.DrawTexture(new Rect(Screen.width*.16f,Screen.height*.69f,Screen.width*.68f,Screen.height*.27f),Texture2D.whiteTexture);GUI.color=Color.white;
    GUI.Label(new Rect(Screen.width*.18f,Screen.height*.70f,Screen.width*.64f,Screen.height*.08f),cooking.Instruction,style);
    GUI.Label(new Rect(Screen.width*.18f,Screen.height*.88f,Screen.width*.64f,Screen.height*.06f),cooking.Message+"   F · 조리대에서 나오기",style);
    if(cooking.Current==ChickenCooking.Step.Cut){GUI.color=new Color(1,.65f,.2f);GUI.Label(new Rect(cooking.CutScreenPosition.x-35,Screen.height-cooking.CutScreenPosition.y-35,70,70),"+",style);}
    else {float progress=cooking.Current==ChickenCooking.Step.Heat?(cooking.Temperature-100)/110:cooking.Current==ChickenCooking.Step.Fry?cooking.Needle:cooking.Amount/1.2f;var r=new Rect(Screen.width*.28f,Screen.height*.82f,Screen.width*.44f,15);GUI.color=Color.gray;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=new Color(.3f,.8f,.4f);float min=cooking.Current==ChickenCooking.Step.Fry?.37f:cooking.Current==ChickenCooking.Step.Heat?77f/110:.92f/1.2f;float width=cooking.Current==ChickenCooking.Step.Fry?.26f:cooking.Current==ChickenCooking.Step.Heat?6f/110:.16f/1.2f;GUI.DrawTexture(new Rect(r.x+r.width*min,r.y,r.width*width,r.height),Texture2D.whiteTexture);GUI.color=Color.white;GUI.DrawTexture(new Rect(r.x+r.width*Mathf.Clamp01(progress),r.y-4,4,r.height+8),Texture2D.whiteTexture);if(cooking.Current==ChickenCooking.Step.Heat)GUI.Label(new Rect(r.x,Screen.height*.77f,r.width,Screen.height*.04f),cooking.Temperature.ToString("0")+"°C",style);}
   }
   if(Fade>0){GUI.color=new Color(0,0,0,Fade);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);}GUI.color=old;
  }
 }
}

