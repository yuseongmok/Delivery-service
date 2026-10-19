using System.Collections.Generic;using UnityEngine;
namespace DeliveryService.Mok {
public sealed class WeaponMerchant:MonoBehaviour,IInteractable {
 PlayerCombat focusPlayer;
 void Start(){focusPlayer=FindFirstObjectByType<PlayerCombat>();}
 PlayerWeapon player;bool open;int selected;readonly Dictionary<Behaviour,bool> paused=new();CursorLockMode oldLock;bool oldVisible;
 GameObject stage,model;Camera preview;RenderTexture texture;string notice="";
 public bool IsOpen=>open;
 public void Interact(ToppingInventory inventory){if(open)return;var combat=inventory.GetComponent<PlayerCombat>();if(combat==null||!combat.CanAct)return;
  player=inventory.GetComponent<PlayerWeapon>();if(player==null)player=inventory.gameObject.AddComponent<PlayerWeapon>();Open();}
 public void Open(){if(player==null||open)return;open=true;oldLock=Cursor.lockState;oldVisible=Cursor.visible;
  foreach(var b in player.GetComponentsInChildren<MonoBehaviour>(true))if(b is PlayerMove||b is PlayerMovement||b is CameraMove||b is PlayerInteraction||b is PlayerInteract){paused[b]=b.enabled;b.enabled=false;}
  var phone=FindFirstObjectByType<PhoneController>();if(phone!=null){paused[phone]=phone.enabled;phone.enabled=false;}
  Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
  stage=new GameObject("Weapon preview studio");stage.transform.position=new Vector3(0,-10000,0);preview=new GameObject("Preview camera",typeof(Camera)).GetComponent<Camera>();preview.transform.SetParent(stage.transform,false);preview.transform.localPosition=new Vector3(0,.5f,-2.3f);preview.transform.LookAt(stage.transform.position+Vector3.up*.5f);preview.clearFlags=CameraClearFlags.SolidColor;preview.backgroundColor=new Color(.055f,.09f,.12f);preview.cullingMask=1<<31;preview.fieldOfView=38;preview.nearClipPlane=.05f;preview.farClipPlane=8;
  texture=new RenderTexture(640,640,24);preview.targetTexture=texture;var l=new GameObject("Studio light",typeof(Light)).GetComponent<Light>();l.transform.SetParent(stage.transform,false);l.transform.localPosition=new Vector3(-1,2,-1);l.type=LightType.Point;l.range=6;l.intensity=8;l.cullingMask=1<<31;ShowModel();}
 void ShowModel(){if(model!=null)Destroy(model);model=Instantiate(WeaponCatalog.Model((WeaponKind)selected),stage.transform);model.transform.localPosition=new Vector3(0,.18f,0);model.transform.localRotation=Quaternion.Euler(0,selected==3?65:20,0);foreach(var t in model.GetComponentsInChildren<Transform>())t.gameObject.layer=31;var renderers=model.GetComponentsInChildren<Renderer>();if(renderers.Length>0){var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);model.transform.position+=stage.transform.position+Vector3.up*.5f-bounds.center;preview.transform.localPosition=new Vector3(0,.5f,-Mathf.Max(.5f,bounds.size.magnitude*1.7f));}notice="";}
 void Update(){if(!open)return;if(player==null||player.Combat.IsDown||PoliceControl.IsArrestInProgress){Close();return;}Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
  float wheel=Input.mouseScrollDelta.y;if(wheel!=0){selected=(selected+(wheel>0?1:3))%4;ShowModel();}if(model!=null)model.transform.Rotate(0,25*Time.unscaledDeltaTime,0);if(Input.GetKeyDown(KeyCode.Escape))Close();}
 public void Close(){if(!open)return;open=false;foreach(var p in paused)if(p.Key!=null)p.Key.enabled=p.Value;paused.Clear();Cursor.lockState=oldLock;Cursor.visible=oldVisible;if(stage!=null)Destroy(stage);if(texture!=null){texture.Release();Destroy(texture);}}
 void OnDisable(){Close();}
 void OnGUI(){using(DeliveryService.Mok.PerformanceMarkers.Merchant.Auto()){if(!open){var p=focusPlayer;if(p!=null&&p.CanAct&&Physics.Raycast(p.View.transform.position,p.View.transform.forward,out var h,3)&&h.collider.GetComponentInParent<WeaponMerchant>()==this)GUI.Label(new Rect(Screen.width/2-150,Screen.height-100,300,40),"E 무기점 둘러보기",new GUIStyle(GUI.skin.label){font=MokUIFont.Font,fontSize=22,alignment=TextAnchor.MiddleCenter});return;}var previous=GUI.matrix;var previousColor=GUI.color;var previousBackground=GUI.backgroundColor;GUI.color=Color.white;float scale=Mathf.Min(Screen.width/1100f,Screen.height/720f);GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1100*scale)/2,(Screen.height-720*scale)/2),Quaternion.identity,Vector3.one*scale);
  var box=new GUIStyle(GUI.skin.box){font=MokUIFont.Font,fontSize=24};var label=new GUIStyle(GUI.skin.label){font=MokUIFont.Font,fontSize=25,wordWrap=true};var button=new GUIStyle(GUI.skin.button){font=MokUIFont.Font,fontSize=25};
  GUI.color=new Color(.045f,.075f,.10f,1);GUI.DrawTexture(new Rect(40,40,1020,640),Texture2D.whiteTexture);GUI.color=Color.white;button.normal.background=Texture2D.whiteTexture;button.normal.textColor=new Color(.06f,.09f,.12f);button.hover.background=Texture2D.whiteTexture;button.hover.textColor=button.normal.textColor;button.active.background=Texture2D.whiteTexture;button.active.textColor=button.normal.textColor;GUI.backgroundColor=new Color(1,.54f,.22f);GUI.Label(new Rect(80,65,800,50),"골목 무기점",new GUIStyle(label){fontSize=36});if(GUI.Button(new Rect(940,65,80,40),"닫기",button))Close();
  if(texture!=null)GUI.DrawTexture(new Rect(80,140,450,450),texture,ScaleMode.ScaleToFit);
  GUI.Label(new Rect(570,155,430,50),WeaponCatalog.Names[selected],new GUIStyle(label){fontSize=36});GUI.Label(new Rect(570,225,430,90),WeaponCatalog.Description[selected],label);
  GUI.Label(new Rect(570,325,430,45),$"{WeaponCatalog.Prices[selected]:N0}원",label);GUI.Label(new Rect(570,375,430,55),$"내 돈  {(MoneyManager.Instance!=null?MoneyManager.Instance.currentMoney:0):N0}원",label);
  if(GUI.Button(new Rect(570,445,430,55),player.HasWeapon?"현재 무기 교체 구매":"구매하기",button))notice=player.Purchase((WeaponKind)selected)?"구매 완료! 기존 무기는 교체됩니다.":"잔액이 부족합니다.";
  if(selected==3&&GUI.Button(new Rect(570,515,430,50),"탄약 10발 · 3,000원",button))notice=player.BuyAmmo()?"탄약 10발 구매 완료":player.Equipped!=WeaponKind.Pistol?"권총을 먼저 구매하세요.":"잔액 부족 또는 탄약 보유 한도입니다.";
  GUI.Label(new Rect(80,605,430,45),"마우스 휠로 무기 둘러보기",new GUIStyle(label){fontSize=20});GUI.Label(new Rect(570,585,430,70),notice,new GUIStyle(label){fontSize=20});GUI.matrix=previous;GUI.color=previousColor;GUI.backgroundColor=previousBackground;}}
}
}
