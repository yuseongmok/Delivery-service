#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using UnityEngine;using UnityEngine.AI;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad] public static class EastRoadworks {
static Scene scene;
static EastRoadworks(){EditorApplication.update+=Poll;}
static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/Mok-east-roadworks"))return;File.Delete("Temp/Mok-east-roadworks");try{Run();}catch(Exception e){File.WriteAllText("Temp/Mok-east-roadworks.txt",e.ToString());Debug.LogException(e);}}
static GameObject Model(Transform parent,string asset,Vector3 position,float yaw,Vector3 limit){
var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonCity/Prefabs/"+asset+".prefab");if(prefab==null)throw new Exception(asset);
var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);g.transform.SetParent(parent,false);g.transform.SetPositionAndRotation(Vector3.zero,Quaternion.Euler(0,yaw,0));
foreach(var c in g.GetComponentsInChildren<Collider>(true)){c.enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(c);}
foreach(var rb in g.GetComponentsInChildren<Rigidbody>(true)){rb.isKinematic=true;rb.detectCollisions=false;PrefabUtility.RecordPrefabInstancePropertyModifications(rb);}
foreach(var behaviour in g.GetComponentsInChildren<MonoBehaviour>(true)){behaviour.enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(behaviour);}
Bounds Bounds(){var rr=g.GetComponentsInChildren<Renderer>();var b=rr[0].bounds;foreach(var r in rr)b.Encapsulate(r.bounds);return b;}
var bounds=Bounds();float scale=Mathf.Min(limit.x/bounds.size.x,limit.y/bounds.size.y,limit.z/bounds.size.z);g.transform.localScale*=scale;bounds=Bounds();g.transform.position=position-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);PrefabUtility.RecordPrefabInstancePropertyModifications(g.transform);return g;
}
static void Block(Transform parent,Vector3 center,Vector3 size){var g=new GameObject("Static obstacle collision");g.transform.SetParent(parent,false);g.transform.position=center;g.AddComponent<BoxCollider>().size=size;var nav=g.AddComponent<NavMeshObstacle>();nav.shape=NavMeshObstacleShape.Box;nav.size=size;nav.carving=true;nav.carveOnlyStationary=true;}
static void Run(){var old=SceneManager.GetActiveScene();for(int i=0;i<SceneManager.sceneCount;i++){var s=SceneManager.GetSceneAt(i);if(s.isDirty&&!EditorSceneManager.SaveScene(s,"Temp/"+s.name+"-before-roadworks.unity",true))throw new Exception("Backup failed");}
scene=SceneManager.GetSceneByPath("Assets/Scenes/JJinCity.unity");bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene("Assets/Scenes/JJinCity.unity",OpenSceneMode.Additive);SceneManager.SetActiveScene(scene);
try{if(!EditorSceneManager.SaveScene(scene,"Temp/JJinCity-before-roadworks.unity",true))throw new Exception("Backup failed");var east=scene.GetRootGameObjects().First(g=>g.name=="Mok East City").transform;if(east.Find("Roadworks and parking")!=null)throw new Exception("Already installed");var root=new GameObject("Roadworks and parking").transform;root.SetParent(east,false);int i=0;
foreach(float x in new[]{210f,290f,370f,440f})foreach(float z in new[]{-65f,40f}){
var site=new GameObject("Roadwork "+(++i)).transform;site.SetParent(root,false);site.position=new Vector3(x,0,z);
// 방호벽과 내부 자재를 하나의 충돌체로 묶어 자잘한 모서리 충돌을 줄인다.
Block(site,new Vector3(x,.65f,z),new Vector3(1.8f,1.3f,14));
foreach(float dz in new[]{-6.5f,6.5f})Model(site,"Props/SM_Prop_Barrier_01",new Vector3(x,.05f,z+dz),0,new Vector3(1.8f,1.2f,.5f));
foreach(float dz in new[]{-4f,0f,4f})foreach(float side in new[]{-1f,1f})Model(site,"Props/SM_Prop_Barrier_01",new Vector3(x+side*.75f,.05f,z+dz),90,new Vector3(.35f,1.1f,3.6f));
foreach(float dz in new[]{-3f,2f})Model(site,"Props/SM_Prop_Pallet_01",new Vector3(x,.08f,z+dz),0,new Vector3(1.3f,1f,2));
foreach(float dz in new[]{-7.7f,7.7f})Model(site,"Props/SM_Prop_Cone_01",new Vector3(x,.05f,z+dz),0,new Vector3(.6f,.85f,.6f));
var sign=new GameObject("공사 중 표지");sign.transform.SetParent(site,false);sign.transform.position=new Vector3(x,1.6f,z-6.5f);var tm=sign.AddComponent<TextMesh>();tm.text="공사 중";tm.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/BMJUA_ttf.ttf");tm.fontSize=48;tm.characterSize=.045f;tm.anchor=TextAnchor.MiddleCenter;tm.color=new Color(1,.65f,.15f);sign.GetComponent<MeshRenderer>().sharedMaterial=tm.font.material;
foreach(float side in new[]{-1f,1f})foreach(float dz in new[]{-3.2f,3.2f}){
var holder=new GameObject("Parked vehicle").transform;holder.SetParent(site,false);
var names=new[]{"Small","Sedan","Medium","Van"};string name=names[(i+(side>0?1:0)+(dz>0?2:0))%4];
var car=Model(holder,"Vehicles/SM_Veh_Car_"+name+"_01",new Vector3(x+side*5.35f,.05f,z+dz),side>0?180:0,new Vector3(1.9f,2.3f,4.8f));
var rr=car.GetComponentsInChildren<Renderer>();var bounds=rr[0].bounds;foreach(var r in rr)bounds.Encapsulate(r.bounds);Block(holder,bounds.center,new Vector3(bounds.size.x*.94f,bounds.size.y*.9f,bounds.size.z*.96f));
}
}
Physics.SyncTransforms();var routes=east.GetComponentsInChildren<WayPoint>();foreach(var node in routes)foreach(var next in node.nextWaypoints){var delta=next.transform.position-node.transform.position;foreach(var hit in Physics.BoxCastAll(node.transform.position+Vector3.up*1.1f,new Vector3(.9f,.5f,.9f),delta.normalized,Quaternion.identity,delta.magnitude,~0,QueryTriggerInteraction.Ignore))if(hit.transform.IsChildOf(root))throw new Exception("Traffic blocked: "+node.name+" / "+hit.transform.parent.name);}
EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);File.WriteAllText("Temp/Mok-east-roadworks.txt","PASS: 8 work zones, 32 parked vehicles, 40 static colliders/carving obstacles; all existing traffic waypoint segments clear; scene saved.");
}finally{SceneManager.SetActiveScene(old);if(opened)EditorSceneManager.CloseScene(scene,true);}
}
}
}
#endif
