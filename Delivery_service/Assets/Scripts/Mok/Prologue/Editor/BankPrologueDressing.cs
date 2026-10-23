#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Unity.AI.Navigation;
namespace DeliveryService.Mok.Editor {
public static partial class BankPrologueBuilder {
    const string DecorArt=Art+"/BankDressing";
    static Transform decor;
    static Material cream, panel, trim, glow, blueScreen, redGlass, blueGlass, carpet, textMat;
    // 장식은 씬에 저장하고, 큰 가구의 충돌은 내비게이션에도 반영한다.
    public static void DressBank(){
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play before dressing the bank.");
        var scene=SceneManager.GetSceneByPath(ScenePath);
        if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        SceneManager.SetActiveScene(scene);Directory.CreateDirectory("Logs");
        if(!EditorSceneManager.SaveScene(scene,"Logs/Bank-before-dressing-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new Exception("Backup failed.");
        world=scene.GetRootGameObjects().Single(g=>g.name=="BANK / Saved Architecture").transform;
        if(world.Cast<Transform>().Any(t=>t.name=="Bank atmosphere / saved"))throw new Exception("Bank dressing already exists. Preserve current scene edits.");
        Directory.CreateDirectory(DecorArt);AssetDatabase.Refresh();
        font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/BMJUA_ttf.ttf");
        stone=LoadMat("Limestone");dark=LoadMat("Gunmetal");brass=LoadMat("Brass");teal=LoadMat("BankGreen");wood=LoadMat("Walnut");white=LoadMat("Ivory");ink=LoadMat("Band");
        cream=DecorMat("Warm plaster",new Color(.72f,.73f,.67f));panel=DecorMat("Deep jade",new Color(.045f,.15f,.145f));
        trim=DecorMat("Satin bronze",new Color(.53f,.36f,.16f));carpet=DecorMat("Entrance carpet",new Color(.045f,.095f,.10f));
        glow=DecorMat("Warm diffuser",new Color(1,.85f,.59f),1.6f);blueScreen=DecorMat("Terminal screen",new Color(.13f,.43f,.43f),.4f);
        redGlass=DecorMat("Emergency red",new Color(.85f,.035f,.015f),1.2f);blueGlass=DecorMat("Emergency blue",new Color(.025f,.18f,.9f),1.2f);
        textMat=AssetDatabase.LoadAssetAtPath<Material>(Art+"/WorldText.mat");
        decor=new GameObject("Bank atmosphere / saved").transform;decor.SetParent(world,false);
        var d=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<BankPrologue>(true)).Single();
        DressWalls();DressTellers();DressSecurity();DressFront(d);DressLights(scene);
        // 새 가구를 피해 가도록 기존 내비게이션 에셋의 GUID를 유지해 다시 굽는다.
        var surface=world.GetComponent<NavMeshSurface>();var old=surface.navMeshData;
        surface.RemoveData();surface.navMeshData=null;surface.BuildNavMesh();var baked=surface.navMeshData;
        if(!baked)throw new Exception("Navigation bake failed.");
        surface.RemoveData();EditorUtility.CopySerialized(baked,old);EditorUtility.SetDirty(old);surface.navMeshData=old;surface.AddData();UnityEngine.Object.DestroyImmediate(baked);
        AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);CaptureDressing();
        File.WriteAllText("Logs/Bank-dressing-build.txt","PASS: saved bank furniture, teller details, security props, facade lighting and emergency lights. Navigation rebaked.\n"+decor.GetComponentsInChildren<Renderer>().Length+" decorative renderers.\n");
    }
    static Material DecorMat(string name,Color color,float emission=0){
        var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,color=color};m.SetFloat("_Smoothness",.24f);
        if(emission>0){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*emission);}
        AssetDatabase.CreateAsset(m,DecorArt+"/"+name+".mat");return m;
    }
    static Transform Detail(string name,Vector3 p,Vector3 size,Material mat,Transform parent=null,bool solid=false)=>Box(name,p,size,mat,parent?parent:decor,solid);
    static Transform Group(string name,Vector3 position,float yaw=0){var t=new GameObject(name).transform;t.SetParent(decor,false);t.position=position;t.rotation=Quaternion.Euler(0,yaw,0);return t;}
    static void Letter(string value,Vector3 local,float size,Transform parent,Color? color=null){
        var g=new GameObject(value);g.transform.SetParent(parent,false);g.transform.localPosition=local;
        var text=g.AddComponent<TextMesh>();text.font=font;text.text=value;text.fontSize=72;text.characterSize=size;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.color=color??new Color(.94f,.87f,.68f);
        g.GetComponent<Renderer>().sharedMaterial=textMat;g.AddComponent<PrologueDepthText>();
    }
    static void Board(string title,string body,Vector3 p,float yaw=0){
        var root=Group("Bank information / "+title,p,yaw);
        Detail("Bronze frame",Vector3.zero,new Vector3(2.8f,2.1f,.09f),trim,root);
        Detail("Printed panel",new Vector3(0,0,-.06f),new Vector3(2.65f,1.95f,.03f),panel,root);
        Letter(title,new Vector3(0,.56f,-.085f),.065f,root);
        Detail("Divider",new Vector3(0,.24f,-.09f),new Vector3(2.1f,.025f,.015f),trim,root);
        Letter(body,new Vector3(0,-.28f,-.09f),.037f,root);
    }
    static Transform Prop(string asset,Vector3 p,float height,float yaw=0,bool solid=false){
        var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonCity/Prefabs/Props/"+asset+".prefab");
        if(!source)throw new Exception("Missing bank prop: "+asset);
        var g=UnityEngine.Object.Instantiate(source,decor);g.name="Bank prop / "+asset;Strip(g);g.transform.rotation=Quaternion.Euler(0,yaw,0);
        var rs=g.GetComponentsInChildren<Renderer>();var bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);
        g.transform.localScale*=height/Mathf.Max(.01f,bounds.size.y);bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);
        g.transform.position+=p-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
        if(solid){var box=g.AddComponent<BoxCollider>();box.center=g.transform.InverseTransformPoint(bounds.center+p-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z));box.size=new Vector3(bounds.size.x/g.transform.lossyScale.x,bounds.size.y/g.transform.lossyScale.y,bounds.size.z/g.transform.lossyScale.z);}
        return g.transform;
    }
    static void DressWalls(){
        foreach(int side in new[]{-1,1}){
            Detail("Timber wall base",new Vector3(side*17.73f,.75f,-29),new Vector3(.12f,1.5f,41),panel);
            Detail("Bronze chair rail",new Vector3(side*17.64f,1.53f,-29),new Vector3(.08f,.055f,41),trim);
            Detail("Ceiling perimeter",new Vector3(side*17.6f,4.65f,-29),new Vector3(.3f,.22f,41),cream);
            for(int z=-11;z>=-47;z-=4)Detail("Wall panel joint",new Vector3(side*17.64f,.75f,z),new Vector3(.035f,1.35f,.045f),trim);
            for(int z=-14;z>=-38;z-=12){Board("UNION BANK","안전한 금융, 가까운 은행\nSINCE 1986",new Vector3(side*17.62f,2.85f,z),side>0?90:-90);}
        }
        foreach(var z in new[]{-18f,-30f,-41f}){
            float x=z==-30?4:-4;
            foreach(int side in new[]{-1,1}){
                Detail("Divider wainscot",new Vector3(x,.7f,z+side*.28f),new Vector3(27.7f,1.4f,.04f),panel);
                Detail("Divider bronze rail",new Vector3(x,1.43f,z+side*.31f),new Vector3(27.7f,.045f,.03f),trim);
            }
        }
        Board("고객 안내","예금 · 출금 · 대출 상담\n평일 09:00 — 16:00",new Vector3(-11,2.8f,-41.32f));
        Board("오늘의 금융","목돈을 위한 첫걸음\nUNION 정기예금",new Vector3(-3,2.8f,-41.32f));
        Board("관계자 외 출입금지","보안 구역 / SECURITY\n출입증을 확인해 주세요",new Vector3(5.5f,2.8f,7.62f));
        foreach(float x in new[]{-13f,-9f})Prop("SM_Prop_ATM_01",new Vector3(x,0,-48.7f),2.2f,180,true);
        var atm=Group("ATM overhead sign",new Vector3(-11,3.1f,-49.6f),180);Letter("ATM  /  현금 서비스",Vector3.zero,.075f,atm);
        foreach(float z in new[]{-22f,-34f})foreach(int side in new[]{-1,1})Prop("SM_Prop_PotPlant_01",new Vector3(side*16.9f,0,z),1.65f,side*35,true);
        foreach(float x in new[]{-13f,-8f,-3f,2f,7f}){
            Detail("Upholstered waiting seat",new Vector3(x,.79f,-47.5f),new Vector3(2.85f,.12f,.9f),panel);
            Detail("Waiting seat back cushion",new Vector3(x,1.03f,-47.84f),new Vector3(2.85f,.7f,.12f),panel);
        }
        Detail("Lobby carpet border",new Vector3(13,.034f,-47.8f),new Vector3(3.6f,.012f,3.4f),trim);
        Detail("Lobby carpet",new Vector3(13,.043f,-47.8f),new Vector3(3.4f,.01f,3.2f),carpet);
    }
    static void DressTellers(){
        int number=0;
        foreach(float z in new[]{-13f,-25f,-36f,-46f})foreach(float x in new[]{-6f,5f}){
            number++;var root=Group("Teller station "+number,new Vector3(x,0,z));
            Detail("Counter bronze fascia",new Vector3(0,1.07f,-.665f),new Vector3(2.9f,.06f,.03f),trim,root);
            Detail("Counter number plaque",new Vector3(0,.75f,-.68f),new Vector3(.8f,.4f,.035f),panel,root);Letter(number.ToString("00"),new Vector3(0,.75f,-.705f),.055f,root);
            Detail("Monitor stand",new Vector3(0,1.47f,0),new Vector3(.3f,.1f,.25f),dark,root);
            Detail("Screen display",new Vector3(0,1.7f,-.066f),new Vector3(.61f,.45f,.012f),blueScreen,root);
            for(int row=0;row<3;row++)Detail("Terminal text line",new Vector3(-.035f,1.79f-row*.08f,-.075f),new Vector3(.41f-row*.07f,.012f,.008f),cream,root);
            Detail("Keyboard",new Vector3(0,1.43f,-.4f),new Vector3(.58f,.04f,.19f),dark,root);
            for(int i=0;i<3;i++)Detail("Document tray",new Vector3(.95f,1.45f+i*.07f,.14f),new Vector3(.5f,.04f,.34f),i==1?white:panel,root);
            Detail("Pen stand",new Vector3(-1,1.5f,-.15f),new Vector3(.17f,.17f,.17f),dark,root);
            for(int i=0;i<3;i++)Detail("Counter pen",new Vector3(-1+i*.045f,1.65f,-.15f),new Vector3(.018f,.24f,.018f),trim,root);
            var badge=Detail("Service hanging sign",new Vector3(0,3.25f,0),new Vector3(1.6f,.48f,.08f),panel,root);
            Letter(number.ToString("00")+"  금융 상담",new Vector3(0,3.25f,-.05f),.038f,root);
            foreach(float sx in new[]{-.6f,.6f})Detail("Sign suspension",new Vector3(sx,4.05f,0),new Vector3(.025f,1.1f,.025f),trim,root);
        }
        foreach(float z in new[]{-24f,-35f}){
            var cabinet=Group("Files cabinet",new Vector3(17.2f,0,z),90);
            Detail("Cabinet body",new Vector3(0,1.05f,0),new Vector3(2.2f,2.1f,.65f),panel,cabinet,true);
            for(int row=0;row<4;row++)for(int col=0;col<2;col++){
                Detail("Drawer face",new Vector3(-.54f+col*1.08f,.3f+row*.5f,-.34f),new Vector3(1.01f,.44f,.035f),cream,cabinet);
                Detail("Drawer handle",new Vector3(-.54f+col*1.08f,.35f+row*.5f,-.38f),new Vector3(.25f,.035f,.055f),trim,cabinet);
            }
        }
        // 바닥 종이는 충돌 없이 시각적 흔적만 남긴다.
        for(int i=0;i<18;i++){
            float x=-7+(i*7%17)*.75f,z=-12-(i*11%33);
            var p=Detail("Scattered bank form",new Vector3(x,.045f,z),new Vector3(.21f,.006f,.3f),white);p.localRotation=Quaternion.Euler(0,i*37,0);
        }
    }
    static void DressSecurity(){
        foreach(var p in new[]{new Vector3(-9.5f,3.65f,6),new Vector3(17.4f,3.65f,-20),new Vector3(-17.4f,3.65f,-40)})Prop("SM_Prop_SecurityCamera_01",p,.35f,p.x>0?90:-90);
        var clockRoot=Group("Lobby wall clock",new Vector3(4,3.45f,-41.34f));
        var rim=Cylinder("Clock rim",Vector3.zero,new Vector3(.85f,.045f,.85f),trim,clockRoot);rim.localRotation=Quaternion.Euler(90,0,0);
        var face=Cylinder("Clock face",new Vector3(0,0,-.055f),new Vector3(.76f,.012f,.76f),cream,clockRoot);face.localRotation=Quaternion.Euler(90,0,0);
        Detail("Clock minute hand",new Vector3(0,.12f,-.08f),new Vector3(.025f,.29f,.018f),dark,clockRoot);
        var hand=Detail("Clock hour hand",new Vector3(.08f,.03f,-.09f),new Vector3(.2f,.03f,.018f),dark,clockRoot);hand.localRotation=Quaternion.Euler(0,0,25);
        for(int i=0;i<12;i++){float a=i*Mathf.PI/6;Detail("Clock tick",new Vector3(Mathf.Sin(a)*.32f,Mathf.Cos(a)*.32f,-.078f),new Vector3(.025f,.035f,.01f),dark,clockRoot);}
        foreach(float x in new[]{-7.8f,7.8f}){
            var root=Group("Vault deposit boxes",new Vector3(x,0,15.65f));
            Detail("Safe deposit case",new Vector3(0,1.7f,0),new Vector3(3.6f,3.2f,.24f),dark,root,true);
            for(int row=0;row<6;row++)for(int col=0;col<4;col++){
                var p=new Vector3(-1.32f+col*.88f,.4f+row*.51f,-.14f);
                Detail("Safe box door",p,new Vector3(.81f,.45f,.035f),cream,root);
                Detail("Safe box latch",p+new Vector3(.21f,0,-.04f),new Vector3(.12f,.035f,.04f),trim,root);
            }
        }
        var reader=Group("Vault access control",new Vector3(2.65f,1.45f,7.62f));Detail("Reader",Vector3.zero,new Vector3(.22f,.38f,.12f),dark,reader);Detail("Reader red status",new Vector3(0,.1f,-.065f),new Vector3(.13f,.045f,.01f),redGlass,reader);
    }
    static void DressFront(BankPrologue d){
        Detail("Canopy bronze edge",new Vector3(13,4.2f,-54.06f),new Vector3(5,.16f,.08f),trim);
        Detail("Canopy downlight",new Vector3(13,4.12f,-52),new Vector3(3.9f,.04f,.35f),glow);
        var welcome=Group("Front door signage",new Vector3(13,3.7f,-50.35f));Letter("UNION BANK",Vector3.zero,.07f,welcome);
        foreach(float x in new[]{10.7f,15.3f})Detail("Entrance bronze edge",new Vector3(x,2,-50.37f),new Vector3(.1f,3.9f,.14f),trim);
        for(int x=-16;x<=8;x+=4){
            Detail("Window sill",new Vector3(x+1.5f,1.35f,-50.43f),new Vector3(2.3f,.14f,.27f),cream);
            Detail("Window transom",new Vector3(x+1.6f,2.7f,-50.38f),new Vector3(2,.06f,.07f),trim);
            Detail("Window upright",new Vector3(x+1.6f,2.7f,-50.38f),new Vector3(.055f,2.5f,.07f),trim);
        }
        foreach(float x in new[]{-14f,-6f,2f,17f}){
            Detail("Facade light housing",new Vector3(x,3.8f,-50.55f),new Vector3(.25f,.55f,.25f),dark);
            Detail("Facade light lens",new Vector3(x,3.7f,-50.7f),new Vector3(.17f,.33f,.04f),glow);
        }
        Board("UNION BANK","영업시간 09:00 — 16:00\n고객 주차장 →",new Vector3(17,2.3f,-50.4f));
        foreach(float x in new[]{-17f,-9f,-1f})Prop("SM_Prop_Barrier_01",new Vector3(x,0,-58),1.05f,8,true);
        foreach(float x in new[]{-19f,-5f,5f})Prop("SM_Prop_Cone_01",new Vector3(x,0,-59.5f),.65f);
        var tape=Group("Police perimeter warning",new Vector3(-9,1.05f,-58.5f));Detail("Warning tape",Vector3.zero,new Vector3(7,.15f,.015f),trim,tape);Letter("POLICE LINE   ·   출입 금지",new Vector3(0,0,-.015f),.025f,tape,dark.color);
        // 기존 경보 타이밍에 연결해 은행 출입구에 번갈아 비치는 출동 조명.
        var lights=d.alarmLights.ToList();
        for(int i=0;i<2;i++){
            var l=LightAt("Entrance police spill "+i,new Vector3(9+i*7,2.5f,-52),i==0?new Color(1,.04f,.02f):new Color(.04f,.18f,1),0,9);l.transform.SetParent(decor,true);lights.Add(l);
            Detail("Exterior alarm beacon",new Vector3(11+i*4,3.6f,-50.55f),new Vector3(.23f,.25f,.13f),i==0?redGlass:blueGlass);
        }
        d.alarmLights=lights.ToArray();
    }
    static void DressLights(Scene scene){
        var roots=scene.GetRootGameObjects();
        foreach(var l in roots.SelectMany(g=>g.GetComponentsInChildren<Light>(true))){
            if(l.name=="Extension ceiling light"||l.name=="Warm ceiling light"){
                l.color=new Color(1,.86f,.67f);l.intensity=2.6f;l.range=10;l.type=LightType.Point;l.shadows=LightShadows.None;
            }
        }
        foreach(var r in roots.SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true)).Where(r=>r.name=="Light panel"||r.name=="Ceiling light")){
            r.sharedMaterial=glow;var p=r.transform.position;var size=r.transform.lossyScale;
            Detail("Ceiling lamp trim",p+Vector3.up*.05f,new Vector3(size.x+.18f,.13f,size.z+.18f),dark);
        }
        foreach(float z in new[]{-17.5f,-29.5f,-40.5f}){
            Detail("Ceiling cross beam",new Vector3(0,4.75f,z),new Vector3(35,.3f,.35f),cream);
            Detail("Ceiling bronze strip",new Vector3(0,4.58f,z),new Vector3(35,.025f,.05f),trim);
        }
        RenderSettings.ambientSkyColor=new Color(.30f,.37f,.43f);RenderSettings.ambientEquatorColor=new Color(.22f,.26f,.28f);RenderSettings.ambientGroundColor=new Color(.13f,.12f,.10f);
    }
    public static void PolishDressing(){
        var scene=EditorSceneManager.OpenScene(ScenePath);world=scene.GetRootGameObjects().Single(g=>g.name=="BANK / Saved Architecture").transform;
        decor=world.Cast<Transform>().SingleOrDefault(t=>t.name=="Bank atmosphere / saved");if(!decor)throw new Exception("Install dressing first.");
        EditorSceneManager.SaveScene(scene,"Logs/Bank-before-dressing-polish.unity",true);
        cream=AssetDatabase.LoadAssetAtPath<Material>(DecorArt+"/Warm plaster.mat");trim=AssetDatabase.LoadAssetAtPath<Material>(DecorArt+"/Satin bronze.mat");panel=AssetDatabase.LoadAssetAtPath<Material>(DecorArt+"/Deep jade.mat");
        foreach(var r in world.GetComponentsInChildren<MeshRenderer>(true)){
            if(new[]{"Outer wall","Security divider","Office divider","Lobby divider"}.Contains(r.name))r.sharedMaterial=cream;
            if(r.name=="Stone tile")r.enabled=false;
        }
        if(!decor.Find("Inlaid stone floor")){
            // 수백 개 오브젝트 대신 재질별 메시로 묶은 작은 석재 타일.
            var floor=new GameObject("Inlaid stone floor").transform;floor.SetParent(decor,false);
            var colors=new[]{new Color(.56f,.57f,.53f),new Color(.62f,.63f,.58f),new Color(.51f,.53f,.50f)};
            for(int shade=0;shade<3;shade++){
                var vertices=new System.Collections.Generic.List<Vector3>();var triangles=new System.Collections.Generic.List<int>();
                for(int x=0;x<24;x++)for(int z=0;z<28;z++){
                    if((x*7+z*3+x*z)%3!=shade)continue;float px=-18+x*1.5f,pz=-50+z*1.5f;int n=vertices.Count;
                    vertices.Add(new Vector3(px+.015f,.032f,pz+.015f));vertices.Add(new Vector3(px+.015f,.032f,pz+1.485f));vertices.Add(new Vector3(px+1.485f,.032f,pz+1.485f));vertices.Add(new Vector3(px+1.485f,.032f,pz+.015f));triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});
                }
                var mesh=new Mesh{name="Bank floor tiles "+shade};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,DecorArt+"/FloorTiles"+shade+".asset");
                var g=new GameObject(mesh.name,typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(floor,false);g.GetComponent<MeshFilter>().sharedMesh=mesh;g.GetComponent<Renderer>().sharedMaterial=DecorMat("Stone tile "+shade,colors[shade]);
            }
            foreach(float x in new[]{-16.8f,16.8f})Detail("Floor bronze border",new Vector3(x,.039f,-29),new Vector3(.055f,.008f,40),trim);
            foreach(float z in new[]{-17.3f,-29.3f,-40.3f})Detail("Threshold stone band",new Vector3(0,.039f,z),new Vector3(34,.008f,.22f),panel);
            foreach(var p in new[]{new Vector3(-6,2.8f,-45),new Vector3(5,2.8f,-24),new Vector3(13,3.5f,-52)}){
                var l=LightAt("Bank warm accent",p,new Color(1,.84f,.60f),2,7);l.transform.SetParent(decor,true);
            }
        }
        // 안내판 글자가 프레임 밖으로 나오지 않도록 실제 메시 너비에 맞춘다.
        foreach(Transform group in decor)if(group.name.StartsWith("Bank information"))foreach(var t in group.GetComponentsInChildren<TextMesh>()){
            var width=t.GetComponent<Renderer>().bounds.size;float w=Mathf.Max(width.x,width.z);if(w>2.4f)t.characterSize*=2.4f/w;
        }
        AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);CaptureDressing();
    }
    public static void VerifyDressingAndTest(){
        var scene=EditorSceneManager.OpenScene(ScenePath);var d=UnityEngine.Object.FindFirstObjectByType<BankPrologue>();
        var root=scene.GetRootGameObjects().Single(g=>g.name=="BANK / Saved Architecture").transform.Cast<Transform>().SingleOrDefault(t=>t.name=="Bank atmosphere / saved");
        if(!root||root.GetComponentsInChildren<Renderer>().Any(r=>r.sharedMaterials.Any(m=>!m||!m.shader||m.shader.name.Contains("InternalError"))))throw new Exception("Missing dressing material.");
        var points=new[]{new Vector3(0,0,12),new Vector3(14,0,-22),new Vector3(-14,0,-34),new Vector3(14,0,-45),new Vector3(14,0,-56),d.exitPoint.position};var path=new NavMeshPath();
        for(int i=1;i<points.Length;i++)if(!NavMesh.CalculatePath(points[i-1],points[i],NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)throw new Exception("Decor blocked escape segment "+i);
        var squad=d.GetComponent<ProloguePoliceSquad>();
        for(int i=0;i<squad.hiding.Length;i++)if(!NavMesh.CalculatePath(squad.hiding[i].position,squad.peeking[i].position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)throw new Exception("Decor blocked cover "+i);
        File.WriteAllText("Logs/Bank-dressing-validation.txt","PASS: decorative materials present; all escape segments connected; all 32 police cover routes connected.\n");
        BankPrologueCheck.StartTest();
    }
    public static void CaptureDressing(){
        var scene=SceneManager.GetSceneByPath(ScenePath);if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(ScenePath);
        var d=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<BankPrologue>(true)).Single();
        var cam=d.filmCamera;var p=cam.transform.position;var q=cam.transform.rotation;var fov=cam.fieldOfView;cam.fieldOfView=65;
        try{
            DecorCapture(cam,new Vector3(11,2.4f,-43),new Vector3(-5,1.7f,-47),"lobby");
            DecorCapture(cam,new Vector3(9,2.2f,-16),new Vector3(-5,1.8f,-11),"tellers");
            DecorCapture(cam,new Vector3(4,2.2f,3.5f),new Vector3(0,2,8),"vault");
            DecorCapture(cam,new Vector3(-15,5,-72),new Vector3(1,4,-50),"exterior");
        }finally{cam.transform.SetPositionAndRotation(p,q);cam.fieldOfView=fov;}
    }
    static void DecorCapture(Camera cam,Vector3 p,Vector3 target,string name){
        cam.transform.position=p;cam.transform.LookAt(target);var rt=new RenderTexture(1600,900,24);var old=cam.targetTexture;var active=RenderTexture.active;
        try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var tex=new Texture2D(1600,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1600,900),0,0);tex.Apply();File.WriteAllBytes("Logs/Bank-dressing-"+name+".png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);}
        finally{cam.targetTexture=old;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
    }
}
}
#endif
