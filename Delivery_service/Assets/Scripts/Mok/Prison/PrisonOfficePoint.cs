using System.Collections;
using UnityEngine;
namespace DeliveryService.Mok {
// 보관함을 연 뒤 내용물을 확보한다. 조사 순서는 자유이며 암구호는 다시 읽을 수 있다.
public sealed class PrisonOfficePoint:MonoBehaviour,IInteractable {
 public enum Kind{Uniforms,VehicleKey,Password}
 public Kind kind;
 public PrisonOfficeSearch mission;
 public Transform movingPart;
 public GameObject contents;
 public Vector3 slide=new Vector3(0,0,-.4f);
 public bool Opened{get;private set;}
 bool moving;
 public void Interact(ToppingInventory inventory){TryInteract();}
 public bool TryInteract(){
  if(!mission.Active||moving||mission.GetComponent<PrisonOfficePickup>().Busy||Time.timeScale<=0)return false;
  var p=mission.previous.previous.prison.player;if(!p.CanAct||Vector3.Distance(p.transform.position,transform.position)>3)return false;
  if(kind==Kind.Password){mission.Collect(kind);return true;}
  if(mission.Found(kind))return false;
  if(!Opened){StartCoroutine(Open());return true;}
  StartCoroutine(mission.GetComponent<PrisonOfficePickup>().Take(this));return true;
 }
 IEnumerator Open(){moving=true;PrisonAudio.Clink(transform.position,.15f);var start=movingPart.localPosition;for(float t=0;t<.55f;t+=Time.deltaTime){movingPart.localPosition=start+slide*Mathf.SmoothStep(0,1,t/.55f);yield return null;}movingPart.localPosition=start+slide;Opened=true;moving=false;mission.Say(mission.dialogue.opened);}
}}
