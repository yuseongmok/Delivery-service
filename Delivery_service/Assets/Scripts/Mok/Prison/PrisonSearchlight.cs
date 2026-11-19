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
        public bool Hold;
        public AudioClip motorSound;AudioSource motor;float lastTravel;
        [Range(0,.3f)] public float endPause=.12f;
        float clock;
        public void ResetSweep() { clock = 0;Hold=false; Aim(); }
        public void SetRunning(bool value)
        {
            Running = value; lamp.enabled = value;if(!value&&motor)motor.Stop();
            if (beam) beam.enabled = value;
        }
        void Update() { if (!Running || Time.timeScale <= 0) return; if(!Hold)clock += Time.deltaTime; Aim(); }
        void Aim()
        {
            float travel=Mathf.PingPong((clock/period+phase)*2,1);
            float t=Mathf.SmoothStep(0,1,Mathf.InverseLerp(endPause,1-endPause,travel));
            var target = Vector3.Lerp(sweepFrom, sweepTo, t);
            transform.LookAt(target);
            if(Running&&Application.isPlaying&&motorSound){
                if(!motor){motor=gameObject.AddComponent<AudioSource>();motor.clip=motorSound;motor.spatialBlend=1;motor.minDistance=8;motor.maxDistance=70;motor.volume=.10f*SuppliedAudio.Volume;}
                if(!Hold&&Mathf.Abs(t-lastTravel)>.0001f&&!motor.isPlaying)motor.Play();
                if(Hold||t==0||t==1)motor.Stop();lastTravel=t;
            }
            if (beam) { beam.SetPosition(0, transform.position); beam.SetPosition(1, target);beam.endWidth=2*Mathf.Tan(lamp.spotAngle*.5f*Mathf.Deg2Rad)*Vector3.Distance(transform.position,target); }
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
