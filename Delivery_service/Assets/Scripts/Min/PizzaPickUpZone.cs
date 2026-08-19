using System.Collections.Generic;
using UnityEngine;

public class PizzaPickUpZone : MonoBehaviour, IInteractable
{
    [Header("포장된 피자가 쌓일 위치")]
    [SerializeField] private Transform placePoint;

    [Header("포장 용기")]
    [SerializeField] private GameObject packagedPizzaPrefab;

    [Header("피자 사이 높이")]
    [SerializeField] private float stackHeight = 0.05f;

    private List<GameObject> packagedPizzas = new List<GameObject>();

    public void AddPackagedPizza(GameObject pizza)
    {
        if (pizza == null)
            return;

        Destroy(pizza);

        if (packagedPizzaPrefab == null)
            return;

        GameObject newPizza = Instantiate(packagedPizzaPrefab, placePoint);
        newPizza.transform.localPosition = new Vector3(0f, stackHeight * packagedPizzas.Count, 0f);
        newPizza.transform.localRotation = Quaternion.identity;
        PizzaPackage package = newPizza.GetComponent<PizzaPackage>();

        if (package != null)
            package.SetPackaged(true);

        packagedPizzas.Add(newPizza);

        Debug.Log($"포장된 피자 추가 현재 개수 : {packagedPizzas.Count}");
    }

    public void Interact(ToppingInventory inventory)
    {
        if (packagedPizzas.Count == 0)
        {
            Debug.Log("가져갈 포장된 피자가 없습니다.");
            return;
        }

        int count = packagedPizzas.Count;

        foreach (GameObject pizza in packagedPizzas)
        {
            if (pizza != null)
            {
                Destroy(pizza);
            }
        }

        packagedPizzas.Clear();

        Debug.Log($"포장된 피자 {count}개를 모두 가져갔습니다.");
    }
}
