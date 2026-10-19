using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DeliveryService.Mok;
public class InteractHoverManager : MonoBehaviour {
 public Camera playerCamera;public float interactDistance=3f;public LayerMask interactLayers;
 public Color outlineColor=Color.white;[Range(0,10)]public float outlineWidth=4;
 public GameObject interactUIContainer;public Text interactText;
 Transform currentTarget;readonly List<Outline> activeOutlines=new List<Outline>();
 PlayerInteraction interaction;PlayerCombat combat;float nextCheck;
 public Transform CurrentTarget=>currentTarget;
 void Start(){interaction=GetComponentInParent<PlayerInteraction>();if(interaction==null&&playerCamera!=null)interaction=playerCamera.GetComponentInParent<PlayerInteraction>();combat=interaction!=null?interaction.GetComponent<PlayerCombat>():null;ClearHighlight();}
 void Update(){
  if(playerCamera==null||interaction==null||!interaction.isActiveAndEnabled||combat!=null&&!combat.CanAct){ClearHighlight();return;}
  if(Time.unscaledTime<nextCheck)return;nextCheck=Time.unscaledTime+.05f;
  var target=InteractionFocus.Find(interaction);
  if(target!=currentTarget){ClearHighlight();if(target!=null)Highlight(target);}
 }
 void Highlight(Transform target){
  currentTarget=target;
  // 자식 콜라이더를 맞혀도 상호작용 본체의 모델 전체를 강조한다.
  var existing=target.GetComponentsInChildren<Outline>(true);
  if(existing.Length>0){foreach(var line in existing)if(line.gameObject.activeInHierarchy)EnableLine(line);}
  else if(target.GetComponentInChildren<Renderer>()!=null)EnableLine(target.gameObject.AddComponent<Outline>());
  string message="";var thrown=target.GetComponent<ThrownBrick>();
  if(thrown!=null)message="E를 눌러 줍기";
  else {var info=target.GetComponentInParent<InteractMessage>();if(info!=null&&MessageManager.Instance!=null)message=MessageManager.Instance.GetMessage(info.messageID);}
  if(interactText!=null){interactText.font=MokUIFont.Font;interactText.text=message;interactText.gameObject.SetActive(!string.IsNullOrEmpty(message));}
  if(interactUIContainer!=null)interactUIContainer.SetActive(!string.IsNullOrEmpty(message));
 }
 void EnableLine(Outline line){line.OutlineMode=Outline.Mode.OutlineVisible;line.OutlineColor=outlineColor;line.OutlineWidth=outlineWidth;line.enabled=true;activeOutlines.Add(line);}
 void ClearHighlight(){foreach(var line in activeOutlines)if(line!=null)line.enabled=false;activeOutlines.Clear();currentTarget=null;if(interactUIContainer!=null)interactUIContainer.SetActive(false);if(interactText!=null){interactText.text="";interactText.gameObject.SetActive(false);}}
 void OnDisable(){ClearHighlight();}
}
