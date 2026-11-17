using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace DeliveryService.Mok
{
    // 결투 판정과 시간 소유권. 일반 플레이어/시민 전투 코드는 사용하지 않는다.
    public sealed class PrisonMedicineDuel : MonoBehaviour
    {
        public enum Phase { Inactive, Ready, Tell, Strike, Counter, Recover, Won, Lost }
        public PrisonMedicineMission mission;
        public PrisonDuelSettings settings;
        public Transform boss,center,playerSpot,bossSpot,wideShot,crowd;
        public PrisonDuelPresentation presentation;
        public PrisonDuelRagdoll ragdoll;
        public PrisonDuelIntroduction introduction;
        public Phase Current { get; private set; }
        public int BossHealth { get; private set; }
        public int PlayerHealth { get; private set; }
        public int Round { get; private set; }
        public int DangerSide { get; private set; }
        public int DodgeSide { get; private set; }
        public float Age { get; private set; }
        public float TellDuration { get; private set; }
        public bool Perfect { get; private set; }
        public bool Active { get; private set; }
        public bool Retrying { get; private set; }
        public string Feedback { get; private set; }
        public float HurtFlash { get; private set; }
        public float Punch { get; private set; }
        public float SlowRemaining { get; private set; }
        float originalScale=1,originalFixed=.02f,dodgePoseUntil;
        bool attempted,dodged,retryRequested,contactResolved;
        bool restoreAfterPause;
        int lastSide,sameSideCount;
        Renderer[] hidden;
        Vector3 bossHome;Quaternion bossHeading;
        public bool CanInput=>Active&&!Retrying&&Time.timeScale>0;
        public int VisualDodgeSide=>dodged&&(Current==Phase.Tell||Current==Phase.Strike)?DodgeSide:Current==Phase.Tell&&Age<dodgePoseUntil?DodgeSide:0;
        public void InitializeSearch(){boss.gameObject.SetActive(true);if(ragdoll)ragdoll.ResetBody();boss.position=bossHome;boss.rotation=bossHeading;presentation.ResetPose();crowd.gameObject.SetActive(false);}
        void Awake(){bossHome=boss.position;bossHeading=boss.rotation;}
        void Start(){boss.gameObject.SetActive(false);crowd.gameObject.SetActive(false);}
        public IEnumerator Run()
        {
            Current=Phase.Inactive;
            originalScale=Time.timeScale>0?Time.timeScale:1;originalFixed=Time.fixedDeltaTime;
            yield return mission.DuelFade(1);
            Active=true;crowd.gameObject.SetActive(true);var space=FindFirstObjectByType<PrisonMissionSpace>();if(space)space.RefreshNow();
            var p=mission.prison.player;var cc=p.GetComponent<CharacterController>();cc.enabled=false;p.transform.SetPositionAndRotation(playerSpot.position,playerSpot.rotation);cc.enabled=true;
            boss.SetPositionAndRotation(bossSpot.position,bossSpot.rotation);
            var mingi=mission.prison.introduction.mingi;mingi.Following=false;
            if(mingi.agent.enabled&&mingi.agent.isOnNavMesh&&NavMesh.SamplePosition(center.position+Vector3.left*4.2f,out var spot,1,NavMesh.AllAreas))mingi.agent.Warp(spot.position);
            mingi.transform.LookAt(new Vector3(center.position.x,mingi.transform.position.y,center.position.z));
            hidden=p.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();foreach(var r in hidden)r.enabled=false;
            mission.camera.transform.SetPositionAndRotation(wideShot.position,wideShot.rotation);
            presentation.Begin();if(introduction)introduction.Arena();yield return mission.DuelFade(0);
            foreach(var line in settings.arena){mission.DuelSay(line);yield return WaitLine();}
            if(introduction)yield return introduction.ClearRing();
            Vector3 from=mission.camera.transform.position;Quaternion rotation=mission.camera.transform.rotation;
            for(float t=0;t<1;t+=Time.deltaTime){float f=Mathf.SmoothStep(0,1,t);mission.camera.transform.SetPositionAndRotation(Vector3.Lerp(from,playerSpot.position+Vector3.up*1.6f,f),Quaternion.Slerp(rotation,playerSpot.rotation,f));if(introduction&&Vector3.Distance(mission.camera.transform.position,playerSpot.position+Vector3.up*1.6f)<1.3f)introduction.playerActor.gameObject.SetActive(false);yield return null;}
            if(introduction)introduction.playerActor.gameObject.SetActive(false);mission.DuelSay("");ResetRound();
            while(Current!=Phase.Won){
                if(Current==Phase.Lost&&retryRequested){
                    retryRequested=false;Retrying=true;RestoreSpeed();yield return mission.DuelFade(1);ResetRound();yield return mission.DuelFade(0);Retrying=false;
                }
                yield return null;
            }
            RestoreSpeed();yield return new WaitForSeconds(.35f);if(ragdoll)ragdoll.Launch(playerSpot.forward);yield return new WaitForSeconds(1.8f);boss.gameObject.SetActive(false);
            mission.DuelSay(settings.won);yield return WaitLine();
            mission.medicine.position=bossSpot.position+bossSpot.forward*.75f+Vector3.up*.12f;
            FinishControls();mission.DuelSay("");
        }
        IEnumerator WaitLine(){for(float t=0;t<settings.lineSeconds;t+=Time.deltaTime){if(Time.timeScale>0&&Keyboard.current!=null&&Keyboard.current.spaceKey.wasPressedThisFrame)break;yield return null;}}
        void ResetRound(){BossHealth=settings.bossHealth;PlayerHealth=settings.playerHealth;Round=0;DodgeSide=0;attempted=dodged=Perfect=false;HurtFlash=Punch=0;Feedback=settings.tutorial;Current=Phase.Ready;Age=0;presentation.ResetPose();}
        void NextAttack()
        {
            Round++;int side=Random.value<.5f?-1:1;if(sameSideCount>=2)side=-lastSide;sameSideCount=side==lastSide?sameSideCount+1:1;lastSide=side;DangerSide=side;
            TellDuration=Round<=2?settings.teachingTell:BossHealth<=settings.bossHealth/2?settings.fastTell:settings.normalTell;
            attempted=dodged=Perfect=false;contactResolved=false;DodgeSide=0;Current=Phase.Tell;Age=0;Feedback="";
        }
        void Update()
        {
            if(restoreAfterPause&&Time.timeScale>0){Time.timeScale=originalScale;Time.fixedDeltaTime=originalFixed;restoreAfterPause=false;}
            if(!Active||Time.timeScale<=0)return;
            if(SlowRemaining>0){SlowRemaining=Mathf.Max(0,SlowRemaining-Time.unscaledDeltaTime);if(SlowRemaining==0)RestoreSpeed();}
            HurtFlash=Mathf.MoveTowards(HurtFlash,0,Time.unscaledDeltaTime*2);Punch=Mathf.MoveTowards(Punch,0,Time.unscaledDeltaTime*4);
            if(Retrying)return;
            Age+=Time.deltaTime;
            var keyboard=Keyboard.current;
            if(Current==Phase.Lost){if(keyboard!=null&&keyboard.enterKey.wasPressedThisFrame)retryRequested=true;return;}
            if(Current==Phase.Ready){if(Age>=2)NextAttack();return;}
            bool q=keyboard!=null&&keyboard.qKey.wasPressedThisFrame,e=keyboard!=null&&keyboard.rKey.wasPressedThisFrame;
            if(Current==Phase.Tell&&(q||e))TryDodge(q==e?0:q?-1:1);
            // 회피 성공 뒤 자동 반격. 마우스 연타로 판정을 건너뛸 수 없다.
            if(Current==Phase.Tell&&Age>=TellDuration){
                Current=Phase.Strike;Age=0;presentation.Swing();
            }
            // 먼저 주먹을 뻗고, 얼굴 옆을 지나는 구간에만 극저속을 건다.
            if(Current==Phase.Strike&&!contactResolved&&Age>=.115f){
                contactResolved=true;
                if(!dodged){PlayerHealth=Mathf.Max(0,PlayerHealth-settings.enemyDamage);HurtFlash=1;Feedback=settings.hit;presentation.Impact(false);}
                else {Age=.115f;SlowRemaining=settings.slowSeconds;Time.timeScale=originalScale*settings.slowScale;Time.fixedDeltaTime=originalFixed*settings.slowScale;Feedback=Perfect?settings.perfect:"회피 성공!";presentation.NearMiss();}
            }
            if(Current==Phase.Strike&&Age>=.38f){if(PlayerHealth==0){Current=Phase.Lost;RestoreSpeed();}else{Current=dodged?Phase.Counter:Phase.Recover;if(dodged)presentation.Swing();}Age=0;}
            if(Current==Phase.Counter){Punch=Mathf.SmoothStep(0,1,Age/.2f);if(Age>=.2f)TryCounter();}
            if(Current==Phase.Recover&&Age>=.65f)NextAttack();
        }
        public void TryDodge(int side)
        {
            if(!CanInput||Current!=Phase.Tell||attempted)return;
            attempted=true;DodgeSide=side;
            dodgePoseUntil=Age+.18f;
            float left=TellDuration-Age;
            dodged=side==-DangerSide&&left<=settings.dodgeWindow&&left>=0;
            Perfect=dodged&&left<=settings.perfectWindow;
            Feedback=side!=-DangerSide?settings.wrong:left>settings.dodgeWindow?settings.early:"회피!";
        }
        public void TryCounter()
        {
            if(!CanInput)return;
            if(Current!=Phase.Counter||Age<.2f)return;
            BossHealth=Mathf.Max(0,BossHealth-(Perfect?settings.perfectDamage:settings.counterDamage));Punch=1;presentation.Impact(true);RestoreSpeed();
            Current=BossHealth==0?Phase.Won:Phase.Recover;Age=0;Feedback=Perfect?"정확한 반격!":"반격 성공!";
        }
        public void RestoreSpeed(){SlowRemaining=0;restoreAfterPause=Time.timeScale<=0;if(!restoreAfterPause)Time.timeScale=originalScale;Time.fixedDeltaTime=originalFixed;}
        void FinishControls(){RestoreSpeed();Active=false;presentation.End();if(introduction)introduction.Finish();if(hidden!=null){foreach(var r in hidden)if(r)r.enabled=true;hidden=null;}mission.prison.introduction.mingi.Following=true;}
        public void Cancel(){StopAllCoroutines();FinishControls();if(presentation.duelAudio)presentation.duelAudio.StopImmediate();Current=Phase.Inactive;crowd.gameObject.SetActive(false);boss.gameObject.SetActive(false);}
        void OnDisable(){if(Active)FinishControls();}
    }
}








