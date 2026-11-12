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
public static class FinaleStoryInstaller {
 static FinaleStoryInstaller(){EditorApplication.update+=Poll;}
 static void Poll(){
  if(EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/FinaleStory-request"))return;
  string request=File.ReadAllText("Temp/FinaleStory-request").Trim();File.Delete("Temp/FinaleStory-request");
  if(request=="test"){VenueFinaleRefactorCheck.RunBatch();return;}
  try{Apply();File.WriteAllText("Temp/FinaleStory-result.txt","PASS");}
  catch(Exception e){Debug.LogException(e);File.WriteAllText("Temp/FinaleStory-result.txt",e.ToString());}
 }
 static void Apply(){
  var scene=SceneManager.GetSceneByPath(StoryTutorialBuilder.Main);bool opened=!scene.isLoaded;
  if(opened)scene=EditorSceneManager.OpenScene(StoryTutorialBuilder.Main,OpenSceneMode.Additive);
  if(!EditorSceneManager.SaveScene(scene,"Logs/Before-finale-story-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new Exception("Backup failed");
  T One<T>() where T:Component=>scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).First();
  var mission=One<VenueFinale>();var inbox=One<StoryPhoneInbox>();
  var book=AssetDatabase.LoadAssetAtPath<GameDialogue>(GameDialogueInstaller.Path);
  void Add(string text,string id,string group){if(book.entries.Any(e=>e.original==text))return;book.entries.Add(new GameDialogue.Entry{id=id,group=group,original=text,text=text});}
  Add(inbox.finaleInvitation,"finale.story.invitation","11 휴대폰 문자");
  if(!book.entries.Any(e=>e.id=="finale.opening.bridge")){mission.opening[0]="민기: 납품 승인도 받았고, 출입구와 탈출로도 확인했어. 정비사는 약속한 합류 지점에 차를 준비할 거야. 이제 행사장으로 가자.";Add(mission.opening[0],"finale.opening.bridge","08 마지막 배달 · 스토리 안내");}
  int n=0;foreach(var text in mission.opening.Concat(mission.overheard).Concat(mission.closing))Add(text,"finale.story."+n++,"08 마지막 배달 · 스토리 안내");
  var plan=AssetDatabase.LoadAssetAtPath<NightPreparationPlan>("Assets/Scripts/Mok/ShopUI/FirstChapterPreparation.asset");
  foreach(var item in plan.missions)if(item.id=="finale")item.description="준비한 납품 승인으로 행사장에 들어가 피자를 전달하고 박진웅의 대화를 엿들으세요. 들키면 조사한 출입구와 골목 경로로 탈출해 정비사가 마련한 차를 타고 빠져나옵니다.";
  foreach(var obj in new UnityEngine.Object[]{mission,inbox,book,plan})EditorUtility.SetDirty(obj);
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();GameDialogue.Invalidate();
  if(opened)EditorSceneManager.CloseScene(scene,true);
 }
}
}
#endif
