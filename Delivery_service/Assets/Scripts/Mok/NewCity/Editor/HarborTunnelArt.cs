#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace DeliveryService.Mok.NewCity.Editor
{
 // 단면이 드러나는 다각형 아치. 메시 자산으로 저장되어 빌드와 Scene 뷰에서도 동일하다.
 internal static class HarborTunnelArt
 {
  static Material concrete,lining,trim,dark,light,paint;
  public static void Build()
  {
   concrete=DistrictArt.Mat("Tunnel pale concrete",new Color(.51f,.52f,.47f));
   lining=DistrictArt.Mat("Tunnel interior concrete",new Color(.28f,.32f,.32f));
   trim=DistrictArt.Mat("Tunnel stone trim",new Color(.64f,.62f,.53f));
   dark=DistrictArt.Mat("Tunnel dark metal",new Color(.065f,.095f,.10f));
   paint=DistrictArt.Mat("Tunnel amber markings",new Color(.9f,.61f,.14f));
   light=DistrictArt.Mat("Tunnel lamp diffuser",new Color(.95f,.85f,.61f));light.EnableKeyword("_EMISSION");light.SetColor("_EmissionColor",new Color(1,.78f,.42f)*2);
   foreach(var material in new[]{concrete,lining,trim}){material.SetFloat("_ReceiveShadows",0);material.EnableKeyword("_RECEIVE_SHADOWS_OFF");EditorUtility.SetDirty(material);}
   Arch("터널 아치형 내벽",-80,-53,5.05f,3.8f,5.6f,4.3f,lining);
   Arch("입구 두꺼운 석재 아치",-53,-51.7f,5.05f,3.8f,6.05f,4.8f,trim);
   foreach(float side in new[]{-1f,1f})
   {
    Box("터널 하부 측벽",new Vector3(-66,1,-70+side*5.55f),new Vector3(28,2,1),concrete);
    Box("입구 기둥 받침",new Vector3(-52.3f,1,-70+side*5.55f),new Vector3(1.5f,2,1.15f),trim);
    Box("벽면 보호 띠",new Vector3(-66,.95f,-70+side*5.02f),new Vector3(27,.25f,.06f),dark,false);
    Box("터널 배수로",new Vector3(-66,.09f,-70+side*4.7f),new Vector3(27,.035f,.28f),dark,false);
    Box("터널 노견선",new Vector3(-66,.105f,-70+side*4.2f),new Vector3(27,.012f,.11f),paint,false);
    for(int i=0;i<22;i++)Box("배수 격자",new Vector3(-53-i*1.2f,.12f,-70+side*4.7f),new Vector3(.055f,.02f,.28f),trim,false);
    for(int i=0;i<7;i++)
    {
     float x=-53.5f-i*4;
     Box("벽면 반사 유도판",new Vector3(x,1.15f,-70+side*4.98f),new Vector3(.3f,.17f,.05f),paint,false);
     Box("터널 벽등 받침",new Vector3(x,2.85f,-70+side*4.85f),new Vector3(.7f,.24f,.2f),dark,false);
     Box("터널 벽등 발광면",new Vector3(x,2.82f,-70+side*4.72f),new Vector3(.54f,.13f,.05f),light,false);
     DistrictArt.Lamp(new Vector3(x,2.6f,-70+side*4.1f),5,.8f,new Color(1,.82f,.59f));
    }
   }
   for(int i=0;i<7;i++)Arch("터널 반복 보강 아치 "+i,-54-i*4-.12f,-54-i*4+.12f,4.96f,3.71f,5.07f,3.82f,concrete);
   Box("입구 도로 안내판",new Vector3(-51.56f,6.3f,-70),new Vector3(.18f,.72f,4.5f),dark,false);
   DistrictArt.Sign("CITY TUNNEL",new Vector3(-51.44f,6.3f,-70),.085f,270);
   for(int i=0;i<9;i++)Box("터널 중앙 점선",new Vector3(-53-i*3,.11f,-70),new Vector3(1.5f,.012f,.1f),paint,false);
   // 북쪽 서비스 측면에 작은 설비함을 배치한다. 보행로에는 돌출시키지 않는다.
   Box("비상 설비함",new Vector3(-55.5f,1.25f,-65.13f),new Vector3(.75f,1,.18f),dark,false);
   Box("비상 설비함 표시",new Vector3(-55.5f,1.4f,-65.24f),new Vector3(.48f,.12f,.025f),paint,false);
   Box("터널 끝 암부",new Vector3(-80,3,-70),new Vector3(.4f,6,10),dark);
  }
  static void Box(string name,Vector3 p,Vector3 s,Material m,bool solid=true)=>DistrictArt.Box(name,p,s,m,solid);
  static void Arch(string name,float rear,float front,float innerWidth,float innerRise,float outerWidth,float outerRise,Material mat)
  {
   var v=new List<Vector3>();var tris=new List<int>();
   for(int i=0;i<16;i++)
   {
    float a=i*Mathf.PI/16,b=(i+1)*Mathf.PI/16;
    Vector3 Point(float x,float angle,bool outer)=>new Vector3(x,2+Mathf.Sin(angle)*(outer?outerRise:innerRise),-70+Mathf.Cos(angle)*(outer?outerWidth:innerWidth));
    var p=new[]{Point(front,a,false),Point(front,b,false),Point(front,b,true),Point(front,a,true),Point(rear,a,false),Point(rear,b,false),Point(rear,b,true),Point(rear,a,true)};
    void Quad(int aa,int bb,int cc,int dd){int n=v.Count;v.Add(p[aa]);v.Add(p[bb]);v.Add(p[cc]);v.Add(p[dd]);tris.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});}
    Quad(0,1,2,3);Quad(7,6,5,4);Quad(4,5,1,0);Quad(3,2,6,7);Quad(0,3,7,4);Quad(5,6,2,1);
   }
   var mesh=new Mesh{name=name};mesh.SetVertices(v);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
   string path=DistrictArt.Folder+"/Tunnel-"+name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing){existing.Clear();EditorUtility.CopySerialized(mesh,existing);existing.UploadMeshData(false);EditorUtility.SetDirty(existing);Object.DestroyImmediate(mesh);mesh=existing;}else AssetDatabase.CreateAsset(mesh,path);
   var g=new GameObject(name);g.transform.SetParent(DistrictArt.Parent,false);g.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=g.AddComponent<MeshRenderer>();renderer.sharedMaterial=mat;renderer.receiveShadows=false;g.AddComponent<MeshCollider>().sharedMesh=mesh;g.isStatic=true;
  }
 }
}
#endif



