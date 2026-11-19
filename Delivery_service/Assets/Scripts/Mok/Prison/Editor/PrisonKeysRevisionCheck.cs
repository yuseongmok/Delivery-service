#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;
namespace DeliveryService.Mok.Editor
{
 [InitializeOnLoad] public static class PrisonKeysRevisionCheck
 {
  const string Log="Logs/Prison-keys-revision-check.txt";static int step;static float wait;static double deadline;
  static PrisonKeysRevisionCheck(){EditorApplication.update+=Tick;}
  public static void Run(){var active=EditorSceneManager.GetActiveScene();if(active.name=="PrisonChapter"){if(!EditorSceneManager.SaveScene(active,"Logs/Prison-before-keys-review-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new Exception("Backup failed");}else EditorSceneManager.OpenScene("Assets/Scenes/PrisonChapter.unity");File.WriteAllText(Log,"Assisted mission entry; actual interaction and scene transition routines.\n");SessionState.SetBool("KeysRevisionCheck",true);var game=EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView"));SessionState.SetBool("KeysReviewWasMaximized",game.maximized);game.maximized=true;ConfigureView(game);game.Focus();EditorApplication.EnterPlaymode();}
  static void ConfigureView(EditorWindow game){
   var assembly=typeof(EditorWindow).Assembly;var type=assembly.GetType("UnityEditor.GameViewSizes");
   var sizes=typeof(ScriptableSingleton<>).MakeGenericType(type).GetProperty("instance").GetValue(null);
   var group=type.GetMethod("GetGroup").Invoke(sizes,new object[]{GameViewSizeGroupType.Standalone});
   var size=Activator.CreateInstance(assembly.GetType("UnityEditor.GameViewSize"),BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,new object[]{Enum.Parse(assembly.GetType("UnityEditor.GameViewSizeType"),"FixedResolution"),1280,720,"Prison key review"},null);
   group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{size});
   var index=game.GetType().GetProperty("selectedSizeIndex",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
   SessionState.SetInt("KeysReviewSize",(int)index.GetValue(game));
   int count=(int)group.GetType().GetMethod("GetBuiltinCount").Invoke(group,null)+(int)group.GetType().GetMethod("GetCustomCount").Invoke(group,null);
   index.SetValue(game,count-1);
  }
  static void Assert(bool b,string message){File.AppendAllText(Log,(b?"PASS ":"FAIL ")+message+"\n");if(!b)throw new Exception(message);}
  static void Set(object o,string name,object value){o.GetType().GetProperty(name).SetValue(o,value);}
  static void Tick(){
   if(!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling&&File.Exists("Temp/PrisonKeysRevisionCheck")){File.Delete("Temp/PrisonKeysRevisionCheck");step=0;Run();return;}
   if(!EditorApplication.isPlayingOrWillChangePlaymode&&SessionState.GetBool("KeysRevisionFinished",false)){SessionState.SetBool("KeysRevisionFinished",false);var game=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));game.GetType().GetProperty("selectedSizeIndex",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).SetValue(game,SessionState.GetInt("KeysReviewSize",0));EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView")).maximized=SessionState.GetBool("KeysReviewWasMaximized",false);return;}
   if(!EditorApplication.isPlaying||!SessionState.GetBool("KeysRevisionCheck",false))return;
   try{
    if(step==0){deadline=EditorApplication.timeSinceStartup+180;step=1;wait=Time.time+3;return;}if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Timeout "+step);if(Time.time<wait)return;
    var m=Object.FindFirstObjectByType<PrisonKeysMission>();if(!m)return;var p=m.previous.prison.player;
    if(step==1){if(PrisonMissionSkip.Target!="소매치기와 배식"){PrisonMissionSkip.Complete();wait=Time.time+1;return;}m.Begin();var theft=m.GetComponent<PrisonPickpocket>();theft.Begin(m);Set(theft,"State",PrisonPickpocket.Phase.Snag);Set(theft,"SnagProgress",.45f);Set(theft,"Progress",.3f);Set(theft,"SnagDirection",1);theft.SendMessage("LateUpdate");var before=m.keyRing.transform.localRotation;typeof(PrisonPickpocket).GetField("resumePhase",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(theft,PrisonPickpocket.Phase.Snag);Set(theft,"State",PrisonPickpocket.Phase.Freeze);theft.SendMessage("LateUpdate");Assert(Quaternion.Angle(before,m.keyRing.transform.localRotation)<.01f,"guard stirring preserves half-turned key pose");Assert(Mathf.Approximately(theft.SnagProgress,.45f),"snag progress retained");theft.End(false);Set(m,"State",PrisonKeysMission.Stage.Unlock);var cc=p.GetComponent<CharacterController>();cc.enabled=false;p.transform.position=m.keyLock.transform.position-m.keyLock.transform.forward*.9f;cc.enabled=true;Cursor.lockState=CursorLockMode.Locked;step=2;wait=Time.time+.2f;return;}
    if(step==2){Assert(m.Interact(),"warehouse key interaction starts");step=3;wait=Time.time+1;return;}
    if(step==3){var interaction=m.GetComponent<PrisonLockInteraction>();Assert(m.keyLock.keyModels.Length==4,"four physical keys available");for(int i=0;i<4;i++){var rect=interaction.KeyChoiceRect(i);Assert(interaction.HitKey(new Vector2(rect.center.x,Screen.height-rect.center.y))==i,"click chooses distinct key "+i);Assert(rect.yMax<Screen.height*.82f,"key above instruction panel "+i);for(int j=0;j<i;j++)Assert(!rect.Overlaps(interaction.KeyChoiceRect(j)),"choice regions separate "+j+"/"+i);}ScreenCapture.CaptureScreenshot("Logs/Prison-four-keys.png");step=4;wait=Time.time+1;return;}
    if(step==4){var interaction=m.GetComponent<PrisonLockInteraction>();Assert(interaction.SelectKey(3)&&interaction.State==PrisonLockInteraction.Phase.Inserting&&m.keyLock.Selected==3,"direct selection enters insertion");m.CancelLock();Assert(p.View.enabled&&!m.SelectingKey,"cancel returns to player");var cheat=DeveloperCheats.Current;cheat.Open();Assert(cheat.StartPrisonFinale()&&!cheat.IsOpen,"finale cheat closes menu and starts fresh scene transition");step=5;wait=0;return;}
    if(step==5){if(PrisonFinaleJump.Running)return;Assert(string.IsNullOrEmpty(PrisonFinaleJump.LastError),"finale jump preparation succeeds");var cctv=Object.FindFirstObjectByType<PrisonCctvMission>();Assert(cctv.State==PrisonCctvMission.Stage.Change&&cctv.office.Complete,"jump begins at disguise, keeps CCTV and escape playable");Assert(p.View.enabled&&p.GetComponent<PlayerMove>().isControllable,"jump restores first-person controls");Assert(Vector3.Distance(p.transform.position,cctv.legacyEscape.changePoint.position)<2.5f,"spawn in disguise interaction range");Cursor.lockState=CursorLockMode.Locked;Assert(cctv.Interact(),"disguise can start immediately after cheat");File.AppendAllText(Log,"FINISHED\n");Finish(0);}
   }catch(Exception e){File.AppendAllText(Log,e.ToString());Finish(1);}
  }
  static void Finish(int code){Time.timeScale=1;SessionState.SetBool("KeysRevisionCheck",false);SessionState.SetBool("KeysRevisionFinished",true);SessionState.SetInt("KeysRevisionExit",code);EditorApplication.ExitPlaymode();}
 }
}
#endif
