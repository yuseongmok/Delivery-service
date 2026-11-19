#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using Object=UnityEngine.Object;
namespace DeliveryService.Mok.Editor
{
 public static class AftermathNewsPolish
 {
 public static void Apply(){try{
 var scene=EditorSceneManager.OpenScene("Assets/Scenes/PrisonAftermath.unity");EditorSceneManager.SaveScene(scene,"Logs/Aftermath-before-news-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
 var f=Object.FindFirstObjectByType<AftermathFilm>();
 f.shots[4].lookAt.position=new Vector3(400.1f,.82f,0);f.shots[4].fieldOfView=36;
 f.shots[5].lookAt.position=new Vector3(399.85f,.85f,-.05f);f.shots[5].fieldOfView=36;
 var transforms=f.shots[7].set.GetComponentsInChildren<Transform>(true);
 var frame=transforms.First(t=>t.name=="TV 프레임");var screen=transforms.First(t=>t.name=="TV 화면 고정 위치");var canvas=transforms.First(t=>t.name=="TV 뉴스 화면");var bracket=transforms.First(t=>t.name=="TV 천장 고정 브래킷");
 var position=new Vector3(-2,1.95f,5.6f);var rotation=Quaternion.Euler(0,-38,0);
 frame.SetPositionAndRotation(position,rotation);frame.localScale=new Vector3(2.25f,1.3125f,.16f);
 screen.SetPositionAndRotation(position+rotation*new Vector3(0,0,-.091f),rotation);screen.localScale=Vector3.one*1.25f;
 bracket.position=new Vector3(-2,3.32f,5.6f);bracket.localScale=new Vector3(.08f,1.5f,.08f);
 foreach(var light in f.shots[7].set.GetComponentsInChildren<Light>(true))if(light.color.b>light.color.r*1.3f)light.transform.position=position+rotation*new Vector3(0,0,-.4f);
 f.shots[7].lookAt.position=position;f.shots[8].lookAt.position=position+Vector3.down*.08f;
 f.shots[9].lookFrom=f.shots[8].lookAt;f.shots[9].lookAt.position=new Vector3(-.25f,.8f,4.1f);f.shots[9].fieldOfView=50;
 f.shots[10].lookFrom=f.shots[9].lookAt;
 f.dialogue.lines[7].subtitle="뉴스: 탈옥한 두 수감자는 수 주째 행방이 확인되지 않고 있습니다.";f.dialogue.lines[7].seconds=4.2f;
 f.dialogue.lines[8].subtitle="뉴스: 경찰은 신분을 바꿔 도피했을 가능성을 수사하고 있습니다.";f.dialogue.lines[8].seconds=4.2f;f.shots[8].subtitleTail=.2f;
 f.dialogue.lines[9].seconds=2.2f;
 f.newsHeadline.fontSize=31;f.newsTicker.fontSize=23;
 EditorUtility.SetDirty(f.dialogue);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
 File.WriteAllText("Logs/Aftermath-news-polish.txt","Saved desk-only dealer framing, closer angled TV, shorter two-beat bulletin and first-person downward reaction before Mingi calls.");AftermathReview.Record();
 }catch(Exception e){File.WriteAllText("Logs/Aftermath-news-polish.txt",e.ToString());EditorApplication.Exit(1);}}
 }
}
#endif
