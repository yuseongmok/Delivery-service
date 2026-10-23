using UnityEngine;
public class PlayerInteract : MonoBehaviour {
    public float interactRange=3f;
    public LayerMask motorcycleLayer;
    public Transform playerCameraTransform;
    void Update(){if(DeliveryService.Mok.PrologueKeyboard.Pressed(UnityEngine.InputSystem.Key.F))TryInteract();}
    void TryInteract(){
        var combat=GetComponent<DeliveryService.Mok.PlayerCombat>();if(combat!=null&&!combat.CanAct)return;
        Vector3 origin=playerCameraTransform!=null?playerCameraTransform.position:transform.position;
        Vector3 direction=playerCameraTransform!=null?playerCameraTransform.forward:transform.forward;
        if(!Physics.Raycast(origin,direction,out var hit,interactRange,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))return;
        FuelNozzle held=null;foreach(var n in FindObjectsByType<FuelNozzle>(FindObjectsSortMode.None))if(n.currentState==FuelNozzle.NozzleState.InHand){held=n;break;}
        var target=hit.collider;
        // 노즐을 들고 있으면 작은 주유구 대신 차체 어디를 가리켜도 연결한다.
        var bike=target.GetComponentInParent<MotorcycleController>();
        var cap=target.GetComponentInParent<FuelCap>();
        // 연결할 때와 마찬가지로 차체를 조준해서 장착된 노즐을 회수한다.
        if(bike==null&&cap!=null&&cap.bikeFuelSystem!=null)bike=cap.bikeFuelSystem.GetComponent<MotorcycleController>();
        if(held==null&&bike!=null){
            foreach(var attached in FindObjectsByType<FuelNozzle>(FindObjectsSortMode.None)){
                if(attached.currentState!=FuelNozzle.NozzleState.Refueling||attached.currentTargetCap==null)continue;
                var fuel=attached.currentTargetCap.bikeFuelSystem;
                if(fuel!=null&&fuel.GetComponent<MotorcycleController>()==bike){attached.PickUpNozzle();return;}
            }
        }
        if(held!=null&&(cap!=null||bike!=null)){
            if(cap==null)cap=bike.GetComponentInChildren<FuelCap>();
            if(cap!=null)held.AttachToBike(cap);return;
        }
        if(target.GetComponentInParent<IInteractable>()!=null)return;
        if((motorcycleLayer.value&(1<<target.gameObject.layer))==0)return;
        var nozzle=target.GetComponentInParent<FuelNozzle>();
        if(nozzle!=null){if(held==null)nozzle.PickUpNozzle();return;}
        if(target.CompareTag("GasPump")){if(held!=null)held.ReturnPump();else{var mounted=target.GetComponentInChildren<FuelNozzle>();if(mounted!=null&&mounted.currentState==FuelNozzle.NozzleState.OnPump)mounted.PickUpNozzle();}return;}
        if(bike!=null&&held==null&&!bike.isRefueling&&bike.EnterBike(gameObject))gameObject.SetActive(false);
    }
}
