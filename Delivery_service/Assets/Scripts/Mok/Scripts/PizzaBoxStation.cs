using System.Collections;
using UnityEngine;

namespace DeliveryService.Mok
{
    // 상자가 완전히 닫힐 때까지 실제 피자를 보여주며 레시피를 재구성하지 않는다.
    public sealed class PizzaBoxStation : MonoBehaviour
    {
        Transform box, hinge;
        Pizza containedPizza;
        bool opened, busy;
        public bool IsOpen => opened;
        public bool IsBusy => busy;
        public Pizza ContainedPizza => containedPizza;
        public bool Initialize()
        {
            if (box != null) return true;
            var prefab = Resources.Load<GameObject>("MokPizzaArt/Box");
            if (prefab == null) return false;
            foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
            box = Instantiate(prefab, transform, false).transform;
            // 기존 포장대는 납작한 큐브이므로 바닥 면적과 높이를 유지한다.
            var scale = transform.lossyScale;
            float width = Mathf.Abs(scale.x);
            box.localScale = new Vector3(1, width / Mathf.Max(.001f, Mathf.Abs(scale.y)), width / Mathf.Max(.001f, Mathf.Abs(scale.z)));
            box.localPosition = new Vector3(0, -.5f, 0);
            hinge = box.Find("LidHinge");
            foreach (var t in box.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = gameObject.layer;
            return true;
        }
        public void Interact(ToppingInventory inventory, PizzaPickUpZone pickupZone)
        {
            if (inventory == null || busy || !Initialize()) return;
            if (!opened) { StartCoroutine(SetLid(true)); return; }
            if (containedPizza != null)
            {
                if (pickupZone == null) { Debug.LogWarning("포장 피자 픽업 위치가 연결되지 않았습니다."); return; }
                StartCoroutine(ClosePackage(pickupZone));
                return;
            }
            if (!inventory.HasPizza()) { StartCoroutine(SetLid(false)); return; }
            var pizza = inventory.HeldPizza.GetComponent<Pizza>();
            if (pizza == null) return;
            inventory.RemovePizza();
            containedPizza = pizza;
            pizza.SetPlaced(false);
            pizza.SetHeld(true);
            StartCoroutine(InsertPizza(pizza.transform));
        }
        IEnumerator InsertPizza(Transform pizza)
        {
            busy = true;
            pizza.SetParent(box, true);
            var position = pizza.localPosition; var rotation = pizza.localRotation; var scale = pizza.localScale;
            for (float t = 0; t < .35f; t += Time.deltaTime)
            {
                float a = Mathf.SmoothStep(0, 1, t / .35f);
                pizza.localPosition = Vector3.Lerp(position, new Vector3(0, .065f, 0), a);
                pizza.localRotation = Quaternion.Slerp(rotation, Quaternion.identity, a);
                pizza.localScale = Vector3.Lerp(scale, Vector3.one, a);
                yield return null;
            }
            pizza.localPosition = new Vector3(0, .065f, 0); pizza.localRotation = Quaternion.identity; pizza.localScale = Vector3.one;
            busy = false;
        }
        IEnumerator SetLid(bool open, bool releaseBusy = true)
        {
            busy = true;
            var from = hinge.localRotation; var to = Quaternion.Euler(open ? 108 : 0, 0, 0);
            for (float t = 0; t < .3f; t += Time.deltaTime)
            {
                hinge.localRotation = Quaternion.Slerp(from, to, Mathf.SmoothStep(0, 1, t / .3f));
                yield return null;
            }
            hinge.localRotation = to; opened = open; busy = !releaseBusy;
        }
        IEnumerator ClosePackage(PizzaPickUpZone pickupZone)
        {
            yield return SetLid(false, false);
            if (containedPizza != null)
            {
                pickupZone.AddPackagedPizzaData(containedPizza.ToData());
                Destroy(containedPizza.gameObject); containedPizza = null;
            }
            busy = false;
        }
        void OnDisable()
        {
            StopAllCoroutines(); busy = false;
            // 닫기가 중단돼도 피자를 보존해 포장을 마칠 수 있게 한다.
            if (containedPizza != null)
            {
                opened = true;
                containedPizza.transform.localPosition = new Vector3(0, .065f, 0);
                containedPizza.transform.localRotation = Quaternion.identity;
                containedPizza.transform.localScale = Vector3.one;
            }
            if (hinge != null) hinge.localRotation = Quaternion.Euler(opened ? 108 : 0, 0, 0);
        }
        public static GameObject CreateClosedPackage(Transform point, int index)
        {
            var prefab = Resources.Load<GameObject>("MokPizzaArt/Box");
            if (prefab == null || point == null) return null;
            var instance = Instantiate(prefab, point.position + Vector3.up * (.075f * index), point.rotation);
            instance.name = "Packaged Pizza";
            instance.transform.localScale = Vector3.one * .6f;
            instance.transform.SetParent(point, true);
            return instance;
        }
    }
}
