#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
[InitializeOnLoad] public static class MokPizzaDiagnosis {
static MokPizzaDiagnosis(){EditorApplication.delayCall+=Run;}
static string PathOf(Transform t)=>t.parent==null?t.name:PathOf(t.parent)+"/"+t.name;
static void Run(){
var s=new StringBuilder();Physics.SyncTransforms();
foreach(var p in Object.FindObjectsByType<PlayerInteraction>(FindObjectsInactive.Include,FindObjectsSortMode.None)){var so=new SerializedObject(p);s.AppendLine("PLAYER "+PathOf(p.transform)+" layerMask="+so.FindProperty("interactLayer").intValue);}
foreach(var m in Object.FindObjectsByType<PizzaMaker>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
s.AppendLine("MAKER "+PathOf(m.transform)+" scale="+m.transform.lossyScale.ToString("F4"));foreach(var c in m.GetComponentsInChildren<Collider>())s.AppendLine("COLLIDER "+PathOf(c.transform)+" bounds="+c.bounds+" layer="+c.gameObject.layer);}
foreach(var t in Object.FindObjectsByType<Topping>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
var d=(PizzaToppingData)new SerializedObject(t).FindProperty("toppingData").objectReferenceValue;s.AppendLine("TOPPING "+PathOf(t.transform)+" layer="+t.gameObject.layer+" data="+(d==null?"NULL":d.toppingType+" cost="+d.cost));
foreach(var c in t.GetComponentsInChildren<Collider>()){
s.AppendLine(" COL "+PathOf(c.transform)+" enabled="+c.enabled+" bounds="+c.bounds+" layer="+c.gameObject.layer);
var target=c.bounds.center;
foreach(var dir in new[]{Vector3.forward,Vector3.back,Vector3.left,Vector3.right}){
var from=target+dir*1.2f+Vector3.up*.3f;
s.AppendLine(" RAY from "+from.ToString("F3")+" -> "+string.Join(" | ",Physics.RaycastAll(from,(target-from).normalized,2f,65480).OrderBy(h=>h.distance).Select(h=>PathOf(h.transform)+" "+h.distance.ToString("F3"))));}
}}
File.WriteAllText("Temp/Mok-pizza-diagnosis.txt",s.ToString());}
}
#endif

