#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad] public static class StoryChainAudit {
 static StoryChainAudit(){EditorApplication.update+=Poll;}
 static void Poll(){
  if(EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/StoryChain-request"))return;
  string request=File.ReadAllText("Temp/StoryChain-request").Trim();File.Delete("Temp/StoryChain-request");
  try{if(request=="escape-test"){EscapeChapterRefactorCheck.RunBatch();return;}if(request=="escape-dialogue")RegisterEscape();if(request=="fix")Fix();Run();}catch(Exception e){File.WriteAllText("Logs/Story-chain-dialogue.txt",e.ToString());}
 }
 static void RegisterEscape(){
  var scene=SceneManager.GetSceneByPath(StoryTutorialBuilder.Main);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(StoryTutorialBuilder.Main,OpenSceneMode.Additive);
  var book=AssetDatabase.LoadAssetAtPath<GameDialogue>(GameDialogueInstaller.Path);
  foreach(var component in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MonoBehaviour>(true)).Where(c=>c&&(c is ShopEscapeChapter||c is ParkEscapeSequence))){
   var it=new SerializedObject(component).GetIterator();while(it.Next(true))if(it.propertyType==SerializedPropertyType.String&&!string.IsNullOrWhiteSpace(it.stringValue)&&it.stringValue.Any(c=>c>='가'&&c<='힣')&&!book.entries.Any(e=>e.original==it.stringValue))book.entries.Add(new GameDialogue.Entry{id="escape.chain."+component.GetType().Name+"."+it.propertyPath,group="10 매장 탈출 · 감옥 도입",original=it.stringValue,text=it.stringValue});
  }
  EditorUtility.SetDirty(book);AssetDatabase.SaveAssets();GameDialogue.Invalidate();if(opened)EditorSceneManager.CloseScene(scene,true);
 }
 static void Fix(){
  var plan=AssetDatabase.LoadAssetAtPath<NightPreparationPlan>("Assets/Scripts/Mok/ShopUI/FirstChapterPreparation.asset");
  var book=AssetDatabase.LoadAssetAtPath<GameDialogue>(GameDialogueInstaller.Path);
  var story=AssetDatabase.LoadAssetAtPath<PrologueStory>("Assets/Scripts/Mok/Prologue/Art/BankPrologueStory.asset");
  File.Copy(AssetDatabase.GetAssetPath(plan),"Logs/Before-chain-plan-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".asset",true);
  File.Copy(GameDialogueInstaller.Path,"Logs/Before-chain-dialogue-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".asset",true);
  string[] results={"박진웅의 행사 정보와 납품 담당자 연락처 확보 · 특별 주문 준비 가능","정식 납품 승인 · 행사장 주변 조사 가능","출입구·사각지대·탈출로 확보 · 정비사에게 차량 요청 가능","탈출 차량 지원 확보 · 마지막 배달 출발 가능"};
  for(int i=0;i<4;i++)plan.missions[i].result=results[i];
  string original=story.ending.text;var entry=book.entries.FirstOrDefault(e=>e.original==original);
  if(entry==null){entry=new GameDialogue.Entry{id="chain.firstdelivery.caption",group="01 프롤로그",original=original,text=original};book.entries.Add(entry);}
  entry.text=GameDialogue.Resolve(original).Replace("첫 영업일","위장 생활의 시작");
  EditorUtility.SetDirty(plan);EditorUtility.SetDirty(book);AssetDatabase.SaveAssets();GameDialogue.Invalidate();
 }
 static void Run(){
  var report=new StringBuilder();
  foreach(var path in new[]{StoryTutorialBuilder.Main,BankPrologueBuilder.ScenePath}){
   var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
   foreach(var component in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MonoBehaviour>(true)).Where(c=>c&&new[]{"ShopEscapeChapter","ParkEscapeSequence","StoryTutorial","StoryCoverTransition","StoryPhoneInbox","BrokerMission","SpecialOrderMission","BackdoorMission","MechanicMission","VenueFinale","BankPrologue","PrologueShopArrival"}.Contains(c.GetType().Name))){
    var it=new SerializedObject(component).GetIterator();while(it.Next(true))if(it.propertyType==SerializedPropertyType.String){string text=GameDialogue.Resolve(it.stringValue);if(Regex.IsMatch(text,"내일|다음 날|일차|마감|영업|개점|낮에|밤마다|매일"))report.AppendLine(component.GetType().Name+"."+it.propertyPath+" = "+text);}
   }
   if(opened)EditorSceneManager.CloseScene(scene,true);
  }
  foreach(var kind in new[]{"PrologueStory","NightPreparationPlan"})foreach(var guid in AssetDatabase.FindAssets("t:"+kind)){var asset=AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid));var it=new SerializedObject(asset).GetIterator();while(it.Next(true))if(it.propertyType==SerializedPropertyType.String){string text=GameDialogue.Resolve(it.stringValue);if(Regex.IsMatch(text,"내일|다음 날|일차|마감|영업|개점|낮에|밤마다|매일"))report.AppendLine(asset.name+"."+it.propertyPath+" = "+text);}}
  File.WriteAllText("Logs/Story-chain-dialogue.txt",report.ToString());
 }
}
}
#endif
