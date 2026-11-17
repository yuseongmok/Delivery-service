using UnityEngine;
namespace DeliveryService.Mok
{
    // 카메라, 팔 동작, 관중 반응, HUD만 담당한다. 판정은 PrisonMedicineDuel에 있다.
    [DefaultExecutionOrder(600)]
    public sealed class PrisonDuelPresentation : MonoBehaviour
    {
        public PrisonMedicineDuel duel;
        public Animator bossAnimator;
        public Animator[] audience;
        public PrisonTheftHands playerHands;
        public AudioClip swing,impact;
        AudioSource sound;
        public PrisonDuelAudio duelAudio;
        PrisonDuelEffects effects;
        float lean,hitPulse,glance,evadePulse;
        Quaternion rightBasis,leftBasis;
        Transform[] bossFingers;
        Quaternion[] bossFingerRest;
        public void Begin(){var hud=GetComponent<PrisonDuelHUD>();if(!hud)hud=gameObject.AddComponent<PrisonDuelHUD>();hud.duel=duel;if(duelAudio)duelAudio.Begin(duel);effects=GetComponent<PrisonDuelEffects>();if(!effects)effects=gameObject.AddComponent<PrisonDuelEffects>();effects.Initialize(duel,bossAnimator);sound=GetComponent<AudioSource>();if(!sound)sound=gameObject.AddComponent<AudioSource>();sound.playOnAwake=false;sound.spatialBlend=0;foreach(var a in audience)if(a)a.cullingMode=AnimatorCullingMode.AlwaysAnimate;bossAnimator.enabled=true;bossAnimator.cullingMode=AnimatorCullingMode.AlwaysAnimate;rightBasis=Basis(playerHands.rightHand);leftBasis=Basis(playerHands.leftHand);playerHands.gameObject.SetActive(false);if(bossFingers==null){bossFingers=System.Array.FindAll(bossAnimator.GetComponentsInChildren<Transform>(),t=>(t.name.Contains("Finger_")||t.name.Contains("Thumb_"))&&!t.name.Contains("end"));bossFingerRest=System.Array.ConvertAll(bossFingers,t=>t.localRotation);}}
        Quaternion Basis(Transform h){var child=h.childCount>0?h.GetChild(0):null;return Quaternion.Inverse(h.rotation)*Quaternion.LookRotation(child?child.position-h.position:h.up,Vector3.up);}
        public void ResetPose(){lean=hitPulse=glance=evadePulse=0;bossAnimator.enabled=true;bossAnimator.Rebind();bossAnimator.Update(0);}
        public void End(){if(duelAudio)duelAudio.Finish();if(playerHands)playerHands.gameObject.SetActive(false);if(duel.Current==PrisonMedicineDuel.Phase.Won)bossAnimator.enabled=false;}
        public void Swing(){if(duelAudio){duelAudio.Swing();return;}if(sound&&swing)sound.PlayOneShot(swing,.35f*SuppliedAudio.Volume);}
        public void Impact(bool counter){hitPulse=counter?1:.5f;if(counter&&effects)effects.Hit();if(duelAudio){duelAudio.Hit();return;}if(sound&&impact)sound.PlayOneShot(impact,(counter?.5f:.4f)*SuppliedAudio.Volume);}
        public void NearMiss(){evadePulse=1;}
        void LateUpdate()
        {
            if(!duel.Active||Time.timeScale<=0)return;
            var phase=duel.Current;
            bool fighting=phase!=PrisonMedicineDuel.Phase.Inactive;
            playerHands.gameObject.SetActive(fighting&&phase!=PrisonMedicineDuel.Phase.Lost);
            hitPulse=Mathf.MoveTowards(hitPulse,0,Time.unscaledDeltaTime*5);evadePulse=Mathf.MoveTowards(evadePulse,0,Time.unscaledDeltaTime*7);
            if(!fighting)return;
            var camera=duel.mission.camera;var spot=duel.playerSpot;
            float target=duel.VisualDodgeSide;
            lean=Mathf.Lerp(lean,target,1-Mathf.Exp(-18*Time.unscaledDeltaTime));
            float look=phase==PrisonMedicineDuel.Phase.Strike&&duel.Age>=.07f&&duel.VisualDodgeSide!=0?duel.DangerSide*20:0;glance=Mathf.Lerp(glance,look,1-Mathf.Exp(-12*Time.unscaledDeltaTime));
            Vector3 shake=new Vector3(Mathf.Sin(Time.unscaledTime*80),Mathf.Cos(Time.unscaledTime*62),0)*hitPulse*.028f+spot.right*(Mathf.Sin(Time.unscaledTime*95)*evadePulse*.009f);
            camera.transform.SetPositionAndRotation(spot.position+Vector3.up*1.6f+spot.right*lean*.24f+shake,spot.rotation*Quaternion.Euler(duel.HurtFlash*3+Mathf.Sin(Time.unscaledTime*70)*hitPulse*1.2f,glance,-lean*5+evadePulse*duel.DangerSide*.8f));
            float strike=phase==PrisonMedicineDuel.Phase.Strike?StrikeReach(duel.Age):0;
            float tell=phase==PrisonMedicineDuel.Phase.Tell?Mathf.Clamp01(duel.Age/duel.TellDuration):0;
            if(duel.ragdoll&&duel.ragdoll.Down){PoseHands(camera,0);return;}
            var root=duel.boss;root.SetPositionAndRotation(duel.bossSpot.position+duel.bossSpot.forward*(strike*.34f-duel.Punch*.2f),duel.bossSpot.rotation);
            for(int i=0;i<bossFingers.Length;i++)bossFingers[i].localRotation=bossFingerRest[i]*Quaternion.Euler(0,0,bossFingers[i].name.Contains("Thumb")?-25:-60);

            bool right=duel.DangerSide<0;
            Vector3 r=root.position+root.right*.22f+Vector3.up*1.3f+root.forward*.3f;
            Vector3 l=root.position-root.right*.22f+Vector3.up*1.3f+root.forward*.3f;
            if(right)r+=root.right*tell*.2f-root.forward*tell*.25f;else l-=root.right*tell*.2f+root.forward*tell*.25f;
            Vector3 punch=spot.position+Vector3.up*1.59f+spot.right*duel.DangerSide*.02f-spot.forward*.1f;
            float extension=phase==PrisonMedicineDuel.Phase.Strike?StrikeReach(duel.Age):0;if(right)r=Vector3.Lerp(r,punch,extension);else l=Vector3.Lerp(l,punch,extension);
            Reach(bossAnimator.GetBoneTransform(HumanBodyBones.RightUpperArm),bossAnimator.GetBoneTransform(HumanBodyBones.RightLowerArm),bossAnimator.GetBoneTransform(HumanBodyBones.RightHand),r,root.right);
            Reach(bossAnimator.GetBoneTransform(HumanBodyBones.LeftUpperArm),bossAnimator.GetBoneTransform(HumanBodyBones.LeftLowerArm),bossAnimator.GetBoneTransform(HumanBodyBones.LeftHand),l,-root.right);
            CloseFist(bossAnimator.GetBoneTransform(HumanBodyBones.RightHand),root.forward,Vector3.up);CloseFist(bossAnimator.GetBoneTransform(HumanBodyBones.LeftHand),root.forward,Vector3.up);
            var spine=bossAnimator.GetBoneTransform(HumanBodyBones.Spine);if(spine)spine.rotation=Quaternion.AngleAxis(-duel.Punch*18,root.right)*spine.rotation;
            if(phase==PrisonMedicineDuel.Phase.Won&&spine)spine.rotation=Quaternion.AngleAxis(40,root.right)*spine.rotation;
            PoseHands(camera,duel.Punch);
            for(int i=0;i<audience.Length;i++){
                var a=audience[i];if(!a||!a.isHuman)continue;
                var upper=a.GetBoneTransform(i%2==0?HumanBodyBones.RightUpperArm:HumanBodyBones.LeftUpperArm);
                if(upper)upper.rotation=Quaternion.AngleAxis(Mathf.Sin(Time.time*3+i)*12-25,a.transform.forward)*upper.rotation;
            }
        }
        void PoseHands(Camera camera,float punch)
        {
            var h=playerHands;h.transform.rotation=camera.transform.rotation;h.transform.position+=camera.transform.TransformPoint(new Vector3(.23f,-.32f,.02f))-h.rightUpper.position;
            for(int i=0;i<h.fingers.Length;i++)if(h.fingers[i])h.fingers[i].localRotation=h.fingerRest[i]*Quaternion.Euler(0,0,-65);
            float extension=punch*.5f;
            float clear=Mathf.Abs(lean);float rightDrop=clear*(duel.DangerSide>0?.3f:.17f),leftDrop=clear*(duel.DangerSide<0?.3f:.17f);
            Reach(h.rightUpper,h.rightLower,h.rightHand,camera.transform.TransformPoint(new Vector3(.24f+clear*.04f,-.1f-rightDrop,.43f+extension*1.2f-clear*.1f)),camera.transform.right);
            Reach(h.leftUpper,h.leftLower,h.leftHand,camera.transform.TransformPoint(new Vector3(-.24f-clear*.04f,-.12f-leftDrop,.43f-clear*.1f)),-camera.transform.right);
            h.rightHand.rotation=camera.transform.rotation*Quaternion.Inverse(rightBasis);h.leftHand.rotation=camera.transform.rotation*Quaternion.Inverse(leftBasis);
            CloseFist(h.rightHand,camera.transform.forward,camera.transform.up);CloseFist(h.leftHand,camera.transform.forward,camera.transform.up);
        }
        static float StrikeReach(float age){return age<.16f?Mathf.SmoothStep(0,1,age/.16f):1-Mathf.SmoothStep(0,1,(age-.16f)/.22f);}
        static void CloseFist(Transform palm,Vector3 forward,Vector3 up){foreach(var tip in palm.GetComponentsInChildren<Transform>()){if(!(tip.name.Contains("_03")||((tip.name.Contains("Finger")||tip.name.Contains("Thumb"))&&tip.childCount==0)))continue;Vector3 goal=palm.position+forward*.06f-up*.025f;for(int pass=0;pass<3;pass++)for(var joint=tip.parent;joint&&joint!=palm;joint=joint.parent)joint.rotation=Quaternion.FromToRotation(tip.position-joint.position,goal-joint.position)*joint.rotation;}}
        static void Reach(Transform u,Transform l,Transform h,Vector3 target,Vector3 outward)
        {
            if(!u||!l||!h)return;
            float a=Vector3.Distance(u.position,l.position),b=Vector3.Distance(l.position,h.position);Vector3 delta=target-u.position;float d=Mathf.Clamp(delta.magnitude,.01f,a+b-.001f);Vector3 dir=delta.normalized;
            float along=(a*a-b*b+d*d)/(2*d);Vector3 bend=Vector3.ProjectOnPlane(Vector3.down+outward*.4f,dir).normalized;
            Vector3 elbow=u.position+dir*along+bend*Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
            u.rotation=Quaternion.FromToRotation(l.position-u.position,elbow-u.position)*u.rotation;l.rotation=Quaternion.FromToRotation(h.position-l.position,target-l.position)*l.rotation;
        }
    }
}

