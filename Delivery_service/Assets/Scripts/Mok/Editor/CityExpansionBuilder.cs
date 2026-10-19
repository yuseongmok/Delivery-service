#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Text;
using UnityEngine;using UnityEngine.AI;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;using Unity.AI.Navigation;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad] public static class CityExpansionBuilder {
const string Dir="Assets/Scripts/Mok/Art/EastCity";static Transform root;static Scene scene;static Material asphalt,stone,grass,line,rail;static readonly List<NavMeshBuildSource> sources=new();static readonly List<(Bounds,int)> map=new();static StringBuilder log;
static CityExpansionBuilder(){EditorApplication.update+=Poll;}
static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/Mok-city-build"))return;File.Delete("Temp/Mok-city-build");try{Build();}catch(Exception e){File.WriteAllText("Temp/Mok-city-build.txt",e.ToString());Debug.LogException(e);}}
static Material Mat(string name,Color color){var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,color=color};m.SetFloat("_Smoothness",.08f);return SaveAsset(m,Dir+"/"+name+".mat");}
static GameObject Box(string name,Vector3 center,Vector3 size,Material mat,bool solid=true,int area=-1,int mapType=-1){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(root,false);g.transform.position=center;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=mat;g.isStatic=true;if(!solid)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());if(area>=0)sources.Add(new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Box,transform=Matrix4x4.TRS(center,Quaternion.identity,Vector3.one),size=size,area=area});if(mapType>=0)map.Add((new Bounds(center,size),mapType));return g;}
static T SaveAsset<T>(T asset,string path) where T:UnityEngine.Object {var existing=AssetDatabase.LoadAssetAtPath<T>(path);if(existing==null){AssetDatabase.CreateAsset(asset,path);return asset;}EditorUtility.CopySerialized(asset,existing);UnityEngine.Object.DestroyImmediate(asset);return existing;} static Bounds BoundsOf(GameObject g){var rr=g.GetComponentsInChildren<Renderer>();var b=rr[0].bounds;foreach(var r in rr)b.Encapsulate(r.bounds);return b;}
static GameObject Asset(string folder,string name,Vector3 center,float yaw=0,float maxWidth=0,float maxDepth=0){var p=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonCity/Prefabs/"+folder+"/"+name+".prefab");if(p==null)throw new Exception("Missing "+name);var g=(GameObject)PrefabUtility.InstantiatePrefab(p,scene);g.transform.SetParent(root,true);g.transform.SetPositionAndRotation(Vector3.zero,Quaternion.Euler(0,yaw,0));var b=BoundsOf(g);if(maxWidth>0){float scale=Mathf.Min(1,maxWidth/b.size.x,maxDepth/b.size.z);g.transform.localScale*=scale;b=BoundsOf(g);}g.transform.position+=center-new Vector3(b.center.x,b.min.y,b.center.z);foreach(var t in g.GetComponentsInChildren<Transform>()){t.gameObject.isStatic=true;t.gameObject.layer=0;}PrefabUtility.RecordPrefabInstancePropertyModifications(g.transform);return g;}
static void Build(){
string stamp=DateTime.Now.ToString("yyyyMMdd-HHmmss");for(int i=0;i<SceneManager.sceneCount;i++)if(!EditorSceneManager.SaveScene(SceneManager.GetSceneAt(i),"Temp/Before-city-"+stamp+"-"+i+".unity",true))throw new Exception("Backup failed");
scene=SceneManager.GetSceneByPath("Assets/Scenes/JJinCity.unity");bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene("Assets/Scenes/JJinCity.unity",OpenSceneMode.Additive);var previous=SceneManager.GetActiveScene();SceneManager.SetActiveScene(scene);
try{
if(scene.GetRootGameObjects().Any(g=>g.name=="Mok East City"))throw new Exception("City already exists; preserve scene edits");if(!EditorSceneManager.SaveScene(scene,"Temp/JJinCity-before-city-"+stamp+".unity",true))throw new Exception("Backup failed");Directory.CreateDirectory(Dir);AssetDatabase.Refresh();log=new StringBuilder();sources.Clear();map.Clear();root=new GameObject("Mok East City").transform;
asphalt=Mat("Asphalt",new Color(.19f,.22f,.23f));stone=Mat("Pavement",new Color(.48f,.49f,.46f));grass=Mat("Park grass",new Color(.26f,.35f,.20f));line=Mat("Road paint",new Color(.8f,.75f,.55f));rail=Mat("Quay stone",new Color(.29f,.32f,.33f));int road=NavMesh.GetAreaFromName("Road"),walk=NavMesh.GetAreaFromName("SideWalk");if(road<0||walk<0)throw new Exception("Navigation areas missing");
// 기존 다리 끝 x=195, z=-5에서 새 도시로 자연스럽게 이어진다.
Box("District foundation",new Vector3(325,-2.17f,-5),new Vector3(260,4,250),rail,true,-1,0);
var support=new GameObject("Continuous road collision");support.transform.SetParent(root,false);support.transform.position=new Vector3(325,-.08f,-5);support.AddComponent<BoxCollider>().size=new Vector3(260,.16f,250);support.isStatic=true;float[] xs={210,290,370,440};float[] zs={-110,-5,100};
foreach(float x in xs)Box("North south avenue",new Vector3(x,-.08f,-5),new Vector3(12,.16f,230),asphalt,false,road,1);
foreach(float z in zs)Box("East west avenue",new Vector3(325,-.08f,z),new Vector3(260,.16f,12),asphalt,false,road,1);
// 교차로 밖 차선만 표시한다.
foreach(float x in xs)for(float z=-115;z<106;z+=7)if(zs.All(v=>Mathf.Abs(z-v)>10))Box("Lane dash",new Vector3(x,.012f,z),new Vector3(.12f,.015f,3),line,false);
foreach(float z in zs)for(float x=200;x<450;x+=7)if(xs.All(v=>Mathf.Abs(x-v)>10))Box("Lane dash",new Vector3(x,.014f,z),new Vector3(3,.015f,.12f),line,false);
string[] buildings={"SM_Bld_OfficeOld_Small_01","SM_Bld_OfficeSquare_01","SM_Bld_OfficeOld_Large_01","SM_Bld_OfficeRound_01","SM_Bld_OfficeSquare_03","SM_Bld_OfficeOld_Small_02"};int count=0;
for(int ix=0;ix<3;ix++)for(int iz=0;iz<2;iz++){
float left=xs[ix]+6,right=xs[ix+1]-6,low=zs[iz]+6,high=zs[iz+1]-6;Box("Block pavement",new Vector3((left+right)/2,-.03f,(low+high)/2),new Vector3(right-left,.16f,high-low),stone,true,walk);
for(int a=0;a<2;a++)for(int b=0;b<2;b++){
float x=Mathf.Lerp(left,right,a==0?.27f:.73f),z=Mathf.Lerp(low,high,b==0?.24f:.76f);
var g=Asset("Buildings",buildings[count%buildings.Length],new Vector3(x,.05f,z),(count%4)*90, (right-left)*.37f,30);g.name="East Block "+(ix+1)+(iz+1)+" Building "+(++count);var bounds=BoundsOf(g);map.Add((bounds,2));
// 복잡한 외벽에 걸리지 않도록 건물 외곽에 충돌체를 둔다.
var block=new GameObject("Building collision");block.transform.SetParent(root,false);block.transform.position=bounds.center;block.AddComponent<BoxCollider>().size=bounds.size;block.isStatic=true;sources.Add(new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Box,transform=Matrix4x4.TRS(bounds.center,Quaternion.identity,Vector3.one),size=bounds.size,area=1});
var marker=new GameObject("Delivery entrance "+count.ToString("00"));marker.transform.SetParent(root,false);marker.transform.position=new Vector3(x,.12f,b==0?low+1.8f:high-1.8f);
}
float middle=(low+high)/2;Box("Courtyard lawn",new Vector3((left+right)/2,.065f,middle),new Vector3(right-left-8,.03f,12),grass,false);
for(int k=0;k<3;k++)Asset("Environments","SM_Env_Tree_0"+(1+k%2),new Vector3(left+9+k*(right-left-18)/2,.07f,middle),k*73);
Asset("Props","SM_Prop_ParkBench_01",new Vector3((left+right)/2,.06f,middle+8),180);
}
// 해안 보행로와 낮은 난간. 다리 연결 부분은 비워 둔다.
Box("North promenade",new Vector3(325,-.04f,116),new Vector3(260,.16f,8),stone,true,walk);
Box("South promenade",new Vector3(325,-.04f,-126),new Vector3(260,.16f,8),stone,true,walk);
Box("East promenade",new Vector3(451,-.04f,-5),new Vector3(8,.16f,250),stone,true,walk);
Box("West promenade north",new Vector3(199,-.04f,60),new Vector3(8,.16f,120),stone,true,walk);Box("West promenade south",new Vector3(199,-.04f,-70),new Vector3(8,.16f,120),stone,true,walk);
Box("North sea wall",new Vector3(325,.48f,120),new Vector3(260,1,1),rail);Box("South sea wall",new Vector3(325,.48f,-130),new Vector3(260,1,1),rail);Box("East sea wall",new Vector3(455,.48f,-5),new Vector3(1,1,250),rail);
foreach(int side in new[]{-1,1})Box("West sea wall",new Vector3(195,.48f,-5+side*68),new Vector3(1,1,114),rail);
// 배경용 원경 건물이 새 도로에 겹치는 경우에만 원본을 보존한 채 숨긴다.
foreach(var r in scene.GetRootGameObjects().Where(g=>g!=root.gameObject).SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>()).Where(r=>r.name.Contains("Background")&&r.bounds.Intersects(new Bounds(new Vector3(325,25,-5),new Vector3(260,50,250))))){r.gameObject.SetActive(false);PrefabUtility.RecordPrefabInstancePropertyModifications(r.gameObject);log.AppendLine("Hidden background: "+r.name);}
// 가로등은 기존 조립 모델을 복제하되 동작 스크립트는 복제하지 않는다.
var lamp=scene.GetRootGameObjects().Where(g=>g!=root.gameObject).SelectMany(g=>g.GetComponentsInChildren<Transform>()).First(t=>t.name=="SM_Prop_LightPole_Base_01");
foreach(float z in zs)for(float x=225;x<440;x+=40){var g=UnityEngine.Object.Instantiate(lamp.gameObject,root);g.name="East street lamp";g.transform.position=new Vector3(x,0,z+7.5f);foreach(var mb in g.GetComponentsInChildren<MonoBehaviour>())UnityEngine.Object.DestroyImmediate(mb);}
BuildMap();BuildNavigation(road);
AssetDatabase.SaveAssets();Physics.SyncTransforms();int samples=0;for(float x=180;x<=440;x+=2){var hits=Physics.RaycastAll(new Vector3(x,2,-5),Vector3.down,4,~(1<<20),QueryTriggerInteraction.Ignore).Where(h=>h.collider.gameObject.scene==scene).OrderBy(h=>h.distance).ToArray();var hit=hits.FirstOrDefault();if(hits.Length==0||Mathf.Abs(hit.point.y)>.2f)log.AppendLine("CHECK height at "+x+" "+string.Join(";",hits.Select(h=>h.collider.name+" y="+h.point.y+" layer="+h.collider.gameObject.layer)));samples++;}log.AppendLine("PASS bridge ground samples: "+samples);log.AppendLine("Buildings: "+count+"; district 260 x 250; 6 blocks; 24 entrance markers");
EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Save failed");Capture();File.WriteAllText("Temp/Mok-city-build.txt",log.ToString());
}finally{SceneManager.SetActiveScene(previous);if(opened)EditorSceneManager.CloseScene(scene,true);}}
static void BuildMap(){var v=new List<Vector3>();var tr=new[]{new List<int>(),new List<int>(),new List<int>()};foreach(var entry in map){var b=entry.Item1;int m=entry.Item2,n=v.Count;float h=-.15f+m*.15f;v.AddRange(new[]{new Vector3(b.min.x,h,b.min.z),new Vector3(b.min.x,h,b.max.z),new Vector3(b.max.x,h,b.max.z),new Vector3(b.max.x,h,b.min.z)});tr[m].AddRange(new[]{n,n+1,n+2,n,n+2,n+3});}var mesh=new Mesh{name="East city navigation"};mesh.SetVertices(v);mesh.subMeshCount=3;for(int i=0;i<3;i++)mesh.SetTriangles(tr[i],i);mesh.RecalculateBounds();mesh=SaveAsset(mesh,Dir+"/Map.asset");var g=new GameObject("East city flat navigation map");g.transform.SetParent(root,false);g.layer=20;g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterials=new[]{"Ground","Roads","Buildings"}.Select(n=>AssetDatabase.LoadAssetAtPath<Material>("Assets/Scripts/Mok/Art/NavigationMap/"+n+".mat")).ToArray();}
static void BuildNavigation(int road){var settings=NavMesh.GetSettingsByIndex(0);var data=NavMeshBuilder.BuildNavMeshData(settings,sources,new Bounds(new Vector3(325,35,-5),new Vector3(262,80,252)),Vector3.zero,Quaternion.identity);data=SaveAsset(data,Dir+"/Navigation.asset");var go=new GameObject("East city navigation surface");go.transform.SetParent(root,false);var surface=go.AddComponent<NavMeshSurface>();surface.agentTypeID=settings.agentTypeID;surface.navMeshData=data;surface.AddData();var link=go.AddComponent<NavMeshLink>();link.startPoint=new Vector3(190,.1f,-5);link.endPoint=new Vector3(201,.1f,-5);link.width=8;link.bidirectional=true;link.area=road;link.agentTypeID=settings.agentTypeID;link.UpdateLink();}
static void Capture(){var others=Enumerable.Range(0,SceneManager.sceneCount).Select(SceneManager.GetSceneAt).Where(s=>s!=scene).SelectMany(s=>s.GetRootGameObjects()).Where(g=>g.activeSelf).ToArray();foreach(var o in others)o.SetActive(false);var g=new GameObject("City preview camera");var camera=g.AddComponent<Camera>();camera.cullingMask=~((1<<20)|(1<<17));camera.farClipPlane=1600;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.35f,.49f,.58f);g.transform.position=new Vector3(130,250,-280);g.transform.LookAt(new Vector3(290,0,-5));camera.fieldOfView=56;var rt=new RenderTexture(1400,900,24);camera.targetTexture=rt;camera.Render();var prev=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(1400,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1400,900),0,0);tex.Apply();File.WriteAllBytes("Temp/Mok-city-overview.png",tex.EncodeToPNG());RenderTexture.active=prev;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(g);foreach(var o in others)o.SetActive(true);}
}}
#endif




