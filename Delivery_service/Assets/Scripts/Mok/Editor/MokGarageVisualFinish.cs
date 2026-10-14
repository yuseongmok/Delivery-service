#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class MokGarageVisualFinish {
[InitializeOnLoadMethod]static void Init(){if(File.Exists("Temp/Mok-garage-visual-request"))EditorApplication.delayCall+=Run;}
static void Run(){if(EditorApplication.isPlayingOrWillChangePlaymode)return;File.Delete("Temp/Mok-garage-visual-request");var root=GameObject.Find("repair_shop/Mok Repair Garage");var font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/BMJUA_ttf.ttf");var path="Assets/Scripts/Mok/Art/RepairShop/GarageSignText.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat==null){mat=new Material(Shader.Find("Mok/GarageDepthText"));mat.name="GarageSignText";AssetDatabase.CreateAsset(mat,path);}mat.SetTexture("_MainTex",font.material.mainTexture);foreach(var t in root.GetComponentsInChildren<TextMesh>())t.GetComponent<Renderer>().sharedMaterial=mat;
if(root.GetComponent<DeliveryService.Mok.GarageTextAtlas>()==null)root.AddComponent<DeliveryService.Mok.GarageTextAtlas>();
foreach(var l in root.GetComponentsInChildren<Light>())l.intensity=22f;
AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(root.scene);EditorSceneManager.SaveScene(root.scene);DeliveryService.Mok.Editor.RepairGarageCheck.Start();}
}
#endif
