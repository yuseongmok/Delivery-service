#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using UnityEngine.UI;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad]public static class FeedbackInstaller {
static FeedbackInstaller(){EditorApplication.update+=Poll;}
static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/Mok-feedback-install"))return;File.Delete("Temp/Mok-feedback-install");try{Run();}catch(Exception e){File.WriteAllText("Temp/Mok-feedback-install.txt",e.ToString());Debug.LogException(e);}}
static void Run(){
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/JJinCity.unity")throw new Exception("Open JJinCity");
if(!File.Exists("Temp/JJinCity-before-feedback.unity")&&!EditorSceneManager.SaveScene(scene,"Temp/JJinCity-before-feedback.unity",true))throw new Exception("Backup failed");
var ui=UnityEngine.Object.FindObjectsByType<MoneyUI>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(x=>x.moneyText!=null&&x.moneyText.gameObject.activeInHierarchy);
if(ui==null)throw new Exception("No active balance UI");
var feedback=ui.GetComponent<IncomeFeedback>();if(feedback==null)feedback=ui.gameObject.AddComponent<IncomeFeedback>();feedback.balanceText=ui.moneyText;
if(feedback.popupText==null){var popup=new GameObject("Mok Income Popup",typeof(RectTransform),typeof(Text));popup.transform.SetParent(ui.moneyText.transform.parent,false);var t=popup.GetComponent<Text>();var rect=t.rectTransform;var balance=ui.moneyText.rectTransform;rect.anchorMin=balance.anchorMin;rect.anchorMax=balance.anchorMax;rect.pivot=balance.pivot;rect.sizeDelta=new Vector2(Mathf.Max(350,balance.sizeDelta.x),55);rect.anchoredPosition=balance.anchoredPosition+Vector2.up*55;t.font=ui.moneyText.font;t.fontSize=Mathf.Max(22,ui.moneyText.fontSize);t.alignment=TextAnchor.MiddleRight;t.color=new Color(.45f,1,.65f);t.raycastTarget=false;t.text="";feedback.popupText=t;}
var popupRect=feedback.popupText.rectTransform;var balanceRect=ui.moneyText.rectTransform;
popupRect.anchorMin=balanceRect.anchorMin;popupRect.anchorMax=balanceRect.anchorMax;popupRect.pivot=balanceRect.pivot;popupRect.sizeDelta=new Vector2(balanceRect.sizeDelta.x,55);popupRect.anchoredPosition=balanceRect.anchoredPosition+new Vector2(-45,45);feedback.popupText.resizeTextForBestFit=true;feedback.popupText.resizeTextMinSize=18;feedback.popupText.resizeTextMaxSize=ui.moneyText.fontSize;
var audio=ui.GetComponent<AudioSource>();if(audio==null)audio=ui.gameObject.AddComponent<AudioSource>();audio.playOnAwake=false;audio.spatialBlend=0;
feedback.incomeSound=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Scripts/Mok/Art/Feedback/Income.wav");if(feedback.incomeSound==null)throw new Exception("Income sound missing");EditorUtility.SetDirty(feedback);
var player=UnityEngine.Object.FindFirstObjectByType<PlayerCombat>();var fade=player.GetComponent<HospitalFade>();if(fade==null)fade=player.gameObject.AddComponent<HospitalFade>();
if(fade.overlay==null){var root=new GameObject("Mok Hospital Blackout",typeof(RectTransform),typeof(Canvas),typeof(CanvasGroup));var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=32760;
var panel=new GameObject("Black",typeof(RectTransform),typeof(Image));panel.transform.SetParent(root.transform,false);var image=panel.GetComponent<Image>();image.color=Color.black;image.raycastTarget=false;image.rectTransform.anchorMin=Vector2.zero;image.rectTransform.anchorMax=Vector2.one;image.rectTransform.offsetMin=Vector2.zero;image.rectTransform.offsetMax=Vector2.zero;
fade.overlay=root.GetComponent<CanvasGroup>();fade.overlay.alpha=0;fade.overlay.blocksRaycasts=false;}
player.hospitalFade=fade;EditorUtility.SetDirty(player);EditorUtility.SetDirty(fade);
if(UnityEngine.Object.FindFirstObjectByType<PoliceManager>()==null)throw new Exception("Existing police manager missing");
EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
File.WriteAllText("Temp/Mok-feedback-install.txt","PASS: income popup/audio and blackout saved. Balance="+ui.moneyText.name+", rect="+ui.moneyText.rectTransform.anchoredPosition+", police manager found");
}
}}
#endif
