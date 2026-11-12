using UnityEngine;
using UnityEngine.UI;
namespace DeliveryService.Mok {
// 해상도에 관계없이 선명하게 표시되는 카드 전용 벡터 아이콘.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class LobbyModeIcon : MaskableGraphic {
    public int kind;
    VertexHelper mesh;
    Vector2 Point(float x,float y){var r=rectTransform.rect;return new Vector2(r.xMin+x*r.width,r.yMin+y*r.height);}
    void Line(float x,float y,float a,float b,float width=.025f){
        Vector2 p=Point(x,y),q=Point(a,b),n=new Vector2(-(q-p).y,(q-p).x).normalized*rectTransform.rect.width*width/2;
        int i=mesh.currentVertCount;mesh.AddVert(p+n,color,Vector2.zero);mesh.AddVert(q+n,color,Vector2.zero);mesh.AddVert(q-n,color,Vector2.zero);mesh.AddVert(p-n,color,Vector2.zero);mesh.AddTriangle(i,i+1,i+2);mesh.AddTriangle(i,i+2,i+3);
    }
    void Circle(float x,float y,float radius){for(int j=0;j<40;j++){float a=j*Mathf.PI/20,b=(j+1)*Mathf.PI/20;Line(x+Mathf.Cos(a)*radius,y+Mathf.Sin(a)*radius,x+Mathf.Cos(b)*radius,y+Mathf.Sin(b)*radius);}}
    protected override void OnPopulateMesh(VertexHelper vh){mesh=vh;vh.Clear();
        if(kind==0){
            Line(.15f,.23f,.15f,.75f);Line(.15f,.75f,.38f,.65f);Line(.38f,.65f,.62f,.75f);Line(.62f,.75f,.85f,.65f);Line(.85f,.65f,.85f,.15f);Line(.85f,.15f,.62f,.25f);Line(.62f,.25f,.38f,.15f);Line(.38f,.15f,.15f,.23f);
            Line(.38f,.15f,.38f,.55f,.012f);Line(.62f,.25f,.62f,.42f,.012f);
            Circle(.62f,.78f,.10f);Line(.535f,.73f,.62f,.55f);Line(.705f,.73f,.62f,.55f);
            for(int j=0;j<4;j++)Line(.25f+j*.08f,.35f+j*.035f,.28f+j*.08f,.365f+j*.035f,.02f);
        }else if(kind==1){
            Line(.15f,.2f,.85f,.2f);Line(.2f,.2f,.2f,.66f);Line(.8f,.2f,.8f,.66f);Line(.12f,.66f,.88f,.66f);Line(.12f,.66f,.23f,.85f);Line(.23f,.85f,.77f,.85f);Line(.77f,.85f,.88f,.66f);
            for(int j=0;j<5;j++)Line(.25f+j*.125f,.83f,.22f+j*.14f,.68f,.017f);
            Line(.3f,.2f,.3f,.51f);Line(.3f,.51f,.47f,.51f);Line(.47f,.51f,.47f,.2f);
            Circle(.65f,.43f,.115f);Circle(.62f,.46f,.015f);Circle(.69f,.41f,.015f);Circle(.62f,.38f,.015f);
        }else{
            for(int j=0;j<3;j++){float y=.25f+j*.25f,x=j==1?.65f:.35f;Line(.15f,y,x-.07f,y);Line(x+.07f,y,.85f,y);Circle(x,y,.07f);}
        }
    }
}
}
