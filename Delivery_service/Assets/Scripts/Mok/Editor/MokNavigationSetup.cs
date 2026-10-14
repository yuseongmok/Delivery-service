#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using DeliveryService.Mok.Navigation;
public static class MokNavigationSetup {
const string Folder="Assets/Scripts/Mok/Art/NavigationMap";
static readonly List<Vector3> vertices=new List<Vector3>();static readonly List<int>[] tris={new List<int>(),new List<int>(),new List<int>()};
[InitializeOnLoadMethod]static void Init(){if(File.Exists("Temp/Mok-nav-build-request"))EditorApplication.delayCall+=()=>{if(EditorApplication.isPlayingOrWillChangePlaymode)return;File.Delete("Temp/Mok-nav-build-request");try{Build();}catch(Exception e){File.WriteAllText("Temp/Mok-nav-result.txt",e.ToString());Debug.LogException(e);}};}
static void Quad(Bounds b,int m,float height){int n=vertices.Count;vertices.Add(new Vector3(b.min.x,height,b.min.z));vertices.Add(new Vector3(b.min.x,height,b.max.z));vertices.Add(new Vector3(b.max.x,height,b.max.z));vertices.Add(new Vector3(b.max.x,height,b.min.z));tris[m].AddRange(new[]{n,n+1,n+2,n,n+2,n+3});}
static Material Mat(string name,string hex){var mat=new Material(Shader.Find("Mok/NavigationFlatColor"));ColorUtility.TryParseHtmlString(hex,out var c);mat.SetColor("_BaseColor",c);mat.name=name;AssetDatabase.CreateAsset(mat,Folder+"/"+name+".mat");return mat;}
static NavigationPlace Place(GameObject owner,string id,string name,NavigationPlaceKind kind,Vector3 entrance){var p=owner.AddComponent<NavigationPlace>();p.placeId=id;p.displayName=name;p.kind=kind;p.mapAnchor=owner.transform;var e=new GameObject("NavigationEntrance");e.transform.SetParent(owner.transform,true);e.transform.position=entrance;p.entrance=e.transform;return p;}
[MenuItem("Tools/Delivery Service/Mok/Install Navigation Map")]
static void Build(){var nav=UnityEngine.Object.FindFirstObjectByType<NavigationManager>();if(nav==null)throw new Exception("Navigation manager missing");if(GameObject.Find("Mok Navigation Map")!=null)throw new Exception("Map already installed, preserve edits");EditorSceneManager.SaveScene(nav.gameObject.scene);File.Copy(nav.gameObject.scene.path,"Temp/Mok-nav-before.unity",true);
var track=GameObject.Find("NavigationCamera").GetComponent<Camera>();var font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/BMJUA_ttf.ttf");var root=new GameObject("Mok Navigation Map");root.layer=NavigationMapStyle.MapLayer;Undo.RegisterCreatedObjectUndo(root,"Install navigation map");
vertices.Clear();foreach(var t in tris)t.Clear();var renderers=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();var ground=renderers.Where(r=>r.name.StartsWith("SM_Env_Sidewalk")||r.name.StartsWith("SM_Env_Road_")).ToArray();if(ground.Length==0)throw new Exception("No map ground");var bounds=ground[0].bounds;foreach(var r in ground)bounds.Encapsulate(r.bounds);Quad(bounds,0,-.15f);int roads=0,buildings=0;
foreach(var r in ground)if(r.name.StartsWith("SM_Env_Road_")&&!r.name.Contains("Lines")&&!r.name.Contains("Arrow")){Quad(r.bounds,1,0);roads++;}
foreach(var r in renderers)if(r.name.StartsWith("SM_Bld_")&&r.bounds.size.y>1&&r.bounds.size.x>1&&r.bounds.size.z>1){Quad(r.bounds,2,.15f);buildings++;}
foreach(string path in new[]{"PizzaStore/Mok Pizza Shop","repair_shop/Mok Repair Garage"}){var g=GameObject.Find(path);if(g==null)continue;var floor=g.transform.Find(path.Contains("Pizza")?"Foundation":"WorkshopFloor");Quad(floor.GetComponent<Collider>().bounds,2,.15f);buildings++;}
var mesh=new Mesh{name="CityNavigationMap",indexFormat=IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.subMeshCount=3;for(int i=0;i<3;i++)mesh.SetTriangles(tris[i],i);mesh.RecalculateBounds();mesh.RecalculateNormals();AssetDatabase.CreateAsset(mesh,Folder+"/CityNavigationMap.asset");root.AddComponent<MeshFilter>().sharedMesh=mesh;root.AddComponent<MeshRenderer>().sharedMaterials=new[]{Mat("Ground","#929B9D"),Mat("Roads","#D9DEDE"),Mat("Buildings","#424A50")};
var line=GameObject.Find("NavigationPathLine").GetComponent<LineRenderer>();line.gameObject.layer=17;line.sharedMaterial=Mat("Route","#20B8A6");line.startColor=line.endColor=Color.white;line.widthMultiplier=1.5f;line.numCornerVertices=4;line.numCapVertices=4;
NavigationMapStyle.Configure(track);track.orthographicSize=40;
var overview=new GameObject("NavigationOverviewCamera").AddComponent<Camera>();overview.transform.position=new Vector3(bounds.center.x,80,bounds.center.z);overview.transform.rotation=Quaternion.Euler(90,0,0);overview.orthographicSize=Mathf.Max(bounds.extents.z+12,(bounds.extents.x+12)/(850f/520f));NavigationMapStyle.Configure(overview);var rt=new RenderTexture(1360,832,24,RenderTextureFormat.ARGB32){name="NavigationOverview",antiAliasing=1};AssetDatabase.CreateAsset(rt,Folder+"/NavigationOverview.renderTexture");overview.targetTexture=rt;overview.enabled=false;
var style=root.AddComponent<NavigationMapStyle>();style.trackingCamera=track;style.overviewCamera=overview;
foreach(var c in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(c!=track&&c!=overview)c.cullingMask&=~(1<<NavigationMapStyle.MapLayer);
Place(GameObject.Find("PizzaStore"),"pizza","피자 매장",NavigationPlaceKind.Pizza,new Vector3(141.25f,.08f,64.5f));
var repair=GameObject.Find("repair_shop/Mok Repair Garage");Place(GameObject.Find("repair_shop"),"repair","수리점",NavigationPlaceKind.Repair,repair.transform.TransformPoint(new Vector3(0,0,6.1f)));
var pump=GameObject.Find("GasPump");Place(pump,"fuel","주유소",NavigationPlaceKind.Fuel,new Vector3(pump.transform.position.x,.1f,pump.transform.position.z+3));
var bike=UnityEngine.Object.FindFirstObjectByType<MotorcycleController>();var bp=bike.gameObject.AddComponent<NavigationPlace>();bp.placeId="bike";bp.displayName="내 오토바이";bp.kind=NavigationPlaceKind.Bike;bp.mapAnchor=bike.transform;bp.entrance=bike.transform;
var planner=root.AddComponent<NavigationPlanner>();planner.manager=nav;planner.overviewCamera=overview;planner.overviewTexture=rt;planner.font=font;
foreach(var raw in UnityEngine.Object.FindObjectsByType<RawImage>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(raw.texture==track.targetTexture){var pins=raw.gameObject.AddComponent<NavigationMapPins>();pins.mapCamera=track;pins.manager=nav;pins.font=font;pins.selectable=false;if(raw.name=="NavigationFinalDisplay"){var hint=new GameObject("DestinationHint",typeof(RectTransform),typeof(Text));hint.layer=raw.gameObject.layer;hint.transform.SetParent(raw.transform,false);var rect=hint.GetComponent<RectTransform>();rect.anchorMin=new Vector2(0,0);rect.anchorMax=new Vector2(1,0);rect.pivot=new Vector2(.5f,0);rect.sizeDelta=new Vector2(0,45);rect.anchoredPosition=new Vector2(0,10);var text=hint.GetComponent<Text>();text.font=font;text.text="N  목적지 선택";text.fontSize=26;text.alignment=TextAnchor.MiddleCenter;text.color=new Color(.05f,.12f,.15f);text.raycastTarget=false;}}
foreach(var icon in UnityEngine.Object.FindObjectsByType<NavigationPlayerIconController>(FindObjectsInactive.Include,FindObjectsSortMode.None))icon.gameObject.SetActive(false);foreach(var icon in UnityEngine.Object.FindObjectsByType<NavigationDestinationIconController>(FindObjectsInactive.Include,FindObjectsSortMode.None))icon.gameObject.SetActive(false);
AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(nav.gameObject.scene);EditorSceneManager.SaveScene(nav.gameObject.scene);File.WriteAllText("Temp/Mok-nav-result.txt",$"PASS: {roads} road footprints, {buildings} building footprints. Four extensible places; tracking + overview maps configured. Scene saved.");}
}
#endif
