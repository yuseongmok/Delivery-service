#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad] public static class RaceGroundCheck {
 static bool running;static int step,index;static double until;static RaceManager race;static MotorcycleController bike;static Transform anchor;
 static RaceGroundCheck(){EditorApplication.update+=Run;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("RaceGroundCheck",false)){SessionState.SetBool("RaceGroundCheck",false);running=true;step=0;until=EditorApplication.timeSinceStartup+2;}};}
 static void Run(){
 if(!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling&&File.Exists("Temp/RaceGroundCheck")){File.Delete("Temp/RaceGroundCheck");SessionState.SetBool("RaceGroundCheck",true);EditorApplication.EnterPlaymode();return;}
 if(!running||!EditorApplication.isPlaying||EditorApplication.timeSinceStartup<until)return;
 try{
 if(step==0){step=1;SceneManager.LoadScene("JJinCity");until=EditorApplication.timeSinceStartup+7;return;}
 if(step==1){race=Object.FindFirstObjectByType<RaceManager>();bike=Object.FindFirstObjectByType<MotorcycleController>();race.StartRace(Object.FindFirstObjectByType<RacerNPCInteraction>()?.transform);step=2;until=EditorApplication.timeSinceStartup+17;return;}
 if(step==2){File.WriteAllText("Logs/Race-ground-check.txt","Stopped-bike course sampling\n");anchor=new GameObject("Test anchor").transform;index=0;step=3;}
 if(step==4){File.AppendAllText("Logs/Race-ground-check.txt",index+" "+anchor.position+" sphere="+bike.sphereRB.position+" driven="+bike.isDriven+"\n");index++;step=3;}
 if(index>=race.PlayerCourse.Length){File.AppendAllText("Logs/Race-ground-check.txt","FINISHED\n");running=false;EditorApplication.ExitPlaymode();return;}
 anchor.position=race.PlayerCourse[index].transform.position;anchor.rotation=Quaternion.identity;bike.RespawnBike(anchor);step=4;until=EditorApplication.timeSinceStartup+2;
 }catch(System.Exception e){File.AppendAllText("Logs/Race-ground-check.txt",e.ToString());running=false;EditorApplication.ExitPlaymode();}
 }
}}
#endif
