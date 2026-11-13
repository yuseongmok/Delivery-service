#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.AI;
using Object=UnityEngine.Object;
namespace DeliveryService.Mok.Editor
{
    [InitializeOnLoad]
    public static class PrisonKeysSetup
    {
        static PrisonKeysSetup(){EditorApplication.update+=Poll;}
        static void Poll(){const string file="Temp/PrisonKeys-request";if(EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(file))return;var action=File.ReadAllText(file).Trim();File.Delete(file);try{if(action=="inspect")Inspect();else if(action=="build")Build();else if(action=="test")PrisonKeysCheck.Run();else if(action=="polish")Polish();}catch(Exception e){File.WriteAllText("Logs/Prison-keys-setup.txt",e.ToString());Debug.LogException(e);}}
        static void Polish(){
            var m=Object.FindFirstObjectByType<PrisonKeysMission>();var scene=m.gameObject.scene;EditorSceneManager.SaveScene(scene,"Logs/Prison-before-keys-polish.unity",true);
            const string wallPath="Assets/DeliveryPrisonChapter/KeysMission/창고 벽.mat";
            var wall=AssetDatabase.LoadAssetAtPath<Material>(wallPath);
            if(!wall){wall=new Material(Shader.Find("Universal Render Pipeline/Lit")){color=new Color(.33f,.39f,.37f)};AssetDatabase.CreateAsset(wall,wallPath);}
            foreach(var name in new[]{"창고 칸막이 왼쪽","창고 칸막이 오른쪽","창고 앞 벽 왼쪽","창고 앞 벽 오른쪽"})m.transform.Find(name).GetComponent<Renderer>().sharedMaterial=wall;
            m.oven.GetComponent<Renderer>().enabled=false;
            foreach(var supply in m.cookingSupplies.GetComponentsInChildren<PrisonPizzaSupply>(true)){
                var name=supply.ingredient.toppingName+" 안내";if(m.transform.Find(name))continue;
                var go=new GameObject(name);go.transform.SetParent(m.transform,false);go.transform.position=supply.transform.position+Vector3.up*.3f;go.transform.rotation=Quaternion.Euler(0,180,0);
                var text=go.AddComponent<TextMesh>();text.text=supply.ingredient.toppingName;text.font=MokUIFont.Font;text.fontSize=48;text.characterSize=.025f;text.anchor=TextAnchor.MiddleCenter;go.GetComponent<Renderer>().sharedMaterial=text.font.material;
            }
            if(!m.transform.Find("피자 재료 폐기통")){var bin=GameObject.CreatePrimitive(PrimitiveType.Cylinder);bin.name="피자 재료 폐기통";bin.transform.SetParent(m.transform);bin.transform.position=new Vector3(35.2f,.5f,-31.5f);bin.transform.localScale=new Vector3(.55f,.5f,.55f);bin.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/DeliveryPrisonChapter/KeysMission/창고 철문.mat");bin.AddComponent<TrashCan>();}
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            m.camera.transform.position=new Vector3(-39.5f,1.8f,10.8f);m.camera.transform.LookAt(new Vector3(-39.5f,1.3f,14.5f));PrisonChickenCheck.Capture(m.camera,"Logs/Keys-pantry-editor.png");
            m.camera.transform.position=new Vector3(31.5f,2.2f,-29);m.camera.transform.LookAt(new Vector3(29.5f,1.1f,-32.5f));PrisonChickenCheck.Capture(m.camera,"Logs/Keys-pizza-editor.png");
            m.camera.transform.position=new Vector3(36,2.2f,-21);m.camera.transform.LookAt(m.loadPoint.position+Vector3.up*.8f);PrisonChickenCheck.Capture(m.camera,"Logs/Keys-cart-editor.png");
        }
        static void Inspect(){var report=new System.Text.StringBuilder();foreach(var t in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))report.AppendLine("LABEL "+t.text+" "+t.transform.position);foreach(var c in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))if(c.bounds.center.x< -16&&(c.bounds.center.z>9||c.bounds.center.y>3.5f))report.AppendLine("COL "+c.name+" "+c.bounds);foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))if(t.name.ToLower().Contains("oven"))report.AppendLine("OVEN "+t.name+" "+t.position);File.WriteAllText("Logs/Prison-keys-layout.txt",report.ToString());}
        static void Field(Object o,string name,Object value){var s=new SerializedObject(o);s.FindProperty(name).objectReferenceValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
        static void Build(){
            var previous=Object.FindFirstObjectByType<PrisonMedicineMission>();var scene=previous.gameObject.scene;if(scene.name!="PrisonChapter")throw new Exception("감옥 씬에서 실행하세요.");
            EditorSceneManager.SaveScene(scene,"Logs/Prison-before-keys-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
            var partial=Object.FindFirstObjectByType<PrisonKeysMission>();if(partial){if(partial.camera)throw new Exception("이미 연결되어 있습니다.");Object.DestroyImmediate(partial.gameObject);}
            const string folder="Assets/DeliveryPrisonChapter/KeysMission";Directory.CreateDirectory(folder);AssetDatabase.Refresh();
            var root=new GameObject("25_소매치기 • 창고와 피자 배식").transform;var m=root.gameObject.AddComponent<PrisonKeysMission>();m.previous=previous;m.dialogue=AssetDatabase.LoadAssetAtPath<PrisonKeysText>(folder+"/소매치기와 배식 대사.asset");if(!m.dialogue){m.dialogue=ScriptableObject.CreateInstance<PrisonKeysText>();AssetDatabase.CreateAsset(m.dialogue,folder+"/소매치기와 배식 대사.asset");}
            Material Mat(string name,Color color){var mat=AssetDatabase.LoadAssetAtPath<Material>(folder+"/"+name+".mat");if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit")){color=color};AssetDatabase.CreateAsset(mat,folder+"/"+name+".mat");}return mat;}
            var metal=Mat("창고 철문",new Color(.19f,.25f,.23f));var gold=Mat("열쇠",new Color(.69f,.48f,.17f));var white=Mat("식료품 포장",new Color(.85f,.8f,.65f));var red=Mat("소스 통",new Color(.55f,.12f,.08f));var wall=Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).First(r=>r.name=="Synty wall panel").sharedMaterial;
            Transform Anchor(string name,Vector3 pos,Transform parent=null){var t=new GameObject(name).transform;t.SetParent(parent?parent:root,false);t.position=pos;return t;}
            Transform Box(string name,Vector3 pos,Vector3 size,Material mat,Transform parent=null,bool solid=true){var t=GameObject.CreatePrimitive(PrimitiveType.Cube).transform;t.name=name;t.SetParent(parent?parent:root,true);t.position=pos;t.localScale=size;t.GetComponent<Renderer>().sharedMaterial=mat;if(!solid)Object.DestroyImmediate(t.GetComponent<Collider>());return t;}
            void Carve(Transform t){var n=t.gameObject.AddComponent<NavMeshObstacle>();n.carving=true;n.shape=NavMeshObstacleShape.Box;n.size=Vector3.one;}
            Carve(Box("창고 칸막이 왼쪽",new Vector3(-43,1.4f,15),new Vector3(.12f,2.8f,4),wall));Carve(Box("창고 칸막이 오른쪽",new Vector3(-36,1.4f,15),new Vector3(.12f,2.8f,4),wall));
            Carve(Box("창고 앞 벽 왼쪽",new Vector3(-41.65f,1.4f,13),new Vector3(2.7f,2.8f,.12f),wall));Carve(Box("창고 앞 벽 오른쪽",new Vector3(-37.35f,1.4f,13),new Vector3(2.7f,2.8f,.12f),wall));
            var pivot=Anchor("식료품 창고 문 경첩",new Vector3(-40.3f,0,13));Box("열리는 창고 철문",new Vector3(-39.5f,1.3f,13),new Vector3(1.6f,2.6f,.12f),metal,pivot);var obstruction=pivot.gameObject.AddComponent<NavMeshObstacle>();obstruction.carving=true;obstruction.center=new Vector3(.8f,1.3f,0);obstruction.size=new Vector3(1.6f,2.6f,.15f);
            var lockPoint=Anchor("식료품 창고 자물쇠",new Vector3(-39,1.15f,12.8f));m.keyLock=lockPoint.gameObject.AddComponent<PrisonKeyLock>();m.keyLock.door=pivot;m.keyLock.keyModels=new GameObject[4];Box("문 잠금장치",lockPoint.position,new Vector3(.12f,.15f,.08f),gold,pivot);
            for(int i=0;i<4;i++){var key=Anchor("열쇠 "+(i+1),lockPoint.position+new Vector3(0,0,-.2f));Box("열쇠 몸",key.position,new Vector3(.035f,.035f,.19f),gold,key,false);Box("열쇠 머리",key.position+Vector3.back*.12f,new Vector3(.10f,.08f,.04f),gold,key,false);for(int j=0;j<=i;j++)Box("열쇠 이",key.position+new Vector3(.035f,0,j*.035f),new Vector3(.06f,.025f,.02f),gold,key,false);key.gameObject.SetActive(false);m.keyLock.keyModels[i]=key.gameObject;}
            Carve(Box("숨을 수 있는 식료품 상자",new Vector3(-35.5f,.8f,12),new Vector3(1.2f,1.6f,1.2f),metal));
            m.checkpoint=Anchor("창고 재시작",new Vector3(-34,1.1f,7));m.checkpoint.rotation=Quaternion.Euler(0,0,0);
            var population=Object.FindFirstObjectByType<PrisonPopulation>();
            PrisonGuard Guard(string name,Vector3 p,bool post){var g=Object.Instantiate(population.guardTemplate,root);g.name=name;g.transform.position=p;g.transform.rotation=Quaternion.Euler(0,180,0);var guard=g.GetComponent<PrisonGuard>();guard.infirmaryWatch=false;guard.holdPost=post;guard.postPosition=p;guard.postYaw=180;guard.route=Array.Empty<Transform>();g.GetComponent<CitizenCombat>().missionEnemy=true;g.SetActive(false);return guard;}
            m.keyGuard=Guard("창고 열쇠 관리 간수",new Vector3(-32,0,14),false);m.keyGuard.sightRange=13;m.keyGuard.sightAngle=75;m.keyGuard.route=new[]{Anchor("창고 순찰 서쪽",new Vector3(-33,0,14)),Anchor("창고 순찰 동쪽",new Vector3(-23,0,14))};
            m.keyRing=Box("간수 허리의 열쇠 꾸러미",m.keyGuard.transform.position+new Vector3(-.2f,.9f,-.15f),new Vector3(.08f,.15f,.05f),gold,m.keyGuard.transform,false).gameObject;
            var all=AssetDatabase.FindAssets("t:PizzaToppingData").Select(g=>AssetDatabase.LoadAssetAtPath<PizzaToppingData>(AssetDatabase.GUIDToAssetPath(g))).ToArray();var types=new[]{ToppingType.Dough,ToppingType.Sauce,ToppingType.Cheese,ToppingType.Pepperoni};m.ingredients=types.Select(t=>all.First(d=>d.toppingType==t&&d.toppingPrefab)).ToArray();
            m.supplies=new GameObject[4];var shelf=Box("창고 식료품 선반",new Vector3(-39.5f,.7f,16),new Vector3(5,.12f,1),metal);
            for(int i=0;i<4;i++){m.supplies[i]=Box(m.ingredients[i].toppingName+" 재료 상자",new Vector3(-41.3f+i*1.2f,1,16),new Vector3(.6f,.45f,.55f),i==1?red:white).gameObject;}
            m.kitchen=Anchor("피자 제작대",new Vector3(29,1,-32.4f));m.maker=Box("피자 제작 상호작용",m.kitchen.position,new Vector3(1,.12f,.85f),metal).gameObject.AddComponent<PizzaMaker>();Field(m.maker,"doughPlacePoint",Anchor("도우 놓는 곳",m.kitchen.position+Vector3.up*.12f));
            var ovenTarget=Box("피자 오븐 상호작용",new Vector3(26.72f,1.25f,-32.32f),new Vector3(.75f,.6f,.12f),metal);m.oven=ovenTarget.gameObject.AddComponent<Oven>();Field(m.oven,"ovenPlacePoint",Anchor("오븐 속 피자",new Vector3(26.72f,1.25f,-32.65f)));var ovenSettings=new SerializedObject(m.oven);ovenSettings.FindProperty("bakingTime").floatValue=5;ovenSettings.ApplyModifiedPropertiesWithoutUndo();
            m.cookingSupplies=Anchor("확보한 피자 재료",Vector3.zero).gameObject;
            for(int i=0;i<4;i++){var item=Box(m.ingredients[i].toppingName+" 꺼내기",new Vector3(31.2f+i*.65f,1.1f,-32.6f),new Vector3(.5f,.2f,.55f),i==1?red:white,m.cookingSupplies.transform);var supply=item.gameObject.AddComponent<PrisonPizzaSupply>();supply.mission=m;supply.ingredient=m.ingredients[i];}
            if(!previous.prison.cart){
                var cart=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Scripts/Woong/Scripts/Cart/Cart.prefab"),root);cart.name="감옥 피자 배식카트";cart.transform.position=new Vector3(33,.15f,-24.5f);
                foreach(var storage in cart.GetComponentsInChildren<FoodCartStorage>(true))Object.DestroyImmediate(storage);
                var meal=cart.AddComponent<PrisonMealCart>();meal.controller=cart.GetComponentInChildren<FoodCartController>(true);meal.controller.cartRoot=cart.transform;meal.controller.cartRigidbody=cart.GetComponent<Rigidbody>();if(!meal.controller.cartRigidbody)meal.controller.cartRigidbody=cart.AddComponent<Rigidbody>();meal.controller.cartStorage=null;meal.controller.pushMoveSpeed=4;meal.controller.minKnockdownSpeed=100;
                meal.preparedMeals=Anchor("빈 준비 공간",cart.transform.position,cart.transform).gameObject;meal.meals=Array.Empty<GameObject>();previous.prison.cart=meal;cart.SetActive(true);
            }
            m.loadPoint=previous.prison.cart.transform;m.pizzaOnCart=Anchor("간수 배식 피자",m.loadPoint.position+Vector3.up*.75f,m.loadPoint).gameObject;
            m.diners=new[]{Guard("수감동 간수",new Vector3(-2,0,3),true),Guard("중앙 통로 간수",new Vector3(2,0,-20),true),Guard("간수장실 경비",new Vector3(-14.2f,3.75f,-31),true)};
            m.warden=Guard("간수장 염윤태",new Vector3(-29,3.75f,-26),true);m.warden.postYaw=90;
            m.officeDoor=Box("간수장실 출입문",new Vector3(-15.85f,5.05f,-31),new Vector3(.15f,2.6f,2.2f),metal);
            var camera=Anchor("소매치기 미션 카메라",Vector3.zero).gameObject;m.camera=camera.AddComponent<Camera>();m.camera.fieldOfView=55;m.camera.nearClipPlane=.03f;m.camera.enabled=false;camera.AddComponent<AudioListener>().enabled=false;
            Object.FindFirstObjectByType<PrisonSharedPresentation>().keys=m;
            EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);File.WriteAllText("Logs/Prison-keys-setup.txt","PASS saved: theft / key lock / pantry / original pizza maker and oven / cart / office");
        }
    }
}
#endif

