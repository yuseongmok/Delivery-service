using UnityEngine;
namespace DeliveryService.Mok
{
    // 조리/배식의 감옥 전용 규칙. 도시의 PizzaMaker와 재료 조작은 그대로 사용한다.
    public sealed class PrisonPizzaService:MonoBehaviour
    {
        public PrisonKeysMission mission;
        public PizzaToppingData medicine;
        public GameObject platePrefab;
        public GameObject Meal{get;private set;}
        public bool Carrying=>Meal&&Inventory.HeldPizza==Meal;
        ToppingInventory Inventory=>mission.previous.prison.player.GetComponent<ToppingInventory>();
        void Update(){
            if(mission.State!=PrisonKeysMission.Stage.Cook)return;
            foreach(var pizza in mission.maker.GetComponentsInChildren<Pizza>()){
                if(!pizza.currentToppings.Contains(medicine.toppingName))continue;
                var dose=pizza.GetComponent<PrisonPizzaDose>();
                if(pizza.IsBaked&&!dose){pizza.currentToppings.Remove(medicine.toppingName);mission.ShowDiversionLine(mission.dialogue.medicineLate);continue;}
                if(!dose){pizza.gameObject.AddComponent<PrisonPizzaDose>();PrisonCookingAudio.Cue("pickup");mission.ShowDiversionLine(mission.dialogue.medicineAdded);}
            }
        }
        public bool Seasoned(Pizza pizza)=>pizza&&pizza.GetComponent<PrisonPizzaDose>();
        public void Plate(){
            Meal=Inventory.HeldPizza;if(!Meal)return;
            if(!Meal.transform.Find("배식용 접시")){
                var plate=Instantiate(platePrefab,Meal.transform);plate.name="배식용 접시";plate.transform.localPosition=Vector3.zero;plate.transform.localRotation=Quaternion.identity;
                foreach(var c in plate.GetComponentsInChildren<Collider>())Destroy(c);
                var renderers=plate.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
                float diameter=Mathf.Max(bounds.size.x,bounds.size.z);plate.transform.localScale*=1.05f/Mathf.Max(.001f,diameter);bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
                plate.transform.position+=Meal.transform.position-new Vector3(bounds.center.x,bounds.max.y+.025f,bounds.center.z);
            }
            Inventory.GetComponent<CarriedItemVisual>().Refresh();
        }
        public Transform Tray=>Inventory.HoldPoint;
        public void Finish(){if(Carrying)Inventory.RemovePizza();if(Meal)Destroy(Meal);Meal=null;}
    }
    public sealed class PrisonPizzaDose:MonoBehaviour{}
}
