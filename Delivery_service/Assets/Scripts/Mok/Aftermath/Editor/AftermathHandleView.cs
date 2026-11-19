#if UNITY_EDITOR
using System.Linq;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using Object=UnityEngine.Object;
namespace DeliveryService.Mok.Editor{public static class AftermathHandleView{public static void Apply(){var s=EditorSceneManager.OpenScene("Assets/Scenes/PrisonAftermath.unity");EditorSceneManager.SaveScene(s,"Logs/Aftermath-before-handleview.unity",true);var f=Object.FindFirstObjectByType<AftermathFilm>();var player=f.firstPerson.hideoutPlayer;
player.transform.position=new Vector3(502.4f,0,-1.8f);
for(int i=1;i<=3;i++){var m=f.shots[i].motions.First(x=>x.actor==player);m.from.position=m.to.position=player.transform.position;}
var crate=f.shots[1].set.GetComponentsInChildren<Transform>(true).First(t=>t.name=="SM_Gen_Prop_Crate_Preset_01");crate.position+=new Vector3(-5.2f,0,-1.5f);
var pallet=f.shots[1].set.GetComponentsInChildren<Transform>(true).First(t=>t.name=="SM_Prop_Pallet_01");pallet.position+=new Vector3(-5.2f,0,-1.5f);
EditorSceneManager.SaveScene(s);AftermathReview.Record();}}}
#endif
