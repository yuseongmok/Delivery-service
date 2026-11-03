#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;
namespace DeliveryService.Mok.Editor {
public static class RecoveryPrologueUpgrade {
 const string Folder="Assets/Scripts/Mok/Prologue/Art/Recovery";
 static Material Mat(string name,Color color){var path=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.color=color;EditorUtility.SetDirty(m);return m;}
 static void Backup(UnityEngine.SceneManagement.Scene scene,string stamp){if(!EditorSceneManager.SaveScene(scene,"Logs/Recovery-before-"+scene.name+"-"+stamp+".unity",true))throw new Exception("Backup failed");}
 static Transform[] Car(Transform root){
 foreach(Transform child in root.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
 if(root.GetComponent<MeshRenderer>())UnityEngine.Object.DestroyImmediate(root.GetComponent<MeshRenderer>());
 if(root.GetComponent<MeshFilter>())UnityEngine.Object.DestroyImmediate(root.GetComponent<MeshFilter>());
 var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonCity/Prefabs/Vehicles/SM_Veh_Car_Sedan_01.prefab");
 var visual=(GameObject)PrefabUtility.InstantiatePrefab(prefab,root);visual.name="조직 세단 외형";visual.transform.localPosition=Vector3.zero;visual.transform.localRotation=Quaternion.identity;
 foreach(var c in visual.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(c);
 return visual.GetComponentsInChildren<Transform>().Where(t=>t.name.Contains("Wheel_")).ToArray();
 }
 static void Piece(Transform root,string name,Vector3 at,Vector3 scale,Material material){var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.SetParent(root,false);o.transform.localPosition=at;o.transform.localScale=scale;o.GetComponent<Renderer>().sharedMaterial=material;var rb=o.AddComponent<Rigidbody>();rb.mass=2;rb.isKinematic=true;}
 public static void Apply(){
 if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit mode required");
 var stamp=DateTime.Now.ToString("yyyyMMdd-HHmmss");Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
 var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(BankPrologueBuilder.ScenePath);if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(BankPrologueBuilder.ScenePath,OpenSceneMode.Additive);Backup(scene,stamp);UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
 var d=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<BankPrologue>(true)).Single();
 File.Copy(AssetDatabase.GetAssetPath(d.story),"Logs/Recovery-story-before-"+stamp+".asset");
 var defaults=ScriptableObject.CreateInstance<PrologueStory>();
 d.story.title=defaults.title;d.story.vault=defaults.vault;d.story.vault.seconds=5;d.story.alarm=defaults.alarm;d.story.alarm.seconds=4;d.story.police=defaults.police;d.story.reinforcement=defaults.reinforcement;d.story.collect=defaults.collect;d.story.driving=defaults.driving;d.story.carConversation=defaults.carConversation;d.story.shopConversation=defaults.shopConversation;d.story.getaway=defaults.getaway;EditorUtility.SetDirty(d.story);UnityEngine.Object.DestroyImmediate(defaults);
 int index=0;
 foreach(var o in d.officers){
  o.name="금고 조직원 "+(++index);
  SyntyStoryUpgrade.Skin(o.transform,"Assets/Synty/PolygonCity/Prefabs/Characters/Character_BusinessMan_Shirt.prefab","RecoveryGang"+index,false,true);
  foreach(var r in o.GetComponentsInChildren<MeshRenderer>(true))if(!r.transform.IsChildOf(o.gun))r.gameObject.SetActive(false);
  var animation=o.GetComponent<PoliceChaseAnimation>();if(animation)animation.enabled=false;
  o.entranceVoice=null;EditorUtility.SetDirty(o);
 }
 // 기존 사람 저지선은 씬에서도 비활성화하고 진행 참조에서 분리한다.
 foreach(var o in d.roadPolice){o.gameObject.SetActive(false);o.name="미사용 도로 인물";}d.roadPolice=Array.Empty<PrologueOfficer>();
 foreach(var c in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ProloguePursuitCar>(true))){
  c.wheels=Car(c.transform);c.name="조직 추격 세단";c.hostileGang=true;c.flashes=Array.Empty<Light>();c.lenses=Array.Empty<Renderer>();
  var light=new GameObject("차량 전조등").AddComponent<Light>();light.transform.SetParent(c.transform,false);light.transform.localPosition=new Vector3(0,.7f,2);light.type=LightType.Spot;light.range=22;light.spotAngle=65;light.intensity=2;c.headlights=light;
  c.siren.clip=SuppliedAudio.Current?SuppliedAudio.Current.playerEngine:null;c.siren.loop=true;c.siren.volume=.07f;EditorUtility.SetDirty(c);
 }
 foreach(var t in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Where(t=>t.name=="SM_Veh_Car_Police_01").ToArray()){Car(t);t.name="조직 주차 세단";}
 foreach(var light in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>(true)).Where(l=>l.name.StartsWith("Entrance police spill"))){light.gameObject.SetActive(false);}
 foreach(var text in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<TextMesh>(true))){
 text.text=text.text.Replace("UNION BANK","UNION STORAGE").Replace("안전한 금융, 가까운 은행","보관 시설 / PRIVATE STORAGE").Replace("고객 안내","시설 안내").Replace("예금 · 출금 · 대출 상담","관계자 외 출입금지").Replace("평일 09:00 — 16:00","출입증을 제시하세요").Replace("오늘의 금융","보안 수칙").Replace("목돈을 위한 첫걸음","허가 없이 금고 접근 금지").Replace("UNION 정기예금","시설 관리실").Replace("ATM  /  현금 서비스","보안 단말 / ACCESS").Replace("금융 상담","시설 관리").Replace("영업시간 09:00 — 16:00","인가 차량 전용").Replace("고객 주차장","하역 주차장").Replace("POLICE LINE","PRIVATE AREA");EditorUtility.SetDirty(text);
 }
 var old=scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="차량 돌파 장애물");if(old)UnityEngine.Object.DestroyImmediate(old);
 var group=new GameObject("차량 돌파 장애물");var wood=Mat("Wood",new Color(.48f,.29f,.14f));var orange=Mat("Warning",new Color(.94f,.45f,.12f));
 foreach(float x in new[]{62f,119f,176f}){
  var root=new GameObject("파손 나무 차단대 "+x).transform;root.SetParent(group.transform);root.position=new Vector3(x,0,-65);root.gameObject.AddComponent<PrologueBreakableBarrier>();
  Piece(root,"나무 가로대",new Vector3(0,.85f,0),new Vector3(.22f,.28f,5),wood);
  Piece(root,"경고 가로대",new Vector3(0,1.25f,0),new Vector3(.18f,.22f,5),orange);
  foreach(float z in new[]{-2f,2f})Piece(root,"지지대",new Vector3(0,.6f,z),new Vector3(.3f,1.2f,.3f),wood);
 }
 EditorUtility.SetDirty(d);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
 // 본편의 사건 대사만 같은 설정으로 맞춘다. 경제·경찰 동작은 변경하지 않는다.
 var main=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/JJinCity.unity");if(!main.isLoaded)main=EditorSceneManager.OpenScene("Assets/Scenes/JJinCity.unity",OpenSceneMode.Additive);Backup(main,stamp);
 foreach(var o in main.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MonoBehaviour>(true)).Where(o=>o is BrokerMission||o is VenueFinale||o is StoryPhoneInbox||o is StoryTutorial))Rewrite(o);
 foreach(var guid in AssetDatabase.FindAssets("t:NightPreparationPlan")){var a=AssetDatabase.LoadAssetAtPath<NightPreparationPlan>(AssetDatabase.GUIDToAssetPath(guid));File.Copy(AssetDatabase.GetAssetPath(a),"Logs/Recovery-plan-before-"+stamp+".asset",true);Rewrite(a);}
 EditorSceneManager.MarkSceneDirty(main);EditorSceneManager.SaveScene(main);AssetDatabase.SaveAssets();
 File.WriteAllText("Logs/Recovery-apply.txt","PASS backed up scenes and story; "+index+" gang skins; 3 breakable barriers; no road people; gang pursuit cars and storage signs; dialogue updated.");
 }
 public static string Wording(string s){return s.Replace("은행까지 털었어","다른 조직의 금고까지 들어갔어").Replace("은행을 털었지만","빼앗긴 돈을 회수하러 갔지만").Replace("은행 돈","경쟁 조직의 금고 자금").Replace("은행 일","금고 회수 작전").Replace("은행 의뢰","회수 의뢰");}
 static void Rewrite(UnityEngine.Object o){var so=new SerializedObject(o);var it=so.GetIterator();while(it.NextVisible(true))if(it.propertyType==SerializedPropertyType.String&&it.stringValue.Contains("은행"))it.stringValue=Wording(it.stringValue);so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(o);}
 public static void Refine(){
 var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(BankPrologueBuilder.ScenePath);Backup(scene,DateTime.Now.ToString("yyyyMMdd-HHmmss"));
 var filters=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshFilter>(true)).Where(f=>AssetDatabase.GetAssetPath(f.sharedMesh).Contains("SM_Veh_Car_Police_01.fbx")).ToArray();
 foreach(var f in filters.Where(f=>!filters.Any(other=>other!=f&&f.transform.IsChildOf(other.transform))).ToArray()){var root=f.transform;Car(root);root.name="조직 경비 세단";}
 EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);File.WriteAllText("Logs/Recovery-refine.txt","Replaced remaining police vehicle meshes: "+filters.Length);
 }
 public static void Preview(){
 var scene=EditorSceneManager.OpenScene(BankPrologueBuilder.ScenePath,OpenSceneMode.Single);var d=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<BankPrologue>(true)).Single();
 var enemy=d.officers[0];bool active=enemy.gameObject.activeSelf;enemy.gameObject.SetActive(true);enemy.animator.Rebind();enemy.animator.Update(0);
 var cam=new GameObject("Recovery preview").AddComponent<Camera>();cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.1f,.13f,.17f);
 void Shot(Vector3 pos,Vector3 look,string name){cam.transform.position=pos;cam.transform.LookAt(look);var rt=new RenderTexture(1280,900,24);cam.targetTexture=rt;cam.Render();var old=RenderTexture.active;RenderTexture.active=rt;var t=new Texture2D(1280,900,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,1280,900),0,0);t.Apply();File.WriteAllBytes("Logs/Recovery-"+name+".png",t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);RenderTexture.active=old;cam.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
 Shot(enemy.transform.position+new Vector3(2,1.5f,3),enemy.transform.position+Vector3.up,"gang");Shot(new Vector3(52,4,-69),new Vector3(62,1,-65),"barrier");
 enemy.gameObject.SetActive(active);UnityEngine.Object.DestroyImmediate(cam.gameObject);
 }
 public static void Inspect(){
 var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(BankPrologueBuilder.ScenePath);
 if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(BankPrologueBuilder.ScenePath,OpenSceneMode.Additive);
 var d=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<BankPrologue>(true)).Single();
 string log="scene="+scene.path+" dirty="+scene.isDirty+"\n";
 foreach(var r in d.officers[0].GetComponentsInChildren<Renderer>(true))log+="OFFICER "+r.name+" "+r.GetType().Name+" enabled="+r.enabled+"\n";
 foreach(var c in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ProloguePursuitCar>(true)))foreach(var r in c.GetComponentsInChildren<Renderer>(true))log+="CAR "+r.name+" mesh="+(r.GetComponent<MeshFilter>()?AssetDatabase.GetAssetPath(r.GetComponent<MeshFilter>().sharedMesh):"")+"\n";
 foreach(var t in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<TextMesh>(true)))log+="TEXT "+t.name+" "+t.text+"\n";
 foreach(var t in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)))if(t.name.IndexOf("police",StringComparison.OrdinalIgnoreCase)>=0||t.name.IndexOf("barrier",StringComparison.OrdinalIgnoreCase)>=0)log+="OBJECT "+t.name+" "+t.position+"\n";
 File.WriteAllText("Logs/Recovery-inspect.txt",log);
 }
}}
#endif
