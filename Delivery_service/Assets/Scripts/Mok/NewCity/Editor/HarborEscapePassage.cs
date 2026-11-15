#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace DeliveryService.Mok.NewCity.Editor
{
    [InitializeOnLoad]
    internal static class HarborEscapePassage
    {
        static Material brick, iron, concrete, red;
        static HarborEscapePassage(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            const string request="Temp/HarborEscapePassage";
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(request))return;
            string command=File.ReadAllText(request).Trim();File.Delete(request);
            try{if(command=="apply")Build();Capture();}catch(Exception e){File.WriteAllText("Logs/HarborEscape-error.txt",e.ToString());Debug.LogException(e);}
        }
        static void Build()
        {
            var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/NewHarborCity.unity")throw new Exception("새 도시 씬 전용 작업입니다.");
            EditorSceneManager.SaveScene(scene,"Logs/HarborEscape-before-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
            DistrictArt.Scene=scene;
            var root=scene.GetRootGameObjects().First(g=>g.name.StartsWith("NEW HARBOR")).transform;
            var old=root.Find("07 세탁 피날레 • 후문 탈출 골목");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
            DistrictArt.Parent=root;var passage=DistrictArt.Group("07 세탁 피날레 • 후문 탈출 골목");DistrictArt.Parent=passage;
            brick=DistrictArt.Pattern("Escape weathered brick",new Color(.36f,.29f,.23f),new Color(.2f,.19f,.17f),true);
            iron=DistrictArt.Mat("Escape gate steel",new Color(.16f,.2f,.19f));
            concrete=DistrictArt.Mat("Escape pavement",new Color(.30f,.33f,.32f));
            red=DistrictArt.Mat("Escape cart red",new Color(.57f,.12f,.07f));
            Box("후문 연결 바닥",9.2f,.005f,22.5f,3.7f,.1f,9,concrete);
            Box("부축 이동과 철문 골목",-1.4f,.005f,27,24.8f,.1f,4,concrete);
            Box("시야가 꺾이는 탈출 바닥",-12,.005f,32.1f,4,.1f,10.2f,concrete);
            // 후문에서 한 번 꺾이고 철문 이후에는 뒤돌아볼 직선 구간을 남긴다.
            Box("후문 동쪽 벽",11.3f,2.2f,23.5f,.3f,4.4f,11,brick);
            Box("후문 서쪽 벽",7.2f,2.2f,21.4f,.3f,4.4f,6.8f,brick);
            Box("골목 남쪽 벽",-3.4f,2.2f,24.8f,21.5f,4.4f,.3f,brick);
            Box("골목 북쪽 벽",.8f,2.2f,29.2f,21.3f,4.4f,.3f,brick);
            Box("탈출 통로 서쪽 벽",-14.2f,2.2f,29.65f,.3f,4.4f,10,brick);
            Box("탈출 통로 동쪽 벽",-9.8f,2.2f,31.85f,.3f,4.4f,5.3f,brick);
            Buildings();
            foreach(float x in new[]{-12f,-8,-2,4,9})
            {
                if(x<7)Box("벽 하부 마감",x,.3f,24.6f,3.8f,.6f,.12f,iron);
                Box("골목 벽등",x,3.1f,25, .28f,.22f,.28f,iron,false);
                DistrictArt.Lamp(new Vector3(x,2.9f,26),4,1.4f,new Color(1,.77f,.48f));
            }
            // 미션 이전에 익히는 코너 상점 셔터와 빨간 반송 카트.
            Box("코너 상점 셔터 프레임",7.38f,1.5f,21.3f,.12f,3,2.9f,iron);
            for(int i=0;i<16;i++)Box("셔터 슬랫",7.47f,.14f+i*.18f,21.3f,.06f,.15f,2.6f,concrete,false);
            DistrictArt.Sign("납품 / 반송",new Vector3(7.5f,3.3f,21.3f),.05f,270);
            var cart=DistrictArt.Group("빨간 손잡이 반송 카트 • 이동 기능 연결 예정");DistrictArt.Parent=cart;
            Box("카트 적재판",7.95f,.32f,23, .9f,.14f,1.25f,iron);
            foreach(float x in new[]{7.58f,8.32f})foreach(float z in new[]{22.5f,23.5f})Box("카트 바퀴",x,.15f,z,.12f,.25f,.25f,iron,false);
            foreach(float x in new[]{7.58f,8.32f})Box("빨간 손잡이 기둥",x,.76f,23.5f,.06f,.9f,.06f,red,false);
            Box("빨간 손잡이",7.95f,1.2f,23.5f,.8f,.07f,.07f,red,false);
            DistrictArt.Parent=passage;
            Gate();
            DistrictArt.Prop("SM_Prop_Trashbin_02",new Vector3(4,.06f,28.45f),.9f,0,true);
            DistrictArt.Prop("SM_Prop_Roof_Aircon_01",new Vector3(-1,3,29),.6f,180);
            Marker("01 민기 부상 지점",new Vector3(6,.08f,27));
            Marker("02 민기 최후 위치 • 철문 안쪽",new Vector3(-3.6f,.08f,27));
            Marker("03 플레이어 밀려나는 위치",new Vector3(-6.5f,.08f,27));
            Marker("04 뒤돌아보는 위치",new Vector3(-11.4f,.08f,27));
            Marker("05 박진웅 접근 위치",new Vector3(1,.08f,27));
            Marker("06 시야가 끊기는 모퉁이",new Vector3(-12,.08f,31));
            Marker("07 마지막 탈출 지점",new Vector3(-12,.08f,37.5f));
            Physics.SyncTransforms();
            var report=new System.Text.StringBuilder();
            Check(report,new Vector3(9.2f,0,18.6f),new Vector3(9.2f,0,27));
            Check(report,new Vector3(9.2f,0,27),new Vector3(-12,0,27));
            Check(report,new Vector3(-12,0,27),new Vector3(-12,0,37));
            bool sight=!Physics.Linecast(new Vector3(-11.4f,1.65f,27),new Vector3(-3.6f,1.65f,27));
            report.AppendLine((sight?"PASS":"FAIL")+" 철문 너머 민기 시야 (철문 열림 상태)");
            File.WriteAllText("Logs/HarborEscape-validation.txt",report.ToString());
            EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();File.WriteAllText("Temp/HarborBlockPolish","validate");
        }
        // 골목의 경계를 독립 담장이 아니라 실제 상가의 후면과 창고 매스로 만든다.
        static void Buildings()
        {
            var parent=DistrictArt.Parent;
            DistrictArt.Parent=DistrictArt.Group("골목을 둘러싼 상가와 매장 부속 창고");
            FitBuilding("북쪽 세탁소", "SM_Bld_Shop_04",new Vector3(-4.5f,.05f,31.95f),new Vector3(10,5.4f,5.1f),0);
            FitBuilding("북쪽 수리 상점", "SM_Bld_Shop_02",new Vector3(6,.05f,31.95f),new Vector3(10.5f,6.3f,5.1f),0);
            FitBuilding("서쪽 코너 상가", "SM_Bld_Shop_06",new Vector3(-18.2f,.05f,29.6f),new Vector3(7.5f,7.8f,9.8f),0);
            FitBuilding("동쪽 납품 창고", "SM_Bld_Shop_02",new Vector3(15.2f,.05f,23.6f),new Vector3(7.4f,5.1f,11),90);
            // 매장 뒤 얕은 부속동: 기존 주방 뒤의 불필요한 틈을 채운다.
            Box("매장 부속 창고 본체",-2.5f,2.2f,21.55f,19,4.4f,6.2f,brick);
            Box("매장 부속 창고 지붕",-2.5f,4.45f,21.55f,19.3f,.18f,6.4f,concrete);
            foreach(float x in new[]{-10f,-6f,-2f,2f})
            {
                Box("창고 후면 창틀",x,2.75f,24.7f,1.5f,.85f,.1f,iron,false);
                Box("창고 후면 불투명 유리",x,2.75f,24.77f,1.3f,.64f,.04f,concrete,false);
                Box("창고 창문 세로 프레임",x,2.75f,24.81f,.05f,.7f,.04f,iron,false);
            }
            foreach(float x in new[]{-7f,3f})
            {
                Box("상가 후문",x,1.15f,29.02f,1.05f,2.3f,.1f,iron);
                Box("상가 후문 손잡이",x+.33f,1.1f,28.91f,.06f,.18f,.12f,red,false);
                Box("상가 후문 처마",x,2.55f,28.9f,1.5f,.12f,.7f,iron,false);
                DistrictArt.Prop("SM_Prop_Roof_Aircon_01",new Vector3(x+2,3.1f,28.95f),.55f,180);
                Box("배수관",x+1.2f,2.2f,29.02f,.1f,4.4f,.1f,iron,false);
            }
            DistrictArt.Prop("SM_Prop_Roof_Aircon_01",new Vector3(-4,4.55f,21),.8f);
            DistrictArt.Parent=parent;
        }
        static void FitBuilding(string label,string prefab,Vector3 ground,Vector3 size,float yaw)
        {
            var g=DistrictArt.Prop(prefab,ground,0,yaw,false);g.name=label;
            var b=DistrictArt.Bounds(g);var scale=new Vector3(size.x/b.size.x,size.y/b.size.y,size.z/b.size.z);
            if(Mathf.Abs(Mathf.Sin(yaw*Mathf.Deg2Rad))>.5f)scale=new Vector3(scale.z,scale.y,scale.x);
            g.transform.localScale=Vector3.Scale(g.transform.localScale,scale);
            b=DistrictArt.Bounds(g);g.transform.position+=ground-new Vector3(b.center.x,b.min.y,b.center.z);
            b=DistrictArt.Bounds(g);var collision=new GameObject(label+" 충돌");collision.transform.SetParent(DistrictArt.Parent,false);collision.transform.position=b.center;collision.AddComponent<BoxCollider>().size=b.size;collision.isStatic=true;
            PrefabUtility.RecordPrefabInstancePropertyModifications(g.transform);
        }
        static void Gate()
        {
            var parent=DistrictArt.Parent;
            foreach(float z in new[]{25f,29f})Box("철문 고정 기둥",-5,1.6f,z,.18f,3.2f,.18f,iron);
            Box("철문 상부 프레임",-5,3.15f,27,.18f,.16f,4.2f,iron);
            var pivot=DistrictArt.Group("철문 회전축 • Y 0 닫힘 / -90 열림");pivot.position=new Vector3(-5,0,25.1f);DistrictArt.Parent=pivot;
            foreach(float y in new[]{.2f,1.3f,2.95f})Box("철문 가로 보강대",-5,y,27,.1f,.09f,3.75f,iron);
            for(float z=25.15f;z<28.95f;z+=.23f)Box("철문 세로 쇠창살",-5,1.57f,z,.065f,2.8f,.065f,iron);
            // 잠금 상태에서 사람은 막고 철창 사이로 장면은 볼 수 있도록 얇은 충돌판 사용.
            var barrier=new GameObject("닫힌 문 충돌판");barrier.transform.SetParent(pivot,true);barrier.transform.position=new Vector3(-5,1.55f,27);barrier.AddComponent<BoxCollider>().size=new Vector3(.12f,3,3.8f);
            Box("안쪽 잠금 손잡이",-4.85f,1.25f,28.6f,.15f,.12f,.32f,iron,false);
            pivot.rotation=Quaternion.Euler(0,-90,0);
            DistrictArt.Parent=parent;
            var chain=DistrictArt.Group("잠금 사슬과 자물쇠 • 컷씬 잠금 시 활성화");DistrictArt.Parent=chain;
            for(int i=0;i<8;i++)
            {
                var ring=new GameObject("사슬 고리");ring.transform.SetParent(chain,false);var lr=ring.AddComponent<LineRenderer>();lr.useWorldSpace=false;lr.loop=true;lr.positionCount=12;lr.widthMultiplier=.025f;lr.sharedMaterial=iron;
                ring.transform.position=new Vector3(-4.83f,1.28f,28.35f+i*.085f);
                for(int j=0;j<12;j++){float a=j*Mathf.PI/6;lr.SetPosition(j,new Vector3(i%2==0?Mathf.Sin(a)*.055f:0,i%2==1?Mathf.Sin(a)*.055f:0,Mathf.Cos(a)*.09f));}
            }
            Box("자물쇠",-4.8f,1.15f,28.7f,.12f,.2f,.15f,red,false);chain.gameObject.SetActive(false);DistrictArt.Parent=parent;
        }
        static void Box(string n,float x,float y,float z,float sx,float sy,float sz,Material m,bool solid=true)=>DistrictArt.Box(n,new Vector3(x,y,z),new Vector3(sx,sy,sz),m,solid);
        static void Marker(string n,Vector3 p){var g=new GameObject(n);g.transform.SetParent(DistrictArt.Parent,false);g.transform.position=p;}
        static void Check(System.Text.StringBuilder report,Vector3 a,Vector3 b)
        {
            bool clear=true;for(float t=0;t<=1;t+=.02f){var p=Vector3.Lerp(a,b,t);if(Physics.CheckCapsule(p+Vector3.up*.42f,p+Vector3.up*1.62f,.28f,~0,QueryTriggerInteraction.Ignore))clear=false;}
            report.AppendLine((clear?"PASS ":"FAIL ")+a+" -> "+b);
        }
        static void Capture()
        {
            var camera=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>(true)).First();var p=camera.transform.position;var q=camera.transform.rotation;var target=camera.targetTexture;var active=RenderTexture.active;
            foreach(var shot in new[]{(new Vector3(15,21,42),new Vector3(-1,0,26),"overview"),(new Vector3(-11.4f,1.7f,27),new Vector3(-3.6f,1.7f,27),"gate"),(new Vector3(9.2f,1.7f,19),new Vector3(9,1.6f,27),"rear")})
            {var rt=new RenderTexture(1600,900,24);var tex=new Texture2D(1600,900,TextureFormat.RGB24,false);try{camera.transform.position=shot.Item1;camera.transform.LookAt(shot.Item2);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1600,900),0,0);tex.Apply();File.WriteAllBytes("Logs/HarborEscape-"+shot.Item3+".png",tex.EncodeToPNG());}finally{camera.targetTexture=target;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(rt);}}
            camera.transform.SetPositionAndRotation(p,q);
        }
    }
}
#endif



