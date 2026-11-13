#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DeliveryService.Mok.Editor
{
    // 감옥 씬에만 배치한다. 도시와 공용 캐릭터 프리팹은 변경하지 않는다.
    [InitializeOnLoad]
    public static class PrisonChickenSetup
    {
        const string Folder = "Assets/DeliveryPrisonChapter/ChickenMission";
        static Transform root;
        static Material metal, cream, brown, chicken, golden, oil;
        static PrisonChickenSetup() { EditorApplication.update += Poll; }
        static T[] All<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true)).ToArray();
        static void Poll()
        {
            if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists("Temp/PrisonChicken-request")) return;
            string action = File.ReadAllText("Temp/PrisonChicken-request").Trim();
            File.Delete("Temp/PrisonChicken-request");
            try { if (action == "test") PrisonChickenCheck.Run(); else if(action=="inspect") Inspect(); else if(action=="polish") Polish(); else if(action=="visual") Visual(); else Build(); }
            catch (Exception e) { File.WriteAllText("Logs/Prison-chicken-build.txt", e.ToString()); Debug.LogException(e); }
        }
        static void Visual(){
            var m=Object.FindFirstObjectByType<PrisonChickenMission>();root=m.transform;
            var c=m.cooking;var scene=m.gameObject.scene;EditorSceneManager.SaveScene(scene,"Logs/Prison-before-chicken-visual.unity",true);
            metal=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/조리도구.mat");oil=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/기름.mat");
            var visual=c.GetComponent<ChickenCookingVisuals>();if(visual)return;
            visual=c.gameObject.AddComponent<ChickenCookingVisuals>();visual.cooking=c;visual.raw=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/생닭.mat");visual.coated=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/라벨과 반죽.mat");visual.fried=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/튀긴 치킨.mat");
            Object.DestroyImmediate(c.bowl.gameObject);
            c.bowl=Pan("반죽 그릇",new Vector3(30.65f,.9f,-30.45f),.22f,metal);
            visual.fryer=Pan("치킨 튀김 팬",new Vector3(29.7f,1.03f,-32.4f),.4f,metal);
            var surface=GameObject.CreatePrimitive(PrimitiveType.Cylinder);surface.name="튀김 기름";surface.transform.SetParent(visual.fryer,false);surface.transform.localPosition=Vector3.up*.035f;surface.transform.localScale=new Vector3(.73f,.012f,.73f);surface.GetComponent<Renderer>().sharedMaterial=oil;Object.DestroyImmediate(surface.GetComponent<Collider>());
            c.rawChicken.transform.localScale=new Vector3(.85f,1,1.15f);
            c.knife.localScale=new Vector3(.025f,.12f,.22f);
            // 오래된 밥 배식 시각 소품은 새 조리 장면에 겹치지 않게 숨긴다.
            foreach(var renderer in m.prison.cooking.GetComponentsInChildren<Renderer>(true))renderer.enabled=false;
            EditorUtility.SetDirty(m.dialogue);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            m.camera.transform.SetPositionAndRotation(m.cookView.position,m.cookView.rotation);PrisonChickenCheck.Capture(m.camera,"Logs/Chicken-cooking-final-preview.png");
        }
        static Transform Pan(string name,Vector3 position,float radius,Material material){
            var go=new GameObject(name);go.transform.SetParent(root);go.transform.position=position;
            var basePart=GameObject.CreatePrimitive(PrimitiveType.Cylinder);basePart.transform.SetParent(go.transform,false);basePart.transform.localScale=new Vector3(radius*2,.015f,radius*2);basePart.GetComponent<Renderer>().sharedMaterial=material;Object.DestroyImmediate(basePart.GetComponent<Collider>());
            for(int i=0;i<16;i++){float angle=i*Mathf.PI*2/16;var side=GameObject.CreatePrimitive(PrimitiveType.Cube);side.transform.SetParent(go.transform,false);side.transform.localPosition=new Vector3(Mathf.Cos(angle)*radius,.04f,Mathf.Sin(angle)*radius);side.transform.localRotation=Quaternion.Euler(0,-angle*Mathf.Rad2Deg,0);side.transform.localScale=new Vector3(.025f,.09f,radius*.4f);side.GetComponent<Renderer>().sharedMaterial=material;Object.DestroyImmediate(side.GetComponent<Collider>());}return go.transform;
        }
        static void Polish(){
            var m=Object.FindFirstObjectByType<PrisonChickenMission>();var scene=m.gameObject.scene;root=m.transform;
            EditorSceneManager.SaveScene(scene,"Logs/Prison-before-chicken-polish.unity",true);
            foreach(var t in root.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="재료 선반"||t.name=="작업대"||t.name=="작업대 다리").ToArray())Object.DestroyImmediate(t.gameObject);
            var hat=root.GetComponentsInChildren<Transform>(true).First(t=>t.name=="주방장 모자");var scale=hat.parent.lossyScale;hat.localScale=new Vector3(.34f/scale.x,.3f/scale.y,.32f/scale.z);Object.DestroyImmediate(hat.GetComponent<Collider>());
            var handle=root.GetComponentsInChildren<Transform>(true).First(t=>t.name=="칼 손잡이");scale=handle.parent.lossyScale;handle.localScale=new Vector3(.07f/scale.x,.07f/scale.y,.18f/scale.z);Object.DestroyImmediate(handle.GetComponent<Collider>());
            Vector3[] positions={new Vector3(31,1.15f,-32.6f),new Vector3(27,1.15f,-27.6f),new Vector3(25,1.15f,-20.45f),new Vector3(39,1.15f,-33.1f),new Vector3(39.5f,1.15f,-24)};
            for(int i=0;i<5;i++)m.ingredients[i].transform.position=positions[i];
            m.checkpoint.position=new Vector3(30,1.1f,-22);m.checkpoint.rotation=Quaternion.Euler(0,180,0);
            m.entrance.transform.position=new Vector3(28.2f,1.1f,-21.7f);
            root.GetComponentsInChildren<Transform>(true).First(t=>t.name=="조리 안내판").position=m.entrance.transform.position;
            root.GetComponentsInChildren<Transform>(true).First(t=>t.name=="낮잠 중 · 조용히!").position=m.entrance.transform.position+new Vector3(0,.1f,-.05f);
            var shift=new Vector3(.14f,-.15f,-1.38f);
            foreach(var t in new[]{m.station.transform,m.cooking.rawChicken.transform,m.cooking.cookedChicken.transform,m.cooking.knife,m.cooking.bowl,m.cooking.pourBottle,root.GetComponentsInChildren<Transform>(true).First(t=>t.name=="도마")})t.position+=shift;
            m.cooking.pourBottle.position=new Vector3(30.65f,.85f,-30.65f);m.cooking.bowl.position=new Vector3(30.55f,.85f,-30.25f);
            m.cookView.position=new Vector3(30.14f,2.1f,-28.85f);m.cookView.LookAt(new Vector3(30.14f,.85f,-30.4f));
            // 모자는 머리 뼈의 수입 배율을 상쇄하고, 몸은 의자에서 졸고 있는 자세로 둔다.
            var anim=m.chef.GetComponentInChildren<Animator>();
            foreach(var bone in new[]{HumanBodyBones.LeftUpperLeg,HumanBodyBones.RightUpperLeg})anim.GetBoneTransform(bone).localRotation*=Quaternion.Euler(-75,0,0);
            foreach(var bone in new[]{HumanBodyBones.LeftLowerLeg,HumanBodyBones.RightLowerLeg})anim.GetBoneTransform(bone).localRotation*=Quaternion.Euler(80,0,0);
            m.chef.position+=Vector3.down*.4f;
            Prop("SM_Gen_Prop_Chair_01",new Vector3(37,0,-32),1,root);
            m.chefView.position=new Vector3(37,1.15f,-33.15f);m.chefView.LookAt(anim.GetBoneTransform(HumanBodyBones.Head).position);
            foreach(var t in root.GetComponentsInChildren<TextMesh>(true)){t.transform.rotation=Quaternion.identity;}
            m.camera.transform.SetPositionAndRotation(m.cookView.position,m.cookView.rotation);PrisonChickenCheck.Capture(m.camera,"Logs/Chicken-cooking-preview.png");
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            File.WriteAllText("Logs/Chicken-polish.txt","PASS placement and scaled bone attachments corrected");
        }
        static void Inspect(){
            var m=Object.FindFirstObjectByType<PrisonChickenMission>();
            var text=new System.Text.StringBuilder();
            for(int z=-38;z<=-16;z+=2)for(int x=20;x<=40;x+=2)if(NavMesh.SamplePosition(new Vector3(x,0,z),out var h,.7f,NavMesh.AllAreas))text.AppendLine("NAV "+h.position);
            foreach(var collider in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None)){var p=collider.bounds.center;if(p.x>20&&p.x<40&&p.z< -18&&p.z> -38)text.AppendLine(collider.name+" "+p+" size="+collider.bounds.size);}
            File.WriteAllText("Logs/Chicken-layout.txt",text.ToString());
            m.camera.transform.position=new Vector3(34,3,-20);m.camera.transform.LookAt(new Vector3(30,1,-30));PrisonChickenCheck.Capture(m.camera,"Logs/Chicken-kitchen-overview.png");
        }
        public static void Build()
        {
            var scene = SceneManager.GetSceneByPath("Assets/Scenes/PrisonChapter.unity");
            if (!scene.isLoaded) scene = EditorSceneManager.OpenScene("Assets/Scenes/PrisonChapter.unity", OpenSceneMode.Additive);
            EditorSceneManager.SaveScene(scene, "Logs/Prison-before-chicken-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".unity", true);
            foreach(var previous in All<PrisonChickenMission>(scene)){if(previous.enemies!=null&&previous.enemies.Length>0)throw new Exception("이미 완성된 배치입니다.");Object.DestroyImmediate(previous.gameObject);}
            Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
            metal = Mat("조리도구", new Color(.32f, .39f, .4f)); cream = Mat("라벨과 반죽", new Color(.9f, .84f, .66f));
            brown = Mat("도마", new Color(.36f, .20f, .11f)); chicken = Mat("생닭", new Color(.86f, .64f, .54f));
            golden = Mat("튀긴 치킨", new Color(.82f, .4f, .07f)); oil = Mat("기름", new Color(.45f, .35f, .12f));
            root = new GameObject("22_갓 튀긴 치킨 • 조리와 정보 거래").transform; SceneManager.MoveGameObjectToScene(root.gameObject, scene);
            var m = root.gameObject.AddComponent<PrisonChickenMission>(); m.prison = All<PrisonMission>(scene).Single();
            m.dialogue = ScriptableObject.CreateInstance<PrisonChickenText>(); AssetDatabase.CreateAsset(m.dialogue, Folder + "/갓 튀긴 치킨 대사와 시간.asset");
            m.checkpoint = Anchor("주방 재시작", new Vector3(24, 1.1f, -26), new Vector3(30, 1.1f, -29));
            m.entrance = Point(m, "주방에서 치킨 준비", new Vector3(24, 1, -26), new Vector3(.5f,.7f,.3f)); m.entrance.entrance = true;
            Box("조리 안내판", m.entrance.transform.position, new Vector3(.52f,.5f,.06f), metal, root);
            Label("낮잠 중 · 조용히!", m.entrance.transform.position + new Vector3(0,.1f,-.05f), 180);
            m.station = Point(m, "치킨 조리대", new Vector3(30, 1.05f, -29), new Vector3(2,.3f,1.2f));
            Box("작업대", new Vector3(30,.93f,-29), new Vector3(2.4f,.12f,1.2f), metal, root);
            for(int x=-1;x<=1;x+=2) for(int z=-1;z<=1;z+=2) Box("작업대 다리",new Vector3(30+x, .44f,-29+z*.45f),new Vector3(.08f,.88f,.08f),metal,root);
            Box("도마", new Vector3(29.7f,1.015f,-29), new Vector3(1.1f,.045f,.7f), brown, root);
            var c = m.station.gameObject.AddComponent<ChickenCooking>(); m.cooking = c; c.mission = m;
            c.rawChicken = Bird(new Vector3(29.7f,1.09f,-29), chicken, root);
            c.cookedChicken = Bird(new Vector3(29.7f,1.09f,-29), golden, root); c.cookedChicken.SetActive(false);
            c.bowl = Prop("SM_Gen_Prop_Pot_01",new Vector3(30.6f,1,-29),.3f,root).transform;
            c.pourBottle = Prop("SM_Gen_Prop_Bottle_01",new Vector3(30.8f,1,-29.3f),.5f,root).transform;
            c.knife = Box("조리용 칼",new Vector3(29.2f,1.12f,-29),new Vector3(.035f,.17f,.35f),metal,root);
            Box("칼 손잡이",c.knife.position+new Vector3(0,0,-.24f),new Vector3(.07f,.07f,.18f),brown,c.knife);
            m.cookView = Anchor("조리 시점", new Vector3(30,2.3f,-30.25f),new Vector3(30,1,-29));
            var cam = new GameObject("치킨 미션 카메라",typeof(Camera),typeof(AudioListener));cam.transform.SetParent(root);m.camera=cam.GetComponent<Camera>();m.camera.enabled=false;m.camera.fieldOfView=55;m.camera.nearClipPlane=.03f;cam.GetComponent<AudioListener>().enabled=false;
            string[] names={"생닭","밀가루","올리브유","물","양념소스"};
            m.ingredients=new ChickenInteraction[5];
            Vector3[] positions={new Vector3(27,1,-32),new Vector3(34,1,-32),new Vector3(36,1,-29),new Vector3(27,1,-24),new Vector3(34,1,-24)};
            for(int i=0;i<5;i++){
                var p=positions[i];Box("재료 선반",p+Vector3.down*.15f,new Vector3(.8f,.12f,.65f),metal,root);
                var item=Point(m,names[i],p+Vector3.up*.15f,new Vector3(.6f,.5f,.5f));item.ingredient=i;m.ingredients[i]=item;
                if(i==0)Bird(p,chicken,item.transform);
                else if(i==1)Box("밀가루 봉지",p+Vector3.up*.15f,new Vector3(.3f,.4f,.25f),cream,item.transform);
                else Prop("SM_Gen_Prop_Bottle_0"+(i==4?3:1),p,.65f,item.transform);
                Label(names[i],p+new Vector3(0,.6f,0),180).SetParent(item.transform,true);
            }
            // 피할 공간을 남기고 장애물을 띄엄띄엄 배치한다.
            for(int i=0;i<6;i++){
                Vector3 pos=new Vector3(27+(i%3)*3, .025f,-27-(i/3)*3);
                var hazard=new GameObject(i%2==0?"바닥 기름 · 미끄럼":"떨어진 조리도구 · 소음");hazard.transform.SetParent(root);hazard.transform.position=pos;
                var trigger=hazard.AddComponent<BoxCollider>();trigger.isTrigger=true;trigger.size=new Vector3(1,.4f,.7f);trigger.center=Vector3.up*.15f;
                var noise=hazard.AddComponent<ChickenNoiseHazard>();noise.mission=m;noise.oil=i%2==0;
                if(noise.oil){var puddle=GameObject.CreatePrimitive(PrimitiveType.Cylinder);puddle.name="기름 얼룩";puddle.transform.SetParent(hazard.transform,false);puddle.transform.localScale=new Vector3(1,.008f,.7f);puddle.GetComponent<Renderer>().sharedMaterial=oil;Object.DestroyImmediate(puddle.GetComponent<Collider>());}
                else Prop("SM_Gen_Prop_Pot_02",pos,.3f,hazard.transform);
            }
            var chef=Object.Instantiate(m.prison.introduction.informant.gameObject,root);chef.name="낮잠 자는 주방장";
            foreach(var mono in chef.GetComponentsInChildren<MonoBehaviour>(true))Object.DestroyImmediate(mono);
            chef.transform.SetPositionAndRotation(new Vector3(37,0,-32),Quaternion.Euler(0,180,0));m.chef=chef.transform;
            var animator=chef.GetComponentInChildren<Animator>();animator.enabled=true;animator.Rebind();animator.Update(0);animator.enabled=false;
            var head=animator.GetBoneTransform(HumanBodyBones.Head);head.localRotation*=Quaternion.Euler(30,0,0);
            Box("주방장 앞치마",new Vector3(37,1,-32.18f),new Vector3(.48f,.75f,.06f),cream,chef.transform);
            Box("주방장 모자",head.position+Vector3.up*.2f,new Vector3(.34f,.3f,.32f),cream,head);
            m.chefView=Anchor("주방장에게 들킨 시점",new Vector3(37,1.6f,-33.4f),head.position);
            m.ambushPoint=Anchor("치킨 강탈 시도",new Vector3(23,0,27),new Vector3(23,0,34));
            m.carrying=Bird(Vector3.zero,golden,root);m.carrying.name="손에 든 치킨";m.carrying.transform.SetParent(m.prison.player.GetComponentInChildren<Camera>(true).transform,false);m.carrying.transform.localPosition=new Vector3(.3f,-.32f,.65f);m.carrying.transform.localScale=Vector3.one*.65f;
            var population=All<PrisonPopulation>(scene).Single();
            var template=Object.Instantiate(population.inmateTemplate,root);template.name="치킨 강탈 죄수 템플릿";
            var resident=template.GetComponent<PrisonResident>();if(resident)Object.DestroyImmediate(resident);
            var combat=template.GetComponent<CitizenCombat>();combat.maximumHealth=60;combat.missionEnemy=true;combat.usesFirearm=false;combat.ExternalAttackControl=true;template.SetActive(false);
            m.enemies=new ChickenAmbusher[2];
            for(int i=0;i<2;i++){
                var wrapper=new GameObject("치킨을 노리는 죄수 "+(i+1));wrapper.transform.SetParent(root);wrapper.transform.position=new Vector3(21+i*4,0,29);
                if(NavMesh.SamplePosition(wrapper.transform.position,out var hit,4,NavMesh.AllAreas))wrapper.transform.position=hit.position;
                var enemy=wrapper.AddComponent<ChickenAmbusher>();enemy.mission=m;enemy.template=template;
                var actor=Object.Instantiate(template,wrapper.transform);actor.transform.localPosition=Vector3.zero;actor.SetActive(true);enemy.combat=actor.GetComponent<CitizenCombat>();m.enemies[i]=enemy;wrapper.SetActive(false);
            }
            All<PrisonSharedPresentation>(scene).Single().chicken=m;
            EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
            File.WriteAllText("Logs/Prison-chicken-build.txt","PASS prison scene saved; editable dialogue: "+AssetDatabase.GetAssetPath(m.dialogue));
        }
        static Material Mat(string name,Color color){var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){color=color};AssetDatabase.CreateAsset(m,Folder+"/"+name+".mat");return m;}
        static Transform Anchor(string name,Vector3 pos,Vector3 look){var t=new GameObject(name).transform;t.SetParent(root);t.position=pos;t.LookAt(look);return t;}
        static Transform Box(string name,Vector3 pos,Vector3 scale,Material material,Transform parent){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,true);go.transform.position=pos;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;return go.transform;}
        static ChickenInteraction Point(PrisonChickenMission mission,string name,Vector3 pos,Vector3 size){var go=new GameObject(name);go.transform.SetParent(root);go.transform.position=pos;go.AddComponent<BoxCollider>().size=size;var p=go.AddComponent<ChickenInteraction>();p.mission=mission;return p;}
        static Transform Label(string text,Vector3 pos,float yaw){var t=new GameObject(text).transform;t.SetParent(root);t.position=pos;t.rotation=Quaternion.Euler(0,yaw,0);var mesh=t.gameObject.AddComponent<TextMesh>();mesh.text=text;mesh.font=MokUIFont.Font;mesh.fontSize=48;mesh.characterSize=.035f;mesh.anchor=TextAnchor.MiddleCenter;mesh.color=new Color(1,.9f,.65f);t.GetComponent<MeshRenderer>().sharedMaterial=mesh.font.material;return t;}
        static GameObject Prop(string name,Vector3 pos,float scale,Transform parent){var path="Assets/Synty/PolygonGeneric/Prefabs/Props/"+name+".prefab";var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!prefab)throw new Exception("소품 없음: "+path);var obj=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);obj.transform.position=pos;obj.transform.localScale=Vector3.one*scale;foreach(var collider in obj.GetComponentsInChildren<Collider>())Object.DestroyImmediate(collider);return obj;}
        static GameObject Bird(Vector3 pos,Material mat,Transform parent){var go=new GameObject(mat==golden?"바삭한 치킨":"손질할 닭");go.transform.SetParent(parent);go.transform.position=pos;for(int i=0;i<5;i++){var part=GameObject.CreatePrimitive(PrimitiveType.Sphere);part.transform.SetParent(go.transform,false);part.transform.localPosition=i==0?Vector3.zero:new Vector3(i%2==0?.18f:-.18f,.01f,i<3?.12f:-.12f);part.transform.localScale=i==0?new Vector3(.32f,.18f,.42f):new Vector3(.13f,.11f,.24f);part.GetComponent<Renderer>().sharedMaterial=mat;Object.DestroyImmediate(part.GetComponent<Collider>());}return go;}
    }
}
#endif
