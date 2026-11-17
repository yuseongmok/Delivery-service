using UnityEngine;
using UnityEngine.InputSystem;

namespace DeliveryService.Mok
{
    // The existing 14 x 6 cells remain the source of truth. Every cell centre sits
    // on visible chicken, so no invisible tray area needs painting to finish.
    public static class PrisonSauceUI
    {
        static Texture2D chicken;
        static GUIStyle label;
        static readonly Color Cream = new Color32(246, 230, 190, 255);
        static readonly Color Ink = new Color32(28, 40, 43, 255);
        public static void Draw(Rect canvas, bool[,] painted, int count, int total,
            string judgement, Color judgementColor, bool showJudgement)
        {
            Init(); Color before = GUI.color;
            int columns = painted.GetLength(0), rows = painted.GetLength(1);
            float cw = canvas.width / columns, ch = canvas.height / rows;
            Fill(new Rect(canvas.x - 24, canvas.y - 18, canvas.width + 48, canvas.height + 47), Ink);
            Fill(new Rect(canvas.x - 18, canvas.y - 22, canvas.width + 36, canvas.height + 36), new Color32(153, 171, 164, 255));
            Fill(new Rect(canvas.x - 7, canvas.y - 10, canvas.width + 14, canvas.height + 12), new Color32(65, 82, 79, 255));
            // Each faceted piece covers four original cells. Clip sauce to the same silhouette.
            for (int x = 0; x < columns; x += 2)
                for (int y = 0; y < rows; y += 2)
                {
                    Rect piece = new Rect(canvas.x + x * cw + 5, canvas.y + y * ch + 5, cw * 2 - 10, ch * 2 - 10);
                    Shape(new Rect(piece.x + 3, piece.y + 6, piece.width, piece.height), new Color32(37, 44, 36, 255));
                    Shape(piece, new Color32(240, 175, 71, 255));
                    for (int dx = 0; dx < 2 && x + dx < columns; dx++)
                        for (int dy = 0; dy < 2 && y + dy < rows; dy++)
                        {
                            int gx = x + dx, gy = y + dy;
                            Rect cell = new Rect(canvas.x + gx * cw, canvas.y + gy * ch, cw, ch);
                            if (painted[gx, gy])
                            {
                                GUI.BeginGroup(cell);
                                Shape(new Rect(piece.x - cell.x, piece.y - cell.y, piece.width, piece.height), new Color32(221, 73, 35, 255));
                                GUI.EndGroup();
                                // Glaze highlight and sesame sit inside the original cell, on food only.
                                Fill(new Rect(cell.center.x - 12, cell.center.y - 14, 20, 3), new Color32(251, 139, 76, 255));
                                if ((gx + gy) % 3 == 0) Fill(new Rect(cell.center.x + 7, cell.center.y + 5, 4, 8), Cream);
                            }
                            else
                            {
                                Fill(new Rect(cell.center.x - 8, cell.center.y - 9, 12, 5), new Color32(251, 205, 119, 255));
                            }
                        }
                }

            Fill(new Rect(475, 718, 970, 163), Ink);
            Text(new Rect(495, 730, 930, 40), "양념 바르기  /  08", 29, Cream);
            Text(new Rect(495, 775, 930, 35), "좌클릭을 누른 채 붓을 움직여 노란 부분까지 골고루 발라주세요", 23, Cream);
            Fill(new Rect(590, 834, 740, 8), new Color32(71, 87, 82, 255));
            Fill(new Rect(590, 834, 740 * count / Mathf.Max(1f, total), 8), new Color32(231, 101, 50, 255));
            Text(new Rect(790, 848, 340, 29), "양념 완성도 " + Mathf.RoundToInt(count * 100f / Mathf.Max(1, total)) + "%", 25, Cream);
            Text(new Rect(660, 964, 600, 32), "F : 조리대에서 나오기", 21, Cream);
            if (showJudgement) Text(new Rect(540, 662, 840, 50), judgement, 34, judgementColor);

            if (Mouse.current != null)
            {
                Vector2 mouse = Mouse.current.position.ReadValue();
                Vector2 tip = new Vector2(mouse.x / Screen.width * 1920, (Screen.height - mouse.y) / Screen.height * 1080);
                bool down = Mouse.current.leftButton.isPressed;
                Vector2 direction = new Vector2(.65f, -.76f);
                // Bristles are centred at the real cursor, not the end of the handle.
                Stroke(tip + direction * 40, tip + direction * 134, 10, new Color32(153, 100, 52, 255));
                Stroke(tip + direction * 51 + Vector2.left * 3, tip + direction * 127 + Vector2.left * 3, 3, new Color32(210, 159, 92, 255));
                Stroke(tip + direction * 24, tip + direction * 48, 17, new Color32(182, 201, 192, 255));
                Vector2 across = new Vector2(-direction.y, direction.x);
                for (int i = -3; i <= 3; i++)
                    Stroke(tip + across * i * 5 - direction * (down ? 12 : 4), tip + direction * 25 + across * i * 5, 3,
                        i % 2 == 0 ? new Color32(226, 87, 42, 255) : new Color32(166, 54, 29, 255));
            }
            GUI.color = before;
        }
        static void Stroke(Vector2 a, Vector2 b, float r, Color c)
        {
            int steps = Mathf.CeilToInt(Vector2.Distance(a, b) / 4);
            for (int i = 0; i <= steps; i++) { Vector2 p=Vector2.Lerp(a,b,i/(float)Mathf.Max(1,steps));Shape(new Rect(p.x-r,p.y-r,r*2,r*2),c); }
        }
        static void Shape(Rect r, Color c) { GUI.color=c;GUI.DrawTexture(r,chicken); }
        static void Fill(Rect r, Color c) { GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture); }
        static void Text(Rect r,string s,int size,Color c) { GUI.color=Color.white;label.fontSize=size;label.normal.textColor=c;GUI.Label(r,s,label); }
        static void Init()
        {
            label ??= new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter};label.font=MokUIFont.Font;
            if(chicken!=null)return;
            chicken=new Texture2D(128,128,TextureFormat.RGBA32,false){hideFlags=HideFlags.HideAndDontSave,filterMode=FilterMode.Bilinear};
            var pixels=new Color32[128*128];
            for(int y=0;y<128;y++)for(int x=0;x<128;x++)
            {
                float dx=Mathf.Abs((x-63.5f)/63.5f),dy=Mathf.Abs((y-63.5f)/63.5f);
                                float angle=Mathf.Atan2(y-63.5f,x-63.5f);
                float radius=Mathf.Sqrt(dx*dx+dy*dy);
                float edge=.93f+.045f*Mathf.Sin(angle*5)+.025f*Mathf.Cos(angle*3);
                bool inside=radius<edge;
                byte shade=(byte)(radius>edge-.12f?185:((x/9+y/11)%7==0?225:255));
                pixels[y*128+x]=new Color32(shade,shade,shade,inside?(byte)255:(byte)0);
            }
            chicken.SetPixels32(pixels);chicken.Apply(false,true);
        }
    }
}

