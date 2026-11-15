#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AI;
namespace DeliveryService.Mok.NewCity.Editor
{
    // 완성된 거리와 사용자 배치를 유지하며 매장 내부만 꾸민다.
    [InitializeOnLoad]
    internal static class ShopInteriorPolish
    {
        static Material wood, green, cream, brass, dark, glass;
        static ShopInteriorPolish() { EditorApplication.update += Poll; }
        static void Poll()
        {
            const string request="Temp/ShopInteriorPolish";
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(request))return;
            string command=File.ReadAllText(request).Trim(); File.Delete(request);
            try { if(command=="apply")Apply(); Capture(); }
            catch(Exception e){File.WriteAllText("Logs/ShopInterior-error.txt",e.ToString());Debug.LogException(e);}
        }
        static void Apply()
        {
            var scene=SceneManager.GetActiveScene();
            if(scene.path!="Assets/Scenes/NewHarborCity.unity")throw new Exception("새 도시 씬에서만 실행할 수 있습니다.");
            EditorSceneManager.SaveScene(scene,"Logs/ShopInterior-before-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
            DistrictArt.Scene=scene;
            var shop=scene.GetRootGameObjects().First(g=>g.name.StartsWith("NEW HARBOR")).transform.Cast<Transform>().First(t=>t.name.StartsWith("02 "));
            var previous=shop.Find("실내 업그레이드 • 부스와 음료 바");if(previous)UnityEngine.Object.DestroyImmediate(previous.gameObject);
            foreach(var t in shop.Cast<Transform>().ToArray())
            {
                bool furniture=t.name.StartsWith("SM_Prop_ShopInterior_Table")||t.name.StartsWith("SM_Prop_ShopInterior_Chair")||t.name.StartsWith("SM_Gen_Prop_Plate")||t.name=="테이블 메뉴"||t.name=="외곽 충돌";
                if(furniture&&Mathf.Abs(t.position.x)>6&&t.position.z>1&&t.position.z<9&&t.position.y<2)UnityEngine.Object.DestroyImmediate(t.gameObject);
            }
            DistrictArt.Parent=shop;DistrictArt.Parent=DistrictArt.Group("실내 업그레이드 • 부스와 음료 바");
            wood=DistrictArt.Mat("Interior walnut",new Color(.25f,.13f,.075f));
            green=DistrictArt.Mat("Interior booth leather",new Color(.13f,.27f,.23f));
            cream=DistrictArt.Mat("Interior ivory",new Color(.84f,.78f,.61f));
            brass=DistrictArt.Mat("Interior brass",new Color(.65f,.42f,.15f));
            dark=DistrictArt.Mat("Interior charcoal",new Color(.045f,.06f,.065f));
            glass=AssetDatabase.LoadAssetAtPath<Material>(DistrictArt.Folder+"/Materials/Clear shop glazing.mat");
            Booth(-9,3);Booth(-9,7);
            Drinks();Display();Staff();
            for(int i=0;i<3;i++)
            {
                float x=-7+i*4;
                Box("벽 장식 액자",new Vector3(x,2.15f,17.8f),new Vector3(2.4f,1.2f,.08f),wood,false);
                Box("액자 속 크림 패널",new Vector3(x,2.15f,17.74f),new Vector3(2.2f,1,.03f),cream,false);
                DistrictArt.Sign(new[]{"FRESH DOUGH\n매일 만드는 도우","HANDMADE\n정성을 담은 한 판","NEW HARBOR\n새로운 시작"}[i],new Vector3(x,2.15f,17.71f),.065f,0,new Color(.2f,.3f,.24f));
            }
            Box("부스 벽 목재 하부",new Vector3(-11.8f,.62f,5.1f),new Vector3(.12f,1.15f,9.5f),wood);
            for(int i=0;i<2;i++)
            {
                Box("부스 벽 액자",new Vector3(-11.68f,2.25f,3+i*4),new Vector3(.08f,1.3f,2.3f),brass,false);
                Box("부스 그림 패널",new Vector3(-11.62f,2.25f,3+i*4),new Vector3(.04f,1.15f,2.15f),green,false);
                DistrictArt.Sign(i==0?"PIZZA\n& FRIENDS":"GOOD FOOD\nGOOD TIMES",new Vector3(-11.58f,2.25f,3+i*4),.075f,270);
            }
            DistrictArt.Prop("SM_Gen_Prop_Clock_01",new Vector3(6,2.7f,11),.55f,0);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            File.WriteAllText("Temp/HarborBlockPolish","validate");
        }
        static void Box(string n,Vector3 p,Vector3 s,Material m,bool solid=true){DistrictArt.Box(n,p,s,m,solid);}
        static void Booth(float x,float z)
        {
            // 쿠션 분할과 목재 받침으로 고정식 식당 부스를 구성한다.
            foreach(float side in new[]{-1f,1f})
            {
                float bz=z+side*1.15f;
                Box("부스 목재 받침",new Vector3(x,.28f,bz),new Vector3(3.15f,.48f,.8f),wood);
                Box("부스 등받이",new Vector3(x,.86f,bz+side*.33f),new Vector3(3.15f,1.13f,.19f),wood);
                for(int j=0;j<3;j++)
                {
                    Box("가죽 좌석 쿠션",new Vector3(x+(j-1)*1.02f,.56f,bz),new Vector3(.98f,.17f,.76f),green);
                    Box("가죽 등 쿠션",new Vector3(x+(j-1)*1.02f,1.02f,bz+side*.2f),new Vector3(.98f,.71f,.16f),green);
                }
            }
            Box("부스 테이블 다리",new Vector3(x,.43f,z),new Vector3(.25f,.77f,.5f),dark);
            Box("부스 테이블 상판",new Vector3(x,.86f,z),new Vector3(2.9f,.12f,1.05f),cream);
            foreach(float dx in new[]{-.8f,.8f})DistrictArt.Prop("SM_Gen_Prop_Plate_01",new Vector3(x+dx,.93f,z),.025f);
            DistrictArt.Prop("SM_Gen_Prop_Bottle_01",new Vector3(x,.93f,z),.23f);
            Box("부스 메뉴",new Vector3(x+.25f,1.08f,z),new Vector3(.2f,.3f,.06f),wood,false);
        }
        static void Drinks()
        {
            Box("음료 바 하부장",new Vector3(10.9f,.55f,3.3f),new Vector3(1.4f,1.02f,4.2f),green);
            Box("음료 바 석재 상판",new Vector3(10.85f,1.12f,3.3f),new Vector3(1.55f,.12f,4.3f),cream);
            Box("음료 메뉴 패널",new Vector3(11.8f,2.15f,3.3f),new Vector3(.1f,1.4f,3.8f),wood,false);
            DistrictArt.Sign("DRINKS\nCOFFEE / SODA / WATER",new Vector3(11.72f,2.25f,3.3f),.065f,90);
            Box("커피 머신",new Vector3(10.9f,1.48f,2),new Vector3(.8f,.6f,.75f),dark);
            Box("커피 머신 금속 전면",new Vector3(10.47f,1.47f,2),new Vector3(.05f,.45f,.63f),brass,false);
            for(int i=0;i<3;i++)
            {
                float z=3+i*.6f;
                Box("음료 디스펜서",new Vector3(11,1.53f,z),new Vector3(.65f,.68f,.48f),dark);
                Box("음료 종류 패널",new Vector3(10.66f,1.65f,z),new Vector3(.035f,.32f,.32f),i==0?green:i==1?brass:cream,false);
                Box("음료 추출구",new Vector3(10.55f,1.4f,z),new Vector3(.2f,.07f,.08f),brass,false);
            }
            for(int i=0;i<4;i++)DistrictArt.Prop("SM_Gen_Prop_Mug_01",new Vector3(10.35f,1.19f,2.6f+i*.4f),.14f,90);
            DistrictArt.Prop("SM_Prop_PotPlant_01",new Vector3(10.9f,.04f,5.8f),1.15f);
        }
        static void Display()
        {
            Box("진열대 받침",new Vector3(-6,1.28f,10),new Vector3(3,.12f,.9f),wood,false);
            foreach(float y in new[]{1.35f,1.78f})
            {
                Box("진열 선반",new Vector3(-6,y,10),new Vector3(2.9f,.035f,.85f),cream,false);
                for(int i=0;i<3;i++)DistrictArt.Prop("SM_Gen_Prop_Food_Bread_01",new Vector3(-7+i,y+.025f,10),.18f,i*35);
            }
            foreach(float x in new[]{-7.5f,-4.5f})Box("진열대 프레임",new Vector3(x,1.66f,10),new Vector3(.06f,.8f,.9f),brass,false);
            Box("진열대 유리",new Vector3(-6,1.66f,9.55f),new Vector3(3,.75f,.015f),glass,false);
            Box("진열대 상단",new Vector3(-6,2.07f,10),new Vector3(3.1f,.08f,.95f),wood,false);
            DistrictArt.Sign("FRESH FROM THE OVEN",new Vector3(-6,1.28f,9.53f),.035f);
        }
        static void Staff()
        {
            for(int i=0;i<3;i++)
            {
                float z=14+i*.8f;
                Box("직원 개인 사물함",new Vector3(6.5f,1.12f,z),new Vector3(.65f,2.15f,.75f),green);
                Box("사물함 문",new Vector3(6.84f,1.12f,z),new Vector3(.035f,2.02f,.65f),cream,false);
                Box("사물함 손잡이",new Vector3(6.88f,1.12f,z+.2f),new Vector3(.05f,.17f,.04f),dark,false);
            }
            Box("직원 공지판",new Vector3(7.2f,2.3f,17.78f),new Vector3(1.5f,1,.06f),wood,false);
            DistrictArt.Sign("STAFF ROOM\n오늘도 수고했어요!",new Vector3(7.2f,2.3f,17.73f),.045f);
            Box("직원 휴게 테이블",new Vector3(7.8f,.8f,16.8f),new Vector3(1.25f,.1f,.8f),wood);
            Box("휴게 테이블 받침",new Vector3(7.8f,.42f,16.8f),new Vector3(.2f,.75f,.4f),dark);
            DistrictArt.Prop("SM_Gen_Prop_Mug_01",new Vector3(7.8f,.86f,16.8f),.14f);
        }
        static void Capture()
        {
            var camera=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>(true)).First();
            var p=camera.transform.position;var q=camera.transform.rotation;var target=camera.targetTexture;var active=RenderTexture.active;
            foreach(var shot in new[]{(new Vector3(0,1.8f,.8f),new Vector3(-2,1.6f,10),"hall"),(new Vector3(-4.7f,1.9f,1),new Vector3(-9,1,5),"booths"),(new Vector3(6.6f,1.8f,6),new Vector3(10.7f,1.5f,3.1f),"drinks"),(new Vector3(9.3f,1.8f,12.4f),new Vector3(7.7f,1.5f,16.5f),"staff")})
            {
                var rt=new RenderTexture(1600,900,24);var tex=new Texture2D(1600,900,TextureFormat.RGB24,false);
                try {camera.transform.position=shot.Item1;camera.transform.LookAt(shot.Item2);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1600,900),0,0);tex.Apply();File.WriteAllBytes("Logs/ShopInterior-"+shot.Item3+".png",tex.EncodeToPNG());}
                finally{camera.targetTexture=target;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}
            }
            camera.transform.SetPositionAndRotation(p,q);
        }
    }
}
#endif

