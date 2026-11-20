using UnityEngine;
namespace DeliveryService.Mok {
// Read-only race presentation. It never advances checkpoints or changes the winner.
public sealed class RaceDrivingHUD : MonoBehaviour {
 RaceManager race;MotorcycleController bike;RacerCarAI rival;GUIStyle text;
 public int Rank {get;private set;}=1;
 public float RivalDistance {get;private set;}
 public bool Visible=>race&&race.IsRaceActive&&race.DrivingStarted&&!race.ShowingResult&&!RaceStartPresentation.Active&&Time.timeScale>0;
 void Awake(){race=GetComponent<RaceManager>();}
 void Update(){
 if(!Visible)return;
 if(!bike||!bike.isDriven){foreach(var b in FindObjectsByType<MotorcycleController>(FindObjectsSortMode.None))if(b.isDriven){bike=b;break;}}
 if(!rival&&race.OpponentCar)rival=race.OpponentCar.GetComponent<RacerCarAI>();
 if(!bike||!rival)return;
 RivalDistance=Vector3.Distance(bike.transform.position,rival.transform.position);
 Rank=race.PlayerFinished||race.OpponentFinished?(race.PlayerWon?1:2):Progress(bike.transform.position,race.PlayerStart,race.PlayerCourse,race.PassedCheckpoints)>=Progress(rival.transform.position,race.OpponentStart,race.OpponentCourse,rival.PassedCheckpoints)?1:2;
 }
 public static float Progress(Vector3 position,Transform start,GameObject[] points,int passed){
 if(!start||points==null||points.Length==0)return 0;if(passed>=points.Length)return 1;
 float total=0,done=0;Vector3 a=start.position;
 for(int i=0;i<points.Length;i++){if(!points[i])continue;Vector3 b=points[i].transform.position;var segment=b-a;segment.y=0;float length=segment.magnitude;total+=length;
 if(i<passed)done+=length;else if(i==passed&&length>.001f){var offset=position-a;offset.y=0;done+=Mathf.Clamp01(Vector3.Dot(offset,segment)/(length*length))*length;}a=b;}
 return total>.001f?done/total:0;
 }
 void Box(Rect r,Color c){GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=Color.white;}
 void Label(Rect r,string value,int size,Color color,TextAnchor align=TextAnchor.MiddleLeft){text.fontSize=size;text.normal.textColor=color;text.alignment=align;GUI.Label(r,value,text);}
 void OnGUI(){
 if(!Visible||!bike)return;
 var previous=GUI.matrix;var color=GUI.color;float scale=Mathf.Min(Screen.width/1920f,Screen.height/1080f);float width=Screen.width/scale,height=Screen.height/scale;
 GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);if(text==null)text=new GUIStyle(GUI.skin.label){font=MokUIFont.Font};
 Color orange=new Color(1,.57f,.23f),cream=new Color(1,.94f,.8f);float x=width-376,y=42;
 Box(new Rect(x,y,334,157),new Color(.045f,.065f,.085f,.91f));Box(new Rect(x,y,5,157),orange);
 Label(new Rect(x+22,y+9,190,27),"STREET RACE",20,orange);
 Label(new Rect(x+22,y+39,120,59),Rank+" / 2",44,cream);
 Label(new Rect(x+155,y+42,160,26),"상대와의 거리",19,new Color(.65f,.72f,.76f));
 Label(new Rect(x+155,y+69,160,31),Mathf.RoundToInt(RivalDistance)+" m",28,cream);
 int total=race.PlayerCourse.Length,passed=race.PassedCheckpoints;
 Label(new Rect(x+22,y+109,290,25),"체크포인트  "+passed+" / "+total,21,cream);
 Box(new Rect(x+22,y+141,290,4),new Color(.23f,.28f,.3f));Box(new Rect(x+22,y+141,290*passed/Mathf.Max(1f,total),4),orange);
 if(!race.PlayerFinished&&passed<total&&race.PlayerCourse[passed]){
 var camera=bike.bikeCamera?bike.bikeCamera.GetComponentInChildren<Camera>():null;
 if(camera){Vector3 target=race.PlayerCourse[passed].transform.position+Vector3.up*2;Vector3 viewport=camera.WorldToViewportPoint(target);
 bool onScreen=viewport.z>0&&viewport.x>.1f&&viewport.x<.8f&&viewport.y>.22f&&viewport.y<.75f;
 Vector2 point=onScreen?new Vector2(viewport.x*width,(1-viewport.y)*height):new Vector2(width*.5f,height*.22f);
 float angle=Vector3.SignedAngle(bike.transform.forward,Vector3.ProjectOnPlane(target-bike.transform.position,Vector3.up),Vector3.up);
 var matrix=GUI.matrix;GUIUtility.RotateAroundPivot(onScreen?0:angle,point);
 Box(new Rect(point.x-4,point.y-18,8,30),orange);GUIUtility.RotateAroundPivot(-45,point);Box(new Rect(point.x-3,point.y-19,6,20),orange);GUI.matrix=matrix;GUIUtility.RotateAroundPivot((onScreen?0:angle)+45,point);Box(new Rect(point.x-3,point.y-19,6,20),orange);GUI.matrix=matrix;
 int distance=Mathf.RoundToInt(Vector3.Distance(bike.transform.position,race.PlayerCourse[passed].transform.position));
 Box(new Rect(point.x-105,point.y+23,210,37),new Color(.045f,.065f,.085f,.85f));Label(new Rect(point.x-105,point.y+23,210,37),"다음 지점 · "+distance+" m",22,cream,TextAnchor.MiddleCenter);
 }
 }else Label(new Rect(width*.5f-200,height*.2f,400,45),"완주 · 상대 도착 대기",28,cream,TextAnchor.MiddleCenter);
 GUI.matrix=previous;GUI.color=color;
 }
}
}
