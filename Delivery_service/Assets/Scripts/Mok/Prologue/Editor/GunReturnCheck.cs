#if UNITY_EDITOR
using System;using System.Collections;using System.Linq;using System.Reflection;using UnityEngine;using UnityEngine.AI;
namespace DeliveryService.Mok.Editor {
public static class GunReturnCheck {
 static void Set(object o,string n,object value)=>o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,value);
 public static IEnumerator VerifyMain(Action<bool,string> check){
  yield return null;yield return null;
  var player=UnityEngine.Object.FindFirstObjectByType<PlayerCombat>();var weapon=player.GetComponent<PlayerWeapon>();var money=MoneyManager.Instance;money.currentMoney=100000;
  check(weapon.Purchase(WeaponKind.Pistol)&&weapon.Equipped==WeaponKind.Pistol,"main-game pistol purchase equips gun");check(weapon.BuyAmmo()&&weapon.Ammo==10,"separate ammunition purchase adds ten rounds");
  var template=UnityEngine.Object.FindObjectsByType<CitizenCombat>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(e=>e.missionEnemy);
  check(!template.usesFirearm&&!template.GetComponent<GangFirearm>(),"original melee mission template stays unarmed");
  var victim=UnityEngine.Object.Instantiate(template);victim.gameObject.SetActive(true);victim.Provoke(player);check(!victim.GetComponent<GangFirearm>(),"provoking melee mission enemy does not grant a firearm");victim.enabled=false;victim.GetComponent<NPCControl>().enabled=false;victim.GetComponent<NavMeshAgent>().enabled=false;victim.transform.position=new Vector3(0,100,0);
  var controller=player.GetComponent<CharacterController>();controller.enabled=false;player.transform.position=new Vector3(0,100,-3);player.View.transform.LookAt(victim.transform.position+Vector3.up*1.1f);Physics.SyncTransforms();
  var stance=player.GetComponent<PlayerTacticalStance>();if(stance){stance.enabled=false;Set(stance,"<AimAmount>k__BackingField",1f);}weapon.UpdateHeldPose();
  typeof(PlayerWeapon).GetMethod("Shoot",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(weapon,null);yield return null;
  check(victim.Health==45&&victim.Npc.IsKnockedDown&&victim.GetComponent<StunStars>().Visible,"main pistol body shot deals 55 with stun stars");UnityEngine.Object.Destroy(victim.gameObject);
  victim=UnityEngine.Object.Instantiate(template);victim.gameObject.SetActive(true);victim.Provoke(player);check(!victim.GetComponent<GangFirearm>(),"provoking melee mission enemy does not grant a firearm");victim.enabled=false;victim.GetComponent<NPCControl>().enabled=false;victim.GetComponent<NavMeshAgent>().enabled=false;victim.transform.position=new Vector3(0,100,0);yield return null;
  var head=victim.GetComponentInChildren<Animator>().GetBoneTransform(HumanBodyBones.Head);player.View.transform.LookAt(head.position);Physics.SyncTransforms();typeof(PlayerWeapon).GetMethod("Shoot",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(weapon,null);yield return null;
  check(victim.Health==0&&victim.Npc.IsDead&&victim.GetComponent<StunStars>().Visible,"main pistol headshot doubles damage and leaves stun stars");UnityEngine.Object.Destroy(victim.gameObject);
 }
 public static IEnumerator Verify(Action<bool,string> check){
  yield return null;yield return null;var d=UnityEngine.Object.FindFirstObjectByType<BankPrologue>();d.autoEnterGame=false;d.AcceptInput=false;d.player.enabled=false;Set(d,"skip",true);while(d.Current==BankPrologue.Phase.Opening)yield return null;
  Set(d,"waveTime",Time.time+1000);foreach(var e in d.officers)e.Stop();
  var enemy=d.officers[2];enemy.Activate();enemy.enabled=false;enemy.GetComponent<NavMeshAgent>().Warp(new Vector3(0,.05f,5));enemy.GetComponent<NavMeshAgent>().isStopped=true;d.player.Teleport(new Vector3(0,.05f,2));Physics.SyncTransforms();yield return null;
  var stance=d.player.GetComponent<PlayerTacticalStance>();stance.enabled=false;Set(stance,"<AimAmount>k__BackingField",1f);check(stance.MoveMultiplier<.6f,"aiming reduces movement to 55 percent");
  var bystander=d.officers[9];bystander.Activate();bystander.enabled=false;bystander.GetComponent<NavMeshAgent>().Warp(new Vector3(0,.05f,6.2f));bystander.GetComponent<NavMeshAgent>().isStopped=true;
  d.player.view.transform.LookAt(enemy.transform.position+Vector3.up*1.1f);int ammo=d.player.Ammo;
  check(d.player.Fire()&&!d.player.Fire(),"rifle shoots and respects fire interval");yield return null;
  check(d.player.Ammo==ammo-1&&enemy.Health==35&&enemy.ragdoll.IsDown,"body shot deals 35 and knocks gang down");
  check(enemy.GetComponent<StunStars>().Visible,"shot victim shows orbiting stars");
  check(d.player.Reload(),"rifle reload starts");float end=Time.time+4;while((d.player.Reloading||enemy.ragdoll.IsDown)&&Time.time<end)yield return null;
  check(d.player.Ammo==d.player.magazineSize&&!enemy.ragdoll.IsDown,"reload completes and surviving gang gets up");
  check(!bystander.ragdoll.IsDown&&bystander.Health==70,"flying gang does not trigger chain damage or ragdoll");
  enemy.GetComponent<NavMeshAgent>().Warp(new Vector3(0,.05f,5));Physics.SyncTransforms();yield return null;var head=enemy.animator.GetBoneTransform(HumanBodyBones.Head);d.player.view.transform.LookAt(head.position);yield return null;Physics.SyncTransforms();
  bool got=FirearmBallistics.Cast(d.player.view.transform.position,d.player.view.transform.forward,10,d.player.transform,out var headHit);
  System.IO.File.WriteAllText("Logs/GunReturn-head-debug.txt",$"camera {d.player.view.transform.position} head {head.position} hit {(got?headHit.collider.name:"none")} point {headHit.point} headboxes {enemy.GetComponentsInChildren<HeadHitbox>().Length}");
  check(got&&FirearmBallistics.Headshot(headHit),"ray hit near head is identified as headshot");
  check(d.player.Fire(),"headshot fires");yield return null;check(enemy.Health==0&&enemy.Dead&&enemy.ragdoll.IsDown,"headshot incapacitates remaining-health enemy");
  float hip=0,aim=0;for(int i=0;i<300;i++){hip+=Vector3.Angle(d.player.view.transform.forward,FirearmBallistics.Direction(d.player.view,0));aim+=Vector3.Angle(d.player.view.transform.forward,FirearmBallistics.Direction(d.player.view,1));}check(hip>aim*10&&hip>300,"hip fire has substantially wider bullet spread");
  var shooter=d.officers[4];shooter.Activate();shooter.GetComponent<NavMeshAgent>().Warp(new Vector3(0,.05f,8));d.player.view.transform.LookAt(shooter.transform.position+Vector3.up*1.3f);end=Time.time+8;while(shooter.ShotsFired==0&&Time.time<end)yield return null;check(shooter.ShotsFired>0,"gang approaches and shoots player");
  end=Time.time+.15f;while(Time.time<end)yield return null;
  var hand=shooter.animator.GetBoneTransform(HumanBodyBones.RightHand);check(Vector3.Distance(shooter.gun.position,hand.position)<.34f,"gang pistol grip stays within palm offset of right hand");check(shooter.gun.parent==shooter.transform,"gun avoids imported hand-bone scale");
  foreach(var other in d.officers)if(other!=shooter)other.gameObject.SetActive(false);
  shooter.enabled=false;shooter.GetComponent<NavMeshAgent>().isStopped=true;
  shooter.animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
  d.filmCamera.transform.position=shooter.transform.position+shooter.transform.forward*1.4f+shooter.transform.right*.8f+Vector3.up*1.5f;d.filmCamera.transform.LookAt(hand.position);
  typeof(BankPrologueCheck).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{d.filmCamera,"gun-return-pose"});
  foreach(float angle in new[]{55f,75f,95f}){ArmedHandPose.FingerCurl=angle;var capture=d.gameObject.AddComponent<GripFrameCapture>();capture.shooter=shooter;capture.director=d;capture.angle=angle;while(capture&&!capture.done)yield return null;UnityEngine.Object.Destroy(capture);}
  ArmedHandPose.FingerCurl=75;
  ArmedHandPose.WristPitch=25;
  check(WeaponCatalog.Model(WeaponKind.Pistol)&&WeaponCatalog.Names.Length==4,"pistol resource and shop catalog restored");
 }
}
[DefaultExecutionOrder(30000)] public sealed class GripFrameCapture:MonoBehaviour {
 public PrologueOfficer shooter;public BankPrologue director;public float angle;public bool done;
 void LateUpdate(){if(done||!shooter)return;var hand=shooter.animator.GetBoneTransform(HumanBodyBones.RightHand);ArmedHandPose.Apply(shooter.animator,shooter.gun,director.player.view.transform.position,true,false);var camera=director.filmCamera;camera.transform.position=shooter.transform.position+shooter.transform.forward*1.4f+shooter.transform.right*.8f+Vector3.up*1.5f;camera.transform.LookAt(hand.position);typeof(BankPrologueCheck).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{camera,"grip-"+angle});var grip=shooter.animator.GetComponent<SyntyGripFingers>();if(grip)System.IO.File.WriteAllText("Logs/Grip-joints.txt",string.Join("\n",grip.joints.Select(t=>t.name+" "+shooter.gun.InverseTransformPoint(t.position).ToString("F4"))));camera.transform.position=shooter.transform.position+shooter.transform.forward*2.2f+Vector3.up*1.55f;camera.transform.LookAt(hand.position);typeof(BankPrologueCheck).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{camera,"grip-front"});done=true;}
}
}
#endif
