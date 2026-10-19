using UnityEngine;
namespace DeliveryService.Mok {
// 강탈 성공 후에만 경고와 추적 표시를 제공한다.
public sealed class RobberTheftFeedback : MonoBehaviour {
    public const string WarningText="피자를 강탈당했습니다.";
    string warningMessage=WarningText;
    RobberAI robber;
    PlayerCombat player;
    AudioSource source;
    AudioClip alarm;
    Texture2D arrow;
    static RobberTheftFeedback visibleWarning;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void ResetWarning(){visibleWarning=null;}
    float warningUntil=-1;
    public bool WarningVisible => visibleWarning==this&&Time.unscaledTime<warningUntil;
    public bool MarkerVisible => robber!=null&&robber.HasStolenLoot;
    public int AlertCount {get;private set;}
    public bool AlarmPlaying => source!=null&&source.isPlaying;
    void Awake(){
        robber=GetComponent<RobberAI>();
        source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=0;source.volume=.65f;
        // 짧은 두 음 경고음. 가장자리에 페이드를 넣어 클릭 잡음을 막는다.
        const int rate=44100;var samples=new float[(int)(rate*.55f)];
        for(int i=0;i<samples.Length;i++){
            float t=(float)i/rate;float local=t<.25f?t:t-.30f;
            if(local<0||local>.25f)continue;
            float envelope=Mathf.Min(1,local/.015f)*Mathf.Clamp01((.25f-local)/.06f);
            float frequency=t<.25f?880:660;
            samples[i]=Mathf.Sin(2*Mathf.PI*frequency*local)*envelope*.45f;
        }
        alarm=AudioClip.Create("피자 강탈 경고",samples.Length,1,rate,false);alarm.SetData(samples,0);
        arrow=new Texture2D(32,32,TextureFormat.RGBA32,false);arrow.filterMode=FilterMode.Bilinear;
        for(int y=0;y<32;y++)for(int x=0;x<32;x++)arrow.SetPixel(x,y,Mathf.Abs(x-15.5f)<=y*.5f?Color.white:Color.clear);
        arrow.Apply();
    }
    public void NotifyTheft(){
        if(!MarkerVisible)return;
        visibleWarning=this;
        warningMessage=robber.StolenCash>0?$"{robber.StolenCash:N0}원을 강탈당했습니다.":WarningText;
        warningUntil=Time.unscaledTime+4f;AlertCount++;source.PlayOneShot(alarm);
    }
    void OnGUI(){
        if(Time.timeScale<=0||Cursor.lockState!=CursorLockMode.Locked)return;
        var previous=GUI.color;
        if(WarningVisible){
            float alpha=Mathf.Clamp01(warningUntil-Time.unscaledTime);
            var rect=new Rect(Screen.width*.25f,Screen.height*.22f,Screen.width*.5f,64);
            GUI.color=new Color(.12f,.025f,.02f,.88f*alpha);GUI.DrawTexture(rect,Texture2D.whiteTexture);
            GUI.color=new Color(1,1,1,alpha);
            var style=new GUIStyle(GUI.skin.label){font=MokUIFont.Font,fontSize=Mathf.Clamp(Screen.height/28,22,38),alignment=TextAnchor.MiddleCenter};
            style.normal.textColor=new Color(1,.38f,.22f);GUI.Label(rect,warningMessage,style);
        }
        if(MarkerVisible){
            if(player==null)player=FindFirstObjectByType<PlayerCombat>(FindObjectsInactive.Include);
            var cargo=RobberAI.RiddenCargo;
            var camera=cargo!=null&&cargo.bikeController.bikeCamera!=null?cargo.bikeController.bikeCamera.GetComponentInChildren<Camera>():player!=null?player.View:null;
            if(camera!=null){
                float size=Mathf.Clamp(Screen.height*.045f,26,48);
                var viewport=camera.WorldToViewportPoint(transform.position+Vector3.up*2.2f);
                var marker=ProjectMarker(viewport,new Vector2(Screen.width,Screen.height),size);
                var matrix=GUI.matrix;
                GUIUtility.RotateAroundPivot(marker.z,new Vector2(marker.x,marker.y));
                GUI.color=new Color(.1f,.03f,.01f,1);GUI.DrawTexture(new Rect(marker.x-size*.5f-2,marker.y-size*.5f-2,size+4,size+4),arrow);
                GUI.color=new Color(1,.32f,.12f,1);GUI.DrawTexture(new Rect(marker.x-size*.5f,marker.y-size*.5f,size,size),arrow);
                GUI.matrix=matrix;
            }
        }
        GUI.color=previous;
    }
    // 화면 좌표와 화살표 각도를 구한다. 뒤쪽 투영은 반전 보정한다.
    public static Vector3 ProjectMarker(Vector3 viewport,Vector2 screen,float margin){
        var center=screen*.5f;
        if(viewport.z>0&&viewport.x>=0&&viewport.x<=1&&viewport.y>=0&&viewport.y<=1)
            return new Vector3(Mathf.Clamp(viewport.x*screen.x,margin,screen.x-margin),Mathf.Clamp((1-viewport.y)*screen.y-margin*.5f,margin,screen.y-margin),0);
        var direction=new Vector2((viewport.x-.5f)*screen.x,(.5f-viewport.y)*screen.y);
        if(viewport.z<=0)direction=-direction;
        // 정확히 뒤에 있으면 아래쪽 화살표로 돌아보도록 안내한다.
        if(direction.sqrMagnitude<.001f)direction=Vector2.up;
        var half=new Vector2(Mathf.Max(1,center.x-margin),Mathf.Max(1,center.y-margin));
        float scale=1/Mathf.Max(Mathf.Abs(direction.x)/half.x,Mathf.Abs(direction.y)/half.y);
        var position=center+direction*scale;
        return new Vector3(position.x,position.y,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg-90);
    }
    void OnDestroy(){if(alarm!=null)Destroy(alarm);if(arrow!=null)Destroy(arrow);}
}}


