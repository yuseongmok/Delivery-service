#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.AI.Navigation;
using UnityEngine.AI;
using Object = UnityEngine.Object;

namespace DeliveryService.Mok.Editor
{
    [InitializeOnLoad]
    public static partial class PrisonMissionBuilder
    {
        static PrisonMissionBuilder() { EditorApplication.update += Request; }
        static void Request()
        {
            const string path = "Temp/Prison-mission-request";
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || !File.Exists(path)) return;
            string action;
            try { action = File.ReadAllText(path).Trim(); File.Delete(path); }
            catch(IOException) { return; }
            try { if(action == "traffic") PrisonCommonSetup.Traffic(); else if(action == "populationtest") PrisonPopulationCheck.Run(); else if(action == "populate") PrisonCommonSetup.Populate(); else if(action == "comforttest") PrisonMissionCheck.RunComfort(); else if(action == "comfort") PrisonCommonSetup.Comfort(); else if(action == "commoninspect") PrisonCommonSetup.Inspect(); else if(action == "inspect") Inspect(); else if(action == "build") Build(); else if(action == "test") PrisonMissionCheck.Run(); else if(action == "polish") PrisonMissionPolish.Apply(); else if(action == "citytest") PrisonMissionCheck.RunCity(); else if(action == "open") EditorSceneManager.OpenScene("Assets/Scenes/PrisonChapter.unity"); }
            catch(Exception e) { File.WriteAllText("Logs/Prison-builder-error.txt", e.ToString()); Debug.LogException(e); }
        }
        static Transform[] All(Scene s) => s.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
        static void Inspect()
        {
            Directory.CreateDirectory("Logs");
            foreach(var s in Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt))
                if(s.isDirty) EditorSceneManager.SaveScene(s, "../.codex-work/prison-mission/" + s.name + ".unsaved.unity", true);
            var prison=EditorSceneManager.OpenScene("Assets/Scenes/PrisonChapter.unity",OpenSceneMode.Additive);
            File.WriteAllLines("Logs/Prison-layout.txt",All(prison).Where(t=>t.parent==null || t.name.Contains("Cell") || t.name.Contains("Spawn") || t.name.Contains("Station") || t.name.Contains("Bed") || t.name.Contains("의무") || t.name.Contains("간수") || t.name.Contains("조리")).Select(t=>t.name+" @ "+t.position+" scale="+t.lossyScale));
            var city=SceneManager.GetSceneByName("JJinCity");
            if(!city.IsValid()) city=EditorSceneManager.OpenScene("Assets/Scenes/JJinCity.unity",OpenSceneMode.Additive);
            var player=All(city).First(t=>t.name=="Player" && t.GetComponent<PlayerMove>());
            File.WriteAllLines("Logs/Prison-source.txt",player.GetComponentsInChildren<Component>(true).Where(c=>c is MonoBehaviour).Select(c=>c.GetType().Name+" @ "+c.name).Concat(All(city).Where(t=>t.name=="Cart"||t.GetComponent<FoodCartController>()||t.GetComponent<CitizenCombat>()).Select(t=>t.name+" @ "+t.position+" parent="+t.parent?.name)));
            EditorSceneManager.CloseScene(prison,true);
        }
    }
}




namespace DeliveryService.Mok.Editor
{
    public static partial class PrisonMissionBuilder
    {
        const string Folder = "Assets/DeliveryPrisonChapter/Mission01";
        static Transform missionRoot;
        static Material metal, rice, stew;
        [MenuItem("Delivery Service/감옥/첫 임무 배치")]
        public static void Build()
        {
            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
            var original = SceneManager.GetActiveScene();
            foreach (var scene in Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt))
                if (scene.isDirty) EditorSceneManager.SaveScene(scene, "../.codex-work/prison-mission/"+scene.name+".unsaved.unity",true);
            var city = SceneManager.GetSceneByName("JJinCity");
            bool cityOpened = !city.IsValid();
            if (cityOpened) city = EditorSceneManager.OpenScene("Assets/Scenes/JJinCity.unity",OpenSceneMode.Additive);
            var prison = SceneManager.GetSceneByName("PrisonChapter");
            if (!prison.IsValid()) prison = EditorSceneManager.OpenScene("Assets/Scenes/PrisonChapter.unity",OpenSceneMode.Additive);
            if (All(prison).Any(t=>t.GetComponent<PrisonMission>())) throw new Exception("이미 첫 임무가 배치되어 있습니다. 씬 편집 내용을 보존하기 위해 덮어쓰지 않습니다.");
            EditorSceneManager.SaveScene(prison,"../.codex-work/prison-mission/PrisonChapter.editor-before.unity",true);
            SceneManager.SetActiveScene(prison);
            foreach (var behaviour in All(prison).SelectMany(t=>t.GetComponents<MonoBehaviour>()).Where(b=>b&&b.GetType().Name=="PrisonPreviewWalker")) behaviour.gameObject.SetActive(false);
            foreach(var camera in All(prison).SelectMany(t=>t.GetComponents<Camera>())) camera.gameObject.SetActive(false);

