using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using Phase = DeliveryService.Mok.VenueFinale.Phase;

namespace DeliveryService.Mok
{
    // 입력 잠금과 복원, 카메라 및 승하차 연출을 담당한다.
    internal sealed class VenueFinaleCinematics
    {
        private readonly VenueFinale mission;
        public VenueFinaleCinematics(VenueFinale mission)
        {
            this.mission = mission;
        }

        private bool locked;
        private bool oldMove;
        private bool oldControl;
        private bool oldLook;
        private Camera gameplay;
        private PlayerMove move;
        private CameraMove look;
        private Renderer[] hiddenRenderers;
        private bool[] hiddenStates;
        private PhoneController lockedPhone;
        private bool phoneWasEnabled;
        private bool phoneWasVisible;
        private GameObject hiddenPhone;
        private Vector3 exitViewStart;
        private Vector3 boardingViewStart;
        private Quaternion exitViewRotation;
        private Quaternion boardingViewRotation;
        private float exitNear;
        private float boardingNear;
        private Transform ejectGuard;
        private Quaternion ejectView;
        public bool IsLocked => locked;
        public void CaptureBoardingView()
        {
            boardingViewStart = mission.Player.View.transform.position;
            boardingViewRotation = mission.Player.View.transform.rotation;
            boardingNear = mission.cinema.nearClipPlane;
        }

        public void CaptureExitView()
        {
            exitViewStart = mission.cinema.transform.position;
            exitViewRotation = mission.cinema.transform.rotation;
            exitNear = mission.cinema.nearClipPlane;
        }

        public void RestoreBoardingClip()
        {
            mission.cinema.nearClipPlane = boardingNear;
        }

        public void RestoreExitClip()
        {
            mission.cinema.nearClipPlane = exitNear;
        }

        public void PositionBoardingCamera()
        {
            mission.cinema.transform.SetPositionAndRotation(boardingViewStart, boardingViewRotation);
        }

        public void HidePlayer()
        {
            hiddenRenderers = mission.Player.GetComponentsInChildren<Renderer>(true);
            hiddenStates = hiddenRenderers.Select(r => r.forceRenderingOff).ToArray();
            foreach (var renderer in hiddenRenderers)
            {
                renderer.forceRenderingOff = true;
            }
        }

        public void Lock()
        {
            if (locked)
            {
                return;
            }

            locked = true;
            lockedPhone = Object.FindFirstObjectByType<PhoneController>();
            if (lockedPhone)
            {
                lockedPhone.ClosePhone();
                if (lockedPhone.VisualRoot)
                {
                    hiddenPhone = lockedPhone.VisualRoot.gameObject;
                    phoneWasVisible = hiddenPhone.activeSelf;
                    hiddenPhone.SetActive(false);
                }

                phoneWasEnabled = lockedPhone.enabled;
                lockedPhone.enabled = false;
            }

            move = mission.Player.GetComponent<PlayerMove>();
            gameplay = mission.Player.View;
            look = gameplay.GetComponent<CameraMove>();
            oldMove = move.enabled;
            oldControl = move.isControllable;
            oldLook = look && look.enabled;
            move.enabled = false;
            move.isControllable = false;
            if (look)
            {
                look.enabled = false;
            }

            gameplay.enabled = false;
            mission.cinema.enabled = true;
        }

        public void Release()
        {
            if (mission.getaway)
            {
                mission.getaway.Stop();
            }

            if (hiddenRenderers != null)
            {
                for (int i = 0; i < hiddenRenderers.Length; i++)
                {
                    if (hiddenRenderers[i])
                    {
                        hiddenRenderers[i].forceRenderingOff = hiddenStates[i];
                    }
                }

                hiddenRenderers = null;
            }

            if (!locked)
            {
                return;
            }

            if (mission.State == Phase.Boarding)
            {
                mission.cinema.nearClipPlane = boardingNear;
            }

            if (mission.State == Phase.Abandoning)
            {
                mission.cinema.nearClipPlane = exitNear;
            }

            locked = false;
            if (hiddenPhone)
            {
                hiddenPhone.SetActive(phoneWasVisible);
                hiddenPhone = null;
            }

            if (lockedPhone)
            {
                lockedPhone.enabled = phoneWasEnabled;
            }

            if (mission.cinema)
            {
                mission.cinema.enabled = false;
            }

            if (gameplay)
            {
                gameplay.enabled = true;
            }

            if (move)
            {
                move.enabled = oldMove;
                move.isControllable = oldControl;
            }

            if (look)
            {
                look.enabled = oldLook;
                look.SyncViewFromTransform();
            }

            if (mission.fade)
            {
                mission.fade.gameObject.SetActive(false);
            }
        }

        public void Shot(Vector3 position, Vector3 at)
        {
            mission.cinema.transform.position = position;
            mission.cinema.transform.LookAt(at);
        }

