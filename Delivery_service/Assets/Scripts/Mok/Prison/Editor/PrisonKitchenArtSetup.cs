#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Object=UnityEngine.Object;

namespace DeliveryService.Mok.Editor
{
    // 모든 소품은 감옥 씬에 저장하고, 실패 연출만 런타임 컴포넌트가 움직인다.
    [InitializeOnLoad]
    public static class PrisonKitchenArtSetup
    {
        const string Folder="Assets/DeliveryPrisonChapter/KitchenArt";
        static Material steel,dark,wood,skin,bone,oil,glint,blue,fabric,gold;
        static Mesh faceted;
        static PrisonKitchenArtSetup(){EditorApplication.update+=Poll;}
        static void Poll(){
            if(EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/PrisonKitchenArt-request"))return;
            var action=File.ReadAllText("Temp/PrisonKitchenArt-request").Trim();File.Delete("Temp/PrisonKitchenArt-request");
            try{if(action=="build")Build();else if(action=="test")PrisonKitchenArtCheck.Run();else if(action=="inspect")Inspect();}
            catch(Exception e){File.WriteAllText("Logs/Prison-kitchen-art.txt",e.ToString());Debug.LogException(e);}
        }
        static void Build(){
            var m=Object.FindFirstObjectByType<PrisonChickenMission>();if(!m)throw new Exception("감옥 씬을 열어 주세요.");
            var scene=m.gameObject.scene;EditorSceneManager.SaveScene(scene,"Logs/Prison-before-kitchen-art-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
            if(m.chefReaction)throw new Exception("이미 주방 연출이 적용되었습니다.");
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            steel=Mat("낡은 스테인리스",new Color(.42f,.49f,.5f),.4f);dark=Mat("도구 손잡이",new Color(.065f,.085f,.09f));wood=Mat("낡은 나무",new Color(.32f,.19f,.10f));
            skin=Mat("생닭 껍질",new Color(.91f,.69f,.57f),.15f);bone=Mat("뼈와 조리복 단추",new Color(.94f,.88f,.71f));oil=Mat("흘린 기름",new Color(.33f,.29f,.08f),.9f);glint=Mat("기름 반사",new Color(.65f,.59f,.25f),.95f);
            blue=Mat("파란 주방장 복장",new Color(.12f,.37f,.68f));fabric=Mat("휴게 침대 천",new Color(.23f,.3f,.28f));gold=Mat("바삭한 치킨",new Color(.8f,.39f,.07f));
            faceted=FacetedSphere();
            var root=new GameObject("23_어질러진 주방 • 로우폴리 소품과 주방장 휴식").transform;root.SetParent(m.transform,false);
            Bird(m.cooking.rawChicken.transform,skin);Bird(m.cooking.cookedChicken.transform,gold);Bird(m.carrying.transform,gold);
            var rawItem=m.ingredients[0].transform;foreach(var t in Children(rawItem).Where(t=>!t.GetComponent<TextMesh>()))Object.DestroyImmediate(t.gameObject);
            var raw=new GameObject("손질 전 생닭").transform;raw.SetParent(rawItem,false);raw.localPosition=Vector3.down*.09f;Bird(raw,skin);
            var visuals=m.cooking.GetComponent<ChickenCookingVisuals>();visuals.raw=skin;visuals.fried=gold;
            // 기존 위험 지점을 읽기 쉬운 기름과 조리도구로 교체한다.
            int hazardIndex=0;
            foreach(var hazard in m.GetComponentsInChildren<ChickenNoiseHazard>(true)){DecorateHazard(hazard,hazardIndex++);}
            Vector3[] positions={new Vector3(28,0,-22.8f),new Vector3(32.8f,0,-22.6f),new Vector3(35.5f,0,-24.7f),new Vector3(37,0,-28),new Vector3(35.4f,0,-30.5f),new Vector3(33.2f,0,-31.1f),new Vector3(24,0,-25),new Vector3(22,0,-29),new Vector3(39.4f,0,-26.5f),new Vector3(25,0,-30.5f)};
            for(int i=0;i<positions.Length;i++){
                var go=new GameObject(i%2==0?"흘린 기름":"어지럽혀진 냄비와 도구");go.transform.SetParent(root,false);go.transform.position=positions[i]+Vector3.up*.025f;
                var hazard=go.AddComponent<ChickenNoiseHazard>();hazard.mission=m;hazard.oil=i%2==0;var col=go.AddComponent<BoxCollider>();col.isTrigger=true;col.center=Vector3.up*.18f;col.size=new Vector3(1.2f,.4f,.8f);DecorateHazard(hazard,hazardIndex++);
            }
            // 벽쪽 보관 상자는 실제 장애물, 바닥의 작은 도구는 소음 위험으로 나눈다.
            foreach(var pos in new[]{new Vector3(22,0,-32.6f),new Vector3(41.5f,0,-27),new Vector3(18.5f,0,-24)}){
                var stack=new GameObject("정리되지 않은 보관 상자").transform;stack.SetParent(root,false);stack.position=pos;
                for(int i=0;i<2;i++){Part(stack,"보관 상자",new Vector3(i*.13f,.3f+i*.56f,0),new Vector3(.95f,.55f,.7f),wood,true);for(int j=-1;j<=1;j+=2)Part(stack,"금속 띠",new Vector3(j*.32f+i*.13f,.3f+i*.56f,-.36f),new Vector3(.055f,.54f,.025f),steel);}
                var obstacle=stack.gameObject.AddComponent<NavMeshObstacle>();obstacle.carving=true;obstacle.center=Vector3.up*.55f;obstacle.size=new Vector3(1.1f,1.1f,.8f);
            }
            // 도마 옆은 계속 사용할 수 있도록 도구를 가장자리에 배치한다.
            ReplaceKnife(m.cooking.knife);
            Bowl(m.cooking.bowl,.22f,false);Bowl(visuals.fryer,.4f,true);
            var utensils=new GameObject("조리대 주변 조리도구").transform;utensils.SetParent(root,false);utensils.position=new Vector3(32,1.03f,-32.6f);
            Utensil(utensils,new Vector3(0,0,0),25);Utensil(utensils,new Vector3(.35f,0,.05f),-30);
            var chef=m.chef;var chefAnimator=chef.GetComponentInChildren<Animator>(true);var reference=m.prison.introduction.informant.GetComponentInChildren<Animator>(true);
            chefAnimator.enabled=false;
            for(int i=0;i<(int)HumanBodyBones.LastBone;i++){var to=chefAnimator.GetBoneTransform((HumanBodyBones)i);var from=reference.GetBoneTransform((HumanBodyBones)i);if(to&&from){to.localPosition=from.localPosition;to.localRotation=from.localRotation;}}
            foreach(var t in chef.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="주방장 앞치마"||t.name=="주방장 모자").ToArray())Object.DestroyImmediate(t.gameObject);
            foreach(var chair in m.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="SM_Gen_Prop_Chair_01"&&Vector3.Distance(t.position,new Vector3(37,0,-32))<1).ToArray())Object.DestroyImmediate(chair.gameObject);
            TintChef(chef,chefAnimator);
            var cot=new GameObject("주방장 휴게 간이침대").transform;cot.SetParent(root,false);cot.position=new Vector3(37,0,-31.9f);
            Part(cot,"매트리스",new Vector3(0,.52f,0),new Vector3(.85f,.18f,2.05f),fabric,true);Part(cot,"베개",new Vector3(0,.67f,-.72f),new Vector3(.65f,.15f,.38f),bone);
            for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)Part(cot,"침대 다리",new Vector3(x*.34f,.24f,z*.8f),new Vector3(.055f,.48f,.055f),steel);
            chef.SetPositionAndRotation(new Vector3(37,.68f,-30.95f),Quaternion.Euler(-90,0,0));
            var reaction=root.gameObject.AddComponent<PrisonChefReaction>();m.chefReaction=reaction;reaction.actor=chef;reaction.head=chefAnimator.GetBoneTransform(HumanBodyBones.Head);
            reaction.standingPoint=Anchor(root,"주방장 기상 위치",new Vector3(36.15f,0,-31.4f),Quaternion.Euler(0,180,0));
            reaction.wideShot=Anchor(root,"잠든 주방장 구도",new Vector3(34.8f,1.6f,-33.5f),Quaternion.identity);reaction.wideShot.LookAt(new Vector3(37,.85f,-31.8f));
            m.chefView.SetPositionAndRotation(reaction.wideShot.position,reaction.wideShot.rotation);
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            Inspect();File.WriteAllText("Logs/Prison-kitchen-art.txt","PASS saved; hazards="+hazardIndex+"; chef sleeping / blue uniform / wake closeup linked");
        }
        static void Inspect(){var m=Object.FindFirstObjectByType<PrisonChickenMission>();m.camera.transform.position=new Vector3(33,2.8f,-21.2f);m.camera.transform.LookAt(new Vector3(30,1,-29));PrisonChickenCheck.Capture(m.camera,"Logs/Kitchen-art-overview.png");m.camera.transform.SetPositionAndRotation(m.chefView.position,m.chefView.rotation);PrisonChickenCheck.Capture(m.camera,"Logs/Kitchen-chef-sleep.png");m.camera.transform.SetPositionAndRotation(m.cookView.position,m.cookView.rotation);PrisonChickenCheck.Capture(m.camera,"Logs/Kitchen-raw-chicken.png");}
        static Transform[] Children(Transform t)=>t.Cast<Transform>().ToArray();
        static Material Mat(string name,Color color,float smooth=.05f){var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){color=color};m.SetFloat("_Smoothness",smooth);AssetDatabase.CreateAsset(m,Folder+"/"+name+".mat");return m;}
        static Transform Anchor(Transform parent,string name,Vector3 pos,Quaternion rotation){var t=new GameObject(name).transform;t.SetParent(parent,false);t.SetPositionAndRotation(pos,rotation);return t;}
        static Transform Part(Transform parent,string name,Vector3 pos,Vector3 scale,Material mat,bool collider=false,bool rounded=false){var g=rounded?new GameObject(name):GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=pos;g.transform.localScale=scale;if(rounded){g.AddComponent<MeshFilter>().sharedMesh=faceted;g.AddComponent<MeshRenderer>().sharedMaterial=mat;}else{g.GetComponent<Renderer>().sharedMaterial=mat;if(!collider)Object.DestroyImmediate(g.GetComponent<Collider>());}return g.transform;}
        static void Bird(Transform parent,Material flesh){
            foreach(var child in Children(parent))Object.DestroyImmediate(child.gameObject);
            Part(parent,"생닭 몸통",new Vector3(0,.045f,0),new Vector3(.34f,.24f,.49f),flesh,false,true);
            Part(parent,"가슴살",new Vector3(0,.09f,.025f),new Vector3(.28f,.2f,.32f),flesh,false,true);
            for(int side=-1;side<=1;side+=2){var leg=new GameObject(side<0?"왼쪽 닭다리":"오른쪽 닭다리").transform;leg.SetParent(parent,false);leg.localPosition=new Vector3(side*.17f,.015f,.13f);leg.localRotation=Quaternion.Euler(0,side*24,0);Part(leg,"두툼한 닭다리",Vector3.zero,new Vector3(.15f,.14f,.24f),flesh,false,true);Part(leg,"뼈 끝",new Vector3(0,0,.15f),new Vector3(.055f,.055f,.10f),bone,false,true);Part(leg,"뼈 관절",new Vector3(0,0,.205f),new Vector3(.085f,.05f,.04f),bone,false,true);var wing=Part(parent,"접힌 날개",new Vector3(side*.18f,.055f,-.09f),new Vector3(.10f,.095f,.23f),flesh,false,true);wing.localRotation=Quaternion.Euler(0,side*-30,side*15);}
        }
        static void ReplaceKnife(Transform knife){foreach(var child in Children(knife))Object.DestroyImmediate(child.gameObject);var filter=knife.GetComponent<MeshFilter>();if(filter)Object.DestroyImmediate(filter);var r=knife.GetComponent<MeshRenderer>();if(r)Object.DestroyImmediate(r);var col=knife.GetComponent<Collider>();if(col)Object.DestroyImmediate(col);knife.localScale=Vector3.one;Part(knife,"넓은 칼날",Vector3.zero,new Vector3(.018f,.12f,.23f),steel);Part(knife,"칼 등",new Vector3(0,.055f,0),new Vector3(.025f,.02f,.23f),dark);Part(knife,"칼 손잡이",new Vector3(0,.025f,-.2f),new Vector3(.055f,.06f,.16f),wood);for(int i=0;i<2;i++)Part(knife,"손잡이 리벳",new Vector3(.03f,.025f,-.16f-i*.07f),Vector3.one*.012f,steel,false,true);}
        static void Bowl(Transform parent,float radius,bool pan){foreach(var t in Children(parent))Object.DestroyImmediate(t.gameObject);Part(parent,"팬 바닥",Vector3.zero,new Vector3(radius*1.9f,.025f,radius*1.9f),steel,false,true);for(int i=0;i<12;i++){float a=i*Mathf.PI/6;var side=Part(parent,"다각형 테두리",new Vector3(Mathf.Cos(a)*radius,.045f,Mathf.Sin(a)*radius),new Vector3(.025f,.1f,radius*.54f),steel);side.localRotation=Quaternion.Euler(0,-a*Mathf.Rad2Deg,0);}if(pan){Part(parent,"팬 손잡이",new Vector3(radius+.19f,.04f,0),new Vector3(.38f,.055f,.08f),dark);Part(parent,"튀김 기름",new Vector3(0,.025f,0),new Vector3(radius*1.85f,.02f,radius*1.85f),oil,false,true);}}
        static void Utensil(Transform parent,Vector3 position,float yaw){var t=new GameObject("뒤집개").transform;t.SetParent(parent,false);t.localPosition=position;t.localRotation=Quaternion.Euler(0,yaw,0);Part(t,"나무 손잡이",new Vector3(0,.03f,-.13f),new Vector3(.045f,.035f,.23f),wood);Part(t,"뒤집개 날",new Vector3(0,.025f,.07f),new Vector3(.14f,.018f,.17f),steel);for(int i=-1;i<=1;i++)Part(t,"홈",new Vector3(i*.035f,.035f,.075f),new Vector3(.012f,.004f,.11f),dark);}
        static void DecorateHazard(ChickenNoiseHazard hazard,int index){foreach(var t in Children(hazard.transform))Object.DestroyImmediate(t.gameObject);var parent=hazard.transform;
            if(hazard.oil){var mesh=new Mesh{name="불규칙 기름 "+index};var vertices=new List<Vector3>{Vector3.zero};for(int i=0;i<16;i++){float a=i*Mathf.PI/8;float r=.8f+Mathf.Sin(i*2.3f+index)*.12f;vertices.Add(new Vector3(Mathf.Cos(a)*r*.8f,0,Mathf.Sin(a)*r*.5f));}var triangles=new List<int>();for(int i=0;i<16;i++){triangles.Add(0);triangles.Add((i+1)%16+1);triangles.Add(i+1);}mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();AssetDatabase.CreateAsset(mesh,Folder+"/기름 얼룩 "+index+".asset");var g=new GameObject("윤기 나는 기름 웅덩이",typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(parent,false);g.GetComponent<MeshFilter>().sharedMesh=mesh;g.GetComponent<Renderer>().sharedMaterial=oil;Part(parent,"기름 빛 반사",new Vector3(-.2f,.004f,.1f),new Vector3(.38f,.004f,.07f),glint,false,true);Part(parent,"작은 기름 방울",new Vector3(.7f,.002f,.08f),new Vector3(.14f,.007f,.1f),oil,false,true);}
            else{var pan=new GameObject("떨어진 프라이팬").transform;pan.SetParent(parent,false);pan.localRotation=Quaternion.Euler(4,index*37,8);Bowl(pan,.24f,true);Utensil(parent,new Vector3(.32f,.03f,.2f),index*43);Part(parent,"떨어진 뚜껑",new Vector3(-.35f,.055f,.12f),new Vector3(.35f,.06f,.35f),steel,false,true);Part(parent,"뚜껑 꼭지",new Vector3(-.35f,.11f,.12f),new Vector3(.08f,.065f,.08f),dark);}
        }
        static Mesh FacetedSphere(){var v=new List<Vector3>();var tri=new List<int>();Vector3 P(int ring,int side){float y=-Mathf.PI/2+ring*Mathf.PI/5;float a=side*Mathf.PI/5;return new Vector3(Mathf.Cos(y)*Mathf.Cos(a),Mathf.Sin(y),Mathf.Cos(y)*Mathf.Sin(a))*.5f;}void Face(Vector3 a,Vector3 b,Vector3 c){int i=v.Count;v.Add(a);v.Add(b);v.Add(c);tri.Add(i);tri.Add(i+1);tri.Add(i+2);}for(int j=0;j<5;j++)for(int i=0;i<10;i++){Face(P(j,i),P(j+1,i),P(j+1,i+1));Face(P(j,i),P(j+1,i+1),P(j,i+1));}var mesh=new Mesh{name="로우폴리 둥근 소품"};mesh.SetVertices(v);mesh.SetTriangles(tri,0);mesh.RecalculateNormals();AssetDatabase.CreateAsset(mesh,Folder+"/로우폴리 둥근 소품.asset");return mesh;}
        static void TintChef(Transform actor,Animator animator){int count=0;foreach(var r in actor.GetComponentsInChildren<SkinnedMeshRenderer>(true)){if(!r.enabled||!r.sharedMesh)continue;var mesh=Object.Instantiate(r.sharedMesh);var weights=mesh.boneWeights;if(weights.Length==0)continue;bool Skin(int vertex){var b=r.bones[weights[vertex].boneIndex0];return new[]{HumanBodyBones.Head,HumanBodyBones.Neck,HumanBodyBones.LeftHand,HumanBodyBones.RightHand,HumanBodyBones.LeftFoot,HumanBodyBones.RightFoot}.Any(part=>{var t=animator.GetBoneTransform(part);return t&&b&&(b==t||b.IsChildOf(t));});}int subs=mesh.subMeshCount;var cloth=new List<int>();var preserved=new List<int>[subs];for(int sub=0;sub<subs;sub++){preserved[sub]=new List<int>();var indices=mesh.GetTriangles(sub);for(int i=0;i<indices.Length;i+=3){int skin=(Skin(indices[i])?1:0)+(Skin(indices[i+1])?1:0)+(Skin(indices[i+2])?1:0);var list=skin>=2?preserved[sub]:cloth;list.AddRange(new[]{indices[i],indices[i+1],indices[i+2]});}}mesh.subMeshCount=subs+1;for(int sub=0;sub<subs;sub++)mesh.SetTriangles(preserved[sub],sub);mesh.SetTriangles(cloth,subs);AssetDatabase.CreateAsset(mesh,Folder+"/파란 주방장 "+count+++".asset");r.sharedMesh=mesh;r.sharedMaterials=r.sharedMaterials.Take(subs).Concat(new[]{blue}).ToArray();}}
    }
}
#endif
