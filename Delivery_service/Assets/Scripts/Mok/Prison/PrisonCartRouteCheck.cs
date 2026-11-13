#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.AI;

namespace DeliveryService.Mok.Editor
{
    // 실제 카트 Rigidbody와 플레이어 충돌 이동으로 배식 동선을 검사한다.
    public sealed class PrisonCartRouteCheck : MonoBehaviour
    {
        public bool Done { get; private set; }
        public string Failure { get; private set; }
        PrisonKeysMission mission;
        PlayerCombat player;
        CharacterController body;
        IEnumerator Start()
        {
            mission=FindFirstObjectByType<PrisonKeysMission>();player=mission.previous.prison.player;
            body=player.GetComponent<CharacterController>();
            File.WriteAllText("Logs/Prison-cart-route.txt","Actual collision movement; no cart teleport during route.\n");
            var move=player.GetComponent<PlayerMove>();var look=player.GetComponentInChildren<CameraMove>();
            bool moved=move.enabled,looked=look.enabled;move.enabled=false;look.enabled=false;
            var cart=mission.previous.prison.cart;
            for(int i=0;i<mission.diners.Length;i++)
            {
                var guard=mission.diners[i];
                var path=new NavMeshPath();
                var feet=player.transform.position;feet.y=body.bounds.min.y+.1f;
                if(!NavMesh.SamplePosition(feet,out var from,2,NavMesh.AllAreas)||
                   !NavMesh.SamplePosition(guard.transform.position+guard.transform.forward*1.8f,out var to,2,NavMesh.AllAreas)||
                   !NavMesh.CalculatePath(from.position,to.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)
                {Failure="No route to "+guard.name;break;}
                var corners=path.corners;
                for(int n=0;n<corners.Length;n++)corners[n]=PrisonMealCart.ClearancePoint(corners[n]);
                if(cart.controller.cartRigidbody.isKinematic)cart.Interact(player.GetComponent<ToppingInventory>());
                int corner=1;float lastAt=Time.time;Vector3 last=player.transform.position;
                while(corner<corners.Length)
                {
                    var delta=Vector3.ProjectOnPlane(corners[corner]-player.transform.position,Vector3.up);
                    if(delta.magnitude<.35f){corner++;continue;}
                    player.transform.rotation=Quaternion.RotateTowards(player.transform.rotation,Quaternion.LookRotation(delta),100*Time.deltaTime);
                    player.View.transform.localRotation=Quaternion.identity;
                    body.Move((delta.normalized*2.5f+Vector3.down*3)*Time.deltaTime);
                    if(Vector3.Distance(last,player.transform.position)>.3f){last=player.transform.position;lastAt=Time.time;}
                    if(Time.time-lastAt>8){Failure=$"Blocked to {guard.name}, player={player.transform.position}, cart={cart.transform.position}, corner={path.corners[corner]}";break;}
                    yield return null;
                }
                if(Failure!=null)break;
                yield return new WaitForSeconds(.7f);
                Cursor.lockState=CursorLockMode.Locked;
                if(!mission.Interact()){Failure=$"Cannot serve {guard.name}, player={player.transform.position}, cart={cart.transform.position}, target={guard.transform.position}, canAct={player.CanAct}, stage={mission.State}";break;}
                while(mission.Served<=i)yield return null;
                File.AppendAllText("Logs/Prison-cart-route.txt","PASS physical cart route to "+guard.name+"\n");
            }
            if(Failure==null)while(mission.State==PrisonKeysMission.Stage.Serve)yield return null;
            cart.StopPushing();move.enabled=moved;look.enabled=looked;
            if(Failure!=null)File.AppendAllText("Logs/Prison-cart-route.txt","FAIL "+Failure+"\n");
            Done=true;
        }
    }
}
#endif


