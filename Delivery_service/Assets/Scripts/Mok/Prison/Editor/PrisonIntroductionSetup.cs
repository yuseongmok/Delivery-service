#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace DeliveryService.Mok.Editor
{
    [InitializeOnLoad]
    public static class PrisonIntroductionSetup
    {
        const string Folder="Assets/DeliveryPrisonChapter/Introduction";
        static PrisonIntroductionSetup(){EditorApplication.update+=Poll;}
        static T[] All<T>(Scene scene) where T:Component=>scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();
        static void Poll()
        {
            if(EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/PrisonIntroduction-request"))return;
            string action=File.ReadAllText("Temp/PrisonIntroduction-request").Trim();File.Delete("Temp/PrisonIntroduction-request");
            try{if(action=="test")PrisonIntroductionCheck.Run();else if(action=="polish")Polish();else Build();}
            catch(Exception e){File.WriteAllText("Logs/Prison-introduction-build.txt",e.ToString());Debug.LogException(e);}
        }
        static void Build()
        {
            var scene=SceneManager.GetSceneByPath("Assets/Scenes/PrisonChapter.unity");
            if(!scene.isLoaded)scene=EditorSceneManager.OpenScene("Assets/Scenes/PrisonChapter.unity",OpenSceneMode.Additive);
            EditorSceneManager.SaveScene(scene,"Logs/Prison-before-introduction-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
            var mission=All<PrisonMission>(scene).Single();
            if(mission.introduction)throw new Exception("도입부가 이미 있습니다. 기존 배치를 덮어쓰지 않습니다.");
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            var root=new GameObject("21_감옥 도입 • 머그샷과 정보거래");SceneManager.MoveGameObjectToScene(root,scene);
            var intro=root.AddComponent<PrisonIntroduction>();intro.mission=mission;mission.introduction=intro;
            var text=ScriptableObject.CreateInstance<PrisonIntroductionText>();AssetDatabase.CreateAsset(text,Folder+"/감옥 도입과 정보거래 대사.asset");intro.dialogue=text;
            Transform Anchor(string name,Vector3 pos,Vector3 look){var t=new GameObject(name).transform;t.SetParent(root.transform,false);t.position=pos;t.LookAt(look);return t;}
            Vector3 Ground(Vector3 pos){if(!NavMesh.SamplePosition(pos,out var hit,2,NavMesh.AllAreas))throw new Exception("걸을 수 없는 위치: "+pos);return hit.position;}
            var cell=Ground(new Vector3(-10,0,-25));
            intro.cellSpawn=Anchor("플레이어 같은 방 시작",cell+Vector3.up*.12f,cell+Vector3.right*3);
            intro.cellMingi=Anchor("민기 같은 방",Ground(new Vector3(-8.2f,0,-24)),cell);
            var mingi=All<Transform>(scene).First(t=>t.name=="민기 • 의무실");
            mingi.name="민기 • 같은 방 동료";mingi.SetPositionAndRotation(intro.cellMingi.position,intro.cellMingi.rotation);
            var animator=mingi.GetComponentInChildren<Animator>(true);animator.enabled=true;animator.applyRootMotion=false;
            animator.runtimeAnimatorController=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Scripts/Mok/Art/StoryCharacters/StoryWalk.controller");animator.Rebind();animator.Update(0);
            var follower=mingi.gameObject.AddComponent<PrisonCompanion>();intro.mingi=follower;follower.animator=animator;follower.follow=mission.player.transform;
            follower.agent=mingi.gameObject.AddComponent<NavMeshAgent>();follower.agent.radius=.28f;follower.agent.height=1.8f;follower.agent.stoppingDistance=1.7f;follower.agent.acceleration=14;follower.agent.angularSpeed=360;
            StoryActorFootsteps.Attach(mingi,animator,null);
            var yard=All<Transform>(scene).First(t=>t.name=="YardMeeting");
            var actor=Object.Instantiate(mission.recipients[0].gameObject,root.transform);actor.name="최형욱 • 운동장 정보통";
            var combat=actor.GetComponent<CitizenCombat>();if(combat)Object.DestroyImmediate(combat);
            foreach(var script in actor.GetComponentsInChildren<MonoBehaviour>(true))Object.DestroyImmediate(script);
            foreach(var joint in actor.GetComponentsInChildren<Joint>(true))Object.DestroyImmediate(joint);
            foreach(var body in actor.GetComponentsInChildren<Rigidbody>(true))Object.DestroyImmediate(body);
            foreach(var collider in actor.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(collider);
            var npcAgent=actor.GetComponent<NavMeshAgent>();if(npcAgent)Object.DestroyImmediate(npcAgent);
            actor.transform.SetPositionAndRotation(Ground(yard.position),Quaternion.Euler(0,180,0));
            var capsule=actor.AddComponent<CapsuleCollider>();capsule.center=Vector3.up*.85f;capsule.height=1.7f;capsule.radius=.3f;
            var npcAnimator=actor.GetComponentInChildren<Animator>();npcAnimator.enabled=true;npcAnimator.Rebind();npcAnimator.Update(0);
            intro.informant=actor.transform;
            // 책과 전용 이름표로 일반 수감자와 구분한다.
            var boardMat=Material("수감 접수판",new Color(.09f,.13f,.16f));var wallMat=Material("촬영실 벽",new Color(.54f,.59f,.57f));var ink=Material("신장 눈금",new Color(.16f,.2f,.21f));
            var book=Box("공무원 수험서",intro.informant.position+new Vector3(.48f,.75f,.1f),new Vector3(.3f,.05f,.4f),boardMat,root.transform);
            Box("수험서 받침",book.position+Vector3.down*.4f,new Vector3(.42f,.75f,.46f),wallMat,root.transform);
            intro.cellCamera=Anchor("감방 대화 구도",cell+new Vector3(-.5f,1.55f,1.2f),mingi.position+Vector3.up*1.3f);
            intro.portraitCamera=Anchor("최형욱 인물 소개",actor.transform.position+actor.transform.forward*2+Vector3.up*1.55f,actor.transform.position+Vector3.up*1.45f);
            var cameraObject=new GameObject("감옥 도입 컷씬 카메라",typeof(Camera),typeof(AudioListener));cameraObject.transform.SetParent(root.transform,false);intro.filmCamera=cameraObject.GetComponent<Camera>();intro.filmCamera.enabled=false;intro.filmCamera.nearClipPlane=.05f;cameraObject.GetComponent<AudioListener>().enabled=false;
            Vector3 booth=new Vector3(80,0,-40);
            Box("머그샷 촬영 바닥",booth+Vector3.down*.15f,new Vector3(7,.3f,7),wallMat,root.transform);
            Box("머그샷 배경 벽",booth+new Vector3(0,1.6f,1),new Vector3(6,3.2f,.2f),wallMat,root.transform);
            for(int i=0;i<12;i++)Box("신장 눈금 "+i,booth+new Vector3(0,.3f+i*.22f,.88f),new Vector3(5,.015f,.018f),ink,root.transform);
            var avatar=mission.player.GetComponentInChildren<PlayerAvatar>(true);
            var photo=Object.Instantiate(avatar.animator.gameObject,root.transform);photo.name="플레이어 • 머그샷 대역";
            foreach(var script in photo.GetComponentsInChildren<MonoBehaviour>(true))Object.DestroyImmediate(script);
            foreach(var joint in photo.GetComponentsInChildren<Joint>(true))Object.DestroyImmediate(joint);
            foreach(var body in photo.GetComponentsInChildren<Rigidbody>(true))Object.DestroyImmediate(body);
            foreach(var collider in photo.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(collider);
            photo.transform.SetPositionAndRotation(booth,Quaternion.Euler(0,180,0));
            foreach(var r in photo.GetComponentsInChildren<Renderer>(true)){r.enabled=true;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;}
            var photoAnimator=photo.GetComponent<Animator>();photoAnimator.enabled=true;photoAnimator.Rebind();photoAnimator.Update(0);
            intro.mugshotActor=photo.transform;intro.mugshotCamera=Anchor("머그샷 구도",booth+new Vector3(0,1.45f,-3),booth+Vector3.up*1.1f);
            var lightObject=new GameObject("촬영 조명",typeof(Light));lightObject.transform.SetParent(root.transform,false);lightObject.transform.position=booth+new Vector3(-1,2.6f,-2);var light=lightObject.GetComponent<Light>();light.type=LightType.Point;light.range=8;light.intensity=3;
            photo.SetActive(false);
            foreach(var point in mission.recipients)point.enabled=false;mission.cooking.enabled=false;mission.infirmary.enabled=false;mission.cell.enabled=false;
            foreach(var distraction in All<PrisonDistraction>(scene))distraction.enabled=false;
            EditorUtility.SetDirty(mission);EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
            File.WriteAllText("Logs/Prison-introduction-build.txt","PASS saved prison introduction only\nCell="+cell+" Yard="+actor.transform.position+"\nDialogue="+AssetDatabase.GetAssetPath(text));
        }
        static void Polish(){
            var scene=SceneManager.GetSceneByPath("Assets/Scenes/PrisonChapter.unity");if(!scene.isLoaded)scene=EditorSceneManager.OpenScene("Assets/Scenes/PrisonChapter.unity",OpenSceneMode.Additive);
            var intro=All<PrisonIntroduction>(scene).Single();
            var npc=intro.informant.GetComponent<NPCControl>();if(npc)Object.DestroyImmediate(npc);
            var agent=intro.informant.GetComponent<NavMeshAgent>();if(agent)Object.DestroyImmediate(agent);
            intro.mugshotCamera.position=intro.mugshotActor.position+new Vector3(0,1.5f,-2.2f);intro.mugshotCamera.LookAt(intro.mugshotActor.position+Vector3.up*1.25f);
            All<Transform>(scene).First(t=>t.name=="머그샷 배경 벽").localScale=new Vector3(9,3.2f,.2f);
            EditorUtility.SetDirty(intro);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        static Material Material(string name,Color color){var material=new Material(Shader.Find("Universal Render Pipeline/Lit")){color=color};AssetDatabase.CreateAsset(material,Folder+"/"+name+".mat");return material;}
        static Transform Box(string name,Vector3 position,Vector3 scale,Material material,Transform parent){var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name;obj.transform.SetParent(parent,false);obj.transform.position=position;obj.transform.localScale=scale;obj.GetComponent<Renderer>().sharedMaterial=material;return obj.transform;}
    }
}
#endif

