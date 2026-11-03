#if UNITY_EDITOR
using System;using System.Collections;using System.Linq;using System.Reflection;using UnityEngine;using UnityEngine.AI;
namespace DeliveryService.Mok.Editor {
public static class MeleeCombatCheck {
 static void Set(object o,string n,object v)=>o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,v);
 public static IEnumerator Verify(Action<bool,string> check){
  yield return null;yield return null;
  var d=UnityEngine.Object.FindFirstObjectByType<BankPrologue>();d.autoEnterGame=false;d.AcceptInput=false;d.player.enabled=false;
  Set(d,"skip",true);while(d.Current==BankPrologue.Phase.Opening)yield return null;
  Set(d,"waveTime",Time.time+1000);foreach(var e in d.officers)e.Stop();
  var enemy=d.officers[0];enemy.Activate();enemy.GetComponent<NavMeshAgent>().Warp(new Vector3(0,.05f,7));d.player.Teleport(new Vector3(0,.05f,2));
  int health=d.player.Health;float deadline=Time.time+1;
  while(Time.time<deadline)yield return null;check(d.player.Health==health,"enemy cannot damage player from firearm distance");
  deadline=Time.time+8;while(d.player.Health==health&&Time.time<deadline)yield return null;
  check(d.player.Health<health&&d.player.KnockedDown,"enemy approaches and connects a delayed melee attack");enemy.Stop();
  deadline=Time.time+4;while(d.player.KnockedDown&&Time.time<deadline)yield return null;
  enemy.enabled=false;enemy.Activate();enemy.GetComponent<NavMeshAgent>().Warp(new Vector3(0,.05f,4.5f));enemy.GetComponent<NavMeshAgent>().isStopped=true;d.player.Teleport(new Vector3(0,.05f,2));d.player.view.transform.LookAt(enemy.transform.position+Vector3.up*1.2f);
  var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=new Vector3(0,1.2f,3.2f);wall.transform.localScale=new Vector3(4,3,.3f);Physics.SyncTransforms();health=enemy.Health;
  check(d.player.Fire(),"swing can start facing a wall");deadline=Time.time+.4f;while(Time.time<deadline)yield return null;check(enemy.Health==health&&!enemy.ragdoll.IsDown,"bat cannot hit an enemy through a wall");UnityEngine.Object.Destroy(wall);
  check(d.player.weapon.name=="야구배트"&&!d.player.gunFeedback&&!d.player.muzzle&&string.IsNullOrEmpty(d.ammo.text),"saved player bat replaces gun and ammunition HUD");
  check(d.officers.All(e=>e.gun&&!e.gun.gameObject.activeSelf&&!e.gunFeedback),"all saved prologue enemies are unarmed");
  deadline=Time.time+1;while(Time.time<deadline)yield return null;
  d.player.weapon.SetActive(true);
  check(d.player.Fire(true),"right-button heavy attack starts");
  deadline=Time.time+.45f;while(Time.time<deadline){typeof(ProloguePlayer).GetMethod("ApplyWeaponRecoil",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(d.player,null);yield return null;}
  check(d.player.weapon.GetComponentInChildren<TrailRenderer>().emitting,"trail emits during heavy contact window");
  typeof(BankPrologueCheck).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{d.player.view,"bat-heavy-contact"});
  check(enemy.Health==health-55&&enemy.ragdoll.IsDown,"overhead heavy attack deals stronger damage at contact");
  check(d.player.weapon.GetComponent<BatSwingVisual>(),"bat has swing trail effect");
  check(WeaponCatalog.UsesHeavySecondary(WeaponKind.Bat)&&!WeaponCatalog.UsesHeavySecondary(WeaponKind.Brick)&&!WeaponCatalog.UsesHeavySecondary(WeaponKind.Tuna),"secondary action differs by weapon");
  check(UnityEngine.Object.FindObjectsByType<PrologueBreakableBarrier>(FindObjectsSortMode.None).Length==0,"wooden road barriers removed from saved scene");
  check(WeaponCatalog.Names.Length==3&&WeaponCatalog.Model(WeaponKind.Pistol)==WeaponCatalog.Model(WeaponKind.Bat),"retired gun identifiers safely resolve to melee gear");
 }
}}
#endif
