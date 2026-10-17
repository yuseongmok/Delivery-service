using UnityEngine;
using UnityEngine.UI;
namespace DeliveryService.Mok {
// 피자 조각과 속도선을 조합한 로비 심볼. UI 메시로 그려 확대해도 선명하다.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class DeliveryBrandSymbol : MaskableGraphic {
 protected override void OnPopulateMesh(VertexHelper v){v.Clear();var cream=new Color32(255,236,193,255);var orange=new Color32(222,126,60,255);var red=new Color32(144,62,39,255);
 Poly(v,cream,new Vector2(.35f,.88f),new Vector2(.95f,.75f),new Vector2(.55f,.12f));
 Poly(v,orange,new Vector2(.43f,.76f),new Vector2(.82f,.67f),new Vector2(.56f,.26f));
 Poly(v,orange,new Vector2(.32f,.88f),new Vector2(.36f,.99f),new Vector2(.99f,.85f),new Vector2(.95f,.74f));
 Disc(v,red,.57f,.65f,.055f);Disc(v,red,.70f,.61f,.05f);Disc(v,red,.57f,.44f,.045f);
 Poly(v,cream,new Vector2(.02f,.69f),new Vector2(.29f,.69f),new Vector2(.32f,.62f),new Vector2(.02f,.62f));
 Poly(v,cream,new Vector2(.10f,.51f),new Vector2(.34f,.51f),new Vector2(.37f,.44f),new Vector2(.10f,.44f));
 Poly(v,cream,new Vector2(.22f,.33f),new Vector2(.39f,.33f),new Vector2(.42f,.26f),new Vector2(.22f,.26f));}
 void Disc(VertexHelper v,Color32 c,float x,float y,float radius){var p=new Vector2[8];for(int i=0;i<8;i++){float a=i*Mathf.PI/4;p[i]=new Vector2(x+Mathf.Cos(a)*radius,y+Mathf.Sin(a)*radius);}Poly(v,c,p);}
 void Poly(VertexHelper v,Color32 c,params Vector2[] p){var r=GetPixelAdjustedRect();int start=v.currentVertCount;foreach(var q in p)v.AddVert(new Vector3(r.x+q.x*r.width,r.y+q.y*r.height),c,Vector2.zero);for(int i=1;i<p.Length-1;i++)v.AddTriangle(start,start+i,start+i+1);}
}
}
