#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad]public static class RoadSurfaceFix {
 static RoadSurfaceFix(){EditorApplication.update+=Poll;}
 static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/Mok-road-surface-request"))return;File.Delete("Temp/Mok-road-surface-request");Run();}
 static void Run(){var old=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath("Assets/Scenes/JJinCity.unity");bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene("Assets/Scenes/JJinCity.unity",OpenSceneMode.Additive);SceneManager.SetActiveScene(scene);try{
 EditorSceneManager.SaveScene(scene,"Temp/JJinCity-before-road-surface.unity",true);
 var roots=scene.GetRootGameObjects();if(roots.Any(g=>g.name=="Mok Unified Road Surface"))throw new Exception("Road surface already installed");
 var all=roots.SelectMany(g=>g.GetComponentsInChildren<BoxCollider>(true)).Where(c=>c.enabled&&!c.isTrigger&&c.gameObject.activeInHierarchy&&c.name.StartsWith("SM_Env_Road_")&&Mathf.Abs(c.bounds.max.y)<.002f&&c.bounds.size.y<.3f).ToArray();
 if(all.Length<100)throw new Exception("Unexpected road collider count: "+all.Length);
 var vertices=new List<Vector3>();var triangles=new List<int>();var lookup=new Dictionary<Vector2Int,int>();
 int Vertex(float x,float z){var key=new Vector2Int(Mathf.RoundToInt(x*100),Mathf.RoundToInt(z*100));if(lookup.TryGetValue(key,out int index))return index;index=vertices.Count;lookup.Add(key,index);vertices.Add(new Vector3(key.x*.01f,0,key.y*.01f));return index;}
 foreach(var c in all){var b=c.bounds;int a=Vertex(b.min.x,b.min.z),d=Vertex(b.min.x,b.max.z),e=Vertex(b.max.x,b.max.z),f=Vertex(b.max.x,b.min.z);triangles.AddRange(new[]{a,d,e,a,e,f});}
 // 겹치는 타일 면도 제거하여 평평한 도로를 하나의 연속 충돌면으로 만든다.
 var seen=new HashSet<string>();var clean=new List<int>();for(int i=0;i<triangles.Count;i+=3){var ids=new[]{triangles[i],triangles[i+1],triangles[i+2]};string key=string.Join(",",ids.OrderBy(n=>n));if(seen.Add(key))clean.AddRange(ids);}
 var mesh=new Mesh{name="Unified city road collision"};mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;mesh.SetVertices(vertices);mesh.SetTriangles(clean,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
 const string path="Assets/Scripts/Mok/Art/UnifiedRoadCollision.asset";AssetDatabase.CreateAsset(mesh,path);
 var go=new GameObject("Mok Unified Road Surface");var collider=go.AddComponent<MeshCollider>();collider.sharedMesh=mesh;
 foreach(var c in all){c.enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(c);}
 EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();File.WriteAllText("Temp/Mok-road-surface-result.txt",$"PASS: {all.Length} tile colliders replaced by one welded, coplanar mesh ({vertices.Count} vertices). Backup includes current scene edits.");
 }catch(Exception e){File.WriteAllText("Temp/Mok-road-surface-result.txt",e.ToString());}finally{SceneManager.SetActiveScene(old);if(opened)EditorSceneManager.CloseScene(scene,true);}}
}}
#endif
