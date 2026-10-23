#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Collections.Generic;
using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using UnityEngine.UI;
namespace DeliveryService.Mok.Editor {
public static class StoryCompanionInstaller {
 const string Dir="Assets/Scripts/Mok/Art/StoryCharacters/";
 public static void Inspect(){var lines=new List<string>();foreach(var b in UnityEngine.Object.FindObjectsByType<MotorcycleController>(FindObjectsSortMode.None)){lines.Add(b.name+" root="+b.transform.position+" scale="+b.transform.lossyScale);foreach(var r in b.GetComponentsInChildren<Renderer>(true))if(r is MeshRenderer)lines.Add(r.name+" center="+b.transform.InverseTransformPoint(r.bounds.center)+" size="+r.bounds.size);}File.WriteAllLines("Logs/Companion-bike-inspect.txt",lines);}
 static void SaveMesh(Mesh mesh,string name){string path=Dir+name+".asset";var prior=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(prior){EditorUtility.CopySerialized(mesh,prior);UnityEngine.Object.DestroyImmediate(mesh);}else AssetDatabase.CreateAsset(mesh,path);}
 static Material[] Palette(string name,Color color){return Enumerable.Range(0,3).Select(i=>{string path=Dir+name+i+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.color=color*(i==0?1:i==1?.82f:.64f);m.SetFloat("_Smoothness",.05f);m.SetColor("_EmissionColor",m.color*.20f);m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;m.EnableKeyword("_EMISSION");EditorUtility.SetDirty(m);return m;}).ToArray();}
 // 8각 단면과 모서리 면을 사용하는 작은 로우폴리 메시.
 static Mesh Facet(){var vertices=new List<Vector3>();var triangles=Enumerable.Range(0,3).Select(_=>new List<int>()).ToArray();Vector2[] outline={new(-.32f,-.5f),new(.32f,-.5f),new(.5f,-.32f),new(.5f,.32f),new(.32f,.5f),new(-.32f,.5f),new(-.5f,.32f),new(-.5f,-.32f)};
  Vector3 P(int ring,int i){var p=outline[i]*(ring==0||ring==3?.78f:1);return new(p.x,p.y,ring==0?-.5f:ring==1?-.35f:ring==2?.35f:.5f);}
  void Face(int color,params Vector3[] points){int start=vertices.Count;vertices.AddRange(points);for(int i=1;i<points.Length-1;i++)triangles[color].AddRange(new[]{start,start+i,start+i+1});}
  for(int ring=0;ring<3;ring++)for(int i=0;i<8;i++){int j=(i+1)%8;Face(i>=3&&i<=5?0:i<2?2:1,P(ring,i),P(ring,j),P(ring+1,j),P(ring+1,i));}
  Face(1,Enumerable.Range(0,8).Select(i=>P(3,i)).ToArray());Face(2,Enumerable.Range(0,8).Reverse().Select(i=>P(0,i)).ToArray());var mesh=new Mesh{name="Faceted hand section"};mesh.SetVertices(vertices);mesh.subMeshCount=3;for(int i=0;i<3;i++)mesh.SetTriangles(triangles[i],i);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
 }
 static void Part(Transform parent,string name,Vector3 position,Vector3 size,Material[] materials){var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=size;go.layer=2;go.GetComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Dir+"FacetedHand.asset");go.GetComponent<MeshRenderer>().sharedMaterials=materials;go.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;}
 static void Hands(PlayerAvatar avatar){SaveMesh(Facet(),"FacetedHand");var skin=Palette("Punch skin ",new(.68f,.46f,.31f));var sleeve=Palette("Punch sleeve ",new(.76f,.29f,.10f));foreach(var root in new[]{avatar.leftFist,avatar.rightFist}){foreach(var child in root.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);Part(root,"Closed palm",Vector3.zero,new(.09f,.067f,.09f),skin);for(int i=0;i<4;i++)Part(root,"Curled finger "+i,new(-.033f+i*.022f,.008f,.036f),new(.021f,.052f,.04f),skin);float sign=root==avatar.leftFist?1:-1;Part(root,"Folded thumb",new(sign*.039f,-.022f,.005f),new(.027f,.028f,.065f),skin);Part(root,"Wrist",new(0,0,-.06f),new(.059f,.054f,.06f),skin);Part(root,"Jacket cuff",new(0,-.02f,-.15f),new(.08f,.15f,.08f),sleeve);}EditorUtility.SetDirty(avatar);}
 static void NoHat(Transform root){
  foreach(var t in root.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Mingi orange work cap"||t.name=="Mingi cap brim").ToArray())UnityEngine.Object.DestroyImmediate(t.gameObject);
 }
 static void Color(Text text){if(text&&!text.GetComponent<StorySpeakerColors>())text.gameObject.AddComponent<StorySpeakerColors>();if(text){text.color=UnityEngine.Color.white;text.supportRichText=true;EditorUtility.SetDirty(text);}}
 public static void RenameCharacter(){
  var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
  if(scene.path!=StoryTutorialBuilder.Main)throw new Exception("Open JJinCity");
  if(!EditorSceneManager.SaveScene(scene,"Logs/JJinCity-before-character-rename-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new Exception("Backup failed");
  int changed=0;
  // 저장된 대사와 미션 데이터의 이름만 교체한다.
  void Replace(UnityEngine.Object target){
   if(!target)return;
   var so=new SerializedObject(target);var it=so.GetIterator();bool dirty=false;
   while(it.Next(true))if(it.propertyType==SerializedPropertyType.String){
    string before=it.stringValue;string after=before.Replace("\uC7A5\uD0DC\uC2DD","박진웅").Replace("\uC7A5\uD0DC\uC11D","박진웅");
    if(before!=after){it.stringValue=after;dirty=true;changed++;}
   }
   if(dirty){so.ApplyModifiedProperties();EditorUtility.SetDirty(target);}
  }
  foreach(var root in scene.GetRootGameObjects())foreach(var c in root.GetComponentsInChildren<MonoBehaviour>(true))Replace(c);
  foreach(var guid in AssetDatabase.FindAssets("t:NightPreparationPlan"))Replace(AssetDatabase.LoadAssetAtPath<NightPreparationPlan>(AssetDatabase.GUIDToAssetPath(guid)));
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
  File.WriteAllText("Logs/Character-rename.txt","PASS renamed serialized fields: "+changed);
 }
 public static void Install(){
  var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!=StoryTutorialBuilder.Main)throw new Exception("Open JJinCity");if(!EditorSceneManager.SaveScene(scene,"Logs/JJinCity-before-companion-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new Exception("Backup failed");
  var m=UnityEngine.Object.FindFirstObjectByType<BrokerMission>();var tutorial=UnityEngine.Object.FindFirstObjectByType<StoryTutorial>();NoHat(tutorial.actor);Color(tutorial.subtitle);Color(m.subtitle);Color(m.GetComponent<FirstWantedLesson>().subtitle);Hands(m.player.avatar);
  m.passenger=m.broker.GetComponent<BrokerPassenger>()??m.broker.gameObject.AddComponent<BrokerPassenger>();m.passenger.mission=m;m.passenger.bikes=UnityEngine.Object.FindObjectsByType<MotorcycleController>(FindObjectsSortMode.None);m.passenger.seats=m.passenger.bikes.Select(b=>{var seat=b.transform.Find("Story passenger seat");if(!seat){seat=new GameObject("Story passenger seat").transform;seat.SetParent(b.transform,false);}seat.localPosition=new Vector3(0,.82f,-.47f);seat.localRotation=Quaternion.identity;return seat;}).ToArray();EditorUtility.SetDirty(m.passenger);EditorUtility.SetDirty(m);
  var inbox=m.GetComponent<StoryPhoneInbox>();inbox.mingi=tutorial.actor;
  if(!inbox.nearbyPanel){var lesson=m.GetComponent<FirstWantedLesson>();inbox.nearbyPanel=UnityEngine.Object.Instantiate(lesson.panel,m.transform);inbox.nearbyPanel.name="Mingi nearby conversation";inbox.nearbySubtitle=inbox.nearbyPanel.GetComponentInChildren<Text>(true);inbox.nearbyPanel.SetActive(false);}Color(inbox.nearbySubtitle);
  inbox.invitation="실장이 연락했어. 은행 일의 배후를 알고 있대. 낮에는 가게를 지키고, 18시 이후에 만나 보자. 위치와 준비 내용은 휴대폰 미션 앱에 정리해 뒀어.";EditorUtility.SetDirty(inbox);
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  var bank=EditorSceneManager.OpenScene(BankPrologueBuilder.ScenePath,OpenSceneMode.Additive);if(!EditorSceneManager.SaveScene(bank,"Logs/Bank-before-companion-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new Exception("Bank backup failed");var d=bank.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<BankPrologue>(true)).Single();NoHat(d.accomplice.transform);NoHat(d.shopArrival.actor);Color(d.subtitle);EditorSceneManager.MarkSceneDirty(bank);EditorSceneManager.SaveScene(bank);EditorSceneManager.CloseScene(bank,true);AssetDatabase.SaveAssets();File.WriteAllText("Logs/Companion-install.txt","PASS saved hat removal, speaker colours, polygon fists, passenger seat and nearby dialogue");
 }
}
}
#endif
