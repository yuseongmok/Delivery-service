#if UNITY_EDITOR
using System;using System.Collections;using System.Linq;using UnityEngine;using UnityEngine.UI;using UnityEngine.SceneManagement;using UnityEngine.EventSystems;
namespace DeliveryService.Mok.Editor {
public static class UiSceneTest {
public static IEnumerator RunCore(Action<string> log){
float until=Time.unscaledTime+2;while(Time.unscaledTime<until)yield return null;
void Check(bool ok,string s){if(!ok)throw new Exception(s);log("PASS: "+s);}
Check(!SceneManager.GetSceneByPath("Assets/Scenes/Woong1.unity").isLoaded,"Woong1 중복 로드 없음");
Check(UnityEngine.Object.FindObjectsByType<PlayerMove>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length==1,"플레이어 1개");
Check(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(x=>x.enabled)==1,"오디오 리스너 1개");
Check(UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Count(x=>x.enabled)==1,"이벤트 시스템 1개");
Check(!UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Any(x=>x.enabled&&x.text=="New Text"),"화면에 기본 New Text 표시 없음");
Check(UnityEngine.Object.FindFirstObjectByType<CompactHUD>()!=null,"정상 게임 HUD 유지");
ScreenCapture.CaptureScreenshot("Temp/Mok-ui-scene-fixed.png");yield return null;yield return null;
}}
}
#endif
