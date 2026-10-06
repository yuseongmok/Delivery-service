using UnityEngine;

[CreateAssetMenu(fileName = "NewTopping", menuName = "Pizza/Topping Data")]
public class PizzaToppingData : ScriptableObject
{
    [Header("이름")]
    public string toppingName;

    [Header("UI아이콘")]
    public Sprite icon;

    [Header("실제 오브젝트")]
    public GameObject toppingPrefab;

    [Header("스폰 갯수")]
    public int spawnCount = 1;

    [Header("타입")]
    public ToppingType toppingType;

    [Header("원가")]  // 박진웅이 추가
    public int cost = 5;

    [Header("판매가")]
    public int sellingPrice;
}

public enum ToppingType
{
    Dough,
    Sauce,
    Cheese,
    Pepper,
    Pepperoni,
    Chocolate,
    Jelly,
    Pineapple,
}