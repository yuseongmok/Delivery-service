using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace DeliveryService.Mok
{
    // 취침 시점 이동만 담당한다. 미션 입력과 시간 경과는 도입부가 관리한다.
    public sealed class PrisonBedtimePresentation : MonoBehaviour
    {
        public Vector3 bedside = new Vector3(-11.0f,1.35f,-24.8f);
        public Vector3 seated = new Vector3(-11.75f,1.15f,-24.1f);
        public Vector3 pillow = new Vector3(-12f,.92f,-22.85f);
        [Min(.5f)] public float approachSeconds=1.6f, sitSeconds=1.1f, lieSeconds=2.2f;
        readonly List<Renderer> hidden=new List<Renderer>();
        public bool Lying { get; private set; }
        public IEnumerator LieDown(PrisonIntroduction intro)
        {
            var camera=intro.filmCamera;
            foreach(var r in intro.mission.player.GetComponentsInChildren<Renderer>())if(r.enabled){r.enabled=false;hidden.Add(r);}
            yield return Move(camera,bedside,Quaternion.LookRotation(seated-bedside),approachSeconds);
            yield return Move(camera,seated,Quaternion.LookRotation(pillow-seated+Vector3.up*.6f),sitSeconds);
            yield return Move(camera,pillow,Quaternion.LookRotation(Vector3.up,Vector3.forward),lieSeconds);
            Lying=true;
        }
        public static IEnumerator Move(Camera camera,Vector3 to,Quaternion rotation,float seconds)
        {
            var start=camera.transform.position;var from=camera.transform.rotation;
            for(float t=0;t<seconds;t+=Time.deltaTime){float f=Mathf.SmoothStep(0,1,t/seconds);camera.transform.SetPositionAndRotation(Vector3.Lerp(start,to,f),Quaternion.Slerp(from,rotation,f));yield return null;}
            camera.transform.SetPositionAndRotation(to,rotation);
        }
        public void Restore(){foreach(var r in hidden)if(r)r.enabled=true;hidden.Clear();Lying=false;}
        void OnDisable(){Restore();}
    }
}
