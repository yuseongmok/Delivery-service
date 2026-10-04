using System.Collections.Generic;
using UnityEngine;

namespace DeliveryService.Yun.ShopUI
{
    // PC and summary may overlap during a modal hand-off. Restore only after the last owner releases.
    internal sealed class ShopUiInputScope
    {
        private sealed class Suspension
        {
            public int owners;
            public bool wasEnabled;
        }

        private static readonly Dictionary<Behaviour, Suspension> suspensions = new Dictionary<Behaviour, Suspension>();
        private static int cursorOwners;
        private static CursorLockMode previousLock;
        private static bool previousVisible;
        private readonly HashSet<Behaviour> targets = new HashSet<Behaviour>();
        private bool acquired;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            suspensions.Clear();
            cursorOwners = 0;
        }

        public void Acquire(ShopPC pc, bool suspendPc)
        {
            if (acquired) return;
            acquired = true;
            if (cursorOwners++ == 0)
            {
                previousLock = Cursor.lockState;
                previousVisible = Cursor.visible;
            }
            if (pc != null)
            {
                if (suspendPc) Suspend(pc);
                // The explicitly assigned look script may be outside the camera hierarchy.
                Suspend(pc.cameraLookScript);
                if (pc.playerCamera != null)
                {
                    Transform player = pc.playerCamera;
                    while (player.parent != null && player.GetComponent<CharacterController>() == null)
                        player = player.parent;
                    foreach (MonoBehaviour behaviour in player.GetComponentsInChildren<MonoBehaviour>(true))
                        if (behaviour is PlayerMove || behaviour is PlayerMovement || behaviour is CameraMove
                            || behaviour is PlayerInteraction || behaviour is PlayerInteract)
                            Suspend(behaviour);
                }
            }
            Maintain();
        }

        private void Suspend(Behaviour target)
        {
            if (target == null || !targets.Add(target)) return;
            if (!suspensions.TryGetValue(target, out Suspension state))
            {
                state = new Suspension { wasEnabled = target.enabled };
                suspensions.Add(target, state);
            }
            state.owners++;
            target.enabled = false;
        }

        public void Maintain()
        {
            if (!acquired) return;
            foreach (Behaviour target in targets)
                if (target != null && target.enabled) target.enabled = false;
            UnlockCursor();
        }

        public void Release()
        {
            if (!acquired) return;
            acquired = false;
            foreach (Behaviour target in targets)
            {
                if (!suspensions.TryGetValue(target, out Suspension state)) continue;
                if (--state.owners == 0)
                {
                    suspensions.Remove(target);
                    if (target != null) target.enabled = state.wasEnabled;
                }
            }
            targets.Clear();
            if (--cursorOwners == 0)
            {
                Cursor.lockState = previousLock;
                Cursor.visible = previousVisible;
            }
            else UnlockCursor();
        }

        private static void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
