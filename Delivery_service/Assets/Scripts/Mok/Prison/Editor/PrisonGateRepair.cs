#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace DeliveryService.Mok.Editor
{
    [InitializeOnLoad] public static class PrisonGateRepair
    {
        static PrisonGateRepair(){EditorApplication.update+=Run;}
        static void Run(){
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||!File.Exists("Temp/PrisonGateRepair"))return;
            File.Delete("Temp/PrisonGateRepair");
            var m=Object.FindFirstObjectByType<PrisonTransportEscape>();if(!m)return;
            var scene=m.gameObject.scene;if(scene.name!="PrisonChapter")return;
            Directory.CreateDirectory("Logs/SceneBackups");
            EditorSceneManager.SaveScene(scene,"Logs/SceneBackups/PrisonChapter-before-gate-"+System.DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
            int count=0;
            foreach(var leaf in new[]{m.gateLeft,m.gateRight})foreach(var t in leaf.GetComponentsInChildren<Transform>(true)){
                Undo.RecordObject(t.gameObject,"Moving prison gate");GameObjectUtility.SetStaticEditorFlags(t.gameObject,0);count++;
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            File.WriteAllText("Logs/Prison-gate-repair.txt","Saved moving gate parts: "+count);
        }
    }
}
#endif
