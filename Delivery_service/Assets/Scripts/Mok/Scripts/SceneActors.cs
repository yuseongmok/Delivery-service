using System.Collections.Generic;
using UnityEngine;
namespace DeliveryService.Mok {
// 생성·삭제만 목록에 반영하고, 활성 상태는 조회 순간에 확인한다.
public static class SceneActors {
 public static IEnumerable<PoliceControl> Police => Store<PoliceControl>.Active;
 public static IEnumerable<RobberAI> Robbers => Store<RobberAI>.Active;
 public static IEnumerable<BikePizzaStorage> Cargo => Store<BikePizzaStorage>.Active;
 public static void Register<T>(T value) where T:Component => Store<T>.Register(value);
 public static void Remove<T>(T value) where T:Component => Store<T>.Remove(value);
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
 static void Reset(){Store<PoliceControl>.Reset();Store<RobberAI>.Reset();Store<BikePizzaStorage>.Reset();}
 static class Store<T> where T:Component {
  static readonly List<T> items=new List<T>();static bool initialized;
  public static void Reset(){items.Clear();initialized=false;}
  public static void Register(T value){if(initialized&&!items.Contains(value))items.Add(value);}
  public static void Remove(T value){if(initialized)items.Remove(value);}
  public static IEnumerable<T> Active {get {
   // 첫 조회는 이미 존재하는 비활성 오브젝트까지 포함한다. 이후 씬 전체 검색은 하지 않는다.
   if(!initialized){items.Clear();items.AddRange(Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None));initialized=true;}
   foreach(var item in items)if(item!=null&&item.gameObject.activeInHierarchy)yield return item;
  }}
 }
}
}
