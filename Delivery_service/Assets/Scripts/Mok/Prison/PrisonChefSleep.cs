using UnityEngine;
using UnityEngine.Rendering;
namespace DeliveryService.Mok
{
    public sealed class PrisonChefSleep : MonoBehaviour
    {
        PrisonChefReaction chef;
        TextMesh[] letters;
        AudioSource snore;
        Material material;
        void Start()
        {
            chef=GetComponent<PrisonChefReaction>();
            var sounds=PrisonCookingSounds.Current;
            snore=chef.head.gameObject.AddComponent<AudioSource>();
            snore.playOnAwake=false;snore.loop=true;snore.spatialBlend=1;
            snore.rolloffMode=AudioRolloffMode.Linear;snore.minDistance=1;snore.maxDistance=9;
            snore.clip=sounds?sounds.snore:null;
            letters=new TextMesh[3];
            for(int i=0;i<letters.Length;i++)
            {
                var obj=new GameObject("Sleep Z "+i);obj.transform.SetParent(transform,false);
                var text=obj.AddComponent<TextMesh>();text.text="Z";text.font=MokUIFont.Font;
                text.fontSize=64;text.characterSize=.02f;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;
                if(!material){material=new Material(text.font.material);if(material.HasProperty("_ZTest"))material.SetInt("_ZTest",(int)CompareFunction.LessEqual);}
                obj.GetComponent<MeshRenderer>().sharedMaterial=material;letters[i]=text;
            }
        }
        void LateUpdate()
        {
            if(!chef||letters==null)return;
            bool sleeping=chef.IsSleeping;
            if(snore){snore.volume=.2f*SuppliedAudio.Volume;if(!sleeping)snore.Stop();else if(Time.timeScale<=0)snore.Pause();else if(snore.clip&&!snore.isPlaying){snore.UnPause();if(!snore.isPlaying)snore.Play();}}
            var player=chef.playerView?chef.playerView.GetComponent<Camera>():null;
            Camera view=player&&player.enabled?player:Camera.main;
            if(!view){foreach(var c in Camera.allCameras)if(c.enabled){view=c;break;}}
            bool visible=sleeping&&Time.timeScale>0&&view;
            if(visible){Vector3 delta=chef.head.position-view.transform.position;visible=!Physics.Raycast(view.transform.position,delta.normalized,Mathf.Max(0,delta.magnitude-.45f),~0,QueryTriggerInteraction.Ignore);}
            for(int i=0;i<letters.Length;i++)
            {
                var text=letters[i];text.gameObject.SetActive(visible);if(!visible)continue;
                float phase=Mathf.Repeat(Time.time/2.4f+i/3f,1);
                text.transform.position=chef.head.position+Vector3.up*(.18f+phase*.75f)+view.transform.right*(phase*.18f);
                text.transform.rotation=view.transform.rotation;text.transform.localScale=Vector3.one*Mathf.Lerp(.65f,1.9f,phase);
                text.color=new Color(.83f,.94f,1,Mathf.Clamp01(phase*8)*Mathf.Clamp01((1-phase)*5));
            }
        }
        void OnDisable(){if(snore)snore.Stop();if(letters!=null)foreach(var t in letters)if(t)t.gameObject.SetActive(false);}
        void OnDestroy(){if(material)Destroy(material);if(snore)Destroy(snore);if(letters!=null)foreach(var t in letters)if(t)Destroy(t.gameObject);}
    }
}

