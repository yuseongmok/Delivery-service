#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using DeliveryService.Yun.Settings;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad] public static class PrologueSettingsSetup {
 static int step; static double next; static bool testing;
 static PrologueSettingsSetup(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("PrologueSettingsTest",false)){SessionState.SetBool("PrologueSettingsTest",false);testing=true;step=0;next=EditorApplication.timeSinceStartup+4;}};}
 static T[] All<T>(Scene s) where T:Component=>s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();
 static void Tick(){
  if(EditorApplication.isCompiling)return;
  try {
   if(!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists("Temp/PrologueSettings-request")){
    File.Delete("Temp/PrologueSettings-request");Apply();EditorSceneManager.OpenScene(BankPrologueBuilder.ScenePath);SessionState.SetBool("PrologueSettingsTest",true);EditorApplication.EnterPlaymode();
   }
   if(!testing||!EditorApplication.isPlaying||EditorApplication.timeSinceStartup<next)return;
   var d=UnityEngine.Object.FindFirstObjectByType<BankPrologue>();if(!d)return;
   if(step==0){
    d.StopAllCoroutines();typeof(BankPrologue).GetField("<Current>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(d,BankPrologue.Phase.Robbery);
    foreach(var o in d.officers)o.gameObject.SetActive(false);
    d.SetPaused(true);Check(d.settingsPanel.gameObject.activeInHierarchy&&Time.timeScale==0&&!d.player.ReceiveHit(5),"shared menu pauses gameplay and damage");
    d.settingsPanel.Close();next=EditorApplication.timeSinceStartup+.3;step++;return;
   }
   if(step==1){Check(!d.Paused&&Time.timeScale==1,"close button resumes prologue");Check(d.player.ReceiveHit(5)&&!d.player.KnockedDown&&d.player.HitsSinceFall==1,"first hit stays standing");}
   if(step==2){Check(d.player.ReceiveHit(5)&&!d.player.KnockedDown&&d.player.HitsSinceFall==2,"second hit stays standing");Check((float)typeof(BankPrologue).GetField("hitOpacity",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(d)>0,"red hit feedback visible");}
   if(step==3){Check(d.player.ReceiveHit(5)&&d.player.KnockedDown&&d.player.HitsSinceFall==0,"third hit triggers ragdoll");Check(!d.player.ReceiveHit(5),"no repeated hits while down");next=EditorApplication.timeSinceStartup+3;step++;return;}
   if(step==4){Check(!d.player.KnockedDown&&d.player.view.enabled,"first person control recovers");testing=false;EditorApplication.ExitPlaymode();return;}
   step++;next=EditorApplication.timeSinceStartup+.75;
  }catch(Exception e){File.AppendAllText("Logs/Prologue-settings-check.txt",e+"\n");testing=false;if(EditorApplication.isPlaying)EditorApplication.ExitPlaymode();}
 }
 static void Check(bool value,string label){File.AppendAllText("Logs/Prologue-settings-check.txt",(value?"PASS ":"FAIL ")+label+"\n");if(!value)throw new Exception(label);}
 static void Apply(){
  var scene=SceneManager.GetSceneByPath(BankPrologueBuilder.ScenePath);if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(BankPrologueBuilder.ScenePath,OpenSceneMode.Additive);
  EditorSceneManager.SaveScene(scene,"Logs/Before-prologue-settings-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
  var d=All<BankPrologue>(scene).Single();
  if(!d.settingsPanel){
   var city=SceneManager.GetSceneByPath("Assets/Scenes/JJinCity.unity");bool opened=!city.isLoaded;if(opened)city=EditorSceneManager.OpenScene("Assets/Scenes/JJinCity.unity",OpenSceneMode.Additive);
   var root=UnityEngine.Object.Instantiate(All<GameAudioSettings>(city).Single().transform.root.gameObject);root.name="Prologue shared settings";SceneManager.MoveGameObjectToScene(root,scene);
   // 도시의 외부 참조만 비우고 설정창 내부 버튼과 슬라이더 연결은 유지한다.
   foreach(var c in root.GetComponentsInChildren<Component>(true)){if(!c)continue;var data=new SerializedObject(c);var it=data.GetIterator();while(it.Next(true))if(it.propertyType==SerializedPropertyType.ObjectReference&&it.objectReferenceValue){var obj=it.objectReferenceValue;var go=obj is Component comp?comp.gameObject:obj as GameObject;if(go&&go.scene==city)it.objectReferenceValue=null;}data.ApplyModifiedPropertiesWithoutUndo();}
   d.settingsPanel=root.GetComponentInChildren<GameSettingsPanel>(true);d.settingsPanel.gameObject.SetActive(false);
   if(!All<UnityEngine.EventSystems.EventSystem>(scene).Any()){var events=UnityEngine.Object.Instantiate(All<UnityEngine.EventSystems.EventSystem>(city).First().gameObject);SceneManager.MoveGameObjectToScene(events,scene);}
   if(opened)EditorSceneManager.CloseScene(city,true);
  }
  EditorUtility.SetDirty(d);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);File.WriteAllText("Logs/Prologue-settings-check.txt","Saved shared city settings in prologue\n");
 }
}
}
#endif

