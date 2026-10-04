using UnityEngine;
using UnityEngine.UI;

namespace DeliveryService.Yun.ShopUI
{
    // Small code-drawn illustrations; labels and quantities remain real UI text.
    public sealed class ShopUiArtwork : MaskableGraphic
    {
        public int Kind;
        private VertexHelper mesh;
        private Rect bounds;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); mesh = vh;
            Rect r = rectTransform.rect;
            float side = Mathf.Min(r.width, r.height);
            bounds = new Rect(r.center - Vector2.one * side / 2, Vector2.one * side);
            Color dough = new Color32(242, 191, 115, 255);
            Color cheese = new Color32(255, 202, 60, 255);
            Color red = new Color32(190, 57, 37, 255);
            if (Kind == -1)
            {
                Triangle(new Vector2(.1f,.2f), new Vector2(.8f,.12f), new Vector2(.68f,.93f), Color.white);
                Triangle(new Vector2(.2f,.25f), new Vector2(.73f,.2f), new Vector2(.65f,.79f), new Color32(25,43,66,255));
                Circle(.42f,.38f,.065f,Color.white); Circle(.61f,.59f,.07f,Color.white); Circle(.61f,.3f,.04f,Color.white);
            }
            else if (Kind == 0)
            {
                Circle(.5f,.5f,.44f,dough); Circle(.5f,.53f,.38f,new Color32(255,223,169,255));
                Circle(.3f,.55f,.05f,dough); Circle(.64f,.65f,.04f,dough); Circle(.56f,.37f,.05f,dough);
            }
            else if (Kind == 1)
            {
                Triangle(new Vector2(.1f,.15f),new Vector2(.91f,.26f),new Vector2(.66f,.91f),cheese);
                Circle(.4f,.32f,.065f,dough); Circle(.63f,.55f,.08f,dough);
            }
            else if (Kind == 2)
            {
                Circle(.5f,.47f,.43f,new Color32(207,216,223,255));
                Circle(.5f,.58f,.36f,red); Circle(.5f,.58f,.23f,new Color32(225,88,48,255));
                Circle(.5f,.58f,.16f,red);
            }
            else if (Kind == 3)
            {
                Circle(.38f,.61f,.28f,red); Circle(.67f,.4f,.28f,red); Circle(.32f,.3f,.25f,red);
                foreach (Vector2 p in new[]{new Vector2(.3f,.67f),new Vector2(.45f,.54f),new Vector2(.72f,.47f),new Vector2(.25f,.26f)})
                    Circle(p.x,p.y,.055f,new Color32(244,135,99,255));
            }
            else if (Kind == 4)
            {
                for(int x=0;x<3;x++) for(int y=0;y<2;y++)
                {
                    Quad(.08f+x*.29f,.2f+y*.3f,.27f,.28f,new Color32(101,51,32,255));
                    Quad(.11f+x*.29f,.24f+y*.3f,.21f,.20f,new Color32(143,76,46,255));
                }
            }
            else if (Kind == 5)
            {
                Circle(.5f,.5f,.44f,cheese); Circle(.5f,.5f,.34f,new Color32(255,225,112,255));
                Circle(.5f,.5f,.15f,new Color32(242,246,249,255));
            }
            else
            {
                Bear(.23f,.40f,new Color32(234,68,53,255));
                Bear(.50f,.57f,new Color32(112,176,51,255));
                Bear(.77f,.38f,cheese);
            }
        }
        private void Bear(float x,float y,Color c)
        {
            Circle(x,y,.15f,c); Circle(x,y+.20f,.12f,c);
            Circle(x-.08f,y+.28f,.05f,c); Circle(x+.08f,y+.28f,.05f,c);
            Circle(x-.08f,y-.14f,.07f,c); Circle(x+.08f,y-.14f,.07f,c);
        }
        private Vector2 Point(Vector2 p) => bounds.min + Vector2.Scale(p,bounds.size);
        private void Triangle(Vector2 a,Vector2 b,Vector2 c,Color color)
        {
            int n=mesh.currentVertCount;
            mesh.AddVert(Point(a),color,Vector2.zero); mesh.AddVert(Point(b),color,Vector2.zero); mesh.AddVert(Point(c),color,Vector2.zero);
            mesh.AddTriangle(n,n+1,n+2);
        }
        private void Quad(float x,float y,float w,float h,Color c)
        {
            Triangle(new Vector2(x,y),new Vector2(x+w,y),new Vector2(x+w,y+h),c);
            Triangle(new Vector2(x,y),new Vector2(x+w,y+h),new Vector2(x,y+h),c);
        }
        private void Circle(float x,float y,float radius,Color color)
        {
            for(int i=0;i<32;i++)
            {
                float a=i*Mathf.PI/16,b=(i+1)*Mathf.PI/16;
                Triangle(new Vector2(x,y),new Vector2(x+Mathf.Cos(a)*radius,y+Mathf.Sin(a)*radius),
                    new Vector2(x+Mathf.Cos(b)*radius,y+Mathf.Sin(b)*radius),color);
            }
        }
    }
}
