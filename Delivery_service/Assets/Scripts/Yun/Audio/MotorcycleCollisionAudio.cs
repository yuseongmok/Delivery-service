using UnityEngine;

namespace DeliveryService.Yun.Audio
{
    [DefaultExecutionOrder(-200)]
    [DisallowMultipleComponent]
    public sealed class MotorcycleCollisionAudio : MonoBehaviour
    {
        [SerializeField] private MotorcycleController motorcycle;
        [SerializeField, Min(0)] private float minimumImpactSpeed = 2f;
        [SerializeField, Min(.01f)] private float cooldown = .25f;
        private float nextImpact;
        private float speedBeforePhysics;
        private bool ridingBeforePhysics;

        public void Bind(MotorcycleController bike) { motorcycle = bike; }
        private void Awake() { if (motorcycle == null) motorcycle = GetComponent<MotorcycleController>(); }
        private void FixedUpdate()
        {
            ridingBeforePhysics = motorcycle != null && motorcycle.isDriven;
            speedBeforePhysics = motorcycle == null ? 0 : Mathf.Abs(motorcycle.CurrentSpeed);
        }

        public void Contact(Collider other, Vector3 point, float impactSpeed, bool trigger)
        {
            if (!isActiveAndEnabled || Time.timeScale <= 0 || motorcycle == null || other == null
                || !(motorcycle.isDriven || ridingBeforePhysics) || Time.time < nextImpact) return;
            if (other.transform.IsChildOf(motorcycle.transform) || (motorcycle.sphereRB != null && other.attachedRigidbody == motorcycle.sphereRB)
                || (motorcycle.bikeBody != null && other.attachedRigidbody == motorcycle.bikeBody)) return;
            bool person = other.GetComponentInParent<NPCControl>() != null || other.GetComponentInParent<RobberAI>() != null;
            if (trigger && !person) return; // Gas stations, delivery zones, etc. are not solid impacts.
            float strength = trigger ? Mathf.Max(speedBeforePhysics, Mathf.Abs(motorcycle.CurrentSpeed)) : impactSpeed;
            if (strength < minimumImpactSpeed) return;
            var manager = AudioManager.Instance;
            if (manager == null) return;
            var handle = manager.PlayAt(person ? SoundId.MotorcycleNpcHit : SoundId.MotorcycleObstacleHit, point);
            if (!manager.IsHandleValid(handle)) return;
            manager.SetVolume(handle, Mathf.Lerp(.35f, 1, Mathf.InverseLerp(minimumImpactSpeed, 20, strength)));
            nextImpact = Time.time + cooldown; // Shared by all colliders on this bike.
        }
    }
}
