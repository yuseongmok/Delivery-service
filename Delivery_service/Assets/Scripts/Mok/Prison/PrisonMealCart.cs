using System.Collections;
using UnityEngine;

namespace DeliveryService.Mok
{
    [DefaultExecutionOrder(-200)]
    public sealed class PrisonMealCart : MonoBehaviour, IInteractable
    {
        public FoodCartController controller;
        public GameObject preparedMeals;
        public GameObject[] meals;
        public int Count { get; private set; }
        float stepTop;
        bool stepping;
        bool restoreCollisions;
        Collider[] cartColliders, playerColliders;
        public void ShowPrepared(bool visible) { preparedMeals.SetActive(visible); }
        public void SetMeals(int count)
        {
            Count = count;
            for (int i = 0; i < meals.Length; i++) meals[i].SetActive(i < count);
        }
        void Start() { SetMeals(0); ShowPrepared(false); controller.minKnockdownSpeed=100; cartColliders=GetComponentsInChildren<Collider>(); }
        void IgnorePlayer(bool ignore)
        {
            var mission=PrisonMission.Current;if(!mission)return;
            playerColliders??=mission.player.GetComponentsInChildren<Collider>(true);
            foreach(var a in cartColliders)foreach(var b in playerColliders)if(a&&b)Physics.IgnoreCollision(a,b,ignore);
        }
        public void StopPushing()
        {
            if(!controller.cartRigidbody.isKinematic)controller.StopPushing();
            IgnorePlayer(true);restoreCollisions=true;stepping=false;
        }
        void FixedUpdate()
        {
            var rb=controller.cartRigidbody;
            if(rb.isKinematic)return;
            if(stepping){var p=rb.position;p.y=Mathf.MoveTowards(p.y,stepTop,2*Time.fixedDeltaTime);rb.MovePosition(p);if(p.y>=stepTop-.005f)stepping=false;return;}
            // 낮은 문턱만 넘어간다. 위쪽도 막힌 벽이나 가구는 통과하지 않는다.
            var player=PrisonMission.Current.player.transform;
            var velocity=Vector3.ProjectOnPlane(player.position+player.forward*controller.holdDistance-transform.position,Vector3.up);
            if(velocity.sqrMagnitude<.04f)return;
            var direction=velocity.normalized;
            var origin=transform.position+direction*1.05f;
            if(Physics.Raycast(origin+Vector3.up*.5f,Vector3.down,out var floor,.65f,~0,QueryTriggerInteraction.Ignore)
                && !floor.transform.IsChildOf(transform) && floor.normal.y>.8f
                && floor.point.y>rb.position.y+.03f && floor.point.y<=rb.position.y+.26f
                && !Physics.Raycast(transform.position+Vector3.up*.34f,direction,1.15f,~0,QueryTriggerInteraction.Ignore))
            {stepTop=floor.point.y+.02f;stepping=true;}
        }
        void Update()
        {
            var mission=PrisonMission.Current;
            if(!mission)return;
            if(restoreCollisions){
                var bounds=mission.player.GetComponent<CharacterController>().bounds;bounds.Expand(.2f);
                bool overlap=false;foreach(var col in cartColliders)if(col&&col.enabled&&bounds.Intersects(col.bounds)){overlap=true;break;}
                if(!overlap){IgnorePlayer(false);restoreCollisions=false;}
            }
            if(controller.cartRigidbody.isKinematic||mission.Failing)return;
            // 카트가 벽에 막혀도 플레이어만 지나가 연결이 끊기지 않도록 손잡이 거리를 유지한다.
            var delta=Vector3.ProjectOnPlane(mission.player.transform.position-transform.position,Vector3.up);
            if(delta.magnitude>1.8f){var body=mission.player.GetComponent<CharacterController>();if(body&&body.enabled)body.Move(-delta.normalized*(delta.magnitude-1.8f));}
        }
        public void Interact(ToppingInventory inventory)
        {
            var mission = PrisonMission.Current;
            if (!mission || mission.Busy || mission.Failing) return;
            if (mission.State == PrisonMission.Phase.Load) mission.LoadCart();
            else if(!controller.cartRigidbody.isKinematic)StopPushing();
            else {restoreCollisions=false;controller.Interact(inventory);}
        }
        public IEnumerator DeliverTo(Vector3 destination)
        {
            if (Count < 1) yield break; PrisonAudio.Clink(transform.position);
            var tray = meals[Count - 1].transform;
            var from = tray.position;
            for (float t = 0; t < .65f; t += Time.deltaTime)
            {
                float p = t / .65f;
                tray.position = Vector3.Lerp(from, destination, p) + Vector3.up * Mathf.Sin(p * Mathf.PI) * .25f;
                yield return null;
            }
            tray.gameObject.SetActive(false); tray.position = from; Count--;
        }
    }
}
