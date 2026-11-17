using UnityEngine;
namespace DeliveryService.Mok
{
    // 열쇠끼리 부딪히는 짧은 금속 공명. 전용 녹음이 생기면 Inspector에서 교체한다.
    public sealed class PrisonTheftAudio : MonoBehaviour
    {
        public AudioClip keyJingle;
        AudioSource keys,music;
        AudioClip generated;
        PrisonPickpocket owner;
        bool paused;
        public int JingleCount { get; private set; }
        public void Begin(PrisonPickpocket theft)
        {
            owner=theft;
            if(!keys){keys=gameObject.AddComponent<AudioSource>();keys.playOnAwake=false;}
            if(!music){music=gameObject.AddComponent<AudioSource>();music.playOnAwake=false;music.loop=true;music.volume=0;}
            if(!keyJingle){generated=BuildJingle();keyJingle=generated;}
            music.clip=SuppliedAudio.Current?SuppliedAudio.Current.missionMusic:null;
            if(music.clip&&!music.isPlaying)music.Play();
        }
        public void Jingle(float volume)
        {
            if(!keys||!keyJingle)return;
            JingleCount++;
            keys.Stop();keys.clip=keyJingle;keys.pitch=Random.Range(.93f,1.1f);
            keys.volume=volume*SuppliedAudio.Volume;keys.Play();
        }
        void Update()
        {
            if(!music)return;
            if(Time.timeScale<=0){if(!paused){music.Pause();keys.Pause();paused=true;}return;}
            if(paused){music.UnPause();keys.UnPause();paused=false;}
            var manager=DeliveryService.Yun.Audio.AudioManager.Instance;
            float setting=manager?manager.MasterVolume*manager.GetCategoryVolume(DeliveryService.Yun.Audio.SoundCategory.Bgm):PlayerPrefs.GetFloat("Yun.Settings.BgmVolume",1);
            if(manager&&(manager.MasterMuted||manager.GetCategoryMuted(DeliveryService.Yun.Audio.SoundCategory.Bgm)))setting=0;
            float target=owner&&owner.Playing?(keys.isPlaying?.045f:.09f)*setting:0;
            if(target>0&&music.clip&&!music.isPlaying)music.Play();
            music.volume=Mathf.MoveTowards(music.volume,target,Time.deltaTime*.35f);
            if(target==0&&music.volume<=0)music.Stop();
        }
        static AudioClip BuildJingle()
        {
            const int rate=44100;var samples=new float[(int)(rate*.42f)];
            float[] strikes={0,.032f,.079f,.137f};float[] frequencies={1820,2670,3410,4190};
            for(int i=0;i<samples.Length;i++){
                float t=(float)i/rate,value=0;
                for(int s=0;s<strikes.Length;s++){
                    float age=t-strikes[s];if(age<0)continue;
                    float attack=Mathf.Min(1,age*1600);
                    for(int f=0;f<frequencies.Length;f++)value+=Mathf.Sin(age*frequencies[f]*(1+s*.027f)*Mathf.PI*2)*Mathf.Exp(-age*(24+f*9))*attack*.12f/(1+s*.3f);
                }
                samples[i]=value;
            }
            var clip=AudioClip.Create("Prison key ring jingle",samples.Length,1,rate,false);clip.SetData(samples,0);return clip;
        }
        void OnDisable(){if(keys)keys.Stop();if(music)music.Stop();}
        void OnDestroy(){if(generated)Destroy(generated);}
    }
}
