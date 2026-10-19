#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;using DeliveryService.Mok.Navigation;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad]public static class WeaponNavigationSetup {
 static WeaponNavigationSetup(){EditorApplication.update+=Poll;}
 static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/Mok-weapon-navigation-setup"))return;File.Delete("Temp/Mok-weapon-navigation-setup");var old=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath("Assets/Scenes/JJinCity.unity");bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene("Assets/Scenes/JJinCity.unity",OpenSceneMode.Additive);try{
 if(!EditorSceneManager.SaveScene(scene,"Temp/JJinCity-before-weapon-navigation.unity",true))throw new Exception("Backup failed");
 var root=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).First(t=>t.name=="Weapon_Shop");var place=root.GetComponent<NavigationPlace>();if(place==null)place=root.gameObject.AddComponent<NavigationPlace>();place.placeId="weapon-shop";place.displayName="무기점";place.kind=NavigationPlaceKind.Weapon;place.mapAnchor=root;
 // 상점 내부가 아닌 입구 앞 보행 공간으로 안내한다.
 var entrance=root.Find("Navigation Entrance");if(entrance==null){entrance=new GameObject("Navigation Entrance").transform;entrance.SetParent(root,false);}entrance.position=root.position-root.forward*2.4f;place.entrance=entrance;
 EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);File.WriteAllText("Temp/Mok-weapon-navigation-setup.txt","PASS: Saved weapon shop map pin and entrance "+entrance.position);
 }catch(Exception e){File.WriteAllText("Temp/Mok-weapon-navigation-setup.txt",e.ToString());}finally{SceneManager.SetActiveScene(old);if(opened)EditorSceneManager.CloseScene(scene,true);}}
}}
#endif
