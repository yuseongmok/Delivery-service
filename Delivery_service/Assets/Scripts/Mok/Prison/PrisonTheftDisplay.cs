using UnityEngine;
namespace DeliveryService.Mok
{
    // 손과 열쇠를 가리지 않는 근접 조작용 계기판과 안내 카드.
    public static class PrisonTheftDisplay
    {
        static readonly Color Ink=new Color(.035f,.052f,.064f,.94f), Brass=new Color(.68f,.58f,.36f), Cream=new Color(1,.94f,.8f);
        static void Box(Rect r,Color c){GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);}
        static void Line(Vector2 a,Vector2 b,float width,Color c){var matrix=GUI.matrix;GUIUtility.RotateAroundPivot(Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg,a);Box(new Rect(a.x,a.y-width*.5f,Vector2.Distance(a,b),width),c);GUI.matrix=matrix;}
        static GUIStyle Style(float size,TextAnchor align=TextAnchor.MiddleCenter)=>new GUIStyle(GUI.skin.label){font=MokUIFont.Font,fontSize=Mathf.RoundToInt(size*Screen.height/1080f),alignment=align,wordWrap=true};
        static void Panel(Rect r){Box(r,Brass);Box(new Rect(r.x+2,r.y+2,r.width-4,r.height-4),Ink);Box(new Rect(r.x+6,r.y+6,r.width-12,2),new Color(1,1,1,.12f));}
        public static void DrawNoise(float noise,bool warning)
        {
            var old=GUI.color;float s=Screen.height/1080f,w=100*s,x=Screen.width*.365f-w*.5f,y=Screen.height*.31f,h=225*s;
            if(warning)x+=Mathf.Sin(Time.time*35)*2*s;
            Color level=warning||noise>=.6f?new Color(1,.3f,.17f):noise>=.35f?new Color(1,.7f,.25f):new Color(.38f,.85f,.77f);
            Panel(new Rect(x,y,w,h+100*s));GUI.color=Cream;GUI.Label(new Rect(x,y+9*s,w,29*s),"소음",Style(22));
            var bar=new Rect(x+34*s,y+47*s,32*s,h);
            for(int i=0;i<20;i++){float f=(i+1)/20f;Box(new Rect(bar.x,bar.y+bar.height*(1-f),bar.width,h/20-3*s),noise>=f?level:new Color(.17f,.23f,.25f));}
            Line(new Vector2(bar.x-5*s,bar.y+h*.4f),new Vector2(bar.x+bar.width+5*s,bar.y+h*.4f),2*s,Cream);
            // 작은 귀 윤곽과 위험 때의 소리 파동.
            Vector2 ear=new Vector2(x+17*s,bar.y+h*.4f);
            for(int i=0;i<11;i++){float a=(-120+i*24)*Mathf.Deg2Rad,b=(-120+(i+1)*24)*Mathf.Deg2Rad;Line(ear+new Vector2(Mathf.Cos(a)*8,Mathf.Sin(a)*12)*s,ear+new Vector2(Mathf.Cos(b)*8,Mathf.Sin(b)*12)*s,2*s,Cream);}
            Line(ear,ear+new Vector2(-3,7)*s,2*s,Cream);
            if(warning){float pulse=Mathf.PingPong(Time.time*2,1);Line(ear+new Vector2(-13-pulse*7,-8)*s,ear+new Vector2(-17-pulse*7,5)*s,2*s,level);}
            GUI.color=level;GUI.Label(new Rect(x,y+h+53*s,w,33*s),warning?"멈춰!":noise>=.35f?"조심":"조용함",Style(23));GUI.color=old;
        }
        public static void Instruction(string action,string detail,bool warning=false)
        {
            var old=GUI.color;float s=Screen.height/1080f,w=Mathf.Min(Screen.width*.72f,820*s),x=(Screen.width-w)*.5f,y=Screen.height*.82f;
            Panel(new Rect(x,y,w,125*s));Box(new Rect(x+2,y+2,5*s,121*s),warning?new Color(1,.35f,.18f):Brass);
            GUI.color=warning?new Color(1,.7f,.45f):Cream;GUI.Label(new Rect(x+18*s,y+12*s,w-36*s,45*s),action,Style(31));
            GUI.color=new Color(.78f,.84f,.84f);GUI.Label(new Rect(x+18*s,y+61*s,w-36*s,43*s),detail,Style(21));GUI.color=old;
        }
        public static void Dialogue(string line)
        {
            if(string.IsNullOrEmpty(line))return;
            var old=GUI.color;float s=Screen.height/1080f,w=Mathf.Min(Screen.width*.8f,1050*s),x=(Screen.width-w)*.5f,y=Screen.height*.82f;
            Panel(new Rect(x,y,w,125*s));int split=line.IndexOf(':');string speaker=split>0?line.Substring(0,split):"",text=split>0?line.Substring(split+1).Trim():line;
            GUI.color=speaker=="간수"?new Color(1,.55f,.4f):new Color(1,.77f,.4f);GUI.Label(new Rect(x+22*s,y+8*s,w-44*s,32*s),speaker,Style(22,TextAnchor.MiddleLeft));
            GUI.color=Cream;GUI.Label(new Rect(x+22*s,y+43*s,w-44*s,68*s),text,Style(28));GUI.color=old;
        }
        public static void Draw(float noise,PrisonPickpocket.Phase phase,int direction,bool warning,float progress)
        {
            DrawNoise(noise,warning);var old=GUI.color;float s=Screen.height/1080f;
            if(phase==PrisonPickpocket.Phase.Pull||phase==PrisonPickpocket.Phase.Snag){
                Vector2 center=new Vector2(Screen.width*.365f,Screen.height*.31f+365*s),dir=phase==PrisonPickpocket.Phase.Pull?Vector2.down:Vector2.right*direction,side=new Vector2(-dir.y,dir.x);float size=22*s;var tip=center+dir*size;
                Line(center-dir*size,tip,5*s,Cream);Line(tip,tip-dir*size*.65f+side*size*.6f,5*s,Cream);Line(tip,tip-dir*size*.65f-side*size*.6f,5*s,Cream);
            }
            GUI.color=old;
        }
    }
}