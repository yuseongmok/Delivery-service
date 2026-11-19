using UnityEngine;
namespace DeliveryService.Mok
{
    public static class PrisonDetailAudio
    {
        public enum Cue { Bite, Drawer, Paper, Message }
        static readonly AudioClip[] clips=new AudioClip[4];
        public static void Play(Cue cue,Vector3 position,float volume)
        {
            int index=(int)cue;if(!clips[index])clips[index]=Resources.Load<AudioClip>("PrisonDetailAudio/"+cue);
            if(clips[index])AudioSource.PlayClipAtPoint(clips[index],position,volume*SuppliedAudio.Volume);
        }
    }
}
