using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.InputSystem;
using UnityEngine;
using UnityEngine.AI;

namespace DeliveryService.Mok
{
    // 증거 발견과 간수장의 승인을 담당한다. 물건 확보와 차량 검문은 기존 미션이 담당한다.
    [DefaultExecutionOrder(2500)]
    public sealed class PrisonOfficeEvidence : MonoBehaviour
    {
        public PrisonOfficeSearch office;
        public PrisonOfficeEvidenceText dialogue;
        public Transform[] evidencePoints;
        public Transform wardenStand, mingiStand, cameraStand;
        public Transform phone;
        public int Discovered { get; private set; }
        public bool Authorized { get; private set; }
        public bool Busy { get; private set; }
        public string Subtitle { get; private set; } = "";
        public string Objective => Discovered < 3 ? "간수장실 조사 · " + dialogue.titles[Discovered] + " 확인하기" : "간수장실 · 증거로 염윤태 압박하기";
        public string Notes => office.cctv?"비리 증거: "+Discovered+"/3\n정기 호송 출발: "+(Authorized?office.cctv.dialogue.departureTime:"미확인"):"비리 증거: " + Discovered + "/3\n호송 출발 승인: " + (Authorized ? "확보" : "미확보");
        public Transform Target => Busy || Authorized ? null : Discovered < 3 ? evidencePoints[Discovered] : office.previous.warden.transform;
        PlayerCombat Player => office.previous.previous.prison.player;
        PrisonCompanion Mingi => office.previous.previous.prison.introduction.mingi;
        Camera Film => office.previous.camera;
        bool moveEnabled, lookEnabled, combatEnabled, interactionEnabled, locked;
        bool friendFollowing, friendTravel;
        readonly List<Renderer> hiddenPlayer = new List<Renderer>();
        int reading = -1;
        Vector3 phonePosition, phoneLiftStart; Quaternion phoneRotation;

        void Update()
        {
            if(Keyboard.current!=null&&Keyboard.current.fKey.wasPressedThisFrame)TryInteraction();
        }
        public bool TryInteraction()
        {
            if(!office.Active||Busy||Authorized||Time.timeScale<=0||!Player.CanAct)return false;
            var view=Player.View.transform;
            var hits=Physics.RaycastAll(view.position,view.forward,3,~0,QueryTriggerInteraction.Ignore);
            Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
            foreach(var hit in hits)
            {
                if(hit.transform.IsChildOf(Player.transform))continue;
                var point=hit.collider.GetComponentInParent<PrisonOfficeEvidencePoint>();
                return point&&point.mission==this&&Inspect(point.index);
            }
            return false;
        }

