#if UNITY_EDITOR
using System.IO;using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad]public static class LobbyFinish {
static LobbyFinish(){EditorApplication.update+=Poll;}
static void Poll(){if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists("Temp/Mok-lobby-finish-request"))return;File.Delete("Temp/Mok-lobby-finish-request");var scene=SceneManager.GetActiveScene();if(scene.name!="Lobby")return;
var c=Object.FindFirstObjectByType<LobbyController>();c.lobbyCamera.transform.position=new Vector3(147,2.6f,75);c.lobbyCamera.transform.LookAt(new Vector3(141,1.8f,61));c.lobbyCamera.fieldOfView=50;
foreach(var p in Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include,FindObjectsSortMode.None)){p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var main=p.main;main.playOnAwake=false;var r=p.GetComponent<ParticleSystemRenderer>();if(r)r.enabled=false;}
var walkers=Object.FindObjectsByType<LobbyWalker>(FindObjectsSortMode.None).OrderBy(w=>w.name).ToArray();for(int i=0;i<walkers.Length;i++){walkers[i].from=new Vector3(130+i*.6f,.23f,i==1?105:35);walkers[i].to=new Vector3(130+i*.6f,.23f,i==1?35:105);walkers[i].transform.position=Vector3.Lerp(walkers[i].from,walkers[i].to,.3f);}
Directory.CreateDirectory("Assets/Scripts/Mok/Art/Lobby");foreach(var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(r=>r.name.ToLowerInvariant().Contains("sky"))){if(r.sharedMaterial==null)continue;var path="Assets/Scripts/Mok/Art/Lobby/DawnSky.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat==null){mat=new Material(r.sharedMaterial);AssetDatabase.CreateAsset(mat,path);}var tint=new Color(.40f,.48f,.65f);foreach(var key in new[]{"_Color","_BaseColor","_TintColor","_EmissionColor"})if(mat.HasProperty(key))mat.SetColor(key,tint);r.sharedMaterial=mat;}
EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();File.WriteAllText("Temp/Mok-lobby-finish.txt","Camera refined; particle effects disabled; pedestrians routed along street; independent dawn sky material saved.");}
}
}
#endif
