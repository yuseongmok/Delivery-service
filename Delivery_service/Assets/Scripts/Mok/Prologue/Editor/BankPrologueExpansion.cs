#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.AI.Navigation;
namespace DeliveryService.Mok.Editor {
public static partial class BankPrologueBuilder {
    [MenuItem("Tools/Delivery Service/Mok/Expand Bank Prologue")]
    public static void Expand() {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play before expanding.");
        var scene=SceneManager.GetSceneByPath(ScenePath);
        if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        string backup="Temp/BankPrologueExpansion-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity";
        EditorSceneManager.SaveScene(scene,backup,true);
        var d=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<BankPrologue>(true)).Single();
        world=scene.GetRootGameObjects().Single(g=>g.name=="BANK / Saved Architecture").transform;
        if(world.Find("Expanded Bank"))throw new Exception("Expanded Bank already exists; edit the saved scene rather than replacing it.");
        font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/BMJUA_ttf.ttf");
        stone=LoadMat("Limestone");dark=LoadMat("Gunmetal");brass=LoadMat("Brass");teal=LoadMat("BankGreen");
        wood=LoadMat("Walnut");paper=LoadMat("Cash");ink=LoadMat("Band");white=LoadMat("Ivory");road=LoadMat("Road");
        // 원본 외벽 장식은 비활성으로 보존하고 실내와 금고의 참조는 유지한다.
        string[] retired={"Facade","Entrance jamb","Entrance lintel","Bank sign board","UNION BANK","Street scenery","Backdrop building","Backdrop window","Street marking"};
        foreach(Transform t in world)if(retired.Contains(t.name))t.gameObject.SetActive(false);
        foreach(var vehicle in scene.GetRootGameObjects())
            if(vehicle.name=="SM_Veh_Car_Police_01")vehicle.SetActive(false);
        var extension=new GameObject("Expanded Bank").transform;extension.SetParent(world,false);
        Box("Bank extension foundation",new Vector3(0,-.25f,-29),new Vector3(36,.5f,42),stone,extension);
        Box("Extension roof",new Vector3(0,5,-29),new Vector3(36,.3f,42),white,extension);
        for(int x=-16;x<=16;x+=4)for(int z=-10;z>=-48;z-=4)
            Box("Stone tile",new Vector3(x,.012f,z),new Vector3(3.96f,.015f,3.96f),((x/4+z/2)%2==0)?stone:white,extension,false);
        foreach(int side in new[]{-1,1}) {
            Box("Outer wall",new Vector3(side*18,2.5f,-29),new Vector3(.4f,5,42),teal,extension);
            Box("North wing wall",new Vector3(side*14,2.5f,-8),new Vector3(8,5,.4f),stone,extension);
        }
        Box("Security divider",new Vector3(-4,2.5f,-18),new Vector3(28,5,.5f),teal,extension);
        Box("Office divider",new Vector3(4,2.5f,-30),new Vector3(28,5,.5f),stone,extension);
        Box("Lobby divider",new Vector3(-4,2.5f,-41),new Vector3(28,5,.5f),teal,extension);
        Box("Main facade",new Vector3(-3.5f,2.5f,-50),new Vector3(29,5,.5f),teal,extension);
        Box("Right facade",new Vector3(16.5f,2.5f,-50),new Vector3(3,5,.5f),teal,extension);
        Box("Exit lintel",new Vector3(13,4.5f,-50),new Vector3(4,1,.5f),dark,extension);
        Sign("보안 통로  →",new Vector3(2,3.5f,-17.65f),extension);
        Sign("←  사무실 / 출구",new Vector3(0,3.5f,-29.65f),extension);
        Sign("대기 홀 / 정문  →",new Vector3(2,3.5f,-40.65f),extension);
        Sign("EXIT",new Vector3(13,3.7f,-49.65f),extension);
        for(int z=-12;z>=-46;z-=11) {
            foreach(int x in new[]{-7,0,7}) {
                Box("Marble column",new Vector3(x,2.4f,z),new Vector3(.7f,4.8f,.7f),white,extension);
                Box("Column trim",new Vector3(x,.25f,z),new Vector3(.9f,.5f,.9f),brass,extension);
            }
            for(int x=-12;x<=12;x+=12) {
                var light=LightAt("Extension ceiling light",new Vector3(x,4,z),new Color(1,.86f,.67f),4,12);
                light.transform.SetParent(extension,true);
                Box("Light panel",new Vector3(x,4.8f,z),new Vector3(2.5f,.1f,.7f),white,extension,false);
            }
        }
        foreach(float z in new[]{-13f,-25f,-36f,-46f})
            foreach(float x in new[]{-6f,5f}) {
                Box("Cover desk",new Vector3(x,.65f,z),new Vector3(3,1.3f,1.3f),wood,extension);
                Box("Desk top",new Vector3(x,1.35f,z),new Vector3(3.2f,.12f,1.5f),dark,extension);
                Box("Computer monitor",new Vector3(x,1.7f,z),new Vector3(.7f,.55f,.12f),dark,extension,false);
                Box("Paperwork",new Vector3(x+.8f,1.46f,z),new Vector3(.45f,.08f,.35f),white,extension,false);
            }
        for(int x=-13;x<8;x+=5) {
            Box("Waiting sofa",new Vector3(x,.45f,-47.5f),new Vector3(3,.6f,1),wood,extension);
            Box("Sofa back",new Vector3(x,.9f,-48),new Vector3(3,1.2f,.2f),teal,extension);
        }
        Box("Escape street",new Vector3(35,-.25f,-65),new Vector3(120,.5f,30),road,extension);
        for(int x=-20;x<=90;x+=7)Box("Road stripe",new Vector3(x,.01f,-70),new Vector3(3,.02f,.14f),ink,extension,false);
        for(int i=0;i<4;i++){
            var car=Vehicle("SM_Veh_Car_Police_01",new Vector3(-11+i*6,0,-56-(i%2)*4),i%2==0?50:-35);
            car.name="Reinforcement car "+i;
        }
        d.getawayCar.position=new Vector3(17,0,-64);d.exitPoint.position=new Vector3(15,0,-61);
        SetShot(d.shots[2],new Vector3(10,2,-43),new Vector3(13,1,-54));
        SetShot(d.shots[3],new Vector3(10,2.5f,-69),new Vector3(17,1,-64));
        d.escapeRoute=new[]{
            Point("01 Security passage",new Vector3(14,0,-22),extension),
            Point("02 Office passage",new Vector3(-14,0,-34),extension),
            Point("03 Lobby passage",new Vector3(14,0,-45),extension)};
        d.policeEntryTargets=new[]{
            Point("Police entry left",new Vector3(12,0,-46),extension),
            Point("Police entry right",new Vector3(14,0,-46),extension)};
        d.initialPolice=8;d.waveSize=4;d.maxActivePolice=16;d.waveInterval=7;
        var police=d.officers.ToList();
        while(police.Count<40)police.Add(Officer(police.Count,Vector3.zero));
        d.officers=police.ToArray();
        Vector3[] initial={
            new Vector3(12,0,-54),new Vector3(15,0,-55),
            new Vector3(-5,0,-4),new Vector3(5,0,-4),
            new Vector3(14,0,-25),new Vector3(-14,0,-33),
            new Vector3(12,0,-46),new Vector3(15,0,-47)};
        for(int i=0;i<d.officers.Length;i++) {
            d.officers[i].transform.position=i<initial.Length?initial[i]:new Vector3(11+(i%4)*1.4f,0,-54-(i/4)*1.2f);
            d.officers[i].director=d;d.officers[i].gameObject.SetActive(false);
        }
        InstallRifle(d.player);
        string storyPath=Art+"/BankPrologueStory.asset";
        d.story=AssetDatabase.LoadAssetAtPath<PrologueStory>(storyPath);
        if(!d.story){d.story=ScriptableObject.CreateInstance<PrologueStory>();AssetDatabase.CreateAsset(d.story,storyPath);}
        // 기존 내비게이션 에셋의 GUID를 유지한 채 확장 지형을 다시 굽는다.
        var surface=world.GetComponent<NavMeshSurface>();var old=surface.navMeshData;
        surface.RemoveData();surface.navMeshData=null;surface.BuildNavMesh();
        var baked=surface.navMeshData;
        if(!baked)throw new Exception("Expanded NavMesh bake failed.");
        surface.RemoveData();EditorUtility.CopySerialized(baked,old);EditorUtility.SetDirty(old);
        surface.navMeshData=old;surface.AddData();UnityEngine.Object.DestroyImmediate(baked);
        AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
        Selection.activeObject=d.story;
        File.WriteAllText("Temp/BankPrologue-expansion.txt","Saved expanded bank, three route checkpoints, 40 police, M4 rifle, reload parts, and editable story.\nBackup: "+backup);
    }
    static Material LoadMat(string name)=>AssetDatabase.LoadAssetAtPath<Material>(Art+"/"+name+".mat");
    static Transform Point(string name,Vector3 pos,Transform parent){var t=new GameObject(name).transform;t.SetParent(parent,false);t.position=pos;return t;}
    static void SetShot(Transform t,Vector3 pos,Vector3 target){t.position=pos;t.LookAt(target);}
    static void Sign(string value,Vector3 pos,Transform parent){
        var g=new GameObject(value);g.transform.SetParent(parent,false);g.transform.position=pos;g.transform.rotation=Quaternion.Euler(0,180,0);
        var t=g.AddComponent<TextMesh>();t.font=font;t.text=value;t.fontSize=64;t.characterSize=.075f;t.anchor=TextAnchor.MiddleCenter;t.color=ink.color;
        g.GetComponent<MeshRenderer>().sharedMaterial=font.material;
    }
    public static void InstallRifle(ProloguePlayer p){
        var old=p.weapon;p.weapon=CreateGun(p.view.transform,"M4_8");p.weapon.name="Held M4 rifle";
        p.weapon.transform.localPosition=new Vector3(.21f,-.23f,.4f);
        p.weapon.transform.localRotation=Quaternion.Euler(0,-2,-4);
        p.magazine=p.weapon.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Mag");
        p.bolt=p.weapon.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Bolt");
        p.muzzle.transform.SetParent(p.weapon.transform,false);p.muzzle.transform.localPosition=new Vector3(0,.06f,.73f);
        foreach(var t in p.weapon.GetComponentsInChildren<Transform>(true))t.gameObject.layer=2;
        p.magazineSize=30;p.shotInterval=.1f;p.reloadSeconds=2.2f;
        UnityEngine.Object.DestroyImmediate(old);
    }
}
}
#endif
