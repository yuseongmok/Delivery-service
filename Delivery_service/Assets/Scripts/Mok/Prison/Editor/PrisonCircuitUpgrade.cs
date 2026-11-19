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
    public static class PrisonCircuitUpgrade
    {
        public static void Survey()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/PrisonChapter.unity");
            var m=Object.FindFirstObjectByType<PrisonCctvMission>();
            var report="";
            foreach(var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))if(r.bounds.Intersects(new Bounds(new Vector3(-40,2,-29),new Vector3(11,7,13))))report+=r.name+" "+r.bounds+" scale="+r.transform.lossyScale+"\n";
            File.WriteAllText("Logs/Circuit-room-survey.txt",report);
            var c=new GameObject("Review camera").AddComponent<Camera>();c.transform.position=new Vector3(-34,1.7f,-27.5f);c.transform.LookAt(new Vector3(-40,1.7f,-30));c.fieldOfView=75;PrisonChickenCheck.Capture(c,"Logs/Circuit-room-before.png");Object.DestroyImmediate(c.gameObject);
        }
        public static void Apply()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/PrisonChapter.unity");var scene=EditorSceneManager.GetActiveScene();
            if(!EditorSceneManager.SaveScene(scene,"Logs/Prison-before-circuit-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new Exception("Backup failed");
            var m=Object.FindFirstObjectByType<PrisonCctvMission>();var p=m.wirePanel;
            p.circuit=p.GetComponent<PrisonCircuitChallenge>();if(!p.circuit)p.circuit=p.gameObject.AddComponent<PrisonCircuitChallenge>();p.circuit.panel=p;
            m.dialogue.changed="민기: 옷은 됐어. 신참처럼 행동하자. CCTV실은 아래층이야.";
            m.dialogue.opened="민기: 내가 입구를 보고 있을게. 영상 회로만 끊어. 경보는 건드리지 마.";
            m.dialogue.interrogation[7]="염윤태: 아래층 보안실 안 CCTV실이다. 점검하러 왔다고 하면 담당자가 들여보내 줄 거야.";
            p.dialogue.hint="민기: CCTV 단자에서 연결된 선을 따라가. 아래에서 맞는 선을 골라.";
            p.dialogue.controls="선 클릭 · 선택   |   자르기 버튼 · 절단   |   F 돌아가기";
            p.dialogue.retry="민기: CCTV 단자부터 연결을 다시 따라가.";
            const string wallPath="Assets/DeliveryPrisonChapter/OfficeSearch/CCTV room painted wall.mat";
            var wall=AssetDatabase.LoadAssetAtPath<Material>(wallPath);if(!wall){wall=new Material(Shader.Find("Universal Render Pipeline/Lit"));wall.color=new Color(.38f,.43f,.42f);wall.SetFloat("_Smoothness",.12f);AssetDatabase.CreateAsset(wall,wallPath);}
            foreach(var r in m.GetComponentsInChildren<MeshRenderer>(true))if(r.name.Contains("칸막이")||r.name.Contains("쪽 벽")||r.name=="CCTV실 문 위 벽")r.sharedMaterial=wall;
            if(!m.transform.Find("CCTV실 벽 마감")){
                var finish=new GameObject("CCTV실 벽 마감").transform;finish.SetParent(m.transform,false);
                var trim=m.door.GetComponentInChildren<MeshRenderer>().sharedMaterial;
                void Strip(string name,Vector3 at,Vector3 size){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(finish);g.transform.position=at;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=trim;Object.DestroyImmediate(g.GetComponent<Collider>());}
                foreach(float y in new[]{.12f,3.18f}){
                    Strip("북벽 몰딩",new Vector3(-40,y,-24.88f),new Vector3(8,.12f,.07f));
                    Strip("남벽 몰딩",new Vector3(-35.88f,y,-31.5f),new Vector3(.07f,.12f,5));
                    Strip("입구 몰딩",new Vector3(-35.88f,y,-25.5f),new Vector3(.07f,.12f,1));
                }
            }
            p.RefreshDiagram();EditorUtility.SetDirty(m.dialogue);EditorUtility.SetDirty(p.dialogue);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            var review=new GameObject("Review camera").AddComponent<Camera>();review.transform.position=new Vector3(-32,1.7f,-27.5f);review.transform.LookAt(new Vector3(-38,1.7f,-29));review.fieldOfView=75;PrisonChickenCheck.Capture(review,"Logs/Circuit-room-after.png");Object.DestroyImmediate(review.gameObject);
            File.WriteAllText("Logs/Circuit-upgrade.txt","Saved circuit interaction and editable disguise conversation.\n");
        }
    }
}
#endif
