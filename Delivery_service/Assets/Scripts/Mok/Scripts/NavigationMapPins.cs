using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
namespace DeliveryService.Mok.Navigation {
public sealed class NavigationMapPins : MonoBehaviour {
 public Camera mapCamera;
 public NavigationManager manager;
 public Font font;
 public bool selectable;
 class Pin {public NavigationPlace place;public RectTransform rect;public Text text;public Image image;public bool player;public bool delivery;}
 readonly List<Pin> pins=new List<Pin>();
 RectTransform area;
 public void Rebuild(){font=DeliveryService.Mok.MokUIFont.Font;foreach(var p in pins)if(p.rect!=null)Destroy(p.rect.gameObject);pins.Clear();area=GetComponent<RectTransform>();foreach(var place in FindObjectsByType<NavigationPlace>(FindObjectsSortMode.None))Make(place.Symbol,place.Tint,place,false,false);Make("D",new Color(.14f,.66f,.61f),null,false,true);Make("^",new Color(.05f,.42f,.76f),null,true,false);}
 void Start(){Rebuild();}
 void Make(string symbol,Color color,NavigationPlace place,bool player,bool delivery){var go=new GameObject("MapPin "+symbol,typeof(RectTransform),typeof(Image));go.layer=gameObject.layer;go.transform.SetParent(transform,false);var rt=go.GetComponent<RectTransform>();rt.anchorMin=rt.anchorMax=new Vector2(.5f,.5f);rt.sizeDelta=Vector2.one*(selectable?34:32);var img=go.GetComponent<Image>();img.color=color;img.raycastTarget=selectable&&!player;
 var label=new GameObject("Symbol",typeof(RectTransform),typeof(Text));label.layer=gameObject.layer;label.transform.SetParent(go.transform,false);var lr=label.GetComponent<RectTransform>();lr.anchorMin=Vector2.zero;lr.anchorMax=Vector2.one;lr.sizeDelta=Vector2.zero;var text=label.GetComponent<Text>();text.font=font;text.text=symbol;text.fontSize=selectable?21:22;text.color=Color.white;text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;
 if(selectable&&!player){var button=go.AddComponent<Button>();button.targetGraphic=img;button.onClick.AddListener(()=>{if(delivery)manager.ResumeDelivery();else manager.SelectPlace(place);});}
 pins.Add(new Pin{place=place,rect=rt,text=text,image=img,player=player,delivery=delivery});}
 void LateUpdate(){if(area==null||mapCamera==null||manager==null)return;foreach(var pin in pins){bool show;Vector3 position;
 if(pin.player){show=manager.NavigationTarget!=null;position=show?manager.NavigationTarget.position:Vector3.zero;}
 else if(pin.delivery){show=manager.CurrentDestination!=null;position=show?manager.CurrentDestination.transform.position:Vector3.zero;}
 else {show=pin.place!=null&&pin.place.IsAvailable;position=show?pin.place.MapPosition:Vector3.zero;}
 var uv=mapCamera.WorldToViewportPoint(position);show=show&&uv.z>0&&uv.x>.025f&&uv.x<.975f&&uv.y>.025f&&uv.y<.975f;pin.rect.gameObject.SetActive(show);if(!show)continue;pin.rect.anchoredPosition=new Vector2((uv.x-.5f)*area.rect.width,(uv.y-.5f)*area.rect.height);
 if(pin.player){float angle=manager.NavigationTarget.eulerAngles.y-mapCamera.transform.eulerAngles.y;pin.rect.localRotation=Quaternion.Euler(0,0,-angle);}else pin.rect.localRotation=Quaternion.identity;
 }}
}
}

