#if UNITY_EDITOR
using System.IO;
using System.Text;
using System.Linq;
using UnityEditor;
using UnityEngine;
[InitializeOnLoad] public static class MokShopBuildingInspect {
static MokShopBuildingInspect(){EditorApplication.delayCall+=Run;}
static string P(Transform t)=>t.parent==null?t.name:P(t.parent)+"/"+t.name;
static void Run(){var shop=GameObject.Find("PizzaStore");if(shop==null)return;var s=new StringBuilder();
s.AppendLine("ROOT "+shop.transform.position+" rot="+shop.transform.eulerAngles+" scale="+shop.transform.lossyScale);
foreach(Transform t in shop.transform){s.AppendLine("CHILD "+P(t)+" active="+t.gameObject.activeSelf+" pos="+t.position.ToString("F3")+" rot="+t.eulerAngles+" scale="+t.lossyScale);
foreach(var r in t.GetComponentsInChildren<Renderer>(true))s.AppendLine(" R "+P(r.transform)+" bounds="+r.bounds+" mats="+string.Join(",",r.sharedMaterials.Select(m=>m==null?"NULL":m.name+"/"+m.shader.name)));
foreach(var c in t.GetComponentsInChildren<Collider>(true))s.AppendLine(" C "+P(c.transform)+" type="+c.GetType().Name+" trigger="+c.isTrigger+" bounds="+c.bounds);
foreach(var b in t.GetComponentsInChildren<MonoBehaviour>(true))if(b!=null)s.AppendLine(" B "+P(b.transform)+" "+b.GetType().Name);
}
foreach(var l in Object.FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(Vector3.Distance(l.transform.position,shop.transform.position)<30)s.AppendLine("LIGHT "+P(l.transform)+" "+l.type+" "+l.intensity+" pos="+l.transform.position);
File.WriteAllText("Temp/Mok-building-inspect.txt",s.ToString());}
}
#endif
