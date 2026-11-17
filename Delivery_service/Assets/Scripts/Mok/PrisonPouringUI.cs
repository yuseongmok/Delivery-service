using UnityEngine;

namespace DeliveryService.Mok
{
    // Read-only presentation. Coordinates match the quest's 560 + normalizedX * 800 mapping.
    public static class PrisonPouringUI
    {
        static GUIStyle label;
        static Texture2D bowl, mouth, bag, jug, bottle;
        static readonly Color Cream = new Color32(245, 230, 196, 255);
        static readonly Color Ink = new Color32(25, 36, 39, 255);
        static readonly Color Gold = new Color32(244, 182, 75, 255);

        public static void Draw(int step, float bowlPosition, float spoutPosition, float progress,
            bool pouring, bool bonus, string judgement, Color judgementColor, bool showJudgement)
        {
            Color previous = GUI.color;
            EnsureShapes();
            label ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
            label.font = MokUIFont.Font;
            string ingredient = step == 1 ? "밀가루" : step == 2 ? "물" : "기름";
            Color material = step == 1 ? Cream : step == 2 ? new Color32(103, 194, 217, 255) : Gold;
            float bx = 560 + bowlPosition * 800;
            float sx = 560 + spoutPosition * 800;
            float difference = Mathf.Abs(spoutPosition - bowlPosition);
            bool caught = difference <= .14f;
            bool boosted = bonus && difference <= .055f;

            // Worktop edge and a small recipe label, instead of a full-screen instruction panel.
            Fill(new Rect(390, 913, 1140, 58), new Color32(60, 79, 77, 255));
            Fill(new Rect(390, 913, 1140, 6), new Color32(182, 193, 179, 255));
            Fill(new Rect(410, 931, 1100, 3), new Color32(40, 57, 57, 255));
            Fill(new Rect(410, 603, 300, 69), Cream);
            Text(new Rect(425, 609, 270, 30), ingredient + " 붓기", 27, Ink);
            Text(new Rect(425, 642, 270, 22), (step + 1) + " / 8   재료 계량", 18, Ink);

            // Bowl opening is exactly 224 px wide: the unchanged +/- 0.14 catch window.
            Shape(bowl, new Rect(bx - 119, 823, 238, 92), new Color32(50, 68, 71, 255));
            Shape(bowl, new Rect(bx - 112, 818, 224, 90), new Color32(149, 169, 166, 255));
            Shape(bowl, new Rect(bx - 98, 830, 196, 66), new Color32(112, 140, 139, 255));
            Shape(mouth, new Rect(bx - 112, 800, 224, 44), new Color32(220, 227, 207, 255));
            Shape(mouth, new Rect(bx - 107, 804, 214, 34), Ink);
            Shape(mouth, new Rect(bx - 103, 810, 206, 23), Color.Lerp(Ink, material, .25f + Mathf.Clamp01(progress) * .75f));
            if (bonus)
            {
                // 88 px matches the unchanged +/- 0.055 bonus window.
                Fill(new Rect(bx - 44, 808, 88, 4), Gold);
                Fill(new Rect(bx - 44, 808, 3, 22), Gold);
                Fill(new Rect(bx + 41, 808, 3, 22), Gold);
                Text(new Rect(bx - 90, 850, 180, 25), "황금 구간", 19, Cream);
            }

            // The outlet is always at sx, including when the container changes silhouette.
            Texture2D vessel = step == 1 ? bag : step == 2 ? jug : bottle;
            Color vesselColor = step == 1 ? new Color32(205, 179, 126, 255) :
                step == 2 ? new Color32(122, 177, 186, 255) : new Color32(117, 139, 77, 255);
            Shape(vessel, new Rect(sx - 55, 680, 110, 100), new Color32(29, 41, 42, 255));
            Shape(vessel, new Rect(sx - 52, 676, 104, 100), vesselColor);
            Fill(new Rect(sx - 34, 698, 68, 35), Cream);
            Text(new Rect(sx - 34, 701, 68, 28), ingredient, 20, Ink);
            Fill(new Rect(sx - 9, 768, 18, 9), material);

            if (pouring)
            {
                float bottom = caught ? 821 : 914;
                for (float y = 780; y < bottom; y += step == 1 ? 10 : 6)
                {
                    float motion = Mathf.Repeat(Time.time * 72, 10);
                    float spread = step == 1 ? Mathf.Sin(y * .6f) * 5 : 0;
                    Fill(new Rect(sx - 4 + spread, Mathf.Min(y + motion, bottom), 8, step == 1 ? 5 : 8), material);
                }
                // Small surface splash, driven only by presentation time.
                for (int i = 0; i < 4; i++)
                {
                    float phase = Mathf.Repeat(Time.time * 2 + i * .25f, 1);
                    Fill(new Rect(sx + (i % 2 == 0 ? -1 : 1) * phase * 25,
                        bottom - Mathf.Sin(phase * Mathf.PI) * 15, 5, 5), material);
                }
            }

            string status = !pouring ? "좌우로 조준하고 좌클릭을 누르고 있으세요" :
                !caught ? "밖으로 흐르고 있어요! 양푼 위로 옮기세요" :
                boosted ? "황금 구간! 빠르게 채우는 중" : "좋아요! 양푼을 따라 부으세요";
            Text(new Rect(500, 556, 920, 36), status, 26,
                pouring && !caught ? new Color32(255, 143, 109, 255) : Cream);
            Text(new Rect(bx - 100, 879, 200, 30), Mathf.RoundToInt(Mathf.Clamp01(progress) * 100) + "%", 26, Cream);
            // Etched volume scale: progress stays readable even when the bowl is moving.
            Fill(new Rect(650, 946, 620, 8), Ink);
            Fill(new Rect(650, 946, 620 * Mathf.Clamp01(progress), 8), material);
            for (int i = 0; i <= 10; i++) Fill(new Rect(650 + i * 62, 943, 2, 14), Cream);
            Text(new Rect(580, 982, 760, 34), "마우스 좌우 : 용기 이동     좌클릭 유지 : 붓기     F : 나오기", 22, Cream);
            if (showJudgement) Text(new Rect(570, 488, 780, 52), judgement, 36, judgementColor);
            GUI.color = previous;
        }

