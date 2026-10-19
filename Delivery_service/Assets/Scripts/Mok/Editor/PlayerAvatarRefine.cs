#if UNITY_EDITOR
using System.IO;using System.Linq;using UnityEditor;using UnityEngine;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad]public static class PlayerAvatarRefine{
static PlayerAvatarRefine(){EditorApplication.update+=Poll;}
static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/Mok-player-refine"))return;File.Delete("Temp/Mok-player-refine");
const string path="Assets/Scripts/Mok/Art/Player/DeliveryPlayer.prefab";var root=PrefabUtility.LoadPrefabContents(path);
try{var a=root.GetComponentInChildren<Animator>();var idle=AssetDatabase.LoadAllAssetsAtPath("Assets/CityPeople/Animation/Male_Idle.fbx").OfType<AnimationClip>().First(c=>!c.name.StartsWith("__"));idle.SampleAnimation(root,0);
var head=a.GetBoneTransform(HumanBodyBones.Head);var chest=a.GetBoneTransform(HumanBodyBones.Chest);
foreach(var t in root.GetComponentsInChildren<Transform>()){
 if(t.name=="Delivery helmet"){t.position=head.position+root.transform.up*.13f;t.rotation=root.transform.rotation;var scale=t.parent.lossyScale;t.localScale=new Vector3(.23f/scale.x,.18f/scale.y,.23f/scale.z);}
 if(t.name=="Helmet brim"){t.position=head.position+root.transform.up*.11f+root.transform.forward*.13f;t.rotation=root.transform.rotation;}
 if(t.name=="Reflective chest stripe"){t.position=chest.position+root.transform.forward*.10f;t.rotation=root.transform.rotation;}
}
PrefabUtility.SaveAsPrefabAsset(root,path);AssetDatabase.SaveAssets();File.WriteAllText("Temp/Mok-player-refine.txt","PASS: accessories fitted to humanoid bones");
}finally{PrefabUtility.UnloadPrefabContents(root);}
}}
}
#endif
