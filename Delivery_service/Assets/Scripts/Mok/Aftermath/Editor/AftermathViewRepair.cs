#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using Object=UnityEngine.Object;
namespace DeliveryService.Mok.Editor
{
    public static class AftermathViewRepair
    {
        const string Folder="Assets/DeliveryPrisonChapter/Aftermath/";
        public static void Apply()
        {
            try
            {
                var scene=EditorSceneManager.OpenScene("Assets/Scenes/PrisonAftermath.unity");
                EditorSceneManager.SaveScene(scene,"Logs/Aftermath-before-viewrepair-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
                var film=Object.FindFirstObjectByType<AftermathFilm>();
                var view=film.firstPerson;
                view.exchangePlayer=film.shots[4].set.GetComponentsInChildren<AftermathActor>(true).First(a=>a.name=="플레이어");
                int index=0;
                foreach(var actor in new[]{view.forestPlayer,view.hideoutPlayer,view.exchangePlayer,view.shopPlayer})
                    MakeArmsOnly(actor,index++);
                film.shots[4].lookAt.position=new Vector3(400.25f,1.15f,.65f);
                film.shots[5].lookAt.position=new Vector3(399.85f,.85f,-.05f);
                var hideout=film.hideout;
                hideout.standingPosition=film.shots[2].motions.First(m=>m.actor==hideout.mingi).from.position;
                hideout.cabinetDoor.localRotation=Quaternion.identity;
                hideout.handleBodyOffset=hideout.standingPosition-hideout.doorGrip.position;
                film.dialogue.lines[2].seconds=9.5f;
                foreach(var s in film.shots)s.set.SetActive(false);
                film.shots[0].set.SetActive(true);film.ApplyShot(0,0);
                EditorUtility.SetDirty(film.dialogue);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
                File.WriteAllText("Logs/Aftermath-viewrepair.txt","Saved cinematic-only arms meshes for four player rigs; exchange POV and reachable handle arc.\n");
                AftermathReview.Record();
            }
            catch(Exception e){File.WriteAllText("Logs/Aftermath-viewrepair.txt",e.ToString());EditorApplication.Exit(1);}
        }

        static void MakeArmsOnly(AftermathActor actor,int actorIndex)
        {
            if(!actor.animator||!actor.animator.isHuman)throw new Exception("Missing human rig: "+actor.name);
            var left=actor.animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            var right=actor.animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            int rendererIndex=0;
            foreach(var renderer in actor.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var source=renderer.sharedMesh;
                if(!source)continue;
                bool[] armBones=renderer.bones.Select(b=>b&&(b==left||b==right||b.IsChildOf(left)||b.IsChildOf(right))).ToArray();
                var weights=source.boneWeights;
                bool[] keep=new bool[source.vertexCount];
                for(int i=0;i<weights.Length;i++)
                {
                    var w=weights[i];float armWeight=0;
                    if(armBones[w.boneIndex0])armWeight+=w.weight0;
                    if(armBones[w.boneIndex1])armWeight+=w.weight1;
                    if(armBones[w.boneIndex2])armWeight+=w.weight2;
                    if(armBones[w.boneIndex3])armWeight+=w.weight3;
                    keep[i]=armWeight>.45f;
                }
                var mesh=Object.Instantiate(source);mesh.name="Cinematic first person arms";
                for(int sub=0;sub<source.subMeshCount;sub++)
                {
                    var triangles=source.GetTriangles(sub);var selected=new List<int>();
                    for(int i=0;i<triangles.Length;i+=3)
                        if(keep[triangles[i]]&&keep[triangles[i+1]]&&keep[triangles[i+2]])
                        {selected.Add(triangles[i]);selected.Add(triangles[i+1]);selected.Add(triangles[i+2]);}
                    mesh.SetTriangles(selected,sub,false);
                }
                string path=Folder+"FirstPersonArms_"+actorIndex+"_"+rendererIndex+++".asset";
                var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(existing){EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);mesh=existing;}
                else AssetDatabase.CreateAsset(mesh,path);
                renderer.sharedMesh=mesh;renderer.updateWhenOffscreen=true;
            }
            foreach(var renderer in actor.GetComponentsInChildren<MeshRenderer>(true))renderer.enabled=false;
        }
    }
}
#endif
