using UnityEngine;
using UnityEngine.UI;
using DeliveryService.Yun.ShopUI;
namespace DeliveryService.Mok {
// 기존 PC의 주문 탭을 밤 준비 화면으로 전환한다. 미션 완료를 임의로 지급하지 않는다.
[DefaultExecutionOrder(11000)]
public sealed class NightPreparationView : MonoBehaviour {
 public DayNightCycle cycle;public OrderManager orders;public ShopPcUiView view;
 public NightPreparationPlan plan;public GameObject panel;
 public Text tabLabel,chapterLabel,introLabel,balanceLabel,detailTitle,detailBody,detailResult,footer,restLabel;
 public Text[] missionLabels;public Button[] missionButtons;public Button restButton;
 public BrokerMission brokerMission;public Button startButton;public Text startLabel;
 public int SelectedMission {get;private set;}
 bool bound,wasNight,confirmRest;int shownDay=-1;
 void OnEnable(){Bind();confirmRest=false;if(cycle&&cycle.IsNightPreparation&&view)view.SelectTab(0);Refresh();}
 void Bind(){if(bound||!restButton)return;bound=true;for(int i=0;i<missionButtons.Length;i++){int index=i;missionButtons[i].onClick.AddListener(()=>SelectMission(index));}restButton.onClick.AddListener(Rest);if(startButton)startButton.onClick.AddListener(StartSelected);}
 void LateUpdate(){Refresh();}
 public void SelectMission(int index){if(plan==null||plan.missions==null||index<0||index>=plan.missions.Length)return;SelectedMission=index;Refresh();}
 public void Refresh(){
  if(!cycle||!panel||!plan)return;bool night=cycle.IsNightPreparation;
  if(shownDay!=cycle.currentDay){shownDay=cycle.currentDay;confirmRest=false;}
  panel.SetActive(night);if(tabLabel)tabLabel.text=night?"영업 종료":"주문";
  if(night&&!wasNight&&view)view.SelectTab(0);wasNight=night;if(!night)return;
  chapterLabel.text="오늘 영업 종료";introLabel.text="민기의 연락은 휴대폰 문자 앱에서 확인하세요.";
  balanceLabel.text=$"DAY {cycle.currentDay:00} · 밤 준비    잔액 {(MoneyManager.Instance?MoneyManager.Instance.currentMoney:0):N0}원";
  foreach(var button in missionButtons)button.gameObject.SetActive(false);
  if(startButton)startButton.gameObject.SetActive(false);
  detailTitle.text="미션은 휴대폰에서";detailBody.text="민기가 보낸 문자를 확인하고 휴대폰 미션 앱에서 밤 준비를 시작하세요.";detailResult.text="이곳에서는 영업 정산과 다음 날 진행을 할 수 있습니다.";
  footer.text=confirmRest?"다음 날로 넘어가면 오늘 비용을 정산하고 남은 주문을 종료합니다. 계속하려면 한 번 더 눌러 주세요.":orders&&orders.HasActiveOrder?"낮에 수락한 배달은 마칠 수 있습니다. 새 주문은 내일 개점 후 들어옵니다.":"낮 영업이 끝났습니다. 준비 내용을 확인한 뒤 다음 날로 넘어갈 수 있습니다.";
  restLabel.text=confirmRest?"정산하고 다음 날로":"다음 날로";restButton.interactable=!cycle.IsTransitioning&&!BrokerMission.Active;if(BrokerMission.Active)footer.text="진행 중인 미션을 마치거나 중단한 뒤 다음 날로 넘어갈 수 있습니다.";else if(orders&&orders.HasActiveOrder)footer.text="수락한 배달을 마친 뒤 밤 미션을 시작하세요.";
 }
 public void StartSelected(){ /* 미션 시작은 휴대폰 앱에서만 처리한다. */ }
 public void Rest(){if(!cycle||!cycle.IsNightPreparation||cycle.IsTransitioning||BrokerMission.Active)return;if(!confirmRest){confirmRest=true;Refresh();return;}confirmRest=false;cycle.OnClick_ConfirmClose();}
 public static void SetClosingLabels(DayNightCycle day){
  if(!day.summaryPanel)return;
  foreach(var b in day.summaryPanel.GetComponentsInChildren<Button>(true))for(int i=0;i<b.onClick.GetPersistentEventCount();i++){
   var method=b.onClick.GetPersistentMethodName(i);var text=b.GetComponentInChildren<Text>(true);if(!text)continue;
   if(method=="OnClick_ConfirmClose")text.text="밤 준비로";
   if(method=="OnClick_CancelClose")text.text="영업 계속";
  }
 }
}
}
