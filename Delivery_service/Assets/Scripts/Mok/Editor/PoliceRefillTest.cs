#if UNITY_EDITOR
using System;using System.Linq;using System.Collections;using UnityEngine;
namespace DeliveryService.Mok.Editor {
public static class PoliceRefillTest {
public static IEnumerator RunCore(Action<string> log){
 void Check(bool b,string s){if(!b)throw new Exception(s);log("PASS: "+s);}
 PoliceControl[] Alive(bool armed)=>SceneActors.Police.Where(c=>{var f=c.GetComponent<PoliceCombat>();return f!=null&&!f.IsDead&&f.Armed==armed;}).ToArray();
 void Freeze(){foreach(var c in SceneActors.Police){c.enabled=false;c.GetComponent<PoliceCombat>().enabled=false;}}
 var manager=UnityEngine.Object.FindFirstObjectByType<PoliceManager>();var player=UnityEngine.Object.FindFirstObjectByType<PlayerCombat>();manager.SpawnPoliceOnAccidentRefused();yield return null;Freeze();
 Check(Alive(false).Length==3&&Alive(true).Length==0,"기본 출동 3명, 공격 전 무장 경찰 없음");
 var first=Alive(false)[0];first.GetComponent<PoliceCombat>().ReceiveHit(player,Vector3.forward,100);Freeze();
 Check(manager.PendingReplacements==1&&Alive(false).Length==2&&Alive(true).Length==1,"일반 경찰 사망 대기열과 무장 출동");
 float until=Time.time+2.3f;while(Time.time<until){Freeze();yield return null;}
 Check(Alive(false).Length==3&&Alive(true).Length==1,"사망 2초 후 기본 3명 무장 1명 복구");
 var gun=Alive(true)[0];gun.GetComponent<PoliceCombat>().ReceiveHit(player,Vector3.forward,100);
 Check(manager.PendingReplacements==1&&Alive(true).Length==0,"무장 경찰 사망 보충 예약");
 until=Time.time+2.3f;while(Time.time<until){Freeze();yield return null;}
 Check(Alive(false).Length==3&&Alive(true).Length==1,"무장 경찰 한 명 재출동");
 foreach(var c in Alive(false))c.GetComponent<PoliceCombat>().ReceiveHit(player,Vector3.forward,100);
 Alive(true)[0].GetComponent<PoliceCombat>().ReceiveHit(player,Vector3.forward,100);Freeze();
 Check(manager.PendingReplacements==4,"동시 전멸 4명 보충 예약");
 until=Time.time+2.3f;while(Time.time<until){Freeze();yield return null;}
 Check(Alive(false).Length==3&&Alive(true).Length==1,"동시 전멸 후에도 3명+1명만 보충");
 var survivor=Alive(false)[0];survivor.GetComponent<PoliceCombat>().ReceiveHit(player,Vector3.forward,30);Freeze();
 Check(manager.PendingReplacements==0&&Alive(false).Length==3,"레그돌 생존 경찰은 보충하지 않음");
 Alive(true)[0].GetComponent<PoliceCombat>().ReceiveHit(player,Vector3.forward,100);manager.CancelReplacements();
 until=Time.time+2.3f;while(Time.time<until){Freeze();yield return null;}
 Check(manager.PendingReplacements==0&&Alive(true).Length==0,"체포·병원 종료 연결로 예약 취소");
 var shortLived=Alive(false).First(c=>!c.GetComponent<PoliceCombat>().IsDown);shortLived.LimitChaseTime(.5f);shortLived.GetComponent<PoliceCombat>().ReceiveHit(player,Vector3.forward,100);Freeze();
 until=Time.time+1;while(Time.time<until){Freeze();yield return null;}
 Check(manager.PendingReplacements==0&&Alive(false).Length==2,"남은 추격 시간 만료 시 보충 취소");
}
}
}
#endif