            // 기존 건축물은 위치와 프리팹 연결을 유지한 채 내비메시 수집 루트에 묶는다.
            var geometry = new GameObject("Prison navigation geometry");
            foreach (var root in prison.GetRootGameObjects()) if(root!=geometry) root.transform.SetParent(geometry.transform,true);
            var surface=geometry.AddComponent<NavMeshSurface>();surface.collectObjects=CollectObjects.Children;
            surface.useGeometry=NavMeshCollectGeometry.PhysicsColliders;surface.overrideVoxelSize=true;surface.voxelSize=.12f;
            surface.BuildNavMesh();
            if(!surface.navMeshData)throw new Exception("감옥 내비메시 생성 실패");
            AssetDatabase.CreateAsset(surface.navMeshData,Folder+"/PrisonNavigation.asset");
            missionRoot=new GameObject("20_감옥 첫 임무 • 민기를 찾아서").transform;
            var mission=missionRoot.gameObject.AddComponent<PrisonMission>();
            var text=AssetDatabase.LoadAssetAtPath<PrisonMissionText>(Folder+"/첫 임무 대사.asset");
            if(!text){text=ScriptableObject.CreateInstance<PrisonMissionText>();AssetDatabase.CreateAsset(text,Folder+"/첫 임무 대사.asset");}mission.dialogue=text;
            var source=All(city).First(t=>t.name=="Player"&&t.GetComponent<PlayerMove>());
            var player=Object.Instantiate(source.gameObject);SceneManager.MoveGameObjectToScene(player,prison);player.name="Player";
            player.transform.SetPositionAndRotation(new Vector3(-9, .15f,-25),Quaternion.Euler(0,90,0));player.SetActive(true);
            mission.player=player.GetComponent<PlayerCombat>();mission.player.hospital=null;
            var interact=new SerializedObject(player.GetComponent<PlayerInteraction>());interact.FindProperty("interactLayer").intValue=-1;interact.ApplyModifiedPropertiesWithoutUndo();
            foreach(var canvas in player.GetComponentsInChildren<Canvas>(true))canvas.gameObject.SetActive(false);
            foreach(var c in player.GetComponentsInChildren<Camera>(true))c.gameObject.SetActive(true);
            // 도시 호출과 도시 네비의 외부 참조는 감옥으로 가져오지 않는다.
            var recall=player.GetComponent<BikeRecall>();if(recall)recall.enabled=false;
            foreach(var component in player.GetComponentsInChildren<MonoBehaviour>(true))
                if(component && (component.GetType().Name=="PlayerInteract" || component.GetType().Name=="InteractHoverManager"))component.enabled=false;
            var hud=missionRoot.gameObject.AddComponent<PrisonMissionHUD>();hud.mission=mission;
            mission.cooking=Point("조리대 • 밥 3인분",new Vector3(30,1,-31.4f),new Vector3(1.1f,.3f,.6f),"밥 3인분 만들기");
            mission.cell=Point("내 방 • 임무 마무리",new Vector3(-8.5f,1,-25),new Vector3(.3f,1.6f,.5f),"방으로 돌아와 쉬기");
            mission.player.hospital=Anchor("의료 복귀",new Vector3(-9,.15f,-25));
            mission.stealthCheckpoint=Anchor("의무실 재시도",new Vector3(-16,.2f,-10));
            metal=Mat("배식 스테인리스",new Color(.38f,.43f,.45f));rice=Mat("밥",new Color(.94f,.91f,.79f));stew=Mat("반찬",new Color(.42f,.19f,.09f));
            var cartSource=All(city).First(t=>t.name=="Cart"&&t.parent==null);
            var cart=Object.Instantiate(cartSource.gameObject);SceneManager.MoveGameObjectToScene(cart,prison);cart.name="배식카트";cart.transform.SetParent(missionRoot,true);
            cart.transform.position=new Vector3(30,.15f,-27);cart.SetActive(true);
            foreach(var storage in cart.GetComponentsInChildren<FoodCartStorage>(true))Object.DestroyImmediate(storage);
            mission.cart=cart.AddComponent<PrisonMealCart>();mission.cart.controller=cart.GetComponentInChildren<FoodCartController>(true);
            mission.cart.controller.cartStorage=null;mission.cart.controller.minKnockdownSpeed=2.5f;mission.cart.controller.pushMoveSpeed=5;
            mission.cart.preparedMeals=new GameObject("완성된 3인분");mission.cart.preparedMeals.transform.SetParent(missionRoot,false);
            mission.cart.meals=new GameObject[3];
            for(int i=0;i<3;i++)
            {
                Tray(new Vector3(29.7f+i*.32f,1.08f,-31.4f),mission.cart.preparedMeals.transform);
                mission.cart.meals[i]=Tray(cart.transform.position+new Vector3(0,1.12f+i*.12f,0),cart.transform).gameObject;
            }
            var recipients=new System.Collections.Generic.List<PrisonMissionPoint>();
            for(int i=0;i<3;i++)
            {
                var actor=Inmate("배식 대상 "+(i+1),new Vector3(7.2f,0,-25+i*7));actor.transform.rotation=Quaternion.Euler(0,-90,0);
                var point=actor.AddComponent<PrisonMissionPoint>();point.prompt="식사 전달하고 민기 소식 묻기";recipients.Add(point);
            }
            mission.recipients=recipients.ToArray();
            foreach(var position in new[]{new Vector3(-10,0,-11),new Vector3(-10,0,-4),new Vector3(10,0,3),new Vector3(10,0,10)})Inmate("감방 재소자",position);
            for(int i=0;i<5;i++)
            {
                var at=new Vector3(-2+i,0,-18+i*5);var actor=Inmate("생활동 재소자 "+(i+1),at);
                actor.GetComponent<PrisonResident>().route=new[]{Anchor("생활 순찰 A",at),Anchor("생활 순찰 B",at+Vector3.forward*5)};
            }
            // 민기는 도시에서 사용하던 Synty 모델을 유지한다.
            var tutorial=All(city).Select(t=>t.GetComponent<StoryTutorial>()).First(t=>t);
            var mingi=Object.Instantiate(tutorial.actor.gameObject);SceneManager.MoveGameObjectToScene(mingi,prison);mingi.name="민기 • 의무실";mingi.transform.SetParent(missionRoot,true);
            foreach(var behaviour in mingi.GetComponentsInChildren<MonoBehaviour>(true))Object.DestroyImmediate(behaviour);
            foreach(var agent in mingi.GetComponentsInChildren<NavMeshAgent>())Object.DestroyImmediate(agent);
            foreach(var cc in mingi.GetComponentsInChildren<CharacterController>())Object.DestroyImmediate(cc);
            foreach(var rb in mingi.GetComponentsInChildren<Rigidbody>())Object.DestroyImmediate(rb);
            foreach(var col in mingi.GetComponentsInChildren<Collider>())Object.DestroyImmediate(col);
            var bed=Box("민기 병상",new Vector3(-30,.5f,4),new Vector3(1.15f,.5f,2.3f),metal);
            Box("병상 매트리스",new Vector3(-30,.82f,4),new Vector3(1.1f,.18f,2.15f),rice);
            mingi.transform.SetPositionAndRotation(new Vector3(-30,.94f,3.2f),Quaternion.Euler(90,0,0));mingi.SetActive(true);
            var animator=mingi.GetComponentInChildren<Animator>();if(animator){animator.Rebind();animator.Update(0);animator.enabled=false;}
            mission.infirmary=bed.gameObject.AddComponent<PrisonMissionPoint>();mission.infirmary.prompt="민기와 조용히 대화하기";
            var guards=new System.Collections.Generic.List<PrisonGuard>();
            guards.Add(Guard("의무실 순찰 간수",new Vector3(-25,0,-2),new Vector3(-25,0,9),true));
            guards.Add(Guard("의무실 입구 간수",new Vector3(-20,0,-7),new Vector3(-20,0,1),true));
            guards.Add(Guard("수감동 간수",new Vector3(1,0,-20),new Vector3(1,0,8),false));
            guards.Add(Guard("주방 간수",new Vector3(21,0,-20),new Vector3(39,0,-20),false));
            mission.guards=guards.ToArray();
            var distraction=Box("배관 • 간수 유인",new Vector3(-17,1,-8),new Vector3(.22f,1.5f,.3f),metal).gameObject.AddComponent<PrisonDistraction>();
            distraction.guards=guards.Take(2).ToArray();distraction.soundPoint=Anchor("배관 소리 지점",new Vector3(-19,0,-10));
            EditorSceneManager.MarkSceneDirty(prison);EditorSceneManager.SaveScene(prison);AssetDatabase.SaveAssets();
            File.WriteAllText("Logs/Prison-build.txt","Saved: "+prison.path+"\nPrisoners: 12; guards: 4; reserved cells empty; player abilities copied.\n");
            if(cityOpened)EditorSceneManager.CloseScene(city,true);
            if(original.IsValid()&&original.isLoaded)SceneManager.SetActiveScene(original);
        }
        static Transform Anchor(string name,Vector3 at){var t=new GameObject(name).transform;t.SetParent(missionRoot);t.position=at;return t;}
        static PrisonMissionPoint Point(string name,Vector3 at,Vector3 size,string hint){var t=Anchor(name,at);var col=t.gameObject.AddComponent<BoxCollider>();col.size=size;var p=t.gameObject.AddComponent<PrisonMissionPoint>();p.prompt=hint;return p;}
        static Material Mat(string name,Color color){var path=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.color=color;AssetDatabase.CreateAsset(m,path);}return m;}
        static Transform Box(string name,Vector3 at,Vector3 size,Material material,Transform parent=null)
        {var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent?parent:missionRoot,true);g.transform.position=at;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=material;return g.transform;}
        static Transform Tray(Vector3 at,Transform parent)
        {
            var root=Anchor("식판",at);root.SetParent(parent,true);
            var baseplate=Box("식판 바닥",at,new Vector3(.48f,.035f,.32f),metal,root);Object.DestroyImmediate(baseplate.GetComponent<Collider>());
            var food=Box("밥",at+new Vector3(-.10f,.05f,0),new Vector3(.19f,.09f,.23f),rice,root);Object.DestroyImmediate(food.GetComponent<Collider>());
            food=Box("반찬",at+new Vector3(.13f,.035f,0),new Vector3(.15f,.06f,.22f),stew,root);Object.DestroyImmediate(food.GetComponent<Collider>());return root;
        }
        static GameObject Inmate(string name,Vector3 at)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Scripts/Min/Prefab/NPC/Male_Adult_NPC_1.prefab");
            var g=Object.Instantiate(prefab);g.name=name;g.transform.SetParent(missionRoot,false);g.transform.position=at;g.SetActive(true);
            foreach(var behaviour in g.GetComponents<MonoBehaviour>())if(!(behaviour is NPCControl)&&!(behaviour is CitizenCombat))Object.DestroyImmediate(behaviour);
            var combat=g.GetComponent<CitizenCombat>()??g.AddComponent<CitizenCombat>();combat.missionEnemy=true;combat.usesFirearm=false;
            SyntyStoryUpgrade.Skin(g.transform,"Assets/Synty/PolygonGeneric/Prefabs/Characters/SM_Gen_Chr_Prisoner_Male_01.prefab","PrisonInmate",false,false,true);
            g.GetComponentInChildren<Animator>().runtimeAnimatorController=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Scripts/Mok/Art/StoryCharacters/StoryWalk.controller");
            g.AddComponent<PrisonResident>().route=Array.Empty<Transform>();PlaceOnNav(g);return g;
        }
        static void PlaceOnNav(GameObject g){if(NavMesh.SamplePosition(g.transform.position,out var hit,2,NavMesh.AllAreas))g.transform.position=hit.position;}
        static PrisonGuard Guard(string name,Vector3 a,Vector3 b,bool watch)
        {
            var g=Inmate(name,a);Object.DestroyImmediate(g.GetComponent<PrisonResident>());
            SyntyStoryUpgrade.Skin(g.transform,"Assets/Synty/PolygonCity/Prefabs/Characters/Character_Male_Police.prefab","PrisonGuard",false,false,true);
            var guard=g.AddComponent<PrisonGuard>();guard.infirmaryWatch=watch;guard.route=new[]{Anchor(name+" A",a),Anchor(name+" B",b)};
            var gun=BankPrologueBuilder.CreateGun(g.transform);guard.gun=gun.transform;
            return guard;
        }
    }
}

#endif







