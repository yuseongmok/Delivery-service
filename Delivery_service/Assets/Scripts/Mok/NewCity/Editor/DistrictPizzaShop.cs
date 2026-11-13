#if UNITY_EDITOR
using UnityEngine;
namespace DeliveryService.Mok.NewCity.Editor
{
    internal static class DistrictPizzaShop
    {
        static Material cream,brick,teal,wood,metal,orange,light,tile;
        public static void Build()
        {
            var root=DistrictArt.Parent;DistrictArt.Parent=DistrictArt.Group("02 큰 피자 매장 • 홀 / 주방 / 창고");
            cream=DistrictArt.Mat("Warm plaster",new Color(.82f,.76f,.63f));brick=DistrictArt.Pattern("Terracotta brick",new Color(.51f,.24f,.14f),new Color(.67f,.56f,.42f),true);
            teal=DistrictArt.Mat("Deep green joinery",new Color(.08f,.22f,.21f));wood=DistrictArt.Pattern("Dining stone",new Color(.65f,.56f,.41f),new Color(.48f,.42f,.33f),false);
            metal=DistrictArt.Mat("Brushed kitchen steel",new Color(.42f,.49f,.48f));orange=DistrictArt.Mat("Pizza orange",new Color(.87f,.30f,.08f));light=DistrictArt.Mat("Warm luminous",new Color(1,.83f,.49f));light.EnableKeyword("_EMISSION");light.SetColor("_EmissionColor",new Color(1,.74f,.37f)*2);
            tile=DistrictArt.Pattern("Kitchen ceramic",new Color(.70f,.75f,.68f),new Color(.40f,.48f,.44f),false);
            B("홀 바닥",new Vector3(0,-.04f,5.5f),new Vector3(24,.16f,11),wood);
            B("주방 바닥",new Vector3(0,-.04f,14.5f),new Vector3(24,.16f,7),tile);
            // 출입구 3개를 비워 두고 외벽을 구획한다.
            B("후면 벽",new Vector3(-2,2.2f,18),new Vector3(20,4.4f,.3f),brick);
            B("후문 옆 벽",new Vector3(11.2f,2.2f,18),new Vector3(1.6f,4.4f,.3f),brick);
            B("후문 상인방",new Vector3(9.2f,3.6f,18),new Vector3(2.4f,1.6f,.3f),brick);
            B("좌측 주방 벽",new Vector3(-12,2.2f,14),new Vector3(.3f,4.4f,8),brick);
            B("우측 주방 벽",new Vector3(12,2.2f,14),new Vector3(.3f,4.4f,8),brick);
            B("지붕과 천장",new Vector3(0,4.55f,9),new Vector3(24.8f,.30f,18.8f),cream);
            B("지붕 테두리",new Vector3(0,4.85f,-.3f),new Vector3(25,.45f,.5f),teal);
            B("전면 간판",new Vector3(0,3.65f,-.18f),new Vector3(24.8f,1.3f,.5f),teal);
            DistrictArt.Sign("DELIVERY SERVICE",new Vector3(0,3.85f,-.46f),.40f);
            DistrictArt.Sign("P I Z Z A   &   K I T C H E N",new Vector3(0,3.30f,-.47f),.15f);
            // 큰 창과 녹색 프레임, 열려 있는 정문.
            foreach(float x in new[]{-10,-6,-2,2,6,10}){
                if(Mathf.Abs(x)==2)continue;Window(new Vector3(x,1.65f,0),new Vector3(3.6f,2.7f,.10f));
                B("전면 벽돌 기단",new Vector3(x,.25f,0),new Vector3(3.8f,.5f,.3f),brick);
            }
            foreach(float x in new[]{-12,-8,-4,-1.5f,1.5f,4,8,12})B("전면 기둥",new Vector3(x,1.55f,0),new Vector3(.24f,3.1f,.36f),teal);
            // 정문 옆 비어 있는 패널은 메뉴 창으로 구성한다.
            foreach(float x in new[]{-2.75f,2.75f}){Window(new Vector3(x,1.6f,0),new Vector3(2.2f,2.8f,.1f));}
            for(int i=0;i<6;i++){float z=1+i*1.5f;B("우측 창 기둥",new Vector3(12,1.6f,z),new Vector3(.28f,3.2f,.15f),teal);B("좌측 창 기둥",new Vector3(-12,1.6f,z),new Vector3(.28f,3.2f,.15f),teal);}
            Window(new Vector3(-12,1.7f,5),new Vector3(.10f,2.7f,9.8f));
            Window(new Vector3(12,1.7f,3),new Vector3(.10f,2.7f,5.8f));
            Window(new Vector3(12,1.7f,9),new Vector3(.10f,2.7f,1.8f));
            foreach(float x in new[]{-12,12})B("측면 벽돌 기단",new Vector3(x,.23f,5),new Vector3(.3f,.46f,10),brick);
            // 오른쪽 측면 문 아래는 통행할 수 있도록 기단을 분할한다.
            var side=DistrictArt.Parent.Find("측면 벽돌 기단");
            foreach(var tr in DistrictArt.Parent.GetComponentsInChildren<Transform>())if(tr.name=="측면 벽돌 기단"&&tr.position.x>0){Object.DestroyImmediate(tr.gameObject);break;}
            B("측면 기단 앞",new Vector3(12,.23f,3),new Vector3(.3f,.46f,6),brick);B("측면 기단 뒤",new Vector3(12,.23f,9),new Vector3(.3f,.46f,2),brick);
            for(int i=0;i<12;i++)B("차양 줄무늬",new Vector3(-11+i*2,3.05f,-1),new Vector3(2,.12f,1.8f),i%2==0?orange:cream,false);
            foreach(float x in new[]{-9,-3,3,9}){B("간판 조명",new Vector3(x,4.35f,-.8f),new Vector3(.7f,.08f,.25f),light,false);}
            DistrictArt.Prop("SM_Prop_LargeSign_Pizza_01",new Vector3(8,4.7f,5),.8f,180);
            DistrictArt.Prop("SM_Prop_Roof_Aircon_01",new Vector3(-6,4.72f,14),1.2f);
            FurnishHall();Kitchen();
            DistrictArt.Parent=root;
        }
        static void B(string n,Vector3 p,Vector3 s,Material m,bool solid=true)=>DistrictArt.Box(n,p,s,m,solid);
        static void Window(Vector3 p,Vector3 s)
        {
            var glass=DistrictArt.Mat("Clear shop glazing",new Color(.48f,.73f,.74f,.13f));glass.SetFloat("_Surface",1);glass.SetFloat("_ZWrite",0);glass.SetInt("_SrcBlend",5);glass.SetInt("_DstBlend",10);glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");glass.renderQueue=3000;
            B("유리",p,s,glass);B("창 상부 프레임",p+Vector3.up*s.y*.5f,new Vector3(s.x+.12f,.12f,s.z+.12f),teal);B("창 하부 프레임",p-Vector3.up*s.y*.5f,new Vector3(s.x+.12f,.12f,s.z+.12f),teal);
        }
        static void FurnishHall()
        {
            foreach(float x in new[]{-8,-4,4,8})foreach(float z in new[]{3,7}){
                DistrictArt.Prop("SM_Prop_ShopInterior_Table_01",new Vector3(x,.04f,z),.78f,0,true);
                foreach(int side in new[]{-1,1})DistrictArt.Prop("SM_Prop_ShopInterior_Chair_01",new Vector3(x+side*1.1f,.04f,z),.92f,side>0?90:270,true);
                DistrictArt.Prop("SM_Gen_Prop_Plate_01",new Vector3(x,.84f,z),.035f);
                B("테이블 메뉴",new Vector3(x+.3f,1,z),new Vector3(.16f,.28f,.07f),teal,false);
            }
            // 중앙 주동선은 입구부터 주문대로 곧게 비워 둔다.
            B("주문 카운터",new Vector3(-2, .58f,10),new Vector3(14,1.08f,.95f),teal);B("석재 카운터 상판",new Vector3(-2,1.15f,10),new Vector3(14.3f,.12f,1.15f),cream);
            for(float x=-8.5f;x<5;x+=.45f)B("카운터 세로 목재",new Vector3(x,.56f,9.5f),new Vector3(.055f,.96f,.035f),wood,false);
            DistrictArt.Prop("SM_Prop_Computer_Screen_01",new Vector3(3,1.21f,10),.44f,180);
            DistrictArt.Prop("SM_Prop_Computer_Keyboard_01",new Vector3(3,1.22f,9.7f),.04f,180);
            DistrictArt.Sign("ORDER / PICK UP",new Vector3(1, .65f,9.44f),.13f);
            foreach(float x in new[]{-6,-1,4}){
                B("메뉴 보드",new Vector3(x,3.15f,11),new Vector3(3.6f,1.1f,.12f),teal,false);
                DistrictArt.Sign(x==-6?"CLASSIC\nCHEESE  /  PEPPERONI":x==-1?"FRESH FROM THE OVEN\nHANDMADE PIZZA":"TAKE AWAY\nPIZZA  &  DRINKS",new Vector3(x,3.15f,10.91f),.075f);
            }
            foreach(float x in new[]{-6,0,6})foreach(float z in new[]{3,8,14}){
                B("천장 펜던트 줄",new Vector3(x,4,z),new Vector3(.035f,.8f,.035f),teal,false);
                B("펜던트 조명 갓",new Vector3(x,3.58f,z),new Vector3(.85f,.18f,.55f),teal,false);
                B("펜던트 발광면",new Vector3(x,3.48f,z),new Vector3(.72f,.03f,.43f),light,false);
            }
            foreach(float x in new[]{-6,6})foreach(float z in new[]{4,13})DistrictArt.Lamp(new Vector3(x,3.3f,z),10,2.0f,new Color(1,.86f,.66f));
            foreach(var p in new[]{new Vector3(-10.8f,.04f,1),new Vector3(10.8f,.04f,1),new Vector3(10.8f,.04f,9.5f)})DistrictArt.Prop("SM_Prop_PotPlant_01",p,1.4f);
        }
        static void Kitchen()
        {
            B("후면 작업대",new Vector3(-3,.52f,16.8f),new Vector3(15,1,1.3f),metal);B("작업대 상판",new Vector3(-3,1.06f,16.8f),new Vector3(15.2f,.12f,1.5f),cream);
            for(float x=-9;x<4;x+=2){B("수납장 문",new Vector3(x,.58f,16.12f),new Vector3(1.85f,.75f,.05f),metal,false);B("수납장 손잡이",new Vector3(x,.85f,16.05f),new Vector3(.55f,.04f,.05f),teal,false);}
            B("중앙 조리대",new Vector3(-2,.55f,13.3f),new Vector3(7,1.02f,1.4f),metal);B("조리대 상판",new Vector3(-2,1.11f,13.3f),new Vector3(7.15f,.10f,1.55f),cream);
            B("반죽 도마",new Vector3(-3,1.19f,13.3f),new Vector3(1.6f,.05f,1),wood,false);
            foreach(float x in new[]{-9,-7,-5}){B("벽 선반",new Vector3(x,2.3f,17.6f),new Vector3(1.8f,.1f,.55f),wood,false);for(int k=0;k<3;k++)DistrictArt.Prop("SM_Gen_Prop_Plate_01",new Vector3(x+(k-1)*.45f,2.36f,17.5f),.05f);}
            foreach(float x in new[]{-9.8f,-7.2f}){
                B("피자 오븐 몸체",new Vector3(x,.85f,13),new Vector3(2.2f,1.7f,1.6f),brick);B("오븐 입구",new Vector3(x,.95f,12.17f),new Vector3(1.55f,.65f,.08f),teal,false);
                B("오븐 내부 불빛",new Vector3(x,.73f,12.11f),new Vector3(1.25f,.08f,.08f),orange,false);B("오븐 선반",new Vector3(x,.55f,11.9f),new Vector3(2.4f,.12f,.6f),metal);
                B("배기 후드",new Vector3(x,2.8f,13),new Vector3(2.4f,.45f,1.8f),metal,false);B("배기 덕트",new Vector3(x,3.7f,13),new Vector3(.65f,1.4f,.65f),metal,false);
            }
            // 조리 도구와 식자재는 장식용이며 본편 제작 시스템 연결은 별도로 한다.
            DistrictArt.Prop("SM_Gen_Prop_Pot_01",new Vector3(-.5f,1.17f,13.3f),.35f);
            DistrictArt.Prop("SM_Gen_Prop_Food_Bread_01",new Vector3(-3,1.23f,13.3f),.18f);
            for(int n=0;n<4;n++)DistrictArt.Prop("SM_Gen_Prop_Bottle_01",new Vector3(-6+n*.35f,1.13f,16.8f),.34f);
            DistrictArt.Prop("SM_Gen_Prop_Pot_02",new Vector3(-8,1.13f,16.8f),.30f);
            B("냉장고",new Vector3(3.8f,1.25f,17.2f),new Vector3(1.2f,2.4f,1.2f),metal);
            B("냉장고 손잡이",new Vector3(3.35f,1.3f,16.55f),new Vector3(.06f,.8f,.08f),teal,false);
            B("싱크볼",new Vector3(0,1.13f,16.8f),new Vector3(1.4f,.02f,.85f),teal,false);
            B("수도꼭지",new Vector3(0,1.4f,17.2f),new Vector3(.06f,.55f,.06f),metal,false);
            B("수도꼭지 끝",new Vector3(0,1.65f,17),new Vector3(.06f,.06f,.4f),metal,false);
            // 오른쪽 창고 및 직원 구역, 서비스 통로는 2m 이상 유지한다.
            B("창고 파티션",new Vector3(6,2.1f,15.6f),new Vector3(.16f,4.2f,4.8f),cream);
            foreach(float z in new[]{14,16.5f})DistrictArt.Prop("SM_Prop_ShopInterior_Shelf_01",new Vector3(10.8f,.04f,z),1.9f,90,true);
            DistrictArt.Prop("SM_Prop_Desk_01",new Vector3(8,.04f,16.5f),.78f,180,true);
            DistrictArt.Prop("SM_Prop_Computer_01",new Vector3(8,.82f,16.5f),.45f,180);
            B("포장대",new Vector3(9,.55f,10.5f),new Vector3(3,1.1f,1.1f),teal);B("포장대 상판",new Vector3(9,1.15f,10.5f),new Vector3(3.1f,.1f,1.2f),cream);
            for(int i=0;i<5;i++)B("접은 피자 상자",new Vector3(9,1.23f+i*.07f,10.5f),new Vector3(.7f,.06f,.7f),cream,false);
            DistrictArt.Sign("STAFF ONLY",new Vector3(9.2f,3.3f,17.8f),.14f,180);
        }
    }
}
#endif


