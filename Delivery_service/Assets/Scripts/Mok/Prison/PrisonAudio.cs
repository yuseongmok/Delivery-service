using UnityEngine;

namespace DeliveryService.Mok
{
    public sealed class PrisonAudio : MonoBehaviour
    {
        AudioSource ambience;
        void Start()
        {
            ambience=gameObject.AddComponent<AudioSource>();ambience.clip=SuppliedAudio.Current?.roomAmbience;
            ambience.loop=true;ambience.spatialBlend=0;ambience.volume=.18f*SuppliedAudio.Volume;
            if(ambience.clip)ambience.Play();
        }
        void Update(){if(ambience){ambience.volume=.18f*SuppliedAudio.Volume;if(Time.timeScale==0)ambience.Pause();else if(!ambience.isPlaying)ambience.UnPause();}}
        public static void Clink(Vector3 at,float volume=.25f)
        {
            var clip=SuppliedAudio.Current?.metalImpact;if(clip)AudioSource.PlayClipAtPoint(clip,at,volume*SuppliedAudio.Volume);
        }
    }
}
