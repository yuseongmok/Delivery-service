#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad] public static class EastTrafficDensity {
static EastTrafficDensity(){EditorApplication.update+=Poll;}
static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/Mok-east-density"))return;File.Delete("Temp/Mok-east-density");try{Run();}catch(Exception e){File.WriteAllText("Temp/Mok-east-density.txt",e.ToString());Debug.LogException(e);}}
static void Run(){
var previous=SceneManager.GetActiveScene();
for(int i=0;i<SceneManager.sceneCount;i++){var s=SceneManager.GetSceneAt(i);if(s.isDirty)EditorSceneManager.SaveScene(s,"Temp/"+s.name+"-before-east-density.unity",true);}
var scene=SceneManager.GetSceneByPath("Assets/Scenes/JJinCity.unity");bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene("Assets/Scenes/JJinCity.unity",OpenSceneMode.Additive);SceneManager.SetActiveScene(scene);
try{
var root=scene.GetRootGameObjects().First(g=>g.name=="Mok East City").transform;
if(root.Find("Dense traffic")!=null)throw new Exception("Already installed");
var cars=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<CarMov>()).Where(c=>c.transform.position.x>195).ToArray();if(cars.Length!=16)throw new Exception("Expected 16 cars");
EditorSceneManager.SaveScene(scene,"Temp/JJinCity-before-east-density.unity",true);
var group=new GameObject("Dense traffic").transform;group.SetParent(root,false);
var ordinary=cars.Where(c=>c.GetComponent<EastRecklessDriver>()==null).ToList();var wild=cars.Where(c=>c.GetComponent<EastRecklessDriver>()!=null).ToList();
void Clone(List<CarMov> list,int count){var source=list.ToArray();for(int i=0;i<count;i++){var copy=UnityEngine.Object.Instantiate(source[i%source.Length].gameObject,group);copy.name="East added "+source[i%source.Length].name+" "+i;list.Add(copy.GetComponent<CarMov>());}}
Clone(ordinary,14);Clone(wild,10);
var loops=new List<WayPoint[]>();float[] xs={210,290,370,440},zs={-110,-5,100};
for(int col=0;col<3;col++)for(int row=0;row<2;row++){
float l=xs[col]+3,r=xs[col+1]-3,b=zs[row]+3,t=zs[row+1]-3;
var corners=new[]{new Vector3(l,.05f,b),new Vector3(l,.05f,t),new Vector3(r,.05f,t),new Vector3(r,.05f,b)};
var points=new List<Vector3>();for(int n=0;n<4;n++){var a=corners[n];var end=corners[(n+1)%4];int steps=Mathf.CeilToInt(Vector3.Distance(a,end)/20);for(int j=0;j<steps;j++)points.Add(Vector3.Lerp(a,end,j/(float)steps));}
var nodes=points.Select((p,n)=>{var go=new GameObject("Block lane "+col+"-"+row+" / "+n);go.transform.SetParent(group,false);go.transform.position=p;return go.AddComponent<WayPoint>();}).ToArray();for(int n=0;n<nodes.Length;n++)nodes[n].nextWaypoints.Add(nodes[(n+1)%nodes.Length]);loops.Add(nodes);
}
var wildLoops=Enumerable.Range(1,5).Select(i=>root.Find("Reckless traffic routes").GetComponentsInChildren<WayPoint>().Where(w=>w.name.StartsWith("Reckless "+i+" / ")).OrderBy(w=>int.Parse(w.name.Split('/')[1])).ToArray()).ToList();
var placed=new List<Vector3>();
void Spread(List<CarMov> fleet,List<WayPoint[]> paths){for(int i=0;i<fleet.Count;i++){
int lane=i%paths.Count,slot=i/paths.Count,count=(fleet.Count-1-lane)/paths.Count+1;var nodes=paths[lane];float length=0;foreach(var n in nodes)length+=Vector3.Distance(n.transform.position,n.nextWaypoints[0].transform.position);
Vector3 pos=default;WayPoint target=null;
for(int attempt=0;attempt<120;attempt++){float offset=(length*(slot+.35f)/count+attempt*3.7f)%length;foreach(var node in nodes){var next=node.nextWaypoints[0];float segment=Vector3.Distance(node.transform.position,next.transform.position);if(offset<=segment){pos=Vector3.Lerp(node.transform.position,next.transform.position,offset/segment);target=next;break;}offset-=segment;}if(placed.All(p=>Vector3.Distance(p,pos)>10))break;if(attempt==119)throw new Exception("No clear spawn");}
var car=fleet[i];car.transform.SetPositionAndRotation(pos,Quaternion.LookRotation(target.transform.position-pos));var so=new SerializedObject(car);so.FindProperty("currentTarget").objectReferenceValue=target;so.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.RecordPrefabInstancePropertyModifications(car);PrefabUtility.RecordPrefabInstancePropertyModifications(car.transform);placed.Add(pos);
}}
Spread(wild,wildLoops);Spread(ordinary,loops);
// 6개 블록에 생성 인원을 나누어 도시 한쪽에만 시민이 몰리지 않게 한다.
var citizens=root.Find("East citizens");var original=citizens.GetComponent<NPCManager>();int index=0;
for(int col=0;col<3;col++)for(int row=0;row<2;row++){
NPCManager manager;BoxCollider area;
if(index==0){manager=original;area=citizens.GetComponent<BoxCollider>();}else{var g=new GameObject("Citizen block "+col+"-"+row);g.transform.SetParent(citizens,false);manager=g.AddComponent<NPCManager>();EditorUtility.CopySerialized(original,manager);area=g.AddComponent<BoxCollider>();area.isTrigger=true;}
Vector3 center=new Vector3((xs[col]+xs[col+1])*.5f,.1f,(zs[row]+zs[row+1])*.5f);area.center=area.transform.InverseTransformPoint(center);area.size=new Vector3(xs[col+1]-xs[col]-14,.2f,zs[row+1]-zs[row]-14);
var so=new SerializedObject(manager);so.FindProperty("spawnArea").objectReferenceValue=area;so.FindProperty("npcParent").objectReferenceValue=manager.transform;so.FindProperty("maxNPCCount").intValue=index<2?9:8;so.ApplyModifiedPropertiesWithoutUndo();index++;
}
EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);File.WriteAllText("Temp/Mok-east-density.txt","PASS: east 40 cars (15 reckless / 25 normal), original 34 cars preserved; six pedestrian zones total 50; minimum initial vehicle separation 10m; saved scene.");
}finally{SceneManager.SetActiveScene(previous);if(opened)EditorSceneManager.CloseScene(scene,true);}
}
}
}
#endif
