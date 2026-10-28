#if UNITY_EDITOR
using System.IO;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;
namespace DeliveryService.Mok.Editor {
public static class VenueHomecomingEdit {
 public static void Apply(){var m=Object.FindFirstObjectByType<VenueFinale>();var k=m.preparation.preparation.delivery.kitchen;var inbox=Object.FindFirstObjectByType<StoryPhoneInbox>();EditorSceneManager.SaveScene(m.gameObject.scene,"Logs/Before-homecoming-"+System.DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
 inbox.invitation="나 실장이야 우리 조직 움직임이 이상해 놈들이 날 없애려고 해\n내일 밤에 표시한 장소로 와 민기와 너 말고는 아무에게도 말하지 말고.";
 m.closing[2]="민기: 내가 믿고 널 끌어들였어. 미안하다. …여기선 잠깐 쉬어. 앞으로 어떻게 할지 같이 생각하자.";
 EditorUtility.SetDirty(m);EditorUtility.SetDirty(inbox);EditorSceneManager.MarkSceneDirty(m.gameObject.scene);EditorSceneManager.SaveScene(m.gameObject.scene);
 File.WriteAllText("Logs/Homecoming-positions.txt","safe="+m.safePoint.position+" rot="+m.safePoint.eulerAngles+" inside="+k.insideStand.position+" rot="+k.insideStand.eulerAngles+" return="+m.preparation.preparation.delivery.returnPoint.position+" actor="+k.actor.position);
 }
}
}
#endif
