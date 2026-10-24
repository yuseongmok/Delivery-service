using UnityEngine;
namespace DeliveryService.Mok {
// 적대 상태와 임무 경비만 표시한다. 시민과 중요한 대화 상대는 제외한다.
public sealed class EnemyLocationMarker : MonoBehaviour {
 CitizenCombat combat; BackdoorGuard guard; PlayerCombat player; Texture2D triangle;
 void Start(){combat=GetComponent<CitizenCombat>();guard=GetComponent<BackdoorGuard>();player=FindFirstObjectByType<PlayerCombat>();triangle=new Texture2D(32,32,TextureFormat.RGBA32,false);for(int y=0;y<32;y++)for(int x=0;x<32;x++)triangle.SetPixel(x,y,Mathf.Abs(x-15.5f)<=y*.5f?Color.white:Color.clear);triangle.Apply();}
 void OnGUI(){
  if(!combat||combat.Health<=0||!combat.Npc||combat.Npc.IsDead||!player||Time.timeScale<=0||Cursor.lockState!=CursorLockMode.Locked||!triangle)return;
  if(!combat.IsAggressive&&!(guard&&guard.mission&&guard.mission.Running))return;
  Camera camera=player.View;if(player.RiddenBike&&player.RiddenBike.bikeCamera)camera=player.RiddenBike.bikeCamera.GetComponentInChildren<Camera>();if(!camera)return;
  if(Vector3.Distance(camera.transform.position,transform.position)>100)return;
  float size=Mathf.Clamp(Screen.height*.032f,22,36);var v=camera.WorldToViewportPoint(transform.position+Vector3.up*2.25f);
  var p=RobberTheftFeedback.ProjectMarker(v,new Vector2(Screen.width,Screen.height),size);var old=GUI.color;var matrix=GUI.matrix;
  GUIUtility.RotateAroundPivot(p.z,new Vector2(p.x,p.y));GUI.color=new Color(.12f,.01f,.01f);GUI.DrawTexture(new Rect(p.x-size/2-2,p.y-size/2-2,size+4,size+4),triangle);GUI.color=new Color(1,.12f,.12f);GUI.DrawTexture(new Rect(p.x-size/2,p.y-size/2,size,size),triangle);GUI.matrix=matrix;GUI.color=old;
 }
 void OnDestroy(){if(triangle)Destroy(triangle);}
}}
