#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad]
public static class LobbyModeBuilder {
    static readonly Color Ink=new Color32(18,29,39,255),Paper=new Color32(247,239,222,255),Orange=new Color32(255,158,70,255);
    static Font font;
    static Sprite rounded;
    static LobbyModeBuilder(){EditorApplication.update+=Poll;}
    static void Poll(){
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/Mok-mode-cards-request"))return;
        bool restore=File.ReadAllText("Temp/Mok-mode-cards-request").Trim()=="restore"; File.Delete("Temp/Mok-mode-cards-request");
        try{if(restore)RestoreOriginal();Apply();File.WriteAllText("Temp/Mok-mode-cards-result.txt","PASS: Lobby saved; story -> BankPrologue, challenge disabled, settings retained.");}
        catch(Exception e){File.WriteAllText("Temp/Mok-mode-cards-result.txt",e.ToString());Debug.LogException(e);}
    }
    static RectTransform Rect(Transform p,string name,float x,float y,float w,float h){
        var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(p,false);Place(r,x,y,w,h);return r;
    }
    static void Place(RectTransform r,float x,float y,float w,float h){r.anchorMin=new Vector2(x,y);r.anchorMax=new Vector2(x+w,y+h);r.offsetMin=r.offsetMax=Vector2.zero;r.localScale=Vector3.one;}
    static Image Panel(Transform p,string name,float x,float y,float w,float h,Color color){var r=Rect(p,name,x,y,w,h);var i=r.gameObject.AddComponent<Image>();i.sprite=rounded;i.type=Image.Type.Sliced;i.color=color;i.raycastTarget=false;return i;}
    static Text Label(Transform p,string name,string value,float x,float y,float w,float h,int size,Color color,TextAnchor align=TextAnchor.MiddleLeft){var t=Rect(p,name,x,y,w,h).gameObject.AddComponent<Text>();t.font=font;t.text=value;t.fontSize=size;t.color=color;t.alignment=align;t.raycastTarget=false;return t;}
    static void Card(Button b,LobbyController c,int index,string title,string description,string action,Color accent){
        foreach(Transform child in b.transform.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
        b.gameObject.SetActive(true);Place((RectTransform)b.transform,.135f+index*.25f,.205f,.23f,.56f);
        var img=b.GetComponent<Image>();img.sprite=rounded;img.type=Image.Type.Sliced;img.color=Paper;img.raycastTarget=true;
        var border=b.GetComponent<UnityEngine.UI.Outline>();if(border)UnityEngine.Object.DestroyImmediate(border);
        b.colors=new ColorBlock{normalColor=Color.white,highlightedColor=new Color(1,.91f,.78f),selectedColor=new Color(1,.91f,.78f),pressedColor=new Color(.8f,.75f,.65f),disabledColor=new Color(.66f,.68f,.7f),colorMultiplier=1,fadeDuration=.12f};
        var field=Panel(b.transform,"Art field",.025f,.355f,.95f,.625f,Ink);
        Panel(b.transform,"Accent line",.075f,.90f,.12f,.006f,accent);
        Label(b.transform,"Index","0"+(index+1),.76f,.865f,.16f,.08f,22,accent,TextAnchor.MiddleRight);
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Scripts/Mok/Art/Lobby/"+new[]{"ModeStory","ModeShop","ModeSettings"}[index]+".png"); if(texture){field.gameObject.AddComponent<Mask>().showMaskGraphic=false;var picture=Rect(field.transform,"Game artwork",0,0,1,1).gameObject.AddComponent<RawImage>();picture.texture=texture;picture.raycastTarget=false;}else{var icon=Rect(b.transform,"Mode icon",.22f,.43f,.56f,.41f).gameObject.AddComponent<LobbyModeIcon>();icon.kind=index;icon.color=accent;icon.raycastTarget=false;}
        Label(b.transform,"Title",title,.075f,.245f,.85f,.10f,36,Ink);
        
        Panel(b.transform,"Action rule",.075f,.125f,.85f,.002f,new Color(.15f,.2f,.23f,.18f));
        Label(b.transform,"Action",action,.075f,.027f,.85f,.075f,22,index==1?new Color(.4f,.43f,.45f):new Color(.62f,.29f,.08f));
        var feedback=b.GetComponent<LobbyMenuFeedback>()??b.gameObject.AddComponent<LobbyMenuFeedback>();feedback.director=c;
        if(index==1){b.interactable=false;feedback.enabled=false;}
    }
    static void RestoreOriginal(){
        var current=SceneManager.GetSceneByPath("Assets/Scenes/Lobby.unity");
        if(current.isLoaded)EditorSceneManager.SaveScene(current,"../.codex-work/lobby-mode-cards/Lobby-before-navigation-fix.unity",true);
        File.Copy("../.codex-work/lobby-mode-cards/Lobby-before-cards.unity","Assets/Scenes/Lobby.unity",true);
        AssetDatabase.ImportAsset("Assets/Scenes/Lobby.unity",ImportAssetOptions.ForceUpdate);
        EditorSceneManager.OpenScene("Assets/Scenes/Lobby.unity",OpenSceneMode.Single);
    }
    [MenuItem("Tools/Delivery Service/Mok/Apply Mode Cards")]
    public static void Apply(){
        var scene=SceneManager.GetSceneByPath("Assets/Scenes/Lobby.unity");if(!scene.isLoaded)scene=EditorSceneManager.OpenScene("Assets/Scenes/Lobby.unity",OpenSceneMode.Additive);
        EditorSceneManager.SaveScene(scene,"../.codex-work/lobby-mode-cards/Lobby-before-action-art.unity",true);
        var c=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<LobbyController>(true)).Single();
        font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/BMJUA_ttf.ttf");rounded=AssetDatabase.LoadAllAssetsAtPath("Assets/Scripts/Yun/ShopUI/Prefabs/ShopPcRounded.asset").OfType<Sprite>().First();
        var canvas=c.menu.GetComponentInParent<Canvas>();
        foreach(var name in new[]{"Mode selection","Mode backdrop"}){var old=canvas.transform.Find(name);if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);}
        var back=Rect(canvas.transform,"Mode backdrop",0,0,1,1);back.SetAsFirstSibling();var raw=back.gameObject.AddComponent<RawImage>();raw.raycastTarget=false;
        raw.material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Scripts/Mok/Art/Lobby/LobbySoftBackground.mat");
        var blur=back.gameObject.AddComponent<LobbyModeBackdrop>();blur.source=c.lobbyCamera;blur.display=raw;
        Panel(back,"Dark wash",0,0,1,1,new Color(.025f,.04f,.055f,.64f));
        var modes=Rect(canvas.transform,"Mode selection",0,0,1,1);modes.SetSiblingIndex(c.settingsPanel.transform.GetSiblingIndex());
        c.modeMenu=modes.gameObject.AddComponent<CanvasGroup>();c.modeBackdrop=back.gameObject;
        Label(modes,"Mode heading","플레이",.135f,.79f,.4f,.09f,48,Paper);
        
