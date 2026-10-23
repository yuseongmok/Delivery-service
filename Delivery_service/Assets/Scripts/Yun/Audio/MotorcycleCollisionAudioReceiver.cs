using UnityEngine;

namespace DeliveryService.Yun.Audio
{
    [DefaultExecutionOrder(-200)]
    [DisallowMultipleComponent]
    public sealed class MotorcycleCollisionAudioReceiver : MonoBehaviour
    {
        [SerializeField] private MotorcycleCollisionAudio audioOwner;
        public void Bind(MotorcycleCollisionAudio owner) { audioOwner = owner; }
        private void OnTriggerEnter(Collider other)
        {
            if (audioOwner == null || other == null) return;
            // 오목한 메시에는 ClosestPoint를 지원하지 않으므로 경계 상자의 접점을 사용한다.
            var mesh = other as MeshCollider;
            Vector3 point = mesh != null && !mesh.convex
                ? other.bounds.ClosestPoint(transform.position)
                : other.ClosestPoint(transform.position);
            audioOwner.Contact(other, point, 0, true);
        }
        private void OnCollisionEnter(Collision collision)
        {
            if (audioOwner == null) return;
            for (int i = 0; i < collision.contactCount; i++)
            {
                var contact = collision.GetContact(i);
                // Ignore road support contacts and landings; side impacts remain audible.
                if (Mathf.Abs(Vector3.Dot(contact.normal, Vector3.up)) > .7f) continue;
                audioOwner.Contact(collision.collider, contact.point, collision.relativeVelocity.magnitude, false);
                break;
            }
        }
    }
}
