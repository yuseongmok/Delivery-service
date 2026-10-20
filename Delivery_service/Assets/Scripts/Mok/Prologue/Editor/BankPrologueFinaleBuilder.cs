#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.AI;
using UnityEngine.UI;
namespace DeliveryService.Mok.Editor {
public static partial class BankPrologueBuilder {
    public static void FixWorldText(){
        var d=UnityEngine.Object.FindFirstObjectByType<BankPrologue>();var scene=d.gameObject.scene;
        EditorSceneManager.SaveScene(scene,"Temp/Bank-before-text-fix-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
        string path=Art+"/WorldText.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!material){material=new Material(Shader.Find("Mok/GarageDepthText"));AssetDatabase.CreateAsset(material,path);}
        foreach(var label in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<TextMesh>(true))){
            material.mainTexture=label.font.material.mainTexture;label.GetComponent<Renderer>().sharedMaterial=material;
            if(!label.GetComponent<PrologueDepthText>())label.gameObject.AddComponent<PrologueDepthText>();
            if(label.text=="UNION BANK"&&label.gameObject.activeInHierarchy){var p=label.transform.position;p.z=-51.1f;label.transform.position=p;}
        }
        foreach(var plate in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Where(t=>t.name=="Facade name plate")){
            var p=plate.position;p.z=-50.9f;plate.position=p;
        }
        AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
    }
    public static void PolishFinale(){
        var d=UnityEngine.Object.FindFirstObjectByType<BankPrologue>();var scene=d.gameObject.scene;
        EditorSceneManager.SaveScene(scene,"Temp/Bank-before-vista-polish-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
        UnityEngine.Object.DestroyImmediate(d.cityVista);
        BuildCityVista(d,scene);
        EditorSceneManager.SaveScene(scene);
    }
    [MenuItem("Tools/Delivery Service/Mok/Upgrade Bank Escape")]
    public static void UpgradeEscape(){
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first.");
        var scene=SceneManager.GetSceneByPath(ScenePath);
        if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        if(!EditorSceneManager.SaveScene(scene,"Temp/Bank-before-finale-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))
            throw new Exception("Scene backup failed.");
        SceneManager.SetActiveScene(scene);
        var d=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<BankPrologue>(true)).Single();
        if(d.drive)throw new Exception("Finale already installed. Preserve scene edits.");
        world=scene.GetRootGameObjects().Single(g=>g.name=="BANK / Saved Architecture").transform;
        font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/BMJUA_ttf.ttf");
        stone=LoadMat("Limestone");dark=LoadMat("Gunmetal");brass=LoadMat("Brass");teal=LoadMat("BankGreen");
        wood=LoadMat("Walnut");paper=LoadMat("Cash");ink=LoadMat("Band");white=LoadMat("Ivory");road=LoadMat("Road");
        var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Scripts/Min/Prefab/NPC/Police.prefab");
        foreach(var o in d.officers)InstallOfficerRig(o,source);
        var avatar=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Scripts/Mok/Art/Player/DeliveryPlayer.prefab"),d.player.transform);
        avatar.name="Prologue player physical body";avatar.transform.localPosition=Vector3.zero;avatar.transform.localRotation=Quaternion.identity;
        foreach(var b in avatar.GetComponentsInChildren<MonoBehaviour>(true))UnityEngine.Object.DestroyImmediate(b);
        d.player.ragdoll=ConfigureRig(d.player.gameObject,avatar.GetComponentInChildren<Animator>());
        d.player.avatarRenderers=avatar.GetComponentsInChildren<Renderer>(true);
        foreach(var r in d.player.avatarRenderers)r.enabled=false;
        DressBankExterior();
        var drive=d.getawayCar.gameObject.AddComponent<PrologueDrive>();d.drive=drive;drive.director=d;
        var rb=d.getawayCar.gameObject.AddComponent<Rigidbody>();rb.isKinematic=true;rb.useGravity=false;rb.interpolation=RigidbodyInterpolation.Interpolate;
        drive.finish=Point("Escape road finish",new Vector3(238,0,-65),world);
        drive.wheels=d.getawayCar.GetComponentsInChildren<Transform>().Where(t=>t.name.ToLowerInvariant().Contains("wheel")&&t.GetComponent<MeshFilter>()).ToArray();
        d.getawayCar.SetPositionAndRotation(new Vector3(17,0,-65),Quaternion.Euler(0,90,0));
        d.roadPolice=new PrologueOfficer[18];
        for(int i=0;i<18;i++){
            var o=Officer(40+i,new Vector3(65+(i/6)*57,0,-71+(i%6)*2.2f));
            o.name="Roadblock officer "+(i+1);o.director=d;o.roadGuard=true;o.transform.rotation=Quaternion.Euler(0,-90,0);
            InstallOfficerRig(o,source);d.roadPolice[i]=o;
        }
        BuildCityVista(d,scene);
        var logoRoot=Rect(d.objective.canvas.transform,"Game logo reveal",new Vector2(.2f,.3f),new Vector2(.8f,.7f));
        d.logo=logoRoot.gameObject.AddComponent<CanvasGroup>();d.logo.alpha=0;d.logo.blocksRaycasts=false;d.logo.interactable=false;
        var image=Rect(logoRoot,"Existing game logo",Vector2.zero,Vector2.one).gameObject.AddComponent<RawImage>();
        image.texture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Scripts/Mok/Art/Lobby/Exhibition/DeliveryLogo.png");
        image.raycastTarget=false;
        var ratio=image.gameObject.AddComponent<AspectRatioFitter>();ratio.aspectMode=AspectRatioFitter.AspectMode.FitInParent;
        ratio.aspectRatio=(float)image.texture.width/image.texture.height;
        d.pausePanel.transform.SetAsLastSibling();
        EditorUtility.SetDirty(d.story);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject=d.gameObject;
        File.WriteAllText("Temp/Bank-finale-build.txt","PASS: bank exterior, physical rigs, 220m player-driven escape, 18 roadblock officers, saved city vista and logo.");
    }
    static void InstallOfficerRig(PrologueOfficer o,GameObject source){
        var map=o.GetComponentsInChildren<Transform>(true).ToDictionary(t=>AnimationUtility.CalculateTransformPath(t,o.transform));
        Transform Map(Transform t)=>map[AnimationUtility.CalculateTransformPath(t,source.transform)];
        foreach(var rb in source.GetComponentsInChildren<Rigidbody>(true)){
            if(rb.transform==source.transform)continue;
            var copy=Map(rb.transform).gameObject.AddComponent<Rigidbody>();EditorUtility.CopySerialized(rb,copy);copy.isKinematic=true;
        }
        foreach(var col in source.GetComponentsInChildren<Collider>(true)){
            if(col.transform==source.transform)continue;
            var copy=(Collider)Map(col.transform).gameObject.AddComponent(col.GetType());EditorUtility.CopySerialized(col,copy);copy.enabled=false;
        }
        foreach(var j in source.GetComponentsInChildren<CharacterJoint>(true)){
            var copy=Map(j.transform).gameObject.AddComponent<CharacterJoint>();EditorUtility.CopySerialized(j,copy);
            copy.connectedBody=j.connectedBody?Map(j.connectedBody.transform).GetComponent<Rigidbody>():null;
            copy.enableProjection=true;copy.projectionDistance=.08f;copy.projectionAngle=15;
        }
        o.ragdoll=ConfigureRig(o.gameObject,o.animator);
        o.gun.SetParent(o.animator.GetBoneTransform(HumanBodyBones.RightHand),true);
    }
    static PrologueRagdoll ConfigureRig(GameObject root,Animator animator){
        var rig=root.AddComponent<PrologueRagdoll>();rig.animator=animator;
        rig.bodies=animator.GetComponentsInChildren<Rigidbody>(true).Where(b=>b.transform!=root.transform).ToArray();
        rig.boneColliders=animator.GetComponentsInChildren<Collider>(true).Where(c=>c.transform!=root.transform).ToArray();
        rig.hips=animator.GetBoneTransform(HumanBodyBones.Hips);
        if(rig.bodies.Length<6||!rig.hips)throw new Exception("Missing physical rig: "+root.name);
        foreach(var rb in rig.bodies){rb.isKinematic=true;rb.interpolation=RigidbodyInterpolation.Interpolate;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;rb.solverIterations=12;rb.solverVelocityIterations=4;}
        foreach(var c in rig.boneColliders)c.enabled=false;
        return rig;
    }
    static void DressBankExterior(){
        var root=new GameObject("Bank exterior and escape boulevard").transform;root.SetParent(world,false);
        Box("Escape boulevard",new Vector3(145,-.25f,-65),new Vector3(260,.5f,24),road,root);
        foreach(int side in new[]{-1,1}){
            Box("Boulevard pavement",new Vector3(125,-.1f,-65+side*14),new Vector3(300,.2f,4),stone,root);
            Box("Road boundary",new Vector3(125,.5f,-65+side*16),new Vector3(300,1,.35f),dark,root);
        }
        for(int x=25;x<250;x+=8)Box("Centre road marking",new Vector3(x,.018f,-65),new Vector3(3,.02f,.15f),white,root,false);
        // 벽 위에 덧댄 석재와 창문은 출구 충돌을 바꾸지 않는다.
        Box("Bank upper storey",new Vector3(0,8,-29),new Vector3(36,6,42),stone,root);
        Box("Bank cornice",new Vector3(0,11.2f,-29),new Vector3(37,.45f,43),white,root);
        Box("Facade name plate",new Vector3(-2,5.5f,-50.4f),new Vector3(25,1.1f,.25f),dark,root,false);
        ExteriorSign("UNION BANK",new Vector3(-2,5.5f,-50.6f),.13f,root);
        for(int x=-16;x<=16;x+=4){
            Box("Facade pilaster",new Vector3(x,5.4f,-50.4f),new Vector3(.55f,10.8f,.6f),white,root);
            Box("Capital",new Vector3(x,10.6f,-50.4f),new Vector3(.85f,.4f,.85f),brass,root,false);
            if(x<10)Box("Lower bank window",new Vector3(x+1.6f,2.7f,-50.29f),new Vector3(2,2.5f,.08f),dark,root,false);
            Box("Upper window",new Vector3(x+1.4f,8,-50.08f),new Vector3(1.8f,2.8f,.1f),teal,root,false);
            Box("Window mullion",new Vector3(x+1.4f,8,-50.17f),new Vector3(.08f,2.8f,.08f),brass,root,false);
        }
        Box("Entrance canopy",new Vector3(13,4.3f,-52),new Vector3(5,.3f,4),dark,root);
        for(int x=-17;x<=8;x+=5){
            Box("Planter",new Vector3(x,.4f,-53),new Vector3(2,.8f,1.2f),stone,root);
            Box("Trimmed hedge",new Vector3(x,1,-53),new Vector3(1.8f,.6f,1),teal,root,false);
        }
        for(int x=-20;x<250;x+=20)foreach(int side in new[]{-1,1}){
            Vector3 p=new Vector3(x,0,-65+side*13);
            Cylinder("Street lamp post",p+Vector3.up*3,new Vector3(.12f,3,.12f),dark,root);
            Box("Street lamp head",p+Vector3.up*6,new Vector3(1,.2f,.7f),brass,root,false);
            var light=LightAt("Boulevard light",p+Vector3.up*5.8f,new Color(1,.78f,.45f),3,11);light.transform.SetParent(root,true);
        }
        for(int x=40;x<250;x+=28)foreach(int side in new[]{-1,1}){
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonCity/Prefabs/Buildings/SM_Bld_Apartment_01.prefab");
            var g=UnityEngine.Object.Instantiate(source,root);Strip(g);
            g.transform.rotation=Quaternion.Euler(0,side>0?180:0,0);
            var rs=g.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
            g.transform.localScale*=23/Mathf.Max(b.size.x,b.size.z);
            b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
            g.transform.position+=new Vector3(x,0,-65+side*30)-new Vector3(b.center.x,b.min.y,b.center.z);
        }
        for(int i=0;i<3;i++){
            var p=Vehicle("SM_Veh_Car_Police_01",new Vector3(59+i*57,0,i%2==0?-73:-57),90);
            p.SetParent(root,true);var col=p.gameObject.AddComponent<BoxCollider>();col.center=new Vector3(0,.7f,0);col.size=new Vector3(1.8f,1.4f,4);
        }
    }
    static void ExteriorSign(string value,Vector3 pos,float size,Transform parent){
        var g=new GameObject(value);g.transform.SetParent(parent,false);g.transform.position=pos;
        var t=g.AddComponent<TextMesh>();t.font=font;t.text=value;t.fontSize=64;t.characterSize=size;t.anchor=TextAnchor.MiddleCenter;t.color=brass.color;
        g.GetComponent<MeshRenderer>().sharedMaterial=font.material;
    }
    static void BuildCityVista(BankPrologue d,Scene bank){
        var preview=EditorSceneManager.OpenPreviewScene(d.gameScene);
        try{
            var roots=preview.GetRootGameObjects();
            var excludedLods=new System.Collections.Generic.HashSet<Renderer>();
            foreach(var group in roots.SelectMany(g=>g.GetComponentsInChildren<LODGroup>(true))){
                var lods=group.GetLODs();foreach(var lod in lods.Skip(1))foreach(var r in lod.renderers)if(r)excludedLods.Add(r);
                if(lods.Length>0)foreach(var r in lods[0].renderers)excludedLods.Remove(r);
            }
            var vista=new GameObject("City arrival / saved visual copy");SceneManager.MoveGameObjectToScene(vista,bank);d.cityVista=vista;
            Vector3 offset=new Vector3(2500,0,0);
            bool first=true;Bounds city=new Bounds();int count=0;
            foreach(var source in roots.SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true))){
                var mesh=source.GetComponent<MeshFilter>();
                if(!mesh||!mesh.sharedMesh||!source.enabled||!source.gameObject.activeInHierarchy||source.GetComponentInParent<Animator>()||source.GetComponentInParent<CarMov>()||source.GetComponentInParent<Canvas>()||excludedLods.Contains(source))continue;
                // 지도 전용 회색 도형은 실제 카메라에 보이지 않는 별도 레이어다.
                string layer=LayerMask.LayerToName(source.gameObject.layer);
                if(layer=="Navigation"||layer=="NavigationUI"||layer=="UI"||source.forceRenderingOff)continue;
                if(source.name.IndexOf("sky",StringComparison.OrdinalIgnoreCase)>=0||source.name.StartsWith("Ocean_"))continue;
                if(source.GetComponentsInParent<Transform>(true).Any(t=>t.name.StartsWith("Ocean_")||t.name.IndexOf("Skydome",StringComparison.OrdinalIgnoreCase)>=0||t.name.IndexOf("Navigation",StringComparison.OrdinalIgnoreCase)>=0))continue;
                if(source.bounds.size.magnitude>2500||!EditorUtility.IsPersistent(mesh.sharedMesh))continue;
                var copy=new GameObject(source.name,typeof(MeshFilter),typeof(MeshRenderer));copy.transform.SetParent(vista.transform,false);
                copy.transform.SetPositionAndRotation(source.transform.position+offset,source.transform.rotation);copy.transform.localScale=source.transform.lossyScale;
                copy.GetComponent<MeshFilter>().sharedMesh=mesh.sharedMesh;copy.GetComponent<MeshRenderer>().sharedMaterials=source.sharedMaterials;
                if(source.bounds.size.y>8&&source.bounds.size.y<200&&source.bounds.size.x<200&&source.bounds.size.z<200){
                    if(first){city=source.bounds;first=false;}else city.Encapsulate(source.bounds);
                }
                count++;
            }
            if(first||count<100)throw new Exception("City geometry snapshot incomplete.");
            var bridge=roots.SelectMany(g=>g.GetComponentsInChildren<BoxCollider>(true))
                .Where(c=>c.name=="Bridge smooth collision"&&c.gameObject.activeInHierarchy)
                .OrderByDescending(c=>Mathf.Max(c.bounds.size.x,c.bounds.size.z)).FirstOrDefault();
            if(!bridge)throw new Exception("No city bridge for arrival shot.");
            Bounds deck=bridge.bounds;Vector3 axis=deck.size.x>deck.size.z?Vector3.right:Vector3.forward;
            File.WriteAllText("Temp/Bank-bridge-renderers.txt",string.Join("\n",roots.SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true)).Where(r=>r.bounds.Contains(deck.center)||r.bounds.Intersects(new Bounds(deck.center,new Vector3(30,.15f,4)))).Select(r=>r.name+" layer "+r.gameObject.layer+" "+r.bounds+" parent "+(r.transform.parent?r.transform.parent.name:"none")+" mat "+string.Join(",",r.sharedMaterials.Select(m=>m?m.name:"null")))));
            if(Vector3.Dot(city.center-deck.center,axis)<0)axis=-axis;
            float length=Mathf.Min(35,Mathf.Max(deck.size.x,deck.size.z)*.55f);
            Vector3 start=deck.center-axis*length*.5f;start.y=deck.max.y+.03f;
            d.cityCarStart=Point("City bridge arrival start",start+offset,vista.transform);d.cityCarStart.rotation=Quaternion.LookRotation(axis);
            d.cityCarEnd=Point("City bridge arrival end",start+axis*length+offset,vista.transform);
            Vector3 center=city.center+offset;
            d.cityLookTarget=Point("City panorama target",center,vista.transform);
            float span=Mathf.Max(city.size.x,city.size.z);
            d.cityWideCamera=Point("City panorama camera",center+new Vector3(-span*.15f,span*.50f,-span*.48f),vista.transform);
            d.cityWideCamera.LookAt(center);
            var water=GameObject.CreatePrimitive(PrimitiveType.Cube);water.name="Cinematic water";
            water.transform.SetParent(vista.transform,false);water.transform.position=new Vector3(center.x,-2,center.z);water.transform.localScale=new Vector3(2200,.1f,2200);
            UnityEngine.Object.DestroyImmediate(water.GetComponent<Collider>());
            string waterPath=Art+"/VistaWater.mat";var waterMat=AssetDatabase.LoadAssetAtPath<Material>(waterPath);
            if(!waterMat){waterMat=new Material(Shader.Find("Universal Render Pipeline/Lit"));waterMat.color=new Color(.045f,.16f,.23f);waterMat.SetFloat("_Smoothness",.65f);AssetDatabase.CreateAsset(waterMat,waterPath);}
            water.GetComponent<Renderer>().sharedMaterial=waterMat;
            vista.SetActive(false);
            File.WriteAllText("Temp/Bank-city-vista.txt","Renderers: "+count+"\nBounds: "+city+"\nBridge: "+deck+"\nCar start: "+d.cityCarStart.position+"\nCar end: "+d.cityCarEnd.position);
        }finally{EditorSceneManager.ClosePreviewScene(preview);SceneManager.SetActiveScene(bank);}
    }
}
}
#endif
