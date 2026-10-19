using UnityEngine;using UnityEngine.UI;using UnityEngine.EventSystems;
namespace DeliveryService.Mok {
// 메뉴 선택에는 작은 이동과 설정된 효과음 볼륨만 적용한다.
public sealed class LobbyMenuFeedback:MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,ISelectHandler,IDeselectHandler {
 public LobbyController director; bool focused; AudioSource source;static AudioClip tone;
 void Awake(){source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=0;if(!tone){int count=2646;var data=new float[count];for(int i=0;i<count;i++){float t=i/44100f;data[i]=Mathf.Sin(t*900*6.28318f)*Mathf.Sin(Mathf.PI*i/count)*.09f;}tone=AudioClip.Create("메뉴 선택",count,1,44100,false);tone.SetData(data,0);}}
 void Update(){transform.localScale=Vector3.Lerp(transform.localScale,Vector3.one*(focused?1.025f:1),Time.unscaledDeltaTime*16);}
 void Focus(bool value){if(value&&!focused&&director&&!director.IsLoading)source.PlayOneShot(tone,director.audioSettings.SfxVolume);focused=value;}
 public void OnPointerEnter(PointerEventData e){Focus(true);}public void OnPointerExit(PointerEventData e){Focus(false);}public void OnSelect(BaseEventData e){Focus(true);}public void OnDeselect(BaseEventData e){Focus(false);}
 void OnDisable(){focused=false;transform.localScale=Vector3.one;}
}
}
