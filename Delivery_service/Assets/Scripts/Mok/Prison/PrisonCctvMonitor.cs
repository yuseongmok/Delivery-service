using UnityEngine;
namespace DeliveryService.Mok
{
    public sealed class PrisonCctvMonitor:MonoBehaviour
    {
        public Camera feed;
        public Renderer screen;
        public Material offline;
        public bool Disabled{get;private set;}
        RenderTexture image;Material live;float next;
        void Start(){feed.enabled=false;image=new RenderTexture(256,144,16);image.Create();feed.targetTexture=image;live=new Material(Shader.Find("Universal Render Pipeline/Unlit"));live.mainTexture=image;screen.sharedMaterial=Disabled?offline:live;}
        void Update(){if(Disabled||Time.timeScale<=0||Time.time<next||!PrisonMission.Current)return;next=Time.time+.5f;if(Vector3.Distance(PrisonMission.Current.player.transform.position,transform.position)<16)feed.Render();}
        public void SetDisabled(bool value){Disabled=value;if(screen)screen.sharedMaterial=value?offline:live;}
        void OnDestroy(){if(feed)feed.targetTexture=null;if(image){image.Release();Destroy(image);}if(live)Destroy(live);}
    }
}
