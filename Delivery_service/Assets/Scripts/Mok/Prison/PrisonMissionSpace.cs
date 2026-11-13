using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
namespace DeliveryService.Mok
{
    // 생활 NPC만 잠시 비운다. 미션 간수, 민기, 전투 인물은 인구 목록에 없으므로 유지한다.
    public sealed class PrisonMissionSpace : MonoBehaviour
    {
        public PrisonPopulation population;
        public PrisonSharedPresentation missions;
        readonly HashSet<GameObject> hidden=new HashSet<GameObject>();
        readonly List<Bounds> zones=new List<Bounds>();
        float next;
        public bool Restricted(Vector3 point){foreach(var zone in zones)if(zone.Contains(point))return true;return false;}
        void Area(Vector3 point,Vector3 size){zones.Add(new Bounds(point,size));}
        void Update(){
            if(Time.timeScale==0||Time.time<next)return;next=Time.time+.25f;zones.Clear();
            var keys=missions.keys;
            if(keys&&keys.Active){
                if(keys.State<=PrisonKeysMission.Stage.Gather)Area(new Vector3(-49,2,8),new Vector3(12,8,26));
                else if(keys.State<=PrisonKeysMission.Stage.Load)Area(new Vector3(30,2,-27),new Vector3(30,8,18));
                else {Area(new Vector3(0,3,-8),new Vector3(30,12,56));Area(new Vector3(-29,5,-27),new Vector3(32,8,20));}
            }else if(missions.medicine&&missions.medicine.Active){var m=missions.medicine;Area(m.State==PrisonMedicineMission.Stage.Return?m.cell.position:m.workshop.position,new Vector3(22,8,22));}
            else if(missions.chicken&&missions.chicken.Active){var c=missions.chicken; if(c.CookingActive||c.State==PrisonChickenMission.Stage.Kitchen)Area(new Vector3(30,2,-27),new Vector3(30,8,18));else if(c.Target)Area(c.Target.position,new Vector3(20,8,20));}
            else if(missions.mission.introduction&&missions.mission.introduction.Target)Area(missions.mission.introduction.Target.position,new Vector3(16,8,16));
            foreach(var slot in population.slots){
                if(!slot.actor)continue;var go=slot.actor.gameObject;
                if(hidden.Contains(go)){if(!Restricted(go.transform.position)&&population.HiddenFromPlayer(go.transform.position)){go.SetActive(true);hidden.Remove(go);}continue;}
                if(!go.activeSelf)continue;
                bool blocked=Restricted(go.transform.position);var agent=go.GetComponent<NavMeshAgent>();
                if(!blocked&&agent&&agent.enabled&&agent.isOnNavMesh&&agent.hasPath){var points=agent.path.corners;for(int i=1;i<points.Length&&!blocked;i++){var delta=points[i]-points[i-1];foreach(var z in zones)if(z.IntersectRay(new Ray(points[i-1],delta.normalized),out float d)&&d<=delta.magnitude){blocked=true;break;}}}
                if(blocked){hidden.Add(go);go.SetActive(false);}
            }
        }
        void OnDisable(){foreach(var go in hidden)if(go)go.SetActive(true);hidden.Clear();}
    }
}
