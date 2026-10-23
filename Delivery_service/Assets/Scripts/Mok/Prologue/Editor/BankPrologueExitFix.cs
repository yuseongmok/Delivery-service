#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor.SceneManagement;
namespace DeliveryService.Mok.Editor {
public static partial class BankPrologueBuilder {
    public static void FixBlockedExit(){
        var d=UnityEngine.Object.FindFirstObjectByType<BankPrologue>();
        if(!d||UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Open bank in Edit mode first.");
        var scene=d.gameObject.scene;
        if(!EditorSceneManager.SaveScene(scene,"Temp/Bank-before-exit-fix-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new Exception("Backup failed.");
        Physics.SyncTransforms();
        var roots=scene.GetRootGameObjects();
        var obstacles=roots.SelectMany(g=>g.GetComponentsInChildren<BoxCollider>()).Where(c=>c.enabled&&c.bounds.Intersects(new Bounds(new Vector3(14,1,-49),new Vector3(.7f,1.8f,.8f)))).ToArray();
        string report="Doorway blockers before: "+string.Join(", ",obstacles.Select(c=>c.name))+"\n";
        foreach(var wall in roots.SelectMany(g=>g.GetComponentsInChildren<Transform>()).Where(t=>t.name=="Road boundary"&&Mathf.Abs(t.position.z+49)<.2f).ToArray()){
            // 문 앞 5m는 비워두고 도로 경계벽의 나머지 부분은 유지한다.
            var right=UnityEngine.Object.Instantiate(wall.gameObject,wall.parent).transform;
            right.name="Road boundary east of bank entrance";wall.name="Road boundary west of bank entrance";
            wall.position=new Vector3(-7.25f,.5f,-49);wall.localScale=new Vector3(35.5f,1,.35f);
            right.position=new Vector3(145.25f,.5f,-49);right.localScale=new Vector3(259.5f,1,.35f);
        }
        foreach(var pillar in roots.SelectMany(g=>g.GetComponentsInChildren<Transform>()).Where(t=>t.name=="Facade pilaster"&&Mathf.Abs(t.position.x-12)<.1f)){
            var p=pillar.position;p.x=10.6f;pillar.position=p;
        }
        Physics.SyncTransforms();EditorSceneManager.SaveScene(scene);
        File.WriteAllText("Temp/Bank-exit-fix.txt",report+"Saved: 5m opening in road boundary and entrance pillar moved outside doorway.");
    }
}
}
#endif
