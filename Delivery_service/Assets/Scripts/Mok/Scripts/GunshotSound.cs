using UnityEngine;
namespace DeliveryService.Mok {
public sealed class GunshotSound:MonoBehaviour {
 AudioSource source;AudioClip clip;
 void Awake(){source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;StorySoundPause.Attach(source);source.volume=.4f;source.minDistance=3;source.maxDistance=65;source.rolloffMode=AudioRolloffMode.Linear;
  if(SuppliedAudio.Current&&SuppliedAudio.Current.gunshot){clip=SuppliedAudio.Current.gunshot;return;}
  const int rate=44100;var samples=new float[rate/3];var rng=new System.Random(419);
  for(int i=0;i<samples.Length;i++){float t=i/(float)rate;float attack=Mathf.Min(1,t/.0015f);samples[i]=attack*((float)(rng.NextDouble()*2-1)*Mathf.Exp(-t*38)*.65f+Mathf.Sin(2*Mathf.PI*95*t)*Mathf.Exp(-t*24)*.3f);}
  clip=AudioClip.Create("권총 발사",samples.Length,1,rate,false);clip.SetData(samples,0);
 }
 public static void Play(GameObject owner,bool spatial){StorySoundMix.Duck(.3f);var sound=owner.GetComponent<GunshotSound>()??owner.AddComponent<GunshotSound>();sound.source.spatialBlend=spatial?1:0;sound.source.pitch=Random.Range(.96f,1.04f);sound.source.PlayOneShot(sound.clip,SuppliedAudio.Volume);}
 void OnDestroy(){if(clip!=null&&(!SuppliedAudio.Current||clip!=SuppliedAudio.Current.gunshot))Destroy(clip);if(source!=null)Destroy(source);}
}
}
