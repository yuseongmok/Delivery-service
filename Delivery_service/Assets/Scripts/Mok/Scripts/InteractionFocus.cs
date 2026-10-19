using UnityEngine;
namespace DeliveryService.Mok {
// 실제 입력 경로의 거리와 레이어를 사용하고 벽 너머 대상은 표시하지 않는다.
public static class InteractionFocus {
 public static Transform Find(PlayerInteraction player){
  var camera=player.InteractionCamera;if(camera==null)return null;
  if(!Physics.Raycast(camera.transform.position,camera.transform.forward,out var hit,Mathf.Max(3,player.InteractionDistance),Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))return null;
  var col=hit.collider;var combat=player.GetComponent<PlayerCombat>();
  var citizen=col.GetComponentInParent<CitizenCombat>();if(citizen!=null&&hit.distance<=2.5f&&citizen.CanBePickpocketed)return citizen.transform;
  var action=col.GetComponentInParent<IInteractable>() as Component;
  if(action!=null&&hit.distance<=player.InteractionDistance&&(player.InteractionLayers&(1<<col.gameObject.layer))!=0){
   var thrown=action as ThrownBrick;if(thrown!=null&&!thrown.CanRecover(player.gameObject))return null;
   return action.transform;
  }
  var pc=col.GetComponent<ShopPC>();if(pc!=null&&hit.distance<=pc.interactRange)return pc.transform;
  var bin=col.GetComponentInParent<HidingDumpster>();if(bin!=null&&hit.distance<=3&&Vector3.Distance(player.transform.position,bin.transform.position)<=3.5f)return bin.transform;
  var bikeInput=player.GetComponent<PlayerInteract>();
  if(bikeInput!=null&&hit.distance<=bikeInput.interactRange){
   var cap=col.GetComponentInParent<FuelCap>();if(cap!=null)return cap.transform;
   if((bikeInput.motorcycleLayer.value&(1<<col.gameObject.layer))!=0){
    var nozzle=col.GetComponentInParent<FuelNozzle>();if(nozzle!=null)return nozzle.transform;
    var bike=col.GetComponentInParent<MotorcycleController>();if(bike!=null&&!bike.isDriven)return bike.transform;
    if(col.CompareTag("GasPump"))return col.transform;
   }
  }
  return null;
 }
}
}
