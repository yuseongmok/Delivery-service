#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;
namespace DeliveryService.Mok.Editor
{
    [InitializeOnLoad] static class PrisonDuelSetup
    {
        const string Folder="Assets/DeliveryPrisonChapter/MedicineMission";
        static PrisonDuelSetup(){EditorApplication.update+=Tick;}
        static void Tick()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||!File.Exists("Temp/PrisonDuelSetup"))return;
            string action=File.ReadAllText("Temp/PrisonDuelSetup").Trim();File.Delete("Temp/PrisonDuelSetup");
            try{if(action=="repair")Repair();else Build();}catch(Exception e){File.WriteAllText("Logs/Prison-duel-setup.txt",e.ToString());}
        }
        static void Build()
        {
            var scene=EditorSceneManager.GetActiveScene();if(scene.name!="PrisonChapter")throw new Exception("Open PrisonChapter");
            if(!EditorSceneManager.SaveScene(scene,"Logs/Prison-before-duel-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new Exception("Backup failed");
            var mission=Object.FindFirstObjectByType<PrisonMedicineMission>();if(mission.duel)throw new Exception("Duel already installed; preserve scene edits");
            Vector3 center=FindArena(mission);var root=new GameObject("김정민 · 운동장 일대일 결투");root.transform.SetParent(mission.transform);
            var duel=root.AddComponent<PrisonMedicineDuel>();duel.mission=mission;
            var config=AssetDatabase.LoadAssetAtPath<PrisonDuelSettings>(Folder+"/김정민 결투 대사와 난이도.asset");if(!config){config=ScriptableObject.CreateInstance<PrisonDuelSettings>();AssetDatabase.CreateAsset(config,Folder+"/김정민 결투 대사와 난이도.asset");}duel.settings=config;
            Transform Anchor(string name,Vector3 p){var t=new GameObject(name).transform;t.SetParent(root.transform);t.position=p;return t;}
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Characters/SM_Gen_Chr_Prisoner_Male_01.prefab");
            var actor=Object.Instantiate(prefab,root.transform);actor.name="김정민 · 파란 완장";actor.SetActive(true);
            actor.transform.SetPositionAndRotation(mission.enemies[0].transform.position,mission.enemies[0].transform.rotation);
            var animator=actor.GetComponentInChildren<Animator>();animator.runtimeAnimatorController=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Scripts/Mok/Art/StoryCharacters/StoryWalk.controller");animator.applyRootMotion=false;animator.Rebind();animator.Update(0);
            if(!animator.isHuman)throw new Exception("Boss requires humanoid avatar");
            var capsule=actor.AddComponent<CapsuleCollider>();capsule.center=Vector3.up*.9f;capsule.height=1.8f;capsule.radius=.32f;
            var band=GameObject.CreatePrimitive(PrimitiveType.Cylinder);band.name="김정민 파란 완장";Object.DestroyImmediate(band.GetComponent<Collider>());var arm=animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);band.transform.SetParent(arm,false);band.transform.localPosition=new Vector3(.13f,0,0);band.transform.localRotation=Quaternion.Euler(0,0,90);band.transform.localScale=new Vector3(.17f,.06f,.17f);band.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/파란 완장.mat");duel.boss=actor.transform;
            duel.center=Anchor("관중 원 중심",center);
            duel.playerSpot=Anchor("일인칭 결투 위치",center+Vector3.back*.85f);duel.playerSpot.rotation=Quaternion.identity;
            duel.bossSpot=Anchor("김정민 결투 위치",center+Vector3.forward*.85f);duel.bossSpot.rotation=Quaternion.Euler(0,180,0);
            duel.wideShot=Anchor("운동장 관중 소개 카메라",center+new Vector3(6,4,-6));duel.wideShot.LookAt(center+Vector3.up*.9f);
            duel.crowd=Anchor("결투를 둘러싼 죄수 관중",center);var population=Object.FindFirstObjectByType<PrisonPopulation>();
            var audience=new Animator[16];
            for(int i=0;i<16;i++){
                var g=Object.Instantiate(population.inmateTemplate,duel.crowd);g.name="결투 관중 "+(i+1);
                StripScripts(g);
                foreach(var joint in g.GetComponentsInChildren<Joint>(true))Object.DestroyImmediate(joint);
                foreach(var body in g.GetComponentsInChildren<Rigidbody>(true))Object.DestroyImmediate(body);
                foreach(var col in g.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(col);
                foreach(var agent in g.GetComponentsInChildren<NavMeshAgent>(true))Object.DestroyImmediate(agent);
                float angle=i*Mathf.PI*2/16;Vector3 pos=center+new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle))*4.5f;
                if(NavMesh.SamplePosition(pos,out var hit,.8f,NavMesh.AllAreas))pos=hit.position;
                g.transform.position=pos;g.transform.rotation=Quaternion.LookRotation(center-pos);g.SetActive(true);
                var a=g.GetComponentInChildren<Animator>();a.runtimeAnimatorController=animator.runtimeAnimatorController;a.enabled=true;a.Rebind();a.Update(0);audience[i]=a;
            }
            var originalHands=Object.FindObjectsByType<PrisonTheftHands>(FindObjectsInactive.Include,FindObjectsSortMode.None).First();
            var hands=Object.Instantiate(originalHands.gameObject,root.transform);hands.name="결투 전용 플레이어 주먹과 죄수복 팔";hands.SetActive(false);
            var fx=root.AddComponent<PrisonDuelPresentation>();fx.duel=duel;fx.bossAnimator=animator;fx.audience=audience;fx.playerHands=hands.GetComponent<PrisonTheftHands>();
            fx.impact=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Scripts/Mok/Prologue/Art/CombatFeedback/Cloth impact.wav");fx.swing=SuppliedAudio.Current?SuppliedAudio.Current.flyby:null;
            duel.presentation=fx;mission.duel=duel;
            mission.dialogue.clue="민기: 작업실에 있는 김정민을 찾아보자. 왼팔의 파란 완장을 보면 알 수 있을 거야.";
            mission.dialogue.won="민기: 해냈네! 김정민이 내놓은 약을 챙기고 방으로 돌아가자.";
            foreach(var enemy in mission.enemies)enemy.gameObject.SetActive(false);
            EditorUtility.SetDirty(mission.dialogue);EditorUtility.SetDirty(mission);EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
            File.WriteAllText("Logs/Prison-duel-setup.txt","PASS saved boss, blue band, 16 spectators, first-person arms; arena="+center+"; city unchanged");
        }
        static void StripScripts(GameObject g){foreach(var combat in g.GetComponentsInChildren<CitizenCombat>(true))Object.DestroyImmediate(combat);foreach(var script in g.GetComponentsInChildren<MonoBehaviour>(true))if(!(script is NPCControl))Object.DestroyImmediate(script);foreach(var npc in g.GetComponentsInChildren<NPCControl>(true))Object.DestroyImmediate(npc);}
        static void Repair(){var scene=EditorSceneManager.GetActiveScene();if(scene.name!="PrisonChapter")return;EditorSceneManager.SaveScene(scene,"Logs/Prison-before-duel-crowd-repair-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);var d=Object.FindFirstObjectByType<PrisonMedicineDuel>();Vector3 center=FindArena(d.mission);Vector3 offset=center-d.center.position;d.center.position=center;d.playerSpot.position+=offset;d.bossSpot.position+=offset;d.wideShot.position+=offset;d.crowd.position+=offset;d.crowd.GetChild(12).gameObject.SetActive(false);DressBoss(d);foreach(Transform g in d.crowd){StripScripts(g.gameObject);foreach(var agent in g.GetComponentsInChildren<NavMeshAgent>(true))Object.DestroyImmediate(agent);}var report="PASS spectator AI removed; saved fixed audience ring\\n";foreach(var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))if(!(r is SkinnedMeshRenderer)&&r.bounds.IntersectRay(new Ray(d.center.position+Vector3.up*1.6f,Vector3.up),out var distance)&&distance<15)report+=r.name+" "+r.bounds+"\\n";var cam=d.mission.camera;cam.transform.position=d.center.position+new Vector3(0,22,-20);cam.transform.LookAt(d.center.position);PrisonChickenCheck.Capture(cam,"Logs/Prison-duel-overview.png");EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);File.WriteAllText("Logs/Prison-duel-crowd-repair.txt",report);}
        static void DressBoss(PrisonMedicineDuel d)
        {
            string path=Folder+"/김정민 죄수복.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.color=new Color(.85f,.25f,.045f);mat.SetFloat("_Smoothness",.1f);AssetDatabase.CreateAsset(mat,path);}
            var a=d.presentation.bossAnimator;int count=0;
            foreach(var r in d.boss.GetComponentsInChildren<SkinnedMeshRenderer>()){
                if(!r.enabled||!r.gameObject.activeInHierarchy||AssetDatabase.GetAssetPath(r.sharedMesh).StartsWith(Folder))continue;
                var mesh=Object.Instantiate(r.sharedMesh);var weights=mesh.boneWeights;var skinBones=new System.Collections.Generic.HashSet<Transform>();
                foreach(var part in new[]{HumanBodyBones.Head,HumanBodyBones.Neck,HumanBodyBones.LeftUpperArm,HumanBodyBones.RightUpperArm,HumanBodyBones.LeftFoot,HumanBodyBones.RightFoot}){var bone=a.GetBoneTransform(part);if(bone)foreach(var b in bone.GetComponentsInChildren<Transform>())skinBones.Add(b);}
                int original=mesh.subMeshCount;var cloth=new System.Collections.Generic.List<int>();
                for(int sub=0;sub<original;sub++){
                    var keep=new System.Collections.Generic.List<int>();var indices=mesh.GetTriangles(sub);
                    for(int i=0;i<indices.Length;i+=3){int exposed=0;for(int v=0;v<3;v++)if(skinBones.Contains(r.bones[weights[indices[i+v]].boneIndex0]))exposed++;var list=exposed>=2?keep:cloth;list.Add(indices[i]);list.Add(indices[i+1]);list.Add(indices[i+2]);}
                    mesh.SetTriangles(keep,sub);
                }
                mesh.subMeshCount=original+1;mesh.SetTriangles(cloth,original);AssetDatabase.CreateAsset(mesh,Folder+"/김정민 죄수복 "+count+++".asset");r.sharedMesh=mesh;r.sharedMaterials=r.sharedMaterials.Take(original).Concat(new[]{mat}).ToArray();
            }
            AssetDatabase.SaveAssets();
        }
        static Vector3 FindArena(PrisonMedicineMission mission)
        {
            var candidates=new[]{new Vector3(0,0,28),new Vector3(10,0,28),new Vector3(-8,0,28)};
            foreach(var p in candidates){
                if(!NavMesh.SamplePosition(p,out var hit,1,NavMesh.AllAreas))continue;var center=hit.position;bool valid=true;
                for(int i=0;i<16;i++){
                    float a=i*Mathf.PI*2/16;var spot=center+new Vector3(Mathf.Sin(a),0,Mathf.Cos(a))*4.5f;
                    if(!NavMesh.SamplePosition(spot,out var edge,.7f,NavMesh.AllAreas)||Mathf.Abs(edge.position.y-center.y)>.4f){valid=false;break;}
                    foreach(var col in Physics.OverlapCapsule(spot+Vector3.up*.45f,spot+Vector3.up*1.6f,.32f,~0,QueryTriggerInteraction.Ignore))if(col.gameObject.isStatic&&col.bounds.max.y>center.y+.6f){valid=false;break;}
                    if(!valid)break;
                }
                if(valid&&!Physics.Raycast(center+Vector3.up*2,Vector3.up,10,~0,QueryTriggerInteraction.Ignore))return center;
            }
            throw new Exception("No clear outdoor ring found; inspect courtyard before placing audience");
        }
    }
}
#endif



