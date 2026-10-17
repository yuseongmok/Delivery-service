#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using UnityEngine.SceneManagement;using UnityEngine.UI;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad] public static class LobbyStyle {
 static LobbyStyle(){EditorApplication.update+=Poll;}
 static void Poll(){if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists("Temp/Mok-lobby-style-request"))return;File.Delete("Temp/Mok-lobby-style-request");try{Apply();File.WriteAllText("Temp/Mok-lobby-style.txt","PASS: cinematic bars, rounded shared UI styling, captions removed; saved scene.");}catch(Exception e){File.WriteAllText("Temp/Mok-lobby-style.txt",e.ToString());}}
 static void Round(Image img,Sprite sprite,Color color){img.sprite=sprite;img.type=Image.Type.Sliced;img.pixelsPerUnitMultiplier=1.5f;img.color=color;}
 static void Bar(Transform parent,string name,float min,float max){var t=parent.Find(name);var g=t?t.gameObject:new GameObject(name,typeof(RectTransform),typeof(Image));g.transform.SetParent(parent,false);var r=(RectTransform)g.transform;r.anchorMin=new Vector2(0,min);r.anchorMax=new Vector2(1,max);r.offsetMin=r.offsetMax=Vector2.zero;g.GetComponent<Image>().color=Color.black;g.GetComponent<Image>().raycastTarget=false;g.transform.SetSiblingIndex(1);}
 static void Apply(){var scene=SceneManager.GetSceneByPath("Assets/Scenes/Lobby.unity");if(!scene.isLoaded)scene=EditorSceneManager.OpenScene("Assets/Scenes/Lobby.unity",OpenSceneMode.Additive);EditorSceneManager.SaveScene(scene,"Temp/Mok-lobby-before-style.unity",true);
 var c=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<LobbyController>(true)).Single();var rounded=AssetDatabase.LoadAllAssetsAtPath("Assets/Scripts/Yun/ShopUI/Prefabs/ShopPcRounded.asset").OfType<Sprite>().Single();Color ink=new Color32(18,55,75,255),teal=new Color32(8,158,178,255),paper=new Color32(239,245,246,255);
 foreach(var t in c.menu.GetComponentsInChildren<Text>(true)){if(t.text=="오늘도, 무사히 배달하세요."||t.text=="이얏호 피자  /  영업 준비 중")UnityEngine.Object.DestroyImmediate(t.gameObject);else t.color=Color.white;}
 var accent=c.menu.transform.Find("Accent");if(accent)accent.GetComponent<Image>().color=teal;
 foreach(var b in new[]{c.startButton,c.settingsButton,c.quitButton,c.backButton,c.screenButton}){Round(b.GetComponent<Image>(),rounded,b==c.startButton||b==c.backButton?teal:ink);var colors=b.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(.78f,1,1);colors.selectedColor=colors.highlightedColor;colors.pressedColor=new Color(.55f,.8f,.85f);b.colors=colors;foreach(var t in b.GetComponentsInChildren<Text>())t.color=Color.white;}
 var window=c.settingsPanel.transform.Find("Settings Window");Round(window.GetComponent<Image>(),rounded,paper);foreach(var t in window.GetComponentsInChildren<Text>(true))if(!t.GetComponentInParent<Button>())t.color=ink;
 foreach(var slider in new[]{c.bgm,c.sfx}){Round(slider.fillRect.GetComponent<Image>(),rounded,teal);Round(slider.handleRect.GetComponent<Image>(),rounded,ink);Round(slider.transform.Find("Track").GetComponent<Image>(),rounded,new Color32(200,216,222,255));}
 var canvas=c.menu.transform.parent;Bar(canvas,"Cinema Top",.915f,1);Bar(canvas,"Cinema Bottom",0,.085f);
 EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();}
}
}
#endif
