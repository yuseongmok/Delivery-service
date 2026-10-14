#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace DeliveryService.Mok.Editor {
public static class ShopPcInspection {
[InitializeOnLoadMethod] static void Schedule(){EditorApplication.delayCall+=Inspect;}
static void Inspect(){
if(EditorApplication.isPlayingOrWillChangePlaymode)return;
var s=new StringBuilder();
foreach(var pc in Object.FindObjectsByType<ShopPC>(FindObjectsInactive.Include,FindObjectsSortMode.None)) {
s.AppendLine("PC "+pc.name+" ID "+GlobalObjectId.GetGlobalObjectIdSlow(pc)+" local "+pc.transform.localPosition+" rot "+pc.transform.localEulerAngles+" scale "+pc.transform.localScale+" worldScale "+pc.transform.lossyScale);
s.AppendLine("Panel "+pc.pcUIPanel+" camera "+pc.playerCamera+" look "+pc.cameraLookScript);
foreach(var c in pc.GetComponentsInChildren<Component>(true))s.AppendLine(c.GetType().Name+" "+c.name);
foreach(var m in pc.GetComponentsInChildren<MeshFilter>(true))s.AppendLine("Mesh "+m.name+" "+AssetDatabase.GetAssetPath(m.sharedMesh)+" bounds "+m.sharedMesh.bounds+" transform "+m.transform.localPosition+" rot "+m.transform.localEulerAngles);
foreach(var c in pc.GetComponentsInChildren<BoxCollider>(true))s.AppendLine("Box "+c.center+" "+c.size);
}
File.WriteAllText("Temp/Mok-PC-inspection.txt",s.ToString());
}
}}
#endif
