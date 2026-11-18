using UnityEngine;
namespace DeliveryService.Mok
{
    // Scene waypoints are sampled by distance so corners do not change vehicle speed.
    public sealed class PrisonTransportRoute : MonoBehaviour
    {
        public Transform[] points;
        [System.NonSerialized] Vector3[] samples;[System.NonSerialized] float[] distances;
        public float Length {get{Ensure();return distances[distances.Length-1];}}
        void Ensure(){if(samples!=null&&samples.Length>1&&distances!=null&&distances.Length==samples.Length)return;var list=new System.Collections.Generic.List<Vector3>();for(int i=0;i<points.Length-1;i++){var a=points[Mathf.Max(0,i-1)].position;var b=points[i].position;var c=points[i+1].position;var d=points[Mathf.Min(points.Length-1,i+2)].position;for(int j=0;j<20;j++){float t=j/20f;list.Add(.5f*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t));}}list.Add(points[points.Length-1].position);samples=list.ToArray();distances=new float[samples.Length];for(int i=1;i<samples.Length;i++)distances[i]=distances[i-1]+Vector3.Distance(samples[i-1],samples[i]);}
        public Vector3 Position(float progress){Ensure();float distance=Mathf.Clamp01(progress)*Length;int i=System.Array.BinarySearch(distances,distance);if(i<0)i=~i;if(i<=0)return samples[0];if(i>=samples.Length)return samples[samples.Length-1];return Vector3.Lerp(samples[i-1],samples[i],Mathf.InverseLerp(distances[i-1],distances[i],distance));}
        public Quaternion Rotation(float progress){var delta=Position(Mathf.Min(1,progress+.002f))-Position(Mathf.Max(0,progress-.002f));return delta.sqrMagnitude>.00001f?Quaternion.LookRotation(delta,Vector3.up):transform.rotation;}
    }
}
