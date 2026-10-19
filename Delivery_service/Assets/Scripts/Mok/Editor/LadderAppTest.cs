#if UNITY_EDITOR
using System;using System.Collections;using System.Linq;using System.Reflection;using UnityEngine;using DeliveryService.Mok.Navigation;
namespace DeliveryService.Mok.Editor {
public static class LadderAppTest {
const BindingFlags F=BindingFlags.NonPublic|BindingFlags.Instance;
static void Seed(LadderGame g,int seed)=>typeof(LadderGame).GetField("random",F).SetValue(g,new System.Random(seed));
public static IEnumerator RunCore(Action<string> log){
void Check(bool ok,string s){if(!ok)throw new Exception(s);log("PASS: "+s);}
var money=MoneyManager.Instance;money.currentMoney=20000;money.ResetDailyStats();
var game=LadderGame.Current;var loan=LoanAccount.Current;loan.Borrow(10000,1,out _);int principal=loan.Principal;
for(int mask=0;mask<256;mask++){var bridges=Enumerable.Range(0,8).Select(i=>(mask>>i)&1).ToArray();var ends=Enumerable.Range(0,3).Select(i=>LadderGame.Trace(i,bridges)).ToArray();if(ends.Distinct().Count()!=3)throw new Exception("중복 도착점");}
Check(true,"256가지 사다리 조합 모두 출발선-도착점 일대일 대응");
Check(LadderGame.MaxBet(1999)==1000&&LadderGame.MaxBet(-100)==0,"현금 한도와 1000원 단위 계산");
var phone=UnityEngine.Object.FindFirstObjectByType<PhoneController>();phone.TogglePhone();yield return null;
var planner=UnityEngine.Object.FindFirstObjectByType<NavigationPlanner>();planner.Show(NavigationPlanner.Screen.Home);
planner.MoveSelection(1);planner.MoveSelection(2);planner.MoveSelection(2);Check(planner.Selection==5,"방향키로 여섯 번째 사다리 앱 선택");
ScreenCapture.CaptureScreenshot("Temp/Mok-ladder-home.png");yield return null;planner.Confirm();
Check(planner.CurrentScreen==NavigationPlanner.Screen.Ladder,"사다리 앱 진입");var app=UnityEngine.Object.FindFirstObjectByType<LadderPhoneApp>();
Check(app.Bet==1000,"최초 베팅 1000원");app.Move(1,false);app.Move(1,false);Check(app.Bet==3000,"위 방향키 1000원씩 증가");app.Move(-1,false);Check(app.Bet==2000,"아래 방향키 1000원 감소");
int cash=money.currentMoney;app.Confirm();Check(app.CurrentPage==LadderPhoneApp.Page.Choose&&money.currentMoney==cash,"금액 확인 단계는 미차감");app.Move(1,true);Check(app.Choice==1,"오른쪽 방향키 출발선 선택");
Check(app.GetComponentInChildren<LadderGraphic>().Reveal==false,"선택 전 경로 비공개");Check(app.Back()&&money.currentMoney==cash,"선택 취소 무손실");
app.Confirm();app.Move(1,true);Seed(game,12);app.Confirm();Check(game.Revealing&&money.currentMoney==cash-2000,"시작 확정 시 한 번 차감");
var bridgesRef=game.Bridges;app.Confirm();app.Move(1,true);Check(game.Choice==1&&game.Bridges==bridgesRef&&money.currentMoney==cash-2000,"연출 중 연타로 선택 변경/중복 차감 불가");
Check(!game.StartRound(1000,0,out _),"진행 중 두 번째 판 차단");
float until=Time.unscaledTime+1.2f;while(Time.unscaledTime<until)yield return null;ScreenCapture.CaptureScreenshot("Temp/Mok-ladder-reveal.png");yield return null;
planner.Show(NavigationPlanner.Screen.Home);until=Time.unscaledTime+4;while(game.Revealing&&Time.unscaledTime<until)yield return null;
Check(game.HasResult&&!game.Revealing,"앱을 닫아도 자동 정산 완료");
int expected=cash-2000+(game.Won?4000:0);Check(money.currentMoney==expected,"실제 경로 결과와 지급액 일치");game.CompleteRound();Check(money.currentMoney==expected,"정산 재호출 중복 지급 방지");
planner.Show(NavigationPlanner.Screen.Ladder);Check(app.CurrentPage==LadderPhoneApp.Page.Result&&ReferenceEquals(bridgesRef,game.Bridges),"재진입 시 동일 결과 보존");yield return null;ScreenCapture.CaptureScreenshot("Temp/Mok-ladder-result.png");yield return null;
Check(loan.Principal==principal,"대출 원금 유지");
// 동일 난수의 당첨 도착점과 경로를 이용해 승리/패배 정산을 각각 확인한다.
int seed=37;var rng=new System.Random(seed);int winner=rng.Next(3);var layout=Enumerable.Range(0,8).Select(_=>rng.Next(2)).ToArray();int winStart=Enumerable.Range(0,3).Single(x=>LadderGame.Trace(x,layout)==winner);
Seed(game,seed);cash=money.currentMoney;Check(game.StartRound(5000,winStart,out _),"당첨 판 시작");game.CompleteRound();Check(game.Won&&game.Prize==10000&&money.currentMoney==cash+5000,"5000 베팅 당첨 10000 반환, 순이익 5000");
app.Open();yield return null;var fx=app.GetComponent<LadderResultFeedback>();int count=fx.PlayCount;Check(fx.LastWon,"당첨 연출 구분");app.Refresh();app.Open();Check(fx.PlayCount==count,"동일 결과 연출 중복 방지");
float effectUntil=Time.unscaledTime+.22f;while(Time.unscaledTime<effectUntil)yield return null;ScreenCapture.CaptureScreenshot("Temp/Mok-ladder-win-feedback.png");yield return null;
Seed(game,seed);cash=money.currentMoney;Check(game.StartRound(5000,(winStart+1)%3,out _),"낙첨 판 시작");game.CompleteRound();Check(!game.Won&&money.currentMoney==cash-5000,"낙첨은 베팅액 전액 손실");
app.Open();yield return null;Check(!fx.LastWon&&fx.PlayCount==count+1,"새 꽝 결과 실패 효과 재생");
var audio=app.GetComponent<AudioSource>();Check(audio!=null&&audio.isPlaying,"결과 효과음 재생 상태");
effectUntil=Time.unscaledTime+.08f;while(Time.unscaledTime<effectUntil)yield return null;ScreenCapture.CaptureScreenshot("Temp/Mok-ladder-lose-feedback.png");yield return null;
Check(!game.StartRound(0,0,out _)&&!game.StartRound(-1000,0,out _)&&!game.StartRound(1500,0,out _)&&!game.StartRound(1000,3,out _),"잘못된 금액과 선택 차단");
money.currentMoney=999;Check(!game.StartRound(1000,0,out _),"잔액 부족 베팅 차단");
money.currentMoney=20000;Seed(game,seed);Check(game.StartRound(5000,winStart,out _),"하루 경계 정산 준비");
var cycle=UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();cycle.currentDay=1;cycle.timeMultiplier=0;cycle.fadeDuration=.05f;
cycle.StartCoroutine((IEnumerator)typeof(DayNightCycle).GetMethod("TransitionToNextDay",F).Invoke(cycle,null));until=Time.unscaledTime+5;while(cycle.currentDay==1&&Time.unscaledTime<until)yield return null;
Check(cycle.currentDay==2&&game.HasResult&&!game.Revealing&&money.currentMoney==23000,"다음 날 전환: 상금 정산 후 대출 이자 2000원 청구");
var bike=UnityEngine.Object.FindFirstObjectByType<MotorcycleController>();var player=UnityEngine.Object.FindFirstObjectByType<PlayerCombat>();bike.EnterBike(player.gameObject);planner.Show(NavigationPlanner.Screen.Ladder);Check(planner.CurrentScreen==NavigationPlanner.Screen.Map,"오토바이에서는 네비게이션만 표시");bike.ExitBike();
}
}}
#endif
