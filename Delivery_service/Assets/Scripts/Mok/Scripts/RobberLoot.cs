using System.Collections.Generic;
using UnityEngine;
namespace DeliveryService.Mok {
public sealed class RobberLoot : MonoBehaviour, IInteractable {
    readonly List<PizzaData> pizzas=new List<PizzaData>();
    int cash;Material cashMaterial;
    public int Cash => cash;
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
    public static RobberLoot CreateCash(Vector3 position,int amount){
        var root=new GameObject("되찾을 현금",typeof(BoxCollider));root.transform.position=position+Vector3.up*.12f;
        int layer=LayerMask.NameToLayer("Interactable");root.layer=layer>=0?layer:0;
        var loot=root.AddComponent<RobberLoot>();loot.cash=amount;
        var visual=GameObject.CreatePrimitive(PrimitiveType.Cube);visual.name="지갑";visual.transform.SetParent(root.transform,false);visual.transform.localScale=new Vector3(.35f,.14f,.25f);visual.layer=root.layer;Destroy(visual.GetComponent<Collider>());
        loot.cashMaterial=new Material(Shader.Find("Universal Render Pipeline/Lit"));loot.cashMaterial.color=new Color(.22f,.35f,.22f);visual.GetComponent<Renderer>().sharedMaterial=loot.cashMaterial;
        root.GetComponent<BoxCollider>().size=new Vector3(.55f,.3f,.45f);return loot;
    }
    void OnDestroy(){if(cashMaterial!=null)Destroy(cashMaterial);}
    public void Interact(ToppingInventory inventory) {
        if(cash>0&&MoneyManager.Instance!=null){int amount=cash;cash=0;MoneyManager.Instance.AddMoney(amount);Destroy(gameObject);return;}
        if(inventory==null || inventory.HasItem() || pizzas.Count==0)return;
        inventory.AddPackagedDataStack(new List<PizzaData>(pizzas));pizzas.Clear();Destroy(gameObject);
    }
    void OnGUI() {
        if(Time.timeScale<=0||Cursor.lockState!=CursorLockMode.Locked)return;
        var player=FindFirstObjectByType<PlayerCombat>();if(player==null||!player.CanAct||player.View==null)return;
        var camera=player.View;Vector3 point=camera.WorldToScreenPoint(transform.position+Vector3.up*.35f);
        if(point.z<=0||Vector3.Distance(player.transform.position,transform.position)>12)return;
        var style=new GUIStyle(GUI.skin.label){font=MokUIFont.Font,fontSize=18,alignment=TextAnchor.MiddleCenter};style.normal.textColor=new Color(1,.85f,.35f);
        GUI.Label(new Rect(point.x-160,Screen.height-point.y-25,320,50),cash>0?$"되찾을 현금 {cash:N0}원 · F로 줍기":player.GetComponent<ToppingInventory>().HasItem()?"되찾을 피자 · 손을 비워주세요":"되찾을 피자 · F로 줍기",style);
    }
}}

