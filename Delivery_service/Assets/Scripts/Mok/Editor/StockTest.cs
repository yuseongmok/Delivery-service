#if UNITY_EDITOR
using System;using System.Collections;using System.Linq;using System.Reflection;using UnityEngine;using DeliveryService.Mok.Stocks;using DeliveryService.Mok.Navigation;using DeliveryService.Yun.ShopUI;
namespace DeliveryService.Mok.Editor {
public static class StockTest {
const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
static void Check(bool ok,string s,Action<string> log){if(!ok)throw new Exception(s);log("PASS: "+s);}
public static IEnumerator RunCore(Action<string> log){
var market=StockMarket.Instance;var clock=UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();var money=MoneyManager.Instance;clock.timeMultiplier=0;clock.isShopOpen=false;
Check(market!=null&&market.Companies.Length==5,"저장된 주식 시스템 및 기업 5개",log);
Check(StockCatalog.Events.Length==25&&Enumerable.Range(0,5).All(c=>StockCatalog.Events.Count(e=>e.company==c)==5),"기업별 사건 5종씩 총 25종",log);
Check(StockCatalog.Events.All(e=>!string.IsNullOrEmpty(e.alternate)&&!string.IsNullOrEmpty(e.goodBody)&&!string.IsNullOrEmpty(e.badBody)),"기사 변형과 양방향 후속 보도",log);
market.Initialize(42);market.AdvanceDay(1);Check(market.Articles.Count==3,"첫날 기사 3개 생성",log);
int savedDay=clock.currentDay;float savedTime=clock.currentTime;int savedCash=money.currentMoney;
int roundsBefore=market.Articles.Count;market.TestNewsRound();Check(market.Articles.Count>roundsBefore&&market.Articles[0].title.StartsWith("[테스트]"),"F9 경로 새 기사 생성 및 테스트 표기",log);
for(int i=0;i<3;i++)market.TestNewsRound();Check(market.Articles.Any(a=>a.followup),"F9 반복으로 후속 사건 결과 진행",log);
int historyBefore=market.Companies[0].history.Count;int[] pricesBefore=market.Companies.Select(c=>c.price).ToArray();market.TestPriceTicks();
Check(market.Companies[0].history.Count==historyBefore+10,"F10 경로 가격 기록 10회 진행",log);Check(!pricesBefore.SequenceEqual(market.Companies.Select(c=>c.price)),"테스트 주가 변화 확인",log);
Check(clock.currentDay==savedDay&&clock.currentTime==savedTime&&!clock.isShopOpen&&money.currentMoney==savedCash,"테스트 키가 일차·시간·영업·현금을 변경하지 않음",log);
int countBefore=market.Articles.Count;market.AdvanceDay(savedDay+1);Check(market.Articles.Count>countBefore,"테스트 이후 실제 다음 날 뉴스 정상 생성",log);
market.Initialize(42);market.AdvanceDay(1);
int revision=market.Revision;yield return null;Check(market.Revision==revision,"개점 전 시세 정지",log);
string message;int price=market.Companies[0].price;Check(!market.Trade(0,1,true,price,out message),"개점 전 거래 차단",log);
clock.isShopOpen=true;money.currentMoney=20000;money.ResetDailyStats();
Check(market.Trade(0,10,true,price,out message),"매수 성공",log);Check(money.currentMoney==20000-price*10&&market.Companies[0].shares==10,"현금과 주식 수량 일치",log);
Check(money.dailyIncome==0&&money.dailyTotalExpense==0,"투자 원금이 영업 통계를 오염시키지 않음",log);
Check(!market.Trade(0,999,true,price,out message),"잔액 부족 매수 차단",log);Check(!market.Trade(0,11,false,price,out message),"보유 초과 매도 차단",log);Check(!market.Trade(0,0,true,price,out message),"수량 0 거래 차단",log);
market.Companies[0].price=900;Check(!market.Trade(0,1,true,price,out message),"확인 중 가격 변동 시 재확인",log);
Check(market.Trade(0,4,false,900,out message),"부분 매도 성공",log);Check(market.Companies[0].cost==4800&&market.RealizedProfit==400,"평균 매수가 및 실현손익 계산",log);
market.AdvanceDay(2);Check(market.Companies[0].shares==6,"다음 날 보유 주식 유지",log);
for(int d=3;d<=10;d++){market.AdvanceDay(d);Check(market.Articles.Count(a=>a.day==d)>=2&&market.Articles.Count(a=>a.day==d)<=3,$"DAY {d} 기사 2~3개",log);}
Check(market.Articles.Any(a=>a.followup),"후속 기사 생성",log);
market.Initialize(123);market.AdvanceDay(1);for(int i=0;i<30;i++)market.TickPrices();var without=market.Companies.Select(c=>c.price).ToArray();
market.Initialize(123);market.AdvanceDay(1);money.currentMoney=20000;market.Trade(0,1,true,market.Companies[0].price,out message);for(int i=0;i<30;i++)market.TickPrices();Check(without.SequenceEqual(market.Companies.Select(c=>c.price)),"플레이어 투자와 독립적인 가격 흐름",log);
Check(market.Companies.All(c=>c.history.Count==31&&c.price>0),"가격 기록과 양수 가격 유지",log);
for(int i=0;i<30;i++)market.DeliveryClue();Check(market.LastClue.Contains("관련 보도"),"활성 사건에 연결된 배달 단서",log);
money.currentMoney=-500;Check(market.CoverDebt()&&money.currentMoney>=0,"보유 주식 처분으로 부족 현금 정산",log);
money.currentMoney=20000;var planner=UnityEngine.Object.FindFirstObjectByType<NavigationPlanner>();var phone=UnityEngine.Object.FindFirstObjectByType<PhoneController>();phone.TogglePhone();yield return null;
planner.Show(NavigationPlanner.Screen.Home);planner.MoveSelection(1);planner.Confirm();Check(planner.CurrentScreen==NavigationPlanner.Screen.Stocks,"핸드폰 메인에서 주식 앱 진입",log);
var app=UnityEngine.Object.FindFirstObjectByType<StockPhoneApp>();Check(app!=null&&app.gameObject.activeInHierarchy,"기존 핸드폰 화면에 주식 앱 표시",log);
var logos=app.GetComponentsInChildren<StockCompanyLogo>(true);Check(logos.Length==6&&logos.All(l=>l.GetComponent<CanvasRenderer>()!=null),"회사별 로고 5개 및 상세 로고 렌더러 연결",log);
app.Confirm();Check(app.Detail,"종목 상세 진입",log);app.Confirm();Check(!app.Confirming,"수량 선택 단계",log);app.Move(1,false);Check(app.Quantity==2,"방향키로 수량 선택",log);app.Confirm();Check(app.Confirming,"거래 확인 단계",log);Check(app.Back()&&!app.Confirming,"주문 확인 취소",log);
int oldShares=market.Companies[app.Selection].shares;
app.Confirm();yield return null;ScreenCapture.CaptureScreenshot("Temp/Mok-stock-confirm.png");yield return null;
app.Confirm();Check(market.Companies[app.Selection].shares==oldShares+2,"사기 확인 화면에서 실제 구매",log);
app.Confirm();app.Confirm();app.Move(-1,false);app.Confirm();app.Confirm();app.Confirm();
Check(market.Companies[app.Selection].shares==oldShares+1,"방향키 팔기 선택 후 실제 판매",log);
app.Confirm();int oldCompany=app.Selection;app.Move(-1,false);Check(app.Selection==(oldCompany+1)%5,"위아래 방향키 회사 목록 선택",log);
app.Confirm();Check(app.GetComponentInChildren<UnityEngine.UI.RawImage>()!=null,"회사 선택 즉시 주가 그래프 표시",log);app.Back();
planner.Show(NavigationPlanner.Screen.Map);Check(!app.gameObject.activeSelf,"지도 화면 복귀",log);
var view=UnityEngine.Object.FindFirstObjectByType<ShopPcUiView>(FindObjectsInactive.Include);var pc=UnityEngine.Object.FindFirstObjectByType<ShopPC>();
pc.playerCamera.position=pc.transform.position+pc.transform.forward*.8f+Vector3.up*.5f;typeof(ShopPC).GetMethod("OpenPanel",Flags).Invoke(pc,null);view.SelectTab(3);yield return null;
var news=UnityEngine.Object.FindFirstObjectByType<StockNewsPage>();Check(view.SelectedTab==3&&news!=null,"PC 뉴스 탭 열기",log);news.Refresh();Check(news.article.text.Contains("DAY")&&news.clue.text.Contains("관련 보도"),"게시 일차와 배달 메모 표시",log);
var before=news.article.text;news.previous.onClick.Invoke();Check(news.article.text!=before,"이전 기사 열람",log);view.SelectTab(0);Check(!news.gameObject.activeSelf,"기존 주문 탭 복귀",log);
view.SelectTab(3);Canvas.ForceUpdateCanvases();yield return null;
ScreenCapture.CaptureScreenshot("Temp/Mok-stock-news.png");yield return null;
typeof(ShopPC).GetMethod("ClosePanel",Flags).Invoke(pc,null);
phone.ClosePhone();phone.TogglePhone();yield return null;planner.Show(NavigationPlanner.Screen.Stocks);app.Open();yield return null;ScreenCapture.CaptureScreenshot("Temp/Mok-stock-card.png");yield return null;app.Confirm();yield return null;
ScreenCapture.CaptureScreenshot("Temp/Mok-stock-phone.png");yield return null;
// 일말 정산 화면에서 버튼을 눌러 부족분을 메우는 실제 흐름.
market.Companies[0].shares=3;market.Companies[0].cost=market.Companies[0].price*3;money.currentMoney=-500;
var rescue=StockDebtPrompt.Resolve();Check(rescue.MoveNext(),"주식 보유 시 파산 전 처분 선택 표시",log);yield return null;
var sell=UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).First(b=>b.name=="주식으로 정산");sell.onClick.Invoke();
Check(!rescue.MoveNext()&&money.currentMoney>=0,"처분 선택으로 실제 현금 부족 해소",log);yield return null;
market.Companies[0].shares=1;money.currentMoney=-500;rescue=StockDebtPrompt.Resolve();rescue.MoveNext();yield return null;
UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).First(b=>b.name=="처분하지 않고 파산").onClick.Invoke();
Check(!rescue.MoveNext()&&money.currentMoney==-500,"처분 거절 시 기존 파산 판정으로 진행",log);yield return null;

}
}}
#endif
