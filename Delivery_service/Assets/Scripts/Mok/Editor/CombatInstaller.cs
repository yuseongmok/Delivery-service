#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad] public static class CombatInstaller {
static CombatInstaller(){EditorApplication.update+=Poll;}
static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/Mok-combat-install"))return;File.Delete("Temp/Mok-combat-install");try{Install();}catch(Exception e){File.WriteAllText("Temp/Mok-combat-install.txt",e.ToString());Debug.LogException(e);}}
[MenuItem("Tools/Delivery Service/Mok/Install Citizen Combat")]
public static void Install(){
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/JJinCity.unity")throw new Exception("Open JJinCity first");
if(!File.Exists("Temp/JJinCity-before-combat.unity")&&!EditorSceneManager.SaveScene(scene,"Temp/JJinCity-before-combat.unity",true))throw new Exception("Scene backup failed");
Directory.CreateDirectory("Temp/Mok-combat-original-prefabs");string log="";
foreach(var path in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Scripts/Min/Prefab/NPC"}).Select(AssetDatabase.GUIDToAssetPath)){
if(!Path.GetFileName(path).Contains("_NPC_"))continue;
string backup="Temp/Mok-combat-original-prefabs/"+Path.GetFileName(path);if(!File.Exists(backup))File.Copy(path,backup);
var root=PrefabUtility.LoadPrefabContents(path);try{
var npc=root.GetComponent<NPCControl>();if(npc==null)continue;
if(root.GetComponent<CitizenCombat>()==null)root.AddComponent<CitizenCombat>();
var a=root.GetComponent<Animator>();log+=path+": rig="+(a!=null&&a.avatar!=null?a.avatar.name:"none")+", rigidbodies="+root.GetComponentsInChildren<Rigidbody>().Length+"\n";
PrefabUtility.SaveAsPrefabAsset(root,path);
}finally{PrefabUtility.UnloadPrefabContents(root);}}
foreach(var npc in UnityEngine.Object.FindObjectsByType<NPCControl>(FindObjectsSortMode.None))if(npc.GetComponent<RobberAI>()==null&&npc.GetComponent<PoliceControl>()==null&&npc.GetComponent<CitizenCombat>()==null)Undo.AddComponent<CitizenCombat>(npc.gameObject);
var player=UnityEngine.Object.FindFirstObjectByType<PlayerInteraction>();if(player==null)throw new Exception("Player missing");
var combat=player.GetComponent<PlayerCombat>();if(combat==null)combat=Undo.AddComponent<PlayerCombat>(player.gameObject);
const string folder="Assets/Scripts/Mok/Art/Combat";Directory.CreateDirectory(folder);
Material MakeMat(string name,Color color){var m=AssetDatabase.LoadAssetAtPath<Material>(folder+"/"+name+".mat");if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,folder+"/"+name+".mat");}m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.05f);EditorUtility.SetDirty(m);return m;}
var pants=MakeMat("Pants",new Color(.12f,.17f,.2f));var boot=MakeMat("Boot",new Color(.08f,.075f,.065f));var sole=MakeMat("Sole",new Color(.23f,.23f,.21f));
if(combat.kickVisual==null){
var leg=new GameObject("Mok Kick Leg");leg.transform.SetParent(player.InteractionCamera.transform,false);
void Part(string name,Vector3 pos,Vector3 scale,Material mat){var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.SetParent(leg.transform,false);o.transform.localPosition=pos;o.transform.localScale=scale;o.GetComponent<Renderer>().sharedMaterial=mat;UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());}
Part("Trouser",new Vector3(0,0,.13f),new Vector3(.14f,.15f,.46f),pants);Part("Boot",new Vector3(0,-.025f,.40f),new Vector3(.18f,.16f,.26f),boot);Part("Sole",new Vector3(0,-.12f,.41f),new Vector3(.19f,.035f,.28f),sole);
combat.kickVisual=leg.transform;leg.SetActive(false);
}
EditorUtility.SetDirty(combat);PrefabUtility.RecordPrefabInstancePropertyModifications(combat);
EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
File.WriteAllText("Temp/Mok-combat-install.txt","PASS: installed and saved\n"+log);
}
}}
#endif
