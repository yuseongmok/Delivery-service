#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace DeliveryService.Mok.Editor {
// 저장된 씬 대사를 한 번만 갱신하고 이후 사용자의 편집은 보존한다.
[InitializeOnLoad]
public static class BackdoorStoryInstaller {
 static BackdoorStoryInstaller(){EditorApplication.update+=Poll;}
 static void Poll(){
  if(EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/BackdoorStory-request"))return;
  File.Delete("Temp/BackdoorStory-request");
  try{Apply();File.WriteAllText("Temp/BackdoorStory-result.txt","PASS");}
  catch(Exception e){Debug.LogException(e);File.WriteAllText("Temp/BackdoorStory-result.txt",e.ToString());}
 }
 static void Apply(){
  var scene=SceneManager.GetSceneByPath(StoryTutorialBuilder.Main);bool opened=!scene.isLoaded;
  if(opened)scene=EditorSceneManager.OpenScene(StoryTutorialBuilder.Main,OpenSceneMode.Additive);
  if(!EditorSceneManager.SaveScene(scene,"Logs/Before-backdoor-story-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new Exception("Backup failed");
  T One<T>() where T:Component=>scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).First();
  var mission=One<BackdoorMission>();var inbox=One<StoryPhoneInbox>();
  var book=AssetDatabase.LoadAssetAtPath<GameDialogue>(GameDialogueInstaller.Path);
  if(!book.entries.Any(e=>e.id=="backdoor.story.bridge")){
   mission.briefing[0]="민기: 납품 승인 덕분에 들어갈 명분은 생겼어. 하지만 박진웅이 우릴 알아보면 어떻게 나올지는 우리가 준비해야 해.";
   mission.briefing[1]="나: 행사장 주변부터 확인하고 올게.\n민기: 그래. 서비스 출입구, 경비 사각지대, 골목 탈출로, 차가 기다릴 자리까지 차례대로 살펴봐. 아직 안으로 들어가지는 말고.";
   mission.debrief[1]="민기: 좋아. 행사에 들어가면 배달원처럼 행동해. 박진웅의 속내를 알아내되, 수상해지면 확인한 길로 바로 빠져나오는 거야.";
   mission.debrief[2]="나: 골목을 벗어나도 쫓아오면 오토바이로는 위험하겠어.\n민기: 둘이 함께 빠져나올 차가 필요하겠네. 내가 아는 정비사에게 연락해 볼게. 조사한 합류 지점에 차를 준비해 두자.";
   book.entries.Add(new GameDialogue.Entry{id="backdoor.story.bridge",group="11 휴대폰 문자",original=inbox.backdoorInvitation,text=inbox.backdoorInvitation});
  }
  void Add(string text,string id,string group){if(book.entries.Any(e=>e.original==text))return;book.entries.Add(new GameDialogue.Entry{id=id,group=group,original=text,text=text});}
  int n=0;foreach(var text in mission.briefing.Concat(mission.findings).Concat(mission.debrief).Append(mission.completionMessage))Add(text,"backdoor.story."+n++,"06 도시의 뒷문");
  var plan=AssetDatabase.LoadAssetAtPath<NightPreparationPlan>("Assets/Scripts/Mok/ShopUI/FirstChapterPreparation.asset");
  foreach(var item in plan.missions)if(item.id=="backdoor")item.description="납품 승인을 받았으니 행사장 주변을 조사하세요. 경비의 시선을 피해 서비스 출입구, 사각지대, 골목 탈출로, 차량 합류 지점을 확인하고 민기에게 보고합니다. 아직 행사장 내부로 잠입하는 단계는 아닙니다.";
  foreach(var obj in new UnityEngine.Object[]{mission,inbox,book,plan})EditorUtility.SetDirty(obj);
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();GameDialogue.Invalidate();
  if(opened)EditorSceneManager.CloseScene(scene,true);
 }
}
}
#endif
