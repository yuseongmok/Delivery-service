#if UNITY_EDITOR
using System;using System.Collections;using UnityEngine;
namespace DeliveryService.Mok.Editor {
public static class WaterRescueTest {
public static IEnumerator RunCore(Action<string> log){
 void Check(bool b,string s){if(!b)throw new Exception(s);log("PASS: "+s);}
 var rescue=UnityEngine.Object.FindFirstObjectByType<WaterRescue>();Check(rescue!=null&&rescue.water!=null&&rescue.towPoint!=null,"씬의 바다와 병원 견인 지점 연결");
 var p=rescue.player;var money=MoneyManager.Instance;money.currentMoney=100000;
 Check(rescue.Submerged(rescue.water.bounds.center-Vector3.up)&&!rescue.Submerged(rescue.water.bounds.center+Vector3.up*3),"수면 아래 감지 및 다리 높이 제외");
 Check(!rescue.Submerged(rescue.water.bounds.max+Vector3.right*5-Vector3.up*10),"바다 범위 밖 제외");
 var before=p.transform.position;Check(rescue.Begin(false,true),"오토바이만 견인 시작");Check(!rescue.Begin(false,true),"동시 요청 중복 차단");
 float until=Time.time+8;while(rescue.Busy&&Time.time<until)yield return null;
 Check(!rescue.Busy&&money.currentMoney==90000&&rescue.TowCharges==1,"견인비만 10000원 차감");
 Check(Vector3.Distance(before,p.transform.position)<1&&rescue.HospitalCharges==0,"오토바이만 입수 시 플레이어 유지");
 Check(Vector3.Distance(rescue.bike.transform.position,rescue.towPoint.position)<12,"병원 주변 안전 지점 견인");
 until=Time.time+3.2f;while(Time.time<until)yield return null;
 Check(rescue.Begin(true,false),"플레이어만 구조 시작");until=Time.time+10;while(rescue.Busy&&Time.time<until)yield return null;
 Check(!rescue.Busy&&money.currentMoney==70000&&rescue.HospitalCharges==1,"플레이어 입수 병원비만 20000원");
 Check(Vector3.Distance(p.transform.position,rescue.hospital.position)<2&&!p.WaterRescueActive&&p.hospitalFade.Alpha<.01f,"병원 복귀 페이드와 입력 상태 복구");
 until=Time.time+3.2f;while(Time.time<until)yield return null;
 Check(rescue.Begin(true,true),"플레이어와 오토바이 동시 구조");until=Time.time+10;while(rescue.Busy&&Time.time<until)yield return null;
 Check(!rescue.Busy&&money.currentMoney==40000&&rescue.HospitalCharges==2&&rescue.TowCharges==2,"동시 입수 합계 30000원 한 번 정산");
 until=Time.time+4;while(Time.time<until)yield return null;Check(money.currentMoney==40000&&!rescue.Busy,"복귀 후 재감지 및 중복 청구 없음");
 money.currentMoney=100000;Check(rescue.bike.EnterBike(p.gameObject),"실제 오토바이 탑승");
 var wet=rescue.water.bounds.center+new Vector3(0,-4,500);rescue.bike.sphereRB.position=wet;rescue.bike.transform.position=wet;Physics.SyncTransforms();
 until=Time.time+2;while(!rescue.Busy&&Time.time<until)yield return null;Check(rescue.Busy,"탑승 상태 수면 진입 자동 감지");
 until=Time.time+12;while(rescue.Busy&&Time.time<until)yield return null;
 Check(!rescue.Busy&&!rescue.bike.isDriven&&p.gameObject.activeInHierarchy,"자동 하차 및 플레이어 활성화");
 Check(money.currentMoney==70000&&rescue.HospitalCharges==3&&rescue.TowCharges==3,"실제 동시 입수 비용 30000원");
 Check(Vector3.Distance(p.transform.position,rescue.hospital.position)<2,"실제 입수 후 병원 위치 복귀");
}
}
}
#endif
