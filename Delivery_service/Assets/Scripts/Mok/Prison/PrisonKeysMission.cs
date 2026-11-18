using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
namespace DeliveryService.Mok
{
    public sealed class PrisonKeysMission:MonoBehaviour
    {
        public enum Stage{Waiting,Steal,Unlock,Gather,Cook,Load,Serve,Office,Conversation,Complete,Rejoin}
        PrisonWarehouseDiversion diversion;
        public bool DiversionReady=>diversion&&diversion.ReadyToRejoin;
        public bool LeadingGuard=>busy&&keyLock.Opened&&State==Stage.Unlock;
        public void ShowDiversionLine(string line){Say(line,5);}
        public PrisonMedicineMission previous;
        public PrisonKeysText dialogue;
        public PrisonGuard keyGuard;
        [Min(.2f)] public float approachNoticeSeconds=1f, closeApproachNoticeSeconds=.65f;
        public GameObject keyRing;
        public PrisonKeyLock keyLock;
        public Transform checkpoint,kitchen,loadPoint,officeDoor;
        public GameObject[] supplies;
        public GameObject cookingSupplies,pizzaOnCart;
        public PrisonGuard[] diners;
        public PrisonGuard warden;
        public Camera camera;
        public PizzaMaker maker;
        public PrisonPizzaService serving;
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
        public Transform Target=>serving&&serving.Meal&&!serving.Carrying&&(State==Stage.Serve||State==Stage.Office)?serving.Meal.transform:LeadingGuard?Prison.introduction.mingi.transform:State==Stage.Rejoin?Prison.introduction.mingi.transform:State==Stage.Steal?keyGuard.transform:State==Stage.Unlock?keyLock.transform:State==Stage.Gather?supplies.FirstOrDefault(g=>g.activeSelf)?.transform:State==Stage.Cook?kitchen:State==Stage.Load?loadPoint:State==Stage.Serve?(Served<diners.Length?diners[Served].transform:officeDoor):State==Stage.Office?warden.transform:null;
        public string Objective=>LeadingGuard?"민기가 간수를 유도하는 동안 기다리기":Failing?"창고 앞에서 다시 시도합니다":State switch{Stage.Steal=>theft && theft.Playing?"소매치기 · 소리를 내지 않고 열쇠 빼내기":"소매치기 · 간수의 움직임을 살피고 뒤에서 [F] 열쇠 살펴보기",Stage.Unlock=>"소매치기 · 소리를 내지 않고 창고 문 열기",Stage.Gather=>$"소매치기 · 피자 재료 챙기기 ({Gathered}/4)",Stage.Rejoin=>"골목 입구에서 민기와 합류하기",Stage.Cook=>serving?"수면제를 재료처럼 넣고 페페로니 피자 굽기":"페페로니 피자를 만들어 오븐에서 굽기",Stage.Load=>serving?"구운 피자를 접시에 담기":"구운 피자를 배식카트에 싣기",Stage.Serve=>$"피자 배식 · 지정 간수에게 전달하기 ({Served}/{diners.Length})",Stage.Office=>"간수장실 · 염윤태에게 피자 전달하기",Stage.Conversation=>"대화 중",Stage.Complete=>"소매치기 완료 · 간수장실 잠입 성공",_=>""};
        PrisonMission Prison=>previous.prison;
        PlayerCombat Player=>Prison.player;
        ToppingInventory Inventory=>Player.GetComponent<ToppingInventory>();
        PrisonPickpocket theft; PrisonLockInteraction lockInteraction;
        float detectionGraceUntil; float noticeUntil; int lockCancelledFrame=-1;
        public bool SelectingKey{get;private set;}
        bool busy,locked,moveControl,lookControl,interactionEnabled,phoneEnabled;
        Vector3 guardStart;Quaternion guardRotation;
        void Start(){guardStart=keyGuard.transform.position;guardRotation=keyGuard.transform.rotation;keyGuard.gameObject.SetActive(false);cookingSupplies.SetActive(false);pizzaOnCart.SetActive(false);foreach(var g in supplies)g.SetActive(false);foreach(var g in diners)g.gameObject.SetActive(false);warden.gameObject.SetActive(false);}
        void Update(){
            if(Time.timeScale<=0||Player.IsDown||lockCancelledFrame==Time.frameCount)return;
            if(State==Stage.Waiting){if(previous.State==PrisonMedicineMission.Stage.Complete)Begin();return;}
            if(!busy&&Time.time>noticeUntil)Subtitle="";
            if(Failing||busy)return;
            if(State==Stage.Rejoin&&diversion&&diversion.ReadyToRejoin&&Near(Prison.introduction.mingi.transform,3)){diversion.Rejoin();State=Stage.Cook;cookingSupplies.SetActive(true);Say(dialogue.rejoined+"\n"+dialogue.cook,10);}
            if(SelectingKey&&Keyboard.current!=null&&Keyboard.current.fKey.wasPressedThisFrame){CancelLock();return;}
            bool danger=State==Stage.Steal&&!(theft&&theft.Playing);
            bool seen=danger&&Time.time>=detectionGraceUntil&&keyGuard.CanSee(Player);
            float notice=State==Stage.Steal
                ? 1f/Mathf.Max(.2f,Vector3.Distance(Player.transform.position,keyGuard.transform.position)<3f?closeApproachNoticeSeconds:approachNoticeSeconds)
                : 1.5f;
            if(Time.time>=detectionGraceUntil)Suspicion=Mathf.Clamp01(Suspicion+Time.deltaTime*(seen?notice:-1));
            if(Suspicion>=1){StartCoroutine(Retry());return;}
            if(theft && theft.Playing) return;
            if(State==Stage.Cook&&Inventory.HeldPizza){var pizza=Inventory.HeldPizza.GetComponent<Pizza>();if(pizza&&ValidPizza(pizza)){State=Stage.Load;Say(dialogue.baked);}}
            if(State==Stage.Load&&!Inventory.HeldPizza)State=Stage.Cook;
            if(State==Stage.Load&&serving&&Inventory.HeldPizza){StartCoroutine(LoadPizza());return;}
            if(SelectingKey)return;
            if(Player.CanAct&&Keyboard.current!=null&&Keyboard.current.fKey.wasPressedThisFrame)Interact();
        }
        public bool CanDeveloperComplete=>!busy&&!Failing;
        public void DeveloperComplete(){StartCoroutine(DeveloperFinish());}
        IEnumerator DeveloperFinish(){
            if(diversion)diversion.Rejoin();
            if(theft&&theft.Playing)theft.End(false);CancelLock();busy=true;
            keyRing.SetActive(false);pizzaOnCart.SetActive(false);if(serving)serving.Finish();foreach(var s in supplies)s.SetActive(false);
            foreach(var g in diners){g.gameObject.SetActive(false);}warden.gameObject.SetActive(false);
            Gathered=supplies.Length;Served=diners.Length;Suspicion=0;
            yield return officeDoor.GetComponent<PrisonOfficeEntrance>().Open();
            State=Stage.Complete;busy=false;Subtitle="";
        }
        public void Begin(){State=Stage.Steal;Gathered=Served=0;keyGuard.gameObject.SetActive(true);keyRing.SetActive(true);Prison.introduction.mingi.Following=true;Say(dialogue.start,10);}
        public void SetTheftView(bool value){if(value)Subtitle="";else lockCancelledFrame=Time.frameCount;Lock(value);}
        public void ReportDroppedKeys(){Suspicion=.65f;detectionGraceUntil=Time.time+1.25f;keyGuard.Distract(Player.transform.position);Say(dialogue.theftDropped,4);}
        public void FinishTheft(){State=Stage.Unlock;keyRing.SetActive(false);TheftProgress=0;if(!diversion)diversion=gameObject.AddComponent<PrisonWarehouseDiversion>();diversion.BeginTalk(this);}
        public void CancelLock(){lockCancelledFrame=Time.frameCount;if(lockInteraction)lockInteraction.End();SelectingKey=false;keyLock.Cancel();}
        public void FinishLock(){if(State==Stage.Unlock&&!Failing&&!busy)StartCoroutine(OpenStore());}
        public void TheftCaught(){if(!Failing)StartCoroutine(Retry());}
        public bool CanSteal(){Vector3 delta=Vector3.ProjectOnPlane(Player.transform.position-keyGuard.transform.position,Vector3.up);return delta.magnitude<1.7f&&Vector3.Dot(keyGuard.transform.forward,delta.normalized)<-.35f&&!keyGuard.CanSee(Player);}
        public bool ValidPizza(Pizza pizza)=>pizza.IsBaked&&ingredients.All(i=>pizza.currentToppings.Contains(i.toppingName))&&(!serving||serving.Seasoned(pizza));
        bool Near(Transform target,float range=2.8f)=>Vector3.Distance(Player.transform.position,target.position)<range;
        public bool Interact(){
            if(busy||Failing||!Player.CanAct)return false;
            if(State==Stage.Steal&&CanSteal()){if(!theft)theft=GetComponent<PrisonPickpocket>();if(!theft)theft=gameObject.AddComponent<PrisonPickpocket>();theft.Begin(this);return true;}
            if(State==Stage.Unlock&&Near(keyLock.transform,2.1f)){SelectingKey=true;if(!lockInteraction)lockInteraction=GetComponent<PrisonLockInteraction>();if(!lockInteraction)lockInteraction=gameObject.AddComponent<PrisonLockInteraction>();lockInteraction.Begin(this);return true;}
            if(State==Stage.Gather){foreach(var item in supplies)if(item.activeSelf&&Near(item.transform)){item.SetActive(false);Gathered++;PrisonCookingAudio.Cue("pickup");if(Gathered==supplies.Length){State=Stage.Rejoin;Say(dialogue.meetAgain,8);}return true;}}
            if(State==Stage.Load&&Near(loadPoint)&&Inventory.HeldPizza){var pizza=Inventory.HeldPizza.GetComponent<Pizza>();if(!pizza||!ValidPizza(pizza))return false;StartCoroutine(LoadPizza());return true;}
            if(State==Stage.Serve&&Near(diners[Served].transform)&&(serving?serving.Carrying:Vector3.Distance(Prison.cart.transform.position,diners[Served].transform.position)<4)){StartCoroutine(Serve());return true;}
            if(State==Stage.Office&&Near(warden.transform)&&(!serving||serving.Carrying)){StartCoroutine(Warden());return true;}return false;
        }
        IEnumerator OpenStore(){busy=true;SelectingKey=false;Lock(false);yield return keyLock.Open();foreach(var s in supplies)s.SetActive(true);if(!diversion)diversion=gameObject.AddComponent<PrisonWarehouseDiversion>();yield return diversion.LeadAway(this);State=Stage.Gather;Suspicion=0;busy=false;}
        IEnumerator LoadPizza(){busy=true;if(serving){serving.Plate();Lock(true);yield return Lines(dialogue.load);foreach(var guard in diners){guard.receivingMeal=true;guard.gameObject.SetActive(true);}warden.gameObject.SetActive(true);Lock(false);State=Stage.Serve;busy=false;yield break;}Prison.cart.StopPushing();var food=Inventory.RemovePizza();food.transform.SetParent(pizzaOnCart.transform,false);food.transform.localPosition=Vector3.zero;food.transform.localRotation=Quaternion.identity;food.transform.localScale=Vector3.one*.75f;food.GetComponent<Pizza>().SetHeld(false);foreach(var collider in food.GetComponentsInChildren<Collider>())collider.enabled=false;Lock(true);yield return Lines(dialogue.load);pizzaOnCart.SetActive(true);foreach(var guard in diners){guard.receivingMeal=true;guard.gameObject.SetActive(true);}warden.gameObject.SetActive(true);Lock(false);State=Stage.Serve;busy=false;}
        IEnumerator Serve(){
            busy=true;if(!serving)Prison.cart.StopPushing();var guard=diners[Served];bool officer=Served==diners.Length-1;
            if(officer){Lock(true);yield return FrameGuard(guard);yield return Lines(dialogue.officer.Take(3).ToArray());}
            else{Say(dialogue.served);yield return new WaitForSeconds(1.3f);}
            yield return Sleep(guard,officer?dialogue.officerTaste:null);Served++;
            if(officer){yield return new WaitForSeconds(.4f);Lock(false);}
            if(Served==diners.Length){yield return officeDoor.GetComponent<PrisonOfficeEntrance>().Open();State=Stage.Office;Say(dialogue.officeOpen);}busy=false;
        }
        IEnumerator FrameGuard(PrisonGuard guard){
            var begin=camera.transform.rotation;var end=Quaternion.LookRotation(guard.transform.position+Vector3.up*1.4f-camera.transform.position);
            for(float t=0;t<.45f;t+=Time.deltaTime){camera.transform.rotation=Quaternion.Slerp(begin,end,Mathf.SmoothStep(0,1,t/.45f));yield return null;}
        }
        IEnumerator Warden(){busy=true;Lock(true);yield return FrameGuard(warden);yield return Lines(dialogue.warden.Take(Mathf.Max(0,dialogue.warden.Length-1)).ToArray());yield return Sleep(warden,dialogue.warden.LastOrDefault());pizzaOnCart.SetActive(false);if(serving)serving.Finish();Say(dialogue.end,8);yield return new WaitForSeconds(dialogue.lineSeconds);Lock(false);State=Stage.Complete;busy=false;}
        IEnumerator Sleep(PrisonGuard guard,string taste=null){var eating=guard.GetComponent<PrisonPizzaEating>();if(eating){yield return eating.Play(serving?serving.Tray:pizzaOnCart.transform,()=>{if(!string.IsNullOrEmpty(taste))Say(taste,4);});}else{guard.enabled=false;guard.gun?.gameObject.SetActive(false);guard.GetComponent<NPCControl>().KnockDown(Vector3.zero,0);}}
        IEnumerator Retry(){if(diversion)diversion.ResetDiversion();if(theft&&theft.Playing)theft.End(false);CancelLock();Failing=true;SelectingKey=false;keyLock.Cancel();Say(dialogue.caught,8);Lock(true);yield return Blackout(1);var cc=Player.GetComponent<CharacterController>();cc.enabled=false;Player.transform.SetPositionAndRotation(checkpoint.position,checkpoint.rotation);cc.enabled=true;var agent=keyGuard.GetComponent<UnityEngine.AI.NavMeshAgent>();agent.Warp(guardStart);agent.ResetPath();keyGuard.transform.rotation=guardRotation;keyGuard.ResetWatch();keyLock.ResetDoor();State=Stage.Steal;TheftProgress=Suspicion=0;detectionGraceUntil=Time.time+1.5f;keyRing.SetActive(true);Lock(false);yield return Blackout(0);Failing=false;}
        IEnumerator Blackout(float value){float from=Fade;for(float t=0;t<.5f;t+=Time.deltaTime){Fade=Mathf.Lerp(from,value,t/.5f);yield return null;}Fade=value;}
        IEnumerator Lines(string[] lines){foreach(var line in lines){Subtitle=line;for(float t=0;t<dialogue.lineSeconds;t+=Time.deltaTime){if(Time.timeScale>0&&Keyboard.current!=null&&Keyboard.current.spaceKey.wasPressedThisFrame)break;yield return null;}}Subtitle="";}
        void Say(string line,float seconds=5){Subtitle=line;noticeUntil=Time.time+seconds;}
        Coroutine returningView;
        void Lock(bool value)
        {
            if(!previous||!previous.prison||!previous.prison.player||!camera)return;
            var move=Player.GetComponent<PlayerMove>();
            var look=Player.GetComponentInChildren<CameraMove>();
            var interaction=Player.GetComponent<PlayerInteraction>();
            var phone=Player.GetComponentInChildren<PhoneController>(true);
            bool wasLocked=locked;
            if(returningView!=null){StopCoroutine(returningView);returningView=null;look.isControllable=lookControl;}
            if(value&&!locked){
                moveControl=move.isControllable;lookControl=look.isControllable;
                interactionEnabled=interaction.enabled;phoneEnabled=phone&&phone.enabled;
                camera.transform.SetPositionAndRotation(Player.View.transform.position,Player.View.transform.rotation);
            }
            locked=value;
            move.isControllable=value?false:moveControl;look.isControllable=value?false:lookControl;
            interaction.enabled=value?false:interactionEnabled;if(phone)phone.enabled=value?false:phoneEnabled;
            Player.View.enabled=!value;
            Player.View.GetComponent<AudioListener>().enabled=!value;
            camera.GetComponent<AudioListener>().enabled=value;
            Cursor.lockState=value?CursorLockMode.None:CursorLockMode.Locked;Cursor.visible=value;
            if(value)camera.enabled=true;
            else if(wasLocked && isActiveAndEnabled){look.isControllable=false;returningView=StartCoroutine(ReturnToPlayer());}
            else camera.enabled=false;
        }
        IEnumerator ReturnToPlayer()
        {
            // 이동 입력은 즉시 돌려주고, 화면만 현재 눈 위치까지 짧게 연결한다.
            Vector3 start=camera.transform.position;Quaternion rotation=camera.transform.rotation;
            float fov=camera.fieldOfView;
            for(float elapsed=0;elapsed<.18f;elapsed+=Time.deltaTime){
                float t=Mathf.SmoothStep(0,1,elapsed/.18f);
                camera.transform.SetPositionAndRotation(Vector3.Lerp(start,Player.View.transform.position,t),Quaternion.Slerp(rotation,Player.View.transform.rotation,t));
                camera.fieldOfView=Mathf.Lerp(fov,Player.View.fieldOfView,t);
                yield return null;
            }
            camera.enabled=false;camera.fieldOfView=fov;returningView=null;
            var look=Player.GetComponentInChildren<CameraMove>();look.SyncViewFromTransform();look.isControllable=lookControl;
        }
        void OnDisable()
        {
            if(returningView!=null){StopCoroutine(returningView);returningView=null;if(previous&&previous.prison&&previous.prison.player){var look=Player.GetComponentInChildren<CameraMove>();if(look)look.isControllable=lookControl;}}
            if(locked)Lock(false);
            if(camera)camera.enabled=false;
        }
        void OnGUI(){if(!Active||Time.timeScale<=0)return;var tint=GUI.color;if(Fade>0){GUI.color=new Color(0,0,0,Fade);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);}GUI.color=Color.white;var style=new GUIStyle(GUI.skin.label){font=MokUIFont.Font,fontSize=22,alignment=TextAnchor.MiddleCenter};if(!busy&&State==Stage.Unlock&&!SelectingKey&&Near(keyLock.transform,2.1f))GUI.Label(new Rect(Screen.width*.25f,Screen.height*.8f,Screen.width*.5f,60),"F · 열쇠고리를 꺼내 창고 문 열기",style);float progress=Suspicion>0?Suspicion:State==Stage.Steal?TheftProgress:keyLock.Progress;if(progress>0){GUI.color=Suspicion>0?new Color(1,.4f,.15f):new Color(.4f,.8f,.7f);GUI.DrawTexture(new Rect(Screen.width*.4f,Screen.height*.17f,Screen.width*.2f*progress,6),Texture2D.whiteTexture);}GUI.color=tint;}
    }
}
