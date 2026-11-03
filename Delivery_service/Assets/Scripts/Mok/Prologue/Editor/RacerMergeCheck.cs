#if UNITY_EDITOR
using System;
using System.Linq;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad] public static class RacerMergeCheck {
 const string PrefabPath="Assets/Scripts/Min/Prefab/NPC/RacerNPC.prefab";
 static RacerMergeCheck(){EditorApplication.update+=Request;}
 static void Request(){
  if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||!File.Exists("Temp/RacerMerge-apply"))return;
  File.Delete("Temp/RacerMerge-apply");
  try{Apply();}catch(Exception e){File.WriteAllText("Logs/RacerMerge-apply.txt",e.ToString());}
 }
 static void Apply(){
  var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/JJinCity.unity");
  if(!scene.isLoaded)scene=EditorSceneManager.OpenScene("Assets/Scenes/JJinCity.unity",OpenSceneMode.Additive);
  EditorSceneManager.SaveScene(scene,"Logs/RacerMerge-scene-before-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
  var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
  if(!prefab||!prefab.GetComponent<RacerNPCInteraction>())throw new Exception("Racer prefab failed to import");
  var text=File.ReadAllText(PrefabPath);
  foreach(System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(text,@"guid: ([0-9a-f]{32})"))
   if(string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(match.Groups[1].Value)))throw new Exception("Missing racer dependency: "+match.Groups[1].Value);
  var managers=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<NPCManager>(true)).ToArray();
  var player=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<PlayerCombat>(true)).First();
  var manager=managers.Where(m=>m.gameObject.activeInHierarchy&&m.enabled).OrderBy(m=>(m.transform.position-player.transform.position).sqrMagnitude).First();
  var data=new SerializedObject(manager);data.FindProperty("racerNPCPrefab").objectReferenceValue=prefab;data.ApplyModifiedPropertiesWithoutUndo();
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  File.WriteAllText("Logs/RacerMerge-apply.txt","PASS prefab references resolved; one racer assigned to "+manager.name+"; scene saved with prior-state backup");
 }
 public static void Verify(Action<bool,string> check){
  var racer=UnityEngine.Object.FindFirstObjectByType<RacerNPCInteraction>();
  check(racer!=null,"racer spawns from saved scene manager");if(!racer)return;
  var player=UnityEngine.Object.FindFirstObjectByType<PlayerCombat>();
  var original=player.transform.position;var controller=player.GetComponent<CharacterController>();bool wasController=controller&&controller.enabled;
  if(controller)controller.enabled=false;player.transform.position=racer.transform.position+racer.transform.forward*1.5f;
  Cursor.lockState=CursorLockMode.Locked;
  var npc=racer.GetComponent<NPCControl>();bool external=npc.ExternalControl;
  var phone=UnityEngine.Object.FindFirstObjectByType<PhoneController>();if(phone)phone.ClosePhone();
  var move=player.GetComponent<PlayerMove>();bool controllable=!move||move.isControllable;if(move)move.isControllable=true;
  check(player.CanAct,"racer test player ready: enabled="+player.enabled+" down="+player.IsDown+" bike="+(player.RiddenBike!=null)+" physical="+(player.avatar&&player.avatar.IsPhysical)+" hit="+player.CanReceiveHit+" scale="+Time.timeScale);
  racer.Interact(player.GetComponent<ToppingInventory>());
  check(racer.IsDialogueActive&&npc.ExternalControl,"F interaction opens racer dialogue and pauses wandering");
  check(!racer.GetComponent<CitizenCombat>().CanBePickpocketed,"racer dialogue does not conflict with pickpocket");
  racer.EndDialogue();check(!racer.IsDialogueActive&&npc.ExternalControl==external&&Cursor.lockState==CursorLockMode.Locked,"closing dialogue restores control");
  racer.Interact(player.GetComponent<ToppingInventory>());racer.enabled=false;
  check(!racer.IsDialogueActive&&Cursor.lockState==CursorLockMode.Locked,"disabling racer safely releases dialogue controls");racer.enabled=true;
  player.transform.position=original;if(controller)controller.enabled=wasController;
  if(move)move.isControllable=controllable;
 }
}}
#endif