        var logo=c.menu.transform.Find("Integrated Logo");if(logo){var copy=UnityEngine.Object.Instantiate(logo.gameObject,modes);copy.SetActive(true);Place((RectTransform)copy.transform,.735f,.80f,.13f,.13f);}
        var buttons=new Button[3];
        for(int i=0;i<3;i++){var g=Rect(modes,new[]{"Story mode","Challenge mode","Mode settings"}[i],0,0,1,1).gameObject;g.AddComponent<Image>();buttons[i]=g.AddComponent<Button>();}
        Card(buttons[0],c,0,"스토리 모드","민기와 함께 시작하는 이야기","이야기 시작  →",Orange);
        Card(buttons[1],c,1,"도전 모드","나만의 피자 매장 경영","준비 중",new Color32(156,179,170,255));
        Card(buttons[2],c,2,"사용자 설정","소리와 화면을 내 취향대로","설정 열기  →",new Color32(137,184,203,255));
        c.storyButton=buttons[0];c.modeSettingsButton=buttons[2];
        var returnPanel=Panel(modes,"Return to lobby",.135f,.095f,.14f,.055f,Ink);returnPanel.raycastTarget=true;
        c.modeBackButton=returnPanel.gameObject.AddComponent<Button>();Label(returnPanel.transform,"Label","← 로비로 돌아가기",.07f,0,.9f,1,20,Paper);
        foreach(var text in c.startButton.GetComponentsInChildren<Text>(true))text.text="플레이";
        foreach(var text in c.settingsPanel.GetComponentsInChildren<Text>(true)){text.font=font;text.color=text.GetComponentInParent<Button>(true)?Paper:Ink;}
        c.gameScene="Assets/Scenes/BankPrologue.unity";c.menu.gameObject.SetActive(true);c.menu.alpha=1;c.settingsPanel.SetActive(false);c.fade.alpha=0;
        modes.gameObject.SetActive(false);back.gameObject.SetActive(false);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
    }
}
}
#endif


