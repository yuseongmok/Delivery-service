#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;
namespace DeliveryService.Mok.Editor
{
    [InitializeOnLoad] public static class PrisonCctvSetup
    {
        static PrisonCctvSetup(){EditorApplication.update+=Poll;}
        static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/PrisonCctvSetup"))return;var action=File.ReadAllText("Temp/PrisonCctvSetup").Trim();File.Delete("Temp/PrisonCctvSetup");try{if(action=="build")Build();else Survey();}catch(Exception ex){File.WriteAllText("Logs/Prison-cctv-setup.txt",ex.ToString());}}
        static void Build()
        {
            var scene=EditorSceneManager.GetActiveScene();if(scene.name!="PrisonChapter")throw new Exception("Prison scene only");
            if(!EditorSceneManager.SaveScene(scene,"Logs/Prison-before-cctv-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new Exception("Backup failed");
            if(Object.FindFirstObjectByType<PrisonCctvMission>())throw new Exception("Already installed");
            var office=Object.FindFirstObjectByType<PrisonOfficeSearch>();var old=Object.FindFirstObjectByType<PrisonEscapeMission>();
            var root=new GameObject("28_CCTV실 잠입과 호송 준비").transform;
            var m=root.gameObject.AddComponent<PrisonCctvMission>();m.office=office;office.cctv=m;m.legacyEscape=old;old.cctvRoute=true;
            const string folder="Assets/DeliveryPrisonChapter/OfficeSearch/";
            m.dialogue=ScriptableObject.CreateInstance<PrisonCctvText>();AssetDatabase.CreateAsset(m.dialogue,folder+"CCTV 잠입과 호송 준비 대사.asset");
            Transform At(string n,Vector3 pos,float yaw=0){var t=new GameObject(n).transform;t.SetParent(root);t.SetPositionAndRotation(pos,Quaternion.Euler(0,yaw,0));return t;}
            Material Mat(string n,Color color){var mat=new Material(Shader.Find("Universal Render Pipeline/Lit")){color=color};AssetDatabase.CreateAsset(mat,folder+n+".mat");return mat;}
            var dark=Mat("CCTV실 금속",new Color(.12f,.19f,.21f));var trim=Mat("CCTV실 테두리",new Color(.34f,.41f,.39f));var off=Mat("CCTV 화면 꺼짐",new Color(.025f,.035f,.04f));var blue=Mat("CCTV 대기 화면",new Color(.12f,.4f,.42f));
            var wall=Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).First(r=>r.name=="Synty wall panel").sharedMaterial;
            GameObject Box(string n,Vector3 at,Vector3 scale,Material material,bool obstacle=false){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(root);g.transform.position=at;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=material;if(obstacle){var nav=g.AddComponent<UnityEngine.AI.NavMeshObstacle>();nav.carving=true;nav.size=Vector3.one;}return g;}
            Transform Prop(string asset,Vector3 at,float yaw=0){var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(asset);if(!prefab)throw new Exception(asset);var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);g.transform.SetParent(root);g.transform.SetPositionAndRotation(at,Quaternion.Euler(0,yaw,0));return g.transform;}
            void Label(string n,string text,Vector3 at,float yaw=0){var t=At(n,at,yaw);var l=t.gameObject.AddComponent<TextMesh>();l.text=text;l.font=MokUIFont.Font;l.fontSize=64;l.characterSize=.025f;l.anchor=TextAnchor.MiddleCenter;l.color=new Color(.9f,.94f,.85f);l.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(folder+"벽에 가려지는 글자.mat");}
            // 기존 1층 보안실 서쪽 공간을 구획한다. 출입구와 기존 가구는 유지한다.
            Box("CCTV실 북쪽 칸막이",new Vector3(-40,1.75f,-25),new Vector3(8,3.5f,.18f),wall,true);
            Box("CCTV실 출입구 남쪽 벽",new Vector3(-36,1.75f,-31.5f),new Vector3(.18f,3.5f,5),wall,true);
            Box("CCTV실 출입구 북쪽 벽",new Vector3(-36,1.75f,-25.5f),new Vector3(.18f,3.5f,1),wall,true);
            Box("CCTV실 문 위 벽",new Vector3(-36,3.05f,-27.5f),new Vector3(.18f,.9f,3),wall);
            foreach(float z in new[]{-29.05f,-25.95f})Box("CCTV 문틀",new Vector3(-35.88f,1.3f,z),new Vector3(.25f,2.6f,.14f),dark);
            m.door=At("CCTV실 여닫이문",new Vector3(-36,0,-29));
            var leaf=Box("CCTV실 보안문",new Vector3(-36,1.25f,-27.5f),new Vector3(.12f,2.5f,2.95f),dark);leaf.transform.SetParent(m.door,true);
            var handle=Box("보안문 손잡이",new Vector3(-35.88f,1.15f,-26.35f),new Vector3(.12f,.09f,.35f),trim);handle.transform.SetParent(m.door,true);
            Label("CCTV실 표지","CCTV 관제실\n허가자 외 출입 금지",new Vector3(-35.85f,2.95f,-27.5f),270);
            m.entry=At("CCTV실 문 · 간수가 등을 돌릴 때 F",new Vector3(-35.1f,.5f,-27.5f));
            m.checkpoint=At("보안실 재시도 위치",new Vector3(-30,0,-23),240);m.mingiWait=At("민기 CCTV실 대기",new Vector3(-31.8f,0,-23.2f));m.safePoint=At("민기와 보안실에서 나오기",new Vector3(-30,0,-16));
            var desk=Prop("Assets/Synty/PolygonHeist/Prefab/Props/SM_Prop_Desk_01.prefab",new Vector3(-40,.0f,-32));
            desk.localScale=new Vector3(1.7f,1,1);
            Prop("Assets/Synty/PolygonHeist/Prefab/Props/SM_Prop_Computer_Keyboard_01.prefab",new Vector3(-40,.82f,-31.9f));
            Prop("Assets/Synty/PolygonHeist/Prefab/Props/SM_Prop_Computer_Mouse_01.prefab",new Vector3(-39.35f,.82f,-31.9f));
            Prop("Assets/Synty/PolygonHeist/Prefab/Props/SM_Prop_Computer_01.prefab",new Vector3(-42,0,-32));
            m.console=At("CCTV 녹화·전송 제어반",new Vector3(-40,1.15f,-32));
            var consoleBox=m.console.gameObject.AddComponent<BoxCollider>();consoleBox.size=new Vector3(1,.4f,.25f);
            m.monitors=new PrisonCctvMonitor[3];
            var cameraPositions=new[]{new Vector3(0,3,-10),new Vector3(3,3,27),new Vector3(-3,3,47)};
            var cameraTargets=new[]{new Vector3(0,1,4),new Vector3(0,0,37),new Vector3(0,1,40)};
            for(int i=0;i<3;i++){
                var at=new Vector3(-41.25f+i*1.25f,1.65f,-32.5f);
                Box("CCTV 모니터 프레임 "+i,at,new Vector3(1.16f,.76f,.15f),dark);
                var screen=Box("CCTV 영상 "+i,at+Vector3.forward*.09f,new Vector3(1.04f,.62f,.025f),blue);Object.DestroyImmediate(screen.GetComponent<Collider>());
                var monitor=screen.AddComponent<PrisonCctvMonitor>();monitor.screen=screen.GetComponent<Renderer>();monitor.offline=off;m.monitors[i]=monitor;
                var cameraRoot=At("녹화 카메라 "+i,cameraPositions[i]);cameraRoot.LookAt(cameraTargets[i]);var camera=cameraRoot.gameObject.AddComponent<Camera>();camera.enabled=false;camera.fieldOfView=65;camera.nearClipPlane=.1f;camera.farClipPlane=100;monitor.feed=camera;
                Prop("Assets/Synty/PolygonCity/Prefabs/Props/SM_Prop_SecurityCamera_01.prefab",cameraPositions[i]);
            }
            Label("녹화 제어반 안내","수감동 / 운동장 / 정문\n녹화 · 외부 전송",new Vector3(-40,2.5f,-32.5f),180);
            var guard=Object.Instantiate(office.previous.keyGuard.gameObject,root);guard.name="CCTV실 담당 간수";guard.transform.SetPositionAndRotation(new Vector3(-33.5f,0,-27.5f),Quaternion.Euler(0,270,0));
            m.watch=guard.GetComponent<PrisonGuard>();m.watch.holdPost=true;m.watch.postPosition=guard.transform.position;m.watch.postYaw=270;m.watch.infirmaryWatch=false;m.watch.route=Array.Empty<Transform>();m.watch.receivingMeal=false;m.watch.sightRange=12;m.watch.sightAngle=70;
            m.restrictedArea=new Bounds(new Vector3(-39.5f,1.5f,-29.2f),new Vector3(9,3,9));
            var bind=office.evidence.gameObject.AddComponent<PrisonWardenRestraint>();bind.warden=office.previous.warden;m.restraint=bind;
            bind.seat=At("염윤태 묶인 의자 자세",new Vector3(-31.98f,3.32f,-26),90);
            bind.ropes=At("염윤태 의자 결박 끈",bind.seat.position).gameObject;
            var ropeMaterial=Mat("의자 결박 끈",new Color(.42f,.30f,.16f));
            for(int ring=0;ring<3;ring++){var g=new GameObject("결박 끈 "+ring);g.transform.SetParent(bind.ropes.transform,false);var line=g.AddComponent<LineRenderer>();line.useWorldSpace=false;line.loop=true;line.widthMultiplier=.035f;line.sharedMaterial=ropeMaterial;line.positionCount=16;for(int k=0;k<16;k++){float a=k*Mathf.PI*2/16;line.SetPosition(k,new Vector3(Mathf.Cos(a)*.36f,(ring-1)*.09f,Mathf.Sin(a)*.27f-.05f));}}
            bind.ropes.SetActive(false);
            var presentation=Object.FindFirstObjectByType<PrisonSharedPresentation>();presentation.cctv=m;
            office.dialogue.complete="민기: 간수복은 확보했어. 갈아입고 아래층 CCTV실로 가자.";
            office.evidence.dialogue.documents[1]="별도 인계 명단\n대상: 이현우 / 민기\n정기 호송 출발 이후 별도 이송\n목적지: 공란\n인계자: 박진웅 측";
            office.evidence.dialogue.transfer=new[]{"나: 여기 우리 이름이 있어. 정기 호송이 나간 뒤에 따로 넘긴대.","민기: 인계자가 박진웅 측이야. 그 전에 빠져나가야 해."};
            office.evidence.dialogue.message=new[]{"민기: 경찰만 한패인 줄 알았는데… 여기까지 연결돼 있었어.","나: 정기 호송차량이 나갈 때 같이 빠져나갈 방법을 찾자.","민기: 이 증거로 염윤태한테 출발 시간과 감시 체계부터 알아내자."};
            EditorUtility.SetDirty(office.dialogue);EditorUtility.SetDirty(office.evidence.dialogue);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            File.WriteAllText("Logs/Prison-cctv-setup.txt","PASS saved CCTV room, restraint, new dialogue and route. Original escape route suspended.");
        }
        static void Survey(){var scene=EditorSceneManager.GetActiveScene();var text="";foreach(var g in scene.GetRootGameObjects()){text+="ROOT "+g.name+" "+g.transform.position+"\n";foreach(var t in g.GetComponentsInChildren<Transform>(true))if(t.name.Contains("관리")||t.name.Contains("사무")||t.name.Contains("면회")||t.name.Contains("접견")||t.name.Contains("간수장")||t.name.Contains("책상")||t.name.Contains("의자"))text+=t.name+" "+t.position+"\n";}foreach(var t in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Where(t=>t.name.StartsWith("03_")||t.name.StartsWith("09_")||t.name.StartsWith("15_"))){foreach(Transform child in t){var rs=child.GetComponentsInChildren<Renderer>();var b=new Bounds(child.position,Vector3.zero);foreach(var r in rs)b.Encapsulate(r.bounds);text+="DETAIL "+child.name+" "+child.position+" "+b+"\n";}}File.WriteAllText("Logs/Prison-cctv-survey.txt",text);}
    }
}
#endif
