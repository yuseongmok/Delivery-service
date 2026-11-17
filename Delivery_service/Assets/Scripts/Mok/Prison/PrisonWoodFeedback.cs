using UnityEngine;
namespace DeliveryService.Mok
{
    // 입력/성공 판정과 분리된 작업대 손, 파편, 타격 피드백.
    [DefaultExecutionOrder(300)]
    public sealed class PrisonWoodFeedback : MonoBehaviour
    {
        public PrisonFirstWork work;
        public Transform[] hands, sleeves, chips;
        public AudioClip impactClip;
        public int ImpactCount { get; private set; }
        Camera view; AudioSource sound;
        Vector3[] velocity, spin;
        float age=10, shake;
        Vector3 basePosition; Quaternion baseRotation;
        bool offsetApplied;
        public void Open(Camera camera){view=camera;age=10;shake=0;foreach(var hand in hands)hand.gameObject.SetActive(true);foreach(var sleeve in sleeves)sleeve.gameObject.SetActive(true);}
        void Awake(){velocity=new Vector3[chips.Length];spin=new Vector3[chips.Length];sound=gameObject.AddComponent<AudioSource>();sound.playOnAwake=false;sound.spatialBlend=0;Close();}
        public void Hit(bool split){
            ImpactCount++;sound.Stop();sound.clip=impactClip;sound.volume=(split?.48f:.22f)*SuppliedAudio.Volume;sound.pitch=Random.Range(.78f,.88f);if(impactClip)sound.Play();
            shake=split?.18f:.07f;
            if(!split)return;age=0;
            for(int i=0;i<chips.Length;i++){var c=chips[i];c.gameObject.SetActive(true);c.position=work.station.position+Random.insideUnitSphere*.035f;c.rotation=Random.rotation;velocity[i]=new Vector3(Random.Range(-1.7f,1.7f),Random.Range(1.1f,2.1f),Random.Range(-.7f,.8f));spin[i]=Random.insideUnitSphere*500;}
        }
        void RestoreCamera(){if(offsetApplied&&view){view.transform.SetPositionAndRotation(basePosition,baseRotation);offsetApplied=false;}}
        void LateUpdate(){
            RestoreCamera();
            if(work.Working&&view){for(int i=0;i<hands.Length;i++){Vector3 wrist=hands[i].position;Vector3 elbow=view.transform.TransformPoint(new Vector3(i==0?-.36f:.36f,-.36f,.3f));var sleeve=sleeves[i];sleeve.position=(elbow+wrist)*.5f;sleeve.rotation=Quaternion.FromToRotation(Vector3.up,wrist-elbow);sleeve.localScale=new Vector3(.075f,Vector3.Distance(elbow,wrist)*.5f,.07f);}}
            if(Time.timeScale<=0)return;
            if(age<1){age+=Time.deltaTime;for(int i=0;i<chips.Length;i++){var c=chips[i];velocity[i]+=Vector3.down*8*Time.deltaTime;c.position+=velocity[i]*Time.deltaTime;c.Rotate(spin[i]*Time.deltaTime,Space.World);if(c.position.y<work.station.position.y-.06f||age>=.85f)c.gameObject.SetActive(false);}}
            if(shake>0&&work.Working&&view){shake=Mathf.Max(0,shake-Time.deltaTime);basePosition=view.transform.position;baseRotation=view.transform.rotation;float envelope=shake/.18f;view.transform.position+=view.transform.up*(Mathf.Sin(shake*115)*.012f*envelope);view.transform.rotation=baseRotation*Quaternion.Euler(Mathf.Sin(shake*90)*.65f*envelope,0,Mathf.Sin(shake*105)*.25f*envelope);offsetApplied=true;}
        }
        public void Close(){RestoreCamera();shake=0;age=10;foreach(var h in hands)if(h)h.gameObject.SetActive(false);foreach(var s in sleeves)if(s)s.gameObject.SetActive(false);foreach(var c in chips)if(c)c.gameObject.SetActive(false);}
        void OnDisable(){Close();if(sound)sound.Stop();}
    }
}
