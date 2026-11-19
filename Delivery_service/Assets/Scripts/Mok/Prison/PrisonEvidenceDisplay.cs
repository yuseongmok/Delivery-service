using UnityEngine;
namespace DeliveryService.Mok
{
    // 증거의 내용은 기존 대사 에셋에 두고, 종이/휴대폰 표현만 분리한다.
    public static class PrisonEvidenceDisplay
    {
        static Texture2D paper,rounded;
        static void EnsureTextures()
        {
            if(!paper){paper=new Texture2D(128,128);var random=new System.Random(61);var pixels=new Color[128*128];for(int i=0;i<pixels.Length;i++){float v=.94f+(float)random.NextDouble()*.06f;pixels[i]=new Color(v,v*.97f,v*.89f);}paper.SetPixels(pixels);paper.Apply();}
            if(!rounded){rounded=new Texture2D(64,64);var pixels=new Color[4096];for(int y=0;y<64;y++)for(int x=0;x<64;x++){float dx=Mathf.Max(9-x,x-54),dy=Mathf.Max(9-y,y-54);bool inside=dx<=0||dy<=0||dx*dx+dy*dy<81;pixels[y*64+x]=new Color(1,1,1,inside?1:0);}rounded.SetPixels(pixels);rounded.Apply();}
        }
        static void Box(Rect r,Color color,Texture texture=null){GUI.color=color;GUI.DrawTexture(r,texture?texture:Texture2D.whiteTexture);}
        static void Text(Rect r,string text,int size,Color color,TextAnchor anchor=TextAnchor.UpperLeft){GUI.color=Color.white;var style=new GUIStyle(GUI.skin.label){font=MokUIFont.Font,fontSize=size,wordWrap=true,alignment=anchor};style.normal.textColor=color;GUI.Label(r,text,style);}
        public static void Draw(int index,string title,string body)
        {
            EnsureTextures();var old=GUI.color;var matrix=GUI.matrix;float scale=Mathf.Min(Screen.width/1920f,Screen.height/1080f);
            GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1920*scale)/2,0,0),Quaternion.identity,Vector3.one*scale);
            Box(new Rect(0,0,1920,1080),new Color(0,0,0,.28f));
            if(index<2)Paper(index,title,body);else Phone(body);
            GUI.color=old;GUI.matrix=matrix;
        }
        static void Paper(int index,string title,string body)
        {
            var ink=new Color(.16f,.19f,.22f);var sheet=new Rect(630,75,660,745);
            Box(new Rect(643,89,660,745),new Color(0,0,0,.35f));Box(sheet,Color.white,paper);
            Box(new Rect(655,75,4,745),new Color(.62f,.27f,.22f,.35f));
            for(int i=0;i<13;i++)Box(new Rect(680,300+i*31,560,1),new Color(.35f,.4f,.43f,.17f));
            Box(new Rect(900,64,110,26),new Color(.29f,.32f,.31f));Box(new Rect(911,69,88,11),new Color(.62f,.64f,.61f));
            Text(new Rect(685,120,540,35),index==0?"개인 정산 기록  /  외부 반출 금지":"교정시설 내부 문서  /  대외비",22,new Color(.42f,.36f,.28f));
            Text(new Rect(685,177,540,86),title,38,ink);
            Box(new Rect(685,273,540,3),ink);
            Text(new Rect(685,313,535,370),body,27,ink);
            Text(new Rect(685,712,280,35),"간수장실 보관용",21,new Color(.4f,.38f,.33f));
            var matrix=GUI.matrix;GUIUtility.RotateAroundPivot(-9,new Vector2(1130,732));
            Box(new Rect(1035,702,190,60),new Color(.58f,.15f,.12f));Box(new Rect(1038,705,184,54),new Color(.93f,.89f,.78f));
            Text(new Rect(1038,705,184,54),"염윤태  결재",27,new Color(.58f,.15f,.12f),TextAnchor.MiddleCenter);GUI.matrix=matrix;
            Text(new Rect(680,777,550,24),"—  1  —",18,new Color(.4f,.38f,.33f),TextAnchor.MiddleCenter);
        }
        static void Phone(string body)
        {
            Box(new Rect(731,71,470,762),new Color(0,0,0,.4f),rounded);Box(new Rect(720,60,470,762),new Color(.045f,.055f,.065f),rounded);
            Box(new Rect(736,79,438,720),new Color(.91f,.93f,.92f),rounded);
            Box(new Rect(896,87,112,12),new Color(.15f,.17f,.18f),rounded);
            Text(new Rect(763,112,180,30),"메시지",20,new Color(.27f,.32f,.36f));Text(new Rect(1030,112,115,30),"●  ▰",20,new Color(.27f,.32f,.36f),TextAnchor.UpperRight);
            Text(new Rect(769,173,370,55),"‹   박진웅",34,new Color(.12f,.18f,.22f));
            Box(new Rect(736,243,438,2),new Color(.72f,.77f,.78f));Text(new Rect(762,271,385,30),"오늘  ·  수신 메시지",21,new Color(.43f,.48f,.49f),TextAnchor.MiddleCenter);
            body=body.Replace("발신: 박진웅\n","");
            Box(new Rect(760,330,376,265),new Color(.76f,.82f,.83f),rounded);Text(new Rect(783,352,330,230),body,27,new Color(.10f,.16f,.19f));
            Text(new Rect(769,612,360,35),"읽음",19,new Color(.43f,.48f,.49f));
            Box(new Rect(759,710,392,52),new Color(.83f,.87f,.87f),rounded);Text(new Rect(780,720,350,30),"메시지 입력",21,new Color(.51f,.56f,.56f));
            Box(new Rect(900,783,110,5),new Color(.23f,.26f,.27f),rounded);
        }
    }
}
