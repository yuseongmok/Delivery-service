#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
[InitializeOnLoad] public static class MokBuildingFinish {
static MokBuildingFinish(){if(File.Exists("Temp/Mok-building-finish-request"))EditorApplication.update+=Apply;}
static void Apply(){if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)return;EditorApplication.update-=Apply;File.Delete("Temp/Mok-building-finish-request");var shop=GameObject.Find("PizzaStore");
foreach(var r in shop.GetComponents<Renderer>()){Undo.RecordObject(r,"Hide original building mesh");r.enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(r);}
foreach(var c in shop.GetComponents<Collider>()){Undo.RecordObject(c,"Disable original building collision");c.enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(c);}
// Ceiling shadows matter indoors; small sign lights do not need their own shadow maps.
var sw=shop.GetComponentInChildren<DeliveryService.Mok.ShopUI.ShopLightSwitch>();foreach(var l in sw.lights)if(l.name=="SignLight")l.shadows=LightShadows.None;
EditorSceneManager.MarkSceneDirty(shop.scene);EditorSceneManager.SaveScene(shop.scene);File.WriteAllText("Temp/Mok-building-finish.txt","Original imported root mesh/collider disabled; original hierarchy retained; scene saved.");EditorApplication.delayCall+=DeliveryService.Mok.Editor.ShopBuildingCheck.Start;
}}
#endif
