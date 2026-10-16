#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Text;
using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using UnityEngine.SceneManagement;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad] public static class LegacyArtMigration {
static readonly string[] Targets={"Assets/GIgaModel/Tray","Assets/GIgaModel/Signboard","Assets/GIgaModel/PizzaStore","Assets/GIgaModel/Pizza_ingredients"};
const string Folder="Assets/Scripts/Mok/Art/MigratedSupport";
static StringBuilder log;static Material[] lastMaterials;
static bool Legacy(string p)=>Targets.Any(t=>p==t||p.StartsWith(t+"/",StringComparison.Ordinal));
static LegacyArtMigration(){EditorApplication.update+=Poll;}
static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/Mok-migrate-legacy"))return;File.Delete("Temp/Mok-migrate-legacy");try{Run();}catch(Exception e){File.WriteAllText("Temp/Mok-migrate-legacy.txt",log+"\nFAIL: "+e);Debug.LogException(e);}}
static string Key(UnityEngine.Object o){AssetDatabase.TryGetGUIDAndLocalFileIdentifier(o,out string g,out long id);return g+"_"+id.ToString().Replace('-','n');}
static Mesh Collision(Mesh old){string path=Folder+"/Collision_"+Key(old)+".asset";var m=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(m==null){m=UnityEngine.Object.Instantiate(old);m.name="Preserved collision only";AssetDatabase.CreateAsset(m,path);}return m;}
static GameObject Prototype(string path){
string type=path.IndexOf("Dough",StringComparison.OrdinalIgnoreCase)>=0?"Dough":path.IndexOf("Cheese",StringComparison.OrdinalIgnoreCase)>=0?"Cheese":path.IndexOf("Tomato",StringComparison.OrdinalIgnoreCase)>=0?"Sauce":path.IndexOf("Chili",StringComparison.OrdinalIgnoreCase)>=0?"Pepper":path.IndexOf("Pepperoni",StringComparison.OrdinalIgnoreCase)>=0?"Pepperoni":path.IndexOf("Pineapple",StringComparison.OrdinalIgnoreCase)>=0?"Pineapple":path.IndexOf("Chocolate",StringComparison.OrdinalIgnoreCase)>=0?"Chocolate":"Jelly";
string name=path.Contains("/Tray/")?"Tray"+type:type=="Dough"?"PizzaStages":type+"Raw";
return Resources.Load<GameObject>("MokPizzaArt/"+name);
}
static Mesh Replacement(Mesh old){
string path=AssetDatabase.GetAssetPath(old);Mesh source;Material[] mats;
if(path.Contains("/PizzaStore/")){source=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Scripts/Mok/Art/ShopBuilding/ShopShell_LowPoly.asset");var r=UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(f=>f.sharedMesh==source);mats=r!=null?r.GetComponent<Renderer>().sharedMaterials:new[]{Resources.Load<Material>("MokPizzaArt/TraySteel")};}
else if(path.Contains("/Signboard/")){var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);source=cube.GetComponent<MeshFilter>().sharedMesh;UnityEngine.Object.DestroyImmediate(cube);mats=new[]{Resources.Load<Material>("MokPizzaArt/InkRed")};}
else{var prototype=Prototype(path);var f=prototype.GetComponentInChildren<MeshFilter>();source=f.sharedMesh;mats=f.GetComponent<Renderer>().sharedMaterials;}
if(source==null)throw new Exception("No replacement mesh for "+path);lastMaterials=mats;
string output=Folder+"/Replacement_"+Key(old)+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(output);if(mesh!=null)return mesh;
mesh=UnityEngine.Object.Instantiate(source);mesh.name="Migrated low-poly visual";
var from=source.bounds;var to=old.bounds;var verts=mesh.vertices;
for(int i=0;i<verts.Length;i++){var v=verts[i]-from.center;verts[i]=to.center+new Vector3(v.x*to.size.x/Mathf.Max(.0001f,from.size.x),v.y*to.size.y/Mathf.Max(.0001f,from.size.y),v.z*to.size.z/Mathf.Max(.0001f,from.size.z));}
mesh.vertices=verts;mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,output);return mesh;
}
static void Clean(GameObject root){
// Unpack only instances whose sources depend on retired assets, retaining script object references.
for(int iteration=0;iteration<30;iteration++){
var instance=root.GetComponentsInChildren<Transform>(true).Select(t=>PrefabUtility.GetOutermostPrefabInstanceRoot(t.gameObject)).Where(g=>g!=null).Distinct().FirstOrDefault(g=>AssetDatabase.GetDependencies(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(g),true).Any(Legacy));
if(instance==null)break;PrefabUtility.UnpackPrefabInstance(instance,PrefabUnpackMode.OutermostRoot,InteractionMode.AutomatedAction);
}
foreach(var c in root.GetComponentsInChildren<MeshCollider>(true))if(c.sharedMesh!=null&&Legacy(AssetDatabase.GetAssetPath(c.sharedMesh))){c.sharedMesh=Collision(c.sharedMesh);EditorUtility.SetDirty(c);}
foreach(var f in root.GetComponentsInChildren<MeshFilter>(true))if(f.sharedMesh!=null&&Legacy(AssetDatabase.GetAssetPath(f.sharedMesh))){f.sharedMesh=Replacement(f.sharedMesh);var r=f.GetComponent<MeshRenderer>();if(r!=null){r.sharedMaterials=lastMaterials;EditorUtility.SetDirty(r);}EditorUtility.SetDirty(f);}
foreach(var r in root.GetComponentsInChildren<Renderer>(true)){var mats=r.sharedMaterials;bool changed=false;for(int i=0;i<mats.Length;i++)if(mats[i]!=null&&Legacy(AssetDatabase.GetAssetPath(mats[i]))){mats[i]=Resources.Load<Material>("MokPizzaArt/TraySteel");changed=true;}if(changed){r.sharedMaterials=mats;EditorUtility.SetDirty(r);}}
}
[MenuItem("Tools/Delivery Service/Mok/Migrate Retired Art References")]
public static void Run(){
log=new StringBuilder();var current=SceneManager.GetActiveScene();if(current.path!="Assets/Scenes/JJinCity.unity")throw new Exception("Open JJinCity first");
if(!EditorSceneManager.SaveScene(current,"Temp/JJinCity-before-legacy-migration.unity",true))throw new Exception("Backup failed");
Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
var prefabs=AssetDatabase.FindAssets("t:Prefab").Select(AssetDatabase.GUIDToAssetPath).Where(p=>p.EndsWith(".prefab")&&!Legacy(p)&&AssetDatabase.GetDependencies(p,true).Any(Legacy)).ToArray();
foreach(var path in prefabs){var go=PrefabUtility.LoadPrefabContents(path);try{Clean(go);PrefabUtility.SaveAsPrefabAsset(go,path);log.AppendLine("MIGRATED prefab "+path);}finally{PrefabUtility.UnloadPrefabContents(go);}}
// Remove retired texture dependencies from the converted shop materials.
foreach(var guid in AssetDatabase.FindAssets("t:Material")){var path=AssetDatabase.GUIDToAssetPath(guid);if(Legacy(path))continue;var m=AssetDatabase.LoadAssetAtPath<Material>(path);bool changed=false;foreach(var name in m.GetTexturePropertyNames()){var texture=m.GetTexture(name);if(texture!=null&&Legacy(AssetDatabase.GetAssetPath(texture))){m.SetTexture(name,null);changed=true;}}if(changed){EditorUtility.SetDirty(m);log.AppendLine("CLEARED legacy texture "+path);}}
AssetDatabase.SaveAssets();
var scenes=AssetDatabase.FindAssets("t:Scene").Select(AssetDatabase.GUIDToAssetPath).Where(p=>AssetDatabase.GetDependencies(p,true).Any(Legacy)).ToList();
if(!scenes.Contains(current.path))scenes.Add(current.path);
// Keep the user's open scene loaded, including their unsaved work.
foreach(var path in scenes){bool active=path==current.path;var scene=active?current:EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);foreach(var root in scene.GetRootGameObjects())Clean(root);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);if(!active)EditorSceneManager.CloseScene(scene,true);log.AppendLine("MIGRATED scene "+path);}
SceneManager.SetActiveScene(current);AssetDatabase.SaveAssets();
var remaining=AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/")&&!Legacy(p)&&!AssetDatabase.IsValidFolder(p)&&new[]{".unity",".prefab",".mat",".asset"}.Contains(Path.GetExtension(p))).Where(p=>AssetDatabase.GetDependencies(p,true).Any(Legacy)).ToArray();
foreach(var p in remaining)log.AppendLine("REMAINING "+p);
log.AppendLine("Remaining dependent assets: "+remaining.Length);File.WriteAllText("Temp/Mok-migrate-legacy.txt",log.ToString());
if(remaining.Length>0)throw new Exception("References remain; do not delete legacy folders.");
}
}}
#endif
