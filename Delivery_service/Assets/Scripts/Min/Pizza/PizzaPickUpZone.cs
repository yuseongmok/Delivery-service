using System.Collections.Generic;
using UnityEngine;

public class PizzaPickUpZone : MonoBehaviour, IInteractable
{
    [Header("포장 피자가 쌓일 위치")]
    [SerializeField] private Transform placePoint;

    [Header("픽업존 전시대용 상자 프리팹 (선택)")]
    [SerializeField] private GameObject packagedPizzaPrefab;

    [Header("피자 사이 높이")]
    [SerializeField] private float stackHeight = 0.05f;

    private List<PizzaData> packagedDataList = new List<PizzaData>();
    private List<GameObject> visualBoxes = new List<GameObject>();

    public void AddPackagedPizzaData(PizzaData data)
    {
        if (data == null) return;

        packagedDataList.Add(data);

        // 픽업존 위 상자 오브젝트 생성
        if (packagedPizzaPrefab != null && placePoint != null)
        {
            GameObject newBox = Instantiate(packagedPizzaPrefab, placePoint);
            newBox.transform.localPosition = new Vector3(0f, stackHeight * visualBoxes.Count, 0f);
            newBox.transform.localRotation = Quaternion.identity;
            visualBoxes.Add(newBox);
        }

        Debug.Log($"픽업존 피자 현재 수량: {packagedDataList.Count}");
    }

    public void Interact(ToppingInventory inventory)
    {
        if (packagedDataList.Count == 0)
        {
            Debug.Log("가져갈 포장된 피자가 없습니다.");
            return;
        }

        if (inventory.HasItem())
        {
            Debug.Log("손에 이미 다른 아이템을 들고 있습니다.");
            return;
        }

        // 인벤토리에 Pure Data 전달
        inventory.AddPackagedDataStack(new List<PizzaData>(packagedDataList));

        foreach (GameObject box in visualBoxes)
        {
            if (box != null) Destroy(box);
        }
        visualBoxes.Clear();
        packagedDataList.Clear();
    }
}