#if UNITY_EDITOR
using System.IO;using System.Linq;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad]public static class PlayerRigInspect{
static PlayerRigInspect(){EditorApplication.update+=Poll;}
static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/Mok-player-inspect"))return;File.Delete("Temp/Mok-player-inspect");
var root=PrefabUtility.LoadPrefabContents("Assets/Scripts/Min/Prefab/NPC/Male_Adult_NPC_1.prefab");
try{var a=root.GetComponentInChildren<Animator>();var r=root.GetComponentInChildren<SkinnedMeshRenderer>();
File.WriteAllText("Temp/Mok-player-inspect.txt","human="+a.isHuman+" avatar="+AssetDatabase.GetAssetPath(a.avatar)+"\nmesh="+r.sharedMesh.name+" bones="+string.Join(",",r.bones.Select(b=>b.name))+"\n"+string.Join("\n",HumanTrait.MuscleName.Where(n=>n.Contains("Right")||n.Contains("Spine"))));
}finally{PrefabUtility.UnloadPrefabContents(root);}
}}
}
#endif
