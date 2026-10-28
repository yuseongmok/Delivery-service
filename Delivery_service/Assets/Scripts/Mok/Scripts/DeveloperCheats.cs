using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
namespace DeliveryService.Mok {
// 발표 빌드에서도 사용한다. 활성 상태는 실행 세션에만 유지하고 저장하지 않는다.
[DefaultExecutionOrder(-10000)]
public sealed class DeveloperCheats : MonoBehaviour {
 public static DeveloperCheats Current{get;private set;}
 public static bool Invincible{get;private set;}
 public bool IsOpen{get;private set;}
 float previousScale;CursorLockMode previousLock;bool previousCursor;int selection;bool confirmSkip;string confirmationTarget,message="";BankPrologue bank;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset(){Current=null;Invincible=false;}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){if(!Current)new GameObject("Presentation developer cheats").AddComponent<DeveloperCheats>();}
 void Awake(){if(Current&&Current!=this){Destroy(gameObject);return;}Current=this;DontDestroyOnLoad(gameObject);SceneManager.sceneLoaded+=Loaded;bank=FindFirstObjectByType<BankPrologue>();}
 void Loaded(Scene scene,LoadSceneMode mode){Close();bank=FindFirstObjectByType<BankPrologue>();confirmSkip=false;}
 void OnDestroy(){if(Current!=this)return;Close();SceneManager.sceneLoaded-=Loaded;Current=null;}
 public void Open(){if(IsOpen)return;previousScale=Time.timeScale;previousLock=Cursor.lockState;previousCursor=Cursor.visible;Time.timeScale=0;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;IsOpen=true;selection=0;confirmSkip=false;}
 public void Close(){if(!IsOpen)return;IsOpen=false;Time.timeScale=previousScale;Cursor.lockState=previousLock;Cursor.visible=previousCursor;confirmSkip=false;}
 public static void SetInvincible(bool value){Invincible=value;}
 public bool GrantMoney(int amount){var money=MoneyManager.Instance;if(!money||amount<=0)return false;int added=(int)System.Math.Min((long)amount,(long)int.MaxValue-money.currentMoney);if(added<=0)return false;money.AddMoney(added);message=added.ToString("N0")+"원 지급";return true;}
 int Target(){
  if(bank&&bank.Current!=BankPrologue.Phase.Complete)return 0;
  if(StoryTutorial.Active)return 1;
  if(VenueFinale.Active)return 6;if(MechanicMission.Active)return 5;if(BackdoorMission.Active)return 4;if(SpecialOrderMission.Active)return 3;if(BrokerMission.Current&&BrokerMission.Current.Running)return 2;
  if(BrokerMission.Current&&!BrokerMission.Current.Completed)return 2;
  if(SpecialOrderMission.Current&&!SpecialOrderMission.Current.Completed)return 3;
  if(BackdoorMission.Current&&!BackdoorMission.Current.Completed)return 4;
  if(MechanicMission.Current&&!MechanicMission.Current.Completed)return 5;
  if(VenueFinale.Current&&!VenueFinale.Current.Completed)return 6;return -1;
 }
 public string SkipTarget{get{int target=Target();return target<0?"스킵할 미션 없음":new[]{"프롤로그 → 피자 가게 튜토리얼","첫 배달 튜토리얼","연락이 끊긴 중개인","특별 주문","도시의 뒷문","빚진 정비사","첫 번째 사건 · 마지막 배달"}[target];}}
 public bool SkipMission(){
  var p=BrokerMission.Current?BrokerMission.Current.player:null;var day=BrokerMission.Current?BrokerMission.Current.day:null;
  if((p&&(p.IsDown||p.WaterRescueActive||(p.avatar&&p.avatar.IsPhysical)))||PoliceControl.IsArrestInProgress||(day&&day.IsTransitioning)||(VenueFinale.Active&&VenueFinale.Current.State==VenueFinale.Phase.Scheduling)){message="기절·구조·체포·날짜 전환이 끝난 뒤 사용하세요.";return false;}
  int target=Target();string title=SkipTarget;if(target<0)return false;Close();
  switch(target){case 0:if(!bank.DeveloperComplete()){message="본게임 씬이 빌드에 포함되어 있는지 확인하세요.";return false;}break;
   case 1:StoryTutorial.Current.DeveloperComplete();break;case 2:BrokerMission.Current.DeveloperComplete();break;case 3:SpecialOrderMission.Current.DeveloperComplete();break;case 4:BackdoorMission.Current.DeveloperComplete();break;case 5:MechanicMission.Current.DeveloperComplete();break;case 6:VenueFinale.Current.DeveloperComplete();break;}
  message=title+" 완료 처리";return true;
 }
 public bool StartFinalDelivery(){
  var finale=VenueFinale.Current;var broker=BrokerMission.Current;
  if(!finale||!broker){message="본게임 도시에서 사용할 수 있습니다.";return false;}
  var player=broker.player;
  if(player.IsDown||player.WaterRescueActive||(player.avatar&&player.avatar.IsPhysical)||PoliceControl.IsArrestInProgress||broker.day.IsTransitioning||finale.State==VenueFinale.Phase.Scheduling){message="기절·구조·체포·날짜 전환이 끝난 뒤 사용하세요.";return false;}
  Close();if(player.RiddenBike)player.RiddenBike.ExitBike();if(StoryTutorial.Active)StoryTutorial.Current.DeveloperComplete();
  broker.orders.ResetOrdersForNewDay();broker.DeveloperComplete();SpecialOrderMission.Current.DeveloperComplete();BackdoorMission.Current.DeveloperComplete();MechanicMission.Current.DeveloperComplete();
  bool started=finale.DeveloperStart();message=started?"마지막 배달 미션 시작":"현재 상태에서는 미션을 시작할 수 없습니다.";return started;
 }
 void Update(){
  bool modifier=(PrologueKeyboard.Held(Key.LeftCtrl)||PrologueKeyboard.Held(Key.RightCtrl))&&(PrologueKeyboard.Held(Key.LeftShift)||PrologueKeyboard.Held(Key.RightShift));
  if(modifier&&PrologueKeyboard.Pressed(Key.F12)){if(IsOpen)Close();else Open();return;}
  if(!IsOpen)return;
  if(PrologueKeyboard.Pressed(Key.UpArrow))selection=(selection+5)%6;if(PrologueKeyboard.Pressed(Key.DownArrow))selection=(selection+1)%6;
  if(PrologueKeyboard.Pressed(Key.Enter)||PrologueKeyboard.Pressed(Key.NumpadEnter))Activate(selection);
 }
 void Activate(int index){
  if(index!=3)confirmSkip=false;
  if(index==0){if(!GrantMoney(100000))message="본게임에서 돈을 지급할 수 있습니다.";}
  else if(index==1){if(!GrantMoney(1000000))message="본게임에서 돈을 지급할 수 있습니다.";}
  else if(index==2){SetInvincible(!Invincible);message=Invincible?"무적 활성화":"무적 해제";}
  else if(index==3){if(!confirmSkip||confirmationTarget!=SkipTarget){confirmSkip=true;confirmationTarget=SkipTarget;return;}SkipMission();confirmSkip=false;}
  else if(index==4)StartFinalDelivery();
  else Close();
 }
 void OnGUI(){if(!IsOpen)return;float scale=Mathf.Clamp(Screen.height/900f,.6f,2f);var matrix=GUI.matrix;GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,Vector3.one*scale);float width=Screen.width/scale,height=Screen.height/scale;
  var oldFont=GUI.skin.font;GUI.skin.font=MokUIFont.Font;var label=new GUIStyle(GUI.skin.label){fontSize=21,wordWrap=true};var button=new GUIStyle(GUI.skin.button){fontSize=22};
  GUI.Box(new Rect((width-620)/2,(height-610)/2,620,610),"");GUILayout.BeginArea(new Rect((width-570)/2,(height-570)/2,570,570));
  GUILayout.Label("발표용 치트 · Ctrl + Shift + F12",label);GUILayout.Space(10);GUILayout.Label("스킵 대상: "+SkipTarget,label);
  GUILayout.Label("무적: "+(Invincible?"ON":"OFF")+" · 공격 피해와 피격 레그돌 방지",label);GUILayout.Space(14);
  string[] buttons={"+ 100,000원","+ 1,000,000원",Invincible?"무적 끄기":"무적 켜기",confirmSkip?"정말 완료 처리하기":"미션 스킵 (완료 처리)","마지막 배달 미션 바로 시작","닫기"};
  for(int i=0;i<buttons.Length;i++)if(GUILayout.Button((selection==i?"▶ ":"")+buttons[i],button,GUILayout.Height(48)))Activate(i);
  GUILayout.Space(10);GUILayout.Label(message,label);GUILayout.Label("↑↓ 선택 · Enter 실행 · 마우스 클릭 가능",label);GUILayout.EndArea();GUI.skin.font=oldFont;GUI.matrix=matrix;
 }
}}
