using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace DeliveryService.Mok.NewCity
{
 // 새 도시 터널 내부에서만 동작한다. 기존 도시 씬과 미션 데이터는 수정하지 않는다.
 public sealed class HarborTunnelTravel : MonoBehaviour
 {
  [SerializeField] string destinationScene="JJinCity";
  [SerializeField] float fadeSeconds=.7f;
  bool loading;
  void OnTriggerEnter(Collider other)
  {
   if(loading)return;
   if(!other.GetComponentInParent<NewCitySurvey>()&&!other.CompareTag("Player"))return;
   if(!Application.CanStreamedLevelBeLoaded(destinationScene)){Debug.LogError("이동할 도시 씬이 빌드 목록에 없습니다.",this);return;}
   loading=true;StartCoroutine(Travel());
  }
  IEnumerator Travel()
  {
   var overlay=new GameObject("도시 이동 암전",typeof(Canvas));DontDestroyOnLoad(overlay);
   var canvas=overlay.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=32760;
   var panel=new GameObject("암전",typeof(RectTransform),typeof(Image));panel.transform.SetParent(overlay.transform,false);
   var rect=panel.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
   var image=panel.GetComponent<Image>();image.color=Color.clear;image.raycastTarget=true;
   // 전환 코루틴은 씬 로드 뒤에도 살아 있는 객체가 수행한다.
   var runner=overlay.AddComponent<HarborTunnelFade>();runner.Begin(destinationScene,fadeSeconds,image);
   yield break;
  }
 }
 public sealed class HarborTunnelFade : MonoBehaviour
 {
  public void Begin(string scene,float seconds,Image image){StartCoroutine(Load(scene,Mathf.Max(.1f,seconds),image));}
  IEnumerator Load(string scene,float seconds,Image image)
  {
   for(float t=0;t<seconds;t+=Time.unscaledDeltaTime){image.color=new Color(0,0,0,t/seconds);yield return null;}
   image.color=Color.black;Time.timeScale=1;
   var operation=SceneManager.LoadSceneAsync(scene,LoadSceneMode.Single);yield return operation;yield return null;
   for(float t=0;t<seconds;t+=Time.unscaledDeltaTime){image.color=new Color(0,0,0,1-t/seconds);yield return null;}
   Destroy(gameObject);
  }
 }
}
