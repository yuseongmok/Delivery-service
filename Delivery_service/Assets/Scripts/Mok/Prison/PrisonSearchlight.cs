using System;
using UnityEngine;
namespace DeliveryService.Mok
{
    // 같은 광원 원뿔과 가림 판정을 사용해 보이는 빛과 발각 범위를 맞춘다.
    public sealed class PrisonSearchlight : MonoBehaviour
    {
        public Light lamp;
        public Vector3 sweepFrom, sweepTo;
        [Min(2)] public float period = 12;
        public float phase;
        public LineRenderer beam;
        public bool Running { get; private set; }
        float clock;
        public void ResetSweep() { clock = 0; Aim(); }
        public void SetRunning(bool value)
        {
            Running = value; lamp.enabled = value;
            if (beam) beam.enabled = value;
        }
        void Update() { if (!Running || Time.timeScale <= 0) return; clock += Time.deltaTime; Aim(); }
        void Aim()
        {
            float t = .5f - .5f * Mathf.Cos((clock / period + phase) * Mathf.PI * 2);
            var target = Vector3.Lerp(sweepFrom, sweepTo, t);
            transform.LookAt(target);
            if (beam) { beam.SetPosition(0, transform.position); beam.SetPosition(1, target); }
        }
        public bool Illuminates(Vector3 point, Transform actor = null)
        {
            if (!Running) return false;
            var delta = point - lamp.transform.position;
            if (delta.sqrMagnitude > lamp.range * lamp.range || Vector3.Angle(lamp.transform.forward, delta) > lamp.spotAngle * .5f) return false;
            var hits = Physics.RaycastAll(lamp.transform.position, delta.normalized, delta.magnitude, ~0, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (a,b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                if (hit.transform.IsChildOf(transform)) continue;
                if (actor && hit.transform.IsChildOf(actor)) return true;
                if (hit.distance < delta.magnitude - .08f) return false;
            }
            return true;
        }
    }
}
