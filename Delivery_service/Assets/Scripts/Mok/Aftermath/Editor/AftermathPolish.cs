#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace DeliveryService.Mok.Editor
{
    public static partial class AftermathSetup
    {
        public static void Polish()
        {
            try
            {
                var scene=EditorSceneManager.OpenScene(ScenePath);
                EditorSceneManager.SaveScene(scene,"Logs/Aftermath-before-polish.unity",true);
                film=Object.FindFirstObjectByType<AftermathFilm>();root=film.transform;
                font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/BMJUA_ttf.ttf");
                dark=AssetDatabase.LoadAssetAtPath<Material>(Folder+"차콜.mat");timber=AssetDatabase.LoadAssetAtPath<Material>(Folder+"오래된 나무.mat");cream=AssetDatabase.LoadAssetAtPath<Material>(Folder+"서류.mat");
                controller=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Scripts/Mok/Art/StoryCharacters/StoryWalk.controller");
                foreach(var text in root.GetComponentsInChildren<TextMesh>(true))text.characterSize/=20;
                var forest=film.shots[0].set.transform;var trade=film.shots[3].set.transform;
                for(int i=0;i<7;i++)Prop("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Mountain_02.prefab",forest,new Vector3(270+i*11,0,24),19+i%3*4,i*47);
                for(int i=0;i<17;i++)Prop("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Tree_Pine_02.prefab",forest,new Vector3(274+i*3,0,15+i%3),6+i%5,i*29);
                Box("거래실 왼쪽 벽",trade,new Vector3(396,2,0),new Vector3(.2f,4,8),timber);
                Box("거래실 오른쪽 벽",trade,new Vector3(404,2,0),new Vector3(.2f,4,8),timber);
                Box("거래실 천장",trade,new Vector3(400,3.3f,0),new Vector3(8,.2f,8),dark);
                var oldDealer=trade.GetComponentsInChildren<AftermathActor>(true).First(a=>a.name=="신분 거래인");
                var newDealer=Prop("Assets/Synty/PolygonHeist/Prefab/Characters/SM_Chr_Male_SuitVest_01.prefab",trade,oldDealer.transform.position,1.8f,180);
                ReplaceActor(oldDealer,newDealer);
                // The dealer remains anonymous: frame hands and documents below the shoulders.
                film.shots[3].cameraFrom.position=new Vector3(401.2f,1.5f,-1.5f);film.shots[3].cameraTo.position=new Vector3(400.8f,1.25f,-1.2f);film.shots[3].lookAt.position=new Vector3(400,.86f,0);film.shots[3].fieldOfView=39;
                var portraitBackdrop=Box("수배 사진 촬영 배경",root,new Vector3(602.5f,1.4f,-.5f),new Vector3(10,3,.1f),dark);
                for(int i=0;i<8;i++)Box("신장 눈금",portraitBackdrop,new Vector3(602.5f,.4f+i*.28f,-.43f),new Vector3(10,.012f,.02f),cream);
                // Read existing prison actors and transport without saving or modifying the prison scene.
                var prison=EditorSceneManager.OpenScene("Assets/Scenes/PrisonChapter.unity",OpenSceneMode.Additive);
                var transport=prison.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<PrisonTransportEscape>(true)).Single();
                var intro=prison.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<PrisonIntroduction>(true)).Single();
                var oldVan=film.shots[0].motions[0].subject;
                var van=CloneVisual(transport.vehicle,forest);van.name="실제 탈옥 호송차량";
                foreach(var s in film.shots)foreach(var m in s.motions??Array.Empty<AftermathFilm.Motion>())if(m.subject==oldVan)m.subject=van.transform;
                Object.DestroyImmediate(oldVan.gameObject);
                var actors=forest.GetComponentsInChildren<AftermathActor>(true);
                foreach(var actor in actors){var source=actor.name=="민기"?transport.friendActor.transform:transport.playerActor.transform;ReplaceActor(actor,CloneVisual(source,forest));}
                var shotSet=film.shots[6].set.transform;
                for(int i=0;i<2;i++)
                {
                    var source=i==0?intro.mugshotActor:intro.mingi.animator.transform;
                    var copy=CloneVisual(source,root);copy.SetActive(true);var a=copy.AddComponent<AftermathActor>();a.animator=copy.GetComponentInChildren<Animator>(true);a.animator.runtimeAnimatorController=controller;a.animator.Rebind();a.animator.Update(0);
                    foreach(var rr in copy.GetComponentsInChildren<Renderer>(true))rr.enabled=true;
                    var tex=Portrait(a,i);shotSet.GetComponentsInChildren<RawImage>(true)[i].texture=tex;Object.DestroyImmediate(copy);
                }
                EditorSceneManager.CloseScene(prison,true);SceneManager.SetActiveScene(scene);
                // Bring the bag into reachable range; keep it in both hands when it rises.
                var bag=forest.GetComponentsInChildren<Transform>(true).First(t=>t.name.Contains("DuffleBag_Open"));
                var bagPos=new Vector3(300,.62f,-.75f);bag.position=bagPos;
                var friend=forest.GetComponentsInChildren<AftermathActor>(true).First(a=>a.name=="민기");
                foreach(int n in new[]{1,2})
                {
                    var m=film.shots[n].motions.First(x=>x.actor==friend);m.from.position=new Vector3(300,-.22f,-.12f);m.to.position=n==1?m.from.position:new Vector3(300,0,-.12f);m.seatedFrom=.35f;m.seatedTo=n==1?.35f:0;
                }
                var bagMove=film.shots[2].motions.First(m=>m.subject==bag);bagMove.from.position=bagPos;bagMove.to.position=bagPos+Vector3.up*.3f;
                // Stand clear of the chair before turning toward the entrance.
                var shop=film.shots[6].set.transform;
                var chair=shop.GetComponentsInChildren<Transform>(true).First(t=>t.name=="SM_Prop_ShopInterior_Chair_01");chair.position=new Vector3(0,0,2.62f);chair.rotation=Quaternion.identity;
                var lift=film.shots[9].motions[0];lift.to.position=new Vector3(0,0,3.22f);
                var take=film.shots[10].motions.First(m=>m.actor);take.from.position=take.to.position=new Vector3(0,0,3.22f);
                var depart=film.shots[11].motions.First(m=>m.actor);depart.from.position=new Vector3(0,0,3.22f);
                film.shots[11].chapterCard=false;
                // Show a brief chapter card only after the figure has left the shop.
                foreach(var s in film.shots)s.set.SetActive(false);film.shots[0].set.SetActive(true);film.ApplyShot(0,0);
                AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);Capture();
                File.WriteAllText("Logs/Aftermath-polish.txt","PASS continuity: source van/guard outfits, prison mugshots, enclosed backgrounds, reachable bag, anonymous dealer, clear shop aisle");EditorApplication.Exit(0);
            }
            catch(Exception e){File.WriteAllText("Logs/Aftermath-polish.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
        }
        static GameObject CloneVisual(Transform source,Transform parent)
        {
            var copy=Object.Instantiate(source.gameObject,parent);copy.SetActive(true);
            foreach(var b in copy.GetComponentsInChildren<MonoBehaviour>(true))Object.DestroyImmediate(b);
            foreach(var j in copy.GetComponentsInChildren<Joint>(true))Object.DestroyImmediate(j);
            foreach(var rb in copy.GetComponentsInChildren<Rigidbody>(true))Object.DestroyImmediate(rb);
            foreach(var c in copy.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(c);
            foreach(var a in copy.GetComponentsInChildren<UnityEngine.AI.NavMeshAgent>(true))Object.DestroyImmediate(a);
            foreach(var r in copy.GetComponentsInChildren<Renderer>(true))r.enabled=true;
            return copy;
        }
        static void ReplaceActor(AftermathActor old,GameObject replacement)
        {
            replacement.name=old.name;replacement.transform.SetPositionAndRotation(old.transform.position,old.transform.rotation);
            var a=replacement.AddComponent<AftermathActor>();a.animator=replacement.GetComponentInChildren<Animator>(true);a.animator.enabled=true;a.animator.runtimeAnimatorController=controller;a.animator.applyRootMotion=false;a.animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            a.animator.Rebind();a.animator.Update(0);
            foreach(var s in film.shots)foreach(var m in s.motions??Array.Empty<AftermathFilm.Motion>())if(m.actor==old){m.actor=a;m.subject=a.transform;}
            Object.DestroyImmediate(old.gameObject);
        }
    }
}
#endif
