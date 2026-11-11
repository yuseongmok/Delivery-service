using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace DeliveryService.Mok {
// 구치소 대사가 끝나면 감옥 첫 임무 씬으로 연결한다.
public sealed class PrisonPreviewTransition:MonoBehaviour {
 public const string SceneName="PrisonChapter";
 public static bool IsLoading{get;private set;}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void ResetState(){IsLoading=false;}
 public static bool Begin(){if(IsLoading)return true;if(!Application.CanStreamedLevelBeLoaded(SceneName)){Debug.LogError("PrisonChapter 씬을 빌드 씬 목록에 추가해야 합니다.");return false;}IsLoading=true;var go=new GameObject("감옥 첫 임무 전환");DontDestroyOnLoad(go);go.AddComponent<PrisonPreviewTransition>();return true;}
 IEnumerator Start(){var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=32760;var panel=new GameObject("전환 암전",typeof(RectTransform),typeof(Image));panel.transform.SetParent(transform,false);var image=panel.GetComponent<Image>();image.raycastTarget=false;var rect=(RectTransform)panel.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
  for(float t=0;t<.8f;t+=Time.unscaledDeltaTime){image.color=new Color(0,0,0,Mathf.Clamp01(t/.8f));yield return null;}image.color=Color.black;
  yield return SceneManager.LoadSceneAsync(SceneName,LoadSceneMode.Single);Time.timeScale=1;Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;
  yield return null;for(float t=0;t<.8f;t+=Time.unscaledDeltaTime){image.color=new Color(0,0,0,1-Mathf.Clamp01(t/.8f));yield return null;}Destroy(gameObject);
 }
 void OnDestroy(){IsLoading=false;}
}
}
