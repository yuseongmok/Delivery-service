using UnityEngine;
namespace DeliveryService.Mok
{
    // One continuous voice and one feedback voice: repeated input cannot stack sounds.
    public sealed class PrisonCookingAudio : MonoBehaviour
    {
        AudioSource loop, feedback, music, slice;
        bool musicPaused;
        float sliceUntil;
        public bool MusicPlaying => music && music.isPlaying;
        public int SliceCount { get; private set; }
        public int MusicStartCount { get; private set; }
        PrisonCookingSounds sounds;
        float loopUntil, shotUntil;
        Vector2 brushPosition;
        public void Brush(Vector2 position,bool pressed,bool overFood)
        {
            bool moved=(position-brushPosition).sqrMagnitude>.25f;brushPosition=position;
            if(pressed && overFood && moved)Sustain("brush");
        }
        public int ChopCount { get; private set; }
        public bool LoopPlaying => loop && loop.isPlaying;
        public string LoopName => loop && loop.clip ? loop.clip.name : "";
        static PrisonCookingAudio current;
        public static PrisonCookingAudio For(PrisonChickenQuest quest)
        {
            if(!quest)return null;
            var audio=quest.GetComponent<PrisonCookingAudio>();
            if(!audio)audio=quest.gameObject.AddComponent<PrisonCookingAudio>();
            return audio;
        }
        void Awake()
        {
            current=this;sounds=PrisonCookingSounds.Current;
            loop=Voice();feedback=Voice();music=Voice();slice=Voice();
            music.loop=true;music.clip=sounds?sounds.music:null;music.volume=0;
        }
        AudioSource Voice(){var s=gameObject.AddComponent<AudioSource>();s.playOnAwake=false;s.spatialBlend=0;return s;}
        public static void Cue(string kind)
        {
            var audio=For(PrisonChickenQuest.Current);
            if(audio)audio.PlayCue(kind);
        }
        public void PlayCue(string kind)
        {
            if(!sounds||Time.timeScale<=0)return;
            AudioClip clip=null;float volume=.25f, duration=.25f;
            switch(kind)
            {
                case "cut":clip=sounds.chop;volume=.5f;duration=.3f;ChopCount++;break;
                case "pickup":clip=sounds.pickup;volume=.23f;duration=.16f;break;
                case "tick":clip=sounds.tick;volume=.16f;duration=.07f;break;
                case "flip":clip=sounds.flip;volume=.2f;duration=.12f;break;
                case "complete":clip=sounds.confirm;volume=.32f;duration=.65f;break;
                case "success":clip=sounds.confirm;volume=.18f;duration=.25f;break;
            }
            if(!clip)return;
            feedback.Stop();feedback.clip=clip;feedback.volume=volume*SuppliedAudio.Volume;
            feedback.pitch=kind=="cut"?Random.Range(.95f,1.05f):1;
            feedback.Play();shotUntil=Time.time+duration;
            if(kind=="cut" && sounds.slice)
            {
                slice.Stop();slice.clip=sounds.slice;slice.pitch=feedback.pitch;
                slice.volume=.42f*SuppliedAudio.Volume;slice.Play();
                sliceUntil=Time.time+.32f;SliceCount++;
            }
        }
        public void Sustain(string kind, float strength=1)
        {
            if(!sounds||Time.timeScale<=0)return;
            AudioClip clip=kind=="flour"?sounds.flour:kind=="liquid"?sounds.liquid:kind=="stir"?sounds.stir:kind=="fry"?sounds.fry:kind=="brush"?sounds.brush:null;
            if(!clip)return;
            if(loop.clip!=clip){loop.Stop();loop.clip=clip;}
            loop.loop=true;loop.pitch=kind=="stir"?Mathf.Lerp(.9f,1.15f,Mathf.Clamp01(strength)):1;
            loop.volume=(kind=="fry"?.22f:.38f)*SuppliedAudio.Volume;
            if(!loop.isPlaying)loop.Play();
            loopUntil=Time.time+.07f;
        }
        public void StopAction(){if(loop)loop.Stop();loopUntil=0;}
        void LateUpdate()
        {
            var quest=GetComponent<PrisonChickenQuest>();
            bool cooking=quest && quest.IsPlayingCustomCooking && quest.teamMission &&
                !quest.teamMission.Failing && quest.teamMission.CookingActive &&
                (!quest.teamMission.chefReaction || quest.teamMission.chefReaction.IsSleeping);
            var mission=quest?quest.teamMission:null;
            bool missionMusic=mission && !mission.Failing &&
                (mission.State==PrisonChickenMission.Stage.Kitchen || mission.State==PrisonChickenMission.Stage.Cook ||
                 mission.State==PrisonChickenMission.Stage.Carry || mission.State==PrisonChickenMission.Stage.Fight ||
                 mission.State==PrisonChickenMission.Stage.Deliver) &&
                (!mission.chefReaction || mission.chefReaction.IsSleeping);
            if(!missionMusic){music.Stop();music.volume=0;musicPaused=false;}
            if(Time.timeScale<=0){loop.Pause();feedback.Pause();slice.Pause();if(music.isPlaying){music.Pause();musicPaused=true;}return;}
            if(missionMusic && music.clip)
            {
                if(musicPaused){music.UnPause();musicPaused=false;}
                if(!music.isPlaying){music.Play();MusicStartCount++;}
                var manager=DeliveryService.Yun.Audio.AudioManager.Instance;
                float setting=manager?manager.MasterVolume*manager.GetCategoryVolume(DeliveryService.Yun.Audio.SoundCategory.Bgm):PlayerPrefs.GetFloat("Yun.Settings.BgmVolume",1);
                if(manager && (manager.MasterMuted || manager.GetCategoryMuted(DeliveryService.Yun.Audio.SoundCategory.Bgm)))setting=0;
                float target=(loop.isPlaying || feedback.isPlaying || slice.isPlaying ? .055f : .12f)*setting;
                music.volume=Mathf.MoveTowards(music.volume,target,Time.deltaTime*.6f);
            }
            // Actions must be refreshed by accepted input each frame.
            if(!quest||!quest.IsPlayingCustomCooking||Time.time>loopUntil)loop.Stop();
            if(Time.time>shotUntil)feedback.Stop();else feedback.UnPause();
            if(Time.time>sliceUntil || !cooking)slice.Stop();else slice.UnPause();
        }
        void OnDisable(){loop?.Stop();feedback?.Stop();music?.Stop();slice?.Stop();}
        void OnDestroy(){if(current==this)current=null;}
    }
}



