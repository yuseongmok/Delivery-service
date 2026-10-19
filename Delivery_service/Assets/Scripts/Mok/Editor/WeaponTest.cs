#if UNITY_EDITOR
using System;using System.Collections;using System.Linq;using UnityEngine;using UnityEngine.AI;using UnityEditor;
namespace DeliveryService.Mok.Editor {
public static class WeaponTest {
 static void Check(bool b,string s,Action<string> log){if(!b)throw new Exception(s);log("PASS: "+s);}
 public static IEnumerator RunCore(Action<string> log){
 var p=UnityEngine.Object.FindFirstObjectByType<PlayerCombat>();var w=p.GetComponent<PlayerWeapon>();Check(w!=null,"Scene player owns weapon slot",log);
 var move=p.GetComponent<PlayerMove>();move.enabled=false;move.isControllable=true;foreach(var look in p.GetComponentsInChildren<CameraMove>())look.enabled=false;UnityEngine.Object.FindFirstObjectByType<PhoneController>()?.ClosePhone();foreach(var bike in UnityEngine.Object.FindObjectsByType<MotorcycleController>(FindObjectsSortMode.None))bike.isDriven=false;Cursor.lockState=CursorLockMode.Locked;
 MoneyManager.Instance.currentMoney=100000;int cash=MoneyManager.Instance.currentMoney;
 Check(w.Purchase(WeaponKind.Bat)&&MoneyManager.Instance.currentMoney==cash-8000,"Bat purchase costs 8000",log);
 Check(w.Purchase(WeaponKind.Tuna)&&w.Equipped==WeaponKind.Tuna,"Second purchase replaces only slot",log);yield return null;
 Check(p.View.GetComponentsInChildren<Transform>(true).Count(t=>t.name.StartsWith("Held "))==1,"One held model after replacement",log);
 MoneyManager.Instance.currentMoney=0;Check(!w.Purchase(WeaponKind.Pistol)&&w.Equipped==WeaponKind.Tuna,"Insufficient balance preserves weapon",log);MoneyManager.Instance.currentMoney=100000;
 Check(!w.BuyAmmo(),"Ammo requires owning pistol",log);Check(w.Purchase(WeaponKind.Pistol)&&w.Ammo==0&&w.BuyAmmo()&&w.Ammo==10,"Pistol starts empty; separate ammo purchase gives ten",log);
 float until=Time.time+.3f;while(Time.time<until)yield return null;Check(w.Attack(false)&&w.Ammo==9&&!w.Attack(false),"Shot spends one round and enforces cooldown",log);
 Cursor.lockState=CursorLockMode.None;Check(!w.Attack(false),"UI blocks attacks",log);Cursor.lockState=CursorLockMode.Locked;
 Check(w.Purchase(WeaponKind.Brick),"Brick purchase",log);until=Time.time+.3f;while(Time.time<until)yield return null;
 Check(w.Attack(true)&&w.Thrown!=null&&!w.HasInHand,"Throw removes held brick while reserving slot",log);var brick=w.Thrown;Check(w.Recover(brick)&&w.HasInHand,"Owned brick can be recovered",log);yield return null;
 until=Time.time+1;while(Time.time<until)yield return null;Check(w.Attack(true),"Recovered brick can be thrown again",log);brick=w.Thrown;w.Purchase(WeaponKind.Bat);yield return null;Check(brick==null&&w.Thrown==null,"New purchase removes previous thrown brick",log);
 Check(NavMesh.SamplePosition(new Vector3(138,0,76),out var spot,30,NavMesh.AllAreas),"Combat testing point exists",log);var cc=p.GetComponent<CharacterController>();cc.enabled=false;p.transform.position=spot.position+Vector3.forward*2.8f;p.View.transform.position=p.transform.position+Vector3.up*1.15f;p.View.transform.rotation=Quaternion.LookRotation(Vector3.back);
 var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Scripts/Min/Prefab/NPC/Male_Adult_NPC_1.prefab");
 var a=UnityEngine.Object.Instantiate(prefab,spot.position,Quaternion.identity).GetComponent<CitizenCombat>();yield return null;a.GetComponent<NavMeshAgent>().enabled=false;a.GetComponent<NPCControl>().enabled=false;a.transform.position=spot.position;Physics.SyncTransforms();
 Check(w.Resolve()==1&&a.Health==55&&a.Npc.IsKnockedDown,"Bat long range: 45 damage and ragdoll",log);UnityEngine.Object.Destroy(a.gameObject);yield return null;
 a=UnityEngine.Object.Instantiate(prefab,spot.position+Vector3.right*.65f,Quaternion.identity).GetComponent<CitizenCombat>();var b=UnityEngine.Object.Instantiate(prefab,spot.position-Vector3.right*.65f,Quaternion.identity).GetComponent<CitizenCombat>();yield return null;
 foreach(var c in new[]{a,b}){c.GetComponent<NavMeshAgent>().enabled=false;c.GetComponent<NPCControl>().enabled=false;}a.transform.position=spot.position+Vector3.right*.65f;b.transform.position=spot.position-Vector3.right*.65f;w.Equip(WeaponKind.Tuna);Physics.SyncTransforms();Check(w.Resolve()==2&&a.Health==60&&b.Health==60,"Tuna hits two distinct citizens once each",log);UnityEngine.Object.Destroy(a.gameObject);UnityEngine.Object.Destroy(b.gameObject);yield return null;
 a=UnityEngine.Object.Instantiate(prefab,spot.position,Quaternion.identity).GetComponent<CitizenCombat>();yield return null;a.GetComponent<NavMeshAgent>().enabled=false;a.GetComponent<NPCControl>().enabled=false;a.transform.position=spot.position;w.Equip(WeaponKind.Brick);Physics.SyncTransforms();Check(w.Resolve()==0&&a.Health==100,"Brick short reach cannot hit at bat distance",log);
 w.Equip(WeaponKind.Bat);var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=spot.position+new Vector3(0,1,1.5f);wall.transform.localScale=new Vector3(3,3,.2f);Physics.SyncTransforms();Check(w.Resolve()==0&&a.Health==100,"Solid wall blocks weapon damage",log);UnityEngine.Object.Destroy(wall);UnityEngine.Object.Destroy(a.gameObject);yield return null;
 var merchant=UnityEngine.Object.FindFirstObjectByType<WeaponMerchant>();Check(merchant!=null,"Saved shop merchant exists",log);Cursor.lockState=CursorLockMode.Locked;merchant.Interact(p.GetComponent<ToppingInventory>());Check(merchant.IsOpen&&Cursor.lockState==CursorLockMode.None&&!p.GetComponent<PlayerInteraction>().enabled,"Merchant interaction opens preview and suspends gameplay",log);until=Time.time+.5f;while(Time.time<until)yield return null;ScreenCapture.CaptureScreenshot("Temp/Mok-weapon-menu.png");until=Time.time+.5f;while(Time.time<until)yield return null;merchant.Close();Check(Cursor.lockState==CursorLockMode.Locked&&p.GetComponent<PlayerInteraction>().enabled,"Closing merchant restores controls",log);
 foreach(WeaponKind kind in new[]{WeaponKind.Bat,WeaponKind.Tuna,WeaponKind.Brick,WeaponKind.Pistol})Check(WeaponCatalog.Model(kind)!=null,"Saved prefab: "+kind,log);
 }
}
}
#endif
