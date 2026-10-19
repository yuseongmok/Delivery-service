#if UNITY_EDITOR
using System;using System.Collections;using System.Reflection;using System.Linq;using UnityEngine;using UnityEngine.UI;using DeliveryService.Mok.Navigation;using DeliveryService.Mok.ShopUI;
namespace DeliveryService.Mok.Editor {
public static class LoanAppTest {
public static IEnumerator RunCore(Action<string> log){
void Check(bool ok,string text){if(!ok)throw new Exception(text);log("PASS: "+text);}
var money=MoneyManager.Instance;money.currentMoney=20000;money.ResetDailyStats();
var cycle=UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();cycle.currentDay=1;cycle.timeMultiplier=0;cycle.isShopOpen=true;
var ledger=UnityEngine.Object.FindFirstObjectByType<ShopDayLedger>();ledger.BeginDay();
var loan=LoanAccount.Current;Check(loan.Principal==0&&loan.Available==50000,"새 세션 원금 0, 한도 5만");
var phone=UnityEngine.Object.FindFirstObjectByType<PhoneController>();phone.TogglePhone();yield return null;
var planner=UnityEngine.Object.FindFirstObjectByType<NavigationPlanner>();planner.Show(NavigationPlanner.Screen.Home);planner.MoveSelection(4);planner.Confirm();
Check(planner.CurrentScreen==NavigationPlanner.Screen.Loans,"홈 방향키 선택에서 급전 앱 진입");var app=UnityEngine.Object.FindFirstObjectByType<LoanPhoneApp>();Check(app!=null,"대출 앱 활성화");
app.Confirm();app.Move(1);app.Confirm();Check(app.CurrentPage==LoanPhoneApp.Page.Confirm&&loan.Principal==0,"3만 대출 확인 전 미입금");
yield return null;ScreenCapture.CaptureScreenshot("Temp/Mok-loan-confirm.png");yield return null;
app.Move(1);app.Confirm();Check(loan.Principal==0,"확인 화면 취소 시 거래 없음");app.Move(1);app.Confirm();app.Confirm();
Check(loan.Principal==30000&&money.currentMoney==50000&&loan.NextInterest==6000,"3만 대출 및 일일 20% 이자 표시");
Check(money.dailyIncome==0&&ledger.Income==0,"대출 원금은 매출 제외");
app.Move(1);app.Confirm();app.Move(1);app.Confirm();app.Confirm();Check(loan.Principal==20000&&money.currentMoney==40000&&loan.NextInterest==4000,"앱에서 원금 1만 상환");
Check(money.dailyTotalExpense==0&&ledger.Expense==0,"원금 상환은 비용 제외");
cycle.isShopOpen=false;int charged=loan.SettleDay(1);Check(charged==4000&&money.currentMoney==36000&&loan.Principal==20000,"마감 후 이자 차감, 원금 유지");
Check(ledger.Interest==4000&&ledger.Expense==4000&&money.dailyTotalExpense==4000,"마감 상태에서도 이자만 지출 통계 반영");
Check(loan.ForDay(1).Borrowed==30000&&loan.ForDay(1).Repaid==10000&&loan.ForDay(1).Interest==4000,"대출/상환/이자 별도 일일 내역");
Check(loan.SettleDay(1)==0&&money.currentMoney==36000,"중복 일일 이자 방지");
cycle.currentDay=2;Check(loan.Borrow(30000,2,out _)&&loan.Principal==50000,"누적 한도까지 추가 대출");Check(!loan.Borrow(10000,2,out _)&&loan.Principal==50000,"5만 원금 한도 초과 차단");
cycle.fadeDuration=.05f;
cycle.StartCoroutine((IEnumerator)typeof(DayNightCycle).GetMethod("TransitionToNextDay",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(cycle,null));
float deadline=Time.unscaledTime+5;while(cycle.currentDay==2&&Time.unscaledTime<deadline)yield return null;
Check(cycle.currentDay==3&&loan.LastSettledDay==2&&money.currentMoney==56000,"실제 다음 날 전환에서 이자 1만원 자동 청구");
Check(loan.Repay(50000,3,out _)&&loan.Principal==0&&loan.SettleDay(3)==0,"전액 상환 후 이자 없음");
cycle.currentDay=4;Check(loan.Borrow(10000,4,out _),"재대출 가능");money.currentMoney=500;Check(!loan.Repay(1000,4,out _)&&loan.Principal==10000,"현금 부족 상환 차단");
Check(!loan.Borrow(-1,4,out _)&&!loan.Repay(-1,4,out _),"음수 거래 차단");
planner.Show(NavigationPlanner.Screen.Home);planner.MoveSelection(2);planner.MoveSelection(2);Check(planner.Selection==4,"홈 아래 방향키로 급전 선택");planner.MoveSelection(2);Check(planner.Selection==0,"마지막 행에서 같은 열로 순환");planner.MoveSelection(4);planner.Confirm();Check(app.CurrentPage==LoanPhoneApp.Page.Home,"앱 재진입 확인 상태 초기화");
var bike=UnityEngine.Object.FindFirstObjectByType<MotorcycleController>();var player=UnityEngine.Object.FindFirstObjectByType<PlayerCombat>();Check(bike.EnterBike(player.gameObject),"탑승 준비");planner.Show(NavigationPlanner.Screen.Loans);Check(planner.CurrentScreen==NavigationPlanner.Screen.Map,"오토바이에서는 대출 앱 차단");bike.ExitBike();
Check(loan.SettleDay(4)==2000&&money.currentMoney==-1500,"현금 부족해도 이자 차감");
Check(BankruptcyGameOver.TryEndDay(cycle),"이자 차감 후 적자 파산 연동");
}
}}
#endif
