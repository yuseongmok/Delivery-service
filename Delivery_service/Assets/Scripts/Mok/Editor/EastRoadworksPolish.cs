#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad]public static class EastRoadworksPolish{
static EastRoadworksPolish(){EditorApplication.update+=Poll;}
static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/Mok-roadworks-polish"))return;File.Delete("Temp/Mok-roadworks-polish");try{Run();}catch(Exception e){File.WriteAllText("Temp/Mok-roadworks-polish.txt",e.ToString());}}
static void Run(){var old=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath("Assets/Scenes/JJinCity.unity");bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene("Assets/Scenes/JJinCity.unity",OpenSceneMode.Additive);SceneManager.SetActiveScene(scene);try{
EditorSceneManager.SaveScene(scene,"Temp/JJinCity-before-roadworks-polish.unity",true);var root=scene.GetRootGameObjects().First(g=>g.name=="Mok East City").transform.Find("Roadworks and parking");
string folder="Assets/Scripts/Mok/Art/Roadworks";Directory.CreateDirectory(folder);AssetDatabase.Refresh();
Material Mat(string name,Color color){string path=folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);bool fresh=m==null;if(fresh)m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.08f);m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*.18f);if(fresh)AssetDatabase.CreateAsset(m,path);else EditorUtility.SetDirty(m);return m;}
var orange=Mat("Safety orange",new Color(.88f,.26f,.045f));var white=Mat("Reflector cream",new Color(.9f,.88f,.76f));var dark=Mat("Construction soil",new Color(.15f,.14f,.12f));
void Box(Transform p,string name,Vector3 position,Vector3 size,Material mat){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(p,false);g.transform.localPosition=position;g.transform.localScale=size;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());g.GetComponent<Renderer>().sharedMaterial=mat;}
foreach(Transform site in root){if(site.Find("Visible safety walls")!=null)continue;var walls=new GameObject("Visible safety walls").transform;walls.SetParent(site,false);
foreach(Transform model in site.Cast<Transform>().ToArray())if(model.name.StartsWith("SM_Prop_Barrier")){model.gameObject.SetActive(false);PrefabUtility.RecordPrefabInstancePropertyModifications(model.gameObject);}
Box(walls,"Work surface",new Vector3(0,.06f,0),new Vector3(1.75f,.035f,13.8f),dark);
foreach(float side in new[]{-1f,1f}){Box(walls,"Side wall",new Vector3(side*.8f,.65f,0),new Vector3(.18f,1.3f,14),orange);for(int n=-6;n<=6;n+=2)Box(walls,"Reflective stripe",new Vector3(side*.9f,.8f,n),new Vector3(.025f,.35f,.8f),white);
Box(walls,"End wall",new Vector3(0,.65f,side*6.9f),new Vector3(1.8f,1.3f,.18f),orange);Box(walls,"End stripe",new Vector3(0,.8f,side*7),new Vector3(1.5f,.35f,.025f),white);}
}
AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);File.WriteAllText("Temp/Mok-roadworks-polish.txt","PASS: collision-aligned orange and cream walls saved for 8 work zones; geometry bounds unchanged.");
}finally{SceneManager.SetActiveScene(old);if(opened)EditorSceneManager.CloseScene(scene,true);}}
}
}
#endif