        public void PlacePlayer(Transform anchor)
        {
            var cc = mission.Player.GetComponent<CharacterController>();
            bool enabled = cc.enabled;
            cc.enabled = false;
            mission.Player.transform.SetPositionAndRotation(anchor.position + Vector3.up * (cc.height * .5f - cc.center.y + .08f), anchor.rotation);
            mission.Player.View.transform.localRotation = Quaternion.identity;
            cc.enabled = enabled;
            mission.Player.View.GetComponent<CameraMove>()?.SyncViewFromTransform();
            Physics.SyncTransforms();
        }

        public void StoryControl(bool cinematic)
        {
            if (cinematic)
            {
                Lock();
                if (hiddenRenderers == null)
                {
                    hiddenRenderers = mission.Player.GetComponentsInChildren<Renderer>(true);
                    hiddenStates = hiddenRenderers.Select(r => r.forceRenderingOff).ToArray();
                    foreach (var r in hiddenRenderers)
                    {
                        r.forceRenderingOff = true;
                    }
                }
            }
            else
            {
                Release();
            }
        }

        public static void Sit(Transform actor)
        {
            var a = actor.GetComponentInChildren<Animator>();
            if (!a || !a.isHuman)
            {
                return;
            }

            foreach (var b in new[]{HumanBodyBones.LeftUpperLeg, HumanBodyBones.RightUpperLeg})
            {
                var t = a.GetBoneTransform(b);
                if (t)
                {
                    t.localRotation *= Quaternion.Euler(-75, 0, 0);
                }
            }

            foreach (var b in new[]{HumanBodyBones.LeftLowerLeg, HumanBodyBones.RightLowerLeg})
            {
                var t = a.GetBoneTransform(b);
                if (t)
                {
                    t.localRotation *= Quaternion.Euler(85, 0, 0);
                }
            }
        }

        public void BeginEjection(Transform caughtBy)
        {
            var viewPosition = mission.Player.View.transform.position;
            ejectView = mission.Player.View.transform.rotation;
            ejectGuard = caughtBy ? caughtBy : mission.stealth.guards.Where(g => g && !g.Incapacitated && g.gameObject.activeInHierarchy).OrderBy(g => (g.transform.position - mission.Player.transform.position).sqrMagnitude).Select(g => g.transform).FirstOrDefault();
            Lock();
            mission.cinema.transform.SetPositionAndRotation(viewPosition, ejectView);
            foreach (var guard in mission.stealth.guards)
            {
                var agent = guard ? guard.GetComponent<UnityEngine.AI.NavMeshAgent>() : null;
                if (agent && agent.enabled && agent.isOnNavMesh)
                {
                    agent.isStopped = true;
                }
            }

            if (ejectGuard)
            {
                var direction = mission.Player.transform.position - ejectGuard.position;
                direction.y = 0;
                if (direction.sqrMagnitude > .01f)
                {
                    ejectGuard.rotation = Quaternion.LookRotation(direction);
                }
            }
        }

        public void Tick(float elapsed)
        {
            switch (mission.State)
            {
                case Phase.Boarding:
                    UpdateBoarding(elapsed);
                    break;
                case Phase.Debrief:
                    UpdateDebrief(elapsed);
                    break;
                case Phase.Ejected:
                    UpdateEjection(elapsed);
                    break;
                case Phase.Abandoning:
                    UpdateExit(elapsed);
                    break;
            }
        }

