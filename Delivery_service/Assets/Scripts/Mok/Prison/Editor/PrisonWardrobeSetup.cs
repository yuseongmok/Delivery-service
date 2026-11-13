#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace DeliveryService.Mok.Editor
{
    [InitializeOnLoad]
    public static class PrisonWardrobeSetup
    {
        const string Folder="Assets/DeliveryPrisonChapter/Wardrobe";
        static Material uniform, shoes;
        static PrisonWardrobeSetup(){EditorApplication.update+=Poll;}
        static void Poll(){
            if(EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/PrisonWardrobe-request"))return;
            string action=File.ReadAllText("Temp/PrisonWardrobe-request").Trim();File.Delete("Temp/PrisonWardrobe-request");
            try{if(action=="test")PrisonWardrobeCheck.Run();else if(action=="inspect")Inspect();else if(action=="clean")Clean();else if(action=="build")Build();}catch(Exception e){File.WriteAllText("Logs/Prison-wardrobe.txt",e.ToString());Debug.LogException(e);}
        }
        static T[] All<T>(Scene scene)where T:Component=>scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();
        static void Clean(){
            var scene=SceneManager.GetSceneByPath("Assets/Scenes/PrisonChapter.unity");var shared=new Dictionary<string,Mesh>();int merged=0;
            foreach(var renderer in All<SkinnedMeshRenderer>(scene)){
                var mesh=renderer.sharedMesh;string path=AssetDatabase.GetAssetPath(mesh);if(!path.StartsWith(Folder+"/"))continue;
                using(var stream=new MemoryStream())using(var writer=new BinaryWriter(stream)){
                    void Number(float value){writer.Write(Mathf.RoundToInt(value*10000));}
                    foreach(var v in mesh.vertices){Number(v.x);Number(v.y);Number(v.z);}foreach(var v in mesh.normals){Number(v.x);Number(v.y);Number(v.z);}foreach(var v in mesh.uv){Number(v.x);Number(v.y);}
                    foreach(var b in mesh.boneWeights){writer.Write(b.boneIndex0);writer.Write(b.boneIndex1);writer.Write(b.boneIndex2);writer.Write(b.boneIndex3);Number(b.weight0);Number(b.weight1);Number(b.weight2);Number(b.weight3);}
                    foreach(var b in mesh.bindposes)for(int i=0;i<16;i++)Number(b[i]);writer.Write(mesh.subMeshCount);for(int i=0;i<mesh.subMeshCount;i++){var triangles=mesh.GetTriangles(i);writer.Write(triangles.Length);foreach(int t in triangles)writer.Write(t);}
                    string key=Convert.ToBase64String(System.Security.Cryptography.SHA256.Create().ComputeHash(stream.ToArray()));
                    if(shared.TryGetValue(key,out var same)){if(same!=mesh){renderer.sharedMesh=same;merged++;}}else shared.Add(key,mesh);
                }
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            var used=new HashSet<string>(AssetDatabase.GetDependencies(scene.path,true));int removed=0;
            foreach(var guid in AssetDatabase.FindAssets("t:Mesh",new[]{Folder})){string path=AssetDatabase.GUIDToAssetPath(guid);if(!used.Contains(path)){AssetDatabase.DeleteAsset(path);removed++;}}
            foreach(var guid in AssetDatabase.FindAssets("PrisonCityInmate t:Mesh",new[]{"Assets/Scripts/Mok/Art/SyntyStory"})){string path=AssetDatabase.GUIDToAssetPath(guid);if(!used.Contains(path))AssetDatabase.DeleteAsset(path);}
            File.WriteAllText("Logs/Prison-wardrobe-clean.txt","Shared meshes="+merged+"; removed intermediate assets="+removed+"; retained mesh variants="+shared.Count);
        }
        static void Inspect(){var scene=SceneManager.GetSceneByPath("Assets/Scenes/PrisonChapter.unity");var actor=All<PrisonResident>(scene).First().transform;var text=new System.Text.StringBuilder();foreach(var c in actor.GetComponentsInChildren<Component>(true)){if(c is Animator a)text.AppendLine("ANIM "+a.name+" enabled="+a.enabled+" root="+a.transform.parent?.name);if(c is SkinnedMeshRenderer r)text.AppendLine("MESH "+r.name+" enabled="+r.enabled+" active="+r.gameObject.activeInHierarchy+" mesh="+r.sharedMesh?.name+" parent="+r.transform.parent?.name);}foreach(var m in actor.GetComponentsInChildren<MonoBehaviour>(true))text.AppendLine("SCRIPT "+m.GetType()+" "+m.name+" enabled="+m.enabled);File.WriteAllText("Logs/Wardrobe-rig.txt",text.ToString());}
        static void Build(){
            var scene=SceneManager.GetSceneByPath("Assets/Scenes/PrisonChapter.unity");if(!scene.isLoaded)scene=EditorSceneManager.OpenScene("Assets/Scenes/PrisonChapter.unity",OpenSceneMode.Additive);
            if(All<SkinnedMeshRenderer>(scene).Any(r=>AssetDatabase.GetAssetPath(r.sharedMesh).StartsWith(Folder+"/")))throw new Exception("이미 죄수복이 적용된 씬입니다. 중복 교체를 중단합니다.");
            EditorSceneManager.SaveScene(scene,"Logs/Prison-before-wardrobe-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            uniform=Material("주황색 죄수복",new Color(.93f,.29f,.055f));shoes=Material("죄수 신발",new Color(.09f,.105f,.11f));
            var intro=All<PrisonIntroduction>(scene).Single();
            var report=new System.Text.StringBuilder();report.AppendLine("Original spawn rotation="+intro.cellSpawn.eulerAngles+" player="+intro.mission.player.transform.eulerAngles);
            intro.cellSpawn.rotation=Quaternion.Euler(0,intro.cellSpawn.eulerAngles.y,0);
            var player=intro.mission.player.transform;player.rotation=Quaternion.Euler(0,player.eulerAngles.y,0);
            var ordinary=new HashSet<Transform>(All<PrisonResident>(scene).Select(r=>r.transform));
            var population=All<PrisonPopulation>(scene).Single();ordinary.Add(population.inmateTemplate.transform);
            foreach(var point in intro.mission.recipients)ordinary.Add(point.transform);
            var chicken=All<PrisonChickenMission>(scene).FirstOrDefault();
            if(chicken)foreach(var e in chicken.enemies){ordinary.Add(e.combat.transform);ordinary.Add(e.template.transform);}
            ordinary.Remove(intro.informant);ordinary.Remove(intro.mingi.transform);
            int index=0;
            foreach(var actor in ordinary){
                if(!actor.GetComponentInChildren<Animator>(true))continue;
                string key="PrisonCityInmate"+index.ToString("00");
                SyntyStoryUpgrade.Skin(actor,"Assets/Scripts/Min/Prefab/NPC/Male_Adult_NPC_"+(index%4+1)+".prefab",key,false,true);
                var primary=actor.GetComponentsInChildren<SkinnedMeshRenderer>(true).First();foreach(var other in actor.GetComponentsInChildren<SkinnedMeshRenderer>(true))other.enabled=other==primary;
                string generated="Assets/Scripts/Mok/Art/SyntyStory/"+key+".asset";
                AssetDatabase.DeleteAsset(Folder+"/"+key+".asset");AssetDatabase.MoveAsset(generated,Folder+"/"+key+".asset");
                Dress(actor,key);report.AppendLine("CITY INMATE "+actor.name);index++;
            }
            Dress(intro.informant,"ChoiHyeonguk");Dress(intro.mingi.transform,"Mingi");
            // 머그샷도 같은 수감 복장으로 보이도록 인물 모델은 유지한다.
            Dress(intro.mugshotActor,"PlayerMugshot");
            EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
            report.AppendLine("Saved ordinary="+index+"; Choi / Mingi identities retained; guards unchanged");File.WriteAllText("Logs/Prison-wardrobe.txt",report.ToString());
            var cam=intro.filmCamera;cam.transform.position=intro.informant.position+intro.informant.forward*3+Vector3.up*1.2f;cam.transform.LookAt(intro.informant.position+Vector3.up*1.1f);PrisonChickenCheck.Capture(cam,"Logs/Prison-Choi-orange.png");
            var first=ordinary.First(t=>t.gameObject.activeInHierarchy);cam.transform.position=first.position+first.forward*3+Vector3.up*1.2f;cam.transform.LookAt(first.position+Vector3.up*1.1f);PrisonChickenCheck.Capture(cam,"Logs/Prison-city-inmate-orange.png");
        }
        static Material Material(string name,Color tint){var m=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/"+name+".mat");if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,Folder+"/"+name+".mat");}m.color=tint;m.SetFloat("_Smoothness",.12f);return m;}
        // 머리·손·신발을 구분해 삼각형 단위로 복장만 새 머티리얼에 배정한다.
        static void Dress(Transform actor,string key){
            var animator=actor.GetComponentInChildren<Animator>(true);if(!animator||!animator.isHuman)return;
            int count=0;
            foreach(var renderer in actor.GetComponentsInChildren<SkinnedMeshRenderer>(true)){
                var original=renderer.sharedMesh;if(!renderer.enabled||!original||original.boneWeights.Length==0)continue;
                var mesh=Object.Instantiate(original);mesh.name=key+" 죄수복";
                var types=new int[renderer.bones.Length];
                for(int i=0;i<types.Length;i++){
                    var bone=renderer.bones[i];types[i]=1;
                    foreach(var part in new[]{HumanBodyBones.Head,HumanBodyBones.Neck,HumanBodyBones.LeftHand,HumanBodyBones.RightHand}){var anchor=animator.GetBoneTransform(part);if(anchor&&bone&&(bone==anchor||bone.IsChildOf(anchor)))types[i]=0;}
                    foreach(var part in new[]{HumanBodyBones.LeftFoot,HumanBodyBones.RightFoot}){var anchor=animator.GetBoneTransform(part);if(anchor&&bone&&(bone==anchor||bone.IsChildOf(anchor)))types[i]=2;}
                }
                var weights=mesh.boneWeights;int Kind(int vertex){var w=weights[vertex];return types[w.boneIndex0];}
                int sourceCount=mesh.subMeshCount;var cloth=new List<int>();var feet=new List<int>();
                var keep=new List<int>[sourceCount];
                for(int sub=0;sub<sourceCount;sub++){keep[sub]=new List<int>();var indices=mesh.GetTriangles(sub);for(int t=0;t<indices.Length;t+=3){int a=Kind(indices[t]),b=Kind(indices[t+1]),c=Kind(indices[t+2]);int kind=a==b||a==c?a:b==c?b:0;var list=kind==1?cloth:kind==2?feet:keep[sub];list.Add(indices[t]);list.Add(indices[t+1]);list.Add(indices[t+2]);}}
                mesh.subMeshCount=sourceCount+2;for(int s=0;s<sourceCount;s++)mesh.SetTriangles(keep[s],s);mesh.SetTriangles(cloth,sourceCount);mesh.SetTriangles(feet,sourceCount+1);
                AssetDatabase.CreateAsset(mesh,Folder+"/"+key+"-clothes-"+count+++".asset");renderer.sharedMesh=mesh;renderer.sharedMaterials=renderer.sharedMaterials.Take(sourceCount).Concat(new[]{uniform,shoes}).ToArray();EditorUtility.SetDirty(renderer);
            }
        }
    }
}
#endif
