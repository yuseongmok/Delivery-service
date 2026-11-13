#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.AI;
using Unity.AI.Navigation;
namespace DeliveryService.Mok.NewCity.Editor
{
    [InitializeOnLoad]
    public static class NewCityBuilder
    {
        const string ScenePath="Assets/Scenes/NewHarborCity.unity";
        static Material pavement,asphalt,paint,grass,stone,trim;
        static readonly Vector3 Spawn=new Vector3(0,1.05f,-7);
        static NewCityBuilder(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/NewHarborBuild"))return;
            string action; try { action=File.ReadAllText("Temp/NewHarborBuild").Trim();File.Delete("Temp/NewHarborBuild"); } catch(IOException) { return; }
            try{if(action=="capture")Capture();else Build(action=="rebuild");}catch(Exception e){File.WriteAllText("Logs/NewHarbor-build.txt",e.ToString());Debug.LogException(e);}
        }
        [MenuItem("Tools/Delivery Service/Mok/Create New Harbor City")]
        public static void Create()=>Build(false);
        static void Build(bool rebuild)
        {
            Directory.CreateDirectory("Logs");var current=SceneManager.GetActiveScene();
            EditorSceneManager.SaveScene(current,"Logs/Before-NewHarbor-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
            if(current.isDirty)throw new Exception("현재 씬에 저장하지 않은 변경이 있습니다. 백업을 만들었으며 원본을 보존하기 위해 생성을 중단했습니다.");
            if(File.Exists(ScenePath)&&!rebuild)throw new Exception("새 지역 씬이 이미 존재합니다. 수동 편집을 보존합니다.");
            if(rebuild&&File.Exists(ScenePath))File.Copy(ScenePath,"Logs/NewHarbor-before-rebuild-"+DateTime.Now.ToString("HHmmss")+".unity",true);
            Directory.CreateDirectory(DistrictArt.Folder+"/Materials");AssetDatabase.Refresh();
            DistrictArt.Scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            DistrictArt.Parent=new GameObject("NEW HARBOR • 새로운 출발의 거리").transform;
            var cityRoot=DistrictArt.Parent;
            Lighting();Ground();DistrictPizzaShop.Build();DistrictArt.Parent=cityRoot;Blocks();Details();Survey();
            EditorSceneManager.SaveScene(DistrictArt.Scene,ScenePath);BuildNavigation();AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(DistrictArt.Scene);
            Physics.SyncTransforms();Validate();Capture();
            SceneView.lastActiveSceneView?.LookAt(new Vector3(0,2,7),Quaternion.Euler(25,25,0),45);
            Selection.activeGameObject=cityRoot.gameObject;
        }
        static void Lighting()
        {
            var skySource=AssetDatabase.LoadAssetAtPath<Material>("Assets/Scripts/Mok/Art/Lobby/DawnSky.mat");
            var sky=AssetDatabase.LoadAssetAtPath<Material>(DistrictArt.Folder+"/Materials/HarborSky.mat");
            if(!sky){sky=skySource?new Material(skySource):new Material(Shader.Find("Skybox/Procedural"));AssetDatabase.CreateAsset(sky,DistrictArt.Folder+"/Materials/HarborSky.mat");}
            if(sky.HasProperty("_SkyTint"))sky.SetColor("_SkyTint",new Color(.47f,.64f,.78f));if(sky.HasProperty("_Exposure"))sky.SetFloat("_Exposure",1.15f);
            RenderSettings.skybox=sky;RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.61f,.70f,.78f);RenderSettings.ambientEquatorColor=new Color(.48f,.49f,.44f);RenderSettings.ambientGroundColor=new Color(.27f,.29f,.26f);RenderSettings.ambientIntensity=1;
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogColor=new Color(.66f,.74f,.77f);RenderSettings.fogStartDistance=130;RenderSettings.fogEndDistance=290;
            var sun=new GameObject("늦은 오후 햇살").AddComponent<Light>();sun.type=LightType.Directional;sun.color=new Color(1,.86f,.68f);sun.intensity=1.35f;sun.shadows=LightShadows.Soft;sun.transform.rotation=Quaternion.Euler(44,-32,0);RenderSettings.sun=sun;
        }
        static void Ground()
        {
            var root=DistrictArt.Parent;DistrictArt.Parent=DistrictArt.Group("01 거리와 광장");
            pavement=DistrictArt.Pattern("Paving blocks",new Color(.59f,.61f,.55f),new Color(.46f,.49f,.44f),false);
            asphalt=DistrictArt.Mat("Asphalt",new Color(.17f,.20f,.21f));paint=DistrictArt.Mat("Road ivory",new Color(.9f,.83f,.61f));grass=DistrictArt.Mat("Garden green",new Color(.31f,.41f,.19f));stone=DistrictArt.Mat("Sandstone",new Color(.65f,.60f,.48f));trim=DistrictArt.Mat("Street iron",new Color(.12f,.22f,.22f));
            DistrictArt.Box("지역 기반",new Vector3(0,-.25f,0),new Vector3(190,.4f,160),pavement);
            foreach(float z in new[]{-16,40})DistrictArt.Box("동서 도로",new Vector3(0,-.025f,z),new Vector3(190,.04f,10),asphalt,false);
            foreach(float x in new[]{-40,40})DistrictArt.Box("남북 도로",new Vector3(x,-.023f,0),new Vector3(10,.04f,160),asphalt,false);
            foreach(float z in new[]{-16,40})for(float x=-90;x<91;x+=6)if(Mathf.Abs(Mathf.Abs(x)-40)>8)DistrictArt.Box("중앙 점선",new Vector3(x,.002f,z),new Vector3(2.6f,.012f,.12f),paint,false);
            foreach(float x in new[]{-40,40})for(float z=-72;z<75;z+=6)if(Mathf.Abs(z+16)>8&&Mathf.Abs(z-40)>8)DistrictArt.Box("중앙 점선",new Vector3(x,.003f,z),new Vector3(.12f,.012f,2.6f),paint,false);
            foreach(float x in new[]{-40,40})foreach(float z in new[]{-16,40})for(int i=-3;i<=3;i++)DistrictArt.Box("횡단보도",new Vector3(x+i*1.1f,.009f,z+7),new Vector3(.6f,.014f,2.7f),paint,false);
            foreach(float z in new[]{-10.7f,-21.3f,34.7f,45.3f})for(float x=-92;x<94;x+=4)if(Mathf.Abs(Mathf.Abs(x)-40)>7)DistrictArt.Box("낮은 보도 경계석",new Vector3(x,.03f,z),new Vector3(3.9f,.12f,.22f),stone);
            // 매장 전면을 비워 둔 광장과 두 개의 가로수 정원.
            foreach(float x in new[]{-24,24}){DistrictArt.Box("매장 앞 정원 테두리",new Vector3(x,.22f,-4),new Vector3(10,.5f,4),stone);DistrictArt.Box("정원 흙",new Vector3(x,.49f,-4),new Vector3(9.5f,.04f,3.5f),grass,false);foreach(float dx in new[]{-3f,3f})DistrictArt.Prop("SM_Env_Tree_02",new Vector3(x+dx,.52f,-4),5.2f);}
            // 지역 이동을 연결할 도로 끝과 도착 기준점. 씬 이동 로직은 아직 연결하지 않는다.
            Marker("기존 도시 방향 • 이동 연결점",new Vector3(-86,.05f,-16));Marker("새 도시 도착 지점",new Vector3(-74,.05f,-13));Marker("매장 정문",new Vector3(0,.05f,-2));Marker("매장 후문",new Vector3(9.2f,.05f,20));
            DistrictArt.Parent=root;
        }
        static void Blocks()
        {
            var root=DistrictArt.Parent;DistrictArt.Parent=DistrictArt.Group("03 낮은 상가와 주거 블록");
            int i=0;foreach(float z in new[]{-38,62})foreach(float x in new[]{-72,-54,-22,-4,14,58,76}){
                var g=DistrictArt.Prop("SM_Bld_Shop_0"+(i%6+1),new Vector3(x,0,z),6.5f,i%2==0?0:180,true);g.name="상가 "+(++i);
                
            }
            foreach(float x in new[]{-67,67})foreach(float z in new[]{5,23}){DistrictArt.Prop("SM_Bld_OfficeOld_Small_01",new Vector3(x,0,z),16,x<0?90:270,true);}
            // 매장 뒤쪽 서비스 골목과 창고.
            foreach(float x in new[]{-26,26})DistrictArt.Prop("SM_Bld_Shop_02",new Vector3(x,0,24),6,x<0?90:270,true);
            for(int n=0;n<9;n++){
                float x=-100+n*25;DistrictArt.Prop("SM_Gen_Bld_Background_0"+(n%5+1),new Vector3(x,-1,100),25+n%3*5,0);
            }
            DistrictArt.Parent=root;
        }
        static void Details()
        {
            var root=DistrictArt.Parent;DistrictArt.Parent=DistrictArt.Group("04 가로수 / 테라스 / 주차 / 생활 소품");
            foreach(float x in new[]{-82,-60,-22,20,60,82})foreach(float z in new[]{-25,49}){
                DistrictArt.Prop("SM_Env_Tree_01",new Vector3(x,0,z),5.5f,x*2);
                DistrictArt.Prop("SM_Prop_ParkBench_01",new Vector3(x+3,0,z),.85f,z>0?180:0,true);
            }
            foreach(float x in new[]{-30,30})foreach(float z in new[]{-7,28}){
                DistrictArt.Prop("SM_Prop_LightPole_Base_01",new Vector3(x,0,z),4.8f);
                DistrictArt.Box("街灯",new Vector3(x,4.85f,z),new Vector3(.7f,.14f,.7f),DistrictArt.Mat("Street lamp glow",new Color(1,.85f,.58f)),false);
            }
            foreach(float z in new[]{2,6}){
                DistrictArt.Prop("SM_Prop_PicnicTable_01",new Vector3(-17,0,z),.85f,90,true);
                DistrictArt.Prop("SM_Prop_Umbrella_01",new Vector3(-17,0,z),2.9f);
            }
            DistrictArt.Box("주차장",new Vector3(25,-.02f,10),new Vector3(18,.04f,16),asphalt,false);
            for(int n=0;n<5;n++)DistrictArt.Box("주차선",new Vector3(18+n*3.3f,.008f,8),new Vector3(.09f,.012f,5),paint,false);
            DistrictArt.Prop("SM_Veh_Car_Small_01",new Vector3(20,0,8),1.5f,0,true);DistrictArt.Prop("SM_Veh_Car_Van_01",new Vector3(29,0,8),2.3f,180,true);
            DistrictArt.Prop("SM_Prop_Trashbin_01",new Vector3(14,0,19),1.1f,90,true);
            foreach(var p in new[]{new Vector3(-11,0,-2.5f),new Vector3(11,0,-2.5f)})DistrictArt.Prop("SM_Prop_Planter_01",p,.9f,0,true);
            DistrictArt.Box("외부 메뉴판",new Vector3(-3, .9f,-3),new Vector3(1,1.6f,.12f),trim,true,-10);DistrictArt.Sign("FRESH PIZZA\nOPEN",new Vector3(-3,.95f,-3.09f),.035f,-10);
            DistrictArt.Box("지역 표지판",new Vector3(-29,2.3f,-9),new Vector3(4,.8f,.15f),trim,false);DistrictArt.Sign("NEW HARBOR",new Vector3(-29,2.3f,-9.1f),.19f);
            DistrictArt.Box("표지판 기둥",new Vector3(-29,1,-9),new Vector3(.1f,2,.1f),trim);
            // 작은 광장: 원경까지 모두 건물로 채우지 않고 휴식 공간을 둔다.
            DistrictArt.Box("광장 정원",new Vector3(0,.1f,-58),new Vector3(54,.3f,24),grass);
            DistrictArt.Box("광장 산책로",new Vector3(0,.28f,-58),new Vector3(5,.04f,24),pavement,false);
            foreach(float x in new[]{-20,-10,10,20}){DistrictArt.Prop("SM_Env_Tree_03",new Vector3(x,.26f,-59),6.5f);DistrictArt.Prop("SM_Prop_ParkBench_01",new Vector3(x,.26f,-51),.85f,180,true);}
            DistrictArt.Parent=root;
        }
        static void Marker(string n,Vector3 p){var g=new GameObject(n);g.transform.SetParent(DistrictArt.Parent,false);g.transform.position=p;}
        static void Survey()
        {
            var g=new GameObject("지역 둘러보기 • 본편 플레이어 연결 전용 시작점");g.transform.position=Spawn;var cc=g.AddComponent<CharacterController>();cc.height=1.8f;cc.radius=.28f;cc.stepOffset=.3f;
            var c=new GameObject("Survey camera").AddComponent<Camera>();c.transform.SetParent(g.transform,false);c.transform.localPosition=new Vector3(0,.6f,0);c.fieldOfView=68;c.nearClipPlane=.06f;c.farClipPlane=350;c.tag="MainCamera";c.gameObject.AddComponent<AudioListener>();g.AddComponent<NewCitySurvey>().view=c;
        }
        static void BuildNavigation()
        {
            var sources=new List<NavMeshBuildSource>();foreach(var c in UnityEngine.Object.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None))if(c.gameObject.scene==DistrictArt.Scene){sources.Add(new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Box,transform=c.transform.localToWorldMatrix*Matrix4x4.Translate(c.center),size=c.size,area=0});}
            var data=NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByIndex(0),sources,new Bounds(new Vector3(0,15,0),new Vector3(195,40,165)),Vector3.zero,Quaternion.identity);
            var path=DistrictArt.Folder+"/HarborNavigation.asset";var old=AssetDatabase.LoadAssetAtPath<NavMeshData>(path);if(old){EditorUtility.CopySerialized(data,old);UnityEngine.Object.DestroyImmediate(data);data=old;}else AssetDatabase.CreateAsset(data,path);
            var surface=new GameObject("새 지역 보행 경로").AddComponent<NavMeshSurface>();surface.navMeshData=data;surface.AddData();
        }
        static void Validate()
        {
            var log=new StringBuilder("New Harbor environment validation\n");
            var targets=new[]{new Vector3(0,0,4),new Vector3(7,0,13),new Vector3(9.2f,0,20),new Vector3(15,0,7),new Vector3(-74,0,-13)};
            foreach(var end in targets){var p=new NavMeshPath();bool ok=NavMesh.SamplePosition(new Vector3(0,0,-5),out var a,1,NavMesh.AllAreas)&&NavMesh.SamplePosition(end,out var b,1,NavMesh.AllAreas)&&NavMesh.CalculatePath(a.position,b.position,NavMesh.AllAreas,p)&&p.status==NavMeshPathStatus.PathComplete;log.AppendLine((ok?"PASS ":"FAIL ")+"walk route "+end);}
            var rs=UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r=>r.gameObject.scene==DistrictArt.Scene).ToArray();log.AppendLine("Renderers: "+rs.Length);log.AppendLine("Missing materials: "+rs.Sum(r=>r.sharedMaterials.Count(m=>!m||!m.shader)));log.AppendLine("Saved "+ScenePath);File.WriteAllText("Logs/NewHarbor-build.txt",log.ToString());
        }
        static void Capture()
        {
            var c=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).First(x=>x.gameObject.scene.path==ScenePath);var pos=c.transform.position;var rot=c.transform.rotation;
            var shots=new[]{(new Vector3(29,14,-33),new Vector3(0,2,6),"exterior"),(new Vector3(0,1.7f,1),new Vector3(-1,1.7f,13),"interior"),(new Vector3(4,2,12),new Vector3(-5,1.5f,15),"kitchen"),(new Vector3(65,60,-76),new Vector3(0,0,5),"district")};
            foreach(var s in shots){c.transform.position=s.Item1;c.transform.LookAt(s.Item2);var rt=new RenderTexture(1600,900,24);var previous=c.targetTexture;var active=RenderTexture.active;c.targetTexture=rt;c.Render();RenderTexture.active=rt;var t=new Texture2D(1600,900,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,1600,900),0,0);t.Apply();File.WriteAllBytes("Logs/NewHarbor-"+s.Item3+".png",t.EncodeToPNG());c.targetTexture=previous;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(t);UnityEngine.Object.DestroyImmediate(rt);}
            c.transform.SetPositionAndRotation(pos,rot);
        }
    }
}
#endif


