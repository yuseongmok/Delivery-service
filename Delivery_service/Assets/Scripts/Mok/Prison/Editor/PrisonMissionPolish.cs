#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Object=UnityEngine.Object;

namespace DeliveryService.Mok.Editor
{
    public static class PrisonMissionPolish
    {
        public static void Apply()
        {
            var mission=Object.FindFirstObjectByType<PrisonMission>();
            if(!mission)throw new System.Exception("Open PrisonChapter first");
            System.IO.File.WriteAllLines("Logs/Prison-cart-bounds.txt",mission.cart.GetComponentsInChildren<Renderer>(true).Select(r=>r.name+" "+r.bounds));
            EditorSceneManager.SaveScene(mission.gameObject.scene,"../.codex-work/prison-mission/PrisonChapter.before-polish.unity",true);
            foreach(var resident in mission.GetComponentsInChildren<PrisonResident>(true))if(!resident.transform.Find("Prison uniform part 0"))Dress(resident.gameObject);
            if(!mission.GetComponent<PrisonAudio>())mission.gameObject.AddComponent<PrisonAudio>();
            if(!mission.player.GetComponent<PrisonPhoneNotes>())mission.player.gameObject.AddComponent<PrisonPhoneNotes>();
            mission.cell.transform.position=new Vector3(-12.5f,1,-25);
            mission.distraction=mission.GetComponentInChildren<PrisonDistraction>();
            mission.distraction.transform.position=new Vector3(-24,1,19);
            if(!mission.transform.Find("병상 배관 • 간수 유인"))
            {
                var exitPipe=Object.Instantiate(mission.distraction,mission.transform);
                exitPipe.name="병상 배관 • 간수 유인";
                exitPipe.transform.position=new Vector3(-31,1,6);
            }
            mission.stealthCheckpoint.position=new Vector3(-24,1.1f,21);
            mission.stealthCheckpoint.rotation=Quaternion.Euler(0,180,0);
            foreach(var guard in mission.guards)guard.distractionSeconds=20;
            foreach(var a in mission.GetComponentsInChildren<Animator>(true)){a.applyRootMotion=false;a.cullingMode=AnimatorCullingMode.AlwaysAnimate;}
            foreach(var agent in mission.GetComponentsInChildren<NavMeshAgent>(true))agent.areaMask=NavMesh.AllAreas;
            var mattress=mission.transform.Find("병상 매트리스");if(mattress)mattress.SetParent(mission.infirmary.transform,true);
            if(!mission.infirmary.transform.Find("민기 대화 판정")){
                var talk=new GameObject("민기 대화 판정");talk.transform.position=new Vector3(-30,1.1f,4);
                talk.transform.SetParent(mission.infirmary.transform,true);
                var box=talk.AddComponent<BoxCollider>();box.size=new Vector3(.72f,.4f,1.6f);
            }
            mission.player.transform.position=new Vector3(-8.5f,1.1f,-25);
            // 배식 통로와 카트가 겹치지 않도록 카운터 바깥으로 뺀다.
            mission.cart.transform.position=new Vector3(30,.02f,-24.5f);
            var cartBox=mission.cart.GetComponent<BoxCollider>();
            cartBox.center=new Vector3(.02f,.55f,0);cartBox.size=new Vector3(1.65f,1.1f,.96f);
            mission.cart.controller.wheels=mission.cart.GetComponentsInChildren<Transform>().Where(t=>t.name=="Wheel1").ToArray();
            var handle=mission.cart.controller;
            if(!handle.transform.Find("배식 손잡이 상호작용")){
                var original=handle.GetComponent<BoxCollider>();
                var proxy=new GameObject("배식 손잡이 상호작용");proxy.transform.SetParent(handle.transform,false);
                var box=proxy.AddComponent<BoxCollider>();box.center=original.center;box.size=original.size;original.enabled=false;
                proxy.AddComponent<PrisonCartHandle>().cart=mission.cart;
            }
            for(int i=0;i<mission.cart.meals.Length;i++)mission.cart.meals[i].transform.position=mission.cart.transform.position+new Vector3(0,1.1f+i*.12f,0);
            for(int i=0;i<mission.cart.preparedMeals.transform.childCount;i++)mission.cart.preparedMeals.transform.GetChild(i).position=new Vector3(28.8f+i*.52f,1.08f,-31.4f);
            var pot=mission.cooking.transform.Find("밥 조리 냄비");
            if(pot)Object.DestroyImmediate(pot.gameObject);
            var cookware=new GameObject("밥 조리 냄비");cookware.transform.SetParent(mission.cooking.transform,false);
            cookware.transform.position=new Vector3(30,1.05f,-31.4f);
            var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/DeliveryPrisonChapter/Mission01/배식 스테인리스.mat");
            AddPotPart(cookware.transform,"냄비 몸통",PrimitiveType.Cylinder,new Vector3(0,.18f,0),new Vector3(.65f,.18f,.65f),material);
            AddPotPart(cookware.transform,"뚜껑",PrimitiveType.Cylinder,new Vector3(0,.37f,0),new Vector3(.7f,.025f,.7f),material);
            AddPotPart(cookware.transform,"뚜껑 손잡이",PrimitiveType.Cube,new Vector3(0,.43f,0),new Vector3(.16f,.07f,.08f),material);
            foreach(float side in new[]{-1f,1f})AddPotPart(cookware.transform,"양옆 손잡이",PrimitiveType.Cube,new Vector3(side*.39f,.25f,0),new Vector3(.18f,.065f,.17f),material);
            if(!mission.transform.Find("민기 병상 가림막"))
            {
                var screen=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Screen_01.prefab"),mission.transform);
                screen.name="민기 병상 가림막";screen.transform.rotation=Quaternion.Euler(0,90,0);
                var rs=screen.GetComponentsInChildren<Renderer>();var bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);
                screen.transform.localScale*=2.2f/bounds.size.y;
                bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);
                screen.transform.position+=new Vector3(-27.6f,.02f,4)-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
                var barrier=new GameObject("가림막 시야 및 충돌");barrier.transform.SetParent(screen.transform,false);barrier.transform.SetPositionAndRotation(new Vector3(-27.6f,1.12f,4),Quaternion.identity);barrier.transform.localScale=Vector3.one/screen.transform.lossyScale.x;
                barrier.AddComponent<BoxCollider>().size=new Vector3(.16f,2.2f,4);
                var obstacle=barrier.AddComponent<NavMeshObstacle>();obstacle.shape=NavMeshObstacleShape.Box;obstacle.size=new Vector3(.2f,2.2f,4);obstacle.carving=true;
            }
            var privacy=mission.transform.Find("민기 병상 가림막");
            if(privacy){var rs=privacy.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
                var barrier=privacy.Find("가림막 시야 및 충돌");if(barrier)barrier.SetParent(mission.transform,true);
                if(b.size.z<3.9f){var scale=privacy.localScale;scale.x*=4/b.size.z;privacy.localScale=scale;}}
            // 씬 밖 도시 오브젝트를 참조하던 필드만 비운다. 내부 플레이어 참조는 유지한다.
            foreach(var component in mission.player.GetComponentsInChildren<Component>(true))
            {
                if(!component)continue;var so=new SerializedObject(component);var it=so.GetIterator();
                while(it.Next(true))if(it.propertyType==SerializedPropertyType.ObjectReference)
                {
                    var value=it.objectReferenceValue;GameObject obj=value is Component c?c.gameObject:value as GameObject;
                    if(obj&&obj.scene.IsValid()&&obj.scene!=mission.gameObject.scene)it.objectReferenceValue=null;
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorUtility.SetDirty(mission);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(mission.gameObject.scene);
        }
        static void AddPotPart(Transform parent,string name,PrimitiveType type,Vector3 at,Vector3 size,Material material)
        {
            var part=GameObject.CreatePrimitive(type);part.name=name;part.transform.SetParent(parent,false);
            part.transform.localPosition=at;part.transform.localScale=size;part.GetComponent<Renderer>().sharedMaterial=material;
            Object.DestroyImmediate(part.GetComponent<Collider>());
        }
        static void Dress(GameObject actor)
        {
            var target=actor.GetComponent<Animator>();
            target.Rebind();target.Update(0);
            var source=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Characters/SM_Gen_Chr_Prisoner_Male_01.prefab"));
            try
            {
                var sourceAnimator=source.GetComponentInChildren<Animator>();sourceAnimator.runtimeAnimatorController=target.runtimeAnimatorController;sourceAnimator.Rebind();sourceAnimator.Update(0);
                float height=target.GetBoneTransform(HumanBodyBones.Head).position.y-target.GetBoneTransform(HumanBodyBones.LeftFoot).position.y;
                float sourceHeight=sourceAnimator.GetBoneTransform(HumanBodyBones.Head).position.y-sourceAnimator.GetBoneTransform(HumanBodyBones.LeftFoot).position.y;
                source.transform.localScale*=height/sourceHeight;source.transform.SetPositionAndRotation(actor.transform.position,actor.transform.rotation);
                source.transform.position+=target.GetBoneTransform(HumanBodyBones.LeftFoot).position-sourceAnimator.GetBoneTransform(HumanBodyBones.LeftFoot).position;
                var map=new Dictionary<Transform,Transform>();
                for(int i=0;i<(int)HumanBodyBones.LastBone;i++){var from=sourceAnimator.GetBoneTransform((HumanBodyBones)i);var to=target.GetBoneTransform((HumanBodyBones)i);if(from&&to)map[from]=to;}
                Transform Match(Transform bone){for(var t=bone;t;t=t.parent)if(map.TryGetValue(t,out var result))return result;return target.transform;}
                foreach(var r in actor.GetComponentsInChildren<SkinnedMeshRenderer>(true))r.enabled=false;
                int part=0;
                foreach(var r in source.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    if(!r.enabled||!r.sharedMesh||!r.gameObject.activeInHierarchy)continue;
                    string name="Prison uniform part "+part++;var old=actor.transform.Find(name);if(old)Object.DestroyImmediate(old.gameObject);
                    var g=new GameObject(name);g.transform.SetParent(actor.transform,false);
                    var dest=g.AddComponent<SkinnedMeshRenderer>();var mesh=new Mesh();r.BakeMesh(mesh);
                    var matrix=actor.transform.worldToLocalMatrix*r.transform.localToWorldMatrix;
                    mesh.vertices=mesh.vertices.Select(matrix.MultiplyPoint3x4).ToArray();mesh.normals=mesh.normals.Select(n=>matrix.MultiplyVector(n).normalized).ToArray();
                    var bones=r.bones.Select(Match).ToArray();mesh.boneWeights=r.sharedMesh.boneWeights;mesh.bindposes=bones.Select(b=>b.worldToLocalMatrix*actor.transform.localToWorldMatrix).ToArray();mesh.RecalculateBounds();
                    string path="Assets/DeliveryPrisonChapter/Mission01/InmatePart"+part+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    if(saved){EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);mesh=saved;EditorUtility.SetDirty(mesh);}else AssetDatabase.CreateAsset(mesh,path);
                    dest.sharedMesh=mesh;dest.bones=bones;dest.rootBone=target.GetBoneTransform(HumanBodyBones.Hips);dest.sharedMaterials=r.sharedMaterials;dest.localBounds=mesh.bounds;dest.updateWhenOffscreen=true;
                }
            }
            finally{Object.DestroyImmediate(source);}
        }
    }
}
#endif

