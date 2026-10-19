using UnityEngine;
namespace DeliveryService.Mok {
// 전시용 카메라는 시작 전환 중에는 제어권을 돌려준다.
public sealed class LobbyExhibition : MonoBehaviour {
 public LobbyController director;
 Vector3 origin; Quaternion rotation;
 void Start(){origin=director.lobbyCamera.transform.position;rotation=director.lobbyCamera.transform.rotation;}
 void LateUpdate(){if(director.IsLoading)return;float t=Time.unscaledTime;
 director.lobbyCamera.transform.position=origin+new Vector3(Mathf.Sin(t*.13f)*.045f,Mathf.Sin(t*.19f)*.018f,0);
 director.lobbyCamera.transform.rotation=rotation*Quaternion.Euler(0,Mathf.Sin(t*.11f)*.12f,0);}
}
}
