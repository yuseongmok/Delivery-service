using UnityEngine;

namespace DeliveryService.Mok
{
    // The body follows the handle arc so the arm never has to stretch across the door.
    public sealed class AftermathHideout : MonoBehaviour
    {
        public AftermathActor mingi;
        public AftermathCabinetSteps steps;
        public Transform cabinetDoor, doorGrip, bag, leftGrip, rightGrip;
        public Vector3 hiddenBag, recoveredBag;
        public Vector3 standingPosition, handleBodyOffset;
        public int arrivalShot = 1, recoveryShot = 2, decisionShot = 3;
        public float openAngle = -95;

        public void Present(int shot, float p)
        {
            if (shot == arrivalShot)
            {
                cabinetDoor.localRotation = Quaternion.identity;
                if (openAngle > 0) { var contact = doorGrip.localPosition; contact.z = -.10f; doorGrip.localPosition = contact; }
                bag.rotation = Quaternion.identity;
                bag.position = hiddenBag;
                if (steps) { steps.active = true; steps.leftProgress = steps.rightProgress = steps.leftLift = steps.rightLift = 0; }
                mingi.reach = 0;
                mingi.forwardLean = 0;
                mingi.rightHandRoll = 0;
                return;
            }
            if (shot == recoveryShot)
            {
                bag.rotation = Quaternion.identity;
                float opening = Smooth(.15f, .43f, p);
                cabinetDoor.localRotation = Quaternion.Euler(0, openAngle * opening, 0);
                if (openAngle > 0)
                {
                    var contact = doorGrip.localPosition;
                    contact.z = Mathf.Lerp(-.10f, .10f, Smooth(.25f, .35f, p));
                    doorGrip.localPosition = contact;
                }
                float rightOut = Smooth(.15f, .23f, p), leftOut = Smooth(.23f, .31f, p);
                float leftBack = Smooth(.55f, .62f, p), rightBack = Smooth(.62f, .69f, p);
                if (steps)
                {
                    steps.active = true;
                    steps.rightProgress = rightOut - rightBack;
                    steps.leftProgress = leftOut - leftBack;
                    steps.rightLift = Lift(.15f,.23f,p) + Lift(.62f,.69f,p);
                    steps.leftLift = Lift(.23f,.31f,p) + Lift(.55f,.62f,p);
                    mingi.transform.position = standingPosition + steps.stepOffset * (steps.leftProgress + steps.rightProgress) * .5f;
                }
                mingi.walkSpeed = 0;
                if (openAngle > 0)
                {
                    // Turn toward the pulling hand while the planted-foot component preserves the steps.
                    float turn = Smooth(.15f, .45f, p) * (1 - Smooth(.47f, .69f, p));
                    mingi.transform.rotation = Quaternion.Euler(0, 38 - 15 * turn, 0);
                }
                float doorLean = Smooth(.15f, .40f, p) * (1 - Smooth(.47f, .57f, p));
                if (openAngle < 0) mingi.transform.rotation = Quaternion.Euler(0, Mathf.Lerp(38, 90, doorLean), 0);
                mingi.forwardLean = Mathf.Max(32 * doorLean, 30 * Smooth(.52f, .65f, p) * (1 - Smooth(.74f, .96f, p)));
                if (p < .54f)
                {
                    // Use the near-hinge arm rather than reaching across the torso as the door opens.
                    mingi.rightContact = doorGrip;
                    mingi.rightHandRoll = 0;
                    mingi.leftContact = null;
                    mingi.reach = Smooth(0, .13f, p) * (1 - Smooth(.47f, .54f, p));
                }
                else
                {
                    mingi.rightHandRoll = 0;
                    mingi.leftContact = leftGrip;
                    mingi.rightContact = rightGrip;
                    mingi.reach = Smooth(.54f, .65f, p);
                }
                // Settle both hands first, clear the shelf vertically, then draw the weight back.
                Vector3 lifted = hiddenBag + Vector3.up * .09f;
                bag.position = Vector3.Lerp(Vector3.Lerp(hiddenBag, lifted, Smooth(.73f, .79f, p)), recoveredBag, Smooth(.79f, .98f, p));
            }
            if (shot == decisionShot)
            {
                mingi.forwardLean = 0;
                if (steps) steps.active = false;
                cabinetDoor.localRotation = Quaternion.Euler(0, openAngle, 0);
                var viewer = GetComponent<AftermathFirstPerson>().hideoutPlayer.transform;
                var direction = viewer.position - mingi.transform.position; direction.y = 0;
                float angle = Vector3.SignedAngle(Quaternion.Euler(0,38,0) * Vector3.forward, direction, Vector3.up);
                var turn = Quaternion.Euler(0, angle * Smooth(0, .55f, p), 0);
                mingi.transform.rotation = turn * Quaternion.Euler(0, 38, 0);
                bag.position = mingi.transform.position + turn * (recoveredBag - mingi.transform.position);
                bag.rotation = turn;
                mingi.rightHandRoll = 0;
                    mingi.leftContact = leftGrip;
                mingi.rightContact = rightGrip;
                mingi.reach = 1;
                mingi.walkSpeed = 0;
            }
        }

        static float Lift(float from, float to, float p)
        {
            if (p <= from || p >= to) return 0;
            return Mathf.Sin(Mathf.PI * Mathf.InverseLerp(from, to, p)) * .035f;
        }

        static float Smooth(float from, float to, float p)
        {
            return Mathf.SmoothStep(0, 1, Mathf.InverseLerp(from, to, p));
        }
    }
}
