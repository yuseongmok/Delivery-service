using UnityEngine;
using UnityEngine.InputSystem;

namespace DeliveryService.Mok
{
    // 조리 판정과 분리된 소품 연출. 조리대와 도구 자체는 씬에 저장한다.
    public sealed class ChickenCookingVisuals : MonoBehaviour
    {
        public ChickenCooking cooking;
        public Transform fryer;
        public Material raw, coated, fried;
        Vector3 chickenRest, knifeRest;
        Quaternion chickenRotation;
        Transform[] pieces;
        Vector3[] offsets;
        int previousCuts;
        float chopAt = -10;

        void Start()
        {
            chickenRest = cooking.rawChicken.transform.position;
            chickenRotation = cooking.rawChicken.transform.rotation;
            knifeRest = cooking.knife.position;
            pieces = new Transform[cooking.rawChicken.transform.childCount];
            offsets = new Vector3[pieces.Length];
            for (int i=0;i<pieces.Length;i++) { pieces[i]=cooking.rawChicken.transform.GetChild(i); offsets[i]=pieces[i].localPosition; }
        }
        void LateUpdate()
        {
            var step = cooking.Current;
            if (previousCuts != cooking.Cuts) { if(cooking.Cuts>previousCuts)chopAt=Time.time; previousCuts=cooking.Cuts; }
            var food = cooking.rawChicken.transform;
            Vector3 target = step==ChickenCooking.Step.Fry ? fryer.position+Vector3.up*.09f : step==ChickenCooking.Step.Mix ? cooking.bowl.position+Vector3.up*.07f : chickenRest;
            food.position = target;
            food.rotation = chickenRotation;
            if(step==ChickenCooking.Step.Fry)food.rotation*=Quaternion.Euler(Mathf.Sin(Time.time*4)*9,0,0);
            foreach(var renderer in food.GetComponentsInChildren<Renderer>())if(!renderer.name.StartsWith("뼈"))renderer.sharedMaterial=step>=ChickenCooking.Step.Fry?fried:step>=ChickenCooking.Step.Mix?coated:raw;
            for(int i=0;i<pieces.Length;i++)pieces[i].localPosition=offsets[i]*(cooking.Cuts>=3?1.4f:1f);
            if(cooking.Open&&step==ChickenCooking.Step.Cut&&Mouse.current!=null)
            {
                var ray=cooking.mission.camera.ScreenPointToRay(Mouse.current.position.ReadValue());
                var plane=new Plane(Vector3.up,chickenRest+Vector3.up*.14f);
                if(plane.Raycast(ray,out float distance))
                {
                    var point=ray.GetPoint(distance);point.x=Mathf.Clamp(point.x,chickenRest.x-.3f,chickenRest.x+.3f);point.z=Mathf.Clamp(point.z,chickenRest.z-.3f,chickenRest.z+.3f);
                    point.y-=Mathf.Sin(Mathf.Clamp01((Time.time-chopAt)/.25f)*Mathf.PI)*.12f;
                    cooking.knife.position=point;
                }
            }
            else cooking.knife.position=knifeRest;
        }
    }
}
