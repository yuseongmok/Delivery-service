using System.Collections;
using UnityEngine;
namespace DeliveryService.Mok
{
    // A short continuation of the forest shot; all existing shot indices remain stable.
    public sealed class AftermathEscapeBridge : MonoBehaviour
    {
        public AftermathActor player, mingi;
        public IEnumerator Play(AftermathFilm film)
        {
            var lines=film.dialogue.escapeBridge;
            if(lines==null || lines.Length==0)yield break;
            Vector3 playerStart=player.transform.position, mingiStart=mingi.transform.position;
            Quaternion originalView=film.film.transform.rotation;
            Quaternion mingiFacing=mingi.transform.rotation;
            Vector3 meetingPoint=playerStart+Vector3.forward*2.3f+Vector3.right*.9f;
            Vector3 toPlayer=playerStart-meetingPoint;toPlayer.y=0;
            Quaternion conversation=Quaternion.LookRotation(toPlayer);
            Vector3 escapeDirection=new Vector3(-1,0,-.35f).normalized;
            mingi.reach=0;mingi.leftContact=mingi.rightContact=null;mingi.walkSpeed=0;
            mingi.lookTarget=player.transform;mingi.lookWeight=.35f;
            film.title.text="";film.curtain.color=Color.clear;
            float elapsed=0;
            for(int i=0;i<lines.Length && !film.SkipRequested;i++)
            {
                var line=lines[i];float duration=Mathf.Max(1,line.seconds);
                film.voice.Stop();if(line.voice){film.voice.clip=line.voice;film.voice.Play();}
                for(float t=0;t<duration && !film.SkipRequested;)
                {
                    if(!film.Paused)
                    {
                        float delta=Time.captureDeltaTime>0?Time.captureDeltaTime:Time.unscaledDeltaTime;
                        film.subtitle.text=line.subtitle;
                        float settle=Mathf.SmoothStep(0,1,elapsed/1.4f);
                        float leave=i==lines.Length-1?Mathf.SmoothStep(0,1,Mathf.InverseLerp(.30f,1,t/duration)):0;
                        float follow=i==lines.Length-1?Mathf.SmoothStep(0,1,Mathf.InverseLerp(.48f,1,t/duration)):0;
                        float approach=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.25f,1.8f,elapsed));
                        mingi.transform.position=Vector3.Lerp(mingiStart,meetingPoint,approach)+escapeDirection*(2.8f*leave);
                        player.transform.position=playerStart+Vector3.left*(1.2f*follow);
                        Quaternion approachFacing=Quaternion.Slerp(mingiFacing,Quaternion.LookRotation(meetingPoint-mingiStart),Mathf.Clamp01(elapsed/.25f));
                        Quaternion restingFacing=Quaternion.Slerp(approachFacing,conversation,Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.6f,2.1f,elapsed)));
                        mingi.transform.rotation=Quaternion.Slerp(restingFacing,Quaternion.LookRotation(escapeDirection),Mathf.Clamp01(leave*3));
                        mingi.walkSpeed=((approach>0 && approach<.99f) || (leave>0 && leave<.99f))?1.1f:0;
                        mingi.lookWeight=.35f*(1-Mathf.Clamp01(leave*3));
                        var eye=player.transform.position+Vector3.up*film.firstPerson.eyeHeight+player.transform.forward*.14f;
                        film.film.transform.position=eye;
                        var look=Quaternion.LookRotation(mingi.transform.position+Vector3.up*1.43f-eye);
                        film.film.transform.rotation=Quaternion.Slerp(originalView,look,settle);
                        float fade=i==lines.Length-1?Mathf.Clamp01((t-duration+.85f)/.85f):0;
                        film.curtain.color=new Color(0,0,0,fade);
                        t+=delta;elapsed+=delta;
                    }
                    yield return null;
                }
            }
            mingi.walkSpeed=0;mingi.lookTarget=null;mingi.lookWeight=0;
            film.subtitle.text="";
        }
    }
}
