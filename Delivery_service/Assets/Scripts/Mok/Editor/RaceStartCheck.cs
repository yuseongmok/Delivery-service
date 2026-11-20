#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad] public static class RaceStartCheck {
 static bool running;static int step;static double until;static RaceManager race;static RacerNPCInteraction racer;static Vector3 idlePosition;static float originalFov;static int stops;static int recoveries,tows;static Vector3 safe;static Camera playerView;static EditorWindow gameView;static bool oldMax;
 static RaceStartCheck(){EditorApplication.update+=Run;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool("RaceTestLayout",false)){EditorWindow.GetWindow(System.Type.GetType("UnityEditor.GameView,UnityEditor")).maximized=SessionState.GetBool("RaceTestOldMax",false);SessionState.SetBool("RaceTestLayout",false);gameView=null;}};EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("RaceStartCheck",false)){SessionState.SetBool("RaceStartCheck",false);running=true;step=0;until=EditorApplication.timeSinceStartup+2;}};}
 static void Run(){
 if(!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling&&File.Exists("Temp/RaceStartCheck")){File.Delete("Temp/RaceStartCheck");gameView=EditorWindow.GetWindow(System.Type.GetType("UnityEditor.GameView,UnityEditor"));oldMax=gameView.maximized;SessionState.SetBool("RaceTestOldMax",oldMax);SessionState.SetBool("RaceTestLayout",true);gameView.maximized=true;SessionState.SetBool("RaceStartCheck",true);EditorApplication.EnterPlaymode();return;}
 if(!running||!EditorApplication.isPlaying||EditorApplication.timeSinceStartup<until)return;
 try{
 if(step==0){step=1;SceneManager.LoadScene("JJinCity");until=EditorApplication.timeSinceStartup+8;return;}
 if(step==1){racer=Object.FindFirstObjectByType<RacerNPCInteraction>();idlePosition=racer.transform.position;step=10;until=EditorApplication.timeSinceStartup+4;return;}
 if(step==10){
 if(Object.FindObjectsByType<RacerNPCInteraction>(FindObjectsSortMode.None).Length!=1||racer.transform.position.x<230)throw new System.Exception("Fixed new-town racer missing/duplicated");
 var cameraObject=new GameObject("Barrier check camera");var shot=cameraObject.AddComponent<Camera>();shot.transform.position=new Vector3(-73,3,-5);shot.transform.LookAt(new Vector3(-72,.7f,2));
 var raceObjects=Object.FindFirstObjectByType<RaceManager>().objects;raceObjects.SetActive(true);PrisonChickenCheck.Capture(shot,"Logs/Race-barriers-polished.png");raceObjects.SetActive(false);Object.Destroy(cameraObject);
 File.WriteAllText("Logs/Racer-bind-diagnostic.txt","pos="+racer.transform.position+" enabled="+racer.enabled+" active="+racer.gameObject.activeInHierarchy+" control="+typeof(RacerNPCInteraction).GetField("npcControl",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(racer)+" parent="+racer.transform.parent+" agentUpdate="+racer.GetComponent<UnityEngine.AI.NavMeshAgent>().updatePosition+"\n");
 foreach(var c in racer.GetComponents<MonoBehaviour>())File.AppendAllText("Logs/Racer-bind-diagnostic.txt",c.GetType().Name+" enabled="+c.enabled+"\n");
 var player=Object.FindFirstObjectByType<PlayerCombat>();var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.transform.position=racer.transform.position+racer.transform.forward*2.2f+Vector3.up*1.1f;cc.enabled=true;
 playerView=player.View;originalFov=playerView.fieldOfView;racer.StartMissionDialogue(new[]{"레이서: 승부할 준비는 됐나?"});step=11;until=EditorApplication.timeSinceStartup+2;return;}
 if(step==11){PrisonChickenCheck.Capture(playerView,"Logs/Racer-conversation-diagnostic.png");if(!racer.IsDialogueActive||playerView.fieldOfView>originalFov)throw new System.Exception("Conversation failed active="+racer.IsDialogueActive+" fov="+playerView.fieldOfView+" original="+originalFov+" NPC down="+racer.GetComponent<NPCControl>().IsKnockedDown);if(!RaceDialogueOverlay.Active||racer.DialoguePanel.GetComponent<CanvasGroup>().alpha!=0)throw new System.Exception("Legacy dialogue panel still visible");
 PrisonChickenCheck.Capture(playerView,"Logs/Racer-conversation.png");ScreenCapture.CaptureScreenshot("Logs/Racer-subtitle-ui.png");step=111;until=EditorApplication.timeSinceStartup+1;return;}
 if(step==111){racer.EndDialogue();idlePosition=racer.transform.position;step=12;until=EditorApplication.timeSinceStartup+2;return;}
 if(step==12){if(Vector3.Distance(idlePosition,racer.transform.position)>.08f||Mathf.Abs(playerView.fieldOfView-originalFov)>.1f)throw new System.Exception("Conversation did not restore idle/view");race=Object.FindFirstObjectByType<RaceManager>();race.StartRace(Object.FindFirstObjectByType<RacerNPCInteraction>()?.transform);step=2;until=EditorApplication.timeSinceStartup+2;return;}
 var bike=Object.FindFirstObjectByType<MotorcycleController>();var cam=bike.bikeCamera.GetComponentInChildren<Camera>();
 if(step==2){PrisonChickenCheck.Capture(cam,"Logs/Race-start-opponent.png");File.WriteAllText("Logs/Race-start-check.txt","Opponent shot active="+RaceStartPresentation.Active+" mounted="+bike.isDriven+"\n");step=3;until=EditorApplication.timeSinceStartup+6;return;}
 if(step==3){PrisonChickenCheck.Capture(cam,"Logs/Race-start-first-person.png");File.AppendAllText("Logs/Race-start-check.txt","First person restored="+!RaceStartPresentation.Active+" race="+race.IsRaceActive+"\n");step=4;until=EditorApplication.timeSinceStartup+7;return;}
 if(step==4){
 var hud=race.GetComponent<RaceDrivingHUD>();if(!hud||!hud.Visible)throw new System.Exception("Race HUD not visible after countdown");
 if(race.PassedCheckpoints!=0)throw new System.Exception("HUD changed checkpoint progress");
 ScreenCapture.CaptureScreenshot("Logs/Race-driving-hud.png");step=5;until=EditorApplication.timeSinceStartup+1;return;}
 if(step==5){stops=0;step=6;}
 if(step==6&&stops++<5){var rb=bike.sphereRB;var hits=Physics.RaycastAll(rb.position+Vector3.up*3,Vector3.down,12,~0,QueryTriggerInteraction.Ignore);File.AppendAllText("Logs/Race-stop-survey.txt",stops+" sphere="+rb.position+" velocity="+rb.linearVelocity+" active="+rb.gameObject.activeInHierarchy+" trigger="+bike.sphereCollider.isTrigger+" driven="+bike.isDriven+"\n");foreach(var h in hits)File.AppendAllText("Logs/Race-stop-survey.txt","  "+h.collider.name+" "+h.point+" ignore="+Physics.GetIgnoreCollision(h.collider,bike.sphereCollider)+" layer="+h.collider.gameObject.layer+"\n");until=EditorApplication.timeSinceStartup+1;return;}
 if(step==6){var recovery=race.GetComponent<RaceRecovery>();recoveries=recovery.Recoveries;safe=bike.sphereRB.position;tows=Object.FindFirstObjectByType<WaterRescue>().TowCharges;bike.sphereRB.position+=Vector3.down*3;Physics.SyncTransforms();step=7;until=EditorApplication.timeSinceStartup+2;return;}
 if(step==7){if(race.GetComponent<RaceRecovery>().Recoveries!=recoveries+1||!bike.isDriven||!race.IsRaceActive||Vector3.Distance(safe,bike.sphereRB.position)>.3f||Object.FindFirstObjectByType<WaterRescue>().TowCharges!=tows)throw new System.Exception("Fall recovery failed count="+race.GetComponent<RaceRecovery>().Recoveries+" before="+recoveries+" pos="+bike.sphereRB.position+" safe="+safe+" driven="+bike.isDriven);File.AppendAllText("Logs/Race-start-check.txt","PASS forced fall: restored bike+rider, no tow, race preserved\n");}
 race.OnCheckpointReached(race.PlayerCourse[1]);if(race.PassedCheckpoints!=0)throw new System.Exception("Out of order checkpoint accepted");
 race.OnCheckpointReached(race.PlayerCourse[0]);if(race.PassedCheckpoints!=1)throw new System.Exception("Checkpoint HUD data did not advance");
 if(RaceDrivingHUD.Progress(race.PlayerStart.position,race.PlayerStart,race.PlayerCourse,0)!=0||RaceDrivingHUD.Progress(race.PlayerStart.position,race.PlayerStart,race.PlayerCourse,race.PlayerCourse.Length)!=1)throw new System.Exception("Course progress normalization failed");
 File.AppendAllText("Logs/Race-start-check.txt","PASS checkpoint order preserved, HUD advances to 1, course progress normalized\n");
 PrisonChickenCheck.Capture(cam,"Logs/Race-start-go.png");File.AppendAllText("Logs/Race-start-check.txt","Start sequence finished; race active="+race.IsRaceActive+"\nFINISHED");
 var mission=Object.FindFirstObjectByType<MechanicMission>();typeof(MechanicMission).GetField("state",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(mission,MechanicMission.Phase.Racing);
 foreach(var checkpoint in race.PlayerCourse)race.OnCheckpointReached(checkpoint);race.OnNpcFinished();
 if(!race.ShowingResult)throw new System.Exception("Result not reached");
 typeof(RaceManager).GetMethod("ConfirmResultAndEndRace",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(race,null);
 if(race.IsRaceActive||mission.State!=MechanicMission.Phase.Return||!mission.PartRecovered)throw new System.Exception("Mechanic handoff failed");
 File.AppendAllText("Logs/Race-start-check.txt","\nPASS result confirmation -> mechanic return with parts\nFINISHED");running=false;EditorApplication.ExitPlaymode();
 }catch(System.Exception e){File.WriteAllText("Logs/Race-start-check.txt",e.ToString());running=false;EditorApplication.ExitPlaymode();}
 }
}}
#endif
