using UnityEngine;
using UnityEngine.UI;
namespace DeliveryService.Mok {
// 피해가 확정된 곳에서 호출한다. 레그돌 상태나 무기 종류와 무관하게 같은 표시를 사용한다.
public sealed class CombatHitMarker : MonoBehaviour {
 public int ConfirmedHits {get;private set;}
 public bool Fatal {get;private set;}
 public float Remaining=>Mathf.Max(0,until-Time.time);
 CanvasGroup group;Image[] strokes;float until;
 public static void Confirm(PlayerCombat player,bool fatal){
  if(!player)return;
  var marker=player.GetComponent<CombatHitMarker>();if(!marker)marker=player.gameObject.AddComponent<CombatHitMarker>();
  marker.ConfirmedHits++;marker.Fatal=fatal;marker.until=Time.time+.2f;
  foreach(var stroke in marker.strokes)stroke.color=fatal?new Color(1,.47f,.16f):new Color(1,.95f,.79f);
  marker.Refresh();
 }
 void Awake(){
  var root=new GameObject("Combat hit confirmation",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(CanvasGroup));root.transform.SetParent(transform,false);
  var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=100;
  var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
  group=root.GetComponent<CanvasGroup>();group.blocksRaycasts=false;group.interactable=false;group.alpha=0;
  strokes=new Image[4];int i=0;
  foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1}){
   var go=new GameObject("Hit tick",typeof(RectTransform),typeof(Image));go.transform.SetParent(root.transform,false);
   var image=go.GetComponent<Image>();image.raycastTarget=false;var rect=image.rectTransform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.sizeDelta=new Vector2(2,7);rect.anchoredPosition=new Vector2(x*8,y*8);rect.localRotation=Quaternion.Euler(0,0,-x*y*45);strokes[i++]=image;
  }
 }
 void Refresh(){if(group)group.alpha=Time.timeScale<=0||StoryCutsceneHUD.Active?0:Mathf.Clamp01(Remaining/.08f);}
 void Update()=>Refresh();
 void OnDisable(){until=0;if(group)group.alpha=0;}
}
}
