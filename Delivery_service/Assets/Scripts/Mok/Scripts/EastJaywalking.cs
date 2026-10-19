using UnityEngine;
using UnityEngine.AI;
namespace DeliveryService.Mok {
// 신도시 생성기 아래 시민만 도로 양쪽의 목적지를 번갈아 고른다.
public sealed class EastJaywalking : MonoBehaviour {
    public float minSpeed=1.4f,maxSpeed=2.4f;
    public static bool TryDestination(NPCControl npc,NavMeshAgent agent){
        var district=npc.GetComponentInParent<EastJaywalking>();if(district==null)return false;
        agent.areaMask=NavMesh.AllAreas;
        var p=npc.transform.position;
        for(int attempt=0;attempt<24;attempt++){
            Vector3 target;
            if(attempt<16){
                // 가장 가까운 도로의 반대편으로 건너가되 교차로에만 몰리지 않게 한다.
                bool vertical=Random.value<.5f;
                float[] roads=vertical?new[]{210f,290f,370f,440f}:new[]{-110f,-5f,100f};float road=roads[0];
                float coordinate=vertical?p.x:p.z;
                foreach(float value in roads)if(Mathf.Abs(value-coordinate)<Mathf.Abs(road-coordinate))road=value;
                float side=coordinate>=road?-1:1;
                target=vertical?new Vector3(road+side*8.5f,.1f,Mathf.Clamp(p.z+Random.Range(-32f,32f),-99,89)):
                    new Vector3(Mathf.Clamp(p.x+Random.Range(-32f,32f),202,448),.1f,road+side*8.5f);
            }else target=new Vector3(Random.Range(202f,448f),.1f,Random.Range(-118f,108f));
            if(!NavMesh.SamplePosition(target,out var hit,3,NavMesh.AllAreas))continue;
            if(hit.position.x<199||hit.position.x>451||hit.position.z< -122||hit.position.z>112)continue;
            var path=new NavMeshPath();if(!agent.CalculatePath(hit.position,path)||path.status!=NavMeshPathStatus.PathComplete)continue;
            if(Vector3.Distance(p,hit.position)<8)continue;
            agent.speed=Random.Range(district.minSpeed,district.maxSpeed);agent.SetPath(path);return true;
        }
        // 완전한 경로가 없으면 원래 배회 알고리즘으로 복귀한다.
        return false;
    }
}
}
