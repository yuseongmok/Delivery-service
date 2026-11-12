#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad] public static class LobbyModeCheck {
 const string Key="Mok.ModeCards.Check";
 static IEnumerator run;static double began;static string log;static int frame;
 static LobbyModeCheck(){EditorApplication.update+=Poll;EditorApplication.playModeStateChanged+=Changed;}
 static void Poll(){
  if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/Mok-mode-test-request"))return;
  File.Delete("Temp/Mok-mode-test-request");
  for(int i=0;i<SceneManager.sceneCount;i++){var s=SceneManager.GetSceneAt(i);if(s.isDirty){File.WriteAllText("Temp/Mok-mode-test-result.txt","STOP: unsaved scene "+s.name);return;}}
  typeof(LobbyCheck).GetMethod("Backup",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);SessionState.SetBool("Mok.Lobby.Check.background",PlayerSettings.runInBackground);
  EditorSceneManager.OpenScene("Assets/Scenes/Lobby.unity",OpenSceneMode.Single);
  SessionState.SetBool(Key,true);EditorApplication.EnterPlaymode();
 }
 static void Changed(PlayModeStateChange s){if(!SessionState.GetBool(Key,false))return;
  if(s==PlayModeStateChange.EnteredPlayMode){log="";began=EditorApplication.timeSinceStartup;frame=-1;run=Run();EditorApplication.update+=Tick;}
  if(s==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Key,false);typeof(LobbyCheck).GetMethod("Restore",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);}
 }
 static void Tick(){if(!EditorApplication.isPlaying||frame==Time.frameCount)return;frame=Time.frameCount;
  try{if(EditorApplication.timeSinceStartup-began>100)throw new Exception("Timeout");if(run.MoveNext())return;log+="RESULT: PASS\n";}catch(Exception e){log+="FAIL "+e;}
  EditorApplication.update-=Tick;File.WriteAllText("Temp/Mok-mode-test-result.txt",log);EditorApplication.ExitPlaymode();
 }
 static void Check(bool pass,string message){if(!pass)throw new Exception(message);log+="PASS "+message+"\n";}
 static IEnumerator Run(){
  float end=Time.realtimeSinceStartup+4;while(Time.realtimeSinceStartup<end)yield return null;
  var c=UnityEngine.Object.FindFirstObjectByType<LobbyController>();Check(c!=null,"Lobby controller");
  Check(c.menu.gameObject.activeSelf&&!c.modeMenu.gameObject.activeSelf&&c.lobbyCamera.targetTexture==null,"Original lobby first / no blur"); ScreenCapture.CaptureScreenshot("Temp/Lobby-restored.png",2);yield return null;yield return null; c.startButton.onClick.Invoke();yield return null;Check(!c.IsLoading&&c.modeMenu.gameObject.activeSelf&&!c.menu.gameObject.activeSelf,"Play opens mode selection only"); var back=UnityEngine.Object.FindFirstObjectByType<LobbyModeBackdrop>();Check(back&&back.source.targetTexture&&back.display.texture==back.source.targetTexture,"Blur background render target");
  Check(c.modeMenu.GetComponentsInChildren<RawImage>().Count(x=>x.name=="Game artwork"&&x.texture)!=0,"Game artwork loaded"); Check(Camera.allCameras.Any(x=>x.enabled&&x.targetTexture==null&&x.targetDisplay==0),"Display camera remains active"); Check(!c.modeMenu.GetComponentsInChildren<Text>().Any(x=>x.text.Contains("당신의 다음")),"Subtitle removed");
  foreach(var icon in c.modeMenu.GetComponentsInChildren<LobbyModeIcon>()){Check(icon.canvasRenderer!=null,"Icon renderer: "+icon.kind);Check(icon.canvasRenderer.GetMesh().vertexCount>0,"Icon geometry: "+icon.kind);}
  var challenge=c.modeMenu.transform.Find("Challenge mode").GetComponent<Button>();Check(!challenge.interactable&&challenge.onClick.GetPersistentEventCount()==0,"Challenge disabled / no scene action");
  ScreenCapture.CaptureScreenshot("Temp/Lobby-mode-cards.png",2);yield return null;yield return null;
  c.modeSettingsButton.onClick.Invoke();yield return null;Check(c.settingsPanel.activeSelf&&!c.modeMenu.blocksRaycasts,"Existing settings open and blocks cards");
  ScreenCapture.CaptureScreenshot("Temp/Lobby-mode-settings.png",2);yield return null;yield return null;
  c.backButton.onClick.Invoke();Check(!c.settingsPanel.activeSelf&&c.modeMenu.blocksRaycasts,"Settings back");
  Check(c.gameScene=="Assets/Scenes/BankPrologue.unity"&&Application.CanStreamedLevelBeLoaded(c.gameScene),"Prologue included in build");
  c.modeBackButton.onClick.Invoke();yield return null;Check(c.menu.gameObject.activeSelf&&!c.modeMenu.gameObject.activeSelf&&c.lobbyCamera.targetTexture==null,"Back restores lobby and camera");c.settingsButton.onClick.Invoke();yield return null;Check(c.settingsPanel.activeSelf,"Lobby settings still opens");c.backButton.onClick.Invoke();c.startButton.onClick.Invoke();yield return null;c.storyButton.onClick.Invoke();Check(c.IsLoading,"Story click starts transition");
  while(SceneManager.GetActiveScene().name!="BankPrologue")yield return null;
  Check(UnityEngine.Object.FindFirstObjectByType<LobbyController>()==null,"Prologue loaded, lobby unloaded");
 }
}
}
#endif