        private void UpdateBoarding(float elapsed)
        {
            float t = Mathf.Clamp01(elapsed / mission.boardingSeconds);
            float open = Mathf.SmoothStep(0, 1, t / .23f) * (1 - Mathf.SmoothStep(0, 1, (t - .78f) / .22f));
            mission.door.localRotation = Quaternion.Euler(0, 70 * open, 0);
            var doorway = mission.car.TransformPoint(new Vector3(-1.05f, 1.12f, .16f));
            var eye = mission.car.TransformPoint(mission.getaway.eye);
            Vector3 pos = t < .48f ? Vector3.Lerp(boardingViewStart, doorway, Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.18f, .48f, t))) : Vector3.Lerp(doorway, eye, Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.48f, .83f, t)));
            pos.y -= Mathf.Sin(Mathf.PI * Mathf.Clamp01(Mathf.InverseLerp(.25f, .83f, t))) * .1f;
            mission.cinema.transform.position = pos;
            var doorLook = Quaternion.LookRotation(mission.car.TransformPoint(new Vector3(-.6f, 1, .6f)) - boardingViewStart);
            mission.cinema.transform.rotation = t < .4f ? Quaternion.Slerp(boardingViewRotation, doorLook, Mathf.SmoothStep(0, 1, t / .4f)) : Quaternion.Slerp(doorLook, mission.car.rotation, Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.4f, .9f, t)));
        }

        private void UpdateDebrief(float elapsed)
        {
            mission.fade.gameObject.SetActive(elapsed < 1.3f);
            mission.fade.color = new Color(0, 0, 0, 1 - Mathf.InverseLerp(.35f, 1.3f, elapsed));
            mission.subtitle.text = elapsed < 1.3f ? "" : mission.closing[Mathf.Min(mission.closing.Length - 1, Mathf.FloorToInt((elapsed - 1.3f) / 4.8f))];
        }

        private void UpdateEjection(float elapsed)
        {
            if (ejectGuard)
            {
                var direction = ejectGuard.position + Vector3.up * 1.5f - mission.cinema.transform.position;
                if (direction.sqrMagnitude > .01f)
                {
                    mission.cinema.transform.rotation = Quaternion.Slerp(ejectView, Quaternion.LookRotation(direction), Mathf.SmoothStep(0, 1, elapsed / .3f));
                }
            }

            mission.fade.gameObject.SetActive(true);
            mission.fade.color = new Color(0, 0, 0, Mathf.InverseLerp(.45f, 1.1f, elapsed));
        }

        private void UpdateExit(float elapsed)
        {
            float t = mission.ExitProgress;
            float open = Mathf.SmoothStep(0, 1, t / .22f) * (1 - Mathf.SmoothStep(0, 1, (t - .78f) / .18f));
            mission.door.localRotation = Quaternion.Euler(0, 70 * open, 0);
            var doorway = mission.car.TransformPoint(new Vector3(-1.05f, 1.12f, .16f));
            var outside = mission.car.TransformPoint(new Vector3(-1.85f, 1.65f, .16f));
            var position = t < .5f ? Vector3.Lerp(exitViewStart, doorway, Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.18f, .5f, t))) : Vector3.Lerp(doorway, outside, Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.5f, .78f, t)));
            mission.cinema.transform.position = position;
            var doorLook = mission.car.rotation;
            var outLook = mission.car.rotation * Quaternion.Euler(0, -90, 0);
            mission.cinema.transform.rotation = t < .5f ? Quaternion.Slerp(exitViewRotation, doorLook, Mathf.SmoothStep(0, 1, t / .5f)) : Quaternion.Slerp(doorLook, outLook, Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.62f, .85f, t)));
            mission.fade.gameObject.SetActive(t > .88f);
            mission.fade.color = new Color(0, 0, 0, Mathf.InverseLerp(.88f, 1, t));
        }

        public void PrepareHomecoming()
        {
            var mingi = mission.preparation.preparation.delivery.kitchen.actor;
            var stand = mission.preparation.preparation.delivery.returnPoint;
            var agent = mingi.GetComponent<NavMeshAgent>();
            if (agent)
            {
                agent.enabled = false;
            }

            var body = mingi.GetComponent<CharacterController>();
            bool bodyEnabled = body && body.enabled;
            if (body)
            {
                body.enabled = false;
            }

            mingi.position = stand.position;
            var toward = mission.Player.transform.position - mingi.position;
            toward.y = 0;
            if (toward.sqrMagnitude > .01f)
            {
                mingi.rotation = Quaternion.LookRotation(toward);
            }

            mingi.gameObject.SetActive(true);
            if (body)
            {
                body.enabled = bodyEnabled;
            }

            var anim = mingi.GetComponentInChildren<Animator>();
            if (anim)
            {
                anim.enabled = true;
                foreach (var parameter in anim.parameters)
                {
                    if (parameter.type == AnimatorControllerParameterType.Float && parameter.name == "MoveSpeed")
                    {
                        anim.SetFloat(parameter.name, 0);
                    }

                    if (parameter.type == AnimatorControllerParameterType.Bool && parameter.name == "IsWalking")
                    {
                        anim.SetBool(parameter.name, false);
                    }
                }
            }

            var aim = mingi.position + Vector3.up * 1.45f;
            var facing = aim - mission.Player.View.transform.position;
            var flat = facing;
            flat.y = 0;
            if (flat.sqrMagnitude > .01f)
            {
                mission.Player.transform.rotation = Quaternion.LookRotation(flat);
            }

            mission.Player.View.transform.rotation = Quaternion.LookRotation(aim - mission.Player.View.transform.position);
            mission.cinema.transform.SetPositionAndRotation(mission.Player.View.transform.position, mission.Player.View.transform.rotation);
            mission.subtitle.text = "";
            mission.fade.gameObject.SetActive(true);
            mission.fade.color = Color.black;
        }

        public void UpdateActors(float elapsed)
        {
            if (!mission.Running)
            {
                return;
            }

            if (mission.State == Phase.Entrance || mission.State == Phase.Deposit)
            {
                mission.pizza.SetActive(!mission.Player.RiddenBike);
                mission.pizza.transform.SetPositionAndRotation(mission.Player.View.transform.TransformPoint(new Vector3(-.27f, -.35f, .65f)), mission.Player.View.transform.rotation);
            }

            if (mission.State == Phase.Exposed)
            {
                var d = mission.Player.transform.position - mission.target.position;
                d.y = 0;
                if (d.sqrMagnitude > .1f)
                {
                    mission.target.rotation = Quaternion.RotateTowards(mission.target.rotation, Quaternion.LookRotation(d), 120 * Time.deltaTime);
                }
            }

            if (mission.State == Phase.Driving || (mission.State == Phase.Boarding && elapsed > mission.boardingSeconds * .7f))
            {
                Sit(mission.passenger);
            }
        }
    }
}
