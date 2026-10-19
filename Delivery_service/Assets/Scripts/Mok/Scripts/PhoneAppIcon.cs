using UnityEngine;
using UnityEngine.UI;
namespace DeliveryService.Mok.Navigation {
// 작은 화면에서도 알아볼 수 있도록 단순한 도형으로 앱 아이콘을 그린다.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class PhoneAppIcon : MaskableGraphic {
    public int App;
    bool selected;
    public bool Selected {get=>selected;set{if(selected==value)return;selected=value;SetVerticesDirty();}}
    VertexHelper mesh;
    protected override void OnPopulateMesh(VertexHelper vh){
        mesh=vh;vh.Clear();
        if(selected)Round(.01f,.01f,.98f,.98f,.21f,new Color(.96f,.85f,.60f));
        Color bg=App==0?new Color(.22f,.59f,.51f):App==1||App==3?new Color(.87f,.49f,.22f):new Color(.32f,.47f,.65f);
        Round(.055f,.055f,.89f,.89f,.18f,bg);
        Color paper=new Color(.97f,.95f,.85f);
        if(App==5){
            for(int i=0;i<3;i++)Quad(.22f+i*.25f,.20f,.28f+i*.25f,.80f,paper);
            Quad(.24f,.63f,.52f,.69f,paper);Quad(.49f,.46f,.77f,.52f,paper);Quad(.24f,.30f,.52f,.36f,paper);
        }else if(App==4){
            Poly(paper,new Vector2(.15f,.64f),new Vector2(.5f,.84f),new Vector2(.85f,.64f));
            Quad(.17f,.22f,.83f,.29f,paper);
            for(int i=0;i<3;i++)Quad(.24f+i*.21f,.31f,.34f+i*.21f,.59f,paper);
        }else if(App==3){
 var inner=new Vector2[5];
 for(int i=0;i<5;i++){float a=(90+i*72)*Mathf.Deg2Rad;float b=a-36*Mathf.Deg2Rad;float c=a+36*Mathf.Deg2Rad;
 var center=new Vector2(.5f,.5f);inner[i]=center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*.15f;
 Poly(paper,inner[i],center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*.34f,center+new Vector2(Mathf.Cos(c),Mathf.Sin(c))*.15f);}
 Poly(paper,inner);
 }else if(App==0){
            Poly(paper,new Vector2(.18f,.25f),new Vector2(.37f,.20f),new Vector2(.37f,.64f),new Vector2(.18f,.70f));
            Poly(new Color(.72f,.86f,.72f),new Vector2(.37f,.20f),new Vector2(.58f,.27f),new Vector2(.58f,.71f),new Vector2(.37f,.64f));
            Poly(paper,new Vector2(.58f,.27f),new Vector2(.80f,.22f),new Vector2(.80f,.66f),new Vector2(.58f,.71f));
            Quad(.22f,.39f,.76f,.44f,Color.white);
            var pin=new Color(.86f,.28f,.18f);
            Poly(pin,new Vector2(.46f,.64f),new Vector2(.70f,.64f),new Vector2(.58f,.40f));
            Circle(.58f,.67f,.14f,pin);Circle(.58f,.67f,.058f,paper);
        }else if(App==1){
            Quad(.20f,.22f,.31f,.43f,paper);Quad(.40f,.22f,.51f,.56f,paper);Quad(.60f,.22f,.71f,.72f,paper);
            Line(new Vector2(.20f,.55f),new Vector2(.41f,.72f),.04f,paper);
            Line(new Vector2(.41f,.72f),new Vector2(.52f,.64f),.04f,paper);
            Line(new Vector2(.52f,.64f),new Vector2(.78f,.84f),.04f,paper);
        }else{
            Round(.18f,.20f,.64f,.60f,.04f,paper);
            Quad(.25f,.66f,.75f,.71f,bg);Quad(.25f,.43f,.45f,.60f,bg);
            Quad(.51f,.55f,.74f,.59f,bg);Quad(.51f,.46f,.74f,.50f,bg);
            Quad(.25f,.34f,.74f,.38f,bg);Quad(.25f,.27f,.64f,.30f,bg);
        }
    }
    void Quad(float x,float y,float r,float t,Color c)=>Poly(c,new Vector2(x,y),new Vector2(r,y),new Vector2(r,t),new Vector2(x,t));
    void Line(Vector2 a,Vector2 b,float width,Color c){var d=(b-a).normalized;var n=new Vector2(-d.y,d.x)*width*.5f;Poly(c,a-n,b-n,b+n,a+n);}
    void Circle(float x,float y,float radius,Color c){var p=new Vector2[24];for(int i=0;i<p.Length;i++){float a=i*Mathf.PI*2/p.Length;p[i]=new Vector2(x+Mathf.Cos(a)*radius,y+Mathf.Sin(a)*radius);}Poly(c,p);}
    void Round(float x,float y,float w,float h,float radius,Color c){
        var p=new Vector2[28];int k=0;
        for(int corner=0;corner<4;corner++){float cx=corner==0||corner==3?x+w-radius:x+radius;float cy=corner<2?y+h-radius:y+radius;
            for(int i=0;i<=6;i++){float a=(corner*90+i*15)*Mathf.Deg2Rad;p[k++]=new Vector2(cx+Mathf.Cos(a)*radius,cy+Mathf.Sin(a)*radius);}}
        Poly(c,p);
    }
    void Poly(Color c,params Vector2[] p){var r=GetPixelAdjustedRect();int start=mesh.currentVertCount;foreach(var v in p)mesh.AddVert(new Vector3(r.x+v.x*r.width,r.y+v.y*r.height),c,Vector2.zero);for(int i=1;i<p.Length-1;i++)mesh.AddTriangle(start,start+i,start+i+1);}
}
}
