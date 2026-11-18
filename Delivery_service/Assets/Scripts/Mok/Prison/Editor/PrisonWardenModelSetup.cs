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
    [InitializeOnLoad] public static class PrisonWardenModelSetup
    {
        const string Request="Temp/PrisonWardenModel";
        static PrisonWardenModelSetup(){EditorApplication.update+=Poll;EditorApplication.update+=Test;}
        static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(Request))return;var action=File.ReadAllText(Request).Trim();File.Delete(Request);try{if(action=="test"){SessionState.SetBool("WardenModelTest",true);EditorApplication.EnterPlaymode();}else if(action=="source")Source();else if(action=="head")RepairHead();else if(action=="visual")Visual();else Build();}catch(Exception e){File.WriteAllText("Logs/Prison-warden-model.txt",e.ToString());}}
        static int step;static float wait;
        static void Test()
        {
            if(!EditorApplication.isPlaying||!SessionState.GetBool("WardenModelTest",false))return;
            try{
                var m=Object.FindFirstObjectByType<PrisonKeysMission>();if(!m)return;
                if(step==0){step=1;wait=Time.time+3;return;}if(Time.time<wait)return;
                var w=m.warden;var eating=w.GetComponent<PrisonPizzaEating>();
                if(step==1){m.previous.prison.introduction.DeveloperComplete();w.gameObject.SetActive(true);w.receivingMeal=true;
                    var player=m.previous.prison.player;var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.transform.position=w.transform.position+w.transform.forward*2+Vector3.up;cc.enabled=true;
                    eating.StartCoroutine(eating.Play(m.pizzaOnCart.transform));step=2;wait=Time.time+2.3f;return;}
                if(step==2){if(!eating.Playing||!eating.slice.gameObject.activeInHierarchy)throw new Exception("Eating did not start");Shot(m,"eating");step=3;wait=Time.time+5;return;}
                if(step==3){if(!eating.Finished||!w.GetComponent<NPCControl>().IsKnockedDown)throw new Exception("Sleep failed");Shot(m,"sleep");var evidence=Object.FindFirstObjectByType<PrisonOfficeEvidence>();var plan=Object.Instantiate(evidence.office.cctv.dialogue);plan.lineSeconds=.1f;evidence.office.cctv.dialogue=plan;evidence.StartCoroutine((System.Collections.IEnumerator)typeof(PrisonOfficeEvidence).GetMethod("Confront",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(evidence,null));step=4;wait=Time.time+8;return;}
                if(!Object.FindFirstObjectByType<PrisonWardenRestraint>().Bound||!Object.FindFirstObjectByType<PrisonOfficeEvidence>().Authorized||!m.previous.prison.player.CanAct)throw new Exception("Confrontation handoff failed");
                Shot(m,"bound");var skin=w.GetComponentsInChildren<SkinnedMeshRenderer>().First();var mesh=new Mesh();skin.BakeMesh(mesh);var size=mesh.bounds.size;Object.DestroyImmediate(mesh);if(size.magnitude>5||size.y<.3f)throw new Exception("Deformed skin "+size);
                File.WriteAllText("Logs/Prison-warden-play.txt","PASS: original eating animation and slice; sleeping ragdoll; actual confrontation/fade/bind coroutine; authorization and player controls restored; bound-chair pose; skin bounds valid. Assisted Play-mode checks.");SessionState.SetBool("WardenModelTest",false);step=0;EditorApplication.ExitPlaymode();
            }catch(Exception e){File.WriteAllText("Logs/Prison-warden-play.txt",e.ToString());SessionState.SetBool("WardenModelTest",false);step=0;EditorApplication.ExitPlaymode();}
        }
        static void Shot(PrisonKeysMission m,string name){var w=m.warden;var cam=m.camera;var p=cam.transform.position;var q=cam.transform.rotation;cam.transform.position=w.transform.position+w.transform.forward*2.6f+w.transform.right*.6f+Vector3.up*1.4f;if(name=="bound")cam.transform.position=w.transform.position+w.transform.right*1.8f+w.transform.forward*.8f+Vector3.up*1.8f;cam.transform.LookAt(w.transform.position+Vector3.up*.95f);PrisonChickenCheck.Capture(cam,"Logs/Prison-warden-"+name+".png");cam.transform.SetPositionAndRotation(p,q);}
        static void Source(){var m=Object.FindFirstObjectByType<PrisonKeysMission>();var w=m.warden;var g=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonHeist/Prefab/Characters/SM_Chr_Male_SuitVest_01.prefab"));var sr=g.GetComponentInChildren<SkinnedMeshRenderer>();var weights=sr.sharedMesh.boneWeights;File.WriteAllText("Logs/Warden-weights.txt","zero="+weights.Count(x=>x.weight0+x.weight1+x.weight2+x.weight3<.01f)+"\n"+string.Join("\n",sr.bones.Select((b,i)=>i+":"+b.name+" count="+weights.Count(x=>x.boneIndex0==i))));g.transform.SetPositionAndRotation(w.transform.position,w.transform.rotation);var cam=m.camera;var p=cam.transform.position;var q=cam.transform.rotation;var active=w.gameObject.activeSelf;w.gameObject.SetActive(false);cam.transform.position=g.transform.position+g.transform.right*1.8f+g.transform.forward*.8f+Vector3.up*2;cam.transform.LookAt(g.transform.position+Vector3.up*1.2f);PrisonChickenCheck.Capture(cam,"Logs/Warden-original.png");cam.transform.SetPositionAndRotation(p,q);w.gameObject.SetActive(active);Object.DestroyImmediate(g);}
        static void Visual(){
            var m=Object.FindFirstObjectByType<PrisonKeysMission>();var w=m.warden;var scene=w.gameObject.scene;
            if(!EditorSceneManager.SaveScene(scene,"Logs/Prison-before-visual-rig-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new Exception("Backup failed");
            if(w.transform.Find("염윤태 SuitVest 원본 외형"))throw new Exception("Already installed");
            var target=w.GetComponent<Animator>();var active=w.gameObject.activeSelf;w.gameObject.SetActive(true);target.Rebind();target.Update(0);
            foreach(var r in w.GetComponentsInChildren<Renderer>(true))r.enabled=false;
            var g=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonHeist/Prefab/Characters/SM_Chr_Male_SuitVest_01.prefab"),w.transform);g.name="염윤태 SuitVest 원본 외형";g.transform.localPosition=Vector3.zero;g.transform.localRotation=Quaternion.identity;
            var a=g.GetComponent<Animator>();a.runtimeAnimatorController=target.runtimeAnimatorController;a.Rebind();a.Update(0);
            var ratio=(target.GetBoneTransform(HumanBodyBones.Head).position-target.GetBoneTransform(HumanBodyBones.LeftFoot).position).magnitude/(a.GetBoneTransform(HumanBodyBones.Head).position-a.GetBoneTransform(HumanBodyBones.LeftFoot).position).magnitude;g.transform.localScale*=ratio;
            var follow=g.AddComponent<PrisonWardenVisual>();var from=new System.Collections.Generic.List<Transform>();var to=new System.Collections.Generic.List<Transform>();var offsets=new System.Collections.Generic.List<Quaternion>();
            var pairs=new System.Collections.Generic.List<(Transform from,Transform to)>();
            for(int i=0;i<(int)HumanBodyBones.LastBone;i++){var f=target.GetBoneTransform((HumanBodyBones)i);var t=a.GetBoneTransform((HumanBodyBones)i);if(f&&t)pairs.Add((f,t));}
            int Depth(Transform t){int n=0;for(;t;t=t.parent)n++;return n;}
            foreach(var pair in pairs.OrderBy(p=>Depth(p.to))){from.Add(pair.from);to.Add(pair.to);offsets.Add(Quaternion.Inverse(pair.from.rotation)*pair.to.rotation);}
            follow.sourceBones=from.ToArray();follow.visualBones=to.ToArray();follow.rotationOffsets=offsets.ToArray();follow.sourceHips=target.GetBoneTransform(HumanBodyBones.Hips);follow.visualHips=a.GetBoneTransform(HumanBodyBones.Hips);follow.visualHips.position=follow.sourceHips.position;a.enabled=false;
            foreach(var c in g.GetComponentsInChildren<Collider>(true))c.enabled=false;
            w.gameObject.SetActive(active);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);File.WriteAllText("Logs/Warden-visual.txt","PASS original SuitVest mesh / skeleton retained, gameplay pose follower installed");
        }
        static void RepairHead(){
            var m=Object.FindFirstObjectByType<PrisonKeysMission>();var w=m.warden;var scene=w.gameObject.scene;
            if(!EditorSceneManager.SaveScene(scene,"Logs/Prison-before-head-fix-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new Exception("Backup failed");
            var g=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonHeist/Prefab/Characters/SM_Chr_Male_SuitVest_01.prefab"));
            try{var src=g.GetComponentInChildren<SkinnedMeshRenderer>();var sa=g.GetComponentInChildren<Animator>();var dst=w.GetComponentsInChildren<SkinnedMeshRenderer>(true).First();var da=w.GetComponentInChildren<Animator>(true);var sh=sa.GetBoneTransform(HumanBodyBones.Head);var dh=da.GetBoneTransform(HumanBodyBones.Head);
                File.WriteAllText("Logs/Warden-mesh.txt","src="+src.sharedMesh.vertexCount+" sub="+src.sharedMesh.subMeshCount+" mats="+src.sharedMaterials.Length+" dst="+dst.sharedMesh.vertexCount+" sub="+dst.sharedMesh.subMeshCount+" mats="+dst.sharedMaterials.Length+"\n"+string.Join("\n",dst.sharedMaterials.Select(x=>x.name))+"\n"+string.Join("\n",src.sharedMaterials.Select(x=>x.name)));
                var ratio=(dh.position-da.GetBoneTransform(HumanBodyBones.LeftFoot).position).magnitude/(sh.position-sa.GetBoneTransform(HumanBodyBones.LeftFoot).position).magnitude;
                var baked=new Mesh();src.BakeMesh(baked);var original=src.sharedMesh;var sourceVertices=baked.vertices;
                var world=sourceVertices.Select(v=>src.transform.TransformPoint(v)).ToArray();var headIndices=new System.Collections.Generic.List<int>();var bodyIndices=new System.Collections.Generic.List<int>();
                var tris=original.triangles;
                for(int i=0;i<tris.Length;i+=3){bool headPart=true;for(int k=0;k<3;k++)headPart &= world[tris[i+k]].y>sh.position.y-.18f;var list=headPart?headIndices:bodyIndices;list.Add(tris[i]);list.Add(tris[i+1]);list.Add(tris[i+2]);}
                var headMesh=new Mesh{name="Warden original rigid head"};var orientation=w.transform.rotation*Quaternion.Inverse(g.transform.rotation);
                headMesh.vertices=world.Select(v=>dh.InverseTransformPoint(dh.position+orientation*(v-sh.position)*ratio)).ToArray();headMesh.uv=original.uv;headMesh.triangles=headIndices.ToArray();headMesh.RecalculateNormals();headMesh.RecalculateBounds();
                const string path="Assets/Scripts/Mok/Art/SyntyStory/PrisonWardenOriginalHead.asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved){EditorUtility.CopySerialized(headMesh,saved);Object.DestroyImmediate(headMesh);headMesh=saved;}else AssetDatabase.CreateAsset(headMesh,path);
                var headObject=dh.Find("염윤태 원본 머리");if(!headObject){headObject=new GameObject("염윤태 원본 머리",typeof(MeshFilter),typeof(MeshRenderer)).transform;headObject.SetParent(dh,false);}headObject.GetComponent<MeshFilter>().sharedMesh=headMesh;headObject.GetComponent<MeshRenderer>().sharedMaterials=src.sharedMaterials;
                dst.sharedMesh.triangles=bodyIndices.ToArray();EditorUtility.SetDirty(dst.sharedMesh);EditorUtility.SetDirty(headMesh);Object.DestroyImmediate(baked);int count=headIndices.Count/3;
                Object.DestroyImmediate(g);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);File.WriteAllText("Logs/Warden-head-fix.txt","Restored original head / hair vertices="+count);
            }finally{if(g)Object.DestroyImmediate(g);}
        }
        static void Build()
        {
            var m=Object.FindFirstObjectByType<PrisonKeysMission>();var scene=m.gameObject.scene;
            if(scene.name!="PrisonChapter")throw new Exception("Prison only");
            if(!EditorSceneManager.SaveScene(scene,"Logs/Prison-before-warden-model-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new Exception("Backup failed");
            var original=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonHeist/Prefab/Characters/SM_Chr_Male_SuitVest_01.prefab");File.WriteAllLines("Logs/Warden-source.txt",original.GetComponentsInChildren<SkinnedMeshRenderer>(true).Select(r=>r.name+" active="+r.gameObject.activeSelf+" enabled="+r.enabled+" mesh="+r.sharedMesh.name));
            var w=m.warden;var active=w.gameObject.activeSelf;w.gameObject.SetActive(true);
            SyntyStoryUpgrade.Skin(w.transform,"Assets/Synty/PolygonHeist/Prefab/Characters/SM_Chr_Male_SuitVest_01.prefab","PrisonWardenSuitVest",false,true,true);
            var primary=w.GetComponentsInChildren<SkinnedMeshRenderer>(true).First();
            foreach(var skin in w.GetComponentsInChildren<SkinnedMeshRenderer>(true))skin.enabled=skin==primary;
            var report="SuitVest replacement, existing humanoid / ragdoll / mission references retained\n";
            foreach(var r in w.GetComponentsInChildren<MeshRenderer>(true))report+="PROP "+r.name+" active="+r.gameObject.activeSelf+" enabled="+r.enabled+"\n";
            if(primary.bones.Any(b=>!b)||primary.sharedMesh.bindposes.Length!=primary.bones.Length)throw new Exception("Bone mapping failed");
            var camera=m.camera;var pos=camera.transform.position;var rot=camera.transform.rotation;
            camera.transform.position=w.transform.position+w.transform.forward*2.7f+w.transform.right*.6f+Vector3.up*1.4f;camera.transform.LookAt(w.transform.position+Vector3.up*1.05f);
            PrisonChickenCheck.Capture(camera,"Logs/Prison-warden-suitvest.png");camera.transform.SetPositionAndRotation(pos,rot);w.gameObject.SetActive(active);
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);File.WriteAllText("Logs/Prison-warden-model.txt","PASS "+report);
        }
    }
}
#endif
