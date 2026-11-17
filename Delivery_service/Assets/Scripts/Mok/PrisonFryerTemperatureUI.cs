using UnityEngine;

namespace DeliveryService.Mok
{
    // Presentation only. The cooking quest owns every input, number and judgement.
    public static class PrisonFryerTemperatureUI
    {
        static Texture2D knob;
        static GUIStyle label;
        static readonly Color Ink = new Color32(22, 31, 33, 255);
        static readonly Color Cream = new Color32(245, 230, 196, 255);
        static readonly Color Orange = new Color32(244, 153, 64, 255);
        static readonly Color Green = new Color32(139, 209, 158, 255);

        public static void Draw(int[][] numbers, int[] indices, bool[] locked, int selected,
            string judgement, Color judgementColor, bool showJudgement)
        {
            Color previous = GUI.color;
            label ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
            label.font = MokUIFont.Font;
            // Folded sheet metal: shallow top lip, dark underside, enamel face and fasteners.
            Fill(390, 626, 1140, 392, new Color32(13, 20, 23, 200));
            Fill(370, 596, 1180, 386, new Color32(54, 68, 69, 255));
            Fill(370, 596, 1180, 18, new Color32(177, 189, 177, 255));
            Fill(386, 614, 1148, 344, new Color32(105, 125, 120, 255));
            Fill(386, 614, 1148, 3, new Color32(212, 215, 191, 255));
            Fill(386, 958, 1148, 24, new Color32(39, 54, 54, 255));
            foreach (float x in new[] { 406f, 1514f })
                foreach (float y in new[] { 634f, 938f })
                {
                    Disc(x, y, 8, new Color32(51, 66, 66, 255));
                    Fill(x - 4, y - 1, 8, 2, new Color32(174, 183, 170, 255));
                }

            Text(new Rect(430, 632, 260, 36), "주방용 전기 튀김기", 26, Cream);
            Text(new Rect(430, 673, 260, 25), "온도 설정  /  06", 19, Ink);
            Fill(446, 718, 226, 119, Ink);
            Text(new Rect(446, 723, 226, 28), "목표 온도", 19, new Color32(157, 178, 162, 255));
            Text(new Rect(446, 752, 226, 69), "180 도", 49, Cream);
            Disc(466, 868, 7, Green);
            Text(new Rect(480, 850, 185, 35), "전원 켜짐", 18, Cream);
            for (int i = 0; i < 6; i++) Fill(450, 901 + i * 5, 217, 2, new Color32(65, 83, 81, 255));

            for (int i = 0; i < 3; i++)
            {
                float x = 825 + i * 245;
                bool active = i == selected;
                Color light = locked[i] ? Green : active ? Orange : new Color32(66, 83, 80, 255);
                Fill(x - 73, 635, 146, 80, new Color32(150, 163, 148, 255));
                Fill(x - 68, 640, 136, 70, Ink);
                Text(new Rect(x - 68, 639, 136, 70), numbers[i][indices[i]].ToString(), 54,
                    locked[i] ? Green : active ? Cream : new Color32(144, 162, 151, 255));
                Text(new Rect(x - 100, 719, 200, 27),
                    locked[i] ? "설정 완료" : active ? "이 손잡이를 돌리세요" : "대기", 18, locked[i] ? Green : Cream);

                for (int tick = 0; tick < 20; tick++)
                {
                    float angle = tick * 18 * Mathf.Deg2Rad;
                    Disc(x + Mathf.Sin(angle) * 77, 832 - Mathf.Cos(angle) * 77, 2.3f,
                        active ? Cream : new Color32(73, 92, 88, 255));
                }
                Disc(x + 3, 837, 63, new Color32(49, 66, 64, 255));
                Disc(x, 832, 60, new Color32(178, 188, 173, 255));
                Disc(x, 832, 53, new Color32(42, 54, 55, 255));
                Disc(x - 2, 829, 44, new Color32(59, 72, 72, 255));
                float radians = indices[i] * 18f * Mathf.Deg2Rad;
                for (int part = 0; part < 10; part++)
                {
                    float distance = 8 + part * 4;
                    float markX = x + Mathf.Sin(radians) * distance;
                    float markY = 832 - Mathf.Cos(radians) * distance;
                    Disc(markX, markY, 5, part > 5 ? light : Ink);
                }                Disc(x - 60, 932, 5, light);
                Text(new Rect(x - 50, 915, 140, 30), locked[i] ? "고정" : active ? "조절 중" : "미설정", 19, Cream);
            }
            Text(new Rect(450, 997, 1020, 34), "A / D 또는 마우스 휠 : 회전       SPACE : 숫자 확정       F : 나오기", 22, Cream);
            Text(new Rect(670, 550, 580, 32), "왼쪽부터 1 / 8 / 0으로 맞추세요", 25, Cream);
            if (showJudgement) Text(new Rect(570, 486, 780, 55), judgement, 36, judgementColor);
            GUI.color = previous;
        }

        static void Fill(float x, float y, float w, float h, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
        }

        static void Text(Rect rect, string text, int size, Color color)
        {
            GUI.color = Color.white;
            label.fontSize = size;
            label.normal.textColor = color;
            GUI.Label(rect, text, label);
        }

        static void Disc(float x, float y, float radius, Color color)
        {
            if (knob == null)
            {
                // Twelve-sided silhouette matches the game's faceted props, without a new asset.
                knob = new Texture2D(128, 128, TextureFormat.RGBA32, false)
                { hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Bilinear };
                var pixels = new Color32[128 * 128];
                for (int py = 0; py < 128; py++)
                    for (int px = 0; px < 128; px++)
                    {
                        float dx = px - 63.5f, dy = py - 63.5f;
                        float sector = Mathf.Repeat(Mathf.Atan2(dy, dx) + Mathf.PI / 12, Mathf.PI / 6) - Mathf.PI / 12;
                        float edge = 61 * Mathf.Cos(Mathf.PI / 12) / Mathf.Cos(sector);
                        byte alpha = (byte)(255 * Mathf.Clamp01(edge - Mathf.Sqrt(dx * dx + dy * dy) + .5f));
                        pixels[py * 128 + px] = new Color32(255, 255, 255, alpha);
                    }
                knob.SetPixels32(pixels);
                knob.Apply(false, true);
            }
            GUI.color = color;
            GUI.DrawTexture(new Rect(x - radius, y - radius, radius * 2, radius * 2), knob);
        }
    }
}

