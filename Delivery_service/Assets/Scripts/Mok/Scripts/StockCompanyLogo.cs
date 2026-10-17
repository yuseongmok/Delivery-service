using UnityEngine;
using UnityEngine.UI;
namespace DeliveryService.Mok.Stocks {
// 작은 휴대폰에서도 구분되는 단색 기하 로고. 확대해도 선명한 UI 메시로 그린다.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class StockCompanyLogo : MaskableGraphic {
    public int Company {get=>company;set{company=Mathf.Clamp(value,0,4);SetVerticesDirty();}}
    [SerializeField] int company;
    static readonly Color32[] Palette={new Color32(54,91,103,255),new Color32(192,115,51,255),new Color32(62,127,119,255),new Color32(183,147,68,255),new Color32(85,102,150,255)};
    VertexHelper mesh;
    protected override void OnPopulateMesh(VertexHelper vh){
        mesh=vh;vh.Clear();
        Polygon(Palette[company],new Vector2(.18f,.06f),new Vector2(.82f,.06f),new Vector2(.98f,.5f),new Vector2(.82f,.94f),new Vector2(.18f,.94f),new Vector2(.02f,.5f));
        var ink=new Color32(245,240,219,255);
        switch(company){
            case 0: // 모터웍스: 기계적인 M과 속도선
                Polygon(ink,new Vector2(.20f,.28f),new Vector2(.31f,.73f),new Vector2(.50f,.48f),new Vector2(.69f,.73f),new Vector2(.80f,.28f),new Vector2(.65f,.28f),new Vector2(.61f,.47f),new Vector2(.50f,.32f),new Vector2(.39f,.47f),new Vector2(.35f,.28f));
                Quad(.30f,.79f,.70f,.84f,ink);break;
            case 1: // 시티에너지: 번개
                Polygon(ink,new Vector2(.54f,.84f),new Vector2(.27f,.45f),new Vector2(.46f,.45f),new Vector2(.38f,.16f),new Vector2(.75f,.58f),new Vector2(.55f,.58f),new Vector2(.65f,.84f));break;
            case 2: // 퀵박스: 상자와 배송 화살표
                Quad(.23f,.26f,.77f,.73f,ink);Quad(.47f,.54f,.54f,.73f,Palette[company]);
                Polygon(Palette[company],new Vector2(.32f,.40f),new Vector2(.57f,.40f),new Vector2(.57f,.32f),new Vector2(.71f,.46f),new Vector2(.57f,.60f),new Vector2(.57f,.51f),new Vector2(.32f,.51f));break;
            case 3: // 치즈팜: 치즈 조각
                Polygon(ink,new Vector2(.23f,.27f),new Vector2(.80f,.27f),new Vector2(.80f,.58f),new Vector2(.33f,.78f),new Vector2(.23f,.58f));
                Quad(.34f,.37f,.43f,.46f,Palette[company]);Quad(.59f,.33f,.69f,.43f,Palette[company]);Quad(.52f,.55f,.60f,.63f,Palette[company]);break;
            case 4: // 네온테크: 회로 칩
                Quad(.29f,.29f,.71f,.71f,ink);Quad(.38f,.38f,.62f,.62f,Palette[company]);
                for(int i=0;i<3;i++){float t=.34f+i*.13f;Quad(t,.19f,t+.055f,.30f,ink);Quad(t,.70f,t+.055f,.81f,ink);Quad(.19f,t,.30f,t+.055f,ink);Quad(.70f,t,.81f,t+.055f,ink);}break;
        }
    }
    void Quad(float x0,float y0,float x1,float y1,Color32 c){Polygon(c,new Vector2(x0,y0),new Vector2(x1,y0),new Vector2(x1,y1),new Vector2(x0,y1));}
    void Polygon(Color32 c,params Vector2[] points){
        // 오목한 문양도 외곽 순서대로 귀를 잘라 삼각형으로 만든다.
        var rect=GetPixelAdjustedRect();int start=mesh.currentVertCount;
        foreach(var p in points)mesh.AddVert(new Vector3(rect.x+p.x*rect.width,rect.y+p.y*rect.height),c,Vector2.zero);
        var ids=new System.Collections.Generic.List<int>();for(int i=0;i<points.Length;i++)ids.Add(i);
        float area=0;for(int i=0;i<points.Length;i++)area+=Cross(points[i],points[(i+1)%points.Length]);float sign=area>=0?1:-1;
        for(int guard=0;ids.Count>2&&guard<100;guard++)for(int i=0;i<ids.Count;i++){
            int a=ids[(i+ids.Count-1)%ids.Count],b=ids[i],d=ids[(i+1)%ids.Count];
            if(Cross(points[b]-points[a],points[d]-points[b])*sign<=0)continue;
            bool inside=false;foreach(int j in ids){if(j==a||j==b||j==d)continue;var p=points[j];if(Cross(points[b]-points[a],p-points[a])*sign>=0&&Cross(points[d]-points[b],p-points[b])*sign>=0&&Cross(points[a]-points[d],p-points[d])*sign>=0){inside=true;break;}}
            if(inside)continue;mesh.AddTriangle(start+a,start+b,start+d);ids.RemoveAt(i);break;
        }
    }
    static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
}}
