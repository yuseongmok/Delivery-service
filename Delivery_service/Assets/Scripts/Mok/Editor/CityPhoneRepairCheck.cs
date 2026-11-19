#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using DeliveryService.Mok.Navigation;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad] public static class CityPhoneRepairCheck {
 static int step;static double until;static bool running;
 static CityPhoneRepairCheck(){EditorApplication.update+=Run;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("CityPhoneCheck",false)){SessionState.SetBool("CityPhoneCheck",false);running=true;step=0;until=EditorApplication.timeSinceStartup+2;}};}
 static void Check(bool ok,string text){File.AppendAllText("Logs/City-phone-check.txt",(ok?"PASS ":"FAIL ")+text+"\n");if(!ok)throw new System.Exception(text);}
 static void Run(){
 if(!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling&&File.Exists("Temp/CityPhoneCheck")){File.Delete("Temp/CityPhoneCheck");File.WriteAllText("Logs/City-phone-check.txt","Runtime app navigation check (API input)\n");SessionState.SetBool("CityPhoneCheck",true);EditorApplication.EnterPlaymode();return;}
 if(!running||!EditorApplication.isPlaying||EditorApplication.timeSinceStartup<until)return;
 try{
 if(step==0){step=1;SceneManager.LoadScene("JJinCity");until=EditorApplication.timeSinceStartup+8;return;}
 var p=Object.FindFirstObjectByType<NavigationPlanner>();var phone=Object.FindFirstObjectByType<PhoneController>();
 if(step==1){Check(p&&p.enabled&&phone,"phone controller and app controller exist");if(StoryPhoneInbox.Current)StoryPhoneInbox.Current.AutoOpenEnabled=false;phone.ClosePhone();typeof(NavigationPlanner).GetMethod("LateUpdate",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(p,null);phone.OpenPhone();step=2;until=EditorApplication.timeSinceStartup+1;return;}
 if(step==2){Check(p.CurrentScreen==NavigationPlanner.Screen.Home,"opening phone shows home; actual="+p.CurrentScreen+" open="+phone.IsPhoneOpen()+" riding="+p.IsRiding);p.Show(NavigationPlanner.Screen.Missions);var app=Object.FindFirstObjectByType<StoryPhoneApp>();Check(app&&app.heading&&app.heading.gameObject.activeInHierarchy,"mission app visible");app.Confirm();Check(app.Detail||StoryTutorial.Active,"mission detail / tutorial objective accessible");p.Show(NavigationPlanner.Screen.Messages);Check(app.MessagesMode,"messages accessible");p.Show(NavigationPlanner.Screen.Home);phone.ClosePhone();step=3;until=EditorApplication.timeSinceStartup+1;return;}
 Check(!phone.IsPhoneOpen(),"phone closes normally");File.AppendAllText("Logs/City-phone-check.txt","FINISHED\n");running=false;EditorApplication.ExitPlaymode();
 }catch(System.Exception e){File.AppendAllText("Logs/City-phone-check.txt",e.ToString());running=false;EditorApplication.ExitPlaymode();}
 }
}}
#endif
