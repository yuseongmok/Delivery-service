#if UNITY_EDITOR
using System;using System.Collections;using System.Linq;using System.Reflection;using UnityEngine;using UnityEngine.AI;
namespace DeliveryService.Mok.Editor {
public static class EastTrafficTest {
    const BindingFlags F=BindingFlags.NonPublic|BindingFlags.Instance;
    static void Check(bool value,string label,Action<string> log){if(!value)throw new Exception(label);log("PASS: "+label);}
    static object Read(object obj,string field)=>obj.GetType().GetField(field,F).GetValue(obj);
    public static IEnumerator RunCore(Action<string> log){
        var cars=UnityEngine.Object.FindObjectsByType<CarMov>(FindObjectsSortMode.None);
        var reckless=UnityEngine.Object.FindObjectsByType<EastRecklessDriver>(FindObjectsSortMode.None);
        Check(cars.Length==74&&reckless.Length==15,"전체 차량 74대 / 신도시 난폭 차량 15대",log);
        Check(reckless.All(c=>c.transform.position.x>195)&&cars.Count(c=>c.transform.position.x<195)==34,"기존 도시 차량 34대 유지",log);
        var jay=UnityEngine.Object.FindFirstObjectByType<EastJaywalking>();var citizens=jay.GetComponentsInChildren<NPCControl>().Where(n=>!n.IsDead).ToArray();
        Check(citizens.Length==50,"신도시 생존 시민 50명 (사고 후 교체된 레그돌 제외)",log);
        var routes=GameObject.Find("Mok East City/Reckless traffic routes").GetComponentsInChildren<WayPoint>().Concat(GameObject.Find("Mok East City/Dense traffic").GetComponentsInChildren<WayPoint>()).ToArray();
        foreach(var node in routes){
            Check(node.nextWaypoints.Count==1&&node.nextWaypoints[0]!=null,"",_=>{});
            var end=node.nextWaypoints[0].transform.position;
            Vector3 delta=end-node.transform.position;
            foreach(var hit in Physics.BoxCastAll(node.transform.position+Vector3.up*1.2f,new Vector3(.8f,.5f,.8f),delta.normalized,Quaternion.identity,delta.magnitude,~0,QueryTriggerInteraction.Ignore)){
                if(hit.collider.GetComponentInParent<CarMov>()!=null||hit.collider.GetComponentInParent<NPCControl>()!=null||hit.collider.GetComponentInParent<MotorcycleController>()!=null||hit.collider.GetComponentInParent<PlayerCombat>()!=null)continue;
                throw new Exception("차량 경로 장애물: "+node.name+" / "+hit.collider.name);
            }
        }
        log("PASS: 모든 난폭 차량 경로의 건물 관통·장애물 검사");
        int crossing=0;
        foreach(var npc in citizens){
            var agent=npc.GetComponent<NavMeshAgent>();
            Check(EastJaywalking.TryDestination(npc,agent),"시민 완전한 이동 경로",_=>{});
            Vector3 a=npc.transform.position,b=agent.destination;
            if(new[]{210f,290f,370f,440f}.Any(x=>(a.x-x)*(b.x-x)<0)||new[]{-110f,-5f,100f}.Any(z=>(a.z-z)*(b.z-z)<0))crossing++;
        }
        Check(crossing>=35,"횡단보도 밖 도로 횡단 경로: "+crossing+"/50",log);
        float[] fastest=new float[reckless.Length];float until=Time.time+15;
        while(Time.time<until){for(int i=0;i<reckless.Length;i++)fastest[i]=Mathf.Max(fastest[i],reckless[i].GetComponent<CarMov>().CurrentTravelSpeed);yield return null;}
        Check(fastest.Count(v=>v>16)>=8,"실제 과속 주행: "+string.Join(",",fastest.Select(v=>v.ToString("F1"))),log);
        foreach(var car in cars){car.enabled=false;typeof(CarMov).GetField("<CurrentTravelSpeed>k__BackingField",F).SetValue(car,0f);}
        UnityEngine.Object.FindFirstObjectByType<RobberyManager>().enabled=false;
        var player=UnityEngine.Object.FindFirstObjectByType<PlayerCombat>();var cc=player.GetComponent<CharacterController>();
        player.GetComponent<PlayerMove>().enabled=false;cc.enabled=false;player.transform.position=new Vector3(290,2,-50);cc.enabled=true;
        var testCar=cars[0];testCar.transform.position=new Vector3(290,0,-60);testCar.transform.forward=Vector3.forward;
        typeof(CarMov).GetField("<CurrentTravelSpeed>k__BackingField",F).SetValue(testCar,12f);
        var trigger=typeof(CarMov).GetMethod("OnTriggerEnter",F);
        Physics.SyncTransforms();trigger.Invoke(testCar,new object[]{cc});
        var rag=player.GetComponentInChildren<PlayerRagdoll>();var guard=player.GetComponent<TrafficRecovery>();
        Check(rag.IsActive&&guard.IsProtected,"첫 차량 충돌로 레그돌 시작",log);
        Check(!guard.TryBegin(),"넘어진 동안 재피격 거부",log);
        trigger.Invoke(testCar,new object[]{cc});
        yield return null;
        var body=rag.colliders.First(c=>c.enabled);var carCollider=testCar.GetComponentsInChildren<Collider>().First(c=>c.enabled);
        Check(Physics.GetIgnoreCollision(body,carCollider),"레그돌 몸체와 차량의 물리 충돌 제외",log);
        while(rag.IsActive)yield return null;
        Check(guard.IsProtected&&!guard.TryBegin(),"기상 직후 재피격 보호",log);
        until=Time.time+1.5f;while(Time.time<until)yield return null;
        Check(guard.IsProtected,"기상 후 1.5초에도 보호 유지",log);
        until=Time.time+1;while(Time.time<until)yield return null;
        Check(!guard.IsProtected&&!Physics.GetIgnoreCollision(cc,carCollider),"기상 후 2초 경과: 정상 충돌 복구 (protected="+guard.IsProtected+", active="+rag.IsActive+", ignored="+Physics.GetIgnoreCollision(cc,carCollider)+")",log);
        trigger.Invoke(testCar,new object[]{cc});Check(rag.IsActive,"보호 종료 후 차량 충돌 정상 작동",log);
        while(rag.IsActive)yield return null;
        var bike=UnityEngine.Object.FindFirstObjectByType<MotorcycleController>();
        Check(bike.EnterBike(player.gameObject),"오토바이 탑승",log);
        bike.CrashAndEject(12,Vector3.forward);
        Check(rag.IsActive&&guard.IsProtected&&!guard.TryBegin(),"오토바이 사고 이탈에도 연속 피격 차단",log);
        while(rag.IsActive)yield return null;
        player.StartCoroutine(PlayerKnockdown.Roll(player.gameObject,3,Vector3.right));
        Check(guard.IsProtected&&!guard.TryBegin(),"전투·자진 하차 구르기에도 차량 재피격 차단",log);
        until=Time.time+3.2f;while(Time.time<until)yield return null;
        Check(guard.IsProtected,"구르기 종료 후 회복 보호 유지",log);
    }
}
}
#endif
