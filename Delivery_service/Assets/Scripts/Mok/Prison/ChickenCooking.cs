using UnityEngine;
using UnityEngine.InputSystem;
namespace DeliveryService.Mok {
 // 재료와 실패 판정은 미션이, 마우스 조작과 조리 진행은 이 컴포넌트가 맡는다.
 public sealed class ChickenCooking:MonoBehaviour {
  public enum Step { Cut, Flour, Water, Mix, Oil, Heat, Fry, Sauce, Ready }
  public PrisonChickenMission mission;
  public Transform knife,bowl,pourBottle;
  public GameObject rawChicken,cookedChicken;
  public Step Current{get;private set;}
  public float Amount{get;private set;}
  public float Temperature{get;private set;}=100;
  public int Cuts{get;private set;}
  public int Flips{get;private set;}
  public bool Open{get;private set;}
  public Vector2 CutScreenPosition=>mission.camera.WorldToScreenPoint(rawChicken.transform.position+Vector3.right*(Cuts-1)*.1f+Vector3.up*.1f);
  public float Needle=>Mathf.PingPong(Time.time*.8f,1);
  float lastAngle,mixed,nextClick;bool hasAngle;int openedFrame;
  Vector3 knifeRest;Quaternion bottleRest;
  public string Message{get;private set;}="";
  public string Instruction=>Current switch {
   Step.Cut=>"표시된 위치를 클릭해 닭을 토막 내세요",
   Step.Flour=>"좌클릭을 누르고 밀가루 계량 · 초록 구간에서 놓기",
   Step.Water=>"좌클릭을 누르고 물 계량 · 초록 구간에서 놓기",
   Step.Mix=>"반죽 그릇 주변으로 마우스를 원형으로 움직이세요",
   Step.Oil=>"좌클릭을 누르고 기름 채우기 · 초록 구간에서 놓기",
   Step.Heat=>"기름이 180°C가 되면 클릭해 닭 넣기",
   Step.Fry=>"표시가 초록 구간에 오면 Space · 세 번 뒤집기",
   Step.Sauce=>"좌클릭으로 양념소스를 곁들여 완성",
   _=>"치킨 완성"
  };
  void Awake(){knifeRest=knife.localPosition;bottleRest=pourBottle.localRotation;}
  public void ResetRecipe(){Current=Step.Cut;Amount=0;Temperature=100;Cuts=Flips=0;mixed=0;hasAngle=false;Message="";rawChicken.SetActive(true);cookedChicken.SetActive(false);}
  public void Begin(){openedFrame=Time.frameCount;Open=true;hasAngle=false;Message="";}
  public void Close(){Open=false;if(knife)knife.localPosition=knifeRest;if(pourBottle)pourBottle.localRotation=bottleRest;}
  void Update(){
   if(Time.frameCount==openedFrame||!Open||Time.timeScale<=0||!mission.CookingActive)return;
   if(Keyboard.current!=null&&Keyboard.current.fKey.wasPressedThisFrame){mission.LeaveCooking();return;}
   var mouse=Mouse.current;if(mouse==null)return;
   if(Current==Step.Cut){
    var p=mouse.position.ReadValue();var wanted=CutScreenPosition;
    knife.localPosition=knifeRest+new Vector3((p.x/Screen.width-.5f)*.5f,Mathf.Sin(Mathf.Clamp01((Time.time-nextClick+.25f)/.25f)*Mathf.PI)*-.12f,0);
    if(mouse.leftButton.wasPressedThisFrame&&Time.time>=nextClick&&Vector2.Distance(p,wanted)<Screen.height*.045f){Cuts++;nextClick=Time.time+.25f;PrisonAudio.Clink(transform.position,.12f);if(Cuts>=3)Advance();}
   }else if(Current==Step.Flour||Current==Step.Water||Current==Step.Oil){
    if(mouse.leftButton.isPressed){Amount+=Time.deltaTime*.28f;pourBottle.localRotation=bottleRest*Quaternion.Euler(0,0,-65);}
    else pourBottle.localRotation=bottleRest;
    if(Amount>1.08f){RejectMeasure();return;}
    if(mouse.leftButton.wasReleasedThisFrame){if(Amount>=.92f&&Amount<=1.08f){Advance();}else Message="조금 더 필요해요. 다시 눌러 채우세요.";}
   }else if(Current==Step.Mix){
    Vector2 p=mouse.position.ReadValue()-new Vector2(Screen.width*.5f,Screen.height*.5f);float angle=Mathf.Atan2(p.y,p.x)*Mathf.Rad2Deg;
    if(p.magnitude>40){if(hasAngle){float delta=Mathf.Abs(Mathf.DeltaAngle(lastAngle,angle));if(delta<60)mixed+=delta;}hasAngle=true;lastAngle=angle;bowl.localRotation=Quaternion.Euler(0,mixed,0);Amount=Mathf.Clamp01(mixed/720);if(mixed>=720)Advance();}
   }else if(Current==Step.Heat){Temperature+=Time.deltaTime*12;if(Temperature>205)Temperature=120;if(mouse.leftButton.wasPressedThisFrame){if(Mathf.Abs(Temperature-180)<=3)Advance();else Message="아직 온도가 맞지 않아요. 180°C에 맞춰 주세요.";}}
   else if(Current==Step.Fry&&Keyboard.current!=null&&Keyboard.current.spaceKey.wasPressedThisFrame){if(Mathf.Abs(Needle-.5f)<.13f){Flips++;rawChicken.transform.Rotate(0,0,180);PrisonAudio.Clink(transform.position,.12f);if(Flips>=3)Advance();}else{Message="타이밍을 놓쳤어요. 다시 뒤집어 주세요.";mission.MakeNoise(false);}}
   else if(Current==Step.Sauce&&mouse.leftButton.wasPressedThisFrame){Current=Step.Ready;rawChicken.SetActive(false);cookedChicken.SetActive(true);mission.FinishCooking();}
  }
  void Advance(){Current++;Amount=0;Message="";hasAngle=false;}
  void RejectMeasure(){int ingredient=Current==Step.Flour?1:Current==Step.Water?3:2;Amount=0;mission.DiscardIngredient(ingredient);Message="정량을 넘겼어요. 해당 재료를 다시 가져오세요.";mission.LeaveCooking();}
 }
}

