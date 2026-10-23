using UnityEngine;
using UnityEngine.UI;
namespace DeliveryService.Mok {
// 글꼴에 의존하지 않는 마름모. 화면 밖 목표도 가장자리에 남긴다.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class StoryTargetDiamond : MaskableGraphic {
 protected override void OnPopulateMesh(VertexHelper vh){vh.Clear();var r=rectTransform.rect;Vector2[] p={new(0,.5f),new(.5f,0),new(0,-.5f),new(-.5f,0)};for(int i=0;i<4;i++){var a=p[i];var b=p[(i+1)%4];int n=vh.currentVertCount;foreach(var v in new[]{a,b,b*.66f,a*.66f})vh.AddVert(new Vector3(r.center.x+v.x*r.width,r.center.y+v.y*r.height),color,Vector2.zero);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);}}
 public static void Place(Text label,Camera camera,Vector3 target,Vector3 observer,string title){
  var p=camera.WorldToViewportPoint(target);bool behind=p.z<=0;Vector2 d=new(p.x-.5f,p.y-.5f);if(behind)d=-d;if(behind&&d.sqrMagnitude<.001f)d=Vector2.down;
  bool outside=behind||p.x<.08f||p.x>.92f||p.y<.16f||p.y>.80f;
  if(outside){float edge=Mathf.Min(.42f/Mathf.Max(.001f,Mathf.Abs(d.x)),.30f/Mathf.Max(.001f,Mathf.Abs(d.y)));p.x=.5f+d.x*edge;p.y=.48f+d.y*edge;}
  label.rectTransform.anchorMin=label.rectTransform.anchorMax=new Vector2(Mathf.Clamp(p.x,.08f,.92f),Mathf.Clamp(p.y,.18f,.80f));label.rectTransform.anchoredPosition=Vector2.zero;
  label.text=title+" · "+Vector3.Distance(observer,target).ToString("0")+"m";
 }
}
}
