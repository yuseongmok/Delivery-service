using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace DeliveryService.Mok {
// 첫 수배에서만 알려 주며 문자함에 남겨 다시 읽을 수 있게 한다.
public sealed class FirstWantedLesson : MonoBehaviour {
 public Text subtitle;public GameObject panel;
 [TextArea(3,5)] public string message="민기: 뭐 했길래 경찰한테 수배가 걸렸어!\n경찰 시야에서 벗어나 쓰레기통에 숨든, 수배가 끝날 때까지 도망치든 해!";
 public bool Shown {get;private set;}float until;
 void Awake(){panel.SetActive(false);}
 public void Notify(){if(Shown)return;Shown=true;subtitle.text=message;panel.SetActive(!(StoryPhoneInbox.Current&&StoryPhoneInbox.Current.IsMingiNearby));until=Time.time+11;StoryPhoneInbox.Current?.Send("경찰을 따돌리는 방법",message.Replace("민기: ","")+"\n경찰이 보는 앞에서 숨으면 들킬 수 있어. 먼저 시야를 끊어!");}
 void Update(){if(Time.timeScale<=0)return;if(!Shown&&SceneActors.Police.Any(p=>p.RemainingChaseSeconds>0))Notify();if(Shown&&Time.time>=until)panel.SetActive(false);}
}
}
