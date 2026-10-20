#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.AI;
using Unity.AI.Navigation;

namespace DeliveryService.Mok.Editor {
public static partial class BankPrologueBuilder {
    public const string ScenePath = "Assets/Scenes/BankPrologue.unity";
    const string Art = "Assets/Scripts/Mok/Prologue/Art";
    static Material stone, dark, brass, teal, wood, paper, ink, road, white;
    static Font font;
    static Transform world;
    [MenuItem("Tools/Delivery Service/Mok/Build Bank Prologue")]
    public static void Build() {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play before building.");
        if (File.Exists(ScenePath)) throw new Exception("BankPrologue already exists. Preserve scene edits.");
        Directory.CreateDirectory("Temp/BankPrologueBackup");
        // 열려 있는 미저장 씬도 복사본으로 보존한다.
        for (int i = 0; i < SceneManager.sceneCount; i++) {
            var opened = SceneManager.GetSceneAt(i);
            if (opened.isLoaded && !string.IsNullOrEmpty(opened.path))
                EditorSceneManager.SaveScene(opened, $"Temp/BankPrologueBackup/{opened.name}-{i}.unity", true);
        }
        File.Copy("Assets/Scenes/Lobby.unity", "Temp/BankPrologueBackup/Lobby.unity", true);
        File.Copy("ProjectSettings/EditorBuildSettings.asset", "Temp/BankPrologueBackup/EditorBuildSettings.asset", true);
        Directory.CreateDirectory(Art);
        AssetDatabase.Refresh();
        font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/BMJUA_ttf.ttf");
        stone = Mat("Limestone", new Color(.68f,.64f,.53f));
        dark = Mat("Gunmetal", new Color(.07f,.11f,.14f));
        brass = Mat("Brass", new Color(.65f,.42f,.15f));
        teal = Mat("BankGreen", new Color(.075f,.25f,.23f));
        wood = Mat("Walnut", new Color(.24f,.12f,.065f));
        paper = Mat("Cash", new Color(.53f,.67f,.46f));
        ink = Mat("Band", new Color(.87f,.79f,.62f));
        white = Mat("Ivory", new Color(.9f,.87f,.76f));
        road = Mat("Road", new Color(.12f,.15f,.17f));
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        world = new GameObject("BANK / Saved Architecture").transform;
        Architecture();
        DressStreet();
        var director = new GameObject("Prologue Director").AddComponent<BankPrologue>();
        director.vaultDoor = Vault();
        director.cash = Cash();
        director.alarmLights = new[] { LightAt("Red Alarm",new Vector3(-2,3.2f,7),Color.red,0,12),
            LightAt("Blue Alarm",new Vector3(2,3.2f,-7),new Color(.1f,.3f,1),0,14) };
        var surface = world.gameObject.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.Children;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.BuildNavMesh();
        if (surface.navMeshData == null) throw new Exception("NavMesh bake failed.");
        AssetDatabase.CreateAsset(surface.navMeshData, Art+"/BankNavigation.asset");
        Player(director);
        director.officers = new PrologueOfficer[6];
        for(int i=0;i<6;i++) director.officers[i]=Officer(i,new Vector3(i%2==0?-5:5,0,-12-i*.7f));
        director.getawayCar = Vehicle("SM_Veh_Car_Muscle_01", new Vector3(6,0,-20), 90);
        var carHit = director.getawayCar.gameObject.AddComponent<BoxCollider>();
        carHit.center=new Vector3(0,.7f,0);carHit.size=new Vector3(1.8f,1.4f,4);
        director.exitPoint = new GameObject("Car boarding point").transform;
        director.exitPoint.position = new Vector3(5,0,-17.6f);
        Vehicle("SM_Veh_Car_Police_01",new Vector3(-8,0,-14),90);
        Vehicle("SM_Veh_Car_Police_01",new Vector3(10,0,-11),-40);
        var film = new GameObject("Cinematic Camera",typeof(Camera),typeof(AudioListener)).GetComponent<Camera>();
        film.fieldOfView=55;film.nearClipPlane=.08f;film.farClipPlane=160;
        film.backgroundColor=new Color(.06f,.09f,.14f);film.clearFlags=CameraClearFlags.SolidColor;
        director.filmCamera=film;
        director.shots = new[] {
            Shot("01 Vault reveal",new Vector3(4,2.2f,3.5f),new Vector3(0,1.8f,8)),
            Shot("02 Money",new Vector3(2.5f,2.5f,11),new Vector3(0,1.1f,14)),
            Shot("03 Police entry",new Vector3(3,2,3),new Vector3(0,1,-10)),
            Shot("04 Getaway",new Vector3(0,2.5f,-25),new Vector3(6,1,-20))
        };
        film.transform.SetPositionAndRotation(director.shots[0].position,director.shots[0].rotation);
        UI(director);
        Lighting();
        EditorSceneManager.SaveScene(scene,ScenePath);
        // 기존 로비의 공개 필드만 변경해 새 게임 흐름에 연결한다.
        var lobby=SceneManager.GetSceneByPath("Assets/Scenes/Lobby.unity");
        bool closeLobby=!lobby.isLoaded;
        if(closeLobby)lobby=EditorSceneManager.OpenScene("Assets/Scenes/Lobby.unity",OpenSceneMode.Additive);
        var controller=lobby.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<LobbyController>(true)).Single();
        controller.gameScene=ScenePath;EditorUtility.SetDirty(controller);EditorSceneManager.SaveScene(lobby);
        if(closeLobby)EditorSceneManager.CloseScene(lobby,true);
        var list=EditorBuildSettings.scenes.ToList();
        if(!list.Any(v=>v.path==ScenePath))list.Insert(Mathf.Min(1,list.Count),new EditorBuildSettingsScene(ScenePath,true));
        EditorBuildSettings.scenes=list.ToArray();
        AssetDatabase.SaveAssets();
        SceneManager.SetActiveScene(scene);
        Selection.activeGameObject=director.gameObject;
        File.WriteAllText("Temp/BankPrologue-build.txt","PASS: saved bank, vault, cash, police, cameras, UI, NavMesh, Lobby route and build scene.");
    }
    static Material Mat(string name,Color color) {
        var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,color=color};
        m.SetFloat("_Smoothness",.2f);
        AssetDatabase.CreateAsset(m,Art+"/"+name+".mat");return m;
    }
    static Transform Box(string name,Vector3 pos,Vector3 size,Material mat,Transform parent=null,bool collider=true) {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;
        g.transform.SetParent(parent?parent:world,false);g.transform.localPosition=pos;g.transform.localScale=size;
        g.GetComponent<Renderer>().sharedMaterial=mat;if(!collider)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
        return g.transform;
    }
    static Transform Cylinder(string name,Vector3 pos,Vector3 size,Material mat,Transform parent) {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name=name;g.transform.SetParent(parent,false);
        g.transform.localPosition=pos;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=mat;
        UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g.transform;
    }
    static void Architecture() {
        Box("Bank foundation",new Vector3(0,-.25f,4),new Vector3(20,.5f,24),stone);
        Box("Street",new Vector3(15,-.25f,-25),new Vector3(110,.5f,34),road);
        for(int x=-9;x<=9;x+=2)for(int z=-7;z<=15;z+=2)
            Box("Floor inlay",new Vector3(x,.006f,z),new Vector3(1.96f,.008f,1.96f),((x+z)/2)%2==0?stone:white,null,false);
        Box("West wall",new Vector3(-10,2.5f,4),new Vector3(.4f,5,24),stone);
        Box("East wall",new Vector3(10,2.5f,4),new Vector3(.4f,5,24),stone);
        Box("Back wall",new Vector3(0,2.5f,16),new Vector3(20,5,.4f),stone);
        Box("Ceiling",new Vector3(0,5,4),new Vector3(20,.3f,24),white);
        Box("Vault partition left",new Vector3(-6.1f,2.5f,8),new Vector3(7.8f,5,.6f),teal);
        Box("Vault partition right",new Vector3(6.1f,2.5f,8),new Vector3(7.8f,5,.6f),teal);
        Box("Vault header",new Vector3(0,4.7f,8),new Vector3(4.4f,.6f,.6f),teal);
        for(int i=-1;i<=1;i+=2) {
            Box("Facade",new Vector3(i*6,2.5f,-8),new Vector3(8,5,.45f),teal);
            Box("Entrance jamb",new Vector3(i*2.05f,2,-8),new Vector3(.15f,4,.7f),brass);
            Box("Counter",new Vector3(i*6, .65f,2),new Vector3(4,1.3f,2),wood);
            Box("Countertop",new Vector3(i*6,1.35f,2),new Vector3(4.2f,.12f,2.2f),dark);
            for(int k=0;k<3;k++) {
                Box("Teller divider",new Vector3(i*6-1.4f+k*1.4f,2.1f,2.7f),new Vector3(.08f,1.5f,.08f),brass);
                Box("Monitor",new Vector3(i*6-1.4f+k*1.4f,1.65f,2.2f),new Vector3(.6f,.4f,.1f),dark);
            }
            for(int z=-5;z<8;z+=6) {
                Box("Wall pilaster",new Vector3(i*9.6f,2.4f,z),new Vector3(.5f,4.8f,.7f),white);
                Cylinder("Planter",new Vector3(i*8.5f,.35f,z),new Vector3(.8f,.35f,.8f),dark,world);
                Box("Plant",new Vector3(i*8.5f,1,z),new Vector3(.65f,1,.65f),teal,null,false);
            }
            Box("Waiting bench",new Vector3(i*6,.45f,-4.8f),new Vector3(3,.25f,.8f),wood);
            Box("Bench back",new Vector3(i*6,.9f,-5.1f),new Vector3(3,.9f,.15f),wood);
        }
        Box("Entrance lintel",new Vector3(0,4.5f,-8),new Vector3(4,1,.6f),teal);
        Box("Bank sign board",new Vector3(0,4.15f,-8.35f),new Vector3(8,.9f,.12f),dark);
        Word("UNION BANK",new Vector3(0,4.15f,-8.43f),.12f,brass.color);
        Word("VAULT  /  금고실",new Vector3(0,4.55f,7.62f),.065f,white.color);
        for(int x=-40;x<60;x+=8)Box("Street marking",new Vector3(x,.012f,-28),new Vector3(3,.015f,.12f),ink,null,false);
        for(int x=-25;x<50;x+=12) {
            Box("Backdrop building",new Vector3(x,7,-39),new Vector3(10,14,8),dark);
            for(int row=0;row<4;row++)for(int col=0;col<3;col++)
                Box("Backdrop window",new Vector3(x-3+col*3,2+row*3,-34.95f),new Vector3(1.5f,1.7f,.08f),teal,null,false);
        }
    }
    static Transform Vault() {
        var hinge=new GameObject("Vault door hinge / animate").transform;hinge.SetParent(world,false);hinge.localPosition=new Vector3(-2.1f,2.1f,8);
        var disk=Cylinder("Steel vault door",new Vector3(2.1f,0,0),new Vector3(4.1f,.2f,4.1f),dark,hinge);disk.localRotation=Quaternion.Euler(90,0,0);
        Box("Door collision",new Vector3(2.1f,0,0),new Vector3(4,4,.35f),dark,hinge).GetComponent<Renderer>().enabled=false;
        var face=Cylinder("Brass inner ring",new Vector3(2.1f,0,-.22f),new Vector3(3.65f,.04f,3.65f),brass,hinge);face.localRotation=Quaternion.Euler(90,0,0);
        var plate=Cylinder("Inner steel",new Vector3(2.1f,0,-.28f),new Vector3(3.38f,.025f,3.38f),dark,hinge);plate.localRotation=Quaternion.Euler(90,0,0);
        for(int i=0;i<8;i++){
            float a=i*Mathf.PI/4;
            var bolt=Cylinder("Lock bolt",new Vector3(2.1f+Mathf.Cos(a)*1.4f,Mathf.Sin(a)*1.4f,-.35f),new Vector3(.16f,.08f,.16f),brass,hinge);bolt.localRotation=Quaternion.Euler(90,0,0);
        }
        for(int i=0;i<3;i++) {
            var spoke=Box("Wheel spoke",new Vector3(2.1f,0,-.45f),new Vector3(1.4f,.09f,.12f),brass,hinge,false);spoke.localRotation=Quaternion.Euler(0,0,i*60);
        }
        hinge.gameObject.AddComponent<NavMeshModifier>().ignoreFromBuild=true;
        return hinge;
    }
    static GameObject[] Cash() {
        Box("Money table",new Vector3(0,.85f,14),new Vector3(7,.2f,1.5f),wood);
        for(int i=-1;i<=1;i+=2)Box("Table leg",new Vector3(i*3,.4f,14),new Vector3(.15f,.8f,.8f),dark);
        for(int x=-8;x<=8;x+=4)for(int y=0;y<4;y++) {
            Box("Deposit box",new Vector3(x, .5f+y*.8f,15.65f),new Vector3(3.5f,.7f,.2f),dark);
            Box("Deposit handle",new Vector3(x,.5f+y*.8f,15.51f),new Vector3(.4f,.06f,.06f),brass,null,false);
        }
        var result=new GameObject[3];
        for(int i=0;i<3;i++){
            var g=new GameObject("Cash bundle "+(i+1));g.transform.SetParent(world,false);g.transform.localPosition=new Vector3((i-1)*2.3f,1.05f,14);
            for(int k=0;k<5;k++) {
                Box("Notes",new Vector3((k%2)*.65f-.3f,k*.1f,(k%2)*.16f),new Vector3(.8f,.09f,.4f),paper,g.transform,false);
                Box("Paper strap",new Vector3((k%2)*.65f-.3f,k*.1f+.05f,(k%2)*.16f),new Vector3(.15f,.015f,.42f),ink,g.transform,false);
            }
            result[i]=g;
        }
        return result;
    }
    static void Player(BankPrologue d) {
        var go=new GameObject("Prologue Player");go.tag="Player";go.transform.position=new Vector3(0,.1f,11.5f);go.transform.rotation=Quaternion.Euler(0,180,0);
        var cc=go.AddComponent<CharacterController>();cc.height=1.8f;cc.radius=.32f;cc.center=Vector3.up*.9f;cc.stepOffset=.25f;
        var cam=new GameObject("Player Camera",typeof(Camera),typeof(AudioListener)).GetComponent<Camera>();cam.transform.SetParent(go.transform,false);cam.transform.localPosition=Vector3.up*1.65f;cam.tag="MainCamera";cam.nearClipPlane=.05f;cam.farClipPlane=160;cam.fieldOfView=65;cam.enabled=false;cam.GetComponent<AudioListener>().enabled=false;
        var gun=CreateGun(cam.transform);gun.name="Held M1911";
        gun.transform.localPosition=new Vector3(.22f,-.19f,.38f);gun.transform.localRotation=Quaternion.Euler(-3,0,-4);
        foreach(var t in gun.GetComponentsInChildren<Transform>())t.gameObject.layer=2;
        var m=LightAt("Player muzzle",Vector3.zero,new Color(1,.65f,.2f),3,3);m.transform.SetParent(cam.transform,false);m.transform.localPosition=new Vector3(.3f,-.2f,.8f);m.enabled=false;
        var player=go.AddComponent<ProloguePlayer>();player.view=cam;player.weapon=gun;player.muzzle=m;player.director=d;d.player=player;
    }
    static PrologueOfficer Officer(int index,Vector3 pos) {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Scripts/Min/Prefab/NPC/Police.prefab");
        var go=UnityEngine.Object.Instantiate(prefab);go.name="Bank officer "+(index+1);Strip(go);go.transform.position=pos;go.transform.rotation=Quaternion.identity;
        var agent=go.AddComponent<NavMeshAgent>();agent.height=1.8f;agent.radius=.3f;agent.speed=2.8f;agent.acceleration=12;agent.stoppingDistance=5;agent.enabled=false;
        var col=go.AddComponent<CapsuleCollider>();col.height=1.8f;col.radius=.32f;col.center=Vector3.up*.9f;
        var officer=go.AddComponent<PrologueOfficer>();officer.animator=go.GetComponentInChildren<Animator>();officer.animator.applyRootMotion=false;
        var gun=CreateGun(go.transform);officer.gun=gun.transform;
        var flash=LightAt("Police muzzle",Vector3.zero,new Color(1,.6f,.2f),2,2);flash.transform.SetParent(gun.transform,false);flash.transform.localPosition=Vector3.forward*.2f;flash.enabled=false;officer.muzzle=flash;
        go.SetActive(false);return officer;
    }
    public static GameObject CreateGun(Transform parent, string modelName="M1911") {
        var holder=new GameObject(modelName+" grip");holder.transform.SetParent(parent,false);
        var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Low Poly Weapons VOL.1/Prefabs/"+modelName+".prefab");
        if(!source)throw new Exception("Low Poly Weapons VOL.1 / "+modelName+" prefab is missing.");
        var model=(GameObject)PrefabUtility.InstantiatePrefab(source,holder.transform);
        // 원본 총구는 -Z 방향이다. 손잡이 루트의 +Z가 발사 방향이 되도록 맞춘다.
        model.transform.localPosition=Vector3.zero;model.transform.localRotation=Quaternion.Euler(0,180,0);model.transform.localScale=Vector3.one;
        var mat=AssetDatabase.LoadAssetAtPath<Material>(Art+"/M1911_URP.mat");
        if(!mat){
            mat=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="M1911 URP"};
            mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Low Poly Weapons VOL.1/Textures_Guns.png"));
            mat.SetFloat("_Smoothness",.2f);AssetDatabase.CreateAsset(mat,Art+"/M1911_URP.mat");
        }
        foreach(var r in model.GetComponentsInChildren<Renderer>(true))r.sharedMaterials=r.sharedMaterials.Select(_=>mat).ToArray();
        return holder;
    }
    public static void InstallGuns(BankPrologue d) {
        var old=d.player.weapon;d.player.weapon=CreateGun(d.player.view.transform);d.player.weapon.name="Held M1911";
        d.player.weapon.transform.localPosition=new Vector3(.22f,-.19f,.38f);
        d.player.weapon.transform.localRotation=Quaternion.Euler(-3,0,-4);
        foreach(var t in d.player.weapon.GetComponentsInChildren<Transform>(true))t.gameObject.layer=2;
        UnityEngine.Object.DestroyImmediate(old);
        foreach(var o in d.officers){
            var oldGun=o.gun;o.gun=CreateGun(o.transform).transform;
            o.muzzle.transform.SetParent(o.gun,false);o.muzzle.transform.localPosition=new Vector3(0,.03f,.3f);
            UnityEngine.Object.DestroyImmediate(oldGun.gameObject);
        }
    }
    public static void DressStreet() {
        world=GameObject.Find("BANK / Saved Architecture").transform;
        if(world.Find("Street scenery")){FinishStreet(world.Find("Street scenery"));return;}
        var scenery=new GameObject("Street scenery").transform;scenery.SetParent(world,false);
        for(int i=0;i<3;i++){
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonCity/Prefabs/Buildings/SM_Bld_Apartment_0"+(i%2+1)+".prefab");
            var building=UnityEngine.Object.Instantiate(source,scenery);Strip(building);
            building.transform.localPosition=Vector3.zero;building.transform.localRotation=Quaternion.identity;
            var renders=building.GetComponentsInChildren<Renderer>();var bounds=renders[0].bounds;
            foreach(var r in renders)bounds.Encapsulate(r.bounds);
            building.transform.localScale*=18f/Mathf.Max(bounds.size.x,bounds.size.z);
            bounds=renders[0].bounds;foreach(var r in renders)bounds.Encapsulate(r.bounds);
            building.transform.position=new Vector3(27+i*21,-bounds.min.y,8)-new Vector3(bounds.center.x,0,bounds.center.z);
        }
        FinishStreet(scenery);
    }
    static void FinishStreet(Transform scenery) {
        int index=0;
        foreach(Transform building in scenery) {
            if(!building.name.StartsWith("SM_Bld_"))continue;
            building.localRotation=Quaternion.Euler(0,180,0);
            var renderers=building.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;
            foreach(var r in renderers)bounds.Encapsulate(r.bounds);
            building.position+=new Vector3(27+index++*21,0,8)-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
        }
        if(!scenery.Find("Block sidewalk"))
            Box("Block sidewalk",new Vector3(48,-.3f,3),new Vector3(75,.6f,38),
                AssetDatabase.LoadAssetAtPath<Material>(Art+"/Limestone.mat"),scenery,false);
    }
    static void Strip(GameObject go) {
        foreach(var b in go.GetComponentsInChildren<MonoBehaviour>(true))UnityEngine.Object.DestroyImmediate(b);
        foreach(var c in go.GetComponentsInChildren<Joint>(true))UnityEngine.Object.DestroyImmediate(c);
        foreach(var c in go.GetComponentsInChildren<Rigidbody>(true))UnityEngine.Object.DestroyImmediate(c);
        foreach(var c in go.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
        foreach(var c in go.GetComponentsInChildren<NavMeshAgent>(true))UnityEngine.Object.DestroyImmediate(c);
        foreach(var c in go.GetComponentsInChildren<AudioSource>(true))UnityEngine.Object.DestroyImmediate(c);
    }
    static Transform Vehicle(string name,Vector3 pos,float yaw) {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonCity/Prefabs/Vehicles/"+name+".prefab");
        var g=UnityEngine.Object.Instantiate(prefab);g.name=name;Strip(g);g.transform.SetPositionAndRotation(pos,Quaternion.Euler(0,yaw,0));return g.transform;
    }
    static Transform Shot(string name,Vector3 pos,Vector3 target) {var g=new GameObject(name).transform;g.position=pos;g.LookAt(target);return g;}
    static Light LightAt(string name,Vector3 position,Color color,float intensity,float range) {var l=new GameObject(name).AddComponent<Light>();l.transform.position=position;l.type=LightType.Point;l.color=color;l.intensity=intensity;l.range=range;l.shadows=LightShadows.None;return l;}
    static void Lighting() {
        RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=new Color(.4f,.48f,.55f);RenderSettings.ambientEquatorColor=new Color(.3f,.34f,.36f);RenderSettings.ambientGroundColor=new Color(.18f,.16f,.13f);
        RenderSettings.fog=true;RenderSettings.fogColor=new Color(.12f,.18f,.23f);RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogStartDistance=60;RenderSettings.fogEndDistance=140;
        var sun=new GameObject("Evening light").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=.75f;sun.color=new Color(.82f,.89f,1);sun.transform.rotation=Quaternion.Euler(32,-25,0);sun.shadows=LightShadows.Soft;
        for(int z=-4;z<=14;z+=6)for(int x=-6;x<=6;x+=6) {
            Box("Ceiling light",new Vector3(x,4.8f,z),new Vector3(1.4f,.08f,.7f),white,null,false);
            LightAt("Warm ceiling light",new Vector3(x,4,z),new Color(1,.85f,.62f),4,10);
        }
    }
    static void Word(string value,Vector3 pos,float size,Color color) {
        var g=new GameObject(value);g.transform.SetParent(world,false);g.transform.position=pos;
        var t=g.AddComponent<TextMesh>();t.text=value;t.font=font;t.fontSize=64;t.characterSize=size;t.anchor=TextAnchor.MiddleCenter;t.color=color;g.GetComponent<MeshRenderer>().sharedMaterial=font.material;
    }
    static RectTransform Rect(Transform parent,string name,Vector2 min,Vector2 max) {
        var g=new GameObject(name,typeof(RectTransform));g.transform.SetParent(parent,false);var r=(RectTransform)g.transform;r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;return r;
    }
    static Text Label(Transform parent,string name,int size,Vector2 min,Vector2 max,TextAnchor align) {
        var t=Rect(parent,name,min,max).gameObject.AddComponent<Text>();t.font=font;t.fontSize=size;t.alignment=align;t.color=new Color(1,.94f,.8f);t.raycastTarget=false;
        var sh=t.gameObject.AddComponent<Shadow>();sh.effectColor=new Color(0,0,0,.85f);sh.effectDistance=new Vector2(2,-2);return t;
    }
    static Image Image(Transform parent,string name,Color color,Vector2 min,Vector2 max) {var i=Rect(parent,name,min,max).gameObject.AddComponent<Image>();i.color=color;i.raycastTarget=false;return i;}
    static void UI(BankPrologue d) {
        var canvas=new GameObject("Prologue UI",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
        var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.matchWidthOrHeight=.5f;
        var root=canvas.transform;
        d.damageFlash=Image(root,"Damage",Color.clear,Vector2.zero,Vector2.one);
        d.bars=Rect(root,"Cinema bars",Vector2.zero,Vector2.one).gameObject.AddComponent<CanvasGroup>();
        Image(d.bars.transform,"Top",Color.black,new Vector2(0,.90f),Vector2.one);Image(d.bars.transform,"Bottom",Color.black,Vector2.zero,new Vector2(1,.10f));
        d.fade=Image(root,"Fade",Color.black,Vector2.zero,Vector2.one).gameObject.AddComponent<CanvasGroup>();d.fade.alpha=0;
        d.objective=Label(root,"Objective",30,new Vector2(.04f,.78f),new Vector2(.8f,.9f),TextAnchor.UpperLeft);
        d.subtitle=Label(root,"Subtitle",30,new Vector2(.12f,.10f),new Vector2(.88f,.2f),TextAnchor.MiddleCenter);
        d.prompt=Label(root,"Prompt",24,new Vector2(.2f,.03f),new Vector2(.8f,.09f),TextAnchor.MiddleCenter);
        d.ammo=Label(root,"Ammo",28,new Vector2(.82f,.12f),new Vector2(.96f,.23f),TextAnchor.MiddleRight);
        d.health=Label(root,"Health",26,new Vector2(.04f,.12f),new Vector2(.18f,.21f),TextAnchor.MiddleLeft);
        d.crosshair=Label(root,"Crosshair",25,new Vector2(.48f,.47f),new Vector2(.52f,.53f),TextAnchor.MiddleCenter).gameObject;d.crosshair.GetComponent<Text>().text="+";
        d.progress=Image(root,"Collection progress",new Color(1,.55f,.2f),new Vector2(.4f,.025f),new Vector2(.6f,.032f));d.progress.sprite=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");d.progress.type=UnityEngine.UI.Image.Type.Filled;d.progress.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;d.progress.fillAmount=0;
        d.pausePanel=Image(root,"Pause",new Color(.02f,.035f,.05f,.95f),Vector2.zero,Vector2.one).gameObject.AddComponent<CanvasGroup>();
        d.pauseText=Label(d.pausePanel.transform,"Pause text",38,new Vector2(.15f,.2f),new Vector2(.85f,.8f),TextAnchor.MiddleCenter);d.pausePanel.alpha=0;
    }
}
}
#endif
