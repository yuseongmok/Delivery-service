using UnityEngine;
using UnityEngine.UI;
namespace DeliveryService.Mok {
[RequireComponent(typeof(CanvasRenderer))]
public sealed class LobbyShade : MaskableGraphic {
 protected override void OnPopulateMesh(VertexHelper vh){vh.Clear();var r=GetPixelAdjustedRect();var a=new Color32(9,19,24,245);var b=new Color32(9,19,24,0);vh.AddVert(new Vector3(r.xMin,r.yMin),a,Vector2.zero);vh.AddVert(new Vector3(r.xMin,r.yMax),a,Vector2.zero);vh.AddVert(new Vector3(r.xMax,r.yMax),b,Vector2.zero);vh.AddVert(new Vector3(r.xMax,r.yMin),b,Vector2.zero);vh.AddTriangle(0,1,2);vh.AddTriangle(2,3,0);}
}
}
