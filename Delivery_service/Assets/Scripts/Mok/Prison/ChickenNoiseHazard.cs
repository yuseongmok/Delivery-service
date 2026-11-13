using UnityEngine;
namespace DeliveryService.Mok {
 public sealed class ChickenNoiseHazard:MonoBehaviour {
  public PrisonChickenMission mission;public bool oil;
  float next;
  void OnTriggerEnter(Collider other){
   if(Time.time<next||!other.GetComponentInParent<PlayerCombat>()||!mission||!mission.CookingActive)return;
   next=Time.time+4;mission.MakeNoise(oil);
  }
 }
}
