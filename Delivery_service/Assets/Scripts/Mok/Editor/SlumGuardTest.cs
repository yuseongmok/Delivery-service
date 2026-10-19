#if UNITY_EDITOR
using System;using System.Collections;using System.Linq;using System.Reflection;using UnityEngine;using UnityEngine.AI;
namespace DeliveryService.Mok.Editor {
public static class SlumGuardTest {
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 static void Check(bool ok,string text,Action<string> log){if(!ok)throw new Exception(text);log("PASS: "+text);}
 static void Set(object target,string field,object value)=>target.GetType().GetField(field,F).SetValue(target,value);
 public static IEnumerator RunCore(Action<string> log){
  var clock=UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();clock.enabled=false;clock.isShopOpen=false;
  var manager=UnityEngine.Object.FindFirstObjectByType<RobberyManager>();manager.enabled=false;
  foreach(var car in UnityEngine.Object.FindObjectsByType<CarMov>(FindObjectsSortMode.None))car.enabled=false;
  var roster=UnityEngine.Object.FindFirstObjectByType<SlumGuardRoster>();
  var player=UnityEngine.Object.FindFirstObjectByType<PlayerCombat>();var move=player.GetComponent<PlayerMove>();move.enabled=false;
  foreach(var look in player.GetComponentsInChildren<CameraMove>())look.enabled=false;
  var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.transform.position=new Vector3(-176,0,-5);Cursor.lockState=CursorLockMode.Locked;
  yield return null;Check(roster.Guards.Count==0,"개점 전 강도 미생성",log);
  clock.isShopOpen=true;yield return null;yield return null;
  var gang=roster.Guards.ToArray();Check(gang.Length==5&&gang.Count(g=>g.GetComponent<SlumRockThrower>()!=null)==1,"고정 5명: 근접 4명과 투석 1명",log);
  Check(gang.All(g=>g.FixedGuard&&!g.GetComponent<RobberAI>().enabled&&!g.Combat.CanBePickpocketed),"고정 경비는 접촉 강탈·소매치기 제외",log);
  Check(gang.All(g=>g.Combat.chaseSpeed==7&&g.Combat.punchRange==3.4f&&g.Combat.attackStartRange==3.2f),"속도 7·타격 범위 3.4·공격 시도 3.2",log);
  Check(!roster.SpawnDay(clock.currentDay)&&roster.Guards.SequenceEqual(gang),"같은 날 중복 생성 방지",log);
  NavMesh.SamplePosition(new Vector3(-265,0,-5),out var spot,3,NavMesh.AllAreas);player.transform.position=spot.position;yield return null;yield return null;
  Check(gang.All(g=>g.Engaged),"빈민가 진입 시 다섯 경비 추격",log);
  Check(!manager.TrySpawnForTesting(),"빈민가 랜덤 무리 중복 소환 차단",log);
  var wounded=gang[0];Check(wounded.Combat.ReceiveKick(player,Vector3.forward,30)&&wounded.Combat.Health==70,"경비에게 발차기 피해 30",log);
  player.transform.position=new Vector3(-176,0,-5);float until=Time.time+4;while(Time.time<until)yield return null;
  Check(gang.All(g=>g!=null&&!g.Engaged)&&wounded.Combat.Health==70,"구역 이탈 후 삭제·체력 리셋 없이 귀환",log);
  player.transform.position=spot.position;yield return null;yield return null;Check(wounded.Engaged&&wounded.Combat.Health==70,"재진입 시 남은 체력으로 재추격",log);
  wounded.Combat.ReceiveKick(player,Vector3.forward,100);player.transform.position=new Vector3(-176,0,-5);until=Time.time+6;while(Time.time<until)yield return null;
  Check(roster.Guards.Count(g=>g!=null)==4&&!roster.SpawnDay(clock.currentDay),"사망 경비는 그날 다시 생성하지 않음",log);
  Check(manager.TrySpawnForTesting(),"고정 경비 네 명이 남아도 일반 지역 강도 생성 가능",log);
  foreach(var ordinary in UnityEngine.Object.FindObjectsByType<RobberAI>(FindObjectsSortMode.None))if(ordinary.GetComponent<SlumRobberCombat>()==null)UnityEngine.Object.Destroy(ordinary.gameObject);
  clock.currentDay++;yield return null;yield return null;gang=roster.Guards.ToArray();Check(gang.Length==5&&gang.All(g=>g!=null&&g.Combat.Health==100),"다음 영업일 다섯 명 새로 배치",log);
  // 다른 공격이 섞이지 않게 실제 투석 AI 한 명만 남겨 검증한다.
  var thrower=gang.Single(g=>g.GetComponent<SlumRockThrower>()!=null).GetComponent<SlumRockThrower>();
  foreach(var guard in gang)if(guard.gameObject!=thrower.gameObject)guard.gameObject.SetActive(false);
  var origin=thrower.transform.position;bool found=false;
  for(int i=0;i<16;i++){
   var p=origin+Quaternion.Euler(0,i*22.5f,0)*Vector3.forward*8;
   if(!NavMesh.SamplePosition(p,out var dest,1,NavMesh.AllAreas))continue;
   if(Physics.Linecast(origin+Vector3.up*1.4f,dest.position+Vector3.up,~0,QueryTriggerInteraction.Ignore))continue;
   player.transform.position=dest.position;found=true;break;
  }
  Check(found,"실제 투석 공격용 시야 확보",log);cc.enabled=true;Physics.SyncTransforms();player.View.transform.LookAt(thrower.transform.position+Vector3.up*2.2f);Set(thrower,"nextThrow",Time.time);
  SlumRock shot=null;bool sawWindup=false;until=Time.time+5;
  while(Time.time<until&&shot==null){sawWindup|=thrower.IsWinding;shot=UnityEngine.Object.FindFirstObjectByType<SlumRock>();yield return null;}
  Check(sawWindup&&shot!=null&&shot.InFlight,"실제 AI가 팔 예고 후 돌 발사",log);
  Check(shot.GetComponent<Rigidbody>().useGravity&&shot.GetComponent<Rigidbody>().linearVelocity.y>0&&shot.landingWarning.gameObject.activeInHierarchy,"상승 포물선·착지 경고·중력 적용",log);
  until=Time.time+.35f;while(Time.time<until)yield return null;
  ScreenCapture.CaptureScreenshot("Temp/Mok-slum-rock-preview.png",2);yield return null;yield return null;
  thrower.enabled=false;cc.enabled=false;player.transform.position+=Vector3.right*5;cc.enabled=true;int hits=player.HitsTaken;
  until=Time.time+4;while(Time.time<until)yield return null;Check(player.HitsTaken==hits&&shot==null,"발사 후 옆으로 이동해 회피·비유도·돌 정리",log);
  foreach(var guard in gang)if(guard!=null)guard.gameObject.SetActive(false);
  // 도심 장애물과 무관하게 물리 충돌과 벽 차단을 따로 검사한다.
  cc.enabled=false;player.transform.position=new Vector3(-265,100,-5);cc.enabled=true;Physics.SyncTransforms();
  var source=new GameObject("Stone test source");source.transform.position=player.transform.position+Vector3.back*8;
  Vector3 target=player.transform.position+Vector3.up;Vector3 launch=source.transform.position+Vector3.up*1.8f;
  var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=(launch+target)*.5f+Vector3.up*2;wall.transform.localScale=new Vector3(6,10,.5f);Physics.SyncTransforms();
  var blocked=UnityEngine.Object.Instantiate(roster.projectilePrefab,launch,Quaternion.identity);blocked.Launch(source.transform,target,1.3f);
  until=Time.time+2;while(Time.time<until)yield return null;Check(blocked==null&&player.HitsTaken==hits,"벽 충돌 시 돌 소멸·벽 뒤 피해 없음",log);UnityEngine.Object.Destroy(wall);yield return null;
  var direct=UnityEngine.Object.Instantiate(roster.projectilePrefab,launch,Quaternion.identity);direct.Launch(source.transform,target,1.3f);
  until=Time.time+2;while(Time.time<until&&!player.IsDown)yield return null;Check(player.IsDown&&player.HitsTaken==hits+1&&direct==null,"돌 실제 충돌이 주먹과 같은 피격·레그돌 적용",log);
  while(player.IsDown)yield return null;
  UnityEngine.Object.Destroy(source);Check(UnityEngine.Object.FindObjectsByType<SlumRock>(FindObjectsSortMode.None).Length==0,"투사체 누적 없음",log);
 }
}
}
#endif
