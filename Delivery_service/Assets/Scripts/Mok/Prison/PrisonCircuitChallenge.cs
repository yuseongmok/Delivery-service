using UnityEngine;
using UnityEngine.InputSystem;
namespace DeliveryService.Mok
{
    // 선의 연결을 눈으로 추적하는 퍼즐. 전류 측정과 타이밍 판정은 사용하지 않는다.
    public sealed class PrisonCircuitChallenge : MonoBehaviour
    {
        public PrisonCctvWirePanel panel;
        public int chosen=-1;
        Vector2[][] paths;PrisonCableDisplay cables;
        void OnDestroy(){cables?.Dispose();}
        int cutting=-1;float cutAge;
        void Update(){cables?.SetVisible(panel.Playing);if(!panel.Playing){cutting=-1;cutAge=0;}else if(cutting>=0&&Time.timeScale>0)cutAge+=Time.deltaTime;}
        public void ResetAttempt(){chosen=cutting=-1;cutAge=0;panel.correctWire=Random.Range(0,4);BuildPaths();}
        void Start(){ResetAttempt();}
        void BuildPaths(){
            paths=new Vector2[4][];
            for(int i=0;i<4;i++){
                paths[i]=new Vector2[13];paths[i][0]=new Vector2(625+i*210,735);
                for(int j=1;j<12;j++){int lane=(i+(j%2==0?j/2:3-j/2)+4)%4;paths[i][j]=new Vector2(610+lane*215+(j%2)*35+Mathf.Sin(j*1.7f+i)*17,735-j*43);}
                paths[i][1]=new Vector2(paths[i][0].x,690);
                // CCTV 단자는 정답 선과 실제 연결되며 다른 단자는 경보 회로다.
                int endpoint=(i-panel.correctWire+4)%4;paths[i][12]=new Vector2(625+endpoint*210,227);
            }
        }
        public bool Confirm(){if(chosen<0||!panel.Ready)return false;bool result=panel.Choose(chosen);if(result){cutting=chosen;cutAge=0;chosen=-1;}return result;}
        void OnGUI(){
            if(!panel||!panel.Playing||(!panel.Ready&&cutting<0)||Time.timeScale<=0)return;
            if(paths==null)BuildPaths();
            var matrix=GUI.matrix;var color=GUI.color;float s=Mathf.Min(Screen.width/1920f,Screen.height/1080f);
            GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1920*s)/2,(Screen.height-1080*s)/2,0),Quaternion.identity,Vector3.one*s);
            Box(new Rect(455,115,990,700),new Color(.045f,.065f,.075f));Box(new Rect(470,130,960,670),new Color(.15f,.18f,.19f));
            if(cables==null)cables=new PrisonCableDisplay();GUI.color=Color.white;GUI.DrawTexture(new Rect(455,115,990,700),cables.Render(paths,cutting>=0&&cutAge>=.55f?cutting:-1));
            Label(new Rect(490,140,910,42),"CCTV 단자에서 검정 선을 따라가 절단 지점을 클릭하세요",25,Color.white);
            for(int i=0;i<4;i++){
                Box(new Rect(565+i*210,185,120,42),i==0?new Color(.15f,.38f,.35f):new Color(.28f,.29f,.29f));
                Label(new Rect(565+i*210,185,120,42),i==0?"CCTV":"경보 "+i,24,Color.white);
            }
            for(int i=0;i<4;i++){
                var rect=new Rect(585+i*210,705,80,68);
                bool hover=rect.Contains(Event.current.mousePosition)&&panel.Ready;
                if(hover)Box(new Rect(rect.x,rect.yMax,rect.width,3),new Color(1,.75f,.35f));
                Label(new Rect(rect.x,rect.yMax+2,rect.width,30),"절단",21,hover?new Color(1,.8f,.45f):Color.white);
                if(panel.Ready&&Event.current.type==EventType.MouseDown&&Event.current.button==0&&rect.Contains(Event.current.mousePosition)){chosen=i;Event.current.Use();Confirm();}
            }
            if(cutting>=0){
                Vector2 tip=Vector2.Lerp(paths[cutting][0],paths[cutting][1],.4f);
                tip+=Vector2.down*170*(1-Mathf.SmoothStep(0,1,cutAge/.35f));
                float spread=28*(1-Mathf.Clamp01((cutAge-.35f)/.2f));
                Line(tip+new Vector2(-8-spread,-18),tip+new Vector2(8,28),14,new Color(.65f,.7f,.72f));
                Line(tip+new Vector2(8+spread,-18),tip+new Vector2(-8,28),14,new Color(.8f,.83f,.84f));
                Line(tip+new Vector2(8,28),tip+new Vector2(30,110),20,new Color(.24f,.27f,.28f));
                Line(tip+new Vector2(-8,28),tip+new Vector2(-30,110),20,new Color(.24f,.27f,.28f));
                Box(new Rect(tip.x-9,tip.y+12,18,18),new Color(.45f,.49f,.5f));
            }
            GUI.matrix=matrix;GUI.color=color;
        }
        static void Cable(Vector2 a,Vector2 b){Line(a,b,15,new Color(.4f,.43f,.44f));Line(a,b,10,new Color(.025f,.03f,.035f));}
        static void Box(Rect r,Color c){GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);}
        static void Label(Rect r,string t,int size,Color c){GUI.color=Color.white;var style=new GUIStyle(GUI.skin.label){font=MokUIFont.Font,fontSize=size,alignment=TextAnchor.MiddleCenter,wordWrap=true};style.normal.textColor=c;GUI.Label(r,t,style);}
        static void Line(Vector2 a,Vector2 b,float width,Color c){var old=GUI.matrix;GUI.matrix=old*Matrix4x4.TRS(a,Quaternion.Euler(0,0,Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg),Vector3.one);Box(new Rect(0,-width/2,Vector2.Distance(a,b),width),c);GUI.matrix=old;}
    }
}

