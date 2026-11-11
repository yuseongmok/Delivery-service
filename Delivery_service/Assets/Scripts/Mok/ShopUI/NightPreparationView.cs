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
 bool bound,wasNight,confirmRest,confirmCancel;int shownDay=-1;
 void OnEnable(){Bind();confirmRest=false;confirmCancel=false;if(cycle&&cycle.IsNightPreparation&&view)view.SelectTab(0);Refresh();}
 void Bind(){if(bound||!restButton)return;bound=true;for(int i=0;i<missionButtons.Length;i++){int index=i;missionButtons[i].onClick.AddListener(()=>SelectMission(index));}restButton.onClick.AddListener(Rest);if(startButton)startButton.onClick.AddListener(StartSelected);}
 void LateUpdate(){Refresh();}
 public void SelectMission(int index){if(plan==null||plan.missions==null||index<0||index>=plan.missions.Length)return;SelectedMission=index;Refresh();}
 public void Refresh(){
  if(!cycle||!panel||!plan)return;bool night=cycle.IsNightPreparation;
  if(shownDay!=cycle.currentDay){shownDay=cycle.currentDay;confirmRest=false;}
  panel.SetActive(night);if(tabLabel)tabLabel.text=night?"스토리":"주문";
  if(night&&!wasNight&&view)view.SelectTab(0);wasNight=night;if(!night)return;
  chapterLabel.text="스토리 준비";introLabel.text="연락과 다음 임무는 휴대폰에서 확인하세요.";
  balanceLabel.text=$"잔액 {(MoneyManager.Instance?MoneyManager.Instance.currentMoney:0):N0}원";
  foreach(var button in missionButtons)button.gameObject.SetActive(false);
  if(startButton){startButton.gameObject.SetActive(orders&&orders.HasActiveOrder&&!orders.StoryLesson);if(startLabel)startLabel.text=confirmCancel?"한 번 더 눌러 주문 취소":"진행 중인 주문 취소";}
  detailTitle.text="미션은 휴대폰에서";detailBody.text="문자를 확인하고 미션 앱에서 다음 이야기를 시작하세요.";detailResult.text="이전 임무를 완료하면 다음 임무가 열립니다.";
  footer.text="일차 대기와 영업 정산 없이 스토리를 진행합니다.";
  restButton.gameObject.SetActive(false);

 }
 public void StartSelected(){if(!orders||!orders.HasActiveOrder)return;if(!confirmCancel){confirmCancel=true;Refresh();return;}orders.CancelActiveOrder();confirmCancel=false;Refresh();}
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
