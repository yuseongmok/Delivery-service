using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
namespace DeliveryService.Mok
{
    public sealed class PrisonKeysMission:MonoBehaviour
    {
        public enum Stage{Waiting,Steal,Unlock,Gather,Cook,Load,Serve,Office,Conversation,Complete}
        public PrisonMedicineMission previous;
        public PrisonKeysText dialogue;
        public PrisonGuard keyGuard;
        public GameObject keyRing;
        public PrisonKeyLock keyLock;
        public Transform checkpoint,kitchen,loadPoint,officeDoor;
        public GameObject[] supplies;
        public GameObject cookingSupplies,pizzaOnCart;
        public PrisonGuard[] diners;
        public PrisonGuard warden;
        public Camera camera;
        public PizzaMaker maker;
        public Oven oven;
        public PizzaToppingData[] ingredients;
        public Stage State{get;private set;}
        public bool Active=>State!=Stage.Waiting;
        public bool Failing{get;private set;}
        public float Suspicion{get;private set;}
        public float TheftProgress{get;private set;}
        public float Fade{get;private set;}
        public int Gathered{get;private set;}
        public int Served{get;private set;}
        public string Subtitle{get;private set;}="";
        public Transform Target=>State==Stage.Steal?keyGuard.transform:State==Stage.Unlock?keyLock.transform:State==Stage.Gather?supplies.FirstOrDefault(g=>g.activeSelf)?.transform:State==Stage.Cook?kitchen:State==Stage.Load?loadPoint:State==Stage.Serve?(Served<diners.Length?diners[Served].transform:officeDoor):State==Stage.Office?warden.transform:null;
        public string Objective=>Failing?"창고 앞에서 다시 시도합니다":State switch{Stage.Steal=>"소매치기 · 간수 뒤에서 [F] 길게 눌러 열쇠 훔치기",Stage.Unlock=>"소매치기 · 창고 열쇠 찾기 / 간수가 보면 숨기",Stage.Gather=>$"소매치기 · 피자 재료 챙기기 ({Gathered}/4)",Stage.Cook=>"페페로니 피자를 만들어 오븐에서 굽기",Stage.Load=>"구운 피자를 배식카트에 싣기",Stage.Serve=>$"피자 배식 · 지정 간수에게 전달하기 ({Served}/{diners.Length})",Stage.Office=>"간수장실 · 염윤태에게 피자 전달하기",Stage.Conversation=>"대화 중",Stage.Complete=>"소매치기 완료 · 간수장실 잠입 성공",_=>""};
        PrisonMission Prison=>previous.prison;
        PlayerCombat Player=>Prison.player;
        ToppingInventory Inventory=>Player.GetComponent<ToppingInventory>();
        float noticeUntil;
        public bool SelectingKey{get;private set;}
        bool busy,locked,moveControl,lookControl,interactionEnabled,phoneEnabled;
        Vector3 guardStart;Quaternion guardRotation;
        void Start(){guardStart=keyGuard.transform.position;guardRotation=keyGuard.transform.rotation;keyGuard.gameObject.SetActive(false);cookingSupplies.SetActive(false);pizzaOnCart.SetActive(false);foreach(var g in supplies)g.SetActive(false);foreach(var g in diners)g.gameObject.SetActive(false);warden.gameObject.SetActive(false);}
        void Update(){
            if(Time.timeScale<=0)return;
            if(State==Stage.Waiting){if(previous.State==PrisonMedicineMission.Stage.Complete)Begin();return;}
            if(!busy&&Time.time>noticeUntil)Subtitle="";
            if(Failing||busy)return;
            if(State==Stage.Steal&&TheftProgress>0&&keyGuard.CanSee(Player)){StartCoroutine(Retry());return;}
            bool danger=State==Stage.Steal?TheftProgress>0:State==Stage.Unlock;
            Suspicion=Mathf.Clamp01(Suspicion+Time.deltaTime*(danger&&keyGuard.CanSee(Player)?1.5f:-1));
            if(Suspicion>=1){StartCoroutine(Retry());return;}
            if(State==Stage.Steal){if(Player.CanAct&&Keyboard.current!=null&&Keyboard.current.fKey.isPressed&&CanSteal())TheftProgress+=Time.deltaTime/.85f;else TheftProgress=0;if(TheftProgress>=1){State=Stage.Unlock;keyRing.SetActive(false);TheftProgress=0;Say(dialogue.stolen,9);}}
            if(State==Stage.Unlock){if(SelectingKey){keyLock.Show(true);float scroll=Mouse.current?.scroll.ReadValue().y??0;if(Mathf.Abs(scroll)>0)keyLock.Select(scroll>0?1:-1);if(Mouse.current!=null&&Mouse.current.leftButton.isPressed){int result=keyLock.Turn(Time.deltaTime);if(result==1)StartCoroutine(OpenStore());else if(result<0){Say(dialogue.wrongKey);PrisonAudio.Clink(keyLock.transform.position,.12f);}}else keyLock.Cancel();}else keyLock.Cancel();}
            if(State==Stage.Cook&&Inventory.HeldPizza){var pizza=Inventory.HeldPizza.GetComponent<Pizza>();if(pizza&&ValidPizza(pizza)){State=Stage.Load;Say(dialogue.baked);}}
            if(State==Stage.Load&&!Inventory.HeldPizza)State=Stage.Cook;
            if(SelectingKey&&Keyboard.current!=null&&Keyboard.current.fKey.wasPressedThisFrame){SelectingKey=false;keyLock.Cancel();Lock(false);return;}
            if(Player.CanAct&&Keyboard.current!=null&&Keyboard.current.fKey.wasPressedThisFrame)Interact();
        }
        public void Begin(){State=Stage.Steal;Gathered=Served=0;keyGuard.gameObject.SetActive(true);keyRing.SetActive(true);Prison.introduction.mingi.Following=true;Say(dialogue.start,10);}
        public bool CanSteal(){Vector3 delta=Vector3.ProjectOnPlane(Player.transform.position-keyGuard.transform.position,Vector3.up);return delta.magnitude<1.7f&&Vector3.Dot(keyGuard.transform.forward,delta.normalized)<-.35f&&!keyGuard.CanSee(Player);}
        public bool ValidPizza(Pizza pizza)=>pizza.IsBaked&&ingredients.All(i=>pizza.currentToppings.Contains(i.toppingName));
        bool Near(Transform target,float range=2.8f)=>Vector3.Distance(Player.transform.position,target.position)<range;
        public bool Interact(){
            if(busy||Failing||!Player.CanAct)return false;
            if(State==Stage.Unlock&&Near(keyLock.transform,2.1f)){SelectingKey=true;Lock(true);return true;}
            if(State==Stage.Gather){foreach(var item in supplies)if(item.activeSelf&&Near(item.transform)){item.SetActive(false);Gathered++;PrisonAudio.Clink(item.transform.position,.1f);if(Gathered==supplies.Length){State=Stage.Cook;cookingSupplies.SetActive(true);Say(dialogue.supplies+"\n"+dialogue.cook,12);}return true;}}
            if(State==Stage.Load&&Near(loadPoint)&&Inventory.HeldPizza){var pizza=Inventory.HeldPizza.GetComponent<Pizza>();if(!pizza||!ValidPizza(pizza))return false;StartCoroutine(LoadPizza());return true;}
            if(State==Stage.Serve&&Near(diners[Served].transform)&&Vector3.Distance(Prison.cart.transform.position,diners[Served].transform.position)<4){StartCoroutine(Serve());return true;}
            if(State==Stage.Office&&Near(warden.transform)){StartCoroutine(Warden());return true;}return false;
        }
        IEnumerator OpenStore(){busy=true;SelectingKey=false;Lock(false);yield return keyLock.Open();foreach(var s in supplies)s.SetActive(true);State=Stage.Gather;Suspicion=0;busy=false;}
        IEnumerator LoadPizza(){busy=true;Prison.cart.StopPushing();var food=Inventory.RemovePizza();food.transform.SetParent(pizzaOnCart.transform,false);food.transform.localPosition=Vector3.zero;food.transform.localRotation=Quaternion.identity;food.transform.localScale=Vector3.one*.75f;food.GetComponent<Pizza>().SetHeld(false);foreach(var collider in food.GetComponentsInChildren<Collider>())collider.enabled=false;Lock(true);yield return Lines(dialogue.load);pizzaOnCart.SetActive(true);foreach(var guard in diners){guard.receivingMeal=true;guard.gameObject.SetActive(true);}warden.gameObject.SetActive(true);Lock(false);State=Stage.Serve;busy=false;}
        IEnumerator Serve(){busy=true;Prison.cart.StopPushing();var guard=diners[Served];if(Served==diners.Length-1){Lock(true);camera.transform.LookAt(guard.transform.position+Vector3.up*1.4f);yield return Lines(dialogue.officer);Lock(false);}else{Say(dialogue.served);yield return new WaitForSeconds(1.3f);}yield return Sleep(guard);Served++;if(Served==diners.Length){yield return officeDoor.GetComponent<PrisonOfficeEntrance>().Open();State=Stage.Office;Say(dialogue.officeOpen);}busy=false;}
        IEnumerator Warden(){busy=true;Lock(true);camera.transform.LookAt(warden.transform.position+Vector3.up*1.4f);yield return Lines(dialogue.warden.Take(Mathf.Max(0,dialogue.warden.Length-1)).ToArray());yield return Sleep(warden,dialogue.warden.LastOrDefault());pizzaOnCart.SetActive(false);Say(dialogue.end,8);yield return new WaitForSeconds(dialogue.lineSeconds);Lock(false);State=Stage.Complete;busy=false;}
        IEnumerator Sleep(PrisonGuard guard,string taste=null){var eating=guard.GetComponent<PrisonPizzaEating>();if(eating){yield return eating.Play(pizzaOnCart.transform,()=>{if(!string.IsNullOrEmpty(taste))Say(taste,4);});}else{guard.enabled=false;guard.gun?.gameObject.SetActive(false);guard.GetComponent<NPCControl>().KnockDown(Vector3.zero,0);}}
        IEnumerator Retry(){Failing=true;SelectingKey=false;keyLock.Cancel();Say(dialogue.caught,8);Lock(true);yield return Blackout(1);var cc=Player.GetComponent<CharacterController>();cc.enabled=false;Player.transform.SetPositionAndRotation(checkpoint.position,checkpoint.rotation);cc.enabled=true;var agent=keyGuard.GetComponent<UnityEngine.AI.NavMeshAgent>();agent.Warp(guardStart);agent.ResetPath();keyGuard.transform.rotation=guardRotation;keyGuard.ResetWatch();keyLock.ResetDoor();State=Stage.Steal;TheftProgress=Suspicion=0;keyRing.SetActive(true);Lock(false);yield return Blackout(0);Failing=false;}
        IEnumerator Blackout(float value){float from=Fade;for(float t=0;t<.5f;t+=Time.deltaTime){Fade=Mathf.Lerp(from,value,t/.5f);yield return null;}Fade=value;}
        IEnumerator Lines(string[] lines){foreach(var line in lines){Subtitle=line;for(float t=0;t<dialogue.lineSeconds;t+=Time.deltaTime){if(Time.timeScale>0&&Keyboard.current!=null&&Keyboard.current.spaceKey.wasPressedThisFrame)break;yield return null;}}Subtitle="";}
        void Say(string line,float seconds=5){Subtitle=line;noticeUntil=Time.time+seconds;}
        void Lock(bool value){var move=Player.GetComponent<PlayerMove>();var look=Player.GetComponentInChildren<CameraMove>();var interaction=Player.GetComponent<PlayerInteraction>();var phone=Player.GetComponentInChildren<PhoneController>(true);if(value&&!locked){moveControl=move.isControllable;lookControl=look.isControllable;interactionEnabled=interaction.enabled;phoneEnabled=phone&&phone.enabled;camera.transform.SetPositionAndRotation(Player.View.transform.position,Player.View.transform.rotation);}locked=value;move.isControllable=value?false:moveControl;look.isControllable=value?false:lookControl;interaction.enabled=value?false:interactionEnabled;if(phone)phone.enabled=value?false:phoneEnabled;Player.View.enabled=!value;camera.enabled=value;Player.View.GetComponent<AudioListener>().enabled=!value;camera.GetComponent<AudioListener>().enabled=value;Cursor.lockState=value?CursorLockMode.None:CursorLockMode.Locked;Cursor.visible=value;}
        void OnGUI(){if(!Active||Time.timeScale<=0)return;var tint=GUI.color;if(Fade>0){GUI.color=new Color(0,0,0,Fade);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);}GUI.color=Color.white;var style=new GUIStyle(GUI.skin.label){font=MokUIFont.Font,fontSize=22,alignment=TextAnchor.MiddleCenter};if(State==Stage.Unlock&&Near(keyLock.transform,2.1f))GUI.Label(new Rect(Screen.width*.25f,Screen.height*.8f,Screen.width*.5f,60),$"열쇠 {keyLock.Selected+1}/4 · 휠로 선택 · 좌클릭 길게 돌리기\nF · 중단하고 숨기",style);float progress=Suspicion>0?Suspicion:State==Stage.Steal?TheftProgress:keyLock.Progress;if(progress>0){GUI.color=Suspicion>0?new Color(1,.4f,.15f):new Color(.4f,.8f,.7f);GUI.DrawTexture(new Rect(Screen.width*.4f,Screen.height*.17f,Screen.width*.2f*progress,6),Texture2D.whiteTexture);}GUI.color=tint;}
    }
}


