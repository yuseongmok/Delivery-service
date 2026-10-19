using System.Collections.Generic;
using UnityEngine;
namespace DeliveryService.Mok {
// 같은 차선에서는 앞차 속도에 맞추고, 갈림길에서는 덜 붐비는 도로를 고른다.
public sealed class TrafficSpacing : MonoBehaviour {
    static readonly List<TrafficSpacing> active=new List<TrafficSpacing>();
    public float bodyLength=5f;
    public float followingGap=1.4f;
    CarMov car;
    void OnEnable(){car=GetComponent<CarMov>();if(!active.Contains(this))active.Add(this);}
    void OnDisable(){active.Remove(this);}
    static TrafficSpacing Leader(CarMov car,out float gap){
        gap=float.PositiveInfinity;var self=car.GetComponent<TrafficSpacing>();if(self==null)return null;
        TrafficSpacing leader=null;
        foreach(var other in active){
            if(other==null||other==self||!other.isActiveAndEnabled||other.car==null)continue;
            if(Vector3.Dot(car.transform.forward,other.transform.forward)<.65f)continue;
            Vector3 offset=car.transform.InverseTransformPoint(other.transform.position);
            float clearance=offset.z-(self.bodyLength+other.bodyLength)*.5f;
            if(offset.z>.1f&&clearance<gap&&Mathf.Abs(offset.x)<1.7f&&Mathf.Abs(offset.y)<2f){gap=clearance;leader=other;}
        }
        return leader;
    }
    public static bool IsBlocked(CarMov car){var leader=Leader(car,out float gap);return leader!=null&&gap<=car.GetComponent<TrafficSpacing>().followingGap;}
    public static float TravelSpeed(CarMov car,float cruise,float deltaTime){
        var self=car.GetComponent<TrafficSpacing>();if(self==null)return cruise;
        var leader=Leader(car,out float gap);float desired=cruise;
        if(leader!=null)desired=Mathf.Min(cruise,Mathf.Max(0,leader.car.CurrentTravelSpeed+(gap-self.followingGap)*.8f));
        float result=Mathf.MoveTowards(car.CurrentTravelSpeed,desired,(desired<car.CurrentTravelSpeed?7f:3f)*deltaTime);
        // 급정지한 앞차를 관통하지 않도록 이번 프레임 이동량도 제한한다.
        if(leader!=null)result=Mathf.Min(result,Mathf.Max(0,gap-self.followingGap)/Mathf.Max(deltaTime,.001f));
        return result;
    }
    static float Density(Vector3 start,Vector3 end,CarMov self){
        Vector3 direction=Vector3.ProjectOnPlane(end-start,Vector3.up);float length=direction.magnitude;if(length<.1f)return 0;direction/=length;int count=0;
        foreach(var entry in active){
            if(entry==null||entry.car==null||entry.car==self||!entry.car.isActiveAndEnabled)continue;
            var offset=entry.transform.position-start;float along=Vector3.Dot(offset,direction);
            if(along< -2||along>length+2||Mathf.Abs(offset.y)>2||Vector3.Dot(entry.transform.forward,direction)<.35f)continue;
            if(Vector3.ProjectOnPlane(offset-direction*along,Vector3.up).magnitude<2)count++;
        }
        return count*20f/Mathf.Max(12f,length);
    }
    public static WayPoint ChooseNext(CarMov self,WayPoint junction){
        if(junction==null||junction.nextWaypoints==null)return null;
        WayPoint best=null;float bestScore=float.PositiveInfinity;
        foreach(var next in junction.nextWaypoints){
            if(next==null)continue;
            float score=Density(junction.transform.position,next.transform.position,self);
            float downstream=0;int branches=0;
            foreach(var after in next.nextWaypoints)if(after!=null){downstream+=Density(next.transform.position,after.transform.position,self);branches++;}
            if(branches>0)score+=.35f*downstream/branches;
            // 혼잡도가 비슷한 길은 무작위로 나누어 같은 길만 고르는 현상을 줄인다.
            score+=Random.Range(0f,.04f);
            if(score<bestScore){bestScore=score;best=next;}
        }
        return best;
    }
}
}
