using UnityEngine;
using UnityEngine.AI;

namespace DeliveryService.Mok
{
    public sealed class PrisonKitchenAccess : MonoBehaviour
    {
        public PrisonChickenMission mission;
        public Transform leftDoor, rightDoor;
        public NavMeshObstacle obstacle;
        public Vector3 entryCentre = new Vector3(0, 1.3f, -1.3f);
        public Vector3 entrySize = new Vector3(3, 2.8f, 1.6f);
        [Min(.1f)] public float openingSeconds = 1.15f;
        float opening;
        public bool IsOpen => opening >= .99f;
        public bool PlayerInside => mission && mission.prison.player &&
            new Bounds(entryCentre,entrySize).Contains(transform.InverseTransformPoint(mission.prison.player.transform.position));
        void Update()
        {
            if (!mission || Time.timeScale <= 0) return;
            bool unlocked = mission.State != PrisonChickenMission.Stage.Waiting;
            opening = Mathf.MoveTowards(opening, unlocked ? 1 : 0, Time.deltaTime / openingSeconds);
            float angle = Mathf.SmoothStep(0, 100, opening);
            if (leftDoor) leftDoor.localRotation = Quaternion.Euler(0, angle, 0);
            if (rightDoor) rightDoor.localRotation = Quaternion.Euler(0, -angle, 0);
            if (obstacle) obstacle.enabled = !IsOpen;
            if (IsOpen && PlayerInside && !mission.Failing && mission.State == PrisonChickenMission.Stage.Kitchen)
                mission.ResetKitchen();
        }
        void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(entryCentre,entrySize);
        }
    }
}
