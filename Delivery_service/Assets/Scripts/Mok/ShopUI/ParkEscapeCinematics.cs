using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace DeliveryService.Mok
{
    // 체포 카메라, 경찰 통화 자세와 경찰차 경광등·사이렌을 담당한다.
    internal sealed class ParkEscapeCinematics
    {
        private readonly ParkEscapeSequence sequence;
        public ParkEscapeCinematics(ParkEscapeSequence sequence)
        {
            this.sequence = sequence;
        }

        private VenueFinale M => sequence.chapter.mission;
        private Animator officerAnimator;
        private AudioSource siren;
        private Light red;
        private Light blue;
        private Quaternion[] officerRest;
        private Transform[] officerBones;
        private Vector3 officerOrigin;
        private Quaternion officerRotation;
        public void Initialize()
        {
            officerAnimator = sequence.officer.GetComponentInChildren<Animator>();
            officerOrigin = sequence.officer.position;
            officerRotation = sequence.officer.rotation;
            siren = sequence.policeCar.gameObject.AddComponent<AudioSource>();
            siren.playOnAwake = false;
            siren.loop = true;
            siren.spatialBlend = .65f;
            siren.volume = .25f;
            siren.clip = SuppliedAudio.Current ? SuppliedAudio.Current.policeSiren : null;
            StorySoundPause.Attach(siren);
            var lights = sequence.policeCar.GetComponentsInChildren<Light>(true);
            if (lights.Length > 1)
            {
                red = lights[0];
                blue = lights[1];
            }
        }

        public void PlaySiren()
        {
            if (siren.clip)
            {
                siren.Play();
            }
        }

        public void StopSiren()
        {
            if (siren)
            {
                siren.Stop();
            }
        }

        public void QuietSiren()
        {
            siren.volume = .08f;
        }

        public void Reset()
        {
            RestoreOfficer();
            if (siren)
            {
                siren.Stop();
                siren.volume = .25f;
            }

            if (sequence.officer)
            {
                sequence.officer.SetPositionAndRotation(officerOrigin, officerRotation);
                SetOfficerSpeed(0);
            }
        }

        public void AimCamera(Vector3 at)
        {
            var d = at - M.cinema.transform.position;
            if (d.sqrMagnitude > .01f)
            {
                M.cinema.transform.rotation = Quaternion.Slerp(M.cinema.transform.rotation, Quaternion.LookRotation(d), Time.deltaTime * 3);
            }
        }

        public void CloseOfficerCamera()
        {
            M.cinema.transform.position = Vector3.Lerp(M.cinema.transform.position, sequence.officer.position + sequence.officer.forward * 2.7f + sequence.officer.right * .45f + Vector3.up * 1.55f, Time.deltaTime * 2);
            AimCamera(sequence.officer.position + Vector3.up * 1.5f);
        }

        public static void Face(Transform t, Vector3 at)
        {
            var d = Vector3.ProjectOnPlane(at - t.position, Vector3.up);
            if (d.sqrMagnitude > .01f)
            {
                t.rotation = Quaternion.RotateTowards(t.rotation, Quaternion.LookRotation(d), 180 * Time.deltaTime);
            }
        }

        public void MoveOfficer(Vector3 goal, float speed)
        {
            Face(sequence.officer, goal);
            sequence.officer.position = Vector3.MoveTowards(sequence.officer.position, goal, speed * Time.deltaTime);
            SetOfficerSpeed(Vector3.Distance(sequence.officer.position, goal) > .1f ? speed : 0);
        }

        public static void SetSpeed(Animator a, float value)
        {
            if (!a)
            {
                return;
            }

            foreach (var p in a.parameters)
            {
                if (p.name == "MoveSpeed" && p.type == AnimatorControllerParameterType.Float)
                {
                    a.SetFloat(p.name, value);
                }

                if (p.name == "IsWalking" && p.type == AnimatorControllerParameterType.Bool)
                {
                    a.SetBool(p.name, value > .1f);
                }
            }
        }

        public void FlashLights()
        {
            if (red)
            {
                red.enabled = Mathf.Repeat(Time.time, .6f) < .3f;
            }

            if (blue)
            {
                blue.enabled = !red.enabled;
            }
        }

        public void RestoreOfficer()
        {
            if (officerBones != null)
            {
                for (int i = 0; i < officerBones.Length; i++)
                {
                    if (officerBones[i])
                    {
                        officerBones[i].localRotation = officerRest[i];
                    }
                }

                officerBones = null;
            }
        }

        public void SetOfficerSpeed(float speed) => SetSpeed(officerAnimator, speed);
        public void TickPose()
        {
            if (Time.timeScale <= 0)
            {
                return;
            }

            if (sequence.policeArrival)
            {
                sequence.policeArrival.AimWeapons = sequence.Current >= ParkEscapeSequence.Beat.Arrival && sequence.Current <= ParkEscapeSequence.Beat.Custody;
                sequence.policeArrival.SpeakerBusy = sequence.Current == ParkEscapeSequence.Beat.Call;
            }

            if (sequence.Current != ParkEscapeSequence.Beat.Call)
            {
                sequence.phoneProp.gameObject.SetActive(false);
                return;
            }

            bool calling = sequence.Elapsed >= sequence.lineSeconds * .8f;
            if (!officerAnimator || !officerAnimator.isHuman)
            {
                return;
            }

            var hand = officerAnimator.GetBoneTransform(HumanBodyBones.RightHand);
            var head = officerAnimator.GetBoneTransform(HumanBodyBones.Head);
            var upper = officerAnimator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            var lower = officerAnimator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            if (officerBones == null)
            {
                officerBones = new[]{upper, lower, hand};
                officerRest = officerBones.Select(b => b.localRotation).ToArray();
            }

            float blend = Mathf.SmoothStep(0, 1, Mathf.Clamp01((sequence.Elapsed - sequence.lineSeconds * .8f) / .8f));
            for (int i = 0; i < officerBones.Length; i++)
            {
                officerBones[i].localRotation = officerRest[i];
            }

            var target = head.position + sequence.officer.right * .13f - sequence.officer.forward * .02f;
            var elbow = upper.position + sequence.officer.forward * .28f - Vector3.up * .2f;
            upper.rotation = Quaternion.Slerp(upper.rotation, Quaternion.FromToRotation(lower.position - upper.position, elbow - upper.position) * upper.rotation, blend);
            lower.rotation = Quaternion.Slerp(lower.rotation, Quaternion.FromToRotation(hand.position - lower.position, target - lower.position) * lower.rotation, blend);
            sequence.phoneProp.gameObject.SetActive(calling);
            sequence.phoneProp.SetPositionAndRotation(hand.position, sequence.officer.rotation * Quaternion.Euler(0, 0, 90));
        }
    }
}
