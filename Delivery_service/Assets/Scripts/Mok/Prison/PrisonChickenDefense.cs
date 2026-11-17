using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace DeliveryService.Mok
{
    // 접근, 음식 인계, 동료 보호를 조리 판정과 분리한다.
    [DefaultExecutionOrder(100)]
    public sealed class PrisonChickenDefense : MonoBehaviour
    {
        public PrisonChickenMission mission;
        public PrisonCompanionRagdoll ragdoll;
        public bool Ready { get; private set; }
        public int MingiHits { get; private set; }
        public bool MingiDown => ragdoll && ragdoll.IsDown;
        public bool EncounterActive { get; private set; }
        public bool AllSpawned { get; private set; }
        public int HiddenSpawns { get; private set; }
        PrisonCompanion Mingi => mission.prison.introduction.mingi;
        Transform foodParent;
        Vector3 foodPosition,foodScale;
        Quaternion foodRotation;
        bool holding,receiving;
        float nextHit;
        public Vector3 RetryPosition { get; private set; }
        public Quaternion RetryRotation { get; private set; }
        void Awake(){var food=mission.carrying.transform;foodParent=food.parent;foodPosition=food.localPosition;foodRotation=food.localRotation;foodScale=food.localScale;}
        public void Begin()
        {
            if(EncounterActive)return;
            EncounterActive=true;Ready=false;AllSpawned=false;MingiHits=0;HiddenSpawns=0;
            RetryPosition=mission.prison.player.transform.position;RetryRotation=mission.prison.player.transform.rotation;
            StartCoroutine(Encounter());
        }
        IEnumerator Encounter()
        {
            mission.Say(mission.dialogue.smell,mission.dialogue.encounterLineSeconds);
            for(int i=0;i<mission.enemies.Length;i++){
                var enemy=mission.enemies[i];
                Vector3 spawn;
                while(!FindHiddenSpawn(i,out spawn))yield return new WaitForSeconds(.25f);
                enemy.gameObject.SetActive(false);enemy.transform.position=spawn;enemy.ResetActor();
                enemy.huntsMingi=i%2==1;
                var point=new GameObject("치킨 강탈 접근 위치").transform;point.SetParent(transform);
                Vector3 block=RetryPosition+mission.prison.player.transform.forward*3+mission.prison.player.transform.right*(i==0?-1.1f:1.1f);
                if(NavMesh.SamplePosition(block,out var hit,1.5f,NavMesh.AllAreas))block=hit.position;
                point.position=block;enemy.approach=point;enemy.Begin();HiddenSpawns++;
            }
            AllSpawned=true;
            // 죄수가 실제로 걸어 들어와 앞을 막은 다음 인계한다.
            bool arrived=false;
            while(!arrived){
                foreach(var enemy in mission.enemies)if(!enemy.Defeated&&(Vector3.Distance(enemy.combat.transform.position,mission.prison.player.transform.position)<5||Vector3.Distance(enemy.combat.transform.position,enemy.approach.position)<2))arrived=true;
                if(System.Array.TrueForAll(mission.enemies,e=>e.Defeated))arrived=true;
                yield return null;
            }
            mission.Say(mission.dialogue.ambush,mission.dialogue.encounterLineSeconds);
            yield return new WaitForSeconds(mission.dialogue.encounterLineSeconds);
            mission.Say(mission.dialogue.handoff,6);
            Mingi.Following=false;Mingi.ScriptedTravel=true;
            while(Vector3.Distance(Mingi.transform.position,mission.prison.player.transform.position)>2.1f){
                if(Mingi.agent.enabled&&Mingi.agent.isOnNavMesh){Mingi.agent.isStopped=false;Mingi.agent.speed=4;Mingi.agent.stoppingDistance=1.2f;Mingi.agent.SetDestination(mission.prison.player.transform.position);}
                yield return null;
            }
            Mingi.ScriptedTravel=false;Mingi.agent.ResetPath();
            Vector3 facing=mission.prison.player.transform.position-Mingi.transform.position;facing.y=0;
            if(facing.sqrMagnitude>.01f)Mingi.transform.rotation=Quaternion.LookRotation(facing);
            mission.BeginHandoff();receiving=true;
            var shot=mission.camera.transform;Quaternion originalView=shot.rotation;
            for(float t=0;t<.45f;t+=Time.deltaTime){shot.rotation=Quaternion.Slerp(originalView,Quaternion.LookRotation(PlatePosition()+Vector3.up*.25f-shot.position),Mathf.SmoothStep(0,1,t/.45f));yield return null;}
            var food=mission.carrying.transform;food.SetParent(null,true);
            Vector3 start=food.position;Quaternion rotation=food.rotation;
            for(float t=0;t<.85f;t+=Time.deltaTime){float f=Mathf.SmoothStep(0,1,t/.85f);food.position=Vector3.Lerp(start,PlatePosition(),f);food.rotation=Quaternion.Slerp(rotation,Mingi.transform.rotation,f);yield return null;}
            holding=true;receiving=false;
            yield return new WaitForSeconds(.45f);
            Quaternion fromView=shot.rotation;
            for(float t=0;t<.35f;t+=Time.deltaTime){shot.rotation=Quaternion.Slerp(fromView,originalView,Mathf.SmoothStep(0,1,t/.35f));yield return null;}
            mission.EndHandoff();Ready=true;nextHit=Time.time+2;mission.Say(mission.dialogue.protect,5);
        }
        public bool FindHiddenSpawn(int index,out Vector3 result)
        {
            var player=mission.prison.player;var view=player.View;
            var filter=new NavMeshQueryFilter{agentTypeID=Mingi.agent.agentTypeID,areaMask=NavMesh.AllAreas};
            var path=new NavMeshPath();
            for(int ring=0;ring<4;ring++)for(int n=0;n<24;n++){
                float angle=(n*15+index*65)*Mathf.Deg2Rad;
                Vector3 p=player.transform.position+new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle))*(9+ring*4);
                if(!NavMesh.SamplePosition(p,out var hit,1.2f,filter)||Mathf.Abs(hit.position.y-player.transform.position.y)>1)continue;
                p=hit.position;
                if(!NavMesh.CalculatePath(p,player.transform.position,filter,path)||path.status!=NavMeshPathStatus.PathComplete)continue;
                bool visible=false;
                foreach(float height in new[]{.3f,1f,1.8f}){
                    Vector3 target=p+Vector3.up*height;Vector3 screen=view.WorldToViewportPoint(target);
                    bool occluded=false;
                    Vector3 ray=target-view.transform.position;
                    foreach(var obstacle in Physics.RaycastAll(view.transform.position,ray.normalized,ray.magnitude,~0,QueryTriggerInteraction.Ignore))
                        if(!obstacle.transform.IsChildOf(player.transform)&&!obstacle.transform.IsChildOf(Mingi.transform)&&obstacle.collider.gameObject.isStatic){occluded=true;break;}
                    if(screen.z>0&&screen.x>-.12f&&screen.x<1.12f&&screen.y>-.12f&&screen.y<1.12f&&!occluded){visible=true;break;}
                }
                if(visible)continue;
                result=p;return true;
            }
            result=default;return false;
        }
        public void HitMingi(Vector3 direction)
        {
            if(!Ready||mission.Failing||MingiDown||Time.time<nextHit)return;
            MingiHits++;nextHit=Time.time+4.2f;
            ragdoll.Knock(direction);mission.Say(mission.dialogue.mingiHurt,3);
            if(MingiHits>=3){mission.Fail(true);}
        }
        public void Victory(){Ready=false;EncounterActive=false;Mingi.Following=true;}
        public void ResetDefense()
        {
            StopAllCoroutines();Ready=EncounterActive=AllSpawned=holding=receiving=false;MingiHits=0;
            mission.EndHandoff();
            if(ragdoll)ragdoll.Restore();
            Mingi.ScriptedTravel=false;Mingi.Following=true;
            foreach(var enemy in mission.enemies)if(enemy.approach){Destroy(enemy.approach.gameObject);enemy.approach=null;}
            var food=mission.carrying.transform;food.SetParent(foodParent,false);food.localPosition=foodPosition;food.localRotation=foodRotation;food.localScale=foodScale;
        }
        Vector3 PlatePosition()=>Mingi.transform.position+Vector3.up*1.05f+Mingi.transform.forward*.42f;
        void LateUpdate()
        {
            if((!holding&&!receiving)||!mission.carrying.activeSelf)return;
            var food=mission.carrying.transform;
            if(MingiDown){food.position=ragdoll.hips.position+ragdoll.hips.forward*.35f;food.rotation=ragdoll.hips.rotation;return;}
            if(holding)food.SetPositionAndRotation(PlatePosition(),Mingi.transform.rotation);
            var fingers=Mingi.animator.GetComponent<SyntyGripFingers>();if(fingers)fingers.Apply(null,false);
            Vector3 wrist=PlatePosition()-Mingi.transform.forward*.12f-Vector3.up*.035f;
            PoseArm(HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,HumanBodyBones.LeftHand,wrist-Mingi.transform.right*.16f);
            PoseArm(HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,HumanBodyBones.RightHand,wrist+Mingi.transform.right*.16f);
            PalmUp(HumanBodyBones.LeftHand,HumanBodyBones.LeftMiddleProximal,HumanBodyBones.LeftThumbProximal,1);
            PalmUp(HumanBodyBones.RightHand,HumanBodyBones.RightMiddleProximal,HumanBodyBones.RightThumbProximal,-1);
        }
        void PalmUp(HumanBodyBones hand,HumanBodyBones middle,HumanBodyBones thumb,float side)
        {
            var a=Mingi.animator;var h=a.GetBoneTransform(hand);var finger=a.GetBoneTransform(middle);var t=a.GetBoneTransform(thumb);
            if(!h)return;
            Transform index=null,other=null;
            foreach(var joint in h.GetComponentsInChildren<Transform>()){
                if(joint.name.StartsWith("Grip IndexFinger_01"))index=joint;
                if(joint.name.StartsWith("Grip Finger_01"))other=joint;
            }
            if(index&&other){
                Vector3 f=h.InverseTransformDirection((index.position+other.position)*.5f-h.position);
                Vector3 n=h.InverseTransformDirection(Vector3.Cross(index.position-h.position,other.position-h.position));
                if(f.sqrMagnitude>.00001f&&n.sqrMagnitude>.00000001f){h.rotation=Quaternion.LookRotation(Mingi.transform.forward,Vector3.up)*Quaternion.Inverse(Quaternion.LookRotation(f,n));return;}
            }
            // Synty의 간단한 손 리그는 Humanoid 손가락 매핑 대신 Finger.L/R 하나를 쓴다.
            var syntyFinger=h.Find(side>0?"Finger.L":"Finger.R");
            if(syntyFinger){h.rotation=Quaternion.LookRotation(Vector3.up,Mingi.transform.forward);syntyFinger.localRotation=Quaternion.identity;return;}
            if(!finger)finger=h.Find(side>0?"Finger.L":"Finger.R");
            if(!finger)return;
            Vector3 forward=finger.position-h.position;Vector3 normal=t?Vector3.Cross(forward,t.position-h.position)*side:h.forward;
            if(forward.sqrMagnitude<.00001f||normal.sqrMagnitude<.000001f)return;
            h.rotation=Quaternion.LookRotation(Mingi.transform.forward,Vector3.up)*Quaternion.Inverse(Quaternion.LookRotation(forward,normal))*h.rotation;
        }
        void PoseArm(HumanBodyBones upper,HumanBodyBones lower,HumanBodyBones hand,Vector3 target)
        {
            var a=Mingi.animator;if(!a.isHuman)return;
            var u=a.GetBoneTransform(upper);var l=a.GetBoneTransform(lower);var h=a.GetBoneTransform(hand);
            float upperLength=Vector3.Distance(u.position,l.position),lowerLength=Vector3.Distance(l.position,h.position);
            Vector3 delta=target-u.position;float distance=Mathf.Clamp(delta.magnitude,.05f,upperLength+lowerLength-.001f);Vector3 axis=delta.normalized;
            float along=(upperLength*upperLength-lowerLength*lowerLength+distance*distance)/(2*distance);
            Vector3 bend=Vector3.ProjectOnPlane(Vector3.down+Mingi.transform.right*(upper==HumanBodyBones.LeftUpperArm?-.3f:.3f),axis).normalized;
            Vector3 elbow=u.position+axis*along+bend*Mathf.Sqrt(Mathf.Max(0,upperLength*upperLength-along*along));
            u.rotation=Quaternion.FromToRotation(l.position-u.position,elbow-u.position)*u.rotation;
            l.rotation=Quaternion.FromToRotation(h.position-l.position,target-l.position)*l.rotation;
        }
        void OnGUI()
        {
            if(!Ready||mission.Failing||Time.timeScale<=0)return;
            var style=new GUIStyle(GUI.skin.label){font=MokUIFont.Font,fontSize=Mathf.RoundToInt(Screen.height*.026f),alignment=TextAnchor.MiddleCenter};
            style.normal.textColor=MingiHits>=2?new Color(1,.45f,.3f):new Color(1,.86f,.6f);
            GUI.Label(new Rect(Screen.width*.32f,Screen.height*.15f,Screen.width*.36f,Screen.height*.05f),"민기 보호  ·  피격 "+MingiHits+" / 3",style);
        }
    }
}
