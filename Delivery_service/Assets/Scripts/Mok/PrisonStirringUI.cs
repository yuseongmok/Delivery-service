using UnityEngine;

namespace DeliveryService.Mok
{
    // Read-only view of the quest's actual mouse angle, time and progress.
    public static class PrisonStirringUI
    {
        static Texture2D facet;
        static GUIStyle label;
        static readonly Color Cream = new Color32(245, 230, 196, 255);
        static readonly Color Ink = new Color32(27, 41, 43, 255);

        public static void Draw(float angle, float progress, float remaining, string feedback,
            string judgement, Color judgementColor, bool showJudgement)
        {
            Color before = GUI.color;
            EnsureTexture();
            label ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, wordWrap = true };
            label.font = MokUIFont.Font;
            // Centre matches the screen-centre pivot used by the existing input calculation.
            Vector2 centre = new Vector2(960, 540);
            Disc(centre + new Vector2(9, 17), 266, 226, new Color32(22, 34, 37, 230));
            Disc(centre, 266, 226, new Color32(124, 151, 151, 255));
            Disc(centre + new Vector2(0, -5), 253, 213, new Color32(209, 221, 202, 255));
            Disc(centre, 237, 197, new Color32(56, 81, 82, 255));
            Disc(centre + new Vector2(0, 6), 224, 183, new Color32(174, 139, 84, 255));
            Disc(centre + new Vector2(-5, 0), 215, 175, new Color32(225, 197, 136, 255));

            // Dough folds turn with the spoon, rather than an unrelated looping animation.
            float radians = -angle * Mathf.Deg2Rad;
            for (int i = 0; i < 24; i++)
            {
                float a = radians + i * .24f;
                float radius = 45 + i * 4;
                Vector2 p = centre + new Vector2(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius * .76f);
                Disc(p, 20, 8, i % 2 == 0 ? new Color32(239, 214, 161, 255) : new Color32(202, 169, 109, 255));
            }

            // Spoon head stays in the dough; its wooden handle extends beyond the rim.
            Vector2 direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
            Vector2 head = centre + new Vector2(direction.x * 115, direction.y * 90);
            Vector2 end = head + direction * 180;
            Stroke(head + new Vector2(5, 7), end + new Vector2(5, 7), 13, new Color32(121, 94, 56, 255));
            Stroke(head, end, 11, new Color32(161, 105, 56, 255));
            Stroke(head - direction * 23, head + direction * 25, 24, new Color32(175, 119, 63, 255));
            Stroke(head - direction * 17, head + direction * 19, 17, new Color32(238, 208, 146, 255));
            Stroke(head + direction * 43 + new Vector2(-3, -3), end + new Vector2(-3, -3), 3, new Color32(205, 153, 88, 255));

            // Progress follows the outside of the bowl; it never influences scoring.
            for (int i = 0; i < 40; i++)
            {
                float a = (-90 + i * 9) * Mathf.Deg2Rad;
                Vector2 p = centre + new Vector2(Mathf.Cos(a) * 287, Mathf.Sin(a) * 247);
                Disc(p, 4, 4, i < Mathf.FloorToInt(Mathf.Clamp01(progress) * 40) ? new Color32(248, 172, 78, 255) : new Color32(77, 100, 101, 255));
            }
            Fill(new Rect(410, 355, 250, 95), Cream);
            Text(new Rect(425, 366, 220, 36), "반죽 섞기", 29, Ink);
            Text(new Rect(425, 407, 220, 27), "04 / 08  조리 중", 19, Ink);
            Fill(new Rect(1270, 375, 240, 144), Ink);
            Text(new Rect(1280, 384, 220, 29), "반죽 제한시간", 20, Cream);
            Text(new Rect(1280, 420, 220, 52), Mathf.CeilToInt(remaining) + "초", 40,
                remaining <= 8 ? new Color32(255, 145, 107, 255) : Cream);
            Text(new Rect(1280, 477, 220, 28), "완성도 " + Mathf.RoundToInt(progress * 100) + "%", 22, Cream);
            Fill(new Rect(460, 835, 1000, 125), new Color32(24, 38, 41, 245));
            Text(new Rect(480, 845, 960, 51), feedback, 24, Cream);
            Text(new Rect(480, 910, 960, 31), "양푼 중심을 따라 마우스를 돌리세요     /     F : 조리대에서 나오기", 21, Cream);
            if (showJudgement) Text(new Rect(560, 232, 800, 58), judgement, 35, judgementColor);
            GUI.color = before;
        }

        static void Stroke(Vector2 from, Vector2 to, float radius, Color color)
        {
            int count = Mathf.CeilToInt(Vector2.Distance(from, to) / 5);
            for (int i = 0; i <= count; i++) Disc(Vector2.Lerp(from, to, i / (float)Mathf.Max(1, count)), radius, radius, color);
        }
        static void Disc(Vector2 centre, float rx, float ry, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(new Rect(centre.x - rx, centre.y - ry, rx * 2, ry * 2), facet);
        }
        static void Fill(Rect rect, Color color) { GUI.color = color; GUI.DrawTexture(rect, Texture2D.whiteTexture); }
        static void Text(Rect rect, string text, int size, Color color)
        {
            GUI.color = Color.white; label.fontSize = size; label.normal.textColor = color;
            GUI.Label(rect, text, label);
        }
        static void EnsureTexture()
        {
            if (facet != null) return;
            facet = new Texture2D(128, 128, TextureFormat.RGBA32, false)
            { hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Bilinear };
            var pixels = new Color32[128 * 128];
            for (int y = 0; y < 128; y++)
                for (int x = 0; x < 128; x++)
                {
                    float dx = x - 63.5f, dy = y - 63.5f;
                    float a = Mathf.Repeat(Mathf.Atan2(dy, dx) + Mathf.PI / 12, Mathf.PI / 6) - Mathf.PI / 12;
                    float edge = 61 * Mathf.Cos(Mathf.PI / 12) / Mathf.Cos(a);
                    pixels[y * 128 + x] = new Color32(255, 255, 255, (byte)(255 * Mathf.Clamp01(edge - Mathf.Sqrt(dx * dx + dy * dy) + .5f)));
                }
            facet.SetPixels32(pixels); facet.Apply(false, true);
        }
    }
}
