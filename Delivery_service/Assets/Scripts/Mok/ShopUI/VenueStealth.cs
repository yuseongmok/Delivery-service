using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace DeliveryService.Mok {
// 마지막 배달 전용 잠입 규칙. 도시의 일반 경찰·경비에는 적용하지 않는다.
public sealed class VenueStealth : MonoBehaviour {
 public VenueFinale mission;public VenuePatrolGuard[] guards;public Transform[] approachRoute,listeningPoints;
 public Image suspicionPanel,suspicionFill;public Text suspicionText;
 public float detectionSeconds=3.5f,forgetSeconds=5,vipRadius=4.5f;
 public float Suspicion{get;private set;}public bool Seen{get;private set;}public bool Warned{get;private set;}
 public bool Active=>mission&&(mission.State==VenueFinale.Phase.Deposit||mission.State==VenueFinale.Phase.Observe||mission.State==VenueFinale.Phase.Conversation);
 VenuePatrolGuard[] templates;bool spawned;
 int routeStep;float nextWarning,hiddenFor;
 public bool AtListeningPoint=>listeningPoints.Any(t=>t&&Vector3.Distance(mission.Player.transform.position,t.position)<1.65f);
 public bool CanListen=>Active&&!Seen&&Suspicion<.6f&&AtListeningPoint;
 public Transform Guidance=>!AtListeningPoint&&routeStep<approachRoute.Length?approachRoute[routeStep]:listeningPoints.OrderBy(t=>(t.position-mission.Player.transform.position).sqrMagnitude).First();
 public void ResetAttempt(){EndPatrol();Suspicion=0;Seen=false;Warned=false;routeStep=0;nextWarning=0;hiddenFor=0;}
 public void BeginPatrol(){
 if(templates==null)templates=guards;EndPatrol();guards=templates.Select(g=>Instantiate(g,g.transform.parent)).ToArray();spawned=true;foreach(var g in guards)g.Activate();
 }
 public CitizenCombat[] TakeCombatGuards(){if(!spawned)return new CitizenCombat[0];foreach(var sleeping in guards.Where(g=>g&&g.Incapacitated))Destroy(sleeping.gameObject);var result=guards.Where(g=>g&&!g.Incapacitated).Select(g=>{g.BeginChase();return g.Combat;}).ToArray();guards=templates;spawned=false;if(suspicionPanel)suspicionPanel.gameObject.SetActive(false);Seen=false;return result;}

 public void EndPatrol(){if(guards!=null)foreach(var g in guards)if(g){g.Deactivate();if(spawned)Destroy(g.gameObject);}if(spawned){guards=templates;spawned=false;}if(suspicionPanel)suspicionPanel.gameObject.SetActive(false);Seen=false;}
 public bool TargetSeesPlayer(){var p=mission.Player;var d=p.View.transform.position-(mission.target.position+Vector3.up*1.45f);return d.magnitude<vipRadius&&Vector3.Angle(mission.target.forward,d)<85&&VenuePatrolGuard.ClearSight(mission.target,p);}
 void Update(){Tick(Time.deltaTime);}
 public void Tick(float seconds){
 if(!Active||Time.timeScale<=0||seconds<=0){if(suspicionPanel)suspicionPanel.gameObject.SetActive(false);return;}
 var p=mission.Player;if(!p||p.IsDown)return;
 // 접수실에서는 배달원 신분을 인정한다. 그 이후는 직원 전용 구역이다.
 bool restricted=mission.interior.transform.InverseTransformPoint(p.transform.position).z>-6;
 var spotting=restricted?guards.FirstOrDefault(g=>g&&g.CanSee(p)):null;
 Seen=spotting||TargetSeesPlayer();
 if(Seen){mission.EjectFromVenue(spotting?spotting.transform:null);return;}
 if(guards.Any(g=>g&&!g.Incapacitated&&g.Combat&&(g.Combat.IsAggressive||g.Combat.Health<g.Combat.maximumHealth))){mission.EjectFromVenue();return;}
 Suspicion=0;if(suspicionPanel)suspicionPanel.gameObject.SetActive(false);
 if(AtListeningPoint)routeStep=approachRoute.Length;
 if(routeStep<approachRoute.Length&&Vector3.Distance(p.transform.position,approachRoute[routeStep].position)<2.4f)routeStep++;
 if(suspicionPanel){suspicionPanel.gameObject.SetActive(Suspicion>.01f||Seen);suspicionFill.rectTransform.anchorMax=new Vector2(Suspicion,1);suspicionFill.color=Color.Lerp(new Color(1,.65f,.2f),new Color(1,.2f,.15f),Suspicion);suspicionText.text=Seen?"의심받는 중 · 시야에서 벗어나세요":"경계가 풀리는 중";}
 }
 void OnDisable(){if(!gameObject.activeInHierarchy)EndPatrol();}
}
}
