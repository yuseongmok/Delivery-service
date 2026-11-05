using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
namespace DeliveryService.PrisonChapter {
// 새 감옥 씬만 둘러보기 위한 독립 조작기. 기존 플레이어/미션 시스템은 참조하지 않는다.
[RequireComponent(typeof(CharacterController))]
public sealed class PrisonPreviewWalker : MonoBehaviour {
 public Camera view; public float walkSpeed=4.2f; public float runSpeed=7f; float pitch,vertical;
 void Start(){Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}
 void Update(){
  Vector2 move=Vector2.zero,look=Vector2.zero;bool sprint=false,jump=false,escape=false,click=false;
#if ENABLE_INPUT_SYSTEM
  var k=Keyboard.current;var m=Mouse.current;
  if(k!=null){move=new Vector2((k.dKey.isPressed?1:0)-(k.aKey.isPressed?1:0),(k.wKey.isPressed?1:0)-(k.sKey.isPressed?1:0));sprint=k.leftShiftKey.isPressed;jump=k.spaceKey.wasPressedThisFrame;escape=k.escapeKey.wasPressedThisFrame;}
  if(m!=null){look=m.delta.ReadValue()*.085f;click=m.leftButton.wasPressedThisFrame;}
#else
  move=new Vector2(Input.GetAxisRaw("Horizontal"),Input.GetAxisRaw("Vertical"));look=new Vector2(Input.GetAxis("Mouse X"),Input.GetAxis("Mouse Y"))*2; sprint=Input.GetKey(KeyCode.LeftShift);jump=Input.GetKeyDown(KeyCode.Space);escape=Input.GetKeyDown(KeyCode.Escape);click=Input.GetMouseButtonDown(0);
#endif
  if(escape){Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
  if(click){Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}
  if(Cursor.lockState!=CursorLockMode.Locked)return;
  transform.Rotate(0,look.x,0);pitch=Mathf.Clamp(pitch-look.y,-80,80);view.transform.localRotation=Quaternion.Euler(pitch,0,0);
  var cc=GetComponent<CharacterController>();if(cc.isGrounded){vertical=-2;if(jump)vertical=5; }else vertical-=18*Time.deltaTime;
  Vector3 velocity=(transform.right*move.x+transform.forward*move.y).normalized*(sprint?runSpeed:walkSpeed);velocity.y=vertical;cc.Move(velocity*Time.deltaTime);
  if(transform.position.y < -10){cc.enabled=false;transform.position=new Vector3(0,.2f,-31);cc.enabled=true;}
 }
 void OnDisable(){Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
}
}
