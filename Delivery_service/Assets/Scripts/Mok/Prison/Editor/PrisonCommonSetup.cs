#if UNITY_EDITOR
using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using DeliveryService.Yun.Settings;
using DeliveryService.Mok.Navigation;
using Object=UnityEngine.Object;
namespace DeliveryService.Mok.Editor {
public static class PrisonCommonSetup {
 public static void Inspect() { Apply(); }
 public static void Traffic(){
 var scene=SceneManager.GetSceneByPath("Assets/Scenes/PrisonChapter.unity");
 if(!scene.isLoaded)scene=EditorSceneManager.OpenScene("Assets/Scenes/PrisonChapter.unity",OpenSceneMode.Additive);
 EditorSceneManager.SaveScene(scene,"../.codex-work/prison-mission/PrisonChapter.before-traffic.unity",true);
 var population=All<PrisonPopulation>(scene).Single();
 float Segment(Vector3 p,Vector3 a,Vector3 b){var d=b-a;return Vector3.Distance(p,a+d*Mathf.Clamp01(Vector3.Dot(p-a,d)/d.sqrMagnitude));}
 float CartDistance(Vector3 p){p.y=0;return Mathf.Min(Segment(p,new Vector3(22,0,-27),new Vector3(22,0,23)),Segment(p,new Vector3(0,0,23),new Vector3(22,0,23)),Segment(p,new Vector3(0,0,-25),new Vector3(0,0,23)),Segment(p,new Vector3(4.8f,0,-25),new Vector3(4.8f,0,-11)));}
 var candidates=population.spawnPoints.Where(p=>CartDistance(p)>3&&NavMesh.FindClosestEdge(p,out var e,NavMesh.AllAreas)&&e.distance<2).ToList();
 if(candidates.Count<16)throw new Exception("Not enough safe guard posts: "+candidates.Count);
 var used=new List<Vector3>();
 foreach(var slot in population.slots.Where(s=>s.guard)){
 var guard=slot.actor.GetComponent<PrisonGuard>();guard.holdPost=slot.guardIndex>=4;slot.holdPost=guard.holdPost;
 if(guard.holdPost){
 var position=candidates.OrderByDescending(p=>used.Count==0?Vector3.Distance(p,new Vector3(0,0,-15)):used.Min(q=>Vector3.Distance(p,q))).First();used.Add(position);candidates.Remove(position);
 guard.transform.position=position;guard.postPosition=position;
 NavMesh.FindClosestEdge(position,out var edge,NavMesh.AllAreas);var toward=position-edge.position;toward.y=0;
 guard.postYaw=toward.sqrMagnitude>.01f?Quaternion.LookRotation(toward).eulerAngles.y:0;guard.transform.rotation=Quaternion.Euler(0,guard.postYaw,0);
 slot.postPosition=position;slot.postYaw=guard.postYaw;
 }
 EditorUtility.SetDirty(guard);
 }
 var anchors=population.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("생활 경유지")&&CartDistance(t.position)>2).ToArray();
 foreach(var slot in population.slots.Where(s=>!s.guard)){
 var resident=slot.actor.GetComponent<PrisonResident>();resident.route=anchors.OrderBy(t=>(t.position-resident.transform.position).sqrMagnitude).Take(14).ToArray();slot.route=resident.route;EditorUtility.SetDirty(resident);
 }
 EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
 File.WriteAllText("Logs/Prison-traffic-setup.txt","PASS: 16 guard posts outside cart route, 4 patrols, 50 inmates retained; local destination groups="+anchors.Length);
 }
 public static void Populate(){
 var scene=SceneManager.GetSceneByPath("Assets/Scenes/PrisonChapter.unity");
 if(!scene.isLoaded)scene=EditorSceneManager.OpenScene("Assets/Scenes/PrisonChapter.unity",OpenSceneMode.Additive);
 EditorSceneManager.SaveScene(scene,"../.codex-work/prison-mission/PrisonChapter.before-population.unity",true);
 var mission=All<PrisonMission>(scene).Single();
 if(All<PrisonPopulation>(scene).Any())throw new Exception("Population already exists; preserve current scene edits");
 var inmates=All<PrisonResident>(scene).Where(r=>r.route!=null&&r.route.Length>0).ToList();
 var guards=mission.guards.ToList();
 var root=new GameObject("감옥 생활 인원 • 죄수 50 / 간수 20");SceneManager.MoveGameObjectToScene(root,scene);
 var population=root.AddComponent<PrisonPopulation>();
 var points=new List<Vector3>();var path=new NavMeshPath();
 NavMesh.SamplePosition(new Vector3(0,0,-15),out var origin,2,NavMesh.AllAreas);
 for(float z=-26;z<=40;z+=3)for(float x=-35;x<=40;x+=3){
 if(z<19&&x<-5)continue; // 감방과 의무실의 미션 동선을 비워 둔다.
 if(z<-18&&x>5)continue;
 if(!NavMesh.SamplePosition(new Vector3(x,0,z),out var hit,.7f,NavMesh.AllAreas)||Mathf.Abs(hit.position.y)>1)continue;
 if(!NavMesh.CalculatePath(origin.position,hit.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)continue;
 if(points.Any(p=>Vector3.Distance(p,hit.position)<2.4f))continue;
 points.Add(hit.position);
 }
 if(points.Count<70)throw new Exception("Not enough connected population positions: "+points.Count);
 var anchors=new List<Transform>();
 for(int i=0;i<points.Count;i+=5){var t=new GameObject("생활 경유지 "+i).transform;t.SetParent(root.transform,false);t.position=points[i];anchors.Add(t);}
 var shuffled=points.OrderBy(p=>UnityEngine.Random.value).ToArray();int spawn=0;
 Transform[] Route()=>anchors.OrderBy(t=>UnityEngine.Random.value).Take(8).ToArray();
 population.inmateTemplate=Object.Instantiate(inmates[0].gameObject,root.transform);population.inmateTemplate.name="죄수 보충 원본";population.inmateTemplate.SetActive(false);
 population.guardTemplate=Object.Instantiate(guards[2].gameObject,root.transform);population.guardTemplate.name="간수 보충 원본";population.guardTemplate.SetActive(false);
 while(inmates.Count<50){var g=Object.Instantiate(population.inmateTemplate,shuffled[spawn++],Quaternion.identity,root.transform);g.name="생활동 재소자 "+(inmates.Count+1);g.SetActive(true);inmates.Add(g.GetComponent<PrisonResident>());}
 while(guards.Count<20){var g=Object.Instantiate(population.guardTemplate,shuffled[spawn++],Quaternion.identity,root.transform);g.name="생활 순찰 간수 "+(guards.Count+1);var guard=g.GetComponent<PrisonGuard>();guard.infirmaryWatch=false;guard.route=Route();g.SetActive(true);guards.Add(guard);}
 foreach(var resident in inmates)resident.route=Route();
 foreach(var actor in inmates.Select(r=>r.gameObject).Concat(guards.Select(g=>g.gameObject)))actor.GetComponent<NavMeshAgent>().avoidancePriority=UnityEngine.Random.Range(25,75);
 mission.guards=guards.ToArray();population.spawnPoints=points.ToArray();
 population.slots=inmates.Select(r=>new PrisonPopulation.Slot{actor=r.GetComponent<NPCControl>(),route=r.route,actorName=r.name}).Concat(guards.Select((g,i)=>new PrisonPopulation.Slot{actor=g.GetComponent<NPCControl>(),guard=true,infirmaryWatch=g.infirmaryWatch,route=g.route,actorName=g.name,guardIndex=i})).ToArray();
 EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
 File.WriteAllText("Logs/Prison-population-setup.txt","PASS: 50 roaming inmates, 20 guards; spawn points="+points.Count);
 }
 public static void Comfort(){
 var paths=new[]{"Assets/Scenes/JJinCity.unity","Assets/Scenes/PrisonChapter.unity"};
 foreach(var path in paths){
 var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;
 if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
 EditorSceneManager.SaveScene(scene,"../.codex-work/prison-mission/"+scene.name+".before-sensitivity.unity",true);
 foreach(var panel in All<GameSettingsPanel>(scene))DeliveryService.Yun.Editor.GameSettingsSetup.AddSensitivity(panel);
 var mission=All<PrisonMission>(scene).FirstOrDefault();
 if(mission){
 var root=mission.transform.Find("생활 이동 지점");
 if(!root){root=new GameObject("생활 이동 지점").transform;root.SetParent(mission.transform,false);
 foreach(var pos in new[]{new Vector3(-3,0,-23),new Vector3(3,0,-14),new Vector3(-3,0,-4),new Vector3(3,0,8),new Vector3(0,0,22),new Vector3(16,0,23),new Vector3(24,0,12),new Vector3(32,0,8)}){
 if(!NavMesh.SamplePosition(pos,out var hit,1.5f,NavMesh.AllAreas))continue;
 var point=new GameObject("생활 지점 "+root.childCount).transform;point.SetParent(root,false);point.position=hit.position;
 }}
 var points=root.Cast<Transform>().ToArray();
 foreach(var resident in All<PrisonResident>(scene))if(resident.route!=null&&resident.route.Length>0){resident.route=points;EditorUtility.SetDirty(resident);}
 }
 EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
 if(opened)EditorSceneManager.CloseScene(scene,true);
 }
 File.WriteAllText("Logs/Prison-comfort-setup.txt","PASS: saved sensitivity sliders in city/prison and shared resident routes");
 }
 public static void Apply(){
 var prison=SceneManager.GetSceneByPath("Assets/Scenes/PrisonChapter.unity");
 if(!prison.isLoaded)prison=EditorSceneManager.OpenScene("Assets/Scenes/PrisonChapter.unity",OpenSceneMode.Additive);
 EditorSceneManager.SaveScene(prison,"../.codex-work/prison-mission/PrisonChapter.before-common-ui.unity",true);
 var city=SceneManager.GetSceneByPath("Assets/Scenes/JJinCity.unity");bool opened=!city.isLoaded;
 if(opened)city=EditorSceneManager.OpenScene("Assets/Scenes/JJinCity.unity",OpenSceneMode.Additive);
 try {
 var mission=All<PrisonMission>(prison).Single();var source=All<PlayerMove>(city).Single().gameObject;var player=mission.player.gameObject;
 var map=new Dictionary<Object,Object>();
 void Pair(GameObject a,GameObject b){map[a]=b;foreach(var ac in a.GetComponents<Component>()){if(!ac)continue;var same=a.GetComponents(ac.GetType());int index=Array.IndexOf(same,ac);var bc=b.GetComponents(ac.GetType());if(index<bc.Length)map[ac]=bc[index];}foreach(Transform t in a.transform){var dest=b.transform.Find(t.name);if(dest)Pair(t.gameObject,dest.gameObject);}}
 Pair(source,player);
 var roots=new List<GameObject>();
 GameObject Copy(GameObject original,string name){var old=prison.GetRootGameObjects().FirstOrDefault(g=>g.name==name);if(old)Object.DestroyImmediate(old);var copy=Object.Instantiate(original);copy.name=name;SceneManager.MoveGameObjectToScene(copy,prison);Pair(original,copy);roots.Add(copy);return copy;}
 Copy(All<GameAudioSettings>(city).Single().transform.root.gameObject,"SettingsCanvas");
 Copy(All<NavigationManager>(city).Single().transform.root.gameObject,"Navigation");
 if(!All<UnityEngine.EventSystems.EventSystem>(prison).Any())Copy(All<UnityEngine.EventSystems.EventSystem>(city).First().gameObject,"EventSystem");
 var stock=All<DeliveryService.Mok.Stocks.StockMarket>(city).FirstOrDefault();if(stock)Copy(stock.gameObject,"Prison common stocks");
 var money=All<MoneyManager>(city).First();var moneyCopy=Copy(money.gameObject,"Prison common money");foreach(var c in moneyCopy.GetComponents<MonoBehaviour>())if(!(c is MoneyManager))Object.DestroyImmediate(c);
 var audio=All<MonoBehaviour>(city).FirstOrDefault(c=>c&&c.GetType().Name=="AudioManager");if(audio)Copy(audio.gameObject,"Prison common audio");
 // 원래 플레이어의 카메라 값과 공용 HUD 활성 상태를 복원한다.
 foreach(var from in source.GetComponentsInChildren<CameraMove>(true)){var to=(CameraMove)map[from];EditorUtility.CopySerialized(from,to);}
 foreach(var from in source.GetComponentsInChildren<Camera>(true)){if(map.TryGetValue(from,out var obj)){var to=(Camera)obj;to.fieldOfView=from.fieldOfView;to.nearClipPlane=from.nearClipPlane;to.transform.localPosition=from.transform.localPosition;}}
 foreach(var from in source.GetComponentsInChildren<Canvas>(true)){if(map.TryGetValue(from,out var obj))((Canvas)obj).gameObject.SetActive(from.gameObject.activeSelf);}
 foreach(string name in new[]{"PlayerInteract","InteractHoverManager","BikeRecall"})foreach(var c in player.GetComponents<MonoBehaviour>())if(c&&c.GetType().Name==name)c.enabled=true;
 var notes=player.GetComponent<PrisonPhoneNotes>();if(notes)Object.DestroyImmediate(notes);
 roots.Add(player);
 // 같은 플레이어/복제한 공용 시스템으로 참조를 연결하고 도시 전용 시설 참조만 비운다.
 foreach(var component in roots.SelectMany(g=>g.GetComponentsInChildren<Component>(true))){if(!component)continue;var data=new SerializedObject(component);var it=data.GetIterator();while(it.Next(true))if(it.propertyType==SerializedPropertyType.ObjectReference&&it.objectReferenceValue){var value=it.objectReferenceValue;if(map.TryGetValue(value,out var replacement))it.objectReferenceValue=replacement;else {var go=value is Component c?c.gameObject:value as GameObject;if(go&&go.scene==city)it.objectReferenceValue=null;}}data.ApplyModifiedPropertiesWithoutUndo();}
 var broker=All<BrokerMission>(city).Single();var ui=Copy(broker.missionUI,"Prison mission UI");ui.SetActive(true);var objectiveCopy=map[broker.objective] as Text;var subtitleCopy=map[broker.subtitle] as Text;var markerCopy=map[broker.marker] as Text;

 if(opened){EditorSceneManager.CloseScene(city,true);opened=false;}
 var nav=All<NavigationManager>(prison).Single();nav.SetNavigationTarget(player.transform);
 var style=All<NavigationMapStyle>(prison).Single();var filter=style.GetComponent<MeshFilter>();
 // 감옥의 실제 이동 가능한 바닥을 공용 지도 색상으로 표시한다.
 var surface=All<Unity.AI.Navigation.NavMeshSurface>(prison).First();var tri=NavMesh.CalculateTriangulation();
 var mesh=new Mesh{name="Prison map",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.vertices=tri.vertices.Select(v=>new Vector3(v.x,.15f,v.z)).ToArray();mesh.triangles=tri.indices;mesh.RecalculateNormals();mesh.RecalculateBounds();
 const string meshPath="Assets/DeliveryPrisonChapter/Mission01/PrisonMap.asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);if(saved){EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);mesh=saved;}else AssetDatabase.CreateAsset(mesh,meshPath);
 filter.sharedMesh=mesh;style.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);style.transform.localScale=Vector3.one;var materials=style.GetComponent<MeshRenderer>().sharedMaterials;style.GetComponent<MeshRenderer>().sharedMaterials=new[]{materials[Mathf.Min(1,materials.Length-1)]};
 var oldHud=mission.GetComponent<PrisonMissionHUD>();if(oldHud)Object.DestroyImmediate(oldHud);
 var view=mission.GetComponent<PrisonSharedPresentation>();if(!view)view=mission.gameObject.AddComponent<PrisonSharedPresentation>();view.mission=mission;
 view.ui=ui;view.objective=objectiveCopy;view.subtitle=subtitleCopy;view.marker=markerCopy;view.navigation=nav;
 var legacyHud=mission.GetComponent<CompactHUD>();if(legacyHud)Object.DestroyImmediate(legacyHud);
 if(!All<CompactHUD>(prison).Any()){var hud=new GameObject("Prison common HUD");SceneManager.MoveGameObjectToScene(hud,prison);hud.AddComponent<CompactHUD>();}
 var point=mission.GetComponent<NavigationPlace>();if(!point)point=mission.gameObject.AddComponent<NavigationPlace>();point.displayName="현재 임무";point.placeId="prison_mission";point.kind=NavigationPlaceKind.Other;view.place=point;
 foreach(var spec in new[]{(mission.cooking.transform,"주방",NavigationPlaceKind.Pizza),(mission.infirmary.transform,"의무실",NavigationPlaceKind.Repair),(mission.cell.transform,"내 방",NavigationPlaceKind.Bike),(mission.cart.transform,"배식카트",NavigationPlaceKind.Weapon)}){var p=spec.Item1.GetComponent<NavigationPlace>();if(!p)p=spec.Item1.gameObject.AddComponent<NavigationPlace>();p.displayName=spec.Item2;p.kind=spec.Item3;p.placeId="prison_"+spec.Item3;p.entrance=spec.Item1;p.mapAnchor=spec.Item1;}
 foreach(var text in All<TMPro.TMP_Text>(prison))text.font=AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/Fonts/Jua_SDF.asset");
 EditorUtility.SetDirty(view);EditorSceneManager.MarkSceneDirty(prison);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(prison);
 File.WriteAllText("Logs/Prison-common-result.txt","PASS saved common settings, navigation, city camera settings, phone Canvas, city mission UI and HUD");
 }finally{if(opened)EditorSceneManager.CloseScene(city,true);}
 }
 static T[] All<T>(Scene scene) where T:Component=>scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();
}
}
#endif







