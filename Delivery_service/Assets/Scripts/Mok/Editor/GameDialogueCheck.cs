#if UNITY_EDITOR
using System;using System.Collections;using System.IO;using System.Linq;using UnityEngine;using UnityEditor;
namespace DeliveryService.Mok.Editor {
public static class GameDialogueCheck {
 static string log;static void Check(bool ok,string message){log+=(ok?"PASS ":"FAIL ")+message+"\n";File.WriteAllText("Logs/Dialogue-play-check.txt",log);if(!ok)throw new Exception(message);}
 static IEnumerator Wait(float seconds){float until=Time.time+seconds;while(Time.time<until)yield return null;}
 public static IEnumerator Verify(){
  log="Editable dialogue verification\n";yield return Wait(.5f);var book=GameDialogue.Current;var f=VenueFinale.Current;var inbox=StoryPhoneInbox.Current;inbox.AutoOpenEnabled=false;
  Check(book&&book.entries.Count>200,"dialogue catalog is available as build resource");
  var sentence=f.overheard[0];var entry=book.entries.Single(e=>e.original==sentence);var mail=book.entries.Single(e=>e.original==inbox.invitation);string old=entry.text,oldMail=mail.text;
  try{
   entry.text="조직원: 편집 확인 첫 줄\n두 번째 줄도 표시됩니다.";mail.text="수정된 실장 문자입니다. 내일 밤에 만나자.";EditorUtility.SetDirty(book);AssetDatabase.SaveAssets();GameDialogue.Invalidate();
   Check(File.ReadAllText(AssetDatabase.GetAssetPath(book)).Length>0&&EditorUtility.IsDirty(book)==false,"edited dialogue asset saves to disk");
   Check(GameDialogue.Resolve(sentence)==entry.text,"saved override resolves");Check(GameDialogue.Resolve(sentence+"\n[F] 계속")==entry.text+"\n[F] 계속","combined subtitle preserves interaction prompt");
   var first=book.entries.First(e=>e.original!=sentence&&e.original!=inbox.invitation);string previous=first.text;try{first.text=sentence;GameDialogue.Invalidate();Check(GameDialogue.Resolve(first.original)==sentence,"replacement text is not recursively substituted");}finally{first.text=previous;GameDialogue.Invalidate();}
   BrokerMission.Current.DeveloperComplete();SpecialOrderMission.Current.DeveloperComplete();BackdoorMission.Current.DeveloperComplete();MechanicMission.Current.DeveloperComplete();Check(f.DeveloperStart(),"mission still starts with edited dialogue");f.SkipCinematic();f.SecurityWarning(sentence);yield return Wait(.15f);
   Check(f.subtitle.text.Contains("편집 확인 첫 줄")&&f.subtitle.text.Contains("두 번째 줄"),"live mission subtitle displays multiline edit");Check(f.overheard[0]==sentence,"mission source and progression strings stay unchanged");
   entry.text="";GameDialogue.Invalidate();f.SecurityWarning(sentence);yield return Wait(.15f);Check(!f.subtitle.text.Contains("편집 확인")&&!f.subtitle.text.Contains(sentence),"empty override hides sentence");entry.text=old;GameDialogue.Invalidate();f.SecurityWarning(sentence);yield return Wait(.15f);Check(f.subtitle.text.Contains(GameDialogue.Resolve(sentence)),"restoring original refreshes current label");f.Cancel();
   inbox.SendFrom("실장","편집 검사",inbox.invitation);var msg=inbox.Messages[0];var planner=UnityEngine.Object.FindFirstObjectByType<DeliveryService.Mok.Navigation.NavigationPlanner>();Check(planner.OpenIncoming(msg),"incoming message opens normally");yield return Wait(.3f);var app=UnityEngine.Object.FindFirstObjectByType<StoryPhoneApp>(FindObjectsInactive.Include);Check(app.body.text.Contains(mail.text),"phone message body uses edited text");Check(msg.body==inbox.invitation,"message data and read detection retain original identity");BrokerMissionCheck.Capture(BrokerMission.Current,"Dialogue-edited-phone");UnityEngine.Object.FindFirstObjectByType<PhoneController>().ClosePhone();
  }finally{entry.text=old;mail.text=oldMail;GameDialogue.Invalidate();EditorUtility.SetDirty(book);AssetDatabase.SaveAssets();if(f.Running)f.Cancel();}
  Check(book.entries.All(e=>e!=entry||e.text==old),"test restores user's existing dialogue edits");
 }
}
}
#endif
