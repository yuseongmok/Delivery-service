using UnityEngine;
namespace DeliveryService.Mok
{
    // 엄폐 대기 중에만 짧은 정지 신호를 보내며 이동 애니메이션은 유지한다.
    [DefaultExecutionOrder(2700)]
    public sealed class PrisonNightCompanionPose:MonoBehaviour
    {
        public PrisonNightMission mission;
        float weight;
        void LateUpdate(){
            if(!mission||Time.timeScale<=0)return;var friend=mission.Friend;var a=friend.animator;if(!a||!a.isHuman)return;
            bool waiting=mission.State==PrisonNightMission.Stage.Sneak&&friend.agent.enabled&&friend.agent.isOnNavMesh&&friend.agent.isStopped&&Vector3.Distance(friend.transform.position,mission.Player.transform.position)>2;
            weight=Mathf.MoveTowards(weight,waiting?1:0,Time.deltaTime*3);
            if(weight<=0)return;
            var arm=a.GetBoneTransform(HumanBodyBones.LeftUpperArm);var fore=a.GetBoneTransform(HumanBodyBones.LeftLowerArm);var hand=a.GetBoneTransform(HumanBodyBones.LeftHand);
            arm.rotation=Quaternion.Slerp(arm.rotation,Quaternion.FromToRotation(fore.position-arm.position,friend.transform.forward+Vector3.down*.2f)*arm.rotation,weight*.65f);
            fore.rotation=Quaternion.Slerp(fore.rotation,Quaternion.FromToRotation(hand.position-fore.position,Vector3.up)*fore.rotation,weight*.7f);
        }
    }
}
