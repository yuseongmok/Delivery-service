using UnityEngine;

namespace DeliveryService.Mok
{
    // A race recovery never invokes the city's paid towing/hospital flow.
    [DefaultExecutionOrder(-1000)]
    public sealed class RaceRecovery : MonoBehaviour
    {
        RaceManager race;
        MotorcycleController bike;
        Transform anchor;
        Vector3 safeSphere, localOffset;
        Quaternion safeRotation;
        CollisionDetectionMode originalCollision;
        bool tracking;
        float noticeUntil;
        public int Recoveries { get; private set; }

        void Awake() { race = GetComponent<RaceManager>(); }
        void Update()
        {
            if (!race.IsRaceActive || race.ShowingResult) { StopTracking(); return; }
            if (!bike) bike = FindFirstObjectByType<MotorcycleController>();
            if (!bike || !bike.sphereRB || !bike.isDriven || !race.DrivingStarted) return;
            if (!tracking)
            {
                originalCollision = bike.sphereRB.collisionDetectionMode;
                bike.sphereRB.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                localOffset = Quaternion.Inverse(bike.transform.rotation) * (bike.transform.position - bike.sphereRB.position);
                safeSphere = bike.sphereRB.position; safeRotation = bike.transform.rotation;
                tracking = true;
            }
            if (!race.DrivingStarted) return;
            var position = bike.sphereRB.position;
            if (position.y < safeSphere.y - 1.3f) { Recover(); return; }
            float radius = bike.sphereCollider.bounds.extents.y;
            foreach (var hit in Physics.RaycastAll(position, Vector3.down, radius + .18f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider == bike.sphereCollider || hit.transform.IsChildOf(bike.transform) || hit.normal.y < .9f || hit.rigidbody) continue;
                // Keep the last supported pose; jumps and falling positions are not checkpoints.
                if (Mathf.Abs(bike.sphereRB.linearVelocity.y) > .5f) continue;
                safeSphere = hit.point + Vector3.up * (radius + .06f);
                safeRotation = Quaternion.Euler(0, bike.transform.eulerAngles.y, 0);
                break;
            }
        }

        public bool Recover()
        {
            if (!tracking || !race.IsRaceActive || race.ShowingResult || !bike || !bike.isDriven) return false;
            if (!anchor) { anchor = new GameObject("Race safe return").transform; anchor.SetParent(transform); }
            anchor.SetPositionAndRotation(safeSphere + safeRotation * localOffset, safeRotation);
            bike.RespawnBike(anchor);
            bike.sphereRB.position = safeSphere;
            bike.sphereRB.transform.position = safeSphere;
            bike.sphereRB.linearVelocity = Vector3.zero;
            bike.sphereRB.angularVelocity = Vector3.zero;
            bike.transform.SetPositionAndRotation(anchor.position, safeRotation);
            if (bike.Rider) bike.Rider.transform.position = anchor.position;
            Physics.SyncTransforms();
            bike.SetControllable(race.DrivingStarted);
            Recoveries++; noticeUntil = Time.unscaledTime + 2;
            return true;
        }

        void StopTracking()
        {
            if (tracking && bike && bike.sphereRB) bike.sphereRB.collisionDetectionMode = originalCollision;
            tracking = false;
        }
        void OnDisable() { StopTracking(); }
        void OnGUI()
        {
            if (Time.unscaledTime >= noticeUntil) return;
            var style = new GUIStyle(GUI.skin.label) { font = MokUIFont.Font, fontSize = Mathf.RoundToInt(Screen.height * .025f), alignment = TextAnchor.MiddleCenter };
            GUI.Label(new Rect(0, Screen.height * .23f, Screen.width, 50), "안전한 코스로 복귀했습니다", style);
        }
    }
}
