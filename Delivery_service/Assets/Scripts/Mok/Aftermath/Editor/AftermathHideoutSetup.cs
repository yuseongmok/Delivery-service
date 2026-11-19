#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using Object=UnityEngine.Object;
namespace DeliveryService.Mok.Editor
{
 public static partial class AftermathSetup
 {
 public static void BuildHideout(){try{
 var scene=EditorSceneManager.OpenScene(ScenePath);EditorSceneManager.SaveScene(scene,"Logs/Aftermath-before-hideout-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
 film=Object.FindFirstObjectByType<AftermathFilm>();root=film.transform;
 if(film.hideout)throw new Exception("Hideout already exists; keep authored edits.");
 dark=AssetDatabase.LoadAssetAtPath<Material>(Folder+"차콜.mat");timber=AssetDatabase.LoadAssetAtPath<Material>(Folder+"오래된 나무.mat");gold=AssetDatabase.LoadAssetAtPath<Material>(Folder+"황동.mat");
 var plaster=Mat("비밀 창고 회벽",new Color(.24f,.28f,.27f));var concrete=Mat("비밀 창고 콘크리트",new Color(.2f,.21f,.2f));
 var set=At("01B 민기만의 비밀 창고",root,Vector3.zero);var o=new Vector3(500,0,0);
 Box("콘크리트 바닥",set,o+new Vector3(0,-.12f,0),new Vector3(8,.24f,8),concrete);
 Box("천장",set,o+new Vector3(0,3.2f,0),new Vector3(8,.18f,8),dark);
 foreach(float x in new[]{-4f,4f})Box("측면 회벽",set,o+new Vector3(x,1.6f,0),new Vector3(.22f,3.2f,8),plaster);
 Box("입구 벽",set,o+new Vector3(0,1.6f,-4),new Vector3(8,3.2f,.22f),plaster);
 var back=Prop("Assets/Synty/PolygonHeist/Prefab/Environment/SM_Env_Wall_Interior_Ground_01.prefab",set,o+new Vector3(0,0,3.85f),3.2f,180);var rb=back.GetComponentsInChildren<Renderer>();var bounds=rb[0].bounds;foreach(var r in rb)bounds.Encapsulate(r.bounds);var sc=back.transform.localScale;sc.x*=8/bounds.size.x;back.transform.localScale=sc;
 foreach(float x in new[]{-3.7f,3.7f})Box("철제 기둥",set,o+new Vector3(x,1.6f,3.65f),new Vector3(.13f,3.2f,.13f),dark);
 for(int i=0;i<3;i++)Box("천장 들보",set,o+new Vector3(0,3.05f,-2+i*2.5f),new Vector3(7.8f,.15f,.14f),dark);
 Prop("Assets/Synty/PolygonHeist/Prefab/Props/SM_Prop_Shelf_01.prefab",set,o+new Vector3(-2.7f,0,2.8f),2.3f);
 Prop("Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Shelf_03.prefab",set,o+new Vector3(3,0,2.7f),2.2f,180);
 for(int i=0;i<5;i++)Prop("Assets/Synty/PolygonCity/Prefabs/Props/SM_Prop_CardboardBox_0"+(1+i%4)+".prefab",set,o+new Vector3(-2.6f+(i%2)*.65f,(i/2)*.4f,2.8f),.38f,i*23);
 Prop("Assets/Synty/PolygonCity/Prefabs/Props/SM_Prop_Pallet_01.prefab",set,o+new Vector3(2.6f,0,-.2f),.15f);
 Prop("Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Crate_Preset_01.prefab",set,o+new Vector3(2.6f,.15f,-.2f),1.15f,90);
 Prop("Assets/Synty/PolygonHeist/Prefab/Environment/SM_Env_Door_02.prefab",set,o+new Vector3(-3.8f,0,-1.8f),2.4f,90);
 Prop("Assets/Synty/PolygonHeist/Prefab/Environment/SM_Env_Ceiling_Light_01.prefab",set,o+new Vector3(.1f,2.85f,.8f),.15f);
 LightAt(set,o+new Vector3(.1f,2.65f,.8f),new Color(1,.76f,.43f),3.2f,6);
 LightAt(set,o+new Vector3(-3,1.8f,-2),new Color(.35f,.52f,1),.7f,5);
 // A proper enclosed cabinet hides the money until Mingi opens its hinged door.
 Box("보관함 뒤판",set,o+new Vector3(.6f,.95f,2.45f),new Vector3(1.5f,1.4f,.08f),timber);
 foreach(float x in new[]{-.15f,1.35f})Box("보관함 측판",set,o+new Vector3(x,.95f,2),new Vector3(.08f,1.4f,.95f),timber);
 foreach(float y in new[]{.25f,.65f,1.65f})Box("보관함 선반",set,o+new Vector3(.6f,y,2),new Vector3(1.5f,.08f,.95f),timber);
 var hinge=At("보관함 문 경첩",set,o+new Vector3(1.35f,.25f,1.5f));
 Box("보관함 문",hinge,o+new Vector3(.6f,.95f,1.5f),new Vector3(1.5f,1.4f,.07f),timber);
 for(int i=0;i<5;i++)Box("문 판재 이음",hinge,o+new Vector3(-.02f+i*.3f,.95f,1.455f),new Vector3(.018f,1.32f,.012f),dark);
 Box("문 손잡이",hinge,o+new Vector3(-.01f,1.13f,1.42f),new Vector3(.055f,.16f,.055f),gold);
 var grip=At("문을 잡는 손",hinge,o+new Vector3(-.01f,1.13f,1.4f));
 var sourceMingi=film.shots[0].motions.First(m=>m.actor&&m.actor.name=="민기").actor;
 var mingi=Object.Instantiate(sourceMingi,set);mingi.name="민기 · 비밀 창고";mingi.transform.SetPositionAndRotation(o+new Vector3(-.45f,0,1.1f),Quaternion.Euler(0,38,0));mingi.reach=0;
 var player=Object.Instantiate(film.firstPerson.forestPlayer,set);player.name="플레이어 · 비밀 창고";player.transform.SetPositionAndRotation(o+new Vector3(-1.4f,0,-.7f),Quaternion.identity);player.reach=0;
 var oldBag=film.shots[3].motions.First(m=>!m.actor).subject;
 var bag=Object.Instantiate(oldBag,set);bag.name="민기의 비상금 가방";bag.position=o+new Vector3(.6f,.7f,2);bag.rotation=Quaternion.identity;
 var left=bag.GetComponentsInChildren<Transform>().First(t=>t.name=="돈가방 왼손");var right=bag.GetComponentsInChildren<Transform>().First(t=>t.name=="돈가방 오른손");
 left.position=bag.position+new Vector3(-.22f,.15f,0);right.position=bag.position+new Vector3(.22f,.15f,0);
 var h=root.gameObject.AddComponent<AftermathHideout>();film.hideout=h;h.mingi=mingi;h.cabinetDoor=hinge;h.doorGrip=grip;h.bag=bag;h.leftGrip=left;h.rightGrip=right;h.hiddenBag=bag.position;h.recoveredBag=o+new Vector3(.1f,.95f,1.45f);film.firstPerson.hideoutPlayer=player;
 for(int i=1;i<=3;i++){
 var s=film.shots[i];s.set=set.gameObject;s.motions=new[]{Move(mingi.transform,mingi.transform.position,mingi.transform.position,38,mingi),Move(player.transform,player.transform.position,player.transform.position,0,player)};
 s.lookAt.position=o+(i==1?new Vector3(.3f,1.35f,2):i==2?new Vector3(.35f,1,1.7f):new Vector3(-.45f,1.4f,1.1f));s.lookFrom=null;s.night=true;
 }
 film.dialogue.nightCard="그날 밤 · 민기의 비밀 창고";
 film.dialogue.lines[1].seconds=4;film.dialogue.lines[1].subtitle="민기: 예전에 혼자 쓰던 창고야. 여긴 아무도 몰라.";
 film.dialogue.lines[2].seconds=7;film.dialogue.lines[2].subtitle="민기: 조직에도 말 안 했어. 잡히기 전에 금고에서 챙긴 돈을 여기 숨겨뒀지.";
 film.shots[2].name=film.dialogue.lines[2].scene="비밀 보관함에서 돈가방 꺼내기";
 var oldCrate=film.shots[0].set.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="돈을 감춘 목재 상자");if(oldCrate)Object.DestroyImmediate(oldCrate.gameObject);Object.DestroyImmediate(oldBag.gameObject);
 foreach(var s in film.shots)s.set.SetActive(false);film.shots[0].set.SetActive(true);film.ApplyShot(0,0);
 EditorUtility.SetDirty(film.dialogue);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);File.WriteAllText("Logs/Aftermath-hideout.txt","Saved separate hideout set at x500, first-person viewpoint, hinged cabinet and bag retrieval. Original city scenes untouched.");AftermathReview.Record();
 }catch(Exception e){File.WriteAllText("Logs/Aftermath-hideout.txt",e.ToString());EditorApplication.Exit(1);}}
 }
}
#endif
