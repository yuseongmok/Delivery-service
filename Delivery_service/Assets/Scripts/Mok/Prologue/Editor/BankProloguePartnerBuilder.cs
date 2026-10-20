#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace DeliveryService.Mok.Editor {
public static partial class BankPrologueBuilder {
    public static void InstallPartner(){
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first.");
        var d=UnityEngine.Object.FindFirstObjectByType<BankPrologue>();
        if(!d)throw new Exception("Open BankPrologue first.");
        var scene=d.gameObject.scene;
        if(!EditorSceneManager.SaveScene(scene,"Temp/Bank-before-partner-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))
            throw new Exception("Scene backup failed.");
        if(!d.accomplice){
            foreach(var partial in UnityEngine.Object.FindObjectsByType<PrologueAccomplice>(FindObjectsSortMode.None))
                if(partial.gameObject.scene==scene)UnityEngine.Object.DestroyImmediate(partial.gameObject);
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Scripts/Min/Prefab/NPC/Male_Adult_NPC_1.prefab");
            var g=UnityEngine.Object.Instantiate(source);g.name="Accomplice - vault cash and escape";
            foreach(var b in g.GetComponentsInChildren<MonoBehaviour>(true).Where(b=>b.GetType().Name=="CitizenCombat"))
                UnityEngine.Object.DestroyImmediate(b);
            Strip(g);g.SetActive(true);g.transform.SetPositionAndRotation(new Vector3(-1.2f,0,6.8f),Quaternion.identity);
            var partner=g.AddComponent<PrologueAccomplice>();partner.director=d;partner.animator=g.GetComponentInChildren<Animator>();
            var agent=g.AddComponent<NavMeshAgent>();agent.radius=.28f;agent.height=1.8f;agent.speed=3;agent.acceleration=18;
            agent.angularSpeed=540;agent.stoppingDistance=.2f;agent.avoidancePriority=15;
            partner.collectionPoints=new Transform[d.cash.Length];
            for(int i=0;i<d.cash.Length;i++){
                var goal=d.cash[i].transform.position;goal.y=0;goal.z-=1.3f;
                if(!NavMesh.SamplePosition(goal,out var hit,2,NavMesh.AllAreas))throw new Exception("No cash approach NavMesh.");
                partner.collectionPoints[i]=Point("Partner cash approach "+i,hit.position,d.transform);
            }
            partner.boardingPoint=Point("Partner passenger approach",d.exitPoint.position+Vector3.left*1.2f,d.transform);
            if(!NavMesh.SamplePosition(partner.boardingPoint.position,out var boardingHit,3,NavMesh.AllAreas))throw new Exception("No boarding NavMesh.");
            partner.boardingPoint.position=boardingHit.position;
            var spine=partner.animator.GetBoneTransform(HumanBodyBones.Chest);
            var bag=Box("Cash duffel bag",Vector3.zero,new Vector3(.48f,.3f,.25f),LoadMat("BankGreen"),spine,false);
            bag.transform.localPosition=new Vector3(0,-.18f,-.22f);bag.transform.localRotation=Quaternion.identity;
            partner.bag=bag.transform;d.accomplice=partner;
        }
        d.accomplice.animator.runtimeAnimatorController=d.officers[0].animator.runtimeAnimatorController;
        d.accomplice.animator.applyRootMotion=false;
        // 도시 입구만 담는 6~8m 높이의 대각선 구도로 저장한다.
        var forward=d.cityCarStart.forward;
        d.cityCarEnd.position=d.cityCarStart.position+forward*50;
        d.cityWideCamera.position=d.cityCarEnd.position-forward*16+d.cityCarStart.right*12+Vector3.up*8;
        d.cityLookTarget.position=d.cityCarEnd.position+forward*6+Vector3.up*1.5f;
        d.cityWideCamera.LookAt(d.cityLookTarget);
        d.story.cityArrivalSeconds=8;d.story.cityPanSeconds=3;
        d.story.vault.text="공범: 금고는 내가 열게. 넌 뒤를 봐줘.";
        d.story.alarm.text="공범: 경보야! 돈은 내가 챙길 테니까 경찰을 막아!";
        d.story.collect="공범: 가방에 담고 있어. 조금만 버텨!";
        d.story.escape="공범: 다 챙겼어! 같이 차로 가자!";
        d.story.boarding.text="공범: 나도 탔어. 출발해!";
        EditorUtility.SetDirty(d.story);EditorUtility.SetDirty(d);
        AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
        File.WriteAllText("Temp/Bank-partner-build.txt","PASS: saved accomplice, cash approach points, joint escape and low city entry shot.");
    }
}
}
#endif
