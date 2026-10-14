using UnityEngine;
using UnityEngine.UI;

namespace DeliveryService.Yun.Settings
{
    // Vector UI artwork: remains sharp at any Canvas scale and editable in the Inspector.
    public sealed class SettingsIconGraphic : MaskableGraphic
    {
        public enum Symbol { Gear, Music, Speaker, Trophy, Home, Power, SliderKnob }
        [SerializeField] private Symbol symbol;
        public Symbol Icon { get => symbol; set { symbol = value; SetVerticesDirty(); } }
        private VertexHelper mesh;
        private Rect bounds;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            mesh = vh; mesh.Clear();
            float size = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height);
            bounds = new Rect(rectTransform.rect.center - Vector2.one * size / 2, Vector2.one * size);
            switch (symbol)
            {
                case Symbol.SliderKnob:
                    Disc(new Vector2(.5f,.5f),.49f,color);
                    Disc(new Vector2(.5f,.5f),.37f,new Color32(255,252,240,255));
                    break;
                case Symbol.Gear:
                    for (int i = 0; i < 40; i++)
                    {
                        float a = i * Mathf.PI / 20, b = (i + 1) * Mathf.PI / 20;
                        float ra = (i % 4 == 1 || i % 4 == 2) ? .48f : .40f;
                        float rb = ((i + 1) % 4 == 1 || (i + 1) % 4 == 2) ? .48f : .40f;
                        Quad(Polar(a, ra), Polar(b, rb), Polar(b, .29f), Polar(a, .29f));
                    }
                    Disc(new Vector2(.5f,.5f), .17f, new Color32(15,158,179,255));
                    break;
                case Symbol.Music:
                    Line(.38f,.25f,.38f,.81f); Line(.38f,.81f,.78f,.91f);
                    Line(.78f,.91f,.78f,.36f); Line(.38f,.66f,.78f,.76f);
                    Disc(new Vector2(.25f,.24f),.16f,color); Disc(new Vector2(.65f,.34f),.16f,color);
                    break;
                case Symbol.Speaker:
                    Line(.10f,.35f,.28f,.35f); Line(.28f,.35f,.48f,.17f);
                    Line(.48f,.17f,.48f,.83f); Line(.48f,.83f,.28f,.65f);
                    Line(.28f,.65f,.10f,.65f); Line(.10f,.65f,.10f,.35f);
                    Arc(new Vector2(.45f,.5f),.28f,-.85f,.85f,.065f);
                    Arc(new Vector2(.45f,.5f),.46f,-.85f,.85f,.065f);
                    break;
                case Symbol.Trophy:
                    Line(.27f,.83f,.73f,.83f); Line(.27f,.83f,.31f,.47f);
                    Line(.73f,.83f,.69f,.47f); Arc(new Vector2(.5f,.47f),.19f,Mathf.PI,2*Mathf.PI,.085f);
                    Line(.5f,.27f,.5f,.12f); Line(.29f,.10f,.71f,.10f);
                    Line(.27f,.74f,.10f,.74f); Line(.10f,.74f,.14f,.49f); Line(.14f,.49f,.31f,.43f);
                    Line(.73f,.74f,.90f,.74f); Line(.90f,.74f,.86f,.49f); Line(.86f,.49f,.69f,.43f);
                    break;
                case Symbol.Home:
                    Line(.10f,.51f,.50f,.87f); Line(.50f,.87f,.90f,.51f);
                    Line(.23f,.60f,.23f,.14f); Line(.77f,.60f,.77f,.14f);
                    Line(.23f,.14f,.42f,.14f); Line(.58f,.14f,.77f,.14f);
                    Line(.42f,.14f,.42f,.40f); Line(.42f,.40f,.58f,.40f); Line(.58f,.40f,.58f,.14f);
                    break;
                case Symbol.Power:
                    Arc(new Vector2(.5f,.45f),.35f,2.20f,7.22f,.095f);
                    Line(.5f,.53f,.5f,.93f,.10f);
                    break;
            }
        }
        private Vector2 Polar(float angle, float radius) => new Vector2(.5f + Mathf.Cos(angle)*radius,.5f + Mathf.Sin(angle)*radius);
        private Vector2 P(Vector2 value) => bounds.min + Vector2.Scale(value,bounds.size);
        private void Quad(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            int n=mesh.currentVertCount;
            foreach(var p in new[]{a,b,c,d}) mesh.AddVert(P(p),color,Vector2.zero);
            mesh.AddTriangle(n,n+1,n+2); mesh.AddTriangle(n,n+2,n+3);
        }
        private void Disc(Vector2 center,float radius,Color ink)
        {
            for(int i=0;i<32;i++)
            {
                float a=i*Mathf.PI/16,b=(i+1)*Mathf.PI/16; int n=mesh.currentVertCount;
                mesh.AddVert(P(center),ink,Vector2.zero);
                mesh.AddVert(P(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius),ink,Vector2.zero);
                mesh.AddVert(P(center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius),ink,Vector2.zero);
                mesh.AddTriangle(n,n+1,n+2);
            }
        }
        private void Line(float ax,float ay,float bx,float by,float width=.085f)
        {
            var a=new Vector2(ax,ay); var b=new Vector2(bx,by);
            Vector2 v=(b-a).normalized; Vector2 normal=new Vector2(-v.y,v.x)*width/2;
            Quad(a-normal,b-normal,b+normal,a+normal);
            Disc(a,width/2,color); Disc(b,width/2,color);
        }
        private void Arc(Vector2 center,float radius,float from,float to,float width)
        {
            for(int i=0;i<24;i++)
            {
                float a=Mathf.Lerp(from,to,i/24f),b=Mathf.Lerp(from,to,(i+1)/24f);
                Line(center.x+Mathf.Cos(a)*radius,center.y+Mathf.Sin(a)*radius,
                    center.x+Mathf.Cos(b)*radius,center.y+Mathf.Sin(b)*radius,width);
            }
        }
    }
}
