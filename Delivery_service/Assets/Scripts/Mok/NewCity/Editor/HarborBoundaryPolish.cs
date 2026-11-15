#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace DeliveryService.Mok.NewCity.Editor
{
 [InitializeOnLoad] internal static class HarborBoundaryPolish
 {
  static Material road,stone,iron,paint;static Transform parent;
  static HarborBoundaryPolish(){EditorApplication.update+=Poll;}
  static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlaying||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/HarborBoundaryPolish"))return;File.Delete("Temp/HarborBoundaryPolish");try{Build();}catch(Exception e){File.WriteAllText("Logs/HarborBoundary-error.txt",e.ToString());Debug.LogException(e);}}
  static void Build()
  {
   var scene=SceneManager.GetActiveScene();if(!scene.path.EndsWith("NewHarborCity.unity"))throw new Exception("새 도시 씬 전용 작업입니다.");
   EditorSceneManager.SaveScene(scene,"Logs/HarborBoundary-before-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);DistrictArt.Scene=scene;
   var city=scene.GetRootGameObjects().First(g=>g.name.StartsWith("NEW HARBOR")).transform;
   var old=city.Find("08 해안 산책로와 외곽 도로");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
   DistrictArt.Parent=city;parent=DistrictArt.Group("08 해안 산책로와 외곽 도로");DistrictArt.Parent=parent;
   road=DistrictArt.Mat("Asphalt",Color.gray);stone=DistrictArt.Mat("Boundary seawall",new Color(.48f,.5f,.47f));iron=DistrictArt.Mat("Street iron",Color.gray);paint=DistrictArt.Mat("Road ivory",Color.white);
   var ground=city.Cast<Transform>().First(t=>t.name.StartsWith("01 "));
   foreach(var t in ground.Cast<Transform>().ToArray())
   {
    if(t.name=="동서 도로")t.localScale=new Vector3(144,.04f,10);
    if(t.name=="남북 도로"){t.position=new Vector3(t.position.x,t.position.y,0);t.localScale=new Vector3(10,.04f,120);}
    if(t.name=="중앙 점선"&&(Mathf.Abs(t.position.x)>71||Mathf.Abs(t.position.z)>58))UnityEngine.Object.DestroyImmediate(t.gameObject);
    if(t && t.name=="낮은 보도 경계석"&&Mathf.Abs(t.position.x)>70)UnityEngine.Object.DestroyImmediate(t.gameObject);
   }
   foreach(float side in new[]{-1f,1f})
   {
    Curve(new Vector3(side*72,0,-6),10,-90,0,side);
    Curve(new Vector3(side*72,0,30),10,0,90,side);
    Box("건물 뒤 해안 연결 도로",new Vector3(side*82,-.025f,12),new Vector3(10,.04f,36),road,false);
    for(int z=-3;z<30;z+=6)Box("해안 도로 점선",new Vector3(side*82,.008f,z),new Vector3(.12f,.01f,2.6f),paint,false);
    // 도로의 끝 대신 상가 측면을 먼저 보도록 모퉁이 건물을 둔다.
    Fit("SM_Bld_OfficeOld_Small_01",new Vector3(side*66,.05f,-3),new Vector3(12,12,12),side>0?90:270);
    Fit("SM_Bld_Shop_03",new Vector3(side*66,.05f,27),new Vector3(12,6,12),side>0?90:270);
    foreach(float end in new[]{-1f,1f})
    {
     // 남북 간선 끝은 바깥쪽 물류 출입구로 굽어진다.
     var points=new System.Collections.Generic.List<Vector3>();
     for(int i=0;i<=16;i++){float a=i*Mathf.PI/32;points.Add(new Vector3(side*(50-10*Mathf.Cos(a)),.01f,end*(60+10*Mathf.Sin(a))));}
     Ribbon(points.ToArray(),10);
     Box("물류 출입구 도로",new Vector3(side*52,-.025f,end*70),new Vector3(4,.04f,10),road,false);
     Box("항만 차량 게이트",new Vector3(side*54,1.2f,end*70),new Vector3(.16f,2.4f,10),iron);
     for(int i=0;i<8;i++)Box("게이트 반사판",new Vector3(side*53.88f,1.3f,end*(66.5f+i)),new Vector3(.04f,.18f,.5f),paint,false);
     Fit("SM_Bld_Shop_02",new Vector3(side*59,.05f,end*70),new Vector3(8,7,12),side>0?270:90);
    }
   }
   // 해안은 연속된 보행로, 낮은 방파제와 난간으로 마감한다.
   Box("남쪽 해안 산책로",new Vector3(0,.015f,-76),new Vector3(188,.12f,6),stone);
   Box("북쪽 해안 산책로",new Vector3(0,.015f,76),new Vector3(188,.12f,6),stone);
   foreach(float side in new[]{-1f,1f})Box("동서 해안 산책로",new Vector3(side*91,.015f,0),new Vector3(6,.12f,152),stone);
   Rail(new Vector3(-94,0,-79),new Vector3(94,0,-79));Rail(new Vector3(-94,0,79),new Vector3(94,0,79));Rail(new Vector3(-94,0,-79),new Vector3(-94,0,79));Rail(new Vector3(94,0,-79),new Vector3(94,0,79));
   foreach(float x in new[]{-80f,-20f,20f,80f})foreach(float z in new[]{-76f,76f})
   {DistrictArt.Prop("SM_Prop_ParkBench_01",new Vector3(x,.08f,z),.85f,z<0?180:0,true);DistrictArt.Prop("SM_Prop_Trashbin_01",new Vector3(x+2,.08f,z),.7f,0,true);DistrictArt.Prop("SM_Prop_LightPole_Base_01",new Vector3(x-2,.08f,z),4);DistrictArt.Prop("SM_Prop_LightPole_Lights_01",new Vector3(x-2,3.8f,z),.7f);}
   EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();File.WriteAllText("Temp/HarborBlockPolish","validate");File.WriteAllText("Temp/HarborFinaleAudit","audit");Capture();
  }
  static void Curve(Vector3 center,float radius,float start,float end,float mirror){var p=new Vector3[17];for(int i=0;i<p.Length;i++){float a=Mathf.Lerp(start,end,i/16f)*Mathf.Deg2Rad;p[i]=center+new Vector3(mirror*radius*Mathf.Cos(a),.01f,radius*Mathf.Sin(a));}Ribbon(p,10);}
  static void Ribbon(Vector3[] points,float width)
  {
   var vertices=new Vector3[points.Length*2];var triangles=new int[(points.Length-1)*6];
   for(int i=0;i<points.Length;i++){var tangent=points[Mathf.Min(i+1,points.Length-1)]-points[Mathf.Max(0,i-1)];var normal=Vector3.Cross(Vector3.up,tangent.normalized)*width*.5f;vertices[i*2]=points[i]-normal;vertices[i*2+1]=points[i]+normal;if(i<points.Length-1){int j=i*6;triangles[j]=i*2;triangles[j+1]=i*2+2;triangles[j+2]=i*2+1;triangles[j+3]=i*2+1;triangles[j+4]=i*2+2;triangles[j+5]=i*2+3;}}
   var mesh=new Mesh{vertices=vertices,triangles=triangles};mesh.RecalculateNormals();
   string path=DistrictArt.Folder+"/BoundaryRoad-"+points[0].x.ToString("F0")+"-"+points[0].z.ToString("F0")+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing){EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);mesh=existing;}else AssetDatabase.CreateAsset(mesh,path);
   var g=new GameObject("建物 뒤 곡선 도로");g.transform.SetParent(parent,false);g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=road;g.isStatic=true;
  }
  static void Rail(Vector3 a,Vector3 b)
  {
   var center=(a+b)/2;bool horizontal=Mathf.Abs(b.x-a.x)>1;float length=Vector3.Distance(a,b);
   Box("방파제",center+Vector3.down*.35f,horizontal?new Vector3(length,.9f,.45f):new Vector3(.45f,.9f,length),stone);
   for(int i=0;i<=Mathf.CeilToInt(length/2);i++){var p=Vector3.Lerp(a,b,i/(float)Mathf.CeilToInt(length/2));Box("해안 난간 기둥",p+Vector3.up*.65f,new Vector3(.09f,1.3f,.09f),iron);}
   foreach(float y in new[]{.45f,1.18f})Box("해안 난간 가로대",center+Vector3.up*y,horizontal?new Vector3(length,.07f,.07f):new Vector3(.07f,.07f,length),iron);
   // 난간 틈으로 떨어지지 않도록 난간 높이에 맞춘 충돌체.
   var c=new GameObject("해안 난간 충돌");c.transform.SetParent(parent,false);c.transform.position=center+Vector3.up*.65f;c.AddComponent<BoxCollider>().size=horizontal?new Vector3(length,1.3f,.12f):new Vector3(.12f,1.3f,length);
  }
  static void Fit(string prefab,Vector3 p,Vector3 size,float yaw){var g=DistrictArt.Prop(prefab,p,0,yaw);var b=DistrictArt.Bounds(g);var ratio=new Vector3(size.x/b.size.x,size.y/b.size.y,size.z/b.size.z);if(yaw==90||yaw==270)ratio=new Vector3(ratio.z,ratio.y,ratio.x);g.transform.localScale=Vector3.Scale(g.transform.localScale,ratio);b=DistrictArt.Bounds(g);g.transform.position+=p-new Vector3(b.center.x,b.min.y,b.center.z);b=DistrictArt.Bounds(g);var c=new GameObject("외곽 상가 충돌");c.transform.SetParent(parent,false);c.transform.position=b.center;c.AddComponent<BoxCollider>().size=b.size;PrefabUtility.RecordPrefabInstancePropertyModifications(g.transform);}
  static GameObject Box(string n,Vector3 p,Vector3 s,Material m,bool solid=true)=>DistrictArt.Box(n,p,s,m,solid);
  static void Capture(){var c=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>()).First();var pos=c.transform.position;var rot=c.transform.rotation;var target=c.targetTexture;var active=RenderTexture.active;foreach(var shot in new[]{(new Vector3(62,1.7f,-16),new Vector3(82,1.7f,-10),"bend"),(new Vector3(20,1.7f,-76),new Vector3(-25,1.7f,-79),"coast"),(new Vector3(110,105,-115),new Vector3(0,0,0),"overview")}){var rt=new RenderTexture(1600,900,24);var t=new Texture2D(1600,900,TextureFormat.RGB24,false);try{c.transform.position=shot.Item1;c.transform.LookAt(shot.Item2);c.targetTexture=rt;c.Render();RenderTexture.active=rt;t.ReadPixels(new Rect(0,0,1600,900),0,0);t.Apply();File.WriteAllBytes("Logs/HarborBoundary-"+shot.Item3+".png",t.EncodeToPNG());}finally{c.targetTexture=target;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(t);UnityEngine.Object.DestroyImmediate(rt);}}c.transform.SetPositionAndRotation(pos,rot);}
 }
}
#endif

