#if UNITY_EDITOR
using System;using System.IO;using UnityEngine;using UnityEngine.AI;using UnityEditor;using UnityEditor.SceneManagement;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad]public static class SlumGuardSetup {
 static SlumGuardSetup(){EditorApplication.update+=Poll;}
 static void Poll(){if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||!File.Exists("Temp/Mok-slum-guard-install-request"))return;File.Delete("Temp/Mok-slum-guard-install-request");try{Install();}catch(Exception e){File.WriteAllText("Temp/Mok-slum-guard-install.txt",e.ToString());Debug.LogException(e);}}
 static void Install(){
  var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/JJinCity.unity")throw new Exception("JJinCity만 설치할 수 있습니다.");
  if(!File.Exists("Temp/JJinCity-before-slum-guards.unity")&&!EditorSceneManager.SaveScene(scene,"Temp/JJinCity-before-slum-guards.unity",true))throw new Exception("백업 실패");
  const string folder="Assets/Scripts/Mok/Models/SlumGuards";Directory.CreateDirectory(folder);AssetDatabase.Refresh();
  bool builtin=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline==null;
  var shader=Shader.Find(builtin?"Standard":"Universal Render Pipeline/Lit");
  var stone=new Material(shader);stone.color=new Color(.65f,.61f,.50f);stone.EnableKeyword("_EMISSION");stone.SetColor("_EmissionColor",new Color(.65f,.4f,.1f));
  AssetDatabase.CreateAsset(stone,folder+"/ThrowingStone.mat");
  var bright=new Material(Shader.Find(builtin?"Unlit/Color":"Universal Render Pipeline/Unlit"));bright.color=new Color(1,.65f,.1f);AssetDatabase.CreateAsset(bright,folder+"/StoneWarning.mat");
  var root=new GameObject("Slum Throwing Stone");var body=root.AddComponent<Rigidbody>();body.mass=.5f;body.useGravity=true;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;root.AddComponent<SphereCollider>().radius=.2f;
  var visual=GameObject.CreatePrimitive(PrimitiveType.Sphere);visual.name="Stone";visual.transform.SetParent(root.transform,false);visual.transform.localScale=new Vector3(.44f,.36f,.4f);UnityEngine.Object.DestroyImmediate(visual.GetComponent<Collider>());visual.GetComponent<Renderer>().sharedMaterial=stone;
  var mesh=StoneMesh();AssetDatabase.CreateAsset(mesh,folder+"/ThrowingStone.asset");visual.GetComponent<MeshFilter>().sharedMesh=mesh;
  var trail=root.AddComponent<TrailRenderer>();trail.time=.45f;trail.startWidth=.14f;trail.endWidth=.015f;trail.sharedMaterial=bright;trail.minVertexDistance=.08f;
  var warning=new GameObject("Landing warning");warning.transform.SetParent(root.transform,false);var ring=warning.AddComponent<LineRenderer>();ring.useWorldSpace=false;ring.loop=true;ring.positionCount=32;ring.startWidth=ring.endWidth=.065f;ring.sharedMaterial=bright;
  for(int i=0;i<32;i++){float a=i*Mathf.PI*2/32;ring.SetPosition(i,new Vector3(Mathf.Cos(a)*.65f,0,Mathf.Sin(a)*.65f));}warning.SetActive(false);
  root.AddComponent<SlumRock>().landingWarning=warning.transform;var prefab=PrefabUtility.SaveAsPrefabAsset(root,folder+"/ThrowingStone.prefab").GetComponent<SlumRock>();UnityEngine.Object.DestroyImmediate(root);
  var roster=UnityEngine.Object.FindFirstObjectByType<SlumGuardRoster>();if(roster==null)roster=new GameObject("Slum Fixed Guards").AddComponent<SlumGuardRoster>();
  var manager=UnityEngine.Object.FindFirstObjectByType<RobberyManager>();roster.robberPrefab=(GameObject)new SerializedObject(manager).FindProperty("robberPrefab").objectReferenceValue;roster.projectilePrefab=prefab;
  var district=UnityEngine.Object.FindFirstObjectByType<RobberyDistrict>();var offsets=new[]{new Vector3(51,0,-28),new Vector3(27,0,24),new Vector3(-5,0,-12),new Vector3(-43,0,28),new Vector3(-35,0,-38)};
  roster.posts=new Transform[5];string report="PASS: scene backed up, rock prefab saved\n";
  for(int i=0;i<5;i++){
   Vector3 desired=district.transform.TransformPoint(district.localCenter+offsets[i]);
   if(!NavMesh.SamplePosition(desired,out var hit,15,NavMesh.AllAreas)||!district.Contains(hit.position))throw new Exception("고정 배치 위치 탐색 실패 "+i+" "+desired);
   string name=i==4?"05 Rock Thrower Post":"0"+(i+1)+" Melee Post";var post=roster.transform.Find(name);if(post==null)post=new GameObject(name).transform;post.SetParent(roster.transform);post.position=hit.position;roster.posts[i]=post;report+=name+" "+hit.position+"\n";
  }
  EditorUtility.SetDirty(roster);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();File.WriteAllText("Temp/Mok-slum-guard-install.txt",report+"PASS: fixed five posts saved in JJinCity\n");
 }
 static Mesh StoneMesh(){
  var points=new Vector3[18];points[16]=new Vector3(.08f,.52f,.03f);points[17]=new Vector3(-.06f,-.47f,.04f);
  for(int i=0;i<8;i++){float a=i*Mathf.PI/4;float r=i%2==0?.48f:.56f;points[i]=new Vector3(Mathf.Cos(a)*r,.19f,Mathf.Sin(a)*r);points[i+8]=new Vector3(Mathf.Cos(a+.13f)*r,-.22f,Mathf.Sin(a+.13f)*r);}
  var vertices=new System.Collections.Generic.List<Vector3>();
  void Face(int a,int b,int c){vertices.Add(points[a]);vertices.Add(points[b]);vertices.Add(points[c]);}
  for(int i=0;i<8;i++){int j=(i+1)%8;Face(16,j,i);Face(i,j,j+8);Face(i,j+8,i+8);Face(17,i+8,j+8);}
  var mesh=new Mesh{name="Faceted throwing stone"};mesh.vertices=vertices.ToArray();var triangles=new int[vertices.Count];for(int i=0;i<triangles.Length;i++)triangles[i]=i;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
 }
}
}
#endif
