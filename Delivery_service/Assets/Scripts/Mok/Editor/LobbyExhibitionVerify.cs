#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using UnityEditor;using UnityEngine;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad] public static class LobbyExhibitionVerify {
 static bool testing;static double began;static Vector3 cameraStart,actorStart;static LobbyController c;static LobbyPasser actor;static string log;
 static LobbyExhibitionVerify(){testing=SessionState.GetBool("Mok.Exhibition.Verify",false);EditorApplication.update+=Tick;}
 static void Tick(){
 if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
 if(!testing&&File.Exists("Temp/Mok-exhibition-verify")&&!EditorApplication.isPlayingOrWillChangePlaymode){File.Delete("Temp/Mok-exhibition-verify");testing=true;SessionState.SetBool("Mok.Exhibition.Verify",true);began=EditorApplication.timeSinceStartup;log="";EditorApplication.EnterPlaymode();return;}
 if(!testing||!EditorApplication.isPlaying)return;
 try{
 if(c==null){c=UnityEngine.Object.FindFirstObjectByType<LobbyController>();if(!c)return;cameraStart=c.lobbyCamera.transform.position;var actors=UnityEngine.Object.FindObjectsByType<LobbyPasser>(FindObjectsSortMode.None);if(actors.Length<3)throw new Exception("추격/차량 배경 누락");foreach(var p in actors)p.delay=0;actor=actors.First(p=>p.name.Contains("Robber"));actorStart=actor.transform.position;began=EditorApplication.timeSinceStartup;Application.runInBackground=true;return;}
 if(EditorApplication.timeSinceStartup-began<3)return;
 if(Vector3.Distance(cameraStart,c.lobbyCamera.transform.position)<.001f)throw new Exception("카메라 이동 없음");
 if(Vector3.Distance(actorStart,actor.transform.position)<1)throw new Exception("배경 추격 이동 없음");
 if(c.menu.transform.Find("Integrated Logo").GetComponent<UnityEngine.UI.RawImage>().texture==null)throw new Exception("로고 누락");
 var a=actor.GetComponentInChildren<Animator>();if(!a||a.GetFloat("MoveSpeed")<1)throw new Exception("달리기 애니메이션 입력 없음");
 if(UnityEngine.Object.FindFirstObjectByType<NPCControl>()||UnityEngine.Object.FindFirstObjectByType<PoliceControl>())throw new Exception("게임 AI 남음");
 LobbyExhibitionBuilder.Capture(c,"Temp/Lobby-exhibition-play.png");log="PASS: camera drift, chase movement/run animation, traffic actors, image logo, no gameplay NPC AI.\n";
 Finish();
 }catch(Exception e){log="FAIL: "+e;Finish();}
 }
 static void Finish(){File.WriteAllText("Temp/Lobby-exhibition-verify.txt",log);testing=false;SessionState.SetBool("Mok.Exhibition.Verify",false);c=null;EditorApplication.ExitPlaymode();}
 }
}
#endif

