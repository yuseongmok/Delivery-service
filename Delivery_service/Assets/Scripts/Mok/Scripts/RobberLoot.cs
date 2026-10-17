using System.Collections.Generic;
using UnityEngine;
namespace DeliveryService.Mok {
public sealed class RobberLoot : MonoBehaviour, IInteractable {
    readonly List<PizzaData> pizzas=new List<PizzaData>();
    public int Count => pizzas.Count;
    public static RobberLoot Create(Vector3 position,IEnumerable<PizzaData> data) {
        var root=new GameObject("되찾을 피자",typeof(BoxCollider));root.transform.position=position+Vector3.up*.06f;
        // 기존 상호작용 레이어를 사용한다.
        int layer=LayerMask.NameToLayer("Interactable");root.layer=layer>=0?layer:0;
        var loot=root.AddComponent<RobberLoot>();loot.pizzas.AddRange(data);
        var box=PizzaBoxStation.CreateClosedPackage(root.transform,0);
        if(box!=null)foreach(var t in box.GetComponentsInChildren<Transform>(true))t.gameObject.layer=root.layer;
        root.GetComponent<BoxCollider>().center=new Vector3(0,.06f,0);root.GetComponent<BoxCollider>().size=new Vector3(.7f,.18f,.7f);
        return loot;
    }
    public void Interact(ToppingInventory inventory) {
        if(inventory==null || inventory.HasItem() || pizzas.Count==0)return;
        inventory.AddPackagedDataStack(new List<PizzaData>(pizzas));pizzas.Clear();Destroy(gameObject);
    }
    void OnGUI() {
        if(Time.timeScale<=0||Cursor.lockState!=CursorLockMode.Locked)return;
        var player=FindFirstObjectByType<PlayerCombat>();if(player==null||!player.CanAct||player.View==null)return;
        var camera=player.View;Vector3 point=camera.WorldToScreenPoint(transform.position+Vector3.up*.35f);
        if(point.z<=0||Vector3.Distance(player.transform.position,transform.position)>12)return;
        var style=new GUIStyle(GUI.skin.label){font=MokUIFont.Font,fontSize=18,alignment=TextAnchor.MiddleCenter};style.normal.textColor=new Color(1,.85f,.35f);
        GUI.Label(new Rect(point.x-160,Screen.height-point.y-25,320,50),player.GetComponent<ToppingInventory>().HasItem()?"되찾을 피자 · 손을 비워주세요":"되찾을 피자 · E로 줍기",style);
    }
}}
