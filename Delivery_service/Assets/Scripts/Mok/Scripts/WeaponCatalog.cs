using UnityEngine;
namespace DeliveryService.Mok {
public enum WeaponKind { None=-1, Bat, Tuna, Brick, Pistol }
public static class WeaponCatalog {
 public static readonly string[] Names={"야구방망이","냉동참치","벽돌","권총"};
 public static readonly int[] Prices={8000,12000,2000,25000};
 public static readonly int[] Damage={45,40,35,55};
 public static readonly float[] Reach={3.3f,3.3f,1.8f,45f};
 public static readonly float[] Cooldown={.85f,1.5f,.85f,.35f};
 public static readonly string[] Description={"좌클릭 가로 휘두르기 · 우클릭 강공격","긴 사거리 · 넓은 휘두르기 · 느린 속도","짧은 내리치기 · 우클릭 투척 · 다시 회수 가능","원거리 사격 · 우클릭 조준 · 탄약 별도 구매"};
 public static bool UsesHeavySecondary(WeaponKind kind)=>kind==WeaponKind.Bat;
 public static GameObject Model(WeaponKind kind) => Resources.Load<GameObject>("MokWeapons/"+kind);
}
}
