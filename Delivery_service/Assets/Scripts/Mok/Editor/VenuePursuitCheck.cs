#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Collections;using UnityEngine;using UnityEngine.AI;using UnityEditor;
namespace DeliveryService.Mok.Editor {
public static class VenuePursuitCheck {
 static string log;static void Check(bool ok,string message){log+=(ok?"PASS ":"FAIL ")+message+"\n";File.WriteAllText("Logs/Venue-pursuit-check.txt",log);if(!ok)throw new Exception(message);}
 static IEnumerator Wait(float seconds){float until=Time.time+seconds;while(Time.time<until)yield return null;}
 public static IEnumerator Verify(){log="Venue outdoor pursuit\n";yield return Wait(.5f);var m=VenueFinale.Current;var chase=m.streetPursuit;var b=m.preparation.preparation.delivery.broker;var recon=m.preparation.preparation;StoryPhoneInbox.Current.AutoOpenEnabled=false;
 typeof(BrokerMission).GetProperty("State").SetValue(b,BrokerMission.Phase.Complete);typeof(SpecialOrderMission).GetProperty("State").SetValue(recon.delivery,SpecialOrderMission.Phase.Complete);typeof(BackdoorMission).GetProperty("State").SetValue(recon,BackdoorMission.Phase.Complete);typeof(MechanicMission).GetProperty("State").SetValue(m.preparation,MechanicMission.Phase.Complete);b.day.EnterNightPreparation();typeof(VenueFinale).GetProperty("EventDay").SetValue(m,b.day.currentDay);Check(m.Begin(),"mission ready for pursuit");m.SkipCinematic();
 typeof(VenueFinale).GetProperty("State").SetValue(m,VenueFinale.Phase.StreetEscape);m.PlacePlayer(m.streetArrival);chase.Begin();DeveloperCheats.SetInvincible(true);var move=m.Player.GetComponent<PlayerMove>();move.enabled=false;
 Check(Vector3.Distance(m.entry.position,m.streetArrival.position)<.01f,"same entry and exit city coordinates");Check(m.alleyRoute.Length>=4,"authored street route to car");Check(chase.WavesStarted==1&&m.Enemies.Length==4,"first four enemies spawn");var first=m.Enemies[0];Vector3 start=first.transform.position;bool movedAnimation=false;float until=Time.time+2;while(Time.time<until){movedAnimation|=first.GetComponentInChildren<Animator>().GetFloat("MoveSpeed")>.1f;yield return null;}Check(Vector3.Distance(first.transform.position,start)>.3f&&first.IsAggressive,"street enemies actively pursue player");Check(movedAnimation,"pursuit has moving animation before reaching attack range");
 Time.timeScale=0;int waves=chase.WavesStarted;for(int i=0;i<8;i++)yield return null;Check(chase.WavesStarted==waves,"paused game does not add waves");Time.timeScale=1;
 m.PlacePlayer(chase.secondTrigger);yield return Wait(1);Check(chase.WavesStarted==2&&m.Enemies.Length==8,"middle interception adds four enemies");yield return Wait(3);m.PlacePlayer(chase.thirdTrigger);yield return Wait(.3f);Check(chase.WavesStarted==3&&m.Enemies.Length==12,"last interception reaches twelve enemies");
 foreach(var enemy in m.Enemies){var agent=enemy.GetComponent<NavMeshAgent>();Check((agent.enabled&&agent.isOnNavMesh)||enemy.Npc.IsKnockedDown||enemy.Npc.IsDead,"pursuer navigates or legitimately ragdolls: enabled="+agent.enabled+" nav="+agent.isOnNavMesh+" down="+enemy.Npc.IsKnockedDown+" dead="+enemy.Npc.IsDead);}BrokerMissionCheck.Capture(b,"Venue-street-pursuit");
 Check(m.passenger.GetComponentsInChildren<SkinnedMeshRenderer>(true).Any(r=>AssetDatabase.GetAssetPath(r.sharedMesh).Contains("PlayerVenuePassenger")),"boarding stand-in uses hoodie player model");
 m.Cancel();yield return null;Check(m.Enemies.Length==0&&chase.WavesStarted==0,"cancelling removes all pursuit waves");Check(m.Begin(),"can retry after pursuit cancellation");Check(chase.WavesStarted==0,"retry has no leftover pursuit state");m.Cancel();DeveloperCheats.SetInvincible(false);move.enabled=true;
 }
}
}
#endif
