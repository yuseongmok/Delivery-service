using UnityEngine;using UnityEngine.UI;
namespace DeliveryService.Mok {
[RequireComponent(typeof(CanvasRenderer))]
public sealed class LadderGraphic : MaskableGraphic {
 public LadderGame Game;public bool Reveal;public int Selection;public float Progress;
 VertexHelper mesh;
 Vector2 Pixel(Vector2 p){var r=rectTransform.rect;return new Vector2(r.x+p.x*r.width,r.y+(1-p.y)*r.height);}
 void Line(Vector2 a,Vector2 b,float width,Color c){a=Pixel(a);b=Pixel(b);var d=(b-a).normalized;var n=new Vector2(-d.y,d.x)*width*.5f;int k=mesh.currentVertCount;mesh.AddVert(a-n,c,Vector2.zero);mesh.AddVert(a+n,c,Vector2.zero);mesh.AddVert(b+n,c,Vector2.zero);mesh.AddVert(b-n,c,Vector2.zero);mesh.AddTriangle(k,k+1,k+2);mesh.AddTriangle(k,k+2,k+3);}
 protected override void OnPopulateMesh(VertexHelper vh){mesh=vh;vh.Clear();var rail=new Color(.54f,.61f,.66f);var gold=new Color(1,.65f,.22f);
  for(int i=0;i<3;i++)Line(LadderGame.Node(i,0),LadderGame.Node(i,1),5,rail);
  if(!Reveal||Game==null)return;
  for(int row=0;row<Game.Bridges.Length;row++){int pair=Game.Bridges[row];float y=(row+1f)/9f;Line(LadderGame.Node(pair,y),LadderGame.Node(pair+1,y),5,rail);}
  var path=Game.Path;float length=0;for(int i=1;i<path.Length;i++)length+=Vector2.Distance(Pixel(path[i-1]),Pixel(path[i]));float remaining=length*Progress;
  for(int i=1;i<path.Length&&remaining>0;i++){float segment=Vector2.Distance(Pixel(path[i-1]),Pixel(path[i]));float t=Mathf.Clamp01(remaining/Mathf.Max(.001f,segment));Line(path[i-1],Vector2.Lerp(path[i-1],path[i],t),10,gold);remaining-=segment;}
 }
}
}
