using System.Collections;
using UnityEngine;
namespace DeliveryService.Mok {
// 본편 매장과 같은 외형을 사용하는 프롤로그 전용 연출. 실제 경제는 건드리지 않는다.
public sealed class PrologueShopArrival:MonoBehaviour {
    public BankPrologue director;
    public Transform actor,shop,carStop,wideShot,doorShot;
    public Animator animator;
    public Transform conversationPlayer,mingiShot,playerShot;
    public Transform[] slats;
    public AudioClip shutterSound;
    public Light cabinLight;
    public bool Completed{get;private set;}
    public bool Opened{get;private set;}
    public int Stage{get;private set;}
    AudioSource sound;
    float opening,handWeight;
    Vector3 handTarget;
    bool seated;
    bool paused;
    Rigidbody carBody;
    float carTravel;
    void Awake(){if(conversationPlayer)conversationPlayer.gameObject.SetActive(false);actor.gameObject.SetActive(false);if(cabinLight)cabinLight.enabled=false;sound=gameObject.AddComponent<AudioSource>();sound.playOnAwake=false;sound.spatialBlend=1;sound.minDistance=5;sound.maxDistance=25;sound.clip=shutterSound;}
    void Update(){
        if(Stage==1&&!director.Paused){
            // 대사가 길어져도 차량이 연출용 도로 밖으로 나가지 않도록 이동 거리를 제한한다.
            float step=Mathf.Min(1.2f*Time.deltaTime,Mathf.Max(0,8-carTravel));carTravel+=step;
            var car=director.getawayCar;var next=car.position+car.forward*step;if(carBody)carBody.position=next;car.position=next;
            director.filmCamera.transform.position=car.TransformPoint(new Vector3(-.35f,1.02f,.25f));
            director.filmCamera.transform.LookAt(car.TransformPoint(new Vector3(.45f,1.03f,-.12f)));
        }
        if(paused==director.Paused)return;paused=director.Paused;if(paused)sound.Pause();else sound.UnPause();
    }
    IEnumerator Wait(float duration){for(float t=0;t<duration&&!director.CinematicSkipRequested;t+=Time.deltaTime)yield return null;}
    IEnumerator Lines(PrologueLine[] lines){if(lines==null)yield break;foreach(var line in lines){if(director.CinematicSkipRequested)break;director.subtitle.text=line.text;yield return Wait(Mathf.Max(.1f,line.seconds));}}
    void DialogueShot(string text){if(!conversationPlayer||!mingiShot||!playerShot){CameraAt(wideShot);return;}var facing=conversationPlayer.position-actor.position;facing.y=0;if(facing.sqrMagnitude>.01f)actor.rotation=Quaternion.LookRotation(facing);conversationPlayer.LookAt(new Vector3(actor.position.x,conversationPlayer.position.y,actor.position.z));CameraAt(text.TrimStart().StartsWith("나:")?playerShot:mingiShot);}
    void CameraAt(Transform shot){director.filmCamera.transform.SetPositionAndRotation(shot.position,shot.rotation);}
    void Open(float p){opening=p;for(int i=0;i<slats.Length;i++)slats[i].localPosition=new Vector3(3.22f,Mathf.Min(3.07f,.09f+i*.16f+p*3.15f),3.15f);}
    public IEnumerator Play(){
        var d=director;var car=d.getawayCar;
        if(d.accomplice)d.accomplice.gameObject.SetActive(false);
        carBody=car.GetComponent<Rigidbody>();if(carBody){carBody.interpolation=RigidbodyInterpolation.None;carBody.position=car.position;carBody.rotation=car.rotation;}
        Completed=false;Opened=false;if(conversationPlayer)conversationPlayer.gameObject.SetActive(false);Stage=1;carTravel=0;if(cabinLight)cabinLight.enabled=true;
        // 운전석 쪽에서 조수석의 공범을 보여준다.
        actor.gameObject.SetActive(true);actor.SetParent(car,false);actor.localPosition=new Vector3(.45f,-.65f,-.1f);actor.localRotation=Quaternion.identity;seated=true;
        animator.SetFloat("MoveSpeed",0);
        d.filmCamera.transform.position=car.TransformPoint(new Vector3(-.38f,1.05f,.35f));
        d.filmCamera.transform.LookAt(car.TransformPoint(new Vector3(.45f,1.05f,-.1f)));
        yield return Lines(d.story.carConversation);
        // 정차와 하차 시간은 짧은 암전으로 연결한다.
        Stage=2;if(cabinLight)cabinLight.enabled=false;d.fade.alpha=1;actor.SetParent(null,true);seated=false;
        car.SetPositionAndRotation(carStop.position,carStop.rotation);
        if(carBody){carBody.position=carStop.position;carBody.rotation=carStop.rotation;}
        actor.SetPositionAndRotation(shop.TransformPoint(new Vector3(4.4f,0,6)),shop.rotation*Quaternion.Euler(0,180,0));
        if(conversationPlayer)conversationPlayer.gameObject.SetActive(true);CameraAt(wideShot);yield return Wait(.25f);d.fade.alpha=0;Stage=2;
        Vector3 from=actor.position,to=shop.TransformPoint(new Vector3(3.22f,0,3.85f));
        animator.SetFloat("MoveSpeed",1.5f);
        for(float t=0;t<1.7f&&!d.CinematicSkipRequested;t+=Time.deltaTime){actor.position=Vector3.Lerp(from,to,t/1.7f);yield return null;}
        actor.position=to;animator.SetFloat("MoveSpeed",0);
        if(d.story.shopConversation.Length>0){d.subtitle.text=d.story.shopConversation[0].text;DialogueShot(d.subtitle.text);yield return Wait(d.story.shopConversation[0].seconds);}
        actor.rotation=shop.rotation*Quaternion.Euler(0,180,0);CameraAt(doorShot);Stage=3;handWeight=1;handTarget=shop.TransformPoint(new Vector3(3.22f,.8f,3.3f));
        if(!d.CinematicSkipRequested)sound.Play();
        for(float t=0;t<2.8f&&!d.CinematicSkipRequested;t+=Time.deltaTime){
            float p=Mathf.SmoothStep(0,1,t/2.8f);Open(p);
            handTarget=shop.TransformPoint(new Vector3(3.22f,Mathf.Min(1.8f,.8f+p*3),3.3f));handWeight=1-Mathf.InverseLerp(.35f,.65f,p);
            yield return null;
        }
        Open(1);Opened=true;Stage=4;handWeight=0;sound.Stop();CameraAt(wideShot);
        var facing=(conversationPlayer?conversationPlayer.position:wideShot.position)-actor.position;facing.y=0;actor.rotation=Quaternion.LookRotation(facing);
        for(int i=1;i<d.story.shopConversation.Length&&!d.CinematicSkipRequested;i++){d.subtitle.text=d.story.shopConversation[i].text;DialogueShot(d.subtitle.text);yield return Wait(d.story.shopConversation[i].seconds);}
        d.subtitle.text="";Completed=true;
    }
    void LateUpdate(){
        if(!actor.gameObject.activeInHierarchy||!animator.isHuman)return;
        if(seated){
            foreach(var b in new[]{HumanBodyBones.LeftUpperLeg,HumanBodyBones.RightUpperLeg})animator.GetBoneTransform(b).localRotation*=Quaternion.Euler(-75,0,0);
            foreach(var b in new[]{HumanBodyBones.LeftLowerLeg,HumanBodyBones.RightLowerLeg})animator.GetBoneTransform(b).localRotation*=Quaternion.Euler(85,0,0);
            var head=animator.GetBoneTransform(HumanBodyBones.Head);head.rotation=Quaternion.AngleAxis(-20,actor.up)*head.rotation;
        }
        if(handWeight<=0)return;
        foreach(var side in new[]{HumanBodyBones.LeftUpperArm,HumanBodyBones.RightUpperArm}){
            bool left=side==HumanBodyBones.LeftUpperArm;var upper=animator.GetBoneTransform(side);var lower=animator.GetBoneTransform(left?HumanBodyBones.LeftLowerArm:HumanBodyBones.RightLowerArm);var hand=animator.GetBoneTransform(left?HumanBodyBones.LeftHand:HumanBodyBones.RightHand);
            var target=handTarget+actor.right*(left?-.2f:.2f);var delta=Quaternion.FromToRotation(hand.position-upper.position,target-upper.position);
            upper.rotation=Quaternion.Slerp(upper.rotation,delta*upper.rotation,handWeight);
            lower.rotation=Quaternion.Slerp(lower.rotation,Quaternion.FromToRotation(hand.position-lower.position,target-lower.position)*lower.rotation,handWeight);
        }
    }
}
}
