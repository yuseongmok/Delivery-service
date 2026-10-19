#if UNITY_EDITOR
using System;using System.Collections;using System.Reflection;using UnityEngine;using DeliveryService.Mok.ShopUI;
namespace DeliveryService.Mok.Editor {
public static class DailyRentTest {
public static IEnumerator RunCore(Action<string> log){
 void Check(bool ok,string text){if(!ok)throw new Exception(text);log("PASS: "+text);}
 var money=MoneyManager.Instance;var cycle=UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();
 var rent=DailyRent.Current;var loan=LoanAccount.Current;var ledger=UnityEngine.Object.FindFirstObjectByType<ShopDayLedger>();
 Check(DailyRent.Cost(1)==30000&&DailyRent.Cost(2)==33000&&DailyRent.Cost(6)==45000&&DailyRent.Cost(10)==57000,"일차별 3만원 시작, 3천원 증가");
 money.currentMoney=100000;money.ResetDailyStats();cycle.currentDay=1;cycle.isShopOpen=true;ledger.BeginDay();
 loan.Borrow(10000,1,out _);Check(DailyRent.ProjectedBalance(1)==78000,"임대료와 이자를 모두 반영한 예상 잔액");
 int events=0;money.TransactionRecorded+=(delta,type)=>{if(type==ExpenseType.Rent&&delta==-30000)events++;};
 cycle.isShopOpen=false;Check(rent.SettleDay(1)==30000&&money.currentMoney==80000,"마감 상태에서 첫날 임대료 차감");
 Check(ledger.Rent==30000&&ledger.Expense==30000&&ledger.Other==0&&money.dailyTotalExpense==30000,"임대료 지출 기록 및 기타 항목 중복 방지");
 Check(rent.SettleDay(1)==0&&events==1&&money.currentMoney==80000,"같은 날 중복 청구와 팝업 방지");
 Check(DailyRent.ProjectedBalance(1)==78000,"이미 낸 임대료 예상 잔액에서 재차감 방지");
 cycle.fadeDuration=.05f;cycle.timeMultiplier=0;
 cycle.StartCoroutine((IEnumerator)typeof(DayNightCycle).GetMethod("TransitionToNextDay",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(cycle,null));
 float until=Time.unscaledTime+6;while(cycle.currentDay==1&&Time.unscaledTime<until)yield return null;
 Check(cycle.currentDay==2&&money.currentMoney==78000&&loan.LastSettledDay==1,"실제 다음 날 전환에서 이자 정산 및 임대료 중복 방지");
 cycle.isShopOpen=true;ledger.BeginDay();cycle.isShopOpen=false;
 cycle.StartCoroutine((IEnumerator)typeof(DayNightCycle).GetMethod("TransitionToNextDay",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(cycle,null));
 until=Time.unscaledTime+6;while(cycle.currentDay==2&&Time.unscaledTime<until)yield return null;
 Check(cycle.currentDay==3&&money.currentMoney==43000&&rent.LastSettledDay==2,"둘째 날 임대료 33000원과 이자 2000원 정산");
 Check(rent.Due(3)==36000&&DailyRent.ProjectedBalance(3)==5000,"셋째 날 미리보기");
 var pc=UnityEngine.Object.FindFirstObjectByType<ShopPC>();
 pc.playerCamera.position=pc.transform.position+pc.transform.forward*.8f+Vector3.up*.5f;
 typeof(ShopPC).GetMethod("OpenPanel",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(pc,null);yield return null;
 var dashboard=UnityEngine.Object.FindFirstObjectByType<ShopPcBusinessDashboard>();dashboard.Refresh();
 Check(dashboard.openingGate.GetComponentInChildren<UnityEngine.UI.Text>(true)!=null&&dashboard.statusLabel.text.Contains("36,000")&&dashboard.statusLabel.text.Contains("5,000"),"PC 개점 및 마감 안내 갱신");
 dashboard.view.SelectTab(0);yield return null;ScreenCapture.CaptureScreenshot("Temp/Mok-rent-opening.png");yield return null;
 dashboard.view.SelectTab(1);yield return null;ScreenCapture.CaptureScreenshot("Temp/Mok-rent-closing.png");yield return null;
 money.currentMoney=10000;Check(rent.SettleDay(3)==36000&&money.currentMoney==-26000,"현금 부족해도 임대료 차감");
 Check(BankruptcyGameOver.TryEndDay(cycle),"임대료로 적자 발생 시 기존 파산 화면 연동");
}
}
}
#endif