        public bool Inspect(int index)
        {
            if (!office.Active || Busy || Authorized || Time.timeScale <= 0 || !Player.CanAct || index != Discovered) return false;
            if (!Target || Vector3.Distance(Player.transform.position, Target.position) > 3) return false;
            StartCoroutine(index < 3 ? Read(index) : Confront());
            return true;
        }
        IEnumerator Read(int index)
        {
            Busy = true; Lock(); reading = index;
            yield return Aim(evidencePoints[index].position, .4f);
            PrisonAudio.Clink(evidencePoints[index].position, .06f);
            yield return Lines(index == 0 ? dialogue.ledger : index == 1 ? dialogue.transfer : dialogue.message);
            reading = -1; Discovered++;
            yield return ReturnView();
            Unlock(); Busy = false;
            office.Say(Discovered == 3 ? dialogue.evidenceReady : "");
        }
        IEnumerator Confront()
        {
            Busy = true; Lock();
            var guard = office.previous.warden;
            var npc = guard.GetComponent<NPCControl>();
            // 암전 사이에 잠에서 깬 인물을 안전한 대화 위치로 정렬한다.
            yield return FadeTo(1);
            phonePosition=phone.position;phoneRotation=phone.rotation;
            phoneLiftStart=wardenStand.position+wardenStand.forward*.32f+wardenStand.right*.22f+Vector3.up*1.0f;
            phone.position=phoneLiftStart;
            guard.gameObject.SetActive(true); guard.enabled = false;
            npc.RecoverFromKnockDown(); npc.ExternalControl = true;
            var combat=guard.GetComponent<CitizenCombat>();if(combat)combat.ExternalAttackControl=true;
            var agent = guard.GetComponent<NavMeshAgent>();
            if (agent && agent.enabled && agent.isOnNavMesh) { agent.Warp(wardenStand.position); agent.ResetPath(); agent.isStopped = true; }
            guard.transform.SetPositionAndRotation(wardenStand.position, wardenStand.rotation);
            friendFollowing = Mingi.Following; friendTravel = Mingi.ScriptedTravel;
            Mingi.Following = false; Mingi.ScriptedTravel = true;
            if (Mingi.agent.enabled && Mingi.agent.isOnNavMesh) { Mingi.agent.Warp(mingiStand.position); Mingi.agent.ResetPath(); }
            Mingi.transform.rotation = mingiStand.rotation;
            Film.transform.SetPositionAndRotation(cameraStand.position, cameraStand.rotation);
            Film.transform.LookAt(wardenStand.position+Vector3.up*1.4f);
            yield return FadeTo(0);
            if(office.cctv){
                var plan=office.cctv.dialogue;
                foreach(var line in plan.interrogation){Subtitle=plan.Format(line);yield return new WaitForSeconds(plan.lineSeconds);}
                foreach(var line in plan.restraint){Subtitle=plan.Format(line);yield return new WaitForSeconds(plan.lineSeconds);}
                yield return FadeTo(1);office.cctv.restraint.Bind();
                // 책상 모니터를 피해 의자와 얼굴이 함께 보이는 측면 구도.
                var seat=office.cctv.restraint.seat;
                Film.transform.position=seat.position+seat.right*1.8f+seat.forward*.8f+Vector3.up*1.8f;
                Film.transform.LookAt(guard.GetComponentInChildren<Animator>().GetBoneTransform(HumanBodyBones.Head));
                yield return FadeTo(0);Subtitle=plan.secured;yield return new WaitForSeconds(plan.lineSeconds);
                Authorized=true;Mingi.ScriptedTravel=friendTravel;Mingi.Following=friendFollowing;
                yield return ReturnView();Unlock();Busy=false;office.Say(plan.secured);yield break;
            }
            yield return Lines(dialogue.confrontation);
            // 휴대폰을 귀 옆으로 들어 올리고 간수장 쪽 대사만 보여 준다.
            var animator = guard.GetComponentInChildren<Animator>();
            var head = animator.GetBoneTransform(HumanBodyBones.Head);
            calling = true;
            phoneRaise=0;
            for (float t=0;t<.7f;t+=Time.deltaTime){phoneRaise=Mathf.SmoothStep(0,1,t/.7f);yield return null;}
            phoneRaise=1;
            yield return Lines(dialogue.call);
            for(float t=0;t<.5f;t+=Time.deltaTime){phoneRaise=1-Mathf.SmoothStep(0,1,t/.5f);yield return null;}
            calling = false; phone.SetPositionAndRotation(phoneLiftStart, phoneRotation);
            Authorized = true;
            Mingi.ScriptedTravel = friendTravel; Mingi.Following = friendFollowing;
            yield return ReturnView(); Unlock(); Busy = false;
            office.Say(dialogue.approved);
        }
        bool calling; float fade, phoneRaise;
        void LateUpdate()
        {
            if (!calling || Time.timeScale <= 0) return;
            var a = office.previous.warden.GetComponentInChildren<Animator>();
            var arm=a.GetBoneTransform(HumanBodyBones.RightUpperArm); var elbow=a.GetBoneTransform(HumanBodyBones.RightLowerArm); var hand=a.GetBoneTransform(HumanBodyBones.RightHand);
            var actor=office.previous.warden.transform;
            var head=a.GetBoneTransform(HumanBodyBones.Head);
            var mesh=phone.GetComponent<MeshFilter>().sharedMesh.bounds;
            var size=mesh.size;int longest=size.x>size.y?(size.x>size.z?0:2):(size.y>size.z?1:2);
            int shortest=size.x<size.y?(size.x<size.z?0:2):(size.y<size.z?1:2);
            Vector3 Axis(int i)=>i==0?Vector3.right:i==1?Vector3.up:Vector3.forward;
            var upright=Quaternion.LookRotation(actor.right,Vector3.up)*Quaternion.Inverse(Quaternion.LookRotation(Axis(shortest),Axis(longest)));
            var ear=head.position+actor.right*.15f+actor.forward*.025f;
            phone.rotation=Quaternion.Slerp(phoneRotation,upright,phoneRaise);
            var pivot=ear;
            phone.position=Vector3.Lerp(phoneLiftStart,pivot,phoneRaise);
            var target=Vector3.Lerp(phoneLiftStart,ear-Vector3.up*.055f+actor.right*.015f,phoneRaise);
            float aLength=Vector3.Distance(arm.position,elbow.position),bLength=Vector3.Distance(elbow.position,hand.position);
            var delta=target-arm.position;float d=Mathf.Clamp(delta.magnitude,.02f,aLength+bLength-.005f);var direction=delta.normalized;
            float along=(aLength*aLength-bLength*bLength+d*d)/(2*d);
            var side=Vector3.ProjectOnPlane(office.previous.warden.transform.right-Vector3.up,direction).normalized;
            var elbowTarget=arm.position+direction*along+side*Mathf.Sqrt(Mathf.Max(0,aLength*aLength-along*along));
            arm.rotation=Quaternion.FromToRotation(elbow.position-arm.position,elbowTarget-arm.position)*arm.rotation;
            elbow.rotation=Quaternion.FromToRotation(hand.position-elbow.position,target-elbow.position)*elbow.rotation;
        }
        IEnumerator Lines(string[] lines) { foreach (var line in lines) { Subtitle=line; yield return new WaitForSeconds(dialogue.lineSeconds); } }
        IEnumerator Aim(Vector3 target,float duration)
        {
            var from=Film.transform.rotation; var to=Quaternion.LookRotation(target-Film.transform.position);
            for(float t=0;t<duration;t+=Time.deltaTime){Film.transform.rotation=Quaternion.Slerp(from,to,t/duration);yield return null;}
            Film.transform.rotation=to;
        }
        IEnumerator FadeTo(float target){float from=fade;for(float t=0;t<.4f;t+=Time.deltaTime){fade=Mathf.Lerp(from,target,t/.4f);yield return null;}fade=target;}
        IEnumerator ReturnView()
        {
            var from=Film.transform.position;var rotation=Film.transform.rotation;
            for(float t=0;t<.35f;t+=Time.deltaTime){float f=Mathf.SmoothStep(0,1,t/.35f);Film.transform.SetPositionAndRotation(Vector3.Lerp(from,Player.View.transform.position,f),Quaternion.Slerp(rotation,Player.View.transform.rotation,f));yield return null;}
        }
        void Lock()
        {
            var move=Player.GetComponent<PlayerMove>();var look=Player.GetComponentInChildren<CameraMove>();var interaction=Player.GetComponent<PlayerInteraction>();
            moveEnabled=move.isControllable;lookEnabled=look.isControllable;combatEnabled=Player.enabled;interactionEnabled=interaction.enabled;locked=true;
            move.isControllable=false;look.isControllable=false;Player.enabled=false;interaction.enabled=false;
            Film.transform.SetPositionAndRotation(Player.View.transform.position,Player.View.transform.rotation);
            foreach(var r in Player.GetComponentsInChildren<Renderer>(true))if(r.enabled){hiddenPlayer.Add(r);r.enabled=false;}
            Player.View.enabled=false;Film.enabled=true;
        }
        void Unlock()
        {
            if(!locked)return;locked=false;
            foreach(var r in hiddenPlayer)if(r)r.enabled=true;hiddenPlayer.Clear();
            Player.GetComponent<PlayerMove>().isControllable=moveEnabled;
            var look=Player.GetComponentInChildren<CameraMove>();look.SyncViewFromTransform();look.isControllable=lookEnabled;
            Player.enabled=combatEnabled;Player.GetComponent<PlayerInteraction>().enabled=interactionEnabled;
            Film.enabled=false;Player.View.enabled=true;Subtitle="";
        }
        public void DeveloperComplete()
        {
            StopAllCoroutines();if(calling){phone.SetPositionAndRotation(phonePosition,phoneRotation);calling=false;}
            Unlock();reading=-1;fade=0;Busy=false;Discovered=3;Authorized=true;if(office.cctv)office.cctv.restraint.Bind();Mingi.ScriptedTravel=false;Mingi.Following=true;
        }
        void OnDisable(){if(locked)Unlock();}
        void OnGUI()
        {
            if(!Busy||Time.timeScale<=0)return;
            if(reading>=0){
                float s=Screen.height/900f;var box=new Rect(Screen.width*.57f,Screen.height*.23f,Screen.width*.37f,Screen.height*.43f);
                var old=GUI.color;GUI.color=new Color(.06f,.09f,.12f,.96f);GUI.DrawTexture(box,Texture2D.whiteTexture);GUI.color=old;
                var style=new GUIStyle(GUI.skin.label){font=MokUIFont.Font,fontSize=Mathf.RoundToInt(25*s),wordWrap=true,padding=new RectOffset(24,24,20,16)};
                style.normal.textColor=new Color(1,.88f,.66f);GUI.Label(box,dialogue.titles[reading]+"\n\n"+dialogue.documents[reading],style);
            }
            if(fade>0){var old=GUI.color;GUI.color=new Color(0,0,0,fade);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=old;}
        }
    }
}
