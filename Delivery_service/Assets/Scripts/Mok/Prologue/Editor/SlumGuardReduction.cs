#if UNITY_EDITOR
using System;using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;
namespace DeliveryService.Mok.Editor {
public static class SlumGuardReduction {
 public static void Apply(){if(EditorApplication.isPlaying)throw new Exception("Edit mode required");var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/JJinCity.unity");if(!scene.isLoaded)scene=EditorSceneManager.OpenScene("Assets/Scenes/JJinCity.unity",OpenSceneMode.Additive);
 if(!EditorSceneManager.SaveScene(scene,"Logs/Slum-before-three-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new Exception("Backup failed");
 var roster=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<SlumGuardRoster>(true)).Single();var old=roster.posts;
 if(old.Length>3)roster.posts=new[]{old[0],old[2],old[3]};foreach(var t in old)if(t&&!roster.posts.Contains(t))t.gameObject.SetActive(false);roster.projectilePrefab=null;
 foreach(var t in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<SlumRockThrower>(true)))UnityEngine.Object.DestroyImmediate(t);
 EditorUtility.SetDirty(roster);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
 }
}}
#endif
