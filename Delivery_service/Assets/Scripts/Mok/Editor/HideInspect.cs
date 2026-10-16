#if UNITY_EDITOR
using System.IO;using System.Linq;using System.Text;using UnityEditor;using UnityEngine;
namespace DeliveryService.Mok.Editor {
[InitializeOnLoad]public static class HideInspect {
static HideInspect(){EditorApplication.update+=Poll;}
static string PathOf(Transform t)=>t.parent==null?t.name:PathOf(t.parent)+"/"+t.name;
static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists("Temp/Mok-hide-inspect"))return;File.Delete("Temp/Mok-hide-inspect");var s=new StringBuilder();
foreach(var b in Object.FindObjectsByType<MotorcycleController>(FindObjectsInactive.Include,FindObjectsSortMode.None)){s.AppendLine("BIKE "+PathOf(b.transform)+" panel="+(b.bikeUIPanel==null?"NULL":PathOf(b.bikeUIPanel.transform)));}
foreach(var f in Object.FindObjectsByType<MotorcycleFuel>(FindObjectsInactive.Include,FindObjectsSortMode.None))s.AppendLine("FUEL "+(f.fuelGaugeUI==null?"NULL":PathOf(f.fuelGaugeUI.transform)+" active="+f.fuelGaugeUI.gameObject.activeInHierarchy+" enabled="+f.fuelGaugeUI.enabled));
foreach(var f in Object.FindObjectsByType<MotorcycleDurability>(FindObjectsInactive.Include,FindObjectsSortMode.None))s.AppendLine("DURABILITY "+(f.durabilityGaugeUI==null?"NULL":PathOf(f.durabilityGaugeUI.transform)));
foreach(var c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(PathOf(c.transform).StartsWith("Bike"))s.AppendLine("CANVAS "+PathOf(c.transform)+" mode="+c.renderMode+" cam="+(c.worldCamera!=null?PathOf(c.worldCamera.transform):"NULL")+" enabled="+c.enabled+" scale="+c.transform.lossyScale+" rect="+c.GetComponent<RectTransform>().rect);
foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>t.name.Contains("Trashbin")||t.name.Contains("Sidewalk")||t.name=="HOSPITAL"))s.AppendLine(t.name+" pos="+t.position+" rot="+t.eulerAngles+" bounds="+(t.GetComponent<Renderer>()!=null?t.GetComponent<Renderer>().bounds.ToString():"none"));
File.WriteAllText("Temp/Mok-hide-inspect.txt",s.ToString());}
}}
#endif
