using UnityEngine;
namespace DeliveryService.Mok.Navigation {
public enum NavigationPlaceKind { Pizza, Repair, Fuel, Bike, Other, Weapon }
public sealed class NavigationPlace : MonoBehaviour {
 public string placeId;
 public string displayName;
 public NavigationPlaceKind kind;
 public Transform mapAnchor;
 public Transform entrance;
 public Vector3 MapPosition => mapAnchor!=null?mapAnchor.position:transform.position;
 public Transform Destination => entrance!=null?entrance:transform;
 public string Symbol => kind==NavigationPlaceKind.Weapon?"W":kind==NavigationPlaceKind.Pizza?"P":kind==NavigationPlaceKind.Repair?"R":kind==NavigationPlaceKind.Fuel?"F":kind==NavigationPlaceKind.Bike?"B":"+";
 public Color Tint => kind==NavigationPlaceKind.Weapon?new Color(.91f,.39f,.13f):kind==NavigationPlaceKind.Pizza?new Color(.82f,.37f,.24f):kind==NavigationPlaceKind.Repair?new Color(.78f,.55f,.17f):kind==NavigationPlaceKind.Fuel?new Color(.21f,.52f,.71f):kind==NavigationPlaceKind.Bike?new Color(.51f,.35f,.75f):new Color(.26f,.52f,.44f);
 public bool IsAvailable {get{var b=GetComponent<MotorcycleController>();return kind!=NavigationPlaceKind.Bike||b==null||!b.isDriven;}}
}
}
