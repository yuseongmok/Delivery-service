#if UNITY_EDITOR
using System;using System.Linq;using System.Collections;using UnityEngine;using UnityEngine.AI;
namespace DeliveryService.Mok.Editor {
public static class PoliceCombatTest {
public static IEnumerator RunCore(Action<string> log){
 void Check(bool b,string s){if(!b)throw new Exception(s);log("PASS: "+s);}
 var player=UnityEngine.Object.FindFirstObjectByType<PlayerCombat>();var manager=UnityEngine.Object.FindFirstObjectByType<PoliceManager>();
 UnityEngine.Object.FindFirstObjectByType<PhoneController>()?.ClosePhone();Cursor.lockState=CursorLockMode.Locked;
 MoneyManager.Instance.currentMoney=100000;
 manager.SpawnPoliceOnAccidentRefused();yield return null;
 var officer=SceneActors.Police.First();foreach(var cop in SceneActors.Police)cop.enabled=false;
 var fighter=officer.GetComponent<PoliceCombat>();Check(fighter!=null&&fighter.Health==100,"일반 경찰 체력 100 연결");
 Check(fighter.ReceiveHit(player,Vector3.forward,30)&&fighter.Health==70&&fighter.IsDown,"경찰 발차기 30 피해 및 시민 레그돌");
 var armed=SceneActors.Police.Where(x=>x.GetComponent<PoliceCombat>().Armed).ToArray();Check(armed.Length==1,"경찰 공격 후 무장 지원 한 명 스폰");
 var support=armed[0].GetComponent<PoliceCombat>();support.enabled=false;armed[0].enabled=false;
 Check(manager.SpawnArmedSupport(player.transform)==armed[0],"추가 공격 시 무장 지원 중복 생성 차단");
 Check(!fighter.ReceiveHit(player,Vector3.forward,30)&&fighter.Health==70,"경찰 다운 중 중복 피해 차단");
 float until=Time.time+9;while(fighter.IsDown&&Time.time<until)yield return null;Check(!fighter.IsDown,"경찰 레그돌 회복");
 var weapon=player.GetComponent<PlayerWeapon>();Check(weapon.Hit(officer.GetComponent<Collider>(),Vector3.forward,100)&&fighter.IsDead,"플레이어 무기로 경찰 공격 및 사망");
 for(int i=0;i<300;i++){int fine=PoliceControl.RollFine();if(fine<5000||fine>15000)throw new Exception("벌금 범위 초과");}Check(true,"벌금 300회 추첨 모두 5000~15000원");
 GunshotSound.Play(player.gameObject,false);GunshotSound.Play(support.gameObject,true);yield return null;
 Check(player.GetComponents<AudioSource>().Any(x=>x.isPlaying)&&support.GetComponents<AudioSource>().Any(x=>x.isPlaying),"플레이어와 경찰 총 발사음 재생");
 foreach(var cop in SceneActors.Police){cop.enabled=false;cop.GetComponent<PoliceCombat>().enabled=false;}
 var agent=support.GetComponent<NavMeshAgent>();agent.enabled=false;
 support.transform.position=player.View.transform.position-Vector3.up*1.35f+player.View.transform.forward*2;
 support.transform.rotation=Quaternion.LookRotation(player.transform.position-support.transform.position);
 Physics.SyncTransforms();Check(support.ClearShot(),"무장 경찰 사격 시야 확보");
 var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=(support.transform.position+Vector3.up*1.35f+player.View.transform.position)*.5f;wall.transform.localScale=new Vector3(1,3,1);Physics.SyncTransforms();Check(!support.ClearShot(),"벽 뒤 플레이어 사격 차단");UnityEngine.Object.Destroy(wall);yield return null;
 for(int i=0;i<3;i++){
  until=Time.time+9;while(!player.CanReceiveBullet&&Time.time<until)yield return null;
  if(i==0){support.enabled=true;until=Time.time+4;while(!player.IsDown&&Time.time<until)yield return null;support.enabled=false;Check(player.IsDown&&player.BulletHitsTaken==1,"무장 경찰 실제 자동 사격으로 첫 레그돌");}
  else Check(player.ReceiveBullet(Vector3.forward)&&player.IsDown,"총알 피격 레그돌 "+(i+1));
  Check(!player.ReceiveBullet(Vector3.forward),"다운 중 총알 연속 피격 보호");
  until=Time.time+10;while(player.IsDown&&Time.time<until)yield return null;
  Check(!player.IsDown,"피격 회복 완료 "+(i+1));
 }
 Check(player.HitsTaken==0&&player.BulletHitsTaken==0,"세 번째 피격 후 병원에서 피해 횟수 초기화");
 Check(player.hospital!=null&&Vector3.Distance(player.transform.position,player.hospital.position)<2,"세 발 피격 후 지정 병원 이동");
 Check(MoneyManager.Instance.currentMoney==80000,"기존 병원비 20000원 한 번 청구");yield return null;
 Check(!SceneActors.Police.Any(),"병원 복귀 후 추격 경찰 정리");
}
}
}
#endif
