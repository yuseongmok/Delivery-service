#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace DeliveryService.Mok.Editor {
public static class ShopPcRelocation {
[InitializeOnLoadMethod] static void Schedule(){EditorApplication.delayCall+=()=>{if(File.Exists("Temp/Mok-PC-relocate-request")){File.Delete("Temp/Mok-PC-relocate-request");Relocate();}else Inspect();};}
static string PathOf(Transform t)=>t.parent==null?t.name:PathOf(t.parent)+"/"+t.name;
[MenuItem("Tools/Delivery Service/Mok/Move PC To Former Order Counter")]
public static void Relocate(){
if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play mode first.");
var pc=UnityEngine.Object.FindObjectsByType<ShopPC>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single();
var old=UnityEngine.Object.FindObjectsByType<Counter>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(c=>PathOf(c.transform)=="PizzaStore/Order/PC");
if(old.transform.childCount!=0)throw new Exception("Old PC gained children; inspect before removal.");
var oldMembers=old.GetComponents<Component>().Cast<UnityEngine.Object>().Append(old.gameObject).ToHashSet();
var references=new System.Collections.Generic.List<System.Tuple<MonoBehaviour,string,UnityEngine.Object>>();
foreach(var c in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
if(c==null||c==old)continue;var it=new SerializedObject(c).GetIterator();while(it.NextVisible(true))if(it.propertyType==SerializedPropertyType.ObjectReference&&it.objectReferenceValue!=null){
if(oldMembers.Contains(it.objectReferenceValue))throw new Exception("Old PC still referenced by "+PathOf(c.transform)+"."+it.propertyPath);
references.Add(System.Tuple.Create(c,it.propertyPath,it.objectReferenceValue));}}
Vector3 target=old.transform.position;Quaternion rotation=old.transform.rotation;Vector3 scale=pc.transform.lossyScale;
var manager=pc.GetComponentInChildren<OrderManager>(true);var panel=pc.pcUIPanel;
var scene=pc.gameObject.scene;EditorSceneManager.SaveScene(scene);File.Copy(scene.path,"Temp/Mok-PC-relocation-before.unity",true);
Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Replace old order PC with unified shop PC");
Undo.SetTransformParent(pc.transform,old.transform.parent,"Move unified PC to order counter");Undo.RecordObject(pc.transform,"Set original order PC placement");
pc.transform.SetPositionAndRotation(target,rotation);
Vector3 parentScale=pc.transform.parent.lossyScale;pc.transform.localScale=new Vector3(scale.x/parentScale.x,scale.y/parentScale.y,scale.z/parentScale.z);
PrefabUtility.RecordPrefabInstancePropertyModifications(pc.transform);
Undo.DestroyObjectImmediate(old.gameObject);Undo.CollapseUndoOperations(group);
foreach(var r in references){var property=new SerializedObject(r.Item1).FindProperty(r.Item2);if(property==null||property.objectReferenceValue!=r.Item3)throw new Exception("Reference changed: "+r.Item1+"."+r.Item2);}
if(pc.GetComponentInChildren<OrderManager>(true)!=manager||pc.pcUIPanel!=panel)throw new Exception("PC dependencies changed");
if(Vector3.Distance(pc.transform.position,target)>.0001f||Quaternion.Angle(pc.transform.rotation,rotation)>.001f)throw new Exception("PC placement mismatch");
Physics.SyncTransforms();var origin=pc.transform.TransformPoint(new Vector3(0,.47f,.9f));
if(!Physics.Raycast(origin,-pc.transform.forward,out var hit,pc.interactRange)||hit.collider.gameObject!=pc.gameObject)throw new Exception("PC raycast obstructed at new counter");
EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Selection.activeGameObject=pc.gameObject;SceneView.lastActiveSceneView?.FrameSelected();
File.WriteAllText("Temp/Mok-PC-relocation-result.txt","PASS: old Order PC removed; original ShopPC moved to "+target.ToString("F6")+" rotation "+rotation.eulerAngles.ToString("F6")+"; preserved world scale "+scale.ToString("F6")+"; all "+references.Count+" serialized script references unchanged; original manager and UI retained; 1.5m interaction ray hits relocated PC; scene saved.");
Debug.Log("MOK_PC_RELOCATION_PASS");
}
static void Inspect(){
if(EditorApplication.isPlayingOrWillChangePlaymode)return;
var sb=new StringBuilder();
var targets=UnityEngine.Object.FindObjectsByType<Counter>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(c=>c.gameObject).Concat(UnityEngine.Object.FindObjectsByType<ShopPC>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(c=>c.gameObject)).Distinct().ToArray();
foreach(var go in targets){
sb.AppendLine("TARGET "+PathOf(go.transform)+" "+GlobalObjectId.GetGlobalObjectIdSlow(go));
sb.AppendLine("world "+go.transform.position.ToString("F6")+" rotation "+go.transform.eulerAngles.ToString("F6")+" scale "+go.transform.lossyScale.ToString("F6"));
foreach(var c in go.GetComponentsInChildren<Component>(true)) {if(c==null)continue;sb.AppendLine(PathOf(c.transform)+" : "+c.GetType().Name);if(c is MonoBehaviour){var so=new SerializedObject(c);var it=so.GetIterator();while(it.NextVisible(true))if(it.propertyType==SerializedPropertyType.ObjectReference&&it.objectReferenceValue!=null)sb.AppendLine("  "+it.propertyPath+" => "+it.objectReferenceValue.name+" "+GlobalObjectId.GetGlobalObjectIdSlow(it.objectReferenceValue));}}
var members=go.GetComponentsInChildren<Component>(true).Where(c=>c!=null).Cast<UnityEngine.Object>().Concat(go.GetComponentsInChildren<Transform>(true).Select(t=>(UnityEngine.Object)t.gameObject)).ToHashSet();
foreach(var c in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None)){if(c==null||members.Contains(c))continue;var it=new SerializedObject(c).GetIterator();while(it.NextVisible(true))if(it.propertyType==SerializedPropertyType.ObjectReference&&it.objectReferenceValue!=null&&members.Contains(it.objectReferenceValue))sb.AppendLine("INBOUND "+PathOf(c.transform)+" "+c.GetType().Name+"."+it.propertyPath+" => "+it.objectReferenceValue.name);}
}
File.WriteAllText("Temp/Mok-PC-relocation-inspection.txt",sb.ToString());
}
}}
#endif
