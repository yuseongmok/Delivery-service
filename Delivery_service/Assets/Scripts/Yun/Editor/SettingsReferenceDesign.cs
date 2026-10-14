#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using DeliveryService.Yun.Settings;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DeliveryService.Yun.Editor
{
    public static class SettingsReferenceDesign
    {
        private static readonly Color Ink = new Color32(18,55,75,255);
        private static readonly Color Paper = new Color32(255,252,240,255);
        private static readonly Color Teal = new Color32(8,158,178,255);
        private static readonly Color Red = new Color32(195,61,65,255);
        private static Sprite rounded;
        private static Font font;
        private const string Output = "C:/Users/q2244/.codex/visualizations/2026/09/09/01a08608-c058-7f91-9893-3ff4324de64c";

        public static void RunBatch()
        {
            try
            {
                rounded = AssetDatabase.LoadAllAssetsAtPath("Assets/Scripts/Yun/ShopUI/Prefabs/ShopPcRounded.asset").OfType<Sprite>().Single();
                font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/BMJUA_ttf.ttf");
                foreach (string name in new[] { "JJinCity", "YunCity" })
                {
                    var scene = EditorSceneManager.OpenScene("Assets/Scenes/" + name + ".unity");
                    var panel = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<GameSettingsPanel>(true)).Single();
                    Apply(panel);
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    Verify(panel);
                    if (name == "JJinCity") Preview(panel.transform.Find("Window"));
                    Debug.Log("SETTINGS_DESIGN_PASSED: " + name + ", two sliders and four main buttons, existing events preserved.");
                }
                EditorApplication.Exit(0);
            }
            catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        private static void Apply(GameSettingsPanel panel)
        {
            panel.GetComponent<Image>().color = new Color(.025f,.055f,.085f,.68f);
            var window = (RectTransform)panel.transform.Find("Window");
            Place(window,0,0,840,620);
            window.localScale=Vector3.one;
            Round(window.GetComponent<Image>(),Ink,.45f);
            var shadow=window.GetComponent<Shadow>() ?? window.gameObject.AddComponent<Shadow>();
            shadow.effectColor=new Color(0,0,0,.3f); shadow.effectDistance=new Vector2(0,-7);
            Surface(window,new Color32(239,245,246,255),6);
            var header=Child(window,"Terminal Header",typeof(Image));
            Place(header,0,245,828,118); Round(header.GetComponent<Image>(),Ink,1.5f);
            header.GetComponent<Image>().raycastTarget=false; header.SetSiblingIndex(1);
            TextStyle(window.Find("Title"),"설정",44,Color.white,-280,245,160,65);
            var subtitle=window.Find("Subtitle");
            if(subtitle!=null)Object.DestroyImmediate(subtitle.gameObject);
            var titleIcon=window.Find("Title Icon");
            if(titleIcon!=null)Object.DestroyImmediate(titleIcon.gameObject);
            var brand=Child(window,"Brand",typeof(Text));
            TextStyle(brand,"DELIVERY SERVICE",20,new Color32(99,209,218,255),235,245,300,40);
            foreach(var pair in new[]{("BGM",109f),("SFX",23f)})
            {
                var card=Child(window,pair.Item1+" Card",typeof(Image));
                Place(card,0,pair.Item2,756,72); Round(card.GetComponent<Image>(),Color.white,1.5f);
                card.GetComponent<Image>().raycastTarget=false; card.SetSiblingIndex(2);
            }
            SliderStyle(window,"BGM","배경음",109,SettingsIconGraphic.Symbol.Music);
            SliderStyle(window,"SFX","효과음",23,SettingsIconGraphic.Symbol.Speaker);
            ButtonStyle(window,"Achievements","업적",0,-73,756,72,Ink,SettingsIconGraphic.Symbol.Trophy,-51,29,140);
            ButtonStyle(window,"Return To Lobby","로비로 돌아가기",-195,-162,366,72,Ink,SettingsIconGraphic.Symbol.Home,-133,20,266);
            ButtonStyle(window,"Quit Game","게임 종료",195,-162,366,72,Red,SettingsIconGraphic.Symbol.Power,-89,33,205);
            var close=window.Find("Close"); Place((RectTransform)close,0,-251,756,76);
            Round(close.GetComponent<Image>(),Teal,1.5f);
            var oldSurface=close.Find("Paper Surface"); if(oldSurface!=null) oldSurface.gameObject.SetActive(false);
            TextStyle(close.Find("Label"),"닫기",37,Color.white,0,0,700,60);
            ButtonColors(close.GetComponent<Button>(),close.GetComponent<Image>(),true);
            var status = panel.transform.Find("Status") ?? window.Find("Status");
            Place((RectTransform)status,0,-284,710,24);
            status.SetParent(window,false);
            Place((RectTransform)status,0,156,710,22);
            status.GetComponent<Text>().fontSize=17;
            status.GetComponent<Text>().color=Red;
            window.gameObject.SetActive(true);
            panel.gameObject.SetActive(false);
        }

        private static void SliderStyle(Transform window,string id,string caption,float y,SettingsIconGraphic.Symbol symbol)
        {
            TextStyle(window.Find(id+" Label"),caption,32,Ink,-244,y,130,48);
            TextStyle(window.Find(id+" Value"),"100%",31,Ink,329,y,100,48);
            Icon(window,id+" Icon",symbol,Ink,-342,y,48);
            var slider=window.Find(id+" Slider").GetComponent<Slider>();
            Place(slider.GetComponent<RectTransform>(),45,y,400,48);
            var track=slider.transform.Find("Background").GetComponent<RectTransform>();
            Stretch(track); track.anchorMin=new Vector2(0,.28f); track.anchorMax=new Vector2(1,.72f);
            Round(track.GetComponent<Image>(),new Color32(205,221,218,255),1);
            Round(slider.fillRect.GetComponent<Image>(),Teal,1);
            var area=slider.transform.Find("Handle Area").GetComponent<RectTransform>(); Stretch(area);
            var handle=slider.handleRect; handle.sizeDelta=new Vector2(50,0);
            var old=handle.GetComponent<Image>(); old.enabled=false; old.raycastTarget=false;
            var knobRect=Child(handle,"Knob Icon",typeof(SettingsIconGraphic)); Stretch(knobRect);
            var knob=knobRect.GetComponent<SettingsIconGraphic>();
            knob.Icon=SettingsIconGraphic.Symbol.SliderKnob; knob.color=Ink; knob.raycastTarget=true;
            slider.targetGraphic=knob;
            slider.SetValueWithoutNotify(1);
            var colors=slider.colors; colors.highlightedColor=new Color(.7f,.94f,.98f); colors.pressedColor=new Color(.5f,.8f,.88f);
            colors.selectedColor=Color.white; slider.colors=colors;
        }
        private static void ButtonStyle(Transform window,string name,string caption,float x,float y,float w,float h,
            Color ink,SettingsIconGraphic.Symbol symbol,float iconX,float labelX,float labelW)
        {
            var button=window.Find(name).GetComponent<Button>();
            Place(button.GetComponent<RectTransform>(),x,y,w,h);
            Round(button.GetComponent<Image>(),new Color32(216,229,232,255),1.5f);
            var surface=Surface(button.transform,name=="Achievements"?new Color32(218,241,242,255):Color.white,1);
            surface.pixelsPerUnitMultiplier=1.5f;
            TextStyle(button.transform.Find("Label"),caption,31,ink,labelX,0,labelW,55);
            Icon(button.transform,"Icon",symbol,ink,iconX,0,39);
            ButtonColors(button,surface,false);
        }
        private static void ButtonColors(Button b,Graphic graphic,bool primary)
        {
            b.targetGraphic=graphic;
            var c=b.colors; c.normalColor=Color.white; c.selectedColor=Color.white;
            c.highlightedColor=primary?new Color(.86f,1,1):new Color(.88f,.97f,.97f);
            c.pressedColor=new Color(.68f,.86f,.88f); c.fadeDuration=.08f; b.colors=c;
        }
        private static Image Surface(Transform parent,Color color,float inset)
        {
            var r=Child(parent,"Paper Surface",typeof(Image)); r.SetAsFirstSibling(); Stretch(r);
            r.offsetMin=Vector2.one*inset; r.offsetMax=-Vector2.one*inset;
            var image=r.GetComponent<Image>(); Round(image,color,.65f); image.raycastTarget=false;
            return image;
        }
        private static void Icon(Transform parent,string name,SettingsIconGraphic.Symbol symbol,Color color,float x,float y,float size)
        {
            var rect=Child(parent,name,typeof(SettingsIconGraphic)); Place(rect,x,y,size,size);
            var icon=rect.GetComponent<SettingsIconGraphic>(); icon.Icon=symbol; icon.color=color; icon.raycastTarget=false;
        }
        private static RectTransform Child(Transform parent,string name,Type component)
        {
            var found=parent.Find(name); if(found!=null)return (RectTransform)found;
            var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),component);
            go.transform.SetParent(parent,false); return (RectTransform)go.transform;
        }
        private static void Round(Image image,Color color,float multiplier)
        { image.sprite=rounded; image.type=Image.Type.Sliced; image.pixelsPerUnitMultiplier=multiplier; image.color=color; }
        private static void TextStyle(Transform transform,string value,int size,Color ink,float x,float y,float w,float h)
        {
            Place((RectTransform)transform,x,y,w,h);
            var text=transform.GetComponent<Text>(); text.text=value; text.font=font; text.fontSize=size;
            text.fontStyle=FontStyle.Normal; text.color=ink; text.alignment=TextAnchor.MiddleCenter; text.raycastTarget=false;
        }
        private static void Stretch(RectTransform r)
        { r.anchorMin=Vector2.zero; r.anchorMax=Vector2.one; r.offsetMin=r.offsetMax=Vector2.zero; }
        private static void Place(RectTransform r,float x,float y,float w,float h)
        {
            r.anchorMin=r.anchorMax=new Vector2(.5f,.5f); r.pivot=new Vector2(.5f,.5f);
            r.sizeDelta=new Vector2(w,h); r.anchoredPosition=new Vector2(x,y);
        }
        private static void Verify(GameSettingsPanel panel)
        {
            if(panel.gameObject.activeSelf)throw new Exception("Settings must start hidden");
            var window=panel.transform.Find("Window");
            foreach(var button in window.GetComponentsInChildren<Button>(true))
                if(button.onClick.GetPersistentEventCount()!=1)throw new Exception("Lost button callback: "+button.name);
            foreach(var slider in window.GetComponentsInChildren<Slider>(true))
                if(slider.onValueChanged.GetPersistentEventCount()!=1)throw new Exception("Lost slider callback");
        }
        private static void Preview(Transform window)
        {
            var cameraObject=new GameObject("Preview camera",typeof(Camera));
            var camera=cameraObject.GetComponent<Camera>(); camera.enabled=false; camera.orthographic=true;
            camera.cullingMask=1<<31;
            camera.orthographicSize=380; camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color32(27,41,51,255);
            camera.transform.position=new Vector3(0,0,-100);
            var root=new GameObject("Preview canvas",typeof(RectTransform),typeof(Canvas));
            var canvas=root.GetComponent<Canvas>(); canvas.renderMode=RenderMode.WorldSpace; canvas.worldCamera=camera;
            root.GetComponent<RectTransform>().sizeDelta=new Vector2(1000,760);
            var clone=Object.Instantiate(window.gameObject,root.transform,false); clone.SetActive(true);
            foreach(var child in root.GetComponentsInChildren<Transform>(true))child.gameObject.layer=31;
            var rt=new RenderTexture(1000,760,24); camera.targetTexture=rt;
            Canvas.ForceUpdateCanvases(); camera.Render();
            var previous=RenderTexture.active; RenderTexture.active=rt;
            var image=new Texture2D(1000,760,TextureFormat.RGB24,false); image.ReadPixels(new Rect(0,0,1000,760),0,0); image.Apply();
            File.WriteAllBytes(Output+"/settings-reference-preview.png",image.EncodeToPNG());
            RenderTexture.active=previous; camera.targetTexture=null;
            Object.DestroyImmediate(image); Object.DestroyImmediate(rt); Object.DestroyImmediate(root); Object.DestroyImmediate(cameraObject);
        }
        public static void RenderBatch()
        {
            try
            {
                var scene=EditorSceneManager.OpenScene("Assets/Scenes/JJinCity.unity");
                var panel=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<GameSettingsPanel>(true)).Single();
                Preview(panel.transform.Find("Window"));
                EditorApplication.Exit(0);
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
    }
}
#endif
