#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
namespace DeliveryService.Mok.Editor {
public static class TacticalControlCheck {
 public static void Install(){
  foreach(string path in new[]{"Assets/Scenes/BankPrologue.unity","Assets/Scenes/JJinCity.unity"}){
   var scene=SceneManager.GetSceneByPath(path);bool loaded=scene.isLoaded;if(!loaded)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
   if(!EditorSceneManager.SaveScene(scene,"Logs/"+Path.GetFileNameWithoutExtension(path)+"-before-tactical-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new Exception("Scene backup failed");
   foreach(var c in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MonoBehaviour>(true))){
    if(!c)continue;
    if((c is PlayerMove||c is ProloguePlayer)&&!c.GetComponent<PlayerTacticalStance>())c.gameObject.AddComponent<PlayerTacticalStance>();
    // 씬에 저장된 안내와 대사를 바꾸되 에셋 경로나 오브젝트 이름은 건드리지 않는다.
    if(c is UnityEngine.UI.Text||c is TMPro.TMP_Text||c is StoryTutorial){
     var so=new SerializedObject(c);var it=so.GetIterator();while(it.Next(true))if(it.propertyType==SerializedPropertyType.String){string before=it.stringValue;string after=Regex.Replace(before,@"\bE(?=\s*[/·:가-힣]|를|로)","F");if(before!=after)it.stringValue=after;}so.ApplyModifiedPropertiesWithoutUndo();
    }
    if(c is BankPrologue d&&d.health)d.health.gameObject.SetActive(false);
   }
   EditorSceneManager.SaveScene(scene);if(!loaded)EditorSceneManager.CloseScene(scene,true);
  }
  File.WriteAllText("Logs/Tactical-install.txt","Saved scene stance components, hidden health display, F prompts and tutorial dialogue.");
 }
 static void Assert(bool value,string message){if(!value)throw new Exception("Tactical: "+message);File.AppendAllText("Logs/Tactical-check.txt","PASS "+message+"\n");}
 static IEnumerator Hold(Keyboard k,params Key[] keys){float end=Time.realtimeSinceStartup+.45f;while(Time.realtimeSinceStartup<end){InputSystem.QueueStateEvent(k,new KeyboardState(keys));yield return null;}}
 static void Capture(Camera cam,string name){
  var target=new RenderTexture(960,540,24);var old=cam.targetTexture;var active=RenderTexture.active;
  try{cam.targetTexture=target;cam.Render();RenderTexture.active=target;var tex=new Texture2D(960,540,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,960,540),0,0);tex.Apply();File.WriteAllBytes("Logs/Tactical-"+name+".png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);}
  finally{cam.targetTexture=old;RenderTexture.active=active;target.Release();UnityEngine.Object.DestroyImmediate(target);}
 }
 public static IEnumerator VerifyMain(){
  var combat=UnityEngine.Object.FindFirstObjectByType<PlayerCombat>();var weapon=combat.GetComponent<PlayerWeapon>();weapon.Equip(WeaponKind.Pistol);
  var keyboard=InputSystem.AddDevice<Keyboard>();
  try{yield return Verify(combat.gameObject,combat.View,keyboard);Assert(weapon.HasInHand&&weapon.Thrown==null,"aim keeps pistol in hand");}
  finally{InputSystem.RemoveDevice(keyboard);}
 }
 public static IEnumerator Verify(GameObject player,Camera camera,Keyboard keyboard){
  File.WriteAllText("Logs/Tactical-check.txt","");Cursor.lockState=CursorLockMode.Locked;
  var body=player.GetComponent<CharacterController>();var stance=player.GetComponent<PlayerTacticalStance>();
  float height=body.height,fov=camera.fieldOfView;Vector3 center=body.center;Vector3 eye=camera.transform.position;
  yield return Hold(keyboard,Key.LeftCtrl);
  Assert(stance.IsCrouching&&body.height<height-.3f,"Ctrl lowers actual collision capsule");
  Assert(camera.transform.position.y<eye.y-.25f,"Ctrl lowers first-person eye");
  var ceiling=GameObject.CreatePrimitive(PrimitiveType.Cube);ceiling.name="Test low ceiling";ceiling.transform.position=player.transform.TransformPoint(center+Vector3.up*(height*.5f-.2f));ceiling.transform.localScale=new Vector3(1.5f,.15f,1.5f);Physics.SyncTransforms();
  yield return Hold(keyboard);Assert(stance.IsCrouching,"low ceiling prevents standing");UnityEngine.Object.Destroy(ceiling);yield return null;

  yield return Hold(keyboard,Key.LeftCtrl,Key.Q);
  Assert(camera.transform.parent.localRotation.eulerAngles.z>1&&camera.transform.parent.localRotation.eulerAngles.z<30,"Q leans left while crouched");
  yield return Hold(keyboard,Key.LeftCtrl,Key.E);
  Assert(camera.transform.parent.localRotation.eulerAngles.z>330,"E leans right while crouched");
  var mouse=InputSystem.AddDevice<Mouse>();
  try{float end=Time.realtimeSinceStartup+.45f;while(Time.realtimeSinceStartup<end){InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Right));yield return null;}
   Assert(stance.AimAmount>.9f&&camera.fieldOfView<fov*.8f,"right mouse holds gun aim");
   var bank=player.GetComponent<ProloguePlayer>();if(bank)Assert(!bank.director.health.gameObject.activeSelf,"health display hidden without removing health");Capture(camera,bank?"rifle-aim":"pistol-aim");File.WriteAllText("Logs/Tactical-view.txt",string.Join("\n",player.GetComponentsInChildren<Transform>(true).Where(t=>t.name.Contains("Pistol")||t.name=="Slide"||t.GetComponent<Camera>()).Select(t=>t.name+" scale "+t.lossyScale+" pos "+camera.transform.InverseTransformPoint(t.position))));
   InputSystem.QueueStateEvent(mouse,new MouseState());yield return Hold(keyboard);
   Assert(!stance.IsCrouching&&Mathf.Abs(body.height-height)<.05f,"release restores standing capsule");
   Assert(Mathf.Abs(camera.fieldOfView-fov)<.1f,"release restores field of view");
   Vector3 neutral=camera.transform.position;var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="Test lean barrier";wall.transform.SetPositionAndRotation(neutral+player.transform.right*.32f,player.transform.rotation);wall.transform.localScale=new Vector3(.1f,2,2);Physics.SyncTransforms();
   yield return Hold(keyboard,Key.E);Assert(Vector3.Dot(camera.transform.position-neutral,player.transform.right)<.18f,"lean camera stops before adjacent wall");UnityEngine.Object.Destroy(wall);yield return Hold(keyboard);
  }finally{InputSystem.RemoveDevice(mouse);InputSystem.QueueStateEvent(keyboard,new KeyboardState());}
 }
}
}
#endif
