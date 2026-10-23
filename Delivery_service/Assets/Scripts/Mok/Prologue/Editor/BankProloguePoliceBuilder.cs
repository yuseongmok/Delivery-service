#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace DeliveryService.Mok.Editor {
public static partial class BankPrologueBuilder {
    public static void BuildPoliceTactics(){
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play before editing.");
        var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(ScenePath);
        if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        var d=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<BankPrologue>()).Single();
        Directory.CreateDirectory("Logs");EditorSceneManager.SaveScene(scene,"Logs/Bank-before-police-tactics-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
        if(d.GetComponent<ProloguePoliceSquad>())throw new Exception("Tactics already installed.");
        var squad=d.gameObject.AddComponent<ProloguePoliceSquad>();
        var root=new GameObject("Police tactical positions / saved").transform;
        var hiding=new List<Transform>();var peeking=new List<Transform>();
        var desks=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<BoxCollider>()).Where(c=>c.name=="Cover desk").ToArray();
        Physics.SyncTransforms();
        foreach(var desk in desks)foreach(int front in new[]{-1,1})foreach(int side in new[]{-1,1}){
            var bounds=desk.bounds;
            var hide=new Vector3(bounds.center.x,0,bounds.center.z+front*(bounds.extents.z+.65f));
            var peek=new Vector3(bounds.center.x+side*(bounds.extents.x+.85f),0,hide.z);
            if(!NavMesh.SamplePosition(hide,out var h,.7f,NavMesh.AllAreas)||!NavMesh.SamplePosition(peek,out var p,.7f,NavMesh.AllAreas))continue;
            var path=new NavMeshPath();if(!NavMesh.CalculatePath(h.position,p.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)continue;
            hiding.Add(Point("Cover "+hiding.Count,h.position,root));peeking.Add(Point("Peek "+peeking.Count,p.position,root));
        }
        squad.hiding=hiding.ToArray();squad.peeking=peeking.ToArray();
        for(int i=0;i<d.officers.Length;i++){
            var o=d.officers[i];var brain=o.gameObject.AddComponent<ProloguePoliceBrain>();o.brain=brain;brain.officer=o;brain.squad=squad;brain.flanker=i%3==1;brain.side=i%2==0?1:-1;
            var agent=o.GetComponent<NavMeshAgent>();agent.avoidancePriority=25+i%40;agent.acceleration=12;agent.angularSpeed=360;
            o.animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
        }
        EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        File.WriteAllText("Logs/Bank-police-tactics-build.txt","PASS: "+hiding.Count+" saved cover/peek pairs; "+d.officers.Length+" bank police assigned cover/flanking roles; road police preserved.");
    }
}
}
#endif
