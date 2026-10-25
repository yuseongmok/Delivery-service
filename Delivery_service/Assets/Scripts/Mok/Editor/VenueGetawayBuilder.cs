#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;
namespace DeliveryService.Mok.Editor {
public static class VenueGetawayBuilder {
 public static void Narrative(){var m=UnityEngine.Object.FindFirstObjectByType<VenueFinale>();var job=m.preparation;
 job.briefing=job.briefing.Select(t=>t.Replace("행사장 뒤에서 기다려 줄 차가 필요하다면서.","행사장 뒤에 준비해 둘 차가 필요하다면서.")).ToArray();
 job.briefing=job.briefing.Select(t=>t.Replace("부품을 달고 직접 운전할게. 행사 당일 네가 확인한 합류 지점에서 기다리겠다.","부품을 달고 행사 당일 네가 확인한 합류 지점에 차를 가져다 둘게. 운전은 네가 해.")).ToArray();
 job.briefing=job.briefing.Select(t=>t.Replace("신호가 오면 문 열어 두고 기다릴게.","차와 열쇠를 거기 두고 먼저 빠질게. 탈출 후에는 지정한 곳에 차를 버려. 추적될 수 있으니까.")).ToArray();
 job.briefing=job.briefing.Select(t=>t.Replace("행사장 탈출 차량·운전 지원 확보","행사장 탈출 차량·차량 폐기 지원 확보")).ToArray();
 job.briefing=job.briefing.Select(t=>t.Replace("정비사가 차량과 운전을 맡기로 했어. 행사 당일 네가 확인한 골목 끝 합류 지점에서 기다릴 거야.","정비사가 행사 당일 골목 끝 합류 지점에 차와 열쇠를 두기로 했어. 직접 운전해 탈출한 다음 지정 장소에 차를 버려. 가게까지 추적되면 안 돼.")).ToArray();
 job.ending=job.ending.Select(t=>t.Replace("행사장 뒤에서 기다려 줄 차가 필요하다면서.","행사장 뒤에 준비해 둘 차가 필요하다면서.")).ToArray();
 job.ending=job.ending.Select(t=>t.Replace("부품을 달고 직접 운전할게. 행사 당일 네가 확인한 합류 지점에서 기다리겠다.","부품을 달고 행사 당일 네가 확인한 합류 지점에 차를 가져다 둘게. 운전은 네가 해.")).ToArray();
 job.ending=job.ending.Select(t=>t.Replace("신호가 오면 문 열어 두고 기다릴게.","차와 열쇠를 거기 두고 먼저 빠질게. 탈출 후에는 지정한 곳에 차를 버려. 추적될 수 있으니까.")).ToArray();
 job.ending=job.ending.Select(t=>t.Replace("행사장 탈출 차량·운전 지원 확보","행사장 탈출 차량·차량 폐기 지원 확보")).ToArray();
 job.ending=job.ending.Select(t=>t.Replace("정비사가 차량과 운전을 맡기로 했어. 행사 당일 네가 확인한 골목 끝 합류 지점에서 기다릴 거야.","정비사가 행사 당일 골목 끝 합류 지점에 차와 열쇠를 두기로 했어. 직접 운전해 탈출한 다음 지정 장소에 차를 버려. 가게까지 추적되면 안 돼.")).ToArray();
 EditorUtility.SetDirty(job);EditorSceneManager.MarkSceneDirty(m.gameObject.scene);EditorSceneManager.SaveScene(m.gameObject.scene);File.WriteAllText("Logs/Venue-manual-narrative.txt","Saved mechanic dialogue: vehicle drop-off, player driving, abandon traceable car.");}
 public static void Apply(){var m=UnityEngine.Object.FindFirstObjectByType<VenueFinale>();if(!m||EditorApplication.isPlaying)throw new Exception("Open main scene in edit mode");if(!EditorSceneManager.SaveScene(m.gameObject.scene,"Logs/Venue-before-manual-drive-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true))throw new Exception("Backup failed");
 Physics.SyncTransforms();Vector3 origin=m.car.position;var seen=new Dictionary<Vector2Int,Vector2Int>();var depth=new Dictionary<Vector2Int,int>();var q=new Queue<Vector2Int>();var zero=Vector2Int.zero;seen[zero]=zero;depth[zero]=0;q.Enqueue(zero);Vector2Int best=zero;float closestGarage=float.PositiveInfinity;
 Vector3 World(Vector2Int p)=>origin+new Vector3(p.x*3,0,p.y*3);
 bool Road(Vector3 at){foreach(var offset in new[]{Vector3.zero,Vector3.right,Vector3.left,Vector3.forward,Vector3.back}){var hits=Physics.RaycastAll(at+offset+Vector3.up*3,Vector3.down,5,~0,QueryTriggerInteraction.Ignore).Where(h=>!h.transform.IsChildOf(m.car)&&!h.transform.GetComponentInParent<CitizenCombat>()&&!h.transform.GetComponentInParent<MotorcycleController>()).OrderBy(h=>h.distance).ToArray();if(hits.Length==0||!hits[0].collider.name.Contains("Road"))return false;}return !Physics.OverlapBox(at+Vector3.up*.85f,new Vector3(1.15f,.55f,1.15f),Quaternion.identity,~0,QueryTriggerInteraction.Ignore).Any(c=>!c.transform.IsChildOf(m.car)&&!c.GetComponentInParent<CitizenCombat>()&&!c.GetComponentInParent<MotorcycleController>()&&!c.attachedRigidbody&&c.bounds.max.y>at.y+.3f);}
 for(int i=0;i<20;i++){var candidate=origin+Vector3.right*i*3;if(Road(candidate)&&Road(candidate+Vector3.right*2)&&Road(candidate-Vector3.right*2)){origin=candidate;m.car.position=origin;break;}}
 while(q.Count>0&&seen.Count<16000){var p=q.Dequeue();if(depth[p]>=50&&Road(World(p)+Vector3.right*1.5f)&&Road(World(p)+Vector3.left*1.5f)&&Road(World(p)+Vector3.forward*1.5f)&&Road(World(p)+Vector3.back*1.5f)&&Vector3.Distance(World(p),m.entry.position)>100&&Vector3.Distance(World(p),m.preparation.approach.position)<closestGarage){best=p;closestGarage=Vector3.Distance(World(p),m.preparation.approach.position);}if(depth[p]>=180)continue;foreach(var d in new[]{Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right}){var next=p+d;if(seen.ContainsKey(next)||!Road(World(next))||!Road(Vector3.Lerp(World(p),World(next),.5f)))continue;seen[next]=p;depth[next]=depth[p]+1;q.Enqueue(next);}}
 if(depth[best]<50||closestGarage>45)throw new Exception("Drive route too short: "+depth[best]+" nodes, origin "+origin);
 var points=new List<Vector2Int>{best};while(points.Last()!=zero)points.Add(seen[points.Last()]);points.Reverse();
 var old=m.transform.Find("Manual getaway road route");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);var root=new GameObject("Manual getaway road route").transform;root.SetParent(m.transform,false);var route=new List<Transform>();for(int i=1;i<points.Count;i++){if(i<points.Count-1&&points[i]-points[i-1]==points[i+1]-points[i]&&i%5!=0)continue;var t=new GameObject("Drive checkpoint "+route.Count).transform;t.SetParent(root);t.position=World(points[i]);route.Add(t);}
 m.car.rotation=Quaternion.LookRotation(route[0].position-origin);m.driveEnd.position=World(best);m.driveEnd.name="Abandon traceable car here";
 var drive=m.car.GetComponent<VenueGetawayDrive>();if(!drive){drive=m.car.gameObject.AddComponent<VenueGetawayDrive>();var hinge=new GameObject("Driver door hinge").transform;hinge.SetParent(m.car,false);hinge.localPosition=new Vector3(-.95f,.65f,.8f);BankPrologueBuilder.OpenSupportDoor(m.car,hinge,true);m.door=hinge;}
 m.getaway=drive;drive.mission=m;drive.steering=110;drive.maximumSpeed=22;drive.eye=new Vector3(-.38f,1.35f,.32f);drive.route=route.ToArray();drive.wheels=m.car.GetComponentsInChildren<Transform>(true).Where(t=>t.name.Contains("Wheel_")&&!t.name.Contains("Steering")).ToArray();drive.impactClip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Scripts/Yun/Audio/Clips/motorcycle-object-impact.mp3");
 m.seat.localPosition=new Vector3(-.48f,-.25f,-.1f);m.carStand.localPosition=new Vector3(-1.8f,.04f,0);m.driver.gameObject.SetActive(false);
 m.opening=new[]{"민기: 오늘은 영업 안 해. 납품 명단, 출입구, 탈출 차량까지 준비됐어.","민기: 피자는 위장이야. 박진웅이 누구와 거래하는지만 확인해. 총부터 꺼내지는 마."};
 m.closing=new[]{"정비사: 차는 지정한 곳에 버려. 가게까지 추적당하면 안 돼.","나: 차는 두고 왔어. 박진웅이 은행 일의 흔적을 지우고 있어. 다음 거래 전에 기록을 찾아야 해.","민기: 잘했어. 차는 정비사가 처리할 거야. 가게에서 다음 일을 준비하자."};
 EditorUtility.SetDirty(m);EditorUtility.SetDirty(drive);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(m.gameObject.scene);EditorSceneManager.SaveScene(m.gameObject.scene);File.WriteAllText("Logs/Venue-manual-drive-install.txt","Saved route "+depth[best]*3+"m, origin="+origin+", destination="+m.driveEnd.position+"\n"+string.Join("\n",route.Select(t=>t.position.ToString())));
 }
}}
#endif
