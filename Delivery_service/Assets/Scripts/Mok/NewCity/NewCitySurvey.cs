using UnityEngine;
using UnityEngine.InputSystem;
namespace DeliveryService.Mok.NewCity
{
    // 새 지역의 동선과 실내를 확인하는 전용 둘러보기 카메라. 본편 플레이어와는 별개다.
    [RequireComponent(typeof(CharacterController))]
    public sealed class NewCitySurvey : MonoBehaviour
    {
        public Camera view;
        CharacterController body;
        float pitch, falling;
        void Start(){body=GetComponent<CharacterController>();Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}
        void Update()
        {
            if(Keyboard.current==null||Mouse.current==null)return;
            if(Keyboard.current.escapeKey.wasPressedThisFrame){Cursor.lockState=Cursor.lockState==CursorLockMode.Locked?CursorLockMode.None:CursorLockMode.Locked;Cursor.visible=Cursor.lockState!=CursorLockMode.Locked;}
            if(Cursor.lockState!=CursorLockMode.Locked)return;
            var d=Mouse.current.delta.ReadValue()*.08f;
            transform.Rotate(0,d.x,0);pitch=Mathf.Clamp(pitch-d.y,-80,80);view.transform.localRotation=Quaternion.Euler(pitch,0,0);
            var k=Keyboard.current;float x=(k.dKey.isPressed?1:0)-(k.aKey.isPressed?1:0),z=(k.wKey.isPressed?1:0)-(k.sKey.isPressed?1:0);
            if(body.isGrounded)falling=-2;else falling-=20*Time.deltaTime;
            var move=Vector3.ClampMagnitude(transform.right*x+transform.forward*z,1)*(k.leftShiftKey.isPressed?7:3.5f);
            body.Move((move+Vector3.up*falling)*Time.deltaTime);
        }
        void OnDisable(){Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
    }
}
