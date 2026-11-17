#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad] public static class PrisonFirstWorkSetup {
 static PrisonFirstWorkSetup(){EditorApplication.update+=Poll;}
 static void Poll(){if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||!File.Exists("Temp/PrisonFirstWorkSetup"))return;File.Delete("Temp/PrisonFirstWorkSetup");var scene=EditorSceneManager.GetActiveScene();if(scene.name!="PrisonChapter")return;
 EditorSceneManager.SaveScene(scene,"Logs/Prison-before-work-"+System.DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
 var intro=Object.FindFirstObjectByType<PrisonIntroduction>();var work=intro.firstWork;if(!work){var go=new GameObject("첫날 작업");work=go.AddComponent<PrisonFirstWork>();intro.firstWork=work;}
 foreach(Transform child in work.transform.Cast<Transform>().ToArray())Object.DestroyImmediate(child.gameObject);
 work.name="첫날 작업 · 판자 다섯 장";work.transform.SetPositionAndRotation(new Vector3(38.3f,0,32),Quaternion.identity);
 GameObject Prefab(string path,Transform parent){var p=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));p.transform.SetParent(parent,false);return p;}
 var table=Prefab("Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Table_01.prefab",work.transform);
 var bounds=table.GetComponentInChildren<Renderer>().bounds;table.transform.localScale=Vector3.Scale(table.transform.localScale,new Vector3(2.2f/bounds.size.x,.9f/bounds.size.y,1.3f/bounds.size.z));bounds=table.GetComponentInChildren<Renderer>().bounds;table.transform.position+=new Vector3(work.transform.position.x-bounds.center.x,-bounds.min.y,work.transform.position.z-bounds.center.z);
 Transform Anchor(string name,Vector3 p){var g=new GameObject(name);g.transform.SetParent(work.transform,false);g.transform.localPosition=p;return g.transform;}
 work.station=Anchor("판자 절단선 · 도끼 날 접촉점",new Vector3(0,.99f,0));work.approach=Anchor("작업대 앞 · 이동 목표",new Vector3(0,0,-1.4f));
 var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Plank_01.prefab");var filter=source.GetComponentInChildren<MeshFilter>();var material=source.GetComponentInChildren<MeshRenderer>().sharedMaterial;
 Mesh[] halves={Half(filter.sharedMesh,false),Half(filter.sharedMesh,true)};
 for(int h=0;h<2;h++){string path="Assets/DeliveryPrisonChapter/Introduction/작업 판자 절반 "+h+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old){EditorUtility.CopySerialized(halves[h],old);Object.DestroyImmediate(halves[h]);halves[h]=old;}else AssetDatabase.CreateAsset(halves[h],path);}
 work.boards=new GameObject[5];for(int b=0;b<5;b++){var board=Anchor("판자 "+(b+1),b==0?new Vector3(0,.95f,0):new Vector3(.65f,.95f+(b-1)*.09f,.32f));work.boards[b]=board.gameObject;for(int h=0;h<2;h++){var piece=new GameObject(h==0?"왼쪽 절반":"오른쪽 절반",typeof(MeshFilter),typeof(MeshRenderer));piece.transform.SetParent(board,false);piece.GetComponent<MeshFilter>().sharedMesh=halves[h];piece.GetComponent<MeshRenderer>().sharedMaterial=material;}}
 var axe=Prefab("Assets/Synty/PolygonGeneric/Prefabs/Weapons/SM_Gen_Wep_Axe_01.prefab",work.transform);work.axe=axe.transform;var mesh=axe.GetComponent<MeshFilter>().sharedMesh;var edge=mesh.vertices.Where(v=>v.x>mesh.bounds.max.x-.012f).ToArray();work.bladeContact=edge.Aggregate(Vector3.zero,(sum,v)=>sum+v)/edge.Length;
 axe.transform.localRotation=Quaternion.Euler(0,-90,0)*Quaternion.Euler(0,0,-90);
 axe.transform.position=work.station.position-axe.transform.TransformVector(work.bladeContact);
 foreach(var c in axe.GetComponentsInChildren<Collider>())Object.DestroyImmediate(c);
 intro.cellMingi.position=new Vector3(-9.1f,.02f,-25.4f);intro.cellMingi.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(intro.cellCamera.position-intro.cellMingi.position,Vector3.up));
 intro.cellCamera.LookAt(intro.cellMingi.position+Vector3.up*1.35f);
 intro.dialogue.workOrder="간수: 작업시간이다! 작업실로 이동해!";EditorUtility.SetDirty(intro.dialogue);
 EditorUtility.SetDirty(intro);EditorUtility.SetDirty(work);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);File.WriteAllText("Logs/Prison-first-work-setup.txt","Saved interior workstation (38.3,0,32), five boards, blade contact="+work.bladeContact+", cell anchor="+intro.cellMingi.position);
 }
 struct Vertex {public Vector3 p;public Vector2 uv;public Vertex(Vector3 p,Vector2 uv){this.p=p;this.uv=uv;}}
 static Mesh Half(Mesh source,bool right){
 var verts=new List<Vector3>();var uvs=new List<Vector2>();var indices=new List<int>();var src=source.vertices;var tex=source.uv;var bounds=source.bounds;
 Vector3 Normal(Vector3 p)=>Vector3.Scale(p-bounds.center,new Vector3(1.1f/bounds.size.x,.08f/bounds.size.y,.32f/bounds.size.z));
 var tri=source.triangles;for(int t=0;t<tri.Length;t+=3){var poly=new List<Vertex>();for(int k=0;k<3;k++){int n=tri[t+k];poly.Add(new Vertex(Normal(src[n]),tex[n]));}var output=new List<Vertex>();for(int k=0;k<poly.Count;k++){var a=poly[k];var b=poly[(k+1)%poly.Count];bool ai=right?a.p.x>=0:a.p.x<=0,bi=right?b.p.x>=0:b.p.x<=0;if(ai)output.Add(a);if(ai!=bi){float f=-a.p.x/(b.p.x-a.p.x);output.Add(new Vertex(Vector3.Lerp(a.p,b.p,f),Vector2.Lerp(a.uv,b.uv,f)));}}for(int k=1;k<output.Count-1;k++)foreach(var v in new[]{output[0],output[k],output[k+1]}){indices.Add(verts.Count);verts.Add(v.p);uvs.Add(v.uv);}}
 var mesh=new Mesh{name="Synty split plank"};mesh.SetVertices(verts);mesh.SetUVs(0,uvs);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
 }
}}
#endif

