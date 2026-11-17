using UnityEngine;
using UnityEngine.InputSystem;

namespace DeliveryService.Mok
{
    // Presentation consumes the quest's coordinates; it never changes the drag tolerances.
    public static class PrisonCuttingUI
    {
        static Texture2D facet;
        static GUIStyle label;
        static Vector2 impactStart, impactEnd;
        static float impactTime = -100;
        public static void Success(Vector2 start, Vector2 end)
        {
            impactStart = start; impactEnd = end; impactTime = Time.time;
        }
        // Brief blade glint and pale food flecks. No blood or full-screen flash.
        public static void DrawImpact()
        {
            float age = Time.time - impactTime;
            if (age < 0 || age > .34f || Time.timeScale <= 0) return;
            Init();
            Color previous = GUI.color;
            float fade = 1 - age / .34f;
            Vector2 direction = (impactEnd - impactStart).normalized;
            Vector2 normal = new Vector2(-direction.y, direction.x);
            Stroke(impactStart, impactEnd, 2 + 5 * fade, new Color(1, .94f, .74f, fade));
            for (int i = 0; i < 10; i++)
            {
                float side = i % 2 == 0 ? 1 : -1;
                Vector2 p = Vector2.Lerp(impactStart, impactEnd, (i + .5f) / 10);
                p += normal * side * age * (55 + i * 9) + Vector2.up * age * age * 160;
                Disc(p, 2 + fade * 3, 2 + fade, new Color(1, .85f, .64f, fade));
            }
            GUI.color = previous;
        }
        static readonly Color Cream = new Color32(246, 230, 197, 255);
        static readonly Color Ink = new Color32(30, 42, 44, 255);
        static readonly Color Meat = new Color32(225, 185, 157, 255);
        public static void Draw(Vector2 start, Vector2 end, float progress, bool tracing, int count, int target,
            string judgement, Color judgementColor, bool showJudgement)
        {
            Init(); Color previous = GUI.color;
            // Keep the original scene chicken and cutting board visible behind the guides.
            // The visible cut grows to the exact fraction already accepted by the quest.
            if (progress > 0)
            {
                Vector2 tip = Vector2.Lerp(start, end, progress);
                Stroke(start, tip, 5, new Color32(144, 103, 82, 255));
                Stroke(start + Vector2.up * 3, tip + Vector2.up * 3, 2, Cream);
            }
            for (int i = 0; i <= 24; i++)
            {
                float f = i / 24f;
                if (f > progress) Disc(Vector2.Lerp(start, end, f), 4, 4, Cream);
            }
            Marker(start, "시작", new Color32(247, 177, 73, 255));
            Marker(end, "끝", new Color32(115, 208, 213, 255));

            Fill(new Rect(460, 725, 1000, 199), Ink);
            Text(new Rect(480, 736, 960, 40), "닭 손질  /  01", 29, Cream);
            Text(new Rect(480, 783, 960, 38), tracing ? "칼을 떼지 말고 점선을 따라 끝까지 그어주세요" : "시작점을 좌클릭한 채 점선을 따라 그어주세요", 23, Cream);
            for (int i = 0; i < target; i++)
            {
                float x = 960 + (i - (target - 1) * .5f) * 59;
                Disc(new Vector2(x, 850), 19, 13, i < count ? Meat : new Color32(76, 94, 92, 255));
            }
            Text(new Rect(710, 880, 500, 32), count + " / " + target + " 회 손질 완료", 23, Cream);
            Text(new Rect(660, 964, 600, 32), "F : 조리대에서 나오기", 21, Cream);
            if (showJudgement) Text(new Rect(560, 672, 800, 48), judgement, 34, judgementColor);

            if (Mouse.current != null)
            {
                Vector2 m = Mouse.current.position.ReadValue();
                Vector2 tip = new Vector2(m.x / Screen.width * 1920, (Screen.height - m.y) / Screen.height * 1080);
                // Knife point, not handle, is anchored to the actual mouse position.
                                Vector2 cutDirection = (end - start).normalized;
                Vector2 d = new Vector2(cutDirection.y, -cutDirection.x);
                if (d.y > 0) d = -d;
                if (d.sqrMagnitude < .5f) d = new Vector2(.65f, -.76f);
                Vector2 side = new Vector2(-d.y, d.x);
                for (int i = 0; i < 22; i++)
                {
                    float f = i / 21f;
                    Vector2 p = tip + d * (f * 112) + side * (f * 10);
                    Disc(p + new Vector2(4, tracing ? 3 : 8), 2 + f * 17, 3 + f * 11, new Color32(88, 72, 54, 180));
                    Disc(p, 2 + f * 17, 3 + f * 11, new Color32(185, 204, 200, 255));
                }
                Stroke(tip, tip + d * 108, 2, new Color32(237, 242, 222, 255));
                Stroke(tip + d * 115, tip + d * 183, 11, new Color32(54, 65, 65, 255));
                for (int i = 0; i < 2; i++) Disc(tip + d * (135 + i * 27), 3, 3, Cream);
            }
            GUI.color = previous;
        }
        static void Marker(Vector2 p, string text, Color color)
        {
            Disc(p, 31, 24, Ink); Disc(p, 27, 20, color);
            Text(new Rect(p.x - 35, p.y - 17, 70, 34), text, 20, Ink);
        }
        static void Stroke(Vector2 a,Vector2 b,float r,Color c)
        {
            int n=Mathf.CeilToInt(Vector2.Distance(a,b)/5);
            for(int i=0;i<=n;i++)Disc(Vector2.Lerp(a,b,i/(float)Mathf.Max(1,n)),r,r,c);
        }
        static void Disc(Vector2 p,float rx,float ry,Color c){GUI.color=c;GUI.DrawTexture(new Rect(p.x-rx,p.y-ry,rx*2,ry*2),facet);}
        static void Fill(Rect r,Color c){GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);}
        static void Text(Rect r,string s,int size,Color c){GUI.color=Color.white;label.fontSize=size;label.normal.textColor=c;GUI.Label(r,s,label);}
        static void Init()
        {
            label ??= new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter};label.font=MokUIFont.Font;
            if(facet!=null)return;
            facet=new Texture2D(64,64,TextureFormat.RGBA32,false){hideFlags=HideFlags.HideAndDontSave,filterMode=FilterMode.Bilinear};
            var pixels=new Color32[4096];
            for(int y=0;y<64;y++)for(int x=0;x<64;x++){
                float dx=x-31.5f,dy=y-31.5f;
                float a=Mathf.Repeat(Mathf.Atan2(dy,dx)+Mathf.PI/8,Mathf.PI/4)-Mathf.PI/8;
                float edge=30*Mathf.Cos(Mathf.PI/8)/Mathf.Cos(a);
                pixels[y*64+x]=new Color32(255,255,255,(byte)(255*Mathf.Clamp01(edge-Mathf.Sqrt(dx*dx+dy*dy)+.5f)));
            }
            facet.SetPixels32(pixels);facet.Apply(false,true);
        }
    }
}


