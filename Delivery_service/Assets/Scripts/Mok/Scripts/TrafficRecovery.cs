using System.Collections.Generic;
using UnityEngine;
namespace DeliveryService.Mok {
// 쓰러진 몸과 차량 사이 충돌만 잠시 제외한다. 바닥·벽 충돌은 그대로 유지한다.
public sealed class TrafficRecovery : MonoBehaviour {
    public const float GraceSeconds=2f;
    float until,scanAt;
    readonly List<(Collider player,Collider car)> ignored=new List<(Collider,Collider)>();
    readonly HashSet<(Collider,Collider)> seen=new HashSet<(Collider,Collider)>();
    public bool IsProtected=>Time.time<until||IsDown;
    bool IsDown {get {var r=GetComponentInChildren<PlayerRagdoll>();var c=GetComponent<PlayerCombat>();return (r!=null&&r.IsActive)||(c!=null&&c.IsDown);}}
    public static TrafficRecovery Get(GameObject player){var result=player.GetComponent<TrafficRecovery>();return result!=null?result:player.AddComponent<TrafficRecovery>();}
    public bool TryBegin(float seconds=2.5f){if(IsProtected)return false;Protect(seconds);return true;}
    public void Protect(float seconds){until=Mathf.Max(until,Time.time+seconds+GraceSeconds);Scan();}
    public void Recovered(){until=Mathf.Max(until,Time.time+GraceSeconds);Scan();}
    void LateUpdate(){
        if(IsDown)until=Mathf.Max(until,Time.time+GraceSeconds);
        if(IsProtected){if(Time.time>=scanAt){Scan();scanAt=Time.time+.15f;}}else Restore();
    }
    void Scan(){
        var own=GetComponentsInChildren<Collider>(true);
        foreach(var car in FindObjectsByType<CarMov>(FindObjectsSortMode.None))foreach(var other in car.GetComponentsInChildren<Collider>(true))foreach(var col in own){
            if(col==null||other==null||!col.enabled||!other.enabled)continue;
            var pair=(col,other);if(seen.Contains(pair)){if(!Physics.GetIgnoreCollision(col,other))Physics.IgnoreCollision(col,other,true);continue;}
            // 다른 시스템이 이미 제외한 충돌은 복구 대상에 넣지 않는다.
            if(Physics.GetIgnoreCollision(col,other))continue;
            Physics.IgnoreCollision(col,other,true);seen.Add(pair);ignored.Add(pair);
        }
    }
    void Restore(){foreach(var pair in ignored)if(pair.player!=null&&pair.car!=null)Physics.IgnoreCollision(pair.player,pair.car,false);ignored.Clear();seen.Clear();}
    void OnDisable(){Restore();}
}
}
