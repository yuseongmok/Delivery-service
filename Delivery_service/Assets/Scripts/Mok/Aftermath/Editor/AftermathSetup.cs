#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DeliveryService.Mok.Editor
{
    public static partial class AftermathSetup
    {
        const string Folder = "Assets/DeliveryPrisonChapter/Aftermath/";
        public const string ScenePath = "Assets/Scenes/PrisonAftermath.unity";
        static Transform root;
        static AftermathFilm film;
        static List<AftermathFilm.Shot> shots;
        static List<AftermathDialogue.Line> lines;
        static Material dark, cream, timber, gold;
        static Font font;
        static RuntimeAnimatorController controller;

        public static void Build()
        {
            try
            {
                Directory.CreateDirectory("Logs");
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var current = SceneManager.GetSceneAt(i);
                    if (current.isLoaded && !string.IsNullOrEmpty(current.path))
                        EditorSceneManager.SaveScene(current, "Logs/Before-aftermath-" + current.name + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".unity", true);
                    if (current.isDirty) throw new Exception("Unsaved scene preserved; save changes before setup.");
                }
                if (File.Exists(ScenePath)) throw new Exception("Cinematic already exists; preserve manual edits.");
                Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
                // Copy the existing new shop and neighborhood. Never save to the source scene.
                var scene = EditorSceneManager.OpenScene("Assets/Scenes/NewHarborCity.unity", OpenSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
                foreach (var g in scene.GetRootGameObjects())
                {
                    foreach (var b in g.GetComponentsInChildren<MonoBehaviour>(true)) if (b) Object.DestroyImmediate(b);
                    foreach (var c in g.GetComponentsInChildren<Camera>(true)) Object.DestroyImmediate(c);
                    foreach (var a in g.GetComponentsInChildren<AudioListener>(true)) Object.DestroyImmediate(a);
                    foreach (var a in g.GetComponentsInChildren<AudioSource>(true)) Object.DestroyImmediate(a);
                }
                root = At("탈옥 이후 · 돈 회수와 새로운 삶", null, Vector3.zero);
                film = root.gameObject.AddComponent<AftermathFilm>();
                film.film = At("시네마틱 카메라", root, Vector3.zero).gameObject.AddComponent<Camera>();
                film.film.nearClipPlane = .04f; film.film.farClipPlane = 400;
                film.film.gameObject.AddComponent<AudioListener>();
                film.sound = root.gameObject.AddComponent<AudioSource>(); film.voice = root.gameObject.AddComponent<AudioSource>(); film.ambience = root.gameObject.AddComponent<AudioSource>();
                film.ambience.clip = Audio("roomAmbience"); film.ambience.loop = true;
                dark = Mat("차콜", new Color(.035f,.055f,.065f)); cream = Mat("서류", new Color(.85f,.82f,.69f));
                timber = Mat("오래된 나무",new Color(.20f,.115f,.065f)); gold = Mat("황동",new Color(.65f,.44f,.18f));
                font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/BMJUA_ttf.ttf");
                controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Scripts/Mok/Art/StoryCharacters/StoryWalk.controller");
                shots = new List<AftermathFilm.Shot>(); lines = new List<AftermathDialogue.Line>();
                BuildForest(); BuildExchange(); BuildShop();
                film.shots = shots.ToArray();
                var dialogue = ScriptableObject.CreateInstance<AftermathDialogue>(); dialogue.lines = lines.ToArray();
                AssetDatabase.CreateAsset(dialogue,Folder+"탈옥 이후 시네마틱 대사.asset"); film.dialogue = dialogue;
                BuildOverlay();
                foreach (var s in film.shots) s.set.SetActive(false);
                film.shots[0].set.SetActive(true); film.ApplyShot(0,0);
                var build = EditorBuildSettings.scenes.ToList();
                foreach(var path in new[]{ScenePath,"Assets/Scenes/NewHarborCity.unity"})
                { int n=build.FindIndex(s=>s.path==path); if(n<0)build.Add(new EditorBuildSettingsScene(path,true)); else build[n].enabled=true; }
                EditorBuildSettings.scenes = build.ToArray();
                AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene);
                File.WriteAllText("Logs/Aftermath-build.txt","PASS: saved "+film.shots.Length+" shots; original city scenes untouched.\n"+string.Join("\n",lines.Select(l=>l.scene+": "+l.subtitle)));
                Capture();
                EditorApplication.Exit(0);
            }
            catch(Exception e) { File.WriteAllText("Logs/Aftermath-build.txt",e.ToString()); Debug.LogException(e); EditorApplication.Exit(1); }
        }

        static Transform At(string name,Transform parent,Vector3 p)
        { var t=new GameObject(name).transform; t.SetParent(parent); t.position=p; return t; }
        static Material Mat(string name,Color color)
        { var m=new Material(Shader.Find("Universal Render Pipeline/Lit")); m.color=color; AssetDatabase.CreateAsset(m,Folder+name+".mat"); return m; }
        static Transform Box(string name,Transform parent,Vector3 p,Vector3 size,Material mat)
        { var g=GameObject.CreatePrimitive(PrimitiveType.Cube); g.name=name; g.transform.SetParent(parent); g.transform.position=p; g.transform.localScale=size; g.GetComponent<Renderer>().sharedMaterial=mat; Object.DestroyImmediate(g.GetComponent<Collider>()); return g.transform; }
        static GameObject Prop(string path,Transform parent,Vector3 p,float height,float yaw=0)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path); if(!prefab)throw new Exception(path);
            var g=Object.Instantiate(prefab,parent);g.name=prefab.name;g.transform.SetPositionAndRotation(Vector3.zero,Quaternion.Euler(0,yaw,0));
            foreach(var b in g.GetComponentsInChildren<MonoBehaviour>(true))Object.DestroyImmediate(b);
            foreach(var c in g.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(c);
            var rs=g.GetComponentsInChildren<Renderer>();var bnd=rs[0].bounds;foreach(var r in rs)bnd.Encapsulate(r.bounds);
            g.transform.localScale*=height/bnd.size.y; bnd=rs[0].bounds;foreach(var r in rs)bnd.Encapsulate(r.bounds);
            g.transform.position+=p-new Vector3(bnd.center.x,bnd.min.y,bnd.center.z);return g;
        }
        static AftermathActor Actor(string who,Transform set,Vector3 pos,float yaw)
        {
            var g=Prop("Assets/Synty/PolygonCity/Prefabs/Characters/Character_Male_"+(who=="민기"?"Jacket":"Hoodie")+".prefab",set,pos,1.8f,yaw);
            g.name=who;var actor=g.AddComponent<AftermathActor>();actor.animator=g.GetComponentInChildren<Animator>();
            actor.animator.runtimeAnimatorController=controller;actor.animator.applyRootMotion=false;actor.animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;actor.animator.Rebind();actor.animator.Update(0);return actor;
        }
        static Transform Marker(string name,Transform set,Vector3 p,float yaw=0)
        {var t=At(name,set,p);t.rotation=Quaternion.Euler(0,yaw,0);return t;}
        static AftermathFilm.Motion Move(Transform obj,Vector3 from,Vector3 to,float yaw=0,AftermathActor actor=null,float sitFrom=0,float sitTo=0,Transform left=null,Transform right=null)
        {return new AftermathFilm.Motion{subject=obj,from=Marker("시작 "+obj.name,root,from,yaw),to=Marker("끝 "+obj.name,root,to,yaw),actor=actor,seatedFrom=sitFrom,seatedTo=sitTo,reach=left||right?1:0,leftContact=left,rightContact=right};}
        static void Shot(string name,Transform set,Vector3 from,Vector3 to,Vector3 target,string line,float duration,params AftermathFilm.Motion[] motions)
        {
            shots.Add(new AftermathFilm.Shot{name=name,set=set.gameObject,cameraFrom=At(name+" 카메라 시작",root,from),cameraTo=At(name+" 카메라 끝",root,to),lookAt=At(name+" 시선",root,target),motions=motions,fadeIn=true,fadeOut=true});
            lines.Add(new AftermathDialogue.Line{scene=name,subtitle=line,seconds=duration});
        }
        static AudioClip Audio(string name)=>AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Scripts/Mok/Art/SuppliedAudio/"+name+".wav");
        static void LightAt(Transform parent,Vector3 p,Color c,float intensity,float range)
        {var l=At("연출 조명",parent,p).gameObject.AddComponent<Light>();l.type=LightType.Point;l.color=c;l.intensity=intensity;l.range=range;}

        static void BuildForest()
        {
            var set=At("01 숲속 · 숨겨둔 돈",root,Vector3.zero);Vector3 o=new Vector3(300,0,0);
            var grass=Mat("숲 지면",new Color(.16f,.23f,.14f));Box("숲길",set,o+new Vector3(0,-.1f,0),new Vector3(55,.2f,55),grass);
            Box("호송 도로",set,o+new Vector3(0,-.015f,9),new Vector3(55,.035f,6),dark);
            for(int i=0;i<25;i++)
            {float angle=i*2.399f;Vector3 p=o+new Vector3(Mathf.Cos(angle)* (8+i%4*3),0,Mathf.Sin(angle)*(7+i%5*2));if(p.z>5&&p.z<13)continue;Prop("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Tree_Pine_0"+(1+i%3)+".prefab",set,p,5+i%4,i*37);}
            for(int i=0;i<16;i++)Prop("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Grass_0"+(1+i%7)+".prefab",set,o+new Vector3(-4+i*.6f,0,2+Mathf.Sin(i)*.5f),.5f,i*27);
            var van=Prop("Assets/Synty/PolygonHeist/Prefab/Vehicles/SM_Veh_SwatVan_01.prefab",set,o+new Vector3(1,0,9),2.7f,90).transform;
            var player=Actor("플레이어",set,o+new Vector3(-1,0,1),15);var friend=Actor("민기",set,o+new Vector3(1,0,1),-20);
            Shot("멀어지는 호송차량",set,o+new Vector3(-5,2.2f,-4),o+new Vector3(-4,2,-3),o+new Vector3(1,1,5),"민기: 멈추지 마. 저 차가 멀어질 때까지 숨어 있어.",5,
                Move(van,o+new Vector3(1,0,9),o+new Vector3(16,0,9),90),Move(player.transform,o+new Vector3(-1,0,2),o+new Vector3(-1,0,0),180,player),Move(friend.transform,o+new Vector3(1,0,2),o+new Vector3(1,0,0),180,friend));
            var crate=Box("돈을 감춘 목재 상자",set,o+new Vector3(0,.22f,-1),new Vector3(1.3f,.44f,.7f),timber);
            var bag=Prop("Assets/Synty/PolygonHeist/Prefab/Items/SM_Item_DuffleBag_Open_Full_01.prefab",set,o+new Vector3(0,.45f,-1),.3f).transform;
            WorldText("야마자키 금고",set,o+new Vector3(0,.63f,-1.36f),.11f,Color.white);
            var l=Marker("돈가방 왼손",bag,bag.position+new Vector3(-.22f,.08f,0));var r=Marker("돈가방 오른손",bag,bag.position+new Vector3(.22f,.08f,0));
            Shot("숨겨둔 야마자키파의 돈",set,o+new Vector3(1.8f,1.7f,-3),o+new Vector3(1.3f,1.4f,-2.6f),o+new Vector3(0,.75f,-.7f),"민기: 그대로 있네. 야마자키파 금고에서 챙겨 숨겨둔 돈이야.",5,
                Move(friend.transform,o+new Vector3(0,-.35f,0),o+new Vector3(0,-.35f,0),180,friend,.55f,.55f,l,r),Move(player.transform,o+new Vector3(-1.3f,0,-.3f),o+new Vector3(-1.3f,0,-.3f),110,player));
            shots.Last().effect=Audio("bag");
            Shot("새 삶을 선택하다",set,o+new Vector3(-2.6f,1.5f,-2.5f),o+new Vector3(-2.2f,1.5f,-2),o+new Vector3(-.3f,1,-.1f),"플레이어: 이걸로 끝내자. 이름도, 사는 곳도… 전부 바꾸는 거야.",5,
                Move(friend.transform,o+new Vector3(0,-.35f,0),o+new Vector3(0,0,0),180,friend,.55f,0,l,r),Move(bag,bag.position,bag.position+Vector3.up*.55f));
            LightAt(set,o+new Vector3(-2,3,-3),new Color(.6f,.74f,1),1.8f,12);
        }

        static void BuildExchange()
        {
            var set=At("02 거래 · 새 신분",root,Vector3.zero);Vector3 o=new Vector3(400,0,0);
            Box("바닥",set,o+Vector3.down*.1f,new Vector3(12,.2f,12),dark);Box("뒷벽",set,o+new Vector3(0,2,3),new Vector3(12,4,.2f),timber);
            Box("작업대",set,o+new Vector3(0,.75f,0),new Vector3(2.8f,.15f,1.3f),timber);
            foreach(float x in new[]{-1.15f,1.15f})foreach(float z in new[]{-.45f,.45f})Box("책상 다리",set,o+new Vector3(x,.35f,z),new Vector3(.12f,.7f,.12f),dark);
            var player=Actor("플레이어",set,o+new Vector3(-.5f,0,-1.1f),0);var friend=Actor("민기",set,o+new Vector3(-1.65f,0,-.9f),35);
            var dealer=Actor("신분 거래인",set,o+new Vector3(.6f,0,1.1f),180);
            var money=Prop("Assets/Synty/PolygonHeist/Prefab/Props/SM_Prop_Money_Stack_03.prefab",set,o+new Vector3(-.7f,.83f,-.2f),.12f).transform;
            var papers=At("두 사람의 새 신분 봉투",set,o+new Vector3(.5f,.85f,.3f));
            Box("봉투",papers,papers.position,new Vector3(.7f,.018f,.4f),cream);
            for(int i=0;i<2;i++)
            {
                Vector3 p=papers.position+new Vector3((i-.5f)*.32f,.016f,-.02f);
                Box("새 신분증 "+i,papers,p,new Vector3(.28f,.008f,.18f),cream);
                // Deliberately fictional document; no real identity data.
                var txt=WorldText(i==0?"NEW ID  /  01":"NEW ID  /  02",papers,p+new Vector3(0,.012f,0),.027f,Color.black);txt.transform.rotation=Quaternion.Euler(90,0,0);
            }
            var r=Marker("봉투에 닿는 손",papers,papers.position+new Vector3(.15f,.03f,.05f));
            Shot("돈과 신분증의 교환",set,o+new Vector3(1.8f,1.7f,-1.9f),o+new Vector3(1.2f,1.55f,-1.5f),o+new Vector3(0,.85f,0),"거래인: 두 사람 몫이야. 여기서는 새 이름으로 살아.",5,
                Move(papers,papers.position,papers.position+new Vector3(0,0,-.35f)),Move(dealer.transform,dealer.transform.position,dealer.transform.position,180,dealer,0,0,null,r),Move(money,money.position,money.position+new Vector3(0,0,.25f)));
            shots.Last().effect=Audio("bag");
            var keys=Box("새 매장 열쇠",set,o+new Vector3(-.3f,.85f,-.05f),new Vector3(.035f,.02f,.2f),gold);
            var ring=GameObject.CreatePrimitive(PrimitiveType.Cylinder);ring.name="열쇠 머리";ring.transform.SetParent(keys);ring.transform.position=keys.position+new Vector3(0,0,-.1f);ring.transform.localScale=new Vector3(2,.3f,.4f);ring.GetComponent<Renderer>().sharedMaterial=gold;Object.DestroyImmediate(ring.GetComponent<Collider>());
            Shot("매장 열쇠",set,o+new Vector3(-.5f,1.7f,-.8f),o+new Vector3(-.4f,1.4f,-.6f),o+new Vector3(-.3f,.85f,-.05f),"민기: 남은 돈으로 가게를 구했어. 이번엔 진짜 피자만 만드는 거야.",4);
            LightAt(set,o+new Vector3(0,2.5f,-.4f),new Color(1,.7f,.36f),3,6);
        }

        static void BuildShop()
        {
            var set=At("03 새 매장 · 뉴스와 배달",root,Vector3.zero);
            var player=Actor("플레이어",set,new Vector3(0,-.43f,3),0);player.seated=1;
            var friend=Actor("민기",set,new Vector3(1,0,12),180);
            Prop("Assets/Synty/PolygonCity/Prefabs/Props/SM_Prop_ShopInterior_Chair_01.prefab",set,new Vector3(0,0,2.85f),.85f,180);
            Box("TV 받침대",set,new Vector3(0,.48f,6),new Vector3(1.9f,.95f,.55f),timber);
            Box("TV 프레임",set,new Vector3(0,1.35f,6),new Vector3(1.8f,1.05f,.16f),dark);
            var canvas=At("TV 뉴스 화면",set,new Vector3(0,1.35f,5.909f)).gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;
            var rt=canvas.GetComponent<RectTransform>();rt.sizeDelta=new Vector2(860,483);rt.localScale=Vector3.one*.00195f;
            var bg=Panel("방송 화면",rt,new Color(.035f,.09f,.15f),Vector2.zero,Vector2.one);
            Label("NEWS 24   |   사회",rt,28,TextAnchor.MiddleLeft,new Vector2(.04f,.87f),new Vector2(.96f,.98f));
            for(int i=0;i<2;i++)
            {
                Texture2D photo=Portrait(i==0?player:friend,i);
                var g=new GameObject("수배 사진",typeof(RectTransform),typeof(RawImage));g.transform.SetParent(rt,false);var p=g.GetComponent<RectTransform>();p.anchorMin=new Vector2(.20f+i*.32f,.32f);p.anchorMax=new Vector2(.45f+i*.32f,.84f);p.offsetMin=p.offsetMax=Vector2.zero;g.GetComponent<RawImage>().texture=photo;
            }
            Panel("속보 띠",rt,new Color(.65f,.08f,.045f),new Vector2(0,.15f),new Vector2(1,.30f));
            film.newsHeadline=Label("탈옥수 2명 행방 묘연",rt,27,TextAnchor.MiddleCenter,new Vector2(.025f,.15f),new Vector2(.975f,.30f));
            film.newsTicker=Label("공개 수배 · 시민 제보 요청",rt,19,TextAnchor.MiddleCenter,new Vector2(.025f,.02f),new Vector2(.975f,.14f));
            LightAt(set,new Vector3(0,1.3f,5.5f),new Color(.32f,.55f,1),.8f,4);
            Shot("몇 주 뒤",set,new Vector3(-7,3.2f,-10),new Vector3(-4,2.5f,-8),new Vector3(0,2,0),"",4);shots.Last().timeCard=true;
            Shot("TV 탈옥수 뉴스",set,new Vector3(0,1.35f,4.15f),new Vector3(.1f,1.35f,3.95f),new Vector3(0,1.35f,6),"뉴스: 교도소를 탈출한 수감자 두 명의 행방이 수 주째 확인되지 않고 있습니다.",6);
            shots.Last().fieldOfView=40;
            Shot("TV 너머의 그림자",set,new Vector3(2.6f,1.7f,5.2f),new Vector3(2.8f,1.6f,4.8f),new Vector3(0,1,3),"뉴스: 경찰은 두 사람이 신분을 숨긴 채 도피 중인 것으로 보고 수사를 이어가고 있습니다.",6,
                Move(player.transform,new Vector3(0,-.43f,3),new Vector3(0,-.43f,3),0,player,1,1));
            Shot("주방에서 부르는 민기",set,new Vector3(-1,1.6f,9.2f),new Vector3(-.8f,1.6f,9.5f),new Vector3(1,1.35f,12),"민기: 야, 피자 다 됐어! 얼른 갔다 와!",4,
                Move(friend.transform,friend.transform.position,friend.transform.position,180,friend));
            var box=Prop("Assets/Scripts/Mok/Resources/MokPizzaArt/Box.prefab",set,new Vector3(.6f,.76f,3.65f),.09f).transform;
            Box("배달 준비 테이블",set,new Vector3(.6f,.36f,3.65f),new Vector3(.8f,.72f,.65f),timber);
            var left=Marker("피자 상자 왼손 받침",box,box.position+new Vector3(-.18f,.01f,0));var right=Marker("피자 상자 오른손 받침",box,box.position+new Vector3(.18f,.01f,0));
            Shot("자리에서 일어나다",set,new Vector3(-2,1.5f,2),new Vector3(-1.8f,1.5f,2.2f),new Vector3(0,1.05f,3.2f),"플레이어: 알았어. 금방 다녀올게.",3,
                Move(player.transform,new Vector3(0,-.43f,3),new Vector3(0,0,3),0,player,1,0));
            Shot("피자 상자를 받치다",set,new Vector3(-1.4f,1.6f,4.4f),new Vector3(-1.1f,1.5f,4.1f),new Vector3(.2f,1,3.5f),"",3,
                Move(box,box.position,new Vector3(0,1,3.5f)),Move(player.transform,new Vector3(0,0,3),new Vector3(0,0,3),0,player,0,0,left,right));
            Shot("배달을 나서는 뒷모습",set,new Vector3(2,1.6f,5),new Vector3(1.5f,1.6f,4.5f),new Vector3(0,1,1),"",6,
                Move(player.transform,new Vector3(0,0,3),new Vector3(0,0,-1.5f),180,player,0,0,left,right),Move(box,new Vector3(0,1,2.55f),new Vector3(0,1,-1.95f),180));
            shots.Last().chapterCard=true;
        }

        static Texture2D Portrait(AftermathActor actor,int index)
        {
            var p=actor.transform.position;var r=actor.transform.rotation;
            actor.transform.SetPositionAndRotation(new Vector3(600+index*5,0,0),Quaternion.identity);
            var cam=new GameObject("수배 사진 카메라").AddComponent<Camera>();cam.transform.position=actor.transform.position+new Vector3(0,1.5f,2.2f);cam.transform.LookAt(actor.transform.position+Vector3.up*1.4f);cam.fieldOfView=26;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.2f,.26f,.29f);
            var rt=new RenderTexture(256,320,24);cam.targetTexture=rt;cam.Render();var previous=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(256,320,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,256,320),0,0);tex.Apply();RenderTexture.active=previous;
            string path=Folder+"수배사진"+index+".png";File.WriteAllBytes(path,tex.EncodeToPNG());Object.DestroyImmediate(tex);cam.targetTexture=null;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(cam.gameObject);actor.transform.SetPositionAndRotation(p,r);AssetDatabase.ImportAsset(path);return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        static TextMesh WorldText(string value,Transform parent,Vector3 p,float size,Color color)
        {var text=At(value,parent,p).gameObject.AddComponent<TextMesh>();text.text=value;text.font=font;text.fontSize=64;text.characterSize=size;text.anchor=TextAnchor.MiddleCenter;text.color=color;text.GetComponent<MeshRenderer>().sharedMaterial=font.material;return text;}
        static Image Panel(string name,Transform parent,Color color,Vector2 min,Vector2 max)
        {var g=new GameObject(name,typeof(RectTransform),typeof(Image));g.transform.SetParent(parent,false);var rt=g.GetComponent<RectTransform>();rt.anchorMin=min;rt.anchorMax=max;rt.offsetMin=rt.offsetMax=Vector2.zero;var image=g.GetComponent<Image>();image.color=color;image.raycastTarget=false;return image;}
        static Text Label(string value,Transform parent,int size,TextAnchor align,Vector2 min,Vector2 max)
        {var g=new GameObject(value,typeof(RectTransform),typeof(Text));g.transform.SetParent(parent,false);var rt=g.GetComponent<RectTransform>();rt.anchorMin=min;rt.anchorMax=max;rt.offsetMin=rt.offsetMax=Vector2.zero;var text=g.GetComponent<Text>();text.text=value;text.font=font;text.fontSize=size;text.alignment=align;text.color=new Color(1,.91f,.72f);text.raycastTarget=false;return text;}
        static void BuildOverlay()
        {
            var c=At("시네마틱 자막",root,Vector3.zero).gameObject.AddComponent<Canvas>();c.renderMode=RenderMode.ScreenSpaceOverlay;c.sortingOrder=32000;
            var scaler=c.gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);
            Panel("위 레터박스",c.transform,Color.black,new Vector2(0,.92f),Vector2.one);Panel("아래 레터박스",c.transform,Color.black,Vector2.zero,new Vector2(1,.08f));
            film.subtitle=Label("",c.transform,32,TextAnchor.MiddleCenter,new Vector2(.1f,.09f),new Vector2(.9f,.19f));film.subtitle.gameObject.AddComponent<Shadow>().effectDistance=new Vector2(2,-2);
            film.hint=Label("",c.transform,18,TextAnchor.MiddleRight,new Vector2(.5f,.015f),new Vector2(.97f,.06f));
            film.curtain=Panel("암전",c.transform,new Color(0,0,0,0),Vector2.zero,Vector2.one);
            film.title=Label("",c.transform,48,TextAnchor.MiddleCenter,new Vector2(.15f,.40f),new Vector2(.85f,.65f));
        }
        public static void Capture()
        {
            for(int i=0;i<film.shots.Length;i++)
            {
                foreach(var s in film.shots)s.set.SetActive(s.set==film.shots[i].set);
                film.ApplyShot(i,.5f);
                foreach(var a in film.shots[i].set.GetComponentsInChildren<AftermathActor>()) { a.animator.Update(0);a.SendMessage("LateUpdate"); }
                PrisonChickenCheck.Capture(film.film,"Logs/Aftermath-"+i.ToString("00")+".png");
            }
        }
    }
}
#endif

