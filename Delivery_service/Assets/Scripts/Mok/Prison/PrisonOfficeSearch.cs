using UnityEngine;
namespace DeliveryService.Mok {
public sealed class PrisonOfficeSearch:MonoBehaviour {
 public PrisonKeysMission previous;
 public PrisonOfficeText dialogue;
 public PrisonOfficePoint[] points;
 public TextMesh passwordBoard;
 public bool Active{get;private set;}
 public bool HasUniforms{get;private set;}
 public bool HasVehicleKey{get;private set;}
 public bool KnowsPassword{get;private set;}
 public PrisonOfficeEvidence evidence;
 public PrisonCctvMission cctv;
 public bool Complete=>HasUniforms&&(cctv||HasVehicleKey&&KnowsPassword)&&(!evidence||evidence.Authorized);
 public string Subtitle{get;private set;}="";
 float until;
 public Transform Target{get{if(evidence&&evidence.Busy)return null;if(evidence&&!evidence.Authorized)return evidence.Target;foreach(var p in points)if((!cctv||p.kind==PrisonOfficePoint.Kind.Uniforms)&&!Found(p.kind))return p.transform;return null;}}
 public string Objective=>evidence&&!evidence.Authorized?evidence.Objective:Complete?"간수장실 조사 완료 · 탈출 준비 확보":cctv?"탈옥 준비 · 보관함에서 간수복 두 벌 확보하기":$"간수장실 조사 · 탈출에 필요한 물건 찾기 ({(HasUniforms?1:0)+(HasVehicleKey?1:0)+(KnowsPassword?1:0)}/3)";
 public string Notes=>cctv?(evidence?evidence.Notes+"\n":"")+"간수복 두 벌: "+(HasUniforms?"확보":"미확보"):(evidence?evidence.Notes+"\n":"")+"간수복 두 벌: "+(HasUniforms?"확보":"미확보")+"\n호송차량 열쇠: "+(HasVehicleKey?"확보":"미확보")+"\n오늘의 암구호: "+(KnowsPassword?dialogue.password:"아직 확인하지 않음");
 void Start(){if(passwordBoard)passwordBoard.text=cctv?"정기 호송 출발\n"+cctv.dialogue.departureTime:"오늘의 암구호\n"+dialogue.password;}
 void Update(){if(Time.timeScale<=0)return;if(!Active&&previous.State==PrisonKeysMission.Stage.Complete){Active=true;Say(dialogue.start);}if(Time.time>until)Subtitle="";}
 public bool Found(PrisonOfficePoint.Kind kind)=>kind==PrisonOfficePoint.Kind.Uniforms?HasUniforms:kind==PrisonOfficePoint.Kind.VehicleKey?HasVehicleKey:KnowsPassword;
 public void Say(string line){Subtitle=line;until=Time.time+dialogue.subtitleSeconds;}
 public void Collect(PrisonOfficePoint.Kind kind){bool wasComplete=Complete;if(kind==PrisonOfficePoint.Kind.Uniforms){HasUniforms=true;Say(dialogue.uniforms);}else if(kind==PrisonOfficePoint.Kind.VehicleKey){HasVehicleKey=true;Say(dialogue.keys);}else{KnowsPassword=true;Say(dialogue.passwordLine+"\n오늘의 암구호: "+dialogue.password);}if(Complete&&!wasComplete)Say(Subtitle+"\n"+dialogue.complete);}
}}
