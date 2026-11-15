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
 [InitializeOnLoad] internal static class HarborTunnelLandscape
 {
  static HarborTunnelLandscape(){EditorApplication.update+=Poll;}
  static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlaying||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/HarborTunnelLandscape"))return;File.Delete("Temp/HarborTunnelLandscape");try{Build();}catch(Exception e){File.WriteAllText("Logs/HarborTunnelLandscape-error.txt",e.ToString());Debug.LogException(e);}}
  static void Build()
  {
   var scene=SceneManager.GetActiveScene();if(!scene.path.EndsWith("NewHarborCity.unity"))return;
   EditorSceneManager.SaveScene(scene,"Logs/HarborTunnelLandscape-before-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
   DistrictArt.Scene=scene;var city=scene.GetRootGameObjects().First(g=>g.name.StartsWith("NEW HARBOR")).transform;
   var old=city.Find("10 터널 해안 암반 지형");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
   DistrictArt.Parent=city;DistrictArt.Parent=DistrictArt.Group("10 터널 해안 암반 지형");
   // 터널 내부 공간과 남쪽 산책로를 침범하지 않는 독립된 암반층.
   RockEnvelope();
   // 산책로 바깥 물가의 낮은 바위가 해안과 암반의 재질을 이어준다.
   foreach(float x in new[]{-56f,-64f,-73f,-81f})Fit("SM_Env_WaterEdge_Rock_01",new Vector3(x,-.65f,-81.7f),new Vector3(6,1.5f,3.8f),x*2);
   Physics.SyncTransforms();var report=new System.Text.StringBuilder();
   int blocks=0;for(float x=-52;x>=-80;x-=.5f)
   {
    var hits=Physics.OverlapCapsule(new Vector3(x,.42f,-76.8f),new Vector3(x,1.62f,-76.8f),.28f,~0,QueryTriggerInteraction.Ignore);
    if(hits.Length>0){blocks++;report.AppendLine("BLOCK 산책로 "+x+" "+string.Join(",",hits.Select(h=>h.name)));}
    if(x>=-67){hits=Physics.OverlapBox(new Vector3(x,.85f,-70),new Vector3(1.9f,.65f,.9f),Quaternion.identity,~0,QueryTriggerInteraction.Ignore);if(hits.Length>0){blocks++;report.AppendLine("BLOCK 터널 "+x+" "+string.Join(",",hits.Select(h=>h.name)));}}
   }
   report.AppendLine(blocks==0?"PASS 산책로와 터널 차량 통과 공간":"FAIL 충돌 확인 필요");
   var travel=city.GetComponentsInChildren<HarborTunnelTravel>().Single();report.AppendLine("이동 트리거 유지: "+travel.transform.position);
   File.WriteAllText("Logs/HarborTunnelLandscape-validation.txt",report.ToString());
   EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();File.WriteAllText("Temp/HarborFinaleAudit","audit");
  }
  static void RockEnvelope()
  {
   var vertices=new System.Collections.Generic.List<Vector3>();var triangles=new System.Collections.Generic.List<int>();
   float[] xs={-53.2f,-57,-62,-68,-74,-81};float[] heights={7.1f,9.4f,10.5f,11.2f,9.6f,7.7f};
   Vector3 Point(int row,int column,bool inner){float a=column*Mathf.PI/12;float wave=column==0||column==12?0:Mathf.Sin(row*2.3f+column*1.7f)*.22f;return new Vector3(xs[row],inner?2+4.35f*Mathf.Sin(a):heights[row]*Mathf.Sin(a)+wave,-70+(inner?5.62f:6.12f)*Mathf.Cos(a));}
   void Triangle(Vector3 a,Vector3 b,Vector3 c){int n=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);triangles.AddRange(new[]{n,n+1,n+2});}
   void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d){Triangle(a,b,c);Triangle(a,c,d);}
   for(int row=0;row<xs.Length-1;row++)for(int col=0;col<12;col++){
    Quad(Point(row,col,false),Point(row,col+1,false),Point(row+1,col+1,false),Point(row+1,col,false));
    Quad(Point(row+1,col,true),Point(row+1,col+1,true),Point(row,col+1,true),Point(row,col,true));
   }
   foreach(int row in new[]{0,xs.Length-1})for(int col=0;col<12;col++){
    if(row==0)Quad(Point(row,col,true),Point(row,col+1,true),Point(row,col+1,false),Point(row,col,false));
    else Quad(Point(row,col,false),Point(row,col+1,false),Point(row,col+1,true),Point(row,col,true));
   }
   foreach(int col in new[]{0,12})for(int row=0;row<xs.Length-1;row++)Quad(Point(row,col,false),Point(row+1,col,false),Point(row+1,col,true),Point(row,col,true));
   var mesh=new Mesh{name="해안 암반 능선"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
   string path=DistrictArt.Folder+"/TunnelRockEnvelope.asset";var previous=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(previous){previous.Clear();EditorUtility.CopySerialized(mesh,previous);previous.UploadMeshData(false);EditorUtility.SetDirty(previous);UnityEngine.Object.DestroyImmediate(mesh);mesh=previous;}else AssetDatabase.CreateAsset(mesh,path);
   var g=new GameObject("터널을 감싸는 로우폴리 암반 능선");g.transform.SetParent(DistrictArt.Parent,false);g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=DistrictArt.Mat("Coastal rock earth",new Color(.34f,.32f,.27f));g.AddComponent<MeshCollider>().sharedMesh=mesh;g.isStatic=true;
  }
  static void Fit(string prefab,Vector3 p,Vector3 size,float yaw)
  {
   var g=DistrictArt.Prop(prefab,p,0,0);var b=DistrictArt.Bounds(g);
   // 비균등 크기는 월드 축에 맞춘 뒤 회전을 생략해 보행 공간의 정확한 경계를 보존한다.
   g.transform.localScale=Vector3.Scale(g.transform.localScale,new Vector3(size.x/b.size.x,size.y/b.size.y,size.z/b.size.z));
   b=DistrictArt.Bounds(g);g.transform.position+=p-new Vector3(b.center.x,b.min.y,b.center.z);
   foreach(var mf in g.GetComponentsInChildren<MeshFilter>())if(mf.sharedMesh){var c=mf.gameObject.AddComponent<MeshCollider>();c.sharedMesh=mf.sharedMesh;}
   PrefabUtility.RecordPrefabInstancePropertyModifications(g.transform);
  }
 }
}
#endif