        static void EnsureShapes()
        {
            if (bowl != null) return;
            bowl = Polygon(new[] { new Vector2(0,0), new Vector2(1,0), new Vector2(.91f,.58f), new Vector2(.72f,1), new Vector2(.28f,1), new Vector2(.09f,.58f) });
            mouth = Polygon(new[] { new Vector2(.12f,0), new Vector2(.88f,0), new Vector2(1,.5f), new Vector2(.88f,1), new Vector2(.12f,1), new Vector2(0,.5f) });
            bag = Polygon(new[] { new Vector2(.08f,0), new Vector2(.9f,.06f), new Vector2(1,.65f), new Vector2(.6f,1), new Vector2(.4f,1), new Vector2(0,.5f) });
            jug = Polygon(new[] { new Vector2(.16f,0), new Vector2(.9f,0), new Vector2(1,.64f), new Vector2(.59f,1), new Vector2(.41f,1), new Vector2(0,.66f), new Vector2(0,.2f) });
            bottle = Polygon(new[] { new Vector2(.15f,0), new Vector2(.85f,0), new Vector2(.92f,.6f), new Vector2(.6f,.78f), new Vector2(.6f,1), new Vector2(.4f,1), new Vector2(.4f,.78f), new Vector2(.08f,.6f) });
        }

        static Texture2D Polygon(Vector2[] points)
        {
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            { hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Bilinear };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2((x + .5f) / size, 1 - (y + .5f) / size);
                    bool inside = false;
                    for (int i = 0, j = points.Length - 1; i < points.Length; j = i++)
                    {
                        Vector2 a = points[i], b = points[j];
                        if ((a.y > p.y) != (b.y > p.y) && p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x) inside = !inside;
                    }
                    pixels[y * size + x] = new Color32(255, 255, 255, inside ? (byte)255 : (byte)0);
                }
            texture.SetPixels32(pixels); texture.Apply(false, true);
            return texture;
        }

        static void Shape(Texture2D texture, Rect rect, Color color) { GUI.color = color; GUI.DrawTexture(rect, texture); }
        static void Fill(Rect rect, Color color) => Shape(Texture2D.whiteTexture, rect, color);
        static void Text(Rect rect, string text, int size, Color color)
        {
            GUI.color = Color.white; label.fontSize = size; label.normal.textColor = color;
            GUI.Label(rect, text, label);
        }
    }
}
