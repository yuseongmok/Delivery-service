#if UNITY_EDITOR
using System.IO;using System.Linq;using System.Collections.Generic;using UnityEditor;using UnityEngine;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad]public static class PoliceArtRefine {
static PoliceArtRefine(){EditorApplication.update+=Poll;}
static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/Mok-police-refine"))return;File.Delete("Temp/Mok-police-refine");const string dir="Assets/Scripts/Mok/Art/Police/";var root=PrefabUtility.LoadPrefabContents("Assets/Scripts/Min/Prefab/NPC/Police.prefab");try{
var renderer=root.GetComponentInChildren<SkinnedMeshRenderer>();var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(dir+"UniformBody.asset");var v=mesh.vertices;var tri=mesh.triangles;var weights=mesh.boneWeights;var lists=Enumerable.Range(0,5).Select(_=>new List<int>()).ToArray();
int Slot(int i){var w=weights[i];int bone=w.boneIndex0;float best=w.weight0;if(w.weight1>best){best=w.weight1;bone=w.boneIndex1;}if(w.weight2>best){best=w.weight2;bone=w.boneIndex2;}if(w.weight3>best)bone=w.boneIndex3;var n=renderer.bones[bone].name;return n.Contains("Head")||n.Contains("Neck")||n.Contains("Hand")||n.Contains("Finger")?2:n.Contains("Foot")||n.Contains("Toe")?3:n.Contains("Leg")||n=="Hips"?1:0;}
for(int i=0;i<tri.Length;i+=3){int a=Slot(tri[i]),b=Slot(tri[i+1]),c=Slot(tri[i+2]);int slot=a==b||a==c?a:b;lists[slot].AddRange(new[]{tri[i],tri[i+1],tri[i+2]});}for(int i=0;i<5;i++)mesh.SetTriangles(lists[i],i);EditorUtility.SetDirty(mesh);
var accessories=root.GetComponentsInChildren<MeshRenderer>().Select(x=>x.transform).ToArray();var positions=accessories.Select(t=>root.transform.InverseTransformPoint(t.position)).ToArray();var rotations=accessories.Select(t=>Quaternion.Inverse(root.transform.rotation)*t.rotation).ToArray();
var idle=AssetDatabase.LoadAllAssetsAtPath("Assets/CityPeople/Animation/Male_Idle.fbx").OfType<AnimationClip>().First(c=>!c.name.StartsWith("__"));var rootPosition=root.transform.position;var rootRotation=root.transform.rotation;idle.SampleAnimation(root,0);root.transform.SetPositionAndRotation(rootPosition,rootRotation);
for(int i=0;i<accessories.Length;i++){accessories[i].position=root.transform.TransformPoint(positions[i]);accessories[i].rotation=root.transform.rotation*rotations[i];}
var navy=AssetDatabase.LoadAssetAtPath<Material>(dir+"Uniform.mat");navy.color=new Color(.10f,.17f,.27f);EditorUtility.SetDirty(navy);
foreach(var t in root.GetComponentsInChildren<Transform>()){var p=root.transform.InverseTransformPoint(t.position);if(t.name=="Chest badge"||t.name=="Nameplate"||t.name=="Tie"){p.z=.185f;t.position=root.transform.TransformPoint(p);}if(t.name=="POLICE lettering"){p.z=-.18f;t.position=root.transform.TransformPoint(p);}}
PrefabUtility.SaveAsPrefabAsset(root,"Assets/Scripts/Min/Prefab/NPC/Police.prefab");AssetDatabase.SaveAssets();File.WriteAllText("Temp/Mok-police-refine.txt","PASS: Uniform collar, face and visible chest equipment refined.");
}finally{PrefabUtility.UnloadPrefabContents(root);}}
}}
#endif
