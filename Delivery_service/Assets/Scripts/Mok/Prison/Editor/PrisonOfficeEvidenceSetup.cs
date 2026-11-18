#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DeliveryService.Mok.Editor
{
    [InitializeOnLoad]
    public static class PrisonOfficeEvidenceSetup
    {
        static PrisonOfficeEvidenceSetup(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            if(EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/PrisonOfficeEvidenceSetup"))return;
            File.Delete("Temp/PrisonOfficeEvidenceSetup");
            try{Build();}catch(Exception e){File.WriteAllText("Logs/Prison-office-evidence-setup.txt",e.ToString());Debug.LogException(e);}
        }
        static void Build()
        {
            var scene=EditorSceneManager.GetActiveScene();
            if(scene.name!="PrisonChapter")throw new Exception("감옥 씬에서만 실행하세요.");
            if(!EditorSceneManager.SaveScene(scene,"Logs/Prison-before-evidence-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new Exception("Backup failed");
            var office=Object.FindFirstObjectByType<PrisonOfficeSearch>();
            var evidence=office.GetComponent<PrisonOfficeEvidence>();
            if(evidence){
                var w=evidence.wardenStand;
                evidence.cameraStand.position=w.position+w.forward*2.1f-w.right*1.2f+Vector3.up*1.6f;
                evidence.cameraStand.LookAt(w.position+Vector3.up*1.4f);
                EditorUtility.SetDirty(evidence.dialogue);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                File.WriteAllText("Logs/Prison-office-evidence-setup.txt","PASS camera angle polished and dialogue defaults saved");return;
            }
            evidence=office.gameObject.AddComponent<PrisonOfficeEvidence>();office.evidence=evidence;evidence.office=office;
            const string folder="Assets/DeliveryPrisonChapter/OfficeSearch/";
            evidence.dialogue=ScriptableObject.CreateInstance<PrisonOfficeEvidenceText>();
            AssetDatabase.CreateAsset(evidence.dialogue,folder+"비리 증거와 협박 대사.asset");
            var root=new GameObject("비리 증거와 호송 인계 계획").transform;root.SetParent(office.transform,false);
            Transform Anchor(string name,Vector3 position,Quaternion rotation){var t=new GameObject(name).transform;t.SetParent(root);t.SetPositionAndRotation(position,rotation);return t;}
            Transform Prop(string name,string asset,Vector3 at,float size,int index)
            {
                var t=Anchor(name,at,Quaternion.identity);
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(asset);
                if(!prefab)throw new Exception("Missing prop "+asset);
                var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);g.transform.SetParent(t,false);
                var renderers=g.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
                g.transform.localScale*=size/Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z);
                bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
                g.transform.position+=new Vector3(at.x-bounds.center.x,at.y-bounds.min.y,at.z-bounds.center.z);
                var box=t.gameObject.AddComponent<BoxCollider>();box.center=new Vector3(0,.06f,0);box.size=new Vector3(size,.15f,size);
                var point=t.gameObject.AddComponent<PrisonOfficeEvidencePoint>();point.mission=evidence;point.index=index;
                return t;
            }
            var ledger=Prop("비밀 장부 · 조사", "Assets/Synty/PolygonHeist/Prefab/Props/SM_Prop_Folder_01.prefab",office.points[0].transform.position+Vector3.up*.97f,.55f,0);
            var transfer=Prop("오늘 밤 호송 서류 · 조사","Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Papers_01.prefab",office.points[1].transform.position+Vector3.up*.97f,.42f,1);
            var mobile=Prop("박진웅의 메시지 · 조사","Assets/Synty/PolygonCity/Prefabs/Props/SM_Prop_SmartPhone_01.prefab",office.points[1].transform.position+new Vector3(.3f,.97f,0),.22f,2);
            evidence.evidencePoints=new[]{ledger,transfer,mobile};evidence.phone=mobile.GetChild(0);
            var guard=office.previous.warden;
            var point=guard.gameObject.AddComponent<PrisonOfficeEvidencePoint>();point.mission=evidence;point.index=3;
            evidence.wardenStand=Anchor("염윤태 협박 위치",guard.transform.position,guard.transform.rotation);
            evidence.mingiStand=Anchor("민기 증거 제시 위치",guard.transform.position+guard.transform.forward*1.4f+guard.transform.right*.8f,Quaternion.LookRotation(-guard.transform.forward));
            evidence.cameraStand=Anchor("염윤태 협박 카메라",guard.transform.position+guard.transform.forward*2.1f+Vector3.up*1.5f,Quaternion.LookRotation(-guard.transform.forward));
            office.dialogue.start="민기: 간수복과 차량 열쇠를 찾아보자. 책상 주변에 단서가 있을 거야.";
            office.dialogue.complete="민기: 승인도 받았고 필요한 건 다 챙겼어. 우리가 먼저 호송차를 타고 나가는 거야.";
            var escape=Object.FindFirstObjectByType<PrisonEscapeMission>();
            escape.dialogue.checkpoint="간수: 출발을 앞당긴다는 연락은 받았습니다. 호송 서류와 오늘의 암구호 확인하겠습니다.";
            escape.dialogue.relief=new[]{"나: 염윤태가 바로 신고하지 않을까?","민기: 신고하려면 우릴 박진웅에게 넘기려던 일부터 설명해야겠지.","민기: 오래 숨기진 못해도, 시간은 벌었어.","나: 그래. 이번엔 멀리 가자. 같이."};
            EditorUtility.SetDirty(office.dialogue);EditorUtility.SetDirty(escape.dialogue);
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            File.WriteAllText("Logs/Prison-office-evidence-setup.txt","PASS saved evidence props, editable dialogue and escape connection\nWarden "+guard.transform.position);
        }
    }
}
#endif
