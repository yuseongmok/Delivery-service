using UnityEngine;

namespace DeliveryService.Mok
{
    // Rendering only: the quest still owns the needle, success zone and all input.
    public static class PrisonFryingUI
    {
        static Texture2D facet;
        static GUIStyle label;
        static float flipAt = -10;
        static int flippedCount;
        static readonly Color Cream = new Color32(245, 230, 196, 255);
        static readonly Color Ink = new Color32(28, 40, 43, 255);
        public static void Success(int count) { flipAt = Time.time; flippedCount = count; }

        public static void Draw(float cursor, float zone, float halfWidth, int count,
            string judgement, Color judgementColor, bool showJudgement)
        {
            Init(); Color old = GUI.color;
            Fill(490, 333, 940, 475, new Color32(51, 68, 70, 255));
            Fill(490, 333, 940, 14, new Color32(199, 211, 191, 255));
            Fill(510, 353, 900, 375, new Color32(145, 165, 160, 255));
            Fill(535, 378, 850, 325, Ink);
            Fill(552, 395, 816, 288, new Color32(113, 84, 39, 255));
            for (int i = 0; i < 18; i++) Fill(565 + i * 46, 401, 2, 273, new Color32(155, 131, 77, 255));
            for (int i = 0; i < 7; i++) Fill(560, 410 + i * 42, 801, 2, new Color32(155, 131, 77, 255));
            // Basket handle and enamel front label.
            Fill(880, 694, 16, 76, new Color32(176, 188, 179, 255));
            Fill(1024, 694, 16, 76, new Color32(176, 188, 179, 255));
            Fill(871, 752, 178, 26, Ink);
            Text(new Rect(510, 746, 320, 40), "치킨 뒤집기  /  07", 28, Cream);
            Text(new Rect(1100, 746, 280, 40), count + " / 5 회 완료", 26, Cream);
            float t = Mathf.Clamp01((Time.time - flipAt) / .65f);
            bool flipping = t < 1 && flippedCount == count && count > 0;
            for (int i = 0; i < 5; i++)
            {
                Vector2 p = new Vector2(650 + i * 153, 531 + (i % 2) * 35);
                bool active = flipping && i == (count - 1) % 5;
                Piece(p, active ? t : 0, active, i < count);
            }
            for (int i = 0; i < 22; i++)
            {
                float phase = Mathf.Repeat(Time.time * .9f + i * .137f, 1);
                Disc(new Vector2(575 + (i * 113) % 760, 640 - phase * 211), 3 + phase * 3, 3,
                    new Color32(232, 190, 94, (byte)(150 * (1 - phase))));
            }
            if (flipping)
            {
                Vector2 target = new Vector2(650 + ((count - 1) % 5) * 153, 531 + ((count - 1) % 2) * 35 - Mathf.Sin(t * Mathf.PI) * 74);
                Tongs(target, t);
            }
            else Tongs(new Vector2(1305, 448), 0);

            // Exact same 800px track and normalized bounds as the original UI.
            Fill(530, 829, 860, 103, Ink);
            Text(new Rect(560, 794, 800, 32), "SPACE  :  바늘이 초록 구간에 오면 뒤집기", 24, Cream);
            Fill(560, 855, 800, 44, new Color32(69, 85, 83, 255));
            Fill(560 + 800 * (zone - halfWidth), 855, 1600 * halfWidth, 44, new Color32(105, 204, 145, 255));
            Fill(560 + cursor * 800 - 6, 847, 12, 60, Cream);
            for (int i = 0; i <= 20; i++) Fill(560 + i * 40, 905, 2, 8, new Color32(145, 161, 153, 255));
            Text(new Rect(560, 962, 800, 34), "5회 성공하면 완성     /     F : 조리대에서 나오기", 21, Cream);
            if (showJudgement) Text(new Rect(560, 257, 800, 55), judgement, 35, judgementColor);
            GUI.color = old;
        }

        // Fifth success immediately advances the quest. Show its last flip in a small inset
        // instead of delaying the next step or covering the sauce interaction area.
        public static void DrawFinalFlip()
        {
            float t = (Time.time - flipAt) / .65f;
            if (flippedCount != 5 || t < 0 || t >= 1) return;
            Init(); Color old = GUI.color;
            Fill(35, 710, 300, 240, Ink);
            Vector2 p = new Vector2(175, 820);
            Piece(p, t, true, true);
            Tongs(p + new Vector2(0, -Mathf.Sin(t * Mathf.PI) * 74), t);
            Text(new Rect(35, 910, 300, 30), "튀김 완성!", 23, Cream);
            GUI.color = old;
        }
        static void Piece(Vector2 p, float t, bool flip, bool done)
        {
            float lift = flip ? Mathf.Sin(t * Mathf.PI) * 74 : 0;
            float width = flip ? Mathf.Max(.16f, Mathf.Abs(Mathf.Cos(t * Mathf.PI))) : 1;
            Disc(p + new Vector2(4, 13), 58, 32, new Color32(64, 54, 31, 255));
            Vector2 body = p + new Vector2(0, -lift);
            Disc(body, 54 * width, 43, new Color32(158, 87, 34, 255));
            Disc(body + new Vector2(-7 * width, -5), 45 * width, 34,
                done && (!flip || t > .5f) ? new Color32(220, 140, 52, 255) : new Color32(238, 181, 81, 255));
            for (int j = 0; j < 6; j++) Disc(body + new Vector2(((j * 17) % 60 - 30) * width, (j * 13) % 38 - 19), 6 * width, 5, new Color32(249, 203, 116, 255));
        }
        static void Tongs(Vector2 tip, float t)
        {
            Vector2 grip = tip + new Vector2(92, -100);
            float gap = 9 + Mathf.Abs(Mathf.Cos(t * Mathf.PI)) * 10;
            Stroke(grip, tip + new Vector2(-gap, 4), 5, new Color32(207, 219, 209, 255));
            Stroke(grip + new Vector2(10, 5), tip + new Vector2(gap, 8), 5, new Color32(138, 164, 161, 255));
            Stroke(grip, grip + new Vector2(25, -30), 9, Ink);
        }
        static void Stroke(Vector2 a, Vector2 b, float width, Color c)
        {
            int n = Mathf.CeilToInt(Vector2.Distance(a, b) / 6);
            for (int i = 0; i <= n; i++) Disc(Vector2.Lerp(a, b, i / (float)Mathf.Max(1,n)), width, width, c);
        }
        static void Disc(Vector2 p, float x, float y, Color c) { GUI.color=c; GUI.DrawTexture(new Rect(p.x-x,p.y-y,x*2,y*2),facet); }
        static void Fill(float x,float y,float w,float h,Color c) { GUI.color=c;GUI.DrawTexture(new Rect(x,y,w,h),Texture2D.whiteTexture); }
        static void Text(Rect r,string s,int size,Color c) { GUI.color=Color.white;label.fontSize=size;label.normal.textColor=c;GUI.Label(r,s,label); }
        static void Init()
        {
            label ??= new GUIStyle(GUI.skin.label){ alignment=TextAnchor.MiddleCenter };
            label.font=MokUIFont.Font;
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
