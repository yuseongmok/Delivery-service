using UnityEngine;

namespace DeliveryService.Mok
{
    // 외형만 담당하며 레시피와 굽기 상태는 Pizza가 관리한다.
    public sealed class PizzaStageVisual : MonoBehaviour
    {
        Transform model;
        bool sauce, cheese;
        public bool Ready => model != null;
        public void Initialize()
        {
            if (model != null) return;
            var prefab = Resources.Load<GameObject>("MokPizzaArt/PizzaStages");
            if (prefab == null) return;
            foreach (var renderer in GetComponentsInChildren<Renderer>()) renderer.enabled = false;
            model = Instantiate(prefab, transform, false).transform;
            model.name = "Mok Pizza Stages";
            foreach (var t in model.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = gameObject.layer;
            Refresh(false);
        }
        public bool Supports(ToppingType type) => type == ToppingType.Sauce || type == ToppingType.Cheese ||
            Resources.Load<GameObject>("MokPizzaArt/" + type + "Raw") != null;
        public void AddLayer(ToppingType type, bool baked)
        {
            if (type == ToppingType.Sauce) sauce = true;
            if (type == ToppingType.Cheese) cheese = true;
            if (type != ToppingType.Sauce && type != ToppingType.Cheese && model.Find(type + "Raw") == null)
            {
                foreach (string state in new[]{"Raw", "Baked"})
                {
                    var prefab = Resources.Load<GameObject>("MokPizzaArt/" + type + state);
                    if (prefab == null) continue;
                    var visual = Instantiate(prefab, model, false); visual.name = type + state;
                    foreach(var t in visual.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = gameObject.layer;
                }
            }
            Refresh(baked);
        }
        public void ResetLayers() {
            sauce = cheese = false;
            if (Ready) foreach(ToppingType type in new[]{ToppingType.Pepper, ToppingType.Pepperoni, ToppingType.Pineapple, ToppingType.Chocolate, ToppingType.Jelly})
                foreach(string state in new[]{"Raw","Baked"}) {var child=model.Find(type+state); if(child!=null){child.gameObject.SetActive(false);Destroy(child.gameObject);}}
            Refresh(false);
        }
        public void Refresh(bool baked)
        {
            if (!Ready) return;
            foreach(ToppingType type in new[]{ToppingType.Pepper, ToppingType.Pepperoni, ToppingType.Pineapple, ToppingType.Chocolate, ToppingType.Jelly})
                foreach(string state in new[]{"Raw","Baked"}) {var child=model.Find(type+state);if(child!=null)child.gameObject.SetActive((state=="Baked")==baked);}
            model.Find("RawCrust").gameObject.SetActive(!baked);
            model.Find("BakedCrust").gameObject.SetActive(baked);
            model.Find("Sauce").gameObject.SetActive(sauce);
            model.Find("ShreddedCheese").gameObject.SetActive(cheese && !baked);
            model.Find("MeltedCheese").gameObject.SetActive(cheese && baked);
        }
    }
}
