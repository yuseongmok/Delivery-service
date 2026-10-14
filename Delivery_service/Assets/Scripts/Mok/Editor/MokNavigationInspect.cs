#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.IO;
using System.Text;
using System.Linq;
public static class MokNavigationInspect {
[InitializeOnLoadMethod]static void Init(){EditorApplication.delayCall+=Run;}
static string P(Transform t)=>t.parent==null?t.name:P(t.parent)+"/"+t.name;
static void Run(){if(EditorApplication.isPlayingOrWillChangePlaymode)return;var s=new StringBuilder();foreach(var c in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include,FindObjectsSortMode.None))s.AppendLine("CAM "+P(c.transform)+" pos="+c.transform.position+" mask="+c.cullingMask+" ortho="+c.orthographic+" size="+c.orthographicSize+" rt="+(c.targetTexture==null?"none":c.targetTexture.name));foreach(var r in Object.FindObjectsByType<RawImage>(FindObjectsInactive.Include,FindObjectsSortMode.None))s.AppendLine("RAW "+P(r.transform)+" texture="+(r.texture==null?"none":r.texture.name)+" rect="+r.rectTransform.rect+" active="+r.gameObject.activeInHierarchy);foreach(var b in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(b!=null&&(b.GetType().Name.StartsWith("Navigation")||b is PhoneController)){s.AppendLine("SCRIPT "+P(b.transform)+" "+b.GetType().Name);var so=new SerializedObject(b);var p=so.GetIterator();while(p.Next(true))if(p.propertyType==SerializedPropertyType.ObjectReference&&p.objectReferenceValue!=null)s.AppendLine(" REF "+p.propertyPath+"="+p.objectReferenceValue.name);}
foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))if(t.name=="GasPump"||t.name=="PizzaStore"||t.name=="repair_shop")s.AppendLine("POI "+P(t)+" pos="+t.position);File.WriteAllText("Temp/Mok-nav-inspect.txt",s.ToString());}
}
#endif
