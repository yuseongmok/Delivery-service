#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AI;
using UnityEngine.Rendering;
using Unity.AI.Navigation;

namespace DeliveryService.Mok.NewCity.Editor
{
    // 기존 씬 전체를 재생성하지 않고 거리 블록만 수정한다.
    [InitializeOnLoad]
    internal static class HarborBlockPolish
    {
        const string Request = "Temp/HarborBlockPolish";
        const string Target = "Assets/Scenes/NewHarborCity.unity";
        static HarborBlockPolish() { EditorApplication.update += Poll; }

        static void Poll()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(Request)) return;
            string command;
            try { command = File.ReadAllText(Request).Trim(); File.Delete(Request); }
            catch (IOException) { return; }
            try
            {
                if (command == "probe") Probe();
                else if (command == "apply") Apply();
                else if (command == "validate") { DistrictArt.Scene=SceneManager.GetActiveScene(); RebuildPaths(); AssetDatabase.SaveAssets(); }
                else Capture();
            }
            catch (Exception e) { File.WriteAllText("Logs/HarborBlock-error.txt", e.ToString()); Debug.LogException(e); }
        }

        static void Probe()
        {
            var report = new StringBuilder();
            var preview = EditorSceneManager.OpenPreviewScene("Assets/Scenes/JJinCity.unity");
            try
            {
                foreach (var r in preview.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MeshRenderer>(true)))
                {
                    if (!EnvironmentName(r.name)) continue;
                    report.AppendLine($"ENV {r.name} active={r.gameObject.activeInHierarchy} position={r.transform.position} scale={r.transform.lossyScale} bounds={r.bounds} mats={string.Join(",", r.sharedMaterials.Select(m => m ? AssetDatabase.GetAssetPath(m) : "null"))}");
                }
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Synty/PolygonCity/Prefabs/Buildings" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.Contains("Shop_0") && !path.Contains("OfficeOld_Small") && !path.Contains("Apartment_Small")) continue;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                report.AppendLine($"PREFAB {prefab.name} bounds={DistrictArt.Bounds(prefab)}");
            }
            File.WriteAllText("Logs/HarborBlock-probe.txt", report.ToString());
        }

        static bool EnvironmentName(string name) => name.StartsWith("SM_Env_Skyline_") || name.StartsWith("SM_Env_Cloud_") || name.StartsWith("SM_Gen_Env_Skydome_") || name == "Ocean_Plane";

        static void Apply()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != Target) throw new Exception("NewHarborCity 씬을 연 상태에서 실행해야 합니다.");
            // 저장하지 않은 사용자 편집까지 사본으로 보존한다.
            Directory.CreateDirectory("Logs");
            EditorSceneManager.SaveScene(scene, "Logs/HarborBlock-before-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".unity", true);
            DistrictArt.Scene = scene;
            BuildBlock();
            CopyEnvironment();
            RebuildPaths();
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Capture();
        }

        static Transform block;
        static Material iron, paving, dark, ivory, terracotta;

        static void BuildBlock()
        {
            var roots = DistrictArt.Scene.GetRootGameObjects();
            var city = roots.First(g => g.name.StartsWith("NEW HARBOR")).transform;
            var previous = city.Find("05 매장 앞 상점 블록");
            if (previous) UnityEngine.Object.DestroyImmediate(previous.gameObject);

            // 최초 배치의 맞은편 상점과 해당 충돌체만 걷어내고 매장 편집은 유지한다.
            var oldBlocks = city.Find("03 낮은 상가와 주거 블록");
            foreach (Transform child in oldBlocks.Cast<Transform>().ToArray())
            {
                if ((child.name.StartsWith("상가") || child.name == "외곽 충돌") && child.position.z < -23 && Mathf.Abs(child.position.x) < 34)
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                else if (child.name.StartsWith("SM_Gen_Bld_Background_"))
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
            DistrictArt.Parent = city;
            block = DistrictArt.Group("05 매장 앞 상점 블록");
            DistrictArt.Parent = block;
            iron = DistrictArt.Mat("Block dark bronze", new Color(.13f,.20f,.21f));
            paving = DistrictArt.Pattern("Block sidewalk", new Color(.52f,.55f,.55f),new Color(.39f,.43f,.44f),false);
            dark = DistrictArt.Mat("Block alley asphalt", new Color(.23f,.27f,.28f));
            ivory = DistrictArt.Mat("Block warm trim",new Color(.83f,.77f,.63f));
            terracotta = DistrictArt.Mat("Block awning rust", new Color(.58f,.22f,.12f));

            Box("맞은편 보행자 보도",new Vector3(0,.005f,-25),new Vector3(68,.10f,7),paving);
            Box("뒤편 연결 골목",new Vector3(0,.01f,-40.5f),new Vector3(68,.11f,5),dark);
            Shop("HARBOUR COFFEE", "SM_Bld_Shop_01", -29,8,2, new Color(.12f,.28f,.25f));
            Shop("LAUNDRY", "SM_Bld_Shop_04", -20,8,1,new Color(.24f,.39f,.48f));
            Shop("CORNER BOOKS", "SM_Bld_Shop_06", -11,8,2,new Color(.46f,.25f,.19f));
            Shop("FLOWER MARKET", "SM_Bld_Shop_01", 2,8,1,new Color(.35f,.43f,.25f));
            Shop("BAKERY", "SM_Bld_Shop_05", 11,8,2,new Color(.58f,.31f,.14f));
            Shop("NEIGHBOUR MART", "SM_Bld_Shop_03", 25,12,1,new Color(.22f,.34f,.40f));

            // 건물 사이 틈은 명확한 통행로로 연결한다. 장식은 벽 가까이 둔다.
            foreach (float x in new[] {-4.5f,17.5f})
            {
                Box("가게 사이 골목 바닥",new Vector3(x,.015f,-33),new Vector3(3.1f,.10f,15),dark);
                Box("골목 배수 홈",new Vector3(x+.9f,.075f,-33),new Vector3(.18f,.015f,13),iron,false);
                for(int n=0;n<20;n++)Box("배수 격자",new Vector3(x+.9f,.087f,-39+n*.65f),new Vector3(.25f,.012f,.05f),ivory,false);
                DistrictArt.Prop("SM_Prop_Trashbin_02",new Vector3(x+1.05f,.07f,-37.8f),.9f,90,true);
                DistrictArt.Sign("SERVICE LANE",new Vector3(x,2.6f,-28),.045f,180);
            }

            // 정면은 머무는 공간, 뒤편은 납품과 관리 공간으로 구분한다.
            foreach (float x in new[] {-27f,-18f,7f,27f})
            {
                DistrictArt.Prop("SM_Prop_Planter_01",new Vector3(x,.06f,-26.9f),.65f,0,true);
                DistrictArt.Prop("SM_Prop_PotPlant_02",new Vector3(x,.7f,-26.9f),.75f);
            }
            foreach (float x in new[] {-31f,0f,31f})
            {
                DistrictArt.Prop("SM_Prop_LightPole_Base_01",new Vector3(x,.06f,-22.3f),4.5f);
                DistrictArt.Prop("SM_Prop_LightPole_Lights_01",new Vector3(x,4.1f,-22.3f),.8f);
                DistrictArt.Prop("SM_Prop_Trashbin_01",new Vector3(x+1,.06f,-22.8f),.8f,0,true);
            }
            foreach (float x in new[] {-24f,5f})
            {
                DistrictArt.Prop("SM_Prop_ParkBench_01",new Vector3(x,.06f,-23),.85f,0,true);
            }
            foreach(float x in new[] {-31.5f,-25f,-18f,-11f,2f,9f,24f,31f})
            {
                Box("보도 볼라드",new Vector3(x,.42f,-21.5f),new Vector3(.16f,.75f,.16f),iron);
                Box("볼라드 반사띠",new Vector3(x,.65f,-21.5f),new Vector3(.18f,.1f,.18f),ivory,false);
            }
            // 매장 옆 테라스를 둘러싸는 낮은 건물은 보행로와 주차장을 침범하지 않는다.
            Fit("SM_Bld_Shop_02",new Vector3(-29,0,10),new Vector3(9,5,13),90,true);
            Fit("SM_Bld_OfficeOld_Small_01",new Vector3(-28,0,27),new Vector3(10,13,10),90,true);
            Fit("SM_Bld_OfficeOld_Small_02",new Vector3(25,0,27),new Vector3(12,13,10),270,true);
            // 기존의 창고 모델과 충돌체가 새 건물과 겹치는 부분을 제거한다.
            foreach(Transform t in oldBlocks.Cast<Transform>().ToArray())
                if((t.name=="SM_Bld_Shop_02" || t.name=="외곽 충돌") && Mathf.Abs(t.position.x)<34 && t.position.z>19 && t.position.z<29)
                    UnityEngine.Object.DestroyImmediate(t.gameObject);
            Physics.SyncTransforms();
        }

        static void Shop(string title,string prefab,float x,float width,int floors,Color color)
        {
            var group = DistrictArt.Group(title);
            DistrictArt.Parent = group;
            Fit(prefab,new Vector3(x,.06f,-33),new Vector3(width,4.2f,10),0,true);
            float y = 4.26f;
            for(int i=0;i<floors;i++)
            {
                Fit("SM_Bld_OfficeOld_Small_Floor_01",new Vector3(x,y,-33.5f),new Vector3(width,3.1f,9),0,true);
                Box("층 사이 마감 띠",new Vector3(x,y+.03f,-33.5f),new Vector3(width+.04f,.20f,9.04f),ivory);
                y += 3.1f;
            }
            Box("상가 처마",new Vector3(x,y+.1f,-33.4f),new Vector3(width+.25f,.2f,9.3f),ivory);
            Box("지붕 앞 파라펫",new Vector3(x,y+.36f,-28.9f),new Vector3(width,.5f,.18f),iron);
            var fascia=DistrictArt.Mat("Block fascia "+title,color);
            Box("상점 간판",new Vector3(x,3.7f,-27.92f),new Vector3(width-.4f,.85f,.16f),fascia);
            DistrictArt.Sign(title,new Vector3(x,3.7f,-27.81f),.065f,180);
            Box("출입문 위 차양",new Vector3(x,3.08f,-27.6f),new Vector3(width-.5f,.12f,1.4f),fascia,false);
            Box("차양 가장자리",new Vector3(x,2.97f,-26.93f),new Vector3(width-.5f,.22f,.08f),fascia,false);
            for(float dx=-width*.4f;dx<width*.45f;dx+=1.2f)
                Box("차양 스트라이프",new Vector3(x+dx,3.15f,-27.6f),new Vector3(.18f,.015f,1.4f),ivory,false);
            DistrictArt.Prop("SM_Prop_Roof_Aircon_01",new Vector3(x,y+.21f,-34),.7f,0);
            // 출입구 옆 안내판은 보도를 막지 않도록 가게 벽에 붙인다.
            Box("벽 메뉴판",new Vector3(x+width*.35f,1.7f,-27.9f),new Vector3(.7f,1,.09f),iron,false);
            DistrictArt.Sign("OPEN\n09 - 21",new Vector3(x+width*.35f,1.7f,-27.83f),.026f,180);
            foreach(float dx in new[]{-width*.32f,width*.32f})
            {
                Box("상가 벽등",new Vector3(x+dx,3.32f,-27.7f),new Vector3(.18f,.28f,.25f),ivory,false);
            }
            // 골목 벽에는 배관을 두되 통행 폭은 남긴다.
            Box("빗물 배관",new Vector3(x+width*.5f-.12f,y*.5f,-37.8f),new Vector3(.11f,y,.11f),iron,false);
            DistrictArt.Parent = block;
        }

        static GameObject Fit(string prefab,Vector3 ground,Vector3 size,float yaw,bool solid)
        {
            var g=DistrictArt.Prop(prefab,ground,0,yaw,false);
            var b=DistrictArt.Bounds(g);
            var scale=new Vector3(size.x/b.size.x,size.y/b.size.y,size.z/b.size.z);
            if(Mathf.Abs(Mathf.Sin(yaw*Mathf.Deg2Rad))>.5f)scale=new Vector3(scale.z,scale.y,scale.x);
            g.transform.localScale=Vector3.Scale(g.transform.localScale,scale);
            b=DistrictArt.Bounds(g);
            g.transform.position+=ground-new Vector3(b.center.x,b.min.y,b.center.z);
            if(solid)
            {
                b=DistrictArt.Bounds(g);
                var col=new GameObject(prefab+" 충돌");
                col.transform.SetParent(DistrictArt.Parent,false);
                col.transform.position=b.center;
                col.AddComponent<BoxCollider>().size=b.size;
                col.isStatic=true;
            }
            return g;
        }

        static void Box(string name,Vector3 position,Vector3 size,Material material,bool solid=true)
            => DistrictArt.Box(name,position,size,material,solid);

        static void CopyEnvironment()
        {
            var root=DistrictArt.Scene.GetRootGameObjects().First(g=>g.name.StartsWith("NEW HARBOR")).transform;
            var old=root.Find("06 기존 도시의 하늘과 원경");
            if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
            DistrictArt.Parent=root;
            var environment=DistrictArt.Group("06 기존 도시의 하늘과 원경");
            var source=EditorSceneManager.OpenPreviewScene("Assets/Scenes/JJinCity.unity");
            int count=0;
            try
            {
                // 렌더링 요소만 복사하여 도시의 미션/시간 시스템이 들어오지 않게 한다.
                foreach(var renderer in source.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true)))
                {
                    if(!EnvironmentName(renderer.name) || !renderer.gameObject.activeInHierarchy)continue;
                    var mesh=renderer.GetComponent<MeshFilter>();
                    if(!mesh || !mesh.sharedMesh)continue;
                    var g=new GameObject(renderer.name);
                    SceneManager.MoveGameObjectToScene(g,DistrictArt.Scene);
                    g.transform.SetParent(environment,false);
                    var p=renderer.transform.position;
                    if(renderer.name.Contains("Skydome"))p.x-=140;
                    else if(renderer.name.Contains("Skyline"))p.x-=58.5f;
                    else if(renderer.name=="Ocean_Plane")p=new Vector3(0,-.6f,0);
                    g.transform.SetPositionAndRotation(p,renderer.transform.rotation);
                    g.transform.localScale=renderer.transform.lossyScale;
                    g.AddComponent<MeshFilter>().sharedMesh=mesh.sharedMesh;
                    var copy=g.AddComponent<MeshRenderer>();
                    copy.sharedMaterials=renderer.sharedMaterials;
                    copy.shadowCastingMode=ShadowCastingMode.Off;
                    copy.receiveShadows=false;
                    count++;
                }
            }
            finally { EditorSceneManager.ClosePreviewScene(source); }
            foreach(var camera in DistrictArt.Scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>(true)))camera.farClipPlane=1800;
            File.WriteAllText("Logs/HarborBlock-environment.txt","JJinCity에서 동일 메시와 재질을 복사한 하늘/구름/원경: "+count);
        }

        static void RebuildPaths()
        {
            var sources=new System.Collections.Generic.List<NavMeshBuildSource>();
            foreach(var c in DistrictArt.Scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<BoxCollider>()))
                sources.Add(new NavMeshBuildSource {shape=NavMeshBuildSourceShape.Box,transform=c.transform.localToWorldMatrix*Matrix4x4.Translate(c.center),size=c.size,area=0});
            var surface=DistrictArt.Scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<NavMeshSurface>()).First();
            surface.RemoveData();
            var data=NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByIndex(0),sources,new Bounds(new Vector3(0,15,0),new Vector3(195,40,165)),Vector3.zero,Quaternion.identity);
            EditorUtility.CopySerialized(data,surface.navMeshData);
            surface.navMeshData.name="HarborNavigation";
            UnityEngine.Object.DestroyImmediate(data);
            EditorUtility.SetDirty(surface.navMeshData);
            surface.AddData();
            var report=new StringBuilder();
            foreach(var destination in new[]{new Vector3(0,0,4),new Vector3(7,0,13),new Vector3(9.2f,0,20),new Vector3(-4.5f,0,-40),new Vector3(17.5f,0,-40),new Vector3(-30,0,-24),new Vector3(30,0,-24)})
            {
                var path=new NavMeshPath();
                bool pass=NavMesh.SamplePosition(new Vector3(0,0,-5),out var start,1,NavMesh.AllAreas)&&NavMesh.SamplePosition(destination,out var end,1,NavMesh.AllAreas)&&NavMesh.CalculatePath(start.position,end.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;
                report.AppendLine((pass?"PASS ":"FAIL ")+destination);
            }
            Physics.SyncTransforms();
            foreach(float x in new[]{-4.5f,17.5f})
            {
                bool clear=true;
                for(float z=-26;z>=-40;z-=.5f)
                    if(Physics.CheckCapsule(new Vector3(x,.42f,z),new Vector3(x,1.62f,z),.30f,~0,QueryTriggerInteraction.Ignore))clear=false;
                report.AppendLine((clear?"PASS ":"FAIL ")+"골목 보행 폭 x="+x);
            }
            File.WriteAllText("Logs/HarborBlock-validation.txt",report.ToString());
        }

        static void Capture()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != Target) throw new Exception("촬영할 새 도시 씬이 열려 있지 않습니다.");
            var camera = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Camera>(true)).First();
            var position = camera.transform.position;
            var rotation = camera.transform.rotation;
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            var shots = new[]
            {
                (new Vector3(0,1.7f,-5), new Vector3(0,3,-34), "front"),
                (new Vector3(28,10,-22), new Vector3(-4,2,-31), "block"),
                (new Vector3(-26,1.7f,-24), new Vector3(22,2,-29), "street"),
                (new Vector3(45,43,-65), new Vector3(0,0,-10), "overview")
            };
            try
            {
                foreach (var shot in shots)
                {
                    camera.transform.position = shot.Item1;
                    camera.transform.LookAt(shot.Item2);
                    var rt = new RenderTexture(1600,900,24);
                    var texture = new Texture2D(1600,900,TextureFormat.RGB24,false);
                    try
                    {
                        camera.targetTexture = rt;
                        camera.Render();
                        RenderTexture.active = rt;
                        texture.ReadPixels(new Rect(0,0,1600,900),0,0);
                        texture.Apply();
                        File.WriteAllBytes("Logs/HarborBlock-" + shot.Item3 + ".png",texture.EncodeToPNG());
                    }
                    finally { camera.targetTexture = previousTarget; RenderTexture.active = previousActive; UnityEngine.Object.DestroyImmediate(texture); UnityEngine.Object.DestroyImmediate(rt); }
                }
            }
            finally { camera.transform.SetPositionAndRotation(position,rotation); }
        }
    }
}
#endif
