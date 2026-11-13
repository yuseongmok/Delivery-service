using UnityEngine;
namespace DeliveryService.Mok {
 public sealed class ChickenInteraction:MonoBehaviour,IInteractable {
  public PrisonChickenMission mission;
  public int ingredient=-1;
  public bool entrance;
  public void Interact(ToppingInventory inventory){if(mission)mission.Use(this);}
 }
}
