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
    [InitializeOnLoad]
    public static class PrisonMedicineSetup
    {
        static PrisonMedicineSetup(){EditorApplication.update+=Poll;}
        static void Poll(){const string file="Temp/PrisonMedicine-request";if(EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(file))return;var action=File.ReadAllText(file).Trim();File.Delete(file);try{if(action=="inspect")Inspect();else if(action=="build")Build();else if(action=="test")PrisonMedicineCheck.Run();}catch(Exception e){File.WriteAllText("Logs/Prison-medicine-setup.txt",e.ToString());Debug.LogException(e);}}
        static void Inspect(){
            var text=new System.Text.StringBuilder();
            foreach(var t in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))if(t.text.Contains("작업"))text.AppendLine(t.text+" "+t.transform.position+" forward="+t.transform.forward);
            var intro=Object.FindFirstObjectByType<PrisonIntroduction>();
            foreach(var c in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))if(c.name.Contains("Workshop")||(c.bounds.center.x>30&&c.bounds.center.x<49&&c.bounds.center.z>19&&c.bounds.center.z<40))text.AppendLine("COL "+c.name+" "+c.bounds);
            foreach(var r in intro.mission.player.GetComponentsInChildren<SkinnedMeshRenderer>(true))text.AppendLine("PLAYER "+r.name+" "+AssetDatabase.GetAssetPath(r.sharedMesh));
            foreach(var r in intro.mugshotActor.GetComponentsInChildren<SkinnedMeshRenderer>(true))text.AppendLine("PHOTO "+r.name+" "+AssetDatabase.GetAssetPath(r.sharedMesh));
            File.WriteAllText("Logs/Prison-medicine-layout.txt",text.ToString());
        }
        static void Build(){
            var intro=Object.FindFirstObjectByType<PrisonIntroduction>();var scene=intro.gameObject.scene;
            if(scene.name!="PrisonChapter")throw new Exception("감옥 씬에서만 실행합니다.");
            EditorSceneManager.SaveScene(scene,"Logs/Prison-before-medicine-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
            if(Object.FindFirstObjectByType<PrisonMedicineMission>())throw new Exception("이미 강탈 미션이 있습니다.");
            var photo=intro.mugshotActor.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            int dressed=0;foreach(var r in intro.mission.player.GetComponentsInChildren<SkinnedMeshRenderer>(true)){
                var source=photo.FirstOrDefault(p=>p.name==r.name);if(!source)continue;
                r.sharedMesh=source.sharedMesh;r.sharedMaterials=source.sharedMaterials;dressed++;
            }
            if(dressed<2)throw new Exception("플레이어 복장 연결을 확인하세요.");
            const string folder="Assets/DeliveryPrisonChapter/MedicineMission";Directory.CreateDirectory(folder);AssetDatabase.Refresh();
            var root=new GameObject("24_강탈 • 작업실에서 수면제 확보");
            var m=root.AddComponent<PrisonMedicineMission>();m.prison=intro.mission;m.chicken=Object.FindFirstObjectByType<PrisonChickenMission>();
            m.dialogue=ScriptableObject.CreateInstance<PrisonMedicineText>();AssetDatabase.CreateAsset(m.dialogue,folder+"/강탈 미션 대사.asset");
            Transform Anchor(string name,Vector3 pos){var t=new GameObject(name).transform;t.SetParent(root.transform,false);t.position=pos;return t;}
            m.workshop=Anchor("작업실 · 파란 완장 단서",new Vector3(32,0,29));m.checkpoint=Anchor("작업실 재시작",new Vector3(28,1.1f,29));m.checkpoint.rotation=Quaternion.Euler(0,90,0);
            m.cell=Anchor("민기와 플레이어의 방",intro.cellSpawn.position);
            var cam=new GameObject("강탈 미션 대화 카메라",typeof(Camera),typeof(AudioListener));cam.transform.SetParent(root.transform);m.camera=cam.GetComponent<Camera>();m.camera.enabled=false;m.camera.fieldOfView=55;m.camera.nearClipPlane=.03f;cam.GetComponent<AudioListener>().enabled=false;
            var blue=new Material(Shader.Find("Universal Render Pipeline/Lit")){color=new Color(.08f,.3f,.8f)};AssetDatabase.CreateAsset(blue,folder+"/파란 완장.mat");
            m.enemies=new MedicineBrawler[3];Vector3[] spots={new Vector3(40,0,32),new Vector3(42,0,33),new Vector3(38,0,33)};
            for(int i=0;i<3;i++){
                var holder=Anchor(i==0?"파란 완장 죄수":"작업실 패거리 "+i,spots[i]);holder.rotation=Quaternion.Euler(0,270,0);
                var enemy=holder.gameObject.AddComponent<MedicineBrawler>();enemy.mission=m;enemy.Hold=true;
                var template=Object.Instantiate(m.chicken.enemies[0].template,root.transform);template.name="강탈 죄수 템플릿 "+i;template.SetActive(false);
                var combat=template.GetComponent<CitizenCombat>();combat.maximumHealth=i==0?90:60;combat.ExternalAttackControl=true;combat.missionEnemy=true;
                if(i==0){var anim=template.GetComponentInChildren<Animator>(true);var arm=anim.GetBoneTransform(HumanBodyBones.LeftUpperArm);var band=GameObject.CreatePrimitive(PrimitiveType.Cube);band.name="왼팔 파란 완장";band.transform.SetPositionAndRotation(arm.position+Vector3.down*.11f,arm.rotation);band.transform.localScale=new Vector3(.15f,.12f,.15f);band.transform.SetParent(arm,true);band.GetComponent<Renderer>().sharedMaterial=blue;Object.DestroyImmediate(band.GetComponent<Collider>());}
                enemy.template=template;var actor=Object.Instantiate(template,holder);actor.transform.localPosition=Vector3.zero;actor.transform.localRotation=Quaternion.identity;actor.SetActive(true);enemy.combat=actor.GetComponent<CitizenCombat>();m.enemies[i]=enemy;holder.gameObject.SetActive(false);
            }
            m.decoys=new Transform[2];for(int i=0;i<2;i++){
                var d=Object.Instantiate(m.chicken.enemies[0].template,root.transform);d.name="작업실 대화 죄수 "+(i+1);d.transform.position=i==0?new Vector3(33,0,24):new Vector3(33,0,39);d.transform.rotation=Quaternion.Euler(0,270,0);d.GetComponent<CitizenCombat>().ExternalAttackControl=true;d.SetActive(false);m.decoys[i]=d.transform;
            }
            var paper=new Material(Shader.Find("Universal Render Pipeline/Lit")){color=new Color(.87f,.85f,.7f)};AssetDatabase.CreateAsset(paper,folder+"/약 봉투.mat");
            var bag=GameObject.CreatePrimitive(PrimitiveType.Cube);bag.name="처방 약 봉투";bag.transform.SetParent(root.transform);bag.transform.position=new Vector3(40,.15f,31);bag.transform.localScale=new Vector3(.3f,.07f,.4f);bag.GetComponent<Renderer>().sharedMaterial=paper;
            var mark=GameObject.CreatePrimitive(PrimitiveType.Cube);mark.name="봉투 표시";mark.transform.SetParent(bag.transform,false);mark.transform.localPosition=new Vector3(0,.51f,0);mark.transform.localScale=new Vector3(.5f,.03f,.35f);mark.GetComponent<Renderer>().sharedMaterial=blue;Object.DestroyImmediate(mark.GetComponent<Collider>());m.medicine=bag.transform;bag.SetActive(false);
            Object.FindFirstObjectByType<PrisonSharedPresentation>().medicine=m;
            EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
            File.WriteAllText("Logs/Prison-medicine-setup.txt","PASS saved; player uniform meshes="+dressed+"; three mission fighters, two witnesses; city unchanged");
        }
    }
}
#endif
