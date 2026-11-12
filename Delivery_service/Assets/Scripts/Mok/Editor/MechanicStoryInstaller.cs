#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace DeliveryService.Mok.Editor {
// 기존 부품 회수 임무는 유지하고 연락과 대사 목록만 연결한다.
[InitializeOnLoad]
public static class MechanicStoryInstaller {
 static MechanicStoryInstaller(){EditorApplication.update+=Poll;}
 static void Poll(){
  if(EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/MechanicStory-request"))return;
  File.Delete("Temp/MechanicStory-request");
  try{Apply();File.WriteAllText("Temp/MechanicStory-result.txt","PASS");}
  catch(Exception e){Debug.LogException(e);File.WriteAllText("Temp/MechanicStory-result.txt",e.ToString());}
 }
 static void Apply(){
  var scene=SceneManager.GetSceneByPath(StoryTutorialBuilder.Main);bool opened=!scene.isLoaded;
  if(opened)scene=EditorSceneManager.OpenScene(StoryTutorialBuilder.Main,OpenSceneMode.Additive);
  if(!EditorSceneManager.SaveScene(scene,"Logs/Before-mechanic-story-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new Exception("Backup failed");
  T One<T>() where T:Component=>scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).First();
  var mission=One<MechanicMission>();var inbox=One<StoryPhoneInbox>();
  var book=AssetDatabase.LoadAssetAtPath<GameDialogue>(GameDialogueInstaller.Path);
  void Add(string text,string id,string group){if(book.entries.Any(e=>e.original==text))return;book.entries.Add(new GameDialogue.Entry{id=id,group=group,original=text,text=text});}
  Add(inbox.mechanicInvitation,"mechanic.story.invitation","11 휴대폰 문자");
  int n=0;foreach(var text in mission.briefing.Concat(mission.ending))Add(text,"mechanic.story."+n++,"07 빚진 정비사");
  var plan=AssetDatabase.LoadAssetAtPath<NightPreparationPlan>("Assets/Scripts/Mok/ShopUI/FirstChapterPreparation.asset");
  foreach(var item in plan.missions)if(item.id=="mechanic")item.description="행사장 출입구와 탈출 경로를 확인했습니다. 수리점에서 정비사를 만나 부탁을 듣고, 조직원들이 가진 부품 상자를 회수해 돌려주세요. 부품을 전달하면 조사한 합류 지점에 탈출 차량과 열쇠를 준비해 주기로 합니다.";
  foreach(var obj in new UnityEngine.Object[]{inbox,book,plan})EditorUtility.SetDirty(obj);
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();GameDialogue.Invalidate();
  if(opened)EditorSceneManager.CloseScene(scene,true);
 }
}
}
#endif
